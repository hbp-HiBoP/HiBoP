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
                return m_Columns.FindIndex((c) => c.IsSelected);
            }
        }
        /// <summary>
        /// Selected column
        /// </summary>
        public Column3D SelectedColumn
        {
            get
            {
                return m_Columns.Find((c) => c.IsSelected);
            }
        }
        
        List<Column3D> m_Columns = new List<Column3D>();
        /// <summary>
        /// Columns of the scene
        /// </summary>
        public List<Column3D> Columns { get { return m_Columns; } }
        /// <summary>
        /// IEEG Columns of the scene
        /// </summary>
        public List<Column3DIEEG> ColumnsIEEG { get { return (from column in m_Columns where column.Type == Column3D.ColumnType.IEEG select (Column3DIEEG)column).ToList(); } }

        public ReadOnlyCollection<View3D> Views
        {
            get
            {
                if (m_Columns.Count == 0) return new ReadOnlyCollection<View3D>(new List<View3D>());
                else return m_Columns[0].Views;
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
                foreach (Column3D column in m_Columns)
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
        /// List of the surfaces for the cuts
        /// </summary>
        public List<DLL.Surface> DLLCutsList = new List<DLL.Surface>();
        
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
        
        private Data.Enums.ColorType m_BrainColor = Data.Enums.ColorType.BrainColor;
        /// <summary>
        /// Brain surface color
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
            }
        }

        private Data.Enums.ColorType m_BrainCutColor = Data.Enums.ColorType.Default;
        /// <summary>
        /// Brain cut color
        /// </summary>
        public Data.Enums.ColorType BrainCutColor
        {
            get
            {
                return m_BrainCutColor;
            }
            set
            {
                m_BrainCutColor = value;
            }
        }

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
            }
        }

        public Texture2D BrainColorMapTexture;
        public Texture2D BrainColorTexture;
        
        [SerializeField] private GameObject m_Column3DPrefab;
        [SerializeField] private GameObject m_Column3DIEEGPrefab;
        #endregion

        #region Events
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
        /// Event called when changing the number of ROIs of this scene
        /// </summary>
        [HideInInspector] public GenericEvent<Column3D> OnChangeNumberOfROI = new GenericEvent<Column3D>();
        /// <summary>
        /// Event called when changing the number of volumes in a ROI of this scene
        /// </summary>
        [HideInInspector] public GenericEvent<Column3D> OnChangeNumberOfVolumeInROI = new GenericEvent<Column3D>();
        /// <summary>
        /// Event called when selecting a ROI in a column
        /// </summary>
        [HideInInspector] public GenericEvent<Column3D> OnSelectROI = new GenericEvent<Column3D>();
        /// <summary>
        /// Event called when changing the radius of a volume in a ROI
        /// </summary>
        [HideInInspector] public GenericEvent<Column3D> OnChangeROIVolumeRadius = new GenericEvent<Column3D>();
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
        [HideInInspector] public GenericEvent<Column3DIEEG> OnUpdateIEEGMaximumInfluence = new GenericEvent<Column3DIEEG>();
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
        /// <summary>
        /// Add a column to the scene
        /// </summary>
        /// <param name="type"></param>
        private void AddColumn(Data.Visualization.Column.ColumnType type)
        {
            Column3D column = null;
            switch (type)
            {
                case Data.Visualization.Column.ColumnType.Anatomy:
                    column = Instantiate(m_Column3DPrefab, transform.Find("Columns")).GetComponent<Column3D>();
                    break;
                case Data.Visualization.Column.ColumnType.iEEG:
                    column = Instantiate(m_Column3DIEEGPrefab, transform.Find("Columns")).GetComponent<Column3DIEEG>();
                    break;
            }
            column.gameObject.name = "Column " + Columns.Count;
            column.OnChangeSelectedState.AddListener((selected) =>
            {
                if (selected)
                {
                    foreach (Column3D c in m_Columns)
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
            column.OnChangeNumberOfROI.AddListener(() =>
            {
                OnChangeNumberOfROI.Invoke(column);
            });
            column.OnChangeNumberOfVolumeInROI.AddListener(() =>
            {
                OnChangeNumberOfVolumeInROI.Invoke(column);
            });
            column.OnSelectROI.AddListener(() =>
            {
                OnSelectROI.Invoke(column);
            });
            column.OnChangeROIVolumeRadius.AddListener(() =>
            {
                OnChangeROIVolumeRadius.Invoke(column);
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
                foreach (Column3D c in m_Columns)
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
            if (type == Data.Visualization.Column.ColumnType.iEEG)
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
                columnIEEG.IEEGParameters.OnUpdateMaximumInfluence.AddListener(() =>
                {
                    OnUpdateIEEGMaximumInfluence.Invoke(columnIEEG);
                    column.IsRenderingUpToDate = false;
                });
                columnIEEG.OnUpdateCurrentTimelineID.AddListener(() =>
                {
                    OnUpdateColumnTimelineID.Invoke(columnIEEG);
                    column.IsRenderingUpToDate = false;
                });
            }
            column.Initialize(m_Columns.Count, SelectedImplantation.PatientElectrodesList, SitesPatientParent, SitesList);
            column.ResetSplitsNumber(MeshSplitNumber);
            m_Columns.Add(column);
            OnAddColumn.Invoke();
        }
        /// <summary>
        /// Synchronize all the cameras from the same view line
        /// </summary>
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
        /// <param name="meshes"></param>
        /// <param name="useSimplifiedMeshes"></param>
        public void InitializeColumnsMeshes(GameObject meshes, bool useSimplifiedMeshes)
        {
            foreach (Column3D column in m_Columns)
            {
                column.InitializeColumnMeshes(meshes, useSimplifiedMeshes);
            }
        }
        /// <summary>
        /// Reset the number of meshes splits for the brain
        /// </summary>
        /// <param name="nbSplits"></param>
        public void ResetSplitsNumber(int nbSplits)
        {
            foreach (Mesh3D mesh in Meshes)
            {
                mesh.Split(MeshSplitNumber);
            }

            DLLCommonBrainTextureGeneratorList = new List<DLL.MRIBrainGenerator>(MeshSplitNumber);
            for (int ii = 0; ii < MeshSplitNumber; ++ii)
                DLLCommonBrainTextureGeneratorList.Add(new DLL.MRIBrainGenerator());

            for (int c = 0; c < m_Columns.Count; c++)
            {
                m_Columns[c].ResetSplitsNumber(nbSplits);
            }
        }
        /// <summary>
        /// Reset color schemes of every columns
        /// </summary>
        public void ResetColors()
        {
            for (int ii = 0; ii < m_Columns.Count; ++ii)
                Columns[ii].CutTextures.ResetColorSchemes(Colormap, BrainCutColor);
        }
        /// <summary>
        /// Update the number of cut planes for every columns
        /// </summary>
        /// <param name="nbCuts"></param>
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

            for (int c = 0; c < m_Columns.Count; c++)
            {
                m_Columns[c].UpdateCutsPlanesNumber(nbCuts);
            }
        }
        /// <summary>
        /// Initialize the columns for this scene
        /// </summary>
        /// <param name="type"></param>
        /// <param name="number"></param>
        public void InitializeColumns(IEnumerable<Data.Visualization.Column> columns)
        {
            foreach (Data.Visualization.Column column in columns)
            {
                AddColumn(column.Type);
            }
        }
        /// <summary>
        /// Set timeline data for all columns
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="columnDataList"></param>
        public void SetTimelineData(List<Data.Visualization.Column> columnDataList)
        {
            for (int c = 0; c < Columns.Count; c++)
            {
                if (columnDataList[c].Type == Data.Visualization.Column.ColumnType.iEEG)
                {
                    ((Column3DIEEG)Columns[c]).SetColumnData(columnDataList[c]);
                }
            }
        }
        /// <summary>
        /// Create the cut mesh texture dll and texture2D
        /// </summary>
        /// <param name="indexCut"></param>
        public void CreateMRITexture(Column3D column, int cutID)
        {
            column.CutTextures.CreateMRITexture(DLLMRIGeometryCutGeneratorList[cutID], SelectedMRI.Volume, cutID, MRICalMinFactor, MRICalMaxFactor);
            if (FMRI != null)
            {
                column.CutTextures.ColorCutsTexturesWithFMRI(FMRI.Volume, cutID, m_FMRICalMinFactor, m_FMRICalMaxFactor, m_FMRIAlpha);
            }
        }
        /// <summary>
        /// Compute the amplitudes textures coordinates for the brain mesh
        /// When to call ? changes in IEEGColumn.currentTimeLineID, IEEGColumn.alphaMin, IEEGColumn.alphaMax
        /// </summary>
        /// <param name="whiteInflatedMeshes"></param>
        /// <param name="indexColumn"></param>
        /// <param name="thresholdInfluence"></param>
        /// <param name="alphaMin"></param>
        /// <param name="alphaMax"></param>
        public void ComputeSurfaceBrainUVWithIEEG(Column3DIEEG column)
        {
            for (int ii = 0; ii < MeshSplitNumber; ++ii)
                column.DLLBrainTextureGenerators[ii].ComputeSurfaceUVIEEG(SelectedMesh.SplittedMeshes[ii], column);
        }
        /// <summary>
        /// Update the plot rendering parameters for all columns
        /// </summary>
        public void UpdateAllColumnsSitesRendering(SceneStatesInfo data)
        {
            foreach (Column3D column in Columns) // unselect hidden sites
            {
                if (column.SelectedSite)
                {
                    if ((column.SelectedSite.State.IsBlackListed && data.HideBlacklistedSites) ||
                        (column.SelectedSite.State.IsOutOfROI && !data.ShowAllSites && !data.DisplayCCEPMode) ||
                        (column.SelectedSite.State.IsMasked && !data.DisplayCCEPMode))
                    {
                        column.UnselectSite();
                    }
                }
            }

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
            foreach (Column3D column in m_Columns)
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
            foreach (Column3D column in m_Columns)
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
        /// <param name="cuts"></param>
        public void UpdateCubeBoundingBox(List<Cut> cuts)
        {
            CubeBoundingBox = SelectedMRI.Volume.GetCubeBoundingBox(cuts);
        }
        #endregion
    }
}