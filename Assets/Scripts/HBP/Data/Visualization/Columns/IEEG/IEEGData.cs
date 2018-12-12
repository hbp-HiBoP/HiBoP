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
        public Dictionary<string, BlocChannelData> DataByChannelID { get; set; } = new Dictionary<string, BlocChannelData>();
        public Dictionary<string, BlocChannelStatistics> StatisticsByChannelID { get; set; } = new Dictionary<string, BlocChannelStatistics>();
        public Dictionary<string, float[]> ProcessedValuesByChannel { get; set; } = new Dictionary<string, float[]>();
        public Dictionary<string, string> UnitByChannel { get; set; } = new Dictionary<string, string>();
        
        private Dictionary<string, Frequency> m_FrequencyByChannelID = new Dictionary<string, Frequency>();
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
                    string channelID = dataInfo.Patient.ID + "_" + channel;
                    if (!DataByChannelID.ContainsKey(channelID)) DataByChannelID.Add(channelID, DataManager.GetData(dataInfo, bloc, channel));
                    if (!StatisticsByChannelID.ContainsKey(channelID)) StatisticsByChannelID.Add(channelID, DataManager.GetStatistics(dataInfo, bloc, channel));
                    if (!m_FrequencyByChannelID.ContainsKey(channelID)) m_FrequencyByChannelID.Add(channelID, data.Frequency);
                    if (!UnitByChannel.ContainsKey(channelID)) UnitByChannel.Add(channelID, data.UnitByChannel[channel]);
                    if (!Frequencies.Contains(data.Frequency)) Frequencies.Add(data.Frequency);
                }
                // Events
                if (!EventStatisticsByPatient.ContainsKey(dataInfo.Patient)) EventStatisticsByPatient.Add(dataInfo.Patient, DataManager.GetEventsStatistics(dataInfo, bloc));
            }
        }
        public void Unload()
        {
            EventStatisticsByPatient.Clear();
            DataByChannelID.Clear();
            StatisticsByChannelID.Clear();
            m_FrequencyByChannelID.Clear();
            Frequencies.Clear();
            ProcessedValuesByChannel.Clear();
            IconicScenario = null;
            Timeline = null;
        }
        public void SetTimeline(Frequency maxFrequency, Experience.Protocol.Bloc columnBloc, IEnumerable<Experience.Protocol.Bloc> blocs)
        {
            // Process frequencies
            Frequencies.Add(maxFrequency);
            Frequencies = Frequencies.GroupBy(f => f.Value).Select(g => g.First()).ToList();

            // Get index of each subBloc
            Dictionary<Experience.Protocol.SubBloc, int> indexBySubBloc = new Dictionary<Experience.Protocol.SubBloc, int>();
            foreach (var bloc in blocs)
            {
                int mainSubBlocPosition = bloc.MainSubBlocPosition;
                Experience.Protocol.SubBloc[] subBlocs = bloc.OrderedSubBlocs.ToArray();
                for (int i = 0; i < subBlocs.Length; ++i)
                {
                    indexBySubBloc.Add(subBlocs[i], i - mainSubBlocPosition);
                }
            }

            // Get all eventStatistics for each SubBloc of the column
            Dictionary<Experience.Protocol.SubBloc, List<SubBlocEventsStatistics>> eventStatisticsBySubBloc = new Dictionary<Experience.Protocol.SubBloc, List<SubBlocEventsStatistics>>();
            foreach (var subBloc in columnBloc.SubBlocs)
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

            // Create timeline and iconic scenario
            Timeline = new Timeline(columnBloc, eventStatisticsBySubBloc, indexBySubBloc, maxFrequency);
            IconicScenario = new IconicScenario(columnBloc, maxFrequency, Timeline);

            // Standardize values
            foreach (var channelID in DataByChannelID.Keys)
            {
                List<float> values = new List<float>();
                Frequency frequency = m_FrequencyByChannelID[channelID];
                BlocChannelStatistics statistics = StatisticsByChannelID[channelID];
                foreach (var subBloc in columnBloc.OrderedSubBlocs)
                {
                    float[] subBlocValues = statistics.Trial.ChannelSubTrialBySubBloc[subBloc].Values;
                    SubTimeline subTimeline = Timeline.SubTimelinesBySubBloc[subBloc];
                    if (subTimeline.Before > 0) values.AddRange(Enumerable.Repeat(subBlocValues[0], subTimeline.Before));
                    values.AddRange(subBlocValues.Interpolate(subTimeline.Length, 0, 0));
                    if (subTimeline.After > 0) values.AddRange(Enumerable.Repeat(subBlocValues[subBlocValues.Length - 1], subTimeline.After));
                }
                ProcessedValuesByChannel.Add(channelID, values.ToArray());
            }
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

