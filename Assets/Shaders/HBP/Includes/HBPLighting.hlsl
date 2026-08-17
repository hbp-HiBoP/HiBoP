#ifndef HBP_LIGHTING_INCLUDED
#define HBP_LIGHTING_INCLUDED

float3 HBP_ApplySurfaceLighting(
    float3 surfaceLinear,
    float3 normalWS,
    float3 viewDirectionWS,
    float ambientStrength,
    float diffuseStrength,
    float smoothness)
{
    float facing = saturate(dot(normalize(normalWS), normalize(viewDirectionWS)));
    float3 litSurface = surfaceLinear * (ambientStrength + diffuseStrength * facing);

    float clampedSmoothness = saturate(smoothness);
    float specularPower = lerp(8.0, 40.0, clampedSmoothness);
    float specularStrength = 0.36 * clampedSmoothness;
    float specular = pow(facing, specularPower) * specularStrength;
    return litSurface + specular;
}

float3 HBP_ApplyScientificRelief(
    float3 scientificLinear,
    float3 normalWS,
    float3 viewDirectionWS,
    float ambientStrength,
    float diffuseStrength,
    float smoothness)
{
    float facing = saturate(dot(normalize(normalWS), normalize(viewDirectionWS)));
    float clampedSmoothness = saturate(smoothness);
    float specularPower = lerp(8.0, 40.0, clampedSmoothness);
    float specularLobe = pow(facing, specularPower);
    float paletteHighlight = specularLobe * (0.12 * clampedSmoothness);
    float relief = min(
        ambientStrength + diffuseStrength * facing + paletteHighlight,
        1.08);

    float brightestChannel = max(max(scientificLinear.r, scientificLinear.g), scientificLinear.b);
    float highlightHeadroom = rcp(max(brightestChannel, 0.0001));
    float neutralHighlight = specularLobe * (0.18 * clampedSmoothness);
    return scientificLinear * min(relief, highlightHeadroom) + neutralHighlight;
}

#endif
