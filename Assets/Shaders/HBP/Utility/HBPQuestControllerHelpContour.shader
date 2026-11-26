Shader "HBP/Quest/Controller Help Contour"
{
    Properties
    {
        _Tint("Shell", Color) = (1, 1, 1, 0.20)
        [HideInInspector] _HighlightedControls("Highlighted controls", Float) = 0
        [HideInInspector] _SolidBackground("Solid background", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Overlay" }
        Cull Back

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "../Includes/HBPQuestControllerHelpDepth.hlsl"
        HBP_QUEST_TEXTURE(float, _HBPQuestControllerHelpControls);

        CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            float _HighlightedControls;
            float _SolidBackground;
        CBUFFER_END

        struct Attributes
        {
            float3 positionOS : POSITION;
            float2 controlUV : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            half highlight : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            UNITY_SETUP_INSTANCE_ID(input);
            Varyings output = (Varyings)0;
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.highlight = ((uint)input.controlUV.x & (uint)_HighlightedControls) != 0 ? 1.0h : 0.0h;
            return output;
        }
        ENDHLSL

        Pass
        {
            Name "ControllerDepth"
            Tags { "LightMode" = "HBPQuestControllerDepth" }
            ZWrite On
            ZTest LEqual
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            half DepthFrag(Varyings input) : SV_Target { return input.highlight; }
            ENDHLSL
        }

        Pass
        {
            Name "ControllerSurface"
            Tags { "LightMode" = "HBPQuestControllerHelp" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment SurfaceFrag
            #pragma multi_compile_instancing

            half4 SurfaceFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                int2 pixel = int2(input.positionCS.xy);
                float nearestDepth = HBP_QuestDepthOrder(HBP_QuestHelpRawDepth(pixel));
                float depth = HBP_QuestDepthOrder(input.positionCS.z);
                // The mask samples the pixel center while the color target uses MSAA.
                clip(nearestDepth - depth + max(0.000001, fwidth(depth)));
                if (_HighlightedControls == 0) return _SolidBackground > 0.5 ? half4(_Tint.rgb, 0) : _Tint;

                clip(input.highlight - 0.001h);
                int2 last = int2(GetScaledScreenParams().xy) - 1;
                half adjacent = min(
                    min(HBP_QUEST_LOAD(_HBPQuestControllerHelpControls, clamp(pixel + int2(2, 0), 0, last)),
                        HBP_QUEST_LOAD(_HBPQuestControllerHelpControls, clamp(pixel - int2(2, 0), 0, last))),
                    min(HBP_QUEST_LOAD(_HBPQuestControllerHelpControls, clamp(pixel + int2(0, 2), 0, last)),
                        HBP_QUEST_LOAD(_HBPQuestControllerHelpControls, clamp(pixel - int2(0, 2), 0, last))));
                return half4(_Tint.rgb, input.highlight * (1.0h - adjacent));
            }
            ENDHLSL
        }
    }
}
