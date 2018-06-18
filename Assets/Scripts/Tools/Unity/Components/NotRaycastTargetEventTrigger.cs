using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Tools.Unity.Components
{
    public class NotRaycastTargetEventTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        public UnityEvent OnPointerEnter;
        public UnityEvent OnPointerExit;
        #endregion

        #region Private Methods
        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            OnPointerEnter.Invoke();
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            OnPointerExit.Invoke();
        }
        #endregion
    }
}