using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HBP.UI.Informations.Graphs
{
    public class GraphsGridItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Properties
        [SerializeField] private RectTransform m_RectTransform;
        [SerializeField] private Selectable[] m_Selectables;

        private GraphsGridContainer m_LastContainer;
        #endregion

        #region Public Methods
        public void OnBeginDrag(PointerEventData eventData)
        {
            m_LastContainer = GetComponentInParent<GraphsGridContainer>();
            m_RectTransform.SetParent(transform.parent.parent);
            m_RectTransform.SetAsLastSibling();
            foreach (var selectable in m_Selectables)
            {
                selectable.interactable = false;
            }
        }
        public void OnDrag(PointerEventData eventData)
        {
            m_RectTransform.position = Input.mousePosition;
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            bool foundContainer = false;
            foreach (RaycastResult result in results)
            {
                GraphsGridContainer container = result.gameObject.GetComponent<GraphsGridContainer>();
                if (container != null)
                {
                    if (container.Content != null)
                        m_LastContainer.Content = container.Content;
                    container.Content = gameObject;
                    foundContainer = true;
                    break;
                }
            }
            if (!foundContainer)
            {
                m_LastContainer.Content = gameObject;
            }
            foreach (var selectable in m_Selectables)
            {
                selectable.interactable = true;
            }
        }
        #endregion
    }
}

