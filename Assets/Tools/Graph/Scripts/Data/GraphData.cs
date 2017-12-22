using UnityEngine;
using System.Linq;
using System.Collections.Generic;

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
        public GraphData(string title, string abscissa, string ordinate, Color background, Color font, CurveData[] curves)
        {
            Title = title;
            Abscissa = abscissa;
            Ordinate = ordinate;
            Background = background;
            Font = font;
            Curves = curves;
            Limits limits = new Limits();
            limits.AbscissaMin = (from curve in Curves select curve.Points.Min((p) => p.x)).Min();
            limits.AbscissaMax = (from curve in Curves select curve.Points.Max((p) => p.x)).Max();
            List<float> max = new List<float>();
            List<float> min = new List<float>();
            foreach (var curve in Curves)
            {
                if(curve is ShapedCurveData)
                {
                    ShapedCurveData shapedCurve = curve as ShapedCurveData;
                    List<float> yMax = new List<float>();
                    List<float> yMin = new List<float>();
                    for (int p = 0; p < shapedCurve.Points.Length; p++)
                    {
                        yMax.Add(shapedCurve.Points[p].y + shapedCurve.Shapes[p] /2);
                        yMin.Add(shapedCurve.Points[p].y - shapedCurve.Shapes[p] /2);
                    }
                    max.Add(yMax.Max());
                    min.Add(yMin.Min());
                }
                else
                {
                    max.Add(curve.Points.Max((p) => p.y));
                    min.Add(curve.Points.Min((p) => p.y));
                }
            }
            limits.OrdinateMin = min.Min();
            limits.OrdinateMax = max.Max();
            if(limits.OrdinateMin == limits.OrdinateMax)
            {
                limits.OrdinateMax += 1;
                limits.OrdinateMin -= 1;
            }
            Limits = limits;
        }
        public GraphData() : this("", "", "", Color.black, Color.white, new CurveData[0], new Limits()) { }
    }
}