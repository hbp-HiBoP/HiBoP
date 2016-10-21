using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    public class RotationFitter : MonoBehaviour, ILayoutSelfController
    {
        public float height;
        public float distanceToLeft;
        public float distanceToTopBotBoard;
        private RectTransform m_rectTransform;
        private RectTransform m_parentRectTransform;

        void Awake()
        {
            m_rectTransform = transform as RectTransform;
            m_parentRectTransform = transform.parent as RectTransform;
        }

        public void SetLayoutHorizontal()
        {
            m_rectTransform.sizeDelta = new Vector2(m_rectTransform.sizeDelta.x, height);
            m_rectTransform.localPosition = new Vector2(distanceToLeft - (m_parentRectTransform.rect.width / 2.0f), m_rectTransform.localPosition.y);

        }
        public void SetLayoutVertical()
        {
            m_rectTransform.localPosition = new Vector2(m_rectTransform.localPosition.x, 0);
            m_rectTransform.sizeDelta = new Vector2(m_parentRectTransform.rect.height - 2 * distanceToTopBotBoard, m_rectTransform.sizeDelta.y);
        }

        void Update()
        {
            SetLayoutHorizontal();
            SetLayoutVertical();
        }
    }
}