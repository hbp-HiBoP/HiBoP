using UnityEngine;

namespace HBP.UI
{
    public abstract class BaseSubModifier : MonoBehaviour
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

        protected object m_Object;
        public virtual object Object
        {
            get
            {
                return m_Object;
            }
            set
            {
                if (!m_Initialized) Initialize();
                m_Object = value;
                SetFields(value);
            }
        }

        public virtual SubWindowsManager SubWindowsManager { get; protected set; } = new SubWindowsManager();
        #endregion

        #region Public Methods
        public virtual void Initialize()
        {
            m_Initialized = true;
        }
        public virtual void Save()
        {

        }
        #endregion

        #region Protected Methods
        protected virtual void SetFields(object objectToDisplay)
        {

        }
        #endregion
    }
    public abstract class SubModifier<T> : BaseSubModifier
    {
        #region Properties
        public new virtual T Object
        {
            get
            {
                return (T) m_Object;
            }
            set
            {
                base.Object = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void SetFields(object objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            SetFields((T) objectToDisplay);
        }
        protected virtual void SetFields(T objectToDisplay)
        {

        }
        #endregion
    }
}