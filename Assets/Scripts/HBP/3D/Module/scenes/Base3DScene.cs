
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
using HBP.UI.Module3D;
using UnityEngine.Rendering;
using System.Collections;
using CielaSpike;
using Tools.Unity;
using HBP.Data.Visualization;

namespace HBP.Module3D
{
    #region Struct
    /// <summary>
    /// Site info to be send to the UI
    /// </summary>
    public class SiteInfo
    {
        public SiteInfo(Site site, bool enabled, Vector3 position, SiteInformationDisplayMode mode = SiteInformationDisplayMode.IEEG, string amplitude = "", string height = "", string latency = "")
        {
            Site = site;
            Enabled = enabled;
            Position = position;
            Amplitude = amplitude;
            Height = height;
            Latency = latency;
            Mode = mode;
        }

        public Site Site { get; set; }
        public bool Enabled { get; set; }
        public Vector3 Position { get; set; }
        public string Amplitude { get; set; }
        public string Height { get; set; }
        public string Latency { get; set; }
        public SiteInformationDisplayMode Mode { get; set; }
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

    /// <summary>
    /// IRMF data to be send to the UI
    /// </summary>
    public struct FMRIDataParameters
    {
        public bool singlePatient;
        public int columnId;
        public float alpha;
        public float calMin; 
        public float calMax;
        public MRICalValues calValues;
    }
    #endregion

    /// <summary>
    /// Class containing all the DLL data and the displayable Gameobjects of a 3D scene.
    /// </summary>
    [AddComponentMenu("Scenes/Base 3D Scene")]
    public abstract class Base3DScene : MonoBehaviour, IConfigurable
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
        public abstract SceneType Type { get; }
        
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
                if (m_IsSelected && !wasSelected)
                {
                    OnSelectScene.Invoke(this);
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
        
        protected Visualization m_Visualization;
        /// <summary>
        /// Visualization associated to this scene
        /// </summary>
        public virtual Visualization Visualization
        {
            get
            {
                return m_Visualization;
            }
            set
            {
                m_Visualization = value;
            }
        }

        protected List<Cut> m_Cuts = new List<Cut>();
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

        [SerializeField]
        protected ModesManager m_ModesManager;
        /// <summary>
        /// Modes of the scene
        /// </summary>
        public ModesManager ModesManager
        {
            get { return m_ModesManager; }
        }

        protected DisplayedObjects3DView m_DisplayedObjects = null;
        /// <summary>
        /// Displayable objects of the scene
        /// </summary>
        public DisplayedObjects3DView DisplayedObjects
        {
            get
            {
                return m_DisplayedObjects;
            }
        }

        [SerializeField]
        protected Column3DManager m_ColumnManager;
        /// <summary>
        /// Column data manager
        /// </summary>
        public Column3DManager ColumnManager { get { return m_ColumnManager; } }

        /// <summary>
        /// ID of the selected column of this scene
        /// </summary>
        public int SelectedColumnID
        {
            get
            {
                return m_ColumnManager.SelectedColumnID;
            }
        }
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
                if (!SceneInformation.MeshesLoaded || !SceneInformation.MRILoaded) return;

                SceneInformation.CutHolesEnabled = value;
                SceneInformation.MeshGeometryNeedsUpdate = true;
                SceneInformation.IsIEEGOutdated = true;
                foreach (Column3D column in m_ColumnManager.Columns)
                {
                    column.IsRenderingUpToDate = false;
                }
                m_ModesManager.UpdateMode(Mode.FunctionsId.UpdatePlane);
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
                m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
                m_ModesManager.SetCurrentModeSpecifications(true);
                ResetIEEG();
                ApplicationState.Module3D.OnRequestUpdateInUI.Invoke();
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
                m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
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
            }
        }

        /// <summary>
        /// Handles triangle erasing
        /// </summary>
        protected TriEraser m_TriEraser = new TriEraser();
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

