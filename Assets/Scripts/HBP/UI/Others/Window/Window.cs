using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI
{
    [RequireComponent(typeof(Theme.WindowThemeGestion))]
    public abstract class Window : MonoBehaviour
    {
        #region Properties
        Tools.Unity.Window.WindowGestion windowGestion;

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
            windowGestion.Interactable = interactable;
        }
        #endregion

        #region Private Methods
        void SetActive(bool active)
        {
            if (windowGestion == null) windowGestion = GetComponent<Tools.Unity.Window.WindowGestion>();
            windowGestion.setActive(active);
            windowGestion.Interactable = active;
        }
        protected abstract void SetWindow();
        #endregion
    }
}
