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
        /// IEEG Columns of the scene
        /// </summary>
        public List<Column3DIEEG> ColumnsIEEG { get { return (from column in Columns where column.Type == Data.Enums.ColumnType.iEEG select (Column3DIEEG)column).ToList(); } }

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
        /// Cube bounding box around the mesh, depending on the cuts
        /// </summary>
        public DLL.BBox CubeBoundingBox { get; private set; }

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
        /// FMRI associated to this scene
        /// </summary>
        public MRI3D FMRI = null;
        
        private float m_FMRIAlpha = 0.5f;
        /// <summary>
        /// Alpha of the FMRI
        /// </summary>
        public float FMRIAlpha
        {
            get
            {
                return m_FMRIAlpha;
            }
            set
            {
                if (m_FMRIAlpha != value)
                {
                    m_FMRIAlpha = value;
                    OnUpdateFMRIParameters.Invoke();
                }
            }
        }

        private float m_FMRICalMinFactor = 0.4f;
        /// <summary>
        /// Cal min factor of the FMRI
        /// </summary>
        public float FMRICalMinFactor
        {
            get
            {
                return m_FMRICalMinFactor;
            }
            set
            {
                if (m_FMRICalMinFactor != value)
                {
                    m_FMRICalMinFactor = value;
                    OnUpdateFMRIParameters.Invoke();
                }
            }
        }
        /// <summary>
        /// Cal min value of the FMRI
        /// </summary>
        public float FMRICalMin
        {
            get
            {
                if (FMRI == null) return 0;
                return m_FMRICalMinFactor * (FMRI.Volume.ExtremeValues.ComputedCalMax - FMRI.Volume.ExtremeValues.ComputedCalMin) + FMRI.Volume.ExtremeValues.ComputedCalMin;
            }
            set
            {
                if (FMRI == null)
                {
                    FMRICalMinFactor = 0;
                    return;
                }
                FMRICalMinFactor = (value - FMRI.Volume.ExtremeValues.ComputedCalMin) / (FMRI.Volume.ExtremeValues.ComputedCalMax - FMRI.Volume.ExtremeValues.ComputedCalMin);
            }
        }

        private float m_FMRICalMaxFactor = 0.6f;
        /// <summary>
        /// Cal max factor of the FMRI
        /// </summary>
        public float FMRICalMaxFactor
        {
            get
            {
                return m_FMRICalMaxFactor;
            }
            set
            {
                if (m_FMRICalMaxFactor != value)
                {
                    m_FMRICalMaxFactor = value;
                    OnUpdateFMRIParameters.Invoke();
                }
            }
        }
        /// <summary>
        /// Cal max value of the FMRI
        /// </summary>
        public float FMRICalMax
        {
            get
            {
                if (FMRI == null) return 0;
                return m_FMRICalMaxFactor * (FMRI.Volume.ExtremeValues.ComputedCalMax - FMRI.Volume.ExtremeValues.ComputedCalMin) + FMRI.Volume.ExtremeValues.ComputedCalMin;
            }
            set
            {
                if (FMRI == null)
                {
                    FMRICalMaxFactor = 1.0f;
                    return;
                }
                FMRICalMaxFactor = (value - FMRI.Volume.ExtremeValues.ComputedCalMin) / (FMRI.Volume.ExtremeValues.ComputedCalMax - FMRI.Volume.ExtremeValues.ComputedCalMin);
            }
        }

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
        [HideInInspector] public UnityEvent OnUpdateFMRIParameters = new UnityEvent();
        /// <summary>
        /// Event called when updating the ROI mask for this column
        /// </summary>
        [HideInInspector] public UnityEvent OnUpdateROIMask = new UnityEvent();
        /// <summary>
        /// Event called when changing the IEEG span values
        /// </summary>
        [HideInInspector] public GenericEvent<Column3DIEEG> OnUpdateIEEGSpan = new GenericEvent<Column3DIEEG>();
        /// <summary>
        /// Event called when changing the transparency of the IEEG
        /// </summary>
        [HideInInspector] public GenericEvent<Column3DIEEG> OnUpdateIEEGAlpha = new GenericEvent<Column3DIEEG>();
        /// <summary>
        /// Event called when changing the gain of the sphere representing the sites
        /// </summary>
        [HideInInspector] public GenericEvent<Column3DIEEG> OnUpdateIEEGGain = new GenericEvent<Column3DIEEG>();
        /// <summary>
        /// Event called when changing the influence of each site on the texture
        /// </summary>
        [HideInInspector] public GenericEvent<Column3DIEEG> OnUpdateInfluenceDistance = new GenericEvent<Column3DIEEG>();
        /// <summary>
        /// Event called when changing the timeline ID of a column
        /// </summary>
        [HideInInspector] public GenericEvent<Column3DIEEG> OnUpdateColumnTimelineID = new GenericEvent<Column3DIEEG>();
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
            CubeBoundingBox.Dispose();
        }
        /// <summary>
        /// Add a column to the scene
        /// </summary>
        /// <param name="type">Type of the column</param>
        private void AddColumn(Data.Visualization.BaseColumn baseColumn)
        {
            Column3D column = null;
            Data.Enums.ColumnType type = baseColumn is Data.Visualization.IEEGColumn ? Data.Enums.ColumnType.iEEG : Data.Enums.ColumnType.Anatomic;
            switch (type)
            {
                case Data.Enums.ColumnType.Anatomic:
                    column = Instantiate(m_Column3DPrefab, transform.Find("Columns")).GetComponent<Column3D>();
                    break;
                case Data.Enums.ColumnType.iEEG:
                    column = Instantiate(m_Column3DIEEGPrefab, transform.Find("Columns")).GetComponent<Column3DIEEG>();
                    break;
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
            if (type == Data.Enums.ColumnType.iEEG)
            {
                Column3DIEEG columnIEEG = column as Column3DIEEG;
                columnIEEG.IEEGParameters.OnUpdateSpanValues.AddListener(() =>
                {
                    OnUpdateIEEGSpan.Invoke(columnIEEG);
                    column.IsRenderingUpToDate = false;
                });
                columnIEEG.IEEGParameters.OnUpdateAlphaValues.AddListener(() =>
                {
                    OnUpdateIEEGAlpha.Invoke(columnIEEG);
                    column.IsRenderingUpToDate = false;
                });
                columnIEEG.IEEGParameters.OnUpdateGain.AddListener(() =>
                {
                    OnUpdateIEEGGain.Invoke(columnIEEG);
                    column.IsRenderingUpToDate = false;
                });
                columnIEEG.IEEGParameters.OnUpdateInfluenceDistance.AddListener(() =>
                {
                    OnUpdateInfluenceDistance.Invoke(columnIEEG);
                    column.IsRenderingUpToDate = false;
                });
                columnIEEG.OnUpdateCurrentTimelineID.AddListener(() =>
                {
                    OnUpdateColumnTimelineID.Invoke(columnIEEG);
                    column.IsRenderingUpToDate = false;
                });
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
        public void InitializeColumns(IEnumerable<Data.Visualization.BaseColumn> columns)
        {
            foreach (Data.Visualization.BaseColumn column in columns)
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
            if (FMRI != null)
            {
                column.CutTextures.ColorCutsTexturesWithFMRI(FMRI.Volume, cutID, m_FMRICalMinFactor, m_FMRICalMaxFactor, m_FMRIAlpha);
            }
        }
        /// <summary>
        /// Compute the UVs of the brain for the iEEG activity of a column
        /// </summary>
        /// <param name="column">Column on which to compute the UVs</param>
        public void ComputeSurfaceBrainUVWithIEEG(Column3DIEEG column)
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
        /// <summary>
        /// Update the cube bounding box
        /// </summary>
        /// <param name="cuts">Cuts used for the cube bounding box</param>
        public void UpdateCubeBoundingBox(List<Cut> cuts)
        {
            CubeBoundingBox?.Dispose();
            CubeBoundingBox = SelectedMRI.Volume.GetCubeBoundingBox(cuts);
        }
        #endregion
    }
}