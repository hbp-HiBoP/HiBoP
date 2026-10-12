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
    public sealed partial class QuestManager : Manager<QuestManager>
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
        public bool CanRetrySceneOperations => CanRetryReconciliation || CanRetryScenes;
        private IDisposable pairingTiming;
        public bool IsPaired => connected && credential != null;
        public bool CanRetry => HasRetryableDelivery && IsPaired;
        public bool HasRetryableDelivery => publications.Values.Any(value => value.Error != null && value.Offer?.CanRetry == true);

        private byte[] pin, credential;
        private string endpoint, store;
        private bool reconciliationNeedsRetry;
        private PairingSnapshot globals;
        private DesktopSessionPreferences sessionPreferences;
        private readonly SemaphoreSlim sessionOperations = new(1, 1);
        private long sessionGeneration;
        private long discoveryGeneration;
        public string PreferencesSyncStatus => IsPaired ? sessionPreferences?.AtlasStatus ?? "Quest session is connecting." : "Quest disconnected. Atlas actions are local.";
        public bool CanRetryAtlas => IsPaired && sessionPreferences?.RetryAtlasId != null;
        public bool HasSharedScene => publications.Values.Any(value => !value.Removed && value.Binding != null && value.Scene != null && !value.Scene.IsClosing);

        public async Task<string> ValidatePreferencesChangeAsync(NormalizationType requested)
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
                return "Cannot prepare the Quest reload: " + error.Message;
            }
        }

        public string ValidatePreferencesChangeNow(NormalizationType requested)
        {
            if (requested == PersistentDataManager.UserPreferences.Data.EEG.Normalization) return null;
            if (busy || reloadingScenes || restoringScenes || !inventoryWork.IsCompleted || publications.Values.Any(value => value.Work != null && !value.Work.IsCompleted)) return "Wait for Quest operations to finish before reloading visualizations.";
            return HasSharedScene && !IsPaired ? "Reconnect Quest before reloading shared visualizations." : null;
        }


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
            InitializeSceneLifecycle();
        }

        private static void LogPairingTiming(string message) => Debug.Log(message);

        private void Update()
        {
            TickSceneLifecycle();
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
                    if (!connected || !ready || NeedsSceneReconnection) await RestoreAsync(token);
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
                if (IsPaired && !reconciliationNeedsRetry && CanRetryScenes)
                {
                    await RetryFailedScenesAsync(token);
                    return;
                }

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
                    if (CanRetryScenes) await RetryFailedScenesAsync(token);
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
            await RestoreScenesAsync(token);
            SetStatus(reconciliationNeedsRetry ? "Quest connected. Some visualizations could not reconnect. Select Retry." : "Quest connected. Scene synchronization is ready.");
        }

        private static async Task<V2ConflictChoice> ChooseConflictAsync(V2CheckpointConflict conflict, CancellationToken stop)
        {
            await UniTask.SwitchToMainThread(stop);
            int answer = await DialogBoxManager.OpenScrollableAsync(DialogBoxType.Warning, "Choose which changes to keep", "This choice applies to conflicts in all common visualizations during this reconnection. Each visualization has its own result.\n\n" + conflict.Description + "\n\nDesktop changes:\n" + conflict.DesktopSummary + "\n\nQuest changes:\n" + conflict.QuestSummary + "\n\nChoose Desktop to keep the Desktop version of these conflicting changes, or Quest to keep the Quest version. Independent changes from both devices are kept automatically. Select one to continue.", stop, "Desktop", "Quest");
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
            if (!retry && entry.Owner is { IsLive: true, IsClosed: false }) return Task.FromResult(true);
            entry.Work = SendSceneAsync(entry, retry);
            return entry.Work;
        }

        public void Cancel()
        {
            operation?.Cancel();
            foreach (var entry in publications.Values) entry.Stop?.Cancel();
        }

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
                    SetStatus("Some visualizations could not reconnect. Select Retry.");
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
            ClearSceneLifecycle();
            reconciliationNeedsRetry = false;
            sessionPreferences?.Dispose();
            sessionPreferences = null;
            if (credential != null) Array.Clear(credential, 0, credential.Length);
            credential = pin = null;
            globals?.Dispose();
            globals = null;
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
            DisposeSceneLifecycle();
            lifetime.Cancel();
            operation?.Cancel();
            try
            {
                await Task.WhenAll(new[] { running, discovering, inventoryWork }.Concat(publications.Values.Where(value => value.Work != null).Select(value => (Task)value.Work)));
            }
            catch (Exception)
            {
            }

            ClearPairing();
            lifetime.Dispose();
        }
    }
}
