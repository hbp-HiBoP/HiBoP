using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace HBP.UI.Module3D
{
    public class ThresholdHandler : MonoBehaviour, IDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        /// <summary>
        /// RectTransform of this handler
        /// </summary>
        [SerializeField]
        private RectTransform m_RectTransform;
        /// <summary>
        /// RectTransform of the parent of this handler
        /// </summary>
        [SerializeField]
        private RectTransform m_ParentRectTransform;

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
                m_RectTransform.anchorMin = new Vector2(m_Position, m_RectTransform.anchorMin.y);
                m_RectTransform.anchorMax = new Vector2(m_Position, m_RectTransform.anchorMax.y);
            }
        }
        /// <summary>
        /// Minimum position of the handler
        /// </summary>
        public float MinimumPosition { get; set; }
        /// <summary>
        /// Maximum position of the handler
        /// </summary>
        public float MaximumPosition { get; set; }

        /// <summary>
        /// Event called when changing the position of the handler
        /// </summary>
        public UnityEvent OnChangePosition = new UnityEvent();
        #endregion

        #region Public Methods
        /// <summary>
        /// Clamps the position of the handler between min and max
        /// </summary>
        public void ClampPosition()
        {
            Position = Position;
        }
        public void OnDrag(PointerEventData data)
        {
            Vector2 localPosition = new Vector2(0, 0);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ParentRectTransform, data.position, null, out localPosition);
            Position = (localPosition.x / m_ParentRectTransform.rect.width) + 0.5f;
            OnChangePosition.Invoke();
        }
        public void OnPointerEnter(PointerEventData data)
        {
            Cursor.SetCursor(ApplicationState.GeneralSettings.Theme.General.LeftRightCursor.Texture, ApplicationState.GeneralSettings.Theme.General.LeftRightCursor.Offset, CursorMode.ForceSoftware);
        }
        public void OnPointerExit(PointerEventData data)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
        }
        #endregion
    }
}