using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;
using System.IO;
using HBP.Module3D.DLL;
using HBP.Data.Enums;
using Tools.Unity;

namespace HBP.Module3D
{
    /// <summary>
    /// Class containing information about the 3D column (specific meshes, sites, ROIs of the column)
    /// </summary>
    public class Column3D : MonoBehaviour, IConfigurable
    {
        #region Properties
        /// <summary>
        /// Data of this column (contains information about what to display)
        /// </summary>
        public Data.Visualization.Column ColumnData { get; protected set; }
        /// <summary>
        /// Name of the column (same name as in the column data)
        /// </summary>
        public string Name
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
                if (m_IsSelected)
                {
                    OnSelect.Invoke();
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
        /// Surface meshes displayed in this column
        /// </summary>
        public List<GameObject> BrainSurfaceMeshes { get; protected set; } = new List<GameObject>();

        /// <summary>
        /// Views of this column
        /// </summary>
        public List<View3D> Views { get; protected set; } = new List<View3D>();
        /// <summary>
        /// Currently selected view
        /// </summary>
        public View3D SelectedView
        {
            get
            {
                return Views.FirstOrDefault(v => v.IsSelected);
            }
        }

        /// <summary>
        /// Currently selected site
        /// </summary>
        public Site SelectedSite { get; private set; }
        /// <summary>
        /// Currently selected site ID
        /// </summary>
        public int SelectedSiteID { get { return SelectedSite != null ? SelectedSite.Information.Index : -1; } }

        /// <summary>
        /// Raw site list (used for DLL operations)
        /// </summary>
        public RawSiteList RawElectrodes { get; protected set; } = new RawSiteList();
        /// <summary>
        /// Sites of this column
        /// </summary>
        public List<Site> Sites { get; protected set; }
        /// <summary>
        /// Site state by site ID (used when changing the implantation to keep the state of sites common to both implantations)
        /// </summary>
        public Dictionary<string, SiteState> SiteStateBySiteID = new Dictionary<string, SiteState>();

        [SerializeField] protected SiteRing m_SelectRing;
        /// <summary>
        /// Selection ring feedback
        /// </summary>
        public SiteRing SelectRing { get { return m_SelectRing; } }

        /// <summary>
        /// List of the ROIs of this column
        /// </summary>
        public List<ROI> ROIs { get; protected set; } = new List<ROI>();

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
                return ROIs.FindIndex((roi) => roi == SelectedROI);
            }
            set
            {
                SelectedROI = value == -1 ? null : ROIs[value];
            }
        }

        /// <summary>
        /// Texture generator for the brain surface
        /// </summary>
        public List<MRIBrainGenerator> DLLBrainTextureGenerators { get; protected set; } = new List<MRIBrainGenerator>();
        /// <summary>
        /// Volume generator for cut textures
        /// </summary>
        public MRIVolumeGenerator DLLMRIVolumeGenerator { get; protected set; } = new MRIVolumeGenerator();
        /// <summary>
        /// Cut Textures Utility
        /// </summary>
        public CutTexturesUtility CutTextures { get; protected set; } = new CutTexturesUtility();

        /// <summary>
        /// Parent of the meshes displayed in this column
        /// </summary>
        [SerializeField] private Transform m_BrainSurfaceMeshesParent;
        /// <summary>
        /// Parent of the sites displayed in this column
        /// </summary>
        [SerializeField] private Transform m_SitesMeshesParent;
        /// <summary>
        /// Parent for ROI GameObjects
        /// </summary>
        [SerializeField] protected Transform m_ROIParent;
        /// <summary>
        /// View prefab
        /// </summary>
        [SerializeField] protected GameObject m_ViewPrefab;
        /// <summary>
        /// Prefab for the ROI
        /// </summary>
        [SerializeField] private GameObject m_ROIPrefab;
        #endregion

        #region Events
        /// <summary>
        /// Event called when this column is selected
        /// </summary>
        [HideInInspector] public UnityEvent OnSelect = new UnityEvent();
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
        #endregion

