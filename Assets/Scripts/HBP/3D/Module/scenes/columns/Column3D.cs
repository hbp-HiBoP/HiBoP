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

namespace HBP.Module3D
{
    /// <summary>
    /// Column 3D view base class
    /// </summary>
    public abstract class Column3D : MonoBehaviour
    {
        #region Properties
        public enum ColumnType
        {
            FMRI, IEEG
        }
        public abstract ColumnType Type { get; }
        public int ID { get; set; }

        [SerializeField, Candlelight.PropertyBackingField]
        protected string m_Label;
        public string Label
        {
            get { return m_Label; }
            set { m_Label = value; }
        }

        [SerializeField, Candlelight.PropertyBackingField]
        protected string m_Layer;
        public string Layer
        {
            get { return m_Layer; }
            set { m_Layer = value; }
        }

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
                    OnSelectColumn.Invoke(this);
                }
            }
        }

        /// <summary>
        /// Does the column rendering need to be updated ?
        /// </summary>
        public bool IsRenderingUpToDate { get; set; }

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

        protected int m_SelectedSiteID = -1;
        public int SelectedSiteID
        {
            get { return m_SelectedSiteID; }
            set { m_SelectedSiteID = value; OnChangeSelectedSite.Invoke(SelectedSite); }
        }
        public Site SelectedSite
        {
            get { return m_SelectedSiteID >= 0 ? Sites[m_SelectedSiteID] : null; }
            set { m_SelectedSiteID = Sites.FindIndex((site) => site == value); OnChangeSelectedSite.Invoke(value); }
        }
        public int SelectedPatientID { get; set; }
        public GenericEvent<Site> OnChangeSelectedSite = new GenericEvent<Site>();

        protected DLL.RawSiteList m_RawElectrodes = null;  /**< raw format of the plots container dll */
        public DLL.RawSiteList RawElectrodes
        {
            get
            {
                return m_RawElectrodes;
            }
        }
        public List<List<List<GameObject>>> SitesGameObjects = null; /**< plots GO list with order : patient/electrode/plot */
        public List<Site> Sites = null; /**< plots list */

        // select plot
        protected SiteRing m_SelectRing = null;
        public SiteRing SelectRing { get { return m_SelectRing; } }

        // ROI
        protected ROI m_SelectedROI = null;   /**< selected ROI of the column */
        public ROI SelectedROI { get { return m_SelectedROI;} }

        // generators
        public List<DLL.MRITextureCutGenerator> DLLMRITextureCutGenerators = null;
        public List<DLL.MRIBrainGenerator> DLLBrainTextureGenerators = new List<DLL.MRIBrainGenerator>();

        //public DLL.Texture DLLBrainColorScheme = null;          /**< brain colorscheme dll texture */
        public DLL.Texture DLLCutColorScheme = null;            /**< cut colorscheme dll texture */
        public DLL.Texture DLLCutFMRIColorScheme = null;        /**< cut colorscheme dll texture */
        public List<DLL.Texture> DLLBrainCutTextures = null;    /**< list of cut DLL textures */
        public List<DLL.Texture> DLLGUIBrainCutTextures = null; /**< list of GUI DLL cut textures| */
        //  texture 2D
        //public DLL.Texture DLLBrainMainColor = null;            /**< main color dll texture of the brain mesh */
        public Texture2D BrainColorSchemeTexture = null;        /**< brain colorscheme unity 2D texture  */
        public List<Texture2D> BrainCutTextures = null;         /**< list of cut textures */
        public List<Texture2D> GUIBrainCutTextures = null;      /**< list of GUI cut textures */
        
        /// <summary>
        /// Event called when this column is selected
        /// </summary>
        public GenericEvent<Column3D> OnSelectColumn = new GenericEvent<Column3D>();
        /// <summary>
        /// Event called when a view is moved
        /// </summary>
        public GenericEvent<View3D> OnMoveView = new GenericEvent<View3D>();
        #endregion

        #region Public Methods
        /// <summary>
        /// Base init class of the column
        /// </summary>
        /// <param name="idColumn"></param>
        /// <param name="nbCuts"></param>
        /// <param name="sites"></param>
        /// <param name="plotsGO"></param>
        public virtual void Initialize(int idColumn, int nbCuts, DLL.PatientElectrodesList sites, List<GameObject> sitesPatientParent, List<GameObject> siteList)
        {
            // scene
            Layer = "Column" + idColumn;

            // select ring
            m_SelectRing = gameObject.GetComponentInChildren<SiteRing>();
            m_SelectRing.SetLayer(Layer);

            // plots
            m_RawElectrodes = new DLL.RawSiteList();
            sites.ExtractRawSiteList(m_RawElectrodes);

            GameObject patientPlotsParent = transform.Find("Sites").gameObject;

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
                        Sites[id].Information = new SiteInformation(siteList[id].GetComponent<Site>().Information);
                        Sites[id].IsActive = true;
                    }
                }
            }


            // generators dll
            DLLMRITextureCutGenerators = new List<DLL.MRITextureCutGenerator>(nbCuts);
            for (int ii = 0; ii < nbCuts; ++ii)
            {                
                DLLMRITextureCutGenerators.Add(new DLL.MRITextureCutGenerator());
            }

            // DLL textures
            DLLCutColorScheme = new DLL.Texture();
            DLLCutFMRIColorScheme = new DLL.Texture();
            DLLGUIBrainCutTextures = new List<DLL.Texture>(nbCuts);
            DLLBrainCutTextures = new List<DLL.Texture>(nbCuts);
            for (int ii = 0; ii < nbCuts; ++ii)
            {
                DLLGUIBrainCutTextures.Add(new DLL.Texture());
                DLLBrainCutTextures.Add(new DLL.Texture());
            }

            // textures 2D
            BrainColorSchemeTexture = Texture2Dutility.GenerateColorScheme();
            BrainCutTextures = new List<Texture2D>(nbCuts);
            GUIBrainCutTextures = new List<Texture2D>(nbCuts);
            for (int ii = 0; ii < nbCuts; ++ii)
            {
                BrainCutTextures.Add(Texture2Dutility.GenerateCut());
                GUIBrainCutTextures.Add(Texture2Dutility.GenerateGUI());
            }

            // view
            AddView();

            // update rendering
            IsRenderingUpToDate = false;
        }
        /// <summary>
        ///  Clean all allocated data
        /// </summary>
        public virtual void Clear()
        {
            DLLCutColorScheme.Dispose();
            DLLCutFMRIColorScheme.Dispose();
            Destroy(BrainColorSchemeTexture);

            // DLL
            // generators
            for (int ii = 0; ii < DLLBrainTextureGenerators.Count; ++ii)
            {
                DLLBrainTextureGenerators[ii].Dispose();
            }
            for (int ii = 0; ii < DLLMRITextureCutGenerators.Count; ++ii)
            {
                DLLMRITextureCutGenerators[ii].Dispose();
            }

            // textures 2D
            for (int ii = 0; ii < BrainCutTextures.Count; ++ii)
                Destroy(BrainCutTextures[ii]);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nbSplits"></param>
        public void ResetSplitsNumber(int nbSplits)
        {
            // generators dll
            //      brain
            DLLBrainTextureGenerators = new List<DLL.MRIBrainGenerator>(nbSplits);
            for (int ii = 0; ii < nbSplits; ++ii)
                DLLBrainTextureGenerators.Add(new DLL.MRIBrainGenerator());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nbCuts"></param>
        public virtual void UpdateCutsPlanesNumber(int diffCuts)
        {
            if (diffCuts < 0)
            {
                for (int ii = 0; ii < -diffCuts; ++ii)
                {
                    // GO textures
                    BrainCutTextures.Add(Texture2Dutility.GenerateCut());                    
                    GUIBrainCutTextures.Add(Texture2Dutility.GenerateGUI());

                    // DLL textures
                    DLLBrainCutTextures.Add(new DLL.Texture());
                    DLLGUIBrainCutTextures.Add(new DLL.Texture());

                    // DLL generators
                    DLLMRITextureCutGenerators.Add(new DLL.MRITextureCutGenerator());
                }
            }
            else if (diffCuts > 0)
            {
                for (int ii = 0; ii < diffCuts; ++ii)
                {
                    // GO textures          
                    Destroy(BrainCutTextures[BrainCutTextures.Count - 1]);
                    BrainCutTextures.RemoveAt(BrainCutTextures.Count - 1);
                    Destroy(GUIBrainCutTextures[GUIBrainCutTextures.Count - 1]);
                    GUIBrainCutTextures.RemoveAt(GUIBrainCutTextures.Count - 1);
                    
                    // DLL textures
                    DLLBrainCutTextures.RemoveAt(DLLBrainCutTextures.Count - 1);
                    DLLGUIBrainCutTextures.RemoveAt(DLLGUIBrainCutTextures.Count - 1);

                    // DLL generators
                    DLLMRITextureCutGenerators[DLLMRITextureCutGenerators.Count - 1].Dispose();
                    DLLMRITextureCutGenerators.RemoveAt(DLLMRITextureCutGenerators.Count - 1);
                }
            }

            if (diffCuts != 0)
            {
                IsRenderingUpToDate = false;
            }
        }
        /// <summary>
        /// Set the sites visibility state
        /// </summary>
        /// <param name="isVisible"></param>
        public void SetSitesVisibility(bool isVisible)
        {
            string layer;
            if (isVisible)
                layer = Layer;
            else
                layer = "Inactive";

            for(int ii = 0; ii < Sites.Count; ++ii)
            {
                Sites[ii].gameObject.layer = LayerMask.NameToLayer(layer);
            }
        }
        /// <summary>
        /// Update the ROI
        /// </summary>
        /// <param name="ROI"></param>
        public void UpdateROI(ROI ROI)
        {
            if(m_SelectedROI != null)
            {
                m_SelectedROI.SetVisibility(false);
            }

            m_SelectedROI = ROI;
            m_SelectedROI.SetVisibility(true);
        }
        /// <summary>
        /// Retrieve a string containing all the plots states
        /// </summary>
        /// <returns></returns>
        public string SiteStatesIntoString()
        {
            string text = "Plots states\n";
            int id = 0;
            for(int ii = 0; ii < SitesGameObjects.Count; ++ii) // patients
            {
                text += "n " + Sites[id].Information.PatientName + "\n";
                for (int jj = 0; jj < SitesGameObjects[ii].Count; ++jj) // electrodes
                {
                    text += "e " + jj + "\n";
                    for (int kk = 0; kk < SitesGameObjects[ii][jj].Count; ++kk, id++) // plots
                    {
                        string plotGOName = SitesGameObjects[ii][jj][kk].name;
                        string[] split = plotGOName.Split('_');
                        text += split[split.Length - 1] + " " + (Sites[id].Information.IsExcluded ? 1 : 0) + " " + (Sites[id].Information.IsBlackListed ? 1 : 0 ) + " " + (Sites[id].Information.IsMasked ? 1 : 0) + " " + (Sites[id].Information.IsHighlighted? 1 : 0) + "\n";
                    }
                }
            }
            
            return text;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string OnlySitesInROIIntoString()
        {
            List<bool> sitesInROIPerPatient = new List<bool>(SitesGameObjects.Count);
            List<List<bool>> sitesInROIPerElectrode = new List<List<bool>>(SitesGameObjects.Count);
            List<List<List<bool>>> sitesInROIPerPlot = new List<List<List<bool>>>(SitesGameObjects.Count);
            int id = 0;
            for (int ii = 0; ii < SitesGameObjects.Count; ++ii)
            {                
                bool isInROIP = false;
                sitesInROIPerPlot.Add(new List<List<bool>>(SitesGameObjects[ii].Count));
                sitesInROIPerElectrode.Add(new List<bool>(SitesGameObjects[ii].Count));
                for (int jj = 0; jj < SitesGameObjects[ii].Count; ++jj)
                {
                    bool isInROIElec = false;
                    sitesInROIPerPlot[ii].Add(new List<bool>(SitesGameObjects[ii][jj].Count));
                    for (int kk = 0; kk < SitesGameObjects[ii][jj].Count; ++kk, ++id)
                    {
                        bool inROI = !Sites[id].Information.IsInROI;
                        bool blackList = Sites[id].Information.IsBlackListed;

                        bool keep = inROI && !blackList;

                        if (!isInROIElec)
                            isInROIElec = keep;

                        if (!isInROIP)
                        {
                            isInROIP = keep;
                        }

                        sitesInROIPerPlot[ii][jj].Add(keep);
                    }
                    sitesInROIPerElectrode[ii].Add(isInROIElec);
                }
                sitesInROIPerPatient.Add(isInROIP);
            }

            string text = "";
            id = 0;
            for (int ii = 0; ii < SitesGameObjects.Count; ++ii) // patients
            {
                if (sitesInROIPerPatient[ii])
                    text += "n " + Sites[id].Information.PatientName + "\n";

                for (int jj = 0; jj < SitesGameObjects[ii].Count; ++jj) // electrodes
                {
                    if (sitesInROIPerElectrode[ii][jj])
                        text += "e " + jj + "\n";

                    for (int kk = 0; kk < SitesGameObjects[ii][jj].Count; ++kk, id++) // plots
                    {
                        if (sitesInROIPerPlot[ii][jj][kk])
                        {
                            string plotGOName = SitesGameObjects[ii][jj][kk].name;
                            string[] split = plotGOName.Split('_');
                            text += split[split.Length - 1] + " " + (Sites[id].Information.IsExcluded ? 1 : 0) + " " + (Sites[id].Information.IsBlackListed ? 1 : 0) + " " + (Sites[id].Information.IsMasked ? 1 : 0) + " " + (Sites[id].Information.IsHighlighted ? 1 : 0) + "\n";
                        }
                    }
                }
            }

            return text;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="colormap"></param>
        /// <param name="colorBrainCut"></param>
        public void ResetColorSchemes(ColorType colormap, ColorType colorBrainCut)
        {
            DLLCutColorScheme = DLL.Texture.Generate2DColorTexture(colorBrainCut, colormap); 
            DLLCutFMRIColorScheme = DLL.Texture.Generate2DColorTexture(colorBrainCut, colormap);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="geometryGenerator"></param>
        /// <param name="volume"></param>
        /// <param name="indexCut"></param>
        /// <param name="MRICalMinFactor"></param>
        /// <param name="MRICalMaxFactor"></param>
        public void CreateMRITexture(DLL.MRIGeometryCutGenerator geometryGenerator, DLL.Volume volume, int indexCut, float MRICalMinFactor, float MRICalMaxFactor)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-Column3DView create_MRI_texture reset 0  ");
            DLL.MRITextureCutGenerator textureGenerator = DLLMRITextureCutGenerators[indexCut];
            textureGenerator.Reset(geometryGenerator);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Column3DView create_MRI_texture fill_texture_with_volume 1  ");
            textureGenerator.FillTextureWithVolume(volume, DLLCutColorScheme, MRICalMinFactor, MRICalMaxFactor);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Column3DView create_MRI_texture updateTexture 2  ");
            textureGenerator.UpdateTexture(DLLBrainCutTextures[indexCut]);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Column3DView create_MRI_texture update_texture_2D 3  ");
            DLLBrainCutTextures[indexCut].UpdateTexture2D(BrainCutTextures[indexCut]); // update mesh cut 2D texture
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="orientation"></param>
        /// <param name="flip"></param>
        /// <param name="cutPlanes"></param>
        /// <param name="drawLines"></param>
        public void CreateGUIMRITexture(int indexCut, string orientation, bool flip, List<Cut> cutPlanes, bool drawLines)
        {
            if (DLLBrainCutTextures[indexCut].m_TextureSize[0] > 0)
            { 
                DLLGUIBrainCutTextures[indexCut].CopyAndRotate(DLLBrainCutTextures[indexCut], orientation, flip, drawLines, indexCut, cutPlanes, DLLMRITextureCutGenerators[indexCut]);                
                DLLGUIBrainCutTextures[indexCut].UpdateTexture2D(GUIBrainCutTextures[indexCut]);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void AddView()
        {
            View3D view = Instantiate(ViewPrefab, transform.Find("Views")).GetComponent<View3D>();
            view.gameObject.name = "View " + m_Views.Count;
            view.LineID = m_Views.Count;
            view.Layer = Layer;
            view.OnSelectView.AddListener((selectedView) =>
            {
                Debug.Log("OnSelectView");
                foreach (View3D v in m_Views)
                {
                    if (v != selectedView)
                    {
                        v.IsSelected = false;
                    }
                }
                IsSelected = true;
                ApplicationState.Module3D.OnSelectView.Invoke(selectedView);
            });
            view.OnMoveView.AddListener(() =>
            {
                OnMoveView.Invoke(view);
            });
            if (IsSelected)
            {
                view.IsColumnSelected = true;
            }
            m_Views.Add(view);
        }
        /// <summary>
        /// 
        /// </summary>
        public void RemoveView(int lineID)
        {
            Destroy(m_Views[lineID].gameObject);
            m_Views.RemoveAt(lineID);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="regionOfInterest"></param>
        public void AddRegionOfInterest(Data.Visualization.RegionOfInterest regionOfInterest)
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="regionOfInterest"></param>
        public void RemoveRegionOfInterest(Data.Visualization.RegionOfInterest regionOfInterest)
        {

        }
        #endregion
    }
}
