using HBP.Data;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.Informations;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class GraphsGrid : MonoBehaviour
    {
        #region Properties
        [SerializeField] private GameObject m_ItemAndContainerPrefab;
        [SerializeField] private RectTransform m_RectTransform;
        [SerializeField] private GridLayoutGroup m_GridLayoutGroup;

        [SerializeField] List<Color> m_Colors;
        Dictionary<Tuple<int, Column>, Color> m_ColorsByColumn = new Dictionary<Tuple<int, Column>, Color>();

        [SerializeField] Column[] m_Columns;
        [SerializeField] ChannelStruct[] m_Channels;
        Color m_DefaultColor = new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1);

        private int m_NumberOfGridColumns = 5;
        #endregion

        #region Public Methods
        public void Display(ChannelStruct[] channels, Column[] columns)
        {
            m_Columns = columns.ToArray();
            m_Channels = channels.ToArray();
            GenerateColors(channels, columns);

            SetGraphs();
        }
        public void SetNumberOfGridColumns(float numberOfColumns)
        {
            m_NumberOfGridColumns = (int)numberOfColumns;
            UpdateLayout();
        }
        #endregion

        #region Private Methods
        private void Update()
        {
            if (transform.hasChanged)
            {
                UpdateLayout();
                transform.hasChanged = false;
            }
        }
        void ClearGraphs()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void SetGraphs()
        {
            //for (int r = 0; r < numberOfItems; r++)
            //{
            //    Instantiate(m_ItemAndContainerPrefab, transform);
            //}

            ClearGraphs();

            Graph.Curve[][] curveByColumnByChannel = GenerateDataCurve(m_Columns, m_Channels);

            List<Vector2> ordinateDisplayRangeByChannel = new List<Vector2>();
            foreach (var curveByColumn in curveByColumnByChannel)
            {
                List<float> values = new List<float>();
                foreach (var curve in curveByColumn)
                {
                    values.AddRange(GetValues(curve));
                }
                Vector2 defaultOrdinateDisplayRange = values.ToArray().CalculateValueLimit(5);
                ordinateDisplayRangeByChannel.Add(defaultOrdinateDisplayRange/*m_useDefaultDisplayRange ? defaultOrdinateDisplayRange : m_OrdinateDisplayRange*/);
            }

            Vector2 abscissaDisplayRange = new Vector2(float.MaxValue, float.MinValue);
            foreach (var column in m_Columns)
            {
                SubBloc mainSubBloc = column.Data.Bloc.MainSubBloc;
                if (mainSubBloc.Window.Start < abscissaDisplayRange.x)
                {
                    abscissaDisplayRange = new Vector2(mainSubBloc.Window.Start, abscissaDisplayRange.y);
                }
                if (mainSubBloc.Window.End > abscissaDisplayRange.y)
                {
                    abscissaDisplayRange = new Vector2(abscissaDisplayRange.x, mainSubBloc.Window.End);
                }
            }

            // Generate settings by columns
            for (int c = 0; c < curveByColumnByChannel.Length; c++)
            {
                var curveByColumn = curveByColumnByChannel[c];
                AddGraph(curveByColumn, abscissaDisplayRange, ordinateDisplayRangeByChannel[c]);
            }
        }
        void AddGraph(Graph.Curve[] curves, Vector2 abscissa, Vector2 ordinate)
        {
            SimpleGraph graph = Instantiate(m_ItemAndContainerPrefab, transform).GetComponent<GraphsGridContainer>().Content.GetComponent<SimpleGraph>();
            graph.DefaultAbscissaDisplayRange = abscissa;
            graph.AbscissaDisplayRange = abscissa;
            graph.DefaultOrdinateDisplayRange = ordinate;
            graph.OrdinateDisplayRange = ordinate;
            foreach (var curve in curves)
            {
                graph.AddCurve(curve);
            }
        }
        private void UpdateLayout()
        {
            float width = m_RectTransform.rect.width / m_NumberOfGridColumns;
            float height = width * 0.5f;
            m_GridLayoutGroup.cellSize = new Vector2(width, height);
        }
        Graph.Curve[][] GenerateDataCurve(Column[] columns, ChannelStruct[] channels)
        {
            // Find all visualized blocs and sort by column.
            IEnumerable<Bloc> blocs = columns.Select(c => c.Data.Bloc);

            List<Graph.Curve[]> result = new List<Graph.Curve[]>();
            foreach (var channel in channels)
            {
                List<Graph.Curve> curves = new List<Graph.Curve>();
                foreach (var column in columns)
                {
                    Graph.Curve curve = GenerateChannelCurve(column, channel, column.Data.Bloc.MainSubBloc);
                    curves.Add(curve);
                }
                result.Add(curves.ToArray());
            }
            return result.ToArray();
        }
        Graph.Curve GenerateChannelCurve(Column column, ChannelStruct channel, SubBloc subBloc)
        {
            string ID = column.Name + "_" + column.Data.Name + "_" + column.Data.Bloc.Name + "_" + column.Data.Dataset.Name + "_" + channel.Patient.Name + "_" + channel.Channel;
            CurveData curveData = GetCurveData(column, subBloc, channel);
            Graph.Curve result = new Graph.Curve(channel.Channel, curveData, true, ID, new Graph.Curve[0], m_DefaultColor);
            return result;
        }

        CurveData GetCurveData(Column column, SubBloc subBloc, ChannelStruct channel)
        {
            CurveData result = null;
            PatientDataInfo dataInfo = null;
            if (column.Data is HBP.Data.Informations.IEEGData ieegDataStruct)
            {
                dataInfo = ieegDataStruct.Dataset.GetIEEGDataInfos().First(d => (d.Patient == channel.Patient && d.Name == ieegDataStruct.Name));
            }
            else if (column.Data is HBP.Data.Informations.CCEPData ccepDataStruct)
            {
                dataInfo = ccepDataStruct.Dataset.GetCCEPDataInfos().First(d => (d.Patient == channel.Patient && d.StimulatedChannel == ccepDataStruct.Source.Channel && d.Patient == ccepDataStruct.Source.Patient && d.Name == ccepDataStruct.Name));
            }
            BlocData blocData = DataManager.GetData(dataInfo, column.Data.Bloc);
            BlocChannelData blocChannelData = DataManager.GetData(dataInfo, column.Data.Bloc, channel.Channel);
            Color color = m_ColorsByColumn.FirstOrDefault(k => k.Key.Item1 == Array.IndexOf(m_Channels, channel) && k.Key.Item2 == column).Value;

            ChannelTrial[] trials = blocChannelData.Trials.Where(t => t.IsValid).ToArray();

            float start = blocData.Frequency.ConvertNumberOfSamplesToMilliseconds(blocData.Frequency.ConvertToCeiledNumberOfSamples(subBloc.Window.Start));
            float end = blocData.Frequency.ConvertNumberOfSamplesToMilliseconds(blocData.Frequency.ConvertToFlooredNumberOfSamples(subBloc.Window.End));

            if (trials.Length > 1)
            {
                ChannelSubTrial[] channelSubTrials = trials.Select(t => t.ChannelSubTrialBySubBloc[subBloc]).ToArray();

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
                ChannelSubTrial channelSubTrial = trials[0].ChannelSubTrialBySubBloc[subBloc];
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
        void GenerateColors(ChannelStruct[] channels, Column[] columns)
        {
            foreach (var column in columns)
            {
                for (int channel = 0; channel < channels.Length; channel++)
                {
                    if (!m_ColorsByColumn.Any(c => c.Key.Item1 == channel && c.Key.Item2 == column))
                    {
                        Color color = m_Colors.FirstOrDefault(col => !m_ColorsByColumn.ContainsValue(col));
                        if (color == default) color = m_DefaultColor;
                        m_ColorsByColumn.Add(new Tuple<int, Column>(channel, column), color);
                    }
                }
            }
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