        private bool m_CuttingMesh;
        /// <summary>
        /// Are we cutting the mesh of this scene ?
        /// </summary>
        public bool CuttingMesh
        {
            get
            {
                return m_CuttingMesh;
            }
            set
            {
                if (m_CuttingMesh != value)
                {
                    m_CuttingMesh = value;
                    SceneInformation.MeshGeometryNeedsUpdate = true;
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

        private bool m_UpdatingColliders = false;

        [SerializeField]
        protected GameObject m_BrainPrefab;
        [SerializeField]
        protected GameObject m_SimplifiedBrainPrefab;
        [SerializeField]
        protected GameObject m_InvisibleBrainPrefab;
        [SerializeField]
        protected GameObject m_CutPrefab;
        [SerializeField]
        protected GameObject m_SitePrefab;
        #endregion

        #region Events
        /// <summary>
        /// Event called when this scene is selected
        /// </summary>
        public GenericEvent<Base3DScene> OnSelectScene = new GenericEvent<Base3DScene>();
        /// <summary>
        /// Event called when showing or hiding the scene in the UI
        /// </summary>
        public GenericEvent<bool> OnChangeVisibleState = new GenericEvent<bool>();
        /// <summary>
        /// Event called when reseting the view positions in the UI
        /// </summary>
        public UnityEvent OnResetViewPositions = new UnityEvent();

        /// <summary>
        /// Event for updating the planes cuts display in the cameras
        /// </summary>
        public UnityEvent OnModifyPlanesCuts = new UnityEvent();
        /// <summary>
        /// Event called when adding a cut to the scene
        /// </summary>
        public GenericEvent<Cut> OnAddCut = new GenericEvent<Cut>();

        /// <summary>
        /// Event called when changing the colors of the colormap
        /// </summary>
        public GenericEvent<ColorType> OnChangeColormap = new GenericEvent<ColorType>();
        /// <summary>
        /// Event for colormap values associated to a column id (params : minValue, middle, maxValue, id)
        /// </summary>
        public GenericEvent<float, float, float, Column3D> OnSendColorMapValues = new GenericEvent<float, float, float, Column3D>();

        /// <summary>
        /// Ask the camera manager to update the target for this scene
        /// </summary>
        public GenericEvent<Vector3> OnUpdateCameraTarget = new GenericEvent<Vector3>();

        /// <summary>
        /// Event called when a FMRI column is added
        /// </summary>
        public UnityEvent OnAddFMRIColumn = new UnityEvent();
        /// <summary>
        /// Event called when a FMRI column is removed
        /// </summary>
        public UnityEvent OnRemoveFMRIColumn = new UnityEvent();
        /// <summary>
        /// Event for sending IRMF data parameters to UI (params : IRMFDataParameters)
        /// </summary>
        public GenericEvent<FMRIDataParameters> OnSendFMRIParameters = new GenericEvent<FMRIDataParameters>();

        public UnityEvent OnChangeColumnMinimizedState = new UnityEvent();
        /// <summary>
        /// Event called when site is clicked to dipslay additionnal infomation.
        /// </summary>
        public GenericEvent<IEnumerable<Site>> OnRequestSiteInformation = new GenericEvent<IEnumerable<Site>>();
        /// <summary>
        /// Event called when updating a ROI
        /// </summary>
        public UnityEvent OnUpdateROI = new UnityEvent();
        /// <summary>
        /// Event called when requesting a screenshot of the scene
        /// </summary>
        public GenericEvent<bool> OnRequestScreenshot = new GenericEvent<bool>();
        #endregion

        #region Private Methods
        protected void Update()
        {
            if (!SceneInformation.IsSceneInitialized) return;

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update: set_current_mode_specifications");
            SetCurrentModeSpecifications();
            UnityEngine.Profiling.Profiler.EndSample();

            if (m_ModesManager.CurrentModeID == Mode.ModesId.NoPathDefined) return;

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update");

            // TEMP : useless
            for (int ii = 0; ii < Cuts.Count; ++ii)
            {
                Cuts[ii].RemoveFrontPlane = 0;
            }

            // check if we must perform new cuts of the brain
            if (SceneInformation.MeshGeometryNeedsUpdate)
            {
                UpdateGeometry();
            }
            UnityEngine.Profiling.Profiler.EndSample();

            if (!SceneInformation.IsSceneDisplayed)
            {
                OnChangeVisibleState.Invoke(true);
                SceneInformation.IsSceneDisplayed = true;
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
                if (!SceneInformation.IsGeometryUpToDate) return;

                { //TEST (maybe FIXME : delete this, the visual effect is not very good)
                  // recompute UV
                    for (int ii = 0; ii < m_ColumnManager.MeshSplitNumber; ++ii)
                        m_ColumnManager.DLLCommonBrainTextureGeneratorList[ii].ComputeUVMainWithVolume(m_ColumnManager.SelectedMesh.SplittedMeshes[ii], m_ColumnManager.SelectedMRI.Volume, m_ColumnManager.MRICalMinFactor, m_ColumnManager.MRICalMaxFactor);

                    // update brain mesh object mesh filter (TODO update only UV)
                    UpdateMeshesFromDLL();
                }

                ComputeMRITextures();
                ResetIEEG();
            });
            m_ColumnManager.OnUpdateIEEGSpan.AddListener((column) =>
            {
                ResetIEEG();
            });
            m_ColumnManager.OnUpdateIEEGAlpha.AddListener((column) =>
            {
                if (SceneInformation.IsGeometryUpToDate && !SceneInformation.IsIEEGOutdated)
                    ComputeIEEGTexturesOfColumn(column);
            });
            m_ColumnManager.OnUpdateIEEGGain.AddListener((column) =>
            {
                m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
            });
            m_ColumnManager.OnUpdateIEEGMaximumInfluence.AddListener((column) =>
            {
                ResetIEEG();
            });
            m_ColumnManager.OnUpdateColumnTimelineID.AddListener((column) =>
            {
                ComputeIEEGTexturesOfColumn(column);
                m_ColumnManager.UpdateColumnIEEGSitesRendering(column, SceneInformation);
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
            m_ColumnManager.OnChangeColumnMinimizedState.AddListener(() =>
            {
                OnChangeColumnMinimizedState.Invoke();
            });
            m_ColumnManager.OnSelectColumnManager.AddListener((columnManager) =>
            {
                IsSelected = true;

                // force mode to update UI
                m_ModesManager.SetCurrentModeSpecifications(true);

                ComputeGUITextures(-1, m_ColumnManager.SelectedColumnID);
                UpdateGUITextures();
            });
            m_ColumnManager.OnSelectSite.AddListener((site) =>
            {
                ClickOnSiteCallback();
                m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
                ApplicationState.Module3D.OnSelectSite.Invoke(site);
                ApplicationState.Module3D.OnRequestUpdateInUI.Invoke();
            });
            SceneInformation.OnUpdateGeneratorState.AddListener((value) =>
            {
                if (!value)
                {
                    foreach (Column3DIEEG column in m_ColumnManager.ColumnsIEEG)
                    {
                        column.IsTimelineLooping = false;
                        column.IsTimelinePlaying = false;
                        column.CurrentTimeLineID = 0;
                        column.IsRenderingUpToDate = false;
                    }
                }
                ApplicationState.Module3D.OnRequestUpdateInUI.Invoke();
            });
        }
        /// <summary>
        /// 
        /// When to call ?  changes in DLLCutColorScheme, MRICalMinFactor, MRICalMaxFactor
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        private void ComputeMRITextures(int indexCut = -1, int indexColumn = -1)
        {
            bool allColumns = indexColumn == -1;
            bool allCuts = indexCut == -1;

            int[] columnsIndexes = allColumns ? Enumerable.Range(0, m_ColumnManager.Columns.Count).ToArray() : new int[1] { indexColumn };
            int[] cutsIndexes = allCuts ? Enumerable.Range(0, Cuts.Count).ToArray() : new int[1] { indexCut };

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene compute_MRI_textures 0 create_MRI_texture ");

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
                for (int jj = 0; jj < cutsIndexes.Length; ++jj)
                    m_ColumnManager.CreateMRITexture(cutsIndexes[jj], columnsIndexes[ii]);

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene compute_MRI_textures 1 compute_GUI_textures");
            ComputeGUITextures(indexCut, m_ColumnManager.SelectedColumnID);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene compute_MRI_textures 2 update_GUI_textures");
            UpdateGUITextures();
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// 
        /// When to call ? changes in IEEGColumn.currentTimeLineID, IEEGColumn.alphaMin, IEEGColumn.alphaMax / DLLCutColorScheme
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        private void ComputeIEEGTextures(int indexCut = -1, int indexColumn = -1, bool surface = true, bool cuts = true, bool plots = true)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-compute_IEEG_textures");

            UnityEngine.Profiling.Profiler.BeginSample("TEST-compute_IEEG_textures 0");
            bool allColumns = indexColumn == -1;
            bool allCuts = indexCut == -1;

            int[] columnsIndexes = allColumns ? Enumerable.Range(0, m_ColumnManager.Columns.Count).ToArray() : new int[1] { indexColumn };
            int[] cutsIndexes = allCuts ? Enumerable.Range(0, Cuts.Count).ToArray() : new int[1] { indexCut };

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
            {
                if (ColumnManager.Columns[columnsIndexes[ii]].Type != Column3D.ColumnType.IEEG)
                    continue;

                Column3DIEEG currCol = (Column3DIEEG)ColumnManager.Columns[columnsIndexes[ii]];

                // brain surface
                if (surface)
                    if (!m_ColumnManager.ComputeSurfaceBrainUVWithIEEG(m_ColumnManager.ColumnsIEEG[columnsIndexes[ii]]))
                        continue;

                // brain cuts
                if (cuts)
                    for (int jj = 0; jj < cutsIndexes.Length; ++jj)
                        m_ColumnManager.ColorCutsTexturesWithIEEG(m_ColumnManager.ColumnsIEEG[columnsIndexes[ii]], cutsIndexes[jj]);

                if (plots)
                {
                    currCol.UpdateSitesSizeAndColorForIEEG();
                    currCol.UpdateSitesRendering(SceneInformation, null);
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-compute_IEEG_textures 1 compute_GUI_textures");
            ComputeGUITextures(indexCut, m_ColumnManager.SelectedColumnID);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-compute_IEEG_textures 2 update_GUI_textures");
            UpdateGUITextures();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Compute IEEG textures for a specific column
        /// </summary>
        /// <param name="column"></param>
        private void ComputeIEEGTexturesOfColumn(Column3DIEEG column)
        {
            if (!SceneInformation.IsGeneratorUpToDate) return;

            UnityEngine.Profiling.Profiler.BeginSample("compute_IEEG_textures");

            UnityEngine.Profiling.Profiler.BeginSample("compute_IEEG_textures 0");

            if (!m_ColumnManager.ComputeSurfaceBrainUVWithIEEG(column)) return;

            for (int jj = 0; jj < Cuts.Count; ++jj)
            {
                m_ColumnManager.ColorCutsTexturesWithIEEG(column, jj);
            }

            column.UpdateSitesSizeAndColorForIEEG();
            column.UpdateSitesRendering(SceneInformation, null);

            UnityEngine.Profiling.Profiler.EndSample();

            if (column == m_ColumnManager.SelectedColumn)
            {
                UnityEngine.Profiling.Profiler.BeginSample("compute_IEEG_textures 1 compute_GUI_textures");
                ComputeGUITextures(-1, m_ColumnManager.SelectedColumnID);
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("compute_IEEG_textures 2 update_GUI_textures");
                UpdateGUITextures();
                UnityEngine.Profiling.Profiler.EndSample();
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// When to call ? changes in FMRIColumn.alpha / calMin/ calMax/ DLLCutColorScheme
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        /// <param name="surface"></param>
        /// <param name="cuts"></param>
        private void ComputeFMRITextures(int indexCut = -1, int indexColumn = -1, bool surface = true, bool cuts = true)
        {
            bool allColumns = indexColumn == -1;
            bool allCuts = indexCut == -1;

            int[] columnsIndexes = allColumns ? Enumerable.Range(0, m_ColumnManager.ColumnsFMRI.Count).ToArray() : new int[1] { indexColumn };
            int[] cutsIndexes = allCuts ? Enumerable.Range(0, Cuts.Count).ToArray() : new int[1] { indexCut };

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
            {
                //Column3DViewFMRI currCol = Column3DViewManager.FMRI_col(columnsIndexes[ii]);

                // brain cuts
                if (cuts)
                    for (int jj = 0; jj < cutsIndexes.Length; ++jj)
                        m_ColumnManager.ColorCutsTexturesWithFMRI(columnsIndexes[ii], cutsIndexes[jj]);
            }

            ComputeGUITextures(indexCut, indexColumn);

            UpdateGUITextures();
        }
        /// <summary>
        /// Compute textures to be displayed on the UI
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        private void ComputeGUITextures(int indexCut = -1, int indexColumn = -1)
        {
            bool allColumns = indexColumn == -1;
            bool allCuts = indexCut == -1;

            int[] columnsIndexes = allColumns ? Enumerable.Range(0, m_ColumnManager.Columns.Count).ToArray() : new int[1] { indexColumn };
            int[] cutsIndexes = allCuts ? Enumerable.Range(0, Cuts.Count).ToArray() : new int[1] { indexCut };

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
            {
                Column3D currCol = m_ColumnManager.Columns[columnsIndexes[ii]];

                for (int jj = 0; jj < Cuts.Count; ++jj)
                {
                    switch (currCol.Type)
                    {
                        case Column3D.ColumnType.Base:
                            m_ColumnManager.CreateGUIMRITexture(cutsIndexes[jj], columnsIndexes[ii]);
                            break;
                        case Column3D.ColumnType.FMRI:
                            m_ColumnManager.CreateGUIFMRITexture(cutsIndexes[jj], columnsIndexes[ii]);
                            break;
                        case Column3D.ColumnType.IEEG:
                            if (!SceneInformation.IsGeneratorUpToDate)
                                m_ColumnManager.CreateGUIMRITexture(cutsIndexes[jj], columnsIndexes[ii]);
                            else
                                m_ColumnManager.CreateGUIIEEGTexture(cutsIndexes[jj], columnsIndexes[ii]);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// Update textures displayed on the UI
        /// </summary>
        /// <param name="indexCut"></param>
        private void UpdateGUITextures()
        {
            Column3D currCol = m_ColumnManager.SelectedColumn;
            if (!currCol) return;

            List<Texture2D> texturesToDisplay = null;
            switch (currCol.Type)
            {
                case Column3D.ColumnType.Base:
                    texturesToDisplay = currCol.GUIBrainCutTextures;
                    break;
                case Column3D.ColumnType.FMRI:
                    texturesToDisplay = ((Column3DFMRI)currCol).GUIBrainCutWithFMRITextures;
                    break;
                case Column3D.ColumnType.IEEG:
                    if (!SceneInformation.IsGeneratorUpToDate)
                        texturesToDisplay = currCol.GUIBrainCutTextures;
                    else
                        texturesToDisplay = ((Column3DIEEG)currCol).GUIBrainCutWithIEEGTextures;
                    break;
                default:
                    break;
            }

            foreach (Cut cut in Cuts)
            {
                cut.OnUpdateGUITextures.Invoke(texturesToDisplay[cut.ID]);
            }
        }
        /// <summary>
        /// Finalize Generators Computing
        /// </summary>
        private void FinalizeGeneratorsComputing()
        {
            // generators are now up to date
            SceneInformation.IsGeneratorUpToDate = true;
            SceneInformation.IsIEEGOutdated = false;

            // send inf values to overlays
            for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
            {
                float maxValue = Math.Max(Math.Abs(m_ColumnManager.ColumnsIEEG[ii].SharedMinInf), Math.Abs(m_ColumnManager.ColumnsIEEG[ii].SharedMaxInf));
                float minValue = -maxValue;
                minValue += m_ColumnManager.ColumnsIEEG[ii].IEEGParameters.Middle;
                maxValue += m_ColumnManager.ColumnsIEEG[ii].IEEGParameters.Middle;
                OnSendColorMapValues.Invoke(minValue, m_ColumnManager.ColumnsIEEG[ii].IEEGParameters.Middle, maxValue, m_ColumnManager.ColumnsIEEG[ii]);
                m_ColumnManager.ColumnsIEEG[ii].CurrentTimeLineID = 0;
            }

            // amplitudes are not displayed yet
            for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
                m_ColumnManager.ColumnsIEEG[ii].UpdateIEEG = true;

            //####### CHECK ACCESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.PostUpdateGenerators))
            {
                Debug.LogError("-ERROR : Base3DScene::post_updateGenerators -> no acess for mode : " + m_ModesManager.CurrentModeName);
            }
            //##################

            // update plots visibility
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);

            // check validity of plot scale
            m_ColumnManager.CheckIEEGParametersIntegrity();

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.PostUpdateGenerators);
            //##################
        }
        /// <summary>
        /// Load FMRI dialog
        /// </summary>
        /// <param name="path">Path of the FMRI file</param>
        /// <returns></returns>
        private bool LoadFMRIDialog(out string path)
        {
            bool loaded = true;
            string[] filters = new string[] { "nii", "img" };
            path = "";
            path = DLL.QtGUI.GetExistingFileName(filters, "Select an fMRI file");

            if (!string.IsNullOrEmpty(path))
            {
                bool result = LoadFMRIFile(path);
                if (!result)
                {
                    Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't load FMRI");
                    loaded = false;
                }
            }
            else
            {
                loaded = false;
            }
            return loaded;
        }
        /// <summary>
        /// Load an FMRI column
        /// </summary>
        /// <param name="fMRIPath"></param>
        /// <returns></returns>
        private bool LoadFMRIFile(string fMRIPath)
        {
            if (m_ColumnManager.SelectedMRI.NII.LoadNIIFile(fMRIPath)) // FIXME : create new array
                return true;

            Debug.LogError("-ERROR : Base3DScene::load_FMRI_file -> load NII file failed. " + fMRIPath);
            return false;
        }
        /// <summary>
        /// Load a FMRI column
        /// </summary>
        /// <param name="fmriLabel"></param>
        /// <returns></returns>
        private bool LoadFMRIColumn(string fmriLabel)
        {
            // Check access
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.AddFMRIColumn))
            {
                Debug.LogError("-ERROR : Base3DScene::add_FMRI_column -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return false;
            }

            // Update column number
            int newFMRIColumnNumber = m_ColumnManager.ColumnsFMRI.Count + 1;
            //m_ColumnManager.SetColumns(m_ColumnManager.ColumnsIEEG.Count, newFMRIColumnNumber, Cuts.Count);

            // Update label
            int newFMRIColumnID = newFMRIColumnNumber - 1;
            m_ColumnManager.ColumnsFMRI[newFMRIColumnID].Label = fmriLabel;

            // Update sites visibility
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);

            // Convert to volume            
            m_ColumnManager.SelectedMRI.NII.ConvertToVolume(m_ColumnManager.DLLVolumeFMriList[newFMRIColumnID]);

            //if (Type == SceneType.SinglePatient)
            //    Events.OnAskRegionOfInterestUpdate.Invoke(m_ColumnManager.ColumnsIEEG.Count + newFMRIColumnID);

            // send parameters to UI
            //IRMCalValues calValues = m_CM.DLLVolumeIRMFList[idCol].retrieveExtremeValues();

            FMRIDataParameters fmriParams = new FMRIDataParameters();
            fmriParams.calValues = m_ColumnManager.DLLVolumeFMriList[newFMRIColumnID].ExtremeValues;
            fmriParams.columnId = newFMRIColumnID;
            fmriParams.alpha = m_ColumnManager.ColumnsFMRI[newFMRIColumnID].Alpha;
            fmriParams.calMin = m_ColumnManager.ColumnsFMRI[newFMRIColumnID].CalMin;
            fmriParams.calMax = m_ColumnManager.ColumnsFMRI[newFMRIColumnID].CalMax;
            fmriParams.singlePatient = Type == SceneType.SinglePatient;

            m_ColumnManager.ColumnsFMRI[newFMRIColumnID].CalMin = fmriParams.calValues.computedCalMin;
            m_ColumnManager.ColumnsFMRI[newFMRIColumnID].CalMax = fmriParams.calValues.computedCalMax;

            // Update camera
            OnUpdateCameraTarget.Invoke(m_ColumnManager.SelectedMesh.Both.BoundingBox.Center);

            ComputeMRITextures();

            OnSendFMRIParameters.Invoke(fmriParams);
            ComputeFMRITextures(-1, -1);

            m_ModesManager.UpdateMode(Mode.FunctionsId.AddFMRIColumn);
            return true;
        }
        /// <summary>
        /// Unload the last FMRI column
        /// </summary>
        private void UnloadLastFMRIColumn()
        {
            // Check access
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.RemoveLastFMRIColumn))
            {
                Debug.LogError("-ERROR : Base3DScene::remove_last_FMRI_column -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }

            // Update columns number
            //m_ColumnManager.SetColumns(m_ColumnManager.ColumnsIEEG.Count, m_ColumnManager.ColumnsFMRI.Count - 1, Cuts.Count);

            // Update plots visibility
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);

            ComputeMRITextures();
            ComputeFMRITextures(-1, -1);
            UpdateGUITextures();

            m_ModesManager.UpdateMode(Mode.FunctionsId.RemoveLastFMRIColumn);
        }
        /// <summary>
        /// Actions to perform when clicking on a site
        /// </summary>
        private void ClickOnSiteCallback()
        {
            if (m_ColumnManager.SelectedColumn.SelectedSiteID == -1) return;

            if (m_ColumnManager.SelectedColumn.Type == Column3D.ColumnType.IEEG)
            {
                if (((Column3DIEEG)m_ColumnManager.SelectedColumn).SendInformation)
                {
                    SendAdditionalSiteInfoRequest();
                }
            }
        }
        /// <summary>
        /// Init gameobjects of the scene
        /// </summary>
        protected void InitializeSceneGameObjects()
        {
            // Mark brain mesh as dynamic
            m_BrainPrefab.GetComponent<MeshFilter>().sharedMesh.MarkDynamic();

            // init parents 
            m_DisplayedObjects.MeshesParent = transform.Find("Meshes").gameObject;
            m_DisplayedObjects.SitesMeshesParent = transform.Find("Sites").gameObject;
            m_DisplayedObjects.BrainSurfaceMeshesParent = transform.Find("Meshes").Find("Brains").gameObject;
            m_DisplayedObjects.InvisibleBrainMeshesParent = transform.Find("Meshes").Find("Erased Brains").gameObject;
            m_DisplayedObjects.BrainCutMeshesParent = transform.Find("Meshes").Find("Cuts").gameObject;

            // init lights
            m_DisplayedObjects.SharedDirectionalLight = transform.parent.Find("Global Light").gameObject;
            m_DisplayedObjects.SharedSpotlight = transform.parent.Find("Global Spotlight").gameObject;

            // init default planes
            m_Cuts = new List<Cut>();
            m_DisplayedObjects.BrainCutMeshes = new List<GameObject>();

            UpdateBrainSurfaceColor(m_ColumnManager.BrainColor);
            UpdateColormap(m_ColumnManager.Colormap, false);
            UpdateBrainCutColor(m_ColumnManager.BrainCutColor, true);
        }
        /// <summary>
        /// Update the surface meshes from the DLL
        /// </summary>
        protected void UpdateMeshesFromDLL()
        {
            for (int ii = 0; ii < m_ColumnManager.MeshSplitNumber; ++ii)
            {
                m_ColumnManager.SelectedMesh.SplittedMeshes[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
            }
            UnityEngine.Profiling.Profiler.BeginSample("Update Columns Meshes");
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.UpdateColumnMeshes(m_DisplayedObjects.BrainSurfaceMeshes);
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Function to be called everytime we want to reset IEEG
        /// </summary>
        protected void ResetIEEG()
        {
            if (!SceneInformation.IsGeometryUpToDate) return;
            SceneInformation.IsGeneratorUpToDate = false;
            SceneInformation.IsIEEGOutdated = true;
            UpdateGUITextures();
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);

            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetIEEG);
        }
        /// <summary>
        /// Generate the split number regarding all meshes
        /// </summary>
        /// <param name="meshes"></param>
        protected void GenerateSplit(IEnumerable<DLL.Surface> meshes)
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
        public void UpdateColormap(ColorType color, bool updateColors = true)
        {
            ColumnManager.Colormap = color;
            if (updateColors)
                ColumnManager.ResetColors();

            SharedMaterials.Brain.BrainMaterials[this].SetTexture("_ColorTex", ColumnManager.BrainColorMapTexture);

            if (SceneInformation.IsGeometryUpToDate && !SceneInformation.IsIEEGOutdated)
                ComputeIEEGTextures();

            OnChangeColormap.Invoke(color);
        }
        /// <summary>
        /// Update the color of the surface of the brain for this scene
        /// </summary>
        /// <param name="color">Color of the brain</param>
        public void UpdateBrainSurfaceColor(ColorType color)
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
        public void UpdateBrainCutColor(ColorType color, bool updateColors = true)
        {
            ColumnManager.BrainCutColor = color;
            if (updateColors)
                ColumnManager.ResetColors();

            SceneInformation.MeshGeometryNeedsUpdate = true;
            SceneInformation.IsIEEGOutdated = true;
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.IsRenderingUpToDate = false;
            }

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.UpdatePlane); // TEMP
            //##################
        }
        /// <summary>
        /// Set the mesh part to be displayed in the scene
        /// </summary>
        /// <param name="meshPartToDisplay"></param>
        public void UpdateMeshPartToDisplay(SceneStatesInfo.MeshPart meshPartToDisplay)
        {
            // Check access
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.SetDisplayedMesh))
            {
                Debug.LogError("-ERROR : Base3DScene::setDisplayedMesh -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }

            if (!SceneInformation.IsGeometryUpToDate) return;

            SceneInformation.MeshPartToDisplay = meshPartToDisplay;
            SceneInformation.MeshGeometryNeedsUpdate = true;
            SceneInformation.IsIEEGOutdated = true;
            //m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.IsRenderingUpToDate = false;
            }

            // Update Mode
            m_ModesManager.UpdateMode(Mode.FunctionsId.SetDisplayedMesh);
        }
        /// <summary>
        /// Set the mesh type to be displayed in the scene
        /// </summary>
        /// <param name="meshTypeToDisplay"></param>
        public void UpdateMeshToDisplay(int meshID)
        {
            // Check access
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.SetDisplayedMesh))
            {
                Debug.LogError("-ERROR : Base3DScene::setDisplayedMesh -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }

            if (!SceneInformation.IsGeometryUpToDate) return;

            if (meshID == -1) meshID = 0;

            m_ColumnManager.SelectedMeshID = meshID;
            SceneInformation.MeshGeometryNeedsUpdate = true;
            SceneInformation.IsIEEGOutdated = true;
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.IsRenderingUpToDate = false;
            }

            OnUpdateCameraTarget.Invoke(m_ColumnManager.SelectedMesh.Both.BoundingBox.Center);

            // Update mode
            m_ModesManager.UpdateMode(Mode.FunctionsId.SetDisplayedMesh);

            foreach (var cut in m_Cuts)
            {
                UpdateCutPlane(cut);
            }
        }
        /// <summary>
        /// Set the MRI to be used
        /// </summary>
        /// <param name="mriID"></param>
        public void UpdateMRIToDisplay(int mriID)
        {
            // Check access
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.SetDisplayedMesh))
            {
                Debug.LogError("-ERROR : Base3DScene::setDisplayedMesh -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }

            if (!SceneInformation.IsGeometryUpToDate) return;

            if (mriID == -1) mriID = 0;

            m_ColumnManager.SelectedMRIID = mriID;
            SceneInformation.VolumeCenter = m_ColumnManager.SelectedMRI.Volume.Center;
            SceneInformation.MeshGeometryNeedsUpdate = true;
            SceneInformation.IsIEEGOutdated = true;
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.IsRenderingUpToDate = false;
            }

            // Update mode
            m_ModesManager.UpdateMode(Mode.FunctionsId.SetDisplayedMesh);
        }
        /// <summary>
        /// Update the gameobjects of the sites
        /// </summary>
        /// <param name="implantationID"></param>
        public void UpdateSites(int implantationID)
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

            m_ColumnManager.SelectedImplantationID = implantationID > 0 ? implantationID : 0;
            DLL.PatientElectrodesList electrodesList = m_ColumnManager.SelectedImplantation.PatientElectrodesList;

            if (SceneInformation.SitesLoaded)
            {
                int currPlotNb = 0;
                for (int ii = 0; ii < electrodesList.NumberOfPatients; ++ii)
                {
                    int patientSiteID = 0;
                    string patientID = electrodesList.PatientName(ii);
                    Data.Patient patient = Visualization.Patients.First((p) => p.ID == patientID);

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
                            site.IsActive = true;

                            m_ColumnManager.SitesList.Add(siteGameObject);
                        }
                    }
                }
            }
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.UpdateSites(m_ColumnManager.SelectedImplantation.PatientElectrodesList, m_ColumnManager.SitesPatientParent, m_ColumnManager.SitesList);
                UpdateCurrentRegionOfInterest(column);
            }
            // reset selected plot
            for (int ii = 0; ii < m_ColumnManager.Columns.Count; ++ii)
            {
                m_ColumnManager.Columns[ii].SelectedSiteID = -1;
            }

