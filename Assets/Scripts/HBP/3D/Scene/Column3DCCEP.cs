using HBP.Data.Enums;
using HBP.Data.Visualization;
using HBP.Module3D.DLL;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    /// <summary>
    /// Class containing the CCEP data for the column
    /// </summary>
    public class Column3DCCEP : Column3DDynamic
    {
        #region Properties
        /// <summary>
        /// CCEP data of this column (contains information about what to display)
        /// </summary>
        public CCEPColumn ColumnCCEPData
        {
            get
            {
                return ColumnData as CCEPColumn;
            }
        }
        /// <summary>
        /// Timeline of this column (contains information about the length, the number of samples, the events etc.)
        /// </summary>
        public override Timeline Timeline
        {
            get
            {
                return ColumnCCEPData.Data.Timeline;
            }
        }

        public enum CCEPMode { Site, MarsAtlas }
        private CCEPMode m_Mode = CCEPMode.Site;
        /// <summary>
        /// Current mode of CCEP
        /// </summary>
        public CCEPMode Mode
        {
            get
            {
                return m_Mode;
            }
            set
            {
                m_Mode = value;
                m_SelectedSiteSource = null;
                m_SelectedSourceMarsAtlasLabel = -1;
                OnSelectSource.Invoke();
                SetActivityData();
            }
        }

        /// <summary>
        /// List of all possible sources for this column
        /// </summary>
        public List<Site> Sources { get; private set; } = new List<Site>();
        private Site m_SelectedSiteSource = null;
        /// <summary>
        /// Currently selected source site
        /// </summary>
        public Site SelectedSourceSite
        {
            get
            {
                return m_SelectedSiteSource;
            }
            set
            {
                if (m_SelectedSiteSource != value)
                {
                    m_SelectedSiteSource = value;
                    OnSelectSource.Invoke();
                    SetActivityData();
                }
            }
        }
        /// <summary>
        /// Is a source selected in this column ?
        /// </summary>
        public bool IsSourceSiteSelected
        {
            get
            {
                return m_SelectedSiteSource != null;
            }
        }

        private int m_SelectedSourceMarsAtlasLabel = -1;
        /// <summary>
        /// Currently selected source area
        /// </summary>
        public int SelectedSourceMarsAtlasLabel
        {
            get
            {
                return m_SelectedSourceMarsAtlasLabel;
            }
            set
            {
                if (m_SelectedSourceMarsAtlasLabel != value)
                {
                    m_SelectedSourceMarsAtlasLabel = value;
                    OnSelectSource.Invoke();
                    SetActivityData();
                }
            }
        }
        /// <summary>
        /// Is a source area selected in this column ?
        /// </summary>
        public bool IsSourceMarsAtlasLabelSelected
        {
            get
            {
                return m_SelectedSourceMarsAtlasLabel != -1;
            }
        }
        /// <summary>
        /// Mask for the mars atlas areas
        /// </summary>
        public int[] AreaMask { get; private set; }

        /// <summary>
        /// Is a source selected ? (site or area)
        /// </summary>
        public bool IsSourceSelected
        {
            get
            {
                return IsSourceSiteSelected || IsSourceMarsAtlasLabelSelected;
            }
        }

        /// <summary>
        /// Latencies of the first spike of each site for the selected source
        /// </summary>
        public float[] Latencies { get; private set; }
        /// <summary>
        /// Amplitudes of the first spike of each site for the selected source
        /// </summary>
        public float[] Amplitudes { get; private set; }
        #endregion

        #region Events
        /// <summary>
        /// Event called when selecting a source for this column
        /// </summary>
        public UnityEvent OnSelectSource = new UnityEvent();
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Set activity data for each site
        /// </summary>
        protected override void SetActivityData()
        {
            if (ColumnCCEPData == null) return;

            // Reset values
            int timelineLength = Timeline.Length;
            int sitesCount = Sites.Count;
            ActivityValuesBySiteID = new float[sitesCount][];
            for (int i = 0; i < sitesCount; i++)
            {
                ActivityValuesBySiteID[i] = new float[timelineLength];
            }
            ActivityUnitsBySiteID = new string[sitesCount];
            Latencies = new float[sitesCount];
            Amplitudes = new float[sitesCount];

            if (IsSourceSiteSelected)
            {
                SetActivityDataSourceSite();
            }
            else if (IsSourceMarsAtlasLabelSelected)
            {
                SetActivityDataSourceArea();
            }
            else
            {
                foreach (var site in Sites)
                {
                    site.State.IsMasked = false;
                }
            }
        }
        /// <summary>
        /// Update sites sizes and colors arrays depending on the activity (to be called before the rendering update)
        /// </summary>
        /// <param name="showAllSites">Display sites that are not in a ROI</param>
        protected override void UpdateSitesSizeAndColorOfSites(bool showAllSites)
        {
            if (IsSourceSiteSelected)
            {
                base.UpdateSitesSizeAndColorOfSites(showAllSites);
            }
        }
        private void SetActivityDataSourceSite()
        {
            int timelineLength = Timeline.Length;
            int sitesCount = Sites.Count;

            // Retrieve values
            if (!ColumnCCEPData.Data.ProcessedValuesByChannelIDByStimulatedChannelID.TryGetValue(SelectedSourceSite.Information.FullID, out Dictionary<string, float[]> processedValuesByChannel)) return;
            if (!ColumnCCEPData.Data.UnityByChannelIDByStimulatedChannelID.TryGetValue(SelectedSourceSite.Information.FullID, out Dictionary<string, string> unitByChannel)) return;
            if (!ColumnCCEPData.Data.DataByChannelIDByStimulatedChannelID.TryGetValue(SelectedSourceSite.Information.FullID, out Dictionary<string, Data.Experience.Dataset.BlocChannelData> dataByChannel)) return;
            if (!ColumnCCEPData.Data.StatisticsByChannelIDByStimulatedChannelID.TryGetValue(SelectedSourceSite.Information.FullID, out Dictionary<string, Data.Experience.Dataset.BlocChannelStatistics> statisticsByChannel)) return;

            int numberOfSitesWithValues = 0;
            foreach (Site site in Sites)
            {
                if (processedValuesByChannel.TryGetValue(site.Information.FullID, out float[] values))
                {
                    if (values.Length > 0)
                    {
                        numberOfSitesWithValues++;
                        ActivityValuesBySiteID[site.Information.Index] = values;
                        site.State.IsMasked = false;
                    }
                    else
                    {
                        ActivityValuesBySiteID[site.Information.Index] = new float[timelineLength];
                        site.State.IsMasked = true;
                    }
                }
                else
                {
                    ActivityValuesBySiteID[site.Information.Index] = new float[timelineLength];
                    site.State.IsMasked = true;
                }
                if (unitByChannel.TryGetValue(site.Information.FullID, out string unit))
                {
                    ActivityUnitsBySiteID[site.Information.Index] = unit;
                }
                else
                {
                    ActivityUnitsBySiteID[site.Information.Index] = "";
                }
                if (dataByChannel.TryGetValue(site.Information.FullID, out Data.Experience.Dataset.BlocChannelData blocChannelData))
                {
                    site.Data = blocChannelData;
                }
                if (statisticsByChannel.TryGetValue(site.Information.FullID, out Data.Experience.Dataset.BlocChannelStatistics blocChannelStatistics))
                {
                    site.Statistics = blocChannelStatistics;
                    Data.Experience.Dataset.ChannelSubTrialStat trial = blocChannelStatistics.Trial.ChannelSubTrialBySubBloc[ColumnCCEPData.Bloc.MainSubBloc];
                    SubTimeline mainSubTimeline = Timeline.SubTimelinesBySubBloc[ColumnCCEPData.Bloc.MainSubBloc];
                    for (int i = mainSubTimeline.StatisticsByEvent[ColumnCCEPData.Bloc.MainSubBloc.MainEvent].RoundedIndexFromStart + 2; i < mainSubTimeline.Length - 2; i++)
                    {
                        if (trial.Values[i - 1] > trial.Values[i - 2] && trial.Values[i] > trial.Values[i - 1] && trial.Values[i] > trial.Values[i + 1] && trial.Values[i + 1] > trial.Values[i + 2]) // Maybe FIXME: method to compute amplitude and latency
                        {
                            Amplitudes[site.Information.Index] = trial.Values[i];
                            Latencies[site.Information.Index] = mainSubTimeline.Frequency.ConvertNumberOfSamplesToMilliseconds(i - mainSubTimeline.StatisticsByEvent[ColumnCCEPData.Bloc.MainSubBloc.MainEvent].RoundedIndexFromStart);
                            break;
                        }
                    }
                }
            }
            if (numberOfSitesWithValues == 0)
            {
                throw new NoMatchingSitesException();
            }

            DynamicParameters.MinimumAmplitude = float.MaxValue;
            DynamicParameters.MaximumAmplitude = float.MinValue;

            int length = timelineLength * sitesCount;
            ActivityValues = new float[length];
            List<float> iEEGNotMasked = new List<float>();
            for (int s = 0; s < sitesCount; ++s)
            {
                for (int t = 0; t < timelineLength; ++t)
                {
                    float val = ActivityValuesBySiteID[s][t];
                    ActivityValues[t * sitesCount + s] = val;
                }
                if (!Sites[s].State.IsMasked)
                {
                    for (int t = 0; t < timelineLength; ++t)
                    {
                        float val = ActivityValuesBySiteID[s][t];
                        iEEGNotMasked.Add(val);

                        //update min/ max values
                        if (val > DynamicParameters.MaximumAmplitude)
                            DynamicParameters.MaximumAmplitude = val;
                        else if (val < DynamicParameters.MinimumAmplitude)
                            DynamicParameters.MinimumAmplitude = val;
                    }
                }
            }
            ActivityValuesOfUnmaskedSites = iEEGNotMasked.ToArray();
            DynamicParameters.ResetSpanValues(this);
        }
        private void SetActivityDataSourceArea()
        {
            int timelineLength = Timeline.Length;

            foreach (var site in Sites)
            {
                site.State.IsMasked = true;
            }

            Data.StringTag marsAtlasTag = ApplicationState.ProjectLoaded.Preferences.Tags.FirstOrDefault(t => t.Name == "MarsAtlas") as Data.StringTag;
            if (marsAtlasTag == null)
                throw new System.Exception("MarsAtlas tag has not been found !");

            int[] marsAtlasLabels = ApplicationState.Module3D.MarsAtlas.Labels();

            // Sort sites by mars atlas label
            Dictionary<int, List<Site>> sitesByMarsAtlasLabel = new Dictionary<int, List<Site>>();
            List<Data.StringTagValue> marsAtlasTagValues = Sites.Select(s => s.Information.SiteData.Tags.FirstOrDefault(t => t.Tag == marsAtlasTag) as Data.StringTagValue).ToList(); // FIXME: try perf with linq
            foreach (var label in marsAtlasLabels)
            {
                string labelName = string.Format("{0}_{1}", ApplicationState.Module3D.MarsAtlas.Hemisphere(label), ApplicationState.Module3D.MarsAtlas.Name(label));
                List<Site> sitesOfLabel = new List<Site>();
                for (int i = 0; i < Sites.Count; i++)
                {
                    Data.StringTagValue marsAtlasTagValue = marsAtlasTagValues[i];
                    if (marsAtlasTagValue != null && marsAtlasTagValue.Value == labelName)
                    {
                        sitesOfLabel.Add(Sites[i]);
                    }
                }
                sitesByMarsAtlasLabel.Add(label, sitesOfLabel);
            }

            // Get all values when sites are in the selected source area
            List<Site> sitesInSelectedSourceArea = sitesByMarsAtlasLabel[m_SelectedSourceMarsAtlasLabel];
            Dictionary<int, List<float[]>> valuesByMarsAtlasArea = new Dictionary<int, List<float[]>>();
            Dictionary<int, bool> maskByMarsAtlasArea = new Dictionary<int, bool>();
            foreach (var label in marsAtlasLabels)
            {
                valuesByMarsAtlasArea.Add(label, new List<float[]>());
            }
            foreach (var sourceSite in sitesInSelectedSourceArea)
            {
                // Retrieve values
                if (!ColumnCCEPData.Data.ProcessedValuesByChannelIDByStimulatedChannelID.TryGetValue(sourceSite.Information.FullID, out Dictionary<string, float[]> processedValuesByChannel)) continue;

                foreach (var label in marsAtlasLabels)
                {
                    List<float[]> valuesOfMarsAtlasArea = new List<float[]>();
                    if (label != m_SelectedSourceMarsAtlasLabel)
                    {
                        foreach (var site in sitesByMarsAtlasLabel[label])
                        {
                            if (processedValuesByChannel.TryGetValue(site.Information.FullID, out float[] values))
                            {
                                if (values.Length > 0)
                                {
                                    valuesOfMarsAtlasArea.Add(values);
                                }
                            }
                        }
                    }
                    valuesByMarsAtlasArea[label].AddRange(valuesOfMarsAtlasArea);
                }
            }
            foreach (var label in marsAtlasLabels)
            {
                maskByMarsAtlasArea.Add(label, valuesByMarsAtlasArea[label].Count == 0);
            }

            // Compute means, construct array of values and set mask
            Dictionary<int, float[]> activityByMarsAtlasArea = new Dictionary<int, float[]>();
            foreach (var label in marsAtlasLabels)
            {
                float[] result = new float[timelineLength];
                List<float[]> values = valuesByMarsAtlasArea[label];
                for (int i = 0; i < timelineLength; i++)
                {
                    result[i] = 0;
                    for (int j = 0; j < values.Count; j++)
                    {
                        result[i] += values[j][i];
                    }
                    if (values.Count != 0)
                        result[i] /= values.Count;
                }
                activityByMarsAtlasArea.Add(label, result);
            }
            DynamicParameters.MinimumAmplitude = float.MaxValue;
            DynamicParameters.MaximumAmplitude = float.MinValue;
            int highestLabel = marsAtlasLabels.Max();
            ActivityValues = new float[timelineLength * (highestLabel + 1)];
            AreaMask = new int[highestLabel + 1];
            List<float> unmaskedActivity = new List<float>();
            foreach(var label in marsAtlasLabels)
            {
                float[] activityOfArea = activityByMarsAtlasArea[label];
                bool isMasked = maskByMarsAtlasArea[label];
                AreaMask[label] = isMasked ? 1 : 0;
                for (int t = 0; t < timelineLength; ++t)
                {
                    float val = activityOfArea[t];
                    ActivityValues[label * timelineLength + t] = val;
                    if (!isMasked)
                    {
                        unmaskedActivity.Add(val);
                        if (val > DynamicParameters.MaximumAmplitude)
                            DynamicParameters.MaximumAmplitude = val;
                        else if (val < DynamicParameters.MinimumAmplitude)
                            DynamicParameters.MinimumAmplitude = val;
                    }
                }
            }
            ActivityValuesOfUnmaskedSites = unmaskedActivity.ToArray();
            DynamicParameters.ResetSpanValues(this);
            ApplicationState.Module3D.SelectedScene.AtlasManager.debug_atlas(AreaMask);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Update the sites of this column (when changing the implantation of the scene)
        /// </summary>
        /// <param name="implantation">Selected implantation</param>
        /// <param name="sceneSitePatientParent">List of the patient parent of the sites as instantiated in the scene</param>
        public override void UpdateSites(Implantation3D implantation, List<GameObject> sceneSitePatientParent)
        {
            base.UpdateSites(implantation, sceneSitePatientParent);
            Sources = Sites.Where(s => ColumnCCEPData.Data.ProcessedValuesByChannelIDByStimulatedChannelID.Keys.Contains(s.Information.FullID)).ToList();
        }
        /// <summary>
        /// Update the visibility, the size and the color of the sites depending on their state
        /// </summary>
        /// <param name="showAllSites">Do we show sites that are not in a ROI ?</param>
        /// <param name="hideBlacklistedSites">Do we hide blacklisted sites ?</param>
        /// <param name="isGeneratorUpToDate">Is the activity generator up to date ?</param>
        public override void UpdateSitesRendering(bool showAllSites, bool hideBlacklistedSites, bool isGeneratorUpToDate)
        {
            UpdateSitesSizeAndColorOfSites(showAllSites);

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
                else if (isGeneratorUpToDate)
                {
                    site.transform.localScale = m_ElectrodesSizeScale[i];
                    siteType = m_ElectrodesPositiveColor[i] ? SiteType.Positive : SiteType.Negative;
                }
                else if (!IsSourceSiteSelected)
                {
                    site.transform.localScale = Vector3.one;
                    siteType = ColumnCCEPData.Data.ProcessedValuesByChannelIDByStimulatedChannelID.ContainsKey(site.Information.FullID) ? SiteType.Source : SiteType.NotASource;
                }
                else
                {
                    site.transform.localScale = Vector3.one;
                    siteType = SiteType.Normal;
                }
                if (!activity) site.IsActive = true;
                site.GetComponent<MeshRenderer>().sharedMaterial = SharedMaterials.SiteSharedMaterial(site.State.IsHighlighted, siteType, site.State.Color);
                site.transform.localScale *= DynamicParameters.Gain;
            }
        }
        /// <summary>
        /// Load the column configuration from the column data
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another load method ?</param>
        public override void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration();
            DynamicParameters.Gain = ColumnCCEPData.DynamicConfiguration.Gain;
            DynamicParameters.InfluenceDistance = ColumnCCEPData.DynamicConfiguration.MaximumInfluence;
            DynamicParameters.AlphaMin = ColumnCCEPData.DynamicConfiguration.Alpha;
            DynamicParameters.SetSpanValues(ColumnCCEPData.DynamicConfiguration.SpanMin, ColumnCCEPData.DynamicConfiguration.Middle, ColumnCCEPData.DynamicConfiguration.SpanMax);
            base.LoadConfiguration(false);
        }
        /// <summary>
        /// Save the configuration of this column to the data column
        /// </summary>
        public override void SaveConfiguration()
        {
            ColumnCCEPData.DynamicConfiguration.Gain = DynamicParameters.Gain;
            ColumnCCEPData.DynamicConfiguration.MaximumInfluence = DynamicParameters.InfluenceDistance;
            ColumnCCEPData.DynamicConfiguration.Alpha = DynamicParameters.AlphaMin;
            ColumnCCEPData.DynamicConfiguration.SpanMin = DynamicParameters.SpanMin;
            ColumnCCEPData.DynamicConfiguration.Middle = DynamicParameters.Middle;
            ColumnCCEPData.DynamicConfiguration.SpanMax = DynamicParameters.SpanMax;
            base.SaveConfiguration();
        }
        /// <summary>
        /// Reset the configuration of this column
        /// </summary>
        public override void ResetConfiguration()
        {
            DynamicParameters.Gain = 1.0f;
            DynamicParameters.InfluenceDistance = 15.0f;
            DynamicParameters.AlphaMin = 0.8f;
            DynamicParameters.ResetSpanValues(this);
            base.ResetConfiguration();
        }
        #endregion
    }
}