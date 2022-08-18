using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UI.Extensions;
using UnityEngine.Events;
using System;

namespace HBP.UI.Tools.Graphs
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
        public Vector2[] Points
        {
            get
            {
                return m_Points;
            }
            set
            {
                SetPropertyUtility.SetClass(ref m_Points, value);
            }
        }
        #endregion

        #region Public Methods
        public virtual void Init(IEnumerable<Vector2> points, Color color, float thickness = 3.0f)
        {
            m_Points = points.ToArray();
            m_Color = color;
            m_Thickness = thickness;
        }
        public static CurveData CreateInstance(IEnumerable<Vector2> points, Color color, float thickness = 3.0f)
        {
            CurveData result = CreateInstance<CurveData>();
            result.Init(points, color, thickness);
            return result;
        }
        #endregion
    }

    [Serializable]
    public class CurvesDataEvent : UnityEvent<CurveData[]> { }
}