            SceneInformation.IsIEEGOutdated = true;
            SceneInformation.IsGeneratorUpToDate = false;
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.IsRenderingUpToDate = false;
            }
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
                    case SceneStatesInfo.MeshPart.Left:
                        SceneInformation.SimplifiedMeshToUse = selectedMesh.SimplifiedLeft;
                        SceneInformation.MeshToDisplay = selectedMesh.Left;
                        break;
                    case SceneStatesInfo.MeshPart.Right:
                        SceneInformation.SimplifiedMeshToUse = selectedMesh.SimplifiedRight;
                        SceneInformation.MeshToDisplay = selectedMesh.Right;
                        break;
                    case SceneStatesInfo.MeshPart.Both:
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
        }
        #endregion

        #region Cuts
        /// <summary>
        /// Add a new cut plane
        /// </summary>
        public Cut AddCutPlane()
        {
            // Check access
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.AddNewPlane))
            {
                Debug.LogError("-ERROR : Base3DScene::addNewPlane -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return null;
            }

            // Add new cut
            Cut cut = new Cut(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
            switch (Cuts.Count)
            {
                case 0:
                    cut.Orientation = CutOrientation.Axial;
                    cut.Flip = false;
                    cut.RemoveFrontPlane = 0;
                    cut.Position = 0.5f;
                    break;
                case 1:
                    cut.Orientation = CutOrientation.Coronal;
                    cut.Flip = false;
                    cut.RemoveFrontPlane = 0;
                    cut.Position = 0.5f;
                    break;
                case 2:
                    cut.Orientation = CutOrientation.Sagital;
                    cut.Flip = false;
                    cut.RemoveFrontPlane = 0;
                    cut.Position = 0.5f;
                    break;
                default:
                    cut.Orientation = CutOrientation.Axial;
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

            // update plots visibility
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);

            SceneInformation.MeshGeometryNeedsUpdate = true;
            SceneInformation.IsIEEGOutdated = true;

            // Update mode
            m_ModesManager.UpdateMode(Mode.FunctionsId.AddNewPlane);

            OnAddCut.Invoke(cut);
            UpdateCutPlane(cut);

            return cut;
        }
        /// <summary>
        /// Remove the last cut plane
        /// </summary>
        public void RemoveCutPlane(Cut cut)
        {
            // Check access
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.RemoveLastPlane))
            {
                Debug.LogError("-ERROR : Base3DScene::removeLastPlane -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }

            Cuts.Remove(cut);
            for (int i = 0; i < Cuts.Count; i++)
            {
                Cuts[i].ID = i;
            }

            Destroy(m_DisplayedObjects.BrainCutMeshes[cut.ID]);
            m_DisplayedObjects.BrainCutMeshes.RemoveAt(cut.ID);

            // update columns manager
            m_ColumnManager.UpdateCutNumber(m_DisplayedObjects.BrainCutMeshes.Count);

            // update plots visibility
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);

            SceneInformation.MeshGeometryNeedsUpdate = true;
            SceneInformation.IsIEEGOutdated = true;

            // Update mode
            m_ModesManager.UpdateMode(Mode.FunctionsId.RemoveLastPlane);

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
            // Check access
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.UpdatePlane))
            {
                Debug.LogError("-ERROR : Base3DScene::updatePlane -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }

            if (!CuttingMesh) CuttingMesh = true;

            if (cut.Orientation == CutOrientation.Custom || !SceneInformation.MRILoaded)
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
                offset *= 1.05f; // upsize a little bit the bbox for planes
            }
            else
                offset = 0.1f;

            cut.Point = SceneInformation.MeshCenter + cut.Normal * (cut.Position - 0.5f) * offset * cut.NumberOfCuts;

            SceneInformation.MeshGeometryNeedsUpdate = true;
            SceneInformation.IsIEEGOutdated = true;

            // update sites visibility
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);

            // update cameras cuts display
            OnModifyPlanesCuts.Invoke();

            // Update mode
            m_ModesManager.UpdateMode(Mode.FunctionsId.UpdatePlane);

            cut.OnUpdateCut.Invoke();
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

        #region FMRI
        /// <summary>
        /// Update the cal min value of the input FMRI column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the FMRI columns</param>
        public void UpdateFMRICalMin(float value, int columnId)
        {
            m_ColumnManager.ColumnsFMRI[columnId].CalMin = value;
            ComputeFMRITextures(-1, -1);
        }
        /// <summary>
        /// Update the cal max value of the input FMRI column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the FMRI columns</param>
        public void UpdateFMRICalMax(float value, int columnId)
        {
            m_ColumnManager.ColumnsFMRI[columnId].CalMax = value;
            ComputeFMRITextures(-1, -1);
        }
        /// <summary>
        /// Update the alpha value of the input FMRI column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the FMRI columns</param>
        public void UpdateFMRIAlpha(float value, int columnId)
        {
            m_ColumnManager.ColumnsFMRI[columnId].Alpha = value;
            ComputeFMRITextures(-1, -1);
        }
        /// <summary>
        /// Add a FMRI column to this scene
        /// </summary>
        /// <returns></returns>
        public bool AddFMRIColumn()
        {
            string fMRIPath;
            if (!LoadFMRIDialog(out fMRIPath)) return false;

            string[] split = fMRIPath.Split(new Char[] { '/', '\\' });
            string fMRILabel = split[split.Length - 1];

            bool result = LoadFMRIColumn(fMRILabel);
            if (!result)
            {
                Debug.LogError("-ERROR : ScenesManager::addIRMF -> can't add IRMF column");
            }
            else
            {
                OnAddFMRIColumn.Invoke();
            }
            return result;
        }
        /// <summary>
        /// Remove the last FMRI column from this scene
        /// </summary>
        public void RemoveLastFMRIColumn()
        {
            if (m_ColumnManager.ColumnsFMRI.Count > 0)
            {
                UnloadLastFMRIColumn();
                OnRemoveFMRIColumn.Invoke();
            }
        }
        /// <summary>
        /// Send FMRI parameters to menu via event
        /// </summary>
        public void SendFMRIParametersToMenu() // TODO / FIXME
        {
            for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
            {
                FMRIDataParameters FMRIDataParams;
                FMRIDataParams.alpha = m_ColumnManager.ColumnsFMRI[ii].Alpha;
                FMRIDataParams.calMin = m_ColumnManager.ColumnsFMRI[ii].CalMin;
                FMRIDataParams.calMax = m_ColumnManager.ColumnsFMRI[ii].CalMax;
                FMRIDataParams.columnId = ii;

                FMRIDataParams.calValues = m_ColumnManager.DLLVolumeFMriList[ii].ExtremeValues;
                FMRIDataParams.singlePatient = Type == SceneType.SinglePatient;

                OnSendFMRIParameters.Invoke(FMRIDataParams);
            }
        }
        #endregion

        #region Save/Load
        /// <summary>
        /// Initialize the scene with the corresponding visualization
        /// </summary>
        /// <param name="visualization"></param>
        public void Initialize(Visualization visualization)
        {
            m_DisplayedObjects = new DisplayedObjects3DView();
            SceneInformation = new SceneStatesInfo();
            
            Visualization = visualization;

            // Init materials
            SharedMaterials.Brain.AddSceneMaterials(this);

            // set meshes layer
            SceneInformation.MeshesLayerName = "Default";
            SceneInformation.HiddenMeshesLayerName = "Hidden Meshes";

            // init modes
            m_ModesManager.Initialize(this);

            AddListeners();
            InitializeSceneGameObjects();
        }
        /// <summary>
        /// Set up the scene to display it properly (and load configurations)
        /// </summary>
        public void FinalizeInitialization()
        {
            m_ColumnManager.Columns[0].Views[0].IsSelected = true; // Select default view
            m_ModesManager.SetCurrentModeSpecifications(true);
            ComputeGUITextures(-1, m_ColumnManager.SelectedColumnID);
            UpdateGUITextures();
            UpdateGeometry();
            LoadConfiguration();
            SceneInformation.IsSceneInitialized = true;
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
            m_ColumnManager.MRICalMinFactor = Visualization.Configuration.MRICalMinFactor;
            m_ColumnManager.MRICalMaxFactor = Visualization.Configuration.MRICalMaxFactor;
            CameraType = Visualization.Configuration.CameraType;

            if (!string.IsNullOrEmpty(Visualization.Configuration.MeshName)) UpdateMeshToDisplay(m_ColumnManager.Meshes.FindIndex((m) => m.Name == Visualization.Configuration.MeshName));
            if (!string.IsNullOrEmpty(Visualization.Configuration.MRIName)) UpdateMRIToDisplay(m_ColumnManager.MRIs.FindIndex((m) => m.Name == Visualization.Configuration.MRIName));
            if (!string.IsNullOrEmpty(Visualization.Configuration.ImplantationName)) UpdateSites(m_ColumnManager.Implantations.FindIndex((i) => i.Name == Visualization.Configuration.ImplantationName));

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

            foreach (var cut in m_Cuts)
            {
                UpdateCutPlane(cut);
            }

            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);

            if (firstCall) ApplicationState.Module3D.OnRequestUpdateInUI.Invoke();
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
            UpdateBrainSurfaceColor(ColorType.BrainColor);
            UpdateBrainCutColor(ColorType.Default);
            UpdateColormap(ColorType.MatLab);
            UpdateMeshPartToDisplay(SceneStatesInfo.MeshPart.Both);
            EdgeMode = false;
            m_ColumnManager.MRICalMinFactor = 0.0f;
            m_ColumnManager.MRICalMaxFactor = 1.0f;
            CameraType = CameraControl.Trackball;

            switch (Type)
            {
                case SceneType.SinglePatient:
                    UpdateMeshToDisplay(m_ColumnManager.Meshes.FindIndex((m) => m.Name == "Grey matter"));
                    UpdateMRIToDisplay(m_ColumnManager.MRIs.FindIndex((m) => m.Name == "Preimplantation"));
                    UpdateSites(m_ColumnManager.Implantations.FindIndex((i) => i.Name == "Patient"));
                    break;
                case SceneType.MultiPatients:
                    UpdateMeshToDisplay(m_ColumnManager.Meshes.FindIndex((m) => m.Name == "MNI Grey matter"));
                    UpdateMRIToDisplay(m_ColumnManager.MRIs.FindIndex((m) => m.Name == "MNI"));
                    UpdateSites(m_ColumnManager.Implantations.FindIndex((i) => i.Name == "MNI"));
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

            if (firstCall) ApplicationState.Module3D.OnRequestUpdateInUI.Invoke();
        }
        #endregion

        /// <summary>
        /// Reset the number of splits of the brain mesh
        /// </summary>
        /// <param name="nbSplits">Number of splits</param>
        public void ResetSplitsNumber(int nbSplits)
        {
            if (m_ColumnManager.MeshSplitNumber == nbSplits) return;

            m_ColumnManager.MeshSplitNumber = nbSplits;

            if(m_DisplayedObjects.BrainSurfaceMeshes.Count > 0)
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
                if (!HBP3DModule.UseSimplifiedMeshes) m_DisplayedObjects.BrainSurfaceMeshes[ii].AddComponent<MeshCollider>();
                m_DisplayedObjects.BrainSurfaceMeshes[ii].SetActive(true);
            }
            if (HBP3DModule.UseSimplifiedMeshes)
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

            // get the middle
            SceneInformation.MeshCenter = SceneInformation.MeshToDisplay.BoundingBox.Center;

            // cut the mesh
            List<DLL.Surface> cuts;
            if (Cuts.Count > 0)
                cuts = new List<DLL.Surface>(SceneInformation.MeshToDisplay.Cut(Cuts.ToArray(), SceneInformation.CutHolesEnabled));
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

            SceneInformation.CollidersUpdated = false; // colliders are now longer up to date
            SceneInformation.MeshGeometryNeedsUpdate = false;   // planes are now longer requested to be updated 
            SceneInformation.IsGeneratorUpToDate = false; // generator is not up to date anymore

            // update amplitude for all columns
            for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
                m_ColumnManager.ColumnsIEEG[ii].UpdateIEEG = true;

            UnityEngine.Profiling.Profiler.EndSample();
            
            UnityEngine.Profiling.Profiler.BeginSample("Changing layers");
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.ChangeMeshesLayer(LayerMask.NameToLayer(column.Layer));
            }
            m_DisplayedObjects.SimplifiedBrain.layer = LayerMask.NameToLayer(SceneInformation.HiddenMeshesLayerName);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        public void ComputeSimplifyMeshCut()
        {
            if (SceneInformation.SimplifiedMeshToUse == null) return;

            // cut the mesh
            List<DLL.Surface> cuts;
            if (Cuts.Count > 0)
                cuts = new List<DLL.Surface>(SceneInformation.SimplifiedMeshToUse.Cut(Cuts.ToArray(), !SceneInformation.CutHolesEnabled)); //Maybe FIXME : do not allow holes
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
            
            SceneInformation.CollidersUpdated = false; // colliders are now longer up to date
            SceneInformation.MeshGeometryNeedsUpdate = false;   // planes are now longer requested to be updated 
            SceneInformation.IsGeneratorUpToDate = false; // generator is not up to date anymore

            // update amplitude for all columns
            for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
                m_ColumnManager.ColumnsIEEG[ii].UpdateIEEG = true;

            UnityEngine.Profiling.Profiler.BeginSample("Changing layers");
            foreach (Column3D column in m_ColumnManager.Columns)
            {
                column.ChangeMeshesLayer(LayerMask.NameToLayer(SceneInformation.HiddenMeshesLayerName));
            }
            m_DisplayedObjects.SimplifiedBrain.layer = LayerMask.NameToLayer(SceneInformation.MeshesLayerName);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Set UI screen space/overlays layers mask settings corresponding to the current mode of the scene
        /// </summary>
        public void SetCurrentModeSpecifications(bool force = false)
        {
            m_ModesManager.SetCurrentModeSpecifications(force);
        }
        /// <summary>
        /// Update the mesh geometry
        /// </summary>
        public void UpdateGeometry()
        {
            SceneInformation.IsGeometryUpToDate = false;
            ColumnManager.PlanesCutsCopy = Cuts;
            UpdateMeshesInformation();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update compute_meshes_cuts 1");
            if (CuttingMesh)
            {
                ComputeSimplifyMeshCut();
            }
            else
            {
                ComputeMeshesCut();
            }
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update compute_MRI_textures 1");
            ComputeMRITextures();
            ComputeFMRITextures();
            UnityEngine.Profiling.Profiler.EndSample();

            SceneInformation.IsGeometryUpToDate = true;

            m_ModesManager.UpdateMode(Mode.FunctionsId.UpdatePlane);
        }
        /// <summary>
        /// Update the sites masks
        /// </summary>
        /// <param name="allColumns">Do we apply the action on all the columns ?</param>
        /// <param name="siteGameObject">GameObject of the site on which we apply the action</param>
        /// <param name="action"> 0 : excluded / 1 : included / 2 : blacklisted / 3 : unblacklist / 4 : highlight / 5 : unhighlight / 6 : marked / 7 : unmarked </param>
        /// <param name="range"> 0 : a plot / 1 : all plots from an electrode / 2 : all plots from a patient / 3 : all highlighted / 4 : all unhighlighted 
        /// / 5 : all plots / 6 : in ROI / 7 : not in ROI / 8 : names filter / 9 : mars filter / 10 : broadman filter </param>
        public void UpdateSitesMasks(bool allColumns, SiteAction action = SiteAction.Exclude, SiteFilter filter = SiteFilter.Site, string nameFilter = "")
        {
            GameObject siteGameObject = null;
            if (m_ColumnManager.SelectedColumn.SelectedSite)
            {
                siteGameObject = m_ColumnManager.SelectedColumn.SelectedSite.gameObject;
            }
            // Check access
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.UpdateMaskPlot))
            {
                Debug.LogError("-ERROR : Base3DScene::updateMaskPlot -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }

            List<Column3D> columns = new List<Column3D>(); // List of columns we inspect
            if (allColumns)
            {
                columns = m_ColumnManager.Columns.ToList();
            }
            else
            {
                columns.Add(m_ColumnManager.SelectedColumn);
            }

            List<int> sitesID = new List<int>();
            // Build the list of the sites on which we apply actions
            foreach (Column3D column in columns)
            {
                switch(filter)
                {
                    case SiteFilter.Site:
                        {
                            sitesID.Add(siteGameObject.GetComponent<Site>().Information.GlobalID);
                        }
                        break;
                    case SiteFilter.Electrode:
                        {
                            Transform parentElectrode = siteGameObject.transform.parent;
                            for (int jj = 0; jj < parentElectrode.childCount; ++jj)
                            {
                                sitesID.Add(parentElectrode.GetChild(jj).gameObject.GetComponent<Site>().Information.GlobalID);
                            }
                        }
                        break;
                    case SiteFilter.Patient:
                        {
                            Transform parentPatient = siteGameObject.transform.parent.parent;
                            for (int jj = 0; jj < parentPatient.childCount; ++jj)
                            {
                                Transform parentElectrode = parentPatient.GetChild(jj);
                                for (int kk = 0; kk < parentElectrode.childCount; kk++)
                                {
                                    sitesID.Add(parentElectrode.GetChild(kk).gameObject.GetComponent<Site>().Information.GlobalID);
                                }
                            }
                        }
                        break;
                    case SiteFilter.Highlighted:
                        {
                            for (int jj = 0; jj < column.Sites.Count; ++jj)
                            {                                
                                if (column.Sites[jj].State.IsHighlighted)
                                    sitesID.Add(jj);
                            }
                        }
                        break;
                    case SiteFilter.Unhighlighted:
                        {                            
                            for (int jj = 0; jj < column.Sites.Count; ++jj)
                            {
                                if (!column.Sites[jj].State.IsHighlighted)
                                    sitesID.Add(jj);
                            }
                        }
                        break;
                    case SiteFilter.All:
                        {
                            for (int jj = 0; jj < column.Sites.Count; ++jj)
                            {
                                sitesID.Add(jj);
                            }
                        }
                        break;
                    case SiteFilter.InRegionOfInterest:
                        {
                            for (int jj = 0; jj < column.Sites.Count; ++jj)
                            {
                                if (!column.Sites[jj].State.IsOutOfROI)
                                    sitesID.Add(jj);
                            }
                        }
                        break;
                    case SiteFilter.OutOfRegionOfInterest:
                        {
                            for (int jj = 0; jj < column.Sites.Count; ++jj)
                            {
                                if (column.Sites[jj].State.IsOutOfROI)
                                    sitesID.Add(jj);
                            }
                        }
                        break;
                    case SiteFilter.Name:
                        {
                            for (int jj = 0; jj < column.Sites.Count; ++jj)
                            {
                                if (column.Sites[jj].Information.FullID.ToLower().Contains(nameFilter.ToLower()))
                                    sitesID.Add(jj);
                            }
                        }
                        break;
                    case SiteFilter.MarsAtlas:
                        {                            
                            for (int jj = 0; jj < column.Sites.Count; ++jj)
                            {
                                if (ApplicationState.Module3D.MarsAtlasIndex.FullName(column.Sites[jj].Information.MarsAtlasIndex).ToLower().Contains(nameFilter.ToLower()))
                                    sitesID.Add(jj);      
                            }
                        }
                        break;
                    case SiteFilter.Broadman:
                        {
                            for (int jj = 0; jj < column.Sites.Count; ++jj)
                            {
                                if (ApplicationState.Module3D.MarsAtlasIndex.BroadmanArea(column.Sites[jj].Information.MarsAtlasIndex).ToLower().Contains(nameFilter.ToLower()))
                                    sitesID.Add(jj);
                            }
                        }
                        break;
                }
            }

            // Apply action
            foreach (Column3D column in columns)
            {
                for (int ii = 0; ii < sitesID.Count; ++ii)
                {
                    switch (action)
                    {
                        case SiteAction.Include:
                            column.Sites[sitesID[ii]].State.IsExcluded = false;
                            break;
                        case SiteAction.Exclude:
                            column.Sites[sitesID[ii]].State.IsExcluded = true;
                            break;
                        case SiteAction.Blacklist:
                            column.Sites[sitesID[ii]].State.IsBlackListed = true;
                            break;
                        case SiteAction.Unblacklist:
                            column.Sites[sitesID[ii]].State.IsBlackListed = false;
                            break;
                        case SiteAction.Highlight:
                            column.Sites[sitesID[ii]].State.IsHighlighted = true;
                            break;
                        case SiteAction.Unhighlight:
                            column.Sites[sitesID[ii]].State.IsHighlighted = false;
                            break;
                        case SiteAction.Mark:
                            column.Sites[sitesID[ii]].State.IsMarked = true;
                            break;
                        case SiteAction.Unmark:
                            column.Sites[sitesID[ii]].State.IsMarked = false;
                            break;
                        default:
                            break;
                    }
                }
            }

            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);

            // Update Mode
            m_ModesManager.UpdateMode(Mode.FunctionsId.UpdateMaskPlot);

            ResetIEEG();
        }
        /// <summary>
        /// Update the sites rendering of all columns
        /// </summary>
        public void UpdateSitesRendering()
        {
            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
        }
        /// <summary>
        /// Update the data render corresponding to the column
        /// </summary>
        /// <param name="indexColumn"></param>
        /// <returns></returns>
        public bool UpdateColumnRendering(Column3D column)
        {
            if (!SceneInformation.IsGeometryUpToDate)
                return false;

            UnityEngine.Profiling.Profiler.BeginSample("TEST-updateColumnRender");

            // update cuts textures
            for (int ii = 0; ii < Cuts.Count; ++ii)
            {
                switch (column.Type)
                {
                    case Column3D.ColumnType.Base:
                        m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = column.BrainCutTextures[ii];
                        break;
                    case Column3D.ColumnType.FMRI:
                        m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = ((Column3DFMRI)column).BrainCutWithFMRITextures[ii];
                        break;
                    case Column3D.ColumnType.IEEG:
                        if (!SceneInformation.IsGeneratorUpToDate)
                        {
                            UnityEngine.Profiling.Profiler.BeginSample("TEST-updateColumnRender-BrainCutTextures");
                            m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = column.BrainCutTextures[ii];
                            UnityEngine.Profiling.Profiler.EndSample();
                        }
                        else
                        {
                            UnityEngine.Profiling.Profiler.BeginSample("TEST-updateColumnRender-BrainCutTexturesWithEEG");
                            m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = ((Column3DIEEG)column).BrainCutWithIEEGTextures[ii];
                            UnityEngine.Profiling.Profiler.EndSample();
                        }
                        break;
                    default:
                        break;
                }
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
            return true;
        }
        /// <summary>
        /// Update the brain and the cuts meshes colliders
        /// </summary>
        public void UpdateMeshesColliders()
        {
            if (!SceneInformation.MeshesLoaded || !SceneInformation.MRILoaded || SceneInformation.CollidersUpdated)
                return;

            UnityEngine.Profiling.Profiler.BeginSample("Update Meshes Colliders");
            this.StartCoroutineAsync(c_UpdateMeshesColliders());
            SceneInformation.CollidersUpdated = true;
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Update the textures generator
        /// </summary>
        public void UpdateGenerators()
        {
            // Check access
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.PreUpdateGenerators))
            {
                Debug.LogError("-ERROR : Base3DScene::pre_updateGenerators -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }

            if (SceneInformation.MeshGeometryNeedsUpdate || !SceneInformation.IsGeometryUpToDate) // if update cut plane is pending, cancel action
                return;

            // Update mode
            m_ModesManager.UpdateMode(Mode.FunctionsId.PreUpdateGenerators);

            SceneInformation.IsGeneratorUpToDate = false;
            this.StartCoroutineAsync(c_ComputeGenerators());
        }
        /// <summary>
        /// Send additionnal site info to hight level UI
        /// </summary>
        public void SendAdditionalSiteInfoRequest(Site previousSite = null)
        {
            if (m_ColumnManager.SelectedColumn.Type == Column3D.ColumnType.IEEG && !IsLatencyModeEnabled)
            {
                Column3DIEEG column = (Column3DIEEG)m_ColumnManager.SelectedColumn;
                if (column.SelectedSiteID != -1)
                {
                    List<Site> sites = new List<Site>();
                    sites.Add(column.SelectedSite);
                    if (previousSite != null) sites.Add(previousSite);
                    OnRequestSiteInformation.Invoke(sites);
                }
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
                foreach (Site site in column.Sites)
                {
                    site.State.IsOutOfROI = !SceneInformation.ShowAllSites;
                }
            }
            else
            {
                bool[] maskROI = new bool[m_ColumnManager.SitesList.Count];

                // update mask ROI
                for (int ii = 0; ii < maskROI.Length; ++ii)
                    maskROI[ii] = column.Sites[ii].State.IsOutOfROI;

                column.SelectedROI.UpdateMask(column.RawElectrodes, maskROI);
                if (SceneInformation.ShowAllSites)
                {
                    for (int ii = 0; ii < column.Sites.Count; ++ii)
                        column.Sites[ii].State.IsOutOfROI = false;
                }
                else
                {
                    for (int ii = 0; ii < column.Sites.Count; ++ii)
                        column.Sites[ii].State.IsOutOfROI = maskROI[ii];
                }
            }
            ResetIEEG();
            OnUpdateROI.Invoke();
        }
        /// <summary>
        /// Manage the mouse movments event in the scene
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="mousePosition"></param>
        /// /// <param name="idColumn"></param>
        public void PassiveRaycastOnScene(Ray ray, Column3D column)
        {
            if (!SceneInformation.MRILoaded) return;

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
                    ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new SiteInfo(site, true, Input.mousePosition, SiteInformationDisplayMode.Anatomy));
                    break;
                case Column3D.ColumnType.FMRI:
                    ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new SiteInfo(site, true, Input.mousePosition, SiteInformationDisplayMode.FMRI));
                    break;
                case Column3D.ColumnType.IEEG:
                    Column3DIEEG columnIEEG = column as Column3DIEEG;
                    int siteID = site.Information.SitePatientID;

                    float amplitude = 0;
                    if (columnIEEG.IEEGValuesBySiteID.Length > 0)
                    {
                        amplitude = columnIEEG.IEEGValuesBySiteID[siteID][columnIEEG.CurrentTimeLineID];
                    }
                    bool amplitudesComputed = SceneInformation.IsGeneratorUpToDate;
                    switch (Type)
                    {
                        case SceneType.SinglePatient:
                            string latency = "none", height = "none";
                            if (columnIEEG.CurrentLatencyFile != -1)
                            {
                                Latencies latencyFile = m_ColumnManager.SelectedImplantation.Latencies[columnIEEG.CurrentLatencyFile];

                                if (columnIEEG.SourceSelectedID == -1) // no source selected
                                {
                                    latency = "...";
                                    height = "no source selected";
                                }
                                else if (columnIEEG.SourceSelectedID == siteID) // site is the source
                                {
                                    latency = "0";
                                    height = "source";
                                }
                                else
                                {
                                    if (latencyFile.IsSiteResponsiveForSource(siteID, columnIEEG.SourceSelectedID))
                                    {
                                        latency = "" + latencyFile.LatenciesValues[columnIEEG.SourceSelectedID][siteID];
                                        height = "" + latencyFile.LatenciesValues[columnIEEG.SourceSelectedID][siteID];
                                    }
                                    else
                                    {
                                        latency = "No data";
                                        height = "No data";
                                    }
                                }
                            }
                            ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new SiteInfo(site, true, Input.mousePosition, SceneInformation.DisplayCCEPMode ? SiteInformationDisplayMode.CCEP : amplitudesComputed ? SiteInformationDisplayMode.IEEG : SiteInformationDisplayMode.Anatomy, "" + amplitude, height, latency));
                            break;
                        case SceneType.MultiPatients:
                            ApplicationState.Module3D.OnDisplaySiteInformation.Invoke(new SiteInfo(site, true, Input.mousePosition, amplitudesComputed ? SiteInformationDisplayMode.IEEG : SiteInformationDisplayMode.Anatomy, "" + amplitude));
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
            if (!SceneInformation.MRILoaded) return;

            int layerMask = 0;
            layerMask |= 1 << LayerMask.NameToLayer(m_ColumnManager.SelectedColumn.Layer);
            layerMask |= 1 << LayerMask.NameToLayer(SceneInformation.HiddenMeshesLayerName);
            layerMask |= 1 << LayerMask.NameToLayer(SceneInformation.MeshesLayerName);

            RaycastHit hit;
            bool isCollision = Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, layerMask);
            if (!isCollision)
            {
                m_ColumnManager.SelectedColumn.SelectedSiteID = -1;
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

            if (siteHit)
            {
                Site site = hit.collider.gameObject.GetComponent<Site>();
                m_ColumnManager.SelectedColumn.SelectedPatientID = site.Information.PatientNumber;
                m_ColumnManager.SelectedColumn.SelectedSiteID = site.Information.GlobalID;
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
                            selectedROI.AddBubble(m_ColumnManager.SelectedColumn.Layer, "Bubble", hit.point - transform.position, 5.0f);
                            m_ColumnManager.UpdateAllColumnsSitesRendering(SceneInformation);
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
                            m_TriEraser.EraseTriangles(ray.direction, hit.point);
                            UpdateMeshesFromDLL();
                        }
                    }
                }
            }

            if (!siteHit)
            {
                m_ColumnManager.SelectedColumn.SelectedSiteID = -1;
            }
        }
        #endregion

        #region Coroutines
        private IEnumerator c_ComputeGenerators()
        {
            yield return Ninja.JumpToUnity;
            LoadingCircle loadingCircle = ApplicationState.LoadingManager.Open();
            GenericEvent<float, float, string> OnChangeLoadingProgress = new GenericEvent<float, float, string>();
            OnChangeLoadingProgress.AddListener((progress, time, message) => loadingCircle.ChangePercentage(progress, time, message));
            Task loadingTask;
            yield return this.StartCoroutineAsync(c_LoadIEEG(OnChangeLoadingProgress), out loadingTask);
            switch (loadingTask.State)
            {
                case TaskState.Done:
                    yield return new WaitForSeconds(0.5f);
                    break;
                case TaskState.Error:
                    Exception exception = loadingTask.Exception;
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, exception.ToString(), exception.Message);
                    break;
            }
            FinalizeGeneratorsComputing();
            ComputeIEEGTextures();
            loadingCircle.Close();
            ApplicationState.Module3D.OnRequestUpdateInUI.Invoke();
        }
        private IEnumerator c_LoadIEEG(GenericEvent<float, float, string> onChangeProgress = null)
        {
            float progress = 0.0f;
            float progressStep = 1.0f / ColumnManager.ColumnsIEEG.Count;

            bool useMultiCPU = true;
            bool addValues = false;
            bool ratioDistances = true;

            // copy from main generators
            for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
            {
                for (int jj = 0; jj < m_ColumnManager.MeshSplitNumber; ++jj)
                {
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj] = (DLL.MRIBrainGenerator)m_ColumnManager.DLLCommonBrainTextureGeneratorList[jj].Clone();
                }
            }

            // Do your threaded task
            for (int ii = 0; ii < m_ColumnManager.ColumnsIEEG.Count; ++ii)
            {
                yield return Ninja.JumpToUnity;
                progress += progressStep;
                onChangeProgress.Invoke(progress, 1.0f, "Computing IEEG for column n°" + ii);
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
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].InitializeOctree(m_ColumnManager.ColumnsIEEG[ii].RawElectrodes);


                    if (!m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].ComputeDistances(m_ColumnManager.ColumnsIEEG[ii].IEEGParameters.MaximumInfluence, true))
                    {
                        Debug.LogError("Abort computing"); // useless
                        yield return null;
                    }

                    if (!m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].ComputeInfluences(m_ColumnManager.ColumnsIEEG[ii], useMultiCPU, addValues, ratioDistances))
                    {
                        Debug.LogError("Abort computing"); // useless
                        yield return null;
                    }
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
                for (int jj = 0; jj < m_ColumnManager.PlanesCutsCopy.Count; ++jj)
                {
                    m_ColumnManager.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].InitializeOctree(m_ColumnManager.ColumnsIEEG[ii].RawElectrodes);
                    m_ColumnManager.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].ComputeDistances(m_ColumnManager.ColumnsIEEG[ii].IEEGParameters.MaximumInfluence, true);

                    if (!m_ColumnManager.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].ComputeInfluences(m_ColumnManager.ColumnsIEEG[ii], useMultiCPU, addValues, ratioDistances))
                    {
                        Debug.LogError("Abort computing");
                        yield return null;
                    }

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
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].SynchronizeWithOthersGenerators(maxDensity, m_ColumnManager.ColumnsIEEG[ii].SharedMinInf, m_ColumnManager.ColumnsIEEG[ii].SharedMaxInf);
                for (int jj = 0; jj < m_ColumnManager.PlanesCutsCopy.Count; ++jj)
                    m_ColumnManager.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].SynchronizeWithOthersGenerators(maxDensity, m_ColumnManager.ColumnsIEEG[ii].SharedMinInf, m_ColumnManager.ColumnsIEEG[ii].SharedMaxInf);

                for (int jj = 0; jj < m_ColumnManager.MeshSplitNumber; ++jj)
                    m_ColumnManager.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].AdjustInfluencesToColormap();
                for (int jj = 0; jj < m_ColumnManager.PlanesCutsCopy.Count; ++jj)
                    m_ColumnManager.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].AdjustInfluencesToColormap();
            }
        }
        /// <summary>
        /// Reset the volume of the scene
        /// </summary>
        /// <param name="pathNIIBrainVolumeFile"></param>
        /// <returns></returns>
        protected IEnumerator c_LoadBrainVolume(Data.Anatomy.MRI mri)
        {
            // Check access
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.ResetNIIBrainVolumeFile))
            {
                throw new ModeAccessException(m_ModesManager.CurrentModeName);
            }

            SceneInformation.MRILoaded = false;

            // checks parameter
            if (!mri.Usable) throw new EmptyFilePathException("NII");

            MRI3D mri3D = new MRI3D(mri);
            if (mri3D.IsLoaded)
            {
                m_ColumnManager.MRIs.Add(mri3D);
            }
            else
            {
                throw new CanNotLoadNIIFile(mri.File);
            }
            
            // Update mode
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetNIIBrainVolumeFile);

            SceneInformation.MRILoaded = true;
            yield return SceneInformation.MRILoaded;
        }
        /// <summary>
        /// Reset all the sites with a new list of pts files
        /// </summary>
        /// <param name="pathsElectrodesPtsFile"></param>
        /// <returns></returns>
        protected IEnumerator c_LoadImplantations(IEnumerable<Data.Patient> patients)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.ResetElectrodesFile))
            {
                throw new ModeAccessException(m_ModesManager.CurrentModeName);
            }
            //##################

            List<string> commonImplantations = new List<string>();
            foreach (Data.Anatomy.Implantation implantation in patients.ToList()[0].Brain.Implantations)
            {
                string implantationName = implantation.Name;
                bool isImplantationCommonToAllPatients = true;
                foreach (Data.Patient patient in patients)
                {
                    if (patient.Brain.Implantations.FindIndex((i) => i.Name == implantationName) == -1)
                    {
                        isImplantationCommonToAllPatients = false;
                        break;
                    }
                }
                if (isImplantationCommonToAllPatients)
                {
                    commonImplantations.Add(implantation.Name);
                }
            }

            SceneInformation.SitesLoaded = false;

            foreach (string implantationName in commonImplantations)
            {
                List<string> ptsFiles = (from patient in patients select patient.Brain.Implantations.Find((i) => i.Name == implantationName).File).ToList();
                List<string> patientIDs = (from patient in patients select patient.ID).ToList();

                Implantation3D implantation3D = new Implantation3D(implantationName, ptsFiles, patientIDs);
                
                if (implantation3D.IsLoaded)
                {
                    m_ColumnManager.Implantations.Add(implantation3D);
                    if (Type == SceneType.SinglePatient)
                    {
                        SinglePatient3DScene scene = this as SinglePatient3DScene;
                        implantation3D.LoadLatencies(scene.Patient);
                    }
                }
                else
                {
                    throw new Exception(); // TODO
                }
            }

            SceneInformation.SitesLoaded = true;
            yield return Ninja.JumpToUnity;
            UpdateSites(0);
            yield return Ninja.JumpBack;

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetElectrodesFile);
            //##################
        }
        /// <summary>
        /// Load MNI
        /// </summary>
        /// <returns></returns>
        protected IEnumerator c_LoadMNIObjects()
        {
            yield return new WaitUntil(delegate { return ApplicationState.Module3D.MNIObjects.Loaded; });
            m_ColumnManager.Meshes.Add(ApplicationState.Module3D.MNIObjects.GreyMatter);
            m_ColumnManager.Meshes.Add(ApplicationState.Module3D.MNIObjects.WhiteMatter);
            m_ColumnManager.Meshes.Add(ApplicationState.Module3D.MNIObjects.InflatedWhiteMatter);
            m_ColumnManager.MRIs.Add(ApplicationState.Module3D.MNIObjects.MRI);
        }
        /// <summary>
        /// Define the timeline data with a patient and a list of column data
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="columnDataList"></param>
        protected IEnumerator c_SetEEGData()
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.SetTimelines))
            {
                yield return Ninja.JumpToUnity;
                throw new ModeAccessException(m_ModesManager.CurrentModeName);
            }
            //##################

            yield return Ninja.JumpToUnity;
            // update columns number
            m_ColumnManager.InitializeColumns(Column3D.ColumnType.IEEG, Visualization.Columns.Count);
            yield return Ninja.JumpBack;

            // update columns names
            for (int ii = 0; ii < Visualization.Columns.Count; ++ii)
            {
                m_ColumnManager.ColumnsIEEG[ii].Label = Visualization.Columns[ii].DisplayLabel;
            }

            // set timelines
            m_ColumnManager.SetTimelineData(Visualization.Columns);

            // set flag
            SceneInformation.TimelinesLoaded = true;

            yield return Ninja.JumpToUnity;
            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.SetTimelines);
            //##################
        }
        private IEnumerator c_UpdateMeshesColliders()
        {
            while(m_UpdatingColliders)
            {
                yield return new WaitForSeconds(0.05f);
            }
            m_UpdatingColliders = true;
            if (HBP3DModule.UseSimplifiedMeshes)
            {
                // update mesh collider TODO : remove next section when this is finished
                yield return Ninja.JumpToUnity;
                Mesh colliderMesh = m_DisplayedObjects.SimplifiedBrain.GetComponent<MeshCollider>().sharedMesh;
                yield return Ninja.JumpBack;
                // cut the simplified mesh
                if (SceneInformation.SimplifiedMeshToUse == null) SceneInformation.SimplifiedMeshToUse = m_ColumnManager.SelectedMesh.SimplifiedBoth;
                List<DLL.Surface> cuts;
                if (Cuts.Count > 0)
                    cuts = new List<DLL.Surface>(SceneInformation.SimplifiedMeshToUse.Cut(Cuts.ToArray(), !SceneInformation.CutHolesEnabled)); //Maybe FIXME : do not allow holes
                else
                    cuts = new List<DLL.Surface>() { (DLL.Surface)SceneInformation.SimplifiedMeshToUse.Clone() };
                yield return Ninja.JumpToUnity;
                cuts[0].UpdateMeshFromDLL(colliderMesh, false, true, false, false, true, false);
                m_DisplayedObjects.SimplifiedBrain.GetComponent<MeshCollider>().sharedMesh = null;
                m_DisplayedObjects.SimplifiedBrain.GetComponent<MeshCollider>().sharedMesh = colliderMesh;
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
}