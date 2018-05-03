using System.Collections.Generic;
using HBP.Data.Experience;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.Preferences;
using System.Linq;
using Tools.CSharp;

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
            m_DataByRequest.Add(new DataRequest(dataInfo, bloc), new EpochedData(bloc, data));
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
                case DataInfo.NormalizationType.Trial:
                    foreach (var request in dataRequestCollection)  if (m_NormalizeByRequest[request] != HBP.Data.Enums.NormalizationType.Trial) NormalizeByTrial(request);
                    break;
                case DataInfo.NormalizationType.Bloc:
                    foreach (var request in dataRequestCollection)  if (m_NormalizeByRequest[request] != HBP.Data.Enums.NormalizationType.Bloc) NormalizeByBloc(request);
                    break;
                case DataInfo.NormalizationType.Protocol:
                    IEnumerable<Tuple<DataRequest, bool>> dataRequestAndNeedToNormalize = from dataRequest in dataRequestCollection select new Tuple<DataRequest, bool>(dataRequest, m_NormalizeByRequest[dataRequest] != HBP.Data.Enums.NormalizationType.Protocol);
                    if (dataRequestAndNeedToNormalize.Any((tuple) => tuple.Object2))
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
                        case HBP.Data.Enums.NormalizationType.Trial:
                            foreach (var request in dataRequestCollection)  if (m_NormalizeByRequest[request] != HBP.Data.Enums.NormalizationType.Trial) NormalizeByTrial(request);
                            break;
                        case HBP.Data.Enums.NormalizationType.Bloc:
                            foreach (var request in dataRequestCollection)  if (m_NormalizeByRequest[request] != HBP.Data.Enums.NormalizationType.Bloc) NormalizeByBloc(request);
                            break;
                        case HBP.Data.Enums.NormalizationType.Protocol:
                            IEnumerable<Tuple<DataRequest, bool>> dataRequestAndNeedToNormalize2 = from dataRequest in dataRequestCollection select new Tuple<DataRequest, bool>(dataRequest, m_NormalizeByRequest[dataRequest] != HBP.Data.Enums.NormalizationType.Protocol);
                            if (dataRequestAndNeedToNormalize2.Any((tuple) => tuple.Object2))
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
        foreach (var bloc in m_DataByRequest[dataRequest].Blocs)
        {
            bloc.Normalize(average, standardDeviation);
        }
        m_NormalizeByRequest[dataRequest] = HBP.Data.Enums.NormalizationType.None;
    }
    static void NormalizeByTrial(DataRequest dataRequest)
    {
        float average = 0;
        float standardDeviation = 1;
        foreach (var bloc in m_DataByRequest[dataRequest].Blocs)
        {
            foreach (var pair in bloc.BaselineValuesBySite)
            {
                average = pair.Value.Mean();
                standardDeviation = pair.Value.StandardDeviation();
                bloc.Normalize(average, standardDeviation, pair.Key);
            }
        }
        m_NormalizeByRequest[dataRequest] = HBP.Data.Enums.NormalizationType.Trial;
    }
    static void NormalizeByBloc(DataRequest dataRequest)
    {
        Dictionary<string, List<float>> BaselineBySite = new Dictionary<string, List<float>>();
        Dictionary<string, float> averageBySite = new Dictionary<string, float>();
        Dictionary<string, float> standardDeviationBySite = new Dictionary<string, float>();
        foreach (var line in m_DataByRequest[dataRequest].Blocs)
        {
            foreach (var site in line.BaselineValuesBySite.Keys)
            {
                if (!BaselineBySite.ContainsKey(site)) BaselineBySite[site] = new List<float>();
                BaselineBySite[site].AddRange(line.BaselineValuesBySite[site]);
            }
        }
        foreach (var site in BaselineBySite.Keys)
        {
            averageBySite[site] = BaselineBySite[site].ToArray().Mean();
            standardDeviationBySite[site] = BaselineBySite[site].ToArray().StandardDeviation();
        }
        foreach (var line in m_DataByRequest[dataRequest].Blocs)
        {
            foreach (var site in line.ValuesBySite.Keys)
            {
                line.Normalize(averageBySite[site], standardDeviationBySite[site], site);
            }
        }
        m_NormalizeByRequest[dataRequest] = HBP.Data.Enums.NormalizationType.Bloc;
    }
    static void NormalizeByProtocol(IEnumerable<Tuple<DataRequest, bool>> dataRequestAndNeedToNormalize)
    {
        Dictionary<string, List<float>> baselineBySite = new Dictionary<string, List<float>>();
        Dictionary<string, float> averageBySite = new Dictionary<string, float>();
        Dictionary<string, float> standardDeviationBySite = new Dictionary<string, float>();

        foreach (var tuple in dataRequestAndNeedToNormalize)
        {
            EpochedData epochedData = m_DataByRequest[tuple.Object1];
            foreach (var line in epochedData.Blocs)
            {
                foreach (var site in line.BaselineValuesBySite.Keys)
                {
                    if (!baselineBySite.ContainsKey(site)) baselineBySite[site] = new List<float>();
                    baselineBySite[site].AddRange(line.BaselineValuesBySite[site]);
                }
            }
        }
        foreach (var site in baselineBySite.Keys)
        {
            averageBySite[site] = baselineBySite[site].ToArray().Mean();
            standardDeviationBySite[site] = baselineBySite[site].ToArray().StandardDeviation();
        }
        foreach (var tuple in dataRequestAndNeedToNormalize)
        {
            if (tuple.Object2)
            {
                EpochedData epochedData = m_DataByRequest[tuple.Object1];
                foreach (var line in epochedData.Blocs)
                {
                    foreach (var site in line.BaselineValuesBySite.Keys)
                    {
                        line.Normalize(averageBySite[site], standardDeviationBySite[site], site);
                    }
                }
                m_NormalizeByRequest[tuple.Object1] = HBP.Data.Enums.NormalizationType.Protocol;
            }
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