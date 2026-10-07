using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.Quest
{
    /// <summary>Quest loading presentation; shared tasks and cancellation stay in LoadingManager.</summary>
    public sealed class QuestLoadingCircle : MonoBehaviour, ILoadingPresenter
    {
        [SerializeField] private Image progressRing;
        [SerializeField] private Image brain;
        [SerializeField] private RectTransform informationBox;
        [SerializeField] private Text prefix, information, suffix, loadingEffect;
        [SerializeField] private GameObject cancelContainer;
        [SerializeField] private Button cancel;
        private float progress, targetProgress, startProgress, elapsed, duration;
        private float effectTime;
        private Sprite[] brainFrames;
        private LoadingText message;
        private bool showInformation, cancelling;
        public UnityEvent OnCancel { get; } = new();

        public void Initialize()
        {
            brainFrames = new Sprite[101];
            for (int i = 0; i < brainFrames.Length; i++) brainFrames[i] = Resources.Load<Sprite>("BrainAnim/" + i);
            cancel.onClick.AddListener(Cancel);
            Close();
        }

        public async void Open(bool showInformations = true, bool cancelable = false)
        {
            await UniTask.SwitchToMainThread();
            if (!this) return;
            showInformation = showInformations;
            cancelling = false;
            progress = targetProgress = startProgress = elapsed = duration = 0;
            effectTime = 0;
            message = null;
            gameObject.SetActive(true);
            cancelContainer.SetActive(cancelable);
            Render(0);
        }

        public async void Close()
        {
            await UniTask.SwitchToMainThread();
            if (this) gameObject.SetActive(false);
        }

        public void ChangePercentage(float value, float durationInSeconds, LoadingText text)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            startProgress = progress;
            targetProgress = Mathf.Clamp01(value);
            duration = float.IsNaN(durationInSeconds) || float.IsInfinity(durationInSeconds) ? 0 : Mathf.Max(0, durationInSeconds);
            elapsed = 0;
            message = text;
        }

        private void Update() => Render(Time.unscaledDeltaTime);

        public void Render(float dt)
        {
            elapsed += Mathf.Max(0, dt);
            progress = Mathf.Lerp(startProgress, targetProgress, duration > 0 ? Mathf.Clamp01(elapsed / duration) : 1);
            progressRing.fillAmount = progress;
            brain.sprite = brainFrames[Mathf.Clamp(Mathf.FloorToInt(progress * 100), 0, 100)];
            informationBox.gameObject.SetActive(showInformation || cancelling);
            bool empty = string.IsNullOrWhiteSpace(message?.ToString());
            prefix.text = cancelling ? "Cancelling" : empty ? "Loading" : message.Prefix;
            information.text = cancelling || empty ? "" : message.Message;
            suffix.text = cancelling || empty ? "" : message.Suffix;
            effectTime += Mathf.Max(0, dt);
            loadingEffect.text = new string('.', Mathf.FloorToInt(effectTime / .25f) % 4);
        }

        private void Cancel()
        {
            if (cancelling) return;
            cancelling = true;
            cancelContainer.SetActive(false);
            Render(0);
            OnCancel.Invoke();
        }
    }
}
