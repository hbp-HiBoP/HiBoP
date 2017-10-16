using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform)), ExecuteInEditMode]
public class VerticalUIFitter : MonoBehaviour, ILayoutSelfController
{
    #region Properties
    DrivenRectTransformTracker tracker;
    RectTransform rectTransform;
    RectTransform parentRectTransform;

    public enum RotationEnum { Left, Right };
    RotationEnum m_rotation;
    public RotationEnum Rotation
    {
        get
        {
            return m_rotation;
        }
        set
        {
            m_rotation = value;
            SetRotation();
        }
    }
    #endregion

    #region Public Methods
    public void SetLayoutHorizontal()
    {
        rectTransform.sizeDelta = new Vector2(parentRectTransform.rect.height,rectTransform.sizeDelta.y);
    }
    public void SetLayoutVertical()
    {
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, parentRectTransform.rect.width);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    #endregion

    #region Events
    void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRectTransform = transform.parent.GetComponent<RectTransform>();
        tracker.Add(this, rectTransform, DrivenTransformProperties.All);
        rectTransform.pivot = new Vector2(0, 1);
        SetRotation();
    }
    void OnDisable()
    {
        tracker.Clear();
    }
    void OnTransformParentChanged()
    {
        parentRectTransform = transform.parent.GetComponent<RectTransform>();
    }
    #endregion

    #region Private Methods
    void Update()
    {
        if(parentRectTransform.hasChanged)
        {
            SetLayoutHorizontal();
            SetLayoutVertical();
        }
    }
    void SetRotation()
    {
        if (isActiveAndEnabled)
        {
            switch (m_rotation)
            {
                case RotationEnum.Left:
                    rectTransform.localRotation = Quaternion.AngleAxis(90, Vector3.forward);
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.zero;
                    break;
                case RotationEnum.Right:
                    rectTransform.localRotation = Quaternion.AngleAxis(-90, Vector3.forward);
                    rectTransform.anchorMin = Vector2.one;
                    rectTransform.anchorMax = Vector2.one;
                    break;
            }
        }
    }
    #endregion
}