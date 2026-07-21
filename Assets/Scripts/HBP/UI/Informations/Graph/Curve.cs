using HBP.Data.Informations.Graphs;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI.Extensions;

namespace HBP.UI.Informations.Graphs
{
    public class Curve : MonoBehaviour
    {
        #region Properties
        [SerializeField] protected CurveData m_Data;
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

        [SerializeField] protected CurveRenderer m_CurveRenderer;

        [SerializeField] protected int m_NumberOfPixelsByPoint;
        public int NumberOfPixelsByPoint
        {
            get
            {
                return m_NumberOfPixelsByPoint;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_NumberOfPixelsByPoint, value))
                {
                    SetNumberOfPixelsByPoint();
                }
            }
        }

        [SerializeField, HideInInspector] protected float m_xRatio;
        [SerializeField, HideInInspector] protected float m_yRatio;
        [SerializeField, HideInInspector] protected RectTransform m_RectTransform;
        protected bool m_NeedSetPoints;
        protected Vector2[] m_RenderPointBuffer = System.Array.Empty<Vector2>();
        protected int m_RenderPointCount;
        #endregion
       
        #region Private Methods
        protected void OnValidate()
        {
            m_RectTransform = transform as RectTransform;
            SetData();
        }
        #endregion

        #region Setters
        protected virtual void SetData()
        {
            m_xRatio = m_RectTransform.rect.width / (m_AbscissaDisplayRange.y - m_AbscissaDisplayRange.x);
            m_yRatio = m_RectTransform.rect.height / (m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x);
            if (m_Data != null)
            {
                m_CurveRenderer.color = m_Data.Color;
                m_CurveRenderer.LineThickness = m_Data.Thickness;
                if(Application.isPlaying) m_NeedSetPoints = true;
                else SetPoints();
            }
        }
        protected virtual void SetAbscissaDisplayRange()
        {
            m_xRatio = m_RectTransform.rect.width / (m_AbscissaDisplayRange.y - m_AbscissaDisplayRange.x);
            if (Application.isPlaying) m_NeedSetPoints = true;
            else SetPoints();
        }
        protected virtual void SetOrdinateDisplayRange()
        {
            m_yRatio = m_RectTransform.rect.height / (m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x);
            if (Application.isPlaying) m_NeedSetPoints = true;
            else SetPoints();
        }
        protected virtual void SetNumberOfPixelsByPoint()
        {
            if (Application.isPlaying) m_NeedSetPoints = true;
            else SetPoints();
        }

        protected virtual void SetPoints()
        {
            Profiler.BeginSample("SetPoints");
            if (m_Data != null && TryGetVisibleRange(out int startIndex, out int endIndex))
            {
                Profiler.BeginSample("Points");
                Profiler.BeginSample("DownSampling");
                int length = endIndex + 1 - startIndex;
                int downSampling = GetDownSampling(length);
                m_RenderPointCount = length / downSampling;
                EnsurePointCapacity(m_RenderPointCount);
                for (int i = 0; i < m_RenderPointCount; i++)
                {
                    int v = i * downSampling + startIndex;
                    Vector2 point = m_Data.GetPoint(v);
                    m_RenderPointBuffer[i] = new Vector2(m_xRatio * (point.x - m_AbscissaDisplayRange.x), m_yRatio * (point.y - m_OrdinateDisplayRange.x));
                }
                Profiler.EndSample();
                Profiler.EndSample();
                m_CurveRenderer.SetPoints(m_RenderPointBuffer, m_RenderPointCount);
            }
            else
            {
                m_RenderPointCount = 0;
                m_CurveRenderer.SetPoints(m_RenderPointBuffer, 0);
            }
            m_NeedSetPoints = false;
            Profiler.EndSample();
        }

        protected bool TryGetVisibleRange(out int startIndex, out int endIndex)
        {
            startIndex = 0;
            endIndex = m_Data?.Count - 1 ?? -1;
            if (endIndex < 0)
                return false;

            if (startIndex < endIndex && m_Data.GetPoint(startIndex).x < m_AbscissaDisplayRange.x)
            {
                int min = startIndex;
                int max = endIndex;
                int iterations = 0;
                while (min + 1 != max && iterations++ < 20)
                {
                    int medium = (max + min) / 2;
                    float xValue = m_Data.GetPoint(medium).x;
                    if (xValue < m_AbscissaDisplayRange.x)
                        min = medium;
                    else if (xValue > m_AbscissaDisplayRange.x)
                        max = medium;
                    else
                    {
                        break;
                    }
                }
                startIndex = min;
            }

            if (startIndex < endIndex && m_Data.GetPoint(endIndex).x > m_AbscissaDisplayRange.y)
            {
                int min = startIndex;
                int max = endIndex;
                int iterations = 0;
                while (min + 1 != max && iterations++ < 100)
                {
                    int medium = (max + min) / 2;
                    float xValue = m_Data.GetPoint(medium).x;
                    if (xValue < m_AbscissaDisplayRange.y)
                        min = medium;
                    else if (xValue > m_AbscissaDisplayRange.y)
                        max = medium;
                    else
                    {
                        max = medium;
                        break;
                    }
                }
                endIndex = max;
            }
            return endIndex >= startIndex;
        }

        protected int GetDownSampling(int length)
        {
            float width = Mathf.Max(1, m_RectTransform.rect.width);
            return Mathf.Max(1, Mathf.CeilToInt(m_NumberOfPixelsByPoint * length / width));
        }

        protected void EnsurePointCapacity(int required)
        {
            if (m_RenderPointBuffer.Length >= required)
                return;
            int capacity = Mathf.NextPowerOfTwo(required);
            System.Array.Resize(ref m_RenderPointBuffer, capacity);
        }

        private void LateUpdate()
        {
            //RectTransform rectTransform = GetComponent<RectTransform>();
            //if(rectTransform.hasChanged)
            //{
            //    SetAbscissaDisplayRange();
            //    SetOrdinateDisplayRange();
            //    rectTransform.hasChanged = false;
            //}
            if(m_NeedSetPoints) SetPoints();
        }
        
        private void OnRectTransformDimensionsChange()
        {
            m_xRatio = m_RectTransform.rect.width / (m_AbscissaDisplayRange.y - m_AbscissaDisplayRange.x);
            m_yRatio = m_RectTransform.rect.height / (m_OrdinateDisplayRange.y - m_OrdinateDisplayRange.x);
            SetPoints();
        }
        #endregion
    }
}
