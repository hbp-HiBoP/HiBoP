using System.Collections.Generic;
using UnityEngine;
using data = HBP.Data.Informations;
using System.Linq;
using Tools.Unity.Graph;
using HBP.Data.Experience.Protocol;
using System;
using HBP.Data.Experience.Dataset;
using HBP.Data.Informations;

namespace HBP.UI.Informations
{
    public class ROIInformations : MonoBehaviour
    {
        #region Properties
        [SerializeField] SceneROIStruct m_ROIStruct;

        public Dictionary<Tuple<DataStruct, Bloc>, Color> ColorsByData { get; private set; } = new Dictionary<Tuple<DataStruct, Bloc>, Color>();
        [SerializeField] GameObject m_GraphPrefab;
        #endregion

        #region Public Methods
        public void Display(SceneROIStruct ROIStruct)
        {
            m_ROIStruct = ROIStruct;
        }
        #endregion

        //#region Private Methods
        //void DisplayGraphs(data.SceneROIStruct ROI)
        //{
        //    Tuple<SubBloc[], Tools.CSharp.Window>[] subBlocsAndWindowByColumn = Bloc.GetSubBlocsAndWindowByColumn(ROI.ChannelsByData.Keys.SelectMany(c => c.Blocs).Select(b => b.Bloc).Distinct());

        //    IEnumerable<data.ChannelStruct> channelStructs = ROI.ChannelsByData.Values.SelectMany(t => t).Distinct();
        //    foreach (var channelStruct in channelStructs)
        //    {
        //        DisplayChannelGraph(channelStruct, ROI, subBlocsAndWindowByColumn);
        //    }
        //}
        //void DisplayChannelGraph(data.ChannelStruct channelStruct, SceneROIStruct ROI, Tuple<SubBloc[], Tools.CSharp.Window>[] subBlocsAndWindowByColumn)
        //{
        //    foreach (var pair in ROI.ChannelsByData)
        //    {
        //        string graphName = channelStruct.Channel;
        //        var curves = GenerateDataCurve(channelStruct, ROI, subBlocsAndWindowByColumn);
        //    }
        //}
        //Tuple<Graph.Curve[], Tools.CSharp.Window, bool>[] GenerateDataCurve(ChannelStruct channelStruct, data.SceneROIStruct ROI, Tuple<SubBloc[], Tools.CSharp.Window>[] subBlocsAndWindowByColumn)
        //{
        //    List<Tuple<Graph.Curve[], Tools.CSharp.Window, bool>> result = new List<Tuple<Graph.Curve[], Tools.CSharp.Window, bool>>();
        //    foreach (var subBlocsAndWindow in subBlocsAndWindowByColumn)
        //    {
        //        List<Graph.Curve> curves = new List<Graph.Curve>();
        //        IEnumerable<data.DataStruct> data = ROI.ChannelsByData.Where(p => p.Value.Contains(channelStruct)).Select(p => p.Key);
        //        foreach (var d in data)
        //        {
        //            Graph.Curve curve = GenerateDataCurve(channelStruct, d, subBlocsAndWindow.Item1);
        //            if (curve != null) curves.Add(curve);
        //            result.Add(new Tuple<Graph.Curve[], Tools.CSharp.Window, bool>(curves.ToArray(), subBlocsAndWindow.Item2, subBlocsAndWindow.Item1[0].Type == Data.Enums.MainSecondaryEnum.Main));
        //        }
        //    }
        //    return result.ToArray();
        //}
        //Graph.Curve GenerateDataCurve(ChannelStruct channel, DataStruct data, SubBloc[] subBlocs)
        //{
        //    string id = "";
        //    if (data is data.IEEGDataStruct ieegDataStruct) id = ieegDataStruct.Data + "_" + "IEEG";
        //    else if (data is data.CCEPDataStruct ccepDataStruct) id = ccepDataStruct.Data + "_CCEP_" + ccepDataStruct.Source.Patient.CompleteName + "_" + ccepDataStruct.Source.Channel;
        //    Graph.Curve result = new Graph.Curve(data.Data, null, true, id, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));
        //    foreach (var bloc in data.Blocs)
        //    {
        //        SubBloc subBloc = bloc.Bloc.SubBlocs.FirstOrDefault(s => subBlocs.Contains(s));
        //        if (subBloc != null)
        //        {
        //            result.AddSubCurve(GenerateBlocCurve(channelStruct, data, bloc.Bloc, subBloc, id));
        //        }
        //    }
        //    if (result.SubCurves.Count == 0) return null;
        //    return result;
        //}
        //Graph.Curve GenerateBlocCurve(ChannelStruct channel, DataStruct data, Bloc bloc, SubBloc subBloc, string id)
        //{
        //    id = id + "_" + bloc.Name;
        //    Graph.Curve result = new Graph.Curve(bloc.Name, null, true, id, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));

