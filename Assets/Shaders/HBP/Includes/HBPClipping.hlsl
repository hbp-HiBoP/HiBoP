#ifndef HBP_CLIPPING_INCLUDED
#define HBP_CLIPPING_INCLUDED

#define HBP_MAX_CLIP_PLANES 20

int _StrongCuts;
int _CutCount;
float4 _CutPoints[HBP_MAX_CLIP_PLANES];
float4 _CutNormals[HBP_MAX_CLIP_PLANES];

float HBP_ClippingValue(float3 localPosition)
{
    int count = min(max(_CutCount, 0), HBP_MAX_CLIP_PLANES);
    if (count == 0)
        return 1.0;

    if (_StrongCuts != 0)
    {
        for (int index = 0; index < count; ++index)
        {
            if (dot(_CutNormals[index].xyz, _CutPoints[index].xyz - localPosition) < 0.0)
                return -1.0;
        }

        return 1.0;
    }

    for (int index = 0; index < count; ++index)
    {
        if (dot(_CutNormals[index].xyz, _CutPoints[index].xyz - localPosition) >= 0.0)
            return 1.0;
    }

    return -1.0;
}

#endif
