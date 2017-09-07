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

        public GenericEvent<Column3DManager> OnSelectColumnManager = new GenericEvent<Column3DManager>();
        /// <summary>
        /// Event called when adding a column
        /// </summary>
        public UnityEvent OnAddColumn = new UnityEvent();
        /// <summary>
        /// Event called when removing a column
        /// </summary>
        public GenericEvent<Column3D> OnRemoveColumn = new GenericEvent<Column3D>();
        /// <summary>
        /// Event called when adding a line of views
        /// </summary>
        public UnityEvent OnAddViewLine = new UnityEvent();
        /// <summary>
        /// Event called when removing a line of views
        /// </summary>
        public GenericEvent<int> OnRemoveViewLine = new GenericEvent<int>();
        
        List<Column3D> m_Columns = new List<Column3D>();
        /// <summary>
        /// Columns of the scene
        /// </summary>
        public ReadOnlyCollection<Column3D> Columns { get { return m_Columns != null ? new ReadOnlyCollection<Column3D>(m_Columns) : new ReadOnlyCollection<Column3D>(new List<Column3D>(0)); } }
        /// <summary>
        /// IEEG Columns of the scene
        /// </summary>
        public ReadOnlyCollection<Column3DIEEG> ColumnsIEEG { get { return m_Columns != null ? new ReadOnlyCollection<Column3DIEEG>((from column in m_Columns where column is Column3DIEEG select (Column3DIEEG)column).ToArray()) : new ReadOnlyCollection<Column3DIEEG>(new List<Column3DIEEG>(0)); } }
        /// <summary>
        /// FMRI Columns of the scene
        /// </summary>
        public ReadOnlyCollection<Column3DFMRI> ColumnsFMRI { get { return m_Columns != null ? new ReadOnlyCollection<Column3DFMRI>((from column in m_Columns where column is Column3DFMRI select (Column3DFMRI)column).ToArray()) : new ReadOnlyCollection<Column3DFMRI>(new List<Column3DFMRI>(0)); } }

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

        // Sites
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

        // latency        
        public bool LatencyFilesDefined = false;
        public bool LatencyFileAvailable = false; /**< latency file is available */
        public List<Latencies> LatenciesFiles = new List<Latencies>(); /*< list of latency files */

        // textures
        public List<Vector2[]> UVNull = null;                   /**< null uv vectors */ // // new List<Vector2[]>(); 
        public Color NotInBrainColor = Color.black;


        public List<DLL.MRIBrainGenerator> DLLCommonBrainTextureGeneratorList = null; /**< common generators for each brain part  */

        // Common columns cut textures 
        //  textures 2D
        public List<Texture2D> RightGUICutTextures = null;                  /**< list of rotated cut textures| */

        //  generator DLL
        public List<DLL.MRIGeometryCutGenerator> DLLMRIGeometryCutGeneratorList = null;      
        
        // surface
        /// <summary>
        /// List of all the mesh3Ds of the scene
        /// </summary>
        public List<Mesh3D> Meshes = new List<Mesh3D>();
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
        public List<DLL.Surface> DLLCutsList = null;

        // volume
        /// <summary>
        /// List of the MRIs of the scene
        /// </summary>
        public List<MRI3D> MRIs = new List<MRI3D>();
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

        public List<DLL.Volume> DLLVolumeFMriList = null;
        
        // planes
        public List<Cut> PlanesCutsCopy = new List<Cut>();

        // UV coordinates
        public List<Vector2[]> UVCoordinatesSplits = null; // uv coordinates for each brain mesh split

        //
        public bool[] CommonMask = null;


        // textures
        private ColorType m_BrainColor = ColorType.BrainColor;
        /// <summary>
        /// Brain surface color
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
            }
        }

        private ColorType m_BrainCutColor = ColorType.Default;
        /// <summary>
        /// Brain cut color
        /// </summary>
        public ColorType BrainCutColor
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

        private ColorType m_Colormap = ColorType.MatLab;
        /// <summary>
        /// Colormap
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
                DLL.Texture tex = DLL.Texture.Generate1DColorTexture(Colormap);
                tex.UpdateTexture2D(BrainColorMapTexture);
            }
        }

        public Texture2D BrainColorMapTexture = null;
        public Texture2D BrainColorTexture = null;

        /// <summary>
        /// Event called when changing the values of the MRI Cal Values
        /// </summary>
        public UnityEvent OnUpdateMRICalValues = new UnityEvent();
        public GenericEvent<Column3DIEEG> OnUpdateIEEGSpan = new GenericEvent<Column3DIEEG>();
        public GenericEvent<Column3DIEEG> OnUpdateIEEGAlpha = new GenericEvent<Column3DIEEG>();
        public GenericEvent<Column3DIEEG> OnUpdateIEEGGain = new GenericEvent<Column3DIEEG>();
        public GenericEvent<Column3DIEEG> OnUpdateIEEGMaximumInfluence = new GenericEvent<Column3DIEEG>();
        public GenericEvent<Column3DIEEG> OnUpdateColumnTimelineID = new GenericEvent<Column3DIEEG>();

        // Column 3D Prefabs
        public GameObject Column3DViewIEEGPrefab;
        public GameObject Column3DViewFMRIPrefab;
        #endregion

        #region Private Methods
        private void Awake()
        {
            BrainColorMapTexture = Texture2Dutility.GenerateColorScheme();
            BrainColorTexture = Texture2Dutility.GenerateColorScheme();
        }
        /// <summary>
        /// Add a IEEG column to this scene
        /// </summary>
        private void AddIEEGColumn()
        {
            Column3DIEEG column = Instantiate(Column3DViewIEEGPrefab, transform.Find("Columns")).GetComponent<Column3DIEEG>();
            column.gameObject.name = "Column IEEG " + ColumnsIEEG.Count;
            column.ID = ++ApplicationState.Module3D.NumberOfColumnsSinceStart;
            column.OnSelectColumn.AddListener((selectedColumn) =>
            {
                foreach (Column3D c in m_Columns)
                {
                    if (c != selectedColumn)
                    {
                        c.IsSelected = false;
                        foreach (View3D v in c.Views)
                        {
                            v.IsSelected = false;
                        }
                    }
                }
                OnSelectColumnManager.Invoke(this);
                ApplicationState.Module3D.OnSelectColumn.Invoke(selectedColumn);
            });
            column.OnMoveView.AddListener((view) =>
            {
                SynchronizeViewsToReferenceView(view);
            });
            column.IEEGParameters.OnUpdateSpanValues.AddListener(() =>
            {
                OnUpdateIEEGSpan.Invoke(column);
                column.IsRenderingUpToDate = false;
            });
            column.IEEGParameters.OnUpdateAlphaValues.AddListener(() =>
            {
                OnUpdateIEEGAlpha.Invoke(column);
                column.IsRenderingUpToDate = false;
            });
            column.IEEGParameters.OnUpdateGain.AddListener(() =>
            {
                OnUpdateIEEGGain.Invoke(column);
                column.IsRenderingUpToDate = false;
            });
            column.IEEGParameters.OnUpdateMaximumInfluence.AddListener(() =>
            {
                OnUpdateIEEGMaximumInfluence.Invoke(column);
                column.IsRenderingUpToDate = false;
            });
            column.OnUpdateCurrentTimelineID.AddListener(() =>
            {
                OnUpdateColumnTimelineID.Invoke(column);
                column.IsRenderingUpToDate = false;
            });
            m_Columns.Add(column);
            //column.transform.localPosition = new Vector3(0, HBP3DModule.SPACE_BETWEEN_SCENES_AND_COLUMNS * m_Columns.Count);
            OnAddColumn.Invoke();
        }
        /// <summary>
        /// Add a FMRI Column to this scene
        /// </summary>
        private void AddFMRIColumn()
        {
            Column3DFMRI column = Instantiate(Column3DViewFMRIPrefab, transform.Find("Columns")).GetComponent<Column3DFMRI>();
            column.gameObject.name = "Column FMRI " + ColumnsFMRI.Count;
            column.ID = ++ApplicationState.Module3D.NumberOfColumnsSinceStart;
            column.OnSelectColumn.AddListener((selectedColumn) =>
            {
                foreach (Column3D c in m_Columns)
                {
                    if (c != selectedColumn)
                    {
                        c.IsSelected = false;
                        foreach (View3D v in c.Views)
                        {
                            v.IsSelected = false;
                        }
                    }
                }
                OnSelectColumnManager.Invoke(this);
                ApplicationState.Module3D.OnSelectColumn.Invoke(selectedColumn);
            });
            m_Columns.Add(column);
            OnAddColumn.Invoke();
        }
        /// <summary>
        /// Remove the last IEEG Column of this scene
        /// </summary>
        private void RemoveIEEGColumn()
        {
            if (ColumnsIEEG.Count > 0)
            {
                Column3DIEEG column = ColumnsIEEG[ColumnsIEEG.Count - 1];
                int columnID = m_Columns.IndexOf(column);
                Destroy(m_Columns[columnID]);
                m_Columns.RemoveAt(columnID);
                OnRemoveColumn.Invoke(column);
            }
        }
        /// <summary>
        /// Remove the last FMRI Column of this scene
        /// </summary>
        private void RemoveFMRIColumn()
        {
            if (ColumnsIEEG.Count > 0)
            {
                Column3DFMRI column = ColumnsFMRI[ColumnsFMRI.Count - 1];
                int columnID = m_Columns.IndexOf(column);
                Destroy(m_Columns[columnID]);
                m_Columns.RemoveAt(columnID);
                OnRemoveColumn.Invoke(column);
            }
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
        /// Reset all data.
        /// </summary>
        public void Initialize(int cutPlanesNb)
        {
            // surfaces
            DLLCutsList = new List<DLL.Surface>();

            if (DLLVolumeFMriList != null)
                for (int ii = 0; ii < DLLVolumeFMriList.Count; ++ii)
                    DLLVolumeFMriList[ii].Dispose();
            DLLVolumeFMriList = new List<DLL.Volume>();

            // cuts
            //  textures 2D            
            RightGUICutTextures = new List<Texture2D>(cutPlanesNb);

            //  DLL generators
            DLLMRIGeometryCutGeneratorList = new List<DLL.MRIGeometryCutGenerator>(cutPlanesNb);
            for (int ii = 0; ii < cutPlanesNb; ++ii)
            {                
                RightGUICutTextures.Add(new Texture2D(1, 1));                
                DLLMRIGeometryCutGeneratorList.Add(new DLL.MRIGeometryCutGenerator());
            }

            if (m_Columns != null)
            {
                for (int c = 0; c < m_Columns.Count; c++)
                {
                    m_Columns[c].Clear();
                    Destroy(m_Columns[c].gameObject);
                }
                m_Columns.Clear();
            }
            else m_Columns = new List<Column3D>();

            //ResetSplitsNumber(1);
        }
        public void InitializeColumnsMeshes(GameObject meshes)
        {
            foreach (Column3D column in m_Columns)
            {
                column.InitializeColumnMeshes(meshes);
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
                mesh.SplittedMeshes = new List<DLL.Surface>(MeshSplitNumber);
            }

            // uv coordinates
            UVCoordinatesSplits = new List<Vector2[]>(Enumerable.Repeat(new Vector2[0], MeshSplitNumber));

            // brain
            //  generators
            DLLCommonBrainTextureGeneratorList = new List<DLL.MRIBrainGenerator>(MeshSplitNumber);
            for (int ii = 0; ii < MeshSplitNumber; ++ii)
                DLLCommonBrainTextureGeneratorList.Add(new DLL.MRIBrainGenerator());

            for (int c = 0; c < m_Columns.Count; c++)
            {
                m_Columns[c].ResetSplitsNumber(nbSplits);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void ResetColors()
        {
            for (int ii = 0; ii < m_Columns.Count; ++ii)
                Columns[ii].ResetColorSchemes(Colormap, BrainCutColor);
        }
        /// <summary>
        /// Update the number of cut planes for every column
        /// </summary>
        /// <param name="nbCuts"></param>
        public void UpdateCutNumber(int nbCuts)
        {
            // update common
            int diffCuts = DLLMRIGeometryCutGeneratorList.Count - nbCuts;
            if (diffCuts < 0)
            {
                // textures 2D
                for (int ii = 0; ii < -diffCuts; ++ii)
                {
                    // GO textures        
                    RightGUICutTextures.Add(new Texture2D(1, 1));
                    int id = RightGUICutTextures.Count - 1;
                    RightGUICutTextures[id].filterMode = FilterMode.Trilinear; // TODO : test performances with this parameter
                    RightGUICutTextures[id].wrapMode = TextureWrapMode.Clamp;
                    RightGUICutTextures[id].anisoLevel = 9; // TODO : test performances with this parameter
                    RightGUICutTextures[id].mipMapBias = -2; // never superior to -1 (colorscheme 8 texture glitch)    

                    // DLL generators       
                    DLLMRIGeometryCutGeneratorList.Add(new DLL.MRIGeometryCutGenerator());
                }
            }
            else if (diffCuts > 0)
            {                
                for (int ii = 0; ii < diffCuts; ++ii)
                {
                    // GO textures
                    Destroy(RightGUICutTextures[RightGUICutTextures.Count - 1]);
                    RightGUICutTextures.RemoveAt(RightGUICutTextures.Count - 1);

                    // DLL generators
                    DLLMRIGeometryCutGeneratorList[DLLMRIGeometryCutGeneratorList.Count - 1].Dispose();
                    DLLMRIGeometryCutGeneratorList.RemoveAt(DLLMRIGeometryCutGeneratorList.Count - 1);
                }
            }

            // update columns
            for (int c = 0; c < m_Columns.Count; c++)
            {
                m_Columns[c].UpdateCutsPlanesNumber(diffCuts);
            }
        }
        /// <summary>
        /// Update the number of columns and set the number of cut planes for each ones
        /// </summary>
        /// <param name="nbIEEGColumns"></param>
        /// /// <param name="nbIRMFColumns"></param>
        /// <param name="nbCuts"></param>
        public void UpdateColumnsNumber(int nbIEEGColumns, int nbIRMFColumns, int nbCuts) //FIXME : rework this function (make it disappear and only use add columns methods)
        {            
            // clean data columns if changes in data columns nb
            if (nbIEEGColumns != ColumnsIEEG.Count)
            {
                foreach (var column in ColumnsIEEG) column.Clear();
            }

            // clean IRMF columns if changes in IRMF columns nb
            if (nbIRMFColumns != ColumnsFMRI.Count)
            {
                foreach (var column in ColumnsFMRI) column.Clear();
            }

            // resize the data GO list            
            int diffIEEGColumns = ColumnsIEEG.Count - nbIEEGColumns;
            if (diffIEEGColumns < 0)
            {
                for (int ii = 0; ii < -diffIEEGColumns; ++ii)
                {
                    AddIEEGColumn();
                }
            }
            else if (diffIEEGColumns > 0)
            {
                for (int ii = 0; ii < diffIEEGColumns; ++ii)
                {
                    RemoveIEEGColumn();
                }
            }

            // resize the IRMF GO list      
            int diffIRMFColumns = ColumnsFMRI.Count - nbIRMFColumns;
            if (diffIRMFColumns < 0)
            {
                for (int ii = 0; ii < -diffIRMFColumns; ++ii)
                {
                    // add column
                    DLLVolumeFMriList.Add(new DLL.Volume());
                    AddFMRIColumn();
                }
            }
            else if (diffIRMFColumns > 0)
            {
                for (int ii = 0; ii < diffIRMFColumns; ++ii)
                {
                    // destroy column
                    int idColumn = ColumnsFMRI.Count - 1;
                    RemoveFMRIColumn();

                    DLLVolumeFMriList[idColumn].Dispose();
                    DLLVolumeFMriList.RemoveAt(idColumn);
                }
            }

            // init new columns IEEG            
            if (diffIEEGColumns != 0)
            {
                for (int ii = 0; ii < nbIEEGColumns; ++ii)
                {
                    ColumnsIEEG[ii].Initialize(ii, nbCuts, SelectedImplantation.PatientElectrodesList, SitesPatientParent, SitesList);
                    ColumnsIEEG[ii].ResetSplitsNumber(MeshSplitNumber);

                    if (LatencyFilesDefined)
                        ColumnsIEEG[ii].CurrentLatencyFile = 0;
                }
            }

            // init new columns IRMF
            if (diffIRMFColumns != 0)
            {
                // update IRMF columns mask
                bool[] maskColumnsOR = new bool[SelectedImplantation.PatientElectrodesList.TotalSitesNumber];
                for (int ii = 0; ii < SitesList.Count; ++ii)
                {
                    bool mask = false;

                    for (int jj = 0; jj < ColumnsIEEG.Count; ++jj)
                    {
                        mask = mask || ColumnsIEEG[jj].Sites[ii].Information.IsMasked;
                    }

                    maskColumnsOR[ii] = mask;
                }

                for (int ii = 0; ii < ColumnsFMRI.Count; ++ii)
                {
                    for (int jj = 0; jj < SitesList.Count; ++jj)
                    {
                        ColumnsFMRI[ii].Sites[jj].Information.IsMasked = maskColumnsOR[jj];
                    }
                }
                
                for (int ii = 0; ii < nbIRMFColumns; ++ii)
                {
                    ColumnsFMRI[ColumnsFMRI.Count - 1].Initialize(ColumnsIEEG.Count + ii, nbCuts, SelectedImplantation.PatientElectrodesList, SitesPatientParent, SitesList);
                    ColumnsFMRI[ColumnsFMRI.Count - 1].ResetSplitsNumber(MeshSplitNumber);

                    for (int jj = 0; jj < SitesList.Count; ++jj)
                    {
                        ColumnsFMRI[ColumnsFMRI.Count - 1].Sites[jj].Information.IsMasked = maskColumnsOR[jj];
                    }
                }
            }


            CommonMask = new bool[SelectedImplantation.PatientElectrodesList.TotalSitesNumber];

            if (SelectedColumnID >= m_Columns.Count && SelectedColumnID > 0)
                m_Columns.Last().IsSelected = true;


            for (int ii = 0; ii < m_Columns.Count; ++ii)
                Columns[ii].ResetColorSchemes(Colormap, BrainCutColor);
        }
        /// <summary>
        /// Set timeline data for all columns
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="columnDataList"></param>
        public void SetTimelineData(List<Data.Visualization.Column> columnDataList)
        {
            for (int c = 0; c < ColumnsIEEG.Count; c++)
            {
                ColumnsIEEG[c].SetColumnData(columnDataList[c]);
            }
        }
        /// <summary>
        /// Create the cut mesh texture dll and texture2D
        /// </summary>
        /// <param name="indexCut"></param>
        public void CreateMRITexture(int indexCut, int indexColumn)
        {
            UnityEngine.Profiling.Profiler.BeginSample("create_MRI_texture");
                Columns[indexColumn].CreateMRITexture(DLLMRIGeometryCutGeneratorList[indexCut], SelectedMRI.Volume, indexCut, MRICalMinFactor, MRICalMaxFactor);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        public void CreateGUIMRITexture(int indexCut, int indexColumn)
        {
            string orientation = "";
            switch (PlanesCutsCopy[indexCut].Orientation)
            {
                case CutOrientation.Axial:
                    orientation = "Axial";
                    break;
                case CutOrientation.Coronal:
                    orientation = "Coronal";
                    break;
                case CutOrientation.Sagital:
                    orientation = "Sagital";
                    break;
                case CutOrientation.Custom:
                    orientation = "custom";
                    break;
                default:
                    orientation = "custom";
                    break;
            }

            Columns[indexColumn].CreateGUIMRITexture(indexCut, orientation, PlanesCutsCopy[indexCut].Flip, PlanesCutsCopy, orientation != "custom");            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        public void CreateGUIIEEGTexture(int indexCut, int indexColumn)
        {
            string orientation = "";
            switch (PlanesCutsCopy[indexCut].Orientation)
            {
                case CutOrientation.Axial:
                    orientation = "Axial";
                    break;
                case CutOrientation.Coronal:
                    orientation = "Coronal";
                    break;
                case CutOrientation.Sagital:
                    orientation = "Sagital";
                    break;
                case CutOrientation.Custom:
                    orientation = "custom";
                    break;
                default:
                    orientation = "custom";
                    break;
            }

            ((Column3DIEEG)Columns[indexColumn]).CreateGUIIEEGTexture(indexCut, orientation, PlanesCutsCopy[indexCut].Flip, PlanesCutsCopy, orientation != "custom");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        public void CreateGUIFMRITexture(int indexCut, int indexColumn)
        {
            string orientation = "";
            switch (PlanesCutsCopy[indexCut].Orientation)
            {
                case CutOrientation.Axial:
                    orientation = "Axial";
                    break;
                case CutOrientation.Coronal:
                    orientation = "Coronal";
                    break;
                case CutOrientation.Sagital:
                    orientation = "Sagital";
                    break;
                case CutOrientation.Custom:
                    orientation = "custom";
                    break;
                default:
                    orientation = "custom";
                    break;
            }

            ((Column3DFMRI)Columns[indexColumn]).CreateGUIFMRITexture(indexCut, orientation, PlanesCutsCopy[indexCut].Flip, PlanesCutsCopy, orientation != "custom");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexColumn"></param>
        /// <param name="indexCut"></param>
        /// <param name="thresholdInfluence"></param>
        public void ColorCutsTexturesWithIEEG(Column3DIEEG column, int indexCut)
        {       
            DLL.MRITextureCutGenerator generator = column.DLLMRITextureCutGenerators[indexCut];        
            generator.FillTextureWithIEEG(column, column.DLLCutColorScheme, NotInBrainColor);

            DLL.Texture cutTexture = column.DLLBrainCutWithIEEGTextures[indexCut];
            generator.UpdateTextureWithIEEG(cutTexture);
            cutTexture.UpdateTexture2D(column.BrainCutWithIEEGTextures[indexCut]); // update mesh cut 2D texture
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexColumn"></param>
        /// <param name="indexCut"></param>
        public void ColorCutsTexturesWithFMRI(int indexColumn, int indexCut)
        {
            Column3DFMRI column = ColumnsFMRI[indexColumn];
            DLL.MRITextureCutGenerator generator = column.DLLMRITextureCutGenerators[indexCut];
            generator.FillTextureWithFMRI(column, DLLVolumeFMriList[indexColumn]);

            DLL.Texture cutTexture = column.DLLBrainCutWithFMRITextures[indexCut];
            generator.UpdateTextureWithFMRI(cutTexture);
            cutTexture.UpdateTexture2D(column.BrainCutWithFMRITextures[indexCut]); // update mesh cut 2D texture
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
        public bool ComputeSurfaceBrainUVWithIEEG(Column3DIEEG column)
        {
            for (int ii = 0; ii < MeshSplitNumber; ++ii)
                if(!column.DLLBrainTextureGenerators[ii].ComputeSurfaceUVIEEG(SelectedMesh.SplittedMeshes[ii], column))
                    return false;

            return true;
        }
        /// <summary>
        /// Update the plot rendering parameters for all columns
        /// </summary>
        public void UpdateAllColumnsSitesRendering(SceneStatesInfo data)
        {
            // unselect blacklisted hidden sites
            if (data.HideBlacklistedSites)
            {
                foreach (Column3D column in Columns)
                {
                    if (column.SelectedSite)
                    {
                        if (column.SelectedSite.Information.IsBlackListed)
                        {
                            column.SelectedSiteID = -1;
                            if (column.IsSelected)
                            {
                                ApplicationState.Module3D.OnSelectSite.Invoke(null);
                            }
                        }
                    }
                }
            }

            for (int ii = 0; ii < ColumnsIEEG.Count; ++ii)
            {
                Latencies latencyFile = null;
                if (ColumnsIEEG[ii].CurrentLatencyFile != -1)
                    latencyFile = LatenciesFiles[ColumnsIEEG[ii].CurrentLatencyFile];

                ColumnsIEEG[ii].UpdateSitesSizeAndColorForIEEG(); // TEST
                ColumnsIEEG[ii].UpdateSitesRendering(data, latencyFile);
            }

            for (int ii = 0; ii < ColumnsFMRI.Count; ++ii)
                ColumnsFMRI[ii].UpdateSitesVisibility(data);
        }
        public void UpdateColumnIEEGSitesRendering(Column3DIEEG column, SceneStatesInfo data)
        {
            Latencies latencyFile = null;
            if (column.CurrentLatencyFile != -1)
                latencyFile = LatenciesFiles[column.CurrentLatencyFile];

            column.UpdateSitesSizeAndColorForIEEG(); // TEST
            column.UpdateSitesRendering(data, latencyFile);
        }
        /// <summary>
        /// Update the visiblity of the ROI for all columns
        /// </summary>
        /// <param name="visible"></param>
        public void UpdateROIVisibility(bool visible)
        {
            for(int ii = 0; ii < m_Columns.Count; ++ii)
                if (m_Columns[ii].SelectedROI != null)
                    m_Columns[ii].SelectedROI.SetRenderingState(visible);
        }
        /// <summary>
        /// Update the visiblity of the plots for all columns
        /// </summary>
        /// <param name="visible"></param>
        public void UpdateSitesVisibility(bool visible)
        {
            foreach (var column in m_Columns) column.SetSitesVisibility(visible);
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
            foreach (Column3D column in m_Columns)
            {
                column.RemoveView(lineID);
            }
            OnRemoveViewLine.Invoke(ViewLineNumber);
        }
        #endregion
    }
}