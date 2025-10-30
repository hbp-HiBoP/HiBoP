// Upgrade NOTE: replaced 'defined FOG_COMBINED_WITH_WORLD_POS' with 'defined (FOG_COMBINED_WITH_WORLD_POS)'

Shader "Custom/TransparentBrain"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB)", 2D) = "white" {}
		_AoTex("AO (RGB)", 2D) = "white" {}
		_ColorTex("Color map (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Atlas("Atlas", int) = 0
		_Activity("Activity", int) = 0
		_FMRI("FMRI", int) = 0
		_Amount("Extrusion Amount", Range(0, 1)) = 0
		_MaxRadius("Maximum extrusion radius", Range(0,100)) = 100
	}

		SubShader
		{
				Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

			
	// ------------------------------------------------------------
	// Surface shader code generated out of a CGPROGRAM block:
	ZWrite Off ColorMask RGBA
	

	// ---- forward rendering base pass:
	Pass {
		Name "FORWARD"
		Tags { "LightMode" = "ForwardBase" }
		Blend One OneMinusSrcAlpha

CGPROGRAM
// compile directives
#pragma vertex vert_surf
#pragma fragment frag_surf
#pragma target 3.0
#pragma multi_compile_instancing
#pragma multi_compile_fog
#pragma multi_compile_fwdbasealpha noshadow
#include "HLSLSupport.cginc"
#define UNITY_INSTANCED_LOD_FADE
#define UNITY_INSTANCED_SH
#define UNITY_INSTANCED_LIGHTMAPSTS
#define UNITY_INSTANCED_RENDERER_BOUNDS
#include "UnityShaderVariables.cginc"
#include "UnityShaderUtilities.cginc"
// -------- variant for: <when no other keywords are defined>
#if !defined(INSTANCING_ON)
// Surface shader code generated based on:
// vertex modifier: 'vert'
// writes to per-pixel normal: no
// writes to emission: no
// writes to occlusion: no
// needs world space reflection vector: no
// needs world space normal vector: no
// needs screen space position: no
// needs world space position: YES
// needs view direction: no
// needs world space view direction: no
// needs world space position for lighting: YES
// needs world space view direction for lighting: YES
// needs world space view direction for lightmaps: no
// needs vertex color: YES
// needs VFACE: no
// needs SV_IsFrontFace: no
// passes tangent-to-world matrix to pixel shader: no
// reads from normal: no
// 3 texcoords actually used
//   float2 _MainTex
//   float2 _AoTex
//   float2 _ColorTex
#define _ALPHAPREMULTIPLY_ON 1
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

// Original surface shader snippet:
#line 20 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif
/* UNITY: Original start of shader */
				//#pragma surface surf Standard vertex:vert alpha
				//#pragma target 3.0

				sampler2D _MainTex;
				sampler2D _AoTex;
				sampler2D _ColorTex;

				int _Atlas;
				int _Activity;
				int _FMRI;
				half _Glossiness;
				half _Metallic;
				fixed4 _Color;
				float _Amount;
				float _MaxRadius;

				uniform int _StrongCuts;
				uniform int _CutCount;
				uniform float3 _CutPoints[20];
				uniform float3 _CutNormals[20];
				uniform float3 _Center;

				struct Input
				{
					float4 pos : SV_POSITION;
					float2 uv_MainTex : TEXCOORD0;
					float2 uv2_AoTex :   TEXCOORD1;
					float2 uv3_ColorTex :   TEXCOORD2;
					float4 vertex_col : COLOR;
					float3 worldPos;
				};

				void vert(inout appdata_full v)
				{
					float3 normal = v.vertex.xyz - _Center;
					float norm = sqrt(normal.x * normal.x + normal.y * normal.y + normal.z * normal.z);
					normal = float3(normal.x / norm, normal.y / norm, normal.z / norm);
					//normal = (1 - _Amount) * v.normal + _Amount * normal;

					v.vertex.xyz += normal * (_MaxRadius - norm) * _Amount;
					v.normal = _Amount * normal + (1 - _Amount) * v.normal;
				}

				float is_clipped(Input IN)
				{
					float3 localPos = IN.worldPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
					float clipping = 1;
					if (!_StrongCuts)
					{
						for (int i = 0; i < _CutCount && i < 20; ++i)
						{
							int value = sign(dot(_CutNormals[i], _CutPoints[i] - localPos));
							if (value < 0)
							{
								clipping = -1;
							}
							else
							{
								clipping = 1;
								break;
							}
						}
					}
					else
					{
						for (int i = 0; i < _CutCount && i < 20; ++i)
						{
							int value = sign(dot(_CutNormals[i], _CutPoints[i] - localPos));
							if (value < 0)
							{
								clipping = -1;
								break;
							}
						}
					}
					return clipping;
				}

				void display_atlas(Input IN, inout SurfaceOutputStandard o)
				{
					float color = IN.vertex_col.a;
					fixed4 col = (1 - color) * tex2D(_MainTex, IN.uv_MainTex) + (color * IN.vertex_col.rgba);
					col *= _Color;
					o.Albedo = col.rgb;
				}

				void display_ieeg(Input IN, inout SurfaceOutputStandard o)
				{
					fixed4 col;
					fixed4 ao = tex2D(_AoTex, IN.uv2_AoTex);
					float color = ao.r * 2.5; // boost alpha (because of low tri mesh density compated to cuts textures)
					if (color > 1) color = 1;
					col = (1 - color) * tex2D(_MainTex, IN.uv_MainTex) + (color)* tex2D(_ColorTex, IN.uv3_ColorTex);
					col *= _Color;
					o.Albedo = col.rgb;
				}

				void surf(Input IN, inout SurfaceOutputStandard o)
				{
					clip(is_clipped(IN));

					if ((_Atlas | _FMRI))
					{
						display_atlas(IN, o);
					}
					else
					{
						display_ieeg(IN, o);
					}

					o.Alpha = _Color.a;
					o.Metallic = _Metallic;
					o.Smoothness = _Glossiness;
				}

			

// vertex-to-fragment interpolation data
// no lightmaps:
#ifndef LIGHTMAP_ON
// half-precision fragment shader registers:
#ifdef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
#define FOG_COMBINED_WITH_WORLD_POS
struct v2f_surf {
  UNITY_POSITION(pos);
  float4 pack0 : TEXCOORD0; // _MainTex _AoTex
  float2 pack1 : TEXCOORD1; // _ColorTex
  float3 worldNormal : TEXCOORD2;
  float4 worldPos : TEXCOORD3;
  fixed4 color : COLOR0;
  #if UNITY_SHOULD_SAMPLE_SH
  half3 sh : TEXCOORD4; // SH
  #endif
  DECLARE_LIGHT_COORDS(5)
  #if SHADER_TARGET >= 30
  float4 lmap : TEXCOORD6;
  #endif
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};
#endif
// high-precision fragment shader registers:
#ifndef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
struct v2f_surf {
  UNITY_POSITION(pos);
  float4 pack0 : TEXCOORD0; // _MainTex _AoTex
  float2 pack1 : TEXCOORD1; // _ColorTex
  float3 worldNormal : TEXCOORD2;
  float3 worldPos : TEXCOORD3;
  fixed4 color : COLOR0;
  #if UNITY_SHOULD_SAMPLE_SH
  half3 sh : TEXCOORD4; // SH
  #endif
  UNITY_FOG_COORDS(5)
  DECLARE_LIGHT_COORDS(6)
  #if SHADER_TARGET >= 30
  float4 lmap : TEXCOORD7;
  #endif
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};
#endif
#endif
// with lightmaps:
#ifdef LIGHTMAP_ON
// half-precision fragment shader registers:
#ifdef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
#define FOG_COMBINED_WITH_WORLD_POS
struct v2f_surf {
  UNITY_POSITION(pos);
  float4 pack0 : TEXCOORD0; // _MainTex _AoTex
  float2 pack1 : TEXCOORD1; // _ColorTex
  float3 worldNormal : TEXCOORD2;
  float4 worldPos : TEXCOORD3;
  fixed4 color : COLOR0;
  float4 lmap : TEXCOORD4;
  DECLARE_LIGHT_COORDS(5)
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};
#endif
// high-precision fragment shader registers:
#ifndef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
struct v2f_surf {
  UNITY_POSITION(pos);
  float4 pack0 : TEXCOORD0; // _MainTex _AoTex
  float2 pack1 : TEXCOORD1; // _ColorTex
  float3 worldNormal : TEXCOORD2;
  float3 worldPos : TEXCOORD3;
  fixed4 color : COLOR0;
  float4 lmap : TEXCOORD4;
  UNITY_FOG_COORDS(5)
  DECLARE_LIGHT_COORDS(6)
  #ifdef DIRLIGHTMAP_COMBINED
  float3 tSpace0 : TEXCOORD7;
  float3 tSpace1 : TEXCOORD8;
  float3 tSpace2 : TEXCOORD9;
  #endif
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};
#endif
#endif
float4 _MainTex_ST;
float4 _AoTex_ST;
float4 _ColorTex_ST;

// vertex shader
v2f_surf vert_surf (appdata_full v) {
  UNITY_SETUP_INSTANCE_ID(v);
  v2f_surf o;
  UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
  UNITY_TRANSFER_INSTANCE_ID(v,o);
  UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  vert (v);
  o.pos = UnityObjectToClipPos(v.vertex);
  o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
  o.pack0.zw = TRANSFORM_TEX(v.texcoord1, _AoTex);
  o.pack1.xy = TRANSFORM_TEX(v.texcoord2, _ColorTex);
  float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
  float3 worldNormal = UnityObjectToWorldNormal(v.normal);
  #if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
  fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
  fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
  fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
  #endif
  #if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED) && !defined(UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS)
  o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
  o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
  o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
  #endif
  o.worldPos.xyz = worldPos;
  o.worldNormal = worldNormal;
  o.color = v.color;
  #ifdef DYNAMICLIGHTMAP_ON
  o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
  #endif
  #ifdef LIGHTMAP_ON
  o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
  #endif

  // SH/ambient and vertex lights
  #ifndef LIGHTMAP_ON
    #if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
      o.sh = 0;
      // Approximated illumination from non-important point lights
      #ifdef VERTEXLIGHT_ON
        o.sh += Shade4PointLights (
          unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
          unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
          unity_4LightAtten0, worldPos, worldNormal);
      #endif
      o.sh = ShadeSHPerVertex (worldNormal, o.sh);
    #endif
  #endif // !LIGHTMAP_ON

  COMPUTE_LIGHT_COORDS(o); // pass light cookie coordinates to pixel shader
  #ifdef FOG_COMBINED_WITH_TSPACE
    UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o,o.pos); // pass fog coordinates to pixel shader
  #elif defined (FOG_COMBINED_WITH_WORLD_POS)
    UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o,o.pos); // pass fog coordinates to pixel shader
  #else
    UNITY_TRANSFER_FOG(o,o.pos); // pass fog coordinates to pixel shader
  #endif
  return o;
}

