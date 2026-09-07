using HBP.Input;
using UnityEngine;

namespace HBP.UI.Tools
{
    [RequireComponent(typeof(RectTransform))]
    public class Dragger : MonoBehaviour
    {
        #region Properties

        Vector3 m_InitialDistanceBetweenMouseAndObject;
        RectTransform m_RectTransform;
        RectTransform m_ParentRectTransform;

        #endregion

        #region Public Methods

        public void OnBeginDrag()
        {
            m_InitialDistanceBetweenMouseAndObject = transform.position - DesktopInput.MousePosition;
        }

        public void OnDrag()
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(m_ParentRectTransform, DesktopInput.MousePosition) && isActiveAndEnabled)
            {
                m_RectTransform.position = DesktopInput.MousePosition + m_InitialDistanceBetweenMouseAndObject;
            }
        }

        #endregion

        #region Private Methods

        void OnTransformParentChanged()
        {
            m_ParentRectTransform = transform.parent.GetComponent<RectTransform>();
        }

        void OnEnable()
        {
            m_RectTransform = GetComponent<RectTransform>();
            if (transform.parent) m_ParentRectTransform = transform.parent.GetComponent<RectTransform>();
        }

        #endregion
    }
}
