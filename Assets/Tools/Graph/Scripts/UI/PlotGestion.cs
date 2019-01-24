using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Tools.Unity.Graph
{
    [RequireComponent(typeof(RectTransform))]
    public class PlotGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject CurvePrefab;
        [SerializeField]
        GameObject ShapedCurvePrefab;
        [SerializeField]
        GameObject OriginAxePrefab;

        Color axeColor;
        public Color OriginAxeColor
        {
            get { return axeColor; }
            set { axeColor = value; }
        }
        //public OnChangeLimitsEvent OnChangeLimits = new OnChangeLimitsEvent();
        OriginAxe OrdinateOriginAxe;
        OriginAxe AbscissaOriginAxe;

        Limits Limits { get; set; }
        List<Curve> curvesDisplayed = new List<Curve>();
        #endregion

        #region Public Methods
        public void Plot(CurveData[] curves, Limits limits, bool onlyUpdate = false)
        {
            Limits = limits;

            // Calculate ratio between rect and limits.
            Vector2 ratio = (transform as RectTransform).rect.GetRatio(limits);
            // Add Origin Axe if not already displayed
            if (!OrdinateOriginAxe || !AbscissaOriginAxe) AddOriginAxes();
            UpdateOriginAxes(limits, ratio);

            if(onlyUpdate)
            {
                // Update curves
                foreach (Curve curve in curvesDisplayed) UpdateCurve(curve, limits, ratio);
            }
            else
            {
                // Curves to remove
                List<Curve> curvesToRemove = new List<Curve>(from curve in curvesDisplayed where !curves.Contains(curve.Data) select curve);
                List<CurveData> curvesToAdd = new List<CurveData>(from curve in curves where !(from curveDisplayer in curvesDisplayed select curveDisplayer.Data).ToArray().Contains(curve) select curve);

                // Remove curves
                foreach (Curve curve in curvesToRemove) RemoveCurve(curve);
                // Update curves
                foreach (Curve curve in curvesDisplayed) UpdateCurve(curve, limits, ratio);
                // Add curves
                foreach (CurveData curve in curvesToAdd) AddCurve(curve, limits, ratio);
            }
        }
        public void Move(Vector2 command)
        {
            Limits limits = Limits;
            float abscissa = (Limits.AbscissaMin - Limits.AbscissaMax) * command.x;
            float ordinate = (Limits.OrdinateMin - Limits.OrdinateMax) * command.y;
            limits.AbscissaMin += abscissa;
            limits.AbscissaMax += abscissa;
            limits.OrdinateMin += ordinate;
            limits.OrdinateMax += ordinate;
            //OnChangeLimits.Invoke(limits,false);
        }
        public void Zoom()
        {
            Limits limits = Limits;
            float abscissa = (Limits.AbscissaMax - Limits.AbscissaMin) * 0.05f;
            float ordinate = (Limits.OrdinateMax - Limits.OrdinateMin) * 0.05f;
            limits.AbscissaMin += abscissa;
            limits.AbscissaMax -= abscissa;
            limits.OrdinateMin += ordinate;
            limits.OrdinateMax -= ordinate;
            //OnChangeLimits.Invoke(limits,false);
        }
        public void Dezoom()
        {
            Limits limits = Limits;
            float abscissa = (Limits.AbscissaMax - Limits.AbscissaMin) * 0.05f;
            float ordinate = (Limits.OrdinateMax - Limits.OrdinateMin) * 0.05f;
            limits.AbscissaMin -= abscissa;
            limits.AbscissaMax += abscissa;
            limits.OrdinateMin -= ordinate;
            limits.OrdinateMax += ordinate;
           // OnChangeLimits.Invoke(limits,false);
        }
        public void ChangeRectSize(Vector2 command)
        {
            //OnChangeLimits.Invoke(Limits,true);
        }
        #endregion

        #region Private Methods
        void AddCurve(CurveData curveData, Limits limits, Vector2 ratio)
        {
            Curve curve;
            if (curveData is ShapedCurveData)
            {
                curve = Instantiate(ShapedCurvePrefab, transform.Find("Curves")).GetComponent<ShapedCurve>();
                curve.SetFields();
                (curve as ShapedCurve).Plot(curveData as ShapedCurveData, limits, ratio);
            }
            else
            {
                curve = Instantiate(CurvePrefab, transform.Find("Curves")).GetComponent<Curve>();
                curve.SetFields();
                curve.Plot(curveData, limits, ratio);
            }
            curvesDisplayed.Add(curve);
        }
        void UpdateCurve(Curve curve, Limits limits, Vector2 ratio)
        {
            if (curve.Data is ShapedCurveData)
            {
                (curve as ShapedCurve).Plot(curve.Data as ShapedCurveData, limits, ratio);
            }
            else
            {
                curve.Plot(curve.Data, limits, ratio);
            }
        }
        void RemoveCurve(Curve curve)
        {
            curvesDisplayed.Remove(curve);
            Destroy(curve.gameObject);
        }
        void AddOriginAxes()
        {
            GameObject OrdinateOrigin = Instantiate(OriginAxePrefab, transform.Find("OriginAxes"));
            OrdinateOriginAxe = OrdinateOrigin.GetComponent<OriginAxe>();
            OrdinateOriginAxe.Set(OriginAxe.TypeEnum.Ordinate, OriginAxeColor);
            GameObject AbscissaOrigin = Instantiate(OriginAxePrefab, transform.Find("OriginAxes"));
            AbscissaOriginAxe = AbscissaOrigin.GetComponent<OriginAxe>();
            AbscissaOriginAxe.Set(OriginAxe.TypeEnum.Abscissa, OriginAxeColor);
        }
        void UpdateOriginAxes(Limits limits, Vector2 ratio)
        {
            OrdinateOriginAxe.UpdatePosition(limits, ratio);
            AbscissaOriginAxe.UpdatePosition(limits, ratio);
        }
        #endregion
    }
}