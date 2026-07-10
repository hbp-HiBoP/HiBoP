using System;
using System.Runtime.InteropServices;
using HBP.Core.DLL.HbpCore;
using UnityEngine;

namespace HBP.Core.DLL
{
    public class CutGeometryGenerator : CppDLLImportBase
    {
        private NativeBackend m_Backend = NativeBackendOptions.ExperimentalBackend;

        #region Properties
        internal NativeBackend Backend => m_Backend;
        public bool UsesHbpCore => m_Backend == NativeBackend.HbpCore;
        public Vector2Int TextureSize
        {
            get
            {
                if (m_Backend != NativeBackend.HbpCore)
                {
                    return Vector2Int.zero;
                }

                ThrowIfFailed(hbp_cut_geometry_generator_get_texture_size(_handle.Handle, out TextureSize size));
                return new Vector2Int(size.width, size.height);
            }
        }
        public BBox BoundingBox
        {
            get
            {
                if (m_Backend == NativeBackend.HbpCore)
                {
                    ThrowIfFailed(hbp_cut_geometry_generator_get_bounding_box(_handle.Handle, out IntPtr bbox));
                    return new BBox(bbox, NativeBackend.HbpCore);
                }

                return new BBox(bounding_box_CutGeometryGenerator(_handle));
            }
        }
        #endregion

        #region Public Methods
        public void Initialize(Volume volume, Object3D.Cut cut, int maxTextureSize)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                EnsureHbpCoreVolume(volume, nameof(Initialize));
                ThrowIfFailed(hbp_cut_geometry_generator_initialize(
                    _handle.Handle,
                    volume.getHandle().Handle,
                    cut.getHandle().Handle,
                    (int)cut.Orientation,
                    cut.Flip ? 1 : 0,
                    maxTextureSize));
                return;
            }

            float[] planeCut = cut.ConvertToArray();
            initialize_CutGeometryGenerator(_handle, volume.getHandle(), planeCut, (int)cut.Orientation, cut.Flip, maxTextureSize);
        }

        public void UpdateSurfaceUV(Surface cutSurface)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                EnsureHbpCoreSurface(cutSurface, nameof(UpdateSurfaceUV));
                HbpCoreStatus status = hbp_cut_geometry_generator_update_surface_uv(_handle.Handle, cutSurface.getHandle().Handle);
                if (status != HbpCoreStatus.Ok)
                {
                    throw new InvalidOperationException($"hbp_core CutGeometryGenerator.UpdateSurfaceUV failed with status {status}: {HbpCoreRuntime.LastError} Vertices={cutSurface.NumberOfVertices} Triangles={cutSurface.NumberOfTriangles} TextureSize={TextureSize.x}x{TextureSize.y}");
                }
                return;
            }

            update_mesh_UV_CutGeometryGenerator(_handle, cutSurface.getHandle());
        }

        public Vector2 GetPositionRatioOnTexture(Vector3 point)
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                Vec3 nativePoint = Vec3.FromVector3(point);
                ThrowIfFailed(hbp_cut_geometry_generator_get_position_ratio_on_texture(_handle.Handle, ref nativePoint, out Vec2 ratio));
                return ratio.ToVector2();
            }

            float[] pointArray = new float[3] { -point.x, point.y, point.z };
            float[] resultArray = new float[2];
            get_position_ratio_on_texture_CutGeometryGenerator(_handle, pointArray, resultArray);
            return new Vector2(resultArray[0], resultArray[1]);
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_cut_geometry_generator_create(out IntPtr generator));
                _handle = new HandleRef(this, generator);
                return;
            }

            _handle = new HandleRef(this, create_CutGeometryGenerator());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            if (m_Backend == NativeBackend.HbpCore)
            {
                ThrowIfFailed(hbp_cut_geometry_generator_destroy(_handle.Handle));
                return;
            }

            delete_CutGeometryGenerator(_handle);
        }
        #endregion

        private static void EnsureHbpCoreVolume(Volume volume, string methodName)
        {
            if (volume.Backend != NativeBackend.HbpCore)
            {
                throw new InvalidOperationException($"CutGeometryGenerator.{methodName} cannot use a {volume.Backend} volume with hbp_core geometry.");
            }
        }

        private static void EnsureHbpCoreSurface(Surface surface, string methodName)
        {
            if (surface.Backend != NativeBackend.HbpCore)
            {
                throw new InvalidOperationException($"CutGeometryGenerator.{methodName} cannot use a {surface.Backend} surface with hbp_core geometry.");
            }
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core CutGeometryGenerator call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        #region DLLImport
        [DllImport(NativeDll.HbpExport, EntryPoint = "create_CutGeometryGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_CutGeometryGenerator();
        [DllImport(NativeDll.HbpExport, EntryPoint = "delete_CutGeometryGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_CutGeometryGenerator(HandleRef generator);
        [DllImport(NativeDll.HbpExport, EntryPoint = "initialize_CutGeometryGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initialize_CutGeometryGenerator(HandleRef generator, HandleRef volume, float[] planeCut, int orientation, bool flip, int maxTextureSize);
        [DllImport(NativeDll.HbpExport, EntryPoint = "update_mesh_UV_CutGeometryGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_mesh_UV_CutGeometryGenerator(HandleRef generator, HandleRef surface);
        [DllImport(NativeDll.HbpExport, EntryPoint = "get_position_ratio_on_texture_CutGeometryGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void get_position_ratio_on_texture_CutGeometryGenerator(HandleRef generator, float[] point, float[] result);
        [DllImport(NativeDll.HbpExport, EntryPoint = "bounding_box_CutGeometryGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr bounding_box_CutGeometryGenerator(HandleRef generator);

        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_geometry_generator_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_geometry_generator_create(out IntPtr generator);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_geometry_generator_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_geometry_generator_destroy(IntPtr generator);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_geometry_generator_initialize", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_geometry_generator_initialize(IntPtr generator, IntPtr volume, IntPtr plane, int cutOrientation, int flip, int maxTextureSize);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_geometry_generator_get_texture_size", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_geometry_generator_get_texture_size(IntPtr generator, out TextureSize outSize);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_geometry_generator_get_bounding_box", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_geometry_generator_get_bounding_box(IntPtr generator, out IntPtr bbox);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_geometry_generator_update_surface_uv", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_geometry_generator_update_surface_uv(IntPtr generator, IntPtr surface);
        [DllImport(NativeDll.HbpCore, EntryPoint = "hbp_cut_geometry_generator_get_position_ratio_on_texture", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_cut_geometry_generator_get_position_ratio_on_texture(IntPtr generator, ref Vec3 point, out Vec2 ratio);
        #endregion
    }
}
