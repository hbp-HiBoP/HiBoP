
/**
 * \file    Column3DViewIEEG.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Column3DViewIEEG class
 */

// system
using System;
using System.Collections.Generic;
using System.Linq;
// unity
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    /// <summary>
    /// A 3D column view IEGG, containing all necessary data concerning a data column
    /// </summary>
    public class Column3DIEEG : Column3D
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
        public Data.Visualization.Column Column = null; /**< column data formalized by the unity main UI part */
        
        // textures
        public List<Texture2D> BrainCutWithIEEGTextures     = null;
        public List<Texture2D> GUIBrainCutWithIEEGTextures  = null;
         
        public List<DLL.Texture> DLLBrainCutWithIEEGTextures        = null;
        public List<DLL.Texture> DLLGUIBrainCutWithIEEGTextures = null;

        // IEEG
        public bool SendInformation = true; /**< send info at each plot click ? */
        public bool UpdateIEEG; /**< amplitude needs to be updated ? */
        public int ColumnTimeLineID = 0; /**< timeline column ID */
        public int CurrentTimeLineID = 0; /**< curent timeline column ID */
        public float SharedMinInf = 0f;
        public float SharedMaxInf = 0f;

        /// <summary>
        /// IEEG data of the column
        /// </summary>
        public class IEEGDataParameters
        {
            private const float MIN_INFLUENCE = 0.0f;
            private const float MAX_INFLUENCE = 50.0f;
            private float m_MaximumInfluence = 15.0f;
            /// <summary>
            /// Maximum influence amplitude of a site
            /// </summary>
            public float MaximumInfluence
            {
                get
                {
                    return m_MaximumInfluence;
                }
                set
                {
                    float val = Mathf.Clamp(value, MIN_INFLUENCE, MAX_INFLUENCE);
                    if (m_MaximumInfluence != val)
                    {
                        m_MaximumInfluence = val;
                        OnUpdateMaximumInfluence.Invoke();
                    }
                }
            }

            private float m_Gain = 1.0f;
            /// <summary>
            /// Gain of the spheres representing the sites
            /// </summary>
            public float Gain
            {
                get
                {
                    return m_Gain;
                }
                set
                {
                    if (m_Gain != value)
                    {
                        m_Gain = value;
                        OnUpdateGain.Invoke();
                    }
                }
            }

            private float m_MinimumAmplitude = float.MinValue;
            /// <summary>
            /// Minimum amplitude value
            /// </summary>
            public float MinimumAmplitude
            {
                get
                {
                    return m_MinimumAmplitude;
                }
                set
                {
                    m_MinimumAmplitude = value;
                }
            }

            private float m_MaximumAmplitude = float.MaxValue;
            /// <summary>
            /// Maximum amplitude value
            /// </summary>
            public float MaximumAmplitude
            {
                get
                {
                    return m_MaximumAmplitude;
                }
                set
                {
                    m_MaximumAmplitude = value;
                }
            }

            private float m_AlphaMin = 0.2f;
            /// <summary>
            /// Minimum Alpha
            /// </summary>
            public float AlphaMin
            {
                get
                {
                    return m_AlphaMin;
                }
                set
                {
                    if (m_AlphaMin != value)
                    {
                        m_AlphaMin = value;
                        OnUpdateAlphaValues.Invoke();
                    }
                }
            }

            private float m_AlphaMax = 1.0f;
            /// <summary>
            /// Maximum Alpha
            /// </summary>
            public float AlphaMax
            {
                get
                {
                    return m_AlphaMax;
                }
                set
                {
                    if (m_AlphaMax != value)
                    {
                        m_AlphaMax = value;
                        OnUpdateAlphaValues.Invoke();
                    }
                }
            }

            private float m_SpanMin = -50.0f;
            /// <summary>
            /// Span Min value
            /// </summary>
            public float SpanMin
            {
                get
                {
                    return m_SpanMin;
                }
                set
                {
                    if (m_SpanMin != value)
                    {
                        m_SpanMin = value;
                        OnUpdateSpanValues.Invoke();
                    }
                }
            }

            private float m_Middle = 0.0f;
            /// <summary>
            /// Middle value
            /// </summary>
            public float Middle
            {
                get
                {
                    return m_Middle;
                }
                set
                {
                    if (m_Middle != value)
                    {
                        m_Middle = value;
                        OnUpdateSpanValues.Invoke();
                    }
                }
            }

            private float m_SpanMax = 50.0f;
            /// <summary>
            /// Span Min value
            /// </summary>
            public float SpanMax
            {
                get
                {
                    return m_SpanMax;
                }
                set
                {
                    if (m_SpanMax != value)
                    {
                        m_SpanMax = value;
                        OnUpdateSpanValues.Invoke();
                    }
                }
            }

            /// <summary>
            /// Event called when updating the span values (min, mid or max)
            /// </summary>
            public UnityEvent OnUpdateSpanValues = new UnityEvent();
            /// <summary>
            /// Event called when updating the alpha values
            /// </summary>
            public UnityEvent OnUpdateAlphaValues = new UnityEvent();
            /// <summary>
            /// Event called when updating the sphere gain
            /// </summary>
            public UnityEvent OnUpdateGain = new UnityEvent();
            /// <summary>
            /// Event called when updating the maximum influence
            /// </summary>
            public UnityEvent OnUpdateMaximumInfluence = new UnityEvent();
        }
        private IEEGDataParameters m_IEEGParameters = new IEEGDataParameters();
        /// <summary>
        /// IEEG Parameters
        /// </summary>
        public IEEGDataParameters IEEGParameters
        {
            get
            {
                return m_IEEGParameters;
            }
        }

        //  amplitudes
        public int[] Dimensions = new int[3]; /**< amplitudes array dimensions (dims[0] = size| dims[1] = 1 (legacy) | dims[2] = plots number ) */
        public float[] IEEGValues = new float[0]; /**< amplitudes 1D array (to be sent to the DLL) */
        public float[][] IEEGValuesBySiteID;
        //  plots
        public List<Vector3> ElectrodesSizeScale = null;  /**< scale of the plots of this column */
        public List<bool> ElectrodesPositiveColor = null; /**< is positive color ? */

        // latencies
        public bool SourceDefined = false; /**< a source has been defined */
        public bool IsSiteASource = false; /**< current selected plot is a source */
        public bool SiteLatencyData = false; /**< latency data defined for the current selected plot */
        public int SourceSelectedID = -1; /**< id of the selected source */
        public int CurrentLatencyFile = -1; /**< id of the current latency file */
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
            UpdateIEEG = false;

            // GO textures
            BrainCutWithIEEGTextures = new List<Texture2D>(nbCuts);
            GUIBrainCutWithIEEGTextures = new List<Texture2D>(nbCuts);
            for (int jj = 0; jj < nbCuts; ++jj)
            {                
                BrainCutWithIEEGTextures.Add(Texture2Dutility.GenerateCut(1,1));
                GUIBrainCutWithIEEGTextures.Add(Texture2Dutility.GenerateGUI(1, 1));                
            }
            // DLL textures
            DLLBrainCutWithIEEGTextures = new List<DLL.Texture>(nbCuts);
            DLLGUIBrainCutWithIEEGTextures = new List<DLL.Texture>(nbCuts);
            for (int jj = 0; jj < nbCuts; ++jj)
            {
                DLLBrainCutWithIEEGTextures.Add(new DLL.Texture());
                DLLGUIBrainCutWithIEEGTextures.Add(new DLL.Texture());
            }

            // plots
            ElectrodesSizeScale = new List<Vector3>(m_RawElectrodes.NumberOfSites);
            ElectrodesPositiveColor = new List<bool>(m_RawElectrodes.NumberOfSites);

            // masks
            for (int ii = 0; ii < m_RawElectrodes.NumberOfSites; ii++)
            {
                ElectrodesSizeScale.Add(new Vector3(1, 1, 1));
                ElectrodesPositiveColor.Add(true);
            }
        }
        /// <summary>
        /// Update the site mask of the dll with all the masks
        /// </summary>
        public void UpdateDLLSitesMask()
        {
            bool noROI = (transform.parent.GetComponent<Base3DScene>().Type == SceneType.SinglePatient) ? false : (m_SelectedROI.NumberOfBubbles == 0);
            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                m_RawElectrodes.UpdateMask(ii, (Sites[ii].Information.IsMasked || Sites[ii].Information.IsBlackListed || Sites[ii].Information.IsExcluded || (Sites[ii].Information.IsInROI && !noROI)));
            }
        }
        /// <summary>
        /// Return the length of the timeline 
        /// </summary>
        /// <returns></returns>
        public int TimelineLength()
        {
            return Dimensions[0];
        }
        /// <summary>
        /// Specify a new columnData to be associated with the columnd3DView
        /// </summary>
        /// <param name="columnData"></param>
        public void SetColumnData(Data.Visualization.Column newColumnData)
        {
            Column = newColumnData;

            // update amplitudes sizes and values
            Dimensions = new int[3];
            Dimensions[0] = Column.TimeLine.Lenght;
            Dimensions[1] = 1;
            Dimensions[2] = 0;

            // Construct sites value array the old way, and set sites masks // maybe FIXME
            IEEGValuesBySiteID = new float[Dimensions[0]][];
            int siteID = 0;
            foreach (var configurationPatient in Column.Configuration.ConfigurationByPatient)
            {
                foreach (var electrodeConfiguration in configurationPatient.Value.ConfigurationByElectrode)
                {
                    foreach (var siteConfiguration in electrodeConfiguration.Value.ConfigurationBySite)
                    {
                        IEEGValuesBySiteID[siteID] = siteConfiguration.Value.Values;
                        Sites[siteID].Information.IsMasked = siteConfiguration.Value.IsMasked; // update mask
                        siteID++;
                    }
                    Dimensions[2] += electrodeConfiguration.Value.ConfigurationBySite.Count;
                }
            }

            IEEGParameters.MinimumAmplitude = float.MaxValue;
            IEEGParameters.MaximumAmplitude = float.MinValue;

            IEEGValues = new float[Dimensions[0] * Dimensions[1] * Dimensions[2]];
            for (int ii = 0; ii < Dimensions[0]; ++ii)
            {
                for (int jj = 0; jj < Dimensions[2]; ++jj)
                {
                    IEEGValues[ii * Dimensions[2] + jj] = IEEGValuesBySiteID[jj][ii];

                    // update min/max values
                    if (IEEGValuesBySiteID[jj][ii] > IEEGParameters.MaximumAmplitude)
                        IEEGParameters.MaximumAmplitude = IEEGValuesBySiteID[jj][ii];

                    if (IEEGValuesBySiteID[jj][ii] < IEEGParameters.MinimumAmplitude)
                        IEEGParameters.MinimumAmplitude = IEEGValuesBySiteID[jj][ii];
                }
            }

            IEEGParameters.Middle = (IEEGParameters.MinimumAmplitude + IEEGParameters.MaximumAmplitude) / 2;
            IEEGParameters.SpanMin = IEEGParameters.MinimumAmplitude;
            IEEGParameters.SpanMax = IEEGParameters.MaximumAmplitude;
        }
        /// <summary>
        /// Update sites sizes and colors arrays for iEEG (to be called before the rendering update)
        /// </summary>
        public void UpdateSitesSizeAndColorForIEEG()
        {
            UnityEngine.Profiling.Profiler.BeginSample("update_sites_size_and_color_arrays_for_IEEG");

            if (IEEGValuesBySiteID == null) return; // FIXME : delete this when reading data

            float diffMin = IEEGParameters.SpanMin - IEEGParameters.Middle;
            float diffMax = IEEGParameters.SpanMax - IEEGParameters.Middle;

            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                if (Sites[ii].Information.IsInROI || Sites[ii].Information.IsMasked)
                    continue;

                float value = IEEGValuesBySiteID[ii][CurrentTimeLineID];
                if (value < IEEGParameters.SpanMin)
                    value = IEEGParameters.SpanMin;
                if (value > IEEGParameters.SpanMax)
                    value = IEEGParameters.SpanMax;

                value -= IEEGParameters.Middle;

                if (value < 0)
                {
                    ElectrodesPositiveColor[ii] = false;
                    value = 0.5f + 2 * (value / diffMin);                    
                }
                else
                {
                    ElectrodesPositiveColor[ii] = true;
                    value = 0.5f + 2 * (value / diffMax);
                }

                ElectrodesSizeScale[ii] = new Vector3(value, value, value);
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Update the cut planes number of the 3D column view
        /// </summary>
        /// <param name="newCutsNb"></param>
        public new void UpdateCutsPlanesNumber(int diffCuts)
        {
            base.UpdateCutsPlanesNumber(diffCuts);            
            if (diffCuts < 0)
            {
                for (int ii = 0; ii < -diffCuts; ++ii)
                {
                    // GO textures 
                    BrainCutWithIEEGTextures.Add(Texture2Dutility.GenerateCut());
                    GUIBrainCutWithIEEGTextures.Add(Texture2Dutility.GenerateGUI());

                    // DLL textures
                    DLLBrainCutWithIEEGTextures.Add(new DLL.Texture());
                    DLLGUIBrainCutWithIEEGTextures.Add(new DLL.Texture());
                }
            }
            else if (diffCuts > 0)
            {          
                for (int ii = 0; ii < diffCuts; ++ii)
                {
                    // GO textures                
                    Destroy(BrainCutWithIEEGTextures[BrainCutWithIEEGTextures.Count - 1]);                    
                    BrainCutWithIEEGTextures.RemoveAt(BrainCutWithIEEGTextures.Count - 1);

                    Destroy(GUIBrainCutWithIEEGTextures[GUIBrainCutWithIEEGTextures.Count - 1]);
                    GUIBrainCutWithIEEGTextures.RemoveAt(GUIBrainCutWithIEEGTextures.Count - 1);

                    // DLL textures
                    DLLBrainCutWithIEEGTextures.RemoveAt(DLLBrainCutWithIEEGTextures.Count - 1);
                    DLLGUIBrainCutWithIEEGTextures.RemoveAt(DLLGUIBrainCutWithIEEGTextures.Count - 1);
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
            for (int ii = 0; ii < BrainCutWithIEEGTextures.Count; ++ii)
            {                
                Destroy(BrainCutWithIEEGTextures[ii]);
                Destroy(GUIBrainCutWithIEEGTextures[ii]);
            }
        }
        /// <summary>
        /// Update the plots rendering (iEEG or CCEP)
        /// </summary>
        public void UpdateSitesRendering(SceneStatesInfo data, Latencies latenciesFile)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-updatePlotsRendering");

            Vector3 noScale = new Vector3(0, 0, 0);
            Vector3 normalScale = new Vector3(1, 1, 1);
            MeshRenderer renderer = null;
            SiteType siteType;

            if (data.DisplayCCEPMode) // CCEP
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
                        if (SourceSelectedID == -1) // no source selected
                        {
                            Sites[ii].transform.localScale = normalScale;
                            siteType = latenciesFile.IsSiteASource(ii) ? SiteType.Source : SiteType.NotASource;
                        }
                        else // source selected
                        {
                            if (ii == SourceSelectedID)
                            {
                                Sites[ii].transform.localScale = normalScale;
                                siteType = SiteType.Source;
                            }
                            else if (latenciesFile.IsSiteResponsiveForSource(ii, SourceSelectedID)) // data available
                            {
                                // set color
                                siteType = (latenciesFile.PositiveHeight[SourceSelectedID][ii]) ? SiteType.NonePos : SiteType.NoneNeg;

                                // set transparency
                                customAlpha = latenciesFile.Transparencies[SourceSelectedID][ii] - 0.25f;

                                if (Sites[ii].Information.IsHighlighted)
                                    customAlpha = 1;

                                // set size
                                float size = latenciesFile.Sizes[SourceSelectedID][ii];
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
                        m_SelectRing.SetSelectedSite(Sites[ii], Sites[ii].transform.localScale);

                    Material siteMaterial = SharedMaterials.SiteSharedMaterial(highlight, siteType);
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
                    if (!Sites[ii].IsInitialized)
                    {
                        Sites[ii].IsInitialized = true;
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
                    else if (data.IsGeneratorUpToDate)
                    {
                        Sites[ii].transform.localScale = ElectrodesSizeScale[ii] * IEEGParameters.Gain;
                        //  plot size (collider and shape) and color are updated with the current timeline amplitude   
                        siteType = ElectrodesPositiveColor[ii] ? SiteType.Positive : SiteType.Negative;
                    }
                    else // no mask and no amplitude computed : all plots have the same size and color
                    {
                        Sites[ii].transform.localScale = normalScale;
                        siteType = Sites[ii].Information.IsMarked ? SiteType.Marked : SiteType.Normal;
                    }

                    UnityEngine.Profiling.Profiler.EndSample();
                    UnityEngine.Profiling.Profiler.BeginSample("TEST-updatePlotsRendering -3 ");

                    Sites[ii].GetComponent<MeshRenderer>().sharedMaterial = SharedMaterials.SiteSharedMaterial(Sites[ii].Information.IsHighlighted, siteType);

                    if (!activity)
                        Sites[ii].gameObject.SetActive(true);

                    Sites[ii].IsActive = true;

                    UnityEngine.Profiling.Profiler.EndSample();
                }

                // select plot ring 
                if (SelectedSiteID >= 0 && SelectedSiteID < Sites.Count)
                    m_SelectRing.SetSelectedSite(Sites[SelectedSiteID], Sites[SelectedSiteID].transform.localScale);


                UnityEngine.Profiling.Profiler.EndSample();
            }

            if (SelectedSiteID == -1)
            {
                m_SelectRing.SetSelectedSite(null, new Vector3(0,0,0));
            }

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
        public void CreateGUIIEEGTexture(int indexCut, string orientation, bool flip, List<Cut> cutPlanes, bool drawLines)
        {
            if (DLLBrainCutTextures[indexCut].m_TextureSize[0] > 0)
            {
                DLLGUIBrainCutWithIEEGTextures[indexCut].CopyAndRotate(DLLBrainCutWithIEEGTextures[indexCut], orientation, flip, drawLines, indexCut, cutPlanes, DLLMRITextureCutGenerators[indexCut]);
                DLLGUIBrainCutWithIEEGTextures[indexCut].UpdateTexture2D(GUIBrainCutWithIEEGTextures[indexCut]);
            }
        }
        #endregion
    }
}
