#ifndef HBP_BRAIN_COMMON_INCLUDED
#define HBP_BRAIN_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "HBPBrainInput.hlsl"
#include "../../Includes/HBPClipping.hlsl"
#include "../../Includes/HBPColor.hlsl"
#include "../../Includes/HBPLighting.hlsl"

struct HBPBrainAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    float2 alphaUv : TEXCOORD1;
    float2 scientificUv : TEXCOORD2;
    float4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct HBPBrainVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionOS : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
    float2 uv : TEXCOORD3;
    float2 alphaUv : TEXCOORD4;
    float2 scientificUv : TEXCOORD5;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void HBP_ExtrudeBrainVertex(inout float3 positionOS, inout float3 normalOS)
{
    float3 radial = positionOS - _Center.xyz;
    float radius = max(length(radial), 0.00001);
    float3 radialNormal = radial / radius;
    positionOS += radialNormal * (_MaxRadius - radius) * saturate(_Amount);
    normalOS = normalize(lerp(normalOS, radialNormal, saturate(_Amount)));
}

HBPBrainVaryings HBP_BrainVertex(HBPBrainAttributes input)
{
    HBPBrainVaryings output = (HBPBrainVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionOS = input.positionOS.xyz;
    float3 normalOS = input.normalOS;
    HBP_ExtrudeBrainVertex(positionOS, normalOS);

    output.positionOS = positionOS;
    output.positionWS = TransformObjectToWorld(positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(normalOS);
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    output.alphaUv = TRANSFORM_TEX(input.alphaUv, _AoTex);
    output.scientificUv = TRANSFORM_TEX(input.scientificUv, _ColorTex);
    output.color = input.color;
    return output;
}

half4 HBP_BrainFragment(HBPBrainVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    clip(HBP_ClippingValue(input.positionOS));

    half3 normalWS = NormalizeNormalPerPixel(input.normalWS);
    float3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 anatomyLinear = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
    anatomyLinear *= _Color.rgb;

    half3 scientificLinear;
    half scientificAlpha;
    if (_Atlas > 0.5 || _FMRI > 0.5)
    {
        scientificLinear = HBP_SRGBToLinear(saturate(input.color.rgb));
        scientificAlpha = saturate(input.color.a);
    }
    else
    {
        scientificLinear = SAMPLE_TEXTURE2D(
            _ColorTex,
            sampler_ColorTex,
            input.scientificUv).rgb;
        scientificAlpha = input.alphaUv.y > 0.5
            ? 0.0
            : SAMPLE_TEXTURE2D(_AoTex, sampler_AoTex, input.alphaUv).r;
    }

    half3 litAnatomy = HBP_ApplySurfaceLighting(
        anatomyLinear,
        normalWS,
        viewDirectionWS,
        _AmbientStrength,
        _DiffuseStrength,
        _Glossiness);
    half3 litScientific = HBP_ApplyScientificRelief(
        scientificLinear,
        normalWS,
        viewDirectionWS,
        _AmbientStrength,
        _DiffuseStrength,
        _Glossiness);
    half3 result = HBP_ComposeScientificColor(
        litAnatomy,
        litScientific,
        scientificAlpha);
    return half4(result, saturate(_Color.a));
}

#endif
