﻿Shader "Hidden/ReadLod"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Lod("LOD Level", Range(0, 10)) = 0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            float _Lod;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            Texture2D<float> _MainTex;  SamplerState sampler_MainTex;

            float4 frag (v2f i) : SV_Target
            {
                float4 col = _MainTex.SampleLevel(sampler_MainTex, i.uv, _Lod);
                
                return col;
            }
            ENDCG
        }
    }
}
