Shader "Unlit/Reflection"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                
            };

            struct v2f
            {
                float4 screenUV : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            float4x4 _InvVP;    //Inverse View Project Matrix
            Texture2D _CameraGBufferTexture0; SamplerState sampler_CameraGBufferTexture0;       //RGB Diffuse A AO
            Texture2D _CameraGBufferTexture1; SamplerState sampler_CameraGBufferTexture1;       //RGB Specular A Smoothness
            Texture2D _CameraGBufferTexture2; SamplerState sampler_CameraGBufferTexture2;       //RGB Normal A Depth
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenUV = ComputeScreenPos(o.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 screenUV = i.screenUV.xy / i.screenUV.w;
                
                return 0;   //TODO
            }
            ENDCG
        }
    }
}