// fragment shader
fixed4 frag_surf (v2f_surf IN) : SV_Target {
  UNITY_SETUP_INSTANCE_ID(IN);
  UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
  // prepare and unpack data
  Input surfIN;
  #ifdef FOG_COMBINED_WITH_TSPACE
    UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
  #elif defined (FOG_COMBINED_WITH_WORLD_POS)
    UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
  #else
    UNITY_EXTRACT_FOG(IN);
  #endif
  UNITY_INITIALIZE_OUTPUT(Input,surfIN);
  surfIN.pos.x = 1.0;
  surfIN.uv_MainTex.x = 1.0;
  surfIN.uv2_AoTex.x = 1.0;
  surfIN.uv3_ColorTex.x = 1.0;
  surfIN.vertex_col.x = 1.0;
  surfIN.worldPos.x = 1.0;
  surfIN.uv_MainTex = IN.pack0.xy;
  surfIN.uv2_AoTex = IN.pack0.zw;
  surfIN.uv3_ColorTex = IN.pack1.xy;
  float3 worldPos = IN.worldPos.xyz;
  #ifndef USING_DIRECTIONAL_LIGHT
    fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
  #else
    fixed3 lightDir = _WorldSpaceLightPos0.xyz;
  #endif
  float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
  surfIN.worldPos = worldPos;
  surfIN.vertex_col = IN.color;
  #ifdef UNITY_COMPILER_HLSL
  SurfaceOutputStandard o = (SurfaceOutputStandard)0;
  #else
  SurfaceOutputStandard o;
  #endif
  o.Albedo = 0.0;
  o.Emission = 0.0;
  o.Alpha = 0.0;
  o.Occlusion = 1.0;
  fixed3 normalWorldVertex = fixed3(0,0,1);
  o.Normal = IN.worldNormal;
  normalWorldVertex = IN.worldNormal;

  // call surface function
  surf (surfIN, o);

  // compute lighting & shadowing factor
  UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
  fixed4 c = 0;

  // Setup lighting environment
  UnityGI gi;
  UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
  gi.indirect.diffuse = 0;
  gi.indirect.specular = 0;
  gi.light.color = _LightColor0.rgb;
  gi.light.dir = lightDir;
  // Call GI (lightmaps/SH/reflections) lighting function
  UnityGIInput giInput;
  UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
  giInput.light = gi.light;
  giInput.worldPos = worldPos;
  giInput.worldViewDir = worldViewDir;
  giInput.atten = atten;
  #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
    giInput.lightmapUV = IN.lmap;
  #else
    giInput.lightmapUV = 0.0;
  #endif
  #if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
    giInput.ambient = IN.sh;
  #else
    giInput.ambient.rgb = 0.0;
  #endif
  giInput.probeHDR[0] = unity_SpecCube0_HDR;
  giInput.probeHDR[1] = unity_SpecCube1_HDR;
  #if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
    giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
  #endif
  #ifdef UNITY_SPECCUBE_BOX_PROJECTION
    giInput.boxMax[0] = unity_SpecCube0_BoxMax;
    giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
    giInput.boxMax[1] = unity_SpecCube1_BoxMax;
    giInput.boxMin[1] = unity_SpecCube1_BoxMin;
    giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
  #endif
  LightingStandard_GI(o, giInput, gi);

  // realtime lighting: call lighting function
  c += LightingStandard (o, worldViewDir, gi);
  UNITY_APPLY_FOG(_unity_fogCoord, c); // apply fog
  return c;
}


