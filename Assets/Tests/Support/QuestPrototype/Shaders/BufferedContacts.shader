// Adapted from eb26c323e:XR/Assets/HiBoPXR/Sites/Shaders/P10BufferedSites.shader.
// Prepared linear colors only; no scientific mask, selection or color rules.
Shader "HiBoP Quest/Buffered Contacts"
{
    Properties { _Ambient ("Ambient", Range(0, 1)) = 0.35 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Contacts"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers vulkan d3d11 metal
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:SetupContact
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // RenderMeshPrimitives supplies instance IDs, while our buffer supplies transforms.
            void SetupContact() { }

            struct Contact { float4 positionRadius; float4 color; };
            StructuredBuffer<Contact> _Contacts;
            float4x4 _ContactLocalToWorld;
            half _Ambient;

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 color : COLOR0;
                float2 disc : TEXCOORD0;
                nointerpolation float3 centerView : TEXCOORD1;
                nointerpolation float radiusWorld : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            struct FragmentOutput { half4 color : SV_Target; float depth : SV_Depth; };

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
                Contact site = _Contacts[siteIndex];
                // This matrix already includes the prefab's single mm-to-m conversion and group scale.
                float3 worldCenter = mul(_ContactLocalToWorld, float4(site.positionRadius.xyz, 1)).xyz;
                float radiusWorld = site.positionRadius.w * length(_ContactLocalToWorld._m00_m10_m20);
                float4 centerView = mul(UNITY_MATRIX_V, float4(worldCenter, 1));
                output.centerView = centerView.xyz;
                output.radiusWorld = radiusWorld;
                centerView.xy += input.positionOS.xy * radiusWorld;
                output.positionCS = mul(UNITY_MATRIX_P, centerView);
                output.disc = input.positionOS.xy;
                output.color = site.color.rgb;
                return output;
            }

            FragmentOutput Frag(Varyings input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                clip(input.radiusWorld > 0 ? 1 : -1);
                float radiusSquared = dot(input.disc, input.disc);
                clip(1 - radiusSquared);
                float facing = sqrt(saturate(1 - radiusSquared));
                half lighting = _Ambient + (1 - _Ambient) * facing;
                float3 sphereSurfaceView = input.centerView + float3(input.disc * input.radiusWorld, facing * input.radiusWorld);
                FragmentOutput output;
                output.color = half4(input.color * lighting, 1);
                output.depth = ComputeNormalizedDeviceCoordinatesWithZ(sphereSurfaceView, UNITY_MATRIX_P).z;
                return output;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
