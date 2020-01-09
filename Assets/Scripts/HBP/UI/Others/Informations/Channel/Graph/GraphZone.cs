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
        public Dictionary<Tuple<int, DataStruct, Data.Experience.Protocol.Bloc>, Color> ColorsByData { get; private set; } = new Dictionary<Tuple<int, DataStruct, Data.Experience.Protocol.Bloc>, Color>();
        Dictionary<Tuple<ROIStruct, DataStruct, Data.Experience.Protocol.Bloc>, Color> m_ColorsByROI = new Dictionary<Tuple<ROIStruct, DataStruct, Data.Experience.Protocol.Bloc>, Color>();
        Dictionary<string, bool> m_StatesByCurves = new Dictionary<string, bool>();

        [SerializeField] Queue<Graph> m_GraphPool = new Queue<Graph>();
        [SerializeField] List<Graph> m_Graphs = new List<Graph>();
        [SerializeField] int m_SelectedColumn;
        [SerializeField] Vector2 m_OrdinateDisplayRange;
        [SerializeField] Vector2[] m_AbscissaDisplayRange;
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
            GenerateColors(channels, data);

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
            SaveSettings();

            ClearGraphs();

            Tuple<Graph.Curve[], Tools.CSharp.Window, bool>[] columns = GenerateDataCurve(m_Data, m_Channels);

            List<float> values = new List<float>();
            foreach (var column in columns)
            {
                foreach (var curve in column.Item1)
                {
                    values.AddRange(GetValues(curve));
                }
            }
            Vector2 defaultOrdinateDisplayRange = values.ToArray().CalculateValueLimit(5);
            Vector2 ordinateDisplayRange = m_useDefaultDisplayRange ? defaultOrdinateDisplayRange : m_OrdinateDisplayRange;

            Vector2[] abscissaDisplayRange = new Vector2[columns.Length];
            for (int c = 0; c < abscissaDisplayRange.Length; c++)
            {
                if (m_useDefaultDisplayRange || c >= m_AbscissaDisplayRange.Length)
                {
                    abscissaDisplayRange[c] = columns[c].Item2.ToVector2();
                }
                else
                {
                    abscissaDisplayRange[c] = m_AbscissaDisplayRange[c];
                }
            }

            // Generate settings by columns
            for (int c = 0; c < columns.Length; c++)
            {
                var column = columns[c];
                bool selected = c == m_SelectedColumn;
                AddGraph(column.Item1, column.Item2.ToVector2(), defaultOrdinateDisplayRange, abscissaDisplayRange[c], ordinateDisplayRange, selected);
            }
        }
        void AddGraph(Graph.Curve[] curves, Vector2 defaultAbscissaDisplayRange, Vector2 defaultOrdinateDisplayRange, Vector2 abscissaDisplayRange, Vector2 ordinateDisplayRange, bool selected)
        {
            string name = "";

            // Add Graph
            Graph graph = m_GraphPool.Dequeue();
            graph.DefaultAbscissaDisplayRange = defaultAbscissaDisplayRange;
            graph.DefaultOrdinateDisplayRange = defaultOrdinateDisplayRange;
            graph.AbscissaDisplayRange = abscissaDisplayRange;
            graph.OrdinateDisplayRange = ordinateDisplayRange;


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

            Dictionary<string, bool> dico = m_StatesByCurves.ToDictionary((t) => t.Key, (t) => t.Value);
            foreach (var pair in dico)
            {
                graph.SetEnabled(pair.Key, pair.Value);
            }

            // Add Toggle
            GameObject toggleGameObject = Instantiate(m_TogglesPrefab, m_ToggleContainer);
            Toggle toggle = toggleGameObject.GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(b => graph.gameObject.SetActive(b));
            toggle.group = m_ToggleContainer.GetComponent<ToggleGroup>();
            toggleGameObject.name = name;
            toggleGameObject.GetComponentInChildren<Text>().text = name;
            toggle.isOn = selected;

            m_Graphs.Add(graph);
        }
        void SaveSettings()
        {
            m_AbscissaDisplayRange = new Vector2[m_Graphs.Count];
            for (int g = 0; g < m_Graphs.Count; g++)
            {
                Graph graph = m_Graphs[g];
                if (graph.isActiveAndEnabled) m_SelectedColumn = g;
                m_useDefaultDisplayRange = graph.UseDefaultDisplayRange;
                m_OrdinateDisplayRange = graph.OrdinateDisplayRange;
                m_AbscissaDisplayRange[g] = graph.AbscissaDisplayRange;
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
                m_StatesByCurves[ID] = isActive;
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
                    if (!blocs.Contains(bloc.Bloc))
                    {
                        blocs.Add(bloc.Bloc);
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
            string id = "";
            if(data is IEEGDataStruct ieegDataStruct) id = ieegDataStruct.Data + "_" + "IEEG";
            else if(data is CCEPDataStruct ccepDataStruct) id = ccepDataStruct.Data + "_CCEP_" + ccepDataStruct.Source.Patient.CompleteName + "_" + ccepDataStruct.Source.Channel;
            Graph.Curve result = new Graph.Curve(data.Data, null, true, id, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));
            foreach (var bloc in data.Blocs)
            {
                SubBloc subBloc = bloc.Bloc.SubBlocs.FirstOrDefault(s => subBlocs.Contains(s));
                if (subBloc != null)
                {
                    result.AddSubCurve(GenerateBlocCurve(channels, data, bloc.Bloc, subBloc, id));
                }
            }
            if (result.SubCurves.Count == 0) return null;
            return result;
        }
        Graph.Curve GenerateBlocCurve(ChannelStruct[] channels, DataStruct data, Data.Experience.Protocol.Bloc bloc, SubBloc subBloc, string id)
        {
            id = id + "_" + bloc.Name;
            Graph.Curve result = new Graph.Curve(bloc.Name, null, true, id, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));

            // Patients
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
                result.AddSubCurve(GeneratePatientCurve(pair.Value.ToArray(), data, bloc, subBloc, id));
            }

            // ROIs
            if (data.Blocs.First(b => b.Bloc == bloc).ROIs.Count > 0)
            {
                result.AddSubCurve(GenerateROIsCurve(data, bloc, subBloc, id));
            }

            return result;
        }
        Graph.Curve GeneratePatientCurve(ChannelStruct[] channels, DataStruct data, Data.Experience.Protocol.Bloc bloc, SubBloc subBloc, string id)
        {
            id = id + "_" + channels[0].Patient.Name;
            Graph.Curve result = new Graph.Curve(channels[0].Patient.Name, null, true, id, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));
            foreach (var channel in channels)
            {
                result.AddSubCurve(GenerateChannelCurve(channel, data, bloc, subBloc, id));
            }
            return result;
        }
        Graph.Curve GenerateROIsCurve(DataStruct data, Data.Experience.Protocol.Bloc bloc, SubBloc subBloc, string id)
        {
            id = id + "_ROI";
            Graph.Curve result = new Graph.Curve("ROI", null, true, id, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));
            BlocStruct blocStruct = data.Blocs.First(b => b.Bloc == bloc);
            foreach (var ROI in blocStruct.ROIs)
            {
                result.AddSubCurve(GenerateROICurve(data, ROI, bloc, subBloc, id));
            }
            return result;
        }
        Graph.Curve GenerateROICurve(DataStruct data, ROIStruct ROI, Data.Experience.Protocol.Bloc bloc, SubBloc subBloc, string id)
        {
            id = id + "_" + ROI.Name;

            CurveData curveData = null;
            Dictionary<Patient, List<string>> ChannelsByPatient = new Dictionary<Patient, List<string>>();
            foreach (var channel in ROI.Channels)
            {
                ChannelsByPatient.AddIfAbsent(channel.Patient, new List<string>());
                ChannelsByPatient[channel.Patient].Add(channel.Channel);
            }
            Dictionary<Patient, PatientDataInfo> dataInfoByPatient = new Dictionary<Patient, PatientDataInfo>(ChannelsByPatient.Count);
            if(data is IEEGDataStruct ieegDataStruct)
            {
                iEEGDataInfo[] ieegDataInfo = ieegDataStruct.Dataset.GetIEEGDataInfos();
                foreach (var patient in ChannelsByPatient.Keys)
                {
                    dataInfoByPatient.Add(patient, ieegDataInfo.First(d => d.Patient == patient && d.Name == ieegDataStruct.Data));
                }
            }
            else if(data is CCEPDataStruct ccepDataStruct)
            {
                CCEPDataInfo[] ccepDataInfo = ccepDataStruct.Dataset.GetCCEPDataInfos();
                foreach (var patient in ChannelsByPatient.Keys)
                {
                    dataInfoByPatient.Add(patient, ccepDataInfo.First(d => d.Patient == patient && d.Patient == ccepDataStruct.Source.Patient && d.StimulatedChannel == ccepDataStruct.Source.Channel && d.Name == ccepDataStruct.Data));
                }
            }

            Color color = m_ColorsByROI[new Tuple<ROIStruct, DataStruct, Data.Experience.Protocol.Bloc>(ROI, data, bloc)];
            if (ROI.Channels.Count > 1)
            {
                int channelCount = ROI.Channels.Count;
                // Get the statistics for all channels in the ROI
                BlocChannelStatistics[] statistics = new BlocChannelStatistics[channelCount];
                for (int c = 0; c < channelCount; ++c)
                {
                    statistics[c] = DataManager.GetStatistics(dataInfoByPatient[ROI.Channels[c].Patient], bloc, ROI.Channels[c].Channel);
                }
                // Create all the required variables
                int length = statistics[0].Trial.ChannelSubTrialBySubBloc[subBloc].Values.Length;
                float[] values = new float[length];
                float[] standardDeviations = new float[length];
                float[][] sum = new float[length][];
                for (int i = 0; i < length; ++i)
                {
                    sum[i] = new float[channelCount];
                }
                // Fill the values array
                for (int c = 0; c < channelCount; ++c)
                {
                    float[] val = statistics[c].Trial.ChannelSubTrialBySubBloc[subBloc].Values;
                    for (int i = 0; i < length; ++i)
                    {
                        sum[i][c] = val[i];
                    }
                }
                // Compute mean and SEM of the values
                for (int i = 0; i < length; ++i)
                {
                    values[i] = sum[i].Mean();
                    standardDeviations[i] = sum[i].SEM();
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
                curveData = ShapedCurveData.CreateInstance(points, standardDeviations, color);
            }
            else if (ROI.Channels.Count == 1)
            {
                ChannelStruct channel = ROI.Channels[0];
                ChannelSubTrialStat stat = DataManager.GetStatistics(dataInfoByPatient[channel.Patient], bloc, channel.Channel).Trial.ChannelSubTrialBySubBloc[subBloc];
                float[] values = stat.Values;

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
                curveData = CurveData.CreateInstance(points, color);
            }

            Graph.Curve result = new Graph.Curve(ROI.Name, curveData, true, id, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));
            return result;
        }
        Graph.Curve GenerateChannelCurve(ChannelStruct channel, DataStruct data, Data.Experience.Protocol.Bloc bloc, SubBloc subBloc, string id)
        {
            id = id + "_" + channel.Channel;
            ChannelBloc channelBloc = m_TrialMatrixGrid.Data.First(d => d.GridData.DataStruct == data).Blocs.First(b => b.Data.Data == bloc).ChannelBlocs.First(c => c.Data.Channel == channel);

            CurveData curveData = GetCurveData(channel, data, bloc, subBloc, channelBloc.TrialIsSelected);
            Graph.Curve result = new Graph.Curve(channel.Channel, curveData, true, id, new Graph.Curve[0], new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1));

            channelBloc.OnChangeTrialSelected.AddListener(() => { result.Data = GetCurveData(channel, data, bloc, subBloc, channelBloc.TrialIsSelected); });
            return result;
        }
        CurveData GetCurveData(ChannelStruct channel, DataStruct data, Data.Experience.Protocol.Bloc bloc, SubBloc subBloc, bool[] selected)
        {
            CurveData result = null;
            PatientDataInfo dataInfo = null;
            if(data is IEEGDataStruct ieegDataStruct)
            {
                dataInfo = ieegDataStruct.Dataset.GetIEEGDataInfos().First(d => (d.Patient == channel.Patient && d.Name == ieegDataStruct.Data));
            }
            else if(data is CCEPDataStruct ccepDataStruct)
            {
                dataInfo = ccepDataStruct.Dataset.GetCCEPDataInfos().First(d => (d.Patient == channel.Patient && d.StimulatedChannel == ccepDataStruct.Source.Channel && d.Patient == ccepDataStruct.Source.Patient && d.Name == ccepDataStruct.Data));
            }
            BlocChannelData blocChannelData = DataManager.GetData(dataInfo, bloc, channel.Channel);
            Color color = ColorsByData[new Tuple<int, DataStruct, Data.Experience.Protocol.Bloc>(Array.IndexOf(m_Channels, channel), data, bloc)];

            ChannelTrial[] validTrials = blocChannelData.Trials.Where(t => t.IsValid).ToArray();
            List<ChannelTrial> trialsToUse = new List<ChannelTrial>(blocChannelData.Trials.Length);
            for (int i = 0; i < validTrials.Length; i++)
            {
                if (selected[i])
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
        void GenerateColors(ChannelStruct[] channels, DataStruct[] data)
        {
            for (int i = 0; i < channels.Length; i++)
            {
                foreach (var d in data)
                {
                    foreach (var bloc in d.Blocs)
                    {
                        Tuple<int, DataStruct, Data.Experience.Protocol.Bloc> key = new Tuple<int, DataStruct, Data.Experience.Protocol.Bloc>(i, d, bloc.Bloc);
                        if (!ColorsByData.ContainsKey(key))
                        {
                            Color color = m_Colors.FirstOrDefault(c => !ColorsByData.ContainsValue(c));
                            if (color == null) color = Color.white;
                            ColorsByData.Add(key, color);
                        }
                    }
                }
            }

            Dictionary<Tuple<ROIStruct, DataStruct, Data.Experience.Protocol.Bloc>, Color> ROIColor = new Dictionary<Tuple<ROIStruct, DataStruct, Data.Experience.Protocol.Bloc>, Color>();
            foreach (var d in data)
            {
                foreach (var bloc in d.Blocs)
                {
                    foreach (var ROI in bloc.ROIs)
                    {
                        Tuple<ROIStruct, DataStruct, Data.Experience.Protocol.Bloc> key = new Tuple<ROIStruct, DataStruct, Data.Experience.Protocol.Bloc>(ROI, d, bloc.Bloc);
                        if (!ROIColor.ContainsKey(key))
                        {
                            if (m_ColorsByROI.ContainsKey(key))
                            {
                                ROIColor.Add(key, m_ColorsByROI[key]);
                            }
                            else
                            {
                                Color color = m_Colors.FirstOrDefault(c => !ColorsByData.ContainsValue(c) && !ROIColor.ContainsValue(c));
                                if (color == null) color = Color.white;
                                ROIColor.Add(key, color);
                            }
                        }
                    }
                }
            }
            m_ColorsByROI = ROIColor;

        }
        List<float> GetValues(Graph.Curve curve)
        {
            List<float> result = new List<float>();
            if(curve.Data != null)
            {
                int lenght = curve.Data.Points.Length;
                for (int i = 0; i < lenght; i++)
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
        void UpdateCurveData(ref CurveData curveData, ChannelBloc channelBloc, BlocChannelData blocChannelData, SubBloc subBloc)
        {
            bool[] trialIsSelected = channelBloc.TrialIsSelected;
            ChannelTrial[] validTrials = blocChannelData.Trials.Where(t => t.IsValid).ToArray();
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
                curveData = ShapedCurveData.CreateInstance(points, standardDeviations, curveData.Color, 30);
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
                curveData = CurveData.CreateInstance(points, curveData.Color, 30);
            }
        }
        #endregion
    }
}