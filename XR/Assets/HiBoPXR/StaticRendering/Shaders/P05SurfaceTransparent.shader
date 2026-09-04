Shader "HiBoP XR/P05/Surface Transparent"
{
    Properties
    {
        [MainColor] _BaseColor("Surface Color", Color) = (0.72, 0.72, 0.74, 0.42)
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
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
            Cull Back
            ZWrite Off
            ZTest Equal

            HLSLPROGRAM
            #pragma target 3.5
            #pragma only_renderers vulkan d3d11 metal
            #pragma vertex P05Vertex
            #pragma fragment P05Fragment
            #pragma multi_compile_instancing
            #define P05_PREMULTIPLY_ALPHA 1
            #include "P05SurfaceCommon.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
