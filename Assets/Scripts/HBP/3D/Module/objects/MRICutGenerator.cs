

/**
 * \file    MRICutGenerator.cs
 * \author  Lance Florian
 * \date    25/10/2016
 * \brief   Define DLL.MRIGeometryGenerator and DLL.MRITextureGenerator classes
 */

// system
using System;
using System.Runtime.InteropServices;

// unity
using UnityEngine;

namespace HBP.Module3D
{
    namespace DLL
    {
        /// <summary>
        /// Geometry generator of a MRI cut, one generator is defined for all columns, all columns share the same geometry
        /// </summary>
        public class MRIGeometryCutGenerator : Tools.DLL.CppDLLImportBase
        {
            #region Public Methods 
            /// <summary>
            /// 
            /// </summary>
            /// <param name="volume"></param>
            /// <param name="plane"></param>
            public void Reset(DLL.Volume volume, Plane plane)
            {
                float[] planeF = new float[6];
                for (int ii = 0; ii < 3; ++ii)
                {
                    planeF[ii]     = plane.Point[ii];
                    planeF[ii + 3] = plane.Normal[ii];
                }

                reset__MRIGeometryCutGenerator(_handle, volume.getHandle(), planeF, 1.0f);
                ApplicationState.DLLDebugManager.check_error();
            }
            /// <summary>
            /// Update the UV of the input mesh
            /// </summary>
            /// <param name="mesh"></param>
            public void UpdateCutMeshUV(DLL.Surface mesh)
            {
                update_cut_mesh_UV__MRIGeometryCutGenerator(_handle, mesh.getHandle());
                ApplicationState.DLLDebugManager.check_error();
            }
            #endregion

            #region Memory Management
            /// <summary>
            /// Allocate DLL memory
            /// </summary>
            protected override void create_DLL_class()
            {
                _handle = new HandleRef(this, create__MRIGeometryCutGenerator());
            }
            /// <summary>
            /// Clean DLL memory
            /// </summary>
            protected override void delete_DLL_class()
            {
                delete__MRIGeometryCutGenerator(_handle);
            }
            /// <summary>
            /// CutTextureGenerator default constructor
            /// </summary>
            public MRIGeometryCutGenerator() : base() { }
            #endregion

            #region DLLImport

