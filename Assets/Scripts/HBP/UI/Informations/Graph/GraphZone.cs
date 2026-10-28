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
using HBP.Core.Preferences;
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
        Dictionary<string, bool> m_StatesByCurves = new();

        [SerializeField] Queue<Graph> m_GraphPool = new();
        [SerializeField] List<Graph> m_Graphs = new();
        [SerializeField] int m_SelectedColumn;
        [SerializeField] Vector2 m_OrdinateDisplayRange;
        [SerializeField] Vector2[] m_AbscissaDisplayRange;
        [SerializeField] bool m_useDefaultDisplayRange = true;

        [SerializeField] Column[] m_Columns;
        [SerializeField] ChannelStruct[] m_Channels;
        private Dictionary<ChannelStruct, List<LocalizerCurveData>> m_LocalizersCurves = new();
        Color m_DefaultColor = new(220.0f / 255f, 220.0f / 255f, 220.0f / 255f, 1);
        bool m_isLock;
        bool m_Rendering;
        bool m_OwnPresentation;

        #endregion

        internal InformationPresentation Presentation { get; private set; }

        internal void BindPresentation(InformationPresentation value)
        {
            if (ReferenceEquals(Presentation, value)) return;
            if (m_OwnPresentation) Presentation?.Dispose();
            Presentation = value;
            m_OwnPresentation = false;
            m_StatesByCurves.Clear();
        }

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
                graph.OnChangeAbscissaDisplayRange.AddListener(value =>
                {
                    int index = m_Graphs.IndexOf(graph);
                    if (!m_Rendering && Presentation != null && index >= 0 && index < Presentation.AbscissaRanges.Length && Presentation.AbscissaRanges[index] != value)
                    {
                        Presentation.AbscissaRanges[index] = value;
                        Presentation.Touch();
                    }
                });

                m_GraphPool.Enqueue(graph);
            }
        }

        public void Display(ChannelStruct[] channels, Column[] columns)
        {
            if (Presentation == null)
            {
                Presentation = new InformationPresentation(null, null);
                m_OwnPresentation = true;
            }

            if (m_OwnPresentation)
            {
                if (m_TrialMatrixGrid != null && m_TrialMatrixGrid.PresentationData != null) Presentation.AttachMatrices(m_TrialMatrixGrid.PresentationData);
                Presentation.Rebuild(channels, columns, rebuildMatrices: m_TrialMatrixGrid == null || m_TrialMatrixGrid.PresentationData == null);
            }

            m_Columns = columns.ToArray();
            m_Channels = channels.ToArray();

            SetGraphs();
        }

        public void UpdateTimeVisibility(bool visible)
        {
            foreach (Graph graph in m_Graphs)
            {
                graph.DisplayCurrentTime = visible;
            }
        }

        public void UpdateTime(Column column, Core.Data.SubBloc subBloc, float currentTime)
        {
            int index = Array.FindIndex(m_SubBlocsAndWindowByColumn, item => item.Item1.Any(t => t.Item1 == column.Data.Bloc && t.Item2 == subBloc));
            if (index != -1)
            {
                Graph graph = m_Graphs[index];
                graph.CurrentTime = currentTime;
            }
        }

        public void SetLocalizersCurves(Dictionary<ChannelStruct, List<LocalizerCurveData>> curves)
        {
            m_LocalizersCurves = curves;
        }

        #endregion

        #region Private Methods

        private void OnDestroy()
        {
            if (m_OwnPresentation) Presentation?.Dispose();
        }

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
            m_Rendering = true;
            SaveSettings();
            m_SelectedColumn = Presentation.ActiveGraph;
            m_useDefaultDisplayRange = Presentation.UseDefaultRange;
            m_OrdinateDisplayRange = Presentation.OrdinateRange;
            m_AbscissaDisplayRange = Presentation.AbscissaRanges;

            ClearGraphs();

            Tuple<Graph.Curve[], Core.Tools.TimeWindow, bool>[] columns = Presentation.Graphs;
            m_SubBlocsAndWindowByColumn = Presentation.SubBlocsByColumn;
            // MEG adds non-epoched graphs beyond the trial-matrix preallocation.
            CreateGraphPool(Math.Max(0, columns.Length - m_GraphPool.Count));

            Core.Data.RunningStatistics valueStatistics = new();
            foreach (var column in columns)
            {
                foreach (var curve in column.Item1)
                    InformationPresentation.AddValues(curve, ref valueStatistics);
            }

            Vector2 defaultOrdinateDisplayRange = Core.Data.StreamingStatistics.CalculateValueLimit(valueStatistics, 5);
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

            m_ToggleContainer.gameObject.SetActive(m_Graphs.Count > 1);
            Presentation.AbscissaRanges = abscissaDisplayRange;
            Presentation.OrdinateRange = ordinateDisplayRange;
            m_Rendering = false;
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


            Queue<Graph.Curve> curveQueue = new();
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

            if (m_OwnPresentation)
            {
                Dictionary<string, bool> dico = m_StatesByCurves.ToDictionary((t) => t.Key, (t) => t.Value);
                foreach (var pair in dico) graph.SetEnabled(pair.Key, pair.Value);
            }

            // Add Toggle
            GameObject toggleGameObject = Instantiate(m_TogglesPrefab, m_ToggleContainer);
            Toggle toggle = toggleGameObject.GetComponent<Toggle>();
            int graphIndex = m_Graphs.Count;
            toggle.onValueChanged.AddListener(b =>
            {
                graph.gameObject.SetActive(b);
                if (!m_Rendering && b && Presentation.ActiveGraph != graphIndex)
                {
                    Presentation.ActiveGraph = graphIndex;
                    Presentation.Touch();
                }
            });
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
            if (!m_isLock && !m_Rendering)
            {
                m_isLock = true;
                if (Presentation.OrdinateRange != ordinateDisplayRange)
                {
                    Presentation.OrdinateRange = ordinateDisplayRange;
                    Presentation.Touch();
                }

                foreach (var graph in m_Graphs)
                {
                    graph.OrdinateDisplayRange = ordinateDisplayRange;
                }

                m_isLock = false;
            }
        }

        void OnChangeCurveIsActiveHandler(string ID, bool isActive)
        {
            if (!m_isLock && !m_Rendering)
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
            if (!m_isLock && !m_Rendering)
            {
                m_isLock = true;
                if (Presentation.UseDefaultRange != useDefaultDisplayRange)
                {
                    Presentation.UseDefaultRange = useDefaultDisplayRange;
                    Presentation.Touch();
                }

                foreach (var graph in m_Graphs)
                {
                    graph.UseDefaultDisplayRange = useDefaultDisplayRange;
                }

                m_isLock = false;
            }
        }

        #endregion
    }
}
