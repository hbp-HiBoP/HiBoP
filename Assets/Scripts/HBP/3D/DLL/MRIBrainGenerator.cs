using System;
using System.Runtime.InteropServices;
using Tools.CSharp;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    /// <summary>
    /// Generator for the activity on the brain surface
    /// </summary>
    /// <remarks>
    /// This class is used to compute the UVs of the brain for the colormap texture in order to display the activity of the sites on the surface of the brain
    /// There is one <see cref="MRIBrainGenerator"/> by <see cref="Column3DDynamic"/>
    /// </remarks>
    public class MRIBrainGenerator : Tools.DLL.CppDLLImportBase, ICloneable
    {
        #region Properties
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
        /// Maximum site density value
        /// </summary>
        public float MaximumDensity
        {
            get
            {
                return getMaximumDensity_BrainSurfaceTextureGenerator(_handle);
            }
        }
        /// <summary>
        /// Minimum influence value of the sites
        /// </summary>
        public float MinimumInfluence
        {
            get
            {
                return getMinInf_BrainSurfaceTextureGenerator(_handle);
            }
        }
        /// <summary>
        /// Maximum influence value of the sites
        /// </summary>
        public float MaximumInfluence
        {
            get
            {
                return getMaxInf_BrainSurfaceTextureGenerator(_handle);
            }
        }

        /// <summary>
        /// Pointer to an array of floats containing the activity UVs
        /// </summary>
        private GCHandle m_UVAmplitudesHandle;
        /// <summary>
        /// Pointer to an array of floats containing the alpha UVs
        /// </summary>
        private GCHandle m_UVAlphaHandle;
        #endregion

        #region Public Methods
        /// <summary>
        /// Reset the generator with the chosen surface and volume
        /// </summary>
        /// <param name="surface">Surface currently selected in the scene</param>
        /// <param name="volume">Volume currently selected in the scene</param>
        public void Reset(Surface surface, Volume volume)
        {
            reset_BrainSurfaceTextureGenerator(_handle, surface.getHandle(), volume.getHandle());
        }
        /// <summary>
        /// Initialize the octree of the surface to make further computations faster
        /// </summary>
        /// <param name="rawSiteList">Raw list of the sites in the scene</param>
        public void InitializeOctree(RawSiteList rawSiteList)
        {
            initOctree_BrainSurfaceTextureGenerator(_handle, rawSiteList.getHandle());
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
            noError = computeDistances_BrainSurfaceTextureGenerator( _handle, maxDistance, multiCPU ? 1 : 0) == 1;

            if (!noError)
                Debug.LogError("computeDistances_BrainSurfaceTextureGenerator failed ! (check DLL console debug output)");

            return noError;
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
            noError = computeInfluences_BrainSurfaceTextureGenerator(_handle, dynamicColumn.ActivityValues, dynamicColumn.Timeline.Length, dynamicColumn.RawElectrodes.NumberOfSites, dynamicColumn.DynamicParameters.InfluenceDistance, multiCPU ? 1 : 0, addValues ? 1 : 0, (int)siteInfluenceByDistanceType,
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
            return compute_influences_with_atlas_BrainSurfaceTextureGenerator(_handle, ccepColumn.ActivityValues, ccepColumn.Timeline.Length, ccepColumn.AreaMask, ApplicationState.Module3D.MarsAtlas.getHandle()) == 1;
        }
        /// <summary>
        /// Synchronize the generators of the columns to the same extreme values
        /// </summary>
        /// <param name="sharedMaxDensity">Maximum site density of a column</param>
        /// <param name="sharedMinInf">Minimum influence value of sites of a column</param>
        /// <param name="sharedMaxInf">Maximum influence value of sites of a column</param>
        public void SynchronizeWithOthersGenerators(float sharedMaxDensity, float sharedMinInf, float sharedMaxInf)
        {
            synchronizeWithOthersGenerators_BrainSurfaceTextureGenerator(_handle, sharedMaxDensity, sharedMinInf, sharedMaxInf);
        }
        /// <summary>
        /// Transform the influence values to a ratio between 0 and 1 to match the activity colormap
        /// </summary>
        /// <param name="dynamicColumn">Parent column of the generator</param>
        public void AdjustInfluencesToColormap(Column3DDynamic dynamicColumn)
        {
            ajustInfluencesToColormap_BrainSurfaceTextureGenerator( _handle, dynamicColumn.DynamicParameters.Middle, dynamicColumn.DynamicParameters.SpanMin, dynamicColumn.DynamicParameters.SpanMax);
        }
        /// <summary>
        /// Compute the main UVs of the surface depending of the value of the closest voxel of each vertex in the volume
        /// </summary>
        /// <param name="surface">Surface to compute the UVs of</param>
        /// <param name="volume">Volume to compute the UVs with</param>
        /// <param name="calMin">Minimum calibration value for the volume</param>
        /// <param name="calMax">Maximum calibration value for the volume</param>
        public void ComputeMainUVWithVolume(Surface surface, Volume volume, float calMin, float calMax)
        {
            compute_UVMain_with_volume_BrainSurfaceTextureGenerator(_handle, volume.getHandle(), surface.getHandle(), calMin, calMax);// calMin, calMax);
        }
        /// <summary>
        /// Compute the UVs for the alpha and the activity of the surface
        /// </summary>
        /// <param name="surface">Surface to compute the UVs for</param>
        /// <param name="dynamicColumn">Parent column of the generator</param>
        /// <returns></returns>
        public bool ComputeSurfaceActivityUV(Surface surface, Column3DDynamic dynamicColumn)
        {                
            bool noError = false;
            noError = computeSurfaceTextCoordAmplitudes_BrainSurfaceTextureGenerator( _handle, surface.getHandle(), dynamicColumn.Timeline.CurrentIndex, dynamicColumn.DynamicParameters.AlphaMin, dynamicColumn.DynamicParameters.AlphaMax) == 1;

            int m_nbVertices = surface.NumberOfVertices;
            if (m_nbVertices == 0) // mesh is empty
                return true;

            // amplitudes
            if (ActivityUV.Length != m_nbVertices)
            {
                ActivityUV = new Vector2[m_nbVertices];
                if (m_UVAmplitudesHandle.IsAllocated) m_UVAmplitudesHandle.Free();
                m_UVAmplitudesHandle = GCHandle.Alloc(ActivityUV, GCHandleType.Pinned); 
            }
            updateUVAmplitudes_BrainSurfaceTextureGenerator(_handle, m_UVAmplitudesHandle.AddrOfPinnedObject());

            // alpha
            if (AlphaUV.Length != m_nbVertices)
            {
                AlphaUV = new Vector2[m_nbVertices];
                if (m_UVAlphaHandle.IsAllocated) m_UVAlphaHandle.Free();
                m_UVAlphaHandle = GCHandle.Alloc(AlphaUV, GCHandleType.Pinned);
            }
            updateUVAlpha_BrainSurfaceTextureGenerator(_handle, m_UVAlphaHandle.AddrOfPinnedObject());

            if (!noError)
                Debug.LogError("computeSurfaceTextCoordAmplitudes_BrainSurfaceTextureGenerator failed ! (check DLL console debug output)");

            return noError;
        }
        /// <summary>
        /// Compute the UVs when no activity is computed for the surface
        /// </summary>
        /// <param name="surface">Surface to compute the UVs for</param>
        public void ComputeNullUV(Surface surface)
        {
            NullUV = new Vector2[surface.NumberOfVertices];
            NullUV.Fill(new Vector2(0.01f, 1f));
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
        public MRIBrainGenerator() : base() { }
        public MRIBrainGenerator(MRIBrainGenerator other) : base(clone_BrainSurfaceTextureGenerator(other.getHandle())) { }
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
        [DllImport("hbp_export", EntryPoint = "create_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr create_BrainSurfaceTextureGenerator();
        [DllImport("hbp_export", EntryPoint = "clone_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern IntPtr clone_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator);
        [DllImport("hbp_export", EntryPoint = "delete_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void delete_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator);
        [DllImport("hbp_export", EntryPoint = "reset_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void reset_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, HandleRef handleSurface, HandleRef handleVolume);
        [DllImport("hbp_export", EntryPoint = "initOctree_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void initOctree_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, HandleRef handleRawPlotList);
        [DllImport("hbp_export", EntryPoint = "computeDistances_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern int computeDistances_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, float maxDistance, int multiCPU);
        [DllImport("hbp_export", EntryPoint = "computeInfluences_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern int computeInfluences_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, float[] timelineAmplitudes, int timelineLength, int sitesNumber, float maxDistance, int multiCPU, int addValues, int ratioDistances, float middle, float spanMin, float spanMax);
        [DllImport("hbp_export", EntryPoint = "compute_influences_with_atlas_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern int compute_influences_with_atlas_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, float[] activity, int timelineLength, int[] mask, HandleRef marsAtlasHandle);
        [DllImport("hbp_export", EntryPoint = "ajustInfluencesToColormap_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void ajustInfluencesToColormap_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, float middle, float min, float max);
        [DllImport("hbp_export", EntryPoint = "synchronizeWithOthersGenerators_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void synchronizeWithOthersGenerators_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, float sharedMaxDensity, float sharedMinInf, float sharedMaxInf);
        [DllImport("hbp_export", EntryPoint = "compute_UVMain_with_volume_BrainSurfaceTextureGenerator", CallingConvention = CallingConvention.Cdecl)]
        static private extern void compute_UVMain_with_volume_BrainSurfaceTextureGenerator(HandleRef handleBrainSurfaceTextureGenerator, HandleRef handleVolume, HandleRef handleSurface, float calMin, float calMax);
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