#ifndef HBP_QUEST_COMPOSITION_INCLUDED
#define HBP_QUEST_COMPOSITION_INCLUDED

#include "HBPQuestPeel.hlsl"
#include "HBPDepthNormalEdges.hlsl"

HBP_QUEST_TEXTURE(float4, _HBPQuestLayerColor0);
HBP_QUEST_TEXTURE(float4, _HBPQuestLayerColor1);
HBP_QUEST_TEXTURE(float4, _HBPQuestLayerColor2);
HBP_QUEST_TEXTURE(float4, _HBPQuestLayerColor3);
HBP_QUEST_TEXTURE(float, _HBPQuestLayerDepth0);
HBP_QUEST_TEXTURE(float, _HBPQuestLayerDepth1);
HBP_QUEST_TEXTURE(float, _HBPQuestLayerDepth2);
HBP_QUEST_TEXTURE(float, _HBPQuestLayerDepth3);
HBP_QUEST_TEXTURE(float4, _HBPQuestTail);
HBP_QUEST_TEXTURE(float4, _HBPQuestTimelineColor);
HBP_QUEST_TEXTURE(float, _HBPQuestTimelineDepth);
HBP_QUEST_TEXTURE(float4, _HBPQuestOpaqueEdgeData);
HBP_QUEST_TEXTURE(float, _HBPQuestOpaqueEdgeDepth);
HBP_QUEST_TEXTURE(float, _HBPQuestTransparentEdgeDepth);

int _HBPQuestEdgesEnabled;
float4 _HBPQuestEdgeColor;
float4 _HBPQuestEdgeParameters; // thickness, depth threshold, normal threshold
float4 _HBPQuestTargetSize;

int2 HBP_QuestOffsetPixel(int2 pixel, int2 offset)
{
    return clamp(pixel + offset, int2(0, 0), int2(_HBPQuestTargetSize.xy) - 1);
}

half4 HBP_QuestOpaqueEdge(int2 pixel)
{
    if (!_HBPQuestEdgesEnabled)
        return 0;

    float edgeDepth = HBP_QuestDepthOrder(HBP_QUEST_LOAD(_HBPQuestOpaqueEdgeDepth, pixel));
    float opaqueDepth = HBP_QuestDepthOrder(HBP_QUEST_LOAD(_HBPQuestOpaqueDepth, pixel));
    if (edgeDepth > opaqueDepth + 0.0000001)
        return 0;

    int offset = max(1, int(round(_HBPQuestEdgeParameters.x)));
    float4 center = HBP_QUEST_LOAD(_HBPQuestOpaqueEdgeData, pixel);
    float4 left = HBP_QUEST_LOAD(_HBPQuestOpaqueEdgeData, HBP_QuestOffsetPixel(pixel, int2(-offset, 0)));
    float4 right = HBP_QUEST_LOAD(_HBPQuestOpaqueEdgeData, HBP_QuestOffsetPixel(pixel, int2(offset, 0)));
    float4 down = HBP_QUEST_LOAD(_HBPQuestOpaqueEdgeData, HBP_QuestOffsetPixel(pixel, int2(0, -offset)));
    float4 up = HBP_QUEST_LOAD(_HBPQuestOpaqueEdgeData, HBP_QuestOffsetPixel(pixel, int2(0, offset)));
    half alpha = HBP_OpaqueEdge(center, left, right, down, up, _HBPQuestEdgeParameters.y, _HBPQuestEdgeParameters.z) * _HBPQuestEdgeColor.a;
    return half4(_HBPQuestEdgeColor.rgb * alpha, alpha);
}

float HBP_QuestSilhouetteDepth(int2 pixel)
{
    return HBP_QuestDepthOrder(HBP_QUEST_LOAD(_HBPQuestTransparentEdgeDepth, pixel));
}

