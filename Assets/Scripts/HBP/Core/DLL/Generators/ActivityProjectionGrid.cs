using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Core.DLL
{
    public class ActivityProjectionGrid : CppDLLImportBase
    {
        public Volume Volume { get; private set; }
        public int MaximumDimension { get; private set; }
        public VolumeInterpolation Interpolation { get; private set; }

        public Vector3Int Dimensions
        {
            get
            {
                ThrowIfFailed(hbp_activity_projection_grid_get_dimensions(_handle.Handle, out ActivityProjectionGridDimensions dimensions));
                return new Vector3Int(dimensions.x, dimensions.y, dimensions.z);
            }
        }

        public int PointCount
        {
            get
            {
                ThrowIfFailed(hbp_activity_projection_grid_get_point_count(_handle.Handle, out int count));
                return count;
            }
        }

        public Vector3[] Points
        {
            get
            {
                Vec3[] nativePoints = new Vec3[PointCount];
                ThrowIfFailed(hbp_activity_projection_grid_copy_points(_handle.Handle, nativePoints, nativePoints.Length));
                Vector3[] points = new Vector3[nativePoints.Length];
                for (int i = 0; i < points.Length; ++i)
                {
                    points[i] = nativePoints[i].ToVector3();
                }

                return points;
            }
        }

        public void Initialize(Volume volume, int maximumDimension)
        {
            Initialize(volume, maximumDimension, VolumeInterpolation.Nearest);
        }

        public void Initialize(Volume volume, int maximumDimension, VolumeInterpolation interpolation)
        {
            if (volume == null) throw new ArgumentNullException(nameof(volume));
            if (maximumDimension < 2) throw new ArgumentOutOfRangeException(nameof(maximumDimension));
            if (!Enum.IsDefined(typeof(VolumeInterpolation), interpolation))
            {
                throw new ArgumentOutOfRangeException(nameof(interpolation));
            }

            ThrowIfFailed(hbp_activity_projection_grid_initialize(_handle.Handle, volume.getHandle().Handle, maximumDimension));
            ThrowIfFailed(hbp_activity_projection_grid_set_volume_interpolation(_handle.Handle, interpolation));
            Volume = volume;
            MaximumDimension = maximumDimension;
            Interpolation = interpolation;
        }

        protected override void create_DLL_class()
        {
            ThrowIfFailed(hbp_activity_projection_grid_create(out IntPtr grid));
            _handle = new HandleRef(this, grid);
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_activity_projection_grid_destroy(_handle.Handle));
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core ActivityProjectionGrid call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_activity_projection_grid_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_activity_projection_grid_create(out IntPtr grid);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_activity_projection_grid_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_activity_projection_grid_destroy(IntPtr grid);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_activity_projection_grid_initialize", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_activity_projection_grid_initialize(IntPtr grid, IntPtr volume, int maximumDimension);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_activity_projection_grid_set_volume_interpolation", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_activity_projection_grid_set_volume_interpolation(IntPtr grid, VolumeInterpolation interpolation);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_activity_projection_grid_get_dimensions", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_activity_projection_grid_get_dimensions(IntPtr grid, out ActivityProjectionGridDimensions dimensions);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_activity_projection_grid_get_point_count", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_activity_projection_grid_get_point_count(IntPtr grid, out int count);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_activity_projection_grid_copy_points", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_activity_projection_grid_copy_points(IntPtr grid, [Out] Vec3[] points, int pointCapacity);
    }
}
