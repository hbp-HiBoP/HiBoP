Shader "HiBoP XR/P12/Desktop Cut"
{
    Properties
    {
        _CutTexture ("Desktop cut", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_CutTexture);
            SAMPLER(sampler_CutTexture);
            TEXTURE2D_ARRAY(_CutTimeline);
            SAMPLER(sampler_CutTimeline);
            StructuredBuffer<uint> _CutTimelineIndex;
            float _UseTimeline;
            float _TimelineInvariant;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                if (_UseTimeline > 0.5)
                {
                    uint layer = _TimelineInvariant > 0.5 ? 0 : _CutTimelineIndex[0];
                    return SAMPLE_TEXTURE2D_ARRAY(_CutTimeline, sampler_CutTimeline, input.uv, layer);
                }
                return SAMPLE_TEXTURE2D(_CutTexture, sampler_CutTexture, input.uv);
            }
            ENDHLSL
        }
    }
}
