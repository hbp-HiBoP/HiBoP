/**
 * \file    Column3DView.cs
 * \author  Lance Florian
 * \date    21/03/2016
 * \brief   Define Column3DView class
 */

using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;
using System.Linq;
using System;
using System.IO;
using HBP.Module3D.DLL;

namespace HBP.Module3D
{
    /// <summary>
    /// Column 3D view base class
    /// </summary>
    public class Column3D : MonoBehaviour
    {
        #region Properties
        public enum ColumnType
        {
            Base, IEEG
        }
        public virtual ColumnType Type
        {
            get
            {
                return ColumnType.Base;
            }
        }
        
        public string Label { get; set; }
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
                bool wasSelected = m_IsSelected;
                m_IsSelected = value;
                OnChangeSelectedState.Invoke(value);
                if (m_IsSelected && !wasSelected)
                {
                    ApplicationState.Module3D.OnSelectColumn.Invoke(this);
                }
            }
        }

        private bool m_IsMinimized;
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

        private bool m_IsRenderingUpToDate = false;
        /// <summary>
        /// Does the column rendering need to be updated ?
        /// </summary>
        public bool IsRenderingUpToDate
        {
            get
            {
                return m_IsRenderingUpToDate;
            }
            set
            {
                m_IsRenderingUpToDate = value;
            }
        }

        [SerializeField] private Transform m_BrainSurfaceMeshesParent;
        [SerializeField] private GameObject m_BrainPrefab;
        private List<GameObject> m_BrainSurfaceMeshes = new List<GameObject>();
        public List<GameObject> BrainSurfaceMeshes
        {
            get
            {
                return m_BrainSurfaceMeshes;
            }
        }

        public GameObject ViewPrefab;
        protected List<View3D> m_Views = new List<View3D>();
        public ReadOnlyCollection<View3D> Views
        {
            get
            {
                return new ReadOnlyCollection<View3D>(m_Views);
            }
        }

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

        public Site SelectedSite { get; protected set; }
        public int SelectedSiteID { get { return SelectedSite != null ? SelectedSite.Information.GlobalID : -1; } }
        public int SelectedPatientID { get { return SelectedSite != null ? SelectedSite.Information.PatientNumber : -1; } }

        protected RawSiteList m_RawElectrodes = null;  /**< raw format of the plots container dll */
        public RawSiteList RawElectrodes
        {
            get
            {
                return m_RawElectrodes;
            }
        }
        public List<List<List<GameObject>>> SitesGameObjects = null; /**< plots GO list with order : patient/electrode/plot */
        public List<Site> Sites = null; /**< plots list */
        public Dictionary<string, SiteState> SiteStateBySiteID = new Dictionary<string, SiteState>();

        // select plot
        [SerializeField] protected SiteRing m_SelectRing;
        public SiteRing SelectRing { get { return m_SelectRing; } }

        // ROI
        [SerializeField]
        protected Transform m_ROIParent;
        protected List<ROI> m_ROIs = new List<ROI>();
        public ReadOnlyCollection<ROI> ROIs
        {
            get
            {
                return new ReadOnlyCollection<ROI>(m_ROIs);
            }
        }
        protected ROI m_SelectedROI = null;
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
                OnSelectROI.Invoke();
            }
        }
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
        [SerializeField]
        private GameObject m_ROIPrefab;

        // generators
        public List<MRIBrainGenerator> DLLBrainTextureGenerators = new List<DLL.MRIBrainGenerator>();

        private CutTexturesUtility m_CutTextures = new CutTexturesUtility();
        /// <summary>
        /// Cut Textures Utility
        /// </summary>
        public CutTexturesUtility CutTextures
        {
            get
            {
                return m_CutTextures;
            }
        }

        // latencies
        public bool SourceDefined { get { return SelectedSiteID != -1; } }
        private int m_CurrentLatencyFile = -1;
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
        /// Event called when changing the number of ROIs of this column
        /// </summary>
        [HideInInspector] public UnityEvent OnChangeNumberOfROI = new UnityEvent();
        /// <summary>
        /// Event called when changing the number of volume in a ROI of this column
        /// </summary>
        [HideInInspector] public UnityEvent OnChangeNumberOfVolumeInROI = new UnityEvent();
        /// <summary>
        /// Event called when selecting a ROI
        /// </summary>
        [HideInInspector] public UnityEvent OnSelectROI = new UnityEvent();
        /// <summary>
        /// Event called when changing the radius of a volume in a ROI
        /// </summary>
        [HideInInspector] public UnityEvent OnChangeROIVolumeRadius = new UnityEvent();
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

        #region Public Methods
        /// <summary>
        /// Base init class of the column
        /// </summary>
        /// <param name="idColumn"></param>
        /// <param name="nbCuts"></param>
        /// <param name="sites"></param>
        /// <param name="plotsGO"></param>
        public void Initialize(int idColumn, PatientElectrodesList sites, List<GameObject> sitesPatientParent, List<GameObject> siteList)
        {
            Layer = "Column" + idColumn;
            m_SelectRing.SetLayer(Layer);
            UpdateSites(sites, sitesPatientParent, siteList);
            AddView();
            IsRenderingUpToDate = false;
        }
        public virtual void UpdateSites(PatientElectrodesList sites, List<GameObject> sitesPatientParent, List<GameObject> siteList)
        {
            GameObject patientPlotsParent = transform.Find("Sites").gameObject;
            foreach (Transform patientSite in patientPlotsParent.transform)
            {
                Destroy(patientSite.gameObject);
            }

            m_RawElectrodes = new RawSiteList();
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
                        if (!SiteStateBySiteID.ContainsKey(baseSite.Information.FullID))
                        {
                            SiteStateBySiteID.Add(baseSite.Information.FullID, new SiteState(baseSite.State));
                        }
                        site.State = SiteStateBySiteID[baseSite.Information.FullID];
                        site.State.OnChangeState.AddListener(() => OnChangeSiteState.Invoke(site));
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
        /// Set the meshes for this column
        /// </summary>
        /// <param name="brainMeshesParent"></param>
        public void InitializeColumnMeshes(GameObject brainMeshesParent, bool useSimplifiedMeshes)
        {
            m_BrainSurfaceMeshes = new List<GameObject>();
            foreach (Transform meshPart in brainMeshesParent.transform)
            {
                if (meshPart.GetComponent<MeshCollider>() == null || !useSimplifiedMeshes) // if the gameobject does not have mesh collider
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
        /// <param name="brainMeshes"></param>
        public void UpdateColumnMeshes(List<GameObject> brainMeshes, bool useSimplifiedMeshes)
        {
            for (int i = 0; i < brainMeshes.Count; i++)
            {
                if (brainMeshes[i].GetComponent<MeshCollider>() == null || !useSimplifiedMeshes) // if the gameobject does not have mesh collider
                {
                    DestroyImmediate(m_BrainSurfaceMeshes[i].GetComponent<MeshFilter>().sharedMesh);
                    m_BrainSurfaceMeshes[i].GetComponent<MeshFilter>().sharedMesh = Instantiate(brainMeshes[i].GetComponent<MeshFilter>().mesh);
                }
            }
        }
        public void ChangeMeshesLayer(int layer)
        {
            foreach (var mesh in m_BrainSurfaceMeshes)
            {
                mesh.layer = layer;
            }
        }
        public void ResetSplitsNumber(int nbSplits)
        {
            // generators dll
            //      brain
            DLLBrainTextureGenerators = new List<DLL.MRIBrainGenerator>(nbSplits);
            for (int ii = 0; ii < nbSplits; ++ii)
                DLLBrainTextureGenerators.Add(new DLL.MRIBrainGenerator());
        }
        public void UpdateCutsPlanesNumber(int nbCuts)
        {
            CutTextures.Resize(nbCuts);
            IsRenderingUpToDate = false;
        }
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
                    else if (site.State.IsExcluded)
                    {
                        site.transform.localScale = Vector3.one;
                        siteType = SiteType.Excluded;
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
                        siteType = site.State.IsMarked ? SiteType.Marked : SiteType.Normal;
                    }
                    if (!activity) site.IsActive = true;
                    Material siteMaterial = SharedMaterials.SiteSharedMaterial(site.State.IsHighlighted, siteType);
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
                    else if (site.State.IsExcluded)
                    {
                        site.transform.localScale = Vector3.one;
                        siteType = SiteType.Excluded;
                    }
                    else if (site.State.IsSuspicious)
                    {
                        site.transform.localScale = Vector3.one;
                        siteType = SiteType.Suspicious;
                    }
                    else
                    {
                        site.transform.localScale = Vector3.one;
                        siteType = site.State.IsMarked ? SiteType.Marked : SiteType.Normal;
                    }
                    if (!activity) site.IsActive = true;
                    site.GetComponent<MeshRenderer>().sharedMaterial = SharedMaterials.SiteSharedMaterial(site.State.IsHighlighted, siteType);
                }
            }

            // Selected site
            if (SelectedSiteID == -1)
            {
                m_SelectRing.SetSelectedSite(null, Vector3.zero);
            }
            else
            {
                Site selectedSite = SelectedSite;
                m_SelectRing.SetSelectedSite(selectedSite, selectedSite.transform.localScale);
            }
        }
        public void SaveSiteStates(string path)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.WriteLine("ID,Excluded,Blacklisted,Highlighted,Marked,Suspicious");
                    foreach (var site in SiteStateBySiteID)
                    {
                        sw.WriteLine("{0},{1},{2},{3},{4},{5}", site.Key, site.Value.IsExcluded, site.Value.IsBlackListed, site.Value.IsHighlighted, site.Value.IsMarked, site.Value.IsSuspicious);
                    }
                }
            }
            catch
            {
                ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.Error, "Can not save site states", "Please verify your rights.");
            }
        }
        public void LoadSiteStates(string path)
        {
            try
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    // Find which column of the csv corresponds to which argument
                    string firstLine = sr.ReadLine();
                    string[] firstLineSplits = firstLine.Split(',');
                    int[] indices = new int[6];
                    for (int i = 0; i < 6; ++i)
                    {
                        string split = firstLineSplits[i];
                        indices[i] = split == "ID" ? 0 : split == "Excluded" ? 1 : split == "Blacklisted" ? 2 : split == "Highlighted" ? 3 : split == "Marked" ? 4 : split == "Suspicious" ? 5 : i;
                    }
                    // Fill states
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] args = line.Split(',');
                        SiteState state = new SiteState();
                        bool stateValue;
                        if (bool.TryParse(args[indices[1]], out stateValue))
                        {
                            state.IsExcluded = stateValue;
                        }
                        else
                        {
                            state.IsExcluded = false;
                        }
                        if (bool.TryParse(args[indices[2]], out stateValue))
                        {
                            state.IsBlackListed = stateValue;
                        }
                        else
                        {
                            state.IsBlackListed = false;
                        }
                        if (bool.TryParse(args[indices[3]], out stateValue))
                        {
                            state.IsHighlighted = stateValue;
                        }
                        else
                        {
                            state.IsHighlighted = false;
                        }
                        if (bool.TryParse(args[indices[4]], out stateValue))
                        {
                            state.IsMarked = stateValue;
                        }
                        else
                        {
                            state.IsMarked = false;
                        }
                        if (bool.TryParse(args[indices[5]], out stateValue))
                        {
                            state.IsSuspicious = stateValue;
                        }
                        else
                        {
                            state.IsSuspicious = false;
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
            catch
            {
                ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.Error, "Can not load site states", "Please verify your files and try again.");
            }
        }
        public void AddView()
        {
            View3D view = Instantiate(ViewPrefab, transform.Find("Views")).GetComponent<View3D>();
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
        public void RemoveView(int lineID)
        {
            Destroy(m_Views[lineID].gameObject);
            m_Views.RemoveAt(lineID);
        }
        public ROI AddROI(string name = ROI.DEFAULT_ROI_NAME)
        {
            GameObject roiGameObject = Instantiate(m_ROIPrefab, m_ROIParent);
            ROI roi = roiGameObject.GetComponent<ROI>();
            roi.Name = name;
            roi.OnChangeNumberOfVolumeInROI.AddListener(() =>
            {
                OnChangeNumberOfVolumeInROI.Invoke();
            });
            roi.OnChangeROISphereParameters.AddListener(() =>
            {
                OnChangeROIVolumeRadius.Invoke();
            });
            m_ROIs.Add(roi);
            OnChangeNumberOfROI.Invoke();
            SelectedROI = m_ROIs.Last();

            return roi;
        }
        public void CopyROI(ROI roi)
        {
            ROI newROI = AddROI();
            newROI.Name = roi.Name;
            foreach (Sphere bubble in roi.Spheres)
            {
                newROI.AddBubble(Layer, "Bubble", bubble.Position, bubble.Radius);
            }
        }
        public void RemoveSelectedROI()
        {
            Destroy(m_SelectedROI.gameObject);
            m_ROIs.Remove(m_SelectedROI);
            OnChangeNumberOfROI.Invoke();

            if (m_ROIs.Count > 0)
            {
                SelectedROI = m_ROIs.Last();
            }
            else
            {
                SelectedROI = null;
            }
        }
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
        public void UnselectSite()
        {
            if (SelectedSite)
            {
                SelectedSite.IsSelected = false;
            }
        }
        #endregion
    }
}
