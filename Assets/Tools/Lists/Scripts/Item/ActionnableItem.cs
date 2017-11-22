using UnityEngine.Events;

namespace Tools.Unity.Lists
{
    public abstract class ActionnableItem<T> : SelectableItem<T>
    {
        #region Properties
        protected GenericEvent<int> m_OnAction = new GenericEvent<int>();
        public GenericEvent<int> OnAction { get { return m_OnAction; } }
        #endregion

        #region Public Methods
        public virtual void DoAction()
        {
            m_OnAction.Invoke(0);
        }
        public virtual void DoActionType(int i)
        {
            m_OnAction.Invoke(i);
        }
        #endregion
    }
}