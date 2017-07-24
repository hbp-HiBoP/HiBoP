using UnityEngine.Events;

namespace Tools.Unity.Lists
{
    public abstract class ActionnableItem<T> : SelectableItem<T>
    {
        #region Properties
        protected GenericEvent<T, int> m_OnAction = new GenericEvent<T, int>();
        public GenericEvent<T, int> OnAction { get { return m_OnAction; } }
        #endregion

        #region Public Methods
        public virtual void DoAction()
        {
            m_OnAction.Invoke(Object, 0);
        }
        public virtual void DoActionType(int i)
        {
            m_OnAction.Invoke(Object,i);
        }
        #endregion
    }
}