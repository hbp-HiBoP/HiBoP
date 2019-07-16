using HBP.Data.Visualization;
using System.Collections.Generic;

namespace HBP.Module3D
{
    public class Column3DIEEG : Column3DDynamic
    {
        #region Properties
        /// <summary>
        /// Column data
        /// </summary>
        public IEEGColumn ColumnIEEGData
        {
            get
            {
                return ColumnData as IEEGColumn;
            }
        }

        public override Timeline Timeline
        {
            get
            {
                return ColumnIEEGData.Data.Timeline;
            }
        }
        #endregion

        #region Private Methods
        protected override void SetEEGData()
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
                if (ColumnIEEGData.Data.ProcessedValuesByChannel.TryGetValue(site.Information.FullCorrectedID, out float[] values))
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
                if (ColumnIEEGData.Data.UnitByChannelID.TryGetValue(site.Information.FullCorrectedID, out string unit))
                {
                    IEEGUnitsBySiteID[site.Information.GlobalID] = unit;
                }
                else
                {
                    IEEGUnitsBySiteID[site.Information.GlobalID] = "";
                }
                if (ColumnIEEGData.Data.DataByChannelID.TryGetValue(site.Information.FullCorrectedID, out Data.Experience.Dataset.BlocChannelData blocChannelData))
                {
                    site.Data = blocChannelData;
                }
                if (ColumnIEEGData.Data.StatisticsByChannelID.TryGetValue(site.Information.FullCorrectedID, out Data.Experience.Dataset.BlocChannelStatistics blocChannelStatistics))
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
            DynamicParameters.Gain = ColumnIEEGData.DynamicConfiguration.Gain;
            DynamicParameters.InfluenceDistance = ColumnIEEGData.DynamicConfiguration.MaximumInfluence;
            DynamicParameters.AlphaMin = ColumnIEEGData.DynamicConfiguration.Alpha;
            DynamicParameters.SetSpanValues(ColumnIEEGData.DynamicConfiguration.SpanMin, ColumnIEEGData.DynamicConfiguration.Middle, ColumnIEEGData.DynamicConfiguration.SpanMax);
            base.LoadConfiguration(false);
        }
        /// <summary>
        /// Save the current settings of this column to the configuration of the linked column data
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
        /// Reset the settings of the loaded column
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another reset method ?</param>
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