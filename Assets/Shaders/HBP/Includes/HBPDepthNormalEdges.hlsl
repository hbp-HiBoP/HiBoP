#ifndef HBP_DEPTH_NORMAL_EDGES_INCLUDED
#define HBP_DEPTH_NORMAL_EDGES_INCLUDED

float HBP_DepthCurvature(float centerDepth, float negativeDepth, float positiveDepth)
{
    float neighborsHaveSurface = step(0.0001, negativeDepth) * step(0.0001, positiveDepth);
    float curvature = abs(negativeDepth + positiveDepth - 2.0 * centerDepth) / max(centerDepth, 1.0);
    return max(1.0 - neighborsHaveSurface, curvature);
}

float HBP_NormalDifference(float4 centerData, float4 sampleData)
{
    float sampleHasSurface = step(0.0001, sampleData.a);
    float3 centerNormal = centerData.rgb * 2.0 - 1.0;
    float3 sampleNormal = sampleData.rgb * 2.0 - 1.0;
    return sampleHasSurface * (1.0 - dot(centerNormal, sampleNormal));
}

float HBP_OpaqueEdge(float4 center, float4 left, float4 right, float4 down, float4 up, float depthThreshold, float normalThreshold)
{
    float depth = max(HBP_DepthCurvature(center.a, left.a, right.a), HBP_DepthCurvature(center.a, down.a, up.a));
    float normal = max(max(HBP_NormalDifference(center, left), HBP_NormalDifference(center, right)),
                       max(HBP_NormalDifference(center, down), HBP_NormalDifference(center, up)));
    return step(0.0001, center.a) * max(smoothstep(depthThreshold, 2.0 * depthThreshold, depth),
                                      smoothstep(normalThreshold, 2.0 * normalThreshold, normal));
}

#endif
