Shader "Hidden/ShadowMask"
{
	SubShader
	{

CGINCLUDE
#pragma target 5.0
#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityGBuffer.cginc"
#include "UnityStandardBRDF.cginc"

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
			//16 for mobile
			//32 for console
			//64 for PC
			//#define SAMPLECOUNT 16
			//#define SAMPLECOUNT 32
			#define SAMPLECOUNT 64
static const float2 DirPoissonDisks[64] =
{
	float2 (0.1187053, 0.7951565),
	float2 (0.1173675, 0.6087878),
	float2 (-0.09958518, 0.7248842),
	float2 (0.4259812, 0.6152718),
	float2 (0.3723574, 0.8892787),
	float2 (-0.02289676, 0.9972908),
	float2 (-0.08234791, 0.5048386),
	float2 (0.1821235, 0.9673787),
	float2 (-0.2137264, 0.9011746),
	float2 (0.3115066, 0.4205415),
	float2 (0.1216329, 0.383266),
	float2 (0.5948939, 0.7594361),
	float2 (0.7576465, 0.5336417),
	float2 (-0.521125, 0.7599803),
	float2 (-0.2923127, 0.6545699),
	float2 (0.6782473, 0.22385),
	float2 (-0.3077152, 0.4697627),
	float2 (0.4484913, 0.2619455),
	float2 (-0.5308799, 0.4998215),
	float2 (-0.7379634, 0.5304936),
	float2 (0.02613133, 0.1764302),
	float2 (-0.1461073, 0.3047384),
	float2 (-0.8451027, 0.3249073),
	float2 (-0.4507707, 0.2101997),
	float2 (-0.6137282, 0.3283674),
	float2 (-0.2385868, 0.08716244),
	float2 (0.3386548, 0.01528411),
	float2 (-0.04230833, -0.1494652),
	float2 (0.167115, -0.1098648),
	float2 (-0.525606, 0.01572019),
	float2 (-0.7966855, 0.1318727),
	float2 (0.5704287, 0.4778273),
	float2 (-0.9516637, 0.002725032),
	float2 (-0.7068223, -0.1572321),
	float2 (0.2173306, -0.3494083),
	float2 (0.06100426, -0.4492816),
	float2 (0.2333982, 0.2247189),
	float2 (0.07270987, -0.6396734),
	float2 (0.4670808, -0.2324669),
	float2 (0.3729528, -0.512625),
	float2 (0.5675077, -0.4054544),
	float2 (-0.3691984, -0.128435),
	float2 (0.8752473, 0.2256988),
	float2 (-0.2680127, -0.4684393),
	float2 (-0.1177551, -0.7205751),
	float2 (-0.1270121, -0.3105424),
	float2 (0.5595394, -0.06309237),
	float2 (-0.9299136, -0.1870008),
	float2 (0.974674, 0.03677348),
	float2 (0.7726735, -0.06944724),
	float2 (-0.4995361, -0.3663749),
	float2 (0.6474168, -0.2315787),
	float2 (0.1911449, -0.8858921),
	float2 (0.3671001, -0.7970535),
	float2 (-0.6970353, -0.4449432),
	float2 (-0.417599, -0.7189326),
	float2 (-0.5584748, -0.6026504),
	float2 (-0.02624448, -0.9141423),
	float2 (0.565636, -0.6585149),
	float2 (-0.874976, -0.3997879),
	float2 (0.9177843, -0.2110524),
	float2 (0.8156927, -0.3969557),
	float2 (-0.2833054, -0.8395444),
	float2 (0.799141, -0.5886372)
};
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = v.vertex;
				o.uv = v.uv;
				return o;
			}
			float4 _SoftParam;
			float4x4 _InvVP;
			float4x4 _ShadowMapVPs[4];
			float4 _ShadowDisableDistance;
			UNITY_DECLARE_TEX2DARRAY(_DirShadowMap);
			Texture2D<float4> _CameraGBufferTexture0; SamplerState sampler_CameraGBufferTexture0;
			Texture2D<float4> _CameraGBufferTexture1; SamplerState sampler_CameraGBufferTexture1;
			Texture2D<float4> _CameraGBufferTexture2; SamplerState sampler_CameraGBufferTexture2;
			float3 _LightFinalColor;
			#define RANDOM(seed) cos(sin(seed * float2(54.135764, 77.468761) + float2(631.543147, 57.4687)) * float2(657.387478, 86.1653) + float2(65.15686, 15.3574563))
			float GetShadow(inout float4 worldPos, float depth, float2 screenUV)
			{
				worldPos /= worldPos.w;
				float eyeDistance = LinearEyeDepth(depth);
				float4 eyeRange = eyeDistance < _ShadowDisableDistance;
				eyeRange.yzw -= eyeRange.xyz;
				float zAxisUV = dot(eyeRange, float4(0, 1, 2, 3));
				float4x4 vpMat = _ShadowMapVPs[zAxisUV];
				float4 shadowPos = mul(vpMat, worldPos);
				float2 shadowUV = shadowPos.xy / shadowPos.w;
				shadowUV = shadowUV * 0.5 + 0.5;
				float softValue = dot(_SoftParam, eyeRange);

				#if UNITY_REVERSED_Z
				float dist = 1 - shadowPos.z;
				#else
				float dist = shadowPos.z;
				#endif
				float2 seed = (_ScreenParams.yx * screenUV.yx + screenUV.xy) * _ScreenParams.xy + _Time.zw;
				float atten = 0;
				for(int i = 0; i < SAMPLECOUNT; ++i)
				{
					seed = RANDOM(seed + DirPoissonDisks[i]).yx;
					float2 dir = DirPoissonDisks[i] + seed;
					atten += dist < UNITY_SAMPLE_TEX2DARRAY(_DirShadowMap, float3(shadowUV + dir * softValue, zAxisUV)).r;
				}
				atten /= SAMPLECOUNT;
				float fadeDistance = saturate( (_ShadowDisableDistance.w - eyeDistance) / (_ShadowDisableDistance.w * 0.05));
				atten = lerp(1, atten, fadeDistance);
				return atten;
			}

			float GetHardShadow(float3 worldPos, float depth)
			{
				float eyeDistance = LinearEyeDepth(depth);
				float4 eyeRange = eyeDistance < _ShadowDisableDistance;
				eyeRange.yzw -= eyeRange.xyz;
				float zAxisUV = dot(eyeRange, float4(0, 1, 2, 3));
				float4x4 vpMat = _ShadowMapVPs[zAxisUV];
				float4 shadowPos = mul(vpMat, float4(worldPos, 1));
				float2 shadowUV = shadowPos.xy / shadowPos.w;
				shadowUV = shadowUV * 0.5 + 0.5;
				#if UNITY_REVERSED_Z
				float dist = 1 - shadowPos.z;
				#else
				float dist = shadowPos.z;
				#endif
				float atten = dist < UNITY_SAMPLE_TEX2DARRAY(_DirShadowMap, float3(shadowUV, zAxisUV)).r;
				float fadeDistance = saturate( (_ShadowDisableDistance.w - eyeDistance) / (_ShadowDisableDistance.w * 0.05));
				atten = lerp(1, atten, fadeDistance);
				return atten;
			}

ENDCG

		Pass
		{
		Cull Off ZWrite Off ZTest Greater
		Blend one one
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4 frag (v2f i) : SV_Target
			{

				half4 gbuffer0 = _CameraGBufferTexture0.Sample(sampler_CameraGBufferTexture0, i.uv);
    			half4 gbuffer1 = _CameraGBufferTexture1.Sample(sampler_CameraGBufferTexture1, i.uv);
    			half4 gbuffer2 = _CameraGBufferTexture2.Sample(sampler_CameraGBufferTexture2, i.uv);
				float depth = gbuffer2.w;
				float4 wpos = mul(_InvVP, float4(i.uv * 2 - 1, depth, 1));
				float atten = GetShadow(wpos, depth, i.uv);
				UnityStandardData data = UnityStandardDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);
				float3 eyeVec = normalize(wpos.xyz - _WorldSpaceCameraPos);
				half oneMinusReflectivity = 1 - SpecularStrength(data.specularColor.rgb);
    			UnityIndirect ind;
    			UNITY_INITIALIZE_OUTPUT(UnityIndirect, ind);
    			ind.diffuse = 0;
    			ind.specular = 0;
				UnityLight light;
				light.dir = _LightPos.xyz;
				light.color = _LightFinalColor * atten;
				return UNITY_BRDF_PBS (data.diffuseColor, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, -eyeVec, light, ind);
			}
			ENDCG
		}

		Pass
		{
		Cull Off ZWrite Off ZTest Greater
		Blend one one
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4 frag (v2f i) : SV_Target
			{
				half4 gbuffer0 = _CameraGBufferTexture0.Sample(sampler_CameraGBufferTexture0, i.uv);
    			half4 gbuffer1 = _CameraGBufferTexture1.Sample(sampler_CameraGBufferTexture1, i.uv);
    			half4 gbuffer2 = _CameraGBufferTexture2.Sample(sampler_CameraGBufferTexture2, i.uv);
				float depth = gbuffer2.w;
				float4 wpos = mul(_InvVP, float4(i.uv * 2 - 1, depth, 1));
				UnityStandardData data = UnityStandardDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);
				float3 eyeVec = normalize(wpos.xyz - _WorldSpaceCameraPos);
				half oneMinusReflectivity = 1 - SpecularStrength(data.specularColor.rgb);
    			UnityIndirect ind;
    			UNITY_INITIALIZE_OUTPUT(UnityIndirect, ind);
    			ind.diffuse = 0;
    			ind.specular = 0;
				UnityLight light;
				light.dir = _LightPos.xyz;
				light.color = _LightFinalColor;
				return UNITY_BRDF_PBS (data.diffuseColor, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, -eyeVec, light, ind);
			}
			ENDCG
		}

		Pass
		{
		Cull Off ZWrite Off ZTest Always
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4 frag (v2f i) : SV_Target
			{
    			half4 gbuffer2 = _CameraGBufferTexture2.Sample(sampler_CameraGBufferTexture2, i.uv);
				float depth = gbuffer2.w;
				float4 wpos = mul(_InvVP, float4(i.uv * 2 - 1, depth, 1));
				wpos /= wpos.w;
				const float step = 1.0 / 512.0;
				float3 viewDir = wpos.xyz - _WorldSpaceCameraPos;
				float len = length(viewDir);
				float value = 0;
				for(float i = 0; i < 1; i += step)
				{
					float3 samplePos = lerp(_WorldSpaceCameraPos, wpos.xyz, i);
					value += GetHardShadow(samplePos, depth);
				}
				value *= step * 0.1 * len;
				return value;
			}
			ENDCG
		}
	}
}
