using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using System.Linq;

namespace Tools.Unity.Graph
{
    public class ShapedCurve : Curve
    {
        #region Properties
        protected UIShapeRenderer shapeRenderer;
        protected float[] shapes;
        #endregion

        #region Public Methods
        public virtual void Plot(ShapedCurveData data, Limits limits, Vector2 ratio)
        {
            base.Plot(data as CurveData,limits,ratio);
            shapes = (from shape in data.Shapes select shape * ratio.y).ToArray();
            shapeRenderer.Points = positions;
            shapeRenderer.ShapeThickness = shapes;
            shapeRenderer.color = new Color(data.Color.r, data.Color.g, data.Color.b, 0.3f);
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
        {
            base.SetFields();
            shapeRenderer = GetComponentInChildren<UIShapeRenderer>();
        }
        void Awake()
        {
            SetFields();
        }
        #endregion
    }
}
