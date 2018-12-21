public interface ILoadable
{
    string GetExtension();
    void Load(string path);
}
