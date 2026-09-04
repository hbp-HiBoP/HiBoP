using System;

namespace CRNL.HiBoP.RenderModel
{
    public sealed class SurfaceRenderStreams
    {
        internal SurfaceRenderStreams(RenderBuffer<Float2> activityUvs, RenderBuffer<Float2> opacityUvs)
        {
            ActivityUvs = activityUvs;
            OpacityUvs = opacityUvs;
        }

        public RenderBuffer<Float2> ActivityUvs { get; }
        public RenderBuffer<Float2> OpacityUvs { get; }
    }

    /// <summary>CPU oracle used by the independent renderer and golden comparisons.</summary>
    public static class RenderModelReconstructor
    {
        public static SurfaceRenderStreams ReconstructSurfaceStreams(SurfaceFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            Float2[] activityUvs = new Float2[frame.VertexCount];
            Float2[] opacityUvs = new Float2[frame.VertexCount];
            for (int index = 0; index < frame.VertexCount; index++)
            {
                float y = frame.ActiveMask[index] == 1 ? 0f : 1f;
                activityUvs[index] = new Float2(frame.ActivityValues[index], y);
                opacityUvs[index] = new Float2(frame.OpacityValues[index], y);
            }

            return new SurfaceRenderStreams(RenderBuffer<Float2>.TakeOwnership(activityUvs), RenderBuffer<Float2>.TakeOwnership(opacityUvs));
        }
    }
}
