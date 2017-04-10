using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class Graph : MonoBehaviour
    {
        #region Properties
        PlotGestion plotGestion;
        InformationsGestion informationsGestion;

        string title;
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                informationsGestion.SetTitle(title);
            }
        }

        string abscissa;
        public string Abscissa
        {
            get { return abscissa; }
            set
            {
                abscissa = value;
                informationsGestion.SetAbscissaLabel(value);
            }
        }

        string ordinate;
        public string Ordinate
        {
            get { return ordinate; }
            set
            {
                ordinate = value;
                informationsGestion.SetOrdinateLabel(ordinate);
            }
        }

        Color fontColor;
        public Color FontColor
        {
            get { return fontColor; }
            set
            {
                fontColor = value;
                informationsGestion.SetColor(fontColor);
                plotGestion.OriginAxeColor = fontColor;
            }
        }

        Color backgroundColor;
        public Color BackgroundColor
        {
            get { return backgroundColor; }
            set
            {
                backgroundColor = value;
                plotGestion.GetComponent<Image>().color = value;
            }
        }

        Limits limits;
        public Limits Limits
        {
            get { return limits; }
            private set
            {
                limits = value;
                informationsGestion.SetLimits(value);
            }
        }

        CurveData[] curves;
        public CurveData[] Curves
        {
            get { return curves; }
            private set
            {
                curves = value;
                informationsGestion.SetLegends(curves);
            }
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
            plotGestion = GetComponentInChildren<PlotGestion>();
            informationsGestion = GetComponentInChildren<InformationsGestion>();
            plotGestion.OnChangeLimits.RemoveAllListeners();
            plotGestion.OnChangeLimits.AddListener((limits) => Plot(Curves, limits, true));
        }
        void Plot (CurveData[] curves, Limits limits, bool onlyUpdate = false)
        {
            Curves = curves;
            Limits = limits;
            plotGestion.Plot(curves, limits, onlyUpdate);
        }
        #endregion
    }
}
