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
        public float MinimumPosition { get; set; } // NaN BUG FIXME
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
                m_Position = RoundAtPrecision(m_Position, 2 / m_ResizableGrid.RectTransform.rect.width);
                RectTransform handler = GetComponent<RectTransform>();
                handler.anchorMin = new Vector2(m_Position, handler.anchorMin.y);
                handler.anchorMax = new Vector2(m_Position, handler.anchorMax.y);
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            Initialize();
            MinimumPosition = m_ResizableGrid.MinimumViewWidth / m_ResizableGrid.RectTransform.rect.width;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Callback event when clicking on the handler
        /// </summary>
        /// <param name="eventData">Data of the pointer when the event occurs</param>
        public override void OnPointerDown(PointerEventData data)
        {
            base.OnPointerDown(data);
            Cursor.SetCursor(ApplicationState.GeneralSettings.Theme.General.LeftRightCursor.Texture, ApplicationState.GeneralSettings.Theme.General.LeftRightCursor.Offset, CursorMode.Auto);
        }
        /// <summary>
        /// Callback event when entering in the handler
        /// </summary>
        /// <param name="data">Data of the pointer when the event occurs</param>
        public override void OnPointerEnter(PointerEventData data)
        {
            if (!m_ResizableGrid.IsHandlerClicked)
            {
                Cursor.SetCursor(ApplicationState.GeneralSettings.Theme.General.LeftRightCursor.Texture, ApplicationState.GeneralSettings.Theme.General.LeftRightCursor.Offset, CursorMode.Auto);
            }
        }
        /// <summary>
        /// Callback event when dragging the handler
        /// </summary>
        /// <param name="data">Data of the pointer when the event occurs</param>
        public override void OnDrag(PointerEventData data)
        {
            Vector2 localPosition = new Vector2(0, 0);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ResizableGrid.RectTransform, data.position, null, out localPosition);
            Position = (localPosition.x / m_ResizableGrid.RectTransform.rect.width) + 0.5f;
            OnChangePosition.Invoke();
        }
        #endregion
    }
}