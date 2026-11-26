Shader "HBP/TriangleSelectionPreview"
{
    Properties
    {
        _Tint ("Selection color", Color) = (0.13, 0.91, 0.99, 0.2)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+10" "RenderType" = "Transparent" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Back
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/HBP/Includes/HBPClipping.hlsl"

            half4 _Tint;

            struct Varyings
            {
                float4 position : SV_POSITION;
                float3 localPoint : TEXCOORD0;
            };

            Varyings Vertex(float3 position : POSITION)
            {
                Varyings output;
                output.position = TransformObjectToHClip(position);
                output.localPoint = position;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                clip(HBP_ClippingValue(input.localPoint));
                return _Tint;
            }
            ENDHLSL
        }
    }
}
