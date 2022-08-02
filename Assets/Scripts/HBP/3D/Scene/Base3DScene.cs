using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using CielaSpike;
using Tools.Unity;
using System.IO;
using HBP.Core.Data.Enums;

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
    public class Base3DScene : MonoBehaviour, Core.IConfigurable
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
        public SceneType Type
        {
            get
            {
                return Visualization.Patients.Count == 1 ? SceneType.SinglePatient : SceneType.MultiPatients;
            }
        }

        /// <summary>
        /// Visualization associated to this scene
        /// </summary>
        public Core.Data.Visualization Visualization { get; private set; }

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
                m_IsSelected = value;
                if (m_IsSelected)
                {
                    OnSelect.Invoke();
                    ApplicationState.Module3D.OnSelectScene.Invoke(this);
                }
            }
        }
        
        /// <summary>
        /// Cuts planes list
        /// </summary>
        public List<Core.Object3D.Cut> Cuts { get; } = new List<Core.Object3D.Cut>();

        /// <summary>
        /// Information about the scene
        /// </summary>
        public SceneInformation SceneInformation { get; set; } = new SceneInformation();

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

        [SerializeField] private ROIManager m_ROIManager;
        /// <summary>
        /// Object that handles the ROIs of the scene
        /// </summary>
        public ROIManager ROIManager { get { return m_ROIManager; } }

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
        /// Anatomical Columns of the scene
        /// </summary>
        public List<Column3DAnatomy> ColumnsAnatomy { get { return Columns.OfType<Column3DAnatomy>().ToList(); } }
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
        /// FMRI Columns of the scene
        /// </summary>
        public List<Column3DFMRI> ColumnsFMRI { get { return Columns.OfType<Column3DFMRI>().ToList(); } }
        /// <summary>
        /// MEG Columns of the scene
        /// </summary>
        public List<Column3DMEG> ColumnsMEG { get { return Columns.OfType<Column3DMEG>().ToList(); } }
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

        private Core.DLL.GeneratorSurface m_GeneratorSurface;
        /// <summary>
        /// Geometry generator for cuts
        /// </summary>
        public List<Core.DLL.CutGeometryGenerator> CutGeometryGenerators { get; set; } = new List<Core.DLL.CutGeometryGenerator>();

        /// <summary>
        /// Material used for the brain mesh
        /// </summary>
        public Core.Object3D.BrainMaterials BrainMaterials { get; private set; }

        private ColorType m_BrainColor = ColorType.BrainColor;
        /// <summary>
        /// Brain surface color type (see <see cref="ColorType"/> for all possible values)
        /// </summary>
        public ColorType BrainColor
        {
            get
            {
                return m_BrainColor;
            }
            set
            {
                m_BrainColor = value;

                BrainColorTexture = Texture2DExtension.Generate();
                Core.DLL.Texture tex = Core.DLL.Texture.Generate1DColorTexture(value);
                tex.UpdateTexture2D(BrainColorTexture);
                tex.Dispose();

                BrainMaterials.SetBrainColorTexture(BrainColorTexture);
            }
        }

        private ColorType m_CutColor = ColorType.Default;
        /// <summary>
        /// Brain cut color type (see <see cref="ColorType"/> for all possible values)
        /// </summary>
        public ColorType CutColor
        {
            get
            {
                return m_CutColor;
            }
            set
            {
                m_CutColor = value;
                ResetColors();
                SceneInformation.CutsNeedUpdate = true;
            }
        }

        private ColorType m_Colormap = ColorType.MatLab;
        /// <summary>
        /// Colormap type (see <see cref="ColorType"/> for all possible values)
        /// </summary>
        public ColorType Colormap
        {
            get
            {
                return m_Colormap;
            }
            set
            {
                m_Colormap = value;

                BrainColorMapTexture = Texture2DExtension.Generate();
                Core.DLL.Texture tex = Core.DLL.Texture.Generate1DColorTexture(value);
                tex.UpdateTexture2D(BrainColorMapTexture);
                tex.Dispose();

                ResetColors();
                BrainMaterials.SetBrainColormapTexture(BrainColorMapTexture);
                SceneInformation.FunctionalCutTexturesNeedUpdate = true;
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

        /// <summary>
        /// Is the brain transparent ?
        /// </summary>
        public bool IsBrainTransparent
        {
            get
            {
                return BrainMaterials.IsTransparent;
            }
            set
            {
                BrainMaterials.IsTransparent = value;
                m_DisplayedObjects.Brain.GetComponent<Renderer>().sharedMaterial = BrainMaterials.BrainMaterial;
                foreach (var column in Columns)
                    column.BrainMesh.GetComponent<Renderer>().sharedMaterial = BrainMaterials.BrainMaterial;
                foreach (var cut in m_DisplayedObjects.BrainCutMeshes)
                    cut.GetComponent<Renderer>().sharedMaterial = BrainMaterials.CutMaterial;
                m_DisplayedObjects.SimplifiedBrain.SetActive(!value);
                BrainMaterials.SetAlpha(BrainMaterials.Alpha);
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
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
                SceneInformation.SitesNeedUpdate = true;
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
                m_ROIManager.UpdateROIMasks();
                SceneInformation.SitesNeedUpdate = true;
            }
        }

        private float m_SiteGain = 1.0f;
        /// <summary>
        /// Gain for the size of the sites
        /// </summary>
        public float SiteGain
        {
            get
            {
                return m_SiteGain;
            }
            set
            {
                if (m_SiteGain != value)
                {
                    m_SiteGain = value;
                    SceneInformation.SitesNeedUpdate = true;
                }
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
                BrainMaterials.SetStrongCuts(m_StrongCuts);
                SceneInformation.CutsNeedUpdate = true;
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

        private CameraControl m_CameraType = CameraControl.Trackball;
        /// <summary>
        /// Camera Control type (see <see cref="CameraControl"/> for possible values)
        /// </summary>
        public CameraControl CameraType
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

        private bool m_DisplayCorrelations = false;
        /// <summary>
        /// Display correlations between sites
        /// </summary>
        public bool DisplayCorrelations
        {
            get
            {
                return m_DisplayCorrelations;
            }
            set
            {
                m_DisplayCorrelations = value;
                OnChangeDisplayCorrelations.Invoke();
            }
        }

        private bool m_AutomaticCutAroundSelectedSite = false;
        /// <summary>
        /// Automatically cuts around the currently selected site
        /// </summary>
        public bool AutomaticCutAroundSelectedSite
        {
            get
            {
                return m_AutomaticCutAroundSelectedSite;
            }
            set
            {
                m_AutomaticCutAroundSelectedSite = value;
                SceneInformation.CutsNeedUpdate = true;
                OnChangeAutomaticCutAroundSelectedSite.Invoke(value);
            }
        }

        /// <summary>
        /// True if we can compute and project the functional values on the mesh and on the MRI
        /// </summary>
        public bool CanComputeFunctionalValues
        {
            get
            {
                bool areAllCCEPColumnsReady = ColumnsCCEP.All(c => c.IsSourceSelected);
                return areAllCCEPColumnsReady;
            }
        }
        /// <summary>
        /// Index of the last cut plane that has been modified (this is used to show the cut circles)
        /// </summary>
        public int LastPlaneModifiedIndex { get; private set; }

        /// <summary>
        /// Lock when updating colliders
        /// </summary>
        private bool m_UpdatingColliders = false;
        /// <summary>
        /// Lock when updating generator
        /// </summary>
        private bool m_UpdatingGenerators = false;

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
                m_IsGeneratorUpToDate = value;
                BrainMaterials.SetActivity(value);
                if (!value)
                {
                    foreach (Column3DDynamic column in ColumnsDynamic)
                    {
                        column.Timeline.IsLooping = false;
                        column.Timeline.IsPlaying = false;
                        column.Timeline.OnUpdateCurrentIndex.Invoke();
                    }
                    foreach (Column3DFMRI column in ColumnsFMRI)
                    {
                        column.Timeline.IsLooping = false;
                        column.Timeline.IsPlaying = false;
                        column.Timeline.OnUpdateCurrentIndex.Invoke();
                    }
                    foreach (Column3DMEG column in ColumnsMEG)
                    {
                        column.Timeline.IsLooping = false;
                        column.Timeline.IsPlaying = false;
                        column.Timeline.OnUpdateCurrentIndex.Invoke();
                    }
                }
                SceneInformation.FunctionalSurfaceNeedsUpdate = true;
                foreach (Column3D column in Columns)
                    column.SurfaceNeedsUpdate = true;

                OnUpdateGeneratorState.Invoke(value);
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }
        
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
        [SerializeField] private GameObject m_Column3DAnatomyPrefab;
        /// <summary>
        /// Prefab for the Column3DIEEG
        /// </summary>
        [SerializeField] private GameObject m_Column3DIEEGPrefab;
        /// <summary>
        /// Prefab for the Column3DCCEP
        /// </summary>
        [SerializeField] private GameObject m_Column3DCCEPPrefab;
        /// <summary>
        /// Prefab for the Column3DFMRI
        /// </summary>
        [SerializeField] private GameObject m_Column3DFMRIPrefab;
        /// <summary>
        /// Prefab for the Column3DMEG
        /// </summary>
        [SerializeField] private GameObject m_Column3DMEGPrefab;
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
        [HideInInspector] public GenericEvent<Core.Object3D.Cut> OnAddCut = new GenericEvent<Core.Object3D.Cut>();
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
        [HideInInspector] public GenericEvent<ColorType> OnChangeColormap = new GenericEvent<ColorType>();
        /// <summary>
        /// Ask the camera manager to update the target for this scene
        /// </summary>
        [HideInInspector] public GenericEvent<Vector3> OnUpdateCameraTarget = new GenericEvent<Vector3>();
        /// <summary>
        /// Event called when site is clicked to dipslay additionnal infomation
        /// </summary>
        [HideInInspector] public GenericEvent<IEnumerable<Core.Object3D.Site>> OnRequestSiteInformation = new GenericEvent<IEnumerable<Core.Object3D.Site>>();
        /// <summary>
        /// Event called when requesting a graph from the filtered sites in the siteactions panel
        /// </summary>
        [HideInInspector] public GenericEvent<IEnumerable<Core.Object3D.Site>> OnRequestFilteredSitesGraph = new GenericEvent<IEnumerable<Core.Object3D.Site>>();
        /// <summary>
        /// Event called when ieeg are outdated or not anymore
        /// </summary>
        [HideInInspector] public GenericEvent<bool> OnIEEGOutdated = new GenericEvent<bool>();
        /// <summary>
        /// Event called when updating the ROI mask for this column
        /// </summary>
        [HideInInspector] public UnityEvent OnUpdateROI = new UnityEvent();
        /// <summary>
        /// Event called when minimizing a column
        /// </summary>
        [HideInInspector] public UnityEvent OnChangeColumnMinimizedState = new UnityEvent();
        /// <summary>
        /// Event called when selecting a source when viewing a CCEP column
        /// </summary>
        [HideInInspector] public UnityEvent OnSelectCCEPSource = new UnityEvent();
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
        [HideInInspector] public GenericEvent<Core.Object3D.Site> OnSelectSite = new GenericEvent<Core.Object3D.Site>();
        /// <summary>
        /// Event called when displaying the correlations
        /// </summary>
        [HideInInspector] public UnityEvent OnChangeDisplayCorrelations = new UnityEvent();
        /// <summary>
        /// Event called when changing the automatic cut around selected site toggle state
        /// </summary>
        [HideInInspector] public GenericEvent<bool> OnChangeAutomaticCutAroundSelectedSite = new GenericEvent<bool>();
        /// <summary>
        /// Event called when finished loading the scene completely
        /// </summary>
        [HideInInspector] public UnityEvent OnSceneCompletelyLoaded = new UnityEvent();
        /// <summary>
        /// Event called when starting or ending the update of the generators
        /// </summary>
        [HideInInspector] public GenericEvent<bool> OnUpdatingGenerators = new GenericEvent<bool>();
        #endregion

        #region Private Methods
        private void Update()
        {
            if (!SceneInformation.Initialized || m_DestroyRequested) return;

            if (!SceneInformation.CompletelyLoaded)
            {
                UpdateVisibleState(true);
                SceneInformation.CompletelyLoaded = true;
                OnSceneCompletelyLoaded.Invoke();
                if (Visualization.Configuration.FirstColumnToSelect < Columns.Count)
                {
                    Columns[Visualization.Configuration.FirstColumnToSelect].SelectFirstOrDefaultSiteByName(Visualization.Configuration.FirstSiteToSelect);
                }
            }

            if (m_UpdatingGenerators) return;
            if (SceneInformation.GeometryNeedsUpdate) UpdateGeometry();
            if (SceneInformation.CutsNeedUpdate) UpdateCuts();
            if (SceneInformation.BaseCutTexturesNeedUpdate) ComputeBaseCutTextures();
            if (SceneInformation.FunctionalCutTexturesNeedUpdate) ComputeFunctionalCutTextures();
            if (SceneInformation.GUICutTexturesNeedUpdate) ComputeGUICutTextures();
            if (SceneInformation.FunctionalSurfaceNeedsUpdate) ComputeFunctionalSurface();
            if (SceneInformation.SitesNeedUpdate) UpdateAllColumnsSitesRendering();
            if (!m_IsGeneratorUpToDate && (ApplicationState.UserPreferences.Visualization._3D.AutomaticEEGUpdate || SceneInformation.GeneratorUpdateRequested)) UpdateGenerator();
        }
        private void OnDestroy()
        {
            foreach (var dllMRIGeometryCutGenerator in CutGeometryGenerators) dllMRIGeometryCutGenerator.Dispose();
        }
        /// <summary>
        /// Compute the textures for the MRI (3D)
        /// </summary>
        private void ComputeBaseCutTextures()
        {
            UnityEngine.Profiling.Profiler.BeginSample("ComputeBaseCutTextures");
            foreach (Column3D column in Columns)
                foreach (Core.Object3D.Cut cut in Cuts)
                    column.CutTextures.CreateMRITexture(MRIManager.SelectedMRI.Volume, cut.ID, MRIManager.MRICalMinFactor, MRIManager.MRICalMaxFactor, 3);

            SceneInformation.BaseCutTexturesNeedUpdate = false;
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Compute the textures for the MRI (3D) with the iEEG activity
        /// </summary>
        /// <param name="column">Specific column to update. If null, every columns will be updated.</param>
        private void ComputeFunctionalCutTextures()
        {
            UnityEngine.Profiling.Profiler.BeginSample("ComputeFunctionalCutTextures");
            foreach (Column3D column in Columns)
            {
                if (m_AtlasManager.DisplayAtlas) m_AtlasManager.ColorCuts(column);
                else if (m_FMRIManager.DisplayFMRI) m_FMRIManager.ColorCuts(column);
                else if (m_IsGeneratorUpToDate) column.CutTextures.ColorCutsTexturesWithActivity();
            }
            SceneInformation.FunctionalCutTexturesNeedUpdate = false;
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Compute the texture for the MRI (GUI)
        /// </summary>
        private void ComputeGUICutTextures()
        {
            UnityEngine.Profiling.Profiler.BeginSample("ComputeGUICutTextures");
            Column3D column = SelectedColumn;
            if (column)
            {
                column.CutTextures.CreateGUIMRITextures(Cuts);
                column.CutTextures.UpdateTextures2D();
                foreach (Core.Object3D.Cut cut in Cuts)
                {
                    cut.OnUpdateGUITextures.Invoke();
                }
            }
            SceneInformation.GUICutTexturesNeedUpdate = false;
            UnityEngine.Profiling.Profiler.EndSample();
        }
        private void ComputeFunctionalSurface()
        {
            UnityEngine.Profiling.Profiler.BeginSample("ComputeFunctionalSurface");
            if (m_IsGeneratorUpToDate)
            {
                foreach (Column3D col in Columns)
                {
                    col.ComputeSurfaceBrainUVWithActivity();
                }
            }
            foreach (Column3D col in Columns)
            {
                if (col.SurfaceNeedsUpdate)
                {
                    if (!m_IsGeneratorUpToDate)
                    {
                        col.BrainMesh.GetComponent<MeshFilter>().mesh.uv2 = col.SurfaceGenerator.NullUV;
                        col.BrainMesh.GetComponent<MeshFilter>().mesh.uv3 = col.SurfaceGenerator.NullUV;
                    }
                    else
                    {
                        col.BrainMesh.GetComponent<MeshFilter>().mesh.uv2 = col.SurfaceGenerator.AlphaUV;
                        col.BrainMesh.GetComponent<MeshFilter>().mesh.uv3 = col.SurfaceGenerator.ActivityUV;
                    }
                }
                col.SurfaceNeedsUpdate = false;
            }
            SceneInformation.FunctionalSurfaceNeedsUpdate = false;
            UnityEngine.Profiling.Profiler.EndSample();
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
            for (int ii = 0; ii < ColumnsFMRI.Count; ++ii)
            {
                ColumnsFMRI[ii].Timeline.OnUpdateCurrentIndex.Invoke();
            }

            SceneInformation.FunctionalCutTexturesNeedUpdate = true;
            SceneInformation.FunctionalSurfaceNeedsUpdate = true;
            SceneInformation.SitesNeedUpdate = true;

            OnIEEGOutdated.Invoke(false);
        }
        /// <summary>
        /// Actions to perform after clicking on a site
        /// </summary>
        /// <param name="site">Site that has been clicked</param>
        private void ClickOnSiteCallback(Core.Object3D.Site site)
        {
            if ((SelectedColumn is Column3DDynamic || SelectedColumn is Column3DMEG) && site)
            {
                List<Core.Object3D.Site> sites = new List<Core.Object3D.Site>();
                if (ImplantationManager.SiteToCompare) sites.Add(ImplantationManager.SiteToCompare);
                sites.Add(site);
                if (SelectedColumn is Column3DCCEP ccepColumn)
                {
                    if (ccepColumn.IsSourceSiteSelected)
                    {
                        OnRequestSiteInformation.Invoke(sites);
                    }
                }
                else
                {
                    OnRequestSiteInformation.Invoke(sites);
                }
            }
            SceneInformation.SitesNeedUpdate = true;
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Compute the cuts of the meshes (compute the cuts meshes, fill parameters in the brain mesh shader and reset generators)
        /// </summary>
        private void ComputeMeshesCut()
        {
            if (MeshManager.BrainSurface == null) return;

            // Create the cuts
            UnityEngine.Profiling.Profiler.BeginSample("cut_generator Create cut");
            List<Core.DLL.Surface> generatedCutMeshes = new List<Core.DLL.Surface>(Cuts.Count);
            if (Cuts.Count > 0)
                generatedCutMeshes = MeshManager.BrainSurface.GenerateCutSurfaces(Cuts, false, StrongCuts);
            UnityEngine.Profiling.Profiler.EndSample();

            // Fill parameters in shader
            UnityEngine.Profiling.Profiler.BeginSample("cut_generator Fill shader");
            BrainMaterials.SetCuts(Cuts);
            UnityEngine.Profiling.Profiler.EndSample();

            // Update cut generators
            UnityEngine.Profiling.Profiler.BeginSample("cut_generator Update generators");
            for (int ii = 0; ii < Cuts.Count; ++ii)
            {
                CutGeometryGenerators[ii].Initialize(m_MRIManager.SelectedMRI.Volume, Cuts[ii], 1);
                CutGeometryGenerators[ii].UpdateSurfaceUV(generatedCutMeshes[ii]);
                generatedCutMeshes[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<MeshFilter>().mesh);
            }
            UnityEngine.Profiling.Profiler.EndSample();

            // Display cuts
            UnityEngine.Profiling.Profiler.BeginSample("cut_generator Misc");
            for (int ii = 0; ii < Cuts.Count; ++ii)
                m_DisplayedObjects.BrainCutMeshes[ii].SetActive(true);

            SceneInformation.CollidersNeedUpdate = true;

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
            UpdateGeneratorsAndUnityMeshes();
            m_TriangleEraser.ResetEraser();
            m_AtlasManager.UpdateAtlasIndices();
            m_FMRIManager.UpdateSurfaceFMRIValues();
            Resources.UnloadUnusedAssets();

            SceneInformation.GeometryNeedsUpdate = false;
        }
        /// <summary>
        /// Update the cuts of the scene
        /// </summary>
        private void UpdateCuts()
        {
            UnityEngine.Profiling.Profiler.BeginSample("UpdateCuts");
            CutAroundSelectedSite();
            ComputeMeshesCut();
            SceneInformation.CutsNeedUpdate = false;
            OnUpdateCuts.Invoke();
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Update the generators for activity and the UV of the meshes
        /// </summary>
        private void UpdateGeneratorsAndUnityMeshes()
        {
            m_GeneratorSurface?.Dispose();
            m_GeneratorSurface = new Core.DLL.GeneratorSurface();
            m_GeneratorSurface.Initialize(m_MeshManager.BrainSurface, m_MRIManager.SelectedMRI.Volume, 120);
            foreach (Column3D column in Columns)
            {
                column.ActivityGenerator.Initialize(m_GeneratorSurface);
                column.SurfaceGenerator.Initialize(column.ActivityGenerator);
                column.SurfaceGenerator.ComputeMainUV(m_MRIManager.MRICalMinFactor, m_MRIManager.MRICalMaxFactor);
                column.SurfaceGenerator.ComputeNullUV();
            }
            m_MeshManager.UpdateMeshesFromDLL();
        }
        /// <summary>
        /// Update the brain and the cuts meshes colliders
        /// </summary>
        private void UpdateMeshesColliders()
        {
            this.StartCoroutineAsync(c_UpdateMeshesColliders());
            SceneInformation.CollidersNeedUpdate = false;
        }
        /// <summary>
        /// Update the sites rendering for all columns
        /// </summary>
        private void UpdateAllColumnsSitesRendering()
        {
            foreach (Column3D column in Columns)
            {
                column.UpdateSitesRendering(m_ShowAllSites, m_HideBlacklistedSites, m_IsGeneratorUpToDate, m_SiteGain);
            }
            SceneInformation.SitesNeedUpdate = false;
            OnSitesRenderingUpdated.Invoke();
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
            while (CutGeometryGenerators.Count < nbCuts)
            {
                CutGeometryGenerators.Add(new Core.DLL.CutGeometryGenerator());
            }
            while (CutGeometryGenerators.Count > nbCuts)
            {
                CutGeometryGenerators.Last().Dispose();
                CutGeometryGenerators.RemoveAt(CutGeometryGenerators.Count - 1);
            }

            for (int c = 0; c < Columns.Count; c++)
            {
                Columns[c].UpdateCutsPlanesNumber(nbCuts, CutGeometryGenerators);
            }
        }
        /// <summary>
        /// Add a column to the scene
        /// </summary>
        /// <param name="type">Base data column</param>
        private void AddColumn(Core.Data.Column baseColumn)
        {
            Column3D column = null;
            if (baseColumn is Core.Data.AnatomicColumn)
            {
                column = Instantiate(m_Column3DAnatomyPrefab, m_ColumnsContainer).GetComponent<Column3D>();
            }
            else if (baseColumn is Core.Data.IEEGColumn)
            {
                column = Instantiate(m_Column3DIEEGPrefab, m_ColumnsContainer).GetComponent<Column3DIEEG>();
            }
            else if (baseColumn is Core.Data.CCEPColumn)
            {
                column = Instantiate(m_Column3DCCEPPrefab, m_ColumnsContainer).GetComponent<Column3DCCEP>();
            }
            else if (baseColumn is Core.Data.FMRIColumn)
            {
                column = Instantiate(m_Column3DFMRIPrefab, m_ColumnsContainer).GetComponent<Column3DFMRI>();
            }
            else if (baseColumn is Core.Data.MEGColumn)
            {
                column = Instantiate(m_Column3DMEGPrefab, m_ColumnsContainer).GetComponent<Column3DMEG>();
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
                SceneInformation.GUICutTexturesNeedUpdate = true;
                OnUpdateCuts.Invoke();
            });
            column.OnMoveView.AddListener((view) =>
            {
                SynchronizeViewsToReferenceView(view);
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
                if (m_AutomaticCutAroundSelectedSite) SceneInformation.CutsNeedUpdate = true;
                OnSelectSite.Invoke(site);
            });
            column.OnChangeSiteState.AddListener((site) =>
            {
                ResetGenerators(false);
            });
            column.OnUpdateActivityAlpha.AddListener(() =>
            {
                SceneInformation.FunctionalCutTexturesNeedUpdate = true;
                SceneInformation.FunctionalSurfaceNeedsUpdate = true;
                column.SurfaceNeedsUpdate = true;
            });
            if (column is Column3DAnatomy anatomyColumn)
            {
                anatomyColumn.AnatomyParameters.OnUpdateInfluenceDistance.AddListener(() =>
                {
                    ResetGenerators(false);
                });
            }
            else if (column is Column3DDynamic dynamicColumn)
            {
                dynamicColumn.DynamicParameters.OnUpdateSpanValues.AddListener(() =>
                {
                    ((Core.DLL.IEEGGenerator)dynamicColumn.ActivityGenerator).AdjustValues(dynamicColumn.DynamicParameters.Middle, dynamicColumn.DynamicParameters.SpanMin, dynamicColumn.DynamicParameters.SpanMax);
                    SceneInformation.FunctionalCutTexturesNeedUpdate = true;
                    SceneInformation.FunctionalSurfaceNeedsUpdate = true;
                    dynamicColumn.SurfaceNeedsUpdate = true;
                    SceneInformation.SitesNeedUpdate = true;
                });
                dynamicColumn.DynamicParameters.OnUpdateInfluenceDistance.AddListener(() =>
                {
                    ResetGenerators(false);
                });
                dynamicColumn.OnUpdateCurrentTimelineID.AddListener(() =>
                {
                    SceneInformation.FunctionalCutTexturesNeedUpdate = true;
                    SceneInformation.FunctionalSurfaceNeedsUpdate = true;
                    SceneInformation.SitesNeedUpdate = true;
                    dynamicColumn.SurfaceNeedsUpdate = true;
                });
                if (dynamicColumn is Column3DCCEP column3DCCEP)
                {
                    column3DCCEP.OnSelectSource.AddListener(() =>
                    {
                        ResetGenerators();
                        OnSelectCCEPSource.Invoke();
                        ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
                    });
                }
            }
            else if (column is Column3DFMRI fmriColumn)
            {
                fmriColumn.FMRIParameters.OnUpdateCalValues.AddListener(() =>
                {
                    ((Core.DLL.FMRIGenerator)fmriColumn.ActivityGenerator).AdjustValues(fmriColumn.FMRIParameters.FMRINegativeCalMinFactor, fmriColumn.FMRIParameters.FMRINegativeCalMaxFactor, fmriColumn.FMRIParameters.FMRIPositiveCalMinFactor, fmriColumn.FMRIParameters.FMRIPositiveCalMaxFactor);
                    SceneInformation.FunctionalCutTexturesNeedUpdate = true;
                    SceneInformation.FunctionalSurfaceNeedsUpdate = true;
                    fmriColumn.SurfaceNeedsUpdate = true;
                    SceneInformation.SitesNeedUpdate = true;
                });
                fmriColumn.FMRIParameters.OnUpdateHideValues.AddListener(() =>
                {
                    ((Core.DLL.FMRIGenerator)fmriColumn.ActivityGenerator).HideExtremeValues(fmriColumn.FMRIParameters.HideLowerValues, fmriColumn.FMRIParameters.HideMiddleValues, fmriColumn.FMRIParameters.HideHigherValues);
                    SceneInformation.FunctionalCutTexturesNeedUpdate = true;
                    SceneInformation.FunctionalSurfaceNeedsUpdate = true;
                    fmriColumn.SurfaceNeedsUpdate = true;
                    SceneInformation.SitesNeedUpdate = true;
                });
                fmriColumn.OnChangeSelectedFMRI.AddListener(() =>
                {
                    SceneInformation.FunctionalCutTexturesNeedUpdate = true;
                    SceneInformation.FunctionalSurfaceNeedsUpdate = true;
                    fmriColumn.SurfaceNeedsUpdate = true;
                    ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
                });
                fmriColumn.OnUpdateCurrentTimelineID.AddListener(() =>
                {
                    SceneInformation.FunctionalCutTexturesNeedUpdate = true;
                    SceneInformation.FunctionalSurfaceNeedsUpdate = true;
                    SceneInformation.SitesNeedUpdate = true;
                    fmriColumn.SurfaceNeedsUpdate = true;
                });
            }
            else if (column is Column3DMEG megColumn)
            {
                megColumn.MEGParameters.OnUpdateCalValues.AddListener(() =>
                {
                    ((Core.DLL.MEGGenerator)megColumn.ActivityGenerator).AdjustValues(megColumn.MEGParameters.FMRINegativeCalMinFactor, megColumn.MEGParameters.FMRINegativeCalMaxFactor, megColumn.MEGParameters.FMRIPositiveCalMinFactor, megColumn.MEGParameters.FMRIPositiveCalMaxFactor);
                    SceneInformation.FunctionalCutTexturesNeedUpdate = true;
                    SceneInformation.FunctionalSurfaceNeedsUpdate = true;
                    megColumn.SurfaceNeedsUpdate = true;
                    SceneInformation.SitesNeedUpdate = true;
                });
                megColumn.MEGParameters.OnUpdateHideValues.AddListener(() =>
                {
                    ((Core.DLL.MEGGenerator)megColumn.ActivityGenerator).HideExtremeValues(megColumn.MEGParameters.HideLowerValues, megColumn.MEGParameters.HideMiddleValues, megColumn.MEGParameters.HideHigherValues);
                    SceneInformation.FunctionalCutTexturesNeedUpdate = true;
                    SceneInformation.FunctionalSurfaceNeedsUpdate = true;
                    megColumn.SurfaceNeedsUpdate = true;
                    SceneInformation.SitesNeedUpdate = true;
                });
                megColumn.OnChangeSelectedMEG.AddListener(() =>
                {
                    SceneInformation.FunctionalCutTexturesNeedUpdate = true;
                    SceneInformation.FunctionalSurfaceNeedsUpdate = true;
                    megColumn.SurfaceNeedsUpdate = true;
                    ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
                });
                megColumn.OnUpdateCurrentTimelineID.AddListener(() =>
                {
                    SceneInformation.FunctionalCutTexturesNeedUpdate = true;
                    SceneInformation.FunctionalSurfaceNeedsUpdate = true;
                    SceneInformation.SitesNeedUpdate = true;
                    megColumn.SurfaceNeedsUpdate = true;
                });
            }
            column.Initialize(Columns.Count, baseColumn, m_ImplantationManager.SelectedImplantation, m_DisplayedObjects.SitesPatientParent);
            Columns.Add(column);
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
        public Core.Object3D.Cut AddCutPlane()
        {
            // Add new cut
            Core.Object3D.Cut cut = new Core.Object3D.Cut(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
            switch (Cuts.Count)
            {
                case 0:
                    cut.Orientation = CutOrientation.Axial;
                    cut.Flip = false;
                    cut.Position = 0.5f;
                    break;
                case 1:
                    cut.Orientation = CutOrientation.Coronal;
                    cut.Flip = false;
                    cut.Position = 0.5f;
                    break;
                case 2:
                    cut.Orientation = CutOrientation.Sagittal;
                    cut.Flip = false;
                    cut.Position = 0.5f;
                    break;
                default:
                    cut.Orientation = CutOrientation.Axial;
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

            SceneInformation.CutsNeedUpdate = true;

            OnAddCut.Invoke(cut);
            UpdateCutPlane(cut);

            return cut;
        }
        /// <summary>
        /// Remove a cut plane
        /// </summary>
        /// <param name="cut">Cut to be removed</param>
        public void RemoveCutPlane(Core.Object3D.Cut cut)
        {
            Cuts.Remove(cut);
            for (int i = 0; i < Cuts.Count; i++)
            {
                Cuts[i].ID = i;
            }

            Destroy(m_DisplayedObjects.BrainCutMeshes[cut.ID]);
            m_DisplayedObjects.BrainCutMeshes.RemoveAt(cut.ID);

            UpdateCutNumber(m_DisplayedObjects.BrainCutMeshes.Count);

            SceneInformation.CutsNeedUpdate = true;

            cut.OnRemoveCut.Invoke();
        }
        /// <summary>
        /// Update a cut plane
        /// </summary>
        /// <param name="cut">Cut to be updated</param>
        /// <param name="changedByUser">Has the cut been updated by the user or programatically ?</param>
        public void UpdateCutPlane(Core.Object3D.Cut cut, bool changedByUser = false)
        {
            if (cut.Orientation == CutOrientation.Custom)
            {
                if (cut.Normal.x == 0 && cut.Normal.y == 0 && cut.Normal.z == 0)
                {
                    cut.Normal = new Vector3(1, 0, 0);
                }
            }
            else
            {
                Core.Object3D.Plane plane = new Core.Object3D.Plane(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
                m_MRIManager.SelectedMRI.Volume.SetPlaneWithOrientation(plane, cut.Orientation, cut.Flip);
                cut.Normal = plane.Normal;
            }

            if (changedByUser) LastPlaneModifiedIndex = cut.ID;

            // Cuts base on the mesh
            float offset;
            if (MeshManager.BrainSurface != null)
            {
                Core.Object3D.Plane plane = new Core.Object3D.Plane(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
                m_MRIManager.SelectedMRI.Volume.SetPlaneWithOrientation(plane, cut.Orientation, false);
                offset = MeshManager.BrainSurface.SizeOffsetCutPlane(plane, cut.NumberOfCuts);
                offset *= 1.05f; // upsize a little bit the bbox for planes
            }
            else
                offset = 0.1f;

            cut.Point = MeshManager.MeshCenter + cut.Normal.normalized * (cut.Position - 0.5f) * offset * cut.NumberOfCuts;

            SceneInformation.CutsNeedUpdate = true;

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
            if (!m_AutomaticCutAroundSelectedSite) return;

            foreach (var cut in Cuts.ToList())
            {
                RemoveCutPlane(cut);
            }

            Core.Object3D.Site site = SelectedColumn.SelectedSite;
            if (!site) return;
            
            Vector3 sitePosition = new Vector3(-site.transform.localPosition.x, site.transform.localPosition.y, site.transform.localPosition.z);

            Core.Object3D.Cut axialCut = AddCutPlane();
            Vector3 axialPoint = MeshManager.MeshCenter + (Vector3.Dot(sitePosition - MeshManager.MeshCenter, axialCut.Normal) / Vector3.Dot(axialCut.Normal, axialCut.Normal)) * axialCut.Normal;
            float axialOffset = MeshManager.BrainSurface.SizeOffsetCutPlane(axialCut, axialCut.NumberOfCuts) * 1.05f;
            axialCut.Position = ((axialPoint.z - MeshManager.MeshCenter.z) / (axialCut.Normal.z * axialOffset * axialCut.NumberOfCuts)) + 0.5f;
            if (axialCut.Position < 0.5f)
            {
                axialCut.Flip = true;
                axialCut.Position = 1 - axialCut.Position;
            }
            UpdateCutPlane(axialCut);

            Core.Object3D.Cut coronalCut = AddCutPlane();
            Vector3 coronalPoint = MeshManager.MeshCenter + (Vector3.Dot(sitePosition - MeshManager.MeshCenter, coronalCut.Normal) / Vector3.Dot(coronalCut.Normal, coronalCut.Normal)) * coronalCut.Normal;
            float coronalOffset = MeshManager.BrainSurface.SizeOffsetCutPlane(coronalCut, coronalCut.NumberOfCuts) * 1.05f;
            coronalCut.Position = ((coronalPoint.y - MeshManager.MeshCenter.y) / (coronalCut.Normal.y * coronalOffset * coronalCut.NumberOfCuts)) + 0.5f;
            if (coronalCut.Position < 0.5f)
            {
                coronalCut.Flip = true;
                coronalCut.Position = 1 - coronalCut.Position;
            }
            UpdateCutPlane(coronalCut);

            Core.Object3D.Cut sagittalCut = AddCutPlane();
            Vector3 sagittalPoint = MeshManager.MeshCenter + (Vector3.Dot(sitePosition - MeshManager.MeshCenter, sagittalCut.Normal) / Vector3.Dot(sagittalCut.Normal, sagittalCut.Normal)) * sagittalCut.Normal;
            float sagittalOffset = MeshManager.BrainSurface.SizeOffsetCutPlane(sagittalCut, sagittalCut.NumberOfCuts) * 1.05f;
            sagittalCut.Position = ((sagittalPoint.x - MeshManager.MeshCenter.x) / (sagittalCut.Normal.x * sagittalOffset * sagittalCut.NumberOfCuts)) + 0.5f;
            if (sagittalCut.Position < 0.5f)
            {
                sagittalCut.Flip = true;
                sagittalCut.Position = 1 - sagittalCut.Position;
            }
            UpdateCutPlane(sagittalCut);
        }
        #endregion

        #region Save/Load
        /// <summary>
        /// Initialize the scene with the corresponding visualization
        /// </summary>
        /// <param name="visualization">Visualization to be loaded in this scene</param>
        public void Initialize(Core.Data.Visualization visualization)
        {
            BrainColorMapTexture = Texture2DExtension.Generate();
            BrainColorTexture = Texture2DExtension.Generate();

            Visualization = visualization;
            gameObject.name = Visualization.Name;
            
            BrainMaterials = new Core.Object3D.BrainMaterials();

            transform.position = new Vector3(HBP3DModule.SPACE_BETWEEN_SCENES_GAME_OBJECTS * ApplicationState.Module3D.NumberOfScenesLoadedSinceStart++, transform.position.y, transform.position.z);
        }
        /// <summary>
        /// Set up the scene to display it properly
        /// </summary>
        public void FinalizeInitialization()
        {
            Columns[0].Views[0].IsSelected = true; // Select default view
            Columns[0].SelectFirstOrDefaultSiteByName();
            SceneInformation.Initialized = true;
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
            Colormap = Visualization.Configuration.Colormap;
            m_MeshManager.SelectMeshPart(Visualization.Configuration.MeshPart);
            EdgeMode = Visualization.Configuration.ShowEdges;
            IsBrainTransparent = Visualization.Configuration.TransparentBrain;
            BrainMaterials.SetAlpha(Visualization.Configuration.BrainAlpha);
            StrongCuts = Visualization.Configuration.StrongCuts;
            HideBlacklistedSites = Visualization.Configuration.HideBlacklistedSites;
            ShowAllSites = Visualization.Configuration.ShowAllSites;
            AutomaticCutAroundSelectedSite = Visualization.Configuration.AutomaticCutAroundSelectedSite;
            SiteGain = Visualization.Configuration.SiteGain;
            m_MRIManager.SetCalValues(Visualization.Configuration.MRICalMinFactor, Visualization.Configuration.MRICalMaxFactor);
            CameraType = Visualization.Configuration.CameraType;

            if (!string.IsNullOrEmpty(Visualization.Configuration.MeshName)) m_MeshManager.Select(Visualization.Configuration.MeshName);
            if (!string.IsNullOrEmpty(Visualization.Configuration.MRIName)) m_MRIManager.Select(Visualization.Configuration.MRIName);
            if (!string.IsNullOrEmpty(Visualization.Configuration.ImplantationName)) m_ImplantationManager.Select(Visualization.Configuration.ImplantationName);

            foreach (Core.Data.Cut cut in Visualization.Configuration.Cuts)
            {
                Core.Object3D.Cut newCut = AddCutPlane();
                newCut.Normal = cut.Normal.ToVector3();
                newCut.Orientation = cut.Orientation;
                newCut.Flip = cut.Flip;
                newCut.Position = cut.Position;
                UpdateCutPlane(newCut);
            }

            for (int i = 0; i < Visualization.Configuration.Views.Count; i++)
            {
                Core.Data.View view = Visualization.Configuration.Views[i];
                if (i != 0)
                {
                    AddViewLine();
                }
                foreach (Column3D column in Columns)
                {
                    column.Views.Last().SetCamera(view.Position.ToVector3(), view.Rotation.ToQuaternion(), view.Target.ToVector3());
                }
            }

            m_ROIManager.LoadROIsFromConfiguration(Visualization.Configuration.RegionsOfInterest);

            foreach (Column3D column in Columns)
            {
                column.LoadConfiguration(false);
            }

            SceneInformation.SitesNeedUpdate = true;

            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Save the current settings of this scene to the configuration of the linked visualization
        /// </summary>
        public void SaveConfiguration()
        {
            Visualization.Configuration.BrainColor = BrainColor;
            Visualization.Configuration.BrainCutColor = CutColor;
            Visualization.Configuration.Colormap = Colormap;
            Visualization.Configuration.MeshPart = MeshManager.MeshPartToDisplay;
            Visualization.Configuration.MeshName = m_MeshManager.SelectedMesh.Name;
            Visualization.Configuration.MRIName = m_MRIManager.SelectedMRI.Name;
            Visualization.Configuration.ImplantationName = m_ImplantationManager.SelectedImplantation != null ? m_ImplantationManager.SelectedImplantation.Name : "";
            Visualization.Configuration.ShowEdges = EdgeMode;
            Visualization.Configuration.TransparentBrain = IsBrainTransparent;
            Visualization.Configuration.BrainAlpha = BrainMaterials.Alpha;
            Visualization.Configuration.StrongCuts = StrongCuts;
            Visualization.Configuration.HideBlacklistedSites = m_HideBlacklistedSites;
            Visualization.Configuration.ShowAllSites = ShowAllSites;
            Visualization.Configuration.AutomaticCutAroundSelectedSite = AutomaticCutAroundSelectedSite;
            Visualization.Configuration.SiteGain = SiteGain;
            Visualization.Configuration.MRICalMinFactor = m_MRIManager.MRICalMinFactor;
            Visualization.Configuration.MRICalMaxFactor = m_MRIManager.MRICalMaxFactor;
            Visualization.Configuration.CameraType = CameraType;

            List<Core.Data.Cut> cuts = new List<Core.Data.Cut>();
            foreach (Core.Object3D.Cut cut in Cuts)
            {
                cuts.Add(new Core.Data.Cut(cut.Normal, cut.Orientation, cut.Flip, cut.Position));
            }
            Visualization.Configuration.Cuts = cuts;

            List<Core.Data.View> views = new List<Core.Data.View>();
            if (Columns.Count > 0)
            {
                foreach (var view in Columns[0].Views)
                {
                    views.Add(new Core.Data.View(view.LocalCameraPosition, view.LocalCameraRotation, view.LocalCameraTarget));
                }
            }
            Visualization.Configuration.Views = views;

            List<Core.Data.RegionOfInterest> rois = new List<Core.Data.RegionOfInterest>();
            foreach (Core.Object3D.ROI roi in ROIManager.ROIs)
            {
                rois.Add(new Core.Data.RegionOfInterest(roi));
            }
            Visualization.Configuration.RegionsOfInterest = rois;

            foreach (Column3D column in Columns)
            {
                column.SaveConfiguration();
            }
        }
        /// <summary>
        /// Reset the settings of the loaded scene
        /// </summary>
        public void ResetConfiguration()
        {
            BrainColor = ColorType.BrainColor;
            CutColor = ColorType.Default;
            Colormap = ColorType.MatLab;
            m_MeshManager.SelectMeshPart(MeshPart.Both);
            EdgeMode = false;
            IsBrainTransparent = false;
            BrainMaterials.SetAlpha(0.2f);
            StrongCuts = false;
            HideBlacklistedSites = false;
            ShowAllSites = false;
            AutomaticCutAroundSelectedSite = false;
            SiteGain = 1.0f;
            m_MRIManager.SetCalValues(0, 1);
            CameraType = CameraControl.Trackball;

            switch (Type)
            {
                case SceneType.SinglePatient:
                    m_MeshManager.Select(ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedMeshInSinglePatientVisualization, true);
                    m_MRIManager.Select(ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedMRIInSinglePatientVisualization, true);
                    m_ImplantationManager.Select(ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedImplantationInSinglePatientVisualization);
                    break;
                case SceneType.MultiPatients:
                    m_MeshManager.Select(ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedMeshInMultiPatientsVisualization);
                    m_MRIManager.Select(ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedMRIInMultiPatientsVisualization);
                    m_ImplantationManager.Select(ApplicationState.UserPreferences.Visualization._3D.DefaultSelectedImplantationInMultiPatientsVisualization);
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

            m_ROIManager.Clear();

            foreach (Column3D column in Columns)
            {
                column.ResetConfiguration();
            }

            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Create all required folders and return the path to the folder used for export
        /// </summary>
        /// <returns>Folder that will contain exported files</returns>
        public string GenerateExportDirectory()
        {
            string result = ApplicationState.UserPreferences.General.Project.DefaultExportLocation;
            if (string.IsNullOrEmpty(result)) result = Path.GetFullPath(Application.dataPath + "/../Export/");
            if (!Directory.Exists(result)) Directory.CreateDirectory(result);
            result = Path.Combine(result, ApplicationState.ProjectLoaded.Preferences.Name);
            if (!Directory.Exists(result)) Directory.CreateDirectory(result);
            result = Path.Combine(result, Name);
            if (!Directory.Exists(result)) Directory.CreateDirectory(result);
            return result;
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
                foreach (Core.Object3D.Site site in column.Sites)
                {
                    site.State.ApplyState(selectedColumn.SiteStateBySiteID[site.Information.FullID]);
                }
            }
        }
        /// <summary>
        /// Update the data rendering for a column
        /// </summary>
        /// <param name="column">Column to be updated</param>
        public void UpdateColumnRendering(Column3D column)
        {
            for (int i = 0; i < Cuts.Count; ++i)
            {
                m_DisplayedObjects.BrainCutMeshes[i].GetComponent<Renderer>().material.mainTexture = column.CutTextures.BrainCutTextures[i];
            }
        }
        /// <summary>
        /// Update the textures generator for iEEG
        /// </summary>
        public void UpdateGenerator()
        {
            if (m_UpdatingGenerators || !CanComputeFunctionalValues)
                return;

            OnIEEGOutdated.Invoke(false);
            SceneInformation.GeneratorNeedsUpdate = false;
            IsGeneratorUpToDate = false;
            SceneInformation.GeneratorUpdateRequested = false;
            StartCoroutine(c_ComputeGenerators());
        }
        /// <summary>
        /// Function to be called everytime we want to reset IEEG
        /// </summary>
        /// <param name="hardReset">Do we need to hard reset (delete the activity on the brain) ?</param>
        public void ResetGenerators(bool hardReset = true)
        {
            SceneInformation.GeneratorNeedsUpdate = true;
            SceneInformation.SitesNeedUpdate = true;
            if (hardReset)
            {
                IsGeneratorUpToDate = false;
                SceneInformation.BaseCutTexturesNeedUpdate = true;
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
            if (SceneInformation.CollidersNeedUpdate) UpdateMeshesColliders();

            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(HBP3DModule.HIDDEN_MESHES_LAYER);
            layerMask |= 1 << LayerMask.NameToLayer(HBP3DModule.DEFAULT_MESHES_LAYER);

            RaycastHitResult raycastResult = column.Raycast(ray, layerMask, out RaycastHit hit);
            Vector3 hitPoint = raycastResult != RaycastHitResult.None ? hit.point - transform.position : Vector3.zero;

            m_AtlasManager.DisplayAtlasInformation(raycastResult == RaycastHitResult.Cut || raycastResult == RaycastHitResult.Mesh, hitPoint);
            m_ImplantationManager.DisplaySiteInformation(raycastResult == RaycastHitResult.Site, column, hit);
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

            RaycastHitResult raycastResult = SelectedColumn.Raycast(ray, layerMask, out RaycastHit hit);
            Vector3 hitPoint = raycastResult != RaycastHitResult.None ? hit.point - transform.position : Vector3.zero;

            if (raycastResult == RaycastHitResult.Site)
            {
                SelectedColumn.Sites[hit.collider.gameObject.GetComponent<Core.Object3D.Site>().Information.Index].IsSelected = true;
            }
            else
            {
                SelectedColumn.UnselectSite();
            }

            if (raycastResult == RaycastHitResult.Mesh)
            {
                if (m_TriangleEraser.IsEnabled && m_TriangleEraser.IsClickAvailable)
                {
                    m_TriangleEraser.EraseTriangles(ray.direction, hitPoint);
                }
            }

            if (m_ROIManager.ROICreationMode)
            {
                Core.Object3D.ROI selectedROI = m_ROIManager.SelectedROI;
                if (selectedROI)
                {
                    if (raycastResult == RaycastHitResult.ROI)
                    {
                        selectedROI.SelectClosestSphere(ray);
                    }
                    else if (raycastResult == RaycastHitResult.Mesh || raycastResult == RaycastHitResult.Cut)
                    {
                        selectedROI.AddSphere(HBP3DModule.DEFAULT_MESHES_LAYER, "Sphere", hitPoint, 5.0f);
                        SceneInformation.SitesNeedUpdate = true;
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
        public IEnumerator c_Initialize(Core.Data.Visualization visualization, Action<float, float, LoadingText> onChangeProgress, Action<Exception> outPut)
        {
            Exception exception = null;

            // Compute progress variables
            float progress = 0f;
            float totalProgress = 0, loadingMeshProgress = 0, loadingMeshTime = 0, loadingMRIProgress = 0, loadingMRITime = 0, loadingImplantationsProgress = 0, loadingImplantationsTime = 0, loadingMNIProgress = 0, loadingMNITime = 0, loadingIEEGProgress = 0, loadingIEEGTime = 0;
            if (Type == SceneType.SinglePatient)
            {
                totalProgress = Visualization.Patients[0].Meshes.Count * LOADING_MESH_WEIGHT + Visualization.Patients[0].MRIs.Count * LOADING_MRI_WEIGHT + LOADING_IMPLANTATIONS_WEIGHT + LOADING_MNI_WEIGHT + LOADING_IEEG_WEIGHT;
                loadingMeshProgress = LOADING_MESH_WEIGHT / totalProgress;
                loadingMeshTime = LOADING_MESH_WEIGHT / 1000.0f;
                loadingMRIProgress = LOADING_MRI_WEIGHT / totalProgress;
                loadingMRITime = LOADING_MRI_WEIGHT / 1000.0f;
                loadingImplantationsProgress = LOADING_IMPLANTATIONS_WEIGHT / totalProgress;
                loadingImplantationsTime = LOADING_IMPLANTATIONS_WEIGHT / 1000.0f;
                loadingMNIProgress = LOADING_MNI_WEIGHT / totalProgress;
                loadingMNITime = LOADING_MNI_WEIGHT / 1000.0f;
                loadingIEEGProgress = LOADING_IEEG_WEIGHT / totalProgress;
                loadingIEEGTime = LOADING_IEEG_WEIGHT / 1000.0f;
            }
            else
            {
                totalProgress = LOADING_IMPLANTATIONS_WEIGHT + LOADING_MNI_WEIGHT + Visualization.Patients.Count * LOADING_IEEG_WEIGHT;
                if (ApplicationState.UserPreferences.Data.Anatomic.PreloadSinglePatientDataInMultiPatientVisualization)
                {
                    totalProgress += Visualization.Patients.Sum(p => p.Meshes.Count) * LOADING_MESH_WEIGHT + Visualization.Patients.Sum(p => p.MRIs.Count) * LOADING_MRI_WEIGHT;
                    loadingMeshProgress = LOADING_MESH_WEIGHT / totalProgress;
                    loadingMeshTime = LOADING_MESH_WEIGHT / 1000.0f;
                    loadingMRIProgress = LOADING_MRI_WEIGHT / totalProgress;
                    loadingMRITime = LOADING_MRI_WEIGHT / 1000.0f;
                }
                loadingImplantationsProgress = (Visualization.Patients.Count * LOADING_IMPLANTATIONS_WEIGHT) / totalProgress;
                loadingImplantationsTime = (Visualization.Patients.Count * LOADING_IMPLANTATIONS_WEIGHT) / 1000.0f;
                loadingMNIProgress = LOADING_MNI_WEIGHT / totalProgress;
                loadingMNITime = LOADING_MNI_WEIGHT / 1000.0f;
                loadingIEEGProgress = (Visualization.Patients.Count * LOADING_IEEG_WEIGHT) / totalProgress;
                loadingIEEGTime = (Visualization.Patients.Count * LOADING_IEEG_WEIGHT) / 1000.0f;
            }
            yield return Ninja.JumpToUnity;
            onChangeProgress(progress, 0.0f, new LoadingText());

            // Checking MNI
            onChangeProgress(progress, 0.0f, new LoadingText("Loading MNI"));
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
            if (Type == SceneType.SinglePatient)
            {
                if (ApplicationState.UserPreferences.Data.Anatomic.PreloadSinglePatientDataInMultiPatientVisualization && Visualization.Configuration.PreloadedMeshes.Count > 0)
                {
                    foreach (var mesh in Visualization.Configuration.PreloadedMeshes)
                    {
                        m_MeshManager.Meshes.Add(mesh);
                        progress += loadingMeshProgress;
                        onChangeProgress.Invoke(progress, loadingMeshTime, new LoadingText("Adding Preloaded Meshes"));
                    }
                }
                else
                {
                    for (int i = 0; i < Visualization.Patients[0].Meshes.Count; ++i)
                    {
                        Core.Data.BaseMesh mesh = Visualization.Patients[0].Meshes[i];
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
            }
            else if (ApplicationState.UserPreferences.Data.Anatomic.PreloadSinglePatientDataInMultiPatientVisualization)
            {
                foreach (var patient in Visualization.Patients)
                {
                    for (int i = 0; i < patient.Meshes.Count; ++i)
                    {
                        Core.Data.BaseMesh mesh = patient.Meshes[i];
                        progress += loadingMeshProgress;
                        onChangeProgress.Invoke(progress, loadingMeshTime, new LoadingText("Loading Mesh ", string.Format("{0} ({1})", mesh.Name, patient.Name), " [" + (i + 1).ToString() + "/" + patient.Meshes.Count + "]"));
                        yield return Ninja.JumpBack;
                        m_MeshManager.AddPreloaded(mesh, patient);
                        yield return Ninja.JumpToUnity;
                    }
                    if (exception != null)
                    {
                        outPut(exception);
                        yield break;
                    }
                }
            }
            m_MeshManager.InitializeMeshes();

            // Loading MRIs
            if (Type == SceneType.SinglePatient)
            {
                if (ApplicationState.UserPreferences.Data.Anatomic.PreloadSinglePatientDataInMultiPatientVisualization && Visualization.Configuration.PreloadedMRIs.Count > 0)
                {
                    foreach (var mri in Visualization.Configuration.PreloadedMRIs)
                    {
                        m_MRIManager.MRIs.Add(mri);
                        progress += loadingMRIProgress;
                        onChangeProgress.Invoke(progress, loadingMRITime, new LoadingText("Adding Preloaded MRIs"));
                    }
                }
                else
                {
                    for (int i = 0; i < Visualization.Patients[0].MRIs.Count; ++i)
                    {
                        Core.Data.MRI mri = Visualization.Patients[0].MRIs[i];
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
            }
            else if (ApplicationState.UserPreferences.Data.Anatomic.PreloadSinglePatientDataInMultiPatientVisualization)
            {
                foreach (var patient in Visualization.Patients)
                {
                    for (int i = 0; i < patient.MRIs.Count; ++i)
                    {
                        Core.Data.MRI mri = patient.MRIs[i];
                        progress += loadingMeshProgress;
                        onChangeProgress.Invoke(progress, loadingMRITime, new LoadingText("Loading MRI ", string.Format("{0} ({1})", mri.Name, patient.Name), " [" + (i + 1).ToString() + "/" + patient.MRIs.Count + "]"));
                        yield return Ninja.JumpBack;
                        m_MRIManager.AddPreloaded(mri, patient);
                        yield return Ninja.JumpToUnity;
                    }
                    if (exception != null)
                    {
                        outPut(exception);
                        yield break;
                    }
                }
            }

            // Loading Sites
            progress += loadingImplantationsProgress;
            onChangeProgress.Invoke(progress, loadingImplantationsTime, new LoadingText("Loading implantations"));
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadSites(visualization.Patients.ToArray(), e => exception = e));
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
                column.InitializeColumnMeshes(m_DisplayedObjects.Brain);
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
        private IEnumerator c_LoadBrainVolume(Core.Data.MRI mri, Action<Exception> outPut)
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
        private IEnumerator c_LoadBrainSurface(Core.Data.BaseMesh mesh, Action<Exception> outPut)
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
        private IEnumerator c_LoadSites(IEnumerable<Core.Data.Patient> patients, Action<Exception> outPut)
        {
            Dictionary<string, List<Core.Object3D.Implantation3D.SiteInfo>> siteInfoByImplantation = new Dictionary<string, List<Core.Object3D.Implantation3D.SiteInfo>>();
            int patientIndex = 0;
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^([a-zA-Z']+)([0-9]+)$");
            foreach (var patient in patients)
            {
                int siteIndex = 0;
                foreach (var site in patient.Sites)
                {
                    foreach (var coordinate in site.Coordinates)
                    {
                        if (!siteInfoByImplantation.TryGetValue(coordinate.ReferenceSystem, out List<Core.Object3D.Implantation3D.SiteInfo> siteInfos))
                        {
                            siteInfos = new List<Core.Object3D.Implantation3D.SiteInfo>();
                            siteInfoByImplantation.Add(coordinate.ReferenceSystem, siteInfos);
                        }
                        System.Text.RegularExpressions.GroupCollection groups = regex.Match(site.Name).Groups;
                        Core.Object3D.Implantation3D.SiteInfo siteInfo = new Core.Object3D.Implantation3D.SiteInfo()
                        {
                            Name = site.Name,
                            Position = coordinate.Position.ToVector3(),
                            Patient = patient,
                            Electrode = groups.Count == 3 ? groups[1].ToString() : "Other",
                            PatientIndex = patientIndex,
                            Index = siteIndex,
                            SiteData = site
                        };
                        siteInfos.Add(siteInfo);
                    }
                    siteIndex++;
                }
                patientIndex++;
            }

            foreach (var kv in siteInfoByImplantation)
            {
                m_ImplantationManager.Add(kv.Key, kv.Value, patients);
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
                m_MeshManager.Meshes.Add((Core.Object3D.LeftRightMesh3D)(ApplicationState.Module3D.MNIObjects.GreyMatter.Clone()));
                m_MeshManager.Meshes.Add((Core.Object3D.LeftRightMesh3D)(ApplicationState.Module3D.MNIObjects.WhiteMatter.Clone()));
                m_MeshManager.Meshes.Add((Core.Object3D.LeftRightMesh3D)(ApplicationState.Module3D.MNIObjects.InflatedWhiteMatter.Clone()));
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
                foreach (Core.Data.Column column in Visualization.Columns)
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
                foreach (var column in Columns)
                {
                    column.ComputeActivityData();
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
            m_UpdatingGenerators = true;
            OnUpdatingGenerators.Invoke(true);
            yield return this.StartCoroutineAsync(c_LoadActivity());
            m_UpdatingGenerators = false;
            OnUpdatingGenerators.Invoke(false);

            if (!SceneInformation.GeneratorNeedsUpdate) FinalizeGeneratorsComputing();
        }
        /// <summary>
        /// Compute the iEEG values on the brain
        /// </summary>
        /// <returns>Coroutine return</returns>
        private IEnumerator c_LoadActivity()
        {
            Core.DLL.ActivityGenerator currentGenerator = null;
            string currentMessage = "";
            int currentColumn = 0;
            int numberOfColumns = Columns.Count;
            IEnumerator checkProgress()
            {
                while(true)
                {
                    if (SceneInformation.GeneratorNeedsUpdate) yield break;
                    float currentProgress = 0;
                    if (currentGenerator != null)
                    {
                        currentProgress = ((float)currentColumn / numberOfColumns) + (currentGenerator.Progress / numberOfColumns);
                    }
                    OnProgressUpdateGenerator.Invoke(currentProgress, currentMessage, 0.05f);
                    yield return new WaitForSeconds(0.05f);
                }
            }
            yield return Ninja.JumpToUnity;
            Coroutine coroutine = this.StartCoroutineAsync(checkProgress());
            yield return Ninja.JumpBack;

            currentMessage = "Initializing";
            for (int i = 0; i < Columns.Count; i++)
            {
                Column3D column = Columns[i];
                currentColumn = i;
                currentMessage = "Loading " + column.Name;
                column.UpdateDLLSitesMask(m_ROIManager.SelectedROI != null);
                if (SceneInformation.GeneratorNeedsUpdate) yield break;
                if (column is Column3DAnatomy anatomyColumn)
                {
                    Core.DLL.DensityGenerator generator = anatomyColumn.ActivityGenerator as Core.DLL.DensityGenerator;
                    currentGenerator = generator;
                    generator.ComputeActivity(anatomyColumn.RawElectrodes, anatomyColumn.AnatomyParameters.InfluenceDistance, ApplicationState.UserPreferences.Visualization._3D.SiteInfluenceByDistance);
                }
                else if (column is Column3DDynamic dynamicColumn)
                {
                    Core.DLL.IEEGGenerator generator = dynamicColumn.ActivityGenerator as Core.DLL.IEEGGenerator;
                    currentGenerator = generator;
                    if (dynamicColumn is Column3DCCEP ccepColumn && ccepColumn.IsSourceMarsAtlasLabelSelected)
                        generator.ComputeActivityAtlas(ccepColumn.ActivityValues, ccepColumn.Timeline.Length, ccepColumn.AreaMask, ApplicationState.Module3D.MarsAtlas);
                    else
                        generator.ComputeActivity(dynamicColumn.RawElectrodes, dynamicColumn.DynamicParameters.InfluenceDistance, dynamicColumn.ActivityValues, dynamicColumn.Timeline.Length, dynamicColumn.RawElectrodes.NumberOfSites, ApplicationState.UserPreferences.Visualization._3D.SiteInfluenceByDistance);
                    generator.AdjustValues(dynamicColumn.DynamicParameters.Middle, dynamicColumn.DynamicParameters.SpanMin, dynamicColumn.DynamicParameters.SpanMax);
                }
                else if (column is Column3DFMRI fmriColumn)
                {
                    Core.DLL.FMRIGenerator generator = fmriColumn.ActivityGenerator as Core.DLL.FMRIGenerator;
                    currentGenerator = generator;
                    generator.ComputeActivity(fmriColumn.ColumnFMRIData.Data.FMRIs.SelectMany(fmri => fmri.Item1.Volumes));
                    generator.AdjustValues(fmriColumn.FMRIParameters.FMRINegativeCalMinFactor, fmriColumn.FMRIParameters.FMRINegativeCalMaxFactor, fmriColumn.FMRIParameters.FMRIPositiveCalMinFactor, fmriColumn.FMRIParameters.FMRIPositiveCalMaxFactor);
                }
                else if (column is Column3DMEG megColumn)
                {
                    Core.DLL.MEGGenerator generator = megColumn.ActivityGenerator as Core.DLL.MEGGenerator;
                    currentGenerator = generator;
                    generator.ComputeActivity(megColumn.ColumnMEGData.Data.MEGItems.SelectMany(fmri => fmri.FMRI.Volumes));
                    generator.AdjustValues(megColumn.MEGParameters.FMRINegativeCalMinFactor, megColumn.MEGParameters.FMRINegativeCalMaxFactor, megColumn.MEGParameters.FMRIPositiveCalMinFactor, megColumn.MEGParameters.FMRIPositiveCalMaxFactor);
                }
                if (SceneInformation.GeneratorNeedsUpdate) yield break;
            }

            currentMessage = "Finalizing";
            yield return Ninja.JumpToUnity;
            StopCoroutine(coroutine);
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
            List<Core.DLL.Surface> cuts;
            if (Cuts.Count > 0) cuts = new List<Core.DLL.Surface>(MeshManager.SimplifiedMeshToUse.Cut(Cuts.ToArray(), false, StrongCuts));
            else cuts = new List<Core.DLL.Surface>() { (Core.DLL.Surface)MeshManager.SimplifiedMeshToUse.Clone() };
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
            SceneInformation.GeneratorNeedsUpdate = true;
            m_DestroyRequested = true;
            yield return new WaitUntil(delegate { return !m_UpdatingGenerators; });
            Visualization.Unload();
            Destroy(gameObject);
            Resources.UnloadUnusedAssets();
            // Clean Meshes
            foreach (var mesh in m_MeshManager.Meshes)
            {
                if (mesh.HasBeenLoadedOutside) continue;
                if (ApplicationState.Module3D.Scenes.Any(s => s.MeshManager.Meshes.Contains(mesh))) continue;
                if (ApplicationState.Module3D.Scenes.Any(s => s.MeshManager.PreloadedMeshes.Values.SelectMany(pm => pm).Contains(mesh))) continue;
                mesh.Clean();
            }
            foreach (var mesh in m_MeshManager.PreloadedMeshes.Values.SelectMany(pm => pm))
            {
                if (mesh.HasBeenLoadedOutside) continue;
                if (ApplicationState.Module3D.Scenes.Any(s => s.MeshManager.Meshes.Contains(mesh))) continue;
                if (ApplicationState.Module3D.Scenes.Any(s => s.MeshManager.PreloadedMeshes.Values.SelectMany(pm => pm).Contains(mesh))) continue;
                mesh.Clean();
            }
            // Clean MRIs
            foreach (var mri in m_MRIManager.MRIs)
            {
                if (mri.HasBeenLoadedOutside) continue;
                if (ApplicationState.Module3D.Scenes.Any(s => s.MRIManager.MRIs.Contains(mri))) continue;
                if (ApplicationState.Module3D.Scenes.Any(s => s.MRIManager.PreloadedMRIs.Values.SelectMany(pm => pm).Contains(mri))) continue;
                mri.Clean();
            }
            foreach (var mri in m_MRIManager.PreloadedMRIs.Values.SelectMany(pm => pm))
            {
                if (mri.HasBeenLoadedOutside) continue;
                if (ApplicationState.Module3D.Scenes.Any(s => s.MRIManager.MRIs.Contains(mri))) continue;
                if (ApplicationState.Module3D.Scenes.Any(s => s.MRIManager.PreloadedMRIs.Values.SelectMany(pm => pm).Contains(mri))) continue;
                mri.Clean();
            }
        }
        #endregion
    }

    public class SceneInformation
    {
        #region Properties
        /// <summary>
        /// Is the scene initialized (loading is finished but displaying may not be finished) ?
        /// </summary>
        public bool Initialized { get; set; }
        private bool m_GeometryNeedsUpdate;
        /// <summary>
        /// Does the mesh need a geometry update (changing vertices, computing cuts etc.)
        /// </summary>
        public bool GeometryNeedsUpdate
        {
            get
            {
                return m_GeometryNeedsUpdate;
            }
            set
            {
                m_GeometryNeedsUpdate = value;
                if (value) CutsNeedUpdate = true;
            }
        }
        private bool m_CutsNeedUpdate;
        public bool CutsNeedUpdate
        {
            get
            {
                return m_CutsNeedUpdate;
            }
            set
            {
                m_CutsNeedUpdate = value;
                if (value) BaseCutTexturesNeedUpdate = true;
            }
        }
        private bool m_BaseCutTexturesNeedUpdate;
        public bool BaseCutTexturesNeedUpdate
        {
            get
            {
                return m_BaseCutTexturesNeedUpdate;
            }
            set
            {
                m_BaseCutTexturesNeedUpdate = value;
                if (value) FunctionalCutTexturesNeedUpdate = true;
            }
        }
        private bool m_FunctionalCutTexturesNeedUpdate;
        public bool FunctionalCutTexturesNeedUpdate
        {
            get
            {
                return m_FunctionalCutTexturesNeedUpdate;
            }
            set
            {
                m_FunctionalCutTexturesNeedUpdate = value;
                if (value) GUICutTexturesNeedUpdate = true;
            }
        }
        private bool m_GUICutTexturesNeedUpdate;
        public bool GUICutTexturesNeedUpdate
        {
            get
            {
                return m_GUICutTexturesNeedUpdate;
            }
            set
            {
                m_GUICutTexturesNeedUpdate = value;
            }
        }
        private bool m_FunctionalSurfaceNeedsUpdate;
        public bool FunctionalSurfaceNeedsUpdate
        {
            get
            {
                return m_FunctionalSurfaceNeedsUpdate;
            }
            set
            {
                m_FunctionalSurfaceNeedsUpdate = value;
            }
        }
        private bool m_SitesNeedUpdate;
        public bool SitesNeedUpdate
        {
            get
            {
                return m_SitesNeedUpdate;
            }
            set
            {
                m_SitesNeedUpdate = value;
            }
        }
        private bool m_GeneratorNeedsUpdate;
        public bool GeneratorNeedsUpdate
        {
            get
            {
                return m_GeneratorNeedsUpdate;
            }
            set
            {
                m_GeneratorNeedsUpdate = value;
            }
        }
        private bool m_GeneratorUpdateRequested;
        public bool GeneratorUpdateRequested
        {
            get
            {
                return m_GeneratorUpdateRequested;
            }
            set
            {
                m_GeneratorUpdateRequested = value;
            }
        }
        private bool m_CollidersNeedUpdate;
        public bool CollidersNeedUpdate
        {
            get
            {
                return m_CollidersNeedUpdate;
            }
            set
            {
                m_CollidersNeedUpdate = value;
            }
        }
        public bool CompletelyLoaded { get; set; }
        #endregion
    }
}