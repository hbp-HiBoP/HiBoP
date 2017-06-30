using System.Linq;

namespace Tools.Unity.Lists
{
    public abstract class SelectableListWithSave<T> : SelectableList<T>
    {
        public override T[] Objects
        {
            get { SaveAll(); return m_Objects.ToArray(); }
        }
        public override T[] GetObjectsSelected()
        {
            SaveAll();
            return base.GetObjectsSelected();
        }
        public virtual void SaveAll()
        {
            foreach(T i_object in m_Objects)
            {
                SavableItem<T> savableItem = m_ObjectsToItems[i_object] as SavableItem<T>;
                savableItem.Save();
            }
        }
    }
}