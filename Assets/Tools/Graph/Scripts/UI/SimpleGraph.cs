using System;
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

        [SerializeField] private BoolEvent m_OnChangeUseDefaultRange;
        public BoolEvent OnChangeUseDefaultRange
        {
            get
            {
                return m_OnChangeUseDefaultRange;
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
            SetUseDefaultDisplayRange();
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
            if (m_OrdinateDisplayRange != m_DefaultOrdinateDisplayRange)
            {
                UseDefaultDisplayRange = false;
                m_OnChangeUseDefaultRange.Invoke(m_UseDefaultDisplayRange);
            }
        }
        void SetAbscissaDisplayRange()
        {
            m_OnChangeAbscissaDisplayRange.Invoke(m_AbscissaDisplayRange);
            if (m_AbscissaDisplayRange != m_DefaultAbscissaDisplayRange)
            {
                UseDefaultDisplayRange = false;
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