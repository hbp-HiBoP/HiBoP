Shader "HBP/Brain"
{
    Properties
    {
        [MainColor] _Color("Color", Color) = (1, 1, 1, 1)
        [MainTexture] _MainTex("Anatomy", 2D) = "white" {}
        [NoScaleOffset] _AoTex("Scientific Alpha", 2D) = "black" {}
        [NoScaleOffset] _ColorTex("Scientific Colormap", 2D) = "white" {}
        _Glossiness("Surface Smoothness", Range(0, 1)) = 0.45
        _Metallic("Legacy Metallic", Range(0, 1)) = 0
        [Toggle] _Atlas("Atlas", Float) = 0
        [Toggle] _Activity("Activity", Float) = 0
        [Toggle] _FMRI("fMRI", Float) = 0
        _Amount("Extrusion Amount", Range(0, 1)) = 0
        _MaxRadius("Maximum Extrusion Radius", Range(0, 100)) = 100
        _AmbientStrength("Anatomy Ambient", Range(0, 1)) = 0.35
        _DiffuseStrength("Anatomy Diffuse", Range(0, 1)) = 0.65
        [HideInInspector] _StrongCuts("Strong Cuts", Int) = 0
        [HideInInspector] _CutCount("Cut Count", Int) = 0
        [HideInInspector] _Center("Brain Center", Vector) = (0, 0, 0, 0)
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
            #pragma vertex HBP_BrainVertex
            #pragma fragment HBP_BrainFragment
            #pragma multi_compile_instancing

            #include "Includes/HBPBrainCommon.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull Back
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex HBP_BrainDepthVertex
            #pragma fragment HBP_BrainDepthFragment
            #pragma multi_compile_instancing

            #include "Includes/HBPBrainDepth.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex HBP_BrainDepthVertex
            #pragma fragment HBP_BrainDepthNormalsFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            #include "Includes/HBPBrainDepth.hlsl"
            ENDHLSL
        }
    }
}
