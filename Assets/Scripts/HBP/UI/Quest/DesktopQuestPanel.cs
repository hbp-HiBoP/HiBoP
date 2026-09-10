using System;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Quest
{
    public sealed class DesktopQuestPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private InputField address, code;
        [SerializeField] private Text fingerprint, status, selection;
        [SerializeField] private Toggle confirmed;
        [SerializeField] private Button inspect, pair, send, retry, cancel;
        private byte[] pin, credential;
        private SceneDelivery offer;
        private PairingSnapshot globals;
        private CancellationTokenSource operation;
        private bool busy;
        private bool failedDelivery;
        private string selectionError;
        private float nextCheck;
        public bool IsBusy => busy;

        private void Awake()
        {
            inspect.onClick.AddListener(() => _ = InspectAsync());
            pair.onClick.AddListener(() => _ = PairAsync());
            send.onClick.AddListener(() => _ = SendAsync(false));
            retry.onClick.AddListener(() => _ = SendAsync(true));
            cancel.onClick.AddListener(() => operation?.Cancel());
            address.onValueChanged.AddListener(_ => ClearPairing());
        }

        public void TogglePanel() => panel.SetActive(!panel.activeSelf);

        private void ClearPairing()
        {
            if (credential != null) Array.Clear(credential, 0, credential.Length);
            credential = pin = null;
            globals?.Dispose();
            globals = null;
            confirmed.isOn = false;
            fingerprint.text = "Compare the complete fingerprint with the headset.";
            offer?.Dispose();
            offer = null;
            failedDelivery = false;
        }

        public Task InspectAsync() =>
            RunAsync(async token =>
            {
                ClearPairing();
                status.text = "Connecting to Quest...";
                pin = await QuestPairing.InspectAsync(address.text.Trim(), token);
                token.ThrowIfCancellationRequested();
                fingerprint.text = QuestPairing.FormatPin(pin);
                status.text = "Compare every fingerprint group in the headset, then confirm and enter its code.";
            });

        public Task PairAsync() =>
            RunAsync(async token =>
            {
                if (!confirmed.isOn || pin == null) throw new InvalidOperationException("Compare and confirm the complete fingerprint first.");
                status.text = "Preparing global preferences, protocols and tags...";
                var snapshot = PairingSnapshot.Capture();
                byte[] next = null;
                try
                {
                    next = await QuestPairing.PairAsync(address.text.Trim(), pin, code.text.Trim(), token, (stream, stop) => snapshot.Delivery.SendAsync(stream, stop));
                    token.ThrowIfCancellationRequested();
                }
                catch
                {
                    if (next != null) Array.Clear(next, 0, next.Length);
                    snapshot.Dispose();
                    throw;
                }

                globals?.Dispose();
                globals = snapshot;
                credential = next;
                code.text = "";
                status.text = "Paired. Select a visualization, then Envoyer au Quest.";
            });

        public Task SendAsync(bool repeat) =>
            RunAsync(async token =>
            {
                if (credential == null) throw new InvalidOperationException("Pair with the Quest first.");
                failedDelivery = false;
                var elapsed = System.Diagnostics.Stopwatch.StartNew();
                double captureMs = 0;
                try
                {
                    if (!repeat)
                    {
                        offer?.Dispose();
                        offer = null;
                        status.text = "Preparing all visualization resources...";
                        offer = await DesktopSceneCapture.CaptureDeliverySelectedAsync(Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), 1, globals.Context, token);
                        captureMs = elapsed.Elapsed.TotalMilliseconds;
                    }

                    if (offer == null) throw new InvalidOperationException("No captured delivery to retry. Use Envoyer au Quest.");
                    token.ThrowIfCancellationRequested();
                    status.text = "Connecting to paired Quest...";
                    var delivery = offer;
                    var context = SynchronizationContext.Current;
                    DeliveryReceipt receipt = await QuestPairing.SendAsync(address.text.Trim(), pin, credential, token, (stream, stop) => delivery.SendAsync(stream, stop, count => context.Post(_ =>
                    {
                        if (!token.IsCancellationRequested && operation != null && operation.Token == token && busy) status.text = count < delivery.EncodedBytes ? $"Sending: {100L * count / delivery.EncodedBytes}%" : "Transfer complete. Waiting for Quest preparation...";
                    }, null)));
                    token.ThrowIfCancellationRequested();
                    status.text = receipt.Status == DeliveryStatus.Published || receipt.Status == DeliveryStatus.AlreadyPublished ? "Visualization ready on Quest. Columns are independent; the headset can work offline." : "This delivery was closed or replaced on Quest. Use Envoyer au Quest for a new snapshot.";
                    if (offer.Summary != null && (receipt.Status == DeliveryStatus.Published || receipt.Status == DeliveryStatus.AlreadyPublished)) status.text = offer.Summary + "\nVisualization ready on Quest.";
                    Debug.Log($"QUEST-011 delivery={receipt.Status}; hash={receipt.ContentHash}; bytes={offer.EncodedBytes}");
                    if (Debug.isDebugBuild)
                        Debug.Log("QUEST012_SEND " + JsonUtility.ToJson(new DeliveryMeasurement { utc = DateTime.UtcNow.ToString("O"), retry = repeat, transfer = offer.TransferId, hash = receipt.ContentHash, bytes = offer.EncodedBytes, captureAndEncodeMs = captureMs, connectSendAndReceiptMs = elapsed.Elapsed.TotalMilliseconds - captureMs, totalMs = elapsed.Elapsed.TotalMilliseconds, status = receipt.Status.ToString() }));
                }
                catch
                {
                    failedDelivery = offer != null;
                    throw;
                }
            });

        private async Task RunAsync(Func<CancellationToken, Task> action)
        {
            if (busy || !isActiveAndEnabled) return;
            busy = true; // Guard before the first await, including direct/double calls.
            using var attempt = new CancellationTokenSource();
            operation = attempt;
            try
            {
                await action(attempt.Token);
            }
            catch (Exception exception)
            {
                if (this) status.text = attempt.IsCancellationRequested ? "Cancelled. A published visualization remains on Quest; retry is safe." : exception is AuthenticationException ? "Pairing/security check failed. Verify the fingerprint and code; Y on Quest starts a new pairing." : exception is SocketException ? "Quest unreachable. Check the address and Wi-Fi, then retry." : exception is ArgumentException || exception is InvalidOperationException ? exception.Message : "Connection or preparation failed. Check Quest and retry. If pairing was interrupted, press Y in the headset.";
            }
            finally
            {
                operation = null;
                busy = false;
                if (!this)
                {
                    offer?.Dispose();
                    offer = null;
                }
            }
        }

        [Serializable]
        private sealed class DeliveryMeasurement
        {
            public string utc, transfer, hash, status;
            public bool retry;
            public long bytes;
            public double captureAndEncodeMs, connectSendAndReceiptMs, totalMs;
        }

        private void Update()
        {
            if (!panel.activeSelf) return;
            if (Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + 0.5f;
                selectionError = DesktopSceneCapture.GetSelectionError();
                selection.text = selectionError ?? "Selected visualization is ready to send.";
            }

            address.interactable = code.interactable = confirmed.interactable = !busy;
            inspect.interactable = !busy && !string.IsNullOrWhiteSpace(address.text);
            pair.interactable = !busy && pin != null && confirmed.isOn && credential == null && code.text.Trim().Length == 6;
            send.interactable = !busy && credential != null && selectionError == null;
            retry.interactable = !busy && credential != null && failedDelivery && offer != null;
            cancel.interactable = busy;
        }

        private void OnDisable() => operation?.Cancel();

        private void OnDestroy()
        {
            operation?.Cancel();
            if (credential != null) Array.Clear(credential, 0, credential.Length);
            credential = pin = null;
            globals?.Dispose();
            globals = null;
            offer?.Dispose();
            offer = null;
        }
    }
}
