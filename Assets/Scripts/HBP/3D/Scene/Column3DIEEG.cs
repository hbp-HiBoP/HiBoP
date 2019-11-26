using HBP.Data.Visualization;
using System.Collections.Generic;

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
        /// Load the column configuration from the column data
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another load method ?</param>
        public override void LoadConfiguration(bool firstCall = true)
        {
            if (firstCall) ResetConfiguration();
            DynamicParameters.Gain = ColumnIEEGData.DynamicConfiguration.Gain;
            DynamicParameters.InfluenceDistance = ColumnIEEGData.DynamicConfiguration.MaximumInfluence;
            DynamicParameters.AlphaMin = ColumnIEEGData.DynamicConfiguration.Alpha;
            DynamicParameters.SetSpanValues(ColumnIEEGData.DynamicConfiguration.SpanMin, ColumnIEEGData.DynamicConfiguration.Middle, ColumnIEEGData.DynamicConfiguration.SpanMax);
            base.LoadConfiguration(false);
        }
        /// <summary>
        /// Save the configuration of this column to the data column
        /// </summary>
        public override void SaveConfiguration()
        {
            ColumnIEEGData.DynamicConfiguration.Gain = DynamicParameters.Gain;
            ColumnIEEGData.DynamicConfiguration.MaximumInfluence = DynamicParameters.InfluenceDistance;
            ColumnIEEGData.DynamicConfiguration.Alpha = DynamicParameters.AlphaMin;
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
            DynamicParameters.Gain = 1.0f;
            DynamicParameters.InfluenceDistance = 15.0f;
            DynamicParameters.AlphaMin = 0.8f;
            DynamicParameters.ResetSpanValues(this);
            base.ResetConfiguration();
        }
        #endregion
    }
}