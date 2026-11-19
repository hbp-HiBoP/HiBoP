Shader "HBP/ROI/Sphere"
{
    Properties
    {
        [Header(Fill)]
        [MainColor] _FillColor("Color", Color) = (1, 1, 1, 1)
        _FillOpacity("Opacity", Range(0, 1)) = 0.3
        _FillShading("Shading Strength", Range(0, 1)) = 0.52
        _FillHighlight("Highlight Strength", Range(0, 1)) = 0.13
        [Header(Rim)]
        _RimColor("Color", Color) = (1, 1, 1, 1)
        _RimOpacity("Opacity", Range(0, 1)) = 1
        _RimThickness("Thickness (px)", Range(0, 12)) = 2
        _RimSoftness("Softness (px)", Range(0, 4)) = 0.85
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "HBPQuestComposition" = "Peel"
            "Queue" = "Transparent+1"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 proxyPositionOS : TEXCOORD0;
            float3 towardCameraOS : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        struct FragmentOutput
        {
            half4 color : SV_Target;
            float depth : SV_Depth;
        };

        CBUFFER_START(UnityPerMaterial)
            float4 _FillColor;
            float4 _RimColor;
            float _FillOpacity;
            float _FillShading;
            float _FillHighlight;
            float _RimOpacity;
            float _RimThickness;
            float _RimSoftness;
        CBUFFER_END

        Varyings Vert(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            // Leave room for the pixel footprint beyond the inscribed unit sphere mesh.
            float4 centerCS = TransformObjectToHClip(float3(0.0, 0.0, 0.0));
            float2 pixelsPerViewUnit = abs(float2(UNITY_MATRIX_P._m00, UNITY_MATRIX_P._m11))
                * GetScaledScreenParams().xy * 0.5;
            float2 viewUnitsPerPixel = max(centerCS.w, 0.0) / max(pixelsPerViewUnit, 0.0001);
            float3 rightOS = TransformWorldToObjectDir(UNITY_MATRIX_I_V._m00_m10_m20, false);
            float3 upOS = TransformWorldToObjectDir(UNITY_MATRIX_I_V._m01_m11_m21, false);
            float pixelRadiusOS = max(length(rightOS) * viewUnitsPerPixel.x, length(upOS) * viewUnitsPerPixel.y);
            output.proxyPositionOS = input.positionOS.xyz * (1.004 + 1.5 * pixelRadiusOS);

            float3 positionWS = TransformObjectToWorld(output.proxyPositionOS);
            output.positionCS = TransformWorldToHClip(positionWS);
            output.towardCameraOS = TransformWorldToObjectDir(GetWorldSpaceViewDir(positionWS), false);
            return output;
        }

        // Integral of the inboard rim profile, starting at the sphere silhouette.
        float IntegratedRim(float distance, float thickness, float softness)
        {
            float ramp = saturate((distance - thickness) / softness);
            return clamp(distance, 0.0, thickness) + softness * ramp * (1.0 - ramp * ramp * (1.0 - 0.5 * ramp));
        }

        FragmentOutput Frag(Varyings input)
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float3 towardCameraOS = normalize(input.towardCameraOS);
            float3 closestOS = input.proxyPositionOS - dot(input.proxyPositionOS, towardCameraOS) * towardCameraOS;
            float limb = 1.0 - dot(closestOS, closestOS);
            float distancePixels = limb / max(fwidth(limb), 0.00001);
            float coverage = saturate(distancePixels + 0.5);
            clip(coverage - 0.0001);

            // Use the real sphere hit for depth and shading. A fringe pixel uses the tangent depth.
            float3 surfaceOS = closestOS + sqrt(max(limb, 0.0)) * towardCameraOS;
            float4 surfaceCS = TransformObjectToHClip(surfaceOS);
            clip(surfaceCS.w);
            float depth = surfaceCS.z / surfaceCS.w;
            #if !UNITY_REVERSED_Z
                depth = (depth - UNITY_NEAR_CLIP_VALUE) / (1.0 - UNITY_NEAR_CLIP_VALUE);
            #endif
            clip(min(depth, 1.0 - depth));

            float3 normal = TransformObjectToWorldNormal(surfaceOS);
            float3 viewDirection = TransformObjectToWorldDir(towardCameraOS);
            float facing = saturate(dot(normal, viewDirection));

            float3 right = normalize(UNITY_MATRIX_I_V._m00_m10_m20);
            float3 up = normalize(UNITY_MATRIX_I_V._m01_m11_m21);
            float3 lightDirection = normalize(viewDirection * 0.68 + up * 0.61 - right * 0.42);
            float diffuse = saturate(dot(normal, lightDirection));
            float fillAlpha = _FillColor.a * _FillOpacity * lerp(0.32, 1.0, facing) * lerp(0.82, 1.0, diffuse);
            float3 fillColor = _FillColor.rgb * lerp(1.0 - _FillShading, 1.0, diffuse);
            fillColor = lerp(fillColor, 1.0, smoothstep(0.86, 0.99, diffuse) * _FillHighlight);

            float softness = max(_RimSoftness, 0.0001);
            float rimCoverage = IntegratedRim(distancePixels + 0.5, _RimThickness, softness)
                - IntegratedRim(distancePixels - 0.5, _RimThickness, softness);
            float rimAlpha = _RimColor.a * _RimOpacity * clamp(rimCoverage, 0.0, coverage) * step(0.0001, _RimThickness);

            float visibleFillAlpha = fillAlpha * (coverage - rimAlpha);
            float alpha = rimAlpha + visibleFillAlpha;
            float3 rgb = _RimColor.rgb * rimAlpha + fillColor * visibleFillAlpha;
            FragmentOutput output;
            output.color = half4(rgb / max(alpha, 0.00001), alpha);
            output.depth = depth;
            return output;
        }
        ENDHLSL

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
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Name "HBPQuestTransparent"
            Tags { "LightMode" = "HBPQuestTransparent" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment QuestFrag
            #pragma multi_compile_instancing

            #include "../Includes/HBPQuestPeel.hlsl"

            FragmentOutput QuestFrag(Varyings input)
            {
                FragmentOutput output = Frag(input);
                float4 positionCS = input.positionCS;
                positionCS.z = output.depth;
                output.color = HBP_QuestPeel(output.color, positionCS);
                return output;
            }
            ENDHLSL
        }
    }
}
