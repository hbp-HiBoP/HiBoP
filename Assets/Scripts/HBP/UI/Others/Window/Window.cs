using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI
{
    public abstract class Window : MonoBehaviour
    {
        #region Properties
        public UnityEvent OnOpen;
        public UnityEvent OnClose;
        bool m_Interactable;
        public virtual bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {

            }
        }
        #endregion

        #region Public Methods
        public virtual void Open()
        {
            OnOpen.Invoke();
            Initialize();
            gameObject.SetActive(true);
        }
        public virtual void Close()
        {
            OnClose.Invoke();
            Destroy(gameObject);        
        }
        #endregion

        #region Private Methods
        protected abstract void Initialize();
        #endregion
    }
}
