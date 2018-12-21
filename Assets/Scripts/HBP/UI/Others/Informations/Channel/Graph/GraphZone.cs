using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.Visualization;
using System.Collections.Generic;
using System.Linq;
using Tools.Unity.Graph;
using UnityEngine;

namespace HBP.UI.Informations
{
    public class GraphZone : MonoBehaviour
    {
        #region Properties
        [SerializeField] Graph m_Graph;
        [SerializeField] List<ColumnColor> m_Colors;
        Dictionary<DataStruct, GroupCurveData> m_GroupCurveDataByData = new Dictionary<DataStruct, GroupCurveData>();
        DataStruct[] m_Data;
        string[] m_Channels;
        #endregion

        #region Public Methods
        public void Display(IEnumerable<DataStruct> data, IEnumerable<string> channels)
        {
            m_Data = data.ToArray();
            m_Channels = channels.ToArray();

            GenerateCurves();
            DisplayCurves();
        }
        #endregion

        #region Private Methods
        void GenerateCurves()
        {
            //m_GroupCurveDataByData.Clear();
            //foreach (var data in m_Data)
            //{
            //    m_GroupCurveDataByData[data] = new GroupCurveData(data.Label);
            //    foreach (var channel in m_Channels)
            //    {
            //        BlocChannelData blocChannelData = DataManager.GetData(data.DataInfo, data.Bloc, channel);
            //        blocChannelData.Trials[]
            //        TrialMatrixData trialMatrixData = m_TrialMatrixByProtocolBySiteByDataInfo[column.Protocol][site][Scene.Visualization.GetDataInfo(site.Information.Patient, column)];
            //        TrialMatrix.TrialMatrix trialMatrix = m_TrialMatrixList.TrialMatrix.FirstOrDefault((t) => t.Data == trialMatrixData);
            //        if (trialMatrix == null) continue;
            //        TrialMatrix.Bloc trialMatrixBloc = null;
            //        foreach (var line in trialMatrix.Lines)
            //        {
            //            foreach (var bloc in line.Blocs)
            //            {
            //                if (bloc.Data.ProtocolBloc == column.Bloc)
            //                {
            //                    trialMatrixBloc = bloc;
            //                    goto Found;
            //                }
            //            }
            //        }
            //        Found:
            //        DataStruct.TrialMatrix.Line[] linesToRead = trialMatrixBloc.Data.GetLines(trialMatrixBloc.SelectedLines);
            //        float[] dataValue = new float[linesToRead.Length > 0 ? linesToRead.First().NormalizedValues.Length : 0];
            //        Timeline timeline = column.TimeLineByFrequency[(int)DataManager.GetData(Scene.Visualization.GetDataInfo(site.Information.Patient, column), column.Bloc).Frequency];
            //        if (linesToRead.Length > 1)
            //        {
            //            // Shape
            //            float[] standardDeviations = new float[data.Length];
            //            for (int i = 0; i < data.Length; i++)
            //            {
            //                List<float> l_dataList = new List<float>();
            //                for (int l = 0; l < linesToRead.Length; l++)
            //                {
            //                    l_dataList.Add(linesToRead[l].NormalizedValues[i]);
            //                }

            //                //Find selectedLines
            //                data[i] = l_dataList.ToArray().Mean();
            //                standardDeviations[i] = l_dataList.ToArray().SEM();
            //            }

            //            // Generate points.
            //            int pMin = timeline.Start.RawPosition;
            //            int pMax = timeline.End.RawPosition;
            //            float min = timeline.Start.RawValue;
            //            float max = timeline.End.RawValue;
            //            int lenght = pMax + 1 - pMin;
            //            Vector2[] points = new Vector2[lenght];
            //            for (int i = 0; i < lenght; i++)
            //            {
            //                int index = pMin + i;
            //                float absciss = min + ((max - min) * (index - pMin) / (pMax - pMin));
            //                points[i] = new Vector2(absciss, data[index]);
            //            }

            //            m_CurvesByColumn[column].Curves.Add(new ShapedCurveData(site.Information.ChannelName, column.Name + "_" + site.Information.FullCorrectedID, points, standardDeviations, GetCurveColor(d, s), 1.5f));
            //        }
            //        else if (linesToRead.Length == 1)
            //        {
            //            // Normal
            //            data = trialMatrixBloc.Data.Lines[trialMatrixBloc.SelectedLines[0]].NormalizedValues;

            //            // Generate points.
            //            int pMin = timeline.Start.RawPosition;
            //            int pMax = timeline.End.RawPosition;
            //            float min = timeline.Start.RawValue;
            //            float max = timeline.End.RawValue;
            //            int lenght = pMax + 1 - pMin;
            //            Vector2[] points = new Vector2[lenght];
            //            for (int i = 0; i < lenght; i++)
            //            {
            //                int index = pMin + i;
            //                float absciss = min + ((max - min) * (index - pMin) / (pMax - pMin));
            //                points[i] = new Vector2(absciss, data[index]);
            //            }

            //            //Create curve
            //            m_CurvesByColumn[column].Curves.Add(new CurveData(site.Information.ChannelName, column.Name + "_" + site.Information.FullCorrectedID, points, GetCurveColor(d, s), 1.5f));
            //        }
            //        else continue;
            //    }

            //    // ROI
            //    if (m_Scene.ColumnManager.ColumnsIEEG[d].ROIs.Count > 0 && m_Scene.ColumnManager.ColumnsIEEG[d].SelectedROI != null)
            //    {
            //        Site[] sites = (from site in m_Scene.ColumnManager.ColumnsIEEG[d].Sites where !site.State.IsOutOfROI && !site.State.IsExcluded && !site.State.IsBlackListed && !site.State.IsMasked select site).ToArray();
            //        if (sites.Length > 0)
            //        {
            //            float[] ROIdata = new float[sites.First().Configuration.Values.Length];
            //            for (int i = 0; i < ROIdata.Length; i++)
            //            {
            //                List<float> sum = new List<float>(sites.Length);
            //                foreach (var site in sites)
            //                {
            //                    sum.Add(site.Configuration.NormalizedValues[i]);
            //                }
            //                ROIdata[i] = sum.ToArray().Mean();
            //            }

            //            // Generate points.
            //            int pMin = column.TimeLine.Start.RawPosition;
            //            int pMax = column.TimeLine.End.RawPosition;
            //            float min = column.TimeLine.Start.RawValue;
            //            float max = column.TimeLine.End.RawValue;
            //            int lenght = pMax + 1 - pMin;
            //            Vector2[] points = new Vector2[lenght];
            //            for (int i = 0; i < lenght; i++)
            //            {
            //                int index = pMin + i;
            //                float absciss = min + ((max - min) * (index - pMin) / (pMax - pMin));
            //                points[i] = new Vector2(absciss, ROIdata[index]);
            //            }
            //            m_CurvesByColumn[column].Curves.Add(new CurveData(m_Scene.ColumnManager.ColumnsIEEG[d].SelectedROI.Name, column.Name + "_" + m_Scene.ColumnManager.ColumnsIEEG[d].SelectedROI.Name, points, GetCurveColor(d, -1), 3.0f));
            //        }
            //    }
            //}
            //UnityEngine.Profiling.Profiler.EndSample();
        }
        void DisplayCurves()
        {
            //UnityEngine.Profiling.Profiler.BeginSample("DisplayCurves()");
            //List<GroupCurveData> groupCurvesToDisplay = new List<GroupCurveData>();
            //foreach (var column in m_Scene.ColumnManager.ColumnsIEEG)
            //{
            //    if (!column.IsMinimized || !ApplicationState.GeneralSettings.HideCurveWhenColumnHidden)
            //    {
            //        groupCurvesToDisplay.Add(m_CurvesByColumn[column.ColumnData]);
            //    }
            //}
            //if (groupCurvesToDisplay.Count > 0)
            //{
            //    GraphData graphData = new GraphData("EEG", "Time(ms)", "Activity(mV)", Color.black, Color.white, groupCurvesToDisplay.ToArray());
            //    m_Graph.Plot(graphData);
            //}
            //UnityEngine.Profiling.Profiler.EndSample();
        }
        Color GetCurveColor(int column, int site)
        {
            ColumnColor columnColor = m_Colors[column];
            if (site == -1)
            {
                return columnColor.ROI;
            }
            else if (site == 0)
            {
                return columnColor.Site1;
            }
            else if (site == 1)
            {
                return columnColor.Site2;
            }
            else if (site == 2)
            {
                return columnColor.Site3;
            }
            else if (site == 3)
            {
                return columnColor.Site4;
            }
            else
            {
                return new Color();
            }
        }
        #endregion

        public struct DataStruct
        {
            public string Label { get; set; }
            public DataInfo DataInfo { get; set; }
            public Bloc Bloc { get; set; }
        }
          
    }
}