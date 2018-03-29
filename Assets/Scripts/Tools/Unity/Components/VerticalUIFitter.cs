using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform)), ExecuteInEditMode]
public class VerticalUIFitter : MonoBehaviour, ILayoutSelfController
{
    #region Properties
    [SerializeField,HideInInspector]
    DrivenRectTransformTracker m_tracker = new DrivenRectTransformTracker();
    [SerializeField, HideInInspector]
    RectTransform m_rectTransform;
    [SerializeField, HideInInspector]
    RectTransform m_parentRectTransform;

    [SerializeField, Candlelight.PropertyBackingField]
    private DirectionEnum m_direction;
    public DirectionEnum direction
    {
        get
        {
            return m_direction;
        }
        set
        {
            m_direction = value;
            UpdateRectTransform();
        }
    }
    public enum DirectionEnum { BotToTop, TopToBot };
    #endregion

    #region Public Methods
    public void SetLayoutHorizontal()
    {
        UpdateRectTransform();
    }
    public void SetLayoutVertical()
    {
        UpdateRectTransform();
    }
    #endregion

    #region Private Methods
    void Awake()
    {
        m_rectTransform = GetComponent<RectTransform>();
        m_parentRectTransform = transform.parent.GetComponent<RectTransform>();
        UpdateRectTransform();
    }
    void OnEnable()
    {
        m_tracker.Add(this, m_rectTransform, DrivenTransformProperties.All);
    }
    void OnDisable()
    {
        m_tracker.Clear();
    }
    void OnTransformParentChanged()
    {
        m_parentRectTransform = transform.parent.GetComponent<RectTransform>();
    }
    void UpdateRectTransform()
    {
        switch (m_direction)
        {
            case DirectionEnum.BotToTop:
                m_rectTransform.localRotation = Quaternion.AngleAxis(90, Vector3.forward);
                m_rectTransform.anchorMin = Vector2.zero;
                m_rectTransform.anchorMax = Vector2.zero;
                break;
            case DirectionEnum.TopToBot:
                m_rectTransform.localRotation = Quaternion.AngleAxis(-90, Vector3.forward);
                m_rectTransform.anchorMin = Vector2.one;
                m_rectTransform.anchorMax = Vector2.one;
                break;
        }
        m_rectTransform.localScale = Vector3.one;
        m_rectTransform.pivot = new Vector2(0, 1);
        m_rectTransform.sizeDelta = new Vector2(m_parentRectTransform.rect.height, m_parentRectTransform.rect.width);
    }
    #endregion
}