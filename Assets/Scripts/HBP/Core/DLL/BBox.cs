using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using UnityEngine;

namespace HBP.Core.DLL
{
    public class BBox : CppDLLImportBase
    {
        public Vector3 Min
        {
            get
            {
                GetUnityMinMax(out Vector3 min, out _);
                return min;
            }
        }

        public Vector3 Max
        {
            get
            {
                GetUnityMinMax(out _, out Vector3 max);
                return max;
            }
        }

        public Vector3 Center
        {
            get
            {
                ThrowIfFailed(hbp_bbox_get_center(_handle.Handle, out Vec3 value));
                return value.ToVector3();
            }
        }

        public float DiagonalLength => (Max - Min).magnitude;

        public List<Vector3> Points
        {
            get
            {
                Vec3[] points = new Vec3[8];
                ThrowIfFailed(hbp_bbox_get_points(_handle.Handle, points, points.Length));
                return new List<Vector3>(ToVector3Array(points));
            }
        }

        public List<Segment3> Segments
        {
            get
            {
                Vec3[] points = new Vec3[24];
                ThrowIfFailed(hbp_bbox_get_segments(_handle.Handle, points, points.Length));
                return ToSegments(ToVector3Array(points));
            }
        }

        public BBox()
        {
        }

        public BBox(IntPtr bBoxPointer) : base(bBoxPointer)
        {
        }

        public List<Vector3> IntersectionPointsWithPlane(Plane planeIntersec)
        {
            if (planeIntersec == null) throw new ArgumentNullException(nameof(planeIntersec));
            Vec3[] points = new Vec3[8];
            ThrowIfFailed(hbp_bbox_intersections_with_plane(_handle.Handle, planeIntersec.getHandle().Handle, points, points.Length, out int count));
            return new List<Vector3>(ToVector3Array(points, count));
        }

        public List<Segment3> IntersectionLinesWithPlane(Plane planeIntersec)
        {
            if (planeIntersec == null) throw new ArgumentNullException(nameof(planeIntersec));
            Vec3[] points = new Vec3[24];
            ThrowIfFailed(hbp_bbox_intersection_segments_with_plane(_handle.Handle, planeIntersec.getHandle().Handle, points, points.Length, out int count));
            return ToSegments(ToVector3Array(points, count * 2));
        }

        public Segment3 IntersectionSegmentBetweenTwoPlanes(Plane planeA, Plane planeB)
        {
            if (planeA == null) throw new ArgumentNullException(nameof(planeA));
            if (planeB == null) throw new ArgumentNullException(nameof(planeB));
            ThrowIfFailed(hbp_bbox_intersection_segment_between_planes(_handle.Handle, planeA.getHandle().Handle, planeB.getHandle().Handle, out Vec3 start, out Vec3 end, out int intersects));
            return intersects != 0 ? new Segment3(start.ToVector3(), end.ToVector3()) : null;
        }

        public void Update(BBox other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            ThrowIfFailed(hbp_bbox_update(_handle.Handle, other._handle.Handle));
        }

        public void Transform(Transformation3 transformation)
        {
            if (transformation == null) throw new ArgumentNullException(nameof(transformation));
            ThrowIfFailed(hbp_bbox_transform(_handle.Handle, transformation.getHandle().Handle));
        }

        public float SizeOffsetCutPlane(Plane cutPlane, int nbCuts)
        {
            if (cutPlane == null || nbCuts <= 0) return 0.0f;
            ThrowIfFailed(hbp_bbox_size_offset_cut_plane(_handle.Handle, cutPlane.getHandle().Handle, nbCuts, out float offset));
            return offset;
        }

        public bool Compare(BBox other)
        {
            return other != null && Min == other.Min && Max == other.Max && Center == other.Center;
        }

        public static BBox Merge(BBox bbox1, BBox bbox2)
        {
            BBox bbox = new();
            bbox.Update(bbox1);
            bbox.Update(bbox2);
            return bbox;
        }

