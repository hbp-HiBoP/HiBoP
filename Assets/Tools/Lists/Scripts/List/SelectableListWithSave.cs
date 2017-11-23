namespace Tools.Unity.Lists
{
    public abstract class SelectableListWithSave<T> : SelectableList<T>
    {
        #region Properties
        public override T[] Objects
        {
            get { SaveAll(); return base.Objects; }
            set { base.Objects = value; }
        }
        public override T[] ObjectsSelected
        {
            get { SaveAll(); return base.ObjectsSelected; }
            set { base.ObjectsSelected = value; }
        }
        #endregion

        #region Public Methods
        public virtual void SaveAll()
        {
            //foreach (var couple in m_ObjectsToItems) (couple.Value as SavableItem<T>).Save();
        }
        #endregion
    }
}