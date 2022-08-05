using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Graph
{
    public class SimpleGraph : MonoBehaviour
    {
        #region Properties
        [SerializeField] private string m_Title;
        public string Title
        {
            get
            {
                return m_Title;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Title, value))
                {
                    SetTitle();
                }
            }
        }

        [SerializeField] private Color m_FontColor;
        public Color FontColor
        {
            get
            {
                return m_FontColor;
            }
            set
            {
                if (SetPropertyUtility.SetColor(ref m_FontColor, value))
                {
                    SetFontColor();
                }
            }
        }

        [SerializeField] private Color m_BackgroundColor;
        public Color BackgroundColor
        {
            get
            {
                return m_BackgroundColor;
            }
            set
            {
                if (SetPropertyUtility.SetColor(ref m_BackgroundColor, value))
                {
                    SetBackgroundColor();
                }
            }
        }

        [SerializeField] private Vector2 m_OrdinateDisplayRange;
        public Vector2 OrdinateDisplayRange
        {
            get
            {
                return m_OrdinateDisplayRange;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_OrdinateDisplayRange, value))
                {
                    SetOrdinateDisplayRange();
                }
            }
        }

        [SerializeField] private Vector2 m_AbscissaDisplayRange;
        public Vector2 AbscissaDisplayRange
        {
            get
            {
                return m_AbscissaDisplayRange;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_AbscissaDisplayRange, value))
                {
                    SetAbscissaDisplayRange();
                }
            }
        }

        [SerializeField] bool m_IsSelected = false;
        public bool IsSelected
        {
            get
            {
                return m_IsSelected;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_IsSelected, value))
                {
                    m_OnChangeSelected.Invoke(value);
                }
            }
        }

        public HBP.Data.Informations.ChannelStruct ChannelStruct { get; set; }

        [SerializeField] List<Graph.Curve> m_Curves = new List<Graph.Curve>();
        public ReadOnlyCollection<Graph.Curve> Curves
        {
            get
            {
                return new ReadOnlyCollection<Graph.Curve>(m_Curves);
            }
        }
        #endregion

        #region Events
        [SerializeField] private StringEvent m_OnChangeTitle;
        public StringEvent OnChangeTitle
        {
            get
            {
                return m_OnChangeTitle;
            }
        }

        [SerializeField] private ColorEvent m_OnChangeFontColor;
        public ColorEvent OnChangeFontColor
        {
            get
            {
                return m_OnChangeFontColor;
            }
        }

        [SerializeField] private ColorEvent m_OnChangeBackgroundColor;
        public ColorEvent OnChangeBackgroundColor
        {
            get
            {
                return m_OnChangeBackgroundColor;
            }
        }

        [SerializeField] private Vector2Event m_OnChangeOrdinateDisplayRange;
        public Vector2Event OnChangeOrdinateDisplayRange
        {
            get
            {
                return m_OnChangeOrdinateDisplayRange;
            }
        }

        [SerializeField] private Vector2Event m_OnChangeAbscissaDisplayRange;
        public Vector2Event OnChangeAbscissaDisplayRange
        {
            get
            {
                return m_OnChangeAbscissaDisplayRange;
            }
        }

        [SerializeField] private Graph.CurvesEvent m_OnChangeCurves;
        public Graph.CurvesEvent OnChangeCurves
        {
            get
            {
                return m_OnChangeCurves;
            }
        }
        
        [SerializeField] private BoolEvent m_OnChangeSelected;
        public BoolEvent OnChangeSelected
        {
            get
            {
                return m_OnChangeSelected;
            }
        }
        #endregion

        #region Public Methods
        public void SetEnabled(string id, bool enabled)
        {
            Graph.Curve curveFound = FindCurve(id);
            if (curveFound != null)
            {
                curveFound.Enabled = enabled;
            }
            SetCurves();
        }
        public void UpdateCurve(string id)
        {
            Graph.Curve curveFound = FindCurve(id);
        }
        public void AddCurve(Graph.Curve curve)
        {
            m_Curves.Add(curve);
            AddListenerOnChangeDataEvent(curve);
            SetCurves();
        }
        public void RemoveCurve(Graph.Curve curve)
        {
            m_Curves.Remove(curve);
            SetCurves();
        }
        public void SetCurves(Graph.Curve[] curves)
        {
            m_Curves = new List<Graph.Curve>(curves);
            foreach (var curve in curves)
            {
                AddListenerOnChangeDataEvent(curve);
            }
            SetCurves();
        }
        public void ClearCurves()
        {
            m_Curves = new List<Graph.Curve>();
            SetCurves();
        }
        public string ToSVG()
        {
            System.Globalization.CultureInfo oldCulture = System.Globalization.CultureInfo.CurrentCulture;
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Text.StringBuilder svgBuilder = new System.Text.StringBuilder();
            Rect curveViewport = new Rect(130, 60, 1600, 900);
            Limits limits = new Limits(m_AbscissaDisplayRange.x, m_AbscissaDisplayRange.y, m_OrdinateDisplayRange.x, m_OrdinateDisplayRange.y);
            Vector2 ratio = curveViewport.GetRatio(limits);
            svgBuilder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            svgBuilder.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" width=\"" + (curveViewport.x + curveViewport.width + 700.0f).ToString() + "\" height=\"" + (curveViewport.y + curveViewport.height + 150.0f).ToString() + "\">");
            foreach (var curve in m_Curves.Where(c => c.Data != null).Select(c => c.Data))
            {
                svgBuilder.AppendLine("<g>");
                System.Text.StringBuilder builder = new System.Text.StringBuilder();

                // Write shape
                if (curve is ShapedCurveData shapedCurve)
                {
                    builder = new System.Text.StringBuilder();
                    for (int i = 0; i < shapedCurve.Points.Length; ++i)
                    {
                        Vector2 point = shapedCurve.Points[i];
                        Vector2 localPoint = point.GetLocalPosition(limits.Origin, ratio);
                        localPoint = new Vector2(localPoint.x + curveViewport.x, localPoint.y - curveViewport.y + (shapedCurve.Shapes[i] * ratio.y) / 2);
                        builder.Append(localPoint.x + "," + (curveViewport.height - localPoint.y).ToString() + " ");
                    }
                    for (int i = shapedCurve.Points.Length - 1; i >= 0; --i)
                    {
                        Vector2 point = shapedCurve.Points[i];
                        Vector2 localPoint = point.GetLocalPosition(limits.Origin, ratio);
                        localPoint = new Vector2(localPoint.x + curveViewport.x, localPoint.y - curveViewport.y - (shapedCurve.Shapes[i] * ratio.y) / 2);
                        builder.Append(localPoint.x + "," + (curveViewport.height - localPoint.y).ToString() + " ");
                    }
                    svgBuilder.AppendLine("<path d=\"M " + builder.ToString() + "\" style=\"" + "fill:" + curve.Color.ToHexString() + ";stroke-width:0;fill-opacity:0.5\"/>");
                }

                // Write curve
                builder = new System.Text.StringBuilder();
                foreach (var point in curve.Points)
                {
                    Vector2 localPoint = point.GetLocalPosition(limits.Origin, ratio);
                    localPoint = new Vector2(localPoint.x + curveViewport.x, localPoint.y - curveViewport.y);
                    builder.Append(localPoint.x + "," + (curveViewport.height - localPoint.y).ToString() + " ");
                }
                svgBuilder.AppendLine("<path d=\"M " + builder.ToString() + "\" style=\"" + "fill:none;stroke:" + curve.Color.ToHexString() + ";stroke-width:" + curve.Thickness + "\"/>");
                svgBuilder.AppendLine("</g>");
            }

            // Write axis
            List<float> GetAxisValues(float min, float max)
            {
                List<float> values = new List<float>();
                float range = max - min;
                if (range > 0)
                {
                    float normalizedStep = range / 10;
                    float coef = 1;

                    if (normalizedStep < 1)
                    {
                        coef /= 10;
                        normalizedStep *= 10;
                    }
                    while (normalizedStep > 10)
                    {
                        float tempResult;
                        tempResult = normalizedStep / 2;
                        if (tempResult >= 1 && tempResult <= 10)
                        {
                            coef *= 2;
                            normalizedStep = tempResult;
                            break;
                        }

                        tempResult = normalizedStep / 5;
                        if (tempResult >= 1 && tempResult <= 10)
                        {
                            normalizedStep = tempResult;
                            coef *= 5;
                            break;
                        }

                        tempResult = normalizedStep / 10;
                        if (normalizedStep >= 1 && normalizedStep <= 10)
                        {
                            normalizedStep = tempResult;
                            coef *= 10;
                            break;
                        }

                        coef *= 10;
                        normalizedStep /= 10;
                    }
                    if (normalizedStep > 1 && normalizedStep <= 5)
                    {
                        normalizedStep = 5;
                    }
                    else if (normalizedStep > 5 && normalizedStep <= 10)
                    {
                        normalizedStep = 10;
                    }
                    float step = normalizedStep * coef;

                    int division = Mathf.FloorToInt(min / step);
                    float rest = min % step;
                    if (rest != 0) division++;
                    float value = division * step;
                    while (value <= max)
                    {
                        values.Add(value);
                        value += step;
                    }
                }
                return values;
            }
            // Title
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<text x=\"" + (curveViewport.x + (curveViewport.width / 2)).ToString() + "\" y=\"" + (curveViewport.y / 2).ToString() + "\" text-anchor=\"middle\" dy=\".3em\" style=\"font-size:50\">" + Title + "</text>");
            svgBuilder.AppendLine("</g>");
            // Ordinate
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<path d=\"M " + curveViewport.x + "," + curveViewport.y + " " + curveViewport.x + "," + (curveViewport.y + curveViewport.height).ToString() + "\" style=\"stroke:#000000;stroke-width:5;fill:none\"/>");
            svgBuilder.AppendLine("<path d=\"M " + curveViewport.x + "," + (curveViewport.y - 25.0f).ToString() + " " + (curveViewport.x + 10.0f).ToString() + "," + curveViewport.y + " " + (curveViewport.x - 10.0f).ToString() + "," + curveViewport.y + "\" style=\"stroke:#000000;stroke-width:0;fill:#000000\"/>");
            foreach (var value in GetAxisValues(limits.OrdinateMin, limits.OrdinateMax))
            {
                float axisRatio = curveViewport.height / (limits.OrdinateMax - limits.OrdinateMin);
                float position = curveViewport.y + (curveViewport.height - ((value - limits.OrdinateMin) * axisRatio));
                svgBuilder.AppendLine("<g>");
                svgBuilder.AppendLine("<path d=\"M " + (curveViewport.x - 10).ToString() + "," + position + " " + (curveViewport.x + 10).ToString() + "," + position + "\" style=\"stroke:#000000;stroke-width:5;fill:none\"/>");
                svgBuilder.AppendLine("<text x=\"" + (curveViewport.x - 40).ToString() + "\" y=\"" + position + "\" text-anchor=\"middle\" dy=\".3em\" style=\"font-size:30\">" + value + "</text>");
                svgBuilder.AppendLine("</g>");
            }
            svgBuilder.AppendLine("</g>");
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<text x=\"" + (curveViewport.x - 100).ToString() + "\" y=\"" + (curveViewport.y + (curveViewport.height / 2)).ToString() + "\" text-anchor=\"middle\" dy=\".3em\" transform=\"rotate(-90 " + (curveViewport.x - 100).ToString() + "," + (curveViewport.y + (curveViewport.height / 2)).ToString() + ")\" style=\"font-size:30\">" + string.Format("{0} ({1})", "Activity", "µV") + "</text>");
            svgBuilder.AppendLine("</g>");
            svgBuilder.AppendLine("</g>");
            // Abscissa
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<path d=\"M " + curveViewport.x + "," + (curveViewport.y + curveViewport.height).ToString() + " " + (curveViewport.x + curveViewport.width).ToString() + "," + (curveViewport.y + curveViewport.height).ToString() + "\" style=\"stroke:#000000;stroke-width:5;fill:none\"/>");
            svgBuilder.AppendLine("<path d=\"M " + (curveViewport.x + curveViewport.width + 25.0f).ToString() + "," + (curveViewport.y + curveViewport.height).ToString() + " " + (curveViewport.x + curveViewport.width).ToString() + "," + (curveViewport.y + curveViewport.height + 10.0f).ToString() + " " + (curveViewport.x + curveViewport.width).ToString() + "," + (curveViewport.y + curveViewport.height - 10.0f).ToString() + "\" style=\"stroke:#000000;stroke-width:0;fill:#000000\"/>");
            foreach (var value in GetAxisValues(limits.AbscissaMin, limits.AbscissaMax))
            {
                float axisRatio = curveViewport.width / (limits.AbscissaMax - limits.AbscissaMin);
                float position = curveViewport.x + (value - limits.AbscissaMin) * axisRatio;
                svgBuilder.AppendLine("<g>");
                svgBuilder.AppendLine("<path d=\"M " + position + "," + (curveViewport.y + curveViewport.height - 10).ToString() + " " + position + "," + (curveViewport.y + curveViewport.height + 10).ToString() + "\" style=\"stroke:#000000;stroke-width:5;fill:none\"/>");
                svgBuilder.AppendLine("<text x=\"" + position + "\" y=\"" + (curveViewport.y + curveViewport.height + 30).ToString() + "\" text-anchor=\"middle\" dy=\".3em\" style=\"font-size:30\">" + value + "</text>");
                svgBuilder.AppendLine("</g>");
            }
            svgBuilder.AppendLine("</g>");
            svgBuilder.AppendLine("<g>");
            svgBuilder.AppendLine("<text x=\"" + (curveViewport.x + (curveViewport.width / 2)).ToString() + "\" y=\"" + (curveViewport.y + curveViewport.height + 70).ToString() + "\" text-anchor=\"middle\" dy=\".3em\" style=\"font-size:30\">" + string.Format("{0} ({1})", "Latency", "ms") + "</text>");
            svgBuilder.AppendLine("</g>");
            svgBuilder.AppendLine("</g>");
            // Write Legend
            svgBuilder.AppendLine("<g>");
            int id = 0;
            float x = curveViewport.x + curveViewport.width + 30.0f;
            foreach (var curve in m_Curves)
            {
                if (curve.Data == null) continue;

                float y = curveViewport.y + (id * 40.0f);
                svgBuilder.AppendLine("<g>");
                svgBuilder.AppendLine("<path d=\"M " + x + "," + y + " " + (x + 30.0f).ToString() + "," + y + "\" style=\"stroke:" + curve.Color.ToHexString() + ";stroke-width:10;fill:none\"/>");
                svgBuilder.AppendLine("<text x=\"" + (x + 40.0f).ToString() + "\" y=\"" + y + "\" text-anchor=\"left\" dy=\".3em\" style=\"font-size:30\">" + curve.ID + "</text>");
                svgBuilder.AppendLine("</g>");
                id++;
            }
            svgBuilder.AppendLine("</g>");
            svgBuilder.AppendLine("</svg>");
            System.Globalization.CultureInfo.CurrentCulture = oldCulture;
            return svgBuilder.ToString();
        }
        #endregion

        #region Private Methods
        private void Start()
        {
            OnValidate();
        }
        #endregion

        #region Setters
        void OnValidate()
        {
            SetTitle();
            SetFontColor();
            SetBackgroundColor();
            SetOrdinateDisplayRange();
            SetAbscissaDisplayRange();
            SetCurves();
        }
        void SetTitle()
        {
            m_OnChangeTitle.Invoke(m_Title);
        }
        void SetFontColor()
        {
            m_OnChangeFontColor.Invoke(m_FontColor);
        }
        void SetBackgroundColor()
        {
            m_OnChangeBackgroundColor.Invoke(m_BackgroundColor);
        }
        void SetOrdinateDisplayRange()
        {
            m_OnChangeOrdinateDisplayRange.Invoke(m_OrdinateDisplayRange);
        }
        void SetAbscissaDisplayRange()
        {
            m_OnChangeAbscissaDisplayRange.Invoke(m_AbscissaDisplayRange);
        }
        void SetCurves()
        {
            m_OnChangeCurves.Invoke(m_Curves.ToArray());
        }
        List<Graph.Curve> GetAllCurves(IEnumerable<Graph.Curve> curves)
        {
            if (curves.Count() == 0) return new List<Graph.Curve>();

            List<Graph.Curve> result = new List<Graph.Curve>();
            foreach (var curve in curves)
            {
                result.Add(curve);
                result.AddRange(GetAllCurves(curve.SubCurves));
            }
            return result;
        }
        Graph.Curve FindCurve(string ID)
        {
            Graph.Curve result = null;
            foreach (var curve in m_Curves)
            {
                result = FindCurve(curve, ID);
                if (result != null) break;
            }
            return result;
        }
        Graph.Curve FindCurve(Graph.Curve curve, string ID)
        {
            Graph.Curve result = null;
            if (curve.ID == ID)
            {
                result = curve;
            }
            else
            {
                foreach (var subCurve in curve.SubCurves)
                {
                    result = FindCurve(subCurve, ID);
                    if (result != null) break;
                }
            }
            return result;
        }
        void AddListenerOnChangeDataEvent(Graph.Curve curve)
        {
            curve.OnChangeData.AddListener(SetCurves);
            foreach (var subCurve in curve.SubCurves)
            {
                AddListenerOnChangeDataEvent(subCurve);
            }
        }
        #endregion
    }
}