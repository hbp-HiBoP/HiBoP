using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using CielaSpike;
using HBP.Data.Visualization;
using Tools.Unity;

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
        /// <summary>
        /// Cuts planes list
        /// </summary>
        public List<Cut> Cuts { get; private set; } = new List<Cut>();

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
                SceneInformation.CutsNeedUpdate = true;
                //ResetIEEG();
                foreach (Column3D column in m_ColumnManager.Columns)
                {
                    column.IsRenderingUpToDate = false;
                }
            }
        }
        /// <summary>
        /// Is the latency (CCEP) mode enabled ?
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
                    column.UpdateROIMask();
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
                if (m_TriEraser.IsEnabled != value)
                {
                    m_TriEraser.IsEnabled = value;
                    SceneInformation.CollidersNeedUpdate = true;
                }
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
        public Data.Enums.TriEraserMode TriangleErasingMode
        {
            get
            {
                return m_TriEraser.CurrentMode;
            }
            set
            {
                Data.Enums.TriEraserMode previousMode = m_TriEraser.CurrentMode;
                m_TriEraser.CurrentMode = value;

                if (value == Data.Enums.TriEraserMode.Expand || value == Data.Enums.TriEraserMode.Invert)
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
        /// State of the triangle erasing
        /// </summary>
        public List<int[]> TriangleErasingCurrentMasks
        {
            get
            {
                return m_TriEraser.CurrentMasks;
            }
            set
            {
                m_TriEraser.CurrentMasks = value;
                UpdateMeshesFromDLL();
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
                SharedMaterials.Brain.BrainMaterials[this].SetInt("_MarsAtlas", SceneInformation.MarsAtlasModeEnabled ? 1 : 0);
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
                        view.ShowEdges = m_EdgeMode;
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
                SharedMaterials.Brain.BrainMaterials[this].SetInt("_StrongCuts", m_StrongCuts ? 1 : 0);
                //ResetIEEG();
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

        private Data.Enums.CameraControl m_CameraType = Data.Enums.CameraControl.Trackball;
        /// <summary>
        /// Camera Control type
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
                foreach (Column3D column in m_ColumnManager.Columns)
                {
                    foreach (View3D view in column.Views)
                    {
                        view.CameraType = m_CameraType;
                    }
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

        private string m_FirstSiteToSelectName = "";
        private int m_FirstSiteToSelectColumnNumber = 0;

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
                for (int ii = 0; ii < ColumnManager.Columns.Count; ++ii)
                    if (ColumnManager.Columns[ii].SelectedROI != null)
                        ColumnManager.Columns[ii].SelectedROI.SetRenderingState(value);
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
        /// Prefab for the 3D brain mesh part
        /// </summary>
        [SerializeField] private GameObject m_BrainPrefab;
        /// <summary>
        /// Prefab for the 3D simplified brain mesh
        /// </summary>
        [SerializeField] private GameObject m_SimplifiedBrainPrefab;
        /// <summary>
        /// Prefab for the 3D invisible brain mesh part
        /// </summary>
        [SerializeField] private GameObject m_InvisibleBrainPrefab;
        /// <summary>
        /// Prefab for the 3D cut
        /// </summary>
        [SerializeField] private GameObject m_CutPrefab;
        /// <summary>
        /// Prefab for the 3D site
        /// </summary>
        [SerializeField] private GameObject m_SitePrefab;
        #endregion

        #region Events
        /// <summary>
        /// Event called when this scene is selected or deselected
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
            if (!SceneInformation.IsSceneInitialized || m_DestroyRequested) return;

            if (SceneInformation.MeshGeometryNeedsUpdate)
            {
                UpdateGeometry();
            }

            if (SceneInformation.CutsNeedUpdate)
            {
                UpdateCuts();
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
            if (!SceneInformation.IsGeneratorUpToDate && !IsLatencyModeEnabled && ApplicationState.UserPreferences.Visualization._3D.AutomaticEEGUpdate)
            {
                UpdateGenerator();
            }
            OnUpdatingGenerator.Invoke(m_UpdatingGenerator);

            if (!SceneInformation.IsSceneCompletelyLoaded)
            {
                UpdateVisibleState(true);
                if (m_FirstSiteToSelectColumnNumber < m_ColumnManager.Columns.Count)
                {
                    m_ColumnManager.Columns[m_FirstSiteToSelectColumnNumber].SelectFirstSite(m_FirstSiteToSelectName);
                }
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
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            });
            m_ColumnManager.OnUpdateMRICalValues.AddListener(() =>
            {
                ComputeMRITextures();
                ComputeGUITextures();
            });
            m_ColumnManager.OnUpdateIEEGSpan.AddListener((column) =>
            {
                for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
                {
                    for (int jj = 0; jj < m_ColumnManager.MeshSplitNumber; ++jj)
                    {
                        m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].AdjustInfluencesToColormap(m_ColumnManager.ColumnsIEEG[ii]);
                    }
                    m_ColumnManager.ColumnsIEEG[ii].DLLMRIVolumeGenerator.AdjustInfluencesToColormap(m_ColumnManager.ColumnsIEEG[ii]);
                }
                ComputeMRITextures();
                ComputeGUITextures();
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
            m_ColumnManager.OnUpdateInfluenceDistance.AddListener((column) =>
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
            m_ColumnManager.OnUpdateROIMask.AddListener(() =>
            {
                ResetIEEG(false);
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            });
            m_ColumnManager.OnChangeSelectedState.AddListener((selected) =>
            {
                IsSelected = selected;
                ComputeGUITextures();
                OnUpdateCuts.Invoke();
            });
            m_ColumnManager.OnSelectSite.AddListener(ClickOnSiteCallback);
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
                        column.Timeline.IsLooping = false;
                        column.Timeline.IsPlaying = false;
                        column.Timeline.OnUpdateCurrentIndex.Invoke();
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
        /// Compute the textures for the MRI (3D)
        /// </summary>
        private void ComputeMRITextures()
        {
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                foreach (Cut cut in Cuts)
                {
                    m_ColumnManager.CreateMRITexture(column, cut.ID, 3);
                }
            }
            ComputeIEEGTextures();
        }
        /// <summary>
        /// Compute the textures for the MRI (3D) with the iEEG activity
        /// </summary>
        /// <param name="column">Specific column to update. If null, every columns will be updated.</param>
        private void ComputeIEEGTextures(Column3DIEEG column = null)
        {
            if (!SceneInformation.IsGeneratorUpToDate) return;

            UnityEngine.Profiling.Profiler.BeginSample("Compute IEEG Textures");
            if (column)
            {
                m_ColumnManager.ComputeSurfaceBrainUVWithIEEG(column);
                column.CutTextures.ColorCutsTexturesWithIEEG(column);
            }
            else
            {
                foreach (Column3DIEEG col in m_ColumnManager.ColumnsIEEG)
                {
                    m_ColumnManager.ComputeSurfaceBrainUVWithIEEG(col);
                    col.CutTextures.ColorCutsTexturesWithIEEG(col);
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Compute the texture for the MRI (GUI)
        /// </summary>
        private void ComputeGUITextures()
        {
            Column3D column = m_ColumnManager.SelectedColumn;
            if (column)
            {
                column.CutTextures.CreateGUIMRITextures(Cuts);
                column.CutTextures.ResizeGUIMRITextures(Cuts);
                column.CutTextures.UpdateTextures2D();
                foreach (Cut cut in Cuts)
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
                m_ColumnManager.ColumnsIEEG[ii].SendColormapValues();
                m_ColumnManager.ColumnsIEEG[ii].Timeline.OnUpdateCurrentIndex.Invoke();
            }

            // update plots visibility
            SceneInformation.AreSitesUpdated = false;

            OnIEEGOutdated.Invoke(false);
        }
        /// <summary>
        /// Actions to perform after clicking on a site
        /// </summary>
        /// <param name="site">Site that has been selected</param>
        private void ClickOnSiteCallback(Site site)
        {
            if (m_ColumnManager.SelectedColumn.Type == Data.Enums.ColumnType.iEEG && !IsLatencyModeEnabled && site)
            {
                List<Site> sites = new List<Site>();
                if (m_SiteToCompare) sites.Add(m_SiteToCompare);
                sites.Add(site);
                OnRequestSiteInformation.Invoke(sites);
            }
            SceneInformation.AreSitesUpdated = false;
            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Initialize some parameters of the scene
        /// </summary>
        private void InitializeParameters()
        {
            transform.position = new Vector3(HBP3DModule.SPACE_BETWEEN_SCENES_GAME_OBJECTS * ApplicationState.Module3D.NumberOfScenesLoadedSinceStart++, transform.position.y, transform.position.z);

            // Mark brain mesh as dynamic
            m_BrainPrefab.GetComponent<MeshFilter>().sharedMesh.MarkDynamic();

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
                m_ColumnManager.SplittedMeshes[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
            }
            UnityEngine.Profiling.Profiler.BeginSample("Update Columns Meshes");
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.UpdateColumnMeshes(m_DisplayedObjects.BrainSurfaceMeshes);
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Generate the split number regarding all meshes
        /// </summary>
        /// <param name="meshes">Meshes loaded</param>
        private void GenerateSplits(IEnumerable<DLL.Surface> meshes)
        {
            int maxVertices = (from mesh in meshes select mesh.NumberOfVertices).Max();
            int splits = (maxVertices / 65000) + (((maxVertices % 60000) != 0) ? 3 : 2);
            if (splits < 3) splits = 3;
            ResetSplitsNumber(splits);
        }
        /// <summary>
        /// Reset the number of splits of the brain mesh
        /// </summary>
        /// <param name="nbSplits">Number of splits</param>
        private void ResetSplitsNumber(int nbSplits)
        {
            if (m_ColumnManager.MeshSplitNumber == nbSplits) return;

            m_ColumnManager.ResetSplitsNumber(nbSplits);

            if (m_DisplayedObjects.BrainSurfaceMeshes.Count > 0)
                for (int ii = 0; ii < m_DisplayedObjects.BrainSurfaceMeshes.Count; ++ii)
                    Destroy(m_DisplayedObjects.BrainSurfaceMeshes[ii]);

            // reset meshes
            m_DisplayedObjects.BrainSurfaceMeshes = new List<GameObject>(nbSplits);
            for (int ii = 0; ii < nbSplits; ++ii)
            {
                m_DisplayedObjects.BrainSurfaceMeshes.Add(Instantiate(m_BrainPrefab));
                m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<Renderer>().sharedMaterial = SharedMaterials.Brain.BrainMaterials[this];
                m_DisplayedObjects.BrainSurfaceMeshes[ii].name = "brain_" + ii;
                m_DisplayedObjects.BrainSurfaceMeshes[ii].transform.parent = m_DisplayedObjects.BrainSurfaceMeshesParent.transform;
                m_DisplayedObjects.BrainSurfaceMeshes[ii].transform.localPosition = Vector3.zero;
                m_DisplayedObjects.BrainSurfaceMeshes[ii].layer = LayerMask.NameToLayer(SceneInformation.HiddenMeshesLayerName);
                m_DisplayedObjects.BrainSurfaceMeshes[ii].SetActive(true);
            }
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
        /// <summary>
        /// Compute the cuts of the regular meshes
        /// </summary>
        private void ComputeMeshesCut()
        {
            if (SceneInformation.MeshToDisplay == null) return;

            // Create the cuts
            UnityEngine.Profiling.Profiler.BeginSample("cut_generator Create cut");
            List<DLL.Surface> generatedCutMeshes = new List<DLL.Surface>(Cuts.Count);
            if (Cuts.Count > 0)
                generatedCutMeshes = SceneInformation.MeshToDisplay.GenerateCutSurfaces(Cuts, !SceneInformation.CutHolesEnabled, StrongCuts);
            UnityEngine.Profiling.Profiler.EndSample();

            // Fill parameters in shader
            UnityEngine.Profiling.Profiler.BeginSample("cut_generator Fill shader");
            Material material = SharedMaterials.Brain.BrainMaterials[this];
            material.SetInt("_CutCount", Cuts.Count);
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
                material.SetVectorArray("_CutPoints", cutPoints);
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
                material.SetVectorArray("_CutNormals", cutNormals);
            }
            UnityEngine.Profiling.Profiler.EndSample();

            // Update cut generators
            UnityEngine.Profiling.Profiler.BeginSample("cut_generator Update generators");
            for (int ii = 0; ii < Cuts.Count; ++ii)
            {
                m_ColumnManager.DLLMRIGeometryCutGeneratorList[ii].Reset(m_ColumnManager.SelectedMRI.Volume, Cuts[ii]);
                m_ColumnManager.DLLMRIGeometryCutGeneratorList[ii].UpdateCutMeshUV(generatedCutMeshes[ii]);
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
        /// Update the mesh geometry
        /// </summary>
        private void UpdateGeometry()
        {
            UpdateMeshesInformation();
            UpdateCuts();
            UpdateGeneratorsAndUV();
            ResetTriangleErasing();

            SceneInformation.MeshGeometryNeedsUpdate = false;
        }
        /// <summary>
        /// Update meshes to display
        /// </summary>
        public void UpdateMeshesInformation()
        {
            if (m_ColumnManager.SelectedMesh is LeftRightMesh3D selectedMesh)
            {
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
            SceneInformation.MeshCenter = SceneInformation.MeshToDisplay.Center;

            m_ColumnManager.GenerateSplits(SceneInformation.MeshToDisplay);

            UpdateAllCutPlanes();
        }
        /// <summary>
        /// Update the cuts of the scene
        /// </summary>
        private void UpdateCuts()
        {
            ComputeMeshesCut();
            m_ColumnManager.UpdateCubeBoundingBox(Cuts);

            ComputeMRITextures();
            ComputeGUITextures();

            OnUpdateCuts.Invoke();
            SceneInformation.CutsNeedUpdate = false;
        }
        /// <summary>
        /// Update the generators for iEEG and the UV of the meshes
        /// </summary>
        private void UpdateGeneratorsAndUV()
        {
            for (int ii = 0; ii < m_ColumnManager.MeshSplitNumber; ++ii)
            {
                m_ColumnManager.DLLCommonBrainTextureGeneratorList[ii].Reset(m_ColumnManager.SplittedMeshes[ii], m_ColumnManager.SelectedMRI.Volume);
                m_ColumnManager.DLLCommonBrainTextureGeneratorList[ii].ComputeUVMainWithVolume(m_ColumnManager.SplittedMeshes[ii], m_ColumnManager.SelectedMRI.Volume, m_ColumnManager.MRICalMinFactor, m_ColumnManager.MRICalMaxFactor);
            }
            
            foreach (var column in m_ColumnManager.ColumnsIEEG)
            {
                column.DLLMRIVolumeGenerator.Reset(m_ColumnManager.SelectedMRI.Volume, 120);
            }

            UpdateMeshesFromDLL();
            m_ColumnManager.UVNull = new List<Vector2[]>(m_ColumnManager.MeshSplitNumber);
            for (int ii = 0; ii < m_ColumnManager.MeshSplitNumber; ++ii)
            {
                m_ColumnManager.UVNull.Add(new Vector2[m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.vertexCount]);
                m_ColumnManager.UVNull[ii].Fill(new Vector2(0.01f, 1f));
            }
        }
        /// <summary>
        /// Update the brain and the cuts meshes colliders
        /// </summary>
        private void UpdateMeshesColliders()
        {
            if (SceneInformation.CollidersNeedUpdate)
            {
                this.StartCoroutineAsync(c_UpdateMeshesColliders());
                SceneInformation.CollidersNeedUpdate = false;
            }
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
            tex.Dispose();

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

            SceneInformation.CutsNeedUpdate = true;
            ComputeMRITextures();
            ComputeGUITextures();
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.IsRenderingUpToDate = false;
            }
        }
        /// <summary>
        /// Set the mesh part to be displayed in the scene
        /// </summary>
        /// <param name="meshPartToDisplay">Mesh part to be displayed</param>
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
        /// <param name="meshName">Name of the mesh to be displayed</param>
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

            OnUpdateCameraTarget.Invoke(m_ColumnManager.SelectedMesh.Both.Center);
        }
        /// <summary>
        /// Set the MRI to be used
        /// </summary>
        /// <param name="mriName">Name of the MRI to be used</param>
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
        /// Set the implantation to be used
        /// </summary>
        /// <param name="implantationName">Name of the implantation to use</param>
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
                        site.Information.FreesurferLabel = electrodesList.FreesurferLabelOfSite(ii, jj, kk).Replace('_', ' ');
                        site.State.IsBlackListed = false;
                        site.State.IsHighlighted = false;
                        site.State.IsOutOfROI = true;
                        site.State.IsMasked = false;
                        site.State.Color = SiteState.DefaultColor;
                        site.IsActive = true;

                        m_ColumnManager.SitesList.Add(siteGameObject);
                    }
                }
            }
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.UpdateSites(m_ColumnManager.SelectedImplantation.PatientElectrodesList, m_ColumnManager.SitesPatientParent, m_ColumnManager.SitesList);
                column.UpdateROIMask();
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
            IsSelected = state;
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

            SceneInformation.CutsNeedUpdate = true;
            //ResetIEEG();

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

            // update columns manager
            m_ColumnManager.UpdateCutNumber(m_DisplayedObjects.BrainCutMeshes.Count);

            SceneInformation.CutsNeedUpdate = true;
            //ResetIEEG();

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
                m_ColumnManager.SelectedMRI.Volume.SetPlaneWithOrientation(plane, cut.Orientation, cut.Flip);
                cut.Normal = plane.Normal;
            }

            if (changedByUser) SceneInformation.LastPlaneModifiedID = cut.ID;

            // Cuts base on the mesh
            float offset;
            if (SceneInformation.MeshToDisplay != null)
            {
                offset = SceneInformation.MeshToDisplay.SizeOffsetCutPlane(cut, cut.NumberOfCuts);
                offset *= 1.05f; // upsize a little bit the bbox for planes
            }
            else
                offset = 0.1f;

            cut.Point = SceneInformation.MeshCenter + cut.Normal.normalized * (cut.Position - 0.5f) * offset * cut.NumberOfCuts;

            SceneInformation.CutsNeedUpdate = true;
            //ResetIEEG();

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
            Site site = ColumnManager.SelectedColumn.SelectedSite;
            if (!site) return;

            foreach (var cut in Cuts.ToList())
            {
                RemoveCutPlane(cut);
            }

            Cut axialCut = AddCutPlane();
            float axialOffset = SceneInformation.MeshToDisplay.SizeOffsetCutPlane(axialCut, axialCut.NumberOfCuts) * 1.05f;
            Vector3 axialMin = SceneInformation.MeshCenter + axialCut.Normal * (-0.5f) * axialOffset * axialCut.NumberOfCuts;
            Vector3 axialMax = SceneInformation.MeshCenter + axialCut.Normal * 0.5f * axialOffset * axialCut.NumberOfCuts;
            axialCut.Position = (site.transform.localPosition.z - axialMin.z) / (axialMax.z - axialMin.z);
            UpdateCutPlane(axialCut);

            Cut coronalCut = AddCutPlane();
            float coronalOffset = SceneInformation.MeshToDisplay.SizeOffsetCutPlane(coronalCut, coronalCut.NumberOfCuts) * 1.05f;
            Vector3 coronalMin = SceneInformation.MeshCenter + coronalCut.Normal * (-0.5f) * coronalOffset * coronalCut.NumberOfCuts;
            Vector3 coronalMax = SceneInformation.MeshCenter + coronalCut.Normal * 0.5f * coronalOffset * coronalCut.NumberOfCuts;
            coronalCut.Position = (site.transform.localPosition.y - coronalMin.y) / (coronalMax.y - coronalMin.y);
            UpdateCutPlane(coronalCut);

            Cut sagitalCut = AddCutPlane();
            float sagitalOffset = SceneInformation.MeshToDisplay.SizeOffsetCutPlane(sagitalCut, sagitalCut.NumberOfCuts) * 1.05f;
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
        /// <param name="updateGO">Do we need to update the meshes from DLL ?</param>
        public void ResetTriangleErasing()
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

            m_TriEraser.Reset(m_DisplayedObjects.InvisibleBrainSurfaceMeshes, SceneInformation.MeshToDisplay, m_ColumnManager.SplittedMeshes);

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
        /// <param name="visualization">Visualization to be loaded in this scene</param>
        public void Initialize(Visualization visualization)
        {
            SceneInformation = new SceneStatesInfo();

            Visualization = visualization;
            gameObject.name = Visualization.Name;

            // Init materials
            SharedMaterials.Brain.AddSceneMaterials(this);

            // Set default SceneInformation values
            SceneInformation.MeshesLayerName = HBP3DModule.DEFAULT_MESHES_LAYER;
            SceneInformation.HiddenMeshesLayerName = HBP3DModule.HIDDEN_MESHES_LAYER;

            AddListeners();
            InitializeParameters();
        }
        /// <summary>
        /// Set up the scene to display it properly
        /// </summary>
        public void FinalizeInitialization()
        {
            m_ColumnManager.Columns[0].Views[0].IsSelected = true; // Select default view
            m_ColumnManager.Columns[0].SelectFirstSite();
            SceneInformation.IsSceneInitialized = true;
            this.StartCoroutineAsync(c_LoadMissingAnatomy());
        }
        /// <summary>
        /// Load the visualization configuration from the loaded visualization
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another load method ?</param>
        public void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration(false);
            UpdateBrainSurfaceColor(Visualization.Configuration.BrainColor);
            UpdateBrainCutColor(Visualization.Configuration.BrainCutColor);
            UpdateColormap(Visualization.Configuration.EEGColormap);
            UpdateMeshPartToDisplay(Visualization.Configuration.MeshPart);
            EdgeMode = Visualization.Configuration.ShowEdges;
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
            Visualization.Configuration.EEGColormap = m_ColumnManager.Colormap;
            Visualization.Configuration.MeshPart = SceneInformation.MeshPartToDisplay;
            Visualization.Configuration.MeshName = m_ColumnManager.SelectedMesh.Name;
            Visualization.Configuration.MRIName = m_ColumnManager.SelectedMRI.Name;
            Visualization.Configuration.ImplantationName = m_ColumnManager.SelectedImplantation.Name;
            Visualization.Configuration.ShowEdges = EdgeMode;
            Visualization.Configuration.StrongCuts = StrongCuts;
            Visualization.Configuration.HideBlacklistedSites = HideBlacklistedSites;
            Visualization.Configuration.ShowAllSites = ShowAllSites;
            Visualization.Configuration.MRICalMinFactor = m_ColumnManager.MRICalMinFactor;
            Visualization.Configuration.MRICalMaxFactor = m_ColumnManager.MRICalMaxFactor;
            Visualization.Configuration.CameraType = CameraType;

            List<Data.Visualization.Cut> cuts = new List<Data.Visualization.Cut>();
            foreach (Cut cut in Cuts)
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
        /// <param name="firstCall">Has this method not been called by another reset method ?</param>
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
            CameraType = Data.Enums.CameraControl.Trackball;

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

            while (Cuts.Count > 0)
            {
                RemoveCutPlane(Cuts.Last());
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
        #endregion

        /// <summary>
        /// Load a FMRI to this scene
        /// </summary>
        public void LoadFMRI(MRI3D fmri)
        {
            m_ColumnManager.FMRI = fmri;
            ResetIEEG();
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
        /// Copy the states of the sites of the selected column to all other columns
        /// </summary>
        public void ApplySelectedColumnSiteStatesToOtherColumns()
        {
            Column3D selectedColumn = m_ColumnManager.SelectedColumn;
            foreach (Column3D column in m_ColumnManager.Columns)
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
        /// Select a site on a column given its name
        /// </summary>
        /// <param name="siteName"></param>
        public void SelectFirstSite(string siteName = "", int columnNumber = 0)
        {
            m_FirstSiteToSelectName = siteName;
            m_FirstSiteToSelectColumnNumber = 0;
        }
        /// <summary>
        /// Update the data rendering for a column
        /// </summary>
        /// <param name="column">Column to be updated</param>
        /// <returns></returns>
        public void UpdateColumnRendering(Column3D column)
        {
            if (SceneInformation.MeshGeometryNeedsUpdate) return;

            UnityEngine.Profiling.Profiler.BeginSample("TEST-updateColumnRender");

            // update cuts textures
            for (int ii = 0; ii < Cuts.Count; ++ii)
            {
                m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = column.CutTextures.BrainCutTextures[ii];
            }

            if (!column.IsRenderingUpToDate)
            {
                for (int i = 0; i < column.BrainSurfaceMeshes.Count; i++)
                {
                    if (column.Type != Data.Enums.ColumnType.iEEG || !SceneInformation.IsGeneratorUpToDate || SceneInformation.DisplayCCEPMode)
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
        /// Update the textures generator for iEEG
        /// </summary>
        public void UpdateGenerator()
        {
            if (SceneInformation.MeshGeometryNeedsUpdate || m_UpdatingGenerator) // if update cut plane is pending, cancel action
                return;

            OnIEEGOutdated.Invoke(false);
            m_GeneratorNeedsUpdate = false;
            SceneInformation.IsGeneratorUpToDate = false;
            StartCoroutine(c_ComputeGenerators());
        }
        /// <summary>
        /// Function to be called everytime we want to reset IEEG
        /// </summary>
        /// <param name="hardReset">Do we need to hard reset (delete the activity on the brain) ?</param>
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
        /// Passive raycast on the scene (to hover sites for instance)
        /// </summary>
        /// <param name="ray">Ray of the raycast</param>
        /// <param name="column">Column on which the raycast in performed</param>
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

            // Compute each required variable
            int siteID = site.Information.GlobalID;
            string CCEPLatency = "none", CCEPAmplitude = "none";
            float iEEGActivity = -1;
            string iEEGUnit = "";
            // CCEP
            if (SceneInformation.DisplayCCEPMode)
            {
                if (column.CurrentLatencyFile != -1)
                {
                    Latencies latencyFile = m_ColumnManager.SelectedImplantation.Latencies[column.CurrentLatencyFile];

                    if (column.SelectedSiteID == -1) // no source selected
                    {
                        CCEPLatency = "...";
                        CCEPAmplitude = "no source selected";
                    }
                    else if (column.SelectedSiteID == siteID) // site is the source
                    {
                        CCEPLatency = "0";
                        CCEPAmplitude = "source";
                    }
                    else
                    {
                        if (latencyFile.IsSiteResponsiveForSource(siteID, column.SelectedSiteID))
                        {
                            CCEPLatency = "" + latencyFile.LatenciesValues[column.SelectedSiteID][siteID];
                            CCEPAmplitude = "" + latencyFile.Heights[column.SelectedSiteID][siteID];
                        }
                        else
                        {
                            CCEPLatency = "No data";
                            CCEPAmplitude = "No data";
                        }
                    }
                }
            }
            // iEEG
            if (column is Column3DIEEG columnIEEG)
            {
                iEEGUnit = columnIEEG.IEEGUnitsBySiteID[siteID];
                iEEGActivity = columnIEEG.IEEGValuesBySiteID[siteID][columnIEEG.Timeline.CurrentIndex];
            }
            // Send Event
            Data.Enums.SiteInformationDisplayMode displayMode;
            if (SceneInformation.DisplayCCEPMode)
            {
                if (SceneInformation.IsGeneratorUpToDate)
                {
                    displayMode = Data.Enums.SiteInformationDisplayMode.IEEGCCEP;
                }
                else
                {
                    displayMode = Data.Enums.SiteInformationDisplayMode.CCEP;
                }
            }
            else
            {
                if (SceneInformation.IsGeneratorUpToDate)
                {
                    displayMode = Data.Enums.SiteInformationDisplayMode.IEEG;
                }
                else
                {
                    displayMode = Data.Enums.SiteInformationDisplayMode.Anatomy;
                }
            }
            ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new SiteInfo(site, true, Input.mousePosition, displayMode, iEEGActivity.ToString("0.00"), iEEGUnit, CCEPAmplitude, CCEPLatency));
        }
        /// <summary>
        /// Manage the clicks on the scene
        /// </summary>
        /// <param name="ray">Ray of the raycast</param>
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
        /// <param name="visualization">Visualization to load in the scene</param>
        /// <param name="onChangeProgress">Event to update the loading circle</param>
        /// <param name="outPut">Action to execute if an exception is raised</param>
        /// <returns></returns>
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
            onChangeProgress.Invoke(progress, 0.0f, new LoadingText());

            // Checking MNI
            onChangeProgress.Invoke(progress, 0.0f, new LoadingText("Loading MNI"));
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
                for (int i = 0; i < Patients[0].Brain.Meshes.Count; ++i)
                {
                    Data.Anatomy.Mesh mesh = Patients[0].Brain.Meshes[i];
                    progress += loadingMeshProgress;
                    onChangeProgress.Invoke(progress, loadingMeshTime, new LoadingText("Loading Mesh ", mesh.Name, " [" + (i + 1).ToString() + "/" + Patients[0].Brain.Meshes.Count + "]"));
                    yield return ApplicationState.CoroutineManager.StartCoroutineAsync(c_LoadBrainSurface(mesh, e => exception = e));
                }
                if (exception != null)
                {
                    outPut(exception);
                    yield break;
                }

                if (m_ColumnManager.Meshes.Count > 0)
                {
                    if (ApplicationState.UserPreferences.Data.Anatomic.MeshPreloading)
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
                    onChangeProgress.Invoke(progress, loadingMRITime, new LoadingText("Loading MRI ", mri.Name, " [" + (i + 1).ToString() + "/" + Patients[0].Brain.MRIs.Count + "]"));
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
            m_ColumnManager.InitializeColumnsMeshes(m_DisplayedObjects.BrainSurfaceMeshesParent);
            OnUpdateCameraTarget.Invoke(m_ColumnManager.SelectedMesh.Both.Center);
            outPut(exception);
        }
        /// <summary>
        /// Load a MRI to the scene
        /// </summary>
        /// <param name="mri">MRI to load</param>
        /// <param name="outPut">Action to execute if an exception is raised</param>
        /// <returns></returns>
        private IEnumerator c_LoadBrainVolume(Data.Anatomy.MRI mri, Action<Exception> outPut)
        {
            try
            {
                if (mri.Usable)
                {

                    MRI3D mri3D = new MRI3D(mri);
                    if (ApplicationState.UserPreferences.Data.Anatomic.MRIPreloading)
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

                        if (ApplicationState.UserPreferences.Data.Anatomic.MeshPreloading)
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

                        if (ApplicationState.UserPreferences.Data.Anatomic.MeshPreloading)
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
                catch (Exception e)
                {
                    Debug.LogException(e);
                    outPut(new CanNotLoadImplantation(implantationName));
                    yield break;
                }
            }
            
            yield return Ninja.JumpToUnity;
            UpdateSites("");
            yield return Ninja.JumpBack;
        }
        /// <summary>
        /// Copy the MNI objects references to this scene
        /// </summary>
        /// <param name="outPut">Action to execute if an exception is raised</param>
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
        /// <returns></returns>
        private IEnumerator c_LoadColumns(Action<Exception> outPut)
        {
            yield return Ninja.JumpToUnity;
            try
            {
                m_ColumnManager.InitializeColumns(Visualization.Columns);
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
                foreach (var columnIEEG in m_ColumnManager.ColumnsIEEG)
                {
                    columnIEEG.ComputeEEGData();
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
            float totalTime = 0.075f * Patients.Count + 1.5f; // Calculated by Linear Regression is 0.0593f * Patients.Count + 1.0956f
            float totalProgress = m_ColumnManager.ColumnsIEEG.Count * (1 + m_ColumnManager.MeshSplitNumber + 10);
            float timeByProgress = totalTime / totalProgress;
            float currentProgress = 0.0f;
            OnProgressUpdateGenerator.Invoke(currentProgress / totalProgress, "Initializing", timeByProgress);
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
                OnProgressUpdateGenerator.Invoke(++currentProgress / totalProgress, "Loading " + m_ColumnManager.ColumnsIEEG[ii].Label, timeByProgress);
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
                    OnProgressUpdateGenerator.Invoke(++currentProgress / totalProgress, "Loading " + m_ColumnManager.ColumnsIEEG[ii].Label, timeByProgress);
                    yield return Ninja.JumpBack;
                    if (m_GeneratorNeedsUpdate) yield break;
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].InitializeOctree(m_ColumnManager.ColumnsIEEG[ii].RawElectrodes);
                    if (m_GeneratorNeedsUpdate) yield break;
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].ComputeDistances(m_ColumnManager.ColumnsIEEG[ii].IEEGParameters.InfluenceDistance, ApplicationState.UserPreferences.General.System.MultiThreading);
                    if (m_GeneratorNeedsUpdate) yield break;
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].ComputeInfluences(m_ColumnManager.ColumnsIEEG[ii], ApplicationState.UserPreferences.General.System.MultiThreading, addValues, (int)ApplicationState.UserPreferences.Visualization._3D.SiteInfluenceByDistance);
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

                // volume
                yield return Ninja.JumpToUnity;
                currentProgress += 10;
                OnProgressUpdateGenerator.Invoke(currentProgress / totalProgress, "Loading " + m_ColumnManager.ColumnsIEEG[ii].Label, timeByProgress * 10);
                yield return Ninja.JumpBack;
                if (m_GeneratorNeedsUpdate) yield break;
                m_ColumnManager.ColumnsIEEG[ii].DLLMRIVolumeGenerator.InitializeOctree(m_ColumnManager.ColumnsIEEG[ii].RawElectrodes);
                if (m_GeneratorNeedsUpdate) yield break;
                m_ColumnManager.ColumnsIEEG[ii].DLLMRIVolumeGenerator.ComputeDistances(m_ColumnManager.ColumnsIEEG[ii].IEEGParameters.InfluenceDistance, ApplicationState.UserPreferences.General.System.MultiThreading);
                if (m_GeneratorNeedsUpdate) yield break;
                m_ColumnManager.ColumnsIEEG[ii].DLLMRIVolumeGenerator.ComputeInfluences(m_ColumnManager.ColumnsIEEG[ii], ApplicationState.UserPreferences.General.System.MultiThreading, addValues, (int)ApplicationState.UserPreferences.Visualization._3D.SiteInfluenceByDistance);
                if (m_GeneratorNeedsUpdate) yield break;

                currentMaxDensity = m_ColumnManager.ColumnsIEEG[ii].DLLMRIVolumeGenerator.MaximumDensity;
                currentMinInfluence = m_ColumnManager.ColumnsIEEG[ii].DLLMRIVolumeGenerator.MinimumInfluence;
                currentMaxInfluence = m_ColumnManager.ColumnsIEEG[ii].DLLMRIVolumeGenerator.MaximumInfluence;

                if (currentMaxDensity > maxDensity)
                    maxDensity = currentMaxDensity;

                if (currentMinInfluence < m_ColumnManager.ColumnsIEEG[ii].SharedMinInf)
                    m_ColumnManager.ColumnsIEEG[ii].SharedMinInf = currentMinInfluence;

                if (currentMaxInfluence > m_ColumnManager.ColumnsIEEG[ii].SharedMaxInf)
                    m_ColumnManager.ColumnsIEEG[ii].SharedMaxInf = currentMaxInfluence;

                // synchronize max density
                for (int jj = 0; jj < m_ColumnManager.MeshSplitNumber; ++jj)
                {
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].SynchronizeWithOthersGenerators(maxDensity, m_ColumnManager.ColumnsIEEG[ii].SharedMinInf, m_ColumnManager.ColumnsIEEG[ii].SharedMaxInf);
                    if (m_GeneratorNeedsUpdate) yield break;
                }
                m_ColumnManager.ColumnsIEEG[ii].DLLMRIVolumeGenerator.SynchronizeWithOthersGenerators(maxDensity, m_ColumnManager.ColumnsIEEG[ii].SharedMinInf, m_ColumnManager.ColumnsIEEG[ii].SharedMaxInf);
                if (m_GeneratorNeedsUpdate) yield break;

                for (int jj = 0; jj < m_ColumnManager.MeshSplitNumber; ++jj)
                {
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].AdjustInfluencesToColormap(m_ColumnManager.ColumnsIEEG[ii]);
                    if (m_GeneratorNeedsUpdate) yield break;
                }
                m_ColumnManager.ColumnsIEEG[ii].DLLMRIVolumeGenerator.AdjustInfluencesToColormap(m_ColumnManager.ColumnsIEEG[ii]);
                if (m_GeneratorNeedsUpdate) yield break;
            }
            yield return Ninja.JumpToUnity;
            OnProgressUpdateGenerator.Invoke(1.0f, "Finalizing", timeByProgress);
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

            yield return Ninja.JumpBack;
            List<DLL.Surface> cuts;
            if (Cuts.Count > 0) cuts = new List<DLL.Surface>(SceneInformation.SimplifiedMeshToUse.Cut(Cuts.ToArray(), !SceneInformation.CutHolesEnabled, StrongCuts));
            else cuts = new List<DLL.Surface>() { (DLL.Surface)SceneInformation.SimplifiedMeshToUse.Clone() };
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
        /// Destroy the scene
        /// </summary>
        /// <returns></returns>
        public IEnumerator c_Destroy()
        {
            m_GeneratorNeedsUpdate = true;
            m_DestroyRequested = true;
            yield return new WaitUntil(delegate { return !m_UpdatingGenerator; });
            Visualization.Unload();
            Destroy(gameObject);
        }
        #endregion
    }
}