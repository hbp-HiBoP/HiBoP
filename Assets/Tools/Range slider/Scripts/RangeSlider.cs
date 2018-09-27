using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RangeSlider : MonoBehaviour
{
    #region Properties
    public bool interactable
    {
        set
        {
            LeftSlider.interactable = value;
            RightSlider.interactable = value;
        }
    }
    public Slider LeftSlider;
    public Slider RightSlider;
    public RectTransform FillRect;

    public int MinPossibleValue;
    public int MaxPossibleValue;
    public int Step;
    bool m_Initialized;
    public int MaxValue
    {
        get
        {
            return (int) RightSlider.value * Step;
        }
        set
        {
            RightSlider.value = value / Step;
            OnMaxValueChanged.Invoke(value);
        }
    }
    public int MinValue
    {
        get
        {
            return (int) LeftSlider.value * Step;
        }
        set
        {
            LeftSlider.value = value / Step;
            OnMinValueChanged.Invoke(value);
        }
    }
    public Slider.SliderEvent OnMaxValueChanged = new Slider.SliderEvent();
    public Slider.SliderEvent OnMinValueChanged = new Slider.SliderEvent();
    #endregion

    #region Public Methods
    private void Awake()
    {
        LeftSlider.minValue = MinPossibleValue / Step;
        LeftSlider.maxValue = MaxPossibleValue / Step;

        RightSlider.minValue = MinPossibleValue / Step;
        RightSlider.maxValue = MaxPossibleValue / Step;

        LeftSlider.onValueChanged.AddListener(OnChangeLeftValue);
        RightSlider.onValueChanged.AddListener(OnChangeRightValue);
        OnChangeLeftValue(LeftSlider.value);
        OnChangeRightValue(RightSlider.value);
        m_Initialized = true;
    }
    public void OnDrag(BaseEventData eventData)
    {
        PointerEventData pointerEventData = (PointerEventData) eventData;
        RectTransform fillAreaRectTransform = (FillRect.parent as RectTransform);
        Rect fillAreaRect = fillAreaRectTransform.rect;
        float ratio = pointerEventData.delta.x / fillAreaRect.width;
        if(ratio > 0)
        {
            ratio = Mathf.Clamp01(ratio + RightSlider.normalizedValue) - RightSlider.normalizedValue;
        }
        else
        {
            ratio = Mathf.Clamp01(ratio + LeftSlider.normalizedValue) - LeftSlider.normalizedValue;
        }
        LeftSlider.normalizedValue += ratio;
        RightSlider.normalizedValue += ratio;
    }
    #endregion

    #region Private Methods
    void OnChangeLeftValue(float value)
    {
        if (RightSlider.value < value) RightSlider.value = value;
        FillRect.anchorMin = new Vector2(LeftSlider.handleRect.anchorMin.x, FillRect.anchorMin.y);
        if(m_Initialized) OnMinValueChanged.Invoke(value * Step);
    }
    void OnChangeRightValue(float value)
    {
        if (LeftSlider.value > value) LeftSlider.value = value;
        FillRect.anchorMax = new Vector2(RightSlider.handleRect.anchorMax.x,FillRect.anchorMax.y);
        if(m_Initialized) OnMaxValueChanged.Invoke(value * Step);
    }
    #endregion
}