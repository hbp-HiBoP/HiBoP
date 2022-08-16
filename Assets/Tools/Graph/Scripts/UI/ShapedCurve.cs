using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace HBP.UI.Graphs
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
                    if (Application.isPlaying) m_NeedSetPoints = true;
                    else SetPoints();
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
        protected override void SetPoints()
        {
            if(m_Data != null)
            {
                if(m_Data is ShapedCurveData)
                {
                    ShapedCurveData shapedData = m_Data as ShapedCurveData;
                    List<Vector2> points = new List<Vector2>(shapedData.Points.Length);
                    List<float> shapes = new List<float>(shapedData.Shapes.Length);
                    bool first = true;
                    float x, y;
                    for (int i = 0; i < shapedData.Points.Length; i++)
                    {
                        Vector2 point = shapedData.Points[i];
                        if (point.x >= m_AbscissaDisplayRange.x)
                        {
                            if (first && i >= 1)
                            {
                                Vector2 pointBefore = m_Data.Points[i - 1];
                                x = m_xRatio * (pointBefore.x - m_AbscissaDisplayRange.x);
                                y = m_yRatio * (pointBefore.y - m_OrdinateDisplayRange.x);
                                points.Add(new Vector2(x, y));
                                shapes.Add(m_yRatio * shapedData.Shapes[i]);
                            }
                            first = false;
                            x = m_xRatio * (point.x - m_AbscissaDisplayRange.x);
                            y = m_yRatio * (point.y - m_OrdinateDisplayRange.x);
                            shapes.Add(m_yRatio * shapedData.Shapes[i]);
                            points.Add(new Vector2(x, y));
                        }
                        if(point.x > m_AbscissaDisplayRange.y)
                        {
                            break;
                        }
                    }

                    float pointsByPixel = m_NumberOfPixelsByPoint * points.Count / (m_RectTransform.rect.width);
                    int downSampling = Mathf.CeilToInt(pointsByPixel);
                    if (downSampling > 1)
                    {
                        List<Vector2> newPoints = new List<Vector2>(points.Count / downSampling);
                        List<float> newShapes = new List<float>(points.Count / downSampling);
                        for (int i = 0; i < (points.Count / downSampling); i++)
                        {
                            int d = i * downSampling;
                            newPoints.Add(points[d]);
                            newShapes.Add(shapes[d]);
                        }
                        newPoints.Add(points[points.Count - 1]);
                        newShapes.Add(shapes[shapes.Count - 1]);
                        points = newPoints;
                        shapes = newShapes;
                    }
                    m_CurveRenderer.Points = points.ToArray();
                    m_ShapeRenderer.Points = m_CurveRenderer.Points;
                    m_ShapeRenderer.ShapeThickness = shapes.ToArray();
                    m_NeedSetPoints = false;
                }
                else
                {
                    base.SetData();
                }        
            }
        }
        #endregion
    }
}