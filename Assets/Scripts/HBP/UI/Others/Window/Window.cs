using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI
{
    public abstract class Window : MonoBehaviour, IClosable, IInteractable
    {
        #region Properties
        UnityEvent m_OnClose = new UnityEvent();
        public virtual UnityEvent OnClose
        {
            get { return m_OnClose; }
            set { m_OnClose = value; }
        }
        protected bool m_Interactable;
        public bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
                SetInteractable(value);
            }
        }
        [SerializeField] protected Button m_CloseButton;
        #endregion

        #region Public Methods
        public virtual void Close()
        {
            OnClose.Invoke();
            Destroy(gameObject);        
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            Initialize();
        }
        protected abstract void SetInteractable(bool interactable);
        protected virtual void Initialize()
        {
            m_CloseButton.onClick.AddListener(Close);
        }
        #endregion
    }
}
