using UnityEngine;

namespace HBP.UI
{
    public abstract class SubModifier<T> : MonoBehaviour
    {
        #region Properties
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

        protected bool m_IsActive;
        public virtual bool IsActive
        {
            get
            {
                return m_IsActive;
            }
            set
            {
                m_IsActive = value;
                gameObject.SetActive(value);
                if (!Initialized) Initialize();
            }
        }

        protected bool m_Initialized;
        public virtual bool Initialized
        {
            get
            {
                return m_Initialized;
            }
        }

        protected T m_Object;
        public virtual T Object
        {
            get
            {
                return m_Object;
            }
            set
            {
                if (!m_Initialized) Initialize();
                m_Object = value;
            }
        }
        #endregion

        #region Public Methods
        public virtual void Initialize()
        {
            m_Initialized = true;
        }
        #endregion
    }
}