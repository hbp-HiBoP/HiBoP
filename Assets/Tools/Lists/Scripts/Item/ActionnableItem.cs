namespace Tools.Unity.Lists
{
    public abstract class ActionnableItem<T> : SelectableItem<T>
    {
        #region Properties
        protected ActionEvent<T> m_Action = new ActionEvent<T>();
        public ActionEvent<T> Action { get { return m_Action; } }
        #endregion

        #region Public Methods
        public virtual void DoAction()
        {
            m_Action.Invoke(Object, 0);
        }
        public virtual void DoActionType(int i)
        {
            m_Action.Invoke(Object,i);
        }
        #endregion
    }
}