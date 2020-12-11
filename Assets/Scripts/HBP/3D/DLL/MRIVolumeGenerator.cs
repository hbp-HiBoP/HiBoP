using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    /// <summary>
    /// Generator for the activity for the whole volume
    /// </summary>
    /// <remarks>
    /// This class is used to compute the texture that will be used for the cut mesh but in a volumic way
    /// There is one <see cref="MRIVolumeGenerator"/> for each <see cref="Column3DDynamic"/>
    /// This class replaced the <see cref="MRITextureCutGenerator"/> for the computation of the activity
    /// </remarks>
    public class MRIVolumeGenerator : Tools.DLL.CppDLLImportBase, ICloneable
    {
        #region Properties
        /// <summary>
        /// Maximum site density value
        /// </summary>
        public float MaximumDensity
        {
            get
            {
                return getMaximumDensity_MRIVolumeGenerator(_handle);
            }
        }
        /// <summary>
        /// Minimum influence value of the sites
        /// </summary>
        public float MinimumInfluence
        {
            get
            {
                return getMinInf_MRIVolumeGenerator(_handle);
            }
        }
        /// <summary>
        /// Maximum influence value of the sites
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
        /// Reset all the volume generators at once using multi-threading for faster operations
        /// </summary>
        /// <param name="generators">Array of generators to be reseted</param>
        /// <param name="volume">Volume to be used for the generators</param>
        /// <param name="dimension">Size of the resulting volume</param>
        public static void ResetAll(MRIVolumeGenerator[] generators, Volume volume, int dimension)
        {
            reset_all_MRIVolumeGenerator(generators.Select(g => g.getHandle()).ToArray(), generators.Length, volume.getHandle(), dimension);
        }
        /// <summary>
        /// Reset the generator with a volume and given a size
        /// </summary>
        /// <param name="volume">Volume to be used for the generator</param>
        /// <param name="dimension">Size of the resulting volume</param>
        public void Reset(Volume volume, int dimension)
        {
            reset_MRIVolumeGenerator(_handle, volume.getHandle(), dimension);
        }
        /// <summary>
        /// Initialize the octree for the cut mesh
        /// </summary>
        /// <param name="sites">List of the sites on the scene</param>
        public void InitializeOctree(RawSiteList rawPlotList)
        {
            initOctree_MRIVolumeGenerator(_handle, rawPlotList.getHandle());
        }
        /// <summary>
        /// Compute the distance from each site to each vertex in range (distance less than maximum influence distance set in <see cref="DynamicDataParameters"/>)
        /// </summary>
        /// <param name="maxDistance">Maximum influence distance</param>
        /// <param name="multiCPU">Do we multi-thread this work ?</param>
        /// <returns>True if no error has been encountered</returns>
        public bool ComputeDistances(float maxDistance, bool multiCPU)
        {
            bool noError = false;
            noError = computeDistances_MRIVolumeGenerator(_handle, maxDistance, multiCPU ? 1 : 0) == 1;

            if (!noError)
                Debug.LogError("computeDistances_BrainSurfaceTextureGenerator failed ! (check DLL console debug output)");

            return noError;
        }
        /// <summary>
        /// Synchronize the generators of the columns to the same extreme values
        /// </summary>
        /// <param name="sharedMaxDensity">Maximum site density of a column</param>
        /// <param name="sharedMinInf">Minimum influence value of sites of a column</param>
        /// <param name="sharedMaxInf">Maximum influence value of sites of a column</param>
        public void SynchronizeWithOthersGenerators(float sharedMaxDensity, float sharedMinInf, float sharedMaxInf)
        {
            synchronizeWithOthersGenerators_MRIVolumeGenerator(_handle, sharedMaxDensity, sharedMinInf, sharedMaxInf);
        }
        /// <summary>
        /// Compute the influence of the sites for each vertex at range of a least one site
        /// </summary>
        /// <param name="dynamicColumn">Parent column of the generator</param>
        /// <param name="multiCPU">Do we multi-thread this work ?</param>
        /// <param name="addValues">Do we just add values instead of using a percentage ?</param>
        /// <param name="siteInfluenceByDistanceType">How to compute the influence taking into account the distance between a vertex and a site</param>
        /// <returns>True if no error has been triggered</returns>
        public bool ComputeInfluences(Column3DDynamic dynamicColumn, bool multiCPU, bool addValues = false, int ratioDistances = 0)
        {
            bool noError = false;
            noError = computeInfluences_MRIVolumeGenerator(_handle, dynamicColumn.ActivityValues, dynamicColumn.Timeline.Length, dynamicColumn.RawElectrodes.NumberOfSites, dynamicColumn.DynamicParameters.InfluenceDistance, multiCPU ? 1 : 0, addValues ? 1 : 0, ratioDistances,
                dynamicColumn.DynamicParameters.Middle, dynamicColumn.DynamicParameters.SpanMin, dynamicColumn.DynamicParameters.SpanMax) == 1;

            if (!noError)
                Debug.LogError("computeInfluences_BrainSurfaceTextureGenerator failed ! (check DLL console debug output)");

            return noError;
        }
        /// <summary>
        /// Compute the influence using mars atlas
        /// </summary>
        /// <param name="ccepColumn">Parent column of the generator</param>
        /// <returns>True if no error has been triggered</returns>
        public bool ComputeInfluencesWithAtlas(Column3DCCEP ccepColumn)
        {
            return compute_influences_with_atlas_MRIVolumeGenerator(_handle, ccepColumn.ActivityValues, ccepColumn.Timeline.Length, ccepColumn.AreaMask, ApplicationState.Module3D.MarsAtlas.getHandle()) == 1;
        }
        /// <summary>
        /// Compute activity with the input FMRI
        /// </summary>
        /// <param name="volume">Input FMRI</param>
        public bool ComputeFMRIActivity(Volume volume)
        {
            return computeActivityWithFMRI_MRIVolumeGenerator(_handle, volume.getHandle()) == 1;
        }
        /// <summary>
        /// Transform the influence values to a ratio between 0 and 1 to match the activity colormap
        /// </summary>
        /// <param name="dynamicColumn">Parent column of the generator</param>
        public void AdjustInfluencesToColormap(Column3DDynamic dynamicColumn)
        {
            ajustInfluencesToColormapIEEG_MRIVolumeGenerator(_handle, dynamicColumn.DynamicParameters.Middle, dynamicColumn.DynamicParameters.SpanMin, dynamicColumn.DynamicParameters.SpanMax);
        }
        /// <summary>
        /// Transform the influence values to a ratio between 0 and 1 to match the activity colormap
        /// </summary>
        /// <param name="fmriColumn">Parent column of the generator</param>
        public void AdjustInfluencesToColormap(Column3DFMRI fmriColumn)
        {
            ajustInfluencesToColormapFMRI_MRIVolumeGenerator(_handle, fmriColumn.FMRIParameters.FMRINegativeCalMinFactor, fmriColumn.FMRIParameters.FMRINegativeCalMaxFactor, fmriColumn.FMRIParameters.FMRIPositiveCalMinFactor, fmriColumn.FMRIParameters.FMRIPositiveCalMaxFactor);
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
        public MRIVolumeGenerator() : base() { }
        public MRIVolumeGenerator(MRIVolumeGenerator other) : base(clone_MRIVolumeGenerator(other.getHandle())) { }
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
        [DllImport("hbp_export", EntryPoint = "reset_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void reset_MRIVolumeGenerator(HandleRef handleBrainVolumeTextureGenerator, HandleRef handleSurface, int dimension);
        [DllImport("hbp_export", EntryPoint = "reset_all_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void reset_all_MRIVolumeGenerator(HandleRef[] handleBrainVolumeTextureGenerator, int count, HandleRef handleSurface, int dimension);
        [DllImport("hbp_export", EntryPoint = "initOctree_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initOctree_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator, HandleRef handleRawPlotList);
        [DllImport("hbp_export", EntryPoint = "computeDistances_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern int computeDistances_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator, float maxDistance, int multiCPU);
        [DllImport("hbp_export", EntryPoint = "computeInfluences_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern int computeInfluences_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator, float[] timelineAmplitudes, int timelineLength, int sitesNumber, float maxDistance, int multiCPU, int addValues, int ratioDistances, float middle, float spanMin, float spanMax);
        [DllImport("hbp_export", EntryPoint = "compute_influences_with_atlas_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern int compute_influences_with_atlas_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator, float[] activity, int timelineLength, int[] mask, HandleRef marsAtlasHandle);
        [DllImport("hbp_export", EntryPoint = "computeActivityWithFMRI_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern int computeActivityWithFMRI_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator, HandleRef volumeHandle);
        [DllImport("hbp_export", EntryPoint = "ajustInfluencesToColormapIEEG_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void ajustInfluencesToColormapIEEG_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator, float middle, float min, float max);
        [DllImport("hbp_export", EntryPoint = "ajustInfluencesToColormapFMRI_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void ajustInfluencesToColormapFMRI_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator, float negativeMin, float negativeMax, float positiveMin, float positiveMax);
        [DllImport("hbp_export", EntryPoint = "synchronizeWithOthersGenerators_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void synchronizeWithOthersGenerators_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator, float sharedMaxDensity, float sharedMinInf, float sharedMaxInf);                      
        [DllImport("hbp_export", EntryPoint = "getMaximumDensity_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern float getMaximumDensity_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator);
        [DllImport("hbp_export", EntryPoint = "getMinInf_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern float getMinInf_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator);
        [DllImport("hbp_export", EntryPoint = "getMaxInf_MRIVolumeGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern float getMaxInf_MRIVolumeGenerator(HandleRef handleBrainSurfaceTextureGenerator);
        #endregion
    }
}