using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Lists
{
    public abstract class ActionnableItem<T> : SelectableItem<T>
    {
        #region Properties
        public GenericEvent<int> OnAction { get; } = new GenericEvent<int>();

        [SerializeField] protected bool m_Actionable;
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
        public virtual void DoAction()
        {
            if(Actionable) OnAction.Invoke(0);
        }
        public virtual void DoActionType(int i)
        {
            if(Actionable) OnAction.Invoke(i);
        }
        #endregion
    }
}