using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace HBP.UI.Tools.Graphs
{
    [ExecuteInEditMode]
    public class Axis : MonoBehaviour
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
                    SetDirection();
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

        [SerializeField] bool m_UseIndependantTickMark;
        public bool UseIndependantTickMark
        {
            get
            {
                return m_UseIndependantTickMark;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_UseIndependantTickMark, value))
                {
                    SetUseIndependentTickMark();
                }
            }
        }

        [SerializeField] float m_IndependantValue;
        public float IndependantValue
        {
            get
            {
                return m_IndependantValue;
            }
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_IndependantValue, value))
                {
                    SetIndependentValue();
                }
            }
        }

        public float IndependantNormalizedValue
        {
            get
            {
                return ValueToNormalizedValue(m_IndependantValue);
            }
            set
            {
                IndependantValue = NormalizedValueToValue(value);
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
                    SetDisplayRange();
                }
            }
        }

        [SerializeField] string m_Format ="N2";
        public string Format
        {
            get
            {
                return m_Format;
            }
            set
            {
                SetPropertyUtility.SetClass(ref m_Format, value);
            }
        }

        [SerializeField] string m_CultureInfo = "en-US";
        public string CultureInfo
        {
            get
            {
                return m_CultureInfo;
            }
            set
            {
                SetPropertyUtility.SetClass(ref m_CultureInfo, value);
            }
        }

        [SerializeField] float[] m_Values;
        Dictionary<float, MajorTickMark> m_TickMarkByValue = new Dictionary<float, MajorTickMark>();
        [SerializeField] List<MajorTickMark> m_TickMarks = new List<MajorTickMark>();
        [SerializeField] MajorTickMark m_IndependantTickMark;
        #endregion

        #region Private Setter
        void SetPosition(TickMark tickMark, float value)
        {
            float position = (value - m_DisplayRange.x) / (m_DisplayRange.y - m_DisplayRange.x);
            if (float.IsNaN(position)) return;

            RectTransform tickMarkRectTransform = tickMark.transform as RectTransform;
            RectTransform rectTransform = transform as RectTransform;
            switch (m_Direction)
            {
                case DirectionEnum.LeftToRight:
                case DirectionEnum.RightToLeft:
                    tickMarkRectTransform.localPosition = new Vector3(position * rectTransform.rect.width, tickMarkRectTransform.localPosition.y, tickMarkRectTransform.localPosition.z);
                    break;
                case DirectionEnum.BottomToTop:
                case DirectionEnum.TopToBottom:
                    tickMarkRectTransform.localPosition = new Vector3(tickMarkRectTransform.localPosition.x, position * rectTransform.rect.height, tickMarkRectTransform.localPosition.z);
                    break;
                default:
                    break;
            }
        }
        void SetIndependentValue()
        {
            SetIndependantTickMark();
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
                if (m_IndependantTickMark != null) m_IndependantTickMark.Color = m_Color;
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
        void SetDirection()
        {
            SetTickMarks();
        }
        void SetDisplayRange()
        {
            float range = m_DisplayRange.y - m_DisplayRange.x;
            if (range > 0 && m_TickMarks.Count > 1)
            {
                float normalizedStep = range / (m_TickMarks.Count - 1);
                float coef = 1;

                if (normalizedStep < 1)
                {
                    coef /= 10;
                    normalizedStep *= 10;
                }
                while (normalizedStep > 10)
                {
                    float tempResult;
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
                float step = normalizedStep * coef;

                List<float> values = new List<float>();
                int division = Mathf.FloorToInt(m_DisplayRange.x / step);
                float rest = m_DisplayRange.x % step;
                if (rest != 0) division++;
                float value = division * step;
                while (value <= m_DisplayRange.y)
                {
                    values.Add(value);
                    value += step;
                }
                m_Values = values.ToArray();
                SetTickMarks();
                SetIndependantTickMark();
            }
        }
        void SetUseIndependentTickMark()
        {
            if(m_IndependantTickMark != null) m_IndependantTickMark.gameObject.SetActive(m_UseIndependantTickMark);
            if(!m_UseIndependantTickMark)
            {
                foreach (var tickMark in m_TickMarks)
                {
                    tickMark.ShowLabel = true;
                }
            }
            else
            {
                SetIndependantTickMark();
            }
        }
        void SetIndependantTickMark()
        {
            if (m_IndependantTickMark && m_UseIndependantTickMark)
            {
                CultureInfo cultureInfo = System.Globalization.CultureInfo.GetCultureInfo(CultureInfo);
                m_IndependantTickMark.Label = m_IndependantValue.ToString(Format, cultureInfo);
                SetPosition(m_IndependantTickMark, m_IndependantValue);

                float marge = 0.04f;
                float independentTickMarkNormalizedValue = ValueToNormalizedValue(m_IndependantValue);
                foreach (var value in m_Values)
                {
                    float normalizedValue = ValueToNormalizedValue(value);
                    if (normalizedValue > independentTickMarkNormalizedValue + marge || normalizedValue < independentTickMarkNormalizedValue - marge)
                    {
                        m_TickMarkByValue[value].ShowLabel = true;
                    }
                    else
                    {
                        m_TickMarkByValue[value].ShowLabel = false;
                    }
                }
            }
        }
        void SetTickMarks()
        {
            m_TickMarkByValue = new Dictionary<float, MajorTickMark>();
            CultureInfo cultureInfo = System.Globalization.CultureInfo.GetCultureInfo(CultureInfo);
            for (int i = 0; i < m_TickMarks.Count; i++)
            {
                MajorTickMark tickMark = m_TickMarks[i];

                if (i < m_Values.Length)
                {
                    tickMark.Label = m_Values[i].ToString(Format, cultureInfo);
                    SetPosition(tickMark, m_Values[i]);
                    tickMark.Color = m_Color;
                    tickMark.gameObject.SetActive(true);
                    m_TickMarkByValue.Add(m_Values[i], tickMark);
                }
                else
                {
                    tickMark.gameObject.SetActive(false);
                }
            }
        }
        float ValueToNormalizedValue(float value)
        {
            return (value - m_DisplayRange.x) / (m_DisplayRange.y - m_DisplayRange.x);
        }
        float NormalizedValueToValue(float normalizedValue)
        {
            return normalizedValue * (m_DisplayRange.y - m_DisplayRange.x) + m_DisplayRange.x;
        }
        void OnValidate()
        {
            SetDirection();
            SetColor();
            SetLabel();
            SetUnit();
            SetDisplayRange();
            SetUseIndependentTickMark();
            SetIndependentValue();
        }
        private void OnRectTransformDimensionsChange()
        {
            SetTickMarks();
            SetIndependantTickMark();
        }
        #endregion
    }
}