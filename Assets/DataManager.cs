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
    //static void NormalizeData()
    //{
    //    float average = 0;
    //    float standardDeviation = 1;
    //    switch (ApplicationState.GeneralSettings.TrialMatrixSettings.Baseline)
    //    {
    //        case HBP.Data.Settings.TrialMatrixSettings.BaselineType.None:
    //            foreach (var epochedData in m_DataByRequest.Values)
    //            {
    //                foreach (var bloc in epochedData.Blocs)
    //                {
    //                    bloc.Normalize(average, standardDeviation);
    //                }
    //            }
    //            break;
    //        case HBP.Data.Settings.TrialMatrixSettings.BaselineType.Line:
    //            foreach (var epochedData in m_DataByRequest.Values)
    //            {
    //                foreach (var bloc in epochedData.Blocs)
    //                {
    //                    foreach (var pair in bloc.BaseLineValuesBySite)
    //                    {
    //                        average = MathfExtension.Average(pair.Value);
    //                        standardDeviation = MathfExtension.StandardDeviation(pair.Value);
    //                        bloc.Normalize(average, standardDeviation);
    //                    }
    //                }
    //            }
    //            break;
    //        case HBP.Data.Settings.TrialMatrixSettings.BaselineType.Bloc:
    //            foreach (var epochedData in m_DataByRequest.Values)
    //            {
    //                foreach (var bloc in epochedData.Blocs)
    //                {
    //                    List<float> baseLine = new List<float>();
    //                    foreach (var value in bloc.BaseLineValuesBySite.Values)
    //                    {
    //                        baseLine.AddRange(value);
    //                    }
    //                    average = MathfExtension.Average(baseLine.ToArray());
    //                    standardDeviation = MathfExtension.StandardDeviation(baseLine.ToArray());
    //                    bloc.Normalize(average, standardDeviation);
    //                }
    //            }
    //            break;
    //        case HBP.Data.Settings.TrialMatrixSettings.BaselineType.Protocol:
    //            List<float> protocol = new List<float>();
    //            foreach (Bloc b in blocs)
    //            {
    //                foreach (Line l in b.Lines)
    //                {
    //                    protocol.AddRange(l.Bloc.BaseLineValuesBySite[site.Information.FullCorrectedID]);
    //                }
    //            }
    //            average = MathfExtension.Average(protocol.ToArray());
    //            standardDeviation = MathfExtension.StandardDeviation(protocol.ToArray());
    //            foreach (Bloc b in blocs)
    //            {
    //                foreach (Line l in b.Lines)
    //                {
    //                    l.Bloc.Normalize(average, standardDeviation);
    //                }
    //            }
    //            break;
    //    }
    //    foreach (Bloc bloc in blocs.ToArray())
    //    {
    //        foreach (Line line in bloc.Lines)
    //        {
    //            line.UpdateValues();
    //        }
    //    }
    //}
    #endregion

    #region Private struct
    struct DataRequest
    {
        public HBP.Data.Experience.Dataset.DataInfo DataInfo;
        public HBP.Data.Experience.Protocol.Bloc Bloc;

        public DataRequest(HBP.Data.Experience.Dataset.DataInfo dataInfo, HBP.Data.Experience.Protocol.Bloc bloc)
        {
            DataInfo = dataInfo;
            Bloc = bloc;
        }
    }
    #endregion
}