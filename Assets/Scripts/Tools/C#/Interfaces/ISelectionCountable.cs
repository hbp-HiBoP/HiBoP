using UnityEngine.Events;

public interface ISelectionCountable
{
    int NumberOfItemSelected { get; }
    UnityEvent OnSelectionChanged { get; }
}
