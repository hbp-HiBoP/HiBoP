using HBP.Core.DLL;
using HBP.Tests.Serialization;
using Plane = HBP.Core.DLL.Plane;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using UnityEngine;

namespace HBP.Tests.Serialization.LegacyNative
{
    /// <summary>
    /// Class representing the bounding box in the DLL
    /// </summary>
    public class BBox : CppDLLImportBase
    {
        private BenchmarkBackend m_Backend = OracleBackendContext.Current;
        internal BenchmarkBackend Backend => m_Backend;
        public bool UsesHbpCore => m_Backend == BenchmarkBackend.HbpCore;

        #region Properties

        /// <summary>
        /// Minimum point of the bounding box
        /// </summary>
        public Vector3 Min
        {
            get
            {
                if (m_Backend == BenchmarkBackend.HbpCore)
                {
                    GetHbpCoreUnityMinMax(out Vector3 min, out _);
                    return min;
                }

                float[] _min = new float[3];
                getMin_BBox(_handle, _min);
                return new Vector3(_min[0], _min[1], _min[2]);
            }
        }

        /// <summary>
        /// Maximum point of the bounding box
        /// </summary>
        public Vector3 Max
        {
            get
            {
                if (m_Backend == BenchmarkBackend.HbpCore)
                {
                    GetHbpCoreUnityMinMax(out _, out Vector3 max);
                    return max;
                }

                float[] _max = new float[3];
                getMax_BBox(_handle, _max);
                return new Vector3(_max[0], _max[1], _max[2]);
            }
        }

        /// <summary>
        /// Center point of the bounding box
        /// </summary>
        public Vector3 Center
        {
            get
            {
                if (m_Backend == BenchmarkBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_bbox_get_center(_handle.Handle, out Vec3 value));
                    return value.ToVector3();
                }

                float[] center = new float[3];
                getCenter_BBox(_handle, center);
                return new Vector3(center[0], center[1], center[2]);
            }
        }

        /// <summary>
        /// Length of the diagonal Max-Min
        /// </summary>
        public float DiagonalLength
        {
            get { return (Max - Min).magnitude; }
        }

        /// <summary>
        /// List of the points of the bounding box (8 points)
        /// </summary>
        public List<Vector3> Points
        {
            get
            {
                if (m_Backend == BenchmarkBackend.HbpCore)
                {
                    Vec3[] points = new Vec3[8];
                    ThrowIfFailed(hbp_bbox_get_points(_handle.Handle, points, points.Length));
                    return new List<Vector3>(ToVector3Array(points));
                }

                float[] pointsF = new float[3 * 8];
                getPoints_BBox(_handle, pointsF);
                List<Vector3> bboxPoints = new(8);

                for (int ii = 0; ii < 8; ii++)
                {
                    bboxPoints.Add(new Vector3(pointsF[3 * ii], pointsF[3 * ii + 1], pointsF[3 * ii + 2]));
                }

                return bboxPoints;
            }
        }

        /// <summary>
        /// List of the pairs of points composing the edges of the bounding box
        /// </summary>
        public List<Segment3> Segments
        {
            get
            {
                if (m_Backend == BenchmarkBackend.HbpCore)
                {
                    Vec3[] points = new Vec3[24];
                    ThrowIfFailed(hbp_bbox_get_segments(_handle.Handle, points, points.Length));
                    return ToSegments(ToVector3Array(points));
                }

                float[] pointsF = new float[3 * 2 * 12];
                getLinesPairPoints_BBox(_handle, pointsF);
                List<Segment3> linesPoints = new(12);

                for (int ii = 0; ii < 12; ii++)
                {
                    int offset = 6 * ii;
                    linesPoints.Add(new Segment3(new Vector3(pointsF[offset], pointsF[offset + 1], pointsF[offset + 2]), new Vector3(pointsF[offset + 3], pointsF[offset + 4], pointsF[offset + 5])));
                }

                return linesPoints;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get the points of the intersection of a plane and this bounding box
        /// </summary>
        /// <param name="planeIntersec">Plane to intersect with</param>
        /// <returns>List of the points composing the intersection</returns>
        public List<Vector3> IntersectionPointsWithPlane(Plane planeIntersec)
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                Vec3[] points = new Vec3[8];
                ThrowIfFailed(hbp_bbox_intersections_with_plane(_handle.Handle, planeIntersec.getHandle().Handle, points, points.Length, out int count));
                return new List<Vector3>(ToVector3Array(points, count));
            }

            float[] pointsF = new float[8 * 3];
            getIntersectionsWithPlane_BBox(_handle, planeIntersec.ConvertToArray(), pointsF);
            List<Vector3> intersecPoints = new(4);

            for (int ii = 0; ii < 8; ++ii)
            {
                Vector3 point = new(pointsF[3 * ii], pointsF[3 * ii + 1], pointsF[3 * ii + 2]);
                if (point.x == 0 && point.y == 0 && point.z == 0)
                    continue;
                intersecPoints.Add(point);
            }

            return intersecPoints;
        }

