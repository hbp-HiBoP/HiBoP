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
        /// <summary>
        /// Reference to parent resizable grid
        /// </summary>
        protected ResizableGrid m_ResizableGrid;

        public OnChangePosition OnChangePosition = new OnChangePosition();
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the handler
        /// </summary>
        protected void Initialize()
        {
            m_ResizableGrid = GetComponentInParent<ResizableGrid>();
        }
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
        /// <param name="data">Data of the pointer when the event occurs</param>
        public virtual void OnPointerDown(PointerEventData data)
        {
            IsClicked = true;
            m_ResizableGrid.IsHandlerClicked = true;
        }
        /// <summary>
        /// Callback event when releasing the click on the handler
        /// </summary>
        /// <param name="data">Data of the pointer when the event occurs</param>
        public virtual void OnPointerUp(PointerEventData data)
        {
            IsClicked = false;
            m_ResizableGrid.IsHandlerClicked = false;
        }
        /// <summary>
        /// Callback event when entering in the handler
        /// </summary>
        /// <param name="data">Data of the pointer when the event occurs</param>
        public void OnPointerEnter(PointerEventData data)
        {
            if (!m_ResizableGrid.IsHandlerClicked)
            {
                Cursor.SetCursor(m_Cursor, m_CursorHotSpot, CursorMode.Auto);
            }
        }
        /// <summary>
        /// Callback event when leaving the handler
        /// </summary>
        /// <param name="data">Data of the pointer when the event occurs</param>
        public void OnPointerExit(PointerEventData data)
        {
            if (!m_ResizableGrid.IsHandlerClicked)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
        #endregion
    }
}