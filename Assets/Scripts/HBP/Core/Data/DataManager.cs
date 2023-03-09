using System.Collections.Generic;
using System.Linq;
using System;
using HBP.Core.Enums;
using HBP.Core.DLL;

namespace HBP.Core.Data
{
    public static class DataManager
    {
        #region Properties
        // General.
        static Dictionary<Request, Data> m_DataByRequest = new Dictionary<Request, Data>();

        // iEEG
        static Dictionary<BlocRequest, BlocData> m_BlocDataByRequest = new Dictionary<BlocRequest, BlocData>();

        static Dictionary<ChannelRequest, ChannelData> m_ChannelDataByRequest = new Dictionary<ChannelRequest, ChannelData>();
        static Dictionary<BlocChannelRequest, BlocChannelData> m_BlocChannelDataByRequest = new Dictionary<BlocChannelRequest, BlocChannelData>();

        // Statistics.
        static Dictionary<ChannelRequest, ChannelStatistics> m_ChannelStatisticsByRequest = new Dictionary<ChannelRequest, ChannelStatistics>();
        static Dictionary<BlocChannelRequest, BlocChannelStatistics> m_BlocChannelStatisticsByRequest = new Dictionary<BlocChannelRequest, BlocChannelStatistics>();

        static Dictionary<Request, EventsStatistics> m_EventsStatisticsByRequest = new Dictionary<Request, EventsStatistics>();
        static Dictionary<BlocRequest, BlocEventsStatistics> m_BlocEventsStatisticsByRequest = new Dictionary<BlocRequest, BlocEventsStatistics>();

        static Stack<BlocRequest> m_BlocRequestsRequiringStatisticsReset = new Stack<BlocRequest>();

        // Normalize
        static Dictionary<BlocRequest, NormalizationType> m_NormalizeByRequest = new Dictionary<BlocRequest, NormalizationType>();

        // Default values
        public static NormalizationType DefaultNormalization = NormalizationType.None;
        public static AveragingType DefaultAveraging = AveragingType.Mean;
        public static AveragingType DefaultPositionAveraging = AveragingType.Mean;
        public static bool HasData
        {
            get
            {
                return m_DataByRequest.Count > 0
                    || m_BlocDataByRequest.Count > 0
                    || m_ChannelDataByRequest.Count > 0
                    || m_BlocChannelDataByRequest.Count > 0
                    || m_ChannelStatisticsByRequest.Count > 0
                    || m_BlocChannelStatisticsByRequest.Count > 0
                    || m_EventsStatisticsByRequest.Count > 0
                    || m_BlocEventsStatisticsByRequest.Count > 0;
            }
        }
        #endregion

