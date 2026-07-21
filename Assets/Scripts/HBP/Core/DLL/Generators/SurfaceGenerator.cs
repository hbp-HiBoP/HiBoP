using HBP.Core.DLL.HbpCore;
using HBP.Core.Tools;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Core.DLL
{
    public class SurfaceGenerator : CppDLLImportBase
    {
        public ActivityGenerator ActivityGenerator { get; private set; }
        public Vector2[] ActivityUV { get; private set; } = Array.Empty<Vector2>();
        public Vector2[] AlphaUV { get; private set; } = Array.Empty<Vector2>();
        public Vector2[] NullUV { get; private set; } = Array.Empty<Vector2>();
        private Vec2[] m_NativeActivityUV = Array.Empty<Vec2>();
        private Vec2[] m_NativeAlphaUV = Array.Empty<Vec2>();

        public void Initialize(ActivityGenerator activityGenerator)
        {
            if (activityGenerator == null) throw new ArgumentNullException(nameof(activityGenerator));
            ActivityGenerator = activityGenerator;
            ThrowIfFailed(hbp_surface_generator_initialize(_handle.Handle, activityGenerator.getHandle().Handle));
        }

        public void ComputeMainUV(float calMin, float calMax)
        {
            ThrowIfFailed(hbp_surface_generator_compute_main_uv(_handle.Handle, calMin, calMax));
        }

        public void ComputeActivityUV(int timelineIndex = 0, float alpha = 0)
        {
            int nbVertices = ActivityGenerator.GeneratorSurface.Surface.NumberOfVertices;
            EnsureUvArrays(nbVertices);
            ThrowIfFailed(hbp_surface_generator_compute_activity_uv(_handle.Handle, timelineIndex, alpha));
            ThrowIfFailed(hbp_surface_generator_copy_activity_uvs(_handle.Handle, m_NativeActivityUV, m_NativeActivityUV.Length));
            ThrowIfFailed(hbp_surface_generator_copy_alpha_uvs(_handle.Handle, m_NativeAlphaUV, m_NativeAlphaUV.Length));
            for (int i = 0; i < nbVertices; ++i)
            {
                ActivityUV[i] = m_NativeActivityUV[i].ToVector2();
                AlphaUV[i] = m_NativeAlphaUV[i].ToVector2();
            }
        }

        public void ComputeNullUV()
        {
            int vertexCount = ActivityGenerator.GeneratorSurface.Surface.NumberOfVertices;
            if (NullUV.Length != vertexCount)
                NullUV = new Vector2[vertexCount];
            NullUV.Fill(new Vector2(0.01f, 1f));
        }

        protected override void create_DLL_class()
        {
            ThrowIfFailed(hbp_surface_generator_create(out IntPtr generator));
            _handle = new HandleRef(this, generator);
        }

        protected override void delete_DLL_class()
        {
            ThrowIfFailed(hbp_surface_generator_destroy(_handle.Handle));
        }

        private void EnsureUvArrays(int nbVertices)
        {
            if (ActivityUV.Length != nbVertices) ActivityUV = new Vector2[nbVertices];
            if (AlphaUV.Length != nbVertices) AlphaUV = new Vector2[nbVertices];
            if (m_NativeActivityUV.Length != nbVertices) m_NativeActivityUV = new Vec2[nbVertices];
            if (m_NativeAlphaUV.Length != nbVertices) m_NativeAlphaUV = new Vec2[nbVertices];
        }

        private static void ThrowIfFailed(HbpCoreStatus status)
        {
            if (status != HbpCoreStatus.Ok)
            {
                throw new InvalidOperationException($"hbp_core SurfaceGenerator call failed with status {status}: {HbpCoreRuntime.LastError}");
            }
        }

        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_generator_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_generator_create(out IntPtr generator);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_generator_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_generator_destroy(IntPtr generator);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_generator_initialize", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_generator_initialize(IntPtr generator, IntPtr activityGenerator);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_generator_compute_main_uv", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_generator_compute_main_uv(IntPtr generator, float calMin, float calMax);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_generator_compute_activity_uv", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_generator_compute_activity_uv(IntPtr generator, int timelineIndex, float alpha);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_generator_copy_activity_uvs", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_generator_copy_activity_uvs(IntPtr generator, [Out] Vec2[] uvs, int uvCapacity);
        [DllImport(HbpCoreLibrary.Name, EntryPoint = "hbp_surface_generator_copy_alpha_uvs", CallingConvention = CallingConvention.Cdecl)]
        private static extern HbpCoreStatus hbp_surface_generator_copy_alpha_uvs(IntPtr generator, [Out] Vec2[] uvs, int uvCapacity);
    }
}
