// system
using CielaSpike;
/**
* \file    Column3DViewIEEG.cs
* \author  Lance Florian
* \date    2015
* \brief   Define Column3DViewIEEG class
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// unity
using UnityEngine;
using UnityEngine.Events;
using HBP.Module3D.DLL;
using Tools.CSharp;

namespace HBP.Module3D
{
    /// <summary>
    /// A 3D column view IEGG, containing all necessary data concerning a data column
    /// </summary>
    public class Column3DIEEG : Column3D, IConfigurable
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
        public Data.Visualization.Column ColumnData = null;

        // IEEG
        private int m_CurrentTimeLineID = 0;
        public int CurrentTimeLineID
        {
            get
            {
                return m_CurrentTimeLineID;
            }
            set
            {
                if (IsTimelineLooping)
                {
                    m_CurrentTimeLineID = (value % (MaxTimeLineID + 1) + (MaxTimeLineID + 1)) % (MaxTimeLineID + 1);
                }
                else
                {
                    m_CurrentTimeLineID = Mathf.Clamp(value, 0, MaxTimeLineID);
                }
                OnUpdateCurrentTimelineID.Invoke();
                if (IsSelected)
                {
                    ApplicationState.Module3D.OnUpdateSelectedColumnTimeLineID.Invoke();
                }
            }
        }
        public int MaxTimeLineID { get; set; }
        public float MinTimeLine { get; set; }
        public float MaxTimeLine { get; set; }
        public float CurrentTimeLine
        {
            get
            {
                return ColumnData.TimeLine.Step * CurrentTimeLineID + MinTimeLine;
            }
        }
        public string TimeLineUnite
        {
            get
            {
                return ColumnData.TimeLine.Start.Unite;
            }
        }
        public float SharedMinInf = 0f;
        public float SharedMaxInf = 0f;

        /// <summary>
        /// Is the column data looping ?
        /// </summary>
        public bool IsTimelineLooping { get; set; }

        private float m_TimeSinceLastSample = 0.0f;
        private bool m_IsTimelinePlaying = false;
        /// <summary>
        /// Is the timeline incrementing automatically ?
        /// </summary>
        public bool IsTimelinePlaying
        {
            get
            {
                return m_IsTimelinePlaying;
            }
            set
            {
                m_IsTimelinePlaying = value;
                m_TimeSinceLastSample = 0.0f;
            }
        }

        private int m_TimelineStep = 1;
        /// <summary>
        /// Timeline step
        /// </summary>
        public int TimelineStep
        {
            get
            {
                return m_TimelineStep;
            }
            set
            {
                m_TimelineStep = value;
            }
        }
        private float TimelineInterval
        {
            get
            {
                return 1.0f / m_TimelineStep;
            }
        }

        /// <summary>
        /// IEEG data of the column
        /// </summary>
        public class IEEGDataParameters
        {
            public Column3DIEEG Column { get; set; }

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

            private float m_AlphaMin = 0.8f;
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

            private float m_SpanMin = 0.0f;
            /// <summary>
            /// Span Min value
            /// </summary>
            public float SpanMin
            {
                get
                {
                    return m_SpanMin;
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
            }

            private float m_SpanMax = 0.0f;
            /// <summary>
            /// Span Min value
            /// </summary>
            public float SpanMax
            {
                get
                {
                    return m_SpanMax;
                }
            }

            /// <summary>
            /// Set the span values of the IEEG column
            /// </summary>
            /// <param name="min"></param>
            /// <param name="middle"></param>
            /// <param name="max"></param>
            public void SetSpanValues(float min, float mid, float max)
            {
                min = Mathf.Clamp(min, m_MinimumAmplitude, m_MaximumAmplitude);
                mid = Mathf.Clamp(mid, m_MinimumAmplitude, m_MaximumAmplitude);
                max = Mathf.Clamp(max, m_MinimumAmplitude, m_MaximumAmplitude);
                if (min > max) min = max;
                mid = Mathf.Clamp(mid, min, max);
                if (Mathf.Approximately(min, mid) && Mathf.Approximately(min, max) && Mathf.Approximately(mid, max))
                {
                    float amplitude = m_MaximumAmplitude - m_MinimumAmplitude;
                    float middle = Column.IEEGValuesForHistogram.Median();
                    mid = middle;
                    min = Mathf.Clamp(middle - 0.05f * amplitude, m_MinimumAmplitude, m_MaximumAmplitude);
                    max = Mathf.Clamp(middle + 0.05f * amplitude, m_MinimumAmplitude, m_MaximumAmplitude);
                }
                m_SpanMin = min;
                m_Middle = mid;
                m_SpanMax = max;
                OnUpdateSpanValues.Invoke();
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
        public int TimelineLength
        {
            get
            {
                return ColumnData.TimeLine.Lenght;
            }
        }
        public int SitesCount
        {
            get
            {
                return Sites.Count;
            }
        }
        public int[] EEGDimensions
        {
            get
            {
                return new int[] { TimelineLength, 1, SitesCount };
            }
        }
        public float[] IEEGValues = new float[0]; /**< amplitudes 1D array (to be sent to the DLL) */
        public float[] IEEGValuesForHistogram = new float[0];
        public float[][] IEEGValuesBySiteID;
        public string[] IEEGUnitsBySiteID = new string[0];

        //  plots
        private List<Vector3> m_ElectrodesSizeScale = null;  /**< scale of the plots of this column */
        private List<bool> m_ElectrodesPositiveColor = null; /**< is positive color ? */
        #endregion

        #region Events
        public UnityEvent OnUpdateCurrentTimelineID = new UnityEvent();
        #endregion

        #region Private Methods
        private void Update()
        {
            if (IsTimelinePlaying)
            {
                m_TimeSinceLastSample += Time.deltaTime;
                if (m_TimeSinceLastSample > TimelineInterval)
                {
                    CurrentTimeLineID++;
                    m_TimeSinceLastSample = 0.0f;
                    if (CurrentTimeLineID >= MaxTimeLineID && !IsTimelineLooping)
                    {
                        IsTimelinePlaying = false;
                        CurrentTimeLineID = 0;
                        ApplicationState.Module3D.OnStopTimelinePlay.Invoke();
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        public override void UpdateSites(PatientElectrodesList sites, List<GameObject> sitesPatientParent, List<GameObject> siteList)
        {
            base.UpdateSites(sites, sitesPatientParent, siteList);

            // plots
            m_ElectrodesSizeScale = new List<Vector3>(m_RawElectrodes.NumberOfSites);
            m_ElectrodesPositiveColor = new List<bool>(m_RawElectrodes.NumberOfSites);

            // masks
            for (int ii = 0; ii < m_RawElectrodes.NumberOfSites; ii++)
            {
                m_ElectrodesSizeScale.Add(new Vector3(1, 1, 1));
                m_ElectrodesPositiveColor.Add(true);
            }

            SetEEGData();
        }
        /// <summary>
        /// Load the visualization configuration from the loaded visualization
        /// </summary>
        public void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration(false);
            IEEGParameters.Gain = ColumnData.Configuration.Gain;
            IEEGParameters.MaximumInfluence = ColumnData.Configuration.MaximumInfluence;
            IEEGParameters.AlphaMin = ColumnData.Configuration.Alpha;
            IEEGParameters.SetSpanValues(ColumnData.Configuration.SpanMin, ColumnData.Configuration.Middle, ColumnData.Configuration.SpanMax);
            foreach (Data.Visualization.RegionOfInterest roi in ColumnData.Configuration.RegionsOfInterest)
            {
                ROI newROI = AddROI(roi.Name);
                foreach (Data.Visualization.Sphere sphere in roi.Spheres)
                {
                    newROI.AddBubble(Layer, "Bubble", sphere.Position.ToVector3(), sphere.Radius);
                }
            }
            foreach (Site site in Sites)
            {
                site.LoadConfiguration(false);
            }

            if (firstCall) ApplicationState.Module3D.OnRequestUpdateInUI.Invoke();
        }
        /// <summary>
        /// Save the current settings of this scene to the configuration of the linked visualization
        /// </summary>
        public void SaveConfiguration()
        {
            ColumnData.Configuration.Gain = IEEGParameters.Gain;
            ColumnData.Configuration.MaximumInfluence = IEEGParameters.MaximumInfluence;
            ColumnData.Configuration.Alpha = IEEGParameters.AlphaMin;
            ColumnData.Configuration.SpanMin = IEEGParameters.SpanMin;
            ColumnData.Configuration.Middle = IEEGParameters.Middle;
            ColumnData.Configuration.SpanMax = IEEGParameters.SpanMax;
            List<Data.Visualization.RegionOfInterest> rois = new List<Data.Visualization.RegionOfInterest>();
            foreach (ROI roi in m_ROIs)
            {
                rois.Add(new Data.Visualization.RegionOfInterest(roi));
            }
            ColumnData.Configuration.RegionsOfInterest = rois;
            foreach (Site site in Sites)
            {
                site.SaveConfiguration();
            }
        }
        /// <summary>
        /// Reset the settings of the loaded scene
        /// </summary>
        public void ResetConfiguration(bool firstCall = true)
        {
            IEEGParameters.Gain = 1.0f;
            IEEGParameters.MaximumInfluence = 15.0f;
            IEEGParameters.AlphaMin = 0.8f;
            IEEGParameters.SetSpanValues(0, 0, 0);
            while (m_ROIs.Count > 0)
            {
                RemoveSelectedROI();
            }
            foreach (Site site in Sites)
            {
                site.ResetConfiguration(false);
            }

            if (firstCall) ApplicationState.Module3D.OnRequestUpdateInUI.Invoke();
        }
        /// <summary>
        /// Update the site mask of the dll with all the masks
        /// </summary>
        public void UpdateDLLSitesMask()
        {
            bool isROI = m_ROIs.Count > 0; // (transform.parent.GetComponent<Base3DScene>().Type == SceneType.SinglePatient) ? false : (m_SelectedROI.NumberOfBubbles == 0);
            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                m_RawElectrodes.UpdateMask(ii, (Sites[ii].State.IsMasked || Sites[ii].State.IsBlackListed || Sites[ii].State.IsExcluded || (Sites[ii].State.IsOutOfROI && isROI)));
            }
        }
        /// <summary>
        /// Set EED Data for each site
        /// </summary>
        public void SetEEGData()
        {
            if (ColumnData == null) return;

            MinTimeLine = ColumnData.TimeLine.Start.Value;
            MaxTimeLine = ColumnData.TimeLine.End.Value;
            MaxTimeLineID = ColumnData.TimeLine.Lenght - 1;

            // Construct sites value array the old way, and set sites masks // maybe FIXME
            IEEGValuesBySiteID = new float[SitesCount][];
            IEEGUnitsBySiteID = new string[SitesCount];
            foreach (Site site in Sites)
            {
                Data.Visualization.SiteConfiguration siteConfiguration;
                if (ColumnData.Configuration.ConfigurationBySite.TryGetValue(site.Information.FullCorrectedID, out siteConfiguration))
                {
                    if (siteConfiguration.Values.Length > 0)
                    {
                        IEEGValuesBySiteID[site.Information.GlobalID] = siteConfiguration.NormalizedValues;
                        site.State.IsMasked = false; // update mask
                    }
                    else
                    {
                        IEEGValuesBySiteID[site.Information.GlobalID] = new float[TimelineLength];
                        site.State.IsMasked = true; // update mask
                    }
                    site.Configuration = siteConfiguration;
                    IEEGUnitsBySiteID[site.Information.GlobalID] = siteConfiguration.Unit;
                }
                else
                {
                    ColumnData.Configuration.ConfigurationBySite.Add(site.Information.FullCorrectedID, site.Configuration);
                    IEEGValuesBySiteID[site.Information.GlobalID] = new float[TimelineLength];
                    IEEGUnitsBySiteID[site.Information.GlobalID] = "";
                    site.State.IsMasked = true; // update mask
                }
            }

            IEEGParameters.MinimumAmplitude = float.MaxValue;
            IEEGParameters.MaximumAmplitude = float.MinValue;

            int length = TimelineLength * SitesCount;
            IEEGValues = new float[length];
            List<float> iEEGHistogramme = new List<float>();
            for (int s = 0; s < Sites.Count; ++s)
            {
                for (int t = 0; t < TimelineLength; ++t)
                {
                    float val = IEEGValuesBySiteID[s][t];
                    IEEGValues[t * SitesCount + s] = val;                   
                }
                if (!Sites[s].State.IsMasked)
                {
                    for (int t = 0; t < TimelineLength; ++t)
                    {
                        float val = IEEGValuesBySiteID[s][t];
                        iEEGHistogramme.Add(val);

                        //update min/ max values
                        if (val > IEEGParameters.MaximumAmplitude)
                            IEEGParameters.MaximumAmplitude = val;
                        else if (val < IEEGParameters.MinimumAmplitude)
                            IEEGParameters.MinimumAmplitude = val;
                    }
                }
            }
            IEEGValuesForHistogram = iEEGHistogramme.ToArray();
        }
        /// <summary>
        /// Specify a new columnData to be associated with the columnd3DView
        /// </summary>
        /// <param name="columnData"></param>
        public void SetColumnData(Data.Visualization.Column newColumnData)
        {
            ColumnData = newColumnData;
            m_IEEGParameters.Column = this;
            SetEEGData();
        }
        /// <summary>
        /// Update sites sizes and colors arrays for iEEG (to be called before the rendering update)
        /// </summary>
        public void UpdateSitesSizeAndColorForIEEG(SceneStatesInfo data)
        {
            UnityEngine.Profiling.Profiler.BeginSample("update_sites_size_and_color_arrays_for_IEEG");

            float diffMin = IEEGParameters.SpanMin - IEEGParameters.Middle;
            float diffMax = IEEGParameters.SpanMax - IEEGParameters.Middle;

            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                if ((Sites[ii].State.IsOutOfROI && !data.ShowAllSites) || Sites[ii].State.IsMasked)
                    continue;

                float value = IEEGValuesBySiteID[ii][CurrentTimeLineID];
                if (value < IEEGParameters.SpanMin)
                    value = IEEGParameters.SpanMin;
                if (value > IEEGParameters.SpanMax)
                    value = IEEGParameters.SpanMax;

                value -= IEEGParameters.Middle;

                if (value < 0)
                {
                    m_ElectrodesPositiveColor[ii] = false;
                    value = 0.5f + 2 * (value / diffMin);
                }
                else if (value > 0)
                {
                    m_ElectrodesPositiveColor[ii] = true;
                    value = 0.5f + 2 * (value / diffMax);
                }
                else
                {
                    m_ElectrodesPositiveColor[ii] = false;
                    value = 0.5f;
                }

                m_ElectrodesSizeScale[ii] = new Vector3(value, value, value);
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Update the plots rendering (iEEG or CCEP)
        /// </summary>
        public override void UpdateSitesRendering(SceneStatesInfo data, Latencies latenciesFile)
        {
            UpdateSitesSizeAndColorForIEEG(data);

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
                        if (SelectedSourceID == -1)
                        {
                            site.transform.localScale = Vector3.one;
                            siteType = latenciesFile.IsSiteASource(i) ? SiteType.Source : SiteType.NotASource;
                        }
                        else
                        {
                            if (i == SelectedSourceID)
                            {
                                site.transform.localScale = Vector3.one;
                                siteType = SiteType.Source;
                            }
                            else if (latenciesFile.IsSiteResponsiveForSource(i, SelectedSourceID))
                            {
                                siteType = latenciesFile.PositiveHeight[SelectedSourceID][i] ? SiteType.NonePos : SiteType.NoneNeg;
                                alpha = site.State.IsHighlighted ? 1.0f : latenciesFile.Transparencies[SelectedSourceID][i] - 0.25f;
                                site.transform.localScale = Vector3.one * latenciesFile.Sizes[SelectedSourceID][i];
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
                    site.transform.localScale *= IEEGParameters.Gain;
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
                    else if (data.IsGeneratorUpToDate)
                    {
                        site.transform.localScale = m_ElectrodesSizeScale[i];
                        siteType = m_ElectrodesPositiveColor[i] ? SiteType.Positive : SiteType.Negative;
                    }
                    else
                    {
                        site.transform.localScale = Vector3.one;
                        siteType = site.State.IsSuspicious ? SiteType.Suspicious : site.State.IsMarked ? SiteType.Marked : SiteType.Normal;
                    }
                    if (!activity) site.IsActive = true;
                    site.GetComponent<MeshRenderer>().sharedMaterial = SharedMaterials.SiteSharedMaterial(site.State.IsHighlighted, siteType);
                    site.transform.localScale *= IEEGParameters.Gain;
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
        /// <summary>
        /// Color the specified cut with IEEG
        /// </summary>
        /// <param name="cutID"></param>
        public void ColorCutsTexturesWithIEEG()
        {
            for (int i = 0; i < DLLMRITextureCutGenerators.Count; ++i)
            {
                MRITextureCutGenerator generator = DLLMRITextureCutGenerators[i];
                generator.FillTextureWithIEEG(this, DLLCutColorScheme);

                DLL.Texture cutTexture = DLLBrainCutTextures[i];
                generator.UpdateTextureWithIEEG(cutTexture);
                cutTexture.UpdateTexture2D(BrainCutTextures[i]);
            }
        }
        #endregion
    }
}
