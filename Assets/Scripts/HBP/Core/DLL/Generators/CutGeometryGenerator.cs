using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using UnityEngine;

namespace HBP.Core.DLL
{
    public class CutGeometryGenerator : CppDLLImportBase
    {
        public Vector2 PhysicalSize { get; private set; }
        private static bool s_DisplaySamplingAvailable = true;

        public Vector2Int TextureSize
        {
            get
            {
                ThrowIfFailed(hbp_cut_geometry_generator_get_texture_size(_handle.Handle, out TextureSize size));
                return new Vector2Int(size.width, size.height);
            }
        }

        public BBox BoundingBox
        {
            get
            {
                ThrowIfFailed(hbp_cut_geometry_generator_get_bounding_box(_handle.Handle, out IntPtr bbox));
                return new BBox(bbox);
            }
        }

        public void Initialize(Volume volume, Object3D.Cut cut, int maxTextureSize)
        {
            if (volume == null) throw new ArgumentNullException(nameof(volume));
            if (cut == null) throw new ArgumentNullException(nameof(cut));
            ThrowIfFailed(hbp_cut_geometry_generator_initialize(_handle.Handle, volume.getHandle().Handle, cut.getHandle().Handle, (int)cut.Orientation, cut.Flip ? 1 : 0, maxTextureSize));
        }

        /// <summary>Native voxel sampling when raw, otherwise trilinear anatomy at display resolution.</summary>
        public void InitializeDisplay(Volume volume, Object3D.Cut cut, bool rawCuts, int targetTextureSize = 512)
        {
            if (volume == null) throw new ArgumentNullException(nameof(volume));
            if (cut == null) throw new ArgumentNullException(nameof(cut));
            if (s_DisplaySamplingAvailable)
            {
                try
                {
                    ThrowIfFailed(hbp_cut_geometry_generator_initialize_display(_handle.Handle, volume.getHandle().Handle, cut.getHandle().Handle, (int)cut.Orientation, cut.Flip ? 1 : 0, rawCuts ? 1 : 0, targetTextureSize, out Vec2 physicalSize));
                    PhysicalSize = physicalSize.ToVector2();
                    return;
                }
                catch (EntryPointNotFoundException)
                {
                    s_DisplaySamplingAvailable = false;
                    Debug.LogWarning("This hbp_core plugin predates display sampling. Update the native plugin to enable voxel-accurate raw cuts and trilinear anatomy; retaining the previous cut renderer.");
                }
            }

            Initialize(volume, cut, -1);
            PhysicalSize = TextureSize;
        }

        public void UpdateSurfaceUV(Surface cutSurface)
        {
            if (cutSurface == null) throw new ArgumentNullException(nameof(cutSurface));
            HbpCoreStatus status = hbp_cut_geometry_generator_update_surface_uv(_handle.Handle, cutSurface.getHandle().Handle);
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core CutGeometryGenerator.UpdateSurfaceUV failed with status {status}: {HbpCoreRuntime.LastError} Vertices={cutSurface.NumberOfVertices} Triangles={cutSurface.NumberOfTriangles} TextureSize={TextureSize.x}x{TextureSize.y}");
            }
        }

        public Vector2 GetPositionRatioOnTexture(Vector3 point)
        {
            Vec3 nativePoint = Vec3.FromVector3(point);
            ThrowIfFailed(hbp_cut_geometry_generator_get_position_ratio_on_texture(_handle.Handle, ref nativePoint, out Vec2 ratio));
            return ratio.ToVector2();
        }

        protected override void create_DLL_class()
        {
            ThrowIfFailed(hbp_cut_geometry_generator_create(out IntPtr generator));
            _handle = new HandleRef(this, generator);
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_cut_geometry_generator_destroy(_handle.Handle));
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core CutGeometryGenerator call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_geometry_generator_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_cut_geometry_generator_create(out IntPtr generator);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_geometry_generator_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_cut_geometry_generator_destroy(IntPtr generator);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_geometry_generator_initialize", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_cut_geometry_generator_initialize(IntPtr generator, IntPtr volume, IntPtr plane, int cutOrientation, int flip, int maxTextureSize);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_geometry_generator_initialize_display", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_cut_geometry_generator_initialize_display(IntPtr generator, IntPtr volume, IntPtr plane, int cutOrientation, int flip, int rawCuts, int targetTextureSize, out Vec2 physicalSize);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_geometry_generator_get_texture_size", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_cut_geometry_generator_get_texture_size(IntPtr generator, out TextureSize outSize);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_geometry_generator_get_bounding_box", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_cut_geometry_generator_get_bounding_box(IntPtr generator, out IntPtr bbox);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_geometry_generator_update_surface_uv", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_cut_geometry_generator_update_surface_uv(IntPtr generator, IntPtr surface);

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_cut_geometry_generator_get_position_ratio_on_texture", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_cut_geometry_generator_get_position_ratio_on_texture(IntPtr generator, ref Vec3 point, out Vec2 ratio);
    }
}
