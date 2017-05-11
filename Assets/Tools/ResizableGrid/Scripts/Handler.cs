using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class OnChangePosition : UnityEvent { }

namespace Tools.Unity.ResizableGrid
{
    public abstract class Handler : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        /// <summary>
        /// Is this handler the last clicked handler ?
        /// </summary>
        public bool IsClicked { get; set; }

        /// <summary>
        /// Texture of the drag cursor
        /// </summary>
        protected Texture2D m_Cursor;
        /// <summary>
        /// Hotspot of the drag cursor
        /// </summary>
        protected Vector2 m_CursorHotSpot;

        public OnChangePosition OnChangePosition = new OnChangePosition();
        #endregion

        #region Private Methods

        #endregion

        #region Public Methods
        /// <summary>
        /// Callback event when dragging the handler
        /// </summary>
        /// <param name="data">Data of the pointer when the event occurs</param>
        public abstract void OnDrag(PointerEventData data);
        /// <summary>
        /// Callback event when clicking on the handler
        /// </summary>
        /// <param name="eventData">Data of the pointer when the event occurs</param>
        public virtual void OnPointerDown(PointerEventData data)
        {
            IsClicked = true;
        }
        /// <summary>
        /// Callback event when releasing the click on the handler
        /// </summary>
        /// <param name="eventData">Data of the pointer when the event occurs</param>
        public virtual void OnPointerUp(PointerEventData data)
        {
            IsClicked = false;
        }
        /// <summary>
        /// Callback event when entering in the handler
        /// </summary>
        /// <param name="eventData">Data of the pointer when the event occurs</param>
        public void OnPointerEnter(PointerEventData data)
        {
            if (!data.dragging)
            {
                Cursor.SetCursor(m_Cursor, m_CursorHotSpot, CursorMode.Auto);
            }
        }
        /// <summary>
        /// Callback event when leaving the handler
        /// </summary>
        /// <param name="eventData">Data of the pointer when the event occurs</param>
        public void OnPointerExit(PointerEventData data)
        {
            if (!data.dragging)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
        #endregion
    }
}