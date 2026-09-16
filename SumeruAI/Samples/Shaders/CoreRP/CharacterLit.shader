Shader "SumeruAI/CoreRP/CharacterLit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1
        _ORMMap ("ORM (Occlusion, Roughness, Metallic)", 2D) = "black" {}
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.4
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        [Toggle(_SKIN_ON)] _Skin ("Skin Wrap", Float) = 0
        [Toggle(_EYE_ON)] _Eye ("Eye Specular", Float) = 0
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "UnityLightingCommon.cginc"
    #include "UnityStandardUtils.cginc"
    #include "CharacterLighting.cginc"

    sampler2D _MainTex;
    sampler2D _BumpMap;
    sampler2D _ORMMap;
    float4 _MainTex_ST;
    fixed4 _Color;
    float _BumpScale;
    float _Metallic;
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

    fixed4 frag(v2f i) : SV_Target
    {
        fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
        #ifdef _ALPHATEST_ON
        clip(albedo.a - _Cutoff);
        #endif

        half3 tnormal = UnpackScaleNormal(tex2D(_BumpMap, i.uv), _BumpScale);
        half3 N = normalize(
            tnormal.x * i.worldTangent +
            tnormal.y * i.worldBinormal +
            tnormal.z * i.worldNormal);
        half3 V = normalize(_WorldSpaceCameraPos - i.worldPos);

        fixed4 orm = tex2D(_ORMMap, i.uv);
        float hasOrm = saturate(orm.r + orm.g + orm.b);
        float metallic = hasOrm > 0.001 ? max(_Metallic, orm.b) : _Metallic;
        float smoothness = hasOrm > 0.001 ? _Glossiness * (1.0 - orm.g) : _Glossiness;
        float ao = hasOrm > 0.001 ? orm.r : 1;

        #ifdef _EYE_ON
        half3 color = AtfEyeColor(albedo.rgb, N, V);
        #elif defined(_SKIN_ON)
        half3 color = AtfLitColor(albedo.rgb, N, V, ao, smoothness, metallic, 1);
        #else
        half3 color = AtfLitColor(albedo.rgb, N, V, ao, smoothness, metallic, 0);
        #endif
        return fixed4(color, albedo.a);
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
        #ifdef _ALPHATEST_ON
        clip(tex2D(_MainTex, i.uv).a * _Color.a - _Cutoff);
        #endif
        return 0;
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull [_Cull]

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _SKIN_ON
            #pragma shader_feature_local _EYE_ON
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma target 3.0
            #pragma shader_feature_local _ALPHATEST_ON
            ENDCG
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma target 3.0
            #pragma shader_feature_local _ALPHATEST_ON
            ENDCG
        }
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull [_Cull]
        LOD 200

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _SKIN_ON
            #pragma shader_feature_local _EYE_ON
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma target 3.0
            #pragma shader_feature_local _ALPHATEST_ON
            ENDCG
        }
    }

    FallBack "Diffuse"
}
