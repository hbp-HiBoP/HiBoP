using System.Collections.Generic;
using UnityEngine;
using data = HBP.Data.Informations;
using System.Linq;
using Tools.Unity.Graph;
using HBP.Data.Experience.Protocol;
using System;
using HBP.Data.Experience.Dataset;
using HBP.Data.Informations;
using HBP.Data;
using Tools.CSharp;

namespace HBP.UI.Informations
{
    public class ROIInformations : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject m_GraphPrefab;
        [SerializeField] Transform m_GraphsZone;

        [SerializeField] List<Color> m_Colors;

        SceneROIStruct m_ROIStruct;
        Dictionary<Tuple<data.Data, Bloc>, Color> ColorsByData { get; set; } = new Dictionary<Tuple<data.Data, Bloc>, Color>();
        Dictionary<Tuple<SceneROIStruct, data.Data, Bloc>, Color> m_ColorsByROI = new Dictionary<Tuple<SceneROIStruct, data.Data, Bloc>, Color>();
        #endregion

        #region Public Methods
        public void Display(SceneROIStruct ROIStruct)
        {
            m_ROIStruct = ROIStruct;
        }
        #endregion

        #region Private Methods
        void GenerateGraphs(SceneROIStruct ROI)
        {
            //// Get window by SubBlocs.
            //Tuple<Tuple<Bloc, SubBloc>[], Tools.CSharp.Window>[] subBlocsAndWindowByColumn = Bloc.GetSubBlocsAndWindowByColumn(ROI.ChannelsByData.Keys.SelectMany(c => c.Blocs).Select(b => b.Bloc).Distinct());

            //// Get ROI curveData by Data.
            //Dictionary<data.Data, Dictionary<Bloc, Dictionary<SubBloc, Graph.Curve>>> ROICurves = GenerateROICurves(ROI);

            //// Channels.
            //IEnumerable<ChannelStruct> channelStructs = ROI.ChannelsByData.Values.SelectMany(t => t).Distinct();
            //Dictionary<ChannelStruct, Graph.Curve[][]> graphInformationByChannel = new Dictionary<ChannelStruct, Graph.Curve[][]>();
            //foreach (var channelStruct in channelStructs)
            //{
            //    graphInformationByChannel.Add(channelStruct, GenerateDataCurve(channelStruct, ROI, ROICurves, subBlocsAndWindowByColumn));
            //}

            //// Limits.
            //List<float> values = new List<float>();
            //foreach (var channelStruct in channelStructs)
            //{
            //    var graphInformation = graphInformationByChannel[channelStruct];
            //    foreach (var curves in graphInformation)
            //    {
            //        foreach (var curve in curves)
            //        {
            //            values.AddRange(GetValues(curve));
            //        }
            //    }
            //}
            //Vector2 defaultOrdinateDisplayRange = values.ToArray().CalculateValueLimit(5);


            //foreach (var item in graphInformationByChannel)
            //{

            //}
        }

        //Graph.Curve[][] GenerateDataCurve(ChannelStruct channelStruct, SceneROIStruct ROI, Dictionary<data.Data, Dictionary<Bloc, Dictionary<SubBloc, Graph.Curve>>> ROICurvesByData, Tuple<Tuple<Bloc, SubBloc>[], Tools.CSharp.Window>[] subBlocsAndWindowByColumn)
        //{
        //    Graph.Curve[][] result = new Graph.Curve[subBlocsAndWindowByColumn.Length][];
        //    for (int c = 0; c < subBlocsAndWindowByColumn.Length; c++)
        //    {
        //        var column = subBlocsAndWindowByColumn[c];
        //        IEnumerable<data.Data> data = ROI.ChannelsByData.Where((KeyValuePair<data.Data, List<ChannelStruct>> p) => p.Value.Contains(channelStruct)).Select((KeyValuePair<data.Data, List<ChannelStruct>> p) => p.Key);
        //        List<Graph.Curve> curves = new List<Graph.Curve>();
        //        foreach (var d in data)
        //        {
        //            List<Graph.Curve> dataCurves = new List<Graph.Curve>();
        //            foreach (var blocStruct in d.Blocs)
        //            {
        //                Bloc bloc = blocStruct.Bloc;
        //                SubBloc subBloc = column.Item1.First(t => t.Item1 == bloc).Item2;

        //                List<Graph.Curve> blocCurves = new List<Graph.Curve>();

        //                // ROI Curve.
        //                blocCurves.Add(ROICurvesByData[d][bloc][subBloc]);

        //                // Channel Curve
        //                Graph.Curve channelCurve = GenerateBlocCurve(channelStruct, d, bloc, subBloc);
        //                if (channelCurve != null) blocCurves.Add(channelCurve);

