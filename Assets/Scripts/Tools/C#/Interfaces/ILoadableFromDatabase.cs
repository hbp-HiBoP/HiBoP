public interface ILoadableFromDatabase<T>
{
    bool LoadFromDatabase(string path, out T[] result);
}
