public interface IClosable
{
    UnityEngine.Events.UnityEvent OnClose { get; set; }
    void Close();
}
