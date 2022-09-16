namespace HBP.Core.Interfaces
{
    public interface ISavable
    {
        UnityEngine.Events.UnityEvent OnSave { get; set; }
        void Save();
    }
}