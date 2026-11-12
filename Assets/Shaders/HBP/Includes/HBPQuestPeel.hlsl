#ifndef HBP_QUEST_PEEL_INCLUDED
#define HBP_QUEST_PEEL_INCLUDED

// Also included by the Quest 3D text pass, which uses UnityCG rather than URP Core.
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    #define HBP_QUEST_TEXTURE(type, name) Texture2DArray<type> name
    #define HBP_QUEST_LOAD(name, pixel) name.Load(int4(pixel, unity_StereoEyeIndex, 0))
#else
    #define HBP_QUEST_TEXTURE(type, name) Texture2D<type> name
    #define HBP_QUEST_LOAD(name, pixel) name.Load(int3(pixel, 0))
#endif

HBP_QUEST_TEXTURE(float, _HBPQuestOpaqueDepth);
HBP_QUEST_TEXTURE(float, _HBPQuestPreviousDepth);
HBP_QUEST_TEXTURE(float4, _HBPQuestPreviousColor);
int _HBPQuestHasPreviousLayer;

float HBP_QuestDepthOrder(float depth)
{
#if UNITY_REVERSED_Z
    return -depth;
#else
    return depth;
#endif
}

half4 HBP_QuestPeel(half4 color, float4 positionCS)
{
    clip(color.a - 0.0001);
    int2 pixel = int2(positionCS.xy);
    float depth = HBP_QuestDepthOrder(positionCS.z);
    float opaque = HBP_QuestDepthOrder(HBP_QUEST_LOAD(_HBPQuestOpaqueDepth, pixel));
    clip(opaque - depth + 0.0000001);
    if (_HBPQuestHasPreviousLayer)
    {
        float previous = HBP_QuestDepthOrder(HBP_QUEST_LOAD(_HBPQuestPreviousDepth, pixel));
        clip(depth - previous - 0.0000001);
        // An opaque fragment terminates the ray and makes any farther layers irrelevant.
        clip(0.9999 - HBP_QUEST_LOAD(_HBPQuestPreviousColor, pixel).a);
    }
    return color;
}

#endif
