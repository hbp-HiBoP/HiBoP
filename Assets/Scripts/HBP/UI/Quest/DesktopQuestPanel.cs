using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        [SerializeField] private Dropdown devices;
        [SerializeField] private Toggle manual;
        [SerializeField] private Text discoveryStatus, status, selection;
        [SerializeField] private Button pair, send, retry, cancel;
        private byte[] pin, credential;
        private string endpoint, selectedId, store;
        private SceneDelivery offer;
        private PairingSnapshot globals;
        private CancellationTokenSource operation;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private Task running = Task.CompletedTask, discovering = Task.CompletedTask;
        private QuestUsbDiscovery usb;
        private List<QuestDevice> candidates = new List<QuestDevice>();
        private bool busy, scanning, connected, reconnect, failedDelivery, rebuilding;
        private string selectionError;
        private float nextCheck, nextDiscovery, nextHeartbeat;
        public bool IsBusy => busy;

        private void Awake()
        {
            store = Path.Combine(Application.persistentDataPath, "QuestPairings");
            string adb = Path.Combine(Application.streamingAssetsPath, "QuestUsb", "adb.exe");
#if UNITY_EDITOR_WIN
            string sdk = Environment.GetEnvironmentVariable("ANDROID_HOME") ?? @"C:\Android\Sdk";
            adb = Path.Combine(sdk, "platform-tools", "adb.exe");
#endif
            usb = new QuestUsbDiscovery(adb);
            pair.onClick.AddListener(() => _ = PairAsync());
            send.onClick.AddListener(() => _ = SendAsync(false));
            retry.onClick.AddListener(() => _ = SendAsync(true));
            cancel.onClick.AddListener(() =>
            {
                reconnect = false;
                operation?.Cancel();
            });
            address.onValueChanged.AddListener(_ =>
            {
                if (manual.isOn && !busy) ClearPairing();
            });
            manual.onValueChanged.AddListener(_ =>
            {
                if (!busy)
                {
                    ClearPairing();
                    SelectCandidate();
                }
            });
            devices.onValueChanged.AddListener(_ =>
            {
                if (!busy && !rebuilding && !manual.isOn)
                {
                    ClearPairing();
                    SelectCandidate();
                }
            });
            RefreshControls();
        }

        public void TogglePanel() => panel.SetActive(!panel.activeSelf);

        private void ClearPairing()
        {
            if (credential != null) Array.Clear(credential, 0, credential.Length);
            credential = pin = null;
            globals?.Dispose();
            globals = null;
            offer?.Dispose();
            offer = null;
            endpoint = selectedId = null;
            connected = reconnect = failedDelivery = false;
            code.text = "";
            status.text = "Select your Quest, then pair. Enter its code only for the first association.";
        }

        private void SelectCandidate()
        {
            if (manual.isOn)
            {
                endpoint = address.text.Trim();
                return;
            }

            if (devices.value < 0 || devices.value >= candidates.Count) return;
            var selected = candidates[devices.value];
            endpoint = selected.Host;
            selectedId = selected.Id;
            pin = (byte[])selected.Pin.Clone();
            credential = ReadCredential(pin);
            status.text = credential == null ? "Enter the code displayed in this Quest, then click Pair." : "Quest remembered. Click Pair; no code needed.";
        }

        private string CredentialPath(byte[] identity) => Path.Combine(store, BitConverter.ToString(identity).Replace("-", "") + ".pair");

        private byte[] ReadCredential(byte[] identity)
        {
            try
            {
                byte[] saved = PairingStorage.Read(CredentialPath(identity));
                if (saved == null || saved.Length == 32) return saved;
                Array.Clear(saved, 0, saved.Length);
            }
            catch (Exception)
            {
                status.text = "Saved pairing cannot be read. Enter the headset code to pair again.";
            }

            return null;
        }

        public Task PairAsync() =>
            RunAsync(async token =>
            {
                if (manual.isOn && credential == null)
                {
                    endpoint = address.text.Trim();
                    if (endpoint.Contains(":")) throw new ArgumentException("Enter the Quest IPv4 address, without a port.");
                    status.text = "Connecting to Quest...";
                    pin = await QuestPairing.InspectAsync(endpoint, token);
                    credential ??= ReadCredential(pin);
                }

                if (pin == null || string.IsNullOrEmpty(endpoint)) throw new InvalidOperationException("Select an available Quest or enter its IP address.");
                if (credential == null)
                {
                    status.text = "Checking the headset code securely...";
                    credential = await QuestPairing.AuthenticateAsync(endpoint, pin, code.text.Trim(), true, token);
                    // Save before globals: an interrupted installation can resume without another code.
                    try
                    {
                        PairingStorage.Write(CredentialPath(pin), credential);
                    }
                    catch (Exception exception)
                    {
                        throw new InvalidOperationException("The Quest was authenticated, but Windows could not save the pairing. Check access to your user data folder.", exception);
                    }

                    code.text = "";
                }

                reconnect = true;
                await RestoreAsync(token);
            });

        private async Task RestoreAsync(CancellationToken token)
        {
            status.text = "Connecting to remembered Quest...";
            globals ??= PairingSnapshot.Capture();
            await QuestPairing.ResumeAsync(endpoint, pin, credential, globals.Context.Id, token, (stream, stop) => globals.Delivery.SendAsync(stream, stop));
            status.text = "Paired. Preparing the selected visualization for transfer...";
            await DesktopSceneCapture.PrepareSelectedResourcesAsync(token);
            token.ThrowIfCancellationRequested();
            connected = true;
            nextHeartbeat = Time.unscaledTime + 5;
            status.text = "Paired and connected. Select a visualization, then Envoyer au Quest.";
        }

        public Task SendAsync(bool repeat) =>
            RunAsync(async token =>
            {
                if (credential == null || !connected)
                    throw new InvalidOperationException("Pair with the Quest first.");

                failedDelivery = false;
                var elapsed = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    if (!repeat)
                    {
                        offer?.Dispose();
                        offer = null;
                        status.text = "Preparing all visualization resources...";
                        bool capturing = true;
                        var progress = new Progress<string>(message =>
                        {
                            if (capturing && this && !token.IsCancellationRequested && operation != null && operation.Token == token && busy)
                                status.text = message;
                        });
                        try
                        {
                            offer = await DesktopSceneCapture.CaptureDeliverySelectedAsync(Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), 1, globals.Context, token, progress);
                        }
                        finally
                        {
                            capturing = false;
                        }
                    }

                    if (offer == null)
                        throw new InvalidOperationException("No captured delivery to retry. Use Envoyer au Quest.");
                    token.ThrowIfCancellationRequested();
                    status.text = "Connecting to paired Quest...";
                    var delivery = offer;
                    var context = SynchronizationContext.Current;
                    DeliveryReceipt receipt = await QuestPairing.SendAsync(endpoint, pin, credential, token, (stream, stop) => delivery.SendAsync(stream, stop, count => context.Post(_ =>
                    {
                        if (!token.IsCancellationRequested && operation != null && operation.Token == token && busy)
                            status.text = delivery.EncodedBytes == 0 ? $"Sending: {count / 1048576.0:F1} MiB" : count < delivery.EncodedBytes ? $"Sending: {100L * count / delivery.EncodedBytes}%" : "Transfer complete. Waiting for Quest preparation...";
                    }, null)));

                    token.ThrowIfCancellationRequested();
                    status.text = receipt.Status == DeliveryStatus.Published || receipt.Status == DeliveryStatus.AlreadyPublished ? "Visualization ready on Quest. Columns are independent; the headset can work offline." : "This delivery was closed or replaced on Quest. Use Envoyer au Quest for a new snapshot.";
                    if (offer.Summary != null && (receipt.Status == DeliveryStatus.Published || receipt.Status == DeliveryStatus.AlreadyPublished))
                        status.text = offer.Summary + "\nVisualization ready on Quest.";
                    Debug.Log($"QUEST_TRANSFER delivery={receipt.Status}; transfer={offer.TransferId}; hash={receipt.ContentHash}; bytes={offer.EncodedBytes}; retry={repeat}; route={(endpoint != null && endpoint.StartsWith("127.") ? "usb-forward" : "lan")}; totalMs={elapsed.Elapsed.TotalMilliseconds:F1}");
                }
                catch (Exception exception)
                {
                    failedDelivery = offer?.CanRetry == true;
                    if (offer != null && !failedDelivery)
                    {
                        var incomplete = offer;
                        offer = null;
                        await incomplete.DisposeAsync();
                    }

                    connected = false;
                    throw;
                }
            });

        private Task RunAsync(Func<CancellationToken, Task> action)
        {
            if (busy || !isActiveAndEnabled) return Task.CompletedTask;
            busy = true;
            return running = ExecuteAsync(action);
        }

        private async Task ExecuteAsync(Func<CancellationToken, Task> action)
        {
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            operation = attempt;
            try
            {
                await action(attempt.Token);
            }
            catch (Exception exception)
            {
                connected = false;
                if (this && !lifetime.IsCancellationRequested)
                {
                    if (exception is AuthenticationException)
                    {
                        reconnect = false;
                        if (credential != null) Array.Clear(credential, 0, credential.Length);
                        credential = null;
                        status.text = "Pairing refused. Check the Quest code. If expired or locked, press Y in the headset, then Pair again.";
                    }
                    else status.text = attempt.IsCancellationRequested ? "Cancelled. Content already received remains on Quest." : exception is ArgumentException || exception is InvalidOperationException ? exception.Message : reconnect ? "Quest disconnected. Retrying automatically every 5 seconds..." : "Quest unreachable. Open HiBoP in the headset and check USB / Wi-Fi.";
                }
            }
            finally
            {
                operation = null;
                busy = false;
            }
        }

        private async Task DiscoverAsync()
        {
            scanning = true;
            try
            {
                Task<List<QuestDevice>> wifi = QuestDiscovery.FindAsync(lifetime.Token);
                Task<List<QuestDevice>> wired = usb.FindAsync(lifetime.Token);
                var found = new List<QuestDevice>();
                string errors = "";
                try
                {
                    found.AddRange(await wired);
                }
                catch (Exception)
                {
                    errors = "USB discovery unavailable. ";
                }

                try
                {
                    found.AddRange(await wifi);
                }
                catch (Exception)
                {
                    errors += "Wi-Fi discovery unavailable. ";
                }

                if (!this || lifetime.IsCancellationRequested) return;
                // A refresh during an operation must never change its target.
                if (busy) return;
                var next = found.GroupBy(x => x.Id).Select(g => g.First()).OrderBy(x => x.Label).ToList();
                string keep = selectedId;
                rebuilding = true;
                candidates = next;
                devices.ClearOptions();
                devices.AddOptions(next.Count == 0 ? new List<string> { "No Quest detected" } : next.Select(x => x.Label).ToList());
                int index = next.FindIndex(x => x.Id == keep);
                devices.SetValueWithoutNotify(Math.Max(0, index));
                devices.RefreshShownValue();
                rebuilding = false;
                if (!manual.isOn)
                {
                    if (index >= 0) endpoint = next[index].Host;
                    else if (keep == null && next.Count > 0) SelectCandidate();
                    else if (keep != null)
                    {
                        // Retain the chosen identity while absent; never silently switch headsets.
                        devices.ClearOptions();
                        devices.AddOptions(new[] { "Selected Quest unavailable" }.Concat(next.Select(x => x.Label)).ToList());
                        candidates.Insert(0, new QuestDevice { Pin = pin, Name = "Selected Quest unavailable", Host = endpoint });
                        devices.SetValueWithoutNotify(0);
                    }
                }

                discoveryStatus.text = errors + (found.Count == 0 ? "Searching USB and Wi-Fi. Open HiBoP on Quest. Manual IP remains available." : next.Count + " Quest detected. List refreshes automatically.");
            }
            finally
            {
                scanning = false;
                if (this) nextDiscovery = Time.unscaledTime + 5;
            }
        }

        private void Update()
        {
            if (!busy && !scanning && Time.unscaledTime >= nextDiscovery && (panel.activeSelf || reconnect))
            {
                nextDiscovery = Time.unscaledTime + 5;
                discovering = DiscoverAsync();
            }

            if (!busy && !scanning && reconnect && credential != null && Time.unscaledTime >= nextHeartbeat)
            {
                nextHeartbeat = Time.unscaledTime + 5;
                _ = RunAsync(async token =>
                {
                    bool ready = await QuestPairing.PingAsync(endpoint, pin, credential, token);
                    if (!connected || !ready) await RestoreAsync(token);
                });
            }

            if (!panel.activeSelf) return;
            if (Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + 0.5f;
                selectionError = DesktopSceneCapture.GetSelectionError();
                selection.text = selectionError ?? "Selected visualization is ready to send.";
            }

            RefreshControls();
        }

        private void RefreshControls()
        {
            address.gameObject.SetActive(manual.isOn);
            devices.interactable = !busy && !manual.isOn;
            manual.interactable = !busy;
            address.interactable = !busy;
            code.interactable = !busy && credential == null;
            pair.interactable = !busy && !connected && (manual.isOn ? !string.IsNullOrWhiteSpace(address.text) : pin != null) && (credential != null || manual.isOn || code.text.Trim().Length == 6);
            send.interactable = !busy && connected && credential != null && selectionError == null;
            retry.interactable = !busy && connected && credential != null && failedDelivery && offer != null;
            cancel.interactable = busy;
        }

        private void OnDisable() => operation?.Cancel();

        private async void OnDestroy()
        {
            lifetime.Cancel();
            try
            {
                await Task.WhenAll(running, discovering);
            }
            catch (Exception)
            {
            }

            if (credential != null) Array.Clear(credential, 0, credential.Length);
            credential = pin = null;
            globals?.Dispose();
            globals = null;
            offer?.Dispose();
            offer = null;
            lifetime.Dispose();
        }
    }
}
