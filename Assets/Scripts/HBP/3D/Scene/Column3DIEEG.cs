using System;
using System.Collections.Generic;
using Tools.CSharp;
using UnityEngine;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Core.Data;

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
        public Core.Data.IEEGColumn ColumnIEEGData
        {
            get
            {
                return ColumnData as Core.Data.IEEGColumn;
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
        public Dictionary<Core.Object3D.Site, Dictionary<Core.Object3D.Site, float>> CorrelationBySitePair { get; set; } = new Dictionary<Core.Object3D.Site, Dictionary<Core.Object3D.Site, float>>();
        /// <summary>
        /// Correlation between two sites
        /// </summary>
        public Dictionary<Core.Object3D.Site, Dictionary<Core.Object3D.Site, float>> CorrelationMeanBySitePair { get; set; } = new Dictionary<Core.Object3D.Site, Dictionary<Core.Object3D.Site, float>>();
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
            foreach (Core.Object3D.Site site in Sites)
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
                if (ColumnIEEGData.Data.DataByChannelID.TryGetValue(site.Information.FullID, out Core.Data.BlocChannelData blocChannelData))
                {
                    site.Data = blocChannelData;
                }
                if (ColumnIEEGData.Data.StatisticsByChannelID.TryGetValue(site.Information.FullID, out Core.Data.BlocChannelStatistics blocChannelStatistics))
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
        public override void UpdateSites(Core.Object3D.Implantation3D implantation, List<GameObject> sceneSitePatientParent)
        {
            base.UpdateSites(implantation, sceneSitePatientParent);

            CorrelationBySitePair.Clear();
            CorrelationMeanBySitePair.Clear();
        }
        /// <summary>
        /// Compute correlations for all site pairs
        /// </summary>
        public void ComputeCorrelations(Action<float, float, LoadingText> onChangeProgress = null)
        {
            CorrelationBySitePair.Clear();
            CorrelationMeanBySitePair.Clear();
            onChangeProgress?.Invoke(0, 0, new LoadingText("Computing correlations"));
            Dictionary<Core.Object3D.Site, List<double[]>> valuesByChannel = new Dictionary<Core.Object3D.Site, List<double[]>>();
            foreach (var site in Sites)
            {
                if (site.Data != null && !site.State.IsBlackListed)
                {
                    List<double[]> values = new List<double[]>();
                    for (int i = 0; i < site.Data.Trials.Length; ++i)
                    {
                        double[] arrayValues = new double[site.Data.Trials[i].Values.Length];
                        for (int j = 0; j < arrayValues.Length; ++j)
                        {
                            arrayValues[j] = site.Data.Trials[i].Values[j];
                        }
                        values.Add(arrayValues);
                    }
                    valuesByChannel.Add(site, values);
                }
            }
            int siteCount = valuesByChannel.Count;
            int progressCount = 0;
            foreach (var kv1 in valuesByChannel)
            {
                onChangeProgress?.Invoke((float)progressCount++ / siteCount, 0, new LoadingText("Computing correlations for ", string.Format("{0} in {1}", kv1.Key.Information.Name, Name)));
                Dictionary<Core.Object3D.Site, float> correlation = new Dictionary<Core.Object3D.Site, float>();
                Dictionary<Core.Object3D.Site, float> mean = new Dictionary<Core.Object3D.Site, float>();
                int numberOfTrials = kv1.Value.Count;

                foreach (var kv2 in valuesByChannel)
                {
                    if (kv1.Key == kv2.Key) continue;
                    if (kv2.Value.Count != numberOfTrials) continue;

                    double[] blackData = new double[numberOfTrials];
                    double[] greyData = new double[numberOfTrials * (numberOfTrials - 1)];

                    int count = 0;
                    for (int i = 0; i < numberOfTrials; ++i)
                        for (int j = 0; j < numberOfTrials; ++j)
                            if (i == j)
                                blackData[i] = MathDLL.Pearson(kv1.Value[i], kv2.Value[i]);
                            else
                                greyData[count++] = MathDLL.Pearson(kv1.Value[i], kv2.Value[j]);

                    correlation.Add(kv2.Key, (float)MathDLL.WilcoxonRankSum(blackData, greyData));
                    mean.Add(kv2.Key, (float)blackData.Mean());
                }
                CorrelationBySitePair.Add(kv1.Key, correlation);
                CorrelationMeanBySitePair.Add(kv1.Key, mean);
            }
        }
        /// <summary>
        /// Which sites are correlated to the input one ?
        /// </summary>
        /// <param name="siteA">Input site to search for correlated sites</param>
        /// <returns>List of correlated sites</returns>
        public List<Core.Object3D.Site> CorrelatedSites(Core.Object3D.Site site)
        {
            int siteCount = CorrelationBySitePair.Count;
            List<Core.Object3D.Site> result = new List<Core.Object3D.Site>();
            if (AreCorrelationsComputed)
            {
                if (CorrelationBySitePair.TryGetValue(site, out Dictionary<Core.Object3D.Site, float> correlationBySite))
                {
                    foreach (var s in Sites)
                    {
                        if (correlationBySite.TryGetValue(s, out float correlationValue))
                        {
                            float threshold = ApplicationState.UserPreferences.Data.EEG.CorrelationAlpha;
                            if (ApplicationState.UserPreferences.Data.EEG.BonferroniCorrection) threshold /= siteCount * (siteCount - 1) / 2;
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