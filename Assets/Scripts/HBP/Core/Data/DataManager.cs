using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using HBP.Core.Enums;
using HBP.Core.DLL;

namespace HBP.Core.Data
{
    public static class DataManager
    {
        #region Properties
        // Thread-safe access using ReaderWriterLockSlim for better performance than ConcurrentDictionary
        private static readonly ReaderWriterLockSlim m_DataLock = new();
        
        // General.
        static Dictionary<Request, Data> m_DataByRequest = new();

        // iEEG
        static Dictionary<BlocRequest, BlocData> m_BlocDataByRequest = new();

        static Dictionary<ChannelRequest, ChannelData> m_ChannelDataByRequest = new();
        static Dictionary<BlocChannelRequest, BlocChannelData> m_BlocChannelDataByRequest = new();

        // Statistics.
        static Dictionary<ChannelRequest, ChannelStatistics> m_ChannelStatisticsByRequest = new();
        static Dictionary<BlocChannelRequest, BlocChannelStatistics> m_BlocChannelStatisticsByRequest = new();

        static Dictionary<Request, EventsStatistics> m_EventsStatisticsByRequest = new();
        static Dictionary<BlocRequest, BlocEventsStatistics> m_BlocEventsStatisticsByRequest = new();

        static Stack<BlocRequest> m_BlocRequestsRequiringStatisticsReset = new();

        // Normalize
        static Dictionary<BlocRequest, NormalizationType> m_NormalizeByRequest = new();

