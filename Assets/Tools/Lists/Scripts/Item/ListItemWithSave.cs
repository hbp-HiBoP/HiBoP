namespace Tools.Unity.Lists
{
    public abstract class ListItemWithSave<T> : ListItem<T>
    {
        public abstract void Save();
    }
}