            [DllImport("hbp_export", EntryPoint = "create__MRIGeometryCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr create__MRIGeometryCutGenerator();

            [DllImport("hbp_export", EntryPoint = "delete__MRIGeometryCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void delete__MRIGeometryCutGenerator(HandleRef handleMRIGeometryCutGenerator);

            [DllImport("hbp_export", EntryPoint = "reset__MRIGeometryCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void reset__MRIGeometryCutGenerator(HandleRef handleMRIGeometryCutGenerator, HandleRef handleVolume, float[] plane, float scaleFactor);

            [DllImport("hbp_export", EntryPoint = "update_cut_mesh_UV__MRIGeometryCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void update_cut_mesh_UV__MRIGeometryCutGenerator(HandleRef handleMRIGeometryCutGenerator, HandleRef handleSurface);


            #endregion
        }

        /// <summary>
        /// Texture generator for MRI cuts, or each column a generator is defined, all columns have differents textures
        /// </summary>
        public class MRITextureCutGenerator : Tools.DLL.CppDLLImportBase
        {
            #region Properties
            /// <summary>
            /// Maximum density
            /// </summary>
            /// <returns></returns>
            public float MaximumDensity
            {
                get
                {
                    return max_density__MRITextureCutGenerator(_handle);
                }
            }
            /// <summary>
            /// Minimum influence
            /// </summary>
            /// <returns></returns>
            public float MinimumInfluence
            {
                get
                {
                    return min_inf__MRITextureCutGenerator(_handle);
                }
            }
            /// <summary>
            /// Maximum influence
            /// </summary>
            /// <returns></returns>
            public float MaximumInfluence
            {
                get
                {
                    return max_inf__MRITextureCutGenerator(_handle);
                }
            }
            #endregion

            #region Public Methods
            /// <summary>
            /// 
            /// </summary>
            /// <param name="geometryGenerator"></param>
            public void Reset(MRIGeometryCutGenerator geometryGenerator)
            {
                reset__MRITextureCutGenerator(_handle, geometryGenerator.getHandle());
            }
            /// <summary>
            /// Will create the cut texture with colors defined with the volume voxels [0f , 1f] and the colorscheme (Gradient of colors)
            /// </summary>
            /// <param name="volume"></param>
            /// <param name="colorScheme"></param>
            /// <param name="calMin"></param>
            /// <param name="calMax"></param>
            public void FillTextureWithVolume(Volume volume, Texture colorScheme, float calMin, float calMax)
            {
                fill_texture_with_volume__MRITextureCutGenerator(_handle, volume.getHandle(), colorScheme.getHandle(), calMin, calMax);
                ApplicationState.DLLDebugManager.check_error();
            }
            /// <summary>
            /// Will update the previously MRI colored texture with a fMRI
            /// </summary>
            /// <param name="column"></param>
            /// <param name="volume"></param>
            public void FillTextureWithFMRI(Texture colorScheme, Volume volume, float calMin, float calMax, float alpha)
            {
                bool noError = false;
                noError = fill_texture_with_IRMF__MRITextureCutGenerator(_handle, volume.getHandle(), colorScheme.getHandle(), calMin, calMax, alpha) ==1;
                ApplicationState.DLLDebugManager.check_error();

                if (!noError)
                    Debug.LogError("fill_texture_with_IRMF__MRITextureCutGenerator failed ! (check DLL console debug output)");
            }
            /// <summary>
            /// Will reset the octree built with the cut points and sites positions
            /// </summary>
            /// <param name="sites"></param>
            public void InitializeOctree(RawSiteList sites)
            {
                init_octree__MRITextureCutGenerator(_handle, sites.getHandle());
                ApplicationState.DLLDebugManager.check_error();
            }
            /// <summary>
            /// Will compute all the distances between the cut points and the sites positions
            /// </summary>
            /// <param name="maxDistance"></param>
            /// <param name="multiCPU"></param>
            public void ComputeDistances(float maxDistance, bool multiCPU)
            {
                bool noError = false;
                noError = compute_distances__MRITextureCutGenerator(_handle, maxDistance, multiCPU?1:0)==1;
                ApplicationState.DLLDebugManager.check_error();

                if (!noError)
                    Debug.LogError("compute_distances__MRITextureCutGenerator failed ! (check DLL console debug output)");
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="IEEGColumn"></param>
            /// <param name="multiCPU"></param>
            /// <param name="addValues"></param>
            /// <param name="ratioDistances"></param>
            /// <returns></returns>
            public bool ComputeInfluences(Column3DIEEG IEEGColumn, bool multiCPU, bool addValues = false, int ratioDistances = 0)
            {
                bool noError = false;
                noError = compute_influences__MRITextureCutGenerator(_handle, IEEGColumn.IEEGValues, IEEGColumn.EEGDimensions, IEEGColumn.IEEGParameters.InfluenceDistance,
                    multiCPU?1:0, addValues?1:0, ratioDistances, IEEGColumn.IEEGParameters.Middle, IEEGColumn.IEEGParameters.SpanMin, IEEGColumn.IEEGParameters.SpanMax)== 1;
                ApplicationState.DLLDebugManager.check_error();

                if (!noError)
                    Debug.LogError("compute_influences__MRITextureCutGenerator failed ! (check DLL console debug output)");

                return noError;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="IEEGColumn"></param>
            /// <param name="colorScheme"></param>
            /// <param name="notInBrainCol"></param>
            /// <returns></returns>
            public bool FillTextureWithIEEG(Column3DIEEG IEEGColumn, DLL.Texture colorScheme)
            {
                bool noError = false;
                noError = fill_texture_with_SSEG__MRITextureCutGenerator(_handle, IEEGColumn.Timeline.CurrentIndex, colorScheme.getHandle(), 
                IEEGColumn.IEEGParameters.AlphaMin, IEEGColumn.IEEGParameters.AlphaMax, new float[] { 0, 0, 0 })==1;
                ApplicationState.DLLDebugManager.check_error();

                if (!noError)
                    Debug.LogError("fill_texture_with_SSEG__MRITextureCutGenerator failed ! (check DLL console debug output)");

                return noError;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="texture"></param>
            public void UpdateTexture(DLL.Texture texture)
            {
                update_texture__MRITextureCutGenerator(_handle, texture.getHandle());
                texture.UpdateSizes();
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="texture"></param>
            public void UpdateTextureWithIEEG(DLL.Texture texture)
            {
                update_texture_with_SEEG__MRITextureCutGenerator(_handle, texture.getHandle());
                texture.UpdateSizes();
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="texture"></param>
            public void UpdateTextureWithFMRI(DLL.Texture texture)
            {
                update_texture_with_IRMF__MRITextureCutGenerator(_handle, texture.getHandle());
                texture.UpdateSizes();
            }
            /// <summary>
            /// 
            /// </summary>
            public void AdjustInfluencesToColormap(Column3DIEEG column3DIEEG)
            {
                ajust_influences_to_colormap__MRITextureCutGenerator(_handle, column3DIEEG.IEEGParameters.Middle, column3DIEEG.IEEGParameters.SpanMin, column3DIEEG.IEEGParameters.SpanMax);
            }
            /// <summary>
            /// Upathe the max density and influences values
            /// </summary>
            /// <param name="sharedMaxDensity"></param>
            /// <param name="sharedMinInf"></param>
            /// <param name="sharedMaxInf"></param>
            public void SynchronizeWithOthersGenerators(float sharedMaxDensity, float sharedMinInf, float sharedMaxInf)
            {
                synchronize_with_others_generators__MRITextureCutGenerator(_handle, sharedMaxDensity, sharedMinInf, sharedMaxInf);
            }
            /// <summary>
            /// Set the MRI Volume Generator (for the volumic computation of the iEEG values)
            /// </summary>
            /// <param name="generator"></param>
            public void SetMRIVolumeGenerator(MRIVolumeGenerator generator)
            {
                set_MRI_volume_generator__MRITextureCutGenerator(_handle, generator.getHandle());
            }
            #endregion

            #region Memory Management
            /// <summary>
            /// Allocate DLL memory
            /// </summary>
            protected override void create_DLL_class()
            {
                _handle = new HandleRef(this, create__MRITextureCutGenerator());
            }
            /// <summary>
            /// Clean DLL memory
            /// </summary>
            protected override void delete_DLL_class()
            {
                delete__MRITextureCutGenerator(_handle);
            }
            /// <summary>
            /// CutTextureGenerator default constructor
            /// </summary>
            public MRITextureCutGenerator() : base() { }
            #endregion

            #region DLLImport
        
            [DllImport("hbp_export", EntryPoint = "create__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr create__MRITextureCutGenerator();
            [DllImport("hbp_export", EntryPoint = "delete__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void delete__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator);
            [DllImport("hbp_export", EntryPoint = "reset__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void reset__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleMRIGeometryCutGenerator);
            [DllImport("hbp_export", EntryPoint = "fill_texture_with_volume__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void fill_texture_with_volume__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleVolume, HandleRef handleColorSchemeTexture,
                                                                float calMin, float calMax);
            [DllImport("hbp_export", EntryPoint = "fill_texture_with_IRMF__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern int fill_texture_with_IRMF__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleVolume, HandleRef handleColorSchemeTexture,
                                                              float calMin, float calMax, float alpha);
            [DllImport("hbp_export", EntryPoint = "init_octree__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void init_octree__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleRawPlotList);
            [DllImport("hbp_export", EntryPoint = "compute_distances__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern int compute_distances__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, float maxDistance, int multiCPU);
            [DllImport("hbp_export", EntryPoint = "compute_influences__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern int compute_influences__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, float[] timelineAmplitudes, int[] dimensions,
                                                float maxDistance, int multiCPU, int addValues, int ratioDistances, float middle, float spanMin, float spanMax);
            [DllImport("hbp_export", EntryPoint = "fill_texture_with_SSEG__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern int fill_texture_with_SSEG__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, int idTimeline, HandleRef handleColorSchemeTexture,
                                                                      float alphaMin, float alphaMax, float[] notInBrainColor);
            [DllImport("hbp_export", EntryPoint = "min_inf__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float min_inf__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator);
            [DllImport("hbp_export", EntryPoint = "max_inf__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float max_inf__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator);
            [DllImport("hbp_export", EntryPoint = "max_density__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern float max_density__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator);
            [DllImport("hbp_export", EntryPoint = "update_texture__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void update_texture__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleTexture);
            [DllImport("hbp_export", EntryPoint = "update_texture_with_SEEG__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void update_texture_with_SEEG__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleTexture);
            [DllImport("hbp_export", EntryPoint = "update_texture_with_IRMF__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void update_texture_with_IRMF__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleTexture);
            [DllImport("hbp_export", EntryPoint = "ajust_influences_to_colormap__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void ajust_influences_to_colormap__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, float middle, float min, float max);
            [DllImport("hbp_export", EntryPoint = "synchronize_with_others_generators__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void synchronize_with_others_generators__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, float sharedMaxDensity, float sharedMinInf, float sharedMaxInf);
            [DllImport("hbp_export", EntryPoint = "set_MRI_volume_generator__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void set_MRI_volume_generator__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleMRIVolumeGenerator);

            #endregion
        }
    }
}