#endif

// -------- variant for: INSTANCING_ON 
#if defined(INSTANCING_ON)
// Surface shader code generated based on:
// vertex modifier: 'vert'
// writes to per-pixel normal: no
// writes to emission: no
// writes to occlusion: no
// needs world space reflection vector: no
// needs world space normal vector: no
// needs screen space position: no
// needs world space position: YES
// needs view direction: no
// needs world space view direction: no
// needs world space position for lighting: YES
// needs world space view direction for lighting: YES
// needs world space view direction for lightmaps: no
// needs vertex color: YES
// needs VFACE: no
// needs SV_IsFrontFace: no
// passes tangent-to-world matrix to pixel shader: no
// reads from normal: no
// 3 texcoords actually used
//   float2 _MainTex
//   float2 _AoTex
//   float2 _ColorTex
#define _ALPHAPREMULTIPLY_ON 1
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

// Original surface shader snippet:
#line 20 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif
/* UNITY: Original start of shader */
				//#pragma surface surf Standard vertex:vert alpha
				//#pragma target 3.0

				sampler2D _MainTex;
				sampler2D _AoTex;
				sampler2D _ColorTex;

				int _Atlas;
				int _Activity;
				int _FMRI;
				half _Glossiness;
				half _Metallic;
				fixed4 _Color;
				float _Amount;
				float _MaxRadius;

				uniform int _StrongCuts;
				uniform int _CutCount;
				uniform float3 _CutPoints[20];
				uniform float3 _CutNormals[20];
				uniform float3 _Center;

				struct Input
				{
					float4 pos : SV_POSITION;
					float2 uv_MainTex : TEXCOORD0;
					float2 uv2_AoTex :   TEXCOORD1;
					float2 uv3_ColorTex :   TEXCOORD2;
					float4 vertex_col : COLOR;
					float3 worldPos;
				};

				void vert(inout appdata_full v)
				{
					float3 normal = v.vertex.xyz - _Center;
					float norm = sqrt(normal.x * normal.x + normal.y * normal.y + normal.z * normal.z);
					normal = float3(normal.x / norm, normal.y / norm, normal.z / norm);
					//normal = (1 - _Amount) * v.normal + _Amount * normal;

					v.vertex.xyz += normal * (_MaxRadius - norm) * _Amount;
					v.normal = _Amount * normal + (1 - _Amount) * v.normal;
				}

				float is_clipped(Input IN)
				{
					float3 localPos = IN.worldPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
					float clipping = 1;
					if (!_StrongCuts)
					{
						for (int i = 0; i < _CutCount && i < 20; ++i)
						{
							int value = sign(dot(_CutNormals[i], _CutPoints[i] - localPos));
							if (value < 0)
							{
								clipping = -1;
							}
							else
							{
								clipping = 1;
								break;
							}
						}
					}
					else
					{
						for (int i = 0; i < _CutCount && i < 20; ++i)
						{
							int value = sign(dot(_CutNormals[i], _CutPoints[i] - localPos));
							if (value < 0)
							{
								clipping = -1;
								break;
							}
						}
					}
					return clipping;
				}

				void display_atlas(Input IN, inout SurfaceOutputStandard o)
				{
					float color = IN.vertex_col.a;
					fixed4 col = (1 - color) * tex2D(_MainTex, IN.uv_MainTex) + (color * IN.vertex_col.rgba);
					col *= _Color;
					o.Albedo = col.rgb;
				}

				void display_ieeg(Input IN, inout SurfaceOutputStandard o)
				{
					fixed4 col;
					fixed4 ao = tex2D(_AoTex, IN.uv2_AoTex);
					float color = ao.r * 2.5; // boost alpha (because of low tri mesh density compated to cuts textures)
					if (color > 1) color = 1;
					col = (1 - color) * tex2D(_MainTex, IN.uv_MainTex) + (color)* tex2D(_ColorTex, IN.uv3_ColorTex);
					col *= _Color;
					o.Albedo = col.rgb;
				}

				void surf(Input IN, inout SurfaceOutputStandard o)
				{
					clip(is_clipped(IN));

					if ((_Atlas | _FMRI))
					{
						display_atlas(IN, o);
					}
					else
					{
						display_ieeg(IN, o);
					}

					o.Alpha = _Color.a;
					o.Metallic = _Metallic;
					o.Smoothness = _Glossiness;
				}

			

