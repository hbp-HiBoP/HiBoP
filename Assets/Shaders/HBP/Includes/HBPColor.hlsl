#ifndef HBP_COLOR_INCLUDED
#define HBP_COLOR_INCLUDED

float3 HBP_SRGBToLinear(float3 srgb)
{
    float3 lower = srgb / 12.92;
    float3 upper = pow(max((srgb + 0.055) / 1.055, 0.0), 2.4);
    return lerp(upper, lower, step(srgb, 0.04045));
}

float3 HBP_LinearToSRGB(float3 linearColor)
{
    float3 lower = linearColor * 12.92;
    float3 upper = 1.055 * pow(max(linearColor, 0.0), 1.0 / 2.4) - 0.055;
    return lerp(upper, lower, step(linearColor, 0.0031308));
}

float HBP_NormalizeRange(float value, float minimum, float maximum)
{
    return maximum == minimum ? 0.5 : saturate((value - minimum) / (maximum - minimum));
}

float HBP_NormalizeDiverging(float value, float minimum, float middle, float maximum)
{
    if (value <= middle)
        return middle == minimum ? 0.5 : 0.5 * saturate((value - minimum) / (middle - minimum));

    return maximum == middle ? 0.5 : 0.5 + 0.5 * saturate((value - middle) / (maximum - middle));
}

float HBP_RemapScientificAlpha(float alpha)
{
    float clampedAlpha = saturate(alpha);
    float transparency = 1.0 - clampedAlpha;
    return 1.0 - transparency * transparency;
}

float3 HBP_ComposeScientificColor(float3 anatomyLinear, float3 scientificLinear, float alpha)
{
    return lerp(anatomyLinear, scientificLinear, HBP_RemapScientificAlpha(alpha));
}

float HBP_ComposeAlpha(float normalizedSourceAlpha, float userAlpha)
{
    return saturate(normalizedSourceAlpha * userAlpha);
}

#endif
