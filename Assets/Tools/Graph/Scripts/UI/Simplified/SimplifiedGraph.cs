using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class SimplifiedGraph : MonoBehaviour
    {
        #region Properties
        public PlotGestion PlotGestion;

        GraphData m_Data = new GraphData();

        public Text TitleText;
        public string Title
        {
            get
            {
                return m_Data.Title;
            }
            set
            {
                m_Data.Title = value;
                TitleText.text = value;
            }
        }

        public Graphic[] FontColorGraphics;
        public Color FontColor
        {
            get { return m_Data.Font; }
            set
            {
                m_Data.Font = value;
                foreach (var graphic in FontColorGraphics) graphic.color = value;
                PlotGestion.OriginAxeColor = value;
            }
        }

        public Graphic[] BackgroundColorGraphics;
        public Color BackgroundColor
        {
            get { return m_Data.Background; }
            set
            {
                m_Data.Background = value;
                foreach (var graphic in BackgroundColorGraphics) graphic.color = value;
            }
        }

        public GroupCurveData[] GroupCurves
        {
            get { return m_Data.GroupCurveData; }
            private set
            {
                m_Data.GroupCurveData = value;
            }
        }

        Limits limits;
        public Limits Limits
        {
            get { return limits; }
            private set
            {
                limits = value;
            }
        }

        bool m_AutoLimits = true;
        #endregion

        #region Public Methods    
        public void Plot(GraphData graph)
        {
            Profiler.BeginSample("Plot totezfz");
            m_Data = graph;
            Title = graph.Title;
            BackgroundColor = graph.Background;
            FontColor = graph.Font;
            if (m_AutoLimits) Plot(graph.GroupCurveData, graph.Limits, false);
            else Plot(graph.GroupCurveData, Limits, false);
            Profiler.EndSample();
        }
        public string ToSVG()
        {
            return m_Data.ToSVG();
        }
        public Dictionary<string, string> ToCSV()
        {
            return m_Data.ToCSV();
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            PlotGestion = GetComponentInChildren<PlotGestion>();
            PlotGestion.OnChangeLimits.RemoveAllListeners();
            PlotGestion.OnChangeLimits.AddListener((limits, ignore) => { if (!ignore) m_AutoLimits = false; Plot(GroupCurves, limits, true); });
        }
        void Plot(GroupCurveData[] groupCurves, Limits limits, bool onlyUpdate = false)
        {
            GroupCurves = groupCurves;
            Limits = limits;
            List<CurveData> curvesToDisplay = new List<CurveData>();
            foreach (var group in groupCurves)
            {
                foreach (var curve in group.Curves)
                {
                    curvesToDisplay.Add(curve);
                }
            }
            PlotGestion.Plot(curvesToDisplay.ToArray(), Limits, onlyUpdate);
        }
        #endregion
    }
}