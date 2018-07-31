
/**
 * \file    Base3DScene.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Base3DScene and ComputeGeneratorsJob classes
 */

// system
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

// unity
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using System.Collections;
using CielaSpike;
using HBP.Data.Visualization;

namespace HBP.Module3D
{
    /// <summary>
    /// Class containing all the DLL data and the displayable Gameobjects of a 3D scene.
    /// </summary>
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
                bool wasSelected = m_IsSelected;
                m_IsSelected = value;
                OnChangeSelectedState.Invoke(value);
                if (m_IsSelected && !wasSelected)
                {
                    ApplicationState.Module3D.OnSelectScene.Invoke(this);
                }
            }
        }

        /// <summary>
        /// List of the patients in this scene
        /// </summary>
        public ReadOnlyCollection<Data.Patient> Patients
        {
            get
            {
                return new ReadOnlyCollection<Data.Patient>(Visualization.Patients);
            }
        }

        /// <summary>
        /// Visualization associated to this scene
        /// </summary>
        public Visualization Visualization { get; private set; }

        private List<Cut> m_Cuts = new List<Cut>();
        /// <summary>
        /// Cuts planes list
        /// </summary>
        public List<Cut> Cuts
        {
            get
            {
                return m_Cuts;
            }
            private set
            {
                m_Cuts = value;
            }
        }

        /// <summary>
        /// Information about the scene
        /// </summary>
        public SceneStatesInfo SceneInformation { get; set; }

        /// <summary>
        /// Displayable objects of the scene
        /// </summary>
        [SerializeField] private DisplayedObjects m_DisplayedObjects;

        [SerializeField] private Column3DManager m_ColumnManager;
        /// <summary>
        /// Column data manager
        /// </summary>
        public Column3DManager ColumnManager { get { return m_ColumnManager; } }

        /// <summary>
        /// Are cut holes in MRI enabled ?
        /// </summary>
        public bool CutHolesEnabled
        {
            get
            {
                return SceneInformation.CutHolesEnabled;
            }
            set
            {
                SceneInformation.CutHolesEnabled = value;
                SceneInformation.MeshGeometryNeedsUpdate = true;
                ResetIEEG();
                foreach (Column3D column in m_ColumnManager.Columns)
                {
                    column.IsRenderingUpToDate = false;
                }
            }
        }
        /// <summary>
        /// Is the latency mode enabled ?
        /// </summary>
        public bool IsLatencyModeEnabled
        {
            get
            {
                return SceneInformation.DisplayCCEPMode;
            }
            set
            {
                SceneInformation.DisplayCCEPMode = value;
                if (value)
                {
                    foreach (Column3D column in m_ColumnManager.Columns)
                    {
                        if (column.CurrentLatencyFile == -1)
                        {
                            column.CurrentLatencyFile = 0;
                        }
                    }
                }
                SceneInformation.AreSitesUpdated = false;
                ResetIEEG();
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            }
        }
        /// <summary>
        /// Are the blacklisted sites hidden ?
        /// </summary>
        public bool HideBlacklistedSites
        {
            get
            {
                return SceneInformation.HideBlacklistedSites;
            }
            set
            {
                SceneInformation.HideBlacklistedSites = value;
                SceneInformation.AreSitesUpdated = false;
            }
        }
        /// <summary>
        /// Are all sites shown ?
        /// </summary>
        public bool ShowAllSites
        {
            get
            {
                return SceneInformation.ShowAllSites;
            }
            set
            {
                SceneInformation.ShowAllSites = value;
                foreach (var column in ColumnManager.Columns)
                {
                    UpdateCurrentRegionOfInterest(column);
                }
                SceneInformation.AreSitesUpdated = false;
            }
        }

        /// <summary>
        /// Handles triangle erasing
        /// </summary>
        private TriEraser m_TriEraser = new TriEraser();
        /// <summary>
        /// Is the triangle eraser enabled ?
        /// </summary>
        public bool IsTriangleErasingEnabled
        {
            get
            {
                return m_TriEraser.IsEnabled;
            }
            set
            {
                m_TriEraser.IsEnabled = value;
            }
        }
        /// <summary>
        /// Can we cancel the last action ?
        /// </summary>
        public bool CanCancelLastTriangleErasingAction
        {
            get
            {
                return m_TriEraser.CanCancelLastAction;
            }
        }
        /// <summary>
        /// Does the brain mesh have invisible parts on ?
        /// </summary>
        public bool HasInvisibleTriangles
        {
            get
            {
                return m_TriEraser.MeshHasInvisibleTriangles;
            }
        }
        /// <summary>
        /// Mode of the triangle eraser
        /// </summary>
        public TriEraser.Mode TriangleErasingMode
        {
            get
            {
                return m_TriEraser.CurrentMode;
            }
            set
            {
                TriEraser.Mode previousMode = m_TriEraser.CurrentMode;
                m_TriEraser.CurrentMode = value;

                if (value == TriEraser.Mode.Expand || value == TriEraser.Mode.Invert)
                {
                    m_TriEraser.EraseTriangles(new Vector3(), new Vector3());
                    UpdateMeshesFromDLL();
                    m_TriEraser.CurrentMode = previousMode;
                }

                m_TriEraser.OnModifyInvisiblePart.Invoke();
            }
        }
        /// <summary>
        /// Degrees limit when selecting a zone with the triangle eraser
        /// </summary>
        public int TriangleErasingZoneDegrees
        {
            get
            {
                return m_TriEraser.Degrees;
            }
            set
            {
                m_TriEraser.Degrees = value;
            }
        }

        /// <summary>
        /// Are Mars Atlas colors displayed ?
        /// </summary>
        public bool IsMarsAtlasEnabled
        {
            get
            {
                return SceneInformation.MarsAtlasModeEnabled;
            }
            set
            {
                SceneInformation.MarsAtlasModeEnabled = value;
                m_DisplayedObjects.BrainSurfaceMeshes[0].GetComponent<Renderer>().sharedMaterial.SetInt("_MarsAtlas", SceneInformation.MarsAtlasModeEnabled ? 1 : 0);
            }
        }

        public bool m_EdgeMode = false;
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
                foreach (Column3D column in m_ColumnManager.Columns)
                {
                    foreach (View3D view in column.Views)
                    {
                        view.EdgeMode = m_EdgeMode;
                    }
                }
            }
        }

        private bool m_StrongCuts = false;
        /// <summary>
        /// Are we using strong cuts or soft cuts ?
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
                ResetIEEG();
                SceneInformation.MeshGeometryNeedsUpdate = true;
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
                foreach (Column3D column in m_ColumnManager.Columns)
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
                foreach (Column3D column in m_ColumnManager.Columns)
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
        /// Camera Control type
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
                foreach (Column3D column in m_ColumnManager.Columns)
                {
                    foreach (View3D view in column.Views)
                    {
                        view.CameraType = m_CameraType;
                    }
                }
            }
        }

        private bool m_CuttingSimplifiedMesh;
        /// <summary>
        /// Are we cutting the mesh of this scene ?
        /// </summary>
        public bool CuttingSimplifiedMesh
        {
            get
            {
                return m_CuttingSimplifiedMesh;
            }
            set
            {
                if (m_CuttingSimplifiedMesh != value)
                {
                    m_CuttingSimplifiedMesh = value;
                    SceneInformation.MeshGeometryNeedsUpdate = true;
                }
            }
        }

        /// <summary>
        /// Site to compare with when using the comparing site feature
        /// </summary>
        private Site m_SiteToCompare = null;
        private bool m_ComparingSites;
        /// <summary>
        /// Are we comparing sites ?
        /// </summary>
        public bool ComparingSites
        {
            get
            {
                return m_ComparingSites;
            }
            set
            {
                m_ComparingSites = value;
                if (m_ComparingSites)
                {
                    m_SiteToCompare = ColumnManager.SelectedColumn.SelectedSite;
                }
                else
                {
                    m_SiteToCompare = null;
                }
            }
        }

        /// <summary>
        /// Ambient mode (rendering)
        /// </summary>
        public AmbientMode AmbientMode = AmbientMode.Flat;
        /// <summary>
        /// Ambient intensity (rendering)
        /// </summary>
        public float AmbientIntensity = 1;
        /// <summary>
        /// Ambient light (rendering)
        /// </summary>
        public Color AmbientLight = new Color(0.2f, 0.2f, 0.2f, 1);

        /// <summary>
        /// Is ROI creation mode activated ?
        /// </summary>
        public bool ROICreation
        {
            get
            {
                return SceneInformation.IsROICreationModeEnabled;
            }
            set
            {
                SceneInformation.IsROICreationModeEnabled = value;
                ColumnManager.UpdateROIVisibility(value);
            }
        }

        /// <summary>
        /// Lock when updating generator
        /// </summary>
        private bool m_UpdatingGenerator = false;
        /// <summary>
        /// True if generator needs an update
        /// </summary>
        private bool m_GeneratorNeedsUpdate = true;
        /// <summary>
        /// Lock when updating colliders
        /// </summary>
        private bool m_UpdatingColliders = false;

        private const int LOADING_MESH_WEIGHT = 2500;
        private const int LOADING_MRI_WEIGHT = 1500;
        private const int LOADING_IMPLANTATIONS_WEIGHT = 50;
        private const int LOADING_MNI_WEIGHT = 100;
        private const int LOADING_IEEG_WEIGHT = 15;

        [SerializeField] private GameObject m_BrainPrefab;
        [SerializeField] private GameObject m_SimplifiedBrainPrefab;
        [SerializeField] private GameObject m_InvisibleBrainPrefab;
        [SerializeField] private GameObject m_CutPrefab;
        [SerializeField] private GameObject m_SitePrefab;
        #endregion

        #region Events
        /// <summary>
        /// Event called when this scene is selected
        /// </summary>
        [HideInInspector] public GenericEvent<bool> OnChangeSelectedState = new GenericEvent<bool>();
        /// <summary>
        /// Event called when showing or hiding the scene in the UI
        /// </summary>
        [HideInInspector] public GenericEvent<bool> OnChangeVisibleState = new GenericEvent<bool>();
        /// <summary>
        /// Event called when reseting the view positions in the UI
        /// </summary>
        [HideInInspector] public UnityEvent OnResetViewPositions = new UnityEvent();
        /// <summary>
        /// Event called when progressing in updating generator.
        /// </summary>
        [HideInInspector] public GenericEvent<float, string> OnProgressUpdateGenerator = new GenericEvent<float, string>();
        /// <summary>
        /// Event for updating the planes cuts display in the cameras
        /// </summary>
        [HideInInspector] public UnityEvent OnModifyPlanesCuts = new UnityEvent();
        /// <summary>
        /// Event called when adding a cut to the scene
        /// </summary>
        [HideInInspector] public GenericEvent<Cut> OnAddCut = new GenericEvent<Cut>();
        /// <summary>
        /// Event called when updating the sites rendering
        /// </summary>
        [HideInInspector] public UnityEvent OnSitesRenderingUpdated = new UnityEvent();
        /// <summary>
        /// Event called when changing the colors of the colormap
        /// </summary>
        [HideInInspector] public GenericEvent<Data.Enums.ColorType> OnChangeColormap = new GenericEvent<Data.Enums.ColorType>();
        /// <summary>
        /// Event for colormap values associated to a column id (params : minValue, middle, maxValue, id)
        /// </summary>
        [HideInInspector] public GenericEvent<float, float, float, Column3D> OnSendColorMapValues = new GenericEvent<float, float, float, Column3D>();
        /// <summary>
        /// Event called when changing the implantation files to use in the scene
        /// </summary>
        [HideInInspector] public UnityEvent OnUpdateSites = new UnityEvent();
        /// <summary>
        /// Ask the camera manager to update the target for this scene
        /// </summary>
        [HideInInspector] public GenericEvent<Vector3> OnUpdateCameraTarget = new GenericEvent<Vector3>();
        /// <summary>
        /// Event called when site is clicked to dipslay additionnal infomation.
        /// </summary>
        [HideInInspector] public GenericEvent<IEnumerable<Site>> OnRequestSiteInformation = new GenericEvent<IEnumerable<Site>>();
        /// <summary>
        /// Event called when requesting a screenshot of the scene
        /// </summary>
        [HideInInspector] public GenericEvent<bool> OnRequestScreenshot = new GenericEvent<bool>();
        /// <summary>
        /// Event called when updating the generator state
        /// </summary>
        [HideInInspector] public GenericEvent<bool> OnUpdatingGenerator = new GenericEvent<bool>();
        /// <summary>
        /// Event called when ieeg are outdated or not anymore
        /// </summary>
        [HideInInspector] public GenericEvent<bool> OnIEEGOutdated = new GenericEvent<bool>();
        #endregion

        #region Private Methods
        private void Update()
        {
            if (!SceneInformation.IsSceneInitialized) return;

            if (SceneInformation.MeshGeometryNeedsUpdate)
            {
                UpdateGeometry();
                SceneInformation.MeshGeometryNeedsUpdate = false;
            }

            if (!SceneInformation.AreSitesUpdated)
            {
                m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
                OnSitesRenderingUpdated.Invoke();
            }

            if (m_GeneratorNeedsUpdate && !IsLatencyModeEnabled)
            {
                OnIEEGOutdated.Invoke(true);
            }
            if (m_GeneratorNeedsUpdate && !IsLatencyModeEnabled && ApplicationState.UserPreferences.Visualization._3D.AutomaticEEGUpdate)
            {
                UpdateGenerator();
            }
            OnUpdatingGenerator.Invoke(m_UpdatingGenerator);

            if (!SceneInformation.IsSceneCompletelyLoaded)
            {
                UpdateVisibleState(true);
                SceneInformation.IsSceneCompletelyLoaded = true;
            }
        }
        /// <summary>
        /// Add every listeners required for the scene
        /// </summary>
        private void AddListeners()
        {
            m_TriEraser.OnModifyInvisiblePart.AddListener(() =>
            {
                ResetIEEG();
                ApplicationState.Module3D.OnModifyInvisiblePart.Invoke();
            });
            m_ColumnManager.OnUpdateMRICalValues.AddListener(() =>
            {
                ResetIEEG();
            });
            m_ColumnManager.OnUpdateIEEGSpan.AddListener((column) =>
            {
                ResetIEEG();
            });
            m_ColumnManager.OnUpdateIEEGAlpha.AddListener((column) =>
            {
                ComputeIEEGTextures(column);
                if (column.IsSelected)
                {
                    ComputeGUITextures();
                }
            });
            m_ColumnManager.OnUpdateIEEGGain.AddListener((column) =>
            {
                SceneInformation.AreSitesUpdated = false;
            });
            m_ColumnManager.OnUpdateIEEGMaximumInfluence.AddListener((column) =>
            {
                ResetIEEG();
            });
            m_ColumnManager.OnUpdateColumnTimelineID.AddListener((column) =>
            {
                ComputeIEEGTextures(column);
                if (column.IsSelected)
                {
                    ComputeGUITextures();
                }
                SceneInformation.AreSitesUpdated = false;
            });
            m_ColumnManager.OnChangeNumberOfROI.AddListener((column) =>
            {
                UpdateCurrentRegionOfInterest(column);
                ApplicationState.Module3D.OnChangeNumberOfROI.Invoke();
            });
            m_ColumnManager.OnChangeNumberOfVolumeInROI.AddListener((column) =>
            {
                UpdateCurrentRegionOfInterest(column);
                ApplicationState.Module3D.OnChangeNumberOfVolumeInROI.Invoke();
            });
            m_ColumnManager.OnSelectROI.AddListener((column) =>
            {
                UpdateCurrentRegionOfInterest(column);
                ApplicationState.Module3D.OnSelectROI.Invoke();
            });
            m_ColumnManager.OnChangeROIVolumeRadius.AddListener((column) =>
            {
                UpdateCurrentRegionOfInterest(column);
                ApplicationState.Module3D.OnChangeROIVolumeRadius.Invoke();
            });
            m_ColumnManager.OnChangeSelectedState.AddListener((selected) =>
            {
                IsSelected = selected;
                ComputeGUITextures();
            });
            m_ColumnManager.OnSelectSite.AddListener((site) =>
            {
                ClickOnSiteCallback();
                SceneInformation.AreSitesUpdated = false;
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            });
            m_ColumnManager.OnChangeSiteState.AddListener((site) =>
            {
                ResetIEEG(false);
            });
            m_ColumnManager.OnChangeCCEPParameters.AddListener(() =>
            {
                SceneInformation.AreSitesUpdated = false;
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            });
            m_ColumnManager.OnUpdateFMRIParameters.AddListener(() =>
            {
                ResetIEEG();
            });
            SceneInformation.OnUpdateGeneratorState.AddListener((value) =>
            {
                if (!value)
                {
                    foreach (Column3DIEEG column in m_ColumnManager.ColumnsIEEG)
                    {
                        column.IsTimelineLooping = false;
                        column.IsTimelinePlaying = false;
                        column.CurrentTimeLineID = column.CurrentTimeLineID;
                        column.IsRenderingUpToDate = false;
                    }
                }
                if (IsSelected)
                {
                    ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
                }
            });
        }
        /// <summary>
        /// 
        /// When to call ?  changes in DLLCutColorScheme, MRICalMinFactor, MRICalMaxFactor
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        private void ComputeMRITextures()
        {
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                foreach (Cut cut in m_Cuts)
                {
                    m_ColumnManager.CreateMRITexture(column, cut.ID);
                }
            }
        }
        /// <summary>
        /// 
        /// When to call ? changes in IEEGColumn.currentTimeLineID, IEEGColumn.alphaMin, IEEGColumn.alphaMax / DLLCutColorScheme
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        private void ComputeIEEGTextures(Column3DIEEG column = null)
        {
            if (!SceneInformation.IsGeneratorUpToDate) return;

            if (column)
            {
                m_ColumnManager.ComputeSurfaceBrainUVWithIEEG(column);
                column.ColorCutsTexturesWithIEEG();
            }
            else
            {
                foreach (Column3DIEEG col in m_ColumnManager.ColumnsIEEG)
                {
                    m_ColumnManager.ComputeSurfaceBrainUVWithIEEG(col);
                    col.ColorCutsTexturesWithIEEG();
                }
            }
        }
        /// <summary>
        /// Compute textures to be displayed on the UI
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        private void ComputeGUITextures()
        {
            Column3D column = m_ColumnManager.SelectedColumn;
            if (column)
            {
                column.CreateGUIMRITextures(m_Cuts);
                column.ResizeGUIMRITextures(m_Cuts);
                foreach (Cut cut in m_Cuts)
                {
                    cut.OnUpdateGUITextures.Invoke(column);
                }
            }
        }
        /// <summary>
        /// Finalize Generators Computing
        /// </summary>
        private void FinalizeGeneratorsComputing()
        {
            // generators are now up to date
            SceneInformation.IsGeneratorUpToDate = true;

            // send inf values to overlays
            for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
            {
                OnSendColorMapValues.Invoke(m_ColumnManager.ColumnsIEEG[ii].IEEGParameters.SpanMin, m_ColumnManager.ColumnsIEEG[ii].IEEGParameters.Middle, m_ColumnManager.ColumnsIEEG[ii].IEEGParameters.SpanMax, m_ColumnManager.ColumnsIEEG[ii]);
                m_ColumnManager.ColumnsIEEG[ii].CurrentTimeLineID = m_ColumnManager.ColumnsIEEG[ii].CurrentTimeLineID;
            }

            // update plots visibility
            SceneInformation.AreSitesUpdated = false;

            // check validity of plot scale
            m_ColumnManager.CheckIEEGParametersIntegrity();

            OnIEEGOutdated.Invoke(false);
        }
        /// <summary>
        /// Actions to perform when clicking on a site
        /// </summary>
        private void ClickOnSiteCallback()
        {
            if (m_ColumnManager.SelectedColumn.SelectedSiteID == -1) return;

            if (m_ColumnManager.SelectedColumn.Type == Column3D.ColumnType.IEEG && !IsLatencyModeEnabled)
            {
                Column3DIEEG column = (Column3DIEEG)m_ColumnManager.SelectedColumn;
                if (column.SelectedSiteID != -1)
                {
                    List<Site> sites = new List<Site>();
                    sites.Add(column.SelectedSite);
                    if (m_SiteToCompare != null) sites.Add(m_SiteToCompare);
                    OnRequestSiteInformation.Invoke(sites);
                }
            }
        }
        /// <summary>
        /// Init gameobjects of the scene
        /// </summary>
        private void InitializeSceneGameObjects()
        {
            transform.position = new Vector3(HBP3DModule.SPACE_BETWEEN_SCENES_GAME_OBJECTS * ApplicationState.Module3D.NumberOfScenesLoadedSinceStart, transform.position.y, transform.position.z);

            // Mark brain mesh as dynamic
            m_BrainPrefab.GetComponent<MeshFilter>().sharedMesh.MarkDynamic();

            // Cuts
            m_DisplayedObjects.BrainCutMeshes = new List<GameObject>();
            m_Cuts = new List<Cut>();

            // Default colors
            UpdateBrainSurfaceColor(m_ColumnManager.BrainColor);
            UpdateColormap(m_ColumnManager.Colormap, false);
            UpdateBrainCutColor(m_ColumnManager.BrainCutColor, true);
        }
        /// <summary>
        /// Update the surface meshes from the DLL
        /// </summary>
        private void UpdateMeshesFromDLL()
        {
            for (int ii = 0; ii < m_ColumnManager.MeshSplitNumber; ++ii)
            {
                m_ColumnManager.SelectedMesh.SplittedMeshes[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
            }
            UnityEngine.Profiling.Profiler.BeginSample("Update Columns Meshes");
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.UpdateColumnMeshes(m_DisplayedObjects.BrainSurfaceMeshes, SceneInformation.UseSimplifiedMeshes);
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Generate the split number regarding all meshes
        /// </summary>
        /// <param name="meshes"></param>
        private void GenerateSplits(IEnumerable<DLL.Surface> meshes)
        {
            int maxVertices = (from mesh in meshes select mesh.NumberOfVertices).Max();
            int splits = (maxVertices / 65000) + (((maxVertices % 60000) != 0) ? 3 : 2);
            if (splits < 3) splits = 3;
            ResetSplitsNumber(splits);
        }
        #endregion

        #region Public Methods

        #region Display
        /// <summary>
        /// Update the IEEG colormap for this scene
        /// </summary>
        /// <param name="color">Color of the colormap</param>
        /// <param name="updateColors">Do the colors need to be reset ?</param>
        public void UpdateColormap(Data.Enums.ColorType color, bool updateColors = true)
        {
            ColumnManager.Colormap = color;
            if (updateColors)
                ColumnManager.ResetColors();

            SharedMaterials.Brain.BrainMaterials[this].SetTexture("_ColorTex", ColumnManager.BrainColorMapTexture);

            if (SceneInformation.IsGeneratorUpToDate)
            {
                ComputeIEEGTextures();
                ComputeGUITextures();
            }

            OnChangeColormap.Invoke(color);
        }
        /// <summary>
        /// Update the color of the surface of the brain for this scene
        /// </summary>
        /// <param name="color">Color of the brain</param>
        public void UpdateBrainSurfaceColor(Data.Enums.ColorType color)
        {
            ColumnManager.BrainColor = color;
            DLL.Texture tex = DLL.Texture.Generate1DColorTexture(ColumnManager.BrainColor);
            tex.UpdateTexture2D(ColumnManager.BrainColorTexture);

            SharedMaterials.Brain.BrainMaterials[this].SetTexture("_MainTex", ColumnManager.BrainColorTexture);
        }
        /// <summary>
        /// Update the color of the cuts for this scene
        /// </summary>
        /// <param name="color">Color of the cuts</param>
        /// <param name="updateColors">Do the colors need to be reset ?</param>
        public void UpdateBrainCutColor(Data.Enums.ColorType color, bool updateColors = true)
        {
            ColumnManager.BrainCutColor = color;
            if (updateColors)
                ColumnManager.ResetColors();

            SceneInformation.MeshGeometryNeedsUpdate = true;
            ResetIEEG();
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.IsRenderingUpToDate = false;
            }
        }
        /// <summary>
        /// Set the mesh part to be displayed in the scene
        /// </summary>
        /// <param name="meshPartToDisplay"></param>
        public void UpdateMeshPartToDisplay(Data.Enums.MeshPart meshPartToDisplay)
        {
            SceneInformation.MeshPartToDisplay = meshPartToDisplay;
            SceneInformation.MeshGeometryNeedsUpdate = true;
            ResetIEEG();
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.IsRenderingUpToDate = false;
            }
        }
        /// <summary>
        /// Set the mesh type to be displayed in the scene
        /// </summary>
        /// <param name="meshTypeToDisplay"></param>
        public void UpdateMeshToDisplay(string meshName)
        {
            int meshID = m_ColumnManager.Meshes.FindIndex(m => m.Name == meshName);
            if (meshID == -1) meshID = 0;

            m_ColumnManager.SelectedMeshID = meshID;
            SceneInformation.MeshGeometryNeedsUpdate = true;
            ResetIEEG();
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.IsRenderingUpToDate = false;
            }

            OnUpdateCameraTarget.Invoke(m_ColumnManager.SelectedMesh.Both.BoundingBox.Center);
        }
        /// <summary>
        /// Set the MRI to be used
        /// </summary>
        /// <param name="mriID"></param>
        public void UpdateMRIToDisplay(string mriName)
        {
            int mriID = m_ColumnManager.MRIs.FindIndex(m => m.Name == mriName);
            if (mriID == -1) mriID = 0;

            m_ColumnManager.SelectedMRIID = mriID;
            SceneInformation.VolumeCenter = m_ColumnManager.SelectedMRI.Volume.Center;
            SceneInformation.MeshGeometryNeedsUpdate = true;
            ResetIEEG();
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.IsRenderingUpToDate = false;
            }
        }
        /// <summary>
        /// Update the gameobjects of the sites
        /// </summary>
        /// <param name="implantationID"></param>
        public void UpdateSites(string implantationName)
        {
            // destroy previous electrodes gameobjects
            for (int ii = 0; ii < m_ColumnManager.SitesList.Count; ++ii)
            {
                //Destroy(go_.PlotsList[ii].GetComponent<MeshFilter>().mesh); // now it's a shared mesh
                Destroy(m_ColumnManager.SitesList[ii]);
            }
            m_ColumnManager.SitesList.Clear();


            // destroy plots elecs/patients parents
            for (int ii = 0; ii < m_ColumnManager.SitesPatientParent.Count; ++ii)
            {
                Destroy(m_ColumnManager.SitesPatientParent[ii]);
                for (int jj = 0; jj < m_ColumnManager.SitesElectrodesParent[ii].Count; ++jj)
                {
                    Destroy(m_ColumnManager.SitesElectrodesParent[ii][jj]);
                }

            }
            m_ColumnManager.SitesPatientParent.Clear();
            m_ColumnManager.SitesElectrodesParent.Clear();

            int implantationID = m_ColumnManager.Implantations.FindIndex(i => i.Name == implantationName);
            m_ColumnManager.SelectedImplantationID = implantationID > 0 ? implantationID : 0;
            DLL.PatientElectrodesList electrodesList = m_ColumnManager.SelectedImplantation.PatientElectrodesList;

            int currPlotNb = 0;
            for (int ii = 0; ii < electrodesList.NumberOfPatients; ++ii)
            {
                int patientSiteID = 0;
                string patientID = electrodesList.PatientName(ii);
                Data.Patient patient = Visualization.Patients.FirstOrDefault((p) => p.ID == patientID);

                // create plot patient parent
                m_ColumnManager.SitesPatientParent.Add(new GameObject("P" + ii + " - " + patientID));
                m_ColumnManager.SitesPatientParent[m_ColumnManager.SitesPatientParent.Count - 1].transform.SetParent(m_DisplayedObjects.SitesMeshesParent.transform);
                m_ColumnManager.SitesPatientParent[m_ColumnManager.SitesPatientParent.Count - 1].transform.localPosition = Vector3.zero;
                m_ColumnManager.SitesElectrodesParent.Add(new List<GameObject>(electrodesList.NumberOfElectrodesInPatient(ii)));

                for (int jj = 0; jj < electrodesList.NumberOfElectrodesInPatient(ii); ++jj)
                {
                    // create plot electrode parent
                    m_ColumnManager.SitesElectrodesParent[ii].Add(new GameObject(electrodesList.ElectrodeName(ii, jj)));
                    m_ColumnManager.SitesElectrodesParent[ii][m_ColumnManager.SitesElectrodesParent[ii].Count - 1].transform.SetParent(m_ColumnManager.SitesPatientParent[ii].transform);
                    m_ColumnManager.SitesElectrodesParent[ii][m_ColumnManager.SitesElectrodesParent[ii].Count - 1].transform.localPosition = Vector3.zero;

                    for (int kk = 0; kk < electrodesList.NumberOfSitesInElectrode(ii, jj); ++kk)
                    {
                        Vector3 invertedPosition = electrodesList.SitePosition(ii, jj, kk);
                        invertedPosition.x = -invertedPosition.x;

                        GameObject siteGameObject = Instantiate(m_SitePrefab);
                        siteGameObject.name = electrodesList.SiteName(ii, jj, kk);

                        siteGameObject.transform.SetParent(m_ColumnManager.SitesElectrodesParent[ii][jj].transform);
                        siteGameObject.transform.localPosition = invertedPosition;
                        siteGameObject.GetComponent<MeshFilter>().sharedMesh = SharedMeshes.Site;

                        siteGameObject.SetActive(true);
                        siteGameObject.layer = LayerMask.NameToLayer("Inactive");

                        Site site = siteGameObject.GetComponent<Site>();
                        site.Information.Patient = patient;
                        site.Information.Name = siteGameObject.name;
                        site.Information.SitePatientID = patientSiteID++;
                        site.Information.PatientNumber = ii;
                        site.Information.ElectrodeNumber = jj;
                        site.Information.SiteNumber = kk;
                        site.Information.GlobalID = currPlotNb++;
                        site.Information.MarsAtlasIndex = electrodesList.MarsAtlasLabelOfSite(ii, jj, kk);
                        site.State.IsBlackListed = false;
                        site.State.IsHighlighted = false;
                        site.State.IsExcluded = false;
                        site.State.IsOutOfROI = true;
                        site.State.IsMarked = false;
                        site.State.IsMasked = false;
                        site.State.IsSuspicious = false;
                        site.IsActive = true;

                        m_ColumnManager.SitesList.Add(siteGameObject);
                    }
                }
            }
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.UpdateSites(m_ColumnManager.SelectedImplantation.PatientElectrodesList, m_ColumnManager.SitesPatientParent, m_ColumnManager.SitesList);
                UpdateCurrentRegionOfInterest(column);
            }
            // reset selected site
            for (int ii = 0; ii < m_ColumnManager.Columns.Count; ++ii)
            {
                m_ColumnManager.Columns[ii].UnselectSite();
            }

            ResetIEEG();
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.IsRenderingUpToDate = false;
            }
            OnUpdateSites.Invoke();
        }
        /// <summary>
        /// Update meshes to display
        /// </summary>
        public void UpdateMeshesInformation()
        {
            if (m_ColumnManager.SelectedMesh is LeftRightMesh3D)
            {
                LeftRightMesh3D selectedMesh = (LeftRightMesh3D)m_ColumnManager.SelectedMesh;
                switch (SceneInformation.MeshPartToDisplay)
                {
                    case Data.Enums.MeshPart.Left:
                        SceneInformation.SimplifiedMeshToUse = selectedMesh.SimplifiedLeft;
                        SceneInformation.MeshToDisplay = selectedMesh.Left;
                        break;
                    case Data.Enums.MeshPart.Right:
                        SceneInformation.SimplifiedMeshToUse = selectedMesh.SimplifiedRight;
                        SceneInformation.MeshToDisplay = selectedMesh.Right;
                        break;
                    case Data.Enums.MeshPart.Both:
                        SceneInformation.SimplifiedMeshToUse = selectedMesh.SimplifiedBoth;
                        SceneInformation.MeshToDisplay = selectedMesh.Both;
                        break;
                    default:
                        SceneInformation.SimplifiedMeshToUse = selectedMesh.SimplifiedBoth;
                        SceneInformation.MeshToDisplay = selectedMesh.Both;
                        break;
                }
            }
            else
            {
                SceneInformation.SimplifiedMeshToUse = m_ColumnManager.SelectedMesh.SimplifiedBoth;
                SceneInformation.MeshToDisplay = m_ColumnManager.SelectedMesh.Both;
            }
            // get the middle
            SceneInformation.MeshCenter = SceneInformation.MeshToDisplay.BoundingBox.Center;

            UpdateAllCutPlanes();
        }
        /// <summary>
        /// Update the visible state of the scene
        /// </summary>
        /// <param name="state"></param>
        public void UpdateVisibleState(bool state)
        {
            OnChangeVisibleState.Invoke(state);
            if (!state)
            {
                ApplicationState.Module3D.OnMinimizeScene.Invoke(this);
                IsSelected = false;
            }
        }
        #endregion

        #region Cuts
        /// <summary>
        /// Add a new cut plane
        /// </summary>
        public Cut AddCutPlane()
        {
            // Add new cut
            Cut cut = new Cut(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
            switch (Cuts.Count)
            {
                case 0:
                    cut.Orientation = Data.Enums.CutOrientation.Axial;
                    cut.Flip = false;
                    cut.RemoveFrontPlane = 0;
                    cut.Position = 0.5f;
                    break;
                case 1:
                    cut.Orientation = Data.Enums.CutOrientation.Coronal;
                    cut.Flip = false;
                    cut.RemoveFrontPlane = 0;
                    cut.Position = 0.5f;
                    break;
                case 2:
                    cut.Orientation = Data.Enums.CutOrientation.Sagital;
                    cut.Flip = false;
                    cut.RemoveFrontPlane = 0;
                    cut.Position = 0.5f;
                    break;
                default:
                    cut.Orientation = Data.Enums.CutOrientation.Axial;
                    cut.Flip = false;
                    cut.RemoveFrontPlane = 0;
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
            GameObject cutGameObject = Instantiate(m_CutPrefab);
            cutGameObject.GetComponent<Renderer>().sharedMaterial = SharedMaterials.Brain.CutMaterials[this];
            cutGameObject.name = "cut_" + (Cuts.Count - 1);
            cutGameObject.transform.parent = m_DisplayedObjects.BrainCutMeshesParent.transform;
            cutGameObject.AddComponent<MeshCollider>();
            cutGameObject.layer = LayerMask.NameToLayer(SceneInformation.MeshesLayerName);
            cutGameObject.transform.localPosition = Vector3.zero;
            m_DisplayedObjects.BrainCutMeshes.Add(cutGameObject);
            m_DisplayedObjects.BrainCutMeshes.Last().layer = LayerMask.NameToLayer(SceneInformation.MeshesLayerName);

            // update columns manager
            m_ColumnManager.UpdateCutNumber(m_DisplayedObjects.BrainCutMeshes.Count);

            SceneInformation.MeshGeometryNeedsUpdate = true;
            ResetIEEG();

            OnAddCut.Invoke(cut);
            UpdateCutPlane(cut);

            return cut;
        }
        /// <summary>
        /// Remove the last cut plane
        /// </summary>
        public void RemoveCutPlane(Cut cut)
        {
            Cuts.Remove(cut);
            for (int i = 0; i < Cuts.Count; i++)
            {
                Cuts[i].ID = i;
            }

            Destroy(m_DisplayedObjects.BrainCutMeshes[cut.ID]);
            m_DisplayedObjects.BrainCutMeshes.RemoveAt(cut.ID);

            // update columns manager
            m_ColumnManager.UpdateCutNumber(m_DisplayedObjects.BrainCutMeshes.Count);

            SceneInformation.MeshGeometryNeedsUpdate = true;
            ResetIEEG();

            cut.OnRemoveCut.Invoke();
        }
        /// <summary>
        /// Update a cut plane
        /// </summary>
        /// <param name="orientationID"></param>
        /// <param name="flip"></param>
        /// <param name="removeFrontPlane"></param>
        /// <param name="customNormal"></param>
        /// <param name="idPlane"></param>
        /// <param name="position"></param>
        public void UpdateCutPlane(Cut cut)
        {
            if (!CuttingSimplifiedMesh && SceneInformation.UseSimplifiedMeshes) CuttingSimplifiedMesh = true;

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
                m_ColumnManager.SelectedMRI.Volume.SetPlaneWithOrientation(plane, cut.Orientation, cut.Flip);
                cut.Normal = plane.Normal;
            }

            SceneInformation.LastPlaneModifiedID = cut.ID;

            // Cuts base on the mesh
            float offset;
            if (SceneInformation.MeshToDisplay != null)
            {
                offset = SceneInformation.MeshToDisplay.SizeOffsetCutPlane(cut, cut.NumberOfCuts);
                //offset *= 1.05f; // upsize a little bit the bbox for planes
            }
            else
                offset = 0.1f;

            cut.Point = SceneInformation.MeshCenter + cut.Normal * (cut.Position - 0.5f) * offset * cut.NumberOfCuts;

            SceneInformation.MeshGeometryNeedsUpdate = true;
            ResetIEEG();

            // update cameras cuts display
            OnModifyPlanesCuts.Invoke();

            cut.OnUpdateCut.Invoke();
        }
        /// <summary>
        /// Update the values of all the cut planes
        /// </summary>
        public void UpdateAllCutPlanes()
        {
            foreach (var cut in m_Cuts)
            {
                UpdateCutPlane(cut);
            }
        }
        /// <summary>
        /// Create 3 cuts surrounding the selected site.
        /// </summary>
        public void CutAroundSelectedSite()
        {
            Site site = ColumnManager.SelectedColumn.SelectedSite;
            if (!site) return;

            foreach (var cut in m_Cuts.ToList())
            {
                RemoveCutPlane(cut);
            }

            Cut axialCut = AddCutPlane();
            float axialOffset = SceneInformation.MeshToDisplay.SizeOffsetCutPlane(axialCut, axialCut.NumberOfCuts);
            Vector3 axialMin = SceneInformation.MeshCenter + axialCut.Normal * (-0.5f) * axialOffset * axialCut.NumberOfCuts;
            Vector3 axialMax = SceneInformation.MeshCenter + axialCut.Normal * 0.5f * axialOffset * axialCut.NumberOfCuts;
            axialCut.Position = (site.transform.localPosition.z - axialMin.z) / (axialMax.z - axialMin.z);
            UpdateCutPlane(axialCut);

            Cut coronalCut = AddCutPlane();
            float coronalOffset = SceneInformation.MeshToDisplay.SizeOffsetCutPlane(coronalCut, coronalCut.NumberOfCuts);
            Vector3 coronalMin = SceneInformation.MeshCenter + coronalCut.Normal * (-0.5f) * coronalOffset * coronalCut.NumberOfCuts;
            Vector3 coronalMax = SceneInformation.MeshCenter + coronalCut.Normal * 0.5f * coronalOffset * coronalCut.NumberOfCuts;
            coronalCut.Position = (site.transform.localPosition.y - coronalMin.y) / (coronalMax.y - coronalMin.y);
            UpdateCutPlane(coronalCut);

            Cut sagitalCut = AddCutPlane();
            float sagitalOffset = SceneInformation.MeshToDisplay.SizeOffsetCutPlane(sagitalCut, sagitalCut.NumberOfCuts);
            Vector3 sagitalMin = SceneInformation.MeshCenter + sagitalCut.Normal * (-0.5f) * sagitalOffset * sagitalCut.NumberOfCuts;
            Vector3 sagitalMax = SceneInformation.MeshCenter + sagitalCut.Normal * 0.5f * sagitalOffset * sagitalCut.NumberOfCuts;
            sagitalCut.Position = (-site.transform.localPosition.x - sagitalMin.x) / (sagitalMax.x - sagitalMin.x);
            UpdateCutPlane(sagitalCut);
        }
        #endregion

        #region Triangle Erasing
        /// <summary>
        /// Reset invisible brain and triangle eraser
        /// </summary>
        /// <param name="updateGO"></param>
        public void ResetTriangleErasing(bool updateGO = true)
        {
            // destroy previous GO
            if (m_DisplayedObjects.InvisibleBrainSurfaceMeshes != null)
                for (int ii = 0; ii < m_DisplayedObjects.InvisibleBrainSurfaceMeshes.Count; ++ii)
                    Destroy(m_DisplayedObjects.InvisibleBrainSurfaceMeshes[ii]);

            // create new GO
            m_DisplayedObjects.InvisibleBrainSurfaceMeshes = new List<GameObject>(m_DisplayedObjects.BrainSurfaceMeshes.Count);
            for (int ii = 0; ii < m_DisplayedObjects.BrainSurfaceMeshes.Count; ++ii)
            {
                GameObject invisibleBrainPart = Instantiate(m_InvisibleBrainPrefab);
                invisibleBrainPart.name = "erased brain part " + ii;
                invisibleBrainPart.transform.SetParent(m_DisplayedObjects.InvisibleBrainMeshesParent.transform);
                invisibleBrainPart.layer = LayerMask.NameToLayer(SceneInformation.MeshesLayerName);
                invisibleBrainPart.AddComponent<MeshFilter>();
                invisibleBrainPart.transform.localScale = new Vector3(-1, 1, 1);
                invisibleBrainPart.transform.localPosition = new Vector3(0, 0, 0);
                invisibleBrainPart.SetActive(m_TriEraser.IsEnabled);
                m_DisplayedObjects.InvisibleBrainSurfaceMeshes.Add(invisibleBrainPart);
            }

            m_TriEraser.Reset(m_DisplayedObjects.InvisibleBrainSurfaceMeshes, m_ColumnManager.DLLCutsList[0], m_ColumnManager.SelectedMesh.SplittedMeshes);

            if (updateGO)
                UpdateMeshesFromDLL();
        }
        /// <summary>
        /// CTRL+Z on triangle eraser
        /// </summary>
        public void CancelLastTriangleErasingAction()
        {
            m_TriEraser.CancelLastAction();
            UpdateMeshesFromDLL();
        }
        #endregion

        #region Save/Load
        /// <summary>
        /// Initialize the scene with the corresponding visualization
        /// </summary>
        /// <param name="visualization"></param>
        public void Initialize(Visualization visualization)
        {
            SceneInformation = new SceneStatesInfo();

            Visualization = visualization;
            gameObject.name = Visualization.Name;

            // Init materials
            SharedMaterials.Brain.AddSceneMaterials(this);

            // Set default SceneInformation values
            SceneInformation.MeshesLayerName = "Default";
            SceneInformation.HiddenMeshesLayerName = "Hidden Meshes";
            SceneInformation.UseSimplifiedMeshes = ApplicationState.UserPreferences.Visualization.Cut.SimplifiedMeshes;

            AddListeners();
            InitializeSceneGameObjects();
        }
        /// <summary>
        /// Set up the scene to display it properly (and load configurations)
        /// </summary>
        public void FinalizeInitialization()
        {
            m_ColumnManager.Columns[0].Views[0].IsSelected = true; // Select default view
            SceneInformation.IsSceneInitialized = true;
            this.StartCoroutineAsync(c_LoadMissingAnatomy());
        }
        /// <summary>
        /// Load the visualization configuration from the loaded visualization
        /// </summary>
        public void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration(false);
            UpdateBrainSurfaceColor(Visualization.Configuration.BrainColor);
            UpdateBrainCutColor(Visualization.Configuration.BrainCutColor);
            UpdateColormap(Visualization.Configuration.Colormap);
            UpdateMeshPartToDisplay(Visualization.Configuration.MeshPart);
            EdgeMode = Visualization.Configuration.EdgeMode;
            StrongCuts = Visualization.Configuration.StrongCuts;
            HideBlacklistedSites = Visualization.Configuration.HideBlacklistedSites;
            ShowAllSites = Visualization.Configuration.ShowAllSites;
            m_ColumnManager.MRICalMinFactor = Visualization.Configuration.MRICalMinFactor;
            m_ColumnManager.MRICalMaxFactor = Visualization.Configuration.MRICalMaxFactor;
            CameraType = Visualization.Configuration.CameraType;

            if (!string.IsNullOrEmpty(Visualization.Configuration.MeshName)) UpdateMeshToDisplay(Visualization.Configuration.MeshName);
            if (!string.IsNullOrEmpty(Visualization.Configuration.MRIName)) UpdateMRIToDisplay(Visualization.Configuration.MRIName);
            if (!string.IsNullOrEmpty(Visualization.Configuration.ImplantationName)) UpdateSites(Visualization.Configuration.ImplantationName);

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
                    m_ColumnManager.AddViewLine();
                }
                foreach (Column3D column in m_ColumnManager.Columns)
                {
                    column.Views.Last().SetCamera(view.Position.ToVector3(), view.Rotation.ToQuaternion(), view.Target.ToVector3());
                }
            }

            ROICreation = !ROICreation;
            foreach (Column3DIEEG column in m_ColumnManager.ColumnsIEEG)
            {
                column.LoadConfiguration(false);
            }
            ROICreation = !ROICreation;

            SceneInformation.AreSitesUpdated = false;

            if (firstCall) ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Save the current settings of this scene to the configuration of the linked visualization
        /// </summary>
        public void SaveConfiguration()
        {
            Visualization.Configuration.BrainColor = m_ColumnManager.BrainColor;
            Visualization.Configuration.BrainCutColor = m_ColumnManager.BrainCutColor;
            Visualization.Configuration.Colormap = m_ColumnManager.Colormap;
            Visualization.Configuration.MeshPart = SceneInformation.MeshPartToDisplay;
            Visualization.Configuration.MeshName = m_ColumnManager.SelectedMesh.Name;
            Visualization.Configuration.MRIName = m_ColumnManager.SelectedMRI.Name;
            Visualization.Configuration.ImplantationName = m_ColumnManager.SelectedImplantation.Name;
            Visualization.Configuration.EdgeMode = EdgeMode;
            Visualization.Configuration.StrongCuts = StrongCuts;
            Visualization.Configuration.HideBlacklistedSites = HideBlacklistedSites;
            Visualization.Configuration.ShowAllSites = ShowAllSites;
            Visualization.Configuration.MRICalMinFactor = m_ColumnManager.MRICalMinFactor;
            Visualization.Configuration.MRICalMaxFactor = m_ColumnManager.MRICalMaxFactor;
            Visualization.Configuration.CameraType = CameraType;

            List<Data.Visualization.Cut> cuts = new List<Data.Visualization.Cut>();
            foreach (Cut cut in m_Cuts)
            {
                cuts.Add(new Data.Visualization.Cut(cut.Normal, cut.Orientation, cut.Flip, cut.Position));
            }
            Visualization.Configuration.Cuts = cuts;

            List<View> views = new List<View>();
            foreach (View3D view in m_ColumnManager.Views)
            {
                views.Add(new View(view.LocalCameraPosition, view.LocalCameraRotation, view.LocalCameraTarget));
            }
            Visualization.Configuration.Views = views;

            foreach (Column3DIEEG column in m_ColumnManager.ColumnsIEEG)
            {
                column.SaveConfiguration();
            }
        }
        /// <summary>
        /// Reset the settings of the loaded scene
        /// </summary>
        public void ResetConfiguration(bool firstCall = true)
        {
            UpdateBrainSurfaceColor(Data.Enums.ColorType.BrainColor);
            UpdateBrainCutColor(Data.Enums.ColorType.Default);
            UpdateColormap(Data.Enums.ColorType.MatLab);
            UpdateMeshPartToDisplay(Data.Enums.MeshPart.Both);
            EdgeMode = false;
            StrongCuts = false;
            HideBlacklistedSites = false;
            m_ColumnManager.MRICalMinFactor = 0.0f;
            m_ColumnManager.MRICalMaxFactor = 1.0f;
            CameraType = CameraControl.Trackball;

            switch (Type)
            {
                case Data.Enums.SceneType.SinglePatient:
                    UpdateMeshToDisplay("Grey matter");
                    UpdateMRIToDisplay("Preimplantation");
                    UpdateSites("Patient");
                    break;
                case Data.Enums.SceneType.MultiPatients:
                    UpdateMeshToDisplay("MNI Grey matter");
                    UpdateMRIToDisplay("MNI");
                    UpdateSites("MNI");
                    break;
                default:
                    break;
            }

            while (m_Cuts.Count > 0)
            {
                RemoveCutPlane(m_Cuts.Last());
            }

            while (m_ColumnManager.Views.Count > 1)
            {
                m_ColumnManager.RemoveViewLine();
            }
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                foreach (View3D view in column.Views)
                {
                    view.Default();
                }
            }

            foreach (Column3DIEEG column in m_ColumnManager.ColumnsIEEG)
            {
                column.ResetConfiguration();
            }

            if (firstCall) ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Save site states of selected column
        /// </summary>
        /// <param name="path"></param>
        public void SaveSiteStatesOfSelectedColumn(string path)
        {
            m_ColumnManager.SelectedColumn.SaveSiteStates(path);
        }
        /// <summary>
        /// Load site states to selected column
        /// </summary>
        /// <param name="path"></param>
        public void LoadSiteStatesToSelectedColumn(string path)
        {
            m_ColumnManager.SelectedColumn.LoadSiteStates(path);
            SceneInformation.AreSitesUpdated = false;
            ResetIEEG(false);
        }
        #endregion

        /// <summary>
        /// Load a FMRI to this scene
        /// </summary>
        public bool LoadFMRI()
        {
            string[] filters = new string[] { "nii", "img" };
            string path = DLL.QtGUI.GetExistingFileName(filters, "Select an fMRI file");
            if (!string.IsNullOrEmpty(path))
            {
                m_ColumnManager.FMRI = new MRI3D(new Data.Anatomy.MRI("FMRI", path));
                ResetIEEG();
                return true;
            }
            return false;
        }
        /// <summary>
        /// Unload the FMRI of this scene
        /// </summary>
        public void UnloadFMRI()
        {
            m_ColumnManager.FMRI = null;
            ResetIEEG();
        }
        /// <summary>
        /// Reset the number of splits of the brain mesh
        /// </summary>
        /// <param name="nbSplits">Number of splits</param>
        public void ResetSplitsNumber(int nbSplits)
        {
            if (m_ColumnManager.MeshSplitNumber == nbSplits) return;

            m_ColumnManager.MeshSplitNumber = nbSplits;

            if (m_DisplayedObjects.BrainSurfaceMeshes.Count > 0)
                for (int ii = 0; ii < m_DisplayedObjects.BrainSurfaceMeshes.Count; ++ii)
                    Destroy(m_DisplayedObjects.BrainSurfaceMeshes[ii]);

            // reset meshes
            m_DisplayedObjects.BrainSurfaceMeshes = new List<GameObject>(m_ColumnManager.MeshSplitNumber);
            for (int ii = 0; ii < m_ColumnManager.MeshSplitNumber; ++ii)
            {
                m_DisplayedObjects.BrainSurfaceMeshes.Add(Instantiate(m_BrainPrefab));
                m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<Renderer>().sharedMaterial = SharedMaterials.Brain.BrainMaterials[this];
                m_DisplayedObjects.BrainSurfaceMeshes[ii].name = "brain_" + ii;
                m_DisplayedObjects.BrainSurfaceMeshes[ii].transform.parent = m_DisplayedObjects.BrainSurfaceMeshesParent.transform;
                m_DisplayedObjects.BrainSurfaceMeshes[ii].transform.localPosition = Vector3.zero;
                m_DisplayedObjects.BrainSurfaceMeshes[ii].layer = LayerMask.NameToLayer(SceneInformation.HiddenMeshesLayerName);
                if (!SceneInformation.UseSimplifiedMeshes) m_DisplayedObjects.BrainSurfaceMeshes[ii].AddComponent<MeshCollider>();
                m_DisplayedObjects.BrainSurfaceMeshes[ii].SetActive(true);
            }
            if (SceneInformation.UseSimplifiedMeshes)
            {
                // mesh collider
                m_DisplayedObjects.SimplifiedBrain = Instantiate(m_SimplifiedBrainPrefab);
                m_DisplayedObjects.SimplifiedBrain.GetComponent<Renderer>().sharedMaterial = SharedMaterials.Brain.SimplifiedBrainMaterials[this];
                m_DisplayedObjects.SimplifiedBrain.transform.name = "brain_simplified";
                m_DisplayedObjects.SimplifiedBrain.transform.parent = m_DisplayedObjects.BrainSurfaceMeshesParent.transform;
                m_DisplayedObjects.SimplifiedBrain.transform.localPosition = Vector3.zero;
                m_DisplayedObjects.SimplifiedBrain.layer = LayerMask.NameToLayer(SceneInformation.HiddenMeshesLayerName);
                m_DisplayedObjects.SimplifiedBrain.AddComponent<MeshCollider>();
                m_DisplayedObjects.SimplifiedBrain.SetActive(true);
            }

            m_ColumnManager.ResetSplitsNumber(nbSplits);
        }
        /// <summary>
        /// 
        /// </summary>
        public void ComputeMeshesCut()
        {
            if (SceneInformation.MeshToDisplay == null) return;

            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 0 cutSurface"); // 40%

            // cut the mesh
            List<DLL.Surface> cuts;
            if (Cuts.Count > 0)
                cuts = new List<DLL.Surface>(SceneInformation.MeshToDisplay.Cut(Cuts.ToArray(), !SceneInformation.CutHolesEnabled, StrongCuts));
            else
                cuts = new List<DLL.Surface>() { (DLL.Surface)SceneInformation.MeshToDisplay.Clone() };

            if (m_ColumnManager.DLLCutsList.Count != cuts.Count)
                m_ColumnManager.DLLCutsList = cuts;
            else
            {
                // swap DLL pointer
                for (int ii = 0; ii < cuts.Count; ++ii)
                    m_ColumnManager.DLLCutsList[ii].SwapDLLHandle(cuts[ii]);
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 1 splitToSurfaces"); // 2%

            // split the cut mesh         
            m_ColumnManager.SelectedMesh.SplittedMeshes = new List<DLL.Surface>(m_ColumnManager.DLLCutsList[0].SplitToSurfaces(m_ColumnManager.MeshSplitNumber));

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 2 reset brain texture generator"); // 11%

            // reset brain texture generator
            for (int ii = 0; ii < m_ColumnManager.MeshSplitNumber; ++ii)
            {
                m_ColumnManager.DLLCommonBrainTextureGeneratorList[ii].Reset(m_ColumnManager.SelectedMesh.SplittedMeshes[ii], m_ColumnManager.SelectedMRI.Volume);
                m_ColumnManager.DLLCommonBrainTextureGeneratorList[ii].ComputeUVMainWithVolume(m_ColumnManager.SelectedMesh.SplittedMeshes[ii], m_ColumnManager.SelectedMRI.Volume, m_ColumnManager.MRICalMinFactor, m_ColumnManager.MRICalMaxFactor);
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 3 update cut brain mesh object mesh filter"); // 6%

            ResetTriangleErasing(false);

            // update brain mesh object mesh filter
            UpdateMeshesFromDLL();

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 4 update cuts generators"); // 17%


            // update cuts generators
            for (int ii = 0; ii < Cuts.Count; ++ii)
            {
                m_ColumnManager.DLLMRIGeometryCutGeneratorList[ii].Reset(m_ColumnManager.SelectedMRI.Volume, Cuts[ii]);
                m_ColumnManager.DLLMRIGeometryCutGeneratorList[ii].UpdateCutMeshUV(ColumnManager.DLLCutsList[ii + 1]);
                m_ColumnManager.DLLCutsList[ii + 1].UpdateMeshFromDLL(m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<MeshFilter>().mesh);
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 5 create null uv2/uv3 arrays"); // 0%

            // create null uv2/uv3 arrays
            m_ColumnManager.UVNull = new List<Vector2[]>(m_ColumnManager.MeshSplitNumber);
            for (int ii = 0; ii < m_ColumnManager.MeshSplitNumber; ++ii)
            {
                m_ColumnManager.UVNull.Add(new Vector2[m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.vertexCount]);
                Extensions.ArrayExtensions.Fill(m_ColumnManager.UVNull[ii], new Vector2(0.01f, 1f));
            }

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-SP3DScene-Update compute_meshes_cuts 6 end"); // 0%


            // enable cuts gameobject
            for (int ii = 0; ii < Cuts.Count; ++ii)
                m_DisplayedObjects.BrainCutMeshes[ii].SetActive(true);

            SceneInformation.CollidersNeedUpdate = true; // colliders are now longer up to date

            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Changing layers");
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.ChangeMeshesLayer(LayerMask.NameToLayer(column.Layer));
            }
            if (SceneInformation.UseSimplifiedMeshes)
            {
                m_DisplayedObjects.SimplifiedBrain.layer = LayerMask.NameToLayer(SceneInformation.HiddenMeshesLayerName);
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        public void ComputeSimplifyMeshCut()
        {
            if (SceneInformation.SimplifiedMeshToUse == null) return;

            // cut the mesh
            List<DLL.Surface> cuts;
            if (Cuts.Count > 0)
                cuts = new List<DLL.Surface>(SceneInformation.SimplifiedMeshToUse.Cut(Cuts.ToArray(), !SceneInformation.CutHolesEnabled, StrongCuts));
            else
                cuts = new List<DLL.Surface>() { (DLL.Surface)SceneInformation.SimplifiedMeshToUse.Clone() };

            if (m_ColumnManager.DLLCutsList.Count != cuts.Count)
                m_ColumnManager.DLLCutsList = cuts;
            else
            {
                // swap DLL pointer
                for (int ii = 0; ii < cuts.Count; ++ii)
                    m_ColumnManager.DLLCutsList[ii].SwapDLLHandle(cuts[ii]);
            }

            m_ColumnManager.DLLCutsList[0].UpdateMeshFromDLL(m_DisplayedObjects.SimplifiedBrain.GetComponent<MeshFilter>().mesh);
            m_DisplayedObjects.SimplifiedBrain.GetComponent<MeshFilter>().mesh.RecalculateNormals();
            ResetTriangleErasing(false);

            // update cuts generators
            for (int ii = 0; ii < Cuts.Count; ++ii)
            {
                m_ColumnManager.DLLMRIGeometryCutGeneratorList[ii].Reset(m_ColumnManager.SelectedMRI.Volume, Cuts[ii]);
                m_ColumnManager.DLLMRIGeometryCutGeneratorList[ii].UpdateCutMeshUV(ColumnManager.DLLCutsList[ii + 1]);
                m_ColumnManager.DLLCutsList[ii + 1].UpdateMeshFromDLL(m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<MeshFilter>().mesh);
            }

            // enable cuts gameobject
            for (int ii = 0; ii < Cuts.Count; ++ii)
                m_DisplayedObjects.BrainCutMeshes[ii].SetActive(true);

            SceneInformation.CollidersNeedUpdate = true; // colliders are now longer up to date

            UnityEngine.Profiling.Profiler.BeginSample("Changing layers");
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.ChangeMeshesLayer(LayerMask.NameToLayer(SceneInformation.HiddenMeshesLayerName));
            }
            m_DisplayedObjects.SimplifiedBrain.layer = LayerMask.NameToLayer(SceneInformation.MeshesLayerName);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Update the mesh geometry
        /// </summary>
        public void UpdateGeometry()
        {
            UpdateMeshesInformation();

            if (SceneInformation.UseSimplifiedMeshes)
            {
                ComputeSimplifyMeshCut();
            }
            if (!CuttingSimplifiedMesh)
            {
                ComputeMeshesCut();
            }
            m_ColumnManager.UpdateCubeBoundingBox(m_Cuts);

            ComputeMRITextures();
            ComputeGUITextures();
        }
        public void ApplySelectedColumnSiteStatesToOtherColumns()
        {
            Column3D selectedColumn = m_ColumnManager.SelectedColumn;
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                if (column == selectedColumn) continue;
                foreach (Site site in column.Sites)
                {
                    site.State.ApplyState(selectedColumn.SiteStateBySiteID[site.Information.FullID]);
                }
            }
            ResetIEEG(false);
        }
        /// <summary>
        /// Update the data render corresponding to the column
        /// </summary>
        /// <param name="indexColumn"></param>
        /// <returns></returns>
        public void UpdateColumnRendering(Column3D column)
        {
            if (SceneInformation.MeshGeometryNeedsUpdate) return;

            UnityEngine.Profiling.Profiler.BeginSample("TEST-updateColumnRender");

            // update cuts textures
            for (int ii = 0; ii < Cuts.Count; ++ii)
            {
                m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = column.BrainCutTextures[ii];
            }

            if (!column.IsRenderingUpToDate)
            {
                for (int i = 0; i < column.BrainSurfaceMeshes.Count; i++)
                {
                    if (column.Type != Column3D.ColumnType.IEEG || !SceneInformation.IsGeneratorUpToDate || SceneInformation.DisplayCCEPMode)
                    {
                        column.BrainSurfaceMeshes[i].GetComponent<MeshFilter>().mesh.uv2 = m_ColumnManager.UVNull[i];
                        column.BrainSurfaceMeshes[i].GetComponent<MeshFilter>().mesh.uv3 = m_ColumnManager.UVNull[i];
                    }
                    else
                    {
                        column.BrainSurfaceMeshes[i].GetComponent<MeshFilter>().mesh.uv2 = ((Column3DIEEG)column).DLLBrainTextureGenerators[i].AlphaUV;
                        column.BrainSurfaceMeshes[i].GetComponent<MeshFilter>().mesh.uv3 = ((Column3DIEEG)column).DLLBrainTextureGenerators[i].IEEGUV;
                    }
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
            column.IsRenderingUpToDate = true;
        }
        /// <summary>
        /// Update the brain and the cuts meshes colliders
        /// </summary>
        public void UpdateMeshesColliders()
        {
            if (SceneInformation.CollidersNeedUpdate)
            {
                this.StartCoroutineAsync(c_UpdateMeshesColliders());
                SceneInformation.CollidersNeedUpdate = false;
            }
        }
        /// <summary>
        /// Update the textures generator
        /// </summary>
        public void UpdateGenerator()
        {
            if (SceneInformation.MeshGeometryNeedsUpdate || CuttingSimplifiedMesh || m_UpdatingGenerator) // if update cut plane is pending, cancel action
                return;

            OnIEEGOutdated.Invoke(false);
            m_GeneratorNeedsUpdate = false;
            SceneInformation.IsGeneratorUpToDate = false;
            StartCoroutine(c_ComputeGenerators());
        }
        /// <summary>
        /// Function to be called everytime we want to reset IEEG
        /// </summary>
        public void ResetIEEG(bool hardReset = true)
        {
            m_GeneratorNeedsUpdate = true;
            SceneInformation.AreSitesUpdated = false;
            if (hardReset)
            {
                SceneInformation.IsGeneratorUpToDate = false;
                ComputeMRITextures();
                ComputeGUITextures();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idColumn"></param>
        public void UpdateCurrentRegionOfInterest(Column3D column)
        {
            if (column.SelectedROI == null)
            {
                for (int ii = 0; ii < column.Sites.Count; ++ii)
                    column.Sites[ii].State.IsOutOfROI = true;
            }
            else
            {
                bool[] maskROI = new bool[m_ColumnManager.SitesList.Count];

                // update mask ROI
                for (int ii = 0; ii < maskROI.Length; ++ii)
                    maskROI[ii] = column.Sites[ii].State.IsOutOfROI;

                column.SelectedROI.UpdateMask(column.RawElectrodes, maskROI);
                for (int ii = 0; ii < column.Sites.Count; ++ii)
                    column.Sites[ii].State.IsOutOfROI = maskROI[ii];
            }
            ResetIEEG(false);
        }
        /// <summary>
        /// Manage the mouse movments event in the scene
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="mousePosition"></param>
        /// /// <param name="idColumn"></param>
        public void PassiveRaycastOnScene(Ray ray, Column3D column)
        {
            // update colliders if necessary
            UpdateMeshesColliders();

            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(column.Layer);
            layerMask |= 1 << LayerMask.NameToLayer(SceneInformation.HiddenMeshesLayerName);
            layerMask |= 1 << LayerMask.NameToLayer(SceneInformation.MeshesLayerName);

            RaycastHit hit;
            bool isCollision = Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, layerMask);
            if (!isCollision)
            {
                ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new SiteInfo(null, false, Input.mousePosition));
                return;
            }

            Site site = hit.collider.GetComponent<Site>();
            if (!site)
            {
                ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new SiteInfo(null, false, Input.mousePosition));
                return;
            }

            switch (column.Type)
            {
                case Column3D.ColumnType.Base:
                    ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new SiteInfo(site, true, Input.mousePosition, Data.Enums.SiteInformationDisplayMode.Anatomy));
                    break;
                case Column3D.ColumnType.IEEG:
                    Column3DIEEG columnIEEG = column as Column3DIEEG;
                    int siteID = site.Information.GlobalID;

                    float iEEGActivity = -1;
                    string iEEGUnit = columnIEEG.IEEGUnitsBySiteID[siteID];
                    if (columnIEEG.IEEGValuesBySiteID.Length > 0)
                    {
                        iEEGActivity = columnIEEG.IEEGValuesBySiteID[siteID][columnIEEG.CurrentTimeLineID];
                    }
                    bool amplitudesComputed = SceneInformation.IsGeneratorUpToDate;
                    switch (Type)
                    {
                        case Data.Enums.SceneType.SinglePatient:
                            string CCEPLatency = "none", CCEPAmplitude = "none";
                            if (columnIEEG.CurrentLatencyFile != -1)
                            {
                                Latencies latencyFile = m_ColumnManager.SelectedImplantation.Latencies[columnIEEG.CurrentLatencyFile];

                                if (columnIEEG.SelectedSiteID == -1) // no source selected
                                {
                                    CCEPLatency = "...";
                                    CCEPAmplitude = "no source selected";
                                }
                                else if (columnIEEG.SelectedSiteID == siteID) // site is the source
                                {
                                    CCEPLatency = "0";
                                    CCEPAmplitude = "source";
                                }
                                else
                                {
                                    if (latencyFile.IsSiteResponsiveForSource(siteID, columnIEEG.SelectedSiteID))
                                    {
                                        CCEPLatency = "" + latencyFile.LatenciesValues[columnIEEG.SelectedSiteID][siteID];
                                        CCEPAmplitude = "" + latencyFile.LatenciesValues[columnIEEG.SelectedSiteID][siteID];
                                    }
                                    else
                                    {
                                        CCEPLatency = "No data";
                                        CCEPAmplitude = "No data";
                                    }
                                }
                            }
                            ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new SiteInfo(site, true, Input.mousePosition, SceneInformation.DisplayCCEPMode ? Data.Enums.SiteInformationDisplayMode.CCEP : amplitudesComputed ? Data.Enums.SiteInformationDisplayMode.IEEG : Data.Enums.SiteInformationDisplayMode.Anatomy, iEEGActivity.ToString("0.00"), iEEGUnit, CCEPAmplitude, CCEPLatency));
                            break;
                        case Data.Enums.SceneType.MultiPatients:
                            ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new SiteInfo(site, true, Input.mousePosition, amplitudesComputed ? Data.Enums.SiteInformationDisplayMode.IEEG : Data.Enums.SiteInformationDisplayMode.Anatomy, iEEGActivity.ToString("0.00"), iEEGUnit));
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// Manage the clicks on the scene
        /// </summary>
        /// <param name="ray"></param>
        public void ClickOnScene(Ray ray)
        {
            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(m_ColumnManager.SelectedColumn.Layer);
            layerMask |= 1 << LayerMask.NameToLayer(SceneInformation.HiddenMeshesLayerName);
            layerMask |= 1 << LayerMask.NameToLayer(SceneInformation.MeshesLayerName);

            RaycastHit hit;
            bool isCollision = Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, layerMask);
            if (!isCollision)
            {
                m_ColumnManager.SelectedColumn.UnselectSite();
                ROI selectedROI = m_ColumnManager.SelectedColumn.SelectedROI;
                if (selectedROI)
                {
                    selectedROI.SelectSphere(-1);
                }
                return;
            }

            // FIXME : maybe create a component instead of checking the name of the parent
            bool cutHit = hit.transform.parent.gameObject.name == "Cuts";
            bool meshHit = hit.transform.parent.gameObject.name == "Brains" || hit.transform.parent.gameObject.name == "Erased Brains";
            bool siteHit = hit.collider.GetComponent<Site>() != null;
            bool roiHit = hit.collider.GetComponent<Sphere>() != null;
            Vector3 hitPoint = hit.point - transform.position;

            if (siteHit)
            {
                m_ColumnManager.SelectedColumn.Sites[hit.collider.gameObject.GetComponent<Site>().Information.GlobalID].IsSelected = true;
            }
            else
            {
                if (SceneInformation.IsROICreationModeEnabled)
                {
                    ROI selectedROI = m_ColumnManager.SelectedColumn.SelectedROI;
                    if (selectedROI)
                    {
                        if (roiHit)
                        {
                            if (m_ColumnManager.SelectedColumn.SelectedROI.CheckCollision(ray))
                            {
                                int bubbleID = m_ColumnManager.SelectedColumn.SelectedROI.CollidedClosestBubbleID(ray);
                                selectedROI.SelectSphere(bubbleID);
                            }
                        }
                        else if (meshHit || cutHit)
                        {
                            selectedROI.AddBubble(m_ColumnManager.SelectedColumn.Layer, "Bubble", hitPoint, 5.0f);
                            SceneInformation.AreSitesUpdated = false;
                        }
                        else
                        {
                            selectedROI.SelectSphere(-1);
                        }
                    }
                }
                else
                {
                    if (meshHit)
                    {
                        if (m_TriEraser.IsEnabled && m_TriEraser.IsClickAvailable)
                        {
                            m_TriEraser.EraseTriangles(ray.direction, hitPoint);
                            UpdateMeshesFromDLL();
                        }
                    }
                }
            }

            if (!siteHit)
            {
                m_ColumnManager.SelectedColumn.UnselectSite();
            }
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// Initialize the scene
        /// </summary>
        /// <param name="visualization"></param>
        /// <param name="onChangeProgress"></param>
        /// <param name="outPut"></param>
        /// <returns></returns>
        public IEnumerator c_Initialize(Visualization visualization, GenericEvent<float, float, string> onChangeProgress, Action<Exception> outPut)
        {
            Exception exception = null;

            // Find all usable implantations
            List<string> usableImplantations = visualization.FindUsableImplantations();

            // Compute progress variables
            float progress = 1.0f;
            float totalTime = 0, loadingMeshProgress = 0, loadingMeshTime = 0, loadingMRIProgress = 0, loadingMRITime = 0, loadingImplantationsProgress = 0, loadingImplantationsTime = 0, loadingMNIProgress = 0, loadingMNITime = 0, loadingIEEGProgress = 0, loadingIEEGTime = 0;
            if (Type == Data.Enums.SceneType.SinglePatient)
            {
                totalTime = Patients[0].Brain.Meshes.Count * LOADING_MESH_WEIGHT + Patients[0].Brain.MRIs.Count * LOADING_MRI_WEIGHT + usableImplantations.Count * LOADING_IMPLANTATIONS_WEIGHT + LOADING_MNI_WEIGHT + LOADING_IEEG_WEIGHT;
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
                totalTime = usableImplantations.Count * LOADING_IMPLANTATIONS_WEIGHT + LOADING_MNI_WEIGHT + Patients.Count * LOADING_IEEG_WEIGHT;
                loadingImplantationsProgress = (Patients.Count * LOADING_IMPLANTATIONS_WEIGHT) / totalTime;
                loadingImplantationsTime = (Patients.Count * LOADING_IMPLANTATIONS_WEIGHT) / 1000.0f;
                loadingMNIProgress = LOADING_MNI_WEIGHT / totalTime;
                loadingMNITime = LOADING_MNI_WEIGHT / 1000.0f;
                loadingIEEGProgress = (Patients.Count * LOADING_IEEG_WEIGHT) / totalTime;
                loadingIEEGTime = (Patients.Count * LOADING_IEEG_WEIGHT) / 1000.0f;
            }
            yield return Ninja.JumpToUnity;
            onChangeProgress.Invoke(progress, 0.0f, "");

            // Checking MNI
            onChangeProgress.Invoke(progress, 0.0f, "Loading MNI");
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            yield return new WaitUntil(delegate { return ApplicationState.Module3D.MNIObjects.Loaded || watch.ElapsedMilliseconds > 5000; });
            watch.Stop();
            if (watch.ElapsedMilliseconds > 5000)
            {
                outPut(new CanNotLoadMNI());
                yield break;
            }

            // Loading MNI
            progress += loadingMNIProgress;
            onChangeProgress.Invoke(progress, loadingMNITime, "Loading MNI objects");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadMNIObjects(e => exception = e));
            if (exception != null)
            {
                outPut(exception);
                yield break;
            }

            // Loading Meshes
            if (Type == Data.Enums.SceneType.SinglePatient)
            {
                for (int i = 0; i < Patients[0].Brain.Meshes.Count; ++i)
                {
                    Data.Anatomy.Mesh mesh = Patients[0].Brain.Meshes[i];
                    progress += loadingMeshProgress;
                    onChangeProgress.Invoke(progress, loadingMeshTime, "Loading Mesh: " + mesh.Name + " [" + (i + 1).ToString() + "/" + Patients[0].Brain.Meshes.Count + "]");
                    yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadBrainSurface(mesh, e => exception = e));
                }
                if (exception != null)
                {
                    outPut(exception);
                    yield break;
                }

                if (m_ColumnManager.Meshes.Count > 0)
                {
                    if (ApplicationState.UserPreferences.Data.Anatomy.PreloadMeshes)
                    {
                        GenerateSplits(from mesh3D in m_ColumnManager.Meshes select mesh3D.Both);
                    }
                    else
                    {
                        ResetSplitsNumber(10);
                    }
                }
                else
                {
                    ResetSplitsNumber(3);
                }
            }
            else
            {
                ResetSplitsNumber(3);
            }
            SceneInformation.MeshGeometryNeedsUpdate = true;

            // Loading MRIs
            if (Type == Data.Enums.SceneType.SinglePatient)
            {
                for (int i = 0; i < Patients[0].Brain.MRIs.Count; ++i)
                {
                    Data.Anatomy.MRI mri = Patients[0].Brain.MRIs[i];
                    progress += loadingMRIProgress;
                    onChangeProgress.Invoke(progress, loadingMRITime, "Loading MRI: " + mri.Name + " [" + (i + 1).ToString() + "/" + Patients[0].Brain.MRIs.Count + "]");
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
                onChangeProgress.Invoke(progress, loadingImplantationsTime, "Loading implantations [" + (i + 1).ToString() + "/" + usableImplantations.Count + "]");
            }, e => exception = e));
            if (exception != null)
            {
                outPut(exception);
                yield break;
            }

            // Loading Columns
            m_ColumnManager.Initialize(Cuts.Count);
            progress += loadingIEEGProgress;
            onChangeProgress.Invoke(progress, loadingIEEGTime, "Loading columns");
            yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadColumns(e => exception = e));
            if (exception != null)
            {
                outPut(exception);
                yield break;
            }

            // Finalization
            m_ColumnManager.InitializeColumnsMeshes(m_DisplayedObjects.BrainSurfaceMeshesParent, SceneInformation.UseSimplifiedMeshes);
            OnUpdateCameraTarget.Invoke(m_ColumnManager.SelectedMesh.Both.BoundingBox.Center);
            outPut(exception);
        }
        /// <summary>
        /// Reset the volume of the scene
        /// </summary>
        /// <param name="pathNIIBrainVolumeFile"></param>
        /// <returns></returns>
        private IEnumerator c_LoadBrainVolume(Data.Anatomy.MRI mri, Action<Exception> outPut)
        {
            try
            {
                if (mri.Usable)
                {

                    MRI3D mri3D = new MRI3D(mri);
                    if (ApplicationState.UserPreferences.Data.Anatomy.PreloadMRIs)
                    {
                        if (mri3D.IsLoaded)
                        {
                            m_ColumnManager.MRIs.Add(mri3D);
                        }
                        else
                        {
                            throw new CanNotLoadNIIFile(mri.File);
                        }
                    }
                    else
                    {
                        string name = !string.IsNullOrEmpty(Visualization.Configuration.MeshName) ? Visualization.Configuration.MeshName : Type == Data.Enums.SceneType.SinglePatient ? "Preimplantation" : "MNI";
                        if (mri3D.Name == name) mri3D.Load();
                        m_ColumnManager.MRIs.Add(mri3D);
                    }
                }
            }
            catch (Exception e)
            {
                outPut(new CanNotLoadNIIFile(mri.File));
                yield break;
            }
        }
        /// <summary>
        /// Reset the meshes of the scene with GII files
        /// </summary>
        /// <param name="pathGIIBrainFiles"></param>
        /// <param name="transformation"></param>
        /// <returns></returns>
        private IEnumerator c_LoadBrainSurface(Data.Anatomy.Mesh mesh, Action<Exception> outPut)
        {
            try
            {
                if (mesh.Usable)
                {
                    if (mesh is Data.Anatomy.LeftRightMesh)
                    {
                        LeftRightMesh3D mesh3D = new LeftRightMesh3D((Data.Anatomy.LeftRightMesh)mesh);

                        if (ApplicationState.UserPreferences.Data.Anatomy.PreloadMeshes)
                        {
                            if (mesh3D.IsLoaded)
                            {
                                m_ColumnManager.Meshes.Add(mesh3D);
                            }
                            else
                            {
                                throw new CanNotLoadGIIFile(mesh.Name);
                            }
                        }
                        else
                        {
                            string name = !string.IsNullOrEmpty(Visualization.Configuration.MeshName) ? Visualization.Configuration.MeshName : "Grey matter";
                            if (mesh3D.Name == name) mesh3D.Load();
                            m_ColumnManager.Meshes.Add(mesh3D);
                        }
                    }
                    else if (mesh is Data.Anatomy.SingleMesh)
                    {
                        SingleMesh3D mesh3D = new SingleMesh3D((Data.Anatomy.SingleMesh)mesh);

                        if (ApplicationState.UserPreferences.Data.Anatomy.PreloadMeshes)
                        {
                            if (mesh3D.IsLoaded)
                            {
                                m_ColumnManager.Meshes.Add(mesh3D);
                            }
                            else
                            {
                                throw new CanNotLoadGIIFile(mesh.Name);
                            }
                        }
                        else
                        {
                            string name = !string.IsNullOrEmpty(Visualization.Configuration.MeshName) ? Visualization.Configuration.MeshName : "Grey matter";
                            if (mesh3D.Name == name) mesh3D.Load();
                            m_ColumnManager.Meshes.Add(mesh3D);
                        }
                    }
                    else
                    {
                        Debug.LogError("Mesh not handled.");
                    }
                }
            }
            catch (Exception e)
            {
                outPut(new CanNotLoadGIIFile(mesh.Name));
                yield break;
            }
            yield return true;
        }
        /// <summary>
        /// Reset all the sites with a new list of pts files
        /// </summary>
        /// <param name="pathsElectrodesPtsFile"></param>
        /// <returns></returns>
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
                    IEnumerable<string> ptsFiles = (from patient in patients select patient.Brain.Implantations.Find((imp) => imp.Name == implantationName).File);
                    IEnumerable<string> marsAtlasFiles = (from patient in patients select patient.Brain.Implantations.Find((imp) => imp.Name == implantationName).MarsAtlas);
                    IEnumerable<string> patientIDs = (from patient in patients select patient.ID);

                    Implantation3D implantation3D = new Implantation3D(implantationName, ptsFiles, marsAtlasFiles, patientIDs);
                    if (implantation3D.IsLoaded)
                    {
                        m_ColumnManager.Implantations.Add(implantation3D);
                        if (Type == Data.Enums.SceneType.SinglePatient)
                        {
                            implantation3D.LoadLatencies(Patients[0]);
                        }
                    }
                    else
                    {
                        throw new CanNotLoadImplantation(implantationName);
                    }
                }
                catch
                {
                    outPut(new CanNotLoadImplantation(implantationName));
                    yield break;
                }
            }
            
            yield return Ninja.JumpToUnity;
            UpdateSites("");
            yield return Ninja.JumpBack;
        }
        /// <summary>
        /// Load MNI
        /// </summary>
        /// <returns></returns>
        private IEnumerator c_LoadMNIObjects(Action<Exception> outPut)
        {
            try
            {
                m_ColumnManager.Meshes.Add((LeftRightMesh3D)(ApplicationState.Module3D.MNIObjects.GreyMatter.Clone()));
                m_ColumnManager.Meshes.Add((LeftRightMesh3D)(ApplicationState.Module3D.MNIObjects.WhiteMatter.Clone()));
                m_ColumnManager.Meshes.Add((LeftRightMesh3D)(ApplicationState.Module3D.MNIObjects.InflatedWhiteMatter.Clone()));
                m_ColumnManager.MRIs.Add(ApplicationState.Module3D.MNIObjects.MRI);
            }
            catch
            {
                outPut(new CanNotLoadMNI());
                yield break;
            }
        }
        /// <summary>
        /// Define the timeline data with a patient and a list of column data
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="columnDataList"></param>
        private IEnumerator c_LoadColumns(Action<Exception> outPut)
        {
            yield return Ninja.JumpToUnity;
            // update columns number
            m_ColumnManager.InitializeColumns(Visualization.Columns);
            yield return Ninja.JumpBack;

            try
            {
                // update columns names
                for (int ii = 0; ii < Visualization.Columns.Count; ++ii)
                {
                    m_ColumnManager.Columns[ii].Label = Visualization.Columns[ii].Name;
                }

                // set timelines
                m_ColumnManager.SetTimelineData(Visualization.Columns);
            }
            catch (Exception e)
            {
                outPut(e);
                yield break;
            }
        }
        /// <summary>
        /// Load missing anatomy if not preloaded
        /// </summary>
        /// <returns></returns>
        private IEnumerator c_LoadMissingAnatomy()
        {
            yield return Ninja.JumpBack;
            foreach (var mesh in m_ColumnManager.Meshes)
            {
                if (!mesh.IsLoaded) mesh.Load();
            }
            foreach (var mri in m_ColumnManager.MRIs)
            {
                if (!mri.IsLoaded) mri.Load();
            }
        }
        /// <summary>
        /// Start the update of the generators for the iEEG signal on the brain
        /// </summary>
        /// <returns></returns>
        private IEnumerator c_ComputeGenerators()
        {
            m_UpdatingGenerator = true;
            yield return this.StartCoroutineAsync(c_LoadIEEG());
            m_UpdatingGenerator = false;

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
        /// <returns></returns>
        private IEnumerator c_LoadIEEG()
        {
            yield return Ninja.JumpToUnity;
            float totalProgress = m_ColumnManager.ColumnsIEEG.Count * (m_ColumnManager.MeshSplitNumber + m_Cuts.Count + 1);
            float currentProgress = 0.0f;
            OnProgressUpdateGenerator.Invoke(currentProgress / totalProgress, "Initializing");
            yield return Ninja.JumpBack;
            bool addValues = false;

            // copy from main generators
            for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
            {
                for (int jj = 0; jj < m_ColumnManager.MeshSplitNumber; ++jj)
                {
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].Dispose();
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj] = (DLL.MRIBrainGenerator)m_ColumnManager.DLLCommonBrainTextureGeneratorList[jj].Clone();
                    if (m_GeneratorNeedsUpdate) yield break;
                }
            }

            // Do your threaded task
            for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
            {
                yield return Ninja.JumpToUnity;
                OnProgressUpdateGenerator.Invoke(++currentProgress / totalProgress, "Loading " + m_ColumnManager.ColumnsIEEG[ii].Label);
                yield return Ninja.JumpBack;

                float currentMaxDensity, currentMinInfluence, currentMaxInfluence;
                float maxDensity = 1;

                m_ColumnManager.ColumnsIEEG[ii].SharedMinInf = float.MaxValue;
                m_ColumnManager.ColumnsIEEG[ii].SharedMaxInf = float.MinValue;

                // update raw electrodes
                m_ColumnManager.ColumnsIEEG[ii].UpdateDLLSitesMask();

                // splits
                for (int jj = 0; jj < m_ColumnManager.MeshSplitNumber; ++jj)
                {
                    yield return Ninja.JumpToUnity;
                    OnProgressUpdateGenerator.Invoke(++currentProgress / totalProgress, "Loading " + m_ColumnManager.ColumnsIEEG[ii].Label);
                    yield return Ninja.JumpBack;
                    if (m_GeneratorNeedsUpdate) yield break;
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].InitializeOctree(m_ColumnManager.ColumnsIEEG[ii].RawElectrodes);
                    if (m_GeneratorNeedsUpdate) yield break;
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].ComputeDistances(m_ColumnManager.ColumnsIEEG[ii].IEEGParameters.MaximumInfluence, ApplicationState.UserPreferences.General.System.MultiThreading);
                    if (m_GeneratorNeedsUpdate) yield break;
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].ComputeInfluences(m_ColumnManager.ColumnsIEEG[ii], ApplicationState.UserPreferences.General.System.MultiThreading, addValues, (int)ApplicationState.UserPreferences.Visualization._3D.SiteInfluence);
                    if (m_GeneratorNeedsUpdate) yield break;

                    currentMaxDensity = m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].MaximumDensity;
                    currentMinInfluence = m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].MinimumInfluence;
                    currentMaxInfluence = m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].MaximumInfluence;

                    if (currentMaxDensity > maxDensity)
                        maxDensity = currentMaxDensity;

                    if (currentMinInfluence < m_ColumnManager.ColumnsIEEG[ii].SharedMinInf)
                        m_ColumnManager.ColumnsIEEG[ii].SharedMinInf = currentMinInfluence;

                    if (currentMaxInfluence > m_ColumnManager.ColumnsIEEG[ii].SharedMaxInf)
                        m_ColumnManager.ColumnsIEEG[ii].SharedMaxInf = currentMaxInfluence;

                }

                // cuts
                for (int jj = 0; jj < m_Cuts.Count; ++jj)
                {
                    yield return Ninja.JumpToUnity;
                    OnProgressUpdateGenerator.Invoke(++currentProgress / totalProgress, "Loading " + m_ColumnManager.ColumnsIEEG[ii].Label);
                    yield return Ninja.JumpBack;
                    if (m_GeneratorNeedsUpdate) yield break;
                    m_ColumnManager.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].InitializeOctree(m_ColumnManager.ColumnsIEEG[ii].RawElectrodes);
                    if (m_GeneratorNeedsUpdate) yield break;
                    m_ColumnManager.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].ComputeDistances(m_ColumnManager.ColumnsIEEG[ii].IEEGParameters.MaximumInfluence, ApplicationState.UserPreferences.General.System.MultiThreading);
                    if (m_GeneratorNeedsUpdate) yield break;
                    m_ColumnManager.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].ComputeInfluences(m_ColumnManager.ColumnsIEEG[ii], ApplicationState.UserPreferences.General.System.MultiThreading, addValues, (int)ApplicationState.UserPreferences.Visualization._3D.SiteInfluence);
                    if (m_GeneratorNeedsUpdate) yield break;

                    currentMaxDensity = m_ColumnManager.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].MaximumDensity;
                    currentMinInfluence = m_ColumnManager.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].MinimumInfluence;
                    currentMaxInfluence = m_ColumnManager.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].MaximumInfluence;

                    if (currentMaxDensity > maxDensity)
                        maxDensity = currentMaxDensity;

                    if (currentMinInfluence < m_ColumnManager.ColumnsIEEG[ii].SharedMinInf)
                        m_ColumnManager.ColumnsIEEG[ii].SharedMinInf = currentMinInfluence;

                    if (currentMaxInfluence > m_ColumnManager.ColumnsIEEG[ii].SharedMaxInf)
                        m_ColumnManager.ColumnsIEEG[ii].SharedMaxInf = currentMaxInfluence;
                }

                // synchronize max density
                for (int jj = 0; jj < m_ColumnManager.MeshSplitNumber; ++jj)
                {
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].SynchronizeWithOthersGenerators(maxDensity, m_ColumnManager.ColumnsIEEG[ii].SharedMinInf, m_ColumnManager.ColumnsIEEG[ii].SharedMaxInf);
                    if (m_GeneratorNeedsUpdate) yield break;
                }
                for (int jj = 0; jj < m_Cuts.Count; ++jj)
                {
                    m_ColumnManager.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].SynchronizeWithOthersGenerators(maxDensity, m_ColumnManager.ColumnsIEEG[ii].SharedMinInf, m_ColumnManager.ColumnsIEEG[ii].SharedMaxInf);
                    if (m_GeneratorNeedsUpdate) yield break;
                }

                for (int jj = 0; jj < m_ColumnManager.MeshSplitNumber; ++jj)
                {
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].AdjustInfluencesToColormap(m_ColumnManager.ColumnsIEEG[ii]);
                    if (m_GeneratorNeedsUpdate) yield break;
                }
                for (int jj = 0; jj < m_Cuts.Count; ++jj)
                {
                    m_ColumnManager.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].AdjustInfluencesToColormap(m_ColumnManager.ColumnsIEEG[ii]);
                    if (m_GeneratorNeedsUpdate) yield break;
                }
            }
            yield return Ninja.JumpToUnity;
            OnProgressUpdateGenerator.Invoke(1.0f, "Finalizing");
            yield return Ninja.JumpBack;
            yield return new WaitForSeconds(0.1f);
        }
        /// <summary>
        /// Update the colliders
        /// </summary>
        /// <returns></returns>
        private IEnumerator c_UpdateMeshesColliders()
        {
            while (m_UpdatingColliders)
            {
                yield return new WaitForSeconds(0.05f);
            }
            m_UpdatingColliders = true;
            if (SceneInformation.UseSimplifiedMeshes)
            {
                yield return Ninja.JumpToUnity;
                m_DisplayedObjects.SimplifiedBrain.GetComponent<MeshCollider>().sharedMesh = null;
                m_DisplayedObjects.SimplifiedBrain.GetComponent<MeshCollider>().sharedMesh = m_DisplayedObjects.SimplifiedBrain.GetComponent<MeshFilter>().sharedMesh;
                yield return Ninja.JumpBack;
            }
            else
            {
                // update splits colliders
                for (int ii = 0; ii < m_DisplayedObjects.BrainSurfaceMeshes.Count; ++ii)
                {
                    yield return Ninja.JumpToUnity;
                    m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshCollider>().sharedMesh = null;
                    m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshCollider>().sharedMesh = m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh;
                    yield return Ninja.JumpBack;
                }
            }
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
        #endregion
    }

    #region Struct
    /// <summary>
    /// Site info to be send to the UI
    /// </summary>
    public class SiteInfo
    {
        public SiteInfo(Site site, bool enabled, Vector3 position, Data.Enums.SiteInformationDisplayMode mode = Data.Enums.SiteInformationDisplayMode.IEEG, string IEEGAmplitude = "", string IEEGUnit ="", string CCEPAmplitude = "", string CCEPLatency = "")
        {
            Site = site;
            Enabled = enabled;
            Position = position;
            this.IEEGAmplitude = IEEGAmplitude;
            this.IEEGUnit = IEEGUnit;
            this.CCEPAmplitude = CCEPAmplitude;
            this.CCEPLatency = CCEPLatency;
            Mode = mode;
        }

        public Site Site { get; set; }
        public bool Enabled { get; set; }
        public Vector3 Position { get; set; }
        public string IEEGAmplitude { get; set; }
        public string IEEGUnit { get; set; }
        public string CCEPAmplitude { get; set; }
        public string CCEPLatency { get; set; }
        public Data.Enums.SiteInformationDisplayMode Mode { get; set; }
    }

    /// <summary>
    /// IRM cal values
    /// </summary>
    public struct MRICalValues
    {
        public float min;
        public float max;
        public float loadedCalMin;
        public float loadedCalMax;
        public float computedCalMin;
        public float computedCalMax;
    }
    #endregion
}