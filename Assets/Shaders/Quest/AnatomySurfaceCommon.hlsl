// Presentation lighting adapted from P05SurfaceCommon at eb26c323e.
#ifndef HIBOP_QUEST_ANATOMY_SURFACE_COMMON_INCLUDED
#define HIBOP_QUEST_ANATOMY_SURFACE_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "../HBP/Includes/HBPLighting.hlsl"
TEXTURE2D(_ColorTex);
SAMPLER(sampler_ColorTex);
TEXTURE2D(_AoTex);
SAMPLER(sampler_AoTex);
CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
    float _DensityEnabled;
    half _AmbientStrength;
    half _DiffuseStrength;
    half _Smoothness;
CBUFFER_END

struct AnatomyAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 scientificUv : TEXCOORD2;
    float2 alphaUv : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct AnatomyVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    half3 normalWS : TEXCOORD1;
    float2 scientificUv : TEXCOORD2;
    float2 alphaUv : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

AnatomyVaryings AnatomyVertex(AnatomyAttributes input)
{
    AnatomyVaryings output = (AnatomyVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.scientificUv = input.scientificUv;
    output.alphaUv = input.alphaUv;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    return output;
}

half4 AnatomyFragment(AnatomyVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    half3 normal = NormalizeNormalPerPixel(input.normalWS);
    half3 view = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
    half facing = saturate(dot(normal, view));
    half smoothness = saturate(_Smoothness);
    half diffuse = _AmbientStrength + _DiffuseStrength * facing;
    half highlightPower = lerp(8.0h, 40.0h, smoothness);
    half highlight = pow(facing, highlightPower) * (0.36h * smoothness);
    half4 color = half4(_BaseColor.rgb * diffuse + highlight, saturate(_BaseColor.a));
    if (_DensityEnabled > 0.5)
    {
        half3 scientific = SAMPLE_TEXTURE2D(_ColorTex, sampler_ColorTex, input.scientificUv).rgb;
        half opacity = input.alphaUv.y > 0.5 ? 0 : SAMPLE_TEXTURE2D(_AoTex, sampler_AoTex, input.alphaUv).r;
        scientific = HBP_ApplyScientificRelief(scientific, normal, view, _AmbientStrength, _DiffuseStrength, _Smoothness);
        color.rgb = lerp(color.rgb, scientific, saturate(opacity));
    }
    return color;
}

#endif
