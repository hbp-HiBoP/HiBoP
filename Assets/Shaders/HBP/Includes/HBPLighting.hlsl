#ifndef HBP_LIGHTING_INCLUDED
#define HBP_LIGHTING_INCLUDED

float3 HBP_ApplyAnatomyLighting(
    float3 anatomyLinear,
    float3 normalWS,
    float3 cameraForwardWS,
    float ambientStrength,
    float diffuseStrength)
{
    float diffuse = saturate(dot(normalize(normalWS), normalize(-cameraForwardWS)));
    return anatomyLinear * (ambientStrength + diffuseStrength * diffuse);
}

#endif
