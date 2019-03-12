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
        /// Generate UV for brain volume
        /// </summary>
        public class MRIVolumeGenerator : Tools.DLL.CppDLLImportBase, ICloneable
        {
            #region Properties
            /// <summary>
            /// Maximum density
            /// </summary>
            public float MaximumDensity
            {
                get
                {
                    return getMaximumDensity_MRIVolumeGenerator(_handle);
                }
            }
            /// <summary>
            /// Minimum influence
            /// </summary>
            public float MinimumInfluence
            {
                get
                {
                    return getMinInf_MRIVolumeGenerator(_handle);
                }
            }
            /// <summary>
            /// Maximum influence
            /// </summary>
            public float MaximumInfluence
            {
                get
                {
                    return getMaxInf_MRIVolumeGenerator(_handle);
                }
            }
            #endregion

            #region Public Methods
            /// <summary>
            /// 
            /// </summary>
            /// <param name="surface"></param>
            /// <param name="volume"></param>
            public void Reset(Surface surface, int precision)
            {
                reset_MRIVolumeGenerator(_handle, surface.getHandle(), precision);
                ApplicationState.DLLDebugManager.check_error();
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="rawPlotList"></param>
            public void InitializeOctree(RawSiteList rawPlotList)
            {
                initOctree_MRIVolumeGenerator(_handle, rawPlotList.getHandle());
                ApplicationState.DLLDebugManager.check_error();
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="maxDistance"></param>
            /// <param name="multiCPU"></param>
            /// <returns></returns>
            public bool ComputeDistances(float maxDistance, bool multiCPU)
            {
                bool noError = false;
                noError = computeDistances_MRIVolumeGenerator(_handle, maxDistance, multiCPU ? 1 : 0) == 1;
                ApplicationState.DLLDebugManager.check_error();

                if (!noError)
                    Debug.LogError("computeDistances_BrainSurfaceTextureGenerator failed ! (check DLL console debug output)");

                return noError;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="sharedMaxDensity"></param>
            /// <param name="sharedMinInf"></param>
            /// <param name="sharedMaxInf"></param>
            public void SynchronizeWithOthersGenerators(float sharedMaxDensity, float sharedMinInf, float sharedMaxInf)
            {
                synchronizeWithOthersGenerators_MRIVolumeGenerator(_handle, sharedMaxDensity, sharedMinInf, sharedMaxInf);
            }
            /// <summary>
            /// Compute the influence
            /// </summary>
            /// <param name="IEEGColumn"></param>
            /// <param name="multiCPU"></param>
            /// <param name="addValues"></param>
            /// <param name="ratioDistances"></param>
            /// <returns></returns>
            public bool ComputeInfluences(Column3DIEEG IEEGColumn, bool multiCPU, bool addValues = false, int ratioDistances = 0)
            {
                bool noError = false;
                noError = computeInfluences_MRIVolumeGenerator(_handle, IEEGColumn.IEEGValues, IEEGColumn.EEGDimensions, IEEGColumn.IEEGParameters.InfluenceDistance, multiCPU ? 1 : 0, addValues ? 1 : 0, ratioDistances,
                    IEEGColumn.IEEGParameters.Middle, IEEGColumn.IEEGParameters.SpanMin, IEEGColumn.IEEGParameters.SpanMax) == 1;
                ApplicationState.DLLDebugManager.check_error();

                if (!noError)
                    Debug.LogError("computeInfluences_BrainSurfaceTextureGenerator failed ! (check DLL console debug output)");

                return noError;
            }
            /// <summary>
            /// 
            /// </summary>
            public void AdjustInfluencesToColormap(Column3DIEEG column3DIEEG)
            {
                ajustInfluencesToColormap_MRIVolumeGenerator(_handle, column3DIEEG.IEEGParameters.Middle, column3DIEEG.IEEGParameters.SpanMin, column3DIEEG.IEEGParameters.SpanMax);
            }
            #endregion

            #region Memory Management
            /// <summary>
            /// Allocate DLL memory
            /// </summary>
            protected override void create_DLL_class()
            {
                _handle = new HandleRef(this, create_MRIVolumeGenerator());
            }
            /// <summary>
            /// Clean DLL memory
            /// </summary>
            protected override void delete_DLL_class()
            {
                delete_MRIVolumeGenerator(_handle);
            }
            /// <summary>
            /// BrainTextureGenerator default constructor
            /// </summary>
            public MRIVolumeGenerator() : base() { }
            /// <summary>
            /// BrainTextureGenerator copy constructor
            /// </summary>
            /// <param name="other"></param>
            public MRIVolumeGenerator(MRIVolumeGenerator other) : base(clone_MRIVolumeGenerator(other.getHandle())) { }
            /// <summary>
            /// Clone the BrainTextureGenerator
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                return new MRIVolumeGenerator(this);
            }
            #endregion

            #region DLLImport
            [DllImport("hbp_export", EntryPoint = "create_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr create_MRIVolumeGenerator();
            [DllImport("hbp_export", EntryPoint = "clone_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr clone_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator);
            [DllImport("hbp_export", EntryPoint = "delete_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void delete_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator);

            // actions
            [DllImport("hbp_export", EntryPoint = "reset_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void reset_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator, HandleRef handleSurface, int precision);
            [DllImport("hbp_export", EntryPoint = "initOctree_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void initOctree_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator, HandleRef handleRawPlotList);
            [DllImport("hbp_export", EntryPoint = "computeDistances_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern int computeDistances_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator, float maxDistance, int multiCPU);
            [DllImport("hbp_export", EntryPoint = "computeInfluences_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern int computeInfluences_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator, float[] timelineAmplitudes, int[] dimensions, float maxDistance, int multiCPU,
                int addValues, int ratioDistances, float middle, float spanMin, float spanMax);
            [DllImport("hbp_export", EntryPoint = "ajustInfluencesToColormap_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void ajustInfluencesToColormap_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator, float middle, float min, float max);
            [DllImport("hbp_export", EntryPoint = "synchronizeWithOthersGenerators_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void synchronizeWithOthersGenerators_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator, float sharedMaxDensity, float sharedMinInf, float sharedMaxInf);

            // retrieve data                            
            [DllImport("hbp_export", EntryPoint = "getMaximumDensity_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float getMaximumDensity_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator);
            [DllImport("hbp_export", EntryPoint = "getMinInf_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float getMinInf_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator);
            [DllImport("hbp_export", EntryPoint = "getMaxInf_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float getMaxInf_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator);

            #endregion
        }

    }
}