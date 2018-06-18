using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Components
{
    [RequireComponent(typeof(RectTransform))]
    public class OnDontMoveEventTrigger : MonoBehaviour
    {
        #region Properties
        public float Delay = 0.5f; // In seconds.
        public UnityEvent OnDontMove; // Event Called when the event is triggered.
        RectTransform m_RectTransform ; // Zone to listen.
        Coroutine m_Coroutine; // Coroutine.
        #endregion

        #region Private Methods

        #endregion
        void Start()
        {
            m_RectTransform = transform as RectTransform;
        }
        void Update()
        {
            Vector2 mousePostion = Input.mousePosition;
            if(RectTransformUtility.RectangleContainsScreenPoint(m_RectTransform, mousePostion))
            {
                if (m_Coroutine == null)
                {
                    m_Coroutine = StartCoroutine(c_Waiting());
                }
                else
                {
                    if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
                    {
                        StopCoroutine(m_Coroutine);
                        m_Coroutine = null;
                    }
                }
            }
            else
            {
                if(m_Coroutine != null)
                {
                    StopCoroutine(m_Coroutine);
                    m_Coroutine = null;
                }
            }
        }
        void OnDisable()
        {
            if(m_Coroutine != null)
            {
                StopCoroutine(m_Coroutine);
            }
        }
        IEnumerator c_Waiting()
        {
            yield return new WaitForSeconds(Delay);
            OnDontMove.Invoke();
        }
    }
}

