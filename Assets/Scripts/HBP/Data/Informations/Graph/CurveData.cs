using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI.Extensions;
using UnityEngine.Events;
using System;

namespace HBP.Data.Informations.Graphs
{
    [CreateAssetMenu(fileName = "Curve", menuName = "Graph/Data/Curve/Empty", order = 1)]
    public class CurveData : ScriptableObject
    {
        #region Properties
        [SerializeField] string m_Label;
        public string Label
        {
            get
            {
                return m_Label;
            }
            set
            {
                SetPropertyUtility.SetClass(ref m_Label, value);
            }
        }

        [SerializeField] Color m_Color;
        public Color Color
        {
            get
            {
                return m_Color;
            }
            set
            {
                SetPropertyUtility.SetColor(ref m_Color, value);
            }
        }

        [SerializeField] float m_Thickness;
        public float Thickness
        {
            get
            {
                return m_Thickness;
            }
            set
            {
                SetPropertyUtility.SetStruct(ref m_Thickness, value);
            }
        }

        [SerializeField] Vector2[] m_Points;
        [NonSerialized] float[] m_RegularValues;
        [NonSerialized] float m_RegularStart;
        [NonSerialized] float m_RegularStep;
        public Vector2[] Points
        {
            get
            {
                if (m_Points == null && m_RegularValues != null)
                {
                    m_Points = new Vector2[m_RegularValues.Length];
                    for (int i = 0; i < m_Points.Length; i++)
                        m_Points[i] = GetPoint(i);
                }
                return m_Points ?? Array.Empty<Vector2>();
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Points, value))
                    m_RegularValues = null;
            }
        }
        public int Count => m_RegularValues?.Length ?? m_Points?.Length ?? 0;
        public bool IsRegular => m_RegularValues != null;
        public bool HasMaterializedPoints => m_Points != null;
        #endregion

        #region Public Methods
        public virtual void Init(IEnumerable<Vector2> points, Color color, float thickness = 3.0f)
        {
            m_Points = points as Vector2[] ?? points.ToArray();
            m_RegularValues = null;
            m_Color = color;
            m_Thickness = thickness;
        }
        public virtual void InitRegular(float[] values, float start, float end, Color color, float thickness = 3.0f)
        {
            m_RegularValues = values ?? Array.Empty<float>();
            m_RegularStart = start;
            m_RegularStep = m_RegularValues.Length <= 1 ? 0 : (end - start) / (m_RegularValues.Length - 1);
            m_Points = null;
            m_Color = color;
            m_Thickness = thickness;
        }
        public Vector2 GetPoint(int index)
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_RegularValues != null
                ? new Vector2(m_RegularStart + index * m_RegularStep, m_RegularValues[index])
                : m_Points[index];
        }
        public float GetOrdinate(int index)
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_RegularValues != null ? m_RegularValues[index] : m_Points[index].y;
        }
        public static CurveData CreateInstance(IEnumerable<Vector2> points, Color color, float thickness = 3.0f)
        {
            CurveData result = CreateInstance<CurveData>();
            result.Init(points, color, thickness);
            return result;
        }
        public static CurveData CreateRegular(float[] values, float start, float end, Color color, float thickness = 3.0f)
        {
            CurveData result = CreateInstance<CurveData>();
            result.InitRegular(values, start, end, color, thickness);
            return result;
        }
        #endregion
    }

    [Serializable]
    public class CurvesDataEvent : UnityEvent<CurveData[]> { }
}
