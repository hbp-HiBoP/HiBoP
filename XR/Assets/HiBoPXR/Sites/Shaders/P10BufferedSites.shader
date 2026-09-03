Shader "HiBoP XR/P10/Buffered Sites"
{
    Properties
    {
        _Ambient ("Ambient", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "P10Sites"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct SiteDynamic
            {
                float radiusMillimeters;
                uint packedColor;
                uint state;
                uint feedback;
            };

            StructuredBuffer<float4> _P10SitePositions;
            StructuredBuffer<SiteDynamic> _P10SiteDynamics;
            float4x4 _P10SiteLocalToWorld;
            half _Ambient;

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR0;
                half2 disc : TEXCOORD0;
                nointerpolation uint state : TEXCOORD1;
                nointerpolation uint feedback : TEXCOORD2;
                nointerpolation float3 centerView : TEXCOORD3;
                nointerpolation float radiusWorld : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct FragmentOutput
            {
                half4 color : SV_Target;
                float depth : SV_Depth;
            };

            half4 UnpackColor(uint packed)
            {
                return half4(
                    (packed & 255u) / 255.0h,
                    ((packed >> 8) & 255u) / 255.0h,
                    ((packed >> 16) & 255u) / 255.0h,
                    ((packed >> 24) & 255u) / 255.0h);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#if UNITY_ANY_INSTANCING_ENABLED
                uint siteIndex = unity_InstanceID;
#else
                uint siteIndex = 0u;
#endif
                SiteDynamic dynamic = _P10SiteDynamics[siteIndex];
                float3 centerMillimeters = _P10SitePositions[siteIndex].xyz;
                float3 localCenterMeters = centerMillimeters * 0.001;
                float3 worldCenter = mul(_P10SiteLocalToWorld, float4(localCenterMeters, 1.0)).xyz;
                float uniformWorldScale = length(_P10SiteLocalToWorld._m00_m10_m20);
                float radiusWorld = dynamic.radiusMillimeters * 0.001 * uniformWorldScale;
                float4 centerView = mul(UNITY_MATRIX_V, float4(worldCenter, 1.0));
                output.centerView = centerView.xyz;
                output.radiusWorld = radiusWorld;
                centerView.xy += input.positionOS.xy * radiusWorld;
                output.positionCS = mul(UNITY_MATRIX_P, centerView);
                output.disc = input.positionOS.xy;
                output.color = UnpackColor(dynamic.packedColor);
                output.state = dynamic.state;
                output.feedback = dynamic.feedback;
                return output;
            }

            FragmentOutput Frag(Varyings input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                clip((input.state & 1u) == 1u ? 1.0h : -1.0h);
                half radiusSquared = dot(input.disc, input.disc);
                clip(1.0h - radiusSquared);
                half facing = sqrt(saturate(1.0h - radiusSquared));
                half lighting = _Ambient + (1.0h - _Ambient) * facing;
                half3 color = input.color.rgb * lighting;
                if ((input.feedback & 4u) != 0u || (input.state & 256u) != 0u)
                    color = lerp(color, half3(0.25h, 1.0h, 0.35h), 0.65h);
                if ((input.feedback & 2u) != 0u)
                    color = lerp(color, half3(1.0h, 0.72h, 0.08h), 0.75h);
                if ((input.feedback & 1u) != 0u)
                    color = lerp(color, half3(1.0h, 1.0h, 1.0h), 0.55h);
                float3 sphereSurfaceView = input.centerView + float3(input.disc * input.radiusWorld, facing * input.radiusWorld);
                FragmentOutput output;
                output.color = half4(color, 1.0h);
                output.depth = ComputeNormalizedDeviceCoordinatesWithZ(sphereSurfaceView, UNITY_MATRIX_P).z;
                return output;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
