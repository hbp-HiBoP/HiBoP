using HBP.Data.Experience.Protocol;
using HBP.Data.Informations;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools.Unity.Graph;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Informations
{
    public class GraphZone : MonoBehaviour
    {
        #region Properties
        [SerializeField] GameObject m_GraphPrefab;
        [SerializeField] RectTransform m_GraphContainer;

        [SerializeField] GameObject m_TogglesPrefab;
        [SerializeField] RectTransform m_ToggleContainer;

        [SerializeField] List<ColumnColor> m_Colors;

        Dictionary<int, Graph> m_GraphByColumn = new Dictionary<int, Graph>();
        //Dictionary<DataStruct, GroupCurveData[]> m_GroupCurveDataByData = new Dictionary<DataStruct, GroupCurveData>();

        DataStruct[] m_Data;
        ChannelStruct[] m_Channels;
        bool isLock;
        #endregion

        #region Public Methods
        public void Display(IEnumerable<ChannelStruct> channels, IEnumerable<DataStruct> data)
        {
            m_Data = data.ToArray();
            m_Channels = channels.ToArray();

            //GenerateCurves();
            //DisplayCurves();
            SetGraphs();
        }
        #endregion

        #region Private Methods
        void SetGraphs()
        {
            // Clear graphs
            ClearGraphs();

            // Subblocs
            List<Tuple<int, List<SubBloc>>> Columns = new List<Tuple<int, List<SubBloc>>>();
            foreach (var data in m_Data)
            {
                foreach (var bloc in data.Blocs)
                {
                    int mainSubBlocPosition = bloc.MainSubBlocPosition;
                    SubBloc[] orderedSubBlocs = bloc.OrderedSubBlocs.ToArray();
                    for (int i = 0; i < orderedSubBlocs.Length; i++)
                    {
                        int column = i - mainSubBlocPosition;
                        if (!Columns.Any(t => t.Item1 == column)) Columns.Add(new Tuple<int, List<SubBloc>>(column, new List<SubBloc>()));
                        Columns.Find(t => t.Item1 == column).Item2.Add(orderedSubBlocs[i]);
                    }
                }
            }
            Columns = Columns.OrderBy(t => t.Item1).ToList();
            foreach (var tuple in Columns) AddGraph(tuple.Item1, tuple.Item2.ToArray());
        }
        void AddGraph(int column, SubBloc[] subBlocs)
        {
            // Add Graph
            GameObject graphGameObject = Instantiate(m_GraphPrefab, m_GraphContainer);
            Graph graph = graphGameObject.GetComponent<Graph>();
            graph.OnChangeOrdinateDisplayRange.AddListener(OnChangeOrdinateDisplayRangeHandler);
            graph.OnChangeAbscissaDisplayRange.AddListener(OnChangeAbscissaDisplayRangeHandler);
            graphGameObject.SetActive(false);

            // Add Toggle
            GameObject toggleGameObject = Instantiate(m_TogglesPrefab, m_ToggleContainer);
            Toggle toggle = toggleGameObject.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(b => graph.gameObject.SetActive(b));
            toggle.group = m_ToggleContainer.GetComponent<ToggleGroup>();
            string name = subBlocs.All(s => s.Name == subBlocs.First().Name) ? subBlocs.First().Name : column.ToString();
            toggleGameObject.name = name;
            toggleGameObject.GetComponentInChildren<Text>().text = name;
            toggle.isOn = column == 0;

            m_GraphByColumn.Add(column, graph);
            
            GenerateCurves(subBlocs);
        }
    
        void GenerateCurves(SubBloc[] subBlocs)
        {
            //m_GroupCurveDataByData.Clear();

            //foreach (var data in m_Data)
            //{
            //    foreach (var bloc in data.Blocs)
            //    {

            //    }
            //    m_GroupCurveDataByData[data] = new GroupCurveData(data.Label);
            //    foreach (var channel in m_Channels)
            //    {
            //        BlocChannelStatistics blocChannelStatistics = DataManager.GetStatistics(data.DataInfo, channel);
            //        BlocChannelData blocChannelData = DataManager.GetData(data.DataInfo, data.Bloc, channel);
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
        void ClearGraphs()
        {
            // Destroy toggles.
            foreach(Transform child in m_ToggleContainer)
            {
                Destroy(child.gameObject);
            }

            // Destroy graphs.
            foreach (Transform child in m_GraphContainer)
            {
                Destroy(child.gameObject);
            }
            m_GraphByColumn = new Dictionary<int, Graph>();
        }
        void OnChangeAbscissaDisplayRangeHandler(Vector2 abscissaDisplayRange)
        {
            if (!isLock)
            {
                isLock = true;
                foreach (var graph in m_GraphByColumn.Values)
                {
                    graph.OrdinateDisplayRange = abscissaDisplayRange;
                }
                isLock = false;
            }
        }
        void OnChangeOrdinateDisplayRangeHandler(Vector2 ordinateDisplayRange)
        {
            if(!isLock)
            {
                isLock = true;
                foreach (var graph in m_GraphByColumn.Values)
                {
                    graph.OrdinateDisplayRange = ordinateDisplayRange;
                }
                isLock = false;
            }
        }
        #endregion
    }
}