        /// <summary>
        /// Get the lines of the intersection of a plane and this bounding box
        /// </summary>
        /// <param name="planeIntersec">Plane to intersect with</param>
        /// <returns>List of the pairs of points composing the lines of the intersection</returns>
        public List<Segment3> IntersectionLinesWithPlane(Plane planeIntersec)
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                Vec3[] points = new Vec3[24];
                ThrowIfFailed(hbp_bbox_intersection_segments_with_plane(_handle.Handle, planeIntersec.getHandle().Handle, points, points.Length, out int segmentCount));
                return ToSegments(ToVector3Array(points, segmentCount * 2));
            }

            float[] pointsF = new float[4 * 2 * 3];
            getLinesIntersectionsWithPlane_BBox(_handle, planeIntersec.ConvertToArray(), pointsF);
            List<Segment3> intersecLines = new(4);

            for (int ii = 0; ii < 4; ++ii)
            {
                int offset = 6 * ii;
                intersecLines.Add(new Segment3(new Vector3(pointsF[offset], pointsF[offset + 1], pointsF[offset + 2]), new Vector3(pointsF[offset + 3], pointsF[offset + 4], pointsF[offset + 5])));
            }

            return intersecLines;
        }

        /// <summary>
        /// Get the intersection segment of two planes with the ends of the segment being on the bounding box
        /// </summary>
        /// <param name="planeA">First plane of the intersection</param>
        /// <param name="planeB">Second plane of the intersection</param>
        /// <returns>List of 2 points composing the segment</returns>
        public Segment3 IntersectionSegmentBetweenTwoPlanes(Plane planeA, Plane planeB)
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_bbox_intersection_segment_between_planes(_handle.Handle, planeA.getHandle().Handle, planeB.getHandle().Handle, out Vec3 start, out Vec3 end, out int intersects));
                return intersects != 0 ? new Segment3(start.ToVector3(), end.ToVector3()) : null;
            }

            float[] result = new float[2 * 3];
            bool isOk = find_intersection_segment_BBox(_handle, planeA.ConvertToArray(), planeB.ConvertToArray(), result);
            if (!isOk)
            {
                return null;
            }
            else
            {
                return new Segment3(new Vector3(result[0], result[1], result[2]), new Vector3(result[3], result[4], result[5]));
            }
        }

        /// <summary>
        /// Merge two BBox into one
        /// </summary>
        /// <param name="other"></param>
        public void Update(BBox other)
        {
            if (m_Backend != other.m_Backend)
            {
                throw new InvalidOperationException("Cannot merge BBox handles created by different native backends.");
            }

            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_bbox_update(_handle.Handle, other._handle.Handle));
                return;
            }

            update_BBox(_handle, other.getHandle());
        }

        public void Transform(Transformation3 transformation)
        {
            if (m_Backend != BenchmarkBackend.HbpCore)
            {
                throw new NotSupportedException("BBox.Transform is only available for hbp_core BBox instances in step 5.");
            }

            ThrowIfFailed(hbp_bbox_transform(_handle.Handle, transformation.getHandle().Handle));
        }

        /// <summary>
        /// Get the offset value for a cut plane given the number of cuts
        /// </summary>
        /// <param name="cutPlane">Cut plane to compute the offset for</param>
        /// <param name="nbCuts">Number of desired cuts</param>
        /// <returns>Value of the offset</returns>
        public float SizeOffsetCutPlane(Plane cutPlane, int nbCuts)
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                if (cutPlane == null || nbCuts <= 0)
                {
                    return 0.0f;
                }

                ThrowIfFailed(hbp_bbox_size_offset_cut_plane(_handle.Handle, cutPlane.getHandle().Handle, nbCuts, out float offset));
                return offset;
            }

            return size_offset_cut_plane_Surface(_handle, cutPlane.ConvertToArray(), nbCuts);
        }

        public bool Compare(BBox other)
        {
            return (Min == other.Min && Max == other.Max && Center == other.Center);
        }

        #endregion

        #region Memory Management

        public BBox()
        {
        }

        public BBox(IntPtr bBoxPointer) : base(bBoxPointer)
        {
            m_Backend = BenchmarkBackend.HbpExport;
        }

        internal BBox(IntPtr bBoxPointer, BenchmarkBackend backend) : base(bBoxPointer)
        {
            m_Backend = backend;
        }

        public static BBox Merge(BBox bbox1, BBox bbox2)
        {
            BBox bbox = new();
            bbox.Update(bbox1);
            bbox.Update(bbox2);
            return bbox;
        }

        public static BBox CreateHbpCore(Vector3 min, Vector3 max)
        {
            Vec3 nativeMin = Vec3.FromVector3(min);
            Vec3 nativeMax = Vec3.FromVector3(max);
            NormalizeNativeMinMax(ref nativeMin, ref nativeMax);
            ThrowIfFailed(hbp_bbox_create_from_min_max(ref nativeMin, ref nativeMax, out IntPtr bbox));
            return new BBox(bbox, BenchmarkBackend.HbpCore);
        }

        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_bbox_create(out IntPtr bbox));
                _handle = new HandleRef(this, bbox);
                return;
            }

            _handle = new HandleRef(this, create_BBox());
        }

        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_bbox_destroy(_handle.Handle));
                return;
            }

            delete_BBox(_handle);
        }

        #endregion

        private static Vector3[] ToVector3Array(Vec3[] points, int count = -1)
        {
            int actualCount = count < 0 ? points.Length : count;
            Vector3[] result = new Vector3[actualCount];
            for (int i = 0; i < actualCount; ++i)
            {
                result[i] = points[i].ToVector3();
            }

            return result;
        }

        private void GetHbpCoreUnityMinMax(out Vector3 min, out Vector3 max)
        {
            ThrowIfFailed(hbp_bbox_get_min(_handle.Handle, out Vec3 nativeMin));
            ThrowIfFailed(hbp_bbox_get_max(_handle.Handle, out Vec3 nativeMax));
            Vector3 first = nativeMin.ToVector3();
            Vector3 second = nativeMax.ToVector3();
            min = Vector3.Min(first, second);
            max = Vector3.Max(first, second);
        }

        private static void NormalizeNativeMinMax(ref Vec3 min, ref Vec3 max)
        {
            Vec3 normalizedMin = new()
            {
                x = Math.Min(min.x, max.x),
                y = Math.Min(min.y, max.y),
                z = Math.Min(min.z, max.z)
            };
            Vec3 normalizedMax = new()
            {
                x = Math.Max(min.x, max.x),
                y = Math.Max(min.y, max.y),
                z = Math.Max(min.z, max.z)
            };
            min = normalizedMin;
            max = normalizedMax;
        }

        private static List<Segment3> ToSegments(Vector3[] points)
        {
            List<Segment3> segments = new(points.Length / 2);
            for (int i = 0; i < points.Length; i += 2)
            {
                segments.Add(new Segment3(points[i], points[i + 1]));
            }

            return segments;
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core BBox call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        #region DLLImport

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "create_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_BBox();

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "delete_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_BBox(HandleRef handleBBox);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "getMin_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getMin_BBox(HandleRef handleBBox, float[] min);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "getMax_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getMax_BBox(HandleRef handleBBox, float[] max);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "getPoints_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getPoints_BBox(HandleRef handleBBox, float[] points);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "getLinesPairPoints_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getLinesPairPoints_BBox(HandleRef handleBBox, float[] points);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "getIntersectionsWithPlane_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getIntersectionsWithPlane_BBox(HandleRef handleBBox, float[] plane, float[] interPoints);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "getLinesIntersectionsWithPlane_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getLinesIntersectionsWithPlane_BBox(HandleRef handleBBox, float[] plane, float[] interPoints);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "find_intersection_segment_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern bool find_intersection_segment_BBox(HandleRef handleBBox, float[] planeA, float[] planeB, float[] interPoints);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "getCenter_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void getCenter_BBox(HandleRef handleBBox, float[] center);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "update_BBox", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_BBox(HandleRef handleBBox1, HandleRef handleBBox2);

        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "size_offset_cut_plane_Surface", CallingConvention = CallingConvention.Cdecl)]
        static private extern float size_offset_cut_plane_Surface(HandleRef handleSurface, float[] planeCut, int nbCuts);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_bbox_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_bbox_create(out IntPtr bbox);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_bbox_create_from_min_max", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_bbox_create_from_min_max(ref Vec3 min, ref Vec3 max, out IntPtr bbox);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_bbox_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_bbox_destroy(IntPtr bbox);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_bbox_update", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_bbox_update(IntPtr target, IntPtr source);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_bbox_transform", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_bbox_transform(IntPtr bbox, IntPtr transform);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_bbox_get_min", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_bbox_get_min(IntPtr bbox, out Vec3 min);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_bbox_get_max", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_bbox_get_max(IntPtr bbox, out Vec3 max);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_bbox_get_center", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_bbox_get_center(IntPtr bbox, out Vec3 center);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_bbox_get_points", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_bbox_get_points(IntPtr bbox, [Out] Vec3[] points, int pointCapacity);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_bbox_get_segments", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_bbox_get_segments(IntPtr bbox, [Out] Vec3[] segmentPoints, int pointCapacity);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_bbox_intersections_with_plane", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_bbox_intersections_with_plane(IntPtr bbox, IntPtr plane, [Out] Vec3[] points, int pointCapacity, out int count);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_bbox_intersection_segments_with_plane", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_bbox_intersection_segments_with_plane(IntPtr bbox, IntPtr plane, [Out] Vec3[] segmentPoints, int pointCapacity, out int segmentCount);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_bbox_intersection_segment_between_planes", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_bbox_intersection_segment_between_planes(IntPtr bbox, IntPtr planeA, IntPtr planeB, out Vec3 start, out Vec3 end, out int intersects);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_bbox_size_offset_cut_plane", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_bbox_size_offset_cut_plane(IntPtr bbox, IntPtr plane, int cutCount, out float offset);

        #endregion
    }
}