        public static BBox FromMinMax(Vector3 min, Vector3 max)
        {
            Vec3 nativeMin = Vec3.FromVector3(min);
            Vec3 nativeMax = Vec3.FromVector3(max);
            NormalizeNativeMinMax(ref nativeMin, ref nativeMax);
            ThrowIfFailed(hbp_bbox_create_from_min_max(ref nativeMin, ref nativeMax, out IntPtr bbox));
            return new BBox(bbox);
        }

        protected override void create_DLL_class()
        {
            ThrowIfFailed(hbp_bbox_create(out IntPtr bbox));
            _handle = new HandleRef(this, bbox);
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_bbox_destroy(_handle.Handle));
        }

        private void GetUnityMinMax(out Vector3 min, out Vector3 max)
        {
            ThrowIfFailed(hbp_bbox_get_min(_handle.Handle, out Vec3 nativeMin));
            ThrowIfFailed(hbp_bbox_get_max(_handle.Handle, out Vec3 nativeMax));
            Vector3 first = nativeMin.ToVector3();
            Vector3 second = nativeMax.ToVector3();
            min = Vector3.Min(first, second);
            max = Vector3.Max(first, second);
        }

        private static Vector3[] ToVector3Array(Vec3[] points, int count = -1)
        {
            int actualCount = count < 0 ? points.Length : count;
            Vector3[] result = new Vector3[actualCount];
            for (int i = 0; i < actualCount; ++i) result[i] = points[i].ToVector3();
            return result;
        }

        private static List<Segment3> ToSegments(Vector3[] points)
        {
            List<Segment3> segments = new(points.Length / 2);
            for (int i = 0; i < points.Length; i += 2) segments.Add(new Segment3(points[i], points[i + 1]));
            return segments;
        }

        private static void NormalizeNativeMinMax(ref Vec3 min, ref Vec3 max)
        {
            Vec3 normalizedMin = new() { x = Math.Min(min.x, max.x), y = Math.Min(min.y, max.y), z = Math.Min(min.z, max.z) };
            Vec3 normalizedMax = new() { x = Math.Max(min.x, max.x), y = Math.Max(min.y, max.y), z = Math.Max(min.z, max.z) };
            min = normalizedMin;
            max = normalizedMax;
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core BBox call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_bbox_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_bbox_create(out IntPtr bbox);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_bbox_create_from_min_max", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_bbox_create_from_min_max(ref Vec3 min, ref Vec3 max, out IntPtr bbox);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_bbox_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_bbox_destroy(IntPtr bbox);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_bbox_update", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_bbox_update(IntPtr target, IntPtr source);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_bbox_transform", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_bbox_transform(IntPtr bbox, IntPtr transform);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_bbox_get_min", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_bbox_get_min(IntPtr bbox, out Vec3 min);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_bbox_get_max", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_bbox_get_max(IntPtr bbox, out Vec3 max);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_bbox_get_center", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_bbox_get_center(IntPtr bbox, out Vec3 center);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_bbox_get_points", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_bbox_get_points(IntPtr bbox, [Out] Vec3[] points, int pointCapacity);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_bbox_get_segments", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_bbox_get_segments(IntPtr bbox, [Out] Vec3[] segmentPoints, int pointCapacity);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_bbox_intersections_with_plane", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_bbox_intersections_with_plane(IntPtr bbox, IntPtr plane, [Out] Vec3[] points, int pointCapacity, out int count);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_bbox_intersection_segments_with_plane", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_bbox_intersection_segments_with_plane(IntPtr bbox, IntPtr plane, [Out] Vec3[] segmentPoints, int pointCapacity, out int segmentCount);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_bbox_intersection_segment_between_planes", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_bbox_intersection_segment_between_planes(IntPtr bbox, IntPtr planeA, IntPtr planeB, out Vec3 start, out Vec3 end, out int intersects);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_bbox_size_offset_cut_plane", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_bbox_size_offset_cut_plane(IntPtr bbox, IntPtr plane, int cutCount, out float offset);
    }
}
