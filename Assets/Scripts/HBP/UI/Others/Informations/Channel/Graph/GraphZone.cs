using HBP.Data;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.Informations;
using HBP.UI.TrialMatrix.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools.CSharp;
using Tools.Unity.Graph;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Informations
{
    public class GraphZone : MonoBehaviour
    {
        #region Properties
        [SerializeField] TrialMatrixGrid m_TrialMatrixGrid;
        [SerializeField] GameObject m_GraphPrefab;
        [SerializeField] RectTransform m_GraphContainer;

        [SerializeField] GameObject m_TogglesPrefab;
        [SerializeField] RectTransform m_ToggleContainer;

        [SerializeField] List<Color> m_Colors;
        Dictionary<Tuple<ChannelStruct, DataStruct, Data.Experience.Protocol.Bloc>, Color> m_ColorsByData;

        [SerializeField] Queue<Graph> m_GraphPool = new Queue<Graph>();
        [SerializeField] List<Graph> m_Graphs = new List<Graph>();
        [SerializeField] Vector2 m_OrdinateDisplayRange;
        [SerializeField] bool m_useDefaultDisplayRange = true;

        [SerializeField] DataStruct[] m_Data;
        [SerializeField] ChannelStruct[] m_Channels;
        bool m_isLock;
        #endregion

        #region Public Methods
        public void CreateGraphPool(int maxNumberOfColumn)
        {
            for (int i = 0; i < maxNumberOfColumn; i++)
            {
                GameObject graphGameObject = Instantiate(m_GraphPrefab, m_GraphContainer);
                graphGameObject.SetActive(false);

                RectTransform rectTransform = graphGameObject.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                Graph graph = graphGameObject.GetComponent<Graph>();
                graph.OnChangeOrdinateDisplayRange.AddListener(OnChangeOrdinateDisplayRangeHandler);
                graph.OnChangeUseDefaultRange.AddListener(OnUseDefaultDisplayRangeHandler);

                m_GraphPool.Enqueue(graph);
            }
        }
        public void Display(ChannelStruct[] channels, DataStruct[] data)
        {
            m_Data = data.ToArray();
            m_Channels = channels.ToArray();
            m_ColorsByData = GenerateColors(channels, data);

            SetGraphs();
        }
        #endregion

        #region Private Methods
        void ClearGraphs()
        {
            // Destroy toggles.
            foreach (Transform child in m_ToggleContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (Graph graph in m_Graphs)
            {
                graph.ClearCurves();
                graph.gameObject.SetActive(false);
                m_GraphPool.Enqueue(graph);
            }
            m_Graphs = new List<Graph>();
        }
        void SetGraphs()
        {
            UnityEngine.Profiling.Profiler.BeginSample("SaveSettings");
            SaveSettings();
            UnityEngine.Profiling.Profiler.EndSample();


            // Clear graphs
            UnityEngine.Profiling.Profiler.BeginSample("ClearGraphs");
            ClearGraphs();
            UnityEngine.Profiling.Profiler.EndSample();

            // Subblocs
            UnityEngine.Profiling.Profiler.BeginSample("GenerateDataCurve");
            Tuple<Graph.Curve[], Tools.CSharp.Window, bool>[] columns = GenerateDataCurve(m_Data, m_Channels);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("FindMinMax");
            Vector2 minMax = new Vector2(float.MaxValue, float.MinValue);
            foreach (var column in columns)
            {
                foreach (var curve in column.Item1)
                {
                    Vector2 curveMinMax = GetMinMax(curve);
                    if (curveMinMax.x < minMax.x) minMax.x = curveMinMax.x;
                    if (curveMinMax.y > minMax.y) minMax.y = curveMinMax.y;
                    minMax = GetMinMax(curve);
                }
            }
            float lenght = minMax.y - minMax.x;

            Vector2 defaultOrdinateDisplayRange = new Vector2(minMax.x - 0.1f * lenght, minMax.y + 0.1f * lenght);
            Vector2 ordinateDisplayRange;
            if (m_useDefaultDisplayRange)
            {
                ordinateDisplayRange = defaultOrdinateDisplayRange;
            }
            else
            {
                ordinateDisplayRange = m_OrdinateDisplayRange;
            }
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("AddGraphs");
            // Generate settings by columns
            foreach (var column in columns)
            {
                Vector2 defaultAbscissaDisplayRange = new Vector2(column.Item2.Start, column.Item2.End);
   
                AddGraph(column.Item1, defaultAbscissaDisplayRange, defaultOrdinateDisplayRange, defaultAbscissaDisplayRange, ordinateDisplayRange, column.Item3);
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }

        void AddGraph(Graph.Curve[] curves,Vector2 defaultAbscissaDisplayRange, Vector2 defaultOrdinateDisplayRange, Vector2 abscissaDisplayRange, Vector2 ordinateDisplayRange, bool isMain)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Instantiate graph");
            string name = "";

            // Add Graph
            Graph graph = m_GraphPool.Dequeue();
            graph.DefaultAbscissaDisplayRange = defaultAbscissaDisplayRange;
            graph.DefaultOrdinateDisplayRange = defaultOrdinateDisplayRange;
            graph.AbscissaDisplayRange = abscissaDisplayRange;
            graph.OrdinateDisplayRange = ordinateDisplayRange;

            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("AddCurves");
            Queue<Graph.Curve> curveQueue = new Queue<Graph.Curve>();
            foreach (var curve in curves)
            {
                curveQueue.Enqueue(curve);
                graph.AddCurve(curve);
            }
            while (curveQueue.Count > 0)
            {
                Graph.Curve curve = curveQueue.Dequeue();
                curve.OnChangeIsActive.AddListener((isActive) => OnChangeCurveIsActiveHandler(curve.ID, isActive));
                foreach (var subCurve in curve.SubCurves)
                {
                    curveQueue.Enqueue(subCurve);
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();

            // Add Toggle
            GameObject toggleGameObject = Instantiate(m_TogglesPrefab, m_ToggleContainer);
            Toggle toggle = toggleGameObject.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(b => graph.gameObject.SetActive(b));
            toggle.group = m_ToggleContainer.GetComponent<ToggleGroup>();
            toggleGameObject.name = name;
            toggleGameObject.GetComponentInChildren<Text>().text = name;
            toggle.isOn = isMain;

            m_Graphs.Add(graph);
        }

        void SaveSettings()
        {
            Graph graph = m_Graphs.FirstOrDefault();
            if (graph != null)
            {
                m_useDefaultDisplayRange = graph.UseDefaultDisplayRange;
                m_OrdinateDisplayRange = graph.OrdinateDisplayRange;
            }

        }

        void OnChangeOrdinateDisplayRangeHandler(Vector2 ordinateDisplayRange)
        {
            if (!m_isLock)
            {
                m_isLock = true;
                foreach (var graph in m_Graphs)
                {
                    graph.OrdinateDisplayRange = ordinateDisplayRange;
                }
                m_isLock = false;
            }
        }
        void OnChangeCurveIsActiveHandler(string ID, bool isActive)
        {
            if (!m_isLock)
            {
                m_isLock = true;
                foreach (var graph in m_Graphs)
                {
                    graph.SetEnabled(ID, isActive);
                }
                m_isLock = false;
            }
        }
        void OnUseDefaultDisplayRangeHandler(bool useDefaultDisplayRange)
        {
            if (!m_isLock)
            {
                m_isLock = true;
                foreach (var graph in m_Graphs)
                {
                    graph.UseDefaultDisplayRange = useDefaultDisplayRange;
                }
                m_isLock = false;
            }
        }

        Tuple<Graph.Curve[], Tools.CSharp.Window, bool>[] GenerateDataCurve(DataStruct[] data, ChannelStruct[] channels)
        {
            List<Data.Experience.Protocol.Bloc> blocs = new List<Data.Experience.Protocol.Bloc>();
            foreach (var d in data)
            {
                foreach (var bloc in d.Blocs)
                {
                    if (!blocs.Contains(bloc))
                    {
                        blocs.Add(bloc);
                    }
                }
            }

            Tuple<SubBloc[], Tools.CSharp.Window>[] subBlocsAndWindowByColumn = Data.Experience.Protocol.Bloc.GetSubBlocsAndWindowByColumn(blocs);
            List<Tuple<Graph.Curve[], Tools.CSharp.Window, bool>> result = new List<Tuple<Graph.Curve[], Tools.CSharp.Window, bool>>();
            foreach (var subBlocsAndWindow in subBlocsAndWindowByColumn)
            {
                List<Graph.Curve> curves = new List<Graph.Curve>();
                foreach (var d in data)
                {
                    Graph.Curve curve = GenerateDataCurve(channels, d, subBlocsAndWindow.Item1);
                    if (curve != null) curves.Add(curve);
                }
                result.Add(new Tuple<Graph.Curve[], Tools.CSharp.Window, bool>(curves.ToArray(), subBlocsAndWindow.Item2, subBlocsAndWindow.Item1[0].Type == Data.Enums.MainSecondaryEnum.Main));
            }
            return result.ToArray();
        }
        Graph.Curve GenerateDataCurve(ChannelStruct[] channels, DataStruct data, SubBloc[] subBlocs)
        {
            Graph.Curve result = new Graph.Curve(data.Data, null, true, data.Data, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));
            foreach (var bloc in data.Blocs)
            {
                SubBloc subBloc = bloc.SubBlocs.FirstOrDefault(s => subBlocs.Contains(s));
                if (subBloc != null)
                {
                    result.AddSubCurve(GenerateBlocCurve(channels, data, bloc, subBloc));
                }
            }
            if (result.SubCurves.Count == 0) return null;
            return result;
        }
        Graph.Curve GenerateBlocCurve(ChannelStruct[] channels, DataStruct data, Data.Experience.Protocol.Bloc bloc, SubBloc subBloc)
        {
            Graph.Curve result = new Graph.Curve(bloc.Name, null, true, data.Data + "_" + bloc.Name, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));
            Dictionary<Patient, List<ChannelStruct>> channelsByPatients = new Dictionary<Patient, List<ChannelStruct>>();
            foreach (var channel in channels)
            {
                if (!channelsByPatients.ContainsKey(channel.Patient))
                {
                    channelsByPatients.Add(channel.Patient, new List<ChannelStruct>());
                }
                channelsByPatients[channel.Patient].Add(channel);
            }
            foreach (var pair in channelsByPatients)
            {
                result.AddSubCurve(GeneratePatientCurve(pair.Value.ToArray(), data, bloc, subBloc));
            }
            return result;
        }
        Graph.Curve GeneratePatientCurve(ChannelStruct[] channels, DataStruct data, Data.Experience.Protocol.Bloc bloc, SubBloc subBloc)
        {
            Graph.Curve result = new Graph.Curve(channels[0].Patient.Name, null, true, data.Data + "_" + bloc.Name + "_" + channels[0].Patient.Name, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));
            foreach (var channel in channels)
            {
                result.AddSubCurve(GenerateChannelCurve(channel, data, bloc, subBloc));
            }
            return result;
        }
        Graph.Curve GenerateChannelCurve(ChannelStruct channel, DataStruct data, Data.Experience.Protocol.Bloc bloc, SubBloc subBloc)
        {
            CurveData curveData = GetCurveData(channel, data, bloc, subBloc);
            Graph.Curve result = new Graph.Curve(channel.Channel, curveData, true, data.Data + "_" + bloc.Name + "_" + channel.Patient.Name + "_" + channel.Channel, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));
            return result;
        }
        CurveData GetCurveData(ChannelStruct channel, DataStruct data, Data.Experience.Protocol.Bloc bloc, SubBloc subBloc)
        {
            CurveData result = null;
            DataInfo dataInfo = data.Dataset.Data.First(d => (d.Patient == channel.Patient && d.Name == data.Data));
            BlocChannelData blocChannelData = DataManager.GetData(dataInfo, bloc, channel.Channel);
            Color color = m_ColorsByData[new Tuple<ChannelStruct, DataStruct, Data.Experience.Protocol.Bloc>(channel, data, bloc)]; 

            ChannelBloc channelBloc = m_TrialMatrixGrid.Data.First(d => d.GridData.DataStruct == data).Blocs.First(b => b.Data.Data == bloc).ChannelBlocs.First(c => c.Data.Channel == channel);
            ChannelTrial[] validTrials = blocChannelData.Trials.Where(t => t.IsValid).ToArray();
            bool[] trialIsSelected = channelBloc.TrialIsSelected;
            List<ChannelTrial> trialsToUse = new List<ChannelTrial>(blocChannelData.Trials.Length);
            for (int i = 0; i < validTrials.Length; i++)
            {
                if (trialIsSelected[i])
                {
                    trialsToUse.Add(validTrials[i]);
                }
            }


            if (trialsToUse.Count > 1)
            {
                ChannelSubTrial[] channelSubTrials = trialsToUse.Select(t => t.ChannelSubTrialBySubBloc[subBloc]).ToArray();

                float[] values = new float[channelSubTrials[0].Values.Length];
                float[] standardDeviations = new float[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    List<float> sum = new List<float>();
                    for (int l = 0; l < trialsToUse.Count; l++)
                    {
                        sum.Add(channelSubTrials[l].Values[i]);
                    }
                    values[i] = sum.ToArray().Mean();
                    standardDeviations[i] = sum.ToArray().SEM();
                }

                // Generate points.
                int start = subBloc.Window.Start;
                int end = subBloc.Window.End;
                Vector2[] points = new Vector2[values.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    float abscissa = start + ((float)i / (points.Length - 1)) * (end - start);
                    float ordinate = values[i];
                    points[i] = new Vector2(abscissa, ordinate);
                }
                result = ShapedCurveData.CreateInstance(points, standardDeviations, color);
            }
            else if (trialsToUse.Count == 1)
            {
                ChannelSubTrial channelSubTrial = trialsToUse[0].ChannelSubTrialBySubBloc[subBloc];
                float[] values = channelSubTrial.Values;

                // Generate points.
                int start = subBloc.Window.Start;
                int end = subBloc.Window.End;
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
        Dictionary<Tuple<ChannelStruct, DataStruct, Data.Experience.Protocol.Bloc>, Color> GenerateColors(ChannelStruct[] channels, DataStruct[] data)
        {
            Dictionary<Tuple<ChannelStruct, DataStruct, Data.Experience.Protocol.Bloc>, Color> result = new Dictionary<Tuple<ChannelStruct, DataStruct, Data.Experience.Protocol.Bloc>, Color>();
            int colorIndex = 0;
            foreach (var channel in channels)
            {
                foreach (var d in data)
                {
                    foreach (var bloc in d.Blocs)
                    {
                        if (colorIndex < m_Colors.Count)
                        {
                            result.Add(new Tuple<ChannelStruct, DataStruct, Data.Experience.Protocol.Bloc>(channel, d, bloc), m_Colors[colorIndex]);
                            colorIndex++;
                        }
                        else
                        {
                            result.Add(new Tuple<ChannelStruct, DataStruct, Data.Experience.Protocol.Bloc>(channel, d, bloc), Color.white);
                        }
                    }
                }
            }
            return result;
        }

        Vector2 GetMinMax(Graph.Curve curve)
        {
            Vector2 result = new Vector2(float.MaxValue, float.MinValue);
            if(curve.Data != null)
            {
                result = new Vector2(curve.Data.Points.Min(p => p.y), curve.Data.Points.Max(p => p.y));
            }
            foreach (var subCurve in curve.SubCurves)
            {
                Vector2 curveResult = GetMinMax(subCurve);
                if (curveResult.x < result.x) result.x = curveResult.x;
                if (curveResult.y > result.y) result.y = curveResult.y;
            }
            return result;
        }
        #endregion

        #region Struct
        struct Settings
        {
            public Vector2 OrdinateDisplayRange;
            public Vector2 AbscissaDisplayRange;
            public Graph.Curve[] Curves;
        }
        #endregion
    }
}