Shader "HiBoP XR/P10/Prototype Buffered Sites"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "P10Prototype"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local _P10_BUFFERED
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            StructuredBuffer<float4> _P10PrototypePositions;
            StructuredBuffer<float4> _P10PrototypeAttributes;

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
#if defined(_P10_BUFFERED)
                float4 site = _P10PrototypePositions[unity_InstanceID];
                float4 attributes = _P10PrototypeAttributes[unity_InstanceID];
                float3 world = (site.xyz + input.positionOS * attributes.x) * 0.001;
                output.positionCS = TransformWorldToHClip(world);
                output.color = half4(attributes.yzw, 1.0);
#else
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.color = half4(0.53h, 0.15h, 0.15h, 1.0h);
#endif
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return input.color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
