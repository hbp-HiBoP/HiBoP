using System;
using System.Runtime.InteropServices;

namespace HBP.Core.DLL
{
    public enum SurfaceProjectionClassification
    {
        Unavailable = 0,
        None = 1,
        Partial = 2,
        Complete = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SurfaceProjectionCoverage
    {
        public int totalVertexCount;
        public int validVertexCount;
        public int invalidVertexCount;
        public float validRatio;
        public SurfaceProjectionClassification classification;
        public ulong bindingVersion;
        public double buildMilliseconds;

        public int WarningInvalidVertexThreshold => Math.Max(32, (int)Math.Floor(totalVertexCount * 0.01));

        public bool RequiresUserMessage => classification == SurfaceProjectionClassification.None || (classification == SurfaceProjectionClassification.Partial && invalidVertexCount > WarningInvalidVertexThreshold);
    }
}
