using HBP.Core.Tools;
using UnityEngine.Events;

namespace HBP.UI.Tools
{
    /// <summary>Visual presentation used by the shared loading service on either platform.</summary>
    public interface ILoadingPresenter
    {
        UnityEvent OnCancel { get; }
        void Initialize();
        void Open(bool showInformations = true, bool cancelable = false);
        void Close();
        void ChangePercentage(float progress, float durationInSeconds, LoadingText message);
    }
}
