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
        public CurveData[] Curves { get; set; }
        public Limits Limits { get; set; }
        #endregion

        #region Constructors
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
            svgBuilder.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" width=\"1730\" height=\"1100\">");
            foreach (var curve in Curves)
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
            // Write axis
            float axisRatio, axisStep, axisStartIndex;
            int numberOfTickMark;
            // Title
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<text x=\"" + (curveViewport.x + (curveViewport.width / 2)).ToString() + "\" y=\"" + (curveViewport.y / 2).ToString() + "\" text-anchor=\"middle\" dy=\".3em\" style=\"font-size:50\">" + Title + "</text>");
            svgBuilder.AppendLine("</g>");
            // Ordinate
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<path d=\"M " + curveViewport.x + "," + curveViewport.y + " " + curveViewport.x + "," + (curveViewport.y + curveViewport.height).ToString() + "\" style=\"stroke:#000000;stroke-width:5;fill:none\"/>");
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
            svgBuilder.AppendLine("<text x=\"" + (curveViewport.x - 100).ToString() + "\" y=\"" + (curveViewport.y + (curveViewport.height / 2)).ToString() + "\" text-anchor=\"middle\" dy=\".3em\" transform=\"rotate(-90 " + (curveViewport.x - 100).ToString() + "," + (curveViewport.y + (curveViewport.height / 2)).ToString() + ")\" style=\"font-size:30\">" + Ordinate + "</text>");
            svgBuilder.AppendLine("</g>");
            // Abscissa
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<path d=\"M " + curveViewport.x + "," + (curveViewport.y + curveViewport.height).ToString() + " " + (curveViewport.x + curveViewport.width).ToString() + "," + (curveViewport.y + curveViewport.height).ToString() + "\" style=\"stroke:#000000;stroke-width:5;fill:none\"/>");
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
            svgBuilder.AppendLine("<text x=\"" + (curveViewport.x + (curveViewport.width / 2)).ToString() + "\" y=\"" + (curveViewport.y + curveViewport.height + 70).ToString() + "\" text-anchor=\"middle\" dy=\".3em\" style=\"font-size:30\">" + Abscissa + "</text>");
            svgBuilder.AppendLine("</g>");
            svgBuilder.AppendLine("</svg>");
            return svgBuilder.ToString();
        }
        #endregion
    }
}