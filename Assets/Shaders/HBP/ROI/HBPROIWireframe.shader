Shader "HBP/ROI/AnalyticCage"
{
    Properties
    {
        [MainColor] _WireColor("Cage Color", Color) = (1, 1, 1, 1)
        _ContrastColor("Cage Contrast Color", Color) = (0.04, 0.04, 0.04, 1)
        _WireThickness("Cage Thickness", Range(0, 800)) = 50
        _WireSmoothness("Cage Smoothness", Range(0, 20)) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+1"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirectionWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _WireColor;
                float4 _ContrastColor;
                float _WireThickness;
                float _WireSmoothness;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 sphereDirection = normalize(input.positionOS);
                const float inverseSquareRootTwo = 0.70710678;
                float primaryCircleDistance = min(
                    abs(sphereDirection.y),
                    min(abs(sphereDirection.x), abs(sphereDirection.z)));
                float diagonalMeridianDistance = min(
                    abs(sphereDirection.x + sphereDirection.z),
                    abs(sphereDirection.x - sphereDirection.z)) * inverseSquareRootTwo;
                float latitudeDistance = abs(abs(sphereDirection.y) - inverseSquareRootTwo);
                float intermediateCircleDistance = min(diagonalMeridianDistance, latitudeDistance);
                float silhouetteDistance = abs(dot(normalize(input.normalWS), normalize(input.viewDirectionWS)));

                float coreWidth = max(_WireThickness / 50.0, 0.1);
                float contrastWidth = coreWidth * 2.25;
                float intermediateCoreWidth = coreWidth * 0.72;
                float intermediateContrastWidth = intermediateCoreWidth * 2.25;
                float smoothing = max(_WireSmoothness / 3.0, 0.5);
                float primaryCircleDerivative = max(fwidth(primaryCircleDistance), 0.00001);
                float intermediateCircleDerivative = max(fwidth(intermediateCircleDistance), 0.00001);
                float silhouetteDerivative = max(fwidth(silhouetteDistance), 0.00001);

                float corePrimaryCircles = 1.0 - smoothstep(
                    primaryCircleDerivative * coreWidth,
                    primaryCircleDerivative * (coreWidth + smoothing),
                    primaryCircleDistance);
                float contrastPrimaryCircles = 1.0 - smoothstep(
                    primaryCircleDerivative * contrastWidth,
                    primaryCircleDerivative * (contrastWidth + smoothing),
                    primaryCircleDistance);
                float coreIntermediateCircles = 1.0 - smoothstep(
                    intermediateCircleDerivative * intermediateCoreWidth,
                    intermediateCircleDerivative * (intermediateCoreWidth + smoothing),
                    intermediateCircleDistance);
                float contrastIntermediateCircles = 1.0 - smoothstep(
                    intermediateCircleDerivative * intermediateContrastWidth,
                    intermediateCircleDerivative * (intermediateContrastWidth + smoothing),
                    intermediateCircleDistance);
                float coreSilhouette = 1.0 - smoothstep(
                    silhouetteDerivative * coreWidth,
                    silhouetteDerivative * (coreWidth + smoothing),
                    silhouetteDistance);
                float contrastSilhouette = 1.0 - smoothstep(
                    silhouetteDerivative * contrastWidth,
                    silhouetteDerivative * (contrastWidth + smoothing),
                    silhouetteDistance);

                float coreCircles = max(corePrimaryCircles, coreIntermediateCircles);
                float contrastCircles = max(contrastPrimaryCircles, contrastIntermediateCircles);
                float core = max(coreCircles, coreSilhouette);
                float contrast = max(contrastCircles, contrastSilhouette);
                float wireColorWeight = saturate(core * 2.0);
                float3 color = lerp(_ContrastColor.rgb, _WireColor.rgb, wireColorWeight);
                float alpha = max(_ContrastColor.a * contrast, _WireColor.a * core);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
