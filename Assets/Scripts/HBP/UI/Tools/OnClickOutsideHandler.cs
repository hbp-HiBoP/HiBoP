using HBP.Input;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Tools
{
    [RequireComponent(typeof(RectTransform))]
    public class OnClickOutsideHandler : MonoBehaviour
    {
        #region Properties

        public UnityEvent OnClick;
        RectTransform m_RectTransform;

        #endregion

        #region Private Methods

        void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
        }

        void Update()
        {
            if (DesktopInput.WasLeftMouseButtonReleasedThisFrame)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(m_RectTransform, DesktopInput.MousePosition))
                {
                    OnClick.Invoke();
                }
            }
        }

        #endregion
    }
}
