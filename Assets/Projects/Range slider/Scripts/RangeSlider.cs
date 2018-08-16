using UnityEngine;
using UnityEngine.UI;

public class RangeSlider : MonoBehaviour
{
    #region Properties
    public Slider LeftSlider;
    public Slider RightSlider;
    public RectTransform FillRect;
    #endregion

    #region Public Methods
    private void Start()
    {
        LeftSlider.onValueChanged.AddListener(OnChangeLeftValue);
        RightSlider.onValueChanged.AddListener(OnChangeRightValue);
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
