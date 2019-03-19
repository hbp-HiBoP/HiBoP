using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI.Extensions;
using System.Collections.ObjectModel;

namespace Tools.Unity.Graph
{
    [RequireComponent(typeof(RectTransform))]
    public class CurvesDisplayer : MonoBehaviour
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

        [SerializeField] List<CurveData> m_Curves;
        public ReadOnlyCollection<CurveData> Curves
        {
            get
            {
                return new ReadOnlyCollection<CurveData>(m_Curves);
            }
        }

        [SerializeField] List<Curve> m_DisplayedCurves;
        [SerializeField] GameObject m_CurvePrefab;
        [SerializeField] GameObject m_ShapedCurvePrefab;
        #endregion

        #region Public Methods
        public void SetCurves(CurveData[] curveDatas)
        {
            m_Curves = new List<CurveData>(curveDatas);
            SetCurves(false);
        }
        public void AddCurve(CurveData curveData)
        {
            SetCurves(false);
        }
        public void RemoveCurve(CurveData curveData)
        {
            SetCurves(false);
        }
        #endregion

        #region Setters
        void OnValidate()
        {
            SetCurves(true);
        }
        void AddCurve(CurveData curveData, bool onValidate = false)
        {
            Curve curve;
            if (curveData is ShapedCurveData)
            {
                ShapedCurve shapedCurve = Instantiate(m_ShapedCurvePrefab, transform).GetComponent<ShapedCurve>();
                curve = shapedCurve;
                shapedCurve.OrdinateDisplayRange = m_OrdinateDisplayRange;
                shapedCurve.AbscissaDisplayRange = m_AbscissaDisplayRange;
                shapedCurve.Data = curveData as ShapedCurveData;
            }
            else
            {
                curve = Instantiate(m_CurvePrefab, transform).GetComponent<Curve>();
                curve.OrdinateDisplayRange = m_OrdinateDisplayRange;
                curve.AbscissaDisplayRange = m_AbscissaDisplayRange;
                curve.Data = curveData;
            }
            m_DisplayedCurves.Add(curve);
        }
        void RemoveCurve(CurveData curveData, bool onValidate = false)
        {
            if (!onValidate) m_Curves.Remove(curveData);
            List<Curve> curvesToRemove = m_DisplayedCurves.FindAll(c => c.Data == curveData);
            foreach (var curveToRemove in curvesToRemove)
            {
                m_DisplayedCurves.Remove(curveToRemove);
                if (Application.isPlaying)
                {
                    Destroy(curveToRemove.gameObject);
                }
                else
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        DestroyImmediate(curveToRemove.gameObject);
                    };
#endif
                }
            }
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
        void SetCurves(bool onValidate)
        {
            CurveData[] curvesToRemove = m_DisplayedCurves.Where(curve => !m_Curves.Any(c => c == curve.Data)).Select(c => c.Data).ToArray();
            foreach (var curve in curvesToRemove) RemoveCurve(curve, onValidate);

            CurveData[] curvesToAdd = m_Curves.Where(curve => !m_DisplayedCurves.Any(c => c.Data == curve)).ToArray();
            foreach (var curve in curvesToAdd) AddCurve(curve, onValidate);
        }
        #endregion
    }
}