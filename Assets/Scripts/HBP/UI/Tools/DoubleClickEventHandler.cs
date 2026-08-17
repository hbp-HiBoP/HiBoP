using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Tools
{
    [RequireComponent(typeof(Selectable))]
    public class DoubleClickEventHandler : MonoBehaviour, IPointerClickHandler
    {
        #region Properties

        public UnityEvent OnSimpleClick;
        public UnityEvent OnDoubleClick;

        private float m_DelayBetweenClicks = 0.3f;
        private float m_LastClickTime = 0f;
        private bool m_IsSecondClick = false;
        private Selectable m_Selectable;

        #endregion

        #region Public Methods

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isActiveAndEnabled && m_Selectable.interactable)
            {
                float currentTime = Time.time;
                if (m_IsSecondClick && (currentTime - m_LastClickTime <= m_DelayBetweenClicks))
                {
                    // Double click detected
                    m_IsSecondClick = false;
                    OnDoubleClick.Invoke();
                }
                else
                {
                    // Possible first click
                    m_IsSecondClick = true;
                    m_LastClickTime = currentTime;
                    Invoke(nameof(HandleSingleClick), m_DelayBetweenClicks);
                }
            }
        }

        #endregion

        #region Private Methods

        void OnEnable()
        {
            m_Selectable = GetComponent<Selectable>();
        }

        private void HandleSingleClick()
        {
            if (m_IsSecondClick)
            {
                // If no second click happened in the delay, treat as single click
                m_IsSecondClick = false;
                OnSimpleClick.Invoke();
            }
        }

        #endregion
    }
}