// vertex-to-fragment interpolation data
// no lightmaps:
#ifndef LIGHTMAP_ON
// half-precision fragment shader registers:
#ifdef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
#define FOG_COMBINED_WITH_WORLD_POS
struct v2f_surf {
  UNITY_POSITION(pos);
  float4 pack0 : TEXCOORD0; // _MainTex _AoTex
  float2 pack1 : TEXCOORD1; // _ColorTex
  float3 worldNormal : TEXCOORD2;
  float4 worldPos : TEXCOORD3;
  fixed4 color : COLOR0;
  #if UNITY_SHOULD_SAMPLE_SH
  half3 sh : TEXCOORD4; // SH
  #endif
  DECLARE_LIGHT_COORDS(5)
  #if SHADER_TARGET >= 30
  float4 lmap : TEXCOORD6;
  #endif
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};
#endif
// high-precision fragment shader registers:
#ifndef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
struct v2f_surf {
  UNITY_POSITION(pos);
  float4 pack0 : TEXCOORD0; // _MainTex _AoTex
  float2 pack1 : TEXCOORD1; // _ColorTex
  float3 worldNormal : TEXCOORD2;
  float3 worldPos : TEXCOORD3;
  fixed4 color : COLOR0;
  #if UNITY_SHOULD_SAMPLE_SH
  half3 sh : TEXCOORD4; // SH
  #endif
  UNITY_FOG_COORDS(5)
  DECLARE_LIGHT_COORDS(6)
  #if SHADER_TARGET >= 30
  float4 lmap : TEXCOORD7;
  #endif
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};
#endif
#endif
// with lightmaps:
#ifdef LIGHTMAP_ON
// half-precision fragment shader registers:
#ifdef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
#define FOG_COMBINED_WITH_WORLD_POS
struct v2f_surf {
  UNITY_POSITION(pos);
  float4 pack0 : TEXCOORD0; // _MainTex _AoTex
  float2 pack1 : TEXCOORD1; // _ColorTex
  float3 worldNormal : TEXCOORD2;
  float4 worldPos : TEXCOORD3;
  fixed4 color : COLOR0;
  float4 lmap : TEXCOORD4;
  DECLARE_LIGHT_COORDS(5)
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};
#endif
// high-precision fragment shader registers:
#ifndef UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS
struct v2f_surf {
  UNITY_POSITION(pos);
  float4 pack0 : TEXCOORD0; // _MainTex _AoTex
  float2 pack1 : TEXCOORD1; // _ColorTex
  float3 worldNormal : TEXCOORD2;
  float3 worldPos : TEXCOORD3;
  fixed4 color : COLOR0;
  float4 lmap : TEXCOORD4;
  UNITY_FOG_COORDS(5)
  DECLARE_LIGHT_COORDS(6)
  #ifdef DIRLIGHTMAP_COMBINED
  float3 tSpace0 : TEXCOORD7;
  float3 tSpace1 : TEXCOORD8;
  float3 tSpace2 : TEXCOORD9;
  #endif
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};
#endif
#endif
float4 _MainTex_ST;
float4 _AoTex_ST;
float4 _ColorTex_ST;

// vertex shader
v2f_surf vert_surf (appdata_full v) {
  UNITY_SETUP_INSTANCE_ID(v);
  v2f_surf o;
  UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
  UNITY_TRANSFER_INSTANCE_ID(v,o);
  UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  vert (v);
  o.pos = UnityObjectToClipPos(v.vertex);
  o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
  o.pack0.zw = TRANSFORM_TEX(v.texcoord1, _AoTex);
  o.pack1.xy = TRANSFORM_TEX(v.texcoord2, _ColorTex);
  float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
  float3 worldNormal = UnityObjectToWorldNormal(v.normal);
  #if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
  fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
  fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
  fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
  #endif
  #if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED) && !defined(UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS)
  o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
  o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
  o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
  #endif
  o.worldPos.xyz = worldPos;
  o.worldNormal = worldNormal;
  o.color = v.color;
  #ifdef DYNAMICLIGHTMAP_ON
  o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
  #endif
  #ifdef LIGHTMAP_ON
  o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
  #endif

  // SH/ambient and vertex lights
  #ifndef LIGHTMAP_ON
    #if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
      o.sh = 0;
      // Approximated illumination from non-important point lights
      #ifdef VERTEXLIGHT_ON
        o.sh += Shade4PointLights (
          unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
          unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
          unity_4LightAtten0, worldPos, worldNormal);
      #endif
      o.sh = ShadeSHPerVertex (worldNormal, o.sh);
    #endif
  #endif // !LIGHTMAP_ON

  COMPUTE_LIGHT_COORDS(o); // pass light cookie coordinates to pixel shader
  #ifdef FOG_COMBINED_WITH_TSPACE
    UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o,o.pos); // pass fog coordinates to pixel shader
  #elif defined (FOG_COMBINED_WITH_WORLD_POS)
    UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o,o.pos); // pass fog coordinates to pixel shader
  #else
    UNITY_TRANSFER_FOG(o,o.pos); // pass fog coordinates to pixel shader
  #endif
  return o;
}

// fragment shader
fixed4 frag_surf (v2f_surf IN) : SV_Target {
  UNITY_SETUP_INSTANCE_ID(IN);
  UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
  // prepare and unpack data
  Input surfIN;
  #ifdef FOG_COMBINED_WITH_TSPACE
    UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
  #elif defined (FOG_COMBINED_WITH_WORLD_POS)
    UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
  #else
    UNITY_EXTRACT_FOG(IN);
  #endif
  UNITY_INITIALIZE_OUTPUT(Input,surfIN);
  surfIN.pos.x = 1.0;
  surfIN.uv_MainTex.x = 1.0;
  surfIN.uv2_AoTex.x = 1.0;
  surfIN.uv3_ColorTex.x = 1.0;
  surfIN.vertex_col.x = 1.0;
  surfIN.worldPos.x = 1.0;
  surfIN.uv_MainTex = IN.pack0.xy;
  surfIN.uv2_AoTex = IN.pack0.zw;
  surfIN.uv3_ColorTex = IN.pack1.xy;
  float3 worldPos = IN.worldPos.xyz;
  #ifndef USING_DIRECTIONAL_LIGHT
    fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
  #else
    fixed3 lightDir = _WorldSpaceLightPos0.xyz;
  #endif
  float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
  surfIN.worldPos = worldPos;
  surfIN.vertex_col = IN.color;
  #ifdef UNITY_COMPILER_HLSL
  SurfaceOutputStandard o = (SurfaceOutputStandard)0;
  #else
  SurfaceOutputStandard o;
  #endif
  o.Albedo = 0.0;
  o.Emission = 0.0;
  o.Alpha = 0.0;
  o.Occlusion = 1.0;
  fixed3 normalWorldVertex = fixed3(0,0,1);
  o.Normal = IN.worldNormal;
  normalWorldVertex = IN.worldNormal;

  // call surface function
  surf (surfIN, o);

  // compute lighting & shadowing factor
  UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
  fixed4 c = 0;

  // Setup lighting environment
  UnityGI gi;
  UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
  gi.indirect.diffuse = 0;
  gi.indirect.specular = 0;
  gi.light.color = _LightColor0.rgb;
  gi.light.dir = lightDir;
  // Call GI (lightmaps/SH/reflections) lighting function
  UnityGIInput giInput;
  UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
  giInput.light = gi.light;
  giInput.worldPos = worldPos;
  giInput.worldViewDir = worldViewDir;
  giInput.atten = atten;
  #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
    giInput.lightmapUV = IN.lmap;
  #else
    giInput.lightmapUV = 0.0;
  #endif
  #if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
    giInput.ambient = IN.sh;
  #else
    giInput.ambient.rgb = 0.0;
  #endif
  giInput.probeHDR[0] = unity_SpecCube0_HDR;
  giInput.probeHDR[1] = unity_SpecCube1_HDR;
  #if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
    giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
  #endif
  #ifdef UNITY_SPECCUBE_BOX_PROJECTION
    giInput.boxMax[0] = unity_SpecCube0_BoxMax;
    giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
    giInput.boxMax[1] = unity_SpecCube1_BoxMax;
    giInput.boxMin[1] = unity_SpecCube1_BoxMin;
    giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
  #endif
  LightingStandard_GI(o, giInput, gi);

  // realtime lighting: call lighting function
  c += LightingStandard (o, worldViewDir, gi);
  UNITY_APPLY_FOG(_unity_fogCoord, c); // apply fog
  return c;
}


