namespace Tools.Unity.Lists
{
    public class OneSelectableListWithItemActions<T> : OneSelectableList<T>
    {
        protected ActionEvent<T> m_action = new ActionEvent<T>();
        public ActionEvent<T> ActionEvent { get { return m_action; } }

        protected override void Set(T objectToSet, ListItem<T> listItem )
        {
            base.Set(objectToSet, listItem);
            ListItemWithActions<T> l_listItemWithActions = (ListItemWithActions<T>)listItem;
            l_listItemWithActions.ActionEvent.RemoveAllListeners();
            l_listItemWithActions.ActionEvent.AddListener((obj, i) => m_action.Invoke(obj, i));
        }
    }
}