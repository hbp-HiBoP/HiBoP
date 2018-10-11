using System.Collections.Generic;
using HBP.Data.Experience;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.Preferences;
using System.Linq;
using Tools.CSharp;
using System;

public static class DataManager
{
    #region Properties
    static Dictionary<DataRequest, EpochedData> m_DataByRequest = new Dictionary<DataRequest, EpochedData>();
    static Dictionary<DataRequest, HBP.Data.Enums.NormalizationType> m_NormalizeByRequest = new Dictionary<DataRequest, HBP.Data.Enums.NormalizationType>();
    public static bool HasData { get { return m_DataByRequest.Count > 0; } }
    #endregion

    #region Public Methods
    public static EpochedData GetData(DataInfo dataInfo,Bloc bloc)
    {
        DataRequest request = new DataRequest(dataInfo, bloc);
        if (IsRequestValid(request))
        {
            EpochedData result;
            if (m_DataByRequest.TryGetValue(request, out result))
            {
                return result;
            }
            else
            {
                Load(request.DataInfo);
                return m_DataByRequest[request];
            }
        }
        else
        {
            return null;
        }
    }
    public static void Load(DataInfo dataInfo)
    {
        Data data = new Data(dataInfo);
        Protocol protocol = ApplicationState.ProjectLoaded.Datasets.First((d) => d.Data.Contains(dataInfo)).Protocol;
        foreach (var bloc in protocol.Blocs)
        {
            m_DataByRequest.Add(new DataRequest(dataInfo, bloc), new EpochedData(data, bloc));
            m_NormalizeByRequest.Add(new DataRequest(dataInfo, bloc), HBP.Data.Enums.NormalizationType.None);
        }
    }
    public static void UnLoad(DataInfo dataInfo)
    {
        foreach (var dataRequest in m_DataByRequest.Keys.Where((d) => d.DataInfo == dataInfo)) 
        {
            m_DataByRequest.Remove(dataRequest);
            m_NormalizeByRequest.Remove(dataRequest);
        }
    }
    public static void Reload(DataInfo dataInfo)
    {
        UnLoad(dataInfo);
        Load(dataInfo);
    }
    public static void Clear()
    {
        m_DataByRequest.Clear();
        m_NormalizeByRequest.Clear();
    }
    public static void NormalizeData()
    {
        IEnumerable<DataInfo> dataInfoCollection = m_DataByRequest.Select((d) => d.Key.DataInfo).Distinct();
        foreach (var dataInfo in dataInfoCollection)
        {
            IEnumerable<DataRequest> dataRequestCollection = m_DataByRequest.Where((d) => d.Key.DataInfo == dataInfo).Select((d) => d.Key);
            switch (dataInfo.Normalization)
            {
                case DataInfo.NormalizationType.None:
                    foreach(var request in dataRequestCollection) if(m_NormalizeByRequest[request] != HBP.Data.Enums.NormalizationType.None) NormalizeByNone(request);
                    break;
                case DataInfo.NormalizationType.SubTrial:
                    foreach (var request in dataRequestCollection) if (m_NormalizeByRequest[request] != HBP.Data.Enums.NormalizationType.SubTrial) NormalizeBySubTrial(request);
                    break;
                case DataInfo.NormalizationType.Trial:
                    foreach (var request in dataRequestCollection)  if (m_NormalizeByRequest[request] != HBP.Data.Enums.NormalizationType.Trial) NormalizeByTrial(request);
                    break;
                case DataInfo.NormalizationType.SubBloc:
                    foreach (var request in dataRequestCollection) if (m_NormalizeByRequest[request] != HBP.Data.Enums.NormalizationType.SubBloc) NormalizeBySubBloc(request);
                    break;
                case DataInfo.NormalizationType.Bloc:
                    foreach (var request in dataRequestCollection)  if (m_NormalizeByRequest[request] != HBP.Data.Enums.NormalizationType.Bloc) NormalizeByBloc(request);
                    break;
                case DataInfo.NormalizationType.Protocol:
                    IEnumerable<Tuple<DataRequest, bool>> dataRequestAndNeedToNormalize = from dataRequest in dataRequestCollection select new Tuple<DataRequest, bool>(dataRequest, m_NormalizeByRequest[dataRequest] != HBP.Data.Enums.NormalizationType.Protocol);
                    if (dataRequestAndNeedToNormalize.Any((tuple) => tuple.Item2))
                    {
                        NormalizeByProtocol(dataRequestAndNeedToNormalize);
                    }
                    break;
                case DataInfo.NormalizationType.Auto:
                    switch (ApplicationState.UserPreferences.Data.EEG.Normalization)
                    {
                        case HBP.Data.Enums.NormalizationType.None:
                            foreach (var request in dataRequestCollection)  if (m_NormalizeByRequest[request] != HBP.Data.Enums.NormalizationType.None) NormalizeByNone(request);
                            break;
                        case HBP.Data.Enums.NormalizationType.SubTrial:
                            foreach (var request in dataRequestCollection) if (m_NormalizeByRequest[request] != HBP.Data.Enums.NormalizationType.SubTrial) NormalizeBySubTrial(request);
                            break;
                        case HBP.Data.Enums.NormalizationType.Trial:
                            foreach (var request in dataRequestCollection)  if (m_NormalizeByRequest[request] != HBP.Data.Enums.NormalizationType.Trial) NormalizeByTrial(request);
                            break;
                        case HBP.Data.Enums.NormalizationType.SubBloc:
                            foreach (var request in dataRequestCollection) if (m_NormalizeByRequest[request] != HBP.Data.Enums.NormalizationType.SubBloc) NormalizeBySubBloc(request);
                            break;
                        case HBP.Data.Enums.NormalizationType.Bloc:
                            foreach (var request in dataRequestCollection)  if (m_NormalizeByRequest[request] != HBP.Data.Enums.NormalizationType.Bloc) NormalizeByBloc(request);
                            break;
                        case HBP.Data.Enums.NormalizationType.Protocol:
                            IEnumerable<Tuple<DataRequest, bool>> dataRequestAndNeedToNormalize2 = from dataRequest in dataRequestCollection select new Tuple<DataRequest, bool>(dataRequest, m_NormalizeByRequest[dataRequest] != HBP.Data.Enums.NormalizationType.Protocol);
                            if (dataRequestAndNeedToNormalize2.Any((tuple) => tuple.Item2))
                            {
                                NormalizeByProtocol(dataRequestAndNeedToNormalize2);
                            }
                            break;
                    }
                    break;
            }
        }
    }
    #endregion