#endif


ENDCG

}

	// ---- forward rendering additive lights pass:
	Pass {
		Name "FORWARD"
		Tags { "LightMode" = "ForwardAdd" }
		ZWrite Off Blend One One
		Blend One One

CGPROGRAM
// compile directives
#pragma vertex vert_surf
#pragma fragment frag_surf
#pragma target 3.0
#pragma multi_compile_instancing
#pragma multi_compile_fog
#pragma skip_variants INSTANCING_ON
#pragma multi_compile_fwdadd noshadow
#include "HLSLSupport.cginc"
#define UNITY_INSTANCED_LOD_FADE
#define UNITY_INSTANCED_SH
#define UNITY_INSTANCED_LIGHTMAPSTS
#define UNITY_INSTANCED_RENDERER_BOUNDS
#include "UnityShaderVariables.cginc"
#include "UnityShaderUtilities.cginc"
// -------- variant for: <when no other keywords are defined>
#if !defined(INSTANCING_ON)
// Surface shader code generated based on:
// vertex modifier: 'vert'
// writes to per-pixel normal: no
// writes to emission: no
// writes to occlusion: no
// needs world space reflection vector: no
// needs world space normal vector: no
// needs screen space position: no
// needs world space position: YES
// needs view direction: no
// needs world space view direction: no
// needs world space position for lighting: YES
// needs world space view direction for lighting: YES
// needs world space view direction for lightmaps: no
// needs vertex color: YES
// needs VFACE: no
// needs SV_IsFrontFace: no
// passes tangent-to-world matrix to pixel shader: no
// reads from normal: no
// 3 texcoords actually used
//   float2 _MainTex
//   float2 _AoTex
//   float2 _ColorTex
#define _ALPHAPREMULTIPLY_ON 1
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

// Original surface shader snippet:
#line 20 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif
/* UNITY: Original start of shader */
				//#pragma surface surf Standard vertex:vert alpha
				//#pragma target 3.0

				sampler2D _MainTex;
				sampler2D _AoTex;
				sampler2D _ColorTex;

				int _Atlas;
				int _Activity;
				int _FMRI;
				half _Glossiness;
				half _Metallic;
				fixed4 _Color;
				float _Amount;
				float _MaxRadius;

				uniform int _StrongCuts;
				uniform int _CutCount;
				uniform float3 _CutPoints[20];
				uniform float3 _CutNormals[20];
				uniform float3 _Center;

				struct Input
				{
					float4 pos : SV_POSITION;
					float2 uv_MainTex : TEXCOORD0;
					float2 uv2_AoTex :   TEXCOORD1;
					float2 uv3_ColorTex :   TEXCOORD2;
					float4 vertex_col : COLOR;
					float3 worldPos;
				};

				void vert(inout appdata_full v)
				{
					float3 normal = v.vertex.xyz - _Center;
					float norm = sqrt(normal.x * normal.x + normal.y * normal.y + normal.z * normal.z);
					normal = float3(normal.x / norm, normal.y / norm, normal.z / norm);
					//normal = (1 - _Amount) * v.normal + _Amount * normal;

					v.vertex.xyz += normal * (_MaxRadius - norm) * _Amount;
					v.normal = _Amount * normal + (1 - _Amount) * v.normal;
				}

				float is_clipped(Input IN)
				{
					float3 localPos = IN.worldPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
					float clipping = 1;
					if (!_StrongCuts)
					{
						for (int i = 0; i < _CutCount && i < 20; ++i)
						{
							int value = sign(dot(_CutNormals[i], _CutPoints[i] - localPos));
							if (value < 0)
							{
								clipping = -1;
							}
							else
							{
								clipping = 1;
								break;
							}
						}
					}
					else
					{
						for (int i = 0; i < _CutCount && i < 20; ++i)
						{
							int value = sign(dot(_CutNormals[i], _CutPoints[i] - localPos));
							if (value < 0)
							{
								clipping = -1;
								break;
							}
						}
					}
					return clipping;
				}

				void display_atlas(Input IN, inout SurfaceOutputStandard o)
				{
					float color = IN.vertex_col.a;
					fixed4 col = (1 - color) * tex2D(_MainTex, IN.uv_MainTex) + (color * IN.vertex_col.rgba);
					col *= _Color;
					o.Albedo = col.rgb;
				}

				void display_ieeg(Input IN, inout SurfaceOutputStandard o)
				{
					fixed4 col;
					fixed4 ao = tex2D(_AoTex, IN.uv2_AoTex);
					float color = ao.r * 2.5; // boost alpha (because of low tri mesh density compated to cuts textures)
					if (color > 1) color = 1;
					col = (1 - color) * tex2D(_MainTex, IN.uv_MainTex) + (color)* tex2D(_ColorTex, IN.uv3_ColorTex);
					col *= _Color;
					o.Albedo = col.rgb;
				}

				void surf(Input IN, inout SurfaceOutputStandard o)
				{
					clip(is_clipped(IN));

					if ((_Atlas | _FMRI))
					{
						display_atlas(IN, o);
					}
					else
					{
						display_ieeg(IN, o);
					}

					o.Alpha = _Color.a;
					o.Metallic = _Metallic;
					o.Smoothness = _Glossiness;
				}

			

// vertex-to-fragment interpolation data
struct v2f_surf {
  UNITY_POSITION(pos);
  float4 pack0 : TEXCOORD0; // _MainTex _AoTex
  float2 pack1 : TEXCOORD1; // _ColorTex
  float3 worldNormal : TEXCOORD2;
  float3 worldPos : TEXCOORD3;
  fixed4 color : COLOR0;
  DECLARE_LIGHT_COORDS(4)
  UNITY_FOG_COORDS(5)
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};
float4 _MainTex_ST;
float4 _AoTex_ST;
float4 _ColorTex_ST;

// vertex shader
v2f_surf vert_surf (appdata_full v) {
  UNITY_SETUP_INSTANCE_ID(v);
  v2f_surf o;
  UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
  UNITY_TRANSFER_INSTANCE_ID(v,o);
  UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  vert (v);
  o.pos = UnityObjectToClipPos(v.vertex);
  o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
  o.pack0.zw = TRANSFORM_TEX(v.texcoord1, _AoTex);
  o.pack1.xy = TRANSFORM_TEX(v.texcoord2, _ColorTex);
  float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
  float3 worldNormal = UnityObjectToWorldNormal(v.normal);
  o.worldPos.xyz = worldPos;
  o.worldNormal = worldNormal;
  o.color = v.color;

  COMPUTE_LIGHT_COORDS(o); // pass light cookie coordinates to pixel shader
  UNITY_TRANSFER_FOG(o,o.pos); // pass fog coordinates to pixel shader
  return o;
}

// fragment shader
fixed4 frag_surf (v2f_surf IN) : SV_Target {
  UNITY_SETUP_INSTANCE_ID(IN);
  UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
  // prepare and unpack data
  Input surfIN;
  #ifdef FOG_COMBINED_WITH_TSPACE
    UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
  #elif defined (FOG_COMBINED_WITH_WORLD_POS)
    UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
  #else
    UNITY_EXTRACT_FOG(IN);
  #endif
  UNITY_INITIALIZE_OUTPUT(Input,surfIN);
  surfIN.pos.x = 1.0;
  surfIN.uv_MainTex.x = 1.0;
  surfIN.uv2_AoTex.x = 1.0;
  surfIN.uv3_ColorTex.x = 1.0;
  surfIN.vertex_col.x = 1.0;
  surfIN.worldPos.x = 1.0;
  surfIN.uv_MainTex = IN.pack0.xy;
  surfIN.uv2_AoTex = IN.pack0.zw;
  surfIN.uv3_ColorTex = IN.pack1.xy;
  float3 worldPos = IN.worldPos.xyz;
  #ifndef USING_DIRECTIONAL_LIGHT
    fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
  #else
    fixed3 lightDir = _WorldSpaceLightPos0.xyz;
  #endif
  float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
  surfIN.worldPos = worldPos;
  surfIN.vertex_col = IN.color;
  #ifdef UNITY_COMPILER_HLSL
  SurfaceOutputStandard o = (SurfaceOutputStandard)0;
  #else
  SurfaceOutputStandard o;
  #endif
  o.Albedo = 0.0;
  o.Emission = 0.0;
  o.Alpha = 0.0;
  o.Occlusion = 1.0;
  fixed3 normalWorldVertex = fixed3(0,0,1);
  o.Normal = IN.worldNormal;
  normalWorldVertex = IN.worldNormal;

  // call surface function
  surf (surfIN, o);
  UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
  fixed4 c = 0;

  // Setup lighting environment
  UnityGI gi;
  UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
  gi.indirect.diffuse = 0;
  gi.indirect.specular = 0;
  gi.light.color = _LightColor0.rgb;
  gi.light.dir = lightDir;
  gi.light.color *= atten;
  c += LightingStandard (o, worldViewDir, gi);
  UNITY_APPLY_FOG(_unity_fogCoord, c); // apply fog
  return c;
}


#endif


ENDCG

}

	// ---- meta information extraction pass:
	Pass {
		Name "Meta"
		Tags { "LightMode" = "Meta" }
		Cull Off

CGPROGRAM
// compile directives
#pragma vertex vert_surf
#pragma fragment frag_surf
#pragma target 3.0
#pragma multi_compile_instancing
#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
#pragma shader_feature EDITOR_VISUALIZATION

#include "HLSLSupport.cginc"
#define UNITY_INSTANCED_LOD_FADE
#define UNITY_INSTANCED_SH
#define UNITY_INSTANCED_LIGHTMAPSTS
#define UNITY_INSTANCED_RENDERER_BOUNDS
#include "UnityShaderVariables.cginc"
#include "UnityShaderUtilities.cginc"
// -------- variant for: <when no other keywords are defined>
#if !defined(INSTANCING_ON)
// Surface shader code generated based on:
// vertex modifier: 'vert'
// writes to per-pixel normal: no
// writes to emission: no
// writes to occlusion: no
// needs world space reflection vector: no
// needs world space normal vector: no
// needs screen space position: no
// needs world space position: YES
// needs view direction: no
// needs world space view direction: no
// needs world space position for lighting: YES
// needs world space view direction for lighting: YES
// needs world space view direction for lightmaps: no
// needs vertex color: YES
// needs VFACE: no
// needs SV_IsFrontFace: no
// passes tangent-to-world matrix to pixel shader: no
// reads from normal: no
// 3 texcoords actually used
//   float2 _MainTex
//   float2 _AoTex
//   float2 _ColorTex
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "UnityPBSLighting.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

// Original surface shader snippet:
#line 20 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif
/* UNITY: Original start of shader */
				//#pragma surface surf Standard vertex:vert alpha
				//#pragma target 3.0

				sampler2D _MainTex;
				sampler2D _AoTex;
				sampler2D _ColorTex;

				int _Atlas;
				int _Activity;
				int _FMRI;
				half _Glossiness;
				half _Metallic;
				fixed4 _Color;
				float _Amount;
				float _MaxRadius;

				uniform int _StrongCuts;
				uniform int _CutCount;
				uniform float3 _CutPoints[20];
				uniform float3 _CutNormals[20];
				uniform float3 _Center;

				struct Input
				{
					float4 pos : SV_POSITION;
					float2 uv_MainTex : TEXCOORD0;
					float2 uv2_AoTex :   TEXCOORD1;
					float2 uv3_ColorTex :   TEXCOORD2;
					float4 vertex_col : COLOR;
					float3 worldPos;
				};

				void vert(inout appdata_full v)
				{
					float3 normal = v.vertex.xyz - _Center;
					float norm = sqrt(normal.x * normal.x + normal.y * normal.y + normal.z * normal.z);
					normal = float3(normal.x / norm, normal.y / norm, normal.z / norm);
					//normal = (1 - _Amount) * v.normal + _Amount * normal;

					v.vertex.xyz += normal * (_MaxRadius - norm) * _Amount;
					v.normal = _Amount * normal + (1 - _Amount) * v.normal;
				}

				float is_clipped(Input IN)
				{
					float3 localPos = IN.worldPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
					float clipping = 1;
					if (!_StrongCuts)
					{
						for (int i = 0; i < _CutCount && i < 20; ++i)
						{
							int value = sign(dot(_CutNormals[i], _CutPoints[i] - localPos));
							if (value < 0)
							{
								clipping = -1;
							}
							else
							{
								clipping = 1;
								break;
							}
						}
					}
					else
					{
						for (int i = 0; i < _CutCount && i < 20; ++i)
						{
							int value = sign(dot(_CutNormals[i], _CutPoints[i] - localPos));
							if (value < 0)
							{
								clipping = -1;
								break;
							}
						}
					}
					return clipping;
				}

				void display_atlas(Input IN, inout SurfaceOutputStandard o)
				{
					float color = IN.vertex_col.a;
					fixed4 col = (1 - color) * tex2D(_MainTex, IN.uv_MainTex) + (color * IN.vertex_col.rgba);
					col *= _Color;
					o.Albedo = col.rgb;
				}

				void display_ieeg(Input IN, inout SurfaceOutputStandard o)
				{
					fixed4 col;
					fixed4 ao = tex2D(_AoTex, IN.uv2_AoTex);
					float color = ao.r * 2.5; // boost alpha (because of low tri mesh density compated to cuts textures)
					if (color > 1) color = 1;
					col = (1 - color) * tex2D(_MainTex, IN.uv_MainTex) + (color)* tex2D(_ColorTex, IN.uv3_ColorTex);
					col *= _Color;
					o.Albedo = col.rgb;
				}

				void surf(Input IN, inout SurfaceOutputStandard o)
				{
					clip(is_clipped(IN));

					if ((_Atlas | _FMRI))
					{
						display_atlas(IN, o);
					}
					else
					{
						display_ieeg(IN, o);
					}

					o.Alpha = _Color.a;
					o.Metallic = _Metallic;
					o.Smoothness = _Glossiness;
				}

			
#include "UnityMetaPass.cginc"

// vertex-to-fragment interpolation data
struct v2f_surf {
  UNITY_POSITION(pos);
  float4 pack0 : TEXCOORD0; // _MainTex _AoTex
  float2 pack1 : TEXCOORD1; // _ColorTex
  float3 worldPos : TEXCOORD2;
  fixed4 color : COLOR0;
#ifdef EDITOR_VISUALIZATION
  float2 vizUV : TEXCOORD3;
  float4 lightCoord : TEXCOORD4;
#endif
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};
float4 _MainTex_ST;
float4 _AoTex_ST;
float4 _ColorTex_ST;

// vertex shader
v2f_surf vert_surf (appdata_full v) {
  UNITY_SETUP_INSTANCE_ID(v);
  v2f_surf o;
  UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
  UNITY_TRANSFER_INSTANCE_ID(v,o);
  UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  vert (v);
  o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
#ifdef EDITOR_VISUALIZATION
  o.vizUV = 0;
  o.lightCoord = 0;
  if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
    o.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, v.texcoord.xy, v.texcoord1.xy, v.texcoord2.xy, unity_EditorViz_Texture_ST);
  else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
  {
    o.vizUV = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    o.lightCoord = mul(unity_EditorViz_WorldToLight, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)));
  }
