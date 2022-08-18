using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Tools
{
    public class ImageResizer : MonoBehaviour, IScrollHandler, IPointerDownHandler
    {
        #region Properties
        public enum ResizingType { Height, Width, Both }
        public ResizingType Type;
        public float Minimum;
        public float Maximum;
        public int Step;

        private RectTransform m_RectTransform;
        private Vector2 m_InitialAnchoredPosition;
        private Vector2 m_InitialSizeDelta;
        #endregion

        #region Public Methods
        public void OnScroll(PointerEventData data)
        {
            Scroll(data.scrollDelta);
        }
        public void OnPointerDown(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Right)
            {
                ResetPosition();
            }
        }
        public void Scroll(Vector2 scroll)
        {
            float height, width;
            switch (Type)
            {
                case ResizingType.Height:
                    height = m_RectTransform.sizeDelta.y + (Step * scroll.y);
                    if (height < Minimum) height = Minimum;
                    if (height > Maximum) height = Maximum;
                    m_RectTransform.sizeDelta = new Vector2(m_RectTransform.sizeDelta.x, height);
                    break;
                case ResizingType.Width:
                    width = m_RectTransform.sizeDelta.x + (Step * scroll.y);
                    if (width < Minimum) width = Minimum;
                    if (width > Maximum) width = Maximum;
                    m_RectTransform.sizeDelta = new Vector2(width, m_RectTransform.sizeDelta.y);
                    break;
                case ResizingType.Both:
                    height = m_RectTransform.sizeDelta.y + (Step * scroll.y);
                    width = m_RectTransform.sizeDelta.x + (Step * scroll.y);
                    if (height < Minimum) height = Minimum;
                    if (height > Maximum) height = Maximum;
                    if (width < Minimum) width = Minimum;
                    if (width > Maximum) width = Maximum;
                    m_RectTransform.sizeDelta = new Vector2(width, height);
                    break;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_RectTransform);
        }
        public void ResetPosition()
        {
            m_RectTransform.anchoredPosition = m_InitialAnchoredPosition;
            m_RectTransform.sizeDelta = m_InitialSizeDelta;
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_RectTransform);
        }
        #endregion

        #region Private Methods
        public void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_InitialAnchoredPosition = m_RectTransform.anchoredPosition;
            m_InitialSizeDelta = m_RectTransform.sizeDelta;
        }
        #endregion
    }
}