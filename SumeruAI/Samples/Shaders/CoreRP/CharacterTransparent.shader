Shader "SumeruAI/CoreRP/CharacterTransparent"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,0.15)
        _MainTex ("Albedo", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.9
        _Metallic ("Metallic", Range(0,1)) = 0
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "UnityLightingCommon.cginc"
    #include "CharacterLighting.cginc"

    sampler2D _MainTex;
    float4 _MainTex_ST;
    fixed4 _Color;
    float _Glossiness;
    float _Metallic;

    struct appdata
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        half3 worldNormal : TEXCOORD1;
    };

    v2f vert(appdata v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        o.worldNormal = UnityObjectToWorldNormal(v.normal);
        return o;
    }

    fixed4 frag(v2f i) : SV_Target
    {
        fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
        half3 N = normalize(i.worldNormal);
        half3 color = AtfSimpleLitColor(albedo.rgb, N);
        return fixed4(color, albedo.a);
    }
    ENDCG

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back

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
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Back
        ZWrite Off
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
    }

    FallBack "Transparent/Diffuse"
}
