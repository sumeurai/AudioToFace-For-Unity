#ifndef SUMERUAI_CHARACTER_LIGHTING_INCLUDED
#define SUMERUAI_CHARACTER_LIGHTING_INCLUDED

// URP sets these globally. Built-in leaves them at zero; XandraCoreRpSetup
// then selects Built-in _LightColor0 via _AtfUseUrpLight.
float _AtfUseUrpLight;
float4 _MainLightPosition;
half4 _MainLightColor;
float4 _AtfExtraLightDir0;
half4 _AtfExtraLightColor0;
float4 _AtfExtraLightDir1;
half4 _AtfExtraLightColor1;

void AtfGetMainLight(out half3 lightDir, out half3 lightCol)
{
    if (_AtfUseUrpLight > 0.5)
    {
        lightDir = _MainLightPosition.xyz;
        lightCol = _MainLightColor.rgb;
        half len2 = dot(lightDir, lightDir);
        lightDir = len2 > 1e-8 ? lightDir * rsqrt(len2) : half3(0, 1, 0);
    }
    else
    {
        lightDir = normalize(_WorldSpaceLightPos0.xyz);
        lightCol = _LightColor0.rgb;
    }
}

half AtfFresnel(half3 N, half3 V, half f0)
{
    return f0 + (1.0 - f0) * pow(1.0 - saturate(dot(N, V)), 5);
}

void AtfAccumLambert(half3 N, half3 V, half3 L, half3 lightCol, half smoothness, half metallic, half skin, inout half3 diffuse, inout half3 specular)
{
    half col2 = dot(lightCol, lightCol);
    half dir2 = dot(L, L);
    if (col2 < 1e-10 || dir2 < 1e-10)
    {
        return;
    }

    L = L * rsqrt(dir2);
    half3 H = normalize(L + V);
    half ndotl = saturate(dot(N, L));
    half ndoth = saturate(dot(N, H));

    if (skin > 0.5)
    {
        const half wrap = 0.32;
        half wrapped = saturate((dot(N, L) + wrap) / (1.0 + wrap));
        half scatterMask = saturate(wrapped * 1.15 - ndotl);
        half3 sss = half3(1.0, 0.36, 0.26) * scatterMask;
        half trans = pow(saturate(dot(V, -L + N * 0.2)), 2.5);
        half3 transCol = half3(0.9, 0.22, 0.1) * trans * 0.28;
        diffuse += lightCol * (wrapped + sss * 0.9 + transCol);

        half fresnel = AtfFresnel(N, V, 0.028);
        half specBroad = pow(ndoth, lerp(10, 32, smoothness)) * 0.08;
        half specTight = pow(ndoth, lerp(48, 160, smoothness)) * smoothness * 0.28;
        half rim = pow(1.0 - saturate(dot(N, V)), 3.5) * smoothness;
        specular += lightCol * fresnel * (specBroad + specTight);
        specular += lightCol * rim * 0.1 * half3(1.0, 0.9, 0.82);
    }
    else
    {
        half fresnel = AtfFresnel(N, V, lerp(0.04, 0.9, metallic));
        half specPower = lerp(14, 72, smoothness);
        half specTerm = pow(ndoth, specPower) * smoothness * lerp(0.12, 0.35, metallic);
        diffuse += lightCol * ndotl;
        specular += lightCol * specTerm * fresnel;
    }
}

half3 AtfLitColor(half3 albedo, half3 N, half3 V, half ao, half smoothness, half metallic, half skin)
{
    if (skin > 0.5)
    {
        half luma = dot(albedo, half3(0.3, 0.59, 0.11));
        albedo = lerp(luma.xxx, albedo, 1.07);
        albedo *= half3(1.03, 0.99, 0.96);
    }

    half3 L;
    half3 lightCol;
    AtfGetMainLight(L, lightCol);

    half3 diffuse = 0;
    half3 specular = 0;
    AtfAccumLambert(N, V, L, lightCol, smoothness, metallic, skin, diffuse, specular);
    AtfAccumLambert(N, V, _AtfExtraLightDir0.xyz, _AtfExtraLightColor0.rgb, smoothness, metallic, skin, diffuse, specular);
    AtfAccumLambert(N, V, _AtfExtraLightDir1.xyz, _AtfExtraLightColor1.rgb, smoothness, metallic, skin, diffuse, specular);

    half3 ambient = max(ShadeSH9(half4(N, 1)), 0);
    if (skin > 0.5)
    {
        ambient += half3(0.04, 0.024, 0.018);
    }

    return albedo * (ambient * ao + diffuse) + specular;
}

