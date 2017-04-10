using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Tools.Unity.Graph
{
    public class Graph : MonoBehaviour
    {
        #region Properties
        PlotGestion m_PlotGestion;
        InformationsGestion m_InformationsGestion;

        string m_Title;
        public string Title
        {
            get { return m_Title; }
            set
            {
                m_Title = value;
                m_InformationsGestion.SetTitle(m_Title);
            }
        }

        string m_Abscissa;
        public string Abscissa
        {
            get { return m_Abscissa; }
            set
            {
                m_Abscissa = value;
                m_InformationsGestion.SetAbscissaLabel(value);
            }
        }

        string m_Ordinate;
        public string Ordinate
        {
            get { return m_Ordinate; }
            set
            {
                m_Ordinate = value;
                m_InformationsGestion.SetOrdinateLabel(m_Ordinate);
            }
        }

        Color m_FontColor;
        public Color FontColor
        {
            get { return m_FontColor; }
            set
            {
                m_FontColor = value;
                m_InformationsGestion.SetColor(m_FontColor);
                m_PlotGestion.OriginAxeColor = m_FontColor;
            }
        }

        Color m_BackgroundColor;
        public Color BackgroundColor
        {
            get { return m_BackgroundColor; }
            set
            {
                m_BackgroundColor = value;
                m_PlotGestion.GetComponent<Image>().color = value;
            }
        }

        Limits m_Limits;
        public Limits Limits
        {
            get { return m_Limits; }
            private set
            {
                m_Limits = value;
                m_InformationsGestion.SetLimits(value);
            }
        }

        CurveData[] m_Curves;
        public CurveData[] Curves
        {
            get { return m_Curves; }
            private set
            {
                m_Curves = value;
                m_InformationsGestion.SetLegends(m_Curves);
            }
        }

        UnityEvent m_OnSetLimitsManually = new UnityEvent();
        public UnityEvent OnSetLimitsManually
        {
            get { return m_OnSetLimitsManually; }
        }
        #endregion

        #region Public Methods
        public void Set(string title, string abcissa, string ordinate, Color backGroundColor, Color fontColor)
        {
            Title = title;
            Abscissa = abcissa;
            Ordinate = ordinate;
            BackgroundColor = backGroundColor;
            FontColor = fontColor;
        }       
        public void Plot(CurveData[] curves, Limits limits)
        {
            Plot(curves, limits,false);
        }
        public void Plot(GraphData graph)
        {
            Set(graph.Title, graph.Abscissa, graph.Ordinate, graph.Background, graph.Font);
            Plot(graph.Curves, graph.Limits);
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_PlotGestion = GetComponentInChildren<PlotGestion>();
            m_InformationsGestion = GetComponentInChildren<InformationsGestion>();
            m_PlotGestion.OnChangeLimits.RemoveAllListeners();
            m_PlotGestion.OnChangeLimits.AddListener((limits) =>
            {
                Plot(Curves, limits, true);
                m_OnSetLimitsManually.Invoke();
            });
        }
        void Plot (CurveData[] curves, Limits limits, bool onlyUpdate = false)
        {
            Curves = curves;
            Limits = limits;
            m_PlotGestion.Plot(curves, limits, onlyUpdate);
        }
        #endregion
    }
}
