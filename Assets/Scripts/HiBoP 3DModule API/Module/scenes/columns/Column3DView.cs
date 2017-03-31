
/**
 * \file    Column3DView.cs
 * \author  Lance Florian
 * \date    21/03/2016
 * \brief   Define Column3DView class
 */


// system
using System.Text;
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;

namespace HBP.VISU3D
{
    /// <summary>
    /// Column 3D view base class
    /// </summary>
    abstract public class Column3DView : MonoBehaviour
    {
        #region members

        public string m_label = ""; /**< name of  the column */
        public string Label
        {
            get { return m_label; }
            set { m_label = value;  }
        }

        // scene
        public bool isFMRI;
        protected bool spScene; /**< is sp scene column ? */
        protected int idColumn; /**< id of the column */
        public string layerColumn; /**< layer of the column */

        // cuts        
        public int nbCuts;  /**< planes cut number */

        // plots
        public int id_selected_site = -1;  /**< id of the selected plot of the column */
        public DLL.RawSiteList RawElectrodes = null;  /**< raw format of the plots container dll */
        public GameObject patientPlotsParent = null; /**< parent of the plots */
        public List<List<List<GameObject>>> plotsGO = null; /**< plots GO list with order : patient/electrode/plot */
        public List<Site> Sites = null; /**< plots list */

        // select plot
        protected SiteRing m_selectRing = null;
        public SiteRing SelectRing { get { return m_selectRing; } }

        // ROI
        protected ROI m_ROI = null;   /**< selected ROI of the column */
        public ROI ROI { get { return m_ROI;} }

        // generators
        public List<DLL.MRITextureCutGenerator> DLLMRITextureCutGeneratorList = null;
        public List<DLL.MRIBrainGenerator> DLLBrainTextureGeneratorList = new List<DLL.MRIBrainGenerator>();

        //public DLL.Texture DLLBrainColorScheme = null;          /**< brain colorscheme dll texture */
        public DLL.Texture DLLCutColorScheme = null;            /**< cut colorscheme dll texture */
        public DLL.Texture DLLCutFMRIColorScheme = null;        /**< cut colorscheme dll texture */
        public List<DLL.Texture> dllBrainCutTextures = null;    /**< list of cut DLL textures */
        public List<DLL.Texture> dllGuiBrainCutTextures = null; /**< list of GUI DLL cut textures| */
        //  texture 2D
        //public DLL.Texture DLLBrainMainColor = null;            /**< main color dll texture of the brain mesh */
        public Texture2D brainColorSchemeTexture = null;        /**< brain colorscheme unity 2D texture  */
        public List<Texture2D> brainCutTextures = null;         /**< list of cut textures */
        public List<Texture2D> guiBrainCutTextures = null;      /**< list of GUI cut textures */

        #endregion members

        #region functions



