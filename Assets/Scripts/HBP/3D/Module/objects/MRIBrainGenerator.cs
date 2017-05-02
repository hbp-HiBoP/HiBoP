

/**
 * \file    TexturesGenerator.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define UITextureGenerator and DLL.BrainTextureGenerator classes
 */

// system
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// unity
using UnityEngine;

namespace HBP.Module3D
{
    namespace DLL
    {
        /// <summary>
        /// Generate UV for brain surfaces
        /// </summary>
        public class MRIBrainGenerator : CppDLLImportBase, ICloneable
        {
            #region Properties

            Vector2[] m_uvAmplitudesV = new Vector2[0];
            Vector2[] m_uvAlphaV = new Vector2[0];

            GCHandle m_uvAmplitudesHandle;
            GCHandle m_uvAlphaHandle;

            #endregion

            #region Public Methods

            /// <summary>
            /// 
            /// </summary>
            /// <param name="surface"></param>
            /// <param name="volume"></param>
            public void reset(DLL.Surface surface, Volume volume)
            {
                reset_BrainSurfaceTextureGenerator(_handle, surface.getHandle(), volume.getHandle());
                StaticComponents.DLLDebugManager.check_error();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="rawPlotList"></param>
            public void init_octree(RawSiteList rawPlotList)
            {
                initOctree_BrainSurfaceTextureGenerator(_handle, rawPlotList.getHandle());
                StaticComponents.DLLDebugManager.check_error();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="maxDistance"></param>
            /// <param name="multiCPU"></param>
            /// <returns></returns>
            public bool compute_distances(float maxDistance, bool multiCPU)
            {
                bool noError = false;
                noError = computeDistances_BrainSurfaceTextureGenerator( _handle, maxDistance, multiCPU ? 1 : 0) == 1;
                StaticComponents.DLLDebugManager.check_error();

                if (!noError)
                    Debug.LogError("computeDistances_BrainSurfaceTextureGenerator failed ! (check DLL console debug output)");

                return noError;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public float getMaximumDensity()
            {
                return getMaximumDensity_BrainSurfaceTextureGenerator( _handle);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public float getMinimumInfluence()
            {
                return getMinInf_BrainSurfaceTextureGenerator(_handle);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public float getMaximumInfluence()
            {
                return getMaxInf_BrainSurfaceTextureGenerator(_handle);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="sharedMaxDensity"></param>
            /// <param name="sharedMinInf"></param>
            /// <param name="sharedMaxInf"></param>
            public void synchronizeWithOthersGenerators(float sharedMaxDensity, float sharedMinInf, float sharedMaxInf)
            {
                synchronizeWithOthersGenerators_BrainSurfaceTextureGenerator(_handle, sharedMaxDensity, sharedMinInf, sharedMaxInf);
            }

            /// <summary>
            /// Compute the influence
            /// </summary>
            /// <param name="IEEGColumn"></param>
            /// <param name="multiCPU"></param>
            /// <param name="addValues"></param>
            /// <param name="ratioDistances"></param>
            /// <returns></returns>
            public bool compute_influences(Column3DViewIEEG IEEGColumn, bool multiCPU, bool addValues = false, bool ratioDistances = false)
            {
                bool noError = false;
                noError = computeInfluences_BrainSurfaceTextureGenerator(_handle, IEEGColumn.iEEG_values(), IEEGColumn.dimensions(), IEEGColumn.maxDistanceElec, multiCPU ? 1 : 0, addValues ? 1 : 0, ratioDistances ? 1 : 0,
                    IEEGColumn.middle, IEEGColumn.spanMin, IEEGColumn.spanMax) == 1;
                StaticComponents.DLLDebugManager.check_error();

                if(!noError)
                    Debug.LogError("computeInfluences_BrainSurfaceTextureGenerator failed ! (check DLL console debug output)");

                return noError;
            }

            /// <summary>
            /// 
            /// </summary>
            public void ajustInfluencesToColormap()
            {
                ajustInfluencesToColormap_BrainSurfaceTextureGenerator( _handle);
            }


            public void compute_UVMain_with_volume(DLL.Surface surface, DLL.Volume volume, float calMin, float calMax)
            {
                compute_UVMain_with_volume_BrainSurfaceTextureGenerator(_handle, volume.getHandle(), surface.getHandle(), calMin, calMax);// calMin, calMax);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="surface"></param>
            /// <param name="IEEGColumn"></param>
            /// <returns></returns>
            public bool compute_surface_UV_IEEG(DLL.Surface surface, Column3DViewIEEG IEEGColumn)
            {                
                bool noError = false;
                noError = computeSurfaceTextCoordAmplitudes_BrainSurfaceTextureGenerator( _handle, surface.getHandle(), IEEGColumn.currentTimeLineID, IEEGColumn.alphaMin, IEEGColumn.alphaMax) == 1;

                int m_nbVertices = surface.vertices_nb();
                if (m_nbVertices == 0) // mesh is empty
                    return true;

                // amplitudes
                if (m_uvAmplitudesV.Length != m_nbVertices)
                {
                    m_uvAmplitudesV = new Vector2[m_nbVertices];
                    m_uvAmplitudesHandle.Free();
                    m_uvAmplitudesHandle = GCHandle.Alloc(m_uvAmplitudesV, GCHandleType.Pinned); 
                }
                updateUVAmplitudes_BrainSurfaceTextureGenerator(_handle, m_uvAmplitudesHandle.AddrOfPinnedObject());

                // alpha
                if (m_uvAlphaV.Length != m_nbVertices)
                {
                    m_uvAlphaV = new Vector2[m_nbVertices];
                    m_uvAlphaHandle.Free();
                    m_uvAlphaHandle = GCHandle.Alloc(m_uvAlphaV, GCHandleType.Pinned);
                }
                updateUVAlpha_BrainSurfaceTextureGenerator(_handle, m_uvAlphaHandle.AddrOfPinnedObject());

                StaticComponents.DLLDebugManager.check_error();
                if (!noError)
                    Debug.LogError("computeSurfaceTextCoordAmplitudes_BrainSurfaceTextureGenerator failed ! (check DLL console debug output)");

                return noError;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public Vector2[] get_iEEG_UV()
            {
                return m_uvAmplitudesV;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public Vector2[] get_alpha_UV()
            {
                return m_uvAlphaV;
            }


            #endregion

            #region Memory Management

            /// <summary>
            /// Allocate DLL memory
            /// </summary>
            protected override void create_DLL_class()
            {
                _handle = new HandleRef(this,create_BrainSurfaceTextureGenerator());
            }

            /// <summary>
            /// Clean DLL memory
            /// </summary>
            protected override void delete_DLL_class()
            {
               delete_BrainSurfaceTextureGenerator(_handle);
            }

            /// <summary>
            /// BrainTextureGenerator default constructor
            /// </summary>
            public MRIBrainGenerator() : base() { }

            /// <summary>
            /// BrainTextureGenerator copy constructor
            /// </summary>
            /// <param name="other"></param>
            public MRIBrainGenerator(MRIBrainGenerator other) : base(clone_BrainSurfaceTextureGenerator(other.getHandle())) { }

            /// <summary>
            /// Clone the BrainTextureGenerator
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                return new MRIBrainGenerator(this);
            }

            #endregion

            #region DLLImport

            [DllImport("hbp_export", EntryPoint = "create_BrainSurfaceTextureGeneratorsContainer", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr create_BrainSurfaceTextureGeneratorsContainer();
            [DllImport("hbp_export", EntryPoint = "add_BrainSurfaceTextureGeneratorsContainer", CallingConvention = CallingConvention.Cdecl)]
            static private extern void add_BrainSurfaceTextureGeneratorsContainer(IntPtr container, HandleRef handleBrainSurfaceTextureGenerator);
            [DllImport("hbp_export", EntryPoint = "display_BrainSurfaceTextureGeneratorsContainer", CallingConvention = CallingConvention.Cdecl)]
            static private extern void display_BrainSurfaceTextureGeneratorsContainer(IntPtr container);

            // memory management
            [DllImport("hbp_export", EntryPoint = "create_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr create_BrainSurfaceTextureGenerator();
            [DllImport("hbp_export", EntryPoint = "clone_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr clone_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator);
            [DllImport("hbp_export", EntryPoint = "delete_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void delete_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator);

            // actions
            [DllImport("hbp_export", EntryPoint = "reset_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void reset_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, HandleRef handleSurface, HandleRef handleVolume);
            [DllImport("hbp_export", EntryPoint = "initOctree_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void initOctree_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, HandleRef handleRawPlotList);
            [DllImport("hbp_export", EntryPoint = "computeDistances_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern int computeDistances_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, float maxDistance, int multiCPU);
            [DllImport("hbp_export", EntryPoint = "computeInfluences_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern int computeInfluences_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, float[] timelineAmplitudes, int[] dimensions, float maxDistance, int multiCPU,
                int addValues, int ratioDistances, float middle, float spanMin, float spanMax);
            [DllImport("hbp_export", EntryPoint = "ajustInfluencesToColormap_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void ajustInfluencesToColormap_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator);
            [DllImport("hbp_export", EntryPoint = "synchronizeWithOthersGenerators_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void synchronizeWithOthersGenerators_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, float sharedMaxDensity, float sharedMinInf, float sharedMaxInf);

            [DllImport("hbp_export", EntryPoint = "compute_UVMain_with_volume_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void compute_UVMain_with_volume_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, HandleRef handleVolume, HandleRef handleSurface, float calMin, float calMax);

            // retrieve data                            
            [DllImport("hbp_export", EntryPoint = "computeSurfaceTextCoordAmplitudes_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern int computeSurfaceTextCoordAmplitudes_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, HandleRef handleSurface, int idTimeline, float alphaMin, float alphaMax);
            [DllImport("hbp_export", EntryPoint = "getUVAmplitudes_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void getUVAmplitudes_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, float[] textureCoordsArray);
            [DllImport("hbp_export", EntryPoint = "updateUVAmplitudes_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void updateUVAmplitudes_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, IntPtr uv);
            [DllImport("hbp_export", EntryPoint = "getUVAlpha_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void getUVAlpha_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, float[] textureCoordsArray);
            [DllImport("hbp_export", EntryPoint = "updateUVAlpha_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void updateUVAlpha_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, IntPtr uv);
            [DllImport("hbp_export", EntryPoint = "getMaximumDensity_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float getMaximumDensity_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator);
            [DllImport("hbp_export", EntryPoint = "getMinInf_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float getMinInf_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator);
            [DllImport("hbp_export", EntryPoint = "getMaxInf_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float getMaxInf_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator);

            #endregion
        }
        
    }
}