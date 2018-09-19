using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RangeSlider : MonoBehaviour
{
    #region Properties
    public Slider LeftSlider;
    public Slider RightSlider;
    public RectTransform FillRect;
    #endregion

    #region Public Methods
    private void Awake()
    {
        LeftSlider.onValueChanged.AddListener(OnChangeLeftValue);
        RightSlider.onValueChanged.AddListener(OnChangeRightValue);
        OnChangeLeftValue(LeftSlider.value);
        OnChangeRightValue(RightSlider.value);
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
    }
    void OnChangeRightValue(float value)
    {
        if (LeftSlider.value > value) LeftSlider.value = value;
        FillRect.anchorMax = new Vector2(RightSlider.handleRect.anchorMax.x,FillRect.anchorMax.y);
    }
    #endregion
}