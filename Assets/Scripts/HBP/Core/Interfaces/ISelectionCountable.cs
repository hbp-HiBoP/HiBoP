using UnityEngine.Events;

namespace HBP.Core.Interfaces
{
    public interface ISelectionCountable
    {
        int NumberOfSelectedObjects { get; }
        int NumberOfObjects { get; }
        int NumberOfFilteredObjects { get; }
        UnityEvent OnSelectionChanged { get; }

        bool CanSelectMultipleObjects { get; }
    }
}