using System.Collections.Generic;
using HBP.Data.Experience;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using System.Linq;
using Tools.CSharp;

public static class DataManager
{
    #region Properties
    static Dictionary<DataRequest, EpochedData> m_DataByRequest = new Dictionary<DataRequest, EpochedData>();
	#endregion

	#region Public Methods
    public static EpochedData GetData(DataInfo dataInfo,Bloc bloc)
    {
        DataRequest request = new DataRequest(dataInfo, bloc);
        if (!m_DataByRequest.ContainsKey(request)) Load(request.DataInfo);
        return m_DataByRequest[request];
    }
    public static void NormalizeData()
    {
        float average = 0;
        float standardDeviation = 1;
        switch (ApplicationState.GeneralSettings.TrialMatrixSettings.Baseline)
        {
            case HBP.Data.Settings.TrialMatrixSettings.BaselineType.None:
                foreach (var epochedData in m_DataByRequest.Values)
                {
                    foreach (var bloc in epochedData.Blocs)
                    {
                        bloc.Normalize(average, standardDeviation);
                    }
                }
                break;
            case HBP.Data.Settings.TrialMatrixSettings.BaselineType.Line:
                foreach (var epochedData in m_DataByRequest.Values)
                {
                    foreach (var bloc in epochedData.Blocs)
                    {
                        foreach (var pair in bloc.BaseLineValuesBySite)
                        {
                            average = MathfExtension.Average(pair.Value);
                            standardDeviation = MathfExtension.StandardDeviation(pair.Value);
                            bloc.Normalize(average, standardDeviation, pair.Key);
                        }
                    }
                }
                break;
            case HBP.Data.Settings.TrialMatrixSettings.BaselineType.Bloc:
                foreach (var epochedData in m_DataByRequest.Values)
                {
                    Dictionary<string, List<float>> baselineBySite = new Dictionary<string, List<float>>();
                    Dictionary<string, float> averageBySite = new Dictionary<string, float>();
                    Dictionary<string, float> standardDeviationBySite = new Dictionary<string, float>();
                    foreach (var line in epochedData.Blocs)
                    {
                        foreach (var site in line.BaseLineValuesBySite.Keys)
                        {
                            if (!baselineBySite.ContainsKey(site)) baselineBySite[site] = new List<float>();
                            baselineBySite[site].AddRange(line.BaseLineValuesBySite[site]);
                        }
                    }
                    foreach (var site in baselineBySite.Keys)
                    {
                        averageBySite[site] = MathfExtension.Average(baselineBySite[site].ToArray());
                        standardDeviationBySite[site] = MathfExtension.StandardDeviation(baselineBySite[site].ToArray());
                    }
                    foreach (var line in epochedData.Blocs)
                    {
                        foreach (var site in line.ValuesBySite.Keys)
                        {
                            line.Normalize(averageBySite[site], standardDeviationBySite[site], site);
                        }
                    }
                }
                break;
            case HBP.Data.Settings.TrialMatrixSettings.BaselineType.Protocol:
                DataInfo[] dataInfo = (from request in m_DataByRequest.Keys select request.DataInfo).Distinct().ToArray();
                foreach (var data in dataInfo)
                {
                    Protocol protocol = ApplicationState.ProjectLoaded.Datasets.First((d) => d.Data.Contains(data)).Protocol;
                    Dictionary<string, List<float>> baselineBySite = new Dictionary<string, List<float>>();
                    Dictionary<string, float> averageBySite = new Dictionary<string, float>();
                    Dictionary<string, float> standardDeviationBySite = new Dictionary<string, float>();

                    foreach (var bloc in protocol.Blocs)
                    {
                        EpochedData epochedData = m_DataByRequest[new DataRequest(data, bloc)];
                        foreach (var line in epochedData.Blocs)
                        {
                            foreach (var site in line.BaseLineValuesBySite.Keys)
                            {
                                if (!baselineBySite.ContainsKey(site)) baselineBySite[site] = new List<float>();
                                baselineBySite[site].AddRange(line.BaseLineValuesBySite[site]);
                            }
                        }
                    }
                    foreach (var site in baselineBySite.Keys)
                    {
                        averageBySite[site] = MathfExtension.Average(baselineBySite[site].ToArray());
                        standardDeviationBySite[site] = MathfExtension.StandardDeviation(baselineBySite[site].ToArray());
                    }
                    foreach (var bloc in protocol.Blocs)
                    {
                        EpochedData epochedData = m_DataByRequest[new DataRequest(data, bloc)];
                        foreach (var line in epochedData.Blocs)
                        {
                            foreach (var site in line.BaseLineValuesBySite.Keys)
                            {
                                line.Normalize(averageBySite[site], standardDeviationBySite[site], site);
                            }
                        }
                    }
                }
                break;
        }
    }
    #endregion

    #region Private Methods
    static void Load(DataInfo dataInfo)
    {
        Data data = new Data(dataInfo);
        Protocol protocol = ApplicationState.ProjectLoaded.Datasets.First((d) => d.Data.Contains(dataInfo)).Protocol;
        foreach (var bloc in protocol.Blocs)
        {
            m_DataByRequest.Add(new DataRequest(dataInfo,bloc),new EpochedData(bloc, data));
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