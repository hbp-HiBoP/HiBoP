using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class Graph : MonoBehaviour
    {
        #region Properties
        PlotGestion m_PlotGestion;
        InformationsGestion m_InformationsGestion;

        GraphData m_Data = new GraphData();
        public string Title
        {
            get { return m_Data.Title; }
            set
            {
                m_Data.Title = value;
                m_InformationsGestion.SetTitle(value);
            }
        }
        public string Abscissa
        {
            get { return m_Data.Abscissa; }
            set
            {
                m_Data.Abscissa = value;
                m_InformationsGestion.SetAbscissaLabel(value);
            }
        }
        public string Ordinate
        {
            get { return m_Data.Ordinate; }
            set
            {
                m_Data.Ordinate = value;
                m_InformationsGestion.SetOrdinateLabel(value);
            }
        }
        public Color FontColor
        {
            get { return m_Data.Font; }
            set
            {
                m_Data.Font = value;
                m_InformationsGestion.SetColor(value);
                m_PlotGestion.OriginAxeColor = value;
            }
        }
        public Color BackgroundColor
        {
            get { return m_Data.Background; }
            set
            {
                m_Data.Background = value;
                m_PlotGestion.GetComponent<Image>().color = value;
            }
        }
        public CurveData[] Curves
        {
            get { return m_Data.Curves; }
            private set
            {
                m_Data.Curves = value;
                m_InformationsGestion.SetLegends(value);
            }
        }

        Limits limits;
        public Limits Limits
        {
            get { return limits; }
            private set
            {
                limits = value;
                m_InformationsGestion.SetLimits(value);
            }
        }

        bool m_AutoLimits = true;
        #endregion

        #region Public Methods    
        public void Plot(GraphData graph)
        {
            m_Data = graph;
            Title = graph.Title;
            Abscissa = graph.Abscissa;
            Ordinate = graph.Ordinate;
            BackgroundColor = graph.Background;
            FontColor = graph.Font;
            if (m_AutoLimits)
            {
                Plot(graph.Curves, graph.Limits, false);
            }
            else
            {
                Plot(graph.Curves, Limits, false);
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_PlotGestion = GetComponentInChildren<PlotGestion>();
            m_InformationsGestion = GetComponentInChildren<InformationsGestion>();
            m_InformationsGestion.OnAutoLimits.RemoveAllListeners();
            m_InformationsGestion.OnAutoLimits.AddListener(() => { m_AutoLimits = true; Plot(m_Data); });
            m_PlotGestion.OnChangeLimits.RemoveAllListeners();
            m_PlotGestion.OnChangeLimits.AddListener((limits,ignore) => { if(!ignore) m_AutoLimits = false; Plot(Curves, limits, true);});
        }
        void Plot (CurveData[] curves, Limits limits, bool onlyUpdate = false)
        {
            Curves = curves;
            Limits = limits;
            m_PlotGestion.Plot(curves, Limits, onlyUpdate);
        }
        #endregion
    }
}