        #region Public Methods
        // General.
        public static void Load(DataInfo dataInfo)
        {
            Load(new Request(dataInfo));
        }
        public static void UnLoad(DataInfo dataInfo)
        {
            UnLoad(new Request(dataInfo));
        }
        public static void Reload(DataInfo dataInfo)
        {
            UnLoad(dataInfo);
            Load(dataInfo);
        }
        public static void Clear()
        {
            foreach (var data in m_DataByRequest.Values)
            {
                data.Clear();
            }
            m_DataByRequest.Clear();
            m_DataByRequest = new Dictionary<Request, Data>();

            foreach (var blocData in m_BlocDataByRequest.Values)
            {
                blocData.Clear();
            }
            m_BlocDataByRequest.Clear();
            m_BlocDataByRequest = new Dictionary<BlocRequest, BlocData>();

            foreach (var channelData in m_ChannelDataByRequest.Values)
            {
                channelData.Clear();
            }
            m_ChannelDataByRequest.Clear();
            m_ChannelDataByRequest = new Dictionary<ChannelRequest, ChannelData>();

            foreach (var blocChannelData in m_BlocChannelDataByRequest.Values)
            {
                blocChannelData.Clear();
            }
            m_BlocChannelDataByRequest.Clear();
            m_BlocChannelDataByRequest = new Dictionary<BlocChannelRequest, BlocChannelData>();

            foreach (var channelStatistics in m_ChannelStatisticsByRequest.Values)
            {
                channelStatistics.Clear();
            }
            m_ChannelStatisticsByRequest.Clear();
            m_ChannelStatisticsByRequest = new Dictionary<ChannelRequest, ChannelStatistics>();

            foreach (var blocChannelStatistics in m_BlocChannelStatisticsByRequest.Values)
            {
                blocChannelStatistics.Clear();
            }
            m_BlocChannelStatisticsByRequest.Clear();
            m_BlocChannelStatisticsByRequest = new Dictionary<BlocChannelRequest, BlocChannelStatistics>();

            foreach (var eventStatistics in m_EventsStatisticsByRequest.Values)
            {
                eventStatistics.Clear();
            }
            m_EventsStatisticsByRequest.Clear();
            m_EventsStatisticsByRequest = new Dictionary<Request, EventsStatistics>();

            foreach (var blocEventsStatistics in m_BlocEventsStatisticsByRequest.Values)
            {
                blocEventsStatistics.Clear();
            }
            m_BlocEventsStatisticsByRequest.Clear();
            m_BlocEventsStatisticsByRequest = new Dictionary<BlocRequest, BlocEventsStatistics>();

            m_NormalizeByRequest.Clear();
            m_NormalizeByRequest = new Dictionary<BlocRequest, NormalizationType>();

            GC.Collect();
        }

        public static Data GetData(DataInfo dataInfo)
        {
            return GetData(new Request(dataInfo));
        }
        public static BlocData GetData(DataInfo dataInfo, Bloc bloc)
        {
            return GetData(new BlocRequest(dataInfo, bloc));
        }
        public static ChannelData GetData(DataInfo dataInfo, string channel)
        {
            return GetData(new ChannelRequest(dataInfo, channel));
        }
        public static BlocChannelData GetData(DataInfo dataInfo, Bloc bloc, string channel)
        {
            return GetData(new BlocChannelRequest(dataInfo, bloc, channel));
        }

        // Statistics.
        public static ChannelStatistics GetStatistics(DataInfo dataInfo, string channel)
        {
            return GetStatistics(new ChannelRequest(dataInfo, channel));
        }
        public static BlocChannelStatistics GetStatistics(DataInfo dataInfo, Bloc bloc, string channel)
        {
            return GetStatistics(new BlocChannelRequest(dataInfo, bloc, channel));
        }
        public static EventsStatistics GetEventsStatistics(DataInfo dataInfo)
        {
            return GetEventsStatistics(new Request(dataInfo));
        }
        public static BlocEventsStatistics GetEventsStatistics(DataInfo dataInfo, Bloc bloc)
        {
            return GetEventsStatistics(new BlocRequest(dataInfo, bloc));
        }

