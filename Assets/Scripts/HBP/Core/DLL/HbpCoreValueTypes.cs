using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Core.DLL
{
    public static class ReferenceSystemConversion
    {
        public const bool InvertX = true;
        public const bool FlipsHandedness = InvertX;

        public static float ConvertX(float value)
        {
            return InvertX ? -value : value;
        }

        public static int[] ConvertTriangleWinding(int[] triangles, bool convertReferenceSystem = true)
        {
            if (!convertReferenceSystem || !FlipsHandedness)
            {
                return triangles;
            }

            int[] result = new int[triangles.Length];
            triangles.CopyTo(result, 0);
            for (int i = 0; i + 2 < result.Length; i += 3)
            {
                (result[i + 1], result[i + 2]) = (result[i + 2], result[i + 1]);
            }
            return result;
        }
    }

    /// <summary>
    /// A three-dimensional value expressed in the native right-handed hbp_core reference system.
    /// Unity <see cref="Vector3"/> values are left-handed by convention and cross the native boundary
    /// according to <see cref="ReferenceSystemConversion"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Vec3
    {
        public float x;
        public float y;
        public float z;

        /// <param name="value">A Unity-space value unless <paramref name="convertReferenceSystem"/> is false.</param>
        /// <param name="convertReferenceSystem">
        /// True for the normal Unity-to-native boundary conversion. False only when the Vector3 stores
        /// an explicitly named native value, or represents an unsigned magnitude such as voxel spacing.
        /// </param>
        public static Vec3 FromVector3(Vector3 value, bool convertReferenceSystem = true)
        {
            // Most runtime Vector3 values are in Unity space; hbp_core Vec3 values are in native/classical space.
            return new Vec3
            {
                x = convertReferenceSystem ? ReferenceSystemConversion.ConvertX(value.x) : value.x,
                y = value.y,
                z = value.z
            };
        }

        /// <param name="convertReferenceSystem">
        /// True for the normal native-to-Unity boundary conversion. False only when the result is an
        /// explicitly named native Vector3, or represents an unsigned magnitude such as voxel spacing.
        /// </param>
        public Vector3 ToVector3(bool convertReferenceSystem = true)
        {
            return new Vector3(convertReferenceSystem ? ReferenceSystemConversion.ConvertX(x) : x, y, z);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Vec2
    {
        public float x;
        public float y;

        public static Vec2 FromVector2(Vector2 value)
        {
            return new Vec2 { x = value.x, y = value.y };
        }

        public Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Color4
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public static Color4 FromColor(Color value)
        {
            return new Color4 { r = value.r, g = value.g, b = value.b, a = value.a };
        }

        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TextureSize
    {
        public int width;
        public int height;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VolumeDimensions
    {
        public int x;
        public int y;
        public int z;
        public int t;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VolumeExtrema
    {
        public float min;
        public float max;
        public float loadedCalMin;
        public float loadedCalMax;
        public float recomputedCalMin;
        public float recomputedCalMax;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IEEGComputeMetrics
    {
        public double totalMilliseconds;
        public double allocationMilliseconds;
        public double spatialIndexMilliseconds;
        public double spatialIndexBuildMilliseconds;
        public double spatialIndexLookupMilliseconds;
        public double neighborQueryMilliseconds;
        public double accumulationMilliseconds;
        public double normalizationMilliseconds;
        public long generatedPointCount;
        public long activeSiteCount;
        public long neighborLinkCount;
        public long storedValueCount;
        public long storedWeightCount;
        public long spatialIndexCacheHitCount;
        public long spatialIndexCacheMissCount;
        public long spatialIndexCacheEntryCount;
        public long spatialIndexCacheBytes;
        public long spatialIndexGeometryVersion;
        public long parallelWorkerCount;
        public long neighborBatchSize;
        public long neighborBatchCount;
        public long temporaryNeighborPeakBytes;
        public long temporaryNeighborBudgetBytes;
        public int timelineLength;
    }
}
