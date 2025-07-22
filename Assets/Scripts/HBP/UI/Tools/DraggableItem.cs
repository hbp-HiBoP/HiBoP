using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.UI.Tools
{
    public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Properties
        private Transform m_OriginalParent;
        private LayoutElement m_LayoutElement;
        private CanvasGroup m_CanvasGroup;
        private RectTransform m_RectTransform;
        private int m_OriginalIndex;
        #endregion

        #region Events
        public GenericEvent<DraggableItem> OnDragStart = new();
        public GenericEvent<DraggableItem, Vector2> OnDragMove = new();
        public GenericEvent<DraggableItem, Vector2> OnDragEnd = new();
        #endregion

        #region Private Methods
        void Awake()
        {
            m_OriginalParent = transform.parent;
            m_CanvasGroup = GetComponent<CanvasGroup>();
            m_RectTransform = GetComponent<RectTransform>();
            m_LayoutElement = GetComponent<LayoutElement>();
        }
        #endregion

        #region Public Methods
        public void OnBeginDrag(PointerEventData eventData)
        {
            m_OriginalParent = transform.parent;
            m_OriginalIndex = transform.GetSiblingIndex();

            m_LayoutElement.ignoreLayout = true;
            m_CanvasGroup.blocksRaycasts = false;
            m_CanvasGroup.alpha = 0.6f;

            transform.SetParent(m_OriginalParent.parent);
            
            OnDragStart.Invoke(this);
        }
        public void OnDrag(PointerEventData eventData)
        {
            m_RectTransform.position = eventData.position;
            OnDragMove.Invoke(this, eventData.position);
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            m_LayoutElement.ignoreLayout = false;
            m_CanvasGroup.blocksRaycasts = true;
            m_CanvasGroup.alpha = 1f;

            transform.SetParent(m_OriginalParent);
            transform.SetSiblingIndex(m_OriginalIndex);

            OnDragEnd.Invoke(this, eventData.position);
        }
        #endregion
    }
}