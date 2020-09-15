using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Tools.Unity.Graph
{
    public class GraphsGridItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Properties
        [SerializeField] private LayoutElement m_LayoutElement;
        [SerializeField] private RectTransform m_RectTransform;

        private GraphsGridContainer m_LastContainer;
        #endregion

        #region Public Methods
        public void OnBeginDrag(PointerEventData eventData)
        {
            m_LastContainer = GetComponentInParent<GraphsGridContainer>();
            m_LayoutElement.ignoreLayout = true;
            m_RectTransform.SetParent(transform.parent.parent);
            m_RectTransform.SetAsLastSibling();
        }
        public void OnDrag(PointerEventData eventData)
        {
            m_RectTransform.position = Input.mousePosition;
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            m_LayoutElement.ignoreLayout = false;
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
        }
        #endregion

        #region Debug
        private void Awake()
        {
            GetComponent<Image>().color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        }
        #endregion
    }
}