        public static void NormalizeiEEGData()
        {
            IEnumerable<IEEGDataInfo> dataInfoCollection = m_DataByRequest.Select((d) => d.Key.DataInfo).OfType<IEEGDataInfo>().Distinct();
            foreach (var dataInfo in dataInfoCollection)
            {
                IEnumerable<BlocRequest> dataRequestCollection = m_BlocDataByRequest.Where((d) => d.Key.DataInfo == dataInfo).Select((d) => d.Key);
                switch (dataInfo.Normalization)
                {
                    case NormalizationType.None:
                        foreach (var request in dataRequestCollection) if (m_NormalizeByRequest[request] != NormalizationType.None) NormalizeByNone(request);
                        break;
                    case NormalizationType.SubTrial:
                        foreach (var request in dataRequestCollection) if (m_NormalizeByRequest[request] != NormalizationType.SubTrial) NormalizeBySubTrial(request);
                        break;
                    case NormalizationType.Trial:
                        foreach (var request in dataRequestCollection) if (m_NormalizeByRequest[request] != NormalizationType.Trial) NormalizeByTrial(request);
                        break;
                    case NormalizationType.SubBloc:
                        foreach (var request in dataRequestCollection) if (m_NormalizeByRequest[request] != NormalizationType.SubBloc) NormalizeBySubBloc(request);
                        break;
                    case NormalizationType.Bloc:
                        foreach (var request in dataRequestCollection) if (m_NormalizeByRequest[request] != NormalizationType.Bloc) NormalizeByBloc(request);
                        break;
                    case NormalizationType.Protocol:
                        IEnumerable<Tuple<BlocRequest, bool>> dataRequestAndNeedToNormalize = from request in dataRequestCollection select new Tuple<BlocRequest, bool>(request, m_NormalizeByRequest[request] != NormalizationType.Protocol);
                        if (dataRequestAndNeedToNormalize.Any((tuple) => tuple.Item2))
                        {
                            NormalizeByProtocol(dataRequestAndNeedToNormalize);
                        }
                        break;
                    case NormalizationType.Auto:
                        switch (DefaultNormalization)
                        {
                            case NormalizationType.None:
                                foreach (var request in dataRequestCollection) if (m_NormalizeByRequest[request] != NormalizationType.None) NormalizeByNone(request);
                                break;
                            case NormalizationType.SubTrial:
                                foreach (var request in dataRequestCollection) if (m_NormalizeByRequest[request] != NormalizationType.SubTrial) NormalizeBySubTrial(request);
                                break;
                            case NormalizationType.Trial:
                                foreach (var request in dataRequestCollection) if (m_NormalizeByRequest[request] != NormalizationType.Trial) NormalizeByTrial(request);
                                break;
                            case NormalizationType.SubBloc:
                                foreach (var request in dataRequestCollection) if (m_NormalizeByRequest[request] != NormalizationType.SubBloc) NormalizeBySubBloc(request);
                                break;
                            case NormalizationType.Bloc:
                                foreach (var request in dataRequestCollection) if (m_NormalizeByRequest[request] != NormalizationType.Bloc) NormalizeByBloc(request);
                                break;
                            case NormalizationType.Protocol:
                                IEnumerable<Tuple<BlocRequest, bool>> dataRequestAndNeedToNormalize2 = from request in dataRequestCollection select new Tuple<BlocRequest, bool>(request, m_NormalizeByRequest[request] != NormalizationType.Protocol);
                                if (dataRequestAndNeedToNormalize2.Any((tuple) => tuple.Item2))
                                {
                                    NormalizeByProtocol(dataRequestAndNeedToNormalize2);
                                }
                                break;
                        }
                        break;
                }
            }
            while (m_BlocRequestsRequiringStatisticsReset.Count > 0)
            {
                UnloadStatistics(m_BlocRequestsRequiringStatisticsReset.Pop());
            }
        }

        // CCEP
        //public static 
        #endregion

