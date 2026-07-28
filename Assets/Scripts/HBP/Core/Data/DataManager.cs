using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
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
        static readonly Dictionary<DataInfo, int> m_ActiveDataPinCounts = new();
        static readonly Dictionary<DataInfo, Stack<RawRecordingSourceKey>> m_ActiveRawSourceKeys = new();
        static readonly MemoryCacheBudget m_MemoryBudget = new();
        static readonly RawRecordingCache m_RawRecordingCache = new(m_MemoryBudget);
        internal static Func<EEGRecordingSource, DynamicData> RawRecordingLoader { get; set; } = source => new DynamicData(source);
        internal static int RawRecordingCacheCount => m_RawRecordingCache.Count;
        public static MemoryCacheSnapshot MemoryCacheSnapshot => m_MemoryBudget.GetSnapshot();

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

        // Default values and the derived-cache dimensions that depend on them.
        static NormalizationType m_DefaultNormalization = NormalizationType.None;
        static AveragingType m_DefaultAveraging = AveragingType.Mean;
        static AveragingType m_DefaultPositionAveraging = AveragingType.Mean;

        public static NormalizationType DefaultNormalization
        {
            get
            {
                m_DataLock.EnterReadLock();
                try
                {
                    return m_DefaultNormalization;
                }
                finally
                {
                    m_DataLock.ExitReadLock();
                }
            }
            set
            {
                m_DataLock.EnterWriteLock();
                try
                {
                    m_DefaultNormalization = value;
                }
                finally
                {
                    m_DataLock.ExitWriteLock();
                }
            }
        }

        public static AveragingType DefaultAveraging
        {
            get
            {
                m_DataLock.EnterReadLock();
                try
                {
                    return m_DefaultAveraging;
                }
                finally
                {
                    m_DataLock.ExitReadLock();
                }
            }
            set
            {
                m_DataLock.EnterWriteLock();
                try
                {
                    if (m_DefaultAveraging == value)
                        return;
                    m_DefaultAveraging = value;
                    m_ChannelStatisticsByRequest = new Dictionary<ChannelRequest, ChannelStatistics>();
                    m_BlocChannelStatisticsByRequest = new Dictionary<BlocChannelRequest, BlocChannelStatistics>();
                }
                finally
                {
                    m_DataLock.ExitWriteLock();
                }
            }
        }

        public static AveragingType DefaultPositionAveraging
        {
            get
            {
                m_DataLock.EnterReadLock();
                try
                {
                    return m_DefaultPositionAveraging;
                }
                finally
                {
                    m_DataLock.ExitReadLock();
                }
            }
            set
            {
                m_DataLock.EnterWriteLock();
                try
                {
                    if (m_DefaultPositionAveraging == value)
                        return;
                    m_DefaultPositionAveraging = value;
                    m_EventsStatisticsByRequest = new Dictionary<Request, EventsStatistics>();
                    m_BlocEventsStatisticsByRequest = new Dictionary<BlocRequest, BlocEventsStatistics>();
                }
                finally
                {
                    m_DataLock.ExitWriteLock();
                }
            }
        }

        public static bool HasData
        {
            get
            {
                m_DataLock.EnterReadLock();
                try
                {
                    return m_DataByRequest.Count > 0 || m_BlocDataByRequest.Count > 0 || m_ChannelDataByRequest.Count > 0 || m_BlocChannelDataByRequest.Count > 0 || m_ChannelStatisticsByRequest.Count > 0 || m_BlocChannelStatisticsByRequest.Count > 0 || m_EventsStatisticsByRequest.Count > 0 || m_BlocEventsStatisticsByRequest.Count > 0;
                }
                finally
                {
                    m_DataLock.ExitReadLock();
                }
            }
        }

        #endregion

        static DataManager()
        {
            m_MemoryBudget.BudgetExceeded += snapshot => UnityEngine.Debug.LogWarning($"Active time-series data exceed the memory cache budget: {snapshot.UsedBytes / (1024d * 1024d):N1} MiB used, " + $"{snapshot.LimitBytes / (1024d * 1024d):N1} MiB allowed. Active exact data remain pinned; no downsampling was applied.");
        }

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
            Clear(true);
        }

        public static void ClearDerivedData()
        {
            Clear(false);
        }

        private static void Clear(bool clearRawRecordings)
        {
            DataInfo[] trackedDataInfos;
            m_DataLock.EnterWriteLock();
            try
            {
                trackedDataInfos = m_DataByRequest.Keys.Select(request => request.DataInfo).Distinct().ToArray();
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

                m_BlocRequestsRequiringStatisticsReset.Clear();
                m_BlocRequestsRequiringStatisticsReset = new Stack<BlocRequest>();
                if (clearRawRecordings)
                {
                    m_ActiveDataPinCounts.Clear();
                    m_ActiveRawSourceKeys.Clear();
                }
            }
            finally
            {
                m_DataLock.ExitWriteLock();
            }

            if (clearRawRecordings)
                m_RawRecordingCache.Clear();
            foreach (DataInfo dataInfo in trackedDataInfos)
                m_MemoryBudget.Unregister(dataInfo);
        }

        public static void ConfigureMemoryBudget(int explicitLimitMiB, int totalPhysicalMemoryMiB)
        {
            m_MemoryBudget.Configure(explicitLimitMiB, totalPhysicalMemoryMiB);
        }

        public static void RegisterMemoryUsage(object owner, MemoryCacheCategory category, long bytes, bool pinned = true)
        {
            m_MemoryBudget.Register(owner, category, bytes, pinned, null);
        }

        public static void UnregisterMemoryUsage(object owner)
        {
            m_MemoryBudget.Unregister(owner);
        }

        internal static void PinData(DataInfo dataInfo)
        {
            RawRecordingSourceKey sourceKey = RawRecordingSourceKey.From(EEGRecordingSource.From(dataInfo));
            m_RawRecordingCache.Pin(sourceKey);
            m_DataLock.EnterWriteLock();
            try
            {
                m_ActiveDataPinCounts.TryGetValue(dataInfo, out int count);
                m_ActiveDataPinCounts[dataInfo] = count + 1;
                if (!m_ActiveRawSourceKeys.TryGetValue(dataInfo, out Stack<RawRecordingSourceKey> sourceKeys))
                {
                    sourceKeys = new Stack<RawRecordingSourceKey>();
                    m_ActiveRawSourceKeys[dataInfo] = sourceKeys;
                }

                sourceKeys.Push(sourceKey);
            }
            finally
            {
                m_DataLock.ExitWriteLock();
            }

            m_MemoryBudget.SetPinned(dataInfo, true);
        }

        internal static void UnpinData(DataInfo dataInfo)
        {
            bool pinned;
            bool hasSourceKey = false;
            RawRecordingSourceKey sourceKey = default;
            m_DataLock.EnterWriteLock();
            try
            {
                m_ActiveDataPinCounts.TryGetValue(dataInfo, out int count);
                count = Math.Max(0, count - 1);
                pinned = count > 0;
                if (pinned) m_ActiveDataPinCounts[dataInfo] = count;
                else m_ActiveDataPinCounts.Remove(dataInfo);
                if (m_ActiveRawSourceKeys.TryGetValue(dataInfo, out Stack<RawRecordingSourceKey> sourceKeys) && sourceKeys.Count > 0)
                {
                    sourceKey = sourceKeys.Pop();
                    hasSourceKey = true;
                    if (sourceKeys.Count == 0)
                        m_ActiveRawSourceKeys.Remove(dataInfo);
                }
            }
            finally
            {
                m_DataLock.ExitWriteLock();
            }

            m_MemoryBudget.SetPinned(dataInfo, pinned);
            if (hasSourceKey)
                m_RawRecordingCache.Unpin(sourceKey);
        }

        internal static void ResetRawRecordingLoader()
        {
            RawRecordingLoader = source => new DynamicData(source);
        }

        public static Data GetData(DataInfo dataInfo)
        {
            return GetData(dataInfo, true);
        }

        public static Data GetData(DataInfo dataInfo, bool updateMemoryUsage)
        {
            Data result = GetData(new Request(dataInfo));
            if (updateMemoryUsage)
                UpdateDerivedMemoryUsage(dataInfo);
            return result;
        }

        public static BlocData GetData(DataInfo dataInfo, Bloc bloc)
        {
            return GetData(dataInfo, bloc, true);
        }

        public static BlocData GetData(DataInfo dataInfo, Bloc bloc, bool updateMemoryUsage)
        {
            BlocData result = GetData(new BlocRequest(dataInfo, bloc));
            if (updateMemoryUsage)
                UpdateDerivedMemoryUsage(dataInfo);
            return result;
        }

        public static ChannelData GetData(DataInfo dataInfo, string channel)
        {
            ChannelData result = GetData(new ChannelRequest(dataInfo, channel));
            UpdateDerivedMemoryUsage(dataInfo);
            return result;
        }

        public static BlocChannelData GetData(DataInfo dataInfo, Bloc bloc, string channel)
        {
            return GetData(dataInfo, bloc, channel, true);
        }

        public static BlocChannelData GetData(DataInfo dataInfo, Bloc bloc, string channel, bool updateMemoryUsage)
        {
            BlocChannelData result = GetData(new BlocChannelRequest(dataInfo, bloc, channel));
            if (updateMemoryUsage)
                UpdateDerivedMemoryUsage(dataInfo);
            return result;
        }

        // Statistics.
        public static ChannelStatistics GetStatistics(DataInfo dataInfo, string channel)
        {
            ChannelStatistics result = GetStatistics(new ChannelRequest(dataInfo, channel));
            UpdateDerivedMemoryUsage(dataInfo);
            return result;
        }

        public static BlocChannelStatistics GetStatistics(DataInfo dataInfo, Bloc bloc, string channel)
        {
            return GetStatistics(dataInfo, bloc, channel, true);
        }

        public static BlocChannelStatistics GetStatistics(DataInfo dataInfo, Bloc bloc, string channel, bool updateMemoryUsage)
        {
            BlocChannelStatistics result = GetStatistics(new BlocChannelRequest(dataInfo, bloc, channel));
            if (updateMemoryUsage)
                UpdateDerivedMemoryUsage(dataInfo);
            return result;
        }

        public static EventsStatistics GetEventsStatistics(DataInfo dataInfo)
        {
            EventsStatistics result = GetEventsStatistics(new Request(dataInfo));
            UpdateDerivedMemoryUsage(dataInfo);
            return result;
        }

        public static BlocEventsStatistics GetEventsStatistics(DataInfo dataInfo, Bloc bloc)
        {
            return GetEventsStatistics(dataInfo, bloc, true);
        }

        public static BlocEventsStatistics GetEventsStatistics(DataInfo dataInfo, Bloc bloc, bool updateMemoryUsage)
        {
            BlocEventsStatistics result = GetEventsStatistics(new BlocRequest(dataInfo, bloc));
            if (updateMemoryUsage)
                UpdateDerivedMemoryUsage(dataInfo);
            return result;
        }

        public static void RefreshDerivedMemoryUsage(DataInfo dataInfo)
        {
            UpdateDerivedMemoryUsage(dataInfo);
        }

        public static void NormalizeiEEGData(bool useParallelProcessing = false)
        {
            m_DataLock.EnterReadLock();
            List<IEEGDataInfo> dataInfoCollection;
            try
            {
                dataInfoCollection = m_DataByRequest.Select((d) => d.Key.DataInfo).OfType<IEEGDataInfo>().Distinct().ToList();
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            Action<IEEGDataInfo> normalizeDataInfo = dataInfo =>
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
                            dataRequestAndNeedToNormalize = (from request in dataRequestCollection select new Tuple<BlocRequest, bool>(request, m_NormalizeByRequest.TryGetValue(request, out NormalizationType currentType) && currentType != NormalizationType.Protocol)).ToList();
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
                                    dataRequestAndNeedToNormalize2 = (from request in dataRequestCollection select new Tuple<BlocRequest, bool>(request, m_NormalizeByRequest.TryGetValue(request, out NormalizationType currentType) && currentType != NormalizationType.Protocol)).ToList();
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
            };

            int normalizationDegree = useParallelProcessing && dataInfoCollection.Count > 1 ? Math.Min(5, dataInfoCollection.Count) : 1;
            if (normalizationDegree > 1)
            {
                Parallel.ForEach(dataInfoCollection, new ParallelOptions { MaxDegreeOfParallelism = normalizationDegree }, normalizeDataInfo);
            }
            else
            {
                foreach (IEEGDataInfo dataInfo in dataInfoCollection)
                    normalizeDataInfo(dataInfo);
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

            m_RawRecordingCache.DiscardUnpinnedCompactRecordings();
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

            if (request.DataInfo is IEEGDataInfo || request.DataInfo is CCEPDataInfo || request.DataInfo is MEGcDataInfo)
            {
                LoadRawBackedData(request);
                return;
            }

            m_DataLock.EnterWriteLock();
            try
            {
                // Double-check pattern to avoid duplicate loading
                if (m_DataByRequest.ContainsKey(request))
                    return;

                if (request.DataInfo is FMRIDataInfo FMRIDataInfo)
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
            }
            finally
            {
                m_DataLock.ExitWriteLock();
            }
        }

        static void LoadRawBackedData(Request request)
        {
            EEGRecordingSource source = EEGRecordingSource.From(request.DataInfo);
            RawRecordingSourceKey sourceKey = RawRecordingSourceKey.From(source);
            DynamicData rawData = m_RawRecordingCache.GetOrLoad(sourceKey, () => RawRecordingLoader(source));
            PublishLoadedValidationMetadata(request.DataInfo, rawData.ValidationMetadata);

            Data data;
            if (request.DataInfo is IEEGDataInfo iEEGDataInfo)
                data = new IEEGData(iEEGDataInfo, rawData);
            else if (request.DataInfo is CCEPDataInfo ccepDataInfo)
                data = new CCEPData(ccepDataInfo, rawData);
            else
                data = new MEGcData(rawData);

            m_DataLock.EnterWriteLock();
            try
            {
                if (m_DataByRequest.ContainsKey(request))
                    return;

                List<BlocRequest> publishedBlocRequests = new();
                try
                {
                    m_DataByRequest.Add(request, data);
                    if (data is EpochedData epochedData)
                    {
                        foreach (Bloc bloc in request.DataInfo.Protocol.Blocs)
                        {
                            BlocRequest blocRequest = new(request.DataInfo, bloc);
                            publishedBlocRequests.Add(blocRequest);
                            m_BlocDataByRequest.Add(blocRequest, epochedData.DataByBloc[bloc]);
                            m_NormalizeByRequest.Add(blocRequest, NormalizationType.None);
                        }
                    }
                }
                catch
                {
                    m_DataByRequest.Remove(request);
                    foreach (BlocRequest blocRequest in publishedBlocRequests)
                    {
                        m_BlocDataByRequest.Remove(blocRequest);
                        m_NormalizeByRequest.Remove(blocRequest);
                    }

                    throw;
                }
            }
            finally
            {
                m_DataLock.ExitWriteLock();
            }

            // Compact epoch backing is self-contained. Keep only the most recently
            // used unpinned recording to preserve cheap immediate reuse without
            // retaining every patient's full raw file for the visualization lifetime.
            if (data is EpochedData)
                m_RawRecordingCache.RetainOnlyUnpinned(sourceKey);
        }

        private static void PublishLoadedValidationMetadata(DataInfo dataInfo, EEGValidationMetadata metadata)
        {
            ValidationAspect aspects = ValidationAspect.SourceReadability;
            if (dataInfo is IEEGDataInfo || dataInfo is CCEPDataInfo)
            {
                aspects |= ValidationAspect.Epoching | ValidationAspect.ChannelMapping;
            }

            ValidationRequest request = new(aspects, dataInfoIDs: new[] { dataInfo.ID }, force: true);
            string sourceDefinition = DataInfoValidationContext.GetSourceDefinitionSignature(dataInfo);
            DataInfo snapshot = dataInfo.CreateValidationSnapshot(request, true, new LoadedMetadataReader(metadata));
            if (snapshot != null && string.Equals(sourceDefinition, DataInfoValidationContext.GetSourceDefinitionSignature(dataInfo), StringComparison.Ordinal))
            {
                dataInfo.ApplyValidationState(snapshot);
            }
        }

        private sealed class LoadedMetadataReader : IEEGValidationMetadataReader
        {
            private readonly EEGValidationMetadata m_Metadata;

            public LoadedMetadataReader(EEGValidationMetadata metadata)
            {
                m_Metadata = metadata;
            }

            public EEGValidationMetadata Read(DataInfo dataInfo)
            {
                return m_Metadata;
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

                    var normalizationRequestsToRemove = m_NormalizeByRequest.Keys.Where(k => k.DataInfo == request.DataInfo).ToList();
                    foreach (var normalizationRequest in normalizationRequestsToRemove)
                    {
                        m_NormalizeByRequest.Remove(normalizationRequest);
                    }

                    var channelStatisticsToRemove = m_ChannelStatisticsByRequest.Keys.Where(k => k.DataInfo == request.DataInfo).ToList();
                    foreach (var channelStatisticsRequest in channelStatisticsToRemove)
                    {
                        m_ChannelStatisticsByRequest.Remove(channelStatisticsRequest);
                    }

                    var blocChannelStatisticsToRemove = m_BlocChannelStatisticsByRequest.Keys.Where(k => k.DataInfo == request.DataInfo).ToList();
                    foreach (var blocChannelStatisticsRequest in blocChannelStatisticsToRemove)
                    {
                        m_BlocChannelStatisticsByRequest.Remove(blocChannelStatisticsRequest);
                    }

                    var eventStatisticsToRemove = m_EventsStatisticsByRequest.Keys.Where(k => k.DataInfo == request.DataInfo).ToList();
                    foreach (var eventStatisticsRequest in eventStatisticsToRemove)
                    {
                        m_EventsStatisticsByRequest.Remove(eventStatisticsRequest);
                    }

                    var blocEventStatisticsToRemove = m_BlocEventsStatisticsByRequest.Keys.Where(k => k.DataInfo == request.DataInfo).ToList();
                    foreach (var blocEventStatisticsRequest in blocEventStatisticsToRemove)
                    {
                        m_BlocEventsStatisticsByRequest.Remove(blocEventStatisticsRequest);
                    }

                    m_BlocRequestsRequiringStatisticsReset = new Stack<BlocRequest>(m_BlocRequestsRequiringStatisticsReset.Where(k => k.DataInfo != request.DataInfo).Reverse());
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
            BlocData blocData = GetData(request);
            BlocEventsStatistics blocEventsStatistics = new(blocData, request.Bloc, DefaultPositionAveraging);

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
            EpochCompatibilityBuffer compatibilityBuffer = new();
            BlocData blocData = GetData(request);
            if (blocData != null)
            {
                foreach (var trial in blocData.Trials)
                {
                    foreach (var subTrial in trial.SubTrialBySubBloc.Values)
                    {
                        foreach (string channel in subTrial.Channels)
                            subTrial.Normalize(0, 1, channel, compatibilityBuffer, normalizedResult: false);
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
            EpochCompatibilityBuffer compatibilityBuffer = new();
            BlocData blocData = GetData(request);
            if (blocData != null)
            {
                foreach (var trial in blocData.Trials)
                {
                    foreach (var subTrial in trial.SubTrialBySubBloc.Values)
                    {
                        foreach (string channel in subTrial.Channels)
                        {
                            subTrial.GetBaselineStatistics(channel, compatibilityBuffer, out float average, out float standardDeviation);
                            subTrial.Normalize(average, standardDeviation, channel, compatibilityBuffer);
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
            EpochCompatibilityBuffer compatibilityBuffer = new();
            BlocData epochedData = GetData(request);
            if (epochedData != null)
            {
                foreach (var trial in epochedData.Trials)
                {
                    Dictionary<string, RunningStatistics> baselineByChannel = new();
                    foreach (var subTrial in trial.SubTrialBySubBloc.Values)
                    {
                        foreach (string channel in subTrial.Channels)
                        {
                            AccumulateBaseline(baselineByChannel, channel, subTrial, compatibilityBuffer);
                        }
                    }

                    float average, standardDeviation;
                    foreach (var channel in baselineByChannel.Keys)
                    {
                        average = baselineByChannel[channel].Mean;
                        standardDeviation = baselineByChannel[channel].StandardDeviation;
                        foreach (var subTrial in trial.SubTrialBySubBloc.Values)
                        {
                            subTrial.Normalize(average, standardDeviation, channel, compatibilityBuffer);
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
            EpochCompatibilityBuffer compatibilityBuffer = new();
            Dictionary<string, RunningStatistics> baselineByChannel;
            BlocData epochedData = GetData(request);
            if (epochedData != null)
            {
                foreach (var subBloc in request.Bloc.SubBlocs)
                {
                    baselineByChannel = new Dictionary<string, RunningStatistics>();
                    foreach (var trial in epochedData.Trials)
                    {
                        SubTrial subTrial = trial.SubTrialBySubBloc[subBloc];
                        foreach (string channel in subTrial.Channels)
                        {
                            AccumulateBaseline(baselineByChannel, channel, subTrial, compatibilityBuffer);
                        }
                    }

                    float average, standardDeviation;
                    foreach (var channel in baselineByChannel.Keys)
                    {
                        average = baselineByChannel[channel].Mean;
                        standardDeviation = baselineByChannel[channel].StandardDeviation;
                        foreach (var trial in epochedData.Trials)
                        {
                            SubTrial subTrial = trial.SubTrialBySubBloc[subBloc];
                            subTrial.Normalize(average, standardDeviation, channel, compatibilityBuffer);
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
            EpochCompatibilityBuffer compatibilityBuffer = new();
            Dictionary<string, RunningStatistics> baselineByChannel = new();

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
                        foreach (string channel in subTrial.Channels)
                        {
                            AccumulateBaseline(baselineByChannel, channel, subTrial, compatibilityBuffer);
                        }
                    }
                }

                float average, standardDeviation;
                foreach (var channel in baselineByChannel.Keys)
                {
                    average = baselineByChannel[channel].Mean;
                    standardDeviation = baselineByChannel[channel].StandardDeviation;
                    foreach (var trial in epochedData.Trials)
                    {
                        foreach (var subTrial in trial.SubTrialBySubBloc.Values)
                        {
                            subTrial.Normalize(average, standardDeviation, channel, compatibilityBuffer);
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
            EpochCompatibilityBuffer compatibilityBuffer = new();
            Dictionary<string, RunningStatistics> baselineByChannel = new();

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
                        foreach (string channel in subTrial.Channels)
                        {
                            AccumulateBaseline(baselineByChannel, channel, subTrial, compatibilityBuffer);
                        }
                    }
                }
            }

            // Apply normalization
            float average, standardDeviation;
            foreach (var channel in baselineByChannel.Keys)
            {
                average = baselineByChannel[channel].Mean;
                standardDeviation = baselineByChannel[channel].StandardDeviation;

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
                                    subTrial.Normalize(average, standardDeviation, channel, compatibilityBuffer);
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

        static void AccumulateBaseline(Dictionary<string, RunningStatistics> baselineByChannel, string channel, SubTrial subTrial, EpochCompatibilityBuffer compatibilityBuffer)
        {
            baselineByChannel.TryGetValue(channel, out RunningStatistics statistics);
            subTrial.AccumulateBaselineStatistics(channel, compatibilityBuffer, ref statistics);
            baselineByChannel[channel] = statistics;
        }

        static void UpdateDerivedMemoryUsage(DataInfo dataInfo)
        {
            if (dataInfo == null)
                return;

            long bytes = 0;
            HashSet<BlocChannelStatistics> statisticsObjects = new();
            bool pinned;
            m_DataLock.EnterReadLock();
            try
            {
                EpochedData epochedData = null;
                if (m_DataByRequest.TryGetValue(new Request(dataInfo), out Data data))
                    epochedData = data as EpochedData;
                if (epochedData != null)
                {
                    foreach (BlocData blocData in epochedData.DataByBloc.Values)
                    {
                        foreach (Trial trial in blocData.Trials)
                        {
                            foreach (SubTrial subTrial in trial.SubTrialBySubBloc.Values)
                            {
                                bytes += subTrial.ManagedBytes;
                            }
                        }
                    }
                }

                if (epochedData != null)
                {
                    foreach (Bloc bloc in epochedData.DataByBloc.Keys)
                    {
                        foreach (string channel in epochedData.UnitByChannel.Keys)
                        {
                            if (m_BlocChannelStatisticsByRequest.TryGetValue(new BlocChannelRequest(dataInfo, bloc, channel), out BlocChannelStatistics statistics))
                                statisticsObjects.Add(statistics);
                        }
                    }
                }

                if (epochedData != null)
                {
                    foreach (string channel in epochedData.UnitByChannel.Keys)
                    {
                        if (!m_ChannelStatisticsByRequest.TryGetValue(new ChannelRequest(dataInfo, channel), out ChannelStatistics channelStatistics))
                            continue;
                        foreach (BlocChannelStatistics statistics in channelStatistics.StatisticsByBloc.Values)
                            statisticsObjects.Add(statistics);
                    }
                }

                pinned = m_ActiveDataPinCounts.TryGetValue(dataInfo, out int count) && count > 0;
            }
            finally
            {
                m_DataLock.ExitReadLock();
            }

            foreach (BlocChannelStatistics statistics in statisticsObjects)
                bytes += statistics.ManagedBytes;

            m_MemoryBudget.Register(dataInfo, MemoryCacheCategory.ManagedDerived, bytes, pinned, () => EvictDerivedData(dataInfo));
        }

        static void EvictDerivedData(DataInfo dataInfo)
        {
            HashSet<Data> dataToClear = new();
            HashSet<BlocData> blocDataToClear = new();
            HashSet<ChannelData> channelDataToClear = new();
            HashSet<BlocChannelData> blocChannelDataToClear = new();
            HashSet<ChannelStatistics> channelStatisticsToClear = new();
            HashSet<BlocChannelStatistics> blocChannelStatisticsToClear = new();
            HashSet<EventsStatistics> eventStatisticsToClear = new();
            HashSet<BlocEventsStatistics> blocEventStatisticsToClear = new();

            m_DataLock.EnterWriteLock();
            try
            {
                foreach (Request request in m_DataByRequest.Keys.Where(key => key.DataInfo == dataInfo).ToArray())
                {
                    dataToClear.Add(m_DataByRequest[request]);
                    m_DataByRequest.Remove(request);
                }

                foreach (BlocRequest request in m_BlocDataByRequest.Keys.Where(key => key.DataInfo == dataInfo).ToArray())
                {
                    blocDataToClear.Add(m_BlocDataByRequest[request]);
                    m_BlocDataByRequest.Remove(request);
                }

                foreach (ChannelRequest request in m_ChannelDataByRequest.Keys.Where(key => key.DataInfo == dataInfo).ToArray())
                {
                    channelDataToClear.Add(m_ChannelDataByRequest[request]);
                    m_ChannelDataByRequest.Remove(request);
                }

                foreach (BlocChannelRequest request in m_BlocChannelDataByRequest.Keys.Where(key => key.DataInfo == dataInfo).ToArray())
                {
                    blocChannelDataToClear.Add(m_BlocChannelDataByRequest[request]);
                    m_BlocChannelDataByRequest.Remove(request);
                }

                foreach (ChannelRequest request in m_ChannelStatisticsByRequest.Keys.Where(key => key.DataInfo == dataInfo).ToArray())
                {
                    channelStatisticsToClear.Add(m_ChannelStatisticsByRequest[request]);
                    m_ChannelStatisticsByRequest.Remove(request);
                }

                foreach (BlocChannelRequest request in m_BlocChannelStatisticsByRequest.Keys.Where(key => key.DataInfo == dataInfo).ToArray())
                {
                    blocChannelStatisticsToClear.Add(m_BlocChannelStatisticsByRequest[request]);
                    m_BlocChannelStatisticsByRequest.Remove(request);
                }

                foreach (Request request in m_EventsStatisticsByRequest.Keys.Where(key => key.DataInfo == dataInfo).ToArray())
                {
                    eventStatisticsToClear.Add(m_EventsStatisticsByRequest[request]);
                    m_EventsStatisticsByRequest.Remove(request);
                }

                foreach (BlocRequest request in m_BlocEventsStatisticsByRequest.Keys.Where(key => key.DataInfo == dataInfo).ToArray())
                {
                    blocEventStatisticsToClear.Add(m_BlocEventsStatisticsByRequest[request]);
                    m_BlocEventsStatisticsByRequest.Remove(request);
                }

                foreach (BlocRequest request in m_NormalizeByRequest.Keys.Where(key => key.DataInfo == dataInfo).ToArray())
                    m_NormalizeByRequest.Remove(request);
                m_BlocRequestsRequiringStatisticsReset = new Stack<BlocRequest>(m_BlocRequestsRequiringStatisticsReset.Where(request => request.DataInfo != dataInfo).Reverse());
            }
            finally
            {
                m_DataLock.ExitWriteLock();
            }

            foreach (Data data in dataToClear)
                data.Clear();
            foreach (BlocData blocData in blocDataToClear)
                blocData.Clear();
            foreach (ChannelData channelData in channelDataToClear)
                channelData.Clear();
            foreach (BlocChannelData blocChannelData in blocChannelDataToClear)
                blocChannelData.Clear();
            foreach (ChannelStatistics channelStatistics in channelStatisticsToClear)
                channelStatistics.Clear();
            foreach (BlocChannelStatistics blocChannelStatistics in blocChannelStatisticsToClear)
                blocChannelStatistics.Clear();
            foreach (EventsStatistics eventStatistics in eventStatisticsToClear)
                eventStatistics.Clear();
            foreach (BlocEventsStatistics blocEventStatistics in blocEventStatisticsToClear)
                blocEventStatistics.Clear();
        }

        #endregion

        #region Private struct

        class Request
        {
            #region Properties

            public virtual DataInfo DataInfo { get; set; }

            public virtual bool IsValid
            {
                get { return DataInfo != null && DataInfo.IsOk; }
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
                get { return base.IsValid && DataInfo.Protocol.Blocs.Contains(Bloc) && DataInfo is IEpochable; }
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
                get { return base.IsValid && DataInfo is IEpochable /*&& AddTestOnChannel */; }
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