        //    // Channel
        //    GenerateChannelCurve(channel, data, bloc, subBloc, id);
             
        //    // ROIs
        //    if (data.Blocs.First(b => b.Bloc == bloc).ROIs.Count > 0)
        //    {
        //        result.AddSubCurve(GenerateROIsCurve(data, bloc, subBloc, id));
        //    }

        //    return result;
        //}
        //Graph.Curve GenerateROIsCurve(DataStruct data, Bloc bloc, SubBloc subBloc, string id)
        //{
        //    id = id + "_ROI";
        //    Graph.Curve result = new Graph.Curve("ROI", null, true, id, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));
        //    BlocStruct blocStruct = data.Blocs.First(b => b.Bloc == bloc);
        //    foreach (var ROI in blocStruct.ROIs)
        //    {
        //        result.AddSubCurve(GenerateROICurve(data, ROI, bloc, subBloc, id));
        //    }
        //    return result;
        //}
        //Graph.Curve GenerateROICurve(DataStruct data, ROIStruct ROI, Data.Experience.Protocol.Bloc bloc, SubBloc subBloc, string id)
        //{
        //    id = id + "_" + ROI.Name;

        //    CurveData curveData = null;
        //    Dictionary<Patient, List<string>> ChannelsByPatient = new Dictionary<Patient, List<string>>();
        //    foreach (var channel in ROI.Channels)
        //    {
        //        ChannelsByPatient.AddIfAbsent(channel.Patient, new List<string>());
        //        ChannelsByPatient[channel.Patient].Add(channel.Channel);
        //    }
        //    Dictionary<Patient, PatientDataInfo> dataInfoByPatient = new Dictionary<Patient, PatientDataInfo>(ChannelsByPatient.Count);
        //    if (data is IEEGDataStruct ieegDataStruct)
        //    {
        //        iEEGDataInfo[] ieegDataInfo = ieegDataStruct.Dataset.GetIEEGDataInfos();
        //        foreach (var patient in ChannelsByPatient.Keys)
        //        {
        //            dataInfoByPatient.Add(patient, ieegDataInfo.First(d => d.Patient == patient && d.Name == ieegDataStruct.Data));
        //        }
        //    }
        //    else if (data is CCEPDataStruct ccepDataStruct)
        //    {
        //        CCEPDataInfo[] ccepDataInfo = ccepDataStruct.Dataset.GetCCEPDataInfos();
        //        foreach (var patient in ChannelsByPatient.Keys)
        //        {
        //            dataInfoByPatient.Add(patient, ccepDataInfo.First(d => d.Patient == patient && d.Patient == ccepDataStruct.Source.Patient && d.StimulatedChannel == ccepDataStruct.Source.Channel && d.Name == ccepDataStruct.Data));
        //        }
        //    }

        //    Color color = m_ColorsByROI[new Tuple<ROIStruct, DataStruct, Data.Experience.Protocol.Bloc>(ROI, data, bloc)];
        //    if (ROI.Channels.Count > 1)
        //    {
        //        int channelCount = ROI.Channels.Count;
        //        // Get the statistics for all channels in the ROI
        //        BlocChannelStatistics[] statistics = new BlocChannelStatistics[channelCount];
        //        for (int c = 0; c < channelCount; ++c)
        //        {
        //            statistics[c] = DataManager.GetStatistics(dataInfoByPatient[ROI.Channels[c].Patient], bloc, ROI.Channels[c].Channel);
        //        }
        //        // Create all the required variables
        //        int length = statistics[0].Trial.ChannelSubTrialBySubBloc[subBloc].Values.Length;
        //        float[] values = new float[length];
        //        float[] standardDeviations = new float[length];
        //        float[][] sum = new float[length][];
        //        for (int i = 0; i < length; ++i)
        //        {
        //            sum[i] = new float[channelCount];
        //        }
        //        // Fill the values array
        //        for (int c = 0; c < channelCount; ++c)
        //        {
        //            float[] val = statistics[c].Trial.ChannelSubTrialBySubBloc[subBloc].Values;
        //            for (int i = 0; i < length; ++i)
        //            {
        //                sum[i][c] = val[i];
        //            }
        //        }
        //        // Compute mean and SEM of the values
        //        for (int i = 0; i < length; ++i)
        //        {
        //            values[i] = sum[i].Mean();
        //            standardDeviations[i] = sum[i].SEM();
        //        }

