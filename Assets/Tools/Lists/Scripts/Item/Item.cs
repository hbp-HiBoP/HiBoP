using UnityEngine;

namespace Tools.Unity.Lists
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

        /// <summary>
        /// True if interactable, False otherwise:
        /// </summary>
        public virtual bool Interactable { get; set; }
        #endregion
    }
}
