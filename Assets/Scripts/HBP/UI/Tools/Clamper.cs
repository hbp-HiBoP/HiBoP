using UnityEngine;

namespace HBP.UI.Tools
{
    [RequireComponent(typeof(RectTransform))]
    public class Clamper : MonoBehaviour
    {
        #region Properties
        public RectOffset Padding;
        public RectTransform Container;
        public bool AlwaysUpdate;

        RectTransform m_RectTransform;
        bool m_Initialized;
        #endregion

        #region Public Methods
        public void Clamp()
        {
            if (!m_Initialized) Initialize();
            Clamp(m_RectTransform, Container);
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            Initialize();
        }
        void LateUpdate()
        {
            if (AlwaysUpdate) Clamp();
        }
        void Initialize()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_Initialized = true;
        }
        void Clamp(RectTransform rectTransform, RectTransform containerRectTransform)
        {
            Vector3 position = rectTransform.position;
            Rect containerRectPadded = Padding.Remove(containerRectTransform.rect);

            Vector2 minPosition = (Vector2)containerRectTransform.position + containerRectPadded.min - rectTransform.rect.min;
            Vector2 maxPosition = (Vector2)containerRectTransform.position + containerRectPadded.max - rectTransform.rect.max;

            position.x = Mathf.Clamp(rectTransform.position.x, minPosition.x, maxPosition.x);
            position.y = Mathf.Clamp(rectTransform.position.y, minPosition.y, maxPosition.y);

            rectTransform.position = position;
        }
        #endregion
    }
}