        #region Private Methods
        static void Load(Request request)
        {
            if (request.IsValid && !m_DataByRequest.ContainsKey(request))
            {
                if (request.DataInfo is IEEGDataInfo iEEGDataInfo)
                {
                    IEEGData data = new IEEGData(iEEGDataInfo);
                    m_DataByRequest.Add(request, data);

                    Protocol protocol = request.DataInfo.Dataset.Protocol;
                    foreach (var bloc in protocol.Blocs)
                    {
                        m_BlocDataByRequest.Add(new BlocRequest(request.DataInfo, bloc), data.DataByBloc[bloc]);
                        m_NormalizeByRequest.Add(new BlocRequest(request.DataInfo, bloc), NormalizationType.None);
                    }
                }
                else if (request.DataInfo is CCEPDataInfo CCEPDataInfo)
                {
                    CCEPData data = new CCEPData(CCEPDataInfo);
                    m_DataByRequest.Add(request, data);

                    Protocol protocol = request.DataInfo.Dataset.Protocol;
                    foreach (var bloc in protocol.Blocs)
                    {
                        m_BlocDataByRequest.Add(new BlocRequest(request.DataInfo, bloc), data.DataByBloc[bloc]);
                        m_NormalizeByRequest.Add(new BlocRequest(request.DataInfo, bloc), NormalizationType.None);
                    }
                }
                else if (request.DataInfo is FMRIDataInfo FMRIDataInfo)
                {
                    FMRIData data = new FMRIData(FMRIDataInfo);
                    m_DataByRequest.Add(request, data);
                }
                else if (request.DataInfo is StaticDataInfo staticDataInfo)
                {
                    StaticData data = new StaticData(staticDataInfo);
                    m_DataByRequest.Add(request, data);
                }
                else if (request.DataInfo is SharedFMRIDataInfo sharedFMRIDataInfo)
                {
                    FMRIData data = new FMRIData(sharedFMRIDataInfo);
                    m_DataByRequest.Add(request, data);
                }
                else if (request.DataInfo is MEGvDataInfo MEGvDataInfo)
                {
                    MEGvData data = new MEGvData(MEGvDataInfo);
                    m_DataByRequest.Add(request, data);
                }
                else if (request.DataInfo is MEGcDataInfo MEGcDataInfo)
                {
                    MEGcData data = new MEGcData(MEGcDataInfo);
                    m_DataByRequest.Add(request, data);
                }
            }
        }
        static void UnLoad(Request request)
        {
            if (request.IsValid && m_DataByRequest.ContainsKey(request))
            {
                m_DataByRequest.Remove(request);

                IEnumerable<ChannelRequest> channelDataRequestsToRemove = m_ChannelDataByRequest.Keys.Where(k => k.DataInfo == request.DataInfo);
                foreach (var channelDataRequest in channelDataRequestsToRemove)
                {
                    m_ChannelDataByRequest.Remove(channelDataRequest);
                }

                IEnumerable<BlocChannelRequest> blocChannelDataRequestsToRemove = m_BlocChannelDataByRequest.Keys.Where(k => k.DataInfo == request.DataInfo);
                foreach (var blocChannelDataRequest in blocChannelDataRequestsToRemove)
                {
                    m_BlocChannelDataByRequest.Remove(blocChannelDataRequest);
                }

                IEnumerable<BlocRequest> blocDataRequestsToRemove = m_BlocDataByRequest.Keys.Where(k => k.DataInfo == request.DataInfo);
                foreach (var blocDataRequest in blocDataRequestsToRemove)
                {
                    m_BlocDataByRequest.Remove(blocDataRequest);
                }
            }
        }
        static void UnloadStatistics(BlocRequest request)
        {
            if (request.IsValid)
            {
                List<ChannelRequest> channelStatisticsToRemove = m_ChannelStatisticsByRequest.Keys.Where(k => k.DataInfo == request.DataInfo).ToList();
                foreach (var channelRequest in channelStatisticsToRemove)
                {
                    m_ChannelStatisticsByRequest.Remove(channelRequest);
                }

                List<BlocChannelRequest> blocChannelStatisticsToRemove = m_BlocChannelStatisticsByRequest.Keys.Where(k => k.DataInfo == request.DataInfo && k.Bloc == request.Bloc).ToList();
                foreach (var blocChannelRequest in blocChannelStatisticsToRemove)
                {
                    m_BlocChannelStatisticsByRequest.Remove(blocChannelRequest);
                }

                List<Request> eventStatisticsToRemove = m_EventsStatisticsByRequest.Keys.Where(k => k.DataInfo == request.DataInfo).ToList();
                foreach (var eventRequest in eventStatisticsToRemove)
                {
                    m_EventsStatisticsByRequest.Remove(eventRequest);
                }

                if (m_BlocEventsStatisticsByRequest.ContainsKey(request))
                {
                    m_BlocEventsStatisticsByRequest.Remove(request);
                }
            }
        }

