using HBP.Core.Tools;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Core.DLL
{
    public class SurfaceGenerator : CppDLLImportBase
    {
        #region Properties
        public ActivityGenerator ActivityGenerator { get; private set; }
        /// <summary>
        /// UVs for the activity colormap texture
        /// </summary>
        public Vector2[] ActivityUV { get; private set; } = new Vector2[0];
        /// <summary>
        /// UVs for the alpha of the site density (uses a black to white texture)
        /// </summary>
        public Vector2[] AlphaUV { get; private set; } = new Vector2[0];
        /// <summary>
        /// UVs used when no activity is computed
        /// </summary>
        public Vector2[] NullUV { get; private set; } = new Vector2[0];
        /// <summary>
        /// Pointer to an array of floats containing the activity UVs
        /// </summary>
        private GCHandle m_UVActivityHandle;
        /// <summary>
        /// Pointer to an array of floats containing the alpha UVs
        /// </summary>
        private GCHandle m_UVAlphaHandle;
        #endregion

        #region Public Methods
        public void Initialize(ActivityGenerator activityGenerator)
        {
            ActivityGenerator = activityGenerator;
            initialize_SurfaceGenerator(_handle, activityGenerator.getHandle());
        }
        public void ComputeMainUV(float calMin, float calMax)
        {
            compute_UV_main_SurfaceGenerator(_handle, calMin, calMax);
        }
        public void ComputeActivityUV(int timelineIndex = 0, float alpha = 0)
        {
            int nbVertices = ActivityGenerator.GeneratorSurface.Surface.NumberOfVertices;
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
            compute_UV_activity_SurfaceGenerator(_handle, timelineIndex, alpha, m_UVActivityHandle.AddrOfPinnedObject(), m_UVAlphaHandle.AddrOfPinnedObject());
        }
        public void ComputeNullUV()
        {
            NullUV = new Vector2[ActivityGenerator.GeneratorSurface.Surface.NumberOfVertices];
            NullUV.Fill(new Vector2(0.01f, 1f));
        }
        #endregion

        #region Memory Management
        /// <summary>
        /// Allocate DLL memory
        /// </summary>
        protected override void create_DLL_class()
        {
            _handle = new HandleRef(this, create_SurfaceGenerator());
        }
        /// <summary>
        /// Clean DLL memory
        /// </summary>
        protected override void delete_DLL_class()
        {
            delete_SurfaceGenerator(_handle);
        }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create_SurfaceGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_SurfaceGenerator();
        [DllImport("hbp_export", EntryPoint = "delete_SurfaceGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_SurfaceGenerator(HandleRef generator);
        [DllImport("hbp_export", EntryPoint = "initialize_SurfaceGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initialize_SurfaceGenerator(HandleRef generator, HandleRef activityGenerator);
        [DllImport("hbp_export", EntryPoint = "compute_UV_main_SurfaceGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_UV_main_SurfaceGenerator(HandleRef generator, float calMin, float calMax);
        [DllImport("hbp_export", EntryPoint = "compute_UV_activity_SurfaceGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_UV_activity_SurfaceGenerator(HandleRef generator, int timelineIndex, float alpha, IntPtr uvActivity, IntPtr uvAlpha);
        #endregion
    }
}