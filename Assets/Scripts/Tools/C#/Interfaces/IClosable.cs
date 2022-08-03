namespace HBP.Core.Interfaces
{
    public interface IClosable
    {
        UnityEngine.Events.UnityEvent OnClose { get; }
        void Close();
    }
}