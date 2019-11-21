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

        [SerializeField] protected bool m_Interactable = true;
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

        [SerializeField] protected WindowsReferencer m_WindowsReferencer = new WindowsReferencer();
        public WindowsReferencer WindowsReferencer { get => m_WindowsReferencer; }
        #endregion

        #region Public Methods
        public virtual void Close()
        {
            WindowsReferencer.CloseAll();
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
        void OnValidate()
        {
            Interactable = Interactable;
        }
        #endregion
    }
}