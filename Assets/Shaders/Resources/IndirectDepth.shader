Shader "Unlit/IndirectDepth"
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
            #pragma target 5.0
            #include "UnityCG.cginc"
            struct v2f
            {
                float3 worldPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
float _CameraFarClipPlane;
            StructuredBuffer<float4x4> resultBuffer;
            StructuredBuffer<float4> verticesBuffer;
            v2f vert (uint vertexID : SV_VERTEXID, uint instanceID : SV_INSTANCEID)
            {
                v2f o;
                float3 vertex = verticesBuffer[vertexID];
                float4x4 info = resultBuffer[instanceID];
                float4 worldPos = mul(info, float4(vertex, 1));
                o.worldPos = worldPos.xyz;
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                return o;
            }

            float frag (v2f i) : SV_Target
            {
                return distance(_WorldSpaceCameraPos, i.worldPos) / _CameraFarClipPlane;
            }
            ENDCG
        }
    }
}
