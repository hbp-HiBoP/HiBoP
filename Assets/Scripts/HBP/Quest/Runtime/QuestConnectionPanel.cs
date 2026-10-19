using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Transfer.Transport;
using HBP.UI.Tools;
using HBP.Core.Enums;
using HBP.Core.Tools;
using UnityEngine;

namespace HBP.Quest
{
    public sealed class QuestConnectionPanel : MonoBehaviour
    {
        [SerializeField] private QuestAnatomySession session;
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestStatusPanel statusPanel;
        private CancellationTokenSource lifetime;
        private UniTask running = UniTask.CompletedTask;
        private QuestPairing pairing;
        private string address, status = "Starting connection...";
        private bool restarting;
        private string identityPath, deviceName;
        private float nextRefresh;
        private readonly SemaphoreSlim advertisementWake = new(0, 1);
        private bool showingRequestedCode;
        private bool confirmingAssociation;
        private CancellationTokenSource associationConfirmation;
        private AndroidJavaObject networkMonitor;
        private NetworkListener networkListener;
        private int networkChanged = 1;
        private bool showingPairingProgress;

        private void WakeAdvertisement()
        {
            Interlocked.Exchange(ref networkChanged, 1);
            try
            {
                advertisementWake.Release();
            }
            catch (SemaphoreFullException)
            {
            }
        }

        private sealed class NetworkListener : AndroidJavaProxy
        {
            private readonly Action changed;
            public NetworkListener(Action changed) : base("com.hbp.quest.NetworkMonitor$Listener") => this.changed = changed;

            [UnityEngine.Scripting.Preserve]
            public void changedNetwork() => changed();
        }

        private void Awake()
        {
            // Initialize the preference path (Unity API) before any detached receiver work can access its type.
            _ = HBP.Core.Preferences.UserPreferences.PATH;
            string root = Application.persistentDataPath;
#if UNITY_ANDROID && !UNITY_EDITOR
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var directory = activity.Call<AndroidJavaObject>("getNoBackupFilesDir");
            root = directory.Call<string>("getAbsolutePath");
#endif
            identityPath = System.IO.Path.Combine(root, "QuestPairing", "identity.pair");
            deviceName = SystemInfo.deviceName;
        }

        private void OnEnable()
        {
            statusPanel.OnNewAssociationRequested.AddListener(NewAssociation);
#if UNITY_ANDROID && !UNITY_EDITOR
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            networkListener = new NetworkListener(WakeAdvertisement);
            networkMonitor = new AndroidJavaObject("com.hbp.quest.NetworkMonitor", activity, networkListener);
#endif
            PairingTiming.OnMeasured += LogTiming;
            RestartAsync().Forget(); // All failures observed by RestartAsync/RunAsync.
        }

        private static void LogTiming(string message) => Debug.Log(message);

        private bool CanChangePairing => pairing?.IsPaired == true && !restarting && !session.IsReady && session.ReceptionState == AnatomyReceptionState.Idle && !pairing.IsPreparing;

        private void NewAssociation()
        {
            if (CanChangePairing && !confirmingAssociation) ConfirmNewAssociationAsync().Forget(Debug.LogException);
        }

        private async UniTask ConfirmNewAssociationAsync()
        {
            confirmingAssociation = true;
            var owner = pairing;
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, this.GetCancellationTokenOnDestroy());
            associationConfirmation = stop;
            try
            {
                int choice = await DialogBoxManager.OpenAsync(DialogBoxType.Warning, "Forget association?", "Forget the Desktop association? You will need to enter a new code on Desktop.", stop.Token, "Forget association", "Cancel");
                if (choice == 0 && ReferenceEquals(owner, pairing) && CanChangePairing)
                    await RestartAsync(true);
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
            }
            finally
            {
                associationConfirmation = null;
                confirmingAssociation = false;
            }
        }

        public async UniTask RestartAsync(bool renew = false)
        {
            if (restarting || !isActiveAndEnabled) return;
            restarting = true;
            lifetime?.Cancel();
            try
            {
                await running;
                lifetime?.Dispose();
                if (!this || !isActiveAndEnabled) return;
                lifetime = new CancellationTokenSource();
                running = RunAsync(lifetime.Token, renew).ToAsyncLazy().Task;
            }
            catch (Exception)
            {
                status = "Could not restart. Reopen HiBoP.";
            }
            finally
            {
                restarting = false;
            }
        }

