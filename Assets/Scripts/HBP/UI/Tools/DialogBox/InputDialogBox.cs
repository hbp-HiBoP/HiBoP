using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tools
{
    public readonly struct InputDialogResult
    {
        public bool Confirmed { get; }
        public string Value { get; }

        public InputDialogResult(bool confirmed, string value)
        {
            Confirmed = confirmed;
            Value = value;
        }
    }

    public sealed class InputDialogBox : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text message;
        [SerializeField] private InputField input;
        [SerializeField] private Text placeholder;
        [SerializeField] private Button confirm;
        [SerializeField] private Text confirmLabel;
        [SerializeField] private Button cancel;
        [SerializeField] private Text cancelLabel;
        private UniTaskCompletionSource<InputDialogResult> completion;
        private CancellationTokenRegistration cancellation;

        public async UniTask<InputDialogResult> OpenAsync(string titleText, string messageText, string placeholderText, string confirmText, string cancelText, CancellationToken token = default)
        {
            title.text = titleText;
            message.text = messageText;
            placeholder.text = placeholderText;
            confirmLabel.text = confirmText;
            cancelLabel.text = cancelText;
            completion = new UniTaskCompletionSource<InputDialogResult>();
            SynchronizationContext context = SynchronizationContext.Current;
            confirm.onClick.AddListener(() => Complete(true));
            cancel.onClick.AddListener(() => Complete(false));
            cancellation = token.Register(() => context.Post(_ => Complete(false), null));
            input.Select();
            try
            {
                return await completion.Task;
            }
            finally
            {
                cancellation.Dispose();
            }
        }

        public void Close() => Complete(false);

        private void Complete(bool accepted)
        {
            if (completion == null) return;
            if (completion.TrySetResult(new InputDialogResult(accepted, accepted ? input.text : null)))
            {
                input.SetTextWithoutNotify("");
                Destroy(gameObject);
            }
        }

        private void OnDestroy() => completion?.TrySetResult(new InputDialogResult(false, null));
    }
}
