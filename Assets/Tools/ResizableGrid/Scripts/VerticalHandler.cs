using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tools.Unity.ResizableGrid
{
    public class VerticalHandler : Handler
    {
        #region Properties
        /// <summary>
        /// Minimum position of the handler
        /// </summary>
        public float MinimumPosition { get; set; }
        /// <summary>
        /// Maximum position of the handler
        /// </summary>
        public float MaximumPosition { get; set; }

        /// <summary>
        /// Threshold to decide when to attract the handler
        /// </summary>
        public float MagneticThreshold { get; set; }
        /// <summary>
        /// Position near which the handler is attracted
        /// </summary>
        public float MagneticPosition { get; set; }

        private float m_Position;
        /// <summary>
        /// Current position of the handler
        /// </summary>
        public float Position
        {
            get
            {
                return m_Position;
            }
            set
            {
                m_Position = Mathf.Clamp(value, MinimumPosition, MaximumPosition);
                if (Mathf.Abs(m_Position - MagneticPosition) < MagneticThreshold && IsClicked)
                {
                    m_Position = MagneticPosition;
                }
                RectTransform handler = GetComponent<RectTransform>();
                handler.anchorMin = new Vector2(m_Position, handler.anchorMin.y);
                handler.anchorMax = new Vector2(m_Position, handler.anchorMax.y);
            }
        }

        public bool IsHovered // FIXME : maybe create another VerticalHandlerUI class to put this method in
        {
            get
            {
                Vector3 mousePosition = Input.mousePosition;
                Rect columnRect = RectTransformToScreenSpace(GetComponent<RectTransform>());
                return mousePosition.x >= columnRect.x && mousePosition.x <= columnRect.x + columnRect.width && mousePosition.y >= columnRect.y && mousePosition.y <= columnRect.y + columnRect.height;
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            Initialize();
            m_Cursor = Resources.Load("Cursor/vertical") as Texture2D;
            m_CursorHotSpot = new Vector2(11, 6);
            MinimumPosition = GetComponentInParent<ResizableGrid>().MinimumViewWidth / GetComponentInParent<ResizableGrid>().GetComponent<RectTransform>().rect.width;
            MaximumPosition = 1 - MinimumPosition;
        }
        /// <summary>
        /// Get RectTransform screen coordinates
        /// </summary>
        /// <param name="transform">Rect Transform to get screen coordinates from</param>
        /// <returns></returns>
        private Rect RectTransformToScreenSpace(RectTransform transform) // FIXME : maybe create another VerticalHandlerUI class to put this method in
        {
            Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
            return new Rect((Vector2)transform.position - (size * 0.5f), size);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Callback event when dragging the handler
        /// </summary>
        /// <param name="data">Data of the pointer when the event occurs</param>
        public override void OnDrag(PointerEventData data)
        {
            Vector2 localPosition = new Vector2(0, 0);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponentInParent<ResizableGrid>().GetComponent<RectTransform>(), data.position, null, out localPosition);
            Position = (localPosition.x / GetComponentInParent<ResizableGrid>().GetComponent<RectTransform>().rect.width) + 0.5f;
            OnChangePosition.Invoke();
        }
        #endregion
    }
}