void AtfAccumEye(half3 N, half3 V, half3 L, half3 lightCol, inout half3 diffuse, inout half3 specular)
{
    half col2 = dot(lightCol, lightCol);
    half dir2 = dot(L, L);
    if (col2 < 1e-10 || dir2 < 1e-10)
    {
        return;
    }

    L = L * rsqrt(dir2);
    half ndotl = saturate(dot(N, L));
    half3 H = normalize(L + V);
    half ndoth = saturate(dot(N, H));
    diffuse += lightCol * ndotl;
    specular += lightCol * (pow(ndoth, 12) * 0.1 + pow(ndoth, 96) * 0.7);
}

half3 AtfEyeColor(half3 albedo, half3 N, half3 V)
{
    half3 L;
    half3 lightCol;
    AtfGetMainLight(L, lightCol);

    half3 diffuse = 0;
    half3 specular = 0;
    AtfAccumEye(N, V, L, lightCol, diffuse, specular);
    AtfAccumEye(N, V, _AtfExtraLightDir0.xyz, _AtfExtraLightColor0.rgb, diffuse, specular);
    AtfAccumEye(N, V, _AtfExtraLightDir1.xyz, _AtfExtraLightColor1.rgb, diffuse, specular);

    half3 ambient = max(ShadeSH9(half4(N, 1)), 0.015);
    return albedo * (ambient * 0.7 + diffuse) + specular;
}

half StrandSpec(half3 T, half3 H, half exponent)
{
    half TdotH = dot(T, H);
    half sinTH = sqrt(saturate(1.0 - TdotH * TdotH));
    half dirAtten = smoothstep(-1.0, 0.0, TdotH);
    return dirAtten * pow(sinTH, exponent);
}

void AtfAccumHair(half3 albedo, half3 N, half3 T, half3 V, half3 L, half3 lightCol, half gloss, inout half3 color)
{
    half col2 = dot(lightCol, lightCol);
    half dir2 = dot(L, L);
    if (col2 < 1e-10 || dir2 < 1e-10)
    {
        return;
    }

    L = L * rsqrt(dir2);
    half ndotl = saturate(dot(N, L));
    half wrap = saturate(dot(N, L) * 0.5 + 0.5);
    half diffuse = lerp(0.12, 0.78, wrap * wrap) * lerp(0.4, 1.0, ndotl);

    half3 H = normalize(L + V);
    half3 tPrimary = normalize(T + N * 0.14);
    half3 tSecondary = normalize(T - N * 0.08);
    half spec1 = StrandSpec(tPrimary, H, lerp(40, 110, gloss)) * lerp(0.1, 0.32, gloss);
    half spec2 = StrandSpec(tSecondary, H, lerp(6, 20, gloss)) * 0.2;
    half3 specCol = lerp(albedo, half3(1.0, 0.94, 0.82), 0.4);

    color += albedo * lightCol * diffuse + lightCol * specCol * (spec1 + spec2 * albedo);
}

half3 AtfHairColor(half3 albedo, half3 N, half3 T, half3 V, half gloss)
{
    half3 L;
    half3 lightCol;
    AtfGetMainLight(L, lightCol);

    half3 color = albedo * max(ShadeSH9(half4(N, 1)), 0) * 0.65;
    AtfAccumHair(albedo, N, T, V, L, lightCol, gloss, color);
    AtfAccumHair(albedo, N, T, V, _AtfExtraLightDir0.xyz, _AtfExtraLightColor0.rgb, gloss, color);
    AtfAccumHair(albedo, N, T, V, _AtfExtraLightDir1.xyz, _AtfExtraLightColor1.rgb, gloss, color);
    return color;
}

half3 AtfSimpleLitColor(half3 albedo, half3 N)
{
    half3 L;
    half3 lightCol;
    AtfGetMainLight(L, lightCol);

    half3 diffuse = 0;
    half dir2 = dot(L, L);
    if (dot(lightCol, lightCol) > 1e-10 && dir2 > 1e-10)
    {
        diffuse += lightCol * saturate(dot(N, L * rsqrt(dir2)));
    }

    dir2 = dot(_AtfExtraLightDir0.xyz, _AtfExtraLightDir0.xyz);
    if (dir2 > 1e-10)
    {
        diffuse += _AtfExtraLightColor0.rgb * saturate(dot(N, _AtfExtraLightDir0.xyz * rsqrt(dir2)));
    }

    dir2 = dot(_AtfExtraLightDir1.xyz, _AtfExtraLightDir1.xyz);
    if (dir2 > 1e-10)
    {
        diffuse += _AtfExtraLightColor1.rgb * saturate(dot(N, _AtfExtraLightDir1.xyz * rsqrt(dir2)));
    }

    return albedo * (max(ShadeSH9(half4(N, 1)), 0) + diffuse);
}

#endif
