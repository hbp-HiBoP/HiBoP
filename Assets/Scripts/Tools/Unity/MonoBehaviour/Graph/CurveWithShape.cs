using UnityEngine;
using System.Collections;

namespace Tools.Unity.Graph
{
    public class CurveWithShape : Curve
    {
        #region Properties
        [SerializeField]
        protected GameObject m_Shape;

        protected Shape[] m_shapes = new Shape[0];
        protected RectTransform shapesRect;
        #endregion

        #region Public Methods
        public override void Zoom(Vector2 ratio)
        {
            if (ratio == Ratio)
            {
                return;
            }
            base.Zoom(ratio);
            for (int i = 0; i < m_shapes.Length; i++)
            {
                UpdateShape(i);
            }
        }
        #endregion

        #region Private Methods
        protected override void SetRects()
        {
            base.SetRects();
            shapesRect = transform.GetChild(2).GetComponent<RectTransform>();
        }

        protected override void Plot()
        {
            base.Plot();
            m_shapes = new Shape[m_dataCurve.Points.Length - 1];
            for (int i = 0; i < m_shapes.Length; i++)
            {
                AddShape(i);
            }
        }

        protected override void Clear()
        {
            base.Clear();
            foreach (Shape shape in m_shapes)
            {
                Destroy(shape.gameObject);
            }
            m_shapes = new Shape[0];
        }


        protected void AddShape(int i)
        {
            GameObject l_object = Instantiate(m_Shape);
            l_object.transform.SetParent(shapesRect);
            Shape l_shape = l_object.GetComponent<Shape>();
            m_shapes[i] = l_shape;
            Data.CurveWithShape l_curve = (Data.CurveWithShape)DataCurve;
            l_shape.Set(m_points[i], m_points[i + 1], l_curve.Shapes[i], l_curve.Shapes[i + 1], Ratio, l_curve.Color);
        }

        protected void UpdateShape(int i)
        {
            m_shapes[i].UpdateShape(Ratio);
        }

        protected override void SetColor()
        {
            foreach (Transform shape in shapesRect)
            {
                shape.GetComponent<Shape>().Color = DataCurve.Color;
            }
        }
        #endregion
    }
}
