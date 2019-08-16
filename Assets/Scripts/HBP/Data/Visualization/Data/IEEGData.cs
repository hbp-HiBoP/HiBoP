using HBP.Data.Experience.Dataset;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;

namespace HBP.Data.Visualization
{
    public class IEEGData : DynamicData
    {
        #region Properties
        public List<BlocEventsStatistics> EventStatistics { get; set; } = new List<BlocEventsStatistics>();
        public Dictionary<string, BlocChannelData> DataByChannelID { get; set; } = new Dictionary<string, BlocChannelData>();
        public Dictionary<string, BlocChannelStatistics> StatisticsByChannelID { get; set; } = new Dictionary<string, BlocChannelStatistics>();
        public Dictionary<string, float[]> ProcessedValuesByChannel { get; set; } = new Dictionary<string, float[]>();
        public Dictionary<string, string> UnitByChannelID { get; set; } = new Dictionary<string, string>();

        private Dictionary<string, Tools.CSharp.EEG.Frequency> m_FrequencyByChannelID = new Dictionary<string, Tools.CSharp.EEG.Frequency>();
        public List<Tools.CSharp.EEG.Frequency> Frequencies = new List<Tools.CSharp.EEG.Frequency>();
        #endregion

        #region Public Methods
        public void Load(IEnumerable<iEEGDataInfo> columnData, Experience.Protocol.Bloc bloc)
        {
            foreach (iEEGDataInfo dataInfo in columnData)
            {
                Experience.Dataset.IEEGData data = DataManager.GetData(dataInfo) as Experience.Dataset.IEEGData;
                // Values
                foreach (var channel in data.UnitByChannel.Keys) 
                {
                    string channelID = dataInfo.Patient.ID + "_" + channel;
                    if (!DataByChannelID.ContainsKey(channelID)) DataByChannelID.Add(channelID, DataManager.GetData(dataInfo, bloc, channel));
                    if (!StatisticsByChannelID.ContainsKey(channelID)) StatisticsByChannelID.Add(channelID, DataManager.GetStatistics(dataInfo, bloc, channel));
                    if (!m_FrequencyByChannelID.ContainsKey(channelID)) m_FrequencyByChannelID.Add(channelID, data.Frequency);
                    if (!UnitByChannelID.ContainsKey(channelID)) UnitByChannelID.Add(channelID, data.UnitByChannel[channel]);
                }
                if (!Frequencies.Contains(data.Frequency)) Frequencies.Add(data.Frequency);
                // Events
                EventStatistics.Add(DataManager.GetEventsStatistics(dataInfo, bloc));
            }
        }
        public override void Unload()
        {
            base.Unload();
            EventStatistics.Clear();
            DataByChannelID.Clear();
            StatisticsByChannelID.Clear();
            UnitByChannelID.Clear();
            m_FrequencyByChannelID.Clear();
            Frequencies.Clear();
            ProcessedValuesByChannel.Clear();
        }
        public void SetTimeline(Tools.CSharp.EEG.Frequency maxFrequency, Experience.Protocol.Bloc columnBloc, IEnumerable<Experience.Protocol.Bloc> blocs)
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
                    if (!indexBySubBloc.ContainsKey(subBlocs[i])) indexBySubBloc.Add(subBlocs[i], i - mainSubBlocPosition);
                }
            }

            // Get all eventStatistics for each SubBloc of the column
            Dictionary<Experience.Protocol.SubBloc, List<SubBlocEventsStatistics>> eventStatisticsBySubBloc = new Dictionary<Experience.Protocol.SubBloc, List<SubBlocEventsStatistics>>();
            foreach (var subBloc in columnBloc.SubBlocs)
            {
                eventStatisticsBySubBloc.Add(subBloc, new List<SubBlocEventsStatistics>());
            }
            foreach (var blocEventStatistics in EventStatistics)
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
                Tools.CSharp.EEG.Frequency frequency = m_FrequencyByChannelID[channelID];
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
        #endregion
    }
}