#endif
  o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
  o.pack0.zw = TRANSFORM_TEX(v.texcoord1, _AoTex);
  o.pack1.xy = TRANSFORM_TEX(v.texcoord2, _ColorTex);
  float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
  float3 worldNormal = UnityObjectToWorldNormal(v.normal);
  o.worldPos.xyz = worldPos;
  o.color = v.color;
  return o;
}

// fragment shader
fixed4 frag_surf (v2f_surf IN) : SV_Target {
  UNITY_SETUP_INSTANCE_ID(IN);
  UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
  // prepare and unpack data
  Input surfIN;
  #ifdef FOG_COMBINED_WITH_TSPACE
    UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
  #elif defined (FOG_COMBINED_WITH_WORLD_POS)
    UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
  #else
    UNITY_EXTRACT_FOG(IN);
  #endif
  UNITY_INITIALIZE_OUTPUT(Input,surfIN);
  surfIN.pos.x = 1.0;
  surfIN.uv_MainTex.x = 1.0;
  surfIN.uv2_AoTex.x = 1.0;
  surfIN.uv3_ColorTex.x = 1.0;
  surfIN.vertex_col.x = 1.0;
  surfIN.worldPos.x = 1.0;
  surfIN.uv_MainTex = IN.pack0.xy;
  surfIN.uv2_AoTex = IN.pack0.zw;
  surfIN.uv3_ColorTex = IN.pack1.xy;
  float3 worldPos = IN.worldPos.xyz;
  #ifndef USING_DIRECTIONAL_LIGHT
    fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
  #else
    fixed3 lightDir = _WorldSpaceLightPos0.xyz;
  #endif
  surfIN.worldPos = worldPos;
  surfIN.vertex_col = IN.color;
  #ifdef UNITY_COMPILER_HLSL
  SurfaceOutputStandard o = (SurfaceOutputStandard)0;
  #else
  SurfaceOutputStandard o;
  #endif
  o.Albedo = 0.0;
  o.Emission = 0.0;
  o.Alpha = 0.0;
  o.Occlusion = 1.0;
  fixed3 normalWorldVertex = fixed3(0,0,1);

  // call surface function
  surf (surfIN, o);
  UnityMetaInput metaIN;
  UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaIN);
  metaIN.Albedo = o.Albedo;
  metaIN.Emission = o.Emission;
