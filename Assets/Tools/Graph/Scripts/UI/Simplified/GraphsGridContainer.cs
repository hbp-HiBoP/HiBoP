using UnityEngine;

namespace HBP.UI.Graphs
{
    public class GraphsGridContainer : MonoBehaviour
    {
        #region Properties
        [SerializeField] private GameObject m_Content;
        public GameObject Content
        {
            get
            {
                return m_Content;
            }
            set
            {
                m_Content = value;
                value.transform.SetParent(transform);
                RectTransform rectTransform = value.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = Vector2.zero;
            }
        }
        #endregion
    }
}