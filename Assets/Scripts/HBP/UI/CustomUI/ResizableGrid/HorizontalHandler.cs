using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tools.Unity.ResizableGrid
{
    public class HorizontalHandler : Handler
    {
        #region Properties
        [SerializeField] protected State m_HorizontalState;

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
                m_Position = RoundAtPrecision(m_Position, 2 / m_ResizableGrid.RectTransform.rect.height);
                RectTransform handler = GetComponent<RectTransform>();
                handler.anchorMin = new Vector2(handler.anchorMin.x, m_Position);
                handler.anchorMax = new Vector2(handler.anchorMax.x, m_Position);
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize(ResizableGrid resizableGrid)
        {
            base.Initialize(resizableGrid);
            MinimumPosition = m_ResizableGrid.MinimumViewHeight / m_ResizableGrid.RectTransform.rect.height;
            MaximumPosition = 1 - MinimumPosition;
        }
        /// <summary>
        /// Callback event when clicking on the handler
        /// </summary>
        /// <param name="eventData">Data of the pointer when the event occurs</param>
        public override void OnPointerDown(PointerEventData data)
        {
            base.OnPointerDown(data);
            m_ThemeElement.Set(m_HorizontalState);
        }
        /// <summary>
        /// Callback event when entering in the handler
        /// </summary>
        /// <param name="data">Data of the pointer when the event occurs</param>
        public override void OnPointerEnter(PointerEventData data)
        {
            if (!m_ResizableGrid.IsHandlerClicked)
            {
                m_ThemeElement.Set(m_HorizontalState);
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
            Position = (localPosition.y / m_ResizableGrid.RectTransform.rect.height) + 0.5f;
            OnChangePosition.Invoke();
        }
        #endregion
    }
}