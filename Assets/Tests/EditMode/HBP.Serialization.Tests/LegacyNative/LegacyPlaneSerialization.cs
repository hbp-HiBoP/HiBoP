using HBP.Core.DLL;
using HBP.Core.DLL.HbpCore;

namespace HBP.Tests.Serialization
{
    internal static class LegacyPlaneSerialization
    {
        internal static float[] ConvertToArray(this Plane plane)
        {
            Vec3 point = Vec3.FromVector3(plane.Point);
            Vec3 normal = Vec3.FromVector3(plane.Normal);
            return new[] { point.x, point.y, point.z, normal.x, normal.y, normal.z };
        }
    }
}