        /// <summary>
        /// Base init class of the column
        /// </summary>
        /// <param name="idColumn"></param>
        /// <param name="nbCuts"></param>
        /// <param name="plots"></param>
        /// <param name="plotsGO"></param>
        public void init(int idColumn, int nbCuts, DLL.PatientElectrodesList plots, List<GameObject> PlotsPatientParent)
        {
            // scene
            this.idColumn = idColumn;
            layerColumn = "C" + this.idColumn + "_";
            spScene = transform.parent.name == "SP";
            if (spScene)
                layerColumn += "SP";
            else
                layerColumn += "MP";

            // cuts
            this.nbCuts = nbCuts;

            // select ring
            gameObject.AddComponent<SiteRing>();
            m_selectRing = gameObject.GetComponent<SiteRing>();
            m_selectRing.set_layer(layerColumn);

            // plots
            RawElectrodes = new DLL.RawSiteList();
            plots.extract_raw_site_list(RawElectrodes);

            GameObject patientPlotsParent = new GameObject("elecs");
            patientPlotsParent.transform.SetParent(transform);

            plotsGO = new List<List<List<GameObject>>>(PlotsPatientParent.Count);
            Sites = new List<Site>(plots.total_sites_nb());
            for (int ii = 0; ii < PlotsPatientParent.Count; ++ii)
            {
                // instantiate patient plots
                GameObject patientPlots = Instantiate(PlotsPatientParent[ii]);
                patientPlots.transform.SetParent(patientPlotsParent.transform);
                patientPlots.name = PlotsPatientParent[ii].name;

                plotsGO.Add(new List<List<GameObject>>(patientPlots.transform.childCount));
                for (int jj = 0; jj < patientPlots.transform.childCount; ++jj)
                {
                    int nbPlots = patientPlots.transform.GetChild(jj).childCount;

                    plotsGO[ii].Add(new List<GameObject>(nbPlots));
                    for (int kk = 0; kk < nbPlots; ++kk)
                    {
                        plotsGO[ii][jj].Add(patientPlots.transform.GetChild(jj).GetChild(kk).gameObject);
                        plotsGO[ii][jj][kk].layer = LayerMask.NameToLayer(layerColumn);
                        Sites.Add(patientPlots.transform.GetChild(jj).GetChild(kk).gameObject.GetComponent<Site>());

                        int id = Sites.Count - 1;
                        Sites[id].exclude = false;
                        Sites[id].columnROI = !spScene;
                        Sites[id].columnMask = false;
                        Sites[id].blackList = false;
                        Sites[id].isActive = spScene;
                    }
                }
            }


            // generators dll
            DLLMRITextureCutGeneratorList = new List<DLL.MRITextureCutGenerator>(nbCuts);
            for (int ii = 0; ii < nbCuts; ++ii)
            {                
                DLLMRITextureCutGeneratorList.Add(new DLL.MRITextureCutGenerator());
            }

            // DLL textures
            DLLCutColorScheme = new DLL.Texture();
            DLLCutFMRIColorScheme = new DLL.Texture();
            dllGuiBrainCutTextures = new List<DLL.Texture>(nbCuts);
            dllBrainCutTextures = new List<DLL.Texture>(nbCuts);
            for (int ii = 0; ii < nbCuts; ++ii)
            {
                dllGuiBrainCutTextures.Add(new DLL.Texture());
                dllBrainCutTextures.Add(new DLL.Texture());
            }

            // textures 2D
            brainColorSchemeTexture = Texture2Dutility.generate_color_scheme();
            brainCutTextures = new List<Texture2D>(nbCuts);
            guiBrainCutTextures = new List<Texture2D>(nbCuts);
            for (int ii = 0; ii < nbCuts; ++ii)
            {
                brainCutTextures.Add(Texture2Dutility.generate_cut());
                guiBrainCutTextures.Add(Texture2Dutility.generate_GUI());
            }
        }

