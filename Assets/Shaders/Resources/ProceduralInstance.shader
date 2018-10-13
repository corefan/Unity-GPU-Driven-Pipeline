// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

 Shader "Maxwell/ProceduralInstance" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap("Normal Map", 2D) = "bump" {}
		_NormalScale("Normal Scale", float) = 1
		_SpecularMap("Specular Map", 2D) = "white"{}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_OcclusionMap("Occlusion Map", 2D) = "white"{}
		_Occlusion("Occlusion Scale", Range(0,1)) = 1
		_SpecularColor("Specular Color",Color) = (0.2,0.2,0.2,1)
		_EmissionColor("Emission Color", Color) = (0,0,0,1)
		_EmissionMultiplier("Emission Level", Range(1, 20)) = 1

	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		
	// ------------------------------------------------------------
	// Surface shader code generated out of a CGPROGRAM block:
CGINCLUDE
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
#pragma exclude_renderers gles
#pragma target 5.0
#include "HLSLSupport.cginc"
#include "UnityShaderVariables.cginc"
#include "UnityShaderUtilities.cginc"
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "UnityMetaPass.cginc"
#include "AutoLight.cginc"
#include "UnityPBSLighting.cginc"
#include "CGINC/Procedural.cginc"
#pragma shader_feature USE_NORMAL
#pragma shader_feature USE_SPECULAR
#pragma shader_feature USE_OCCLUSION
#pragma shader_feature USE_ALBEDO
		struct Input {
			float2 uv_MainTex;
		};

    float4 _SpecularColor;
    float4 _EmissionColor;
	float _EmissionMultiplier;
		float _NormalScale;
		float _Occlusion;
		float _VertexScale;
		float _VertexOffset;
		sampler2D _BumpMap;
		sampler2D _SpecularMap;
		sampler2D _OcclusionMap;
		sampler2D _MainTex;



		float _Glossiness;
		float4 _Color;


		inline void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
			// Albedo comes from a texture tinted by color
			float2 uv = IN.uv_MainTex;// - parallax_mapping(IN.uv_MainTex,IN.viewDir);
			#if USE_ALBEDO
			float4 c = tex2D (_MainTex, uv) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			#else
			o.Albedo = _Color.rgb;
			o.Alpha = _Color.a;
			#endif
			#if USE_OCCLUSION
			o.Occlusion = lerp(1, tex2D(_OcclusionMap,uv).r, _Occlusion);
			#else
			o.Occlusion = 1;
			#endif
			#if USE_SPECULAR
			float4 spec = tex2D(_SpecularMap,uv);
			o.Specular = _SpecularColor  * spec.rgb;
			o.Smoothness = _Glossiness * spec.a;
			#else
			o.Specular = _SpecularColor;
			o.Smoothness = _Glossiness;
			#endif
			#if USE_NORMAL
			o.Normal = UnpackNormal(tex2D(_BumpMap,uv));
			o.Normal.xy *= _NormalScale;
			#else
			o.Normal = float3(0,0,1);
			#endif
			o.Emission = _EmissionColor * _EmissionMultiplier;
		}


#define GetScreenPos(pos) ((float2(pos.x, pos.y) * 0.5) / pos.w + 0.5)


float4 ProceduralStandardSpecular_Deferred (SurfaceOutputStandardSpecular s, float3 viewDir, out float4 outGBuffer0, out float4 outGBuffer1, out float4 outGBuffer2)
{
    // energy conservation
    float oneMinusReflectivity;
    s.Albedo = EnergyConservationBetweenDiffuseAndSpecular (s.Albedo, s.Specular, /*out*/ oneMinusReflectivity);
    // RT0: diffuse color (rgb), occlusion (a) - sRGB rendertarget
    outGBuffer0 = float4(s.Albedo, s.Occlusion);

    // RT1: spec color (rgb), smoothness (a) - sRGB rendertarget
    outGBuffer1 = float4(s.Specular, s.Smoothness);

    // RT2: normal (rgb), --unused, very low precision-- (a)
    outGBuffer2 = float4(s.Normal * 0.5f + 0.5f, 0);
    float4 emission = float4(s.Emission, 1);

    return emission;
}
float4x4 _LastVp;
float4x4 _NonJitterVP;
float2 CalculateMotionVector(float4x4 lastvp, float3 worldPos, float2 screenUV)
{
	float4 lastScreenPos = mul(lastvp, float4(worldPos, 1));
	float2 lastScreenUV = GetScreenPos(lastScreenPos);
	return screenUV - lastScreenUV;
}

struct v2f_surf {
  UNITY_POSITION(pos);
  float2 pack0 : TEXCOORD0; 
  float4 worldTangent : TEXCOORD1;
  float4 worldBinormal : TEXCOORD2;
  float4 worldNormal : TEXCOORD3;
  float3 worldViewDir : TEXCOORD4;
};
float4 _MainTex_ST;

v2f_surf vert_surf (uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID) 
{
  	Point v = getVertex(vertexID, instanceID);
  	v2f_surf o;
  	o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
  	o.pos = mul(UNITY_MATRIX_VP, float4(v.vertex, 1));
  	o.worldNormal =float4(v.normal, v.vertex.x);
  	o.worldTangent = float4( v.tangent.xyz, v.vertex.z);
  	float tangentSign = v.tangent.w * unity_WorldTransformParams.w;
  	o.worldBinormal = float4(cross(v.normal, o.worldTangent.xyz) * tangentSign, v.vertex.y);
  	o.worldViewDir = UnityWorldSpaceViewDir(v.vertex);
  	return o;
}
float4 unity_Ambient;

// fragment shader
void frag_surf (v2f_surf IN,
    out float4 outGBuffer0 : SV_Target0,
    out float4 outGBuffer1 : SV_Target1,
    out float4 outGBuffer2 : SV_Target2,
    out float4 outEmission : SV_Target3,
	out float2 outMotionVector : SV_Target4
) {
  // prepare and unpack data
  Input surfIN;
  surfIN.uv_MainTex = IN.pack0.xy;
  float3 worldPos = float3(IN.worldTangent.w, IN.worldBinormal.w, IN.worldNormal.w);
  float3 worldViewDir = normalize(IN.worldViewDir);
  SurfaceOutputStandardSpecular o;
  float3x3 wdMatrix= float3x3(normalize(IN.worldTangent.xyz), normalize(IN.worldBinormal.xyz), normalize(IN.worldNormal.xyz));
  // call surface function
  surf (surfIN, o);
  o.Normal = normalize(mul(o.Normal, wdMatrix));
  outEmission = ProceduralStandardSpecular_Deferred (o, worldViewDir, outGBuffer0, outGBuffer1, outGBuffer2); //GI neccessary here!
  outGBuffer2.w = IN.pos.z;
  //Calculate Motion Vector
  float4 screenPos = mul(_NonJitterVP, float4(worldPos, 1));
  float2 screenUV = GetScreenPos(screenPos);
  outMotionVector = CalculateMotionVector(_LastVp, worldPos, screenUV);
}

ENDCG

//Pass 0 deferred
Pass {
stencil{
  Ref 1
  comp always
  pass replace
}
ZTest Less
CGPROGRAM

#pragma vertex vert_surf
#pragma fragment frag_surf
#pragma exclude_renderers nomrt
#define UNITY_PASS_DEFERRED
ENDCG
}
}
CustomEditor "SpecularShaderEditor"
}

