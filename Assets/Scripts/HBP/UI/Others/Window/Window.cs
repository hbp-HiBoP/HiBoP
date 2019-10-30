using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI
{
    public abstract class Window : MonoBehaviour, IClosable, IInteractable
    {
        #region Properties
        protected UnityEvent m_OnClose = new UnityEvent();
        public UnityEvent OnClose
        {
            get { return m_OnClose; }
        }

        protected bool m_Interactable;
        public virtual bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
            }
        }

        [SerializeField] protected SubWindowsManager m_SubWindowsManager = new SubWindowsManager();
        public SubWindowsManager SubWindowsManager { get => m_SubWindowsManager; }
        #endregion

        #region Public Methods
        public virtual void Close()
        {
            SubWindowsManager.CloseAll();
            OnClose.Invoke();
            Destroy(gameObject);
        }
        #endregion

        #region Private Methods
        protected virtual void Awake()
        {
            Initialize();
        }
        protected virtual void Initialize()
        {
            SetFields();
        }
        protected virtual void SetFields()
        {

        }
        #endregion
    }
}