using HBP.Display.Informations;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace HBP.UI.Tools.Graphs
{
    public class GraphsGrid : MonoBehaviour
    {
        #region Properties
        [SerializeField] private GameObject m_ItemAndContainerPrefab;
        [SerializeField] private ScrollRect m_ScrollRect;
        
        [SerializeField] Column[] m_Columns;
        [SerializeField] ChannelStruct[] m_Channels;
        Color m_DefaultColor = new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1);

        public List<SimpleGraph> Graphs { get; private set; } = new List<SimpleGraph>();
        private List<GraphsGridContainer> m_Containers = new List<GraphsGridContainer>();
        private Dictionary<SimpleGraph, ChannelStruct> m_ChannelByGraph = new Dictionary<SimpleGraph, ChannelStruct>();

        [SerializeField] private bool m_UseDefaultOrdinateRange;
        public bool UseDefaultOrdinateRange
        {
            get
            {
                return m_UseDefaultOrdinateRange;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_UseDefaultOrdinateRange, value))
                {
                    m_OnChangeUseDefaultOrdinateRange.Invoke(value);
                    if (value)
                    {
                        foreach (var graph in Graphs)
                        {
                            List<float> values = new List<float>();
                            foreach (var curve in graph.Curves)
                            {
                                values.AddRange(GetValues(curve));
                            }
                            graph.OrdinateDisplayRange = values.ToArray().CalculateValueLimit(5);
                        }
                    }
                    else
                    {
                        foreach (var graph in Graphs)
                        {
                            graph.OrdinateDisplayRange = OrdinateDisplayRange;
                        }
                    }
                }
            }
        }
        [SerializeField] private Vector2 m_OrdinateDisplayRange;
        public Vector2 OrdinateDisplayRange
        {
            get
            {
                return m_OrdinateDisplayRange;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_OrdinateDisplayRange, value))
                {
                    m_OnChangeOrdinateDisplayRange.Invoke(value);
                    if (!m_UseDefaultOrdinateRange)
                    {
                        foreach (var graph in Graphs)
                        {
                            graph.OrdinateDisplayRange = value;
                        }
                    }
                }
            }
        }
        [SerializeField] private Vector2 m_AbscissaDisplayRange;
        public Vector2 AbscissaDisplayRange
        {
            get
            {
                return m_AbscissaDisplayRange;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_AbscissaDisplayRange, value))
                {
                    m_OnChangeAbscissaDisplayRange.Invoke(value);
                    foreach (var graph in Graphs)
                    {
                        graph.AbscissaDisplayRange = value;
                    }
                }
            }
        }

        private int m_NumberOfGridColumns = 2;
        public int NumberOfGridLines { get { return (int)Mathf.Ceil((float)m_Containers.Count / m_NumberOfGridColumns); } }
        #endregion

        #region Events
        [SerializeField] private BoolEvent m_OnChangeUseDefaultOrdinateRange;
        public BoolEvent OnChangeUseDefaultOrdinateRange
        {
            get
            {
                return m_OnChangeUseDefaultOrdinateRange;
            }
        }
        [SerializeField] private Vector2Event m_OnChangeOrdinateDisplayRange;
        public Vector2Event OnChangeOrdinateDisplayRange
        {
            get
            {
                return m_OnChangeOrdinateDisplayRange;
            }
        }
        [SerializeField] private Vector2Event m_OnChangeAbscissaDisplayRange;
        public Vector2Event OnChangeAbscissaDisplayRange
        {
            get
            {
                return m_OnChangeAbscissaDisplayRange;
            }
        }
        [SerializeField] private Graph.CurvesEvent m_OnSetGraphs;
        public Graph.CurvesEvent OnSetGraphs
        {
            get
            {
                return m_OnSetGraphs;
            }
        }
        [SerializeField] private ChannelsEvent m_OnRequestDisplayChannelsOnGraph;
        public ChannelsEvent OnRequestDisplayChannelsOnGraph
        {
            get
            {
                return m_OnRequestDisplayChannelsOnGraph;
            }
        }
        [SerializeField] private ChannelsEvent m_OnRequestFilterChannels;
        public ChannelsEvent OnRequestFilterChannels
        {
            get
            {
                return m_OnRequestFilterChannels;
            }
        }
        #endregion

        #region Public Methods
        public void SetEnabled(string id, bool enabled)
        {
            foreach (var graph in Graphs)
            {
                graph.SetEnabled(id, enabled);
            }
        }
        public void Display(ChannelStruct[] channels, Column[] columns)
        {
            m_Columns = columns.ToArray();
            m_Channels = channels.ToArray();

            SetGraphs();
        }
        public void SetNumberOfGridColumns(float numberOfColumns)
        {
            m_NumberOfGridColumns = (int)numberOfColumns;
            UpdateLayout();
        }
        public void DisplaySelectedGraphs()
        {
            ChannelStruct[] channels = m_ChannelByGraph.Where(cbg => cbg.Key.IsSelected).Select(cbg => cbg.Value).ToArray();
            m_OnRequestDisplayChannelsOnGraph.Invoke(channels);
        }
        public void UnselectAll()
        {
            foreach (var graph in Graphs)
            {
                graph.IsSelected = false;
            }
        }
        public void FilterSelectedSites()
        {
            ChannelStruct[] channels = m_ChannelByGraph.Where(cbg => cbg.Key.IsSelected).Select(cbg => cbg.Value).ToArray();
            m_OnRequestFilterChannels.Invoke(channels);
        }
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_ScrollRect.viewport.hasChanged)
            {
                float ratio = Mathf.Ceil((float)m_Containers.Count / m_NumberOfGridColumns) / m_NumberOfGridColumns;
                m_ScrollRect.content.sizeDelta = new Vector2(m_ScrollRect.content.sizeDelta.x, (m_ScrollRect.viewport.rect.width / 2) * ratio);
                m_ScrollRect.viewport.hasChanged = false;
            }
        }
        void ClearGraphs()
        {
            foreach (Transform child in m_ScrollRect.content)
            {
                Destroy(child.gameObject);
            }
            Graphs = new List<SimpleGraph>();
            m_Containers = new List<GraphsGridContainer>();
            m_ChannelByGraph = new Dictionary<SimpleGraph, ChannelStruct>();
        }

        private void SetGraphs()
        {
            ClearGraphs();

            Graph.Curve[][] curveByColumnByChannel = GenerateDataCurve(m_Columns, m_Channels);

            List<Vector2> ordinateDisplayRangeByChannel = new List<Vector2>();
            foreach (var curveByColumn in curveByColumnByChannel)
            {
                if (m_UseDefaultOrdinateRange)
                {
                    List<float> values = new List<float>();
                    foreach (var curve in curveByColumn)
                    {
                        values.AddRange(GetValues(curve));
                    }
                    Vector2 defaultOrdinateDisplayRange = values.ToArray().CalculateValueLimit(5);
                    ordinateDisplayRangeByChannel.Add(defaultOrdinateDisplayRange);
                }
                else
                {
                    ordinateDisplayRangeByChannel.Add(m_OrdinateDisplayRange);
                }
            }

            for (int c = 0; c < curveByColumnByChannel.Length; c++)
            {
                var curveByColumn = curveByColumnByChannel[c];
                AddGraph(m_Channels[c], curveByColumn, m_AbscissaDisplayRange, ordinateDisplayRangeByChannel[c]);
            }

            UpdateLayout();

            if (curveByColumnByChannel.Length > 0)
            {
                m_OnSetGraphs.Invoke(curveByColumnByChannel[0]);
            }
        }
        void AddGraph(ChannelStruct channelStruct, Graph.Curve[] curves, Vector2 abscissa, Vector2 ordinate)
        {
            GraphsGridContainer container = Instantiate(m_ItemAndContainerPrefab, m_ScrollRect.content).GetComponent<GraphsGridContainer>();
            m_Containers.Add(container);
            SimpleGraph graph = container.Content.GetComponent<SimpleGraph>();
            graph.AbscissaDisplayRange = abscissa;
            graph.OrdinateDisplayRange = ordinate;
            graph.Title = string.Format("{0} ({1})", channelStruct.Channel, channelStruct.Patient.Name);
            graph.ChannelStruct = channelStruct;
            foreach (var curve in curves)
            {
                graph.AddCurve(curve);
            }
            Graphs.Add(graph);
            m_ChannelByGraph.Add(graph, channelStruct);
        }
        private void UpdateLayout()
        {
            int numberOfLines = NumberOfGridLines;
            for (int i = 0; i < m_Containers.Count;)
            {
                for (int j = 0; j < m_NumberOfGridColumns && i < m_Containers.Count; ++j, ++i)
                {
                    RectTransform rectTransform = m_Containers[i].GetComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2((float)j / m_NumberOfGridColumns, 1f - ((float)((i / m_NumberOfGridColumns) + 1) / numberOfLines));
                    rectTransform.anchorMax = new Vector2((float)(j + 1) / m_NumberOfGridColumns, 1f - ((float)(i / m_NumberOfGridColumns) / numberOfLines));
                }
            }
            m_ScrollRect.viewport.hasChanged = true;
        }
        Graph.Curve[][] GenerateDataCurve(Column[] columns, ChannelStruct[] channels)
        {
            List<Graph.Curve[]> result = new List<Graph.Curve[]>();

            // Find all visualized blocs and sort by column.
            IEnumerable<Column> epochedDataColumns = columns.Where(c => c.Data is IEEGData || c.Data is CCEPData);
            IEnumerable<Column> nonEpochedDataColumns = columns.Where(c => c.Data is MEGData);
            IEnumerable<Core.Data.Bloc> blocs = epochedDataColumns.Select(c => c.Data.Bloc);

            foreach (var channel in channels)
            {
                List<Graph.Curve> curves = new List<Graph.Curve>();
                foreach (var column in epochedDataColumns)
                {
                    Graph.Curve curve = GenerateChannelCurve(column, channel, column.Data.Bloc.MainSubBloc);
                    curves.Add(curve);
                }
                foreach (var column in nonEpochedDataColumns)
                {
                    Graph.Curve curve = GenerateNonEpochedChannelCurve(column, channel);
                    curves.Add(curve);
                }
                result.Add(curves.ToArray());
            }

            return result.ToArray();
        }
        Graph.Curve GenerateChannelCurve(Column column, ChannelStruct channel, Core.Data.SubBloc subBloc)
        {
            string ID = column.Name + "_" + column.Data.Name + "_" + column.Data.Bloc.Name + "_" + column.Data.Dataset.Name;
            CurveData curveData = GetCurveData(column, subBloc, channel);
            Graph.Curve result = new Graph.Curve(column.Name, curveData, true, ID, new Graph.Curve[0], m_DefaultColor);
            return result;
        }
        Graph.Curve GenerateNonEpochedChannelCurve(Column column, ChannelStruct channel)
        {
            string ID = column.Name + "_" + column.Data.Name + "_" + column.Data.Dataset.Name;
            CurveData curveData = GetNonEpochedCurveData(column, channel);
            Graph.Curve result = new Graph.Curve(column.Name, curveData, true, ID, new Graph.Curve[0], m_DefaultColor);
            return result;
        }

        CurveData GetCurveData(Column column, Core.Data.SubBloc subBloc, ChannelStruct channel)
        {
            CurveData result = null;
            Core.Data.PatientDataInfo dataInfo = null;
            if (column.Data is IEEGData ieegDataStruct)
            {
                dataInfo = ieegDataStruct.Dataset.GetIEEGDataInfos().First(d => (d.Patient == channel.Patient && d.Name == ieegDataStruct.Name));
            }
            else if (column.Data is CCEPData ccepDataStruct)
            {
                dataInfo = ccepDataStruct.Dataset.GetCCEPDataInfos().First(d => (d.Patient == channel.Patient && d.StimulatedChannel == ccepDataStruct.Source.Channel && d.Patient == ccepDataStruct.Source.Patient && d.Name == ccepDataStruct.Name));
            }
            Core.Data.BlocData blocData = Core.Data.DataManager.GetData(dataInfo, column.Data.Bloc);
            Core.Data.BlocChannelData blocChannelData = Core.Data.DataManager.GetData(dataInfo, column.Data.Bloc, channel.Channel);
            Color color = Core.Data.ApplicationState.UserPreferences.Visualization.Graph.GetColor(0, Array.IndexOf(m_Columns, column));

            Core.Data.ChannelTrial[] trials = blocChannelData.Trials.Where(t => t.IsValid).ToArray();

            float start = blocData.Frequency.ConvertNumberOfSamplesToMilliseconds(blocData.Frequency.ConvertToCeiledNumberOfSamples(subBloc.Window.Start));
            float end = blocData.Frequency.ConvertNumberOfSamplesToMilliseconds(blocData.Frequency.ConvertToFlooredNumberOfSamples(subBloc.Window.End));

            if (trials.Length > 1)
            {
                Core.Data.ChannelSubTrial[] channelSubTrials = trials.Select(t => t.ChannelSubTrialBySubBloc[subBloc]).ToArray();

                float[] values = new float[channelSubTrials[0].Values.Length];
                float[] standardDeviations = new float[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    List<float> sum = new List<float>();
                    for (int l = 0; l < trials.Length; l++)
                    {
                        sum.Add(channelSubTrials[l].Values[i]);
                    }
                    values[i] = sum.ToArray().Mean();
                    standardDeviations[i] = sum.ToArray().SEM();
                }

                // Generate points.
                Vector2[] points = new Vector2[values.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    float abscissa = start + ((float)i / (points.Length - 1)) * (end - start);
                    float ordinate = values[i];
                    points[i] = new Vector2(abscissa, ordinate);
                }
                result = ShapedCurveData.CreateInstance(points, standardDeviations, color);
            }
            else if (trials.Length == 1)
            {
                Core.Data.ChannelSubTrial channelSubTrial = trials[0].ChannelSubTrialBySubBloc[subBloc];
                float[] values = channelSubTrial.Values;

                // Generate points.
                Vector2[] points = new Vector2[values.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    float abscissa = start + ((float)i / (points.Length - 1)) * (end - start);
                    float ordinate = values[i];
                    points[i] = new Vector2(abscissa, ordinate);
                }
                result = CurveData.CreateInstance(points, color);
            }
            return result;
        }
        CurveData GetNonEpochedCurveData(Column column, ChannelStruct channel)
        {
            CurveData result = null;
            Core.Data.PatientDataInfo dataInfo = null;
            if (column.Data is MEGData megDataStruct)
            {
                dataInfo = megDataStruct.Dataset.GetMEGDataInfos().OfType<Core.Data.MEGcDataInfo>().FirstOrDefault(d => (d.Patient == channel.Patient && d.Name == megDataStruct.Name));
                if (dataInfo == null) return null;
            }
            Core.Data.MEGcData megData = Core.Data.DataManager.GetData(dataInfo) as Core.Data.MEGcData;
            Color color = Core.Data.ApplicationState.UserPreferences.Visualization.Graph.GetColor(Array.IndexOf(m_Channels, channel), Array.IndexOf(m_Columns, column));
            if (megData == null)
                return null;

            float start = 0;
            float end = 0;
            if (megData.ValuesByChannel.TryGetValue(channel.Channel, out float[] values))
            {
                end = megData.Frequency.ConvertNumberOfSamplesToMilliseconds(values.Length);

                // Generate points.
                Vector2[] points = new Vector2[values.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    float abscissa = start + ((float)i / (points.Length - 1)) * (end - start);
                    float ordinate = values[i];
                    points[i] = new Vector2(abscissa, ordinate);
                }
                result = CurveData.CreateInstance(points, color);
            }

            return result;
        }

        List<float> GetValues(Graph.Curve curve)
        {
            List<float> result = new List<float>();
            if (curve.Data != null)
            {
                int length = curve.Data.Points.Length;
                for (int i = 0; i < length; i++)
                {
                    result.Add(curve.Data.Points[i].y);
                }
            }
            foreach (var subCurve in curve.SubCurves)
            {
                result.AddRange(GetValues(subCurve));
            }
            return result;
        }
        #endregion
    }
}