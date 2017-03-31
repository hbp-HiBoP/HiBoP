

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

namespace HBP.VISU3D
{
    namespace DLL
    {
        /// <summary>
        /// Geometry generator of a MRI cut, one generator is defined for all columns, all columns share the same geometry
        /// </summary>
        public class MRIGeometryCutGenerator : CppDLLImportBase
        {
            #region functions 

            public void reset(DLL.Volume volume, Plane plane)
            {
                float[] planeF = new float[6];
                for (int ii = 0; ii < 3; ++ii)
                {
                    planeF[ii]     = plane.point[ii];
                    planeF[ii + 3] = plane.normal[ii];
                }

                reset__MRIGeometryCutGenerator(_handle, volume.getHandle(), planeF);
                StaticComponents.DLLDebugManager.check_error();
            }

            /// <summary>
            /// Update the UV of the input mesh
            /// </summary>
            /// <param name="mesh"></param>
            public void update_cut_mesh_UV(DLL.Surface mesh)
            {
                update_cut_mesh_UV__MRIGeometryCutGenerator(_handle, mesh.getHandle());
                StaticComponents.DLLDebugManager.check_error();
            }

            #endregion functions

            #region memory_management

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

            #endregion memory_management

            #region DLLImport

            [DllImport("hbp_export", EntryPoint = "create__MRIGeometryCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern IntPtr create__MRIGeometryCutGenerator();

            [DllImport("hbp_export", EntryPoint = "delete__MRIGeometryCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void delete__MRIGeometryCutGenerator(HandleRef handleMRIGeometryCutGenerator);

            [DllImport("hbp_export", EntryPoint = "reset__MRIGeometryCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void reset__MRIGeometryCutGenerator(HandleRef handleMRIGeometryCutGenerator, HandleRef handleVolume, float[] plane);

            [DllImport("hbp_export", EntryPoint = "update_cut_mesh_UV__MRIGeometryCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void update_cut_mesh_UV__MRIGeometryCutGenerator(HandleRef handleMRIGeometryCutGenerator, HandleRef handleSurface);


            #endregion DLLImport
        }

        /// <summary>
        /// Texture generator for MRI cuts, or each column a generator is defined, all columns have differents textures
        /// </summary>
        public class MRITextureCutGenerator : CppDLLImportBase
        {
            #region functions

            public void reset(MRIGeometryCutGenerator geometryGenerator)
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
            public void fill_texture_with_volume(DLL.Volume volume, DLL.Texture colorScheme, float calMin, float calMax)
            {
                fill_texture_with_volume__MRITextureCutGenerator(_handle, volume.getHandle(), colorScheme.getHandle(), calMin, calMax);
                StaticComponents.DLLDebugManager.check_error();
            }

            /// <summary>
            /// Will update the previously MRI colored texture with a fMRI
            /// </summary>
            /// <param name="FMRIColumn"></param>
            /// <param name="volume"></param>
            public void fill_texture_with_FMRI(Column3DViewFMRI FMRIColumn, DLL.Volume volume)
            {
                bool noError = false;
                noError = fill_texture_with_IRMF__MRITextureCutGenerator(_handle, volume.getHandle(), FMRIColumn.DLLCutFMRIColorScheme.getHandle(), FMRIColumn.calMin, FMRIColumn.calMax, FMRIColumn.alpha) ==1;
                StaticComponents.DLLDebugManager.check_error();

                if (!noError)
                    Debug.LogError("fill_texture_with_IRMF__MRITextureCutGenerator failed ! (check DLL console debug output)");
            }

            /// <summary>
            /// Will reset the octree built with the cut points and sites positions
            /// </summary>
            /// <param name="sites"></param>
            public void init_octree(RawSiteList sites)
            {
                init_octree__MRITextureCutGenerator(_handle, sites.getHandle());
                StaticComponents.DLLDebugManager.check_error();
            }

            /// <summary>
            /// Will compute all the distances between the cut points and the sites positions
            /// </summary>
            /// <param name="maxDistance"></param>
            /// <param name="multiCPU"></param>
            public void compute_distances(float maxDistance, bool multiCPU)
            {
                bool noError = false;
                noError = compute_distances__MRITextureCutGenerator(_handle, maxDistance, multiCPU?1:0)==1;
                StaticComponents.DLLDebugManager.check_error();

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
            public bool compute_influences(Column3DViewIEEG IEEGColumn, bool multiCPU, bool addValues = false, bool ratioDistances = false)
            {
                bool noError = false;
                noError = compute_influences__MRITextureCutGenerator(_handle, IEEGColumn.iEEG_values(), IEEGColumn.dimensions(), IEEGColumn.maxDistanceElec,
                    multiCPU?1:0, addValues?1:0, ratioDistances?1:0, IEEGColumn.middle, IEEGColumn.spanMin, IEEGColumn.spanMax)== 1;
                StaticComponents.DLLDebugManager.check_error();

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
            public bool fill_texture_with_IEEG(Column3DViewIEEG IEEGColumn, DLL.Texture colorScheme, Color notInBrainCol)
            {
                bool noError = false;
                float[] notInBrainColor = new float[3];
                notInBrainColor[0] = notInBrainCol.b;
                notInBrainColor[1] = notInBrainCol.r;
                notInBrainColor[2] = notInBrainCol.r;
                noError = fill_texture_with_SSEG__MRITextureCutGenerator(_handle, IEEGColumn.currentTimeLineID, colorScheme.getHandle(), 
                IEEGColumn.alphaMin, IEEGColumn.alphaMax, notInBrainColor)==1;
                StaticComponents.DLLDebugManager.check_error();

                if (!noError)
                    Debug.LogError("fill_texture_with_SSEG__MRITextureCutGenerator failed ! (check DLL console debug output)");

                return noError;
            }

            public void update_texture(DLL.Texture texture)
            {
                update_texture__MRITextureCutGenerator(_handle, texture.getHandle());
                texture.update_sizes();
            }
            
            public void update_texture_with_IEEG(DLL.Texture texture)
            {
                update_texture_with_SEEG__MRITextureCutGenerator(_handle, texture.getHandle());
                texture.update_sizes();
            }

            public void update_texture_with_FMRI(DLL.Texture texture)
            {
                update_texture_with_IRMF__MRITextureCutGenerator(_handle, texture.getHandle());
                texture.update_sizes();
            }

            public void ajust_influences_to_colormap()
            {
                ajust_influences_to_colormap__MRITextureCutGenerator(_handle);
            }

            /// <summary>
            /// Upathe the max density and influences values
            /// </summary>
            /// <param name="sharedMaxDensity"></param>
            /// <param name="sharedMinInf"></param>
            /// <param name="sharedMaxInf"></param>
            public void synchronize_with_others_generators(float sharedMaxDensity, float sharedMinInf, float sharedMaxInf)
            {
                synchronize_with_others_generators__MRITextureCutGenerator(_handle, sharedMaxDensity, sharedMinInf, sharedMaxInf);
            }

            public float maximum_density()
            {
                return max_density__MRITextureCutGenerator(_handle);
            }

            public float minimum_influence()
            {
                return min_inf__MRITextureCutGenerator(_handle);
            }

            public float maximum_influence()
            {
                return max_inf__MRITextureCutGenerator(_handle);
            }

            #endregion functions

            #region memory_management

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

            #endregion memory_management

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
            static private extern void ajust_influences_to_colormap__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator);
            [DllImport("hbp_export", EntryPoint = "synchronize_with_others_generators__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
            static private extern void synchronize_with_others_generators__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, float sharedMaxDensity, float sharedMinInf, float sharedMaxInf);

            #endregion DLLImport
        }
    }
}