        // Default values
        public static NormalizationType DefaultNormalization = NormalizationType.None;
        public static AveragingType DefaultAveraging = AveragingType.Mean;
        public static AveragingType DefaultPositionAveraging = AveragingType.Mean;
        public static bool HasData
        {
            get
            {
                m_DataLock.EnterReadLock();
                try
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
                finally
                {
                    m_DataLock.ExitReadLock();
                }
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
            m_DataLock.EnterWriteLock();
            try
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
            }
            finally
            {
                m_DataLock.ExitWriteLock();
            }
            
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
            m_DataLock.EnterReadLock();
            IEnumerable<IEEGDataInfo> dataInfoCollection;
            try
            {
                dataInfoCollection = m_DataByRequest.Select((d) => d.Key.DataInfo).OfType<IEEGDataInfo>().Distinct().ToList();
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            foreach (var dataInfo in dataInfoCollection)
            {
                m_DataLock.EnterReadLock();
                IEnumerable<BlocRequest> dataRequestCollection;
                try
                {
                    dataRequestCollection = m_BlocDataByRequest.Where((d) => d.Key.DataInfo == dataInfo).Select((d) => d.Key).ToList();
                }
                finally
                {
                    m_DataLock.ExitReadLock();
                }

                switch (dataInfo.Normalization)
                {
                    case NormalizationType.None:
                        foreach (var request in dataRequestCollection)
                        {
                            m_DataLock.EnterReadLock();
                            bool needsNormalization;
                            try
                            {
                                needsNormalization = m_NormalizeByRequest.TryGetValue(request, out NormalizationType currentType) && currentType != NormalizationType.None;
                            }
                            finally
                            {
                                m_DataLock.ExitReadLock();
                            }
                            if (needsNormalization) NormalizeByNone(request);
                        }
                        break;
                    case NormalizationType.SubTrial:
                        foreach (var request in dataRequestCollection)
                        {
                            m_DataLock.EnterReadLock();
                            bool needsNormalization;
                            try
                            {
                                needsNormalization = m_NormalizeByRequest.TryGetValue(request, out NormalizationType currentType) && currentType != NormalizationType.SubTrial;
                            }
                            finally
                            {
                                m_DataLock.ExitReadLock();
                            }
                            if (needsNormalization) NormalizeBySubTrial(request);
                        }
                        break;
                    case NormalizationType.Trial:
                        foreach (var request in dataRequestCollection)
                        {
                            m_DataLock.EnterReadLock();
                            bool needsNormalization;
                            try
                            {
                                needsNormalization = m_NormalizeByRequest.TryGetValue(request, out NormalizationType currentType) && currentType != NormalizationType.Trial;
                            }
                            finally
                            {
                                m_DataLock.ExitReadLock();
                            }
                            if (needsNormalization) NormalizeByTrial(request);
                        }
                        break;
                    case NormalizationType.SubBloc:
                        foreach (var request in dataRequestCollection)
                        {
                            m_DataLock.EnterReadLock();
                            bool needsNormalization;
                            try
                            {
                                needsNormalization = m_NormalizeByRequest.TryGetValue(request, out NormalizationType currentType) && currentType != NormalizationType.SubBloc;
                            }
                            finally
                            {
                                m_DataLock.ExitReadLock();
                            }
                            if (needsNormalization) NormalizeBySubBloc(request);
                        }
                        break;
                    case NormalizationType.Bloc:
                        foreach (var request in dataRequestCollection)
                        {
                            m_DataLock.EnterReadLock();
                            bool needsNormalization;
                            try
                            {
                                needsNormalization = m_NormalizeByRequest.TryGetValue(request, out NormalizationType currentType) && currentType != NormalizationType.Bloc;
                            }
                            finally
                            {
                                m_DataLock.ExitReadLock();
                            }
                            if (needsNormalization) NormalizeByBloc(request);
                        }
                        break;
                    case NormalizationType.Protocol:
                        m_DataLock.EnterReadLock();
                        IEnumerable<Tuple<BlocRequest, bool>> dataRequestAndNeedToNormalize;
                        try
                        {
                            dataRequestAndNeedToNormalize = (from request in dataRequestCollection 
                                                            select new Tuple<BlocRequest, bool>(request, 
                                                                m_NormalizeByRequest.TryGetValue(request, out NormalizationType currentType) && currentType != NormalizationType.Protocol)).ToList();
                        }
                        finally
                        {
                            m_DataLock.ExitReadLock();
                        }
                        if (dataRequestAndNeedToNormalize.Any((tuple) => tuple.Item2))
                        {
                            NormalizeByProtocol(dataRequestAndNeedToNormalize);
                        }
                        break;
                    case NormalizationType.Auto:
                        switch (DefaultNormalization)
                        {
                            case NormalizationType.None:
                                foreach (var request in dataRequestCollection)
                                {
                                    m_DataLock.EnterReadLock();
                                    bool needsNormalization;
                                    try
                                    {
                                        needsNormalization = m_NormalizeByRequest.TryGetValue(request, out NormalizationType currentType) && currentType != NormalizationType.None;
                                    }
                                    finally
                                    {
                                        m_DataLock.ExitReadLock();
                                    }
                                    if (needsNormalization) NormalizeByNone(request);
                                }
                                break;
                            case NormalizationType.SubTrial:
                                foreach (var request in dataRequestCollection)
                                {
                                    m_DataLock.EnterReadLock();
                                    bool needsNormalization;
                                    try
                                    {
                                        needsNormalization = m_NormalizeByRequest.TryGetValue(request, out NormalizationType currentType) && currentType != NormalizationType.SubTrial;
                                    }
                                    finally
                                    {
                                        m_DataLock.ExitReadLock();
                                    }
                                    if (needsNormalization) NormalizeBySubTrial(request);
                                }
                                break;
                            case NormalizationType.Trial:
                                foreach (var request in dataRequestCollection)
                                {
                                    m_DataLock.EnterReadLock();
                                    bool needsNormalization;
                                    try
                                    {
                                        needsNormalization = m_NormalizeByRequest.TryGetValue(request, out NormalizationType currentType) && currentType != NormalizationType.Trial;
                                    }
                                    finally
                                    {
                                        m_DataLock.ExitReadLock();
                                    }
                                    if (needsNormalization) NormalizeByTrial(request);
                                }
                                break;
                            case NormalizationType.SubBloc:
                                foreach (var request in dataRequestCollection)
                                {
                                    m_DataLock.EnterReadLock();
                                    bool needsNormalization;
                                    try
                                    {
                                        needsNormalization = m_NormalizeByRequest.TryGetValue(request, out NormalizationType currentType) && currentType != NormalizationType.SubBloc;
                                    }
                                    finally
                                    {
                                        m_DataLock.ExitReadLock();
                                    }
                                    if (needsNormalization) NormalizeBySubBloc(request);
                                }
                                break;
                            case NormalizationType.Bloc:
                                foreach (var request in dataRequestCollection)
                                {
                                    m_DataLock.EnterReadLock();
                                    bool needsNormalization;
                                    try
                                    {
                                        needsNormalization = m_NormalizeByRequest.TryGetValue(request, out NormalizationType currentType) && currentType != NormalizationType.Bloc;
                                    }
                                    finally
                                    {
                                        m_DataLock.ExitReadLock();
                                    }
                                    if (needsNormalization) NormalizeByBloc(request);
                                }
                                break;
                            case NormalizationType.Protocol:
                                m_DataLock.EnterReadLock();
                                IEnumerable<Tuple<BlocRequest, bool>> dataRequestAndNeedToNormalize2;
                                try
                                {
                                    dataRequestAndNeedToNormalize2 = (from request in dataRequestCollection 
                                                                     select new Tuple<BlocRequest, bool>(request, 
                                                                         m_NormalizeByRequest.TryGetValue(request, out NormalizationType currentType) && currentType != NormalizationType.Protocol)).ToList();
                                }
                                finally
                                {
                                    m_DataLock.ExitReadLock();
                                }
                                if (dataRequestAndNeedToNormalize2.Any((tuple) => tuple.Item2))
                                {
                                    NormalizeByProtocol(dataRequestAndNeedToNormalize2);
                                }
                                break;
                        }
                        break;
                }
            }
            List<BlocRequest> blocRequestsRequiringStatisticsReset = new();
            m_DataLock.EnterWriteLock();
            try
            {
                while (m_BlocRequestsRequiringStatisticsReset.Count > 0)
                {
                    blocRequestsRequiringStatisticsReset.Add(m_BlocRequestsRequiringStatisticsReset.Pop());
                }
            }
            finally
            {
                m_DataLock.ExitWriteLock();
            }
            foreach (var request in blocRequestsRequiringStatisticsReset)
            {
                UnloadStatistics(request);
            }
        }

        /// <summary>
        /// Clean up resources when the DataManager is no longer needed.
        /// Call this method when shutting down the application.
        /// </summary>
        public static void Cleanup()
        {
            m_DataLock?.Dispose();
        }
        #endregion

        #region Private Methods
        static void Load(Request request)
        {
            if (!request.IsValid)
                return;

            m_DataLock.EnterReadLock();
            bool alreadyExists;
            try
            {
                alreadyExists = m_DataByRequest.ContainsKey(request);
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            if (alreadyExists)
                return;

            m_DataLock.EnterWriteLock();
            try
            {
                // Double-check pattern to avoid duplicate loading
                if (m_DataByRequest.ContainsKey(request))
                    return;

                if (request.DataInfo is IEEGDataInfo iEEGDataInfo)
                {
                    IEEGData data = new(iEEGDataInfo);
                    m_DataByRequest.Add(request, data);

                    foreach (var bloc in request.DataInfo.Protocol.Blocs)
                    {
                        m_BlocDataByRequest.Add(new BlocRequest(request.DataInfo, bloc), data.DataByBloc[bloc]);
                        m_NormalizeByRequest.Add(new BlocRequest(request.DataInfo, bloc), NormalizationType.None);
                    }
                }
                else if (request.DataInfo is CCEPDataInfo CCEPDataInfo)
                {
                    CCEPData data = new(CCEPDataInfo);
                    m_DataByRequest.Add(request, data);

                    foreach (var bloc in request.DataInfo.Protocol.Blocs)
                    {
                        m_BlocDataByRequest.Add(new BlocRequest(request.DataInfo, bloc), data.DataByBloc[bloc]);
                        m_NormalizeByRequest.Add(new BlocRequest(request.DataInfo, bloc), NormalizationType.None);
                    }
                }
                else if (request.DataInfo is FMRIDataInfo FMRIDataInfo)
                {
                    FMRIData data = new(FMRIDataInfo);
                    m_DataByRequest.Add(request, data);
                }
                else if (request.DataInfo is StaticDataInfo staticDataInfo)
                {
                    StaticData data = new(staticDataInfo);
                    m_DataByRequest.Add(request, data);
                }
                else if (request.DataInfo is SharedFMRIDataInfo sharedFMRIDataInfo)
                {
                    FMRIData data = new(sharedFMRIDataInfo);
                    m_DataByRequest.Add(request, data);
                }
                else if (request.DataInfo is MEGvDataInfo MEGvDataInfo)
                {
                    MEGvData data = new(MEGvDataInfo);
                    m_DataByRequest.Add(request, data);
                }
                else if (request.DataInfo is MEGcDataInfo MEGcDataInfo)
                {
                    MEGcData data = new(MEGcDataInfo);
                    m_DataByRequest.Add(request, data);
                }
            }
            finally
            {
                m_DataLock.ExitWriteLock();
            }
        }
        static void UnLoad(Request request)
        {
            if (!request.IsValid)
                return;

            m_DataLock.EnterReadLock();
            bool exists;
            try
            {
                exists = m_DataByRequest.ContainsKey(request);
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            if (!exists)
                return;

            m_DataLock.EnterWriteLock();
            try
            {
                if (m_DataByRequest.ContainsKey(request))
                {
                    m_DataByRequest.Remove(request);

                    var channelDataRequestsToRemove = m_ChannelDataByRequest.Keys.Where(k => k.DataInfo == request.DataInfo).ToList();
                    foreach (var channelDataRequest in channelDataRequestsToRemove)
                    {
                        m_ChannelDataByRequest.Remove(channelDataRequest);
                    }

                    var blocChannelDataRequestsToRemove = m_BlocChannelDataByRequest.Keys.Where(k => k.DataInfo == request.DataInfo).ToList();
                    foreach (var blocChannelDataRequest in blocChannelDataRequestsToRemove)
                    {
                        m_BlocChannelDataByRequest.Remove(blocChannelDataRequest);
                    }

                    var blocDataRequestsToRemove = m_BlocDataByRequest.Keys.Where(k => k.DataInfo == request.DataInfo).ToList();
                    foreach (var blocDataRequest in blocDataRequestsToRemove)
                    {
                        m_BlocDataByRequest.Remove(blocDataRequest);
                    }
                }
            }
            finally
            {
                m_DataLock.ExitWriteLock();
            }
        }
        static void UnloadStatistics(BlocRequest request)
        {
            if (!request.IsValid)
                return;

            m_DataLock.EnterWriteLock();
            try
            {
                var channelStatisticsToRemove = m_ChannelStatisticsByRequest.Keys.Where(k => k.DataInfo == request.DataInfo).ToList();
                foreach (var channelRequest in channelStatisticsToRemove)
                {
                    m_ChannelStatisticsByRequest.Remove(channelRequest);
                }

                var blocChannelStatisticsToRemove = m_BlocChannelStatisticsByRequest.Keys.Where(k => k.DataInfo == request.DataInfo && k.Bloc == request.Bloc).ToList();
                foreach (var blocChannelRequest in blocChannelStatisticsToRemove)
                {
                    m_BlocChannelStatisticsByRequest.Remove(blocChannelRequest);
                }

                var eventStatisticsToRemove = m_EventsStatisticsByRequest.Keys.Where(k => k.DataInfo == request.DataInfo).ToList();
                foreach (var eventRequest in eventStatisticsToRemove)
                {
                    m_EventsStatisticsByRequest.Remove(eventRequest);
                }

                if (m_BlocEventsStatisticsByRequest.ContainsKey(request))
                {
                    m_BlocEventsStatisticsByRequest.Remove(request);
                }
            }
            finally
            {
                m_DataLock.ExitWriteLock();
            }
        }

        static Data GetData(Request request)
        {
            if (!request.IsValid)
                return null;

            m_DataLock.EnterReadLock();
            try
            {
                if (m_DataByRequest.TryGetValue(request, out Data result))
                {
                    return result;
                }
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            // Data not found, load it
            Load(request);
            
            m_DataLock.EnterReadLock();
            try
            {
                return m_DataByRequest.TryGetValue(request, out Data result) ? result : null;
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }
        }
        static BlocData GetData(BlocRequest request)
        {
            if (!request.IsValid)
                return null;

            m_DataLock.EnterReadLock();
            try
            {
                if (m_BlocDataByRequest.TryGetValue(request, out BlocData result))
                {
                    return result;
                }
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            // Data not found, load it
            Load(request.DataInfo);
            
            m_DataLock.EnterReadLock();
            try
            {
                return m_BlocDataByRequest.TryGetValue(request, out BlocData result) ? result : null;
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }
        }
        static ChannelData GetData(ChannelRequest request)
        {
            if (!request.IsValid)
                return null;

            m_DataLock.EnterReadLock();
            try
            {
                if (m_ChannelDataByRequest.TryGetValue(request, out ChannelData result))
                {
                    return result;
                }
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            // Channel data not found, create it
            Request dataRequest = new(request.DataInfo);
            
            m_DataLock.EnterReadLock();
            bool dataExists;
            Data data = null;
            try
            {
                dataExists = m_DataByRequest.TryGetValue(dataRequest, out data);
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            if (!dataExists)
            {
                Load(dataRequest);
                m_DataLock.EnterReadLock();
                try
                {
                    m_DataByRequest.TryGetValue(dataRequest, out data);
                }
                finally
                {
                    m_DataLock.ExitReadLock();
                }
            }

            if (data is EpochedData epochedData)
            {
                ChannelData channelData = new(epochedData, request.Channel);
                
                m_DataLock.EnterWriteLock();
                try
                {
                    // Double-check pattern to avoid duplicate creation
                    if (!m_ChannelDataByRequest.ContainsKey(request))
                    {
                        m_ChannelDataByRequest.Add(request, channelData);
                    }
                    return m_ChannelDataByRequest[request];
                }
                finally
                {
                    m_DataLock.ExitWriteLock();
                }
            }

            return null;
        }
        static BlocChannelData GetData(BlocChannelRequest request)
        {
            if (!request.IsValid)
                return null;

            m_DataLock.EnterReadLock();
            try
            {
                if (m_BlocChannelDataByRequest.TryGetValue(request, out BlocChannelData result))
                {
                    return result;
                }
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            // BlocChannel data not found, create it
            Request dataRequest = new(request.DataInfo);
            EpochedData data = GetData(dataRequest) as EpochedData;
            
            if (data != null)
            {
                if (data.UnitByChannel.ContainsKey(request.Channel))
                {
                    BlocRequest blocDataRequest = new(request.DataInfo, request.Bloc);
                    
                    m_DataLock.EnterReadLock();
                    BlocData blocData;
                    try
                    {
                        m_BlocDataByRequest.TryGetValue(blocDataRequest, out blocData);
                    }
                    finally
                    {
                        m_DataLock.ExitReadLock();
                    }

                    if (blocData != null)
                    {
                        BlocChannelData blocChannelData = new(blocData, request.Channel);
                        
                        m_DataLock.EnterWriteLock();
                        try
                        {
                            // Double-check pattern to avoid duplicate creation
                            if (!m_BlocChannelDataByRequest.ContainsKey(request))
                            {
                                m_BlocChannelDataByRequest.Add(request, blocChannelData);
                            }
                            return m_BlocChannelDataByRequest[request];
                        }
                        finally
                        {
                            m_DataLock.ExitWriteLock();
                        }
                    }
                }
            }

            return null;
        }

        // Statistics.
        static ChannelStatistics GetStatistics(ChannelRequest request)
        {
            if (!request.IsValid)
                return null;

            m_DataLock.EnterReadLock();
            try
            {
                if (m_ChannelStatisticsByRequest.TryGetValue(request, out ChannelStatistics result))
                {
                    return result;
                }
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            // Statistics not found, create them
            ChannelData channelData = GetData(request);
            if (channelData != null)
            {
                ChannelStatistics channelStatistics = new(channelData, DefaultAveraging);
                
                m_DataLock.EnterWriteLock();
                try
                {
                    // Double-check pattern to avoid duplicate creation
                    if (!m_ChannelStatisticsByRequest.ContainsKey(request))
                    {
                        m_ChannelStatisticsByRequest.Add(request, channelStatistics);
                    }
                    return m_ChannelStatisticsByRequest[request];
                }
                finally
                {
                    m_DataLock.ExitWriteLock();
                }
            }

            return null;
        }
        static BlocChannelStatistics GetStatistics(BlocChannelRequest request)
        {
            if (!request.IsValid)
                return null;

            m_DataLock.EnterReadLock();
            try
            {
                if (m_BlocChannelStatisticsByRequest.TryGetValue(request, out BlocChannelStatistics result))
                {
                    return result;
                }
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            // Statistics not found, create them
            BlocChannelData blocChannelData = GetData(request);
            if (blocChannelData != null)
            {
                BlocChannelStatistics blocChannelStatistics = new(blocChannelData, DefaultAveraging);
                
                m_DataLock.EnterWriteLock();
                try
                {
                    // Double-check pattern to avoid duplicate creation
                    if (!m_BlocChannelStatisticsByRequest.ContainsKey(request))
                    {
                        m_BlocChannelStatisticsByRequest.Add(request, blocChannelStatistics);
                    }
                    return m_BlocChannelStatisticsByRequest[request];
                }
                finally
                {
                    m_DataLock.ExitWriteLock();
                }
            }

            return null;
        }
        static EventsStatistics GetEventsStatistics(Request request)
        {
            if (!request.IsValid)
                return null;

            m_DataLock.EnterReadLock();
            try
            {
                if (m_EventsStatisticsByRequest.TryGetValue(request, out EventsStatistics result))
                {
                    return result;
                }
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            // Statistics not found, create them
            EventsStatistics eventsStatistics = new(request.DataInfo);
            
            m_DataLock.EnterWriteLock();
            try
            {
                // Double-check pattern to avoid duplicate creation
                if (!m_EventsStatisticsByRequest.ContainsKey(request))
                {
                    foreach (var pair in eventsStatistics.EventsStatisticsByBloc) 
                    {
                        var blocRequest = new BlocRequest(request.DataInfo, pair.Key);
                        if (!m_BlocEventsStatisticsByRequest.ContainsKey(blocRequest))
                        {
                            m_BlocEventsStatisticsByRequest.Add(blocRequest, pair.Value);
                        }
                    }
                    m_EventsStatisticsByRequest.Add(request, eventsStatistics);
                }
                return m_EventsStatisticsByRequest[request];
            }
            finally
            {
                m_DataLock.ExitWriteLock();
            }
        }
        static BlocEventsStatistics GetEventsStatistics(BlocRequest request)
        {
            if (!request.IsValid)
                return null;

            m_DataLock.EnterReadLock();
            try
            {
                if (m_BlocEventsStatisticsByRequest.TryGetValue(request, out BlocEventsStatistics result))
                {
                    return result;
                }
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            // Statistics not found, create them
            BlocEventsStatistics blocEventsStatistics = new(request.DataInfo, request.Bloc, DefaultPositionAveraging);
            
            m_DataLock.EnterWriteLock();
            try
            {
                // Double-check pattern to avoid duplicate creation
                if (!m_BlocEventsStatisticsByRequest.ContainsKey(request))
                {
                    m_BlocEventsStatisticsByRequest.Add(request, blocEventsStatistics);
                }
                return m_BlocEventsStatisticsByRequest[request];
            }
            finally
            {
                m_DataLock.ExitWriteLock();
            }
        }

        static void NormalizeByNone(BlocRequest request)
        {
            BlocData blocData = GetData(request);
            if (blocData != null)
            {
                foreach (var trial in blocData.Trials)
                {
                    foreach (var subTrial in trial.SubTrialBySubBloc.Values)
                    {
                        subTrial.Normalize(0, 1);
                    }
                }
                
                m_DataLock.EnterWriteLock();
                try
                {
                    m_NormalizeByRequest[request] = NormalizationType.None;
                    m_BlocRequestsRequiringStatisticsReset.Push(request);
                }
                finally
                {
                    m_DataLock.ExitWriteLock();
                }
            }
        }
        static void NormalizeBySubTrial(BlocRequest request)
        {
            BlocData blocData = GetData(request);
            if (blocData != null)
            {
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
                
                m_DataLock.EnterWriteLock();
                try
                {
                    m_NormalizeByRequest[request] = NormalizationType.SubTrial;
                    m_BlocRequestsRequiringStatisticsReset.Push(request);
                }
                finally
                {
                    m_DataLock.ExitWriteLock();
                }
            }
        }
        static void NormalizeByTrial(BlocRequest request)
        {
            BlocData epochedData = GetData(request);
            if (epochedData != null)
            {
                foreach (var trial in epochedData.Trials)
                {
                    Dictionary<string, List<float>> baselineByChannel = new();
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
                
                m_DataLock.EnterWriteLock();
                try
                {
                    m_NormalizeByRequest[request] = NormalizationType.Trial;
                    m_BlocRequestsRequiringStatisticsReset.Push(request);
                }
                finally
                {
                    m_DataLock.ExitWriteLock();
                }
            }
        }
        static void NormalizeBySubBloc(BlocRequest request)
        {
            Dictionary<string, List<float>> baselineByChannel;
            BlocData epochedData = GetData(request);
            if (epochedData != null)
            {
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
                
                m_DataLock.EnterWriteLock();
                try
                {
                    m_NormalizeByRequest[request] = NormalizationType.SubBloc;
                    m_BlocRequestsRequiringStatisticsReset.Push(request);
                }
                finally
                {
                    m_DataLock.ExitWriteLock();
                }
            }
        }
        static void NormalizeByBloc(BlocRequest request)
        {
            Dictionary<string, List<float>> baselineByChannel = new();
            
            m_DataLock.EnterReadLock();
            BlocData epochedData;
            try
            {
                m_BlocDataByRequest.TryGetValue(request, out epochedData);
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            if (epochedData != null)
            {
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
                
                m_DataLock.EnterWriteLock();
                try
                {
                    m_NormalizeByRequest[request] = NormalizationType.Bloc;
                    m_BlocRequestsRequiringStatisticsReset.Push(request);
                }
                finally
                {
                    m_DataLock.ExitWriteLock();
                }
            }
        }
        static void NormalizeByProtocol(IEnumerable<Tuple<BlocRequest, bool>> dataRequestAndNeedToNormalize)
        {
            Dictionary<string, List<float>> baselineByChannel = new();

            m_DataLock.EnterReadLock();
            var epochedDataList = new List<BlocData>();
            try
            {
                foreach (var tuple in dataRequestAndNeedToNormalize)
                {
                    if (m_BlocDataByRequest.TryGetValue(tuple.Item1, out BlocData epochedData))
                    {
                        epochedDataList.Add(epochedData);
                    }
                }
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            // Calculate baseline statistics
            foreach (var epochedData in epochedDataList)
            {
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

            // Apply normalization
            float average, standardDeviation;
            foreach (var channel in baselineByChannel.Keys)
            {
                average = baselineByChannel[channel].ToArray().Mean();
                standardDeviation = baselineByChannel[channel].ToArray().StandardDeviation();
                
                foreach (var tuple in dataRequestAndNeedToNormalize)
                {
                    if (tuple.Item2)
                    {
                        m_DataLock.EnterReadLock();
                        BlocData epochedData;
                        try
                        {
                            m_BlocDataByRequest.TryGetValue(tuple.Item1, out epochedData);
                        }
                        finally
                        {
                            m_DataLock.ExitReadLock();
                        }

                        if (epochedData != null)
                        {
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
            }

            // Update normalization status
            m_DataLock.EnterWriteLock();
            try
            {
                foreach (var tuple in dataRequestAndNeedToNormalize)
                {
                    if (tuple.Item2)
                    {
                        m_NormalizeByRequest[tuple.Item1] = NormalizationType.Protocol;
                        m_BlocRequestsRequiringStatisticsReset.Push(tuple.Item1);
                    }
                }
            }
            finally
            {
                m_DataLock.ExitWriteLock();
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
                    return base.IsValid && DataInfo.Protocol.Blocs.Contains(Bloc) && DataInfo is IEpochable;
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
                    return base.IsValid && DataInfo.Protocol.Blocs.Contains(Bloc); // AddTestOnChannel
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
