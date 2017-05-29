using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tools.Unity.ResizableGrid
{
    public class HorizontalHandler : Handler
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
                RectTransform handler = GetComponent<RectTransform>();
                handler.anchorMin = new Vector2(handler.anchorMin.x, m_Position);
                handler.anchorMax = new Vector2(handler.anchorMax.x, m_Position);
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            Initialize();
            m_Cursor = Resources.Load("Cursor/horizontal") as Texture2D;
            m_CursorHotSpot = new Vector2(4, 13);
            MinimumPosition = GetComponentInParent<ResizableGrid>().MinimumViewHeight / GetComponentInParent<ResizableGrid>().GetComponent<RectTransform>().rect.height;
            MaximumPosition = 1 - MinimumPosition;
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
            Position = (localPosition.y / GetComponentInParent<ResizableGrid>().GetComponent<RectTransform>().rect.height) + 0.5f;
            OnChangePosition.Invoke();
        }
        #endregion
    }
}