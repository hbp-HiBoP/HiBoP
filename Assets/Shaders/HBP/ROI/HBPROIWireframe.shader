Shader "HBP/ROI/Wireframe"
{
    Properties
    {
        [MainColor] _BaseColor("Fill Color", Color) = (1, 1, 1, 0)
        _WireColor("Wire Color", Color) = (1, 1, 1, 1)
        _WireWidth("Wire Width", Range(0.25, 4)) = 1
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
                float3 barycentric : TEXCOORD3;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                noperspective float3 barycentric : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _WireColor;
                float _WireWidth;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.barycentric = input.barycentric;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 edgeDistance = smoothstep(
                    0.0,
                    fwidth(input.barycentric) * _WireWidth,
                    input.barycentric);
                float wire = 1.0 - min(edgeDistance.x, min(edgeDistance.y, edgeDistance.z));
                return lerp(_BaseColor, _WireColor, wire);
            }
            ENDHLSL
        }
    }
}
