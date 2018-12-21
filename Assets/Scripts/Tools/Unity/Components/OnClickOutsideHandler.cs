using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Components
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
            if(Input.GetMouseButtonUp(0))
            {
                if(!RectTransformUtility.RectangleContainsScreenPoint(m_RectTransform, Input.mousePosition))
                {
                    OnClick.Invoke();
                }
            }
        }
        #endregion
    }
}

