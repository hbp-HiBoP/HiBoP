using HBP.Data.Enums;
using HBP.Data.Visualization;
using HBP.Module3D.DLL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    public class Column3DCCEP : Column3DDynamic
    {
        #region Properties
        /// <summary>
        /// Column data
        /// </summary>
        public CCEPColumn ColumnCCEPData
        {
            get
            {
                return ColumnData as Data.Visualization.CCEPColumn;
            }
        }

        public override Timeline Timeline
        {
            get
            {
                return ColumnCCEPData.Data.Timeline;
            }
        }

        private Site m_SelectedSource;
        public Site SelectedSource
        {
            get
            {
                return m_SelectedSource;
            }
            set
            {
                m_SelectedSource = value;
                SetEEGData();
            }
        }
        #endregion

        #region Private Methods
        protected override void Update()
        {
            if (Input.GetKeyDown(KeyCode.A)) SelectedSource = SelectedSite;
            base.Update();
        }
        /// <summary>
        /// Set EEG Data for each site
        /// </summary>
        protected override void SetEEGData()
        {
            if (ColumnCCEPData == null || m_SelectedSource == null) return;

            int timelineLength = Timeline.Length;
            int sitesCount = Sites.Count;
            // Construct sites value array the old way, and set sites masks // maybe FIXME
            IEEGValuesBySiteID = new float[sitesCount][];
            IEEGUnitsBySiteID = new string[sitesCount];
            int numberOfSitesWithValues = 0;

            // Retrieve values
            if (!ColumnCCEPData.Data.ProcessedValuesByChannelIDByStimulatedChannelID.TryGetValue(m_SelectedSource.Information.FullCorrectedID, out Dictionary<string, float[]> processedValuesByChannel)) return;
            if (!ColumnCCEPData.Data.UnityByChannelIDByStimulatedChannelID.TryGetValue(m_SelectedSource.Information.FullCorrectedID, out Dictionary<string, string> unitByChannel)) return;
            if (!ColumnCCEPData.Data.DataByChannelIDByStimulatedChannelID.TryGetValue(m_SelectedSource.Information.FullCorrectedID, out Dictionary<string, Data.Experience.Dataset.BlocChannelData> dataByChannel)) return;
            if (!ColumnCCEPData.Data.StatisticsByChannelIDByStimulatedChannelID.TryGetValue(m_SelectedSource.Information.FullCorrectedID, out Dictionary<string, Data.Experience.Dataset.BlocChannelStatistics> statisticsByChannel)) return;

            foreach (Site site in Sites)
            {
                if (processedValuesByChannel.TryGetValue(site.Information.FullCorrectedID, out float[] values))
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
                if (unitByChannel.TryGetValue(site.Information.FullCorrectedID, out string unit))
                {
                    IEEGUnitsBySiteID[site.Information.GlobalID] = unit;
                }
                else
                {
                    IEEGUnitsBySiteID[site.Information.GlobalID] = "";
                }
                if (dataByChannel.TryGetValue(site.Information.FullCorrectedID, out Data.Experience.Dataset.BlocChannelData blocChannelData))
                {
                    site.Data = blocChannelData;
                }
                if (statisticsByChannel.TryGetValue(site.Information.FullCorrectedID, out Data.Experience.Dataset.BlocChannelStatistics blocChannelStatistics))
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
                        if (val > DynamicParameters.MaximumAmplitude)
                            DynamicParameters.MaximumAmplitude = val;
                        else if (val < DynamicParameters.MinimumAmplitude)
                            DynamicParameters.MinimumAmplitude = val;
                    }
                }
            }
            IEEGValuesOfUnmaskedSites = iEEGNotMasked.ToArray();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Load the column configuration from the loaded column data
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another load method ?</param>
        public override void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration();
            DynamicParameters.Gain = ColumnCCEPData.DynamicConfiguration.Gain;
            DynamicParameters.InfluenceDistance = ColumnCCEPData.DynamicConfiguration.MaximumInfluence;
            DynamicParameters.AlphaMin = ColumnCCEPData.DynamicConfiguration.Alpha;
            DynamicParameters.SetSpanValues(ColumnCCEPData.DynamicConfiguration.SpanMin, ColumnCCEPData.DynamicConfiguration.Middle, ColumnCCEPData.DynamicConfiguration.SpanMax, this);
            base.LoadConfiguration(false);
        }
        /// <summary>
        /// Save the current settings of this column to the configuration of the linked column data
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
        /// Reset the settings of the loaded column
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another reset method ?</param>
        public override void ResetConfiguration()
        {
            DynamicParameters.Gain = 1.0f;
            DynamicParameters.InfluenceDistance = 15.0f;
            DynamicParameters.AlphaMin = 0.8f;
            DynamicParameters.SetSpanValues(0, 0, 0, this);
            base.ResetConfiguration();
        }
        #endregion
    }
}