        //                dataCurves.Add(new Graph.Curve(bloc.Name, null, true, d.Name + "_" + bloc.Name, blocCurves.ToArray(), new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1)));
        //            }
        //            curves.Add(new Graph.Curve(d.Name, null, true, d.Name, dataCurves.ToArray(), new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1)));
        //        }
        //        result[c] = curves.ToArray();
        //    }
        //    return result.ToArray();
        //}
        //Dictionary<data.Data, Dictionary<Bloc, Dictionary<SubBloc, Graph.Curve>>> GenerateROICurves(SceneROIStruct ROI)
        //{
        //    Dictionary<data.Data, Dictionary<Bloc, Dictionary<SubBloc, Graph.Curve>>> ROICurvesByData = new Dictionary<data.Data, Dictionary<Bloc, Dictionary<SubBloc, Graph.Curve>>>();
        //    foreach (var data in ROI.ChannelsByData.Keys)
        //    {
        //        IEnumerable<ChannelStruct> channels = ROI.ChannelsByData[data];
        //        Dictionary<Bloc, Dictionary<SubBloc, Graph.Curve>> ROICurvesByBloc = new Dictionary<Bloc, Dictionary<SubBloc, Graph.Curve>>();
        //        foreach (var blocStruct in data.Blocs)
        //        {
        //            Dictionary<SubBloc, Graph.Curve> ROICurveBySubBloc = new Dictionary<SubBloc, Graph.Curve>();
        //            foreach (var subBloc in blocStruct.Bloc.SubBlocs)
        //            {
        //                CurveData curveData = null;
        //                Dictionary<Patient, List<string>> ChannelsByPatient = new Dictionary<Patient, List<string>>();
        //                foreach (var channel in channels)
        //                {
        //                    ChannelsByPatient.AddIfAbsent(channel.Patient, new List<string>());
        //                    ChannelsByPatient[channel.Patient].Add(channel.Channel);
        //                }

        //                Dictionary<Patient, PatientDataInfo> dataInfoByPatient = new Dictionary<Patient, PatientDataInfo>(ChannelsByPatient.Count);
        //                if (data is data.IEEGData ieegDataStruct)
        //                {
        //                    iEEGDataInfo[] ieegDataInfo = ieegDataStruct.Dataset.GetIEEGDataInfos();
        //                    foreach (var patient in ChannelsByPatient.Keys)
        //                    {
        //                        dataInfoByPatient.Add(patient, ieegDataInfo.First(d => d.Patient == patient && d.Name == ieegDataStruct.Name));
        //                    }
        //                }
        //                else if (data is data.CCEPData ccepDataStruct)
        //                {
        //                    CCEPDataInfo[] ccepDataInfo = ccepDataStruct.Dataset.GetCCEPDataInfos();
        //                    foreach (var patient in ChannelsByPatient.Keys)
        //                    {
        //                        dataInfoByPatient.Add(patient, ccepDataInfo.First(d => d.Patient == patient && d.Patient == ccepDataStruct.Source.Patient && d.StimulatedChannel == ccepDataStruct.Source.Channel && d.Name == ccepDataStruct.Name));
        //                    }
        //                }

        //                Color color = m_ColorsByROI[new Tuple<SceneROIStruct, data.Data, Bloc>(ROI, data, blocStruct.Bloc)];

        //                if (channels.Count() > 1)
        //                {
        //                    int channelCount = ROI.ChannelsByData[data].Count;
        //                    // Get the statistics for all channels in the ROI
        //                    List<BlocChannelStatistics> statistics = new List<BlocChannelStatistics>();
        //                    foreach (var channel in channels)
        //                    {
        //                        statistics.Add(DataManager.GetStatistics(dataInfoByPatient[channel.Patient], blocStruct.Bloc, channel.Channel));
        //                    }

        //                    // Create all the required variables
        //                    int length = statistics[0].Trial.ChannelSubTrialBySubBloc[subBloc].Values.Length;
        //                    float[] values = new float[length];
        //                    float[] standardDeviations = new float[length];
        //                    float[][] sum = new float[length][];
        //                    for (int i = 0; i < length; ++i)
        //                    {
        //                        sum[i] = new float[channelCount];
        //                    }
        //                    // Fill the values array
        //                    for (int c = 0; c < channelCount; ++c)
        //                    {
        //                        float[] val = statistics[c].Trial.ChannelSubTrialBySubBloc[subBloc].Values;
        //                        for (int i = 0; i < length; ++i)
        //                        {
        //                            sum[i][c] = val[i];
        //                        }
        //                    }
        //                    // Compute mean and SEM of the values
        //                    for (int i = 0; i < length; ++i)
        //                    {
        //                        values[i] = sum[i].Mean();
        //                        standardDeviations[i] = sum[i].SEM();
        //                    }

