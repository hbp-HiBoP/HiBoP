/**
 * \file    Column3DViewManager.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Column3DViewManager class
 */
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HBP.Module3D
{
    /// <summary>
    /// A class for managing all the columns data from a specific scene
    /// </summary>
    public class Column3DManager : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// ID of the seleccted column
        /// </summary>
        public int SelectedColumnID 
        {
            get
            {
                return Columns.FindIndex((c) => c.IsSelected);
            }
        }
        /// <summary>
        /// Selected column
        /// </summary>
        public Column3D SelectedColumn
        {
            get
            {
                return Columns.Find((c) => c.IsSelected);
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
        /// IEEG Columns of the scene
        /// </summary>
        public List<Column3DCCEP> ColumnsCCEP { get { return Columns.OfType<Column3DCCEP>().ToList(); } }

        public ReadOnlyCollection<View3D> Views
        {
            get
            {
                if (Columns.Count == 0) return new ReadOnlyCollection<View3D>(new List<View3D>());
                else return Columns[0].Views;
            }
        }
        /// <summary>
        /// Maximum number of view in a column
        /// </summary>
        public int ViewLineNumber
        {
            get
            {
                int viewNumber = 0;
                foreach (Column3D column in Columns)
                {
                    if (column.Views.Count > viewNumber)
                    {
                        viewNumber = column.Views.Count;
                    }
                }
                return viewNumber;
            }
        }
        
        /// <summary>
        /// List of the implantation3Ds of the scene
        /// </summary>
        public List<Implantation3D> Implantations = new List<Implantation3D>();
        /// <summary>
        /// Selected implantation3D ID
        /// </summary>
        public int SelectedImplantationID { get; set; }
        /// <summary>
        /// Selected implantation3D
        /// </summary>
        public Implantation3D SelectedImplantation
        {
            get
            {
                return Implantations[SelectedImplantationID];
            }
        }
        /// <summary>
        /// List of the site gameObjects
        /// </summary>
        public List<GameObject> SitesList = new List<GameObject>();
        /// <summary>
        /// List of the patient gameObjects that contain sites
        /// </summary>
        public List<GameObject> SitesPatientParent = new List<GameObject>();
        /// <summary>
        /// List of the electrode gameObjects that contain sites
        /// </summary>
        public List<List<GameObject>> SitesElectrodesParent = new List<List<GameObject>>();
        
        /// <summary>
        /// List of all the mesh3Ds of the scene
        /// </summary>
        public List<Mesh3D> Meshes = new List<Mesh3D>();
        /// <summary>
        /// List of all the loaded meshes
        /// </summary>
        public List<Mesh3D> LoadedMeshes { get { return (from mesh in Meshes where mesh.IsLoaded select mesh).ToList(); } }
        /// <summary>
        /// Number of splits of the meshes
        /// </summary>
        public int MeshSplitNumber { get; set; }
        /// <summary>
        /// Selected mesh3D ID
        /// </summary>
        public int SelectedMeshID { get; set; }
        /// <summary>
        /// Selected Mesh3D (Surface)
        /// </summary>
        public Mesh3D SelectedMesh
        {
            get
            {
                return Meshes[SelectedMeshID];
            }
        }
        /// <summary>
        /// List of splitted meshes
        /// </summary>
        public List<DLL.Surface> SplittedMeshes = new List<DLL.Surface>();
        
        /// <summary>
        /// List of the MRIs of the scene
        /// </summary>
        public List<MRI3D> MRIs = new List<MRI3D>();
        /// <summary>
        /// List of loaded MRIs
        /// </summary>
        public List<MRI3D> LoadedMRIs { get { return (from mri in MRIs where mri.IsLoaded select mri).ToList(); } }
        /// <summary>
        /// Selected MRI3D ID
        /// </summary>
        public int SelectedMRIID { get; set; }
        /// <summary>
        /// Selected MRI3D
        /// </summary>
        public MRI3D SelectedMRI
        {
            get
            {
                return MRIs[SelectedMRIID];
            }
        }

        /// <summary>
        /// Null UV vector
        /// </summary>
        public List<Vector2[]> UVNull;

        /// <summary>
        /// Common generator for each brain part
        /// </summary>
        public List<DLL.MRIBrainGenerator> DLLCommonBrainTextureGeneratorList = new List<DLL.MRIBrainGenerator>();
        /// <summary>
        /// Geometry generator for cuts
        /// </summary>
        public List<DLL.MRIGeometryCutGenerator> DLLMRIGeometryCutGeneratorList = new List<DLL.MRIGeometryCutGenerator>();
        
        private float m_MRICalMinFactor = 0.0f;
        /// <summary>
        /// MRI Cal Min Value
        /// </summary>
        public float MRICalMinFactor
        {
            get
            {
                return m_MRICalMinFactor;
            }
            set
            {
                if (m_MRICalMinFactor != value)
                {
                    m_MRICalMinFactor = value;
                    OnUpdateMRICalValues.Invoke();
                }
            }
        }

        private float m_MRICalMaxFactor = 1.0f;
        /// <summary>
        /// MRI Cal Max Value
        /// </summary>
        public float MRICalMaxFactor
        {
            get
            {
                return m_MRICalMaxFactor;
            }
            set
            {
                if (m_MRICalMaxFactor != value)
                {
                    m_MRICalMaxFactor = value;
                    OnUpdateMRICalValues.Invoke();
                }
            }
        }

        /// <summary>
        /// Manage everything concerning the FMRIs
        /// </summary>
        public FMRIManager FMRIManager { get; } = new FMRIManager();
        
        /// <summary>
        /// Brain surface color
        /// </summary>
        public Data.Enums.ColorType BrainColor { get; set; } = Data.Enums.ColorType.BrainColor;
        /// <summary>
        /// Brain cut color
        /// </summary>
        public Data.Enums.ColorType BrainCutColor { get; set; } = Data.Enums.ColorType.Default;

        private Data.Enums.ColorType m_Colormap = Data.Enums.ColorType.MatLab;
        /// <summary>
        /// Colormap
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
                DLL.Texture tex = DLL.Texture.Generate1DColorTexture(Colormap);
                tex.UpdateTexture2D(BrainColorMapTexture);
                tex.Dispose();
            }
        }

        /// <summary>
        /// Colormap texture
        /// </summary>
        public Texture2D BrainColorMapTexture;
        /// <summary>
        /// Brain texture
        /// </summary>
        public Texture2D BrainColorTexture;
        
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
        #endregion

        #region Events
        /// <summary>
        /// Event called when changing the selected state of the column manager
        /// </summary>
        [HideInInspector] public GenericEvent<bool> OnChangeSelectedState = new GenericEvent<bool>();
        /// <summary>
        /// Event called when selecting a column
        /// </summary>
        [HideInInspector] public GenericEvent<Column3D> OnSelectColumn = new GenericEvent<Column3D>();
        /// <summary>
        /// Event called when adding a column
        /// </summary>
        [HideInInspector] public UnityEvent OnAddColumn = new UnityEvent();
        /// <summary>
        /// Event called when removing a column
        /// </summary>
        [HideInInspector] public GenericEvent<Column3D> OnRemoveColumn = new GenericEvent<Column3D>();
        /// <summary>
        /// Event called when adding a line of views
        /// </summary>
        [HideInInspector] public UnityEvent OnAddViewLine = new UnityEvent();
        /// <summary>
        /// Event called when removing a line of views
        /// </summary>
        [HideInInspector] public GenericEvent<int> OnRemoveViewLine = new GenericEvent<int>();
        /// <summary>
        /// Event called when changing the values of the MRI Cal Values
        /// </summary>
        [HideInInspector] public UnityEvent OnUpdateMRICalValues = new UnityEvent();
        /// <summary>
        /// Event called when updating the alpha or cal values of the FMRI
        /// </summary>
        [HideInInspector] public UnityEvent OnRequestResetIEEG = new UnityEvent();
        /// <summary>
        /// Event called when updating the ROI mask for this column
        /// </summary>
        [HideInInspector] public UnityEvent OnUpdateROIMask = new UnityEvent();
        /// <summary>
        /// Event called when changing the IEEG span values
        /// </summary>
        [HideInInspector] public GenericEvent<Column3DDynamic> OnUpdateIEEGSpan = new GenericEvent<Column3DDynamic>();
        /// <summary>
        /// Event called when changing the transparency of the IEEG
        /// </summary>
        [HideInInspector] public GenericEvent<Column3DDynamic> OnUpdateIEEGAlpha = new GenericEvent<Column3DDynamic>();
        /// <summary>
        /// Event called when changing the gain of the sphere representing the sites
        /// </summary>
        [HideInInspector] public GenericEvent<Column3DDynamic> OnUpdateIEEGGain = new GenericEvent<Column3DDynamic>();
        /// <summary>
        /// Event called when changing the timeline ID of a column
        /// </summary>
        [HideInInspector] public GenericEvent<Column3DDynamic> OnUpdateColumnTimelineID = new GenericEvent<Column3DDynamic>();
        /// <summary>
        /// Event called when minimizing a column
        /// </summary>
        [HideInInspector] public UnityEvent OnChangeColumnMinimizedState = new UnityEvent();
        /// <summary>
        /// Event called when selecting a site in a column
        /// </summary>
        [HideInInspector] public GenericEvent<Site> OnSelectSite = new GenericEvent<Site>();
        /// <summary>
        /// Event called when changing the state of a site
        /// </summary>
        [HideInInspector] public GenericEvent<Site> OnChangeSiteState = new GenericEvent<Site>();
        /// <summary>
        /// Event called when selecting a source in a column or changing the latency file of a column
        /// </summary>
        [HideInInspector] public UnityEvent OnChangeCCEPParameters = new UnityEvent();

        [HideInInspector] public UnityEvent OnSelectCCEPSource = new UnityEvent();
        #endregion

        #region Private Methods
        private void Awake()
        {
            BrainColorMapTexture = Texture2Dutility.GenerateColorScheme();
            BrainColorTexture = Texture2Dutility.GenerateColorScheme();
        }
        private void OnDestroy()
        {
            foreach (var mesh in Meshes)
            {
                if (!mesh.HasBeenLoadedOutside)
                {
                    mesh.Clean();
                }
            }
            foreach (var mri in MRIs)
            {
                if (!mri.HasBeenLoadedOutside)
                {
                    mri.Clean();
                }
            }
            foreach (var implantation in Implantations) implantation.Clean();
            foreach (var mesh in SplittedMeshes)
            {
                mesh?.Dispose();
            }
            foreach (var dllCommonBrainTextureGenerator in DLLCommonBrainTextureGeneratorList) dllCommonBrainTextureGenerator.Dispose();
            foreach (var dllMRIGeometryCutGenerator in DLLMRIGeometryCutGeneratorList) dllMRIGeometryCutGenerator.Dispose();
        }
        /// <summary>
        /// Add a column to the scene
        /// </summary>
        /// <param name="type">Type of the column</param>
        private void AddColumn(Data.Visualization.Column baseColumn)
        {
            Column3D column = null;
            if (baseColumn is Data.Visualization.AnatomicColumn)
            {
                column = Instantiate(m_Column3DPrefab, transform.Find("Columns")).GetComponent<Column3D>();
            }
            else if (baseColumn is Data.Visualization.IEEGColumn)
            {
                column = Instantiate(m_Column3DIEEGPrefab, transform.Find("Columns")).GetComponent<Column3DIEEG>();
            }
            else if (baseColumn is Data.Visualization.CCEPColumn)
            {
                column = Instantiate(m_Column3DCCEPPrefab, transform.Find("Columns")).GetComponent<Column3DCCEP>();
            }
            column.gameObject.name = "Column " + Columns.Count;
            column.OnChangeSelectedState.AddListener((selected) =>
            {
                if (selected)
                {
                    foreach (Column3D c in Columns)
                    {
                        if (c != column)
                        {
                            foreach (View3D v in c.Views)
                            {
                                v.IsSelected = false;
                            }
                        }
                    }
                }
                OnChangeSelectedState.Invoke(selected);
                if (selected) OnSelectColumn.Invoke(column);
            });
            column.OnMoveView.AddListener((view) =>
            {
                SynchronizeViewsToReferenceView(view);
            });
            column.OnUpdateROIMask.AddListener(() =>
            {
                OnUpdateROIMask.Invoke();
            });
            column.OnChangeMinimizedState.AddListener(() =>
            {
                OnChangeColumnMinimizedState.Invoke();
            });
            column.OnChangeCCEPParameters.AddListener(() =>
            {
                OnChangeCCEPParameters.Invoke();
            });
            column.OnSelectSite.AddListener((site) =>
            {
                OnSelectSite.Invoke(site);
                foreach (Column3D c in Columns)
                {
                    if (!c.IsSelected)
                    {
                        c.UnselectSite();
                    }
                }
            });
            column.OnChangeSiteState.AddListener((site) =>
            {
                OnChangeSiteState.Invoke(site);
            });
            if (column is Column3DDynamic dynamicColumn)
            {
                dynamicColumn.DynamicParameters.OnUpdateSpanValues.AddListener(() =>
                {
                    OnUpdateIEEGSpan.Invoke(dynamicColumn);
                    column.IsRenderingUpToDate = false;
                });
                dynamicColumn.DynamicParameters.OnUpdateAlphaValues.AddListener(() =>
                {
                    OnUpdateIEEGAlpha.Invoke(dynamicColumn);
                    column.IsRenderingUpToDate = false;
                });
                dynamicColumn.DynamicParameters.OnUpdateGain.AddListener(() =>
                {
                    OnUpdateIEEGGain.Invoke(dynamicColumn);
                    column.IsRenderingUpToDate = false;
                });
                dynamicColumn.DynamicParameters.OnUpdateInfluenceDistance.AddListener(() =>
                {
                    OnRequestResetIEEG.Invoke();
                    column.IsRenderingUpToDate = false;
                });
                dynamicColumn.OnUpdateCurrentTimelineID.AddListener(() =>
                {
                    OnUpdateColumnTimelineID.Invoke(dynamicColumn);
                    column.IsRenderingUpToDate = false;
                });
                if (dynamicColumn is Column3DCCEP column3DCCEP)
                {
                    column3DCCEP.OnSelectSource.AddListener(() =>
                    {
                        OnSelectCCEPSource.Invoke();
                    });
                }
            }
            column.Initialize(Columns.Count, baseColumn, SelectedImplantation.PatientElectrodesList, SitesPatientParent, SitesList);
            column.ResetSplitsNumber(MeshSplitNumber);
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

        #region Public Method
        /// <summary>
        /// Initialize the meshes of each column
        /// </summary>
        /// <param name="meshes">Parent of the meshes</param>
        /// <param name="useSimplifiedMeshes">Are we using simplified meshes ?</param>
        public void InitializeColumnsMeshes(GameObject meshes)
        {
            foreach (Column3D column in Columns)
            {
                column.InitializeColumnMeshes(meshes);
            }
        }
        /// <summary>
        /// Reset the number of meshes splits for the brain
        /// </summary>
        /// <param name="nbSplits">Number of splits</param>
        public void ResetSplitsNumber(int nbSplits)
        {
            MeshSplitNumber = nbSplits;
            DLLCommonBrainTextureGeneratorList = new List<DLL.MRIBrainGenerator>(MeshSplitNumber);
            for (int ii = 0; ii < MeshSplitNumber; ++ii)
                DLLCommonBrainTextureGeneratorList.Add(new DLL.MRIBrainGenerator());
        }
        /// <summary>
        /// Generate the splits for the mesh
        /// </summary>
        /// <param name="meshToDisplay"></param>
        public void GenerateSplits(DLL.Surface meshToDisplay)
        {
            SplittedMeshes = meshToDisplay.SplitToSurfaces(MeshSplitNumber);
        }
        /// <summary>
        /// Reset color schemes of every columns
        /// </summary>
        public void ResetColors()
        {
            for (int ii = 0; ii < Columns.Count; ++ii)
                Columns[ii].CutTextures.ResetColorSchemes(Colormap, BrainCutColor);
        }
        /// <summary>
        /// Update the number of cut planes for every columns
        /// </summary>
        /// <param name="nbCuts">Number of cuts</param>
        public void UpdateCutNumber(int nbCuts)
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
        /// Initialize the columns for the scene
        /// </summary>
        /// <param name="type"></param>
        /// <param name="number"></param>
        public void InitializeColumns(IEnumerable<Data.Visualization.Column> columns)
        {
            foreach (Data.Visualization.Column column in columns)
            {
                AddColumn(column);
            }
        }
        /// <summary>
        /// Create the MRI texture of the cut
        /// </summary>
        /// <param name="column">Column for which the texture will be created</param>
        /// <param name="cutID">ID of the cut used to create the texture</param>
        public void CreateMRITexture(Column3D column, int cutID, int blurFactor)
        {
            column.CutTextures.CreateMRITexture(DLLMRIGeometryCutGeneratorList[cutID], SelectedMRI.Volume, cutID, MRICalMinFactor, MRICalMaxFactor, blurFactor);
            if (FMRIManager.DisplayFMRI)
            {
                FMRIManager.ColorCutTexture(column, cutID, blurFactor);
            }
        }
        /// <summary>
        /// Compute the UVs of the brain for the iEEG activity of a column
        /// </summary>
        /// <param name="column">Column on which to compute the UVs</param>
        public void ComputeSurfaceBrainUVWithIEEG(Column3DDynamic column)
        {
            for (int ii = 0; ii < MeshSplitNumber; ++ii)
                column.DLLBrainTextureGenerators[ii].ComputeSurfaceUVIEEG(SplittedMeshes[ii], column);
        }
        /// <summary>
        /// Update the sites rendering for all columns
        /// </summary>
        /// <param name="data">Information about the scene</param>
        public void UpdateAllColumnsSitesRendering(SceneStatesInfo data)
        {

            foreach (Column3D column in Columns)
            {
                Latencies latencyFile = null;
                if (column.CurrentLatencyFile != -1)
                {
                    latencyFile = SelectedImplantation.Latencies[column.CurrentLatencyFile];
                }
                column.UpdateSitesRendering(data, latencyFile);
            }

            data.AreSitesUpdated = true;
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
            if (lineID == -1) lineID = Views.Count - 1;
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
    }
}