Shader "HBP/Quest/Controller Help Leader"
{
    Properties
    {
        [HideInInspector] _AnchorWS("Anchor in world space", Vector) = (0, 0, 0, 0)
        [HideInInspector] _ControlSampleCount("Control sample count", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Overlay+1" }

        Pass
        {
            Tags { "LightMode" = "HBPQuestControllerHelp" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Includes/HBPQuestControllerHelpDepth.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _AnchorWS;
                float _ControlSampleCount;
                // Matches QuestControllerModelGeometry.MaxVisibilitySamples.
                float4 _ControlSamplesWS[24];
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                half controlVisible : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            bool PointVisible(float3 positionWS)
            {
                float eyeDepth = -TransformWorldToView(positionWS).z;
                if (eyeDepth <= 0.0) return false;
                float2 uv = ComputeNormalizedDeviceCoordinates(positionWS, UNITY_MATRIX_VP);
                if (any(uv < 0.0) || any(uv >= 1.0)) return false;
                float shell = HBP_QuestHelpEyeDepth(HBP_QuestHelpRawDepth(int2(uv * GetScaledScreenParams().xy)));
                return eyeDepth <= shell + 0.0008;
            }

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.color = input.color;

                bool visible = PointVisible(_AnchorWS.xyz);
                // A side of the control can still face this eye when its anchor is behind the shell.
                [loop] for (int i = 0; i < (int)_ControlSampleCount && !visible; i++)
                    visible = PointVisible(_ControlSamplesWS[i].xyz);
                output.controlVisible = visible ? 1.0h : 0.0h;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                if (input.controlVisible < 0.5h)
                {
                    float shell = HBP_QuestHelpEyeDepth(HBP_QuestHelpRawDepth(int2(input.positionCS.xy)));
                    clip(shell - HBP_QuestHelpEyeDepth(input.positionCS.z) + 0.0002);
                }
                return input.color;
            }
            ENDHLSL
        }
    }
}
