using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Graph
{
    [ExecuteInEditMode]
    public class Axe : MonoBehaviour
    {
        #region Properties
        public enum DirectionEnum { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
        [SerializeField] DirectionEnum m_Direction;
        public DirectionEnum Direction
        {
            get
            {
                return m_Direction;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_Direction, value))
                {
                    SetType();
                }
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
                if (SetPropertyUtility.SetColor(ref m_Color, value))
                {
                    SetColor();
                }
            }
        }

        [SerializeField] Text m_UnitText;
        [SerializeField] string m_Unit;
        public string Unit
        {
            get
            {
                return m_Unit;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Unit, value))
                {
                    SetUnit();
                }
            }
        }

        [SerializeField] Text m_LabelText;
        [SerializeField] string m_Label;
        public string Label
        {
            get
            {
                return m_Label;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Label, value))
                {
                    SetLabel();
                }
            }
        }

        [SerializeField] bool m_UseIndependentTickMark;
        public bool UseIndependentTickMark
        {
            get
            {
                return m_UseIndependentTickMark;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_UseIndependentTickMark, value))
                {
                    SetUseIndependentTickMark();
                }
            }
        }

        [SerializeField] float m_IndependentTickMarkValue;
        public float IndependentTickMarkValue
        {
            get
            {
                return m_IndependentTickMarkValue;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_IndependentTickMarkValue, value))
                {
                    SetIndependentValue();
                }
            }
        }
        
        public float IndependantTickMarkValueRatio
        {
            get
            {
                return (m_IndependentTickMarkValue - m_DisplayRange.x) / (m_DisplayRange.y - m_DisplayRange.x);
            }
            set
            {
                float independentValue = value * (m_DisplayRange.y - m_DisplayRange.x) + m_DisplayRange.x;
                IndependentTickMarkValue = independentValue;
            }
        }

        [SerializeField] Image m_VisualImage;
        [SerializeField] Image m_VisualArrowImage;

        [SerializeField] Vector2 m_DisplayRange;
        public Vector2 DisplayRange
        {
            get
            {
                return m_DisplayRange;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_DisplayRange, value))
                {
                    //SetDisplayRange();
                }
            }
        }

        [SerializeField] List<MajorTickMark> m_TickMarks = new List<MajorTickMark>();
        [SerializeField] MajorTickMark m_IndependentTickMark;
        #endregion

        #region Private Setter
        void SetIndependentValue()
        {
            if(m_IndependentTickMark)
            {
                m_IndependentTickMark.Label = m_IndependentTickMarkValue.ToString();
                m_IndependentTickMark.Position = ((float)m_IndependentTickMarkValue - m_DisplayRange.x) / (m_DisplayRange.y - m_DisplayRange.x);
                m_IndependentTickMark.Direction = m_Direction;
                m_IndependentTickMark.Color = m_Color;
            }
        }
        void SetColor()
        {
            if (m_Color != null)
            {
                if (m_TickMarks != null)
                {
                    foreach (var tickMark in m_TickMarks)
                    {
                        if (tickMark != null) tickMark.Color = m_Color;
                    }
                }
                if (m_IndependentTickMark != null) m_IndependentTickMark.Color = m_Color;
                if (m_VisualArrowImage != null) m_VisualArrowImage.color = m_Color;
                if (m_VisualImage != null) m_VisualImage.color = m_Color;
                if (m_LabelText != null) m_LabelText.color = m_Color;
                if (m_UnitText != null) m_UnitText.color = m_Color;
            }
        }
        void SetLabel()
        {
            if (m_LabelText != null && m_Label != null) m_LabelText.text = m_Label;
        }
        void SetUnit()
        {
            if (m_UnitText != null && m_Unit != null) m_UnitText.text = string.Format("({0})", m_Unit);
        }
        void SetType()
        {

        }
        void SetDisplayRange()
        {
            double range = m_DisplayRange.y - m_DisplayRange.x;
            if (range > 0 && m_TickMarks.Count > 1)
            {
                double normalizedStep = range / (m_TickMarks.Count - 1);
                double coef = 1;

                if (normalizedStep < 1)
                {
                    coef /= 10;
                    normalizedStep *= 10;
                }
                while (normalizedStep > 10)
                {
                    double tempResult;
                    tempResult = normalizedStep / 2;
                    if (tempResult >= 1 && tempResult <= 10)
                    {
                        coef *= 2;
                        normalizedStep = tempResult;
                        break;
                    }

                    tempResult = normalizedStep / 5;
                    if (tempResult >= 1 && tempResult <= 10)
                    {
                        normalizedStep = tempResult;
                        coef *= 5;
                        break;
                    }

                    tempResult = normalizedStep / 10;
                    if (normalizedStep >= 1 && normalizedStep <= 10)
                    {
                        normalizedStep = tempResult;
                        coef *= 10;
                        break;
                    }

                    coef *= 10;
                    normalizedStep /= 10;
                }
                if (normalizedStep > 1 && normalizedStep <= 5)
                {
                    normalizedStep = 5;
                }
                else if (normalizedStep > 5 && normalizedStep <= 10)
                {
                    normalizedStep = 10;
                }
                double step = normalizedStep * coef;

                List<double> tickMarks = new List<double>();
                int division = Mathf.FloorToInt((float)(m_DisplayRange.x / step));
                double rest = m_DisplayRange.x % step;
                if (rest != 0) division++;
                double value = division * step;
                while (value <= m_DisplayRange.y)
                {
                    tickMarks.Add(value);
                    value += step;
                }

                for (int i = 0; i < m_TickMarks.Count; i++)
                {
                    MajorTickMark tickMark = m_TickMarks[i];

                    if (i < tickMarks.Count)
                    {
                        tickMark.Label = tickMarks[i].ToString();
                        tickMark.Position = ((float)tickMarks[i] - m_DisplayRange.x) / (m_DisplayRange.y - m_DisplayRange.x);
                        tickMark.Direction = m_Direction;
                        tickMark.Color = m_Color;
                        tickMark.gameObject.SetActive(true);
                    }
                    else
                    {
                        tickMark.gameObject.SetActive(false);
                    }

                }
            }

        }
        void SetUseIndependentTickMark()
        {
            m_IndependentTickMark.gameObject.SetActive(m_UseIndependentTickMark);
        }
        void OnValidate()
        {
            SetType();
            SetColor();
            SetLabel();
            SetUnit();
            SetDisplayRange();
            SetUseIndependentTickMark();
            SetIndependentValue();
        }
        #endregion
    }
}