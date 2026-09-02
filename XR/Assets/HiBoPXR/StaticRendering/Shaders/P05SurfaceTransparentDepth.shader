Shader "HiBoP XR/P05/Surface Transparent Depth"
{
    Properties
    {
        [MainColor] _BaseColor("Surface Color", Color) = (1, 1, 1, 1)
        _AmbientStrength("Ambient", Range(0, 1)) = 0.35
        _DiffuseStrength("Diffuse", Range(0, 1)) = 0.65
        _Smoothness("Smoothness", Range(0, 1)) = 0.45
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-1"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            ColorMask 0
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma only_renderers vulkan d3d11 metal
            #pragma vertex P05Vertex
            #pragma fragment P05DepthFragment
            #pragma multi_compile_instancing
            #include "P05SurfaceCommon.hlsl"

            half4 P05DepthFragment(P05Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
