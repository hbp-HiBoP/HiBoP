namespace HBP.Core.Interfaces
{
    public interface ILoadable<T>
    {
        string[] GetExtensions();
        bool LoadFromFile(string path, out T[] result);
    }
}