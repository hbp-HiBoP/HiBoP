using UnityEngine;

namespace HBP.UI.Tools.Lists
{
    /// <summary>
    /// Abstract component to display item in a list.
    /// </summary>
    /// <typeparam name="T">Type of object to display</typeparam>
    public abstract class Item<T> : MonoBehaviour
    {
        #region Properties

        protected T m_Object;

        /// <summary>
        /// Object to display.
        /// </summary>
        public virtual T Object
        {
            get { return m_Object; }
            set { m_Object = value; }
        }

        private bool m_Interactable;

        /// <summary>
        /// True if interactable, False otherwise:
        /// </summary>
        public virtual bool Interactable
        {
            get { return m_Interactable; }
            set
            {
                m_Interactable = value;
                if (m_Object != null) Object = m_Object;
            }
        }

        [SerializeField] private Theme.State m_InteractableState;
        [SerializeField] private Theme.State m_NotInteractableState;
        [SerializeField] private Theme.ThemeElement[] m_InteractableElements;

        #endregion

        #region Private Methods

        protected void SetInteractable()
        {
            foreach (var element in m_InteractableElements)
            {
                element.Set(m_InteractableState);
            }
        }

        protected void SetNotInteractable()
        {
            if (!Interactable)
            {
                foreach (var element in m_InteractableElements)
                {
                    element.Set(m_NotInteractableState);
                }
            }
        }

        #endregion
    }
}