        //        // Generate points.
        //        int start = subBloc.Window.Start;
        //        int end = subBloc.Window.End;
        //        Vector2[] points = new Vector2[values.Length];
        //        for (int i = 0; i < points.Length; i++)
        //        {
        //            float abscissa = start + ((float)i / (points.Length - 1)) * (end - start);
        //            float ordinate = values[i];
        //            points[i] = new Vector2(abscissa, ordinate);
        //        }
        //        curveData = ShapedCurveData.CreateInstance(points, standardDeviations, color);
        //    }
        //    else if (ROI.Channels.Count == 1)
        //    {
        //        ChannelStruct channel = ROI.Channels[0];
        //        ChannelSubTrialStat stat = DataManager.GetStatistics(dataInfoByPatient[channel.Patient], bloc, channel.Channel).Trial.ChannelSubTrialBySubBloc[subBloc];
        //        float[] values = stat.Values;

        //        // Generate points.
        //        int start = subBloc.Window.Start;
        //        int end = subBloc.Window.End;
        //        Vector2[] points = new Vector2[values.Length];
        //        for (int i = 0; i < points.Length; i++)
        //        {
        //            float abscissa = start + ((float)i / (points.Length - 1)) * (end - start);
        //            float ordinate = values[i];
        //            points[i] = new Vector2(abscissa, ordinate);
        //        }
        //        curveData = CurveData.CreateInstance(points, color);
        //    }

        //    Graph.Curve result = new Graph.Curve(ROI.Name, curveData, true, id, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));
        //    return result;
        //}
        //Graph.Curve GenerateChannelCurve(ChannelStruct channel, DataStruct data, Bloc bloc, SubBloc subBloc, string id)
        //{
        //    id = id + "_" + channel.Channel;
        //    CurveData curveData = GetCurveData(channel, data, bloc, subBloc);
        //    return new Graph.Curve(channel.Channel, curveData, true, id, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));
        //}
        //CurveData GetCurveData(ChannelStruct channel, DataStruct data, Bloc bloc, SubBloc subBloc)
        //{
        //    CurveData result = null;
        //    PatientDataInfo dataInfo = null;
        //    if (data is IEEGDataStruct ieegDataStruct)
        //    {
        //        dataInfo = ieegDataStruct.Dataset.GetIEEGDataInfos().First(d => (d.Patient == channel.Patient && d.Name == ieegDataStruct.Data));
        //    }
        //    else if (data is CCEPDataStruct ccepDataStruct)
        //    {
        //        dataInfo = ccepDataStruct.Dataset.GetCCEPDataInfos().First(d => (d.Patient == channel.Patient && d.StimulatedChannel == ccepDataStruct.Source.Channel && d.Patient == ccepDataStruct.Source.Patient && d.Name == ccepDataStruct.Data));
        //    }
        //    var blocChannelStatistics = DataManager.GetStatistics(dataInfo, bloc, channel.Channel);
        //    Color color = ColorsByData[new Tuple<DataStruct, Bloc>(data, bloc)];
        //    var channelTrialStat = blocChannelStatistics.Trial.ChannelSubTrialBySubBloc[subBloc];

        //    // Generate points.
        //    int start = subBloc.Window.Start;
        //    int end = subBloc.Window.End;
        //    Vector2[] points = new Vector2[channelTrialStat.Values.Length];
        //    for (int i = 0; i < points.Length; i++)
        //    {
        //        float abscissa = start + ((float)i / (points.Length - 1)) * (end - start);
        //        float ordinate = channelTrialStat.Values[i];
        //        points[i] = new Vector2(abscissa, ordinate);
        //    }
        //    result = ShapedCurveData.CreateInstance(points, channelTrialStat.SEM, color);
        //    return result;
        //}
        //#endregion
    }
}