        #region Private Methods
        private void OnDestroy()
        {
            RawElectrodes?.Dispose();
            foreach (var dllBrainTextureGenerator in DLLBrainTextureGenerators) dllBrainTextureGenerator?.Dispose();
            DLLMRIVolumeGenerator?.Dispose();
            CutTextures.Clean();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the column with all the required parameters
        /// </summary>
        /// <param name="idColumn">ID of the column</param>
        /// <param name="baseColumn">Data of the column</param>
        /// <param name="sites">List of the sites in the DLL</param>
        /// <param name="sceneSitePatientParent">List of the patient parent of the sites as instantiated in the scene</param>
        public void Initialize(int idColumn, Data.Visualization.Column baseColumn, PatientElectrodesList sites, List<GameObject> sceneSitePatientParent)
        {
            Layer = "Column" + idColumn;
            ColumnData = baseColumn;
            m_SelectRing.SetLayer(Layer);
            UpdateSites(sites, sceneSitePatientParent);
            AddView();
            IsRenderingUpToDate = false;
        }
        /// <summary>
        /// Update the sites of this column (when changing the implantation of the scene)
        /// </summary>
        /// <param name="sites">List of the sites in the DLL</param>
        /// <param name="sceneSitePatientParent">List of the patient parent of the sites as instantiated in the scene</param>
        public virtual void UpdateSites(PatientElectrodesList sites, List<GameObject> sceneSitePatientParent)
        {
            sites.ExtractRawSiteList(RawElectrodes);

            foreach (Transform patientSite in m_SitesMeshesParent)
            {
                Destroy(patientSite.gameObject);
            }
            Sites = new List<Site>(sites.NumberOfSites);
            
            for (int i = 0; i < sceneSitePatientParent.Count; ++i)
            {
                Transform sceneSitePatient = sceneSitePatientParent[i].transform;
                Transform sitePatient = Instantiate(sceneSitePatient, m_SitesMeshesParent);
                sitePatient.transform.localPosition = Vector3.zero;
                sitePatient.name = sceneSitePatient.name;

                for (int j = 0; j < sceneSitePatient.childCount; ++j)
                {
                    Transform sceneSiteElectrode = sceneSitePatient.GetChild(j);
                    Transform siteElectrode = sitePatient.GetChild(j);
                    for (int k = 0; k < sceneSiteElectrode.childCount; ++k)
                    {
                        GameObject sceneSiteGameObject = sceneSiteElectrode.GetChild(k).gameObject;
                        GameObject siteGameObject = siteElectrode.GetChild(k).gameObject;
                        siteGameObject.layer = LayerMask.NameToLayer(Layer);
                        Site site = siteGameObject.GetComponent<Site>();
                        Site baseSite = sceneSiteGameObject.GetComponent<Site>();
                        site.Information = baseSite.Information;
                        // State
                        if (!SiteStateBySiteID.TryGetValue(baseSite.Information.FullCorrectedID, out SiteState siteState))
                        {
                            siteState = new SiteState();
                            siteState.ApplyState(baseSite.State);
                            SiteStateBySiteID.Add(baseSite.Information.FullCorrectedID, siteState);
                        }
                        site.State = siteState;
                        site.State.OnChangeState.AddListener(() => OnChangeSiteState.Invoke(site));
                        // Configuration
                        if (ColumnData.BaseConfiguration.ConfigurationBySite.TryGetValue(site.Information.FullCorrectedID, out Data.Visualization.SiteConfiguration siteConfiguration))
                        {
                            site.Configuration = siteConfiguration;
                        }
                        else
                        {
                            ColumnData.BaseConfiguration.ConfigurationBySite.Add(site.Information.FullCorrectedID, site.Configuration);
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
                        Sites.Add(site);
                    }
                }
            }
        }
        /// <summary>
        /// Instantiate the brain meshes for this column (required because we need different UVs to display a different activity on each column)
        /// </summary>
        /// <param name="brainMeshesParent">Parent of the meshes in the scene</param>
        public void InitializeColumnMeshes(Transform brainMeshesParent)
        {
            BrainSurfaceMeshes = new List<GameObject>();
            foreach (Transform meshPart in brainMeshesParent.transform)
            {
                if (meshPart.GetComponent<MeshCollider>() == null) // if the gameobject does not have mesh collider
                {
                    GameObject brainPart = Instantiate(meshPart.gameObject, m_BrainSurfaceMeshesParent);
                    brainPart.layer = LayerMask.NameToLayer(Layer);
                    brainPart.GetComponent<MeshFilter>().mesh = Instantiate(meshPart.GetComponent<MeshFilter>().mesh);
                    brainPart.SetActive(true);
                    BrainSurfaceMeshes.Add(brainPart);
                }
            }
        }
        /// <summary>
        /// Update the meshes of this column (when updating the base meshes in the scene)
        /// </summary>
        /// <param name="brainMeshes">Meshes of the base scene</param>
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
        /// Reset the splits number (called when changing the splits number in the scene)
        /// </summary>
        /// <param name="nbSplits">Number of splits</param>
        public void ResetSplitsNumber(int nbSplits)
        {
            DLLBrainTextureGenerators = new List<MRIBrainGenerator>(nbSplits);
            for (int ii = 0; ii < nbSplits; ++ii)
                DLLBrainTextureGenerators.Add(new MRIBrainGenerator());
        }
        /// <summary>
        /// Update the number of cuts (called when changing the number of cuts in the scene)
        /// </summary>
        /// <param name="nbCuts">Number of cuts</param>
        public void UpdateCutsPlanesNumber(int nbCuts)
        {
            CutTextures.Resize(nbCuts);
            CutTextures.SetMRIVolumeGenerator(DLLMRIVolumeGenerator);
            IsRenderingUpToDate = false;
        }
        /// <summary>
        /// Update the visibility, the size and the color of the sites depending on their state
        /// </summary>
        /// <param name="showAllSites">Do we show sites that are not in a ROI ?</param>
        /// <param name="hideBlacklistedSites">Do we hide blacklisted sites ?</param>
        /// <param name="isGeneratorUpToDate">Is the activity generator up to date ?</param>
        public virtual void UpdateSitesRendering(bool showAllSites, bool hideBlacklistedSites, bool isGeneratorUpToDate)
        {
            for (int i = 0; i < Sites.Count; ++i)
            {
                Site site = Sites[i];
                bool activity = site.IsActive;
                SiteType siteType;
                if (site.State.IsMasked || (site.State.IsOutOfROI && !showAllSites) || !site.State.IsFiltered)
                {
                    if (activity) site.IsActive = false;
                    continue;
                }
                else if (site.State.IsBlackListed)
                {
                    site.transform.localScale = Vector3.one;
                    siteType = SiteType.BlackListed;
                    if (hideBlacklistedSites)
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
        /// <summary>
        /// Save the state of the sites of this column to a file
        /// </summary>
        /// <param name="path">Path where to save the data</param>
        public void SaveSiteStates(string path)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.WriteLine("ID,Blacklisted,Highlighted,Color,Labels");
                    foreach (var site in SiteStateBySiteID)
                    {
                        sw.WriteLine("{0},{1},{2},{3},{4}", site.Key, site.Value.IsBlackListed, site.Value.IsHighlighted, site.Value.Color.ToHexString(), string.Join(";", site.Value.Labels));
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Can not save site states", "Please verify your rights.");
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
                    int[] indices = new int[5];
                    for (int i = 0; i < indices.Length; ++i)
                    {
                        string split = firstLineSplits[i];
                        indices[i] = split == "ID" ? 0 : split == "Blacklisted" ? 1 : split == "Highlighted" ? 2 : split == "Color" ? 3 : split == "Labels" ? 4 : i;
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

                        string[] labels = args[indices[4]].Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
                        foreach (var label in labels)
                        {
                            state.AddLabel(label);
                        }

                        if (SiteStateBySiteID.TryGetValue(args[indices[0]], out SiteState existingState))
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
            view.gameObject.name = "View " + Views.Count;
            view.LineID = Views.Count;
            view.Layer = Layer;
            view.OnSelect.AddListener(() =>
            {
                foreach (View3D v in Views)
                {
                    if (v != view)
                    {
                        v.IsSelected = false;
                    }
                }
                IsSelected = true;
            });
            view.OnMoveView.AddListener(() =>
            {
                OnMoveView.Invoke(view);
            });
            Views.Add(view);
        }
        /// <summary>
        /// Remove a view from this column
        /// </summary>
        /// <param name="lineID">ID of the view line to be removed</param>
        public void RemoveView(int lineID)
        {
            Destroy(Views[lineID].gameObject);
            Views.RemoveAt(lineID);
        }
        /// <summary>
        /// Add a ROI to this column
        /// </summary>
        /// <param name="name">Name of the new ROI</param>
        /// <returns>Newly created ROI</returns>
        public ROI AddROI(string name = "ROI")
        {
            GameObject roiGameObject = Instantiate(m_ROIPrefab, m_ROIParent);
            ROI roi = roiGameObject.GetComponent<ROI>();
            roi.Name = name;
            roi.OnChangeNumberOfSpheres.AddListener(() =>
            {
                UpdateROIMask();
            });
            roi.OnChangeSphereParameters.AddListener(() =>
            {
                UpdateROIMask();
            });
            ROIs.Add(roi);
            UpdateROIMask();
            SelectedROI = ROIs.Last();

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
                newROI.AddSphere(Layer, "Bubble", bubble.Position, bubble.Radius);
            }
        }
        /// <summary>
        /// Remove the currently selected ROI
        /// </summary>
        public void RemoveSelectedROI()
        {
            Destroy(m_SelectedROI.gameObject);
            ROIs.Remove(m_SelectedROI);
            UpdateROIMask();

            if (ROIs.Count > 0)
            {
                SelectedROI = ROIs.Last();
            }
            else
            {
                SelectedROI = null;
            }
        }
        /// <summary>
        /// Move the selected sphere by a specific delta from a camera perspective
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
        /// Select the first unmasked site by name
        /// </summary>
        /// <param name="siteName">Name of the site to select</param>
        public void SelectFirstOrDefaultSiteByName(string siteName = "")
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
        /// <summary>
        /// Update the sites mask of the DLL using the state of each site
        /// </summary>
        public void UpdateDLLSitesMask()
        {
            bool isROI = ROIs.Count > 0;
            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                RawElectrodes.UpdateMask(ii, (Sites[ii].State.IsMasked || Sites[ii].State.IsBlackListed || (Sites[ii].State.IsOutOfROI && isROI) || !Sites[ii].State.IsFiltered));
            }
        }
        /// <summary>
        /// Performs a raycast on the column
        /// </summary>
        /// <param name="ray">Ray of the raycast</param>
        /// <param name="layerMask">Layer on which to perform the raycast</param>
        /// <param name="hit">Result of the raycast</param>
        /// <returns></returns>
        public RaycastHitResult Raycast(Ray ray, int layerMask, out RaycastHit hit)
        {
            layerMask |= 1 << LayerMask.NameToLayer(Layer);
            RaycastHitResult result = RaycastHitResult.None;
            if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity, layerMask))
            {
                if (hit.transform.parent.gameObject.name == "Cuts") result = RaycastHitResult.Cut;
                if (hit.transform.parent.gameObject.name == "Brains" || hit.transform.parent.gameObject.name == "Erased Brains") result = RaycastHitResult.Mesh;
                if (hit.collider.GetComponent<Site>() != null) result = RaycastHitResult.Site;
                if (hit.collider.GetComponent<Sphere>() != null) result = RaycastHitResult.ROI;
            }
            return result;
        }
        /// <summary>
        /// Load the column configuration from the column data
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another load method ?</param>
        public virtual void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration();
            foreach (Data.Visualization.RegionOfInterest roi in ColumnData.BaseConfiguration.RegionsOfInterest)
            {
                ROI newROI = AddROI(roi.Name);
                foreach (Data.Visualization.Sphere sphere in roi.Spheres)
                {
                    newROI.AddSphere(Layer, "Bubble", sphere.Position.ToVector3(), sphere.Radius);
                }
            }
            foreach (Site site in Sites)
            {
                site.LoadConfiguration(false);
            }

            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Save the configuration of this column to the data column
        /// </summary>
        public virtual void SaveConfiguration()
        {
            List<Data.Visualization.RegionOfInterest> rois = new List<Data.Visualization.RegionOfInterest>();
            foreach (ROI roi in ROIs)
            {
                rois.Add(new Data.Visualization.RegionOfInterest(roi));
            }
            ColumnData.BaseConfiguration.RegionsOfInterest = rois;
            foreach (Site site in Sites)
            {
                site.SaveConfiguration();
            }
        }
        /// <summary>
        /// Reset the configuration of this column
        /// </summary>
        public virtual void ResetConfiguration()
        {
            while (ROIs.Count > 0)
            {
                RemoveSelectedROI();
            }
            foreach (Site site in Sites)
            {
                site.ResetConfiguration();
            }

            ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        #endregion
    }
}
