
/**
 * \file    Column3DViewIEEG.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Column3DViewIEEG class
 */

// system
using System;
using System.Collections.Generic;

// unity
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// A 3D column view IEGG, containing all necessary data concerning a data column
    /// </summary>
    public class Column3DViewIEEG : Column3DView
    {
        #region Properties
        public override ColumnType Type
        {
            get
            {
                return ColumnType.IEEG;
            }
        }
        // data
        public Data.Visualisation.Column columnData = null; /**< column data formalized by the unity main UI part */
        
        // textures
        public List<Texture2D> brainCutWithIEEGTextures     = null;
        public List<Texture2D> guiBrainCutWithIEEGTextures  = null;
         
        public List<DLL.Texture> dllBrainCutWithIEEGTextures        = null;
        public List<DLL.Texture> dllGuiBrainCutWithIEEGTextures = null;

        // IEEG
        public bool sendInfos = true; /**< send info at each plot click ? */
        public bool updateIEEG; /**< amplitude needs to be updated ? */
        public int columnTimeLineID = 0; /**< timeline column ID */
        public int currentTimeLineID = 0; /**< curent timeline column ID */
        public float sharedMinInf = 0f;
        public float sharedMaxInf = 0f;
        public float minAmp = float.MaxValue; /**< min amplitude value */
        public float maxAmp = float.MinValue; /**< max amplitude value */
        public float gainBubbles = 1f; /**< gain bubbles */
        public float middle = 0f;   /**< middle value */
        public float maxDistanceElec = 15f; /**< amplitude maximum influence of a plot */
        public float spanMin = -50f;
        public float spanMax = 50f;
        public float alphaMin = 0.2f; /**< minimum alpha */
        public float alphaMax = 1f; /**< maximum alpha */
        //  amplitudes
        public int[] dims = new int[3]; /**< amplitudes array dimensions (dims[0] = size| dims[1] = 1 (legacy) | dims[2] = plots number ) */
        public float[] m_iEegValues = new float[0]; /**< amplitudes 1D array (to be sent to the DLL) */        
        //  plots
        public List<Vector3> electrodesSizeScale = null;  /**< scale of the plots of this column */
        public List<bool> electrodesPositiveColor = null; /**< is positive color ? */

        // latencies
        public bool sourceDefined = false; /**< a source has been defined */
        public bool siteIsSource = false; /**< current selected plot is a source */
        public bool siteLatencyData = false; /**< latency data defined for the current selected plot */
        public int idSourceSelected = -1; /**< id of the selected source */
        public int currentLatencyFile = -1; /**< id of the current latency file */

        #endregion

        #region Public Methods
       

        /// <summary>
        /// Init class of the data column
        /// </summary>
        /// <param name="idColumn"></param>
        /// <param name="nbCuts"></param>
        /// <param name="plots"></param>
        /// <param name="plotsGO"></param>
        public override void Initialize(int idColumn, int nbCuts, DLL.PatientElectrodesList plots, List<GameObject> PlotsPatientParent)
        {
            // call parent init
            base.Initialize(idColumn, nbCuts, plots, PlotsPatientParent);

            // amplitudes
            updateIEEG = false;

            // GO textures
            brainCutWithIEEGTextures = new List<Texture2D>(nbCuts);
            guiBrainCutWithIEEGTextures = new List<Texture2D>(nbCuts);
            for (int jj = 0; jj < nbCuts; ++jj)
            {                
                brainCutWithIEEGTextures.Add(Texture2Dutility.generate_cut(1,1));
                guiBrainCutWithIEEGTextures.Add(Texture2Dutility.generate_GUI(1, 1));                
            }
            // DLL textures
            dllBrainCutWithIEEGTextures = new List<DLL.Texture>(nbCuts);
            dllGuiBrainCutWithIEEGTextures = new List<DLL.Texture>(nbCuts);
            for (int jj = 0; jj < nbCuts; ++jj)
            {
                dllBrainCutWithIEEGTextures.Add(new DLL.Texture());
                dllGuiBrainCutWithIEEGTextures.Add(new DLL.Texture());
            }

            // plots
            electrodesSizeScale = new List<Vector3>(m_RawElectrodes.sites_nb());
            electrodesPositiveColor = new List<bool>(m_RawElectrodes.sites_nb());

            // masks
            for (int ii = 0; ii < m_RawElectrodes.sites_nb(); ii++)
            {
                electrodesSizeScale.Add(new Vector3(1, 1, 1));
                electrodesPositiveColor.Add(true);
            }
        }

        /// <summary>
        /// Update the site mask of the dll with all the masks
        /// </summary>
        public void update_DLL_sites_mask()
        {
            bool noROI = (transform.parent.GetComponent<Base3DScene>().Type == SceneType.SinglePatient) ? false : (m_SelectedROI.bubbles_nb() == 0);
            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                m_RawElectrodes.update_mask(ii, (Sites[ii].Information.IsMasked || Sites[ii].Information.IsBlackListed || Sites[ii].Information.IsExcluded || (Sites[ii].Information.IsInROI && !noROI)));
            }
        }

        /// <summary>
        /// Return the amplitudes dimension array
        /// </summary>
        /// <returns></returns>
        public int[] dimensions() { return dims; }

        /// <summary>
        /// Return the amplitudes array
        /// </summary>
        /// <returns></returns>
        public float[] iEEG_values()
        {
            return m_iEegValues;
        }

        /// <summary>
        /// Return the length of the timeline 
        /// </summary>
        /// <returns></returns>
        public int timeline_length()
        {
            return dims[0];
        }

        /// <summary>
        /// Specify a new columnData to be associated with the columnd3DView
        /// </summary>
        /// <param name="columnData"></param>
        public void SetColumnData(Data.Visualisation.Column newColumnData)
        {
            columnData = newColumnData;

            // update amplitudes sizes and values
            dims = new int[3];
            dims[0] = columnData.TimeLine.Lenght;
            dims[1] = 1;
            dims[2] = columnData.SiteMask.Length;

            minAmp = float.MaxValue;
            maxAmp = float.MinValue;

            m_iEegValues = new float[dims[0] * dims[1] * dims[2]];
            for (int ii = 0; ii < dims[0]; ++ii)
            {
                for (int jj = 0; jj < dims[2]; ++jj)
                {
                    m_iEegValues[ii * dims[2] + jj] = columnData.Values[jj][ii];

                    // update min/max values
                    if (columnData.Values[jj][ii] > maxAmp)
                        maxAmp = columnData.Values[jj][ii];

                    if (columnData.Values[jj][ii] < minAmp)
                        minAmp = columnData.Values[jj][ii];
                }
            }

            middle = (minAmp + maxAmp) / 2;
            spanMin = minAmp;
            spanMax = maxAmp;

            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                Sites[ii].Information.IsMasked = columnData.SiteMask[ii];
            }
        }

        /// <summary>
        /// Update sites sizes and colors arrays for iEEG (to be called before the rendering update)
        /// </summary>
        public void update_sites_size_and_color_arrays_for_IEEG()
        {
            UnityEngine.Profiling.Profiler.BeginSample("update_sites_size_and_color_arrays_for_IEEG");

            float diffMin = spanMin - middle;
            float diffMax = spanMax - middle;

            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                if (Sites[ii].Information.IsInROI || Sites[ii].Information.IsMasked)
                    continue;

                float value = columnData.Values[ii][currentTimeLineID];
                if (value < spanMin)
                    value = spanMin;
                if (value > spanMax)
                    value = spanMax;

                value -= middle;

                if (value < 0)
                {
                    electrodesPositiveColor[ii] = false;
                    value = 0.5f + 2 * (value / diffMin);                    
                }
                else
                {
                    electrodesPositiveColor[ii] = true;
                    value = 0.5f + 2 * (value / diffMax);
                }

                electrodesSizeScale[ii] = new Vector3(value, value, value);
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }


        /// <summary>
        /// Update the cut planes number of the 3D column view
        /// </summary>
        /// <param name="newCutsNb"></param>
        public new void update_cuts_planes_nb(int diffCuts)
        {
            base.update_cuts_planes_nb(diffCuts);            
            if (diffCuts < 0)
            {
                for (int ii = 0; ii < -diffCuts; ++ii)
                {
                    // GO textures 
                    brainCutWithIEEGTextures.Add(Texture2Dutility.generate_cut());
                    guiBrainCutWithIEEGTextures.Add(Texture2Dutility.generate_GUI());

                    // DLL textures
                    dllBrainCutWithIEEGTextures.Add(new DLL.Texture());
                    dllGuiBrainCutWithIEEGTextures.Add(new DLL.Texture());
                }
            }
            else if (diffCuts > 0)
            {          
                for (int ii = 0; ii < diffCuts; ++ii)
                {
                    // GO textures                
                    Destroy(brainCutWithIEEGTextures[brainCutWithIEEGTextures.Count - 1]);                    
                    brainCutWithIEEGTextures.RemoveAt(brainCutWithIEEGTextures.Count - 1);

                    Destroy(guiBrainCutWithIEEGTextures[guiBrainCutWithIEEGTextures.Count - 1]);
                    guiBrainCutWithIEEGTextures.RemoveAt(guiBrainCutWithIEEGTextures.Count - 1);

                    // DLL textures
                    dllBrainCutWithIEEGTextures.RemoveAt(dllBrainCutWithIEEGTextures.Count - 1);
                    dllGuiBrainCutWithIEEGTextures.RemoveAt(dllGuiBrainCutWithIEEGTextures.Count - 1);
                }
            }
        }

        /// <summary>
        ///  Clean all dll data and unity textures
        /// </summary>
        public override void Clear()
        {
            base.Clear();

            // plots
            m_RawElectrodes.Dispose();

            // textures 2D
            for (int ii = 0; ii < brainCutWithIEEGTextures.Count; ++ii)
            {                
                Destroy(brainCutWithIEEGTextures[ii]);
                Destroy(guiBrainCutWithIEEGTextures[ii]);
            }
        }

        /// <summary>
        /// Update the plots rendering (iEEG or CCEP)
        /// </summary>
        public void update_sites_rendering(SceneStatesInfo data, Latencies latenciesFile)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-updatePlotsRendering");

            Vector3 noScale = new Vector3(0, 0, 0);
            Vector3 normalScale = new Vector3(1, 1, 1);
            MeshRenderer renderer = null;
            SiteType siteType;

            if (data.displayCcepMode) // CCEP
            {
                for (int ii = 0; ii < Sites.Count; ++ii)
                {
                    //MaterialPropertyBlock props = new MaterialPropertyBlock();

                    bool activity = true;
                    bool highlight = Sites[ii].Information.IsHighlighted;
                    float customAlpha = -1f;
                    renderer = Sites[ii].GetComponent<MeshRenderer>();

                    if (Sites[ii].Information.IsBlackListed) // blacklisted plot
                    {
                        Sites[ii].transform.localScale = noScale;
                        siteType = SiteType.BlackListed;
                    }
                    else if (Sites[ii].Information.IsExcluded) // excluded plot
                    {
                        Sites[ii].transform.localScale = normalScale;
                        siteType = SiteType.Excluded;
                    }
                    else if (latenciesFile != null) // latency file available
                    {
                        if (idSourceSelected == -1) // no source selected
                        {
                            Sites[ii].transform.localScale = normalScale;
                            siteType = latenciesFile.is_size_a_source(ii) ? SiteType.Source : SiteType.NotASource;
                        }
                        else // source selected
                        {
                            if (ii == idSourceSelected)
                            {
                                Sites[ii].transform.localScale = normalScale;
                                siteType = SiteType.Source;
                            }
                            else if (latenciesFile.is_site_responsive_for_source(ii, idSourceSelected)) // data available
                            {
                                // set color
                                siteType = (latenciesFile.positiveHeight[idSourceSelected][ii]) ? SiteType.NonePos : SiteType.NoneNeg;

                                // set transparency
                                customAlpha = latenciesFile.transparencies[idSourceSelected][ii] - 0.25f;

                                if (Sites[ii].Information.IsHighlighted)
                                    customAlpha = 1;

                                // set size
                                float size = latenciesFile.sizes[idSourceSelected][ii];
                                Sites[ii].transform.localScale = new Vector3(size, size, size);
                            }
                            else // no data available
                            {
                                Sites[ii].transform.localScale = normalScale;
                                siteType = SiteType.NoLatencyData;
                            }
                        }
                    }
                    else // no mask and no latency file available : all plots have the same size and color
                    {
                        Sites[ii].transform.localScale = normalScale;
                        siteType = Sites[ii].Information.IsMarked ? SiteType.Marked : SiteType.Normal;
                    }

                    // select plot ring 
                    if (ii == SelectedSiteID)
                        m_selectRing.set_selected_site(Sites[ii], Sites[ii].transform.localScale);

                    Material siteMaterial = SharedMaterials.site_shared_material(highlight, siteType);
                    if (customAlpha > 0f)
                    {
                        Color col = siteMaterial.color;
                        col.a = customAlpha;
                        siteMaterial.color = col;
                    }                    

                    renderer.sharedMaterial = siteMaterial;
                    Sites[ii].gameObject.SetActive(activity);
                }
            }
            else // iEEG
            {
                UnityEngine.Profiling.Profiler.BeginSample("TEST-updatePlotsRendering -1 ");


                for (int ii = 0; ii < Sites.Count; ++ii)
                {
                    bool activity;
                    if(Sites[ii].firstUse)
                    {
                        Sites[ii].firstUse = false;
                        activity = Sites[ii].gameObject.activeSelf;
                    }
                    else
                        activity = Sites[ii].IsActive;

      
                    if (Sites[ii].Information.IsMasked || Sites[ii].Information.IsInROI) // column mask : plot is not visible can't be clicked // ROI mask : plot is not visible, can't be clicked
                    {
                        if (activity)
                            Sites[ii].gameObject.SetActive(false);

                        Sites[ii].IsActive = false;
                        continue;
                    }


                    UnityEngine.Profiling.Profiler.BeginSample("TEST-updatePlotsRendering -2 ");

                    if (Sites[ii].Information.IsBlackListed) // blacklist mask : plot is barely visible with another color, can be clicked
                    {
                        Sites[ii].transform.localScale = normalScale;
                        siteType = SiteType.BlackListed;
                    }
                    else if (Sites[ii].Information.IsExcluded) // excluded mask : plot is a little visible with another color, can be clicked
                    {
                        Sites[ii].transform.localScale = normalScale;
                        siteType = SiteType.Excluded;
                    }
                    else if (data.generatorUpToDate)
                    {
                        Sites[ii].transform.localScale = electrodesSizeScale[ii] * gainBubbles;
                        //  plot size (collider and shape) and color are updated with the current timeline amplitude   
                        siteType = electrodesPositiveColor[ii] ? SiteType.Positive : SiteType.Negative;
                    }
                    else // no mask and no amplitude computed : all plots have the same size and color
                    {
                        Sites[ii].transform.localScale = normalScale;
                        siteType = Sites[ii].Information.IsMarked ? SiteType.Marked : SiteType.Normal;
                    }

                    UnityEngine.Profiling.Profiler.EndSample();
                    UnityEngine.Profiling.Profiler.BeginSample("TEST-updatePlotsRendering -3 ");

                    Sites[ii].GetComponent<MeshRenderer>().sharedMaterial = SharedMaterials.site_shared_material(Sites[ii].Information.IsHighlighted, siteType);

                    if (!activity)
                        Sites[ii].gameObject.SetActive(true);

                    Sites[ii].IsActive = true;

                    UnityEngine.Profiling.Profiler.EndSample();
                }

                // select plot ring 
                if (SelectedSiteID >= 0 && SelectedSiteID < Sites.Count)
                    m_selectRing.set_selected_site(Sites[SelectedSiteID], Sites[SelectedSiteID].transform.localScale);


                UnityEngine.Profiling.Profiler.EndSample();
            }

            if (SelectedSiteID == -1)
            {
                m_selectRing.set_selected_site(null, new Vector3(0,0,0));
            }

            UnityEngine.Profiling.Profiler.EndSample();

        }

        public void create_GUI_IEEG_texture(int indexCut, string orientation, bool flip, List<Plane> cutPlanes, bool drawLines)
        {
            if (dllBrainCutTextures[indexCut].m_sizeTexture[0] > 0)
            {
                dllGuiBrainCutWithIEEGTextures[indexCut].copy_frome_and_rotate(dllBrainCutWithIEEGTextures[indexCut], orientation, flip, drawLines, indexCut, cutPlanes, DLLMRITextureCutGeneratorList[indexCut]);
                dllGuiBrainCutWithIEEGTextures[indexCut].update_texture_2D(guiBrainCutWithIEEGTextures[indexCut]);
            }
        }


        #endregion
    }

}
