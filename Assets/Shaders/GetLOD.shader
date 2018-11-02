﻿Shader "Hidden/GetLOD"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }
            Texture2D<half> _MainTex; SamplerState sampler_MainTex;
            uint _PreviousLevel;
            half frag (v2f i) : SV_Target
            {
                half4 value = half4(
                    _MainTex.SampleLevel(sampler_MainTex, i.uv, _PreviousLevel, uint2(-1,-1)),
                    _MainTex.SampleLevel(sampler_MainTex, i.uv, _PreviousLevel, uint2(-1, 1)),
                    _MainTex.SampleLevel(sampler_MainTex, i.uv, _PreviousLevel, uint2(1,-1)),
                    _MainTex.SampleLevel(sampler_MainTex, i.uv, _PreviousLevel, uint2(1, 1))
                );
                return max(max(value.x, value.y), max(value.z, value.w));
            }
            ENDCG
        }
    }
}
