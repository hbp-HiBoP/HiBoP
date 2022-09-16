using UnityEngine.Events;

namespace HBP.Core.Interfaces
{
    public interface ISelectionCountable
    {
        int NumberOfItemSelected { get; }
        UnityEvent OnSelectionChanged { get; }
    }
}