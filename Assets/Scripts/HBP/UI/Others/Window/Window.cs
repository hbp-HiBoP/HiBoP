using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Interfaces;

namespace HBP.UI
{
    /// <summary>
    /// Abstract base class for every window.
    /// </summary>
    public abstract class Window : MonoBehaviour, IClosable, IInteractable
    {
        #region Properties
        [SerializeField] protected UnityEvent m_OnClose;
        /// <summary>
        /// Callback executed when the window is closed.
        /// </summary>
        public UnityEvent OnClose
        {
            get
            {
                return m_OnClose;
            }
        }

        [SerializeField] protected bool m_Interactable = true;
        /// <summary>
        /// Use to enable or disable the ability to select a selectable UI element (for example, a Button).
        /// </summary>
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
        /// <summary>
        /// Children windows referencer.
        /// </summary>
        public WindowsReferencer WindowsReferencer { get => m_WindowsReferencer; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Close the window and all its children.
        /// </summary>
        public virtual void Close()
        {
            WindowsReferencer.CloseAll();
            OnClose.Invoke();
            Destroy(gameObject);
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            Initialize();
        }
        void OnValidate()
        {
            Interactable = Interactable;
        }
        /// <summary>
        /// Called on Awake(). You can override this function and use this to initialize anything needed by your window.
        /// </summary>
        protected virtual void Initialize()
        {
            SetFields();
        }
        /// <summary>
        /// Called on Initialize(). You can override this function and use this to set all the fields of your window.
        /// </summary>
        protected virtual void SetFields()
        {

        }
        #endregion
    }
}