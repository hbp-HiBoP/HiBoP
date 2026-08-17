#ifndef HBP_BRAIN_INPUT_INCLUDED
#define HBP_BRAIN_INPUT_INCLUDED

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_AoTex);
SAMPLER(sampler_AoTex);
TEXTURE2D(_ColorTex);
SAMPLER(sampler_ColorTex);

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float4 _AoTex_ST;
    float4 _ColorTex_ST;
    float4 _Color;
    float4 _Center;
    float _Glossiness;
    float _Metallic;
    float _Atlas;
    float _Activity;
    float _FMRI;
    float _Amount;
    float _MaxRadius;
    float _AmbientStrength;
    float _DiffuseStrength;
CBUFFER_END

#endif
