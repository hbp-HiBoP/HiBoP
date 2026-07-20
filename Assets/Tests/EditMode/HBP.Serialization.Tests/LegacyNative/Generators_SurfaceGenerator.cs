using HBP.Core.DLL;
using HBP.Tests.Serialization;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Tools;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Tests.Serialization.LegacyNative
{
    public class SurfaceGenerator : CppDLLImportBase
    {
        private BenchmarkBackend m_Backend = OracleBackendContext.Current;

        #region Properties
        internal BenchmarkBackend Backend => m_Backend;
        public bool UsesHbpCore => m_Backend == BenchmarkBackend.HbpCore;
        public ActivityGenerator ActivityGenerator { get; private set; }
        public Vector2[] ActivityUV { get; private set; } = new Vector2[0];
        public Vector2[] AlphaUV { get; private set; } = new Vector2[0];
        public Vector2[] NullUV { get; private set; } = new Vector2[0];
        private GCHandle m_UVActivityHandle;
        private GCHandle m_UVAlphaHandle;
        #endregion

        #region Public Methods
        public void Initialize(ActivityGenerator activityGenerator)
        {
            ActivityGenerator = activityGenerator;
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                if (activityGenerator.Backend != BenchmarkBackend.HbpCore)
                {
                    throw new InvalidOperationException($"SurfaceGenerator.Initialize cannot use a {activityGenerator.Backend} activity generator with hbp_core.");
                }
                ThrowIfFailed(hbp_surface_generator_initialize(_handle.Handle, activityGenerator.getHandle().Handle));
                return;
            }
            initialize_SurfaceGenerator(_handle, activityGenerator.getHandle());
        }

        public void ComputeMainUV(float calMin, float calMax)
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_surface_generator_compute_main_uv(_handle.Handle, calMin, calMax));
                return;
            }
            compute_UV_main_SurfaceGenerator(_handle, calMin, calMax);
        }

        public void ComputeActivityUV(int timelineIndex = 0, float alpha = 0)
        {
            int nbVertices = ActivityGenerator.GeneratorSurface.Surface.NumberOfVertices;
            EnsureUvArrays(nbVertices);

            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_surface_generator_compute_activity_uv(_handle.Handle, timelineIndex, alpha));
                Vec2[] nativeActivity = new Vec2[nbVertices];
                Vec2[] nativeAlpha = new Vec2[nbVertices];
                ThrowIfFailed(hbp_surface_generator_copy_activity_uvs(_handle.Handle, nativeActivity, nativeActivity.Length));
                ThrowIfFailed(hbp_surface_generator_copy_alpha_uvs(_handle.Handle, nativeAlpha, nativeAlpha.Length));
                for (int i = 0; i < nbVertices; ++i)
                {
                    ActivityUV[i] = nativeActivity[i].ToVector2();
                    AlphaUV[i] = nativeAlpha[i].ToVector2();
                }
                return;
            }

            compute_UV_activity_SurfaceGenerator(_handle, timelineIndex, alpha, m_UVActivityHandle.AddrOfPinnedObject(), m_UVAlphaHandle.AddrOfPinnedObject());
        }

        public void ComputeNullUV()
        {
            NullUV = new Vector2[ActivityGenerator.GeneratorSurface.Surface.NumberOfVertices];
            NullUV.Fill(new Vector2(0.01f, 1f));
        }
        #endregion

        #region Memory Management
        protected override void create_DLL_class()
        {
            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_surface_generator_create(out IntPtr generator));
                _handle = new HandleRef(this, generator);
                return;
            }
            _handle = new HandleRef(this, create_SurfaceGenerator());
        }

        protected override void delete_DLL_class()
        {
            if (m_UVActivityHandle.IsAllocated) m_UVActivityHandle.Free();
            if (m_UVAlphaHandle.IsAllocated) m_UVAlphaHandle.Free();

            if (m_Backend == BenchmarkBackend.HbpCore)
            {
                ThrowIfFailed(hbp_surface_generator_destroy(_handle.Handle));
                return;
            }
            delete_SurfaceGenerator(_handle);
        }
        #endregion

        private void EnsureUvArrays(int nbVertices)
        {
            if (ActivityUV.Length != nbVertices)
            {
                ActivityUV = new Vector2[nbVertices];
                if (m_UVActivityHandle.IsAllocated) m_UVActivityHandle.Free();
                m_UVActivityHandle = GCHandle.Alloc(ActivityUV, GCHandleType.Pinned);
            }
            if (AlphaUV.Length != nbVertices)
            {
                AlphaUV = new Vector2[nbVertices];
                if (m_UVAlphaHandle.IsAllocated) m_UVAlphaHandle.Free();
                m_UVAlphaHandle = GCHandle.Alloc(AlphaUV, GCHandleType.Pinned);
            }
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core SurfaceGenerator call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        #region DLLImport
        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "create_SurfaceGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_SurfaceGenerator();
        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "delete_SurfaceGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_SurfaceGenerator(HandleRef generator);
        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "initialize_SurfaceGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initialize_SurfaceGenerator(HandleRef generator, HandleRef activityGenerator);
        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "compute_UV_main_SurfaceGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_UV_main_SurfaceGenerator(HandleRef generator, float calMin, float calMax);
        [DllImport(LegacyNativeLibrary.HbpExport, EntryPoint = "compute_UV_activity_SurfaceGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_UV_activity_SurfaceGenerator(HandleRef generator, int timelineIndex, float alpha, IntPtr uvActivity, IntPtr uvAlpha);

        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_surface_generator_create", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_generator_create(out IntPtr generator);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_surface_generator_destroy", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_generator_destroy(IntPtr generator);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_surface_generator_initialize", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_generator_initialize(IntPtr generator, IntPtr activityGenerator);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_surface_generator_compute_main_uv", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_generator_compute_main_uv(IntPtr generator, float calMin, float calMax);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_surface_generator_compute_activity_uv", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_generator_compute_activity_uv(IntPtr generator, int timelineIndex, float alpha);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_surface_generator_copy_activity_uvs", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_generator_copy_activity_uvs(IntPtr generator, [Out] Vec2[] uvs, int uvCapacity);
        [DllImport(LegacyNativeLibrary.HbpCore, EntryPoint = "hbp_surface_generator_copy_alpha_uvs", CallingConvention = CallingConvention.Cdecl)]
        static private extern HbpCoreStatus hbp_surface_generator_copy_alpha_uvs(IntPtr generator, [Out] Vec2[] uvs, int uvCapacity);
        #endregion
    }
}