        //                    // Generate points.
        //                    int start = subBloc.Window.Start;
        //                    int end = subBloc.Window.End;
        //                    Vector2[] points = new Vector2[values.Length];
        //                    for (int i = 0; i < points.Length; i++)
        //                    {
        //                        float abscissa = start + ((float)i / (points.Length - 1)) * (end - start);
        //                        float ordinate = values[i];
        //                        points[i] = new Vector2(abscissa, ordinate);
        //                    }
        //                    curveData = ShapedCurveData.CreateInstance(points, standardDeviations, color);
        //                }
        //                else if (channels.Count() == 1)
        //                {
        //                    ChannelStruct channel = channels.First();
        //                    ChannelSubTrialStat stat = DataManager.GetStatistics(dataInfoByPatient[channel.Patient], blocStruct.Bloc, channel.Channel).Trial.ChannelSubTrialBySubBloc[subBloc];
        //                    float[] values = stat.Values;

        //                    // Generate points.
        //                    int start = subBloc.Window.Start;
        //                    int end = subBloc.Window.End;
        //                    Vector2[] points = new Vector2[values.Length];
        //                    for (int i = 0; i < points.Length; i++)
        //                    {
        //                        float abscissa = start + ((float)i / (points.Length - 1)) * (end - start);
        //                        float ordinate = values[i];
        //                        points[i] = new Vector2(abscissa, ordinate);
        //                    }
        //                    curveData = CurveData.CreateInstance(points, color);
        //                }

        //                Graph.Curve curve = new Graph.Curve(ROI.Name, curveData, true, ROI.Name + "_" + data.Name + "_" + blocStruct.Bloc.Name + "_" + subBloc.Name, new Graph.Curve[0], Color.red);
        //                ROICurveBySubBloc.Add(subBloc, curve);
        //            }
        //            ROICurvesByBloc.Add(blocStruct.Bloc, ROICurveBySubBloc);
        //        }
        //        ROICurvesByData.Add(data, ROICurvesByBloc);
        //    }
        //    return ROICurvesByData;
        //}
        //Graph.Curve GenerateBlocCurve(ChannelStruct channel, data.Data data, Bloc bloc, SubBloc subBloc)
        //{
        //    CurveData curveData = GetCurveData(channel, data, bloc, subBloc);
        //    return new Graph.Curve(bloc.Name, curveData, true, data.Name + "_" + bloc.Name, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));
        //}
        //CurveData GetCurveData(ChannelStruct channelStruct, data.Data dataStruct, Bloc bloc, SubBloc subBloc)
        //{
        //    PatientDataInfo dataInfo = null;
        //    if (dataStruct is data.IEEGData ieegDataStruct) dataInfo = ieegDataStruct.Dataset.GetIEEGDataInfos().First(d => (d.Patient == channelStruct.Patient && d.Name == ieegDataStruct.Name));
        //    else if (dataStruct is data.CCEPData ccepDataStruct) dataInfo = ccepDataStruct.Dataset.GetCCEPDataInfos().First(d => (d.Patient == channelStruct.Patient && d.StimulatedChannel == ccepDataStruct.Source.Channel && d.Patient == ccepDataStruct.Source.Patient && d.Name == ccepDataStruct.Name));

        //    BlocChannelStatistics blocChannelStatistic = DataManager.GetStatistics(dataInfo, bloc, channelStruct.Channel);
        //    Color color = ColorsByData[new Tuple<data.Data, Bloc>(dataStruct, bloc)];

        //    ChannelSubTrialStat channelSubTrials = blocChannelStatistic.Trial.ChannelSubTrialBySubBloc[subBloc];

        //    // Generate points.
        //    int start = subBloc.Window.Start;
        //    int end = subBloc.Window.End;
        //    Vector2[] points = new Vector2[channelSubTrials.Values.Length];
        //    for (int i = 0; i < points.Length; i++)
        //    {
        //        float abscissa = start + ((float)i / (points.Length - 1)) * (end - start);
        //        float ordinate = channelSubTrials.Values[i];
        //        points[i] = new Vector2(abscissa, ordinate);
        //    }
        //    return ShapedCurveData.CreateInstance(points, channelSubTrials.SEM, color);
        //}
        //float[] GetValues(Graph.Curve curve)
        //{
        //    List<float> result = new List<float>();
        //    if (curve.Data != null)
        //    {
        //        int lenght = curve.Data.Points.Length;
        //        for (int i = 0; i < lenght; i++)
        //        {
        //            result.Add(curve.Data.Points[i].y);
        //        }
        //    }
        //    foreach (var subCurve in curve.SubCurves)
        //    {
        //        result.AddRange(GetValues(subCurve));
        //    }
        //    return result.ToArray();
        //}
        #endregion
    }
}