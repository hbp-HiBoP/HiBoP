using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Tools.Unity.Components
{
	public class DoubleClickEventHandler : MonoBehaviour //, IPointerClickHandler
	{
        #region Properties
        public float DelayBetweenClick = 0.1f;
        public UnityEvent OnSimpleClick;
        public UnityEvent OnDoubleClick;

        bool m_IsSecondClick = false;
		bool m_IsWaiting = false;
        #endregion

        #region Public Methods
        public void Click()
        {
            if (!m_IsWaiting)
            {
                m_IsWaiting = true;
                StartCoroutine(c_WaitForClick(DelayBetweenClick));
            }
            else
            {
                m_IsSecondClick = true;
            }
        }
  //      public void OnPointerClick(PointerEventData eventData)
		//{
  //          if (!m_IsWaiting)
  //          {
  //              m_IsWaiting = true;
  //              StartCoroutine(c_WaitForClick(DelayBetweenClick));
  //          }
  //          else
  //          {
  //              m_IsSecondClick = true;
  //          }
  //      }
        #endregion

        #region Private Methods		
		IEnumerator c_WaitForClick(float delay) 
		{
			yield return new WaitForSeconds(delay);
            if (m_IsSecondClick == true && m_IsWaiting == true) OnDoubleClick.Invoke();
            else OnSimpleClick.Invoke();
			m_IsWaiting=false;
			m_IsSecondClick=false;
		}
        #endregion
    }
}


