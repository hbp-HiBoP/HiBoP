Shader "HBP/ROI/Wireframe"
{
    Properties
    {
        [MainColor] _BaseColor("Fill Color", Color) = (1, 1, 1, 0)
        _WireColor("Wire Color", Color) = (1, 1, 1, 1)
        _WireThickness("Wire Thickness", Range(0, 800)) = 50
        _WireSmoothness("Wire Smoothness", Range(0, 20)) = 3
        _MaxTriSize("Maximum Triangle Size", Float) = 25
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
                float _WireThickness;
                float _WireSmoothness;
                float _MaxTriSize;
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
                // Meshes created before the URP migration do not yet carry
                // barycentric coordinates. Keep their translucent fill visible;
                // Phase 3 will wire barycentrics for the actual wireframe.
                float barycentricSum = input.barycentric.x
                    + input.barycentric.y
                    + input.barycentric.z;
                if (barycentricSum < 0.5)
                    return _BaseColor;

                float wireWidth = max(_WireThickness / 50.0, 0.25);
                float smoothing = max(_WireSmoothness / 3.0, 0.25);
                float3 edgeDistance = smoothstep(
                    fwidth(input.barycentric) * wireWidth,
                    fwidth(input.barycentric) * (wireWidth + smoothing),
                    input.barycentric);
                float wire = 1.0 - min(edgeDistance.x, min(edgeDistance.y, edgeDistance.z));
                return lerp(_BaseColor, _WireColor, wire);
            }
            ENDHLSL
        }
    }
}
