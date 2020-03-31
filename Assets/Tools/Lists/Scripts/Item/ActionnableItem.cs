using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Lists
{
    /// <summary>
    /// Component to display a actionnable item in a list.
    /// </summary>
    /// <typeparam name="T">Type of the object to display</typeparam>
    public abstract class ActionnableItem<T> : SelectableItem<T>
    {
        #region Properties
        /// <summary>
        /// Event called when a item launch a action.
        /// </summary>
        public GenericEvent<int> OnAction { get; } = new GenericEvent<int>();

        [SerializeField] protected bool m_Actionable;
        /// <summary>
        /// True if the item is actionnable.
        /// </summary>
        public bool Actionable
        {
            get
            {
                return m_Actionable;
            }
            set
            {
                m_Actionable = value;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Do the action with the index 0;
        /// </summary>
        public virtual void DoAction()
        {
            if(Actionable) OnAction.Invoke(0);
        }
        /// <summary>
        /// Do the action with a specified index.
        /// </summary>
        /// <param name="index">Index</param>
        public virtual void DoActionType(int index)
        {
            if(Actionable) OnAction.Invoke(index);
        }
        #endregion
    }
}