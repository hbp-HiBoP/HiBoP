// URP version of the Custom/Brain shader in HLSL with proper lighting using UniversalFragment implementation

Shader "Custom/BrainURP"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB)", 2D) = "white" {}
        _AoTex("AO (RGB)", 2D) = "white" {}
        _ColorTex("Color map (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Atlas("Atlas", Int) = 0
        _Activity("Activity", Int) = 0
        _FMRI("FMRI", Int) = 0
        _Amount("Extrusion Amount", Range(0, 1)) = 0
        _MaxRadius("Maximum extrusion radius", Range(0,100)) = 100
        _StrongCuts("Strong Cuts", Int) = 0
        _CutCount("Cut Count", Int) = 0
        _Center("Extrusion Center", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Glossiness;
                float _Metallic;
                float _Amount;
                float _MaxRadius;
                float4 _Center;
                int _Atlas;
                int _FMRI;
                int _Activity;
                int _StrongCuts;
                int _CutCount;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_AoTex); SAMPLER(sampler_AoTex);
            TEXTURE2D(_ColorTex); SAMPLER(sampler_ColorTex);

            float3 _CutPoints[20];
            float3 _CutNormals[20];

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float2 uv2        : TEXCOORD1;
                float2 uv3        : TEXCOORD2;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float2 uv2         : TEXCOORD1;
                float2 uv3         : TEXCOORD2;
                float3 worldPos    : TEXCOORD3;
                float3 normalWS    : TEXCOORD4;
                float4 color       : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 objPos = IN.positionOS.xyz;
                float3 dir = normalize(objPos - _Center.xyz);
                float dist = length(objPos - _Center.xyz);
                float3 extruded = objPos + dir * (_MaxRadius - dist) * _Amount;
                OUT.worldPos = mul(GetObjectToWorldMatrix(), float4(extruded, 1.0)).xyz;
                float3 normalWS = normalize(mul((float3x3)GetObjectToWorldMatrix(), lerp(IN.normalOS, dir, _Amount)));
                OUT.normalWS = normalWS;
                OUT.positionHCS = TransformWorldToHClip(OUT.worldPos);
                OUT.uv = IN.uv;
                OUT.uv2 = IN.uv2;
                OUT.uv3 = IN.uv3;
                OUT.color = IN.color;
                return OUT;
            }

            float is_clipped(float3 worldPos)
            {
                float3 localPos = worldPos - mul(GetObjectToWorldMatrix(), float4(0, 0, 0, 1)).xyz;
                float clipping = 1;

                if (_StrongCuts == 0)
                {
                    for (int i = 0; i < _CutCount && i < 20; ++i)
                    {
                        int val = sign(dot(_CutNormals[i], _CutPoints[i] - localPos));
                        if (val < 0)
                            clipping = -1;
                        else
                        {
                            clipping = 1;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _CutCount && i < 20; ++i)
                    {
                        int val = sign(dot(_CutNormals[i], _CutPoints[i] - localPos));
                        if (val < 0)
                        {
                            clipping = -1;
                            break;
                        }
                    }
                }
                return clipping;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                if (is_clipped(IN.worldPos) < 0)
                    discard;

                float4 finalCol;
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                if ((_Atlas | _FMRI) != 0)
                {
                    float alpha = IN.color.a;
                    float4 mixColor = lerp(baseTex, IN.color, alpha);
                    finalCol = mixColor * _Color;
                }
                else
                {
                    float4 ao = SAMPLE_TEXTURE2D(_AoTex, sampler_AoTex, IN.uv2);
                    float mix = saturate(ao.r * 2.5);
                    float4 colTex = SAMPLE_TEXTURE2D(_ColorTex, sampler_ColorTex, IN.uv3);
                    float4 mixColor = lerp(baseTex, colTex, mix);
                    finalCol = mixColor * _Color;
                }

                float3 viewDirWS = normalize(GetCameraPositionWS() - IN.worldPos);

                InputData inputData;
                inputData.positionWS = IN.worldPos;
                inputData.normalWS = normalize(IN.normalWS);
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = float4(0, 0, 0, 0);
                inputData.fogCoord = 0;
                inputData.vertexLighting = float3(0, 0, 0);
                inputData.bakedGI = float3(0, 0, 0);

                SurfaceData surfaceData;
                surfaceData.albedo = finalCol.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = float3(0.0, 0.0, 0.0);
                surfaceData.smoothness = _Glossiness;
                surfaceData.normalTS = float3(0, 0, 1);
                surfaceData.occlusion = 1.0;
                surfaceData.emission = float3(0, 0, 0);
                surfaceData.alpha = finalCol.a;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                return UniversalFragmentBlinnPhong(inputData, surfaceData);
            }
            ENDHLSL
        }
    }

    FallBack Off
}