half HBP_QuestTransparentEdge(int2 pixel, out float depth)
{
    depth = 2.0;
    if (!_HBPQuestEdgesEnabled)
        return 0;

    int offset = max(1, int(round(_HBPQuestEdgeParameters.x)));
    float center = HBP_QuestSilhouetteDepth(pixel);
    float left = HBP_QuestSilhouetteDepth(HBP_QuestOffsetPixel(pixel, int2(-offset, 0)));
    float right = HBP_QuestSilhouetteDepth(HBP_QuestOffsetPixel(pixel, int2(offset, 0)));
    float down = HBP_QuestSilhouetteDepth(HBP_QuestOffsetPixel(pixel, int2(0, -offset)));
    float up = HBP_QuestSilhouetteDepth(HBP_QuestOffsetPixel(pixel, int2(0, offset)));
    float farDepth = HBP_QuestDepthOrder(UNITY_RAW_FAR_CLIP_VALUE);
    bool onSurface = center < farDepth;
    bool contour = onSurface ? max(max(left, right), max(down, up)) >= farDepth : min(min(left, right), min(down, up)) < farDepth;
    depth = onSurface ? center : min(min(left, right), min(down, up));
    float opaqueDepth = HBP_QuestDepthOrder(HBP_QUEST_LOAD(_HBPQuestOpaqueDepth, pixel));
    return contour && depth <= opaqueDepth + 0.0000001 ? _HBPQuestEdgeColor.a : 0;
}

void HBP_QuestAccumulate(inout half4 result, half4 premultiplied)
{
    result += (1.0h - result.a) * premultiplied;
}

void HBP_QuestInsertSurface(inout half4 result, inout half4 surface, float depth, float nextDepth, float limit)
{
    if (surface.a > 0 && depth <= nextDepth + 0.0000001 && depth < limit)
    {
        HBP_QuestAccumulate(result, surface);
        surface = 0;
    }
}

// Insert the resolved timeline and silhouette by their physical depths among the peel layers.
half4 HBP_QuestFrontScience(int2 pixel, float limit)
{
    float edgeDepth;
    half edgeAlpha = HBP_QuestTransparentEdge(pixel, edgeDepth);
    half4 first = half4(_HBPQuestEdgeColor.rgb * edgeAlpha, edgeAlpha);
    float firstDepth = edgeDepth;
    half4 second = HBP_QUEST_LOAD(_HBPQuestTimelineColor, pixel);
    float secondDepth = second.a > 0 ? HBP_QuestDepthOrder(HBP_QUEST_LOAD(_HBPQuestTimelineDepth, pixel)) : 2.0;
    if (secondDepth < firstDepth)
    {
        half4 edge = first;
        first = second;
        firstDepth = secondDepth;
        second = edge;
        secondDepth = edgeDepth;
    }
    half4 result = 0;
    float lastDepth;

    #define HBP_QUEST_ACCUMULATE_LAYER(index) \
        lastDepth = HBP_QuestDepthOrder(HBP_QUEST_LOAD(_HBPQuestLayerDepth##index, pixel)); \
        HBP_QuestInsertSurface(result, first, firstDepth, lastDepth, limit); \
        HBP_QuestInsertSurface(result, second, secondDepth, lastDepth, limit); \
        if (lastDepth < limit && result.a < 1.0h) \
        { \
            half4 layer = HBP_QUEST_LOAD(_HBPQuestLayerColor##index, pixel); \
            HBP_QuestAccumulate(result, half4(layer.rgb * layer.a, layer.a)); \
        }

    HBP_QUEST_ACCUMULATE_LAYER(0)
    HBP_QUEST_ACCUMULATE_LAYER(1)
    HBP_QUEST_ACCUMULATE_LAYER(2)
    HBP_QUEST_ACCUMULATE_LAYER(3)
    #undef HBP_QUEST_ACCUMULATE_LAYER

    HBP_QuestInsertSurface(result, first, firstDepth, 2.0, limit);
    HBP_QuestInsertSurface(result, second, secondDepth, 2.0, limit);
    if (lastDepth < limit && result.a < 1.0h)
        HBP_QuestAccumulate(result, HBP_QUEST_LOAD(_HBPQuestTail, pixel));
    return result;
}

#endif
