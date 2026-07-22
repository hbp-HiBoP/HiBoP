using HBP.Core.DLL;
using System.Collections.Generic;
using System.Linq;

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

        private Dictionary<string, Tools.Frequency> m_FrequencyByChannelID = new();
        private List<Tools.Frequency> m_Frequencies = new();
        private readonly List<IEEGDataInfo> m_PinnedDataInfos = new();
        public float MaxFrequency { get { return m_Frequencies.Count > 0 ? m_Frequencies.Max(f => f.RawValue) : 0; } }
        #endregion

        #region Public Methods
        public void Load(IEnumerable<IEEGDataInfo> columnData, Bloc bloc)
        {
            foreach (IEEGDataInfo dataInfo in columnData)
            {
                bool pinAdded = !m_PinnedDataInfos.Contains(dataInfo);
                if (pinAdded)
                {
                    DataManager.PinData(dataInfo);
                    m_PinnedDataInfos.Add(dataInfo);
                }
                Core.Data.IEEGData data;
                try
                {
                    data = DataManager.GetData(dataInfo, updateMemoryUsage: false) as Core.Data.IEEGData;
                }
                catch
                {
                    if (pinAdded)
                    {
                        m_PinnedDataInfos.Remove(dataInfo);
                        DataManager.UnpinData(dataInfo);
                    }
                    throw;
                }
                // Values
                foreach (var channel in data.UnitByChannel.Keys) 
                {
                    string channelID = dataInfo.Patient.ID + "_" + channel;
                    if (!DataByChannelID.ContainsKey(channelID)) DataByChannelID.Add(channelID, DataManager.GetData(dataInfo, bloc, channel, updateMemoryUsage: false));
                    if (!StatisticsByChannelID.ContainsKey(channelID)) StatisticsByChannelID.Add(channelID, DataManager.GetStatistics(dataInfo, bloc, channel, updateMemoryUsage: false));
                    if (!m_FrequencyByChannelID.ContainsKey(channelID)) m_FrequencyByChannelID.Add(channelID, data.Frequency);
                    if (!UnitByChannelID.ContainsKey(channelID)) UnitByChannelID.Add(channelID, data.UnitByChannel[channel]);
                }
                if (!m_Frequencies.Contains(data.Frequency)) m_Frequencies.Add(data.Frequency);
                // Events
                EventStatistics.Add(DataManager.GetEventsStatistics(dataInfo, bloc, updateMemoryUsage: false));
                // Refresh once after all channel-level derived data have been created.
                DataManager.RefreshDerivedMemoryUsage(dataInfo);
            }
        }
        public override void Unload()
        {
            DataManager.UnregisterMemoryUsage(this);
            foreach (IEEGDataInfo dataInfo in m_PinnedDataInfos)
                DataManager.UnpinData(dataInfo);
            m_PinnedDataInfos.Clear();
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
            // Get index of each subBloc
            Dictionary<SubBloc, int> indexBySubBloc = new();
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
            Dictionary<SubBloc, List<SubBlocEventsStatistics>> eventStatisticsBySubBloc = new();
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
            Tools.Frequency projectionFrequency = new(MaxFrequency);
            ProjectionTimeline = projectionFrequency.Value == maxFrequency.Value
                ? Timeline
                : new Timeline(columnBloc, eventStatisticsBySubBloc, indexBySubBloc, projectionFrequency);
            IconicScenario = new IconicScenario(columnBloc, maxFrequency, Timeline);

            // Standardize values
            foreach (var channelID in DataByChannelID.Keys)
            {
                List<float> values = new();
                Tools.Frequency frequency = m_FrequencyByChannelID[channelID];
                BlocChannelStatistics statistics = StatisticsByChannelID[channelID];
                foreach (var subBloc in columnBloc.OrderedSubBlocs)
                {
                    if (!statistics.Trial.ChannelSubTrialBySubBloc.ContainsKey(subBloc)) continue;

                    float[] subBlocValues = statistics.Trial.ChannelSubTrialBySubBloc[subBloc].Values;
                    SubTimeline subTimeline = ProjectionTimeline.SubTimelinesBySubBloc[subBloc];
                    if (subTimeline.Before > 0) values.AddRange(Enumerable.Repeat(subBlocValues[0], subTimeline.Before));
                    values.AddRange(subBlocValues.Interpolate(subTimeline.Length, 0, 0));
                    if (subTimeline.After > 0) values.AddRange(Enumerable.Repeat(subBlocValues[subBlocValues.Length - 1], subTimeline.After));
                }
                ProcessedValuesByChannel[channelID] = values.ToArray();
            }
            long managedBytes = ProcessedValuesByChannel.Values.Sum(values => values.LongLength * sizeof(float));
            DataManager.RegisterMemoryUsage(this, MemoryCacheCategory.ManagedDerived, managedBytes, true);
        }
        #endregion
    }
}

