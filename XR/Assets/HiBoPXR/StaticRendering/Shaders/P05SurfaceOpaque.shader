Shader "HiBoP XR/P05/Surface Opaque"
{
    Properties
    {
        [MainColor] _BaseColor("Surface Color", Color) = (0.72, 0.72, 0.74, 1)
        _AmbientStrength("Ambient", Range(0, 1)) = 0.35
        _DiffuseStrength("Diffuse", Range(0, 1)) = 0.65
        _Smoothness("Smoothness", Range(0, 1)) = 0.45
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma only_renderers vulkan d3d11 metal
            #pragma vertex P05Vertex
            #pragma fragment P05Fragment
            #pragma multi_compile_instancing
            #include "P05SurfaceCommon.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
