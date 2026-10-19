using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Core.Database;
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
    public sealed partial class QuestManager : Manager<QuestManager>
    {
        public static QuestManager Instance => m_Instance;
        public UnityEvent OnChanged { get; } = new();

        public IReadOnlyList<QuestDevice> Devices => devices;
        public IReadOnlyList<string> DiscoveryMessages { get; private set; } = new[] { "Searching for Quests on USB and Wi-Fi..." };
        public string Status { get; private set; } = "Select a Quest to pair.";
        public string SelectedId { get; private set; }
        public bool IsManualSelection => manualSelected;
        public string SelectedAddress => manualSelected ? selectedEndpoint : string.Empty;
        public bool IsBusy => busy;
        public bool LastOperationCancelled { get; private set; }
        public bool CanRetryReconciliation => reconciliationNeedsRetry;
        public bool CanRetrySceneOperations => CanRetryReconciliation || CanRetryScenes;
        private IDisposable pairingTiming;
        public bool IsPaired => connected && credential != null;
        public string ConnectedId => pin == null ? null : Convert.ToBase64String(pin);
        public string ConnectedLabel => devices.FirstOrDefault(device => device.Id == ConnectedId)?.Label ?? (pin == null ? "Quest" : "Quest · " + BitConverter.ToString(pin, 0, 3).Replace("-", ""));
        public bool IsSelectedConnected => IsPaired && SelectedId == ConnectedId;
        public bool LastOperationSucceeded { get; private set; }
        public Exception BlockingError { get; private set; }
        private string selectedEndpoint;
        private byte[] selectedPin;
        private string activeRequest;
        private bool automaticOperation;
        private LoadingManager.PresentationHandle pairingPresentation;
        private Action<float, float, LoadingText> pairingProgress;
        public bool CanRetry => HasRetryableDelivery && IsPaired;
        public bool HasRetryableDelivery => publications.Values.Any(value => value.Error != null && value.Offer?.CanRetry == true);

        private byte[] pin, credential;
        private string endpoint, store;
        private bool reconciliationNeedsRetry;
        private PairingSnapshot globals;
        private DesktopSessionPreferences sessionPreferences;
        private readonly SemaphoreSlim sessionOperations = new(1, 1);
        private long sessionGeneration;
        public string PreferencesSyncStatus => IsPaired ? sessionPreferences?.AtlasStatus ?? "Quest session is connecting." : "Quest disconnected. Atlas actions are local.";
        public bool CanRetryAtlas => IsPaired && sessionPreferences?.RetryAtlasId != null;
        public bool HasSharedScene => publications.Values.Any(value => !value.Removed && value.Binding != null && value.Scene != null && !value.Scene.IsClosing);

        public async UniTask<string> ValidatePreferencesChangeAsync(NormalizationType requested)
        {
            string blocked = ValidatePreferencesChangeNow(requested);
            if (blocked != null || requested == PersistentDataManager.UserPreferences.Data.EEG.Normalization || !IsPaired) return blocked;
            try
            {
                var inventory = await InventoryAsync(lifetime.Token);
                if (inventory.Any(remote => remote.Orphan || !publications.Values.Any(local => !local.Removed && local.Binding?.TransferId == remote.TransferId)))
                    return "Close local/orphan visualizations on Quest before changing EEG normalization. Their original recordings cannot be reloaded from Desktop.";
                return null;
            }
            catch (Exception error)
            {
                return "Cannot prepare the Quest reload: " + TransferError.Describe(error);
            }
        }

        public string ValidatePreferencesChangeNow(NormalizationType requested)
        {
            if (requested == PersistentDataManager.UserPreferences.Data.EEG.Normalization) return null;
            if (busy || reloadingScenes || restoringScenes || !inventoryWork.IsCompleted || publications.Values.Any(value => value.Work != null && !value.Work.IsCompleted)) return "Wait for Quest operations to finish before reloading visualizations.";
            return HasSharedScene && !IsPaired ? "Reconnect Quest before reloading shared visualizations." : null;
        }


        public async UniTask SetAtlasLoadedAsync(string id, bool loaded)
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
                await UniTask.SwitchToMainThread();
                sessionOperations.Release();
            }
        }

        public async UniTask RetryAtlasAsync()
        {
            await sessionOperations.WaitAsync(lifetime.Token);
            try
            {
                if (sessionPreferences != null) await sessionPreferences.RetryAtlasAsync();
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                sessionOperations.Release();
            }
        }

        private void SessionConnectionLost()
        {
            connected = false;
            LastOperationSucceeded = false;
            sessionPreferences?.Dispose();
            UpdateDiscoveryWatch();
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
            PairingTiming.OnMeasured += LogPairingTiming;
            InitializeSceneLifecycle();
            InitializeDiscovery();
        }

        private static void LogPairingTiming(string message) => Debug.Log(message);

        private void Update()
        {
            TickSceneLifecycle();
            TickDiscovery();
            if (!busy && reconnect && credential != null && !autoConnectSuspended && !reconciliationNeedsRetry && Time.unscaledTime >= nextHeartbeat)
            {
                nextHeartbeat = Time.unscaledTime + 5;
                _ = RunAsync(async token =>
                {
                    bool ready = await QuestPairing.PingAsync(endpoint, pin, credential, token);
                    if (!ready || NeedsSceneReconnection) await RestoreAsync(token);
                }, true);
            }
        }

        public void SetDiscoveryActive(bool active)
        {
            discoveryActive = active;
            if (active) RequestDiscovery();
            UpdateDiscoveryWatch();
        }

        public void Disconnect()
        {
            if (busy) throw new InvalidOperationException("Wait for the current Quest operation to finish.");
            autoConnectSuspended = true;
            DisconnectTransport();
            SetStatus("Quest disconnected. Select Connect to reconnect.");
        }

        private void DisconnectTransport()
        {
            connected = reconnect = false;
            LastOperationSucceeded = false;
            sessionPreferences?.Dispose();
            sessionPreferences = null;
            foreach (var entry in publications.Values)
            {
                entry.Owner?.Dispose();
                entry.Owner = null;
            }
        }

        public void Select(QuestDevice device)
        {
            if (busy || device?.Pin == null) return;
            manualSelected = false;
            SelectedId = device.Id;
            selectedEndpoint = device.Host;
            selectedPin = (byte[])device.Pin.Clone();
            OnChanged.Invoke();
        }

        public void SelectManual(string address)
        {
            if (busy) return;
            manualSelected = true;
            selectedEndpoint = address?.Trim();
            selectedPin = null;
            SelectedId = null;
            OnChanged.Invoke();
        }

        public void SelectAutomatic()
        {
            if (busy) return;
            manualSelected = false;
            var device = devices.FirstOrDefault(candidate => candidate.Id == SelectedId) ?? devices.FirstOrDefault();
            if (device != null)
            {
                Select(device);
                return;
            }

            SelectedId = null;
            selectedEndpoint = null;
            selectedPin = null;
            OnChanged.Invoke();
        }

        public Task PairAsync(Func<CancellationToken, UniTask<InputDialogResult>> requestCode)
        {
            string target = SelectedId ?? selectedEndpoint;
            if (busy) return activeRequest == target ? running : Task.FromException(new InvalidOperationException("Another Quest operation is in progress."));
            autoConnectSuspended = false;
            if (SelectedId != null) automaticExcluded.Remove(SelectedId);
            if (SelectedId != null) automaticRetryAfter.Remove(SelectedId);
            activeRequest = target;
            return RunAsync(token => ConnectSelectedAsync(requestCode, false, token));
        }

        private async Task ConnectSelectedAsync(Func<CancellationToken, UniTask<InputDialogResult>> requestCode, bool automatic, CancellationToken token)
        {
            if (IsSelectedConnected)
            {
                await PrepareDatabaseAsync(token);
                {
                    var stop = token;
                    // A manual reconnect may refresh definitions only once neither side uses the old snapshot.
                    if (!HasSharedScene && inventoryWork.IsCompleted && !publications.Values.Any(entry => entry.HasPendingWork))
                    {
                        await FlushClosesAsync(stop);
                        if ((await InventoryAsync(stop)).Length == 0)
                        {
                            stop.ThrowIfCancellationRequested();
                            DisconnectTransport();
                            globals?.Dispose();
                            globals = null;
                        }
                    }

                    await RestoreAsync(stop);
                    if (CanRetryScenes) await RetryFailedScenesAsync(stop);
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedEndpoint)) throw new ArgumentException("Select a Quest or enter its IP address.");
            byte[] nextPin = selectedPin;
            if (nextPin == null)
            {
                if (selectedEndpoint.Contains(":")) throw new ArgumentException("Enter the Quest IPv4 address without a port.");
                nextPin = await QuestPairing.InspectAsync(selectedEndpoint, token);
                SelectedId = Convert.ToBase64String(nextPin);
                selectedPin = nextPin;
            }

            token.ThrowIfCancellationRequested();
            if (pin != null && !pin.SequenceEqual(nextPin))
            {
                if (HasSharedScene) throw new InvalidOperationException("Close the visualizations shared with the current Quest before connecting another headset.");
                ClearPairing();
            }

            DisconnectTransport();
            pin = (byte[])nextPin.Clone();
            endpoint = selectedEndpoint;
            if (credential == null) credential = await Task.Run(() => ReadCredential(pin), token);
            await UniTask.SwitchToMainThread(token);
            if (automatic && credential == null) throw new AuthenticationException("Pair this Quest manually before connecting automatically.");
            reconciliationNeedsRetry = false;
            await PrepareDatabaseAsync(token);
            if (credential == null) await AuthenticateWithCodeAsync(requestCode, token);
            try
            {
                await RestoreAsync(token);
            }
            catch (AuthenticationException) when (!automatic)
            {
                if (credential != null) Array.Clear(credential, 0, credential.Length);
                credential = null;
                await AuthenticateWithCodeAsync(requestCode, token);
                await RestoreAsync(token);
            }
        }

        private async Task PrepareDatabaseAsync(CancellationToken token)
        {
            async UniTask Prepare(Action<float, float, LoadingText> progress, CancellationToken stop)
            {
                SetStatus("Waiting for the Desktop database");
                await ApplicationManager.WaitForDatabaseInitializationAsync(stop);
                stop.ThrowIfCancellationRequested();
                await DatabaseManager.Database.EnsureDatabaseReadyAsync(progress, stop);
            }

            try
            {
                await Prepare(pairingProgress, token);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                BlockingError = error;
                throw;
            }
        }

        private async Task PresentPhaseAsync(Func<Action<float, float, LoadingText>, CancellationToken, UniTask> phase, CancellationToken token)
        {
            var previous = pairingPresentation;
            var previousProgress = pairingProgress;
            try
            {
                await LoadingManager.PresentAsync(async (update, stop) =>
                {
                    pairingProgress = update;
                    update(0, 0, new LoadingText(Status));
                    await phase(update, stop);
                }, token, handle => pairingPresentation = handle);
            }
            finally
            {
                pairingPresentation = previous;
                pairingProgress = previousProgress;
            }
        }

        private async Task AuthenticateWithCodeAsync(Func<CancellationToken, UniTask<InputDialogResult>> requestCode, CancellationToken token)
        {
            if (requestCode == null) throw new AuthenticationException("Connect manually to enter the Quest code.");
            Guid codeRequest = await QuestPairing.RequestCodeAsync(endpoint, pin, token);
            try
            {
                InputDialogResult answer;
                var presentation = pairingPresentation;
                if (presentation != null) await presentation.PauseAsync();
                try
                {
                    answer = await requestCode(token);
                }
                finally
                {
                    if (!token.IsCancellationRequested) presentation?.Resume();
                }

                if (!answer.Confirmed) throw new OperationCanceledException(token);
                if (answer.Value == null || answer.Value.Length != 6 || !answer.Value.All(char.IsDigit))
                    throw new ArgumentException("Enter the six-digit code shown in your Quest.");
                {
                    var stop = token;
                    SetStatus("Checking the Quest code");
                    pairingTiming?.Dispose();
                    pairingTiming = PairingTiming.Measure("desktop.pairing-total");
                    credential = await QuestPairing.AuthenticateAsync(endpoint, pin, answer.Value, true, stop);
                    // Once received, persist even if cancellation arrives: the receiver already owns this secret.
                    await Task.Run(() => PairingStorage.Write(CredentialPath(pin), credential));
                    stop.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                try
                {
                    using var close = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await QuestPairing.RequestCodeAsync(endpoint, pin, close.Token, codeRequest);
                }
                catch (Exception)
                {
                    /* The bounded display request expires if the peer disappeared. */
                }
            }
        }

        private async Task RestoreAsync(CancellationToken token)
        {
            SetStatus("Preparing Quest pairing");
            await UniTask.NextFrame(cancellationToken: token);
            globals ??= await PairingSnapshot.CaptureAsync(token);
            sessionPreferences?.Dispose();
            var controls = new DesktopSessionPreferences(Guid.ParseExact(globals.Context.Id, "N"), ++sessionGeneration, PersistentDataManager.UserPreferences, (request, stop) => QuestPairing.SendSessionControlAsync(endpoint, pin, credential, request, stop), globals.Context.Data.Preferences);
            sessionPreferences = controls;
            controls.OnChanged.AddListener(() =>
            {
                if (ReferenceEquals(sessionPreferences, controls)) OnChanged.Invoke();
            });
            controls.OnConnectionLost.AddListener(() =>
            {
                if (ReferenceEquals(sessionPreferences, controls)) SessionConnectionLost();
            });
            try
            {
                SetStatus("Applying shared preferences and definitions on Quest");
                using (PairingTiming.Measure("desktop.globals-resume"))
                    await QuestPairing.ResumeAsync(endpoint, pin, credential, globals.Context.Id, token, SendGlobalsAsync);
                SetStatus("Opening the Quest session");
                await controls.StartAsync(token);
                token.ThrowIfCancellationRequested();
                connected = reconnect = true;
                LastOperationSucceeded = true;
                long date = DateTime.UtcNow.Ticks;
                history[ConnectedId] = date;
                try
                {
                    await Task.Run(() => associations.RecordSuccess(pin, date));
                }
                catch (Exception error)
                {
                    Debug.LogWarning("Could not save Quest connection history: " + error.Message);
                }
            }
            catch
            {
                controls.Dispose();
                throw;
            }

            nextHeartbeat = Time.unscaledTime + 5;
            try
            {
                // Pairing is silent in automatic mode; scene reconciliation remains visible.
                if (automaticOperation && (HasSharedScene || autoSync && Module3DMain.IsInitialized && Module3DMain.Scenes.Count != 0))
                    await PresentPhaseAsync(async (_, stop) => await RestoreScenesAsync(stop), token);
                else await RestoreScenesAsync(token);
            }
            catch (Exception error)
            {
                reconciliationNeedsRetry = true;
                LastOperationCancelled = error is OperationCanceledException || token.IsCancellationRequested;
                LastOperationSucceeded = IsPaired && ReferenceEquals(sessionPreferences, controls);
                SetStatus(LastOperationSucceeded ? "Quest connected. Scene synchronization needs attention: " + TransferError.Describe(error) : "Quest disconnected. Reconnect to resume scene synchronization.");
                return;
            }

            LastOperationSucceeded = IsPaired && ReferenceEquals(sessionPreferences, controls);
            if (!LastOperationSucceeded)
            {
                SetStatus("Quest disconnected. Reconnect to resume scene synchronization.");
                return;
            }

            SetStatus(reconciliationNeedsRetry ? "Quest connected. Some visualizations could not reconnect. Select Retry." : "Quest connected. Scene synchronization is ready.");
        }

        private async Task<ConflictChoice> ChooseConflictAsync(CheckpointConflict conflict, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(stop);
            var presentation = displayReservation?.Presentation ?? pairingPresentation;
            if (presentation != null) await presentation.PauseAsync();
            try
            {
                int answer = await DialogBoxManager.OpenScrollableAsync(DialogBoxType.Warning, "Choose which changes to keep", "This choice applies to conflicts in all common visualizations during this reconnection. Each visualization has its own result.\n\n" + conflict.Description + "\n\nDesktop changes:\n" + conflict.DesktopSummary + "\n\nQuest changes:\n" + conflict.QuestSummary + "\n\nChoose Desktop to keep the Desktop version of these conflicting changes, or Quest to keep the Quest version. Independent changes from both devices are kept automatically. Select one to continue.", stop, "Desktop", "Quest");
                return answer == 0 ? ConflictChoice.Desktop : ConflictChoice.Quest;
            }
            finally
            {
                if (!stop.IsCancellationRequested) presentation?.Resume();
            }
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

        public Task<bool> SendAsync(bool retry, Base3DScene sourceScene = null)
        {
            var scene = sourceScene != null ? sourceScene : Module3DMain.IsInitialized ? Module3DMain.SelectedScene : null;
            if (scene == null)
            {
                SetStatus("Select an open visualization first.");
                return Task.FromResult(false);
            }

            var entry = GetPublication(scene);
            if (entry.Work != null && !entry.Work.IsCompleted) return entry.Work;
            if (retry) return RetryPublishedSceneAsync(scene);
            if (!retry && entry.Owner is { IsLive: true, IsClosed: false } && !entry.UserRetryRequired && entry.Owner.IsInitialRenderingReady) return Task.FromResult(true);
            if (entry.UserRetryRequired && !retry) return Task.FromResult(false);
            entry.Work = SendSceneAsync(entry, retry);
            return entry.Work;
        }

        private async Task<bool> RetryPublishedSceneAsync(Base3DScene scene)
        {
            await RetryDisplayScenesAsync(new[] { scene }, lifetime.Token);
            return IsSceneSynchronized(scene);
        }

        public void Cancel()
        {
            CancelConnectionAttempt();
            foreach (var entry in publications.Values) entry.Stop?.Cancel();
        }

        public void CancelConnectionAttempt() => operation?.Cancel();

        private Task RunAsync(Func<CancellationToken, Task> action, bool automatic = false)
        {
            if (busy) return Task.FromException(new InvalidOperationException("Another Quest operation is in progress."));
            if (!isActiveAndEnabled) return Task.FromException(new ObjectDisposedException(nameof(QuestManager)));
            automaticOperation = automatic;
            LastOperationSucceeded = false;
            BlockingError = null;
            busy = true;
            LastOperationCancelled = false;
            OnChanged.Invoke();
            return running = ExecuteAsync(action);
        }

        private async Task ExecuteAsync(Func<CancellationToken, Task> action)
        {
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            operation = attempt;
            long initialGeneration = sessionGeneration;
            bool acquired = false;

            async Task InvokeAsync(CancellationToken token)
            {
                // Finish an existing inventory before taking its session gate: it can itself await scene sends.
                if (!inventoryWork.IsCompleted)
                {
                    SetStatus("Waiting for Quest scene operations");
                    // This work already owns any scene-transfer presentation. Drain it even after cancellation,
                    // so this attempt cannot finish while the inventory still starts new sends behind it.
                    await inventoryWork;
                    token.ThrowIfCancellationRequested();
                }

                await UniTask.SwitchToMainThread(token);
                await sessionOperations.WaitAsync(token);
                acquired = true;
                await action(token);
            }

            try
            {
                if (automaticOperation) await InvokeAsync(attempt.Token);
                else
                    await PresentPhaseAsync(async (_, stop) =>
                    {
                        using var cancel = stop.Register(attempt.Cancel);
                        await InvokeAsync(attempt.Token);
                    }, attempt.Token);
            }
            catch (Exception exception)
            {
                await UniTask.SwitchToMainThread();
                bool restoreSession = reconnect && exception is not AuthenticationException && exception is not OperationCanceledException && !attempt.IsCancellationRequested;
                bool preserved = IsPaired && initialGeneration == sessionGeneration && exception is ArgumentException or InvalidOperationException or OperationCanceledException;
                if (!preserved) DisconnectTransport();
                reconnect = preserved || restoreSession;
                nextHeartbeat = Time.unscaledTime + 15;
                nextAutomaticAttempt = Time.unscaledTime + 15;
                if (automaticOperation && ConnectedId != null) automaticRetryAfter[ConnectedId] = Time.unscaledTime + 30;
                if (!automaticOperation) autoConnectSuspended = true;
                if (BlockingError != null) autoConnectSuspended = true;
                if (exception is QuestContextException)
                {
                    reconnect = false;
                    if (ConnectedId != null) automaticExcluded.Add(ConnectedId);
                }

                if (exception is AuthenticationException)
                {
                    if (ConnectedId != null) automaticExcluded.Add(ConnectedId);
                    reconnect = false;
                    if (credential != null) Array.Clear(credential, 0, credential.Length);
                    credential = null;
                    SetStatus("Pairing needs attention. Connect manually to enter the Quest code, or forget the association on Quest if it is locked.");
                }
                else if (reconciliationNeedsRetry)
                {
                    LastOperationCancelled = exception is OperationCanceledException || attempt.IsCancellationRequested;
                    SetStatus("Some visualizations could not reconnect. Select Retry.");
                }
                else if (exception is OperationCanceledException || attempt.IsCancellationRequested)
                {
                    LastOperationCancelled = true;
                    SetStatus("Operation cancelled.");
                }
                else
                    SetStatus(exception is ArgumentException || exception is InvalidOperationException ? TransferError.Describe(exception) : reconnect ? "Shared data or connection interrupted. Reconnecting with the saved association" : "Quest unreachable. Check USB or Wi-Fi.");

                Debug.LogWarning("Quest operation failed: " + exception.Message);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                pairingTiming?.Dispose();
                pairingTiming = null;
                if (acquired) sessionOperations.Release();
                operation = null;
                activeRequest = null;
                busy = false;
                OnChanged.Invoke();
            }
        }

        private string CredentialPath(byte[] identity) => associations.CredentialPath(identity);

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
                // Report on the owning Unity thread through the connection result.
            }

            return null;
        }

        private void ClearPairing()
        {
            ClearSceneLifecycle();
            reconciliationNeedsRetry = false;
            sessionPreferences?.Dispose();
            sessionPreferences = null;
            if (credential != null) Array.Clear(credential, 0, credential.Length);
            credential = pin = null;
            globals?.Dispose();
            globals = null;
            endpoint = null;
            connected = reconnect = failedDelivery = false;
            OnChanged.Invoke();
        }

        private void SetStatus(string value)
        {
            Status = value;
            pairingProgress?.Invoke(0, 0, new LoadingText(value));
            OnChanged.Invoke();
        }

        private async void OnDestroy()
        {
            PairingTiming.OnMeasured -= LogPairingTiming;
            DisposeDiscovery();
            DisposeSceneLifecycle();
            lifetime.Cancel();
            operation?.Cancel();
            try
            {
                await Task.WhenAll(new[] { running, discovering, listening, inventoryWork }.Concat(publications.Values.Where(value => value.Work != null).Select(value => (Task)value.Work)));
            }
            catch (Exception)
            {
            }

            ClearPairing();
            lifetime.Dispose();
        }
    }
}
