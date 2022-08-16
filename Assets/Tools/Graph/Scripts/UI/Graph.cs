using HBP.Core.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

namespace HBP.UI.Graphs
{
    public class Graph : MonoBehaviour
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

        [SerializeField] private string m_AbscissaUnit;
        public string AbscissaUnit
        {
            get
            {
                return m_AbscissaUnit;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_AbscissaUnit, value))
                {

                }
            }
        }

        [SerializeField] private string m_AbscissaLabel;
        public string AbscissaLabel
        {
            get
            {
                return m_AbscissaLabel;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_AbscissaLabel, value))
                {
                    SetAbscissaLabel();
                }
            }
        }

        [SerializeField] private string m_OrdinateUnit;
        public string OrdinateUnit
        {
            get
            {
                return m_OrdinateUnit;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_OrdinateUnit, value))
                {
                    SetOrdinateUnit();
                }
            }
        }

        [SerializeField] private string m_OrdinateLabel;
        public string OrdinateLabel
        {
            get
            {
                return m_OrdinateLabel;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_OrdinateLabel, value))
                {
                    OnChangeOrdinateLabel.Invoke(value);
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

        [SerializeField] private Vector2 m_DefaultOrdinateDisplayRange;
        public Vector2 DefaultOrdinateDisplayRange
        {
            get
            {
                return m_DefaultOrdinateDisplayRange;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_DefaultOrdinateDisplayRange, value))
                {
                    SetDefaultOrdinateDisplayRange();
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

        [SerializeField] private Vector2 m_DefaultAbscissaDisplayRange;
        public Vector2 DefaultAbscissaDisplayRange
        {
            get
            {
                return m_DefaultAbscissaDisplayRange;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_DefaultAbscissaDisplayRange, value))
                {
                    SetDefaultAbscissaDisplayRange();
                }
            }
        }

        [SerializeField] bool m_UseDefaultDisplayRange = true;
        public bool UseDefaultDisplayRange
        {
            get
            {
                return m_UseDefaultDisplayRange;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_UseDefaultDisplayRange, value))
                {
                    SetUseDefaultDisplayRange();
                }
            }
        }


        [SerializeField] private bool m_DisplayCurrentTime;
        public bool DisplayCurrentTime
        {
            get
            {
                return m_DisplayCurrentTime;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_DisplayCurrentTime, value))
                {
                    SetCurrentTime();
                }
            }
        }

        [SerializeField] private float m_CurrentTime;
        public float CurrentTime
        {
            get
            {
                return m_CurrentTime;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_CurrentTime, value))
                {
                    SetCurrentTime();
                }
            }
        }

        [SerializeField] List<Curve> m_Curves = new List<Curve>();
        public ReadOnlyCollection<Curve> Curves
        {
            get
            {
                return new ReadOnlyCollection<Curve>(m_Curves);
            }
        }

        #region Events
        [SerializeField] private StringEvent m_OnChangeTitle;
        public StringEvent OnChangeTitle
        {
            get
            {
                return m_OnChangeTitle;
            }
        }

        [SerializeField] private StringEvent m_OnChangeAbscissaUnit;
        public StringEvent OnChangeAbscissaUnit
        {
            get
            {
                return m_OnChangeAbscissaUnit;
            }
        }

        [SerializeField] private StringEvent m_OnChangeAbscissaLabel;
        public StringEvent OnChangeAbscissaLabel
        {
            get
            {
                return m_OnChangeAbscissaLabel;
            }
        }

        [SerializeField] private StringEvent m_OnChangeOrdinateUnit;
        public StringEvent OnChangeOrdinateUnit
        {
            get
            {
                return m_OnChangeOrdinateUnit;
            }
        }

        [SerializeField] private StringEvent m_OnChangeOrdinateLabel;
        public StringEvent OnChangeOrdinateLabel
        {
            get
            {
                return m_OnChangeOrdinateLabel;
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

        [SerializeField] private BoolEvent m_OnChangeUseDefaultRange;
        public BoolEvent OnChangeUseDefaultRange
        {
            get
            {
                return m_OnChangeUseDefaultRange;
            }
        }

        [SerializeField] private BoolEvent m_OnChangeDisplayCurrentTime;
        public BoolEvent OnChangeDisplayCurrentTime
        {
            get
            {
                return m_OnChangeDisplayCurrentTime;
            }
        }

        [SerializeField]  private FloatEvent m_OnChangeCurrentTime;
        public FloatEvent OnChangeCurrentTime
        {
            get
            {
                return m_OnChangeCurrentTime;
            }
        }

        [SerializeField] private CurvesEvent m_OnChangeCurves;
        public CurvesEvent OnChangeCurves
        {
            get
            {
                return m_OnChangeCurves;
            }
        }
        #endregion
        #endregion

        #region Public Methods
        public void SetEnabled(string id, bool enabled)
        {
            Curve curveFound = FindCurve(id);
            if (curveFound != null)
            {
                curveFound.Enabled = enabled;
            }
            SetCurves();
        }
        public void UpdateCurve(string id)
        {
            Curve curveFound = FindCurve(id);
            Debug.Log(curveFound.Data.Thickness);
        }
        public void AddCurve(Curve curve)
        {
            m_Curves.Add(curve);
            AddListenerOnChangeDataEvent(curve);
            SetCurves();
        }
        public void RemoveCurve(Curve curve)
        {
            m_Curves.Remove(curve);
            SetCurves();
        }
        public void SetCurves(Curve[] curves)
        {
            m_Curves = new List<Curve>(curves);
            foreach (var curve in curves)
            {
                AddListenerOnChangeDataEvent(curve);
            }
            SetCurves();
        }
        public void ClearCurves()
        {
            m_Curves = new List<Curve>();
            SetCurves();
        }
        public string[] GetEnabledCurvesName()
        {
            return GetEnabledCurves(m_Curves).Where(c => c.Data != null || c.SubCurves.Any(sc => sc.Data != null && sc.Enabled)).Select(c => c.Name).Distinct().ToArray();
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
            List<Curve> curves = GetEnabledCurves(m_Curves);
            foreach (var curve in curves.Where(c => c.Data != null).Select(c => c.Data))
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
            svgBuilder.AppendLine("<text x=\"" + (curveViewport.x - 100).ToString() + "\" y=\"" + (curveViewport.y + (curveViewport.height / 2)).ToString() + "\" text-anchor=\"middle\" dy=\".3em\" transform=\"rotate(-90 " + (curveViewport.x - 100).ToString() + "," + (curveViewport.y + (curveViewport.height / 2)).ToString() + ")\" style=\"font-size:30\">" + string.Format("{0} ({1})", m_OrdinateLabel, m_OrdinateUnit) + "</text>");
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
            svgBuilder.AppendLine("<text x=\"" + (curveViewport.x + (curveViewport.width / 2)).ToString() + "\" y=\"" + (curveViewport.y + curveViewport.height + 70).ToString() + "\" text-anchor=\"middle\" dy=\".3em\" style=\"font-size:30\">" + string.Format("{0} ({1})", m_AbscissaLabel, m_AbscissaUnit) + "</text>");
            svgBuilder.AppendLine("</g>");
            svgBuilder.AppendLine("</g>");
            // Write Legend
            svgBuilder.AppendLine("<g>");
            int id = 0;
            float x = curveViewport.x + curveViewport.width + 30.0f;
            foreach (var curve in curves)
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
        public Dictionary<string, string> ToCSV()
        {
            Dictionary<string, string> csv = new Dictionary<string, string>();
            foreach (var curve in GetEnabledCurves(m_Curves))
            {
                if (curve.Data == null) continue;

                CurveData curveData = curve.Data;
                System.Text.StringBuilder csvBuilder = new System.Text.StringBuilder();
                csvBuilder.AppendLine("X\tY\tSEM");
                if (curveData is ShapedCurveData shapedCurveData)
                {
                    for (int i = 0; i < shapedCurveData.Points.Length; ++i)
                    {
                        Vector2 point = shapedCurveData.Points[i];
                        csvBuilder.AppendLine(string.Format("{0}\t{1}\t{2}", point.x, point.y, shapedCurveData.Shapes[i]));
                    }
                }
                else
                {
                    for (int i = 0; i < curveData.Points.Length; ++i)
                    {
                        Vector2 point = curveData.Points[i];
                        csvBuilder.AppendLine(string.Format("{0}\t{1}\t{2}", point.x, point.y, 0));
                    }
                }
                csv.Add(curve.ID, csvBuilder.ToString());
            }
            return csv;
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
            SetAbscissaUnit();
            SetAbscissaLabel();
            SetOrdinateUnit();
            SetOrdinateLabel();
            SetFontColor();
            SetBackgroundColor();
            SetUseDefaultDisplayRange();
            SetOrdinateDisplayRange();
            SetAbscissaDisplayRange();
            SetCurves();
        }
        void SetTitle()
        {
            m_OnChangeTitle.Invoke(m_Title);
        }
        void SetAbscissaUnit()
        {
            m_OnChangeAbscissaUnit.Invoke(m_AbscissaUnit);
        }
        void SetAbscissaLabel()
        {
            m_OnChangeAbscissaLabel.Invoke(m_AbscissaLabel);
        }
        void SetOrdinateUnit()
        {
            m_OnChangeOrdinateUnit.Invoke(m_OrdinateUnit);
        }
        void SetOrdinateLabel()
        {
            m_OnChangeOrdinateLabel.Invoke(m_OrdinateLabel);
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
            if (m_OrdinateDisplayRange != m_DefaultOrdinateDisplayRange)
            {
                UseDefaultDisplayRange = false;
                m_OnChangeUseDefaultRange.Invoke(m_UseDefaultDisplayRange);
            }
            //else if (m_OrdinateDisplayRange == m_DefaultOrdinateDisplayRange && m_OrdinateDisplayRange == m_DefaultOrdinateDisplayRange)
            //{
            //    UseDefaultDisplayRange = true;
            //    m_OnChangeUseDefaultRange.Invoke(m_UseDefaultDisplayRange);
            //}
        }
        void SetAbscissaDisplayRange()
        {
            m_OnChangeAbscissaDisplayRange.Invoke(m_AbscissaDisplayRange);
            if (m_AbscissaDisplayRange != m_DefaultAbscissaDisplayRange)
            {
                UseDefaultDisplayRange = false;
                m_OnChangeUseDefaultRange.Invoke(m_UseDefaultDisplayRange);
            }
            //else if (m_AbscissaDisplayRange == m_DefaultAbscissaDisplayRange && m_OrdinateDisplayRange == m_DefaultOrdinateDisplayRange)
            //{
            //    UseDefaultDisplayRange = true;
            //    m_OnChangeUseDefaultRange.Invoke(m_UseDefaultDisplayRange);
            //}
        }
        void SetDefaultAbscissaDisplayRange()
        {
            if (m_UseDefaultDisplayRange)
            {
                AbscissaDisplayRange = m_DefaultAbscissaDisplayRange;
            }
        }
        void SetDefaultOrdinateDisplayRange()
        {
            if (m_UseDefaultDisplayRange)
            {
                OrdinateDisplayRange = m_DefaultOrdinateDisplayRange;
            }
        }
        void SetUseDefaultDisplayRange()
        {
            if (m_UseDefaultDisplayRange)
            {
                AbscissaDisplayRange = DefaultAbscissaDisplayRange;
                OrdinateDisplayRange = DefaultOrdinateDisplayRange;
            }
            m_OnChangeUseDefaultRange.Invoke(m_UseDefaultDisplayRange);
        }
        void SetCurrentTime()
        {
            OnChangeCurrentTime.Invoke(m_CurrentTime);
        }

        void SetDisplayCurrentTime()
        {
            OnChangeDisplayCurrentTime.Invoke(m_DisplayCurrentTime);
        }
        void SetCurves()
        {
            m_OnChangeCurves.Invoke(m_Curves.ToArray());
        }
        List<Curve> GetEnabledCurves(IEnumerable<Curve> curves)
        {
            if (curves.Count() == 0) return new List<Curve>();

            List<Curve> result = new List<Curve>();
            foreach (var curve in curves)
            {
                if (curve.Enabled)
                {
                    result.Add(curve);
                    result.AddRange(GetEnabledCurves(curve.SubCurves));
                }
            }
            return result;
        }
        Curve FindCurve(string ID)
        {
            Curve result = null;
            foreach (var curve in m_Curves)
            {
                result = FindCurve(curve, ID);
                if (result != null) break;
            }
            return result;
        }
        Curve FindCurve(Curve curve, string ID)
        {
            Curve result = null;
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
        void AddListenerOnChangeDataEvent(Curve curve)
        {
            curve.OnChangeData.AddListener(SetCurves);
            foreach (var subCurve in curve.SubCurves)
            {
                AddListenerOnChangeDataEvent(subCurve);
            }
        }
        #endregion

        #region Public Class
        [Serializable]
        public class Curve
        {
            #region Properties
            [SerializeField] bool m_Enabled;
            public bool Enabled
            {
                get
                {
                    return m_Enabled;
                }
                set
                {
                    if (SetPropertyUtility.SetStruct(ref m_Enabled, value))
                    {
                        SetEnabled();
                    }
                }
            }

            [SerializeField] CurveData m_Data;
            public CurveData Data
            {
                get
                {
                    return m_Data;
                }
                set
                {
                    if (SetPropertyUtility.SetClass(ref m_Data, value))
                    {
                        SetData();
                    }
                }
            }
            [SerializeField] UnityEvent m_OnChangeData = new UnityEvent();
            public UnityEvent OnChangeData
            {
                get
                {
                    return m_OnChangeData;
                }
            }

            [SerializeField] Color m_DefaultColor;
            public Color DefaultColor
            {
                get
                {
                    return m_DefaultColor;
                }
            }

            public Color Color
            {
                get
                {
                    if (m_Data != null)
                    {
                        return m_Data.Color;
                    }
                    else
                    {
                        return m_DefaultColor;
                    }
                }
            }

            [SerializeField] string m_Name;
            public string Name
            {
                get
                {
                    return m_Name;
                }
                set
                {
                    SetPropertyUtility.SetClass(ref m_Name, value);
                }
            }

            [SerializeField] string m_ID;
            public string ID
            {
                get
                {
                    return m_ID;
                }
                set
                {
                    SetPropertyUtility.SetClass(ref m_ID, value);
                }
            }

            [SerializeField] List<Curve> m_SubCurves;
            public ReadOnlyCollection<Curve> SubCurves
            {
                get
                {
                    return new ReadOnlyCollection<Curve>(m_SubCurves);
                }
            }

            [SerializeField] BoolEvent m_OnChangeIsActive = new BoolEvent();
            public BoolEvent OnChangeIsActive
            {
                get
                {
                    return m_OnChangeIsActive;
                }
            }
            #endregion

            #region Constructor
            public Curve(string name, CurveData data, bool isActive, string id, Curve[] subCurves, Color defaultColor)
            {
                Name = name;
                Data = data;
                Enabled = isActive;
                ID = id;
                m_SubCurves = subCurves.ToList();
                m_DefaultColor = defaultColor;
            }
            #endregion

            #region Public Methods
            public void AddSubCurve(Curve curve)
            {
                m_SubCurves.Add(curve);
            }
            public void RemoveSubCurve(Curve curve)
            {
                m_SubCurves.Remove(curve);
            }
            #endregion

            #region Private Methods
            void SetEnabled()
            {
                m_OnChangeIsActive.Invoke(m_Enabled);
            }
            void SetData()
            {
                m_OnChangeData.Invoke();
            }
            #endregion
        }
        [Serializable] public class CurveEvent : UnityEvent<Curve> { }
        [Serializable] public class CurvesEvent : UnityEvent<Curve[]> { }
        #endregion
    }
}