using UnityEngine;

namespace Tools.Unity.Components
{
    [RequireComponent(typeof(RectTransform))]
    public class MousePositionAndClamp : MonoBehaviour
    {

        #region Properties
        public Vector2 BottomRightOffset;
        public Vector2 BottomLeftOffset;
        public Vector2 TopRightOffset;
        public Vector2 TopLeftOffset;

        public RectOffset Padding;
        public RectTransform Container;
        public bool AlwaysUpdate;

        RectTransform m_RectTransform;
        CanvasScalerHandler m_CanvasScalerHandler;
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
        void Update()
        {
            if (AlwaysUpdate) Clamp();
        }
        void Initialize()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_CanvasScalerHandler = GetComponentInParent<CanvasScalerHandler>();
            m_Initialized = true;
        }
        void Clamp(RectTransform rectTransform, RectTransform containerRectTransform)
        {
            Vector2 mousePosition = Input.mousePosition;
            float scale = m_CanvasScalerHandler.Scale;
            Vector2 scaledMousePosition = new Vector2(scale * Input.mousePosition.x, scale * Input.mousePosition.y);
            Vector2 containerScaledPosition = new Vector2(scale * containerRectTransform.position.x, scale * containerRectTransform.position.y);

            Rect containerRectPadded = Padding.Remove(containerRectTransform.rect);
            Vector2 containerMinPosition = containerScaledPosition + containerRectPadded.min;
            Vector2 containerMaxPosition = containerScaledPosition + containerRectPadded.max;

            // Test bottom-right.
            float xMax = scaledMousePosition.x + rectTransform.rect.width  + BottomRightOffset.x;
            float yMin = scaledMousePosition.y - rectTransform.rect.height + BottomRightOffset.y;

            if(xMax < containerMaxPosition.x && yMin > containerMinPosition.y) // Bottom-right.
            {
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.position = mousePosition + BottomRightOffset;
            }
            if(xMax >= containerMaxPosition.x &&  yMin > containerMinPosition.y) // Bottom-left.
            {
                rectTransform.pivot = new Vector2(1, 1);
                rectTransform.position = mousePosition + BottomLeftOffset;
            }
            if (xMax >= containerMaxPosition.x && yMin <= containerMinPosition.y) // Top-left.
            {
                rectTransform.pivot = new Vector2(1, 0);
                rectTransform.position = mousePosition + TopLeftOffset;
            }
            if (xMax < containerMaxPosition.x && yMin <= containerMinPosition.y) // Top-right.
            {
                rectTransform.pivot = new Vector2(0, 0);
                rectTransform.position = mousePosition + TopRightOffset;
            }
        }
        #endregion
    }
}