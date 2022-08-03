using HBP.Theme.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace HBP.UI.Module3D
{
    public class ThresholdHandler : MonoBehaviour, IDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        /// <summary>
        /// Theme element to handle the skin of the cursor
        /// </summary>
        [SerializeField] ThemeElement m_ThemeElement;
        /// <summary>
        /// State to be used for the left-right cursor
        /// </summary>
        [SerializeField] HBP.Theme.State m_LeftRightState;
        /// <summary>
        /// RectTransform of this handler
        /// </summary>
        [SerializeField] private RectTransform m_RectTransform;
        /// <summary>
        /// RectTransform of the parent of this handler
        /// </summary>
        [SerializeField] private RectTransform m_ParentRectTransform;

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
        public GenericEvent<float> OnChangePosition = new GenericEvent<float>();
        #endregion

        #region Public Methods
        public void OnDrag(PointerEventData data)
        {
            float currentPosition = Position;
            Vector2 localPosition = new Vector2(0, 0);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_ParentRectTransform, data.position, null, out localPosition);
            Position = (localPosition.x / m_ParentRectTransform.rect.width) + 0.5f;
            OnChangePosition.Invoke(Position - currentPosition);
        }
        public void OnPointerEnter(PointerEventData data)
        {
            m_ThemeElement.Set(m_LeftRightState);
        }
        public void OnPointerExit(PointerEventData data)
        {
            m_ThemeElement.Set();
        }
        #endregion
    }
}