        /// <summary>
        ///  Clean all allocated data
        /// </summary>
        public void clean()
        {
            DLLCutColorScheme.Dispose();
            DLLCutFMRIColorScheme.Dispose();
            Destroy(brainColorSchemeTexture);

            // DLL
            // generators
            for (int ii = 0; ii < DLLBrainTextureGeneratorList.Count; ++ii)
            {
                DLLBrainTextureGeneratorList[ii].Dispose();
            }
            for (int ii = 0; ii < DLLMRITextureCutGeneratorList.Count; ++ii)
            {
                DLLMRITextureCutGeneratorList[ii].Dispose();
            }

            // textures 2D
            for (int ii = 0; ii < brainCutTextures.Count; ++ii)
                Destroy(brainCutTextures[ii]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nbSplits"></param>
        public void reset_splits_nb(int nbSplits)
        {
            // generators dll
            //      brain
            DLLBrainTextureGeneratorList = new List<DLL.MRIBrainGenerator>(nbSplits);
            for (int ii = 0; ii < nbSplits; ++ii)
                DLLBrainTextureGeneratorList.Add(new DLL.MRIBrainGenerator());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nbCuts"></param>
        public void update_cuts_planes_nb(int newCutsNb)
        {
            int diffCuts = nbCuts - newCutsNb;
            if (diffCuts < 0)
            {
                for (int ii = 0; ii < -diffCuts; ++ii)
                {
                    // GO textures
                    brainCutTextures.Add(Texture2Dutility.generate_cut());                    
                    guiBrainCutTextures.Add(Texture2Dutility.generate_GUI());

                    // DLL textures
                    dllBrainCutTextures.Add(new DLL.Texture());
                    dllGuiBrainCutTextures.Add(new DLL.Texture());

                    // DLL generators
                    DLLMRITextureCutGeneratorList.Add(new DLL.MRITextureCutGenerator());
                }
            }
            else if (diffCuts > 0)
            {
                for (int ii = 0; ii < diffCuts; ++ii)
                {
                    // GO textures          
                    Destroy(brainCutTextures[brainCutTextures.Count - 1]);
                    brainCutTextures.RemoveAt(brainCutTextures.Count - 1);
                    Destroy(guiBrainCutTextures[guiBrainCutTextures.Count - 1]);
                    guiBrainCutTextures.RemoveAt(guiBrainCutTextures.Count - 1);
                    
                    // DLL textures
                    dllBrainCutTextures.RemoveAt(dllBrainCutTextures.Count - 1);
                    dllGuiBrainCutTextures.RemoveAt(dllGuiBrainCutTextures.Count - 1);

                    // DLL generators
                    DLLMRITextureCutGeneratorList[DLLMRITextureCutGeneratorList.Count - 1].Dispose();
                    DLLMRITextureCutGeneratorList.RemoveAt(DLLMRITextureCutGeneratorList.Count - 1);
                }
            }
            nbCuts = newCutsNb;
        }


        /// <summary>
        /// Set the sites visibility state
        /// </summary>
        /// <param name="isVisible"></param>
        public void set_visible_sites(bool isVisible)
        {
            string layer;
            if (isVisible)
                layer = layerColumn;
            else
                layer = "Inactive";

            for(int ii = 0; ii < Sites.Count; ++ii)
            {
                Sites[ii].gameObject.layer = LayerMask.NameToLayer(layer);
            }
        }


        /// <summary>
        /// Return the current selected site of the column
        /// </summary>
        /// <returns></returns>
        public Site current_selected_site()
        {
            if (id_selected_site >= 0 && id_selected_site < Sites.Count)
                return Sites[id_selected_site];

            return null;
        }

        /// <summary>
        /// Update the ROI
        /// </summary>
        /// <param name="ROI"></param>
        public void update_ROI(ROI ROI)
        {
            if(m_ROI != null)
            {
                m_ROI.set_visible(false);
            }

            m_ROI = ROI;
            m_ROI.set_visible(true);
        }

        /// <summary>
        /// Retrieve a string containing all the plots states
        /// </summary>
        /// <returns></returns>
        public string site_state_str()
        {
            string text = "Plots states\n";
            int id = 0;
            for(int ii = 0; ii < plotsGO.Count; ++ii) // patients
            {
                text += "n " + Sites[id].patientName + "\n";
                for (int jj = 0; jj < plotsGO[ii].Count; ++jj) // electrodes
                {
                    text += "e " + jj + "\n";
                    for (int kk = 0; kk < plotsGO[ii][jj].Count; ++kk, id++) // plots
                    {
                        string plotGOName = plotsGO[ii][jj][kk].name;
                        string[] split = plotGOName.Split('_');
                        text += split[split.Length - 1] + " " + (Sites[id].exclude ? 1 : 0) + " " + (Sites[id].blackList ? 1 : 0 ) + " " + (Sites[id].columnMask ? 1 : 0) + " " + (Sites[id].highlight? 1 : 0) + "\n";
                    }
                }
            }
            
            return text;
        }

        public string only_sites_in_ROI_str()
        {
            List<bool> sitesInROIPerPatient = new List<bool>(plotsGO.Count);
            List<List<bool>> sitesInROIPerElectrode = new List<List<bool>>(plotsGO.Count);
            List<List<List<bool>>> sitesInROIPerPlot = new List<List<List<bool>>>(plotsGO.Count);
            int id = 0;
            for (int ii = 0; ii < plotsGO.Count; ++ii)
            {                
                bool isInROIP = false;
                sitesInROIPerPlot.Add(new List<List<bool>>(plotsGO[ii].Count));
                sitesInROIPerElectrode.Add(new List<bool>(plotsGO[ii].Count));
                for (int jj = 0; jj < plotsGO[ii].Count; ++jj)
                {
                    bool isInROIElec = false;
                    sitesInROIPerPlot[ii].Add(new List<bool>(plotsGO[ii][jj].Count));
                    for (int kk = 0; kk < plotsGO[ii][jj].Count; ++kk, ++id)
                    {
                        bool inROI = !Sites[id].columnROI;
                        bool blackList = Sites[id].blackList;

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
            for (int ii = 0; ii < plotsGO.Count; ++ii) // patients
            {
                if (sitesInROIPerPatient[ii])
                    text += "n " + Sites[id].patientName + "\n";

                for (int jj = 0; jj < plotsGO[ii].Count; ++jj) // electrodes
                {
                    if (sitesInROIPerElectrode[ii][jj])
                        text += "e " + jj + "\n";

                    for (int kk = 0; kk < plotsGO[ii][jj].Count; ++kk, id++) // plots
                    {
                        if (sitesInROIPerPlot[ii][jj][kk])
                        {
                            string plotGOName = plotsGO[ii][jj][kk].name;
                            string[] split = plotGOName.Split('_');
                            text += split[split.Length - 1] + " " + (Sites[id].exclude ? 1 : 0) + " " + (Sites[id].blackList ? 1 : 0) + " " + (Sites[id].columnMask ? 1 : 0) + " " + (Sites[id].highlight ? 1 : 0) + "\n";
                        }
                    }
                }
            }

            return text;
        }

        public void reset_color_schemes(int idColorMap, int idColorBrainCut)
        {
            DLLCutColorScheme = DLL.Texture.generate_2D_color_texture(idColorBrainCut, idColorMap); 
            DLLCutFMRIColorScheme = DLL.Texture.generate_2D_color_texture(idColorBrainCut, idColorMap);
        }

        public void create_MRI_texture(DLL.MRIGeometryCutGenerator geometryGenerator, DLL.Volume volume, int indexCut, float MRICalMinFactor, float MRICalMaxFactor)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-Column3DView create_MRI_texture reset 0  ");
            DLL.MRITextureCutGenerator textureGenerator = DLLMRITextureCutGeneratorList[indexCut];
            textureGenerator.reset(geometryGenerator);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Column3DView create_MRI_texture fill_texture_with_volume 1  ");
            textureGenerator.fill_texture_with_volume(volume, DLLCutColorScheme, MRICalMinFactor, MRICalMaxFactor);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Column3DView create_MRI_texture updateTexture 2  ");
            textureGenerator.update_texture(dllBrainCutTextures[indexCut]);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Column3DView create_MRI_texture update_texture_2D 3  ");
            dllBrainCutTextures[indexCut].update_texture_2D(brainCutTextures[indexCut]); // update mesh cut 2D texture
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public void create_GUI_MRI_texture(int indexCut, string orientation, bool flip, List<Plane> cutPlanes, bool drawLines)
        {
            if (dllBrainCutTextures[indexCut].m_sizeTexture[0] > 0)
            { 
                dllGuiBrainCutTextures[indexCut].copy_frome_and_rotate(dllBrainCutTextures[indexCut], orientation, flip, drawLines, indexCut, cutPlanes, DLLMRITextureCutGeneratorList[indexCut]);                
                dllGuiBrainCutTextures[indexCut].update_texture_2D(guiBrainCutTextures[indexCut]);
            }
        }
        
        #endregion members
    }
}
