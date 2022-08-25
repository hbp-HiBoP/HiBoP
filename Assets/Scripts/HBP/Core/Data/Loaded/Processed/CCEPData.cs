using HBP.Core.DLL;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Core.Data.Processed
{
    public class CCEPData : DynamicData
    {
        #region Properties
        public List<BlocEventsStatistics> EventStatistics { get; set; } = new List<BlocEventsStatistics>();
        public Dictionary<string, Dictionary<string, BlocChannelData>> DataByChannelIDByStimulatedChannelID { get; set; } = new Dictionary<string, Dictionary<string, BlocChannelData>>();
        public Dictionary<string, Dictionary<string, BlocChannelStatistics>> StatisticsByChannelIDByStimulatedChannelID { get; set; } = new Dictionary<string, Dictionary<string, BlocChannelStatistics>>();
        public Dictionary<string, Dictionary<string, float[]>> ProcessedValuesByChannelIDByStimulatedChannelID { get; set; } = new Dictionary<string, Dictionary<string, float[]>>();
        public Dictionary<string, Dictionary<string, string>> UnityByChannelIDByStimulatedChannelID { get; set; } = new Dictionary<string, Dictionary<string, string>>();

        private Dictionary<string, Dictionary<string, Tools.Frequency>> m_FrequencyByChannelIDByStimulatedChannelID = new Dictionary<string, Dictionary<string, Tools.Frequency>>();
        public List<Tools.Frequency> Frequencies = new List<Tools.Frequency>();
        #endregion

        #region Public Methods
        public void Load(IEnumerable<CCEPDataInfo> columnData, Bloc bloc)
        {
            foreach (CCEPDataInfo dataInfo in columnData)
            {
                Core.Data.CCEPData data = DataManager.GetData(dataInfo) as Core.Data.CCEPData;
                string stimulatedChannelID = dataInfo.Patient.ID + "_" + data.StimulatedChannel;

                // Values
                Dictionary<string, BlocChannelData> dataByChannelID = new Dictionary<string, BlocChannelData>();
                Dictionary<string, BlocChannelStatistics> statisticsByChannelID = new Dictionary<string, BlocChannelStatistics>();
                Dictionary<string, Tools.Frequency> frequencyByChannelID = new Dictionary<string, Tools.Frequency>();
                Dictionary<string, string> unitByChannelID = new Dictionary<string, string>();
                foreach (var channel in data.UnitByChannel.Keys)
                {
                    string channelID = dataInfo.Patient.ID + "_" + channel;
                    if (!dataByChannelID.ContainsKey(channelID)) dataByChannelID.Add(channelID, DataManager.GetData(dataInfo, bloc, channel));
                    if (!statisticsByChannelID.ContainsKey(channelID)) statisticsByChannelID.Add(channelID, DataManager.GetStatistics(dataInfo, bloc, channel));
                    if (!frequencyByChannelID.ContainsKey(channelID)) frequencyByChannelID.Add(channelID, data.Frequency);
                    if (!unitByChannelID.ContainsKey(channelID)) unitByChannelID.Add(channelID, data.UnitByChannel[channel]);
                }
                if (!DataByChannelIDByStimulatedChannelID.ContainsKey(stimulatedChannelID)) DataByChannelIDByStimulatedChannelID.Add(stimulatedChannelID, dataByChannelID);
                if (!StatisticsByChannelIDByStimulatedChannelID.ContainsKey(stimulatedChannelID)) StatisticsByChannelIDByStimulatedChannelID.Add(stimulatedChannelID, statisticsByChannelID);
                if (!m_FrequencyByChannelIDByStimulatedChannelID.ContainsKey(stimulatedChannelID)) m_FrequencyByChannelIDByStimulatedChannelID.Add(stimulatedChannelID, frequencyByChannelID);
                if (!UnityByChannelIDByStimulatedChannelID.ContainsKey(stimulatedChannelID)) UnityByChannelIDByStimulatedChannelID.Add(stimulatedChannelID, unitByChannelID);
                if (!Frequencies.Contains(data.Frequency)) Frequencies.Add(data.Frequency);

                // Events
                EventStatistics.Add(DataManager.GetEventsStatistics(dataInfo, bloc));
            }
        }
        public override void Unload()
        {
            base.Unload();
            EventStatistics.Clear();
            DataByChannelIDByStimulatedChannelID.Clear();
            StatisticsByChannelIDByStimulatedChannelID.Clear();
            ProcessedValuesByChannelIDByStimulatedChannelID.Clear();
            UnityByChannelIDByStimulatedChannelID.Clear();
            m_FrequencyByChannelIDByStimulatedChannelID.Clear();
            Frequencies.Clear();
        }
        public void SetTimeline(Tools.Frequency maxFrequency, Bloc columnBloc, IEnumerable<Bloc> blocs)
        {
            // Process frequencies
            Frequencies.Add(maxFrequency);
            Frequencies = Frequencies.GroupBy(f => f.Value).Select(g => g.First()).ToList();

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
            Timeline = new Timeline(columnBloc, eventStatisticsBySubBloc, indexBySubBloc, maxFrequency);
            IconicScenario = new IconicScenario(columnBloc, maxFrequency, Timeline);

            // Standardize values
            foreach (var dataByChannelIDByStimulatedChannelID in DataByChannelIDByStimulatedChannelID)
            {
                Dictionary<string, float[]> processedValuesByChannelID = new Dictionary<string, float[]>();
                foreach (var channelID in dataByChannelIDByStimulatedChannelID.Value.Keys)
                {
                    List<float> values = new List<float>();
                    Tools.Frequency frequency = m_FrequencyByChannelIDByStimulatedChannelID[dataByChannelIDByStimulatedChannelID.Key][channelID];
                    BlocChannelStatistics statistics = StatisticsByChannelIDByStimulatedChannelID[dataByChannelIDByStimulatedChannelID.Key][channelID];
                    foreach (var subBloc in columnBloc.OrderedSubBlocs)
                    {
                        float[] subBlocValues = statistics.Trial.ChannelSubTrialBySubBloc[subBloc].Values;
                        SubTimeline subTimeline = Timeline.SubTimelinesBySubBloc[subBloc];
                        if (subTimeline.Before > 0) values.AddRange(Enumerable.Repeat(subBlocValues[0], subTimeline.Before));
                        values.AddRange(subBlocValues.Interpolate(subTimeline.Length, 0, 0));
                        if (subTimeline.After > 0) values.AddRange(Enumerable.Repeat(subBlocValues[subBlocValues.Length - 1], subTimeline.After));
                    }
                    processedValuesByChannelID.Add(channelID, values.ToArray());
                }
                ProcessedValuesByChannelIDByStimulatedChannelID.Add(dataByChannelIDByStimulatedChannelID.Key, processedValuesByChannelID);
            }
        }
        #endregion
    }
}

