namespace Tools.Unity.Lists
{
    public abstract class SavableItem<T> : SelectableItem<T>
    {
        public abstract void Save();
    }
}

