using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using HBP.Sync.Scene;
using HBP.Sync;
using HBP.UI.Quest;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.Quest.Desktop
{
    /// <summary>Owns the Desktop Quest connection independently of disposable windows.</summary>
    public sealed class QuestManager : Manager<QuestManager>
    {
        public static QuestManager Instance => m_Instance;
        public event Action Changed;

        public IReadOnlyList<QuestDevice> Devices => devices;
        public IReadOnlyList<string> DiscoveryMessages { get; private set; } = new[] { "Searching for Quests on USB and Wi-Fi..." };
        public string Status { get; private set; } = "Select a Quest to pair.";
        public string SelectedId { get; private set; }
        public bool IsBusy => busy;
        public bool IsPaired => connected && credential != null;
        public bool CanRetry => offer?.CanRetry == true && failedDelivery && IsPaired;
        public bool HasRetryableDelivery => offer?.CanRetry == true && failedDelivery;

        private byte[] pin, credential;
        private string endpoint, store;
        private SceneDelivery offer;
        private DesktopReplicaSession replica;
        private DesktopV2ReplicaSession publicationReplica;
        private PairingSnapshot globals;
        private CancellationTokenSource operation;
        private readonly CancellationTokenSource lifetime = new();
        private Task running = Task.CompletedTask, discovering = Task.CompletedTask;
        private QuestUsbDiscovery usb;
        private List<QuestDevice> devices = new();
        private bool busy, scanning, connected, reconnect, failedDelivery, discoveryActive;
        private bool manualSelected;
        private float nextDiscovery, nextHeartbeat;
        private long nextTransferCaptureGeneration;

        protected override void Initialization()
        {
            base.Initialization();
            store = Path.Combine(Application.persistentDataPath, "QuestPairings");
            string adb = Path.Combine(Application.streamingAssetsPath, "QuestUsb", "adb.exe");
#if UNITY_EDITOR_WIN
            string sdk = Environment.GetEnvironmentVariable("ANDROID_HOME") ?? @"C:\Android\Sdk";
            adb = Path.Combine(sdk, "platform-tools", "adb.exe");
#endif
            usb = new QuestUsbDiscovery(adb);
        }

        private void Update()
        {
            if (replica != null)
            {
                if (replica.IsClosed) replica = null;
                else replica.Tick(Time.unscaledTime);
                if (replica?.RejectionReason is { } rejection && Status != rejection) SetStatus(rejection);
            }

            if (publicationReplica != null && publicationReplica.IsClosed)
            {
                publicationReplica.Dispose();
                publicationReplica = null;
            }

            if (!busy && !scanning && (discoveryActive || reconnect) && Time.unscaledTime >= nextDiscovery)
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
        }

        public void SetDiscoveryActive(bool active)
        {
            discoveryActive = active;
            if (active) nextDiscovery = 0;
        }

        public void Disconnect()
        {
            if (!IsPaired) return;
            if (busy) throw new InvalidOperationException("Wait for the current Quest operation to finish.");
            devices.Clear();
            manualSelected = false;
            DiscoveryMessages = new[] { "Searching for Quests on USB and Wi-Fi..." };
            ClearPairing();
            SetStatus("Select a Quest to pair.");
        }

        public void Select(QuestDevice device)
        {
            if (busy || device == null || device.Pin == null) return;
            ClearPairing();
            manualSelected = false;
            endpoint = device.Host;
            SelectedId = device.Id;
            pin = (byte[])device.Pin.Clone();
            credential = ReadCredential(pin);
            SetStatus(credential == null ? "Select Pair to enter the code shown in your Quest." : "Quest remembered. Select Pair to reconnect.");
        }

        public void SelectManual(string address)
        {
            if (busy) return;
            ClearPairing();
            manualSelected = true;
            endpoint = address?.Trim();
            SetStatus("Select Pair to connect to this address.");
        }

        public Task PairAsync(Func<CancellationToken, UniTask<InputDialogResult>> requestCode)
        {
            return RunAsync(async token =>
            {
                if (string.IsNullOrWhiteSpace(endpoint)) throw new ArgumentException("Select a Quest or enter its IP address.");
                if (pin == null)
                {
                    if (endpoint.Contains(":")) throw new ArgumentException("Enter the Quest IPv4 address without a port.");
                    SetStatus("Connecting to Quest...");
                    pin = await QuestPairing.InspectAsync(endpoint, token);
                    credential = ReadCredential(pin);
                }

                bool remembered = credential != null;
                if (!remembered) await AuthenticateWithCodeAsync(requestCode, token);

                reconnect = true;
                try
                {
                    await RestoreAsync(token);
                }
                catch (AuthenticationException) when (remembered)
                {
                    Array.Clear(credential, 0, credential.Length);
                    credential = null;
                    connected = reconnect = false;
                    await AuthenticateWithCodeAsync(requestCode, token);
                    reconnect = true;
                    await RestoreAsync(token);
                }
            });
        }

        private async Task AuthenticateWithCodeAsync(Func<CancellationToken, UniTask<InputDialogResult>> requestCode, CancellationToken token)
        {
            InputDialogResult answer = await requestCode(token);
            if (!answer.Confirmed) throw new OperationCanceledException(token);
            if (answer.Value == null || answer.Value.Length != 6 || !answer.Value.All(char.IsDigit))
                throw new ArgumentException("Enter the six-digit code shown in your Quest.");
            SetStatus("Checking the Quest code...");
            credential = await QuestPairing.AuthenticateAsync(endpoint, pin, answer.Value, true, token);
            PairingStorage.Write(CredentialPath(pin), credential);
        }

        private async Task RestoreAsync(CancellationToken token)
        {
            SetStatus("Preparing Quest pairing...");
            await UniTask.NextFrame(cancellationToken: token);
            globals ??= PairingSnapshot.Capture();
            SetStatus("Connecting to paired Quest...");
            await QuestPairing.ResumeAsync(endpoint, pin, credential, globals.Context.Id, token, (stream, stop) => globals.Delivery.SendAsync(stream, stop));
            connected = true;
            nextHeartbeat = Time.unscaledTime + 5;
            SetStatus("Quest paired and ready for a visualization.");
        }

        public async Task<bool> SendAsync(bool retry, Base3DScene sourceScene = null)
        {
            SyncTelemetryPoint requestPoint = retry ? default : SyncTelemetry.CapturePoint();
            bool sent = false;
            string requestedTransferId = null;
            long captureGeneration = 0;
            if (!retry)
            {
                requestedTransferId = Guid.NewGuid().ToString("N");
                captureGeneration = Interlocked.Increment(ref nextTransferCaptureGeneration);
            }

            string userRequestTransferId = requestedTransferId;
            long userRequestCaptureGeneration = captureGeneration;

            try
            {
                await RunAsync(async token =>
                {
                    if (!IsPaired && reconnect && credential != null) await RestoreAsync(token);
                    if (!IsPaired) throw new InvalidOperationException("Pair a Quest first.");
                    if (retry && !CanRetry) throw new InvalidOperationException("No prepared visualization is available to retry.");
                    failedDelivery = false;
                    var context = SynchronizationContext.Current;
                    bool retryCurrent = retry && publicationReplica?.CanRetryTransfer == true;
                    int publicationRestarts = 0;
                    try
                    {
                        while (true)
                        {
                            if (!retryCurrent && (retry || publicationRestarts > 0))
                            {
                                requestedTransferId = Guid.NewGuid().ToString("N");
                                captureGeneration = Interlocked.Increment(ref nextTransferCaptureGeneration);
                            }

                            var result = await LoadingManager.LoadAsync<(DeliveryReceipt Receipt, Exception Error)>(async (update, loadingToken) =>
                            {
                                try
                                {
                                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, loadingToken);
                                    var stop = linked.Token;
                                    void Report(float value, string message) => context.Post(_ => update(value, 0, new LoadingText(message)), null);
                                    if (!retryCurrent)
                                    {
                                        offer?.Dispose();
                                        offer = null;
                                        publicationReplica?.Dispose();
                                        publicationReplica = null;
                                        Report(0.02f, "Preparing visualization");
                                        offer = await DesktopSceneCapture.CaptureForQuestAsync(globals.Context, stop, update, sourceScene, requestedTransferId, captureGeneration, (scene, transferId, sessionId) => { publicationReplica = new DesktopV2ReplicaSession(scene, sessionId, transferId); });
                                        Report(0.20f, "Connecting to Quest");
                                    }

                                    DesktopV2ReplicaSession owner = publicationReplica ?? throw new InvalidOperationException("The prepared visualization has no v2 publication owner.");
                                    using var publicationStop = CancellationTokenSource.CreateLinkedTokenSource(stop, owner.PublicationAbortToken);
                                    var delivery = offer;
                                    DeliveryReceipt receipt = await QuestPairing.SendAsync(endpoint, pin, credential, publicationStop.Token, (stream, sendStop) => delivery.SendAsync(stream, sendStop, null, (count, total) =>
                                    {
                                        float value = 0.20f + 0.65f * Mathf.Clamp01((float)count / total);
                                        Report(value, $"Sending visualization: {100L * Math.Min(count, total) / total}%");
                                    }));
                                    await UniTask.SwitchToMainThread(stop);
                                    var binding = PreparedSceneDeliveryBinding.FromSent(delivery, receipt);
                                    await owner.StartAfterPublicationAsync(binding, endpoint, pin, credential, publicationStop.Token);
                                    update(1, 0, new LoadingText("Visualization ready on Quest"));
                                    return (receipt, null);
                                }
                                catch (Exception error)
                                {
                                    await UniTask.SwitchToMainThread();
                                    return (null, error);
                                }
                            }, true);
                            await UniTask.SwitchToMainThread(token);
                            if (result.Error != null)
                            {
                                bool restart = publicationReplica?.RequiresPublicationRestart == true || result.Error is DesktopV2ReplicaSession.V2PublicationRestartException;
                                if (!restart || publicationRestarts >= 2) throw result.Error;
                                publicationRestarts++;
                                retryCurrent = false;
                                publicationReplica?.Dispose();
                                publicationReplica = null;
                                if (offer != null)
                                {
                                    SceneDelivery incomplete = offer;
                                    offer = null;
                                    await incomplete.DisposeAsync();
                                }

                                continue;
                            }

                            sent = result.Receipt.Status == DeliveryStatus.Published || result.Receipt.Status == DeliveryStatus.AlreadyPublished;
                            if (sent)
                            {
                                replica?.Dispose();
                                replica = null;
                            }

                            break;
                        }

                        SetStatus(sent ? "Visualization ready on Quest." : "The visualization was closed or replaced on Quest. Send a new snapshot.");
                    }
                    catch
                    {
                        sent = false;
                        await UniTask.SwitchToMainThread();
                        failedDelivery = offer?.CanRetry == true;
                        if (offer != null && !failedDelivery)
                        {
                            var incomplete = offer;
                            offer = null;
                            await incomplete.DisposeAsync();
                        }

                        if (!failedDelivery)
                        {
                            publicationReplica?.Dispose();
                            publicationReplica = null;
                        }

                        await UniTask.SwitchToMainThread();
                        connected = false;
                        Changed?.Invoke();
                        throw;
                    }
                });
            }
            finally
            {
                if (!retry)
                {
                    var identity = new SyncTelemetryIdentity(userRequestTransferId, 1, userRequestCaptureGeneration);
                    SyncTelemetry.MarkAt(SyncProfile.InitialTransfer, identity, SyncMilestone.UserRequest, requestPoint);
                }
            }

            return sent;
        }

        public void Cancel() => operation?.Cancel();

        private Task RunAsync(Func<CancellationToken, Task> action)
        {
            if (busy || !isActiveAndEnabled) return Task.CompletedTask;
            busy = true;
            Changed?.Invoke();
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
                await UniTask.SwitchToMainThread();
                connected = false;
                if (exception is AuthenticationException)
                {
                    reconnect = false;
                    if (credential != null) Array.Clear(credential, 0, credential.Length);
                    credential = null;
                    SetStatus("Pairing refused. Check the Quest code and try again.");
                }
                else if (exception is OperationCanceledException || attempt.IsCancellationRequested)
                    SetStatus("Operation cancelled.");
                else
                    SetStatus(exception is ArgumentException || exception is InvalidOperationException ? exception.Message : reconnect ? "Quest disconnected. Reconnecting automatically..." : "Quest unreachable. Check USB or Wi-Fi.");

                Debug.LogWarning("Quest operation failed: " + exception.Message);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                operation = null;
                busy = false;
                Changed?.Invoke();
            }
        }

        private async Task DiscoverAsync()
        {
            scanning = true;
            DiscoveryMessages = new[] { "Searching for Quests on USB and Wi-Fi..." };
            Changed?.Invoke();
            try
            {
                Task<List<QuestDevice>> wifi = QuestDiscovery.FindAsync(lifetime.Token);
                Task<List<QuestDevice>> wired = usb.FindAsync(lifetime.Token);
                var found = new List<QuestDevice>();
                var messages = new List<string>();
                try
                {
                    found.AddRange(await wired);
                }
                catch (Exception)
                {
                    messages.Add("USB discovery unavailable");
                }

                try
                {
                    found.AddRange(await wifi);
                }
                catch (Exception)
                {
                    messages.Add("Wi-Fi discovery unavailable");
                }

                if (!this || lifetime.IsCancellationRequested || busy) return;
                devices = found.GroupBy(x => x.Id).Select(g => g.First()).OrderBy(x => x.Label).ToList();
                if (!manualSelected && SelectedId != null)
                {
                    QuestDevice selected = devices.FirstOrDefault(x => x.Id == SelectedId);
                    if (selected != null) endpoint = selected.Host;
                }

                if (!manualSelected && SelectedId == null && devices.Count > 0) Select(devices[0]);
                messages.Add(devices.Count == 0 ? "No Quest found. Try Manual IP." : $"{devices.Count} Quest headset{(devices.Count == 1 ? "" : "s")} found");
                DiscoveryMessages = messages;
                Changed?.Invoke();
            }
            finally
            {
                scanning = false;
                if (this) nextDiscovery = Time.unscaledTime + 5;
            }
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
                SetStatus("Saved pairing cannot be read. Enter the headset code to pair again.");
            }

            return null;
        }

        private void ClearPairing()
        {
            replica?.Dispose();
            replica = null;
            publicationReplica?.Dispose();
            publicationReplica = null;
            if (credential != null) Array.Clear(credential, 0, credential.Length);
            credential = pin = null;
            globals?.Dispose();
            globals = null;
            offer?.Dispose();
            offer = null;
            endpoint = SelectedId = null;
            connected = reconnect = failedDelivery = false;
            Changed?.Invoke();
        }

        private void SetStatus(string value)
        {
            Status = value;
            Changed?.Invoke();
        }

        private async void OnDestroy()
        {
            replica?.Dispose();
            publicationReplica?.Dispose();
            lifetime.Cancel();
            operation?.Cancel();
            try
            {
                await Task.WhenAll(running, discovering);
            }
            catch (Exception)
            {
            }

            ClearPairing();
            lifetime.Dispose();
        }
    }
}
