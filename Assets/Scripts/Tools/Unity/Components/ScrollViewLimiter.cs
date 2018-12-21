using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect),typeof(LayoutElement))]
public class ScrollViewLimiter : MonoBehaviour
{
    #region Properties
    ScrollRect m_ScrollRect;
    LayoutElement m_LayoutElement;
    public int Max;
    #endregion

    #region Private Methods
    private void OnEnable()
    {
        m_ScrollRect = GetComponent<ScrollRect>();
        m_LayoutElement = GetComponent<LayoutElement>();
        m_LayoutElement.preferredHeight = Mathf.Min(0, Max);
    }

    private void Update()
    {
        m_LayoutElement.preferredHeight = Mathf.Min(m_ScrollRect.content.rect.height,Max);
    }
    #endregion

}
