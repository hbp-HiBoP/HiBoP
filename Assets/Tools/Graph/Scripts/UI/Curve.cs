using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI.Extensions;

namespace HBP.UI.Tools.Graphs
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
            if (m_Data != null)
            {
                float x, y;

                Profiler.BeginSample("Points");
                Profiler.BeginSample("First-Last");

                int startIndex = 0;
                int endIndex = m_Data.Points.Length - 1;
                if (m_Data.Points[startIndex].x < m_AbscissaDisplayRange.x)
                {
                    int min = startIndex;
                    int max = endIndex;
                    int i = 0;
                    while(min + 1 != max && i < 20)
                    {
                        i++;
                        int medium = (max + min)/2;
                        float xValue = m_Data.Points[medium].x;
                        if(xValue < m_AbscissaDisplayRange.x)
                        {
                            min = medium;
                        }
                        else if(xValue > m_AbscissaDisplayRange.x)
                        {
                            max = medium;
                        }
                        else
                        {
                            startIndex = medium;
                            break;
                        }
                    }
                    startIndex = min;
                }

                if (m_Data.Points[endIndex].x > m_AbscissaDisplayRange.y)
                {
                    int min = startIndex;
                    int max = endIndex;
                    int i = 0;
                    while (min + 1 != max && i < 100)
                    {
                        i++;
                        int medium = (max + min) / 2;
                        float xValue = m_Data.Points[medium].x;
                        if (xValue < m_AbscissaDisplayRange.y)
                        {
                            min = medium;
                        }
                        else if (xValue > m_AbscissaDisplayRange.y)
                        {
                            max = medium;
                        }
                        else
                        {
                            max = medium;
                            break;
                        }
                    }
                    endIndex = max;
                }
                Profiler.EndSample();

                Profiler.BeginSample("DownSampling");
                int lenght = endIndex + 1 - startIndex;
                int downSampling = Mathf.Max(1,Mathf.CeilToInt(m_NumberOfPixelsByPoint * lenght / (m_RectTransform.rect.width)));
                Vector2[] points = new Vector2[lenght / downSampling];
                for (int i = 0; i < points.Length; i++)
                {
                    int v = i * downSampling + startIndex;
                    points[i] = new Vector2(m_xRatio * (m_Data.Points[v].x - m_AbscissaDisplayRange.x), m_yRatio * (m_Data.Points[v].y - m_OrdinateDisplayRange.x));
                }
                Profiler.EndSample();
            
                Profiler.EndSample();
                m_CurveRenderer.Points = points;

            }
            m_NeedSetPoints = false;
            Profiler.EndSample();
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