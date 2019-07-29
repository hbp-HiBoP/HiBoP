public interface ILoadableFromDatabase<T>
{
    T[] LoadFromDatabase(string path);
}
