public interface ILoadable<T>
{
    string GetExtension();
    bool LoadFromFile(string path, out T result);
}
