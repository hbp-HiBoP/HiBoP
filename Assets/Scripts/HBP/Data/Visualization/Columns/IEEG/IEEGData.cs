using HBP.Data.Experience.Dataset;
using HBP.Data.Localizer;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;

namespace HBP.Data.Visualization
{
    public class IEEGData
    {
        #region Properties
        public IconicScenario IconicScenario { get; set; }
        public Timeline Timeline { get; set; }
        public Dictionary<Patient, BlocEventsStatistics> EventStatisticsByPatient { get; set; } = new Dictionary<Patient, BlocEventsStatistics>();
        public Dictionary<string, BlocChannelData> DataByChannel { get; set; } = new Dictionary<string, BlocChannelData>();
        public Dictionary<string, BlocChannelStatistics> StatisticsByChannel { get; set; } = new Dictionary<string, BlocChannelStatistics>();
        public Dictionary<string, float[]> ProcessedValuesByChannel { get; set; } = new Dictionary<string, float[]>();
        
        private Dictionary<string, Frequency> m_FrequencyByChannel = new Dictionary<string, Frequency>();
        public List<Frequency> Frequencies = new List<Frequency>();
        #endregion

        #region Public Methods
        public void Load(IEnumerable<DataInfo> columnData, Experience.Protocol.Bloc bloc)
        {
            foreach (DataInfo dataInfo in columnData)
            {
                Experience.Dataset.Data data = DataManager.GetData(dataInfo);
                // Values
                foreach (var channel in data.UnitByChannel.Keys)
                {
                    if (!DataByChannel.ContainsKey(channel)) DataByChannel.Add(channel, DataManager.GetData(dataInfo, bloc, channel));
                    if (!StatisticsByChannel.ContainsKey(channel)) StatisticsByChannel.Add(channel, DataManager.GetStatistics(dataInfo, bloc, channel));
                    if (!m_FrequencyByChannel.ContainsKey(channel)) m_FrequencyByChannel.Add(channel, data.Frequency);
                    if (!Frequencies.Contains(data.Frequency)) Frequencies.Add(data.Frequency);
                }
                // Events
                if (!EventStatisticsByPatient.ContainsKey(dataInfo.Patient)) EventStatisticsByPatient.Add(dataInfo.Patient, DataManager.GetEventsStatistics(dataInfo, bloc));
            }
        }
        public void Unload()
        {
            EventStatisticsByPatient.Clear();
            DataByChannel.Clear();
            StatisticsByChannel.Clear();
            m_FrequencyByChannel.Clear();
            Frequencies.Clear();
            ProcessedValuesByChannel.Clear();
            IconicScenario = null;
            Timeline = null;
        }
        public void SetTimeline(Frequency maxFrequency, Experience.Protocol.Bloc bloc)
        {
            Frequencies.Add(maxFrequency);
            Frequencies = Frequencies.GroupBy(f => f.Value).Select(g => g.First()).ToList();
            Dictionary<Experience.Protocol.SubBloc, List<SubBlocEventsStatistics>> eventStatisticsBySubBloc = new Dictionary<Experience.Protocol.SubBloc, List<SubBlocEventsStatistics>>();
            foreach (var subBloc in bloc.SubBlocs)
            {
                eventStatisticsBySubBloc.Add(subBloc, new List<SubBlocEventsStatistics>());
            }
            foreach (var blocEventStatistics in EventStatisticsByPatient.Values)
            {
                foreach (var subBlocEventStatistics in blocEventStatistics.EventsStatisticsBySubBloc)
                {
                    eventStatisticsBySubBloc[subBlocEventStatistics.Key].Add(subBlocEventStatistics.Value);
                }
            }
            Timeline = new Timeline(bloc, eventStatisticsBySubBloc, maxFrequency);
            IconicScenario = new IconicScenario(bloc, maxFrequency, Timeline);

            if (Frequencies.Count > 1)
            {
                foreach (var channel in DataByChannel.Keys)
                {
                    List<float> values = new List<float>();
                    Frequency frequency = m_FrequencyByChannel[channel];
                    BlocChannelStatistics statistics = StatisticsByChannel[channel];
                    foreach (var subBloc in bloc.SubBlocs.OrderBy(sb => sb.Order).ThenBy(sb => sb.Name))
                    {
                        float[] subBlocValues = statistics.Trial.ChannelSubTrialBySubBloc[subBloc].Values;
                        SubTimeline subTimeline = Timeline.SubTimelinesBySubBloc[subBloc];
                        // TODO : find number of values before and after for a better interpolation
                        values.AddRange(subBlocValues.Interpolate(subTimeline.Length, 0, 0));
                    }
                    ProcessedValuesByChannel.Add(channel, values.ToArray());
                }
            }
            else
            {
                foreach (var channel in DataByChannel.Keys)
                {
                    ProcessedValuesByChannel.Add(channel, StatisticsByChannel[channel].Trial.AllValues);
                }
            }

            //// Resampling taking frequencies into account
            //if (Frequencies.Count > 1)
            //{
            //    //int maxSize = (from siteConfiguration in Configuration.ConfigurationBySite.Values select siteConfiguration.Values).Max(v => v.Length);
            //    foreach (var siteConfiguration in Configuration.ConfigurationBySite.Values)
            //    {
            //        int frequency;
            //        if (m_FrequencyBySiteConfiguration.TryGetValue(siteConfiguration, out frequency))
            //        {
            //            Timeline timeline = TimeLineByFrequency[frequency];
            //            int samplesBefore = (int)((timeline.Start.RawValue - TimeLine.Start.RawValue) / TimeLine.Step);
            //            int samplesAfter = (int)((TimeLine.End.RawValue - timeline.End.RawValue) / TimeLine.Step);
            //            siteConfiguration.ResizeValues(TimeLine.Lenght, samplesBefore, samplesAfter);
            //        }
            //    }
            //}
        }
        /// <summary>
        /// Standardize column.
        /// </summary>
        /// <param name="before">sample before</param>
        /// <param name="after">sample after</param>
        public void Standardize(int before, int after)
        {
            //int diffBefore = before - TimeLine.MainEvent.Position;
            //int diffAfter = after - (TimeLine.Lenght - TimeLine.MainEvent.Position);
            //TimeLine.Resize(diffBefore, diffAfter);
            //foreach (var siteConfiguration in IEEGConfiguration.ConfigurationBySite.Values)
            //{
            //    siteConfiguration.Resize(diffBefore, diffAfter);
            //}
        }
        #endregion
    }
}

