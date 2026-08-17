Shader "Hidden/HBP/Dev/RenderingBaselinePatches"
{
    Properties
    {
        _UniformColor("Uniform color", Color) = (0.25, 0.5, 0.75, 1)
        _SrgbTexture("sRGB texture", 2D) = "white" {}
        _LinearTexture("Linear texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "BaselinePatches"
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One Zero

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_SrgbTexture);
            SAMPLER(sampler_SrgbTexture);
            TEXTURE2D(_LinearTexture);
            SAMPLER(sampler_LinearTexture);
            CBUFFER_START(UnityPerMaterial)
                float4 _UniformColor;
            CBUFFER_END

            struct Attributes
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = TransformObjectToHClip(input.position.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                int column = min((int)floor(input.uv.x * 4.0), 3);
                float4 color;
                if (column == 0)
                {
                    color = _UniformColor;
                }
                else if (column == 1)
                {
                    color = SAMPLE_TEXTURE2D(_SrgbTexture, sampler_SrgbTexture, float2(0.5, 0.5));
                }
                else if (column == 2)
                {
                    color = SAMPLE_TEXTURE2D(_LinearTexture, sampler_LinearTexture, float2(0.5, 0.5));
                }
                else
                {
                    color = input.color;
                }

                color.a = input.uv.y < 0.5 ? 0.5 : 1.0;
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
