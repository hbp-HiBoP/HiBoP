using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Graph
{
    public class ShapedCurve : Curve
    {
        #region Properties
        [SerializeField] UIVerticalShapeRenderer m_ShapeRenderer;

        public new ShapedCurveData Data
        {
            get
            {
                return m_Data as ShapedCurveData;
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
            if(m_Data != null)
            {
                base.SetData();
                if (m_Data is ShapedCurveData)
                {
                    m_ShapeRenderer.color = new Color(m_Data.Color.r, m_Data.Color.g, m_Data.Color.b, 0.5f);

                    RectTransform rectTransform = transform as RectTransform;
                    float[] shapes = new float[Data.Shapes.Length];
                    for (int i = 0; i < Data.Shapes.Length; i++)
                    {
                        shapes[i] = Data.Shapes[i] * rectTransform.rect.height / (m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x);
                    }

                    m_ShapeRenderer.Points = m_CurveRenderer.Points;
                    m_ShapeRenderer.ShapeThickness = shapes;
                }
                else
                {
                    m_ShapeRenderer.Points = new Vector2[0];
                    m_ShapeRenderer.ShapeThickness = new float[0];
                }
            }
        }
        protected override void SetAbscissaDisplayRange()
        {
            if(m_Data != null)
            {
                base.SetAbscissaDisplayRange();
                if(m_Data is ShapedCurveData)
                {
                    m_ShapeRenderer.Points = m_CurveRenderer.Points;
                }
                else
                {
                    m_ShapeRenderer.Points = new Vector2[0];
                    m_ShapeRenderer.ShapeThickness = new float[0];
                }
            }
        }
        protected override void SetOrdinateDisplayRange()
        {
            if(m_Data != null)
            {
                base.SetOrdinateDisplayRange();
                if(m_Data is ShapedCurveData)
                {
                    RectTransform rectTransform = transform as RectTransform;
                    float[] shapes = new float[Data.Shapes.Length];
                    for (int i = 0; i < Data.Shapes.Length; i++)
                    {
                        shapes[i] = Data.Shapes[i] * rectTransform.rect.height / (m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x);
                    }

                    m_ShapeRenderer.Points = m_CurveRenderer.Points;
                    m_ShapeRenderer.ShapeThickness = shapes;
                }
                else
                {
                    m_ShapeRenderer.Points = new Vector2[0];
                    m_ShapeRenderer.ShapeThickness = new float[0];
                }

            }
        }
        #endregion
    }
}