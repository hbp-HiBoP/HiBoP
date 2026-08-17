using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using HBP.Data.Informations.Graphs;

namespace HBP.UI.Informations.Graphs
{
    public class ShapedCurve : Curve
    {
        #region Properties

        [SerializeField] UIVerticalShapeRenderer m_ShapeRenderer;
        float[] m_RenderShapeBuffer = System.Array.Empty<float>();

        public new ShapedCurveData Data
        {
            get { return m_Data as ShapedCurveData; }
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
            if (m_Data != null)
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
                    m_ShapeRenderer.SetData(System.Array.Empty<Vector2>(), System.Array.Empty<float>(), 0);
                }
            }
        }

        protected override void SetAbscissaDisplayRange()
        {
            if (m_Data != null)
            {
                base.SetAbscissaDisplayRange();
                if (m_Data is ShapedCurveData)
                {
                    if (!Application.isPlaying) SetPoints();
                }
                else
                {
                    m_ShapeRenderer.SetData(System.Array.Empty<Vector2>(), System.Array.Empty<float>(), 0);
                }
            }
        }

        protected override void SetOrdinateDisplayRange()
        {
            if (m_Data != null)
            {
                base.SetOrdinateDisplayRange();
                if (m_Data is ShapedCurveData)
                {
                    if (!Application.isPlaying) SetPoints();
                }
                else
                {
                    m_ShapeRenderer.SetData(System.Array.Empty<Vector2>(), System.Array.Empty<float>(), 0);
                }
            }
        }

        protected override void SetPoints()
        {
            if (m_Data != null)
            {
                if (m_Data is ShapedCurveData)
                {
                    ShapedCurveData shapedData = m_Data as ShapedCurveData;
                    if (!TryGetVisibleRange(out int startIndex, out int endIndex))
                    {
                        m_RenderPointCount = 0;
                        m_CurveRenderer.SetPoints(m_RenderPointBuffer, 0);
                        m_ShapeRenderer.SetData(m_RenderPointBuffer, m_RenderShapeBuffer, 0);
                        m_NeedSetPoints = false;
                        return;
                    }

                    int length = endIndex + 1 - startIndex;
                    int downSampling = GetDownSampling(length);
                    m_RenderPointCount = downSampling > 1 ? length / downSampling + 1 : length;
                    EnsurePointCapacity(m_RenderPointCount);
                    EnsureShapeCapacity(m_RenderPointCount);

                    int regularCount = downSampling > 1 ? m_RenderPointCount - 1 : m_RenderPointCount;
                    for (int i = 0; i < regularCount; i++)
                    {
                        int sourceIndex = startIndex + i * downSampling;
                        FillPointAndShape(shapedData, sourceIndex, i, startIndex);
                    }

                    if (downSampling > 1)
                        FillPointAndShape(shapedData, endIndex, m_RenderPointCount - 1, startIndex);

                    m_CurveRenderer.SetPoints(m_RenderPointBuffer, m_RenderPointCount);
                    m_ShapeRenderer.SetData(m_RenderPointBuffer, m_RenderShapeBuffer, m_RenderPointCount);
                    m_NeedSetPoints = false;
                }
                else
                {
                    base.SetPoints();
                }
            }
        }

        void FillPointAndShape(ShapedCurveData data, int sourceIndex, int destinationIndex, int startIndex)
        {
            Vector2 point = data.GetPoint(sourceIndex);
            m_RenderPointBuffer[destinationIndex] = new Vector2(m_xRatio * (point.x - m_AbscissaDisplayRange.x), m_yRatio * (point.y - m_OrdinateDisplayRange.x));
            int shapeIndex = sourceIndex == startIndex && point.x < m_AbscissaDisplayRange.x && sourceIndex + 1 < data.Count ? sourceIndex + 1 : sourceIndex;
            m_RenderShapeBuffer[destinationIndex] = m_yRatio * data.Shapes[shapeIndex];
        }

        void EnsureShapeCapacity(int required)
        {
            if (m_RenderShapeBuffer.Length >= required)
                return;
            System.Array.Resize(ref m_RenderShapeBuffer, Mathf.NextPowerOfTwo(required));
        }

        #endregion
    }
}
