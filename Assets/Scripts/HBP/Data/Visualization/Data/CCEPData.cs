using HBP.Data.Experience.Dataset;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;

namespace HBP.Data.Visualization
{
    public class CCEPData : DynamicData
    {
        #region Properties
        public BlocEventsStatistics BlocEventsStatistics { get; set; }
        public Dictionary<string, BlocChannelData> DataByChannelID { get; set; } = new Dictionary<string, BlocChannelData>();
        public Dictionary<string, BlocChannelStatistics> StatisticsByChannelID { get; set; } = new Dictionary<string, BlocChannelStatistics>();
        public Dictionary<string, float[]> ProcessedValuesByChannel { get; set; } = new Dictionary<string, float[]>();
        public Tools.CSharp.EEG.Frequency Frequency { get; set; }
        #endregion

        #region Public Methods
        public void Load(CCEPDataInfo dataInfo, Experience.Protocol.Bloc bloc)
        {
            Experience.Dataset.CCEPData data = DataManager.GetData(dataInfo) as Experience.Dataset.CCEPData;

            // Values
            foreach (var channel in data.UnitByChannel.Keys)
            {
                string channelID = dataInfo.Patient.ID + "_" + channel;
                if (!DataByChannelID.ContainsKey(channelID)) DataByChannelID.Add(channelID, DataManager.GetData(dataInfo, bloc, channel));
                if (!StatisticsByChannelID.ContainsKey(channelID)) StatisticsByChannelID.Add(channelID, DataManager.GetStatistics(dataInfo, bloc, channel));
                if (!UnitByChannel.ContainsKey(channelID)) UnitByChannel.Add(channelID, data.UnitByChannel[channel]);
            }
            BlocEventsStatistics = DataManager.GetEventsStatistics(dataInfo, bloc);
            Frequency = data.Frequency;
        }
        public override void Unload()
        {
            base.Unload();
            BlocEventsStatistics = null;
            DataByChannelID.Clear();
            StatisticsByChannelID.Clear();
            ProcessedValuesByChannel.Clear();
        }
        public void SetTimeline(Tools.CSharp.EEG.Frequency maxFrequency, Experience.Protocol.Bloc columnBloc, IEnumerable<Experience.Protocol.Bloc> blocs)
        {
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
                eventStatisticsBySubBloc.Add(subBloc, new List<SubBlocEventsStatistics>() { BlocEventsStatistics.EventsStatisticsBySubBloc[subBloc] });
            }

            // Create timeline and iconic scenario
            Timeline = new Timeline(columnBloc, eventStatisticsBySubBloc, indexBySubBloc, maxFrequency);
            IconicScenario = new IconicScenario(columnBloc, maxFrequency, Timeline);

            // Standardize values
            foreach (var channelID in DataByChannelID.Keys)
            {
                List<float> values = new List<float>();
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

