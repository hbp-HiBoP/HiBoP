using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using HBP.Module3D.DLL;
using HBP.Data.Enums;
using System.Linq;

namespace HBP.Module3D
{
    /// <summary>
    /// Column 3D iEEG class (functional iEEG data)
    /// </summary>
    public class Column3DIEEG : Column3D, IConfigurable
    {
        #region Properties
        /// <summary>
        /// Type of the column
        /// </summary>
        public override ColumnType Type
        {
            get
            {
                return ColumnType.iEEG;
            }
        }
        /// <summary>
        /// Column data
        /// </summary>
        public Data.Visualization.IEEGColumn ColumnIEEGData
        {
            get
            {
                return ColumnData as Data.Visualization.IEEGColumn;
            }
        }
        /// <summary>
        /// Timeline of this column
        /// </summary>
        public Data.Visualization.Timeline Timeline
        {
            get
            {
                return ColumnIEEGData.Data.Timeline;
            }
        }

        /// <summary>
        /// Shared minimum influence (for iEEG on the surface)
        /// </summary>
        public float SharedMinInf = 0f;
        /// <summary>
        /// Shared maximum influence (for iEEG of the surface)
        /// </summary>
        public float SharedMaxInf = 0f;

        /// <summary>
        /// IEEG Parameters
        /// </summary>
        public IEEGDataParameters IEEGParameters { get; } = new IEEGDataParameters();

        /// <summary>
        /// Dimensions of the EEG array (used for the DLL)
        /// </summary>
        public int[] EEGDimensions
        {
            get
            {
                return new int[] { Timeline.Length, 1, Sites.Count };
            }
        }
        /// <summary>
        /// Values of the iEEG signal in a 1D array (used for the DLL)
        /// </summary>
        public float[] IEEGValues = new float[0];
        /// <summary>
        /// Values of the iEEG signal of the sites that are not masked (and have correct values)
        /// </summary>
        public float[] IEEGValuesOfUnmaskedSites = new float[0];
        /// <summary>
        /// iEEG values by site global ID
        /// </summary>
        public float[][] IEEGValuesBySiteID;
        /// <summary>
        /// Units of the iEEG values of each site
        /// </summary>
        public string[] IEEGUnitsBySiteID = new string[0];

        /// <summary>
        /// Size of each site
        /// </summary>
        private List<Vector3> m_ElectrodesSizeScale;
        /// <summary>
        /// Does the site have a positive value ?
        /// </summary>
        private List<bool> m_ElectrodesPositiveColor;
        #endregion

        #region Events
        /// <summary>
        /// Events to send the computed values for the colormap of this column
        /// </summary>
        [HideInInspector] public GenericEvent<float, float, float> OnSendColorMapValues = new GenericEvent<float, float, float>();
        /// <summary>
        /// Event called when updating the current timeline ID
        /// </summary>
        [HideInInspector] public UnityEvent OnUpdateCurrentTimelineID = new UnityEvent();
        #endregion

