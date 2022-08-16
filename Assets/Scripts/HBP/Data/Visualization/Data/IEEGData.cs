using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;

namespace HBP.Core.Data.Processed
{
    public class IEEGData : DynamicData
    {
        #region Properties
        public List<BlocEventsStatistics> EventStatistics { get; set; } = new List<BlocEventsStatistics>();
        public Dictionary<string, BlocChannelData> DataByChannelID { get; set; } = new Dictionary<string, BlocChannelData>();
        public Dictionary<string, BlocChannelStatistics> StatisticsByChannelID { get; set; } = new Dictionary<string, BlocChannelStatistics>();
        public Dictionary<string, float[]> ProcessedValuesByChannel { get; set; } = new Dictionary<string, float[]>();
        public Dictionary<string, string> UnitByChannelID { get; set; } = new Dictionary<string, string>();

        private Dictionary<string, Tools.Frequency> m_FrequencyByChannelID = new Dictionary<string, Tools.Frequency>();
        private List<Tools.Frequency> m_Frequencies = new List<Tools.Frequency>();
        public float MaxFrequency { get { return m_Frequencies.Count > 0 ? m_Frequencies.Max(f => f.RawValue) : 0; } }
        #endregion

        #region Public Methods
        public void Load(IEnumerable<IEEGDataInfo> columnData, Bloc bloc)
        {
            foreach (IEEGDataInfo dataInfo in columnData)
            {
                Core.Data.IEEGData data = DataManager.GetData(dataInfo) as Core.Data.IEEGData;
                // Values
                foreach (var channel in data.UnitByChannel.Keys) 
                {
                    string channelID = dataInfo.Patient.ID + "_" + channel;
                    if (!DataByChannelID.ContainsKey(channelID)) DataByChannelID.Add(channelID, DataManager.GetData(dataInfo, bloc, channel));
                    if (!StatisticsByChannelID.ContainsKey(channelID)) StatisticsByChannelID.Add(channelID, DataManager.GetStatistics(dataInfo, bloc, channel));
                    if (!m_FrequencyByChannelID.ContainsKey(channelID)) m_FrequencyByChannelID.Add(channelID, data.Frequency);
                    if (!UnitByChannelID.ContainsKey(channelID)) UnitByChannelID.Add(channelID, data.UnitByChannel[channel]);
                }
                if (!m_Frequencies.Contains(data.Frequency)) m_Frequencies.Add(data.Frequency);
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
            m_Frequencies.Clear();
            ProcessedValuesByChannel.Clear();
        }
        public void SetTimeline(Tools.Frequency maxFrequency, Bloc columnBloc, IEnumerable<Bloc> blocs)
        {
            // Process frequencies
            m_Frequencies.Add(maxFrequency);
            m_Frequencies = m_Frequencies.GroupBy(f => f.Value).Select(g => g.First()).ToList();

            // Get index of each subBloc
            Dictionary<SubBloc, int> indexBySubBloc = new Dictionary<SubBloc, int>();
            foreach (var bloc in blocs)
            {
                int mainSubBlocPosition = bloc.MainSubBlocPosition;
                SubBloc[] subBlocs = bloc.OrderedSubBlocs.ToArray();
                for (int i = 0; i < subBlocs.Length; ++i)
                {
                    if (!indexBySubBloc.ContainsKey(subBlocs[i])) indexBySubBloc.Add(subBlocs[i], i - mainSubBlocPosition);
                }
            }

            // Get all eventStatistics for each SubBloc of the column
            Dictionary<SubBloc, List<SubBlocEventsStatistics>> eventStatisticsBySubBloc = new Dictionary<SubBloc, List<SubBlocEventsStatistics>>();
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
            Timeline timeline = new Timeline(columnBloc, eventStatisticsBySubBloc, indexBySubBloc, maxFrequency);
            IconicScenario = new IconicScenario(columnBloc, maxFrequency, timeline);

            // Standardize values
            foreach (var channelID in DataByChannelID.Keys)
            {
                List<float> values = new List<float>();
                Tools.Frequency frequency = m_FrequencyByChannelID[channelID];
                BlocChannelStatistics statistics = StatisticsByChannelID[channelID];
                foreach (var subBloc in columnBloc.OrderedSubBlocs)
                {
                    float[] subBlocValues = statistics.Trial.ChannelSubTrialBySubBloc[subBloc].Values;
                    SubTimeline subTimeline = timeline.SubTimelinesBySubBloc[subBloc];
                    if (subTimeline.Before > 0) values.AddRange(Enumerable.Repeat(subBlocValues[0], subTimeline.Before));
                    values.AddRange(subBlocValues.Interpolate(subTimeline.Length, 0, 0));
                    if (subTimeline.After > 0) values.AddRange(Enumerable.Repeat(subBlocValues[subBlocValues.Length - 1], subTimeline.After));
                }
                ProcessedValuesByChannel[channelID] = values.ToArray();
            }
        }
        #endregion
    }
}