#ifdef EDITOR_VISUALIZATION
  metaIN.VizUV = IN.vizUV;
  metaIN.LightCoord = IN.lightCoord;
#endif
  return UnityMetaFragment(metaIN);
}


#endif

// -------- variant for: INSTANCING_ON 
#if defined(INSTANCING_ON)
// Surface shader code generated based on:
// vertex modifier: 'vert'
// writes to per-pixel normal: no
// writes to emission: no
// writes to occlusion: no
// needs world space reflection vector: no
// needs world space normal vector: no
// needs screen space position: no
// needs world space position: YES
// needs view direction: no
// needs world space view direction: no
// needs world space position for lighting: YES
// needs world space view direction for lighting: YES
// needs world space view direction for lightmaps: no
// needs vertex color: YES
// needs VFACE: no
// needs SV_IsFrontFace: no
// passes tangent-to-world matrix to pixel shader: no
// reads from normal: no
// 3 texcoords actually used
//   float2 _MainTex
//   float2 _AoTex
//   float2 _ColorTex
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "UnityPBSLighting.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

// Original surface shader snippet:
#line 20 ""
#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
#endif
/* UNITY: Original start of shader */
				//#pragma surface surf Standard vertex:vert alpha
				//#pragma target 3.0

				sampler2D _MainTex;
				sampler2D _AoTex;
				sampler2D _ColorTex;

				int _Atlas;
				int _Activity;
				int _FMRI;
				half _Glossiness;
				half _Metallic;
				fixed4 _Color;
				float _Amount;
				float _MaxRadius;

				uniform int _StrongCuts;
				uniform int _CutCount;
				uniform float3 _CutPoints[20];
				uniform float3 _CutNormals[20];
				uniform float3 _Center;

				struct Input
				{
					float4 pos : SV_POSITION;
					float2 uv_MainTex : TEXCOORD0;
					float2 uv2_AoTex :   TEXCOORD1;
					float2 uv3_ColorTex :   TEXCOORD2;
					float4 vertex_col : COLOR;
					float3 worldPos;
				};

				void vert(inout appdata_full v)
				{
					float3 normal = v.vertex.xyz - _Center;
					float norm = sqrt(normal.x * normal.x + normal.y * normal.y + normal.z * normal.z);
					normal = float3(normal.x / norm, normal.y / norm, normal.z / norm);
					//normal = (1 - _Amount) * v.normal + _Amount * normal;

					v.vertex.xyz += normal * (_MaxRadius - norm) * _Amount;
					v.normal = _Amount * normal + (1 - _Amount) * v.normal;
				}

				float is_clipped(Input IN)
				{
					float3 localPos = IN.worldPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
					float clipping = 1;
					if (!_StrongCuts)
					{
						for (int i = 0; i < _CutCount && i < 20; ++i)
						{
							int value = sign(dot(_CutNormals[i], _CutPoints[i] - localPos));
							if (value < 0)
							{
								clipping = -1;
							}
							else
							{
								clipping = 1;
								break;
							}
						}
					}
					else
					{
						for (int i = 0; i < _CutCount && i < 20; ++i)
						{
							int value = sign(dot(_CutNormals[i], _CutPoints[i] - localPos));
							if (value < 0)
							{
								clipping = -1;
								break;
							}
						}
					}
					return clipping;
				}

				void display_atlas(Input IN, inout SurfaceOutputStandard o)
				{
					float color = IN.vertex_col.a;
					fixed4 col = (1 - color) * tex2D(_MainTex, IN.uv_MainTex) + (color * IN.vertex_col.rgba);
					col *= _Color;
					o.Albedo = col.rgb;
				}

				void display_ieeg(Input IN, inout SurfaceOutputStandard o)
				{
					fixed4 col;
					fixed4 ao = tex2D(_AoTex, IN.uv2_AoTex);
					float color = ao.r * 2.5; // boost alpha (because of low tri mesh density compated to cuts textures)
					if (color > 1) color = 1;
					col = (1 - color) * tex2D(_MainTex, IN.uv_MainTex) + (color)* tex2D(_ColorTex, IN.uv3_ColorTex);
					col *= _Color;
					o.Albedo = col.rgb;
				}

				void surf(Input IN, inout SurfaceOutputStandard o)
				{
					clip(is_clipped(IN));

					if ((_Atlas | _FMRI))
					{
						display_atlas(IN, o);
					}
					else
					{
						display_ieeg(IN, o);
					}

					o.Alpha = _Color.a;
					o.Metallic = _Metallic;
					o.Smoothness = _Glossiness;
				}

			