        static Data GetData(Request request)
        {
            if (request.IsValid)
            {
                if (m_DataByRequest.TryGetValue(request, out Data result))
                {
                    return result;
                }
                else
                {
                    Load(request);
                    return m_DataByRequest[request];
                }
            }
            else
            {
                return null;
            }
        }
        static BlocData GetData(BlocRequest request)
        {
            if (request.IsValid)
            {
                if (m_BlocDataByRequest.TryGetValue(request, out BlocData result))
                {
                    return result;
                }
                else
                {
                    Load(request.DataInfo);
                    return m_BlocDataByRequest[request];
                }
            }
            else
            {
                return null;
            }
        }
        static ChannelData GetData(ChannelRequest request)
        {
            if (request.IsValid)
            {
                ChannelData result;
                if (m_ChannelDataByRequest.TryGetValue(request, out result))
                {
                    return result;
                }
                else
                {
                    Request dataRequest = new Request(request.DataInfo);
                    if (!m_DataByRequest.ContainsKey(dataRequest))
                    {
                        Load(dataRequest);
                    }
                    ChannelData channelData = new ChannelData(m_DataByRequest[dataRequest] as EpochedData, request.Channel);
                    m_ChannelDataByRequest.Add(request, channelData);
                    return channelData;
                }
            }
            else
            {
                return null;
            }
        }
        static BlocChannelData GetData(BlocChannelRequest request)
        {
            if (request.IsValid)
            {
                BlocChannelData result;
                if (m_BlocChannelDataByRequest.TryGetValue(request, out result))
                {
                    return result;
                }
                else
                {
                    Request dataRequest = new Request(request.DataInfo);
                    EpochedData data = GetData(dataRequest) as EpochedData;
                    if (data != null)
                    {
                        if (data.UnitByChannel.ContainsKey(request.Channel))
                        {
                            BlocRequest blocDataRequest = new BlocRequest(request.DataInfo, request.Bloc);
                            BlocChannelData blocChannelData = new BlocChannelData(m_BlocDataByRequest[blocDataRequest], request.Channel);
                            m_BlocChannelDataByRequest.Add(request, blocChannelData);
                            return blocChannelData;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                return null;
            }
        }

        // Statistics.
        static ChannelStatistics GetStatistics(ChannelRequest request)
        {
            if (request.IsValid)
            {
                ChannelStatistics result;
                if (m_ChannelStatisticsByRequest.TryGetValue(request, out result))
                {
                    return result;
                }
                else
                {
                    ChannelData channelData = GetData(request);
                    ChannelStatistics channelStatistics = new ChannelStatistics(channelData, DefaultAveraging);
                    m_ChannelStatisticsByRequest.Add(request, channelStatistics);
                    return channelStatistics;
                }
            }
            else
            {
                return null;
            }
        }
        static BlocChannelStatistics GetStatistics(BlocChannelRequest request)
        {
            if (request.IsValid)
            {
                BlocChannelStatistics result;
                if (m_BlocChannelStatisticsByRequest.TryGetValue(request, out result))
                {
                    return result;
                }
                else
                {
                    BlocChannelData blocChannelData = GetData(request);
                    BlocChannelStatistics blocChannelStatistics = new BlocChannelStatistics(blocChannelData, DefaultAveraging);
                    m_BlocChannelStatisticsByRequest.Add(request, blocChannelStatistics);
                    return blocChannelStatistics;
                }
            }
            else
            {
                return null;
            }
        }
        static EventsStatistics GetEventsStatistics(Request request)
        {
            if (request.IsValid)
            {
                if (m_EventsStatisticsByRequest.TryGetValue(request, out EventsStatistics result))
                {
                    return result;
                }
                else
                {
                    EventsStatistics eventsStatistics = new EventsStatistics(request.DataInfo);
                    foreach (var pair in eventsStatistics.EventsStatisticsByBloc) m_BlocEventsStatisticsByRequest.Add(new BlocRequest(request.DataInfo, pair.Key), pair.Value);
                    m_EventsStatisticsByRequest.Add(request, eventsStatistics);
                    return eventsStatistics;
                }
            }
            else return null;
        }
        static BlocEventsStatistics GetEventsStatistics(BlocRequest request)
        {
            if (request.IsValid)
            {
                if (m_BlocEventsStatisticsByRequest.TryGetValue(request, out BlocEventsStatistics result))
                {
                    return result;
                }
                else
                {
                    BlocEventsStatistics blocEventsStatistics = new BlocEventsStatistics(request.DataInfo, request.Bloc, DefaultPositionAveraging);
                    m_BlocEventsStatisticsByRequest.Add(request, blocEventsStatistics);
                    return blocEventsStatistics;
                }
            }
            else return null;
        }

        static void NormalizeByNone(BlocRequest request)
        {
            BlocData blocData = GetData(request);
            foreach (var trial in blocData.Trials)
            {
                foreach (var subTrial in trial.SubTrialBySubBloc.Values)
                {
                    subTrial.Normalize(0, 1);
                }
            }
            m_NormalizeByRequest[request] = NormalizationType.None;
            m_BlocRequestsRequiringStatisticsReset.Push(request);
        }
        static void NormalizeBySubTrial(BlocRequest request)
        {
            BlocData blocData = GetData(request);
            foreach (var trial in blocData.Trials)
            {
                foreach (var subTrial in trial.SubTrialBySubBloc.Values)
                {
                    foreach (var pair in subTrial.BaselineValuesByChannel)
                    {
                        subTrial.Normalize(pair.Value.Mean(), pair.Value.StandardDeviation(), pair.Key);
                    }
                }
            }
            m_NormalizeByRequest[request] = NormalizationType.SubTrial;
            m_BlocRequestsRequiringStatisticsReset.Push(request);
        }
        static void NormalizeByTrial(BlocRequest request)
        {
            BlocData epochedData = GetData(request);
            foreach (var trial in epochedData.Trials)
            {
                Dictionary<string, List<float>> baselineByChannel = new Dictionary<string, List<float>>();
                foreach (var subTrial in trial.SubTrialBySubBloc.Values)
                {
                    foreach (var channel in subTrial.BaselineValuesByChannel.Keys)
                    {
                        if (!baselineByChannel.ContainsKey(channel)) baselineByChannel[channel] = new List<float>();
                        baselineByChannel[channel].AddRange(subTrial.BaselineValuesByChannel[channel]);
                    }
                }

                float average, standardDeviation;
                foreach (var channel in baselineByChannel.Keys)
                {
                    average = baselineByChannel[channel].ToArray().Mean();
                    standardDeviation = baselineByChannel[channel].ToArray().StandardDeviation();
                    foreach (var subTrial in trial.SubTrialBySubBloc.Values)
                    {
                        subTrial.Normalize(average, standardDeviation, channel);
                    }
                }
            }
            m_NormalizeByRequest[request] = NormalizationType.Trial;
            m_BlocRequestsRequiringStatisticsReset.Push(request);
        }
        static void NormalizeBySubBloc(BlocRequest request)
        {
            Dictionary<string, List<float>> baselineByChannel;
            BlocData epochedData = GetData(request);
            foreach (var subBloc in request.Bloc.SubBlocs)
            {
                baselineByChannel = new Dictionary<string, List<float>>();
                foreach (var trial in epochedData.Trials)
                {
                    SubTrial subTrial = trial.SubTrialBySubBloc[subBloc];
                    foreach (var channel in subTrial.BaselineValuesByChannel.Keys)
                    {
                        if (!baselineByChannel.ContainsKey(channel)) baselineByChannel[channel] = new List<float>();
                        baselineByChannel[channel].AddRange(subTrial.BaselineValuesByChannel[channel]);
                    }
                }

                float average, standardDeviation;
                foreach (var channel in baselineByChannel.Keys)
                {
                    average = baselineByChannel[channel].ToArray().Mean();
                    standardDeviation = baselineByChannel[channel].ToArray().StandardDeviation();
                    foreach (var trial in epochedData.Trials)
                    {
                        SubTrial subTrial = trial.SubTrialBySubBloc[subBloc];
                        subTrial.Normalize(average, standardDeviation, channel);
                    }
                }
            }
            m_NormalizeByRequest[request] = NormalizationType.SubBloc;
            m_BlocRequestsRequiringStatisticsReset.Push(request);
        }
        static void NormalizeByBloc(BlocRequest request)
        {
            Dictionary<string, List<float>> baselineByChannel = new Dictionary<string, List<float>>();
            BlocData epochedData = m_BlocDataByRequest[request];
            foreach (var trial in epochedData.Trials)
            {
                foreach (var subTrial in trial.SubTrialBySubBloc.Values)
                {
                    foreach (var channel in subTrial.BaselineValuesByChannel.Keys)
                    {
                        if (!baselineByChannel.ContainsKey(channel)) baselineByChannel[channel] = new List<float>();
                        baselineByChannel[channel].AddRange(subTrial.BaselineValuesByChannel[channel]);
                    }
                }
            }

            float average, standardDeviation;
            foreach (var channel in baselineByChannel.Keys)
            {
                average = baselineByChannel[channel].ToArray().Mean();
                standardDeviation = baselineByChannel[channel].ToArray().StandardDeviation();
                foreach (var trial in epochedData.Trials)
                {
                    foreach (var subTrial in trial.SubTrialBySubBloc.Values)
                    {
                        subTrial.Normalize(average, standardDeviation, channel);
                    }
                }
            }
            m_NormalizeByRequest[request] = NormalizationType.Bloc;
            m_BlocRequestsRequiringStatisticsReset.Push(request);
        }
        static void NormalizeByProtocol(IEnumerable<Tuple<BlocRequest, bool>> dataRequestAndNeedToNormalize)
        {
            Dictionary<string, List<float>> baselineByChannel = new Dictionary<string, List<float>>();

            foreach (var tuple in dataRequestAndNeedToNormalize)
            {
                BlocData epochedData = m_BlocDataByRequest[tuple.Item1];
                foreach (var trial in epochedData.Trials)
                {
                    foreach (var subTrial in trial.SubTrialBySubBloc.Values)
                    {
                        foreach (var channel in subTrial.BaselineValuesByChannel.Keys)
                        {
                            if (!baselineByChannel.ContainsKey(channel)) baselineByChannel[channel] = new List<float>();
                            baselineByChannel[channel].AddRange(subTrial.BaselineValuesByChannel[channel]);
                        }
                    }
                }
            }

            float average, standardDeviation;
            foreach (var channel in baselineByChannel.Keys)
            {
                average = baselineByChannel[channel].ToArray().Mean();
                standardDeviation = baselineByChannel[channel].ToArray().StandardDeviation();
                foreach (var tuple in dataRequestAndNeedToNormalize)
                {
                    if (tuple.Item2)
                    {
                        BlocData epochedData = m_BlocDataByRequest[tuple.Item1];
                        foreach (var trial in epochedData.Trials)
                        {
                            foreach (var subTrial in trial.SubTrialBySubBloc.Values)
                            {
                                subTrial.Normalize(average, standardDeviation, channel);
                            }
                        }
                    }
                }
            }

            foreach (var tuple in dataRequestAndNeedToNormalize)
            {
                if (tuple.Item2)
                {
                    m_NormalizeByRequest[tuple.Item1] = NormalizationType.Protocol;
                    m_BlocRequestsRequiringStatisticsReset.Push(tuple.Item1);
                }
            }
        }
        #endregion

        #region Private struct
        class Request
        {
            #region Properties
            public virtual DataInfo DataInfo { get; set; }
            public virtual bool IsValid
            {
                get
                {
                    return DataInfo != null && DataInfo.IsOk;
                }
            }
            #endregion

            #region Constructors
            public Request(DataInfo dataInfo)
            {
                DataInfo = dataInfo;
            }
            #endregion

            #region Public Methods
            public override bool Equals(object obj)
            {
                //Check for null and compare run-time types.
                if ((obj == null) || !GetType().Equals(obj.GetType()))
                {
                    return false;
                }
                else
                {
                    Request request = (Request)obj;
                    return (DataInfo == request.DataInfo);
                }
            }
            public override int GetHashCode()
            {
                return DataInfo.GetHashCode();
            }
            public static bool operator ==(Request left, Request right)
            {
                return left.Equals(right);
            }
            public static bool operator !=(Request left, Request right)
            {
                return !left.Equals(right);
            }
            #endregion
        }
        class BlocRequest : Request
        {
            #region Properties
            public virtual Bloc Bloc { get; set; }
            public override bool IsValid
            {
                get
                {
                    return base.IsValid && DataInfo.Dataset.Protocol.Blocs.Contains(Bloc) && DataInfo is IEpochable;
                }
            }
            #endregion

            #region Constructors
            public BlocRequest(DataInfo dataInfo, Bloc bloc) : base(dataInfo)
            {
                Bloc = bloc;
            }
            #endregion

            #region Public Methods
            public override bool Equals(object obj)
            {
                //Check for null and compare run-time types.
                if ((obj == null) || !GetType().Equals(obj.GetType()))
                {
                    return false;
                }
                else
                {
                    BlocRequest request = (BlocRequest)obj;
                    return base.Equals(obj) && request.Bloc == Bloc;
                }
            }
            public override int GetHashCode()
            {
                return base.GetHashCode() * Bloc.GetHashCode();
            }
            #endregion
        }
        class ChannelRequest : Request
        {
            #region Properties
            public virtual string Channel { get; set; }
            public override bool IsValid
            {
                get
                {
                    return base.IsValid && DataInfo is IEpochable /*&& AddTestOnChannel */;
                }
            }
            #endregion

            #region Constructors
            public ChannelRequest(DataInfo dataInfo, string channel) : base(dataInfo)
            {
                DataInfo = dataInfo;
                Channel = channel;
            }
            #endregion

            #region Public Methods
            public override bool Equals(object obj)
            {
                //Check for null and compare run-time types.
                if ((obj == null) || !GetType().Equals(obj.GetType()))
                {
                    return false;
                }
                else
                {
                    ChannelRequest request = (ChannelRequest)obj;
                    return base.Equals(obj) && request.Channel == Channel;
                }
            }
            public override int GetHashCode()
            {
                return base.GetHashCode() * Channel.GetHashCode();
            }
            #endregion
        }
        class BlocChannelRequest : BlocRequest
        {
            #region Properties
            public virtual string Channel { get; set; }
            public override bool IsValid
            {
                get
                {
                    return base.IsValid && DataInfo.Dataset.Protocol.Blocs.Contains(Bloc); // AddTestOnChannel
                }
            }
            #endregion

            #region Constructors
            public BlocChannelRequest(DataInfo dataInfo, Bloc bloc, string channel) : base(dataInfo, bloc)
            {
                Channel = channel;
            }
            #endregion

            #region Public Methods
            public override bool Equals(object obj)
            {
                //Check for null and compare run-time types.
                if ((obj == null) || !GetType().Equals(obj.GetType()))
                {
                    return false;
                }
                else
                {
                    BlocChannelRequest request = (BlocChannelRequest)obj;
                    return base.Equals(obj) && request.Channel == Channel;
                }
            }
            public override int GetHashCode()
            {
                return base.GetHashCode() * Channel.GetHashCode();
            }
            #endregion
        }
        #endregion
    }
}