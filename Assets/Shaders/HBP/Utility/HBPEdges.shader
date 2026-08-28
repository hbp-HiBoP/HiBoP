Shader "Hidden/HBP/Edges"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "HBPEdges"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D_X(_BlitTexture);
            TEXTURE2D_X(_HBPEdgeOpaqueData);
            TEXTURE2D_X(_HBPEdgeTransparentMask);

            float4 _BlitTexture_TexelSize;
            float4 _HBPEdgeColor;
            float _HBPEdgeThickness;
            float _HBPEdgeDepthThreshold;
            float _HBPEdgeNormalThreshold;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float DepthCurvature(float centerDepth, float negativeDepth, float positiveDepth)
            {
                float neighborsHaveSurface = step(0.0001, negativeDepth) * step(0.0001, positiveDepth);
                float curvature = abs(negativeDepth + positiveDepth - 2.0 * centerDepth) / max(centerDepth, 1.0);
                return max(1.0 - neighborsHaveSurface, curvature);
            }

            float NormalDifference(float4 centerData, float4 sampleData)
            {
                float sampleHasSurface = step(0.0001, sampleData.a);
                float3 centerNormal = centerData.rgb * 2.0 - 1.0;
                float3 sampleNormal = sampleData.rgb * 2.0 - 1.0;
                return sampleHasSurface * (1.0 - dot(centerNormal, sampleNormal));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 offset = _BlitTexture_TexelSize.xy * max(_HBPEdgeThickness, 0.5);
                float2 leftUv = input.uv - float2(offset.x, 0.0);
                float2 rightUv = input.uv + float2(offset.x, 0.0);
                float2 downUv = input.uv - float2(0.0, offset.y);
                float2 upUv = input.uv + float2(0.0, offset.y);

                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.uv);
                float4 centerData = SAMPLE_TEXTURE2D_X(_HBPEdgeOpaqueData, sampler_PointClamp, input.uv);
                float4 leftData = SAMPLE_TEXTURE2D_X(_HBPEdgeOpaqueData, sampler_PointClamp, leftUv);
                float4 rightData = SAMPLE_TEXTURE2D_X(_HBPEdgeOpaqueData, sampler_PointClamp, rightUv);
                float4 downData = SAMPLE_TEXTURE2D_X(_HBPEdgeOpaqueData, sampler_PointClamp, downUv);
                float4 upData = SAMPLE_TEXTURE2D_X(_HBPEdgeOpaqueData, sampler_PointClamp, upUv);
                float centerDepth = centerData.a;
                float depthDifference = max(
                    DepthCurvature(centerDepth, leftData.a, rightData.a),
                    DepthCurvature(centerDepth, downData.a, upData.a));

                float normalDifference = max(
                    max(NormalDifference(centerData, leftData), NormalDifference(centerData, rightData)),
                    max(NormalDifference(centerData, downData), NormalDifference(centerData, upData)));

                float hasOpaqueSurface = step(0.0001, centerDepth);
                float opaqueEdge = hasOpaqueSurface * max(
                    smoothstep(_HBPEdgeDepthThreshold, _HBPEdgeDepthThreshold * 2.0, depthDifference),
                    smoothstep(_HBPEdgeNormalThreshold, _HBPEdgeNormalThreshold * 2.0, normalDifference));

                float centerMask = SAMPLE_TEXTURE2D_X(_HBPEdgeTransparentMask, sampler_LinearClamp, input.uv).r;
                float transparentDifference = max(
                    max(abs(centerMask - SAMPLE_TEXTURE2D_X(_HBPEdgeTransparentMask, sampler_LinearClamp, leftUv).r),
                        abs(centerMask - SAMPLE_TEXTURE2D_X(_HBPEdgeTransparentMask, sampler_LinearClamp, rightUv).r)),
                    max(abs(centerMask - SAMPLE_TEXTURE2D_X(_HBPEdgeTransparentMask, sampler_LinearClamp, downUv).r),
                        abs(centerMask - SAMPLE_TEXTURE2D_X(_HBPEdgeTransparentMask, sampler_LinearClamp, upUv).r)));

                float edgeAlpha = saturate(max(opaqueEdge, transparentDifference)) * _HBPEdgeColor.a;
                float outputAlpha = edgeAlpha + source.a * (1.0 - edgeAlpha);
                float3 outputColor = _HBPEdgeColor.rgb * edgeAlpha + source.rgb * (1.0 - edgeAlpha);
                return half4(outputColor, outputAlpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "HBPTransparentBrainComposite"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D_X(_BlitTexture);
            TEXTURE2D_X(_HBPTransparentBrainSurface);
            TEXTURE2D_X_FLOAT(_HBPTransparentBrainDepth);
            TEXTURE2D_X_FLOAT(_HBPSceneDepth);

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.uv);
                half4 surface = SAMPLE_TEXTURE2D_X(_HBPTransparentBrainSurface, sampler_LinearClamp, input.uv);
                float brainDepth = SAMPLE_TEXTURE2D_X(_HBPTransparentBrainDepth, sampler_PointClamp, input.uv).r;
                float sceneDepth = SAMPLE_TEXTURE2D_X(_HBPSceneDepth, sampler_PointClamp, input.uv).r;

            #if UNITY_REVERSED_Z
                float brainIsVisible = step(sceneDepth - 0.00001, brainDepth);
            #else
                float brainIsVisible = step(brainDepth, sceneDepth + 0.00001);
            #endif

                half alpha = surface.a * brainIsVisible;
                half3 color = surface.rgb * alpha + source.rgb * (1.0h - alpha);
                half outputAlpha = alpha + source.a * (1.0h - alpha);
                return half4(color, outputAlpha);
            }
            ENDHLSL
        }
    }
}
