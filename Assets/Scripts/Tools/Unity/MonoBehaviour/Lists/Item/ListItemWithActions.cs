namespace Tools.Unity.Lists
{
    public abstract class ListItemWithActions<T> : ListItem<T>
    {
        protected ActionEvent<T> m_action = new ActionEvent<T>();
        public ActionEvent<T> ActionEvent { get { return m_action; } }

        public virtual void DoAction()
        {
            m_action.Invoke(Object, 0);
        }

        public virtual void DoActionType(int i)
        {
            m_action.Invoke(Object,i);
        }


    }
}