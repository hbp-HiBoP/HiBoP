Shader "HiBoP Quest/Anatomy Opaque"
{
    Properties
    {
        [MainColor] _BaseColor("Surface Color", Color) = (0.72, 0.72, 0.74, 1)
        [NoScaleOffset] _ColorTex("Density colormap", 2D) = "white" {}
        [NoScaleOffset] _AoTex("Scientific opacity", 2D) = "white" {}
        _DensityEnabled("Density enabled", Float) = 0
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
            #pragma vertex AnatomyVertex
            #pragma fragment AnatomyFragment
            #pragma multi_compile_instancing
            #include "AnatomySurfaceCommon.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