#include "UnityMetaPass.cginc"

// vertex-to-fragment interpolation data
struct v2f_surf {
  UNITY_POSITION(pos);
  float4 pack0 : TEXCOORD0; // _MainTex _AoTex
  float2 pack1 : TEXCOORD1; // _ColorTex
  float3 worldPos : TEXCOORD2;
  fixed4 color : COLOR0;
#ifdef EDITOR_VISUALIZATION
  float2 vizUV : TEXCOORD3;
  float4 lightCoord : TEXCOORD4;
#endif
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};
float4 _MainTex_ST;
float4 _AoTex_ST;
float4 _ColorTex_ST;

// vertex shader
v2f_surf vert_surf (appdata_full v) {
  UNITY_SETUP_INSTANCE_ID(v);
  v2f_surf o;
  UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
  UNITY_TRANSFER_INSTANCE_ID(v,o);
  UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  vert (v);
  o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
#ifdef EDITOR_VISUALIZATION
  o.vizUV = 0;
  o.lightCoord = 0;
  if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
    o.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, v.texcoord.xy, v.texcoord1.xy, v.texcoord2.xy, unity_EditorViz_Texture_ST);
  else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
  {
    o.vizUV = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    o.lightCoord = mul(unity_EditorViz_WorldToLight, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)));
  }
#endif
  o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
  o.pack0.zw = TRANSFORM_TEX(v.texcoord1, _AoTex);
  o.pack1.xy = TRANSFORM_TEX(v.texcoord2, _ColorTex);
  float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
  float3 worldNormal = UnityObjectToWorldNormal(v.normal);
  o.worldPos.xyz = worldPos;
  o.color = v.color;
  return o;
}

// fragment shader
fixed4 frag_surf (v2f_surf IN) : SV_Target {
  UNITY_SETUP_INSTANCE_ID(IN);
  UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
  // prepare and unpack data
  Input surfIN;
  #ifdef FOG_COMBINED_WITH_TSPACE
    UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
  #elif defined (FOG_COMBINED_WITH_WORLD_POS)
    UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
  #else
    UNITY_EXTRACT_FOG(IN);
  #endif
  UNITY_INITIALIZE_OUTPUT(Input,surfIN);
  surfIN.pos.x = 1.0;
  surfIN.uv_MainTex.x = 1.0;
  surfIN.uv2_AoTex.x = 1.0;
  surfIN.uv3_ColorTex.x = 1.0;
  surfIN.vertex_col.x = 1.0;
  surfIN.worldPos.x = 1.0;
  surfIN.uv_MainTex = IN.pack0.xy;
  surfIN.uv2_AoTex = IN.pack0.zw;
  surfIN.uv3_ColorTex = IN.pack1.xy;
  float3 worldPos = IN.worldPos.xyz;
  #ifndef USING_DIRECTIONAL_LIGHT
    fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
  #else
    fixed3 lightDir = _WorldSpaceLightPos0.xyz;
  #endif
  surfIN.worldPos = worldPos;
  surfIN.vertex_col = IN.color;
  #ifdef UNITY_COMPILER_HLSL
  SurfaceOutputStandard o = (SurfaceOutputStandard)0;
  #else
  SurfaceOutputStandard o;
  #endif
  o.Albedo = 0.0;
  o.Emission = 0.0;
  o.Alpha = 0.0;
  o.Occlusion = 1.0;
  fixed3 normalWorldVertex = fixed3(0,0,1);

  // call surface function
  surf (surfIN, o);
  UnityMetaInput metaIN;
  UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaIN);
  metaIN.Albedo = o.Albedo;
  metaIN.Emission = o.Emission;
#ifdef EDITOR_VISUALIZATION
  metaIN.VizUV = IN.vizUV;
  metaIN.LightCoord = IN.lightCoord;
#endif
  return UnityMetaFragment(metaIN);
}


#endif


ENDCG

}

	// ---- end of surface shader generated code

#LINE 138

		}

			FallBack "Diffuse"
}