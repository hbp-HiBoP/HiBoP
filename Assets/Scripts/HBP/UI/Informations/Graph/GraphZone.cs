using HBP.Data.Informations;
using HBP.UI.Informations.TrialMatrix;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.UI.Informations.Graphs;
using HBP.Data.Informations.Graphs;
using HBP.Data.Preferences;
using HBP.Core.DLL;
using HBP.Core.Tools;

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
        
        Tuple<Tuple<Core.Data.Bloc, Core.Data.SubBloc>[], Core.Tools.TimeWindow>[] m_SubBlocsAndWindowByColumn;
        Dictionary<string, bool> m_StatesByCurves = new Dictionary<string, bool>();

        [SerializeField] Queue<Graph> m_GraphPool = new Queue<Graph>();
        [SerializeField] List<Graph> m_Graphs = new List<Graph>();
        [SerializeField] int m_SelectedColumn;
        [SerializeField] Vector2 m_OrdinateDisplayRange;
        [SerializeField] Vector2[] m_AbscissaDisplayRange;
        [SerializeField] bool m_useDefaultDisplayRange = true;

        [SerializeField] Column[] m_Columns;
        [SerializeField] ChannelStruct[] m_Channels;
        Color m_DefaultColor = new Color(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1);
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
        public void Display(ChannelStruct[] channels, Column[] columns)
        {
            m_Columns = columns.ToArray();
            m_Channels = channels.ToArray();

            SetGraphs();
        }
        public void UpdateTime(Column column, Core.Data.SubBloc subBloc, float currentTime)
        {
            int index = Array.FindIndex(m_SubBlocsAndWindowByColumn, item => item.Item1.Any(t => t.Item1 == column.Data.Bloc && t.Item2 == subBloc));
            if(index != -1)
            {
                Graph graph = m_Graphs[index];
                graph.CurrentTime = currentTime;
            }
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

            Tuple<Graph.Curve[], Core.Tools.TimeWindow, bool>[] columns = GenerateDataCurve(m_Columns, m_Channels);

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

        Tuple<Graph.Curve[], Core.Tools.TimeWindow, bool>[] GenerateDataCurve(Column[] columns, ChannelStruct[] channels)
        {
            List<Tuple<Graph.Curve[], Core.Tools.TimeWindow, bool>> result = new List<Tuple<Graph.Curve[], Core.Tools.TimeWindow, bool>>();

            // Epoched Data
            // Find all visualized blocs and sort by column.
            IEnumerable<Column> epochedDataColumns = columns.Where(c => c.Data is IEEGData || c.Data is CCEPData);
            IEnumerable<Core.Data.Bloc> blocs = epochedDataColumns.Select(c => c.Data.Bloc);
            m_SubBlocsAndWindowByColumn = Core.Data.Bloc.GetSubBlocsAndWindowByColumn(blocs);

            foreach (var subBlocsAndWindow in m_SubBlocsAndWindowByColumn)
            {
                List<Graph.Curve> curves = new List<Graph.Curve>();
                foreach (var column in epochedDataColumns)
                {
                    var tuple = subBlocsAndWindow.Item1.FirstOrDefault(p => p.Item1 == column.Data.Bloc);
                    if (tuple != null)
                    {
                        Graph.Curve curve = GenerateColumnCurve(column, channels, tuple.Item2);
                        curves.Add(curve);
                    }
                }
                result.Add(new Tuple<Graph.Curve[], Core.Tools.TimeWindow, bool>(curves.ToArray(), subBlocsAndWindow.Item2, subBlocsAndWindow.Item1[0].Item2.Type == MainSecondaryEnum.Main));
            }

            // Non-epoched data
            IEnumerable<Column> nonEpochedDataColumns = columns.Where(c => c.Data is MEGData);
            foreach (var column in nonEpochedDataColumns)
            {
                Graph.Curve curve = GenerateNonEpochedColumnCurve(column, channels);
                result.Add(new Tuple<Graph.Curve[], Core.Tools.TimeWindow, bool>(new Graph.Curve[] { curve }, (column.Data as MEGData).Window, true));
            }

            return result.ToArray();
        }

        Graph.Curve GenerateColumnCurve(Column column, ChannelStruct[] channels, Core.Data.SubBloc subBloc)
        {
            string ID = column.Name + "_" + column.Data.Name + "_" + column.Data.Bloc.Name + "_" + column.Data.Dataset.Name;
            List<Graph.Curve> subcurves = new List<Graph.Curve>();

            // Add ROI Curve.
            for (int i = 0; i < column.ChannelGroups.Count; i++)
            {
                subcurves.Add(GenerateGroupsCurve(column, i, subBloc, ID));
            }

            // Generate Channels By Patient.
            Dictionary<Core.Data.Patient, List<ChannelStruct>> channelsByPatient = new Dictionary<Core.Data.Patient, List<ChannelStruct>>();
            foreach (var channel in channels)
            {
                if (!channelsByPatient.ContainsKey(channel.Patient)) channelsByPatient[channel.Patient] = new List<ChannelStruct>();
                channelsByPatient[channel.Patient].Add(channel);
            }

            // Add Patient Curves.
            foreach (var patient in channelsByPatient.Keys)
            {
                subcurves.Add(GeneratePatientCurve(column, channelsByPatient[patient].ToArray(), subBloc, ID));
            }

            Graph.Curve curve = new Graph.Curve(column.Name, null, true, ID, subcurves.ToArray(), m_DefaultColor);
            return curve;
        }
        Graph.Curve GenerateGroupsCurve(Column column, int index, Core.Data.SubBloc subBloc, string ID)
        {
            ID += "_" + column.ChannelGroups[index].Name;
            CurveData curveData = null;
            Dictionary<Core.Data.Patient, List<string>> ChannelsByPatient = new Dictionary<Core.Data.Patient, List<string>>();
            foreach (var channel in column.ChannelGroups[index].Channels)
            {
                ChannelsByPatient.AddIfAbsent(channel.Patient, new List<string>());
                ChannelsByPatient[channel.Patient].Add(channel.Channel);
            }
            Dictionary<Core.Data.Patient, Core.Data.PatientDataInfo> dataInfoByPatient = new Dictionary<Core.Data.Patient, Core.Data.PatientDataInfo>(ChannelsByPatient.Count);
            if (column.Data is IEEGData ieegDataStruct)
            {
                Core.Data.IEEGDataInfo[] ieegDataInfo = ieegDataStruct.Dataset.GetIEEGDataInfos();
                foreach (var patient in ChannelsByPatient.Keys)
                {
                    dataInfoByPatient.Add(patient, ieegDataInfo.First(d => d.Patient == patient && d.Name == ieegDataStruct.Name));
                }
            }
            else if (column.Data is CCEPData ccepDataStruct)
            {
                Core.Data.CCEPDataInfo[] ccepDataInfo = ccepDataStruct.Dataset.GetCCEPDataInfos();
                foreach (var patient in ChannelsByPatient.Keys)
                {
                    dataInfoByPatient.Add(patient, ccepDataInfo.First(d => d.Patient == patient && d.Patient == ccepDataStruct.Source.Patient && d.StimulatedChannel == ccepDataStruct.Source.Channel && d.Name == ccepDataStruct.Name));
                }
            }

            Color color = PreferencesManager.UserPreferences.Visualization.Graph.GetColor(index + 2, Array.IndexOf(m_Columns, column));
            if (column.ChannelGroups[index].Channels.Count > 1)
            {
                int channelCount = column.ChannelGroups[index].Channels.Count;
                // Get the statistics for all channels in the ROI
                Core.Data.BlocChannelStatistics[] statistics = new Core.Data.BlocChannelStatistics[channelCount];
                for (int c = 0; c < channelCount; ++c)
                {
                    statistics[c] = Core.Data.DataManager.GetStatistics(dataInfoByPatient[column.ChannelGroups[index].Channels[c].Patient], column.Data.Bloc, column.ChannelGroups[index].Channels[c].Channel);
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
            else if (column.ChannelGroups[index].Channels.Count == 1)
            {
                ChannelStruct channel = column.ChannelGroups[index].Channels[0];
                Core.Data.ChannelSubTrialStat stat = Core.Data.DataManager.GetStatistics(dataInfoByPatient[channel.Patient], column.Data.Bloc, channel.Channel).Trial.ChannelSubTrialBySubBloc[subBloc];
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

            Graph.Curve result = new Graph.Curve(column.ChannelGroups[index].Name, curveData, true, ID, new Graph.Curve[0], m_DefaultColor);
            return result;
        }
        Graph.Curve GeneratePatientCurve(Column column, ChannelStruct[] channels, Core.Data.SubBloc subBloc, string ID)
        {
            ID += "_" + channels[0].Patient.Name;
            Graph.Curve[] subcurves = channels.Select(channel => GenerateChannelCurve(column, channel, subBloc, ID)).ToArray();
            return new Graph.Curve(channels[0].Patient.Name, null, true, ID, subcurves, m_DefaultColor);
        }
        Graph.Curve GenerateChannelCurve(Column column, ChannelStruct channel, Core.Data.SubBloc subBloc, string ID)
        {
            ID += "_" + channel.Channel;
            TrialMatrix.Data data = m_TrialMatrixGrid.Data.First(d => d.GridData.DataStruct.Dataset == column.Data.Dataset && d.GridData.DataStruct.Blocs.Contains(column.Data.Bloc) && d.GridData.DataStruct.Name == column.Data.Name);
            Bloc bloc = data.Blocs.First(b => b.Data.Data == column.Data.Bloc);
            ChannelBloc channelBloc = bloc.ChannelBlocs.First(c => c.Data.Channel == channel);
            CurveData curveData = GetCurveData(column, subBloc, channel, channelBloc.TrialIsSelected);
            Graph.Curve result = new Graph.Curve(channel.Channel, curveData, true, ID, new Graph.Curve[0], m_DefaultColor);
            channelBloc.OnChangeTrialSelected.AddListener(() => { result.Data = GetCurveData(column, subBloc, channel, channelBloc.TrialIsSelected); });
            return result;
        }

        Graph.Curve GenerateNonEpochedColumnCurve(Column column, ChannelStruct[] channels)
        {
            string ID = column.Name + "_" + column.Data.Name + "_" + column.Data.Dataset.Name;
            List<Graph.Curve> subcurves = new List<Graph.Curve>();

            // Add Channels curves.
            subcurves.AddRange(channels.Select(channel => GenerateNonEpochedChannelCurve(column, channel, ID)));

            Graph.Curve curve = new Graph.Curve(column.Name, null, true, ID, subcurves.ToArray(), m_DefaultColor);
            return curve;
        }
        Graph.Curve GenerateNonEpochedChannelCurve(Column column, ChannelStruct channel, string ID)
        {
            ID += "_" + channel.Channel;
            CurveData curveData = GetNonEpochedCurveData(column, channel);
            Graph.Curve result = new Graph.Curve(channel.Channel, curveData, true, ID, new Graph.Curve[0], m_DefaultColor);
            return result;
        }

        CurveData GetCurveData(Column column, Core.Data.SubBloc subBloc, ChannelStruct channel, bool[] selected)
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
            Color color = PreferencesManager.UserPreferences.Visualization.Graph.GetColor(Array.IndexOf(m_Channels, channel), Array.IndexOf(m_Columns, column));// m_ColorsByColumn.FirstOrDefault(k => k.Key.Item1 == Array.IndexOf(m_Channels, channel) && k.Key.Item2 == column).Value;
            if (blocChannelData == null)
                return null;

            Core.Data.ChannelTrial[] validTrials = blocChannelData.Trials.Where(t => t.IsValid).ToArray();
            List<Core.Data.ChannelTrial> trialsToUse = new List<Core.Data.ChannelTrial>(blocChannelData.Trials.Length);
            for (int i = 0; i < validTrials.Length; i++)
            {
                if (selected[i]) trialsToUse.Add(validTrials[i]);

            }

            float start = blocData.Frequency.ConvertNumberOfSamplesToMilliseconds(blocData.Frequency.ConvertToCeiledNumberOfSamples(subBloc.Window.Start));
            float end = blocData.Frequency.ConvertNumberOfSamplesToMilliseconds(blocData.Frequency.ConvertToFlooredNumberOfSamples(subBloc.Window.End));

            if (trialsToUse.Count > 1)
            {
                Core.Data.ChannelSubTrial[] channelSubTrials = trialsToUse.Select(t => t.ChannelSubTrialBySubBloc[subBloc]).ToArray();

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
                Core.Data.ChannelSubTrial channelSubTrial = trialsToUse[0].ChannelSubTrialBySubBloc[subBloc];
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
            Color color = PreferencesManager.UserPreferences.Visualization.Graph.GetColor(Array.IndexOf(m_Channels, channel), Array.IndexOf(m_Columns, column));
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
        void UpdateCurveData(ref CurveData curveData, ChannelBloc channelBloc, Core.Data.BlocChannelData blocChannelData, Core.Data.SubBloc subBloc)
        {
            bool[] trialIsSelected = channelBloc.TrialIsSelected;
            Core.Data.ChannelTrial[] validTrials = blocChannelData.Trials.Where(t => t.IsValid).ToArray();
            List<Core.Data.ChannelTrial> trialsToUse = new List<Core.Data.ChannelTrial>(blocChannelData.Trials.Length);
            for (int i = 0; i < validTrials.Length; i++)
            {
                if (trialIsSelected[i])
                {
                    trialsToUse.Add(validTrials[i]);
                }
            }

            if (trialsToUse.Count > 1)
            {
                Core.Data.ChannelSubTrial[] channelSubTrials = trialsToUse.Select(t => t.ChannelSubTrialBySubBloc[subBloc]).ToArray();

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
                Core.Data.ChannelSubTrial channelSubTrial = trialsToUse[0].ChannelSubTrialBySubBloc[subBloc];
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