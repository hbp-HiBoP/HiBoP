using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI.Extensions;
using UnityEngine.Events;
using System.Collections.ObjectModel;

namespace Tools.Unity.Graph
{
    [RequireComponent(typeof(RectTransform))]
    public class PlotGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] Vector2 m_AbscissaDisplayRange;
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

        [SerializeField] Vector2 m_OrdinateDisplayRange;
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

        [SerializeField] List<CurveData> m_Curves = new List<CurveData>();
        public ReadOnlyCollection<CurveData> Curves
        {
            get
            {
                return new ReadOnlyCollection<CurveData>(m_Curves);
            }
        }

        [SerializeField] List<Curve> m_DisplayedCurves = new List<Curve>();
        [SerializeField] GameObject m_CurvePrefab;
        [SerializeField] GameObject m_ShapedCurvePrefab;

        //#region Events
        //[SerializeField] private Vector2Event m_OnChangeOrdinateDisplayRange;
        //public Vector2Event OnChangeOrdinateDisplayRange
        //{
        //    get
        //    {
        //        return m_OnChangeOrdinateDisplayRange;
        //    }
        //}

        //[SerializeField] private Vector2Event m_OnChangeAbscissaDisplayRange;
        //public Vector2Event OnChangeAbscissaDisplayRange
        //{
        //    get
        //    {
        //        return m_OnChangeAbscissaDisplayRange;
        //    }
        //}
        //#endregion
        #endregion

        #region Public Methods
        public void UpdateCurve(Curve curve)
        {
            curve.Data = curve.Data;
        }
        public void AddCurve(CurveData curveData)
        {
            Curve curve;
            if (curveData is ShapedCurveData)
            {
                ShapedCurve shapedCurve = Instantiate(m_ShapedCurvePrefab, transform.Find("Curves")).GetComponent<ShapedCurve>();
                curve = shapedCurve;
                shapedCurve.OrdinateDisplayRange = m_OrdinateDisplayRange;
                shapedCurve.AbscissaDisplayRange = m_AbscissaDisplayRange;
                shapedCurve.Data = curveData as ShapedCurveData;
            }
            else
            {
                curve = Instantiate(m_CurvePrefab, transform.Find("Curves")).GetComponent<Curve>();
                curve.OrdinateDisplayRange = m_OrdinateDisplayRange;
                curve.AbscissaDisplayRange = m_AbscissaDisplayRange;
                curve.Data = curveData;
            }
            m_DisplayedCurves.Add(curve);
            m_Curves.Add(curveData);
        }
        public void RemoveCurve(Curve curve)
        {
            m_Curves.Remove(curve.Data);
            m_DisplayedCurves.Remove(curve);
            Destroy(curve.gameObject);
        }

        public void Move(Vector2 command)
        {
            float abscissa = (m_AbscissaDisplayRange.x - m_AbscissaDisplayRange.y) * command.x;
            float ordinate = (m_OrdinateDisplayRange.x - m_OrdinateDisplayRange.y) * command.y;
            m_AbscissaDisplayRange += abscissa * Vector2.one;
            m_OrdinateDisplayRange += ordinate * Vector2.one;
            //OnChangeLimits.Invoke(limits,false);
        }
        public void Zoom()
        {
            float abscissa = (m_AbscissaDisplayRange.y - m_AbscissaDisplayRange.x) * 0.05f;
            float ordinate = (m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x) * 0.05f;
            m_AbscissaDisplayRange += abscissa * new Vector2(-1, 1);
            m_OrdinateDisplayRange += ordinate * new Vector2(-1, 1);
            //OnChangeLimits.Invoke(limits,false);
        }
        public void Dezoom()
        {
            float abscissa = (m_AbscissaDisplayRange.y - m_AbscissaDisplayRange.x) * 0.05f;
            float ordinate = (m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x) * 0.05f;
            m_AbscissaDisplayRange += abscissa * new Vector2(-1, 1);
            m_OrdinateDisplayRange += ordinate * new Vector2(-1, 1);

            // OnChangeLimits.Invoke(limits,false);
        }
        public void ChangeRectSize(Vector2 command)
        {
            //OnChangeLimits.Invoke(Limits,true);
        }
        #endregion

        #region Setters
        void OnValidate()
        {
            SetCurves();
        }
        void SetAbscissaDisplayRange()
        {
            foreach (var curve in m_DisplayedCurves)
            {
                curve.AbscissaDisplayRange = m_AbscissaDisplayRange;
            }
        }
        void SetOrdinateDisplayRange()
        {
            foreach (var curve in m_DisplayedCurves)
            {
                curve.OrdinateDisplayRange = m_OrdinateDisplayRange;
            }
        }
        void SetCurves()
        {
            IEnumerable<Curve> curvesToRemove = m_DisplayedCurves.Where(curve => !m_Curves.Any(c => c == curve.Data));
            foreach (var curve in curvesToRemove) RemoveCurve(curve);

            IEnumerable<CurveData> curvesToAdd = m_Curves.Where(curve => !m_DisplayedCurves.Any(c => c.Data == curve));
            foreach (var curve in curvesToAdd) AddCurve(curve);
        }
        #endregion
    }
}