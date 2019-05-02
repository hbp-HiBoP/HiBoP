using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

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

        protected List<Window> m_SubWindows = new List<Window>();
        #endregion

        #region Public Methods
        public virtual void Close()
        {
            foreach (var subWindow in m_SubWindows.ToArray()) subWindow.Close();
            OnClose.Invoke();
            Destroy(gameObject);
        }
        #endregion

        #region Private Methods
        void Awake()
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