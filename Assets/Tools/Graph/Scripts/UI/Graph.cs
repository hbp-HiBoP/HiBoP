using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Graph
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
        public void AddCurve(Curve data)
        {
            m_Curves.Add(data);
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
            SetCurves();
        }
        public void ClearCurves()
        {
            m_Curves = new List<Curve>();
            SetCurves();
        }
        public string ToSVG()
        {
            return "";
            //return m_Data.ToSVG();
        }
        public Dictionary<string, string> ToCSV()
        {
            return new Dictionary<string, string>();
            //return m_Data.ToCSV();
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
            if (m_OrdinateDisplayRange != m_DefaultOrdinateDisplayRange && m_UseDefaultDisplayRange)
            {
                m_UseDefaultDisplayRange = false;
                m_OnChangeUseDefaultRange.Invoke(m_UseDefaultDisplayRange);
            }
            else if (m_OrdinateDisplayRange == m_DefaultOrdinateDisplayRange && m_OrdinateDisplayRange == m_DefaultOrdinateDisplayRange && !m_UseDefaultDisplayRange)
            {
                m_UseDefaultDisplayRange = true;
                m_OnChangeUseDefaultRange.Invoke(m_UseDefaultDisplayRange);

            }
        }
        void SetAbscissaDisplayRange()
        {
            m_OnChangeAbscissaDisplayRange.Invoke(m_AbscissaDisplayRange);
            if (m_AbscissaDisplayRange != m_DefaultAbscissaDisplayRange && m_UseDefaultDisplayRange)
            {
                m_UseDefaultDisplayRange = false;
                m_OnChangeUseDefaultRange.Invoke(m_UseDefaultDisplayRange);
            }
            else if (m_AbscissaDisplayRange == m_DefaultAbscissaDisplayRange && m_OrdinateDisplayRange == m_DefaultOrdinateDisplayRange && !m_UseDefaultDisplayRange)
            {
                m_UseDefaultDisplayRange = true;
                m_OnChangeUseDefaultRange.Invoke(m_UseDefaultDisplayRange);
            }
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
        void SetCurves()
        {
            m_OnChangeCurves.Invoke(m_Curves.ToArray());
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

            }
            #endregion
        }
        [Serializable] public class CurveEvent : UnityEvent<Curve> { }
        [Serializable] public class CurvesEvent : UnityEvent<Curve[]> { }
        #endregion
    }
}