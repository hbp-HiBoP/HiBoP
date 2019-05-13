using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;
using System.Linq;
using System.IO;
using HBP.Module3D.DLL;
using HBP.Data.Enums;
using Tools.Unity;

namespace HBP.Module3D
{
    /// <summary>
    /// Column 3D base class (anatomical data only)
    /// </summary>
    public class Column3D : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Type of the column
        /// </summary>
        public virtual ColumnType Type
        {
            get
            {
                return ColumnType.Anatomic;
            }
        }
        /// <summary>
        /// Column data of this column 3D
        /// </summary>
        public Data.Visualization.BaseColumn ColumnData { get; private set; }
        /// <summary>
        /// Name of the column
        /// </summary>
        public string Label
        {
            get
            {
                return ColumnData.Name;
            }
        }
        /// <summary>
        /// Layer on which the objects of this column are displayed
        /// </summary>
        public string Layer { get; protected set; }

        private bool m_IsSelected;
        /// <summary>
        /// Is this column selected ?
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
                OnChangeSelectedState.Invoke(value);
                if (m_IsSelected)
                {
                    ApplicationState.Module3D.OnSelectColumn.Invoke(this);
                }
            }
        }

        private bool m_IsMinimized;
        /// <summary>
        /// Is this column minimized ?
        /// </summary>
        public bool IsMinimized
        {
            get
            {
                return m_IsMinimized;
            }
            set
            {
                if (m_IsMinimized != value)
                {
                    m_IsMinimized = value;
                    OnChangeMinimizedState.Invoke();
                }
            }
        }
        /// <summary>
        /// Does the column rendering need to be updated ?
        /// </summary>
        public bool IsRenderingUpToDate { get; set; } = false;

        /// <summary>
        /// Parent of the meshes displayed in this column
        /// </summary>
        [SerializeField] private Transform m_BrainSurfaceMeshesParent;
        /// <summary>
        /// Prefab for the brain surface
        /// </summary>
        [SerializeField] private GameObject m_BrainPrefab;
        /// <summary>
        /// Surface meshes displayed in this column
        /// </summary>
        public List<GameObject> BrainSurfaceMeshes { get; private set; } = new List<GameObject>();

        /// <summary>
        /// View prefab
        /// </summary>
        [SerializeField] protected GameObject m_ViewPrefab;
        protected List<View3D> m_Views = new List<View3D>();
        /// <summary>
        /// Views of this column
        /// </summary>
        public ReadOnlyCollection<View3D> Views
        {
            get
            {
                return new ReadOnlyCollection<View3D>(m_Views);
            }
        }
        /// <summary>
        /// Currently selected view
        /// </summary>
        public View3D SelectedView
        {
            get
            {
                foreach (View3D view in Views)
                {
                    if (view.IsSelected)
                    {
                        return view;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Currently selected site
        /// </summary>
        public Site SelectedSite { get; protected set; }
        /// <summary>
        /// Currently selected site ID
        /// </summary>
        public int SelectedSiteID { get { return SelectedSite != null ? SelectedSite.Information.GlobalID : -1; } }
        /// <summary>
        /// ID of the patient which site is currently selected
        /// </summary>
        public int SelectedPatientID { get { return SelectedSite != null ? SelectedSite.Information.PatientNumber : -1; } }

        protected RawSiteList m_RawElectrodes;
        /// <summary>
        /// Raw site list
        /// </summary>
        public RawSiteList RawElectrodes
        {
            get
            {
                return m_RawElectrodes;
            }
        }
        /// <summary>
        /// Sites GameObjects. List order is Patient > Electrode > Site
        /// </summary>
        public List<List<List<GameObject>>> SitesGameObjects;
        /// <summary>
        /// Sites of this column
        /// </summary>
        public List<Site> Sites;
        /// <summary>
        /// Site state by site ID. Used when changing the implantation
        /// </summary>
        public Dictionary<string, SiteState> SiteStateBySiteID = new Dictionary<string, SiteState>();

        [SerializeField] protected SiteRing m_SelectRing;
        /// <summary>
        /// Selection ring feedback
        /// </summary>
        public SiteRing SelectRing { get { return m_SelectRing; } }

        /// <summary>
        /// Parent for ROI GameObjects
        /// </summary>
        [SerializeField] protected Transform m_ROIParent;
        protected List<ROI> m_ROIs = new List<ROI>();
        /// <summary>
        /// List of the ROIs of this column
        /// </summary>
        public ReadOnlyCollection<ROI> ROIs
        {
            get
            {
                return new ReadOnlyCollection<ROI>(m_ROIs);
            }
        }
        protected ROI m_SelectedROI = null;
        /// <summary>
        /// Currently selected ROI
        /// </summary>
        public ROI SelectedROI
        {
            get
            {
                return m_SelectedROI;
            }
            set
            {
                if (value == null)
                {
                    m_SelectedROI = null;
                }
                else
                {
                    if (m_SelectedROI != null)
                    {
                        m_SelectedROI.SetVisibility(false);
                    }

                    m_SelectedROI = value;
                    m_SelectedROI.SetVisibility(true);
                    m_SelectedROI.StartAnimation();
                }
                UpdateROIMask();
            }
        }
        /// <summary>
        /// ID of the currently selected ROI
        /// </summary>
        public int SelectedROIID
        {
            get
            {
                return m_ROIs.FindIndex((roi) => roi == SelectedROI);
            }
            set
            {
                SelectedROI = value == -1 ? null : m_ROIs[value];
            }
        }
        /// <summary>
        /// Prefab for the ROI
        /// </summary>
        [SerializeField] private GameObject m_ROIPrefab;

        /// <summary>
        /// Texture generator for the brain surface
        /// </summary>
        public List<MRIBrainGenerator> DLLBrainTextureGenerators = new List<MRIBrainGenerator>();
        /// <summary>
        /// Volume generator for cut textures
        /// </summary>
        public MRIVolumeGenerator DLLMRIVolumeGenerator;
        /// <summary>
        /// Cut Textures Utility
        /// </summary>
        public CutTexturesUtility CutTextures { get; private set; }

        /// <summary>
        /// Is a source defined ?
        /// </summary>
        public bool SourceDefined { get { return SelectedSiteID != -1; } }
        private int m_CurrentLatencyFile = -1;
        /// <summary>
        /// Currently selected latency file
        /// </summary>
        public int CurrentLatencyFile
        {
            get
            {
                return m_CurrentLatencyFile;
            }
            set
            {
                m_CurrentLatencyFile = value;
                OnChangeCCEPParameters.Invoke();
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Event called when this column is selected
        /// </summary>
        [HideInInspector] public GenericEvent<bool> OnChangeSelectedState = new GenericEvent<bool>();
        /// <summary>
        /// Event called when a view is moved
        /// </summary>
        [HideInInspector] public GenericEvent<View3D> OnMoveView = new GenericEvent<View3D>();
        /// <summary>
        /// Event called when updating the ROI mask for this column
        /// </summary>
        [HideInInspector] public UnityEvent OnUpdateROIMask = new UnityEvent();
        /// <summary>
        /// Event called when minimizing a column
        /// </summary>
        [HideInInspector] public UnityEvent OnChangeMinimizedState = new UnityEvent();
        /// <summary>
        /// Event called when selecting a site
        /// </summary>
        [HideInInspector] public GenericEvent<Site> OnSelectSite = new GenericEvent<Site>();
        /// <summary>
        /// Event called each time we change the state of a site
        /// </summary>
        [HideInInspector] public GenericEvent<Site> OnChangeSiteState = new GenericEvent<Site>();
        /// <summary>
        /// Event called when selecting a source or when changing the latency file
        /// </summary>
        [HideInInspector] public UnityEvent OnChangeCCEPParameters = new UnityEvent();
        #endregion

        #region Private Methods
        private void OnDestroy()
        {
            m_RawElectrodes?.Dispose();
            foreach (var dllBrainTextureGenerator in DLLBrainTextureGenerators) dllBrainTextureGenerator?.Dispose();
            DLLMRIVolumeGenerator?.Dispose();
            CutTextures.Clean();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the column
        /// </summary>
        /// <param name="idColumn">ID of the column</param>
        /// <param name="sites">List of sites (DLL)</param>
        /// <param name="sitesPatientParent">List of the gameobjects for the sites corresponding to the patients</param>
        /// <param name="siteList">List of the sites gameobjects</param>
        public void Initialize(int idColumn, Data.Visualization.BaseColumn baseColumn, PatientElectrodesList sites, List<GameObject> sitesPatientParent, List<GameObject> siteList)
        {
            Layer = "Column" + idColumn;
            ColumnData = baseColumn;
            CutTextures = new CutTexturesUtility();
            DLLMRIVolumeGenerator = new MRIVolumeGenerator();
            m_RawElectrodes = new RawSiteList();
            m_SelectRing.SetLayer(Layer);
            UpdateSites(sites, sitesPatientParent, siteList);
            AddView();
            IsRenderingUpToDate = false;
        }
        /// <summary>
        /// Update the implantation for this column
        /// </summary>
        /// <param name="sites">List of the sites (DLL)</param>
        /// <param name="sitesPatientParent">List of the gameobjects for the sites corresponding to the patients</param>
        /// <param name="siteList">List of the sites gameobjects</param>
        public virtual void UpdateSites(PatientElectrodesList sites, List<GameObject> sitesPatientParent, List<GameObject> siteList)
        {
            GameObject patientPlotsParent = transform.Find("Sites").gameObject;
            foreach (Transform patientSite in patientPlotsParent.transform)
            {
                Destroy(patientSite.gameObject);
            }

            sites.ExtractRawSiteList(m_RawElectrodes);

            SitesGameObjects = new List<List<List<GameObject>>>(sitesPatientParent.Count);
            Sites = new List<Site>(sites.TotalSitesNumber);
            for (int ii = 0; ii < sitesPatientParent.Count; ++ii)
            {
                // instantiate patient plots
                GameObject patientPlots = Instantiate(sitesPatientParent[ii]);
                patientPlots.transform.SetParent(patientPlotsParent.transform);
                patientPlots.transform.localPosition = Vector3.zero;
                patientPlots.name = sitesPatientParent[ii].name;

                SitesGameObjects.Add(new List<List<GameObject>>(patientPlots.transform.childCount));
                for (int jj = 0; jj < patientPlots.transform.childCount; ++jj)
                {
                    int nbPlots = patientPlots.transform.GetChild(jj).childCount;

                    SitesGameObjects[ii].Add(new List<GameObject>(nbPlots));
                    for (int kk = 0; kk < nbPlots; ++kk)
                    {
                        SitesGameObjects[ii][jj].Add(patientPlots.transform.GetChild(jj).GetChild(kk).gameObject);
                        SitesGameObjects[ii][jj][kk].layer = LayerMask.NameToLayer(Layer);
                        Sites.Add(patientPlots.transform.GetChild(jj).GetChild(kk).gameObject.GetComponent<Site>());

                        int id = Sites.Count - 1;
                        Site baseSite = siteList[id].GetComponent<Site>();
                        Site site = Sites[id];
                        site.Information = baseSite.Information;
                        // State
                        if (!SiteStateBySiteID.ContainsKey(baseSite.Information.FullID))
                        {
                            SiteStateBySiteID.Add(baseSite.Information.FullID, new SiteState(baseSite.State));
                        }
                        site.State = SiteStateBySiteID[baseSite.Information.FullID];
                        site.State.OnChangeState.AddListener(() => OnChangeSiteState.Invoke(site));
                        // Configuration
                        Data.Visualization.SiteConfiguration siteConfiguration;
                        if (ColumnData.BaseConfiguration.ConfigurationBySite.TryGetValue(site.Information.FullCorrectedID, out siteConfiguration))
                        {
                            site.Configuration = siteConfiguration;
                        }
                        else
                        {
                            ColumnData.BaseConfiguration.ConfigurationBySite.Add(site.Information.FullCorrectedID, site.Configuration); // TODO creation des sites configurations au chargement de la VISU d'apres le PTS.
                        }
                        site.IsActive = true;
                        site.OnSelectSite.AddListener((selected) =>
                        {
                            if (selected)
                            {
                                UnselectSite();
                                SelectedSite = site;
                            }
                            else
                            {
                                SelectedSite = null;
                            }
                            OnSelectSite.Invoke(SelectedSite);
                        });
                    }
                }
            }
        }
        /// <summary>
        /// Initialize the column specific meshes (differents UVs for the iEEG signal)
        /// </summary>
        /// <param name="brainMeshesParent">Parent of the main meshes</param>
        /// <param name="useSimplifiedMeshes">Are we using simplified meshes ?</param>
        public void InitializeColumnMeshes(GameObject brainMeshesParent)
        {
            BrainSurfaceMeshes = new List<GameObject>();
            foreach (Transform meshPart in brainMeshesParent.transform)
            {
                if (meshPart.GetComponent<MeshCollider>() == null) // if the gameobject does not have mesh collider
                {
                    GameObject brainPart = Instantiate(m_BrainPrefab, m_BrainSurfaceMeshesParent);
                    brainPart.GetComponent<Renderer>().sharedMaterial = meshPart.GetComponent<Renderer>().sharedMaterial;
                    brainPart.name = meshPart.name;
                    brainPart.transform.localPosition = Vector3.zero;
                    brainPart.layer = LayerMask.NameToLayer(Layer);
                    brainPart.GetComponent<MeshFilter>().mesh = Instantiate(meshPart.GetComponent<MeshFilter>().mesh);
                    brainPart.SetActive(true);
                    BrainSurfaceMeshes.Add(brainPart);
                }
            }
        }
        /// <summary>
        /// Update the meshes of this column
        /// </summary>
        /// <param name="brainMeshes">Parent of the main meshes</param>
        /// <param name="useSimplifiedMeshes">Are we using simplified meshes ?</param>
        public void UpdateColumnMeshes(List<GameObject> brainMeshes)
        {
            for (int i = 0; i < brainMeshes.Count; i++)
            {
                if (brainMeshes[i].GetComponent<MeshCollider>() == null) // if the gameobject does not have mesh collider
                {
                    DestroyImmediate(BrainSurfaceMeshes[i].GetComponent<MeshFilter>().sharedMesh);
                    BrainSurfaceMeshes[i].GetComponent<MeshFilter>().sharedMesh = Instantiate(brainMeshes[i].GetComponent<MeshFilter>().mesh);
                }
            }
        }
        /// <summary>
        /// Reset the splits number
        /// </summary>
        /// <param name="nbSplits">Number of splits</param>
        public void ResetSplitsNumber(int nbSplits)
        {
            DLLBrainTextureGenerators = new List<MRIBrainGenerator>(nbSplits);
            for (int ii = 0; ii < nbSplits; ++ii)
                DLLBrainTextureGenerators.Add(new MRIBrainGenerator());
        }
        /// <summary>
        /// Update the number of cuts
        /// </summary>
        /// <param name="nbCuts">Number of cuts</param>
        public void UpdateCutsPlanesNumber(int nbCuts)
        {
            CutTextures.Resize(nbCuts);
            CutTextures.SetMRIVolumeGenerator(DLLMRIVolumeGenerator);
            IsRenderingUpToDate = false;
        }
        /// <summary>
        /// Update the shape and color of the sites of this column
        /// </summary>
        /// <param name="data">Information about the scene</param>
        /// <param name="latenciesFile">CCEP files</param>
        public virtual void UpdateSitesRendering(SceneStatesInfo data, Latencies latenciesFile)
        {
            if (data.DisplayCCEPMode) // CCEP
            {
                for (int i = 0; i < Sites.Count; ++i)
                {
                    Site site = Sites[i];
                    bool activity = site.IsActive;
                    SiteType siteType;
                    float alpha = -1.0f;
                    if (site.State.IsBlackListed)
                    {
                        site.transform.localScale = Vector3.one;
                        siteType = SiteType.BlackListed;
                        if (data.HideBlacklistedSites)
                        {
                            if (activity) site.IsActive = false;
                            continue;
                        }
                    }
                    else if (latenciesFile != null)
                    {
                        if (SelectedSiteID == -1)
                        {
                            site.transform.localScale = Vector3.one;
                            siteType = latenciesFile.IsSiteASource(i) ? SiteType.Source : SiteType.NotASource;
                        }
                        else
                        {
                            if (i == SelectedSiteID)
                            {
                                site.transform.localScale = Vector3.one;
                                siteType = SiteType.Source;
                            }
                            else if (latenciesFile.IsSiteResponsiveForSource(i, SelectedSiteID))
                            {
                                siteType = latenciesFile.PositiveHeight[SelectedSiteID][i] ? SiteType.NonePos : SiteType.NoneNeg;
                                alpha = site.State.IsHighlighted ? 1.0f : latenciesFile.Transparencies[SelectedSiteID][i] - 0.25f;
                                site.transform.localScale = Vector3.one * latenciesFile.Sizes[SelectedSiteID][i];
                            }
                            else
                            {
                                site.transform.localScale = Vector3.one;
                                siteType = SiteType.NoLatencyData;
                            }
                        }
                    }
                    else
                    {
                        site.transform.localScale = Vector3.one;
                        siteType = SiteType.Normal;
                    }
                    if (!activity) site.IsActive = true;
                    Material siteMaterial = SharedMaterials.SiteSharedMaterial(site.State.IsHighlighted, siteType, site.State.Color);
                    if (alpha > 0.0f)
                    {
                        Color materialColor = siteMaterial.color;
                        materialColor.a = alpha;
                        siteMaterial.color = materialColor;
                    }
                    site.GetComponent<MeshRenderer>().sharedMaterial = siteMaterial;
                }
            }
            else // iEEG
            {
                for (int i = 0; i < Sites.Count; ++i)
                {
                    Site site = Sites[i];
                    bool activity = site.IsActive;
                    SiteType siteType;
                    if (site.State.IsMasked || (site.State.IsOutOfROI && !data.ShowAllSites))
                    {
                        if (activity) site.IsActive = false;
                        continue;
                    }
                    else if (site.State.IsBlackListed)
                    {
                        site.transform.localScale = Vector3.one;
                        siteType = SiteType.BlackListed;
                        if (data.HideBlacklistedSites)
                        {
                            if (activity) site.IsActive = false;
                            continue;
                        }
                    }
                    else
                    {
                        site.transform.localScale = Vector3.one;
                        siteType = SiteType.Normal;
                    }
                    if (!activity) site.IsActive = true;
                    site.GetComponent<MeshRenderer>().sharedMaterial = SharedMaterials.SiteSharedMaterial(site.State.IsHighlighted, siteType, site.State.Color);
                }
            }
        }
        /// <summary>
        /// Save the states of the sites of this column to a file
        /// </summary>
        /// <param name="path">Path where to save the data</param>
        public void SaveSiteStates(string path)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.WriteLine("ID,Blacklisted,Highlighted,Color");
                    foreach (var site in SiteStateBySiteID)
                    {
                        sw.WriteLine("{0},{1},{2},{3}", site.Key, site.Value.IsBlackListed, site.Value.IsHighlighted, site.Value.Color.ToHexString());
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.Error, "Can not save site states", "Please verify your rights.");
            }
        }
        /// <summary>
        /// Load the states of the sites to this column from a file
        /// </summary>
        /// <param name="path">Path of the file to load data from</param>
        public void LoadSiteStates(string path)
        {
            try
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    // Find which column of the csv corresponds to which argument
                    string firstLine = sr.ReadLine();
                    string[] firstLineSplits = firstLine.Split(',');
                    int[] indices = new int[4];
                    for (int i = 0; i < indices.Length; ++i)
                    {
                        string split = firstLineSplits[i];
                        indices[i] = split == "ID" ? 0 : split == "Blacklisted" ? 1 : split == "Highlighted" ? 2 : split == "Color" ? 3 : i;
                    }
                    // Fill states
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] args = line.Split(',');
                        SiteState state = new SiteState();
                        if (bool.TryParse(args[indices[1]], out bool stateValue))
                        {
                            state.IsBlackListed = stateValue;
                        }
                        else
                        {
                            state.IsBlackListed = false;
                        }
                        if (bool.TryParse(args[indices[2]], out stateValue))
                        {
                            state.IsHighlighted = stateValue;
                        }
                        else
                        {
                            state.IsHighlighted = false;
                        }
                        if (ColorUtility.TryParseHtmlString(args[indices[3]], out Color color))
                        {
                            state.Color = color;
                        }
                        else
                        {
                            state.Color = SiteState.DefaultColor;
                        }
                        SiteState existingState;
                        if (SiteStateBySiteID.TryGetValue(args[indices[0]], out existingState))
                        {
                            existingState.ApplyState(state);
                        }
                        else
                        {
                            SiteStateBySiteID.Add(args[indices[0]], state);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.Error, "Can not load site states", "Please verify your files and try again.");
            }
        }
        /// <summary>
        /// Add a view to this column
        /// </summary>
        public void AddView()
        {
            View3D view = Instantiate(m_ViewPrefab, transform.Find("Views")).GetComponent<View3D>();
            view.gameObject.name = "View " + m_Views.Count;
            view.LineID = m_Views.Count;
            view.Layer = Layer;
            view.OnChangeSelectedState.AddListener((selected) =>
            {
                if (selected)
                {
                    foreach (View3D v in m_Views)
                    {
                        if (v != view)
                        {
                            v.IsSelected = false;
                        }
                    }
                }
                IsSelected = selected;
            });
            view.OnMoveView.AddListener(() =>
            {
                OnMoveView.Invoke(view);
            });
            m_Views.Add(view);
        }
        /// <summary>
        /// Remove a view from this column
        /// </summary>
        /// <param name="lineID">ID of the view line to be removed</param>
        public void RemoveView(int lineID)
        {
            Destroy(m_Views[lineID].gameObject);
            m_Views.RemoveAt(lineID);
        }
        /// <summary>
        /// Add a ROI to this column
        /// </summary>
        /// <param name="name">Name of the new ROI</param>
        /// <returns>Newly created ROI</returns>
        public ROI AddROI(string name = ROI.DEFAULT_ROI_NAME)
        {
            GameObject roiGameObject = Instantiate(m_ROIPrefab, m_ROIParent);
            ROI roi = roiGameObject.GetComponent<ROI>();
            roi.Name = name;
            roi.OnChangeNumberOfVolumeInROI.AddListener(() =>
            {
                UpdateROIMask();
            });
            roi.OnChangeROISphereParameters.AddListener(() =>
            {
                UpdateROIMask();
            });
            m_ROIs.Add(roi);
            UpdateROIMask();
            SelectedROI = m_ROIs.Last();

            return roi;
        }
        /// <summary>
        /// Create a new ROI using the parameters of another ROI
        /// </summary>
        /// <param name="roi">ROI to copy parameters from</param>
        public void CopyROI(ROI roi)
        {
            ROI newROI = AddROI();
            newROI.Name = roi.Name;
            foreach (Sphere bubble in roi.Spheres)
            {
                newROI.AddBubble(Layer, "Bubble", bubble.Position, bubble.Radius);
            }
        }
        /// <summary>
        /// Remove the currently selected ROI
        /// </summary>
        public void RemoveSelectedROI()
        {
            Destroy(m_SelectedROI.gameObject);
            m_ROIs.Remove(m_SelectedROI);
            UpdateROIMask();

            if (m_ROIs.Count > 0)
            {
                SelectedROI = m_ROIs.Last();
            }
            else
            {
                SelectedROI = null;
            }
        }
        /// <summary>
        /// Move the selected sphere by delta
        /// </summary>
        /// <param name="camera">Reference camera</param>
        /// <param name="delta">Distance and direction of the movement</param>
        public void MoveSelectedROISphere(Camera camera, Vector3 delta)
        {
            if (m_SelectedROI)
            {
                if (m_SelectedROI.SelectedSphereID != -1)
                {
                    Vector3 position = camera.WorldToScreenPoint(m_SelectedROI.SelectedSphere.transform.position);
                    position += delta;
                    position = camera.ScreenToWorldPoint(position);
                    position -= m_SelectedROI.SelectedSphere.transform.position;
                    m_SelectedROI.MoveSelectedSphere(position);
                }
            }
        }
        /// <summary>
        /// Update the ROI mask for this column
        /// </summary>
        public void UpdateROIMask()
        {
            if (SelectedROI == null)
            {
                for (int ii = 0; ii < Sites.Count; ++ii)
                    Sites[ii].State.IsOutOfROI = true;
            }
            else
            {
                bool[] maskROI = new bool[Sites.Count];

                // update mask ROI
                for (int ii = 0; ii < maskROI.Length; ++ii)
                    maskROI[ii] = Sites[ii].State.IsOutOfROI;

                SelectedROI.UpdateMask(RawElectrodes, maskROI);
                for (int ii = 0; ii < Sites.Count; ++ii)
                    Sites[ii].State.IsOutOfROI = maskROI[ii];
            }
            OnUpdateROIMask.Invoke();
        }
        /// <summary>
        /// Unselect the selected site
        /// </summary>
        public void UnselectSite()
        {
            if (SelectedSite)
            {
                SelectedSite.IsSelected = false;
            }
        }
        /// <summary>
        /// Select the first unmasked site
        /// </summary>
        public void SelectFirstSite(string siteName = "")
        {
            Site site;
            if (!string.IsNullOrEmpty(siteName))
            {
                site = Sites.FirstOrDefault(s => s.Information.ChannelName == siteName);
            }
            else
            {
                site = Sites.FirstOrDefault(s => !s.State.IsMasked);
            }
            if (site != null)
            {
                site.IsSelected = true;
            }
        }
        #endregion
    }
}
