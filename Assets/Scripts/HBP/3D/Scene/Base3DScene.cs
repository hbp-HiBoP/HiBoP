using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using CielaSpike;
using HBP.Data.Visualization;
using Tools.Unity;

namespace HBP.Module3D
{
    /// <summary>
    /// Class containing all the data concerning the gameObjects, the DLL objects and the parameters of the 3D scene
    /// </summary>
    /// <remarks>
    /// This class manages everything concerning the scene.
    /// It manages directly the cuts, the coloring of the meshes and the different parameters.
    /// It also uses other classes to manage meshes, MRIs, implantations, triangle erasing, atlases, fMRIs and displayed gameObjects.
    /// <seealso cref="MeshManager"/> <seealso cref="MRIManager"/> <seealso cref="ImplantationManager"/> <seealso cref="TriangleEraser"/> <seealso cref="AtlasManager"/> <seealso cref="FMRIManager"/> <seealso cref="DisplayedObjects"/>
    /// </remarks>
    public class Base3DScene : MonoBehaviour, IConfigurable
    {
        #region Properties
        /// <summary>
        /// Name of the scene
        /// </summary>
        public string Name
        {
            get
            {
                return Visualization.Name;
            }
        }
        /// <summary>
        /// Type of the scene (Single / Multi)
        /// </summary>
        public Data.Enums.SceneType Type
        {
            get
            {
                return Visualization.Patients.Count > 1 ? Data.Enums.SceneType.MultiPatients : Data.Enums.SceneType.SinglePatient;
            }
        }

        /// <summary>
        /// Visualization associated to this scene
        /// </summary>
        public Visualization Visualization { get; private set; }

