using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using NewTheme.Components;

namespace Tools.Unity.Components
{
    public class OldRangeSlider : Selectable
    {
        #region Properties
        [SerializeField] Slider m_MinSlider;
        [SerializeField] Slider m_MaxSlider;
        [SerializeField] RectTransform m_FillRect;

        bool m_Interactable;
        public bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
                m_MinSlider.interactable = value;
                m_MaxSlider.interactable = value;
            }
        }

        bool m_WholeNumbers;
        public bool WholeNumbers
        {
            get
            {
                return m_WholeNumbers;
            }
            set
            {
                m_WholeNumbers = value;
                if(value) Value = new Vector2((int)Value.x, (int)Value.y);
            }
        }

        Vector2 m_Range = new Vector2(-1500, 1500);
        public Vector2 Range
        {
            get
            {
                return m_Range;
            }
            set
            {
                m_Range = value;
                SetSliders();
            }
        }

        float m_Step = 100;
        public float Step
        {
            get
            {
                return m_Step;
            }
            set
            {
                m_Step = value;
                SetSliders();
            }
        }

        Vector2 m_Value;
        public Vector2 Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if(m_WholeNumbers)
                {
                    m_Value = new Vector2((int)value.x, (int)value.y);
                }
                else
                {
                    m_Value = value;
                }
                m_IsSetting = true;
                Debug.Log("Set:" + value);
                Vector2 correctedValue = CorrectValue(value);
                Debug.Log(correctedValue);

                Display(correctedValue);
                OnValueChanged.Invoke(correctedValue);
                m_IsSetting = false;
            }
        }

        bool m_IsSetting = false;

        public Vector2Event OnValueChanged { get; set; } = new Vector2Event();
        #endregion

        #region Events
        public void OnDrag(BaseEventData eventData)
        {
            if (m_Interactable)
            {
                PointerEventData pointerEventData = (PointerEventData) eventData;
                RectTransform fillAreaRectTransform = (m_FillRect.parent as RectTransform);
                Rect fillAreaRect = fillAreaRectTransform.rect;
                float ratio = pointerEventData.delta.x / fillAreaRect.width;
                if (ratio > 0)
                {
                    ratio = Mathf.Clamp01(ratio + m_MaxSlider.normalizedValue) - m_MaxSlider.normalizedValue;
                }
                else
                {
                    ratio = Mathf.Clamp01(ratio + m_MinSlider.normalizedValue) - m_MinSlider.normalizedValue;
                }
                Value += NormalizedValueToValue(ratio * Vector2.one);
            }
        }

        public void OnMinSliderValueChanged(float value)
        {
            if (!m_IsSetting)
            {
                Debug.Log("OnMin: " + value * m_Step);
                Value = CorrectValue(new Vector2(value * m_Step, m_Value.y));
            }
        }
        public void OnMinSliderBeginDrag(BaseEventData eventData)
        {
            if(m_Interactable)
            {
                m_MinSlider.wholeNumbers = true;
            }
        }
        public void OnMinSliderEndDrag(BaseEventData eventData)
        {
            if (m_Interactable)
            {
                m_MinSlider.wholeNumbers = false;
            }
        }

        public void OnMaxSliderValueChanged(float value)
        {
            if (!m_IsSetting)
            {
                Debug.Log("OnMax: " + value * m_Step);
                Value = CorrectValue(new Vector2(m_Value.x, value * m_Step), false);
            }
        }
        public void OnMaxSliderBeginDrag(BaseEventData eventData)
        {
            if (m_Interactable)
            {
                m_MaxSlider.wholeNumbers = true;
            }
        }
        public void OnMaxSliderEndDrag(BaseEventData eventData)
        {
            if (m_Interactable)
            {
                m_MaxSlider.wholeNumbers = false;
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            Interactable = Interactable;
        }
        void SetSliders()
        {
            m_MinSlider.minValue = m_Range.x / m_Step;
            m_MinSlider.maxValue = m_Range.y / m_Step;

            m_MaxSlider.minValue = m_Range.x / m_Step;
            m_MaxSlider.maxValue = m_Range.y / m_Step;
        }
        void Display(Vector2 value)
        {
            Vector2 normalizedValue = ValueToNormalizedValue(value);
            Debug.Log("NormalizedValue" + normalizedValue.x + " " + normalizedValue.y);
            m_MinSlider.normalizedValue = normalizedValue.x;
            m_MaxSlider.normalizedValue = normalizedValue.y;
            m_FillRect.anchorMin = new Vector2(m_MinSlider.normalizedValue, m_FillRect.anchorMin.y);
            m_FillRect.anchorMax = new Vector2(m_MaxSlider.normalizedValue, m_FillRect.anchorMax.y);
        }

        Vector2 CorrectValue(Vector2 value, bool minCorrectMax = true)
        {
            if (minCorrectMax) return new Vector2(value.x, Mathf.Clamp(value.y, value.x, m_Range.y));
            else return new Vector2(Mathf.Clamp(value.x, m_Range.x, value.y), value.y);
        }
        Vector2 NormalizedValueToValue(Vector2 normalizedValue)
        {
            return normalizedValue * m_Range.Range() + m_Range.x * Vector2.one;
        }
        Vector2 ValueToNormalizedValue(Vector2 value)
        {
            return (value - m_Range.x * Vector2.one) / m_Range.Range();
        }
        #endregion
    }
}