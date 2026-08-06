#ifndef HBP_BRAIN_DEPTH_INCLUDED
#define HBP_BRAIN_DEPTH_INCLUDED

#include "HBPBrainCommon.hlsl"

struct HBPBrainDepthVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionOS : TEXCOORD0;
    half3 normalWS : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

HBPBrainDepthVaryings HBP_BrainDepthVertex(HBPBrainAttributes input)
{
    HBPBrainDepthVaryings output = (HBPBrainDepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionOS = input.positionOS.xyz;
    float3 normalOS = input.normalOS;
    HBP_ExtrudeBrainVertex(positionOS, normalOS);
    output.positionOS = positionOS;
    output.positionCS = TransformObjectToHClip(positionOS);
    output.normalWS = TransformObjectToWorldNormal(normalOS);
    return output;
}

half HBP_BrainDepthFragment(HBPBrainDepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    clip(HBP_ClippingValue(input.positionOS));
    return input.positionCS.z;
}

half4 HBP_BrainDepthNormalsFragment(HBPBrainDepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    clip(HBP_ClippingValue(input.positionOS));

    float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
#if defined(_GBUFFER_NORMALS_OCT)
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
    return half4(packedNormalWS, 0.0);
#else
    return half4(normalWS, 0.0);
#endif
}

#endif
