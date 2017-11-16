using UnityEngine;
using UnityEngine.UI.Extensions;
using System.Linq;

namespace Tools.Unity.Graph
{
    public class Curve : MonoBehaviour
    {
        #region Properties
        public CurveData Data { get; protected set; }
        protected Limits Limits { get; set; }
        protected Vector2 Ratio { get; set; }
        protected RectTransform curveRect;
        protected UILineRenderer lineRenderer;
        protected Vector2[] positions;
        #endregion

        #region Public Methods
        public virtual void Plot(CurveData data, Limits limits, Vector2 ratio)
        {
            Data = data;
            Limits = limits;
            Ratio = ratio;
            positions = (from point in data.Points select point.GetLocalPosition(Limits.Origin,Ratio)).ToArray();
            lineRenderer.Points = positions;
            lineRenderer.LineThickness = data.Width;
            lineRenderer.color = data.Color;
        }
        public virtual void SetFields()
        {
            curveRect = transform as RectTransform;
            curveRect.offsetMin = Vector2.zero;
            curveRect.offsetMax = Vector2.zero;
            lineRenderer = GetComponentInChildren<UILineRenderer>();
        }
        #endregion
    }
}