    #region Private Methods
    static bool IsRequestValid(DataRequest request)
    {
        return ApplicationState.ProjectLoaded.Datasets.First((d) => d.Data.Contains(request.DataInfo)).Protocol.Blocs.Contains(request.Bloc);
    }
    static void NormalizeByNone(DataRequest dataRequest)
    {
        float average = 0;
        float standardDeviation = 1;
        EpochedData epochedData = m_DataByRequest[dataRequest];
        foreach (var trial in epochedData.Trials)
        {
            foreach (var subTrial in trial.SubTrialBySubBloc.Values)
            {
                subTrial.Normalize(average, standardDeviation);
            }
        }
        m_NormalizeByRequest[dataRequest] = HBP.Data.Enums.NormalizationType.None;
    }
    static void NormalizeBySubTrial(DataRequest dataRequest)
    {
        float average = 0;
        float standardDeviation = 1;
        EpochedData epochedData = m_DataByRequest[dataRequest];
        foreach (var trial in epochedData.Trials)
        {
            foreach (var subTrial in trial.SubTrialBySubBloc.Values)
            {
                foreach (var pair in subTrial.BaselineValuesByChannel)
                {
                    average = pair.Value.Mean();
                    standardDeviation = pair.Value.StandardDeviation();
                    subTrial.Normalize(average, standardDeviation, pair.Key);
                }
            }
        }
        m_NormalizeByRequest[dataRequest] = HBP.Data.Enums.NormalizationType.SubTrial;
    }
    static void NormalizeByTrial(DataRequest dataRequest)
    {
        Dictionary<string, List<float>> baselineByChannel;
        EpochedData epochedData = m_DataByRequest[dataRequest];
        foreach (var trial in epochedData.Trials)
        {
            baselineByChannel = new Dictionary<string, List<float>>();
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
        m_NormalizeByRequest[dataRequest] = HBP.Data.Enums.NormalizationType.Trial;
    }
    static void NormalizeBySubBloc(DataRequest dataRequest)
    {
        Dictionary<string, List<float>> baselineByChannel;
        EpochedData epochedData = m_DataByRequest[dataRequest];
        foreach (var subBloc in dataRequest.Bloc.SubBlocs)
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
        m_NormalizeByRequest[dataRequest] = HBP.Data.Enums.NormalizationType.SubBloc;
    }
    static void NormalizeByBloc(DataRequest dataRequest)
    {
        Dictionary<string, List<float>> baselineByChannel = new Dictionary<string, List<float>>();
        EpochedData epochedData = m_DataByRequest[dataRequest];
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
        m_NormalizeByRequest[dataRequest] = HBP.Data.Enums.NormalizationType.Bloc;
    }
    static void NormalizeByProtocol(IEnumerable<Tuple<DataRequest, bool>> dataRequestAndNeedToNormalize)
    {
        Dictionary<string, List<float>> baselineByChannel = new Dictionary<string, List<float>>();

        foreach (var tuple in dataRequestAndNeedToNormalize)
        {
            EpochedData epochedData = m_DataByRequest[tuple.Item1];
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
                    EpochedData epochedData = m_DataByRequest[tuple.Item1];
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
            if (tuple.Item2) m_NormalizeByRequest[tuple.Item1] = HBP.Data.Enums.NormalizationType.Protocol;
        }
    }
    #endregion

    #region Private struct
    struct DataRequest
    {
        public DataInfo DataInfo;
        public Bloc Bloc;

        public DataRequest(DataInfo dataInfo, Bloc bloc)
        {
            DataInfo = dataInfo;
            Bloc = bloc;
        }
    }
    #endregion
}