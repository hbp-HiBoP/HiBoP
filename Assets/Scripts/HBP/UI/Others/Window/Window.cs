using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI
{
    public abstract class Window : MonoBehaviour
    {
        #region Properties
        protected UnityEvent closeEvent = new UnityEvent { };
        public UnityEvent CloseEvent { get { return closeEvent; } }

        protected UnityEvent openEvent = new UnityEvent { };
        public UnityEvent OpenEvent { get { return openEvent; } }
        #endregion

        #region Public Methods
        public virtual void Open()
        {
            OpenEvent.Invoke();
            SetActive(true);
            SetWindow();
        }
        public virtual void Close()
        {
            CloseEvent.Invoke();
            Destroy(gameObject);        
        }
        public virtual void SetInteractable(bool interactable)
        {

        }
        #endregion

        #region Private Methods
        void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        protected abstract void SetWindow();
        #endregion
    }
}
