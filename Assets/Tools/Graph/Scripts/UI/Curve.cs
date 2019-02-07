using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Graph
{
    public class Curve : MonoBehaviour
    {
        #region Properties
        [SerializeField] CurveData m_Data;
        public CurveData Data
        {
            get
            {
                return m_Data;
            }
            set
            {
                if(SetPropertyUtility.SetClass(ref m_Data, value))
                {
                    SetData();
                }
            }
        }

        [SerializeField] protected Vector2 m_OrdinateDisplayRange;
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

        [SerializeField] protected Vector2 m_AbscissaDisplayRange;
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

        [SerializeField] protected UILineRenderer m_LineRenderer;
        #endregion
       
        #region Private Methods
        protected void OnValidate()
        {
            SetData();
        }
        #endregion

        #region Setters
        protected virtual void SetData()
        {
            if(m_Data != null)
            {
                m_LineRenderer.color = m_Data.Color;
                m_LineRenderer.LineThickness = m_Data.Thickness;

                RectTransform rectTransform = transform as RectTransform;
                Vector2[] points = new Vector2[m_Data.Points.Length];
                for (int i = 0; i < m_Data.Points.Length; i++)
                {
                    Vector2 point = m_Data.Points[i];
                    float x = rectTransform.rect.width * (point.x - m_AbscissaDisplayRange.x) / (m_AbscissaDisplayRange.y - m_AbscissaDisplayRange.x);
                    float y = rectTransform.rect.height * (point.y - m_OrdinateDisplayRange.x) / (m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x);
                    points[i] = new Vector2(x, y);
                }
                m_LineRenderer.Points = points;
            }
        }
        protected virtual void SetAbscissaDisplayRange()
        {
            if(m_Data != null)
            {
                RectTransform rectTransform = transform as RectTransform;
                Vector2[] points = new Vector2[m_Data.Points.Length];
                for (int i = 0; i < m_Data.Points.Length; i++)
                {
                    float x = rectTransform.rect.width * (m_Data.Points[i].x - m_AbscissaDisplayRange.x) / (m_AbscissaDisplayRange.y - m_AbscissaDisplayRange.x);
                    points[i] = new Vector2(x, m_LineRenderer.Points[i].y);
                }
                m_LineRenderer.Points = points;
            }
        }
        protected virtual void SetOrdinateDisplayRange()
        {
            if(m_Data != null)
            {
                RectTransform rectTransform = transform as RectTransform;
                Vector2[] points = new Vector2[m_Data.Points.Length];
                for (int i = 0; i < m_Data.Points.Length; i++)
                {
                    float y = rectTransform.rect.height * (m_Data.Points[i].y - m_OrdinateDisplayRange.x) / (m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x);
                    points[i] = new Vector2(m_LineRenderer.Points[i].x, y);
                }
                m_LineRenderer.Points = points;
            }
        }
        #endregion
    }
}