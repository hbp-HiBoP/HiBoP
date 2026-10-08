using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Core.Enums;
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
        public bool LastOperationCancelled { get; private set; }
        public bool CanRetryReconciliation => reconciliationNeedsRetry;
        private IDisposable pairingTiming;
        public bool IsPaired => connected && credential != null;
        public bool CanRetry => offer?.CanRetry == true && failedDelivery && IsPaired;
        public bool HasRetryableDelivery => offer?.CanRetry == true && failedDelivery;

        private byte[] pin, credential;
        private string endpoint, store;
        private SceneDelivery offer;
        private DesktopReplicaSession replica;
        private DesktopV2ReplicaSession publicationReplica;
        private V2SceneReconciliationRecord reconciliation;
        private bool reconciliationNeedsRetry;
        private PairingSnapshot globals;
        private DesktopSessionPreferences sessionPreferences;
        private readonly SemaphoreSlim sessionOperations = new(1, 1);
        private long sessionGeneration;
        private long discoveryGeneration;
        public string PreferencesSyncStatus => IsPaired ? sessionPreferences?.AtlasStatus ?? "Quest session is connecting." : "Quest disconnected. Atlas actions are local.";
        public bool CanRetryAtlas => IsPaired && sessionPreferences?.RetryAtlasId != null;
        public bool HasSharedScene => publicationReplica != null && !publicationReplica.IsClosed || replica != null && !replica.IsClosed || reconciliation != null && reconciliation.Scene && !reconciliation.Scene.IsClosing;
        public Task<string> ValidatePreferencesChangeAsync(NormalizationType requested) => IsPaired && sessionPreferences != null ? sessionPreferences.ValidateNormalizationAsync(requested, HasSharedScene) : Task.FromResult(ValidatePreferencesChangeNow(requested));
        public string ValidatePreferencesChangeNow(NormalizationType requested) => requested != PersistentDataManager.UserPreferences.Data.EEG.Normalization && (HasSharedScene || IsBusy || sessionPreferences?.QuestHasSharedScene == true) ? "Close shared visualizations and wait for the Quest operation to finish before changing EEG normalization." : null;

        public async Task SetAtlasLoadedAsync(string id, bool loaded)
        {
            await sessionOperations.WaitAsync(lifetime.Token);
            try
            {
                if (IsPaired && sessionPreferences != null) await sessionPreferences.SetAtlasLoadedAsync(id, loaded);
                else if (loaded)
                {
                    var result = await AtlasResources.LoadAsync(id, lifetime.Token);
                    if (!result.Succeeded) throw new InvalidOperationException(result.Error);
                }
                else AtlasResources.Unload(id);
            }
            finally
            {
                sessionOperations.Release();
            }
        }

        public async Task RetryAtlasAsync()
        {
            await sessionOperations.WaitAsync(lifetime.Token);
            try
            {
                if (sessionPreferences != null) await sessionPreferences.RetryAtlasAsync();
            }
            finally
            {
                sessionOperations.Release();
            }
        }

        private void SessionConnectionLost()
        {
            connected = false;
            sessionPreferences?.Dispose();
            SetStatus("Quest disconnected. Pending preference and atlas operations were abandoned.");
        }

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
            PairingTiming.Measured += LogPairingTiming;
        }

        private static void LogPairingTiming(string message) => Debug.Log(message);

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

            if (!busy && reconnect && credential != null && !reconciliationNeedsRetry && Time.unscaledTime >= nextHeartbeat)
            {
                nextHeartbeat = Time.unscaledTime + 5;
                _ = RunAsync(async token =>
                {
                    bool ready = await QuestPairing.PingAsync(endpoint, pin, credential, token);
                    if (!connected || !ready || publicationReplica == null && reconciliation != null && !reconciliationNeedsRetry && reconciliation.Status != V2ReconciliationStatus.Orphan) await RestoreAsync(token);
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
            if (device.Id == SelectedId && pin != null && pin.SequenceEqual(device.Pin))
            {
                endpoint = device.Host;
                return;
            }

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
            if (pin != null && string.Equals(endpoint, address?.Trim(), StringComparison.Ordinal)) return;
            ClearPairing();
            manualSelected = true;
            endpoint = address?.Trim();
            SetStatus("Select Pair to connect to this address.");
        }

        public Task PairAsync(Func<CancellationToken, UniTask<InputDialogResult>> requestCode)
        {
            return RunAsync(async token =>
            {
                reconciliationNeedsRetry = false;
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
            pairingTiming?.Dispose();
            pairingTiming = PairingTiming.Measure("desktop.pairing-total");
            credential = await QuestPairing.AuthenticateAsync(endpoint, pin, answer.Value, true, token);
            PairingStorage.Write(CredentialPath(pin), credential);
        }

        private async Task RestoreAsync(CancellationToken token)
        {
            SetStatus("Preparing Quest pairing...");
            await UniTask.NextFrame(cancellationToken: token);
            globals ??= await PairingSnapshot.CaptureAsync(token);
            SetStatus("Connecting to paired Quest...");
            sessionPreferences?.Dispose();
            var controls = new DesktopSessionPreferences(Guid.ParseExact(globals.Context.Id, "N"), ++sessionGeneration, PersistentDataManager.UserPreferences, (request, stop) => QuestPairing.SendSessionControlAsync(endpoint, pin, credential, request, stop), globals.Context.Data.Preferences);
            sessionPreferences = controls;
            controls.Changed += () => Changed?.Invoke();
            controls.ConnectionLost += SessionConnectionLost;
            try
            {
                SetStatus("Applying shared preferences and definitions on Quest...");
                using (PairingTiming.Measure("desktop.globals-resume"))
                    await QuestPairing.ResumeAsync(endpoint, pin, credential, globals.Context.Id, token, SendGlobalsAsync);
                SetStatus("Opening the Quest session...");
                await controls.StartAsync();
                connected = true;
            }
            catch
            {
                controls.Dispose();
                throw;
            }

            nextHeartbeat = Time.unscaledTime + 5;
            if (reconciliation != null && publicationReplica == null && reconciliation.Status != V2ReconciliationStatus.Orphan)
                await ReconcileRetainedSceneAsync(token);
            else SetStatus("Quest paired and ready for a visualization.");
        }

        private async Task ReconcileRetainedSceneAsync(CancellationToken token)
        {
            try
            {
                SetStatus("Reconnecting: comparing scene changes...");
                await QuestPairing.OpenReplicaAsync(endpoint, pin, credential, token, (stream, stop) => V2SceneReconciliationProtocol.ReconcileDesktopAsync(stream, reconciliation, ChooseConflictAsync, stop, RunReconciliationApplyAsync));
                await UniTask.SwitchToMainThread(token);
                if (reconciliation.Status == V2ReconciliationStatus.Orphan)
                {
                    SetStatus(reconciliation.Message);
                    return;
                }

                var binding = reconciliation.Binding;
                publicationReplica = new DesktopV2ReplicaSession(reconciliation.Scene, binding.GlobalContextId, binding.TransferId);
                await publicationReplica.StartAfterPublicationAsync(binding, endpoint, pin, credential, token);
                await UniTask.SwitchToMainThread(token);
                SetStatus("Scene synchronized with Quest.");
            }
            catch
            {
                await UniTask.SwitchToMainThread();
                reconciliationNeedsRetry = true;
                SetStatus(reconciliation.Message ?? "Reconnection interrupted. Select Retry to try again.");
                throw;
            }
        }

        private static async Task RunReconciliationApplyAsync(Func<CancellationToken, Task> apply, CancellationToken stop)
        {
            await LoadingManager.LoadDelayedAsync(async (update, token) =>
            {
                update(0.1f, 0, new LoadingText("Applying merged changes"));
                await apply(token);
                await UniTask.SwitchToMainThread(token);
                update(1, 0, new LoadingText("Changes merged"));
            }, stop);
        }

        private static async Task<V2ConflictChoice> ChooseConflictAsync(V2CheckpointConflict conflict, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(stop);
            int answer = await DialogBoxManager.OpenScrollableAsync(DialogBoxType.Warning, "Choose which changes to keep", conflict.Description + "\n\nDesktop changes:\n" + conflict.DesktopSummary + "\n\nQuest changes:\n" + conflict.QuestSummary + "\n\nChoose Desktop to keep the Desktop version of these conflicting changes, or Quest to keep the Quest version. Independent changes from both devices are kept automatically. Select one to continue.", stop, "Desktop", "Quest");
            return answer == 0 ? V2ConflictChoice.Desktop : V2ConflictChoice.Quest;
        }

        private async Task<DeliveryReceipt> SendGlobalsAsync(Stream stream, CancellationToken token)
        {
            var transfer = PairingTiming.Measure("desktop.globals-transfer");
            try
            {
                return await globals.Delivery.SendAsync(stream, token, null, (sent, total) =>
                {
                    if (sent < total) return;
                    transfer?.Dispose();
                    transfer = null;
                });
            }
            finally
            {
                transfer?.Dispose();
            }
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
                    if (sessionPreferences != null) await sessionPreferences.FlushPreferencesAsync();
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
                                        await UniTask.SwitchToMainThread(stop);
                                        var sceneToSend = sourceScene != null ? sourceScene : Module3DMain.IsInitialized ? Module3DMain.SelectedScene : null;
                                        if (sceneToSend != null && sessionPreferences != null)
                                            foreach (string atlas in sceneToSend.RequiredAtlasIds())
                                                await sessionPreferences.EnsureAtlasReadyAsync(atlas);
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
                                    reconciliation?.Dispose();
                                    reconciliation = V2SceneReconciliationRecord.Retain(owner.Scene, binding);
                                    reconciliationNeedsRetry = false;
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
            LastOperationCancelled = false;
            Changed?.Invoke();
            return running = ExecuteAsync(action);
        }

        private async Task ExecuteAsync(Func<CancellationToken, Task> action)
        {
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            operation = attempt;
            await sessionOperations.WaitAsync(lifetime.Token);
            try
            {
                await action(attempt.Token);
            }
            catch (Exception exception)
            {
                await UniTask.SwitchToMainThread();
                connected = false;
                sessionPreferences?.Dispose();
                if (exception is AuthenticationException)
                {
                    reconnect = false;
                    if (credential != null) Array.Clear(credential, 0, credential.Length);
                    credential = null;
                    SetStatus("Pairing refused. Check the Quest code and try again.");
                }
                else if (reconciliationNeedsRetry)
                {
                    LastOperationCancelled = exception is OperationCanceledException || attempt.IsCancellationRequested;
                    SetStatus((reconciliation?.Message ?? "Reconnection interrupted.") + " Select Retry.");
                }
                else if (exception is OperationCanceledException || attempt.IsCancellationRequested)
                {
                    LastOperationCancelled = true;
                    SetStatus("Operation cancelled.");
                }
                else
                    SetStatus(exception is ArgumentException || exception is InvalidOperationException ? exception.Message : reconnect ? "Shared data or connection interrupted. Reconnecting with the saved association..." : "Quest unreachable. Check USB or Wi-Fi.");

                Debug.LogWarning("Quest operation failed: " + exception.Message);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                pairingTiming?.Dispose();
                pairingTiming = null;
                sessionOperations.Release();
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
                long generation = ++discoveryGeneration;
                var context = SynchronizationContext.Current;
                var found = new List<QuestDevice>();

                void Discovered(QuestDevice device) =>
                    context.Post(_ =>
                    {
                        if (!this || lifetime.IsCancellationRequested || !scanning || generation != discoveryGeneration) return;
                        found.Add(device);
                        PublishDiscovery(found, false);
                    }, null);

                var pending = new Dictionary<Task<List<QuestDevice>>, string>
                {
                    [QuestDiscovery.FindAsync(lifetime.Token, Discovered)] = "Wi-Fi",
                    [usb.FindAsync(lifetime.Token, Discovered)] = "USB"
                };
                var messages = new List<string>();
                while (pending.Count > 0)
                {
                    var completed = await Task.WhenAny(pending.Keys);
                    string source = pending[completed];
                    pending.Remove(completed);
                    try
                    {
                        found.AddRange(await completed);
                    }
                    catch (Exception) when (!lifetime.IsCancellationRequested)
                    {
                        messages.Add(source + " discovery unavailable");
                    }

                    if (!this || lifetime.IsCancellationRequested) return;
                    PublishDiscovery(found, pending.Count == 0);
                }

                if (!manualSelected && SelectedId != null)
                {
                    QuestDevice selected = devices.FirstOrDefault(x => x.Id == SelectedId);
                    if (selected != null && !busy) endpoint = selected.Host;
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

        private void PublishDiscovery(List<QuestDevice> found, bool complete)
        {
            devices = (complete ? found : found.Concat(devices)).GroupBy(x => x.Id).Select(g => g.OrderBy(x => x.UsbSerial == null).First()).OrderBy(x => x.Label).ToList();
            if (!manualSelected && !busy && SelectedId == null && devices.Count > 0) Select(devices[0]);
            Changed?.Invoke();
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
            reconciliation?.Dispose();
            reconciliation = null;
            reconciliationNeedsRetry = false;
            sessionPreferences?.Dispose();
            sessionPreferences = null;
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
            PairingTiming.Measured -= LogPairingTiming;
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