        #region Private Methods
        private void Update()
        {
            if (Timeline != null)
            {
                Timeline.Play();
            }
        }
        /// <summary>
        /// Set EEG Data for each site
        /// </summary>
        private void SetEEGData()
        {
            if (ColumnIEEGData == null) return;

            int timelineLength = Timeline.Length;
            int sitesCount = Sites.Count;
            // Construct sites value array the old way, and set sites masks // maybe FIXME
            IEEGValuesBySiteID = new float[sitesCount][];
            IEEGUnitsBySiteID = new string[sitesCount];
            int numberOfSitesWithValues = 0;
            foreach (Site site in Sites)
            {
                float[] values;
                if (ColumnIEEGData.Data.ProcessedValuesByChannel.TryGetValue(site.Information.FullCorrectedID, out values))
                {
                    if (values.Length > 0)
                    {
                        numberOfSitesWithValues++;
                        IEEGValuesBySiteID[site.Information.GlobalID] = values;
                        site.State.IsMasked = false;
                    }
                    else
                    {
                        IEEGValuesBySiteID[site.Information.GlobalID] = new float[timelineLength];
                        site.State.IsMasked = true;
                    }
                }
                else
                {
                    IEEGValuesBySiteID[site.Information.GlobalID] = new float[timelineLength];
                    site.State.IsMasked = true;
                }
                string unit;
                if (ColumnIEEGData.Data.UnitByChannel.TryGetValue(site.Information.FullCorrectedID, out unit))
                {
                    IEEGUnitsBySiteID[site.Information.GlobalID] = unit;
                }
                else
                {
                    IEEGUnitsBySiteID[site.Information.GlobalID] = "";
                }
                Data.Experience.Dataset.BlocChannelData blocChannelData;
                if (ColumnIEEGData.Data.DataByChannelID.TryGetValue(site.Information.FullCorrectedID, out blocChannelData))
                {
                    site.Data = blocChannelData;
                }
                Data.Experience.Dataset.BlocChannelStatistics blocChannelStatistics;
                if (ColumnIEEGData.Data.StatisticsByChannelID.TryGetValue(site.Information.FullCorrectedID, out blocChannelStatistics))
                {
                    site.Statistics = blocChannelStatistics;
                }
            }
            if (numberOfSitesWithValues == 0)
            {
                throw new NoMatchingSitesException();
            }

            IEEGParameters.MinimumAmplitude = float.MaxValue;
            IEEGParameters.MaximumAmplitude = float.MinValue;

            int length = timelineLength * sitesCount;
            IEEGValues = new float[length];
            List<float> iEEGNotMasked = new List<float>();
            for (int s = 0; s < sitesCount; ++s)
            {
                for (int t = 0; t < timelineLength; ++t)
                {
                    float val = IEEGValuesBySiteID[s][t];
                    IEEGValues[t * sitesCount + s] = val;
                }
                if (!Sites[s].State.IsMasked)
                {
                    for (int t = 0; t < timelineLength; ++t)
                    {
                        float val = IEEGValuesBySiteID[s][t];
                        iEEGNotMasked.Add(val);

                        //update min/ max values
                        if (val > IEEGParameters.MaximumAmplitude)
                            IEEGParameters.MaximumAmplitude = val;
                        else if (val < IEEGParameters.MinimumAmplitude)
                            IEEGParameters.MinimumAmplitude = val;
                    }
                }
            }
            IEEGValuesOfUnmaskedSites = iEEGNotMasked.ToArray();
        }
        /// <summary>
        /// Update sites sizes and colors arrays for iEEG (to be called before the rendering update)
        /// </summary>
        /// <param name="data">Information about the scene</param>
        private void UpdateSitesSizeAndColorForIEEG(SceneStatesInfo data)
        {
            UnityEngine.Profiling.Profiler.BeginSample("update_sites_size_and_color_arrays_for_IEEG");

            float diffMin = IEEGParameters.SpanMin - IEEGParameters.Middle;
            float diffMax = IEEGParameters.SpanMax - IEEGParameters.Middle;

            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                if ((Sites[ii].State.IsOutOfROI && !data.ShowAllSites) || Sites[ii].State.IsMasked)
                    continue;

                float value = IEEGValuesBySiteID[ii][Timeline.CurrentIndex];
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
        #endregion

        #region Public Methods
        /// <summary>
        /// Send colormap values to the overlay
        /// </summary>
        public void SendColormapValues()
        {
            OnSendColorMapValues.Invoke(IEEGParameters.SpanMin, IEEGParameters.Middle, IEEGParameters.SpanMax);
        }
        /// <summary>
        /// Update the implantation for this column
        /// </summary>
        /// <param name="sites">List of the sites (DLL)</param>
        /// <param name="sitesPatientParent">List of the gameobjects for the sites corresponding to the patients</param>
        /// <param name="siteList">List of the sites gameobjects</param>
        public override void UpdateSites(PatientElectrodesList sites, List<GameObject> sitesPatientParent, List<GameObject> siteList)
        {
            base.UpdateSites(sites, sitesPatientParent, siteList);
            
            m_ElectrodesSizeScale = new List<Vector3>(m_RawElectrodes.NumberOfSites);
            m_ElectrodesPositiveColor = new List<bool>(m_RawElectrodes.NumberOfSites);
            
            for (int ii = 0; ii < m_RawElectrodes.NumberOfSites; ii++)
            {
                m_ElectrodesSizeScale.Add(new Vector3(1, 1, 1));
                m_ElectrodesPositiveColor.Add(true);
            }

            SetEEGData();
        }
        /// <summary>
        /// Load the column configuration from the loaded column data
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another load method ?</param>
        public void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration(false);
            IEEGParameters.Gain = ColumnIEEGData.IEEGConfiguration.Gain;
            IEEGParameters.InfluenceDistance = ColumnIEEGData.IEEGConfiguration.MaximumInfluence;
            IEEGParameters.AlphaMin = ColumnIEEGData.IEEGConfiguration.Alpha;
            IEEGParameters.SetSpanValues(ColumnIEEGData.IEEGConfiguration.SpanMin, ColumnIEEGData.IEEGConfiguration.Middle, ColumnIEEGData.IEEGConfiguration.SpanMax, this);
            foreach (Data.Visualization.RegionOfInterest roi in ColumnIEEGData.BaseConfiguration.RegionsOfInterest)
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

            if (firstCall) ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Save the current settings of this column to the configuration of the linked column data
        /// </summary>
        public void SaveConfiguration()
        {
            ColumnIEEGData.IEEGConfiguration.Gain = IEEGParameters.Gain;
            ColumnIEEGData.IEEGConfiguration.MaximumInfluence = IEEGParameters.InfluenceDistance;
            ColumnIEEGData.IEEGConfiguration.Alpha = IEEGParameters.AlphaMin;
            ColumnIEEGData.IEEGConfiguration.SpanMin = IEEGParameters.SpanMin;
            ColumnIEEGData.IEEGConfiguration.Middle = IEEGParameters.Middle;
            ColumnIEEGData.IEEGConfiguration.SpanMax = IEEGParameters.SpanMax;
            List<Data.Visualization.RegionOfInterest> rois = new List<Data.Visualization.RegionOfInterest>();
            foreach (ROI roi in m_ROIs)
            {
                rois.Add(new Data.Visualization.RegionOfInterest(roi));
            }
            ColumnIEEGData.BaseConfiguration.RegionsOfInterest = rois;
            foreach (Site site in Sites)
            {
                site.SaveConfiguration();
            }
        }
        /// <summary>
        /// Reset the settings of the loaded column
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another reset method ?</param>
        public void ResetConfiguration(bool firstCall = true)
        {
            IEEGParameters.Gain = 1.0f;
            IEEGParameters.InfluenceDistance = 15.0f;
            IEEGParameters.AlphaMin = 0.8f;
            IEEGParameters.SetSpanValues(0, 0, 0, this);
            while (m_ROIs.Count > 0)
            {
                RemoveSelectedROI();
            }
            foreach (Site site in Sites)
            {
                site.ResetConfiguration(false);
            }

            if (firstCall) ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
        }
        /// <summary>
        /// Update the site mask of the dll with all the masks
        /// </summary>
        public void UpdateDLLSitesMask()
        {
            bool isROI = m_ROIs.Count > 0;
            for (int ii = 0; ii < Sites.Count; ++ii)
            {
                m_RawElectrodes.UpdateMask(ii, (Sites[ii].State.IsMasked || Sites[ii].State.IsBlackListed || Sites[ii].State.IsExcluded || (Sites[ii].State.IsOutOfROI && isROI)));
            }
        }
        /// <summary>
        /// Specify a column data for this column
        /// </summary>
        /// <param name="columnData">Column data to use</param>
        public void ComputeEEGData()
        {
            Timeline.OnUpdateCurrentIndex.AddListener(() =>
            {
                OnUpdateCurrentTimelineID.Invoke();
                if (IsSelected)
                {
                    ApplicationState.Module3D.OnUpdateSelectedColumnTimeLineID.Invoke();
                }
            });
            SetEEGData();
        }
        /// <summary>
        /// Update the shape and color of the sites of this column
        /// </summary>
        /// <param name="data">Information about the scene</param>
        /// <param name="latenciesFile">CCEP files</param>
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
                    else if (site.State.IsSuspicious)
                    {
                        site.transform.localScale = Vector3.one;
                        siteType = SiteType.Suspicious;
                    }
                    else if (data.IsGeneratorUpToDate)
                    {
                        site.transform.localScale = m_ElectrodesSizeScale[i];
                        siteType = m_ElectrodesPositiveColor[i] ? SiteType.Positive : SiteType.Negative;
                    }
                    else
                    {
                        site.transform.localScale = Vector3.one;
                        siteType = site.State.IsMarked ? SiteType.Marked : SiteType.Normal;
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
        #endregion
    }
}
