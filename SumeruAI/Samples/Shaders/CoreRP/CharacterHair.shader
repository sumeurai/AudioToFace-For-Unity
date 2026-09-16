Shader "SumeruAI/CoreRP/CharacterHair"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _OpacityMap ("Opacity", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _AoMap ("AO", 2D) = "white" {}
        _RootMap ("Root", 2D) = "white" {}
        _FlowMap ("Flow", 2D) = "gray" {}
        _BlendMap ("Blend", 2D) = "black" {}
        _BumpScale ("Normal Scale", Float) = 0.45
        _FlowStrength ("Flow Strength", Range(0,1)) = 0.35
        _Glossiness ("Smoothness", Range(0,1)) = 0.42
        _Cutoff ("Alpha Clip", Range(0,1)) = 0.07
        _BlendColor ("Blend Color", Color) = (0.9, 0.75, 0.55, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "UnityLightingCommon.cginc"
    #include "CharacterLighting.cginc"

    sampler2D _MainTex;
    sampler2D _OpacityMap;
    sampler2D _BumpMap;
    sampler2D _AoMap;
    sampler2D _RootMap;
    sampler2D _FlowMap;
    sampler2D _BlendMap;
    float4 _MainTex_ST;
    fixed4 _Color;
    fixed4 _BlendColor;
    float _BumpScale;
    float _FlowStrength;
    float _Glossiness;
    float _Cutoff;

    struct appdata
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float4 tangent : TANGENT;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 worldPos : TEXCOORD1;
        half3 worldNormal : TEXCOORD2;
        half3 worldTangent : TEXCOORD3;
        half3 worldBinormal : TEXCOORD4;
    };

    v2f vert(appdata v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        o.worldNormal = UnityObjectToWorldNormal(v.normal);
        o.worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
        o.worldBinormal = cross(o.worldNormal, o.worldTangent) * v.tangent.w;
        return o;
    }

    float SampleOpacity(float2 uv)
    {
        // HDRP uses the opacity map only. Multiplying by albedo alpha punches holes.
        return tex2D(_OpacityMap, uv).r * _Color.a;
    }

    half3 UnpackNormalTS(float4 packed, float scale)
    {
        half3 n;
        n.xy = (packed.wy * 2 - 1) * scale;
        n.z = sqrt(saturate(1.0 - saturate(dot(n.xy, n.xy))));
        return n;
    }

    float Dither(float2 screenPos)
    {
        return frac(52.9829189 * frac(dot(floor(screenPos), float2(0.06711056, 0.00583715))));
    }

    fixed4 frag(v2f i) : SV_Target
    {
        float opacity = SampleOpacity(i.uv);
        clip(opacity - 0.02 - Dither(i.pos.xy) * 0.05);

        fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
        half ao = tex2D(_AoMap, i.uv).r;
        half root = tex2D(_RootMap, i.uv).r;
        half blend = tex2D(_BlendMap, i.uv).r;
        albedo.rgb *= lerp(0.55, 1.0, ao);
        albedo.rgb *= lerp(0.68, 1.0, saturate(root + 0.12));
        albedo.rgb = lerp(albedo.rgb, albedo.rgb * _BlendColor.rgb, blend * 0.4);

        half3 tnormal = UnpackNormalTS(tex2D(_BumpMap, i.uv), _BumpScale);
        half3 N = normalize(
            tnormal.x * i.worldTangent +
            tnormal.y * i.worldBinormal +
            tnormal.z * i.worldNormal);
        half3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
        half2 flow = tex2D(_FlowMap, i.uv).rg * 2.0 - 1.0;
        half3 T = normalize(i.worldBinormal + i.worldTangent * flow.x * _FlowStrength);
        half3 color = AtfHairColor(albedo.rgb, N, T, V, _Glossiness);
        float alpha = smoothstep(0.02, 0.28, opacity);
        return fixed4(color, alpha);
    }

    struct appdataShadow
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2fShadow
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    v2fShadow vertShadow(appdataShadow v)
    {
        v2fShadow o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        return o;
    }

    fixed4 fragShadow(v2fShadow i) : SV_Target
    {
        clip(SampleOpacity(i.uv) - max(_Cutoff, 0.35));
        return 0;
    }
    ENDCG

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }
        Cull [_Cull]
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma target 3.0
            ENDCG
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma target 3.0
            ENDCG
        }
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        Cull [_Cull]
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 200

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma target 3.0
            ENDCG
        }
    }

    FallBack "Transparent/Cutout/Diffuse"
}
