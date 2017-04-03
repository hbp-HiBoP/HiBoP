using System.Linq;

namespace Tools.Unity.Lists
{
    public abstract class SelectableListWithSave<T> : SelectableList<T>
    {
        public override T[] Objects
        {
            get { SaveAll(); return m_objects.ToArray(); }
        }
        public override T[] GetObjectsSelected()
        {
            SaveAll();
            return base.GetObjectsSelected();
        }
        public virtual void SaveAll()
        {
            foreach(T i_object in m_objects)
            {
                ListItemWithSave<T> l_listItemWithSave = m_objectsToItems[i_object] as ListItemWithSave<T>;
                l_listItemWithSave.Save();
            }
        }
    }
}