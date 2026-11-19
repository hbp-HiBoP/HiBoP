Shader "Hidden/HBP/Quest Panel"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "CanUseSpriteAtlas" = "True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "../Includes/HBPQuestComposition.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            half4 color : COLOR;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            half4 color : COLOR;
            float2 uv : TEXCOORD0;
            float4 clipMask : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        float4 _MainTex_ST, _ClipRect;
        half4 _Color, _TextureSampleAdd;
        float _UIMaskSoftnessX, _UIMaskSoftnessY;
        int _UIVertexColorAlwaysGammaSpace;

        Varyings Vert(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = TRANSFORM_TEX(input.uv, _MainTex);
        #ifndef UNITY_COLORSPACE_GAMMA
            if (_UIVertexColorAlwaysGammaSpace)
                input.color.rgb = SRGBToLinear(input.color.rgb);
        #endif
            output.color = input.color * _Color;
        #if UNITY_UI_CLIP_RECT
            float4 rect = clamp(_ClipRect, -2e10, 2e10);
            float2 pixelSize = output.positionCS.w / abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
            float2 softness = float2(_UIMaskSoftnessX, _UIMaskSoftnessY);
            output.clipMask = float4(2 * input.positionOS.xy - rect.xy - rect.zw, 0.25 / (0.25 * softness + abs(pixelSize)));
        #endif
            return output;
        }

        half4 PanelColor(Varyings input)
        {
            half4 color = (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) + _TextureSampleAdd) * input.color;
        #if UNITY_UI_CLIP_RECT
            float2 mask = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.clipMask.xy)) * input.clipMask.zw);
            color.a *= mask.x * mask.y;
        #endif
        #if UNITY_UI_ALPHACLIP
            clip(color.a - 0.001);
        #endif
            return color;
        }

        half4 VisibleTimelineColor(Varyings input)
        {
            half4 color = PanelColor(input);
            clip(color.a - 0.0001);
            float sceneDepth = HBP_QuestDepthOrder(HBP_QUEST_LOAD(_HBPQuestOpaqueDepth, int2(input.positionCS.xy)));
            clip(sceneDepth - HBP_QuestDepthOrder(input.positionCS.z) + 0.0000001);
            return color;
        }
        ENDHLSL

        Pass
        {
            Name "Window"
            ZTest LEqual
            ZWrite Off
            Blend One OneMinusSrcAlpha
            ColorMask [_ColorMask]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment WindowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            half4 WindowFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 color = PanelColor(input);
                if (color.a <= 0)
                    return 0;
                half4 front = HBP_QuestFrontScience(int2(input.positionCS.xy), HBP_QuestDepthOrder(input.positionCS.z));
                // Replace the panel-covered part of the framebuffer, including the science in front of this panel.
                return half4(color.a * (front.rgb + (1.0h - front.a) * color.rgb), color.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "TimelineDepth"
            ZTest LEqual
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment TimelineDepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            float TimelineDepthFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                VisibleTimelineColor(input);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "TimelineColor"
            ZTest Always
            ZWrite Off
            Blend One OneMinusSrcAlpha
            ColorMask [_ColorMask]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment TimelineColorFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            half4 TimelineColorFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 color = VisibleTimelineColor(input);
                float nearestDepth = HBP_QUEST_LOAD(_HBPQuestTimelineDepth, int2(input.positionCS.xy));
                // Coplanar canvas triangles can round to slightly different raster depths.
                clip(0.000001 - abs(input.positionCS.z - nearestDepth));
                return half4(color.rgb * color.a, color.a);
            }
            ENDHLSL
        }
    }
}
