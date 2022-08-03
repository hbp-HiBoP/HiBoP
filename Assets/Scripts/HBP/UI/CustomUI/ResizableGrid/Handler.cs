using HBP.Theme.Components;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Tools.Unity.ResizableGrid
{
    public abstract class Handler : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        /// <summary>
        /// Is this handler the last clicked handler ?
        /// </summary>
        public bool IsClicked { get; set; }
        /// <summary>
        /// Reference to parent resizable grid
        /// </summary>
        protected ResizableGrid m_ResizableGrid;

        [SerializeField] protected ThemeElement m_ThemeElement;

        public UnityEvent OnChangePosition = new UnityEvent();
        #endregion

        #region Private Methods
        protected float RoundAtPrecision(float number, float precision)
        {
            if (precision > 1.0f || precision <= 0.0f) return number;
            return precision * Mathf.Round(number / precision);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the handler
        /// </summary>
        public virtual void Initialize(ResizableGrid resizableGrid)
        {
            m_ResizableGrid = resizableGrid;
        }
        /// <summary>
        /// Callback event when dragging the handler
        /// </summary>
        /// <param name="data">Data of the pointer when the event occurs</param>
        public abstract void OnDrag(PointerEventData data);
        /// <summary>
        /// Callback event when ending the drag of the handler
        /// </summary>
        /// <param name="data">Data of the pointer when the event occurs</param>
        public void OnEndDrag(PointerEventData data)
        {
            m_ThemeElement.Set();
        }
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
        public abstract void OnPointerEnter(PointerEventData data);
        /// <summary>
        /// Callback event when leaving the handler
        /// </summary>
        /// <param name="data">Data of the pointer when the event occurs</param>
        public void OnPointerExit(PointerEventData data)
        {
            if (!m_ResizableGrid.IsHandlerClicked)
            {
                m_ThemeElement.Set();
            }
        }
        #endregion
    }
}