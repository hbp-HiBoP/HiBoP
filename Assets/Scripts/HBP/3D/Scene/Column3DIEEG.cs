using CielaSpike;
using HBP.Data.Visualization;
using System;
using System.Collections.Generic;
using Tools.CSharp;
using UnityEngine;

namespace HBP.Module3D
{
    /// <summary>
    /// Class containing the iEEG data for the column
    /// </summary>
    public class Column3DIEEG : Column3DDynamic
    {
        #region Properties
        /// <summary>
        /// IEEG data of this column (contains information about what to display)
        /// </summary>
        public IEEGColumn ColumnIEEGData
        {
            get
            {
                return ColumnData as IEEGColumn;
            }
        }
        /// <summary>
        /// Timeline of this column (contains information about the length, the number of samples, the events etc.)
        /// </summary>
        public override Timeline Timeline
        {
            get
            {
                return ColumnIEEGData.Data.Timeline;
            }
        }
        /// <summary>
        /// Correlation between two sites
        /// </summary>
        public Dictionary<Site, Dictionary<Site, float>> CorrelationBySitePair { get; set; } = new Dictionary<Site, Dictionary<Site, float>>();
        /// <summary>
        /// Are the correlations between site pairs computed ?
        /// </summary>
        public bool AreCorrelationsComputed { get { return CorrelationBySitePair.Count > 0; } }
        #endregion

        #region Private Methods
        /// <summary>
        /// Set activity data for each site
        /// </summary>
        protected override void SetActivityData()
        {
            if (ColumnIEEGData == null) return;

            int timelineLength = Timeline.Length;
            int sitesCount = Sites.Count;
            ActivityValuesBySiteID = new float[sitesCount][];
            ActivityUnitsBySiteID = new string[sitesCount];
            int numberOfSitesWithValues = 0;
            foreach (Site site in Sites)
            {
                if (ColumnIEEGData.Data.ProcessedValuesByChannel.TryGetValue(site.Information.FullID, out float[] values))
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
                if (ColumnIEEGData.Data.UnitByChannelID.TryGetValue(site.Information.FullID, out string unit))
                {
                    ActivityUnitsBySiteID[site.Information.Index] = unit;
                }
                else
                {
                    ActivityUnitsBySiteID[site.Information.Index] = "";
                }
                if (ColumnIEEGData.Data.DataByChannelID.TryGetValue(site.Information.FullID, out Data.Experience.Dataset.BlocChannelData blocChannelData))
                {
                    site.Data = blocChannelData;
                }
                if (ColumnIEEGData.Data.StatisticsByChannelID.TryGetValue(site.Information.FullID, out Data.Experience.Dataset.BlocChannelStatistics blocChannelStatistics))
                {
                    site.Statistics = blocChannelStatistics;
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

                        if (val > DynamicParameters.MaximumAmplitude)
                            DynamicParameters.MaximumAmplitude = val;
                        else if (val < DynamicParameters.MinimumAmplitude)
                            DynamicParameters.MinimumAmplitude = val;
                    }
                }
            }
            ActivityValuesOfUnmaskedSites = iEEGNotMasked.ToArray();
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

            CorrelationBySitePair.Clear();
        }
        /// <summary>
        /// Compute correlations for all site pairs
        /// </summary>
        public void ComputeCorrelations(Action<float, float, LoadingText> onChangeProgress = null)
        {
            CorrelationBySitePair.Clear();
            onChangeProgress?.Invoke(0, 0, new LoadingText("Computing correlations"));
            Dictionary<Site, List<float[]>> valuesByChannel = new Dictionary<Site, List<float[]>>();
            foreach (var site in Sites)
            {
                if (site.Data != null)
                {
                    List<float[]> values = new List<float[]>();
                    for (int i = 0; i < site.Data.Trials.Length; i++)
                    {
                        values.Add(site.Data.Trials[i].Values);
                    }
                    valuesByChannel.Add(site, values);
                }
            }
            int siteCount = valuesByChannel.Count;
            int progressCount = 0;
            foreach (var kv1 in valuesByChannel)
            {
                onChangeProgress?.Invoke((float)progressCount++ / siteCount, 0, new LoadingText("Computing correlations for ", string.Format("{0} in {1}", kv1.Key.Information.Name, Name)));
                Dictionary<Site, float> correlation = new Dictionary<Site, float>();
                int numberOfTrials = kv1.Value.Count;

                foreach (var kv2 in valuesByChannel)
                {
                    if (kv1.Key == kv2.Key) continue;
                    if (kv2.Value.Count != numberOfTrials) continue;

                    float[] blackData = new float[numberOfTrials];
                    float[] greyData = new float[numberOfTrials * (numberOfTrials - 1)];
                    int count = 0;
                    for (int i = 0; i < numberOfTrials; i++)
                    {
                        for (int j = 0; j < numberOfTrials; j++)
                        {
                            if (i == j)
                            {
                                blackData[i] = MathDLL.Pearson(kv1.Value[i], kv2.Value[i]);
                            }
                            else
                            {
                                greyData[count++] = MathDLL.Pearson(kv1.Value[i], kv2.Value[j]);
                            }
                        }
                    }
                    correlation.Add(kv2.Key, MathDLL.WilcoxonRankSum(blackData, greyData));
                }
                CorrelationBySitePair.Add(kv1.Key, correlation);
            }
        }
        /// <summary>
        /// Which sites are correlated to the input one ?
        /// </summary>
        /// <param name="siteA">Input site to search for correlated sites</param>
        /// <returns>List of correlated sites</returns>
        public List<Site> CorrelatedSites(Site site)
        {
            List<Site> result = new List<Site>();
            if (AreCorrelationsComputed)
            {
                if (CorrelationBySitePair.TryGetValue(site, out Dictionary<Site, float> correlationBySite))
                {
                    foreach (var s in Sites)
                    {
                        if (correlationBySite.TryGetValue(s, out float correlationValue))
                        {
                            float threshold = ApplicationState.UserPreferences.Data.EEG.CorrelationAlpha;
                            if (ApplicationState.UserPreferences.Data.EEG.BonferroniCorrection) threshold /= (Sites.Count * (Sites.Count - 1) / 2);
                            if (correlationValue < threshold)
                            {
                                result.Add(s);
                            }
                        }
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// Load the column configuration from the column data
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another load method ?</param>
        public override void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration();
            DynamicParameters.InfluenceDistance = ColumnIEEGData.DynamicConfiguration.MaximumInfluence;
            DynamicParameters.SetSpanValues(ColumnIEEGData.DynamicConfiguration.SpanMin, ColumnIEEGData.DynamicConfiguration.Middle, ColumnIEEGData.DynamicConfiguration.SpanMax);
            base.LoadConfiguration(false);
        }
        /// <summary>
        /// Save the configuration of this column to the data column
        /// </summary>
        public override void SaveConfiguration()
        {
            ColumnIEEGData.DynamicConfiguration.MaximumInfluence = DynamicParameters.InfluenceDistance;
            ColumnIEEGData.DynamicConfiguration.SpanMin = DynamicParameters.SpanMin;
            ColumnIEEGData.DynamicConfiguration.Middle = DynamicParameters.Middle;
            ColumnIEEGData.DynamicConfiguration.SpanMax = DynamicParameters.SpanMax;
            base.SaveConfiguration();
        }
        /// <summary>
        /// Reset the configuration of this column
        /// </summary>
        public override void ResetConfiguration()
        {
            DynamicParameters.InfluenceDistance = 15.0f;
            DynamicParameters.ResetSpanValues(this);
            base.ResetConfiguration();
        }
        #endregion
    }
}