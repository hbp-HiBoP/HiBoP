using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Graph
{
    public class ShapedCurve : Curve
    {
        #region Properties
        [SerializeField] UIShapeRenderer m_ShapeRenderer;

        [SerializeField] ShapedCurveData m_Data;
        public new ShapedCurveData Data
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
        #endregion

        #region Setters
        protected override void SetData()
        {
            base.SetData();
            m_ShapeRenderer.color = new Color(m_Data.Color.r, m_Data.Color.g, m_Data.Color.b, 0.5f);

            RectTransform rectTransform = transform as RectTransform;
            float[] shapes = new float[m_Data.Shapes.Length];
            for (int i = 0; i < m_Data.Shapes[i]; i++)
            {
                shapes[i] = m_Data.Shapes[i] * rectTransform.rect.height / (m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x);
            }

            m_ShapeRenderer.Points = m_LineRenderer.Points;
            m_ShapeRenderer.ShapeThickness = shapes;
        }
        protected override void SetAbscissaDisplayRange()
        {
            if(m_Data != null)
            {
                base.SetAbscissaDisplayRange();
                m_ShapeRenderer.Points = m_LineRenderer.Points;
            }
        }
        protected override void SetOrdinateDisplayRange()
        {
            if(m_Data != null)
            {
                base.SetOrdinateDisplayRange();
                RectTransform rectTransform = transform as RectTransform;
                float[] shapes = new float[m_Data.Shapes.Length];
                for (int i = 0; i < m_Data.Shapes[i]; i++)
                {
                    shapes[i] = m_Data.Shapes[i] * rectTransform.rect.height / (m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x);
                }

                m_ShapeRenderer.Points = m_LineRenderer.Points;
                m_ShapeRenderer.ShapeThickness = shapes;
            }
        }
        #endregion
    }
}