        private bool m_IsSelected;
        /// <summary>
        /// Is this scene selected ?
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return m_IsSelected;
            }
            set
            {
                if (m_IsSelected != value)
                {
                    m_IsSelected = value;
                    OnSelect.Invoke();
                }
                ApplicationState.Module3D.OnSelectScene.Invoke(this);
            }
        }
        
        /// <summary>
        /// Cuts planes list
        /// </summary>
        public List<Cut> Cuts { get; } = new List<Cut>();

        #region Managers
        [SerializeField] private MeshManager m_MeshManager;
        /// <summary>
        /// Object that handles the meshes of the scene
        /// </summary>
        public MeshManager MeshManager { get { return m_MeshManager; } }
        
        [SerializeField] private MRIManager m_MRIManager;
        /// <summary>
        /// Object that handles the MRIs of the scene
        /// </summary>
        public MRIManager MRIManager { get { return m_MRIManager; } }

        [SerializeField] private ImplantationManager m_ImplantationManager;
        /// <summary>
        /// Object that handles the implantations of the scene
        /// </summary>
        public ImplantationManager ImplantationManager { get { return m_ImplantationManager; } }
        
        [SerializeField] private TriangleEraser m_TriangleEraser;
        /// <summary>
        /// Object that handles the erasing of triangles on the selected brain mesh
        /// </summary>
        public TriangleEraser TriangleEraser { get { return m_TriangleEraser; } }

        [SerializeField] private AtlasManager m_AtlasManager;
        /// <summary>
        /// Object that handles the JuBrain and Mars atlases
        /// </summary>
        public AtlasManager AtlasManager { get { return m_AtlasManager; } }

        [SerializeField] private FMRIManager m_FMRIManager;
        /// <summary>
        /// Object that handles the FMRI of the scene
        /// </summary>
        public FMRIManager FMRIManager { get { return m_FMRIManager; } }

        /// <summary>
        /// Displayable objects of the scene
        /// </summary>
        [SerializeField] private DisplayedObjects m_DisplayedObjects;
        #endregion

        /// <summary>
        /// Selected column
        /// </summary>
        public Column3D SelectedColumn
        {
            get
            {
                return Columns.FirstOrDefault((c) => c.IsSelected);
            }
        }
        /// <summary>
        /// Columns of the scene
        /// </summary>
        public List<Column3D> Columns { get; } = new List<Column3D>();
        /// <summary>
        /// Dynamic Columns of the scene
        /// </summary>
        public List<Column3DDynamic> ColumnsDynamic { get { return Columns.OfType<Column3DDynamic>().ToList(); } }
        /// <summary>
        /// IEEG Columns of the scene
        /// </summary>
        public List<Column3DIEEG> ColumnsIEEG { get { return Columns.OfType<Column3DIEEG>().ToList(); } }
        /// <summary>
        /// CCEP Columns of the scene
        /// </summary>
        public List<Column3DCCEP> ColumnsCCEP { get { return Columns.OfType<Column3DCCEP>().ToList(); } }
        /// <summary>
        /// Number of views in any column
        /// </summary>
        public int ViewLineNumber
        {
            get
            {
                if (Columns.Count > 0)
                {
                    return Columns[0].Views.Count;
                }
                return 0;
            }
        }

        /// <summary>
        /// Common generator for each brain part
        /// </summary>
        public List<DLL.MRIBrainGenerator> DLLCommonBrainTextureGeneratorList { get; set; } = new List<DLL.MRIBrainGenerator>();
        /// <summary>
        /// Geometry generator for cuts
        /// </summary>
        public List<DLL.MRIGeometryCutGenerator> DLLMRIGeometryCutGeneratorList { get; set; } = new List<DLL.MRIGeometryCutGenerator>();

        /// <summary>
        /// Material used for the brain mesh
        /// </summary>
        public Material BrainMaterial { get; private set; }
        /// <summary>
        /// Material used for the simplified mesh (not really used now, this is a bit legacy but could be useful)
        /// </summary>
        public Material SimplifiedBrainMaterial { get; private set; }
        /// <summary>
        /// Material used for the cut meshes
        /// </summary>
        public Material CutMaterial { get; private set; }

        private Data.Enums.ColorType m_BrainColor = Data.Enums.ColorType.BrainColor;
        /// <summary>
        /// Brain surface color type (see <see cref="Data.Enums.ColorType"/> for all possible values)
        /// </summary>
        public Data.Enums.ColorType BrainColor
        {
            get
            {
                return m_BrainColor;
            }
            set
            {
                m_BrainColor = value;

                BrainColorTexture = Texture2DExtension.Generate();
                DLL.Texture tex = DLL.Texture.Generate1DColorTexture(value);
                tex.UpdateTexture2D(BrainColorTexture);
                tex.Dispose();

                BrainMaterial.SetTexture("_MainTex", BrainColorTexture);
            }
        }

        private Data.Enums.ColorType m_CutColor = Data.Enums.ColorType.Default;
        /// <summary>
        /// Brain cut color type (see <see cref="Data.Enums.ColorType"/> for all possible values)
        /// </summary>
        public Data.Enums.ColorType CutColor
        {
            get
            {
                return m_CutColor;
            }
            set
            {
                m_CutColor = value;

                ResetColors();

                m_CutsNeedUpdate = true;
                CutTexturesNeedUpdate = true;
                foreach (Column3D column in Columns)
                {
                    column.IsRenderingUpToDate = false;
                }
            }
        }

        private Data.Enums.ColorType m_Colormap = Data.Enums.ColorType.MatLab;
        /// <summary>
        /// Colormap type (see <see cref="Data.Enums.ColorType"/> for all possible values)
        /// </summary>
        public Data.Enums.ColorType Colormap
        {
            get
            {
                return m_Colormap;
            }
            set
            {
                m_Colormap = value;

                BrainColorMapTexture = Texture2DExtension.Generate();
                DLL.Texture tex = DLL.Texture.Generate1DColorTexture(value);
                tex.UpdateTexture2D(BrainColorMapTexture);
                tex.Dispose();

                ResetColors();

                BrainMaterial.SetTexture("_ColorTex", BrainColorMapTexture);

                if (m_IsGeneratorUpToDate)
                {
                    ComputeIEEGTextures();
                    ComputeGUITextures();
                }

                OnChangeColormap.Invoke(value);
            }
        }

        /// <summary>
        /// Colormap Unity texture
        /// </summary>
        public Texture2D BrainColorMapTexture { get; private set; }
        /// <summary>
        /// Brain Unity texture
        /// </summary>
        public Texture2D BrainColorTexture { get; private set; }

        private bool m_CutHolesEnabled = true;
        /// <summary>
        /// Are cut holes in MRI enabled (that means black zones are invisible) ?
        /// </summary>
        public bool CutHolesEnabled
        {
            get
            {
                return m_CutHolesEnabled;
            }
            set
            {
                m_CutHolesEnabled = value;
                m_CutsNeedUpdate = true;
                foreach (Column3D column in Columns)
                {
                    column.IsRenderingUpToDate = false;
                }
            }
        }

        private bool m_HideBlacklistedSites = false;
        /// <summary>
        /// Are the blacklisted sites hidden ?
        /// </summary>
        public bool HideBlacklistedSites
        {
            get
            {
                return m_HideBlacklistedSites;
            }
            set
            {
                m_HideBlacklistedSites = value;
                m_SitesUpToDate = false;
            }
        }

        private bool m_ShowAllSites = false;
        /// <summary>
        /// Are all sites shown (if false, only sites in ROIs are displayed) ?
        /// </summary>
        public bool ShowAllSites
        {
            get
            {
                return m_ShowAllSites;
            }
            set
            {
                m_ShowAllSites = value;
                foreach (var column in Columns)
                {
                    column.UpdateROIMask();
                }
                m_SitesUpToDate = false;
            }
        }
        
        private bool m_EdgeMode = false;
        /// <summary>
        /// Are the edges displayed ?
        /// </summary>
        public bool EdgeMode
        {
            get
            {
                return m_EdgeMode;
            }
            set
            {
                m_EdgeMode = value;
                foreach (Column3D column in Columns)
                {
                    foreach (View3D view in column.Views)
                    {
                        view.ShowEdges = m_EdgeMode;
                    }
                }
            }
        }

        private bool m_StrongCuts = false;
        /// <summary>
        /// Are we using strong cuts (cuts the vertices in front of each cut) or soft cuts (cuts only the vertices in front of every cuts) ?
        /// </summary>
        public bool StrongCuts
        {
            get
            {
                return m_StrongCuts;
            }
            set
            {
                m_StrongCuts = value;
                BrainMaterial.SetInt("_StrongCuts", m_StrongCuts ? 1 : 0);
                m_CutsNeedUpdate = true;
            }
        }

        private bool m_AutomaticRotation = false;
        /// <summary>
        /// Are the brains automatically rotating ?
        /// </summary>
        public bool AutomaticRotation
        {
            get
            {
                return m_AutomaticRotation;
            }
            set
            {
                m_AutomaticRotation = value;
                foreach (Column3D column in Columns)
                {
                    foreach (View3D view in column.Views)
                    {
                        view.AutomaticRotation = m_AutomaticRotation;
                    }
                }
            }
        }

        private float m_AutomaticRotationSpeed = 30.0f;
        /// <summary>
        /// Automatic rotation speed
        /// </summary>
        public float AutomaticRotationSpeed
        {
            get
            {
                return m_AutomaticRotationSpeed;
            }
            set
            {
                m_AutomaticRotationSpeed = value;
                foreach (Column3D column in Columns)
                {
                    foreach (View3D view in column.Views)
                    {
                        view.AutomaticRotationSpeed = m_AutomaticRotationSpeed;
                    }
                }
            }
        }

        private Data.Enums.CameraControl m_CameraType = Data.Enums.CameraControl.Trackball;
        /// <summary>
        /// Camera Control type (see <see cref="Data.Enums.CameraControl"/> for possible values)
        /// </summary>
        public Data.Enums.CameraControl CameraType
        {
            get
            {
                return m_CameraType;
            }
            set
            {
                m_CameraType = value;
                foreach (Column3D column in Columns)
                {
                    foreach (View3D view in column.Views)
                    {
                        view.CameraType = m_CameraType;
                    }
                }
            }
        }
        
        private bool m_ROICreationMode;
        /// <summary>
        /// Is ROI creation mode activated ?
        /// </summary>
        public bool ROICreationMode
        {
            get
            {
                return m_ROICreationMode;
            }
            set
            {
                m_ROICreationMode = value;
                for (int ii = 0; ii < Columns.Count; ++ii)
                    if (Columns[ii].SelectedROI != null)
                        Columns[ii].SelectedROI.SetRenderingState(value);
            }
        }
        
        /// <summary>
        /// True if we can compute and project the functional values on the mesh and on the MRI
        /// </summary>
        public bool CanComputeFunctionalValues
        {
            get
            {
                bool isOneColumnDynamic = ColumnsDynamic.Count > 0;
                bool areAllCCEPColumnsReady = ColumnsCCEP.All(c => c.IsSourceSelected);
                return isOneColumnDynamic && areAllCCEPColumnsReady;
            }
        }
        /// <summary>
        /// Index of the last cut plane that has been modified (this is used to show the cut circles)
        /// </summary>
        public int LastPlaneModifiedIndex { get; private set; }
        /// <summary>
        /// Does the mesh need a geometry update (changing vertices, computing cuts etc.)
        /// </summary>
        public bool MeshGeometryNeedsUpdate { get; set; }
        /// <summary>
        /// Does the collider of the brain mesh needs an update ?
        /// </summary>
        public bool ColliderNeedsUpdate { get; set; }
        /// <summary>
        /// True if generator needs an update
        /// </summary>
        private bool m_GeneratorNeedsUpdate = true;
        /// <summary>
        /// True if sites rendering is up to date
        /// </summary>
        private bool m_SitesUpToDate = true;
        /// <summary>
        /// True if cuts need an update
        /// </summary>
        private bool m_CutsNeedUpdate = true;
        /// <summary>
        /// True if cut textures are not up to date
        /// </summary>
        public bool CutTexturesNeedUpdate { get; set; }

        /// <summary>
        /// Lock when updating colliders
        /// </summary>
        private bool m_UpdatingColliders = false;
        /// <summary>
        /// Lock when updating generator
        /// </summary>
        public bool UpdatingGenerators { get; private set; } = false;

        private bool m_IsGeneratorUpToDate = false;
        /// <summary>
        /// Is the iEEG generator up to date ?
        /// </summary>
        public bool IsGeneratorUpToDate
        {
            get
            {
                return m_IsGeneratorUpToDate;
            }
            set
            {
                if (value != m_IsGeneratorUpToDate)
                {
                    m_IsGeneratorUpToDate = value;
                    BrainMaterial.SetInt("_Activity", value ? 1 : 0);
                    if (!value)
                    {
                        foreach (Column3DDynamic column in ColumnsDynamic)
                        {
                            column.Timeline.IsLooping = false;
                            column.Timeline.IsPlaying = false;
                            column.Timeline.OnUpdateCurrentIndex.Invoke();
                            column.IsRenderingUpToDate = false;
                        }
                    }
                    OnUpdateGeneratorState.Invoke(value);
                    ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
                }
            }
        }

        /// <summary>
        /// Is the scene initialized (loading is finished but displaying may not be finished) ?
        /// </summary>
        private bool m_Initialized = false;
        /// <summary>
        /// Has the scene finished loading (everything is loaded, scene is ready to be used) ?
        /// </summary>
        public bool IsSceneCompletelyLoaded { get; private set; } = false;
        /// <summary>
        /// True if the coroutine c_Destroy has been called
        /// </summary>
        private bool m_DestroyRequested = false;

        /// <summary>
        /// Weight of the mesh loading step
        /// </summary>
        private const int LOADING_MESH_WEIGHT = 2500;
        /// <summary>
        /// Weight of the MRI loading step
        /// </summary>
        private const int LOADING_MRI_WEIGHT = 1500;
        /// <summary>
        /// Weight of the implantation loading step
        /// </summary>
        private const int LOADING_IMPLANTATIONS_WEIGHT = 50;
        /// <summary>
        /// Weight of the MNI copy step
        /// </summary>
        private const int LOADING_MNI_WEIGHT = 100;
        /// <summary>
        /// Weight of the iEEG setup step
        /// </summary>
        private const int LOADING_IEEG_WEIGHT = 15;

        /// <summary>
        /// Prefab for the Column3D
        /// </summary>
        [SerializeField] private GameObject m_Column3DPrefab;
        /// <summary>
        /// Prefab for the Column3DIEEG
        /// </summary>
        [SerializeField] private GameObject m_Column3DIEEGPrefab;
        /// <summary>
        /// Prefab for the Column3DCCEP
        /// </summary>
        [SerializeField] private GameObject m_Column3DCCEPPrefab;
        /// <summary>
        /// Transform where to instantiate columns
        /// </summary>
        [SerializeField] private Transform m_ColumnsContainer;
        #endregion

        #region Events
        /// <summary>
        /// Event called when this scene is selected
        /// </summary>
        [HideInInspector] public UnityEvent OnSelect = new UnityEvent();
        /// <summary>
        /// Event called when showing or hiding the scene in the UI
        /// </summary>
        [HideInInspector] public GenericEvent<bool> OnChangeVisibleState = new GenericEvent<bool>();
        /// <summary>
        /// Event called when reseting the view positions in the UI
        /// </summary>
        [HideInInspector] public UnityEvent OnResetViewPositions = new UnityEvent();
        /// <summary>
        /// Event called when progressing in updating generator
        /// </summary>
        [HideInInspector] public GenericEvent<float, string, float> OnProgressUpdateGenerator = new GenericEvent<float, string, float>();
        /// <summary>
        /// Event for updating the planes cuts display in the cameras
        /// </summary>
        [HideInInspector] public UnityEvent OnModifyPlanesCuts = new UnityEvent();
        /// <summary>
        /// Event called when adding a cut to the scene
        /// </summary>
        [HideInInspector] public GenericEvent<Cut> OnAddCut = new GenericEvent<Cut>();
        /// <summary>
        /// Event called when cuts are updated
        /// </summary>
        [HideInInspector] public UnityEvent OnUpdateCuts = new UnityEvent();
        /// <summary>
        /// Event called when updating the sites rendering
        /// </summary>
        [HideInInspector] public UnityEvent OnSitesRenderingUpdated = new UnityEvent();
        /// <summary>
        /// Event called when changing the colors of the colormap
        /// </summary>
        [HideInInspector] public GenericEvent<Data.Enums.ColorType> OnChangeColormap = new GenericEvent<Data.Enums.ColorType>();
        /// <summary>
        /// Ask the camera manager to update the target for this scene
        /// </summary>
        [HideInInspector] public GenericEvent<Vector3> OnUpdateCameraTarget = new GenericEvent<Vector3>();
        /// <summary>
        /// Event called when site is clicked to dipslay additionnal infomation
        /// </summary>
        [HideInInspector] public GenericEvent<IEnumerable<Site>> OnRequestSiteInformation = new GenericEvent<IEnumerable<Site>>();
        /// <summary>
        /// Event called when requesting a screenshot of the scene (true if multi screenshots)
        /// </summary>
        [HideInInspector] public GenericEvent<bool> OnRequestScreenshot = new GenericEvent<bool>();
        /// <summary>
        /// Event called when ieeg are outdated or not anymore
        /// </summary>
        [HideInInspector] public GenericEvent<bool> OnIEEGOutdated = new GenericEvent<bool>();
        /// <summary>
        /// Event called when updating the ROI mask for this column
        /// </summary>
        [HideInInspector] public UnityEvent OnUpdateROIMask = new UnityEvent();
        /// <summary>
        /// Event called when minimizing a column
        /// </summary>
        [HideInInspector] public UnityEvent OnChangeColumnMinimizedState = new UnityEvent();
        /// <summary>
        /// Event called when selecting a source when viewing a CCEP column
        /// </summary>
        [HideInInspector] public UnityEvent OnSelectCCEPSource = new UnityEvent();
        /// <summary>
        /// Event called when adding a column
        /// </summary>
        [HideInInspector] public UnityEvent OnAddColumn = new UnityEvent();
        /// <summary>
        /// Event called when adding a line of views
        /// </summary>
        [HideInInspector] public UnityEvent OnAddViewLine = new UnityEvent();
        /// <summary>
        /// Event called when removing a line of views
        /// </summary>
        [HideInInspector] public GenericEvent<int> OnRemoveViewLine = new GenericEvent<int>();
        /// <summary>
        /// Event called when the generator is updated or not up to date
        /// </summary>
        [HideInInspector] public GenericEvent<bool> OnUpdateGeneratorState = new GenericEvent<bool>();
        /// <summary>
        /// Event called when selecting a site on a column
        /// </summary>
        [HideInInspector] public GenericEvent<Site> OnSelectSite = new GenericEvent<Site>();
        #endregion

        #region Private Methods
        private void Update()
        {
            if (!m_Initialized || m_DestroyRequested) return;

            if (MeshGeometryNeedsUpdate)
            {
                UpdateGeometry();
            }

            if (m_CutsNeedUpdate)
            {
                UpdateCuts();
            }

            if (CutTexturesNeedUpdate)
            {
                ComputeCutTextures();
            }

            if (!m_SitesUpToDate)
            {
                UpdateAllColumnsSitesRendering();
                OnSitesRenderingUpdated.Invoke();
            }

            if (!m_IsGeneratorUpToDate && CanComputeFunctionalValues && ApplicationState.UserPreferences.Visualization._3D.AutomaticEEGUpdate)
            {
                UpdateGenerator();
            }

            if (!IsSceneCompletelyLoaded)
            {
                UpdateVisibleState(true);
                IsSceneCompletelyLoaded = true;
                if (Visualization.Configuration.FirstColumnToSelect < Columns.Count)
                {
                    Columns[Visualization.Configuration.FirstColumnToSelect].SelectFirstOrDefaultSiteByName(Visualization.Configuration.FirstSiteToSelect);
                }
            }
        }
        private void OnDestroy()
        {
            foreach (var dllCommonBrainTextureGenerator in DLLCommonBrainTextureGeneratorList) dllCommonBrainTextureGenerator.Dispose();
            foreach (var dllMRIGeometryCutGenerator in DLLMRIGeometryCutGeneratorList) dllMRIGeometryCutGenerator.Dispose();
        }
        /// <summary>
        /// Compute the textures for the MRI (3D)
        /// </summary>
        private void ComputeMRITextures()
        {
            if (m_CutsNeedUpdate) return;

            foreach (Column3D column in Columns)
            {
                foreach (Cut cut in Cuts)
                {
                    column.CutTextures.CreateMRITexture(DLLMRIGeometryCutGeneratorList[cut.ID], MRIManager.SelectedMRI.Volume, cut.ID, MRIManager.MRICalMinFactor, MRIManager.MRICalMaxFactor, 3);
                    if (m_AtlasManager.DisplayJuBrainAtlas)
                    {
                        column.CutTextures.ColorCutsTexturesWithAtlas(cut.ID, m_AtlasManager.AtlasAlpha, m_AtlasManager.HoveredArea);
                    }
                    else if (FMRIManager.DisplayFMRI)
                    {
                        FMRIManager.ColorCutTexture(column, cut.ID);
                    }
                }
            }
        }
        /// <summary>
        /// Compute the textures for the MRI (3D) with the iEEG activity
        /// </summary>
        /// <param name="column">Specific column to update. If null, every columns will be updated.</param>
        private void ComputeIEEGTextures(Column3DDynamic column = null)
        {
            if (!m_IsGeneratorUpToDate) return;
            
            UnityEngine.Profiling.Profiler.BeginSample("Compute IEEG Textures");
            List<Column3DDynamic> columns = column ? new List<Column3DDynamic>() { column } : ColumnsDynamic;
            foreach (Column3DDynamic col in columns)
            {
                col.ComputeSurfaceBrainUVWithActivity(m_MeshManager.SplittedMeshes);
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Compute the texture for the MRI (GUI)
        /// </summary>
        private void ComputeGUITextures()
        {
            if (m_CutsNeedUpdate) return;

            Column3D column = SelectedColumn;
            if (column)
            {
                column.CutTextures.CreateGUIMRITextures(Cuts);
                column.CutTextures.UpdateTextures2D();
                foreach (Cut cut in Cuts)
                {
                    cut.OnUpdateGUITextures.Invoke(column);
                }
            }
        }
        /// <summary>
        /// Compute the cut textures (Cut mesh texture, activity coloration and GUI textures)
        /// </summary>
        private void ComputeCutTextures()
        {
            ComputeMRITextures();
            ComputeIEEGTextures();
            ComputeGUITextures();
            CutTexturesNeedUpdate = false;
        }
        /// <summary>
        /// Finalize Generators Computing (method called at the end of the computing of the activity)
        /// </summary>
        private void FinalizeGeneratorsComputing()
        {
            // generators are now up to date
            IsGeneratorUpToDate = true;

            // send inf values to overlays
            for (int ii = 0; ii < ColumnsDynamic.Count; ++ii)
            {
                ColumnsDynamic[ii].Timeline.OnUpdateCurrentIndex.Invoke();
            }

            // update plots visibility
            m_SitesUpToDate = false;

            OnIEEGOutdated.Invoke(false);
        }
        /// <summary>
        /// Actions to perform after clicking on a site
        /// </summary>
        /// <param name="site">Site that has been clicked</param>
        private void ClickOnSiteCallback(Site site)
        {
            if (SelectedColumn is Column3DDynamic && site)
            {
                List<Site> sites = new List<Site>();
                if (ImplantationManager.SiteToCompare) sites.Add(ImplantationManager.SiteToCompare);
                sites.Add(site);
                if (SelectedColumn is Column3DCCEP ccepColumn)
                {
                    if (ccepColumn.IsSourceSelected)
                    {
                        OnRequestSiteInformation.Invoke(sites);
                    }
                }
                else
                {
                    OnRequestSiteInformation.Invoke(sites);
                }
            }
            m_SitesUpToDate = false;
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Compute the cuts of the meshes (compute the cuts meshes, fill parameters in the brain mesh shader and reset generators)
        /// </summary>
        private void ComputeMeshesCut()
        {
            if (MeshManager.MeshToDisplay == null) return;

            // Create the cuts
            UnityEngine.Profiling.Profiler.BeginSample("cut_generator Create cut");
            List<DLL.Surface> generatedCutMeshes = new List<DLL.Surface>(Cuts.Count);
            if (Cuts.Count > 0)
                generatedCutMeshes = MeshManager.MeshToDisplay.GenerateCutSurfaces(Cuts, !m_CutHolesEnabled, StrongCuts);
            UnityEngine.Profiling.Profiler.EndSample();

            // Fill parameters in shader
            UnityEngine.Profiling.Profiler.BeginSample("cut_generator Fill shader");
            BrainMaterial.SetInt("_CutCount", Cuts.Count);
            if (Cuts.Count > 0)
            {
                List<Vector4> cutPoints = new List<Vector4>(20);
                for (int i = 0; i < 20; ++i)
                {
                    if (i < Cuts.Count)
                    {
                        cutPoints.Add(new Vector4(-Cuts[i].Point.x, Cuts[i].Point.y, Cuts[i].Point.z));
                    }
                    else
                    {
                        cutPoints.Add(Vector4.zero);
                    }
                }
                BrainMaterial.SetVectorArray("_CutPoints", cutPoints);
                List<Vector4> cutNormals = new List<Vector4>(20);
                for (int i = 0; i < 20; ++i)
                {
                    if (i < Cuts.Count)
                    {
                        cutNormals.Add(new Vector4(-Cuts[i].Normal.x, Cuts[i].Normal.y, Cuts[i].Normal.z));
                    }
                    else
                    {
                        cutNormals.Add(Vector4.zero);
                    }
                }
                BrainMaterial.SetVectorArray("_CutNormals", cutNormals);
            }
            UnityEngine.Profiling.Profiler.EndSample();

            // Update cut generators
            UnityEngine.Profiling.Profiler.BeginSample("cut_generator Update generators");
            for (int ii = 0; ii < Cuts.Count; ++ii)
            {
                DLLMRIGeometryCutGeneratorList[ii].Reset(m_MRIManager.SelectedMRI.Volume, Cuts[ii]);
                DLLMRIGeometryCutGeneratorList[ii].UpdateCutMeshUV(generatedCutMeshes[ii]);
                generatedCutMeshes[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<MeshFilter>().mesh);
            }
            UnityEngine.Profiling.Profiler.EndSample();

            // Display cuts
            UnityEngine.Profiling.Profiler.BeginSample("cut_generator Misc");
            for (int ii = 0; ii < Cuts.Count; ++ii)
                m_DisplayedObjects.BrainCutMeshes[ii].SetActive(true);

            ColliderNeedsUpdate = true;

            foreach (var cut in generatedCutMeshes)
            {
                cut.Dispose();
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Update the mesh geometry (information, cuts, generators, triangle eraser and atlas)
        /// </summary>
        private void UpdateGeometry()
        {
            m_MeshManager.UpdateMeshesInformation();
            UpdateCuts();
            UpdateGeneratorsAndUnityMeshes();
            m_TriangleEraser.ResetEraser();
            m_AtlasManager.UpdateAtlasIndices();

            MeshGeometryNeedsUpdate = false;
        }
        /// <summary>
        /// Update the cuts of the scene
        /// </summary>
        private void UpdateCuts()
        {
            ComputeMeshesCut();
            m_CutsNeedUpdate = false;
            OnUpdateCuts.Invoke();
            CutTexturesNeedUpdate = true;
        }
        /// <summary>
        /// Update the generators for activity and the UV of the meshes
        /// </summary>
        private void UpdateGeneratorsAndUnityMeshes()
        {
            for (int i = 0; i < m_MeshManager.MeshSplitNumber; ++i)
            {
                DLLCommonBrainTextureGeneratorList[i].Reset(m_MeshManager.SplittedMeshes[i], m_MRIManager.SelectedMRI.Volume);
                DLLCommonBrainTextureGeneratorList[i].ComputeMainUVWithVolume(m_MeshManager.SplittedMeshes[i], m_MRIManager.SelectedMRI.Volume, m_MRIManager.MRICalMinFactor, m_MRIManager.MRICalMaxFactor);
                DLLCommonBrainTextureGeneratorList[i].ComputeNullUV(m_MeshManager.SplittedMeshes[i]);
            }
            m_MeshManager.UpdateMeshesFromDLL();
        }
        /// <summary>
        /// Update the brain and the cuts meshes colliders
        /// </summary>
        private void UpdateMeshesColliders()
        {
            if (ColliderNeedsUpdate)
            {
                this.StartCoroutineAsync(c_UpdateMeshesColliders());
                ColliderNeedsUpdate = false;
            }
        }
        /// <summary>
        /// Update the sites rendering for all columns
        /// </summary>
        private void UpdateAllColumnsSitesRendering()
        {
            foreach (Column3D column in Columns)
            {
                column.UpdateSitesRendering(m_ShowAllSites, m_HideBlacklistedSites, m_IsGeneratorUpToDate);
            }
            m_SitesUpToDate = true;
        }
        /// <summary>
        /// Reset color schemes of every columns
        /// </summary>
        private void ResetColors()
        {
            for (int ii = 0; ii < Columns.Count; ++ii)
                Columns[ii].CutTextures.ResetColorSchemes(Colormap, CutColor);
        }
        /// <summary>
        /// Update the number of cut planes
        /// </summary>
        /// <param name="nbCuts">Number of cuts</param>
        private void UpdateCutNumber(int nbCuts)
        {
            while (DLLMRIGeometryCutGeneratorList.Count < nbCuts)
            {
                DLLMRIGeometryCutGeneratorList.Add(new DLL.MRIGeometryCutGenerator());
            }
            while (DLLMRIGeometryCutGeneratorList.Count > nbCuts)
            {
                DLLMRIGeometryCutGeneratorList.Last().Dispose();
                DLLMRIGeometryCutGeneratorList.RemoveAt(DLLMRIGeometryCutGeneratorList.Count - 1);
            }

            for (int c = 0; c < Columns.Count; c++)
            {
                Columns[c].UpdateCutsPlanesNumber(nbCuts);
            }
        }
        /// <summary>
        /// Add a column to the scene
        /// </summary>
        /// <param name="type">Base data column</param>
        private void AddColumn(Column baseColumn)
        {
            Column3D column = null;
            if (baseColumn is AnatomicColumn)
            {
                column = Instantiate(m_Column3DPrefab, m_ColumnsContainer).GetComponent<Column3D>();
            }
            else if (baseColumn is IEEGColumn)
            {
                column = Instantiate(m_Column3DIEEGPrefab, m_ColumnsContainer).GetComponent<Column3DIEEG>();
            }
            else if (baseColumn is CCEPColumn)
            {
                column = Instantiate(m_Column3DCCEPPrefab, m_ColumnsContainer).GetComponent<Column3DCCEP>();
            }
            column.gameObject.name = "Column " + Columns.Count;
            column.OnSelect.AddListener(() =>
            {
                foreach (Column3D c in Columns)
                {
                    if (c != column)
                    {
                        c.IsSelected = false;
                        foreach (View3D v in c.Views)
                        {
                            v.IsSelected = false;
                        }
                    }
                }
                IsSelected = true;
                ComputeGUITextures();
                OnUpdateCuts.Invoke();
            });
            column.OnMoveView.AddListener((view) =>
            {
                SynchronizeViewsToReferenceView(view);
            });
            column.OnUpdateROIMask.AddListener(() =>
            {
                ResetIEEG(false);
                OnUpdateROIMask.Invoke();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            });
            column.OnChangeMinimizedState.AddListener(() =>
            {
                OnChangeColumnMinimizedState.Invoke();
            });
            column.OnSelectSite.AddListener((site) =>
            {
                ClickOnSiteCallback(site);
                foreach (Column3D c in Columns)
                {
                    if (!c.IsSelected)
                    {
                        c.UnselectSite();
                    }
                }
                OnSelectSite.Invoke(site);
            });
            column.OnChangeSiteState.AddListener((site) =>
            {
                ResetIEEG(false);
            });
            if (column is Column3DDynamic dynamicColumn)
            {
                dynamicColumn.DynamicParameters.OnUpdateSpanValues.AddListener(() =>
                {
                    for (int jj = 0; jj < m_MeshManager.MeshSplitNumber; ++jj)
                    {
                        dynamicColumn.DLLBrainTextureGenerators[jj].AdjustInfluencesToColormap(dynamicColumn);
                    }
                    dynamicColumn.DLLMRIVolumeGenerator.AdjustInfluencesToColormap(dynamicColumn);
                    CutTexturesNeedUpdate = true;
                    dynamicColumn.IsRenderingUpToDate = false;
                    m_SitesUpToDate = false;
                });
                dynamicColumn.DynamicParameters.OnUpdateAlphaValues.AddListener(() =>
                {
                    ComputeIEEGTextures(dynamicColumn);
                    if (dynamicColumn.IsSelected)
                    {
                        ComputeGUITextures();
                    }
                    dynamicColumn.IsRenderingUpToDate = false;
                });
                dynamicColumn.DynamicParameters.OnUpdateGain.AddListener(() =>
                {
                    m_SitesUpToDate = false;
                    dynamicColumn.IsRenderingUpToDate = false;
                });
                dynamicColumn.DynamicParameters.OnUpdateInfluenceDistance.AddListener(() =>
                {
                    ResetIEEG();
                    dynamicColumn.IsRenderingUpToDate = false;
                });
                dynamicColumn.OnUpdateCurrentTimelineID.AddListener(() =>
                {
                    ComputeIEEGTextures(dynamicColumn);
                    if (dynamicColumn.IsSelected)
                    {
                        ComputeGUITextures();
                    }
                    m_SitesUpToDate = false;
                    dynamicColumn.IsRenderingUpToDate = false;
                });
                if (dynamicColumn is Column3DCCEP column3DCCEP)
                {
                    column3DCCEP.OnSelectSource.AddListener(() =>
                    {
                        ResetIEEG();
                        OnSelectCCEPSource.Invoke();
                        ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
                    });
                }
            }
            column.Initialize(Columns.Count, baseColumn, m_ImplantationManager.SelectedImplantation.PatientElectrodesList, m_DisplayedObjects.SitesPatientParent);
            column.ResetSplitsNumber(m_MeshManager.MeshSplitNumber);
            Columns.Add(column);
            OnAddColumn.Invoke();
        }
        /// <summary>
        /// Synchronize all cameras from the same view line
        /// </summary>
        /// <param name="referenceView">View to synchronize with</param>
        private void SynchronizeViewsToReferenceView(View3D referenceView)
        {
            foreach (Column3D column in Columns)
            {
                foreach (View3D view in column.Views)
                {
                    if (view.LineID == referenceView.LineID)
                    {
                        view.SynchronizeCamera(referenceView);
                    }
                }
            }
        }
        #endregion

        #region Public Methods

        #region Display
        /// <summary>
        /// Update the visible state of the scene
        /// </summary>
        /// <param name="state">Visible or not visible</param>
        public void UpdateVisibleState(bool state)
        {
            gameObject.SetActive(state);
            OnChangeVisibleState.Invoke(state);
            if (!state)
            {
                ApplicationState.Module3D.OnMinimizeScene.Invoke(this);
            }
            else
            {
                ApplicationState.Module3D.OnSelectScene.Invoke(this);
                ApplicationState.Module3D.OnSelectColumn.Invoke(SelectedColumn);
                ApplicationState.Module3D.OnSelectView.Invoke(SelectedColumn.SelectedView);
            }
            IsSelected = state;
        }
        /// <summary>
        /// Add a view to every columns
        /// </summary>
        public void AddViewLine()
        {
            foreach (Column3D column in Columns)
            {
                column.AddView();
            }
            OnAddViewLine.Invoke();
        }
        /// <summary>
        /// Remove a view from every columns
        /// </summary>
        /// <param name="lineID">ID of the line of the view to be removed</param>
        public void RemoveViewLine(int lineID = -1)
        {
            if (lineID == -1) lineID = ViewLineNumber - 1;
            bool wasSelected = false;
            foreach (Column3D column in Columns)
            {
                wasSelected |= column.Views[lineID].IsSelected;
                column.RemoveView(lineID);
            }
            OnRemoveViewLine.Invoke(ViewLineNumber);
            if (wasSelected)
            {
                SelectedColumn.Views.First().IsSelected = true;
            }
        }
        #endregion

        #region Cuts
        /// <summary>
        /// Add a new cut plane
        /// </summary>
        /// <returns>Newly created cut</returns>
        public Cut AddCutPlane()
        {
            // Add new cut
            Cut cut = new Cut(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
            switch (Cuts.Count)
            {
                case 0:
                    cut.Orientation = Data.Enums.CutOrientation.Axial;
                    cut.Flip = false;
                    cut.Position = 0.5f;
                    break;
                case 1:
                    cut.Orientation = Data.Enums.CutOrientation.Coronal;
                    cut.Flip = false;
                    cut.Position = 0.5f;
                    break;
                case 2:
                    cut.Orientation = Data.Enums.CutOrientation.Sagital;
                    cut.Flip = false;
                    cut.Position = 0.5f;
                    break;
                default:
                    cut.Orientation = Data.Enums.CutOrientation.Axial;
                    cut.Flip = false;
                    cut.Position = 0.5f;
                    break;
            }
            Cuts.Add(cut);

            // Update IDs
            for (int i = 0; i < Cuts.Count; i++)
            {
                Cuts[i].ID = i;
            }

            // Add new cut GameObject
            m_DisplayedObjects.InstantiateCut();

            UpdateCutNumber(m_DisplayedObjects.BrainCutMeshes.Count);

            m_CutsNeedUpdate = true;

            OnAddCut.Invoke(cut);
            UpdateCutPlane(cut);

            return cut;
        }
        /// <summary>
        /// Remove a cut plane
        /// </summary>
        /// <param name="cut">Cut to be removed</param>
        public void RemoveCutPlane(Cut cut)
        {
            Cuts.Remove(cut);
            for (int i = 0; i < Cuts.Count; i++)
            {
                Cuts[i].ID = i;
            }

            Destroy(m_DisplayedObjects.BrainCutMeshes[cut.ID]);
            m_DisplayedObjects.BrainCutMeshes.RemoveAt(cut.ID);

            UpdateCutNumber(m_DisplayedObjects.BrainCutMeshes.Count);

            m_CutsNeedUpdate = true;

            cut.OnRemoveCut.Invoke();
        }
        /// <summary>
        /// Update a cut plane
        /// </summary>
        /// <param name="cut">Cut to be updated</param>
        /// <param name="changedByUser">Has the cut been updated by the user or programatically ?</param>
        public void UpdateCutPlane(Cut cut, bool changedByUser = false)
        {
            if (cut.Orientation == Data.Enums.CutOrientation.Custom)
            {
                if (cut.Normal.x == 0 && cut.Normal.y == 0 && cut.Normal.z == 0)
                {
                    cut.Normal = new Vector3(1, 0, 0);
                }
            }
            else
            {
                Plane plane = new Plane(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
                m_MRIManager.SelectedMRI.Volume.SetPlaneWithOrientation(plane, cut.Orientation, cut.Flip);
                cut.Normal = plane.Normal;
            }

            if (changedByUser) LastPlaneModifiedIndex = cut.ID;

            // Cuts base on the mesh
            float offset;
            if (MeshManager.MeshToDisplay != null)
            {
                Plane plane = new Plane(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
                m_MRIManager.SelectedMRI.Volume.SetPlaneWithOrientation(plane, cut.Orientation, false);
                offset = MeshManager.MeshToDisplay.SizeOffsetCutPlane(plane, cut.NumberOfCuts);
                offset *= 1.05f; // upsize a little bit the bbox for planes
            }
            else
                offset = 0.1f;

            cut.Point = MeshManager.MeshCenter + cut.Normal.normalized * (cut.Position - 0.5f) * offset * cut.NumberOfCuts;

            m_CutsNeedUpdate = true;

            // update cameras cuts display
            OnModifyPlanesCuts.Invoke();
        }
        /// <summary>
        /// Update the values of all the cut planes
        /// </summary>
        public void UpdateAllCutPlanes()
        {
            foreach (var cut in Cuts)
            {
                UpdateCutPlane(cut);
            }
        }
        /// <summary>
        /// Create 3 cuts surrounding the selected site
        /// </summary>
        public void CutAroundSelectedSite()
        {
            Site site = SelectedColumn.SelectedSite;
            if (!site) return;

            foreach (var cut in Cuts.ToList())
            {
                RemoveCutPlane(cut);
            }

            Vector3 sitePosition = new Vector3(-site.transform.localPosition.x, site.transform.localPosition.y, site.transform.localPosition.z);

            Cut axialCut = AddCutPlane();
            Vector3 axialPoint = MeshManager.MeshCenter + (Vector3.Dot(sitePosition - MeshManager.MeshCenter, axialCut.Normal) / Vector3.Dot(axialCut.Normal, axialCut.Normal)) * axialCut.Normal;
            float axialOffset = MeshManager.MeshToDisplay.SizeOffsetCutPlane(axialCut, axialCut.NumberOfCuts) * 1.05f;
            axialCut.Position = ((axialPoint.z - MeshManager.MeshCenter.z) / (axialCut.Normal.z * axialOffset * axialCut.NumberOfCuts)) + 0.5f;
            if (axialCut.Position < 0.5f)
            {
                axialCut.Flip = true;
                axialCut.Position = 1 - axialCut.Position;
            }
            UpdateCutPlane(axialCut);

            Cut coronalCut = AddCutPlane();
            Vector3 coronalPoint = MeshManager.MeshCenter + (Vector3.Dot(sitePosition - MeshManager.MeshCenter, coronalCut.Normal) / Vector3.Dot(coronalCut.Normal, coronalCut.Normal)) * coronalCut.Normal;
            float coronalOffset = MeshManager.MeshToDisplay.SizeOffsetCutPlane(coronalCut, coronalCut.NumberOfCuts) * 1.05f;
            coronalCut.Position = ((coronalPoint.y - MeshManager.MeshCenter.y) / (coronalCut.Normal.y * coronalOffset * coronalCut.NumberOfCuts)) + 0.5f;
            if (coronalCut.Position < 0.5f)
            {
                coronalCut.Flip = true;
                coronalCut.Position = 1 - coronalCut.Position;
            }
            UpdateCutPlane(coronalCut);

            Cut sagitalCut = AddCutPlane();
            Vector3 sagitalPoint = MeshManager.MeshCenter + (Vector3.Dot(sitePosition - MeshManager.MeshCenter, sagitalCut.Normal) / Vector3.Dot(sagitalCut.Normal, sagitalCut.Normal)) * sagitalCut.Normal;
            float sagitalOffset = MeshManager.MeshToDisplay.SizeOffsetCutPlane(sagitalCut, sagitalCut.NumberOfCuts) * 1.05f;
            sagitalCut.Position = ((sagitalPoint.x - MeshManager.MeshCenter.x) / (sagitalCut.Normal.x * sagitalOffset * sagitalCut.NumberOfCuts)) + 0.5f;
            if (sagitalCut.Position < 0.5f)
            {
                sagitalCut.Flip = true;
                sagitalCut.Position = 1 - sagitalCut.Position;
            }
            UpdateCutPlane(sagitalCut);

            m_CutsNeedUpdate = true;
        }
        #endregion

        #region Save/Load
        /// <summary>
        /// Initialize the scene with the corresponding visualization
        /// </summary>
        /// <param name="visualization">Visualization to be loaded in this scene</param>
        public void Initialize(Visualization visualization)
        {
            BrainColorMapTexture = Texture2DExtension.Generate();
            BrainColorTexture = Texture2DExtension.Generate();

            Visualization = visualization;
            gameObject.name = Visualization.Name;

            // Init materials
            BrainMaterial = Instantiate(Resources.Load("Materials/Brain/Brain", typeof(Material))) as Material;
            SimplifiedBrainMaterial = Instantiate(Resources.Load("Materials/Brain/Simplified", typeof(Material))) as Material;
            CutMaterial = Instantiate(Resources.Load("Materials/Brain/Cut", typeof(Material))) as Material;

            transform.position = new Vector3(HBP3DModule.SPACE_BETWEEN_SCENES_GAME_OBJECTS * ApplicationState.Module3D.NumberOfScenesLoadedSinceStart++, transform.position.y, transform.position.z);
        }
        /// <summary>
        /// Set up the scene to display it properly
        /// </summary>
        public void FinalizeInitialization()
        {
            Columns[0].Views[0].IsSelected = true; // Select default view
            Columns[0].SelectFirstOrDefaultSiteByName();
            m_Initialized = true;
            this.StartCoroutineAsync(c_LoadMissingAnatomy());
        }
        /// <summary>
        /// Load the visualization configuration from the loaded visualization
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another load method ?</param>
        public void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration();
            BrainColor = Visualization.Configuration.BrainColor;
            CutColor = Visualization.Configuration.BrainCutColor;
            Colormap = Visualization.Configuration.EEGColormap;
            m_MeshManager.SelectMeshPart(Visualization.Configuration.MeshPart);
            EdgeMode = Visualization.Configuration.ShowEdges;
            StrongCuts = Visualization.Configuration.StrongCuts;
            HideBlacklistedSites = Visualization.Configuration.HideBlacklistedSites;
            ShowAllSites = Visualization.Configuration.ShowAllSites;
            m_MRIManager.SetCalValues(Visualization.Configuration.MRICalMinFactor, Visualization.Configuration.MRICalMaxFactor);
            CameraType = Visualization.Configuration.CameraType;

            if (!string.IsNullOrEmpty(Visualization.Configuration.MeshName)) m_MeshManager.Select(Visualization.Configuration.MeshName);
            if (!string.IsNullOrEmpty(Visualization.Configuration.MRIName)) m_MRIManager.Select(Visualization.Configuration.MRIName);
            if (!string.IsNullOrEmpty(Visualization.Configuration.ImplantationName)) m_ImplantationManager.Select(Visualization.Configuration.ImplantationName);

            foreach (Data.Visualization.Cut cut in Visualization.Configuration.Cuts)
            {
                Cut newCut = AddCutPlane();
                newCut.Normal = cut.Normal.ToVector3();
                newCut.Orientation = cut.Orientation;
                newCut.Flip = cut.Flip;
                newCut.Position = cut.Position;
                UpdateCutPlane(newCut);
            }

            for (int i = 0; i < Visualization.Configuration.Views.Count; i++)
            {
                View view = Visualization.Configuration.Views[i];
                if (i != 0)
                {
                    AddViewLine();
                }
                foreach (Column3D column in Columns)
                {
                    column.Views.Last().SetCamera(view.Position.ToVector3(), view.Rotation.ToQuaternion(), view.Target.ToVector3());
                }
            }

            ROICreationMode = !ROICreationMode;
            foreach (Column3D column in Columns)
            {
                column.LoadConfiguration(false);
            }
            ROICreationMode = !ROICreationMode;

            m_SitesUpToDate = false;

            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Save the current settings of this scene to the configuration of the linked visualization
        /// </summary>
        public void SaveConfiguration()
        {
            Visualization.Configuration.BrainColor = BrainColor;
            Visualization.Configuration.BrainCutColor = CutColor;
            Visualization.Configuration.EEGColormap = Colormap;
            Visualization.Configuration.MeshPart = MeshManager.MeshPartToDisplay;
            Visualization.Configuration.MeshName = m_MeshManager.SelectedMesh.Name;
            Visualization.Configuration.MRIName = m_MRIManager.SelectedMRI.Name;
            Visualization.Configuration.ImplantationName = m_ImplantationManager.SelectedImplantation.Name;
            Visualization.Configuration.ShowEdges = EdgeMode;
            Visualization.Configuration.StrongCuts = StrongCuts;
            Visualization.Configuration.HideBlacklistedSites = m_HideBlacklistedSites;
            Visualization.Configuration.ShowAllSites = ShowAllSites;
            Visualization.Configuration.MRICalMinFactor = m_MRIManager.MRICalMinFactor;
            Visualization.Configuration.MRICalMaxFactor = m_MRIManager.MRICalMaxFactor;
            Visualization.Configuration.CameraType = CameraType;

            List<Data.Visualization.Cut> cuts = new List<Data.Visualization.Cut>();
            foreach (Cut cut in Cuts)
            {
                cuts.Add(new Data.Visualization.Cut(cut.Normal, cut.Orientation, cut.Flip, cut.Position));
            }
            Visualization.Configuration.Cuts = cuts;

            List<View> views = new List<View>();
            if (Columns.Count > 0)
            {
                foreach (var view in Columns[0].Views)
                {
                    views.Add(new View(view.LocalCameraPosition, view.LocalCameraRotation, view.LocalCameraTarget));
                }
            }
            Visualization.Configuration.Views = views;

            foreach (Column3D column in Columns)
            {
                column.SaveConfiguration();
            }
            if (SelectedColumn.SelectedSite)
            {
                Visualization.Configuration.FirstSiteToSelect = SelectedColumn.SelectedSite.Information.ChannelName;
                Visualization.Configuration.FirstColumnToSelect = Columns.FindIndex(c => c = SelectedColumn);
            }
        }
        /// <summary>
        /// Reset the settings of the loaded scene
        /// </summary>
        public void ResetConfiguration()
        {
            BrainColor = Data.Enums.ColorType.BrainColor;
            CutColor = Data.Enums.ColorType.Default;
            Colormap = Data.Enums.ColorType.MatLab;
            m_MeshManager.SelectMeshPart(Data.Enums.MeshPart.Both);
            EdgeMode = false;
            StrongCuts = false;
            HideBlacklistedSites = false;
            m_MRIManager.SetCalValues(0, 1);
            CameraType = Data.Enums.CameraControl.Trackball;

            switch (Type)
            {
                case Data.Enums.SceneType.SinglePatient:
                    m_MeshManager.Select("Grey matter");
                    m_MRIManager.Select("Preimplantation");
                    m_ImplantationManager.Select("Patient");
                    break;
                case Data.Enums.SceneType.MultiPatients:
                    m_MeshManager.Select("MNI Grey matter");
                    m_MRIManager.Select("MNI");
                    m_ImplantationManager.Select("MNI");
                    break;
                default:
                    break;
            }

            while (Cuts.Count > 0)
            {
                RemoveCutPlane(Cuts.Last());
            }

            while (ViewLineNumber > 1)
            {
                RemoveViewLine();
            }
            foreach (Column3D column in Columns)
            {
                foreach (View3D view in column.Views)
                {
                    view.Default();
                }
            }

            foreach (Column3D column in Columns)
            {
                column.ResetConfiguration();
            }

            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
        
        /// <summary>
        /// Copy the states of the sites of the selected column to all other columns
        /// </summary>
        public void ApplySelectedColumnSiteStatesToOtherColumns()
        {
            Column3D selectedColumn = SelectedColumn;
            foreach (Column3D column in Columns)
            {
                if (column == selectedColumn) continue;
                foreach (Site site in column.Sites)
                {
                    site.State.ApplyState(selectedColumn.SiteStateBySiteID[site.Information.FullCorrectedID]);
                }
            }
            ResetIEEG(false);
        }
        /// <summary>
        /// Update the data rendering for a column
        /// </summary>
        /// <param name="column">Column to be updated</param>
        public void UpdateColumnRendering(Column3D column)
        {
            if (MeshGeometryNeedsUpdate) return;

            UnityEngine.Profiling.Profiler.BeginSample("TEST-updateColumnRender");

            // update cuts textures
            for (int i = 0; i < Cuts.Count; ++i)
            {
                m_DisplayedObjects.BrainCutMeshes[i].GetComponent<Renderer>().material.mainTexture = column.CutTextures.BrainCutTextures[i];
            }

            if (!column.IsRenderingUpToDate)
            {
                for (int i = 0; i < column.BrainSurfaceMeshes.Count; ++i)
                {
                    if (!(column is Column3DDynamic) || !m_IsGeneratorUpToDate)
                    {
                        column.BrainSurfaceMeshes[i].GetComponent<MeshFilter>().mesh.uv2 = DLLCommonBrainTextureGeneratorList[i].NullUV;
                        column.BrainSurfaceMeshes[i].GetComponent<MeshFilter>().mesh.uv3 = DLLCommonBrainTextureGeneratorList[i].NullUV;
                    }
                    else
                    {
                        column.BrainSurfaceMeshes[i].GetComponent<MeshFilter>().mesh.uv2 = ((Column3DDynamic)column).DLLBrainTextureGenerators[i].AlphaUV;
                        column.BrainSurfaceMeshes[i].GetComponent<MeshFilter>().mesh.uv3 = ((Column3DDynamic)column).DLLBrainTextureGenerators[i].ActivityUV;
                    }
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
            column.IsRenderingUpToDate = true;
        }
        /// <summary>
        /// Update the textures generator for iEEG
        /// </summary>
        public void UpdateGenerator()
        {
            if (MeshGeometryNeedsUpdate || UpdatingGenerators) // if update cut plane is pending, cancel action
                return;

            OnIEEGOutdated.Invoke(false);
            m_GeneratorNeedsUpdate = false;
            IsGeneratorUpToDate = false;
            StartCoroutine(c_ComputeGenerators());
        }
        /// <summary>
        /// Function to be called everytime we want to reset IEEG
        /// </summary>
        /// <param name="hardReset">Do we need to hard reset (delete the activity on the brain) ?</param>
        public void ResetIEEG(bool hardReset = true)
        {
            m_GeneratorNeedsUpdate = true;
            m_SitesUpToDate = false;
            if (hardReset)
            {
                IsGeneratorUpToDate = false;
                CutTexturesNeedUpdate = true;
            }
            OnIEEGOutdated.Invoke(true);
        }
        /// <summary>
        /// Passive raycast on the scene (to hover sites for instance)
        /// </summary>
        /// <param name="ray">Ray of the raycast</param>
        /// <param name="column">Column on which the raycast in performed</param>
        public void PassiveRaycastOnScene(Ray ray, Column3D column)
        {
            UpdateMeshesColliders();

            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(HBP3DModule.HIDDEN_MESHES_LAYER);
            layerMask |= 1 << LayerMask.NameToLayer(HBP3DModule.DEFAULT_MESHES_LAYER);

            Data.Enums.RaycastHitResult raycastResult = column.Raycast(ray, layerMask, out RaycastHit hit);
            Vector3 hitPoint = raycastResult != Data.Enums.RaycastHitResult.None ? hit.point - transform.position : Vector3.zero;

            m_AtlasManager.DisplayAtlasInformation(raycastResult == Data.Enums.RaycastHitResult.Cut || raycastResult == Data.Enums.RaycastHitResult.Mesh, hitPoint);
            m_ImplantationManager.DisplaySiteInformation(raycastResult == Data.Enums.RaycastHitResult.Site, column, hit);
        }
        /// <summary>
        /// Manage the clicks on the scene
        /// </summary>
        /// <param name="ray">Ray of the raycast</param>
        public void ClickOnScene(Ray ray)
        {
            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(HBP3DModule.HIDDEN_MESHES_LAYER);
            layerMask |= 1 << LayerMask.NameToLayer(HBP3DModule.DEFAULT_MESHES_LAYER);

            Data.Enums.RaycastHitResult raycastResult = SelectedColumn.Raycast(ray, layerMask, out RaycastHit hit);
            Vector3 hitPoint = raycastResult != Data.Enums.RaycastHitResult.None ? hit.point - transform.position : Vector3.zero;

            if (raycastResult == Data.Enums.RaycastHitResult.Site)
            {
                SelectedColumn.Sites[hit.collider.gameObject.GetComponent<Site>().Information.Index].IsSelected = true;
            }
            else
            {
                SelectedColumn.UnselectSite();
            }

            if (raycastResult == Data.Enums.RaycastHitResult.Mesh)
            {
                if (m_TriangleEraser.IsEnabled && m_TriangleEraser.IsClickAvailable)
                {
                    m_TriangleEraser.EraseTriangles(ray.direction, hitPoint);
                }
            }

            if (m_ROICreationMode)
            {
                ROI selectedROI = SelectedColumn.SelectedROI;
                if (selectedROI)
                {
                    if (raycastResult == Data.Enums.RaycastHitResult.ROI)
                    {
                        selectedROI.SelectClosestSphere(ray);
                    }
                    else if (raycastResult == Data.Enums.RaycastHitResult.Mesh || raycastResult == Data.Enums.RaycastHitResult.Cut)
                    {
                        selectedROI.AddSphere(SelectedColumn.Layer, "Bubble", hitPoint, 5.0f);
                        m_SitesUpToDate = false;
                    }
                    else
                    {
                        selectedROI.SelectSphere(-1);
                    }
                }
            }
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// Initialize the scene
        /// </summary>
        /// <param name="visualization">Visualization to load in the scene</param>
        /// <param name="onChangeProgress">Event to update the loading circle</param>
        /// <param name="outPut">Action to execute if an exception is raised</param>
        /// <returns>Coroutine return</returns>
        public IEnumerator c_Initialize(Visualization visualization, GenericEvent<float, float, LoadingText> onChangeProgress, Action<Exception> outPut)
        {
            Exception exception = null;

            // Find all usable implantations
            List<string> usableImplantations = visualization.FindUsableImplantations();

            // Compute progress variables
            float progress = 1.0f;
            float totalTime = 0, loadingMeshProgress = 0, loadingMeshTime = 0, loadingMRIProgress = 0, loadingMRITime = 0, loadingImplantationsProgress = 0, loadingImplantationsTime = 0, loadingMNIProgress = 0, loadingMNITime = 0, loadingIEEGProgress = 0, loadingIEEGTime = 0;
            if (Type == Data.Enums.SceneType.SinglePatient)
            {
                totalTime = Visualization.Patients[0].Meshes.Count * LOADING_MESH_WEIGHT + Visualization.Patients[0].MRIs.Count * LOADING_MRI_WEIGHT + usableImplantations.Count * LOADING_IMPLANTATIONS_WEIGHT + LOADING_MNI_WEIGHT + LOADING_IEEG_WEIGHT;
                loadingMeshProgress = LOADING_MESH_WEIGHT / totalTime;
                loadingMeshTime = LOADING_MESH_WEIGHT / 1000.0f;
                loadingMRIProgress = LOADING_MRI_WEIGHT / totalTime;
                loadingMRITime = LOADING_MRI_WEIGHT / 1000.0f;
                loadingImplantationsProgress = LOADING_IMPLANTATIONS_WEIGHT / totalTime;
                loadingImplantationsTime = LOADING_IMPLANTATIONS_WEIGHT / 1000.0f;
                loadingMNIProgress = LOADING_MNI_WEIGHT / totalTime;
                loadingMNITime = LOADING_MNI_WEIGHT / 1000.0f;
                loadingIEEGProgress = LOADING_IEEG_WEIGHT / totalTime;
                loadingIEEGTime = LOADING_IEEG_WEIGHT / 1000.0f;
            }
            else
            {
                totalTime = usableImplantations.Count * LOADING_IMPLANTATIONS_WEIGHT + LOADING_MNI_WEIGHT + Visualization.Patients.Count * LOADING_IEEG_WEIGHT;
                loadingImplantationsProgress = (Visualization.Patients.Count * LOADING_IMPLANTATIONS_WEIGHT) / totalTime;
                loadingImplantationsTime = (Visualization.Patients.Count * LOADING_IMPLANTATIONS_WEIGHT) / 1000.0f;
                loadingMNIProgress = LOADING_MNI_WEIGHT / totalTime;
                loadingMNITime = LOADING_MNI_WEIGHT / 1000.0f;
                loadingIEEGProgress = (Visualization.Patients.Count * LOADING_IEEG_WEIGHT) / totalTime;
                loadingIEEGTime = (Visualization.Patients.Count * LOADING_IEEG_WEIGHT) / 1000.0f;
            }
            yield return Ninja.JumpToUnity;
            onChangeProgress.Invoke(progress, 0.0f, new LoadingText());

            // Checking MNI
            onChangeProgress.Invoke(progress, 0.0f, new LoadingText("Loading MNI"));
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            yield return new WaitUntil(delegate { return ApplicationState.Module3D.MNIObjects.IsLoaded || watch.ElapsedMilliseconds > 5000; });
            watch.Stop();
            if (watch.ElapsedMilliseconds > 5000)
            {
                outPut(new CanNotLoadMNI());
                yield break;
            }

            // Loading MNI
            progress += loadingMNIProgress;
            onChangeProgress.Invoke(progress, loadingMNITime, new LoadingText("Loading MNI objects"));
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadMNIObjects(e => exception = e));
            if (exception != null)
            {
                outPut(exception);
                yield break;
            }

            // Loading Meshes
            if (Type == Data.Enums.SceneType.SinglePatient)
            {
                for (int i = 0; i < Visualization.Patients[0].Meshes.Count; ++i)
                {
                    Data.Anatomy.Mesh mesh = Visualization.Patients[0].Meshes[i];
                    progress += loadingMeshProgress;
                    onChangeProgress.Invoke(progress, loadingMeshTime, new LoadingText("Loading Mesh ", mesh.Name, " [" + (i + 1).ToString() + "/" + Visualization.Patients[0].Meshes.Count + "]"));
                    yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadBrainSurface(mesh, e => exception = e));
                }
                if (exception != null)
                {
                    outPut(exception);
                    yield break;
                }
            }
            m_MeshManager.GenerateSplits(ApplicationState.UserPreferences.Data.Anatomic.MeshPreloading || Type == Data.Enums.SceneType.MultiPatients);
            MeshGeometryNeedsUpdate = true;

            // Loading MRIs
            if (Type == Data.Enums.SceneType.SinglePatient)
            {
                for (int i = 0; i < Visualization.Patients[0].MRIs.Count; ++i)
                {
                    Data.Anatomy.MRI mri = Visualization.Patients[0].MRIs[i];
                    progress += loadingMRIProgress;
                    onChangeProgress.Invoke(progress, loadingMRITime, new LoadingText("Loading MRI ", mri.Name, " [" + (i + 1).ToString() + "/" + Visualization.Patients[0].MRIs.Count + "]"));
                    yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadBrainVolume(mri, e => exception = e));
                }
                if (exception != null)
                {
                    outPut(exception);
                    yield break;
                }
            }

            // Loading Sites
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadImplantations(visualization.Patients, usableImplantations, (i) =>
            {
                progress += loadingImplantationsProgress;
                onChangeProgress.Invoke(progress, loadingImplantationsTime, new LoadingText("Loading implantations ", "", "[" + (i + 1).ToString() + "/" + usableImplantations.Count + "]"));
            }, e => exception = e));
            if (exception != null)
            {
                outPut(exception);
                yield break;
            }

            // Loading Columns
            progress += loadingIEEGProgress;
            onChangeProgress.Invoke(progress, loadingIEEGTime, new LoadingText("Loading columns"));
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadColumns(e => exception = e));
            if (exception != null)
            {
                outPut(exception);
                yield break;
            }

            // Finalization
            foreach (Column3D column in Columns)
            {
                column.InitializeColumnMeshes(m_DisplayedObjects.BrainSurfaceMeshesParent);
            }
            OnUpdateCameraTarget.Invoke(m_MeshManager.SelectedMesh.Both.Center);
            outPut(exception);
        }
        /// <summary>
        /// Load a MRI to the scene
        /// </summary>
        /// <param name="mri">MRI to load</param>
        /// <param name="outPut">Action to execute if an exception is raised</param>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_LoadBrainVolume(Data.Anatomy.MRI mri, Action<Exception> outPut)
        {
            try
            {
                m_MRIManager.Add(mri);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                outPut(new CanNotLoadNIIFile(mri.File));
                yield break;
            }
        }
        /// <summary>
        /// Load a mesh to the scene
        /// </summary>
        /// <param name="mesh">Mesh to be loaded</param>
        /// <param name="outPut">Action to execute if an exception is raised</param>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_LoadBrainSurface(Data.Anatomy.Mesh mesh, Action<Exception> outPut)
        {
            try
            {
                m_MeshManager.Add(mesh);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                outPut(new CanNotLoadGIIFile(mesh.Name));
                yield break;
            }
            yield return true;
        }
        /// <summary>
        /// Load the implantation files to the scene
        /// </summary>
        /// <param name="patients">Patients to load implantations from</param>
        /// <param name="commonImplantations">List of the implantations that are in all patients of the visualization</param>
        /// <param name="updateCircle">Action to update the loading circle</param>
        /// <param name="outPut">Action to execute if an exception is raised</param>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_LoadImplantations(IEnumerable<Data.Patient> patients, List<string> commonImplantations, Action<int> updateCircle, Action<Exception> outPut)
        {
            for (int i = 0; i < commonImplantations.Count; ++i)
            {
                yield return Ninja.JumpToUnity;
                updateCircle(i);
                yield return Ninja.JumpBack;
                string implantationName = commonImplantations[i];
                try
                {
                    IEnumerable<string> ptsFiles = from patient in patients select patient.Implantations.Find((imp) => imp.Name == implantationName).File;
                    IEnumerable<string> marsAtlasFiles = from patient in patients select patient.Implantations.Find((imp) => imp.Name == implantationName).MarsAtlas;
                    IEnumerable<string> patientIDs = from patient in patients select patient.ID;

                    m_ImplantationManager.Add(implantationName, ptsFiles, marsAtlasFiles, patientIDs);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    outPut(new CanNotLoadImplantation(implantationName));
                    yield break;
                }
            }
            
            yield return Ninja.JumpToUnity;
            m_ImplantationManager.Select("");
            yield return Ninja.JumpBack;
        }
        /// <summary>
        /// Copy the MNI objects references to this scene
        /// </summary>
        /// <param name="outPut">Action to execute if an exception is raised</param>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_LoadMNIObjects(Action<Exception> outPut)
        {
            try
            {
                m_MeshManager.Meshes.Add((LeftRightMesh3D)(ApplicationState.Module3D.MNIObjects.GreyMatter.Clone()));
                m_MeshManager.Meshes.Add((LeftRightMesh3D)(ApplicationState.Module3D.MNIObjects.WhiteMatter.Clone()));
                m_MeshManager.Meshes.Add((LeftRightMesh3D)(ApplicationState.Module3D.MNIObjects.InflatedWhiteMatter.Clone()));
                m_MRIManager.MRIs.Add(ApplicationState.Module3D.MNIObjects.MRI);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                outPut(new CanNotLoadMNI());
                yield break;
            }
        }
        /// <summary>
        /// Load the iEEG values to the columns
        /// </summary>
        /// <param name="outPut">Action to execute if an exception is raised</param>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_LoadColumns(Action<Exception> outPut)
        {
            yield return Ninja.JumpToUnity;
            try
            {
                foreach (Column column in Visualization.Columns)
                {
                    AddColumn(column);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                outPut(e);
                yield break;
            }
            yield return Ninja.JumpBack;

            try
            {
                foreach (var columnIEEG in ColumnsDynamic)
                {
                    columnIEEG.ComputeActivityData();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                outPut(e);
                yield break;
            }
        }
        /// <summary>
        /// Load missing anatomy if not preloaded
        /// </summary>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_LoadMissingAnatomy()
        {
            yield return Ninja.JumpBack;
            m_MeshManager.LoadMissing();
            m_MRIManager.LoadMissing();
        }
        /// <summary>
        /// Start the update of the generators for the iEEG signal on the brain
        /// </summary>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_ComputeGenerators()
        {
            UpdatingGenerators = true;
            yield return this.StartCoroutineAsync(c_LoadIEEG());
            UpdatingGenerators = false;

            if (!m_GeneratorNeedsUpdate)
            {
                FinalizeGeneratorsComputing();
                ComputeIEEGTextures();
                ComputeGUITextures();
            }
        }
        /// <summary>
        /// Compute the iEEG values on the brain
        /// </summary>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_LoadIEEG()
        {
            yield return Ninja.JumpToUnity;
            float totalTime = 0.075f * Visualization.Patients.Count + 1.5f; // Calculated by Linear Regression is 0.0593f * Patients.Count + 1.0956f
            float totalProgress = ColumnsDynamic.Count * (1 + m_MeshManager.MeshSplitNumber + 10);
            float timeByProgress = totalTime / totalProgress;
            float currentProgress = 0.0f;
            OnProgressUpdateGenerator.Invoke(currentProgress / totalProgress, "Initializing", timeByProgress);
            yield return Ninja.JumpBack;
            bool addValues = false;

            // copy from main generators
            for (int ii = 0; ii < ColumnsDynamic.Count; ++ii)
            {
                for (int jj = 0; jj < m_MeshManager.MeshSplitNumber; ++jj)
                {
                    ColumnsDynamic[ii].DLLBrainTextureGenerators[jj].Dispose();
                    ColumnsDynamic[ii].DLLBrainTextureGenerators[jj] = (DLL.MRIBrainGenerator)DLLCommonBrainTextureGeneratorList[jj].Clone();
                    if (m_GeneratorNeedsUpdate) yield break;
                }
            }

            // Do your threaded task
            for (int ii = 0; ii < ColumnsDynamic.Count; ++ii)
            {
                yield return Ninja.JumpToUnity;
                OnProgressUpdateGenerator.Invoke(++currentProgress / totalProgress, "Loading " + ColumnsDynamic[ii].Name, timeByProgress);
                yield return Ninja.JumpBack;

                float currentMaxDensity, currentMinInfluence, currentMaxInfluence;
                float maxDensity = 1;

                ColumnsDynamic[ii].SharedMinInf = float.MaxValue;
                ColumnsDynamic[ii].SharedMaxInf = float.MinValue;

                // update raw electrodes
                ColumnsDynamic[ii].UpdateDLLSitesMask();

                // splits
                for (int jj = 0; jj < m_MeshManager.MeshSplitNumber; ++jj)
                {
                    yield return Ninja.JumpToUnity;
                    OnProgressUpdateGenerator.Invoke(++currentProgress / totalProgress, "Loading " + ColumnsDynamic[ii].Name, timeByProgress);
                    yield return Ninja.JumpBack;
                    if (m_GeneratorNeedsUpdate) yield break;
                    ColumnsDynamic[ii].DLLBrainTextureGenerators[jj].InitializeOctree(ColumnsDynamic[ii].RawElectrodes);
                    if (m_GeneratorNeedsUpdate) yield break;
                    ColumnsDynamic[ii].DLLBrainTextureGenerators[jj].ComputeDistances(ColumnsDynamic[ii].DynamicParameters.InfluenceDistance, ApplicationState.UserPreferences.General.System.MultiThreading);
                    if (m_GeneratorNeedsUpdate) yield break;
                    ColumnsDynamic[ii].DLLBrainTextureGenerators[jj].ComputeInfluences(ColumnsDynamic[ii], ApplicationState.UserPreferences.General.System.MultiThreading, addValues, ApplicationState.UserPreferences.Visualization._3D.SiteInfluenceByDistance);
                    if (m_GeneratorNeedsUpdate) yield break;

                    currentMaxDensity = ColumnsDynamic[ii].DLLBrainTextureGenerators[jj].MaximumDensity;
                    currentMinInfluence = ColumnsDynamic[ii].DLLBrainTextureGenerators[jj].MinimumInfluence;
                    currentMaxInfluence = ColumnsDynamic[ii].DLLBrainTextureGenerators[jj].MaximumInfluence;

                    if (currentMaxDensity > maxDensity)
                        maxDensity = currentMaxDensity;

                    if (currentMinInfluence < ColumnsDynamic[ii].SharedMinInf)
                        ColumnsDynamic[ii].SharedMinInf = currentMinInfluence;

                    if (currentMaxInfluence > ColumnsDynamic[ii].SharedMaxInf)
                        ColumnsDynamic[ii].SharedMaxInf = currentMaxInfluence;

                }

                // volume
                yield return Ninja.JumpToUnity;
                currentProgress += 10;
                OnProgressUpdateGenerator.Invoke(currentProgress / totalProgress, "Loading " + ColumnsDynamic[ii].Name, timeByProgress * 10);
                yield return Ninja.JumpBack;
                if (m_GeneratorNeedsUpdate) yield break;
                ColumnsDynamic[ii].DLLMRIVolumeGenerator.InitializeOctree(ColumnsDynamic[ii].RawElectrodes);
                if (m_GeneratorNeedsUpdate) yield break;
                ColumnsDynamic[ii].DLLMRIVolumeGenerator.ComputeDistances(ColumnsDynamic[ii].DynamicParameters.InfluenceDistance, ApplicationState.UserPreferences.General.System.MultiThreading);
                if (m_GeneratorNeedsUpdate) yield break;
                ColumnsDynamic[ii].DLLMRIVolumeGenerator.ComputeInfluences(ColumnsDynamic[ii], ApplicationState.UserPreferences.General.System.MultiThreading, addValues, (int)ApplicationState.UserPreferences.Visualization._3D.SiteInfluenceByDistance);
                if (m_GeneratorNeedsUpdate) yield break;

                currentMaxDensity = ColumnsDynamic[ii].DLLMRIVolumeGenerator.MaximumDensity;
                currentMinInfluence = ColumnsDynamic[ii].DLLMRIVolumeGenerator.MinimumInfluence;
                currentMaxInfluence = ColumnsDynamic[ii].DLLMRIVolumeGenerator.MaximumInfluence;

                if (currentMaxDensity > maxDensity)
                    maxDensity = currentMaxDensity;

                if (currentMinInfluence < ColumnsDynamic[ii].SharedMinInf)
                    ColumnsDynamic[ii].SharedMinInf = currentMinInfluence;

                if (currentMaxInfluence > ColumnsDynamic[ii].SharedMaxInf)
                    ColumnsDynamic[ii].SharedMaxInf = currentMaxInfluence;

                // synchronize max density
                for (int jj = 0; jj < m_MeshManager.MeshSplitNumber; ++jj)
                {
                    ColumnsDynamic[ii].DLLBrainTextureGenerators[jj].SynchronizeWithOthersGenerators(maxDensity, ColumnsDynamic[ii].SharedMinInf, ColumnsDynamic[ii].SharedMaxInf);
                    if (m_GeneratorNeedsUpdate) yield break;
                }
                ColumnsDynamic[ii].DLLMRIVolumeGenerator.SynchronizeWithOthersGenerators(maxDensity, ColumnsDynamic[ii].SharedMinInf, ColumnsDynamic[ii].SharedMaxInf);
                if (m_GeneratorNeedsUpdate) yield break;

                for (int jj = 0; jj < m_MeshManager.MeshSplitNumber; ++jj)
                {
                    ColumnsDynamic[ii].DLLBrainTextureGenerators[jj].AdjustInfluencesToColormap(ColumnsDynamic[ii]);
                    if (m_GeneratorNeedsUpdate) yield break;
                }
                ColumnsDynamic[ii].DLLMRIVolumeGenerator.AdjustInfluencesToColormap(ColumnsDynamic[ii]);
                if (m_GeneratorNeedsUpdate) yield break;
            }
            yield return Ninja.JumpToUnity;
            OnProgressUpdateGenerator.Invoke(1.0f, "Finalizing", timeByProgress);
            yield return Ninja.JumpBack;
            yield return new WaitForSeconds(0.1f);
        }
        /// <summary>
        /// Update the colliders (cuts and brain meshes)
        /// </summary>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_UpdateMeshesColliders()
        {
            while (m_UpdatingColliders)
            {
                yield return new WaitForSeconds(0.05f);
            }

            m_UpdatingColliders = true;

            yield return Ninja.JumpBack;
            List<DLL.Surface> cuts;
            if (Cuts.Count > 0) cuts = new List<DLL.Surface>(MeshManager.SimplifiedMeshToUse.Cut(Cuts.ToArray(), !m_CutHolesEnabled, StrongCuts));
            else cuts = new List<DLL.Surface>() { (DLL.Surface)MeshManager.SimplifiedMeshToUse.Clone() };
            yield return Ninja.JumpToUnity;

            cuts[0].UpdateMeshFromDLL(m_DisplayedObjects.SimplifiedBrain.GetComponent<MeshFilter>().mesh);
            yield return Ninja.JumpBack;
            foreach (var cut in cuts)
            {
                cut.Dispose();
            }
            yield return Ninja.JumpToUnity;
            m_DisplayedObjects.SimplifiedBrain.GetComponent<MeshCollider>().sharedMesh = null;
            m_DisplayedObjects.SimplifiedBrain.GetComponent<MeshCollider>().sharedMesh = m_DisplayedObjects.SimplifiedBrain.GetComponent<MeshFilter>().sharedMesh;

            // update cuts colliders
            for (int ii = 0; ii < m_DisplayedObjects.BrainCutMeshes.Count; ++ii)
            {
                yield return Ninja.JumpToUnity;
                m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<MeshCollider>().sharedMesh = null;
                m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<MeshCollider>().sharedMesh = m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<MeshFilter>().mesh;
                yield return Ninja.JumpBack;
            }

            m_UpdatingColliders = false;
        }
        /// <summary>
        /// Coroutine triggered when destroying the scene (waiting for generators to be updated before completely destroying the scene)
        /// </summary>
        /// <returns>Coroutine return</returns>
        public IEnumerator c_Destroy()
        {
            m_GeneratorNeedsUpdate = true;
            m_DestroyRequested = true;
            yield return new WaitUntil(delegate { return !UpdatingGenerators; });
            Visualization.Unload();
            Destroy(gameObject);
        }
        #endregion
    }
}