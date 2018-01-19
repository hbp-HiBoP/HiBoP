using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Tools.Unity.Graph
{
    public class GraphData
    {
        #region Properties
        public string Title { get; set; }
        public string Abscissa { get; set; }
        public string Ordinate { get; set; }
        public Color Background { get; set; }
        public Color Font { get; set; }
        public GroupCurveData[] GroupCurveData { get; set; }
        public Limits Limits { get; set; }
        #endregion

        #region Constructors
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
        #endregion

        #region Private Methods
        void CalculateAxisValue(float max, float min, float axisSize, out float ratio, out float step, out int numberOfTickMarkNeeded, out float startIndex)
        {
            // Calculate the range of the axe.
            float length = max - min;

            // Calculate the normalized range(1-10) of the axe.
            float normalizedLength = length;
            float coef = 1f;
            if (normalizedLength > 0)
            {
                while (normalizedLength >= 10.0f)
                {
                    coef *= 10.0f;
                    normalizedLength /= 10.0f;
                    break;
                }
                while (normalizedLength < 1.0f)
                {
                    coef /= 10.0f;
                    normalizedLength *= 10.0f;
                    break;
                }
                // Calculate the normalizedStep then the Step.
                float normalizedStep = normalizedLength / 10.0f;
                normalizedStep = (Mathf.Ceil(normalizedStep * 2.0f)) / 2.0f;
                step = normalizedStep * coef;

                // Calculate the firstScalePoint of the axe
                if (min < 0.0f)
                {
                    startIndex = Mathf.CeilToInt(min / step);
                }
                else
                {
                    startIndex = Mathf.CeilToInt(min / step);
                }

                // Calculate the number of ScalePoint in the axe
                numberOfTickMarkNeeded = 0;
                while ((numberOfTickMarkNeeded + startIndex) * step <= max)
                {
                    numberOfTickMarkNeeded += 1;
                }
                
                // Find the value of the scalesPoints
                ratio = axisSize / length;
            }
            else
            {
                ratio = 0;
                step = 0;
                numberOfTickMarkNeeded = 0;
                startIndex = 0;
            }
        }
        #endregion

        #region Public Methods
        public string ToSVG()
        {
            System.Text.StringBuilder svgBuilder = new System.Text.StringBuilder();
            Rect curveViewport = new Rect(130, 60, 1600, 900);
            Vector2 ratio = curveViewport.GetRatio(Limits);
            svgBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            svgBuilder.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" width=\"" + (curveViewport.x + curveViewport.width + 300.0f).ToString() + "\" height=\"" + (curveViewport.y + curveViewport.height + 150.0f).ToString() + "\">");
            foreach (var group in GroupCurveData)
            {
                foreach (var curve in group.Curves)
                {
                    svgBuilder.AppendLine("<g>");
                    System.Text.StringBuilder builder = new System.Text.StringBuilder();

                    // Write curve
                    foreach (var point in curve.Points)
                    {
                        Vector2 localPoint = point.GetLocalPosition(Limits.Origin, ratio);
                        localPoint = new Vector2(localPoint.x + curveViewport.x, localPoint.y - curveViewport.y);
                        builder.Append(localPoint.x + "," + (curveViewport.height - localPoint.y).ToString() + " ");
                    }
                    svgBuilder.AppendLine("<path d=\"M " + builder.ToString() + "\" style=\"" + "fill:none;stroke:" + curve.Color.ToHexString() + ";stroke-width:" + curve.Width + "\"/>");

                    // Write shape
                    if (curve is ShapedCurveData)
                    {
                        ShapedCurveData shapedCurve = curve as ShapedCurveData;
                        builder = new System.Text.StringBuilder();
                        for (int i = 0; i < shapedCurve.Points.Length; ++i)
                        {
                            Vector2 point = shapedCurve.Points[i];
                            Vector2 localPoint = point.GetLocalPosition(Limits.Origin, ratio);
                            localPoint = new Vector2(localPoint.x + curveViewport.x, localPoint.y - curveViewport.y + (shapedCurve.Shapes[i] * ratio.y) / 2);
                            builder.Append(localPoint.x + "," + (curveViewport.height - localPoint.y).ToString() + " ");
                        }
                        for (int i = shapedCurve.Points.Length - 1; i >= 0; --i)
                        {
                            Vector2 point = shapedCurve.Points[i];
                            Vector2 localPoint = point.GetLocalPosition(Limits.Origin, ratio);
                            localPoint = new Vector2(localPoint.x + curveViewport.x, localPoint.y - curveViewport.y - (shapedCurve.Shapes[i] * ratio.y) / 2);
                            builder.Append(localPoint.x + "," + (curveViewport.height - localPoint.y).ToString() + " ");
                        }
                        svgBuilder.AppendLine("<path d=\"M " + builder.ToString() + "\" style=\"" + "fill:" + curve.Color.ToHexString() + ";stroke-width:0;fill-opacity:0.5\"/>");
                    }
                    svgBuilder.AppendLine("</g>");
                }
            }

            // Write axis
            float axisRatio, axisStep, axisStartIndex;
            int numberOfTickMark;
            // Title
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<text x=\"" + (curveViewport.x + (curveViewport.width / 2)).ToString() + "\" y=\"" + (curveViewport.y / 2).ToString() + "\" text-anchor=\"middle\" dy=\".3em\" style=\"font-size:50\">" + Title + "</text>");
            svgBuilder.AppendLine("</g>");
            // Ordinate
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<path d=\"M " + curveViewport.x + "," + curveViewport.y + " " + curveViewport.x + "," + (curveViewport.y + curveViewport.height).ToString() + "\" style=\"stroke:#000000;stroke-width:5;fill:none\"/>");
            svgBuilder.AppendLine("<path d=\"M " + curveViewport.x + "," + (curveViewport.y - 25.0f).ToString() + " " + (curveViewport.x + 10.0f).ToString() + "," + curveViewport.y + " " + (curveViewport.x - 10.0f).ToString() + "," + curveViewport.y + "\" style=\"stroke:#000000;stroke-width:0;fill:#000000\"/>");
            CalculateAxisValue(Limits.OrdinateMax, Limits.OrdinateMin, curveViewport.height, out axisRatio, out axisStep, out numberOfTickMark, out axisStartIndex);
            for (int i = 0; i < numberOfTickMark; ++i)
            {
                float value = (axisStartIndex + i) * axisStep;
                float position = (curveViewport.height - ((value - Limits.OrdinateMin) * axisRatio)) + curveViewport.y;
                svgBuilder.AppendLine("<g>");
                svgBuilder.AppendLine("<path d=\"M " + (curveViewport.x - 10).ToString() + "," + position + " " + (curveViewport.x + 10).ToString() + "," + position + "\" style=\"stroke:#000000;stroke-width:5;fill:none\"/>");
                svgBuilder.AppendLine("<text x=\"" + (curveViewport.x - 40).ToString() + "\" y=\"" + position + "\" text-anchor=\"middle\" dy=\".3em\" style=\"font-size:30\">" + value + "</text>");
                svgBuilder.AppendLine("</g>");
            }
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("</g>");
            svgBuilder.AppendLine("<text x=\"" + (curveViewport.x - 100).ToString() + "\" y=\"" + (curveViewport.y + (curveViewport.height / 2)).ToString() + "\" text-anchor=\"middle\" dy=\".3em\" transform=\"rotate(-90 " + (curveViewport.x - 100).ToString() + "," + (curveViewport.y + (curveViewport.height / 2)).ToString() + ")\" style=\"font-size:30\">" + Ordinate + "</text>");
            svgBuilder.AppendLine("</g>");
            svgBuilder.AppendLine("</g>");
            // Abscissa
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<path d=\"M " + curveViewport.x + "," + (curveViewport.y + curveViewport.height).ToString() + " " + (curveViewport.x + curveViewport.width).ToString() + "," + (curveViewport.y + curveViewport.height).ToString() + "\" style=\"stroke:#000000;stroke-width:5;fill:none\"/>");
            svgBuilder.AppendLine("<path d=\"M " + (curveViewport.x + curveViewport.width + 25.0f).ToString() + "," + (curveViewport.y + curveViewport.height).ToString() + " " + (curveViewport.x + curveViewport.width).ToString() + "," + (curveViewport.y + curveViewport.height + 10.0f).ToString() + " " + (curveViewport.x + curveViewport.width).ToString() + "," + (curveViewport.y + curveViewport.height - 10.0f).ToString() + "\" style=\"stroke:#000000;stroke-width:0;fill:#000000\"/>");
            CalculateAxisValue(Limits.AbscissaMax, Limits.AbscissaMin, curveViewport.width, out axisRatio, out axisStep, out numberOfTickMark, out axisStartIndex);
            for (int i = 0; i < numberOfTickMark; ++i)
            {
                float value = (axisStartIndex + i) * axisStep;
                float position = (value - Limits.AbscissaMin) * axisRatio + curveViewport.x;
                svgBuilder.AppendLine("<g>");
                svgBuilder.AppendLine("<path d=\"M " + position + "," + (curveViewport.y + curveViewport.height - 10).ToString() + " " + position + "," + (curveViewport.y + curveViewport.height + 10).ToString() + "\" style=\"stroke:#000000;stroke-width:5;fill:none\"/>");
                svgBuilder.AppendLine("<text x=\"" + position + "\" y=\"" + (curveViewport.y + curveViewport.height + 30).ToString() + "\" text-anchor=\"middle\" dy=\".3em\" style=\"font-size:30\">" + value + "</text>");
                svgBuilder.AppendLine("</g>");
            }
            svgBuilder.AppendLine("</g>");
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<text x=\"" + (curveViewport.x + (curveViewport.width / 2)).ToString() + "\" y=\"" + (curveViewport.y + curveViewport.height + 70).ToString() + "\" text-anchor=\"middle\" dy=\".3em\" style=\"font-size:30\">" + Abscissa + "</text>");
            svgBuilder.AppendLine("</g>");
            svgBuilder.AppendLine("</g>");
            // Write Legend
            svgBuilder.AppendLine("<g>");
            int id = 0;
            float x = curveViewport.x + curveViewport.width + 30.0f;
            for (int i = 0; i < GroupCurveData.Length; ++i, ++id)
            {
                GroupCurveData group = GroupCurveData[i];
                float y = curveViewport.y + (id * 40.0f);
                svgBuilder.AppendLine("<g>");
                svgBuilder.AppendLine("<text x=\"" + x + "\" y=\"" + y + "\" text-anchor=\"left\" dy=\".3em\" style=\"font-size:30\">" + group.Name + "</text>");
                svgBuilder.AppendLine("</g>");
                for (int j = 0; j < group.Curves.Count; ++j)
                {
                    ++id;
                    CurveData curve = group.Curves[j];
                    y = curveViewport.y + (id * 40.0f);
                    svgBuilder.AppendLine("<g>");
                    svgBuilder.AppendLine("<path d=\"M " + x + "," + y + " " + (x + 30.0f).ToString() + "," + y + "\" style=\"stroke:" + curve.Color.ToHexString() + ";stroke-width:10;fill:none\"/>");
                    svgBuilder.AppendLine("<text x=\"" + (x + 40.0f).ToString() + "\" y=\"" + y + "\" text-anchor=\"left\" dy=\".3em\" style=\"font-size:30\">" + curve.Name + "</text>");
                    svgBuilder.AppendLine("</g>");
                }
            }
            svgBuilder.AppendLine("</g>");
            svgBuilder.AppendLine("</svg>");
            return svgBuilder.ToString();
        }
        public Dictionary<string,string> ToCSV()
        {
            Dictionary<string, string> csv = new Dictionary<string, string>();
            for (int g = 0; g < GroupCurveData.Length; ++g)
            {
                GroupCurveData group = GroupCurveData[g];
                for (int c = 0; c < group.Curves.Count; ++c)
                {
                    CurveData curve = group.Curves[c];
                    System.Text.StringBuilder csvBuilder = new System.Text.StringBuilder();
                    csvBuilder.AppendLine("X,Y,SEM");
                    if (curve is ShapedCurveData)
                    {
                        ShapedCurveData shapedCurve = curve as ShapedCurveData;
                        for (int i = 0; i < shapedCurve.Points.Length; ++i)
                        {
                            Vector2 point = shapedCurve.Points[i];
                            csvBuilder.AppendLine(string.Format("{0},{1},{2}", point.x, point.y, shapedCurve.Shapes[i]));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < curve.Points.Length; ++i)
                        {
                            Vector2 point = curve.Points[i];
                            csvBuilder.AppendLine(string.Format("{0},{1},{2}", point.x, point.y, 0));
                        }
                    }
                    if (csv.ContainsKey(group.Name + " " + curve.Name))
                    {
                        csv.Add(group.Name + " " + curve.Name + " (" + g + "-" + c + ")", csvBuilder.ToString());
                    }
                    else
                    {
                        csv.Add(group.Name + " " + curve.Name, csvBuilder.ToString());
                    }
                }
            }
            return csv;
        }
        #endregion
    }
}