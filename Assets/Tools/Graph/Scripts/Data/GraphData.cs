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
        public GroupCurveData[] GroupCurveData { get; set; }
        public Limits Limits { get; set; }

        public GraphData(string title, string abscissa, string ordinate, Color background, Color font, GroupCurveData[] curves, Limits limits)
        {
            Title = title;
            Abscissa = abscissa;
            Ordinate = ordinate;
            Background = background;
            Font = font;
            GroupCurveData = curves;
            Limits = limits;
        }
        public GraphData(string title, string abscissa, string ordinate, Color background, Color font, GroupCurveData[] curves)
        {
            Title = title;
            Abscissa = abscissa;
            Ordinate = ordinate;
            Background = background;
            Font = font;
            GroupCurveData = curves;
            Limits limits = new Limits();

            float abscissaMax = float.MinValue;
            float abscissaMin = float.MaxValue;
            float ordinateMax = float.MinValue;
            float ordinateMin = float.MaxValue;
            foreach (var groupCurves in GroupCurveData)
            {
                foreach (var curve in groupCurves.Curves)
                {
                    if (curve is ShapedCurveData)
                    {
                        ShapedCurveData shapedCurve = curve as ShapedCurveData;
                        for (int p = 0; p < shapedCurve.Points.Length; p++)
                        {
                            ordinateMax = Mathf.Max(ordinateMax, shapedCurve.Points[p].y + shapedCurve.Shapes[p] / 2);
                            ordinateMin = Mathf.Min(ordinateMin, shapedCurve.Points[p].y - shapedCurve.Shapes[p] / 2);
                            abscissaMax = Mathf.Max(abscissaMax, shapedCurve.Points[p].x);
                            abscissaMin = Mathf.Min(abscissaMin, shapedCurve.Points[p].x);
                        }
                    }
                    else
                    {
                        for (int p = 0; p < curve.Points.Length; p++)
                        {
                            ordinateMax = Mathf.Max(ordinateMax, curve.Points[p].y);
                            ordinateMin = Mathf.Min(ordinateMin, curve.Points[p].y);
                            abscissaMax = Mathf.Max(abscissaMax, curve.Points[p].x);
                            abscissaMin = Mathf.Min(abscissaMin, curve.Points[p].x);
                        }
                    }
                }
            }
            limits.AbscissaMin = abscissaMin;
            limits.AbscissaMax = abscissaMax;
            limits.OrdinateMin = ordinateMin;
            limits.OrdinateMax = ordinateMax;

            if (limits.OrdinateMin == limits.OrdinateMax)
            {
                limits.OrdinateMax += 1;
                limits.OrdinateMin -= 1;
            }
            Limits = limits;
        }
        public GraphData() : this("", "", "", Color.black, Color.white, new GroupCurveData[0], new Limits()) { }
    }
}