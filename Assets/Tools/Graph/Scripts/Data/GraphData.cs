using UnityEngine;

namespace Tools.Unity.Graph
{
    public class GraphData
    {
        public string Title { get; set; }
        public string Abscissa { get; set; }
        public string Ordinate { get; set; }
        public Color Background { get; set; }
        public Color Font { get; set; }
        public CurveData[] Curves { get; set; }
        public Limits Limits { get; set; }

        public GraphData(string title, string abscissa, string ordinate, Color background, Color font, CurveData[] curves, Limits limits)
        {
            Title = title;
            Abscissa = abscissa;
            Ordinate = ordinate;
            Background = background;
            Font = font;
            Curves = curves;
            Limits = limits;
        }
    }
}