#ifndef HIBOP_XR_P05_SURFACE_COMMON_INCLUDED
#define HIBOP_XR_P05_SURFACE_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
    half _AmbientStrength;
    half _DiffuseStrength;
    half _Smoothness;
CBUFFER_END

struct P05Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct P05Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    half3 normalWS : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

P05Varyings P05Vertex(P05Attributes input)
{
    P05Varyings output = (P05Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    return output;
}

half4 P05Fragment(P05Varyings input) : SV_Target
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
#if defined(P05_PREMULTIPLY_ALPHA)
    color.rgb *= color.a;
#endif
    return color;
}

#endif