        private async UniTask RunAsync(CancellationToken stop, bool renew)
        {
            try
            {
                status = "Preparing secure pairing...";
                using var nextPairing = await UniTask.RunOnThreadPool(() =>
                {
                    stop.ThrowIfCancellationRequested();
                    return new QuestPairing(identityPath, deviceName, renew);
                });
                stop.ThrowIfCancellationRequested();
                pairing = nextPairing;
                status = "Select this Quest on Desktop, then click Pair.";
                while (!stop.IsCancellationRequested)
                {
                    try
                    {
                        var listener = new TcpListener(IPAddress.Any, QuestPairing.Port);
                        listener.Start(4);
                        var context = SynchronizationContext.Current;
                        using var advertising = CancellationTokenSource.CreateLinkedTokenSource(stop);
                        UniTask discovery = AdvertiseAsync(nextPairing, advertising.Token);
                        try
                        {
                            await nextPairing.ServeAsync(listener, stop, session.ReceiveStreamAsync, message => context.Post(_ =>
                            {
                                if (!stop.IsCancellationRequested) status = message;
                            }, null), session.ReceiveGlobalsAsync, session.ReceiveReplicaAsync, session.ReceiveSessionControlAsync, session.ActiveGlobalContextAsync, session.ReceiveDefinitionsAsync);
                        }
                        finally
                        {
                            advertising.Cancel();
                            await discovery;
                        }
                    }
                    catch (Exception exception) when (!stop.IsCancellationRequested)
                    {
                        // Keep the same pairing owner, attempt budget and globals if the
                        // network listener must be recreated after an interface failure.
                        status = "Connection interrupted. Reconnecting automatically...";
                        Debug.LogWarning("Quest listener will retry: " + exception.GetType().Name);
                        await UniTask.Delay(TimeSpan.FromMilliseconds(5000), DelayType.Realtime, cancellationToken: stop);
                    }
                }
            }
            catch (Exception) when (stop.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                status = "Connection unavailable. Check Wi-Fi and reconnect from Desktop.";
                Debug.LogWarning("QUEST-011 listener failed: " + exception.GetType().Name + ": " + exception.Message);
            }
            finally
            {
                pairing = null;
            }
        }

        private async UniTask AdvertiseAsync(QuestPairing receiver, CancellationToken stop)
        {
            try
            {
                string announcement = receiver.Announcement;
                await System.Threading.Tasks.Task.Run(() => QuestDiscovery.AdvertiseAsync(() => announcement, stop, () => !receiver.IsConnected, advertisementWake), stop);
            }
            catch (Exception exception) when (!stop.IsCancellationRequested)
            {
                Debug.LogWarning("Quest Wi-Fi discovery unavailable: " + exception.GetType().Name);
            }
        }

        public string Summary => session == null ? "Connection unavailable" : session.ReceptionState != AnatomyReceptionState.Idle ? "Receiving..." : session.IsReady ? pairing?.IsConnected == true || session.IsSynchronizationConnected ? "Connected" : "Offline" : pairing?.IsPaired == true ? "Paired" : "Pairing";

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            if (Interlocked.Exchange(ref networkChanged, 0) != 0)
                address = string.Join(" / ", NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up).SelectMany(n => n.GetIPProperties().UnicastAddresses).Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address)).Select(a => a.Address.ToString()).Distinct());
            var state = pairing?.GetStatus();
            bool pairingInProgress = state?.Phase is PairingPhase.Authenticating or PairingPhase.InstallingGlobals;
            bool requested = pairing?.IsCodeRequested == true && !pairingInProgress;
            if (requested && !showingRequestedCode) statusPanel.ShowRequestedCode();
            if (!requested && showingRequestedCode) statusPanel.Hide();
            showingRequestedCode = requested;
            if (associationConfirmation != null && !CanChangePairing) associationConfirmation.Cancel();
            if (pairing == null)
                statusPanel.ShowUnavailable(status);
            else if (pairing.IsPaired)
                statusPanel.ShowPaired(pairing.DesktopAddress, CanChangePairing && !confirmingAssociation);
            else
            {
                if (pairingInProgress)
                {
                    statusPanel.Hide();
                    if (!showingPairingProgress && !pairing.IsPreparing)
                        ShowPairingProgressAsync(pairing, lifetime.Token).Forget(Debug.LogException);
                }

                statusPanel.ShowPairing(state.Value, address);
            }
        }

        private async UniTask ShowPairingProgressAsync(QuestPairing owner, CancellationToken stop)
        {
            showingPairingProgress = true;
            try
            {
                await LoadingManager.LoadAsync(async update =>
                {
                    update(0, 0, new LoadingText("Pairing with Desktop"));
                    try
                    {
                        // The globals receiver owns the same loading presenter once data starts arriving.
                        await UniTask.WaitUntil(() => !ReferenceEquals(owner, pairing) || owner.IsPreparing || owner.GetStatus().Phase is not (PairingPhase.Authenticating or PairingPhase.InstallingGlobals), cancellationToken: stop);
                    }
                    catch (OperationCanceledException) when (stop.IsCancellationRequested)
                    {
                    }
                });
            }
            finally
            {
                showingPairingProgress = false;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            // Keep the identity, code attempt budget and globals across normal headset sleep.
            if (!paused && isActiveAndEnabled && running.Status.IsCompleted()) RestartAsync().Forget();
            if (!paused) WakeAdvertisement();
        }

        private void OnDisable()
        {
            associationConfirmation?.Cancel();
            statusPanel.OnNewAssociationRequested.RemoveListener(NewAssociation);
            networkMonitor?.Call("close");
            networkMonitor?.Dispose();
            networkMonitor = null;
            networkListener = null;
            PairingTiming.OnMeasured -= LogTiming;
            lifetime?.Cancel();
        }
    }
}
