using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    /// <summary>
    /// Generator for the geometry of a cut mesh
    /// </summary>
    /// <remarks>
    /// This class is used to compute the geometry of the mesh resulting from a cut
    /// There is one <see cref="MRIGeometryCutGenerator"/> by cut (in the <see cref="Base3DScene"/>)
    /// </remarks>
    public class MRIGeometryCutGenerator : Tools.DLL.CppDLLImportBase
    {
        #region Properties
        /// <summary>
        /// Bounding box of the cut mesh
        /// </summary>
        public BBox BoundingBox
        {
            get
            {
                return new BBox(bounding_box_MRIGeometryCutGenerator(_handle));
            }
        }
        #endregion

        #region Public Methods 
        /// <summary>
        /// Reset the geometry generator with the chosen volume for a cut plane
        /// </summary>
        /// <param name="volume">Currently selected volume in the scene</param>
        /// <param name="cut">Cut plane to consider for this generator</param>
        public void Reset(Volume volume, Cut cut)
        {
            float[] planeF = new float[6];
            for (int ii = 0; ii < 3; ++ii)
            {
                planeF[ii]     = cut.Point[ii];
                planeF[ii + 3] = cut.Normal[ii];
            }

            reset__MRIGeometryCutGenerator(_handle, volume.getHandle(), planeF, (int)cut.Orientation, cut.Flip, 1.0f);
        }
        /// <summary>
        /// Update the UVs of the cut mesh
        /// </summary>
        /// <param name="mesh">Mesh of the cut</param>
        public void UpdateCutMeshUV(Surface mesh)
        {
            update_cut_mesh_UV__MRIGeometryCutGenerator(_handle, mesh.getHandle());
        }
        /// <summary>
        /// Get the horizontal and vertical ratio (between 0 and 1) of a 3D point in the referential of the texture of the cut
        /// </summary>
        /// <param name="point">3D point to get the ratio of</param>
        /// <returns>The ratio on the cut texture</returns>
        public Vector2 GetPositionRatioOnTexture(Vector3 point)
        {
            float[] pointArray = new float[3] { -point.x, point.y, point.z };
            float[] resultArray = new float[2];
            get_position_ratio_on_texture_MRIGeometryCutGenerator(_handle, pointArray, resultArray);
            return new Vector2(resultArray[0], resultArray[1]);
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
        public MRIGeometryCutGenerator() : base() { }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create__MRIGeometryCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create__MRIGeometryCutGenerator();
        [DllImport("hbp_export", EntryPoint = "delete__MRIGeometryCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete__MRIGeometryCutGenerator(HandleRef handleMRIGeometryCutGenerator);
        [DllImport("hbp_export", EntryPoint = "reset__MRIGeometryCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void reset__MRIGeometryCutGenerator(HandleRef handleMRIGeometryCutGenerator, HandleRef handleVolume, float[] plane, int idOrientation, bool flip, float scaleFactor);
        [DllImport("hbp_export", EntryPoint = "update_cut_mesh_UV__MRIGeometryCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_cut_mesh_UV__MRIGeometryCutGenerator(HandleRef handleMRIGeometryCutGenerator, HandleRef handleSurface);
        [DllImport("hbp_export", EntryPoint = "bounding_box_MRIGeometryCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr bounding_box_MRIGeometryCutGenerator(HandleRef handleMRIGeometryCutGenerator);
        [DllImport("hbp_export", EntryPoint = "get_position_ratio_on_texture_MRIGeometryCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr get_position_ratio_on_texture_MRIGeometryCutGenerator(HandleRef handleMRIGeometryCutGenerator, float[] point, float[] result);
        #endregion
    }

    /// <summary>
    /// Generator for the colors and activity on the cut texture
    /// </summary>
    /// <remarks>
    /// This class is used to compute the texture that will be used for the cut mesh
    /// There is one <see cref="MRITextureCutGenerator"/> by cut for each <see cref="Column3D"/>
    /// This class has been replaced with the <see cref="MRIVolumeGenerator"/> for the computation of the activity, but methods have been kept just in case
    /// </remarks>
    public class MRITextureCutGenerator : Tools.DLL.CppDLLImportBase
    {
        #region Properties
        /// <summary>
        /// Maximum site density value
        /// </summary>
        public float MaximumDensity
        {
            get
            {
                return max_density__MRITextureCutGenerator(_handle);
            }
        }
        /// <summary>
        /// Minimum influence value of the sites
        /// </summary>
        public float MinimumInfluence
        {
            get
            {
                return min_inf__MRITextureCutGenerator(_handle);
            }
        }
        /// <summary>
        /// Maximum influence value of the sites
        /// </summary>
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
        /// Reset the generator with the corresponding geometry generator (for the same cut)
        /// </summary>
        /// <param name="geometryGenerator">Geometry generator associated to this generator</param>
        /// <param name="blurFactor">Blur factor of the resulting textures</param>
        public void Reset(MRIGeometryCutGenerator geometryGenerator, int blurFactor)
        {
            reset__MRITextureCutGenerator(_handle, geometryGenerator.getHandle(), blurFactor);
        }
        /// <summary>
        /// Fill the pixels of the cut texture with the closest voxel value using a colormap
        /// </summary>
        /// <param name="volume">Volume used to fill the texture</param>
        /// <param name="colorScheme">Colormap used to paint the texture</param>
        /// <param name="calMin">Minimum calibration value for the volume</param>
        /// <param name="calMax">Maximum calibration value for the volume</param>
        public void FillTextureWithVolume(Volume volume, Texture colorScheme, float calMin, float calMax)
        {
            fill_texture_with_volume__MRITextureCutGenerator(_handle, volume.getHandle(), colorScheme.getHandle(), calMin, calMax);
        }
        /// <summary>
        /// Fill the pixels of the cut texture with the currently selected fMRI
        /// </summary>
        /// <param name="volume">Currently selected volume</param>
        /// <param name="negativeCalMin">Minimum calibration value of the fMRI</param>
        /// <param name="negativeCalMax">Maximum calibration value of the fMRI</param>
        /// <param name="alpha">Alpha value of the fMRI</param>
        public void FillTextureWithFMRI(Volume volume, float negativeCalMin, float negativeCalMax, float positiveCalMin, float positiveCalMax, float alpha)
        {
            bool noError = false;
            noError = fill_texture_with_IRMF__MRITextureCutGenerator(_handle, volume.getHandle(), negativeCalMin, negativeCalMax, positiveCalMin, positiveCalMax, alpha) == 1;

            if (!noError)
                Debug.LogError("fill_texture_with_IRMF__MRITextureCutGenerator failed ! (check DLL console debug output)");
        }
        /// <summary>
        /// Fill the pixels of the cut texture with the JuBrain Atlas
        /// </summary>
        /// <param name="atlas">Reference to the atlas object</param>
        /// <param name="alpha">Alpha of the colors on the cut texture</param>
        /// <param name="selectedArea">Currently selected (hovered with mouse) atlas area</param>
        public void FillTextureWithBrainAtlas(BrainAtlas atlas, float alpha, int selectedArea)
        {
            bool noError = false;
            noError = fill_texture_with_Atlas__MRITextureCutGenerator(_handle, atlas.getHandle(), alpha, selectedArea) == 1;

            if (!noError)
                Debug.LogError("fill_texture_with_Atlas__MRITextureCutGenerator failed ! (check DLL console debug output)");
        }
        /// <summary>
        /// Initialize the octree for the cut mesh
        /// </summary>
        /// <param name="sites">List of the sites on the scene</param>
        public void InitializeOctree(RawSiteList sites)
        {
            init_octree__MRITextureCutGenerator(_handle, sites.getHandle());
        }
        /// <summary>
        /// Compute the distance from each site to each vertex in range (distance less than maximum influence distance set in <see cref="DynamicDataParameters"/>)
        /// </summary>
        /// <param name="maxDistance">Maximum influence distance</param>
        /// <param name="multiCPU">Do we multi-thread this work ?</param>
        /// <returns>True if no error has been encountered</returns>
        public void ComputeDistances(float maxDistance, bool multiCPU)
        {
            bool noError = false;
            noError = compute_distances__MRITextureCutGenerator(_handle, maxDistance, multiCPU?1:0)==1;

            if (!noError)
                Debug.LogError("compute_distances__MRITextureCutGenerator failed ! (check DLL console debug output)");
        }
        /// <summary>
        /// Compute the influence of the sites for each vertex at range of a least one site
        /// </summary>
        /// <param name="dynamicColumn">Parent column of the generator</param>
        /// <param name="multiCPU">Do we multi-thread this work ?</param>
        /// <param name="addValues">Do we just add values instead of using a percentage ?</param>
        /// <param name="siteInfluenceByDistanceType">How to compute the influence taking into account the distance between a vertex and a site</param>
        /// <returns>True if no error has been triggered</returns>
        public bool ComputeInfluences(Column3DDynamic dynamicColumn, bool multiCPU, bool addValues = false, Data.Enums.SiteInfluenceByDistanceType siteInfluenceByDistanceType = 0)
        {
            bool noError = false;
            noError = compute_influences__MRITextureCutGenerator(_handle, dynamicColumn.ActivityValues, dynamicColumn.Timeline.Length, dynamicColumn.Sites.Count, dynamicColumn.DynamicParameters.InfluenceDistance,
                multiCPU?1:0, addValues?1:0, (int)siteInfluenceByDistanceType, dynamicColumn.DynamicParameters.Middle, dynamicColumn.DynamicParameters.SpanMin, dynamicColumn.DynamicParameters.SpanMax)== 1;

            if (!noError)
                Debug.LogError("compute_influences__MRITextureCutGenerator failed ! (check DLL console debug output)");

            return noError;
        }
        /// <summary>
        /// Fill the pixels of the cut texture with the computed activity
        /// </summary>
        /// <param name="dynamicColumn">Parent column of the generator</param>
        /// <param name="colorScheme">Colormap used for the activity</param>
        /// <returns></returns>
        public bool FillTextureWithActivity(Column3DDynamic dynamicColumn, Texture colorScheme)
        {
            bool noError = false;
            noError = fill_texture_with_SSEG__MRITextureCutGenerator(_handle, dynamicColumn.Timeline.CurrentIndex, colorScheme.getHandle(), 
            dynamicColumn.DynamicParameters.AlphaMin, dynamicColumn.DynamicParameters.AlphaMax, new float[] { 0, 0, 0 })==1;

            if (!noError)
                Debug.LogError("fill_texture_with_SSEG__MRITextureCutGenerator failed ! (check DLL console debug output)");

            return noError;
        }
        /// <summary>
        /// Update the DLL texture with the raw one in the generator
        /// </summary>
        /// <param name="texture">Texture to be updated</param>
        public void UpdateTexture(Texture texture)
        {
            update_texture__MRITextureCutGenerator(_handle, texture.getHandle());
        }
        /// <summary>
        /// Update the DLL texture with the activity filled one in the generator
        /// </summary>
        /// <param name="texture">Texture to be updated</param>
        public void UpdateTextureWithActivity(Texture texture)
        {
            update_texture_with_SEEG__MRITextureCutGenerator(_handle, texture.getHandle());
        }
        /// <summary>
        /// Update the DLL texture with the fMRI filled one in the generator
        /// </summary>
        /// <param name="texture">Texture to be updated</param>
        public void UpdateTextureWithFMRI(Texture texture)
        {
            update_texture_with_IRMF__MRITextureCutGenerator(_handle, texture.getHandle());
        }
        /// <summary>
        /// Update the DLL texture with the atlas filled one in the generator
        /// </summary>
        /// <param name="texture">Texture to be updated</param>
        public void UpdateTextureWithAtlas(Texture texture)
        {
            update_texture_with_Atlas__MRITextureCutGenerator(_handle, texture.getHandle());
        }
        /// <summary>
        /// Transform the influence values to a ratio between 0 and 1 to match the activity colormap
        /// </summary>
        /// <param name="dynamicColumn">Parent column of the generator</param>
        public void AdjustInfluencesToColormap(Column3DDynamic dynamicColumn)
        {
            ajust_influences_to_colormap__MRITextureCutGenerator(_handle, dynamicColumn.DynamicParameters.Middle, dynamicColumn.DynamicParameters.SpanMin, dynamicColumn.DynamicParameters.SpanMax);
        }
        /// <summary>
        /// Synchronize the generators of the columns to the same extreme values
        /// </summary>
        /// <param name="sharedMaxDensity">Maximum site density of a column</param>
        /// <param name="sharedMinInf">Minimum influence value of sites of a column</param>
        /// <param name="sharedMaxInf">Maximum influence value of sites of a column</param>
        public void SynchronizeWithOthersGenerators(float sharedMaxDensity, float sharedMinInf, float sharedMaxInf)
        {
            synchronize_with_others_generators__MRITextureCutGenerator(_handle, sharedMaxDensity, sharedMinInf, sharedMaxInf);
        }
        /// <summary>
        /// Set the MRI Volume Generator (for the volumic computation of the activity values)
        /// </summary>
        /// <param name="generator">Volume generator to use</param>
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
        public MRITextureCutGenerator() : base() { }
        #endregion

        #region DLLImport
        [DllImport("hbp_export", EntryPoint = "create__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create__MRITextureCutGenerator();
        [DllImport("hbp_export", EntryPoint = "delete__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator);
        [DllImport("hbp_export", EntryPoint = "reset__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void reset__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleMRIGeometryCutGenerator, int blurFactor);
        [DllImport("hbp_export", EntryPoint = "fill_texture_with_volume__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void fill_texture_with_volume__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleVolume, HandleRef handleColorSchemeTexture,float calMin, float calMax);
        [DllImport("hbp_export", EntryPoint = "fill_texture_with_IRMF__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern int fill_texture_with_IRMF__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleVolume, float negativeCalMin, float negativeCalMax, float positiveCalMin, float positiveCalMax, float alpha);
        [DllImport("hbp_export", EntryPoint = "fill_texture_with_Atlas__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern int fill_texture_with_Atlas__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleAtlas, float alpha, int selectedArea);
        [DllImport("hbp_export", EntryPoint = "init_octree__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void init_octree__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleRawPlotList);
        [DllImport("hbp_export", EntryPoint = "compute_distances__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern int compute_distances__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, float maxDistance, int multiCPU);
        [DllImport("hbp_export", EntryPoint = "compute_influences__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern int compute_influences__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, float[] timelineAmplitudes, int timelineLength, int sitesNumber,float maxDistance, int multiCPU, int addValues, int ratioDistances, float middle, float spanMin, float spanMax);
        [DllImport("hbp_export", EntryPoint = "fill_texture_with_SSEG__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern int fill_texture_with_SSEG__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, int idTimeline, HandleRef handleColorSchemeTexture, float alphaMin, float alphaMax, float[] notInBrainColor);
        [DllImport("hbp_export", EntryPoint = "fill_texture_with_Density__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern int fill_texture_with_Density__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, int idTimeline, HandleRef handleColorSchemeTexture, float[] notInBrainColor);
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
        [DllImport("hbp_export", EntryPoint = "update_texture_with_Atlas__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void update_texture_with_Atlas__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleTexture);
        [DllImport("hbp_export", EntryPoint = "ajust_influences_to_colormap__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void ajust_influences_to_colormap__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, float middle, float min, float max);
        [DllImport("hbp_export", EntryPoint = "synchronize_with_others_generators__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void synchronize_with_others_generators__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, float sharedMaxDensity, float sharedMinInf, float sharedMaxInf);
        [DllImport("hbp_export", EntryPoint = "set_MRI_volume_generator__MRITextureCutGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void set_MRI_volume_generator__MRITextureCutGenerator(HandleRef handleMRITextureCutGenerator, HandleRef handleMRIVolumeGenerator);
        #endregion
    }
}

