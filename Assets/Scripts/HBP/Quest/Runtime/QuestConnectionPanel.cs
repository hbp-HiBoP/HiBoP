using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using HBP.Transfer.Transport;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HBP.Quest
{
    public sealed class QuestConnectionPanel : MonoBehaviour
    {
        [SerializeField] private QuestAnatomySession session;
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private TextMesh statusText;
        private InputAction restart, toggle;
        private CancellationTokenSource lifetime;
        private Task running = Task.CompletedTask;
        private QuestPairing pairing;
        private string address, status = "Starting connection...";
        private bool restarting;
        private string identityPath, deviceName;
        private bool details = true;
        private float nextRefresh;

        private void Awake()
        {
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
            restart = new InputAction("Restart pairing (Y)", binding: "<XRController>{LeftHand}/secondaryButton");
            toggle = new InputAction("Connection panel (B)", binding: "<XRController>{RightHand}/secondaryButton");
            restart.Enable();
            toggle.Enable();
            _ = RestartAsync(); // All failures observed by RestartAsync/RunAsync.
        }

        public async Task RestartAsync(bool renew = false)
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
                details = true;
                running = RunAsync(lifetime.Token, renew);
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

        private async Task RunAsync(CancellationToken stop, bool renew)
        {
            try
            {
                status = "Preparing secure pairing...";
                using var nextPairing = await Task.Run(() => new QuestPairing(identityPath, deviceName, renew), stop);
                stop.ThrowIfCancellationRequested();
                pairing = nextPairing;
                address = string.Join(" / ", NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up).SelectMany(n => n.GetIPProperties().UnicastAddresses).Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address)).Select(a => a.Address.ToString()).Distinct());
                status = "Select this Quest on Desktop, then click Pair.";
                while (!stop.IsCancellationRequested)
                {
                    try
                    {
                        var listener = new TcpListener(IPAddress.Any, QuestPairing.Port);
                        listener.Start(4);
                        var context = SynchronizationContext.Current;
                        using var advertising = CancellationTokenSource.CreateLinkedTokenSource(stop);
                        Task discovery = AdvertiseAsync(nextPairing, advertising.Token);
                        try
                        {
                            await nextPairing.ServeAsync(listener, stop, session.ReceiveStreamAsync, message => context.Post(_ =>
                            {
                                if (!stop.IsCancellationRequested) status = message;
                            }, null), session.ReceiveGlobalsAsync);
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
                        await Task.Delay(5000, stop);
                    }
                }
            }
            catch (Exception) when (stop.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                status = "Connection unavailable. Check Wi-Fi, then press Y to retry.";
                Debug.LogWarning("QUEST-011 listener failed: " + exception.GetType().Name + ": " + exception.Message);
            }
            finally
            {
                pairing = null;
            }
        }

        private static async Task AdvertiseAsync(QuestPairing receiver, CancellationToken stop)
        {
            try
            {
                await QuestDiscovery.AdvertiseAsync(() => receiver.Announcement, stop);
            }
            catch (Exception exception) when (!stop.IsCancellationRequested)
            {
                Debug.LogWarning("Quest Wi-Fi discovery unavailable: " + exception.GetType().Name);
            }
        }

        private void Update()
        {
            if (restart.WasPressedThisFrame()) _ = RestartAsync(true);
            if (toggle.WasPressedThisFrame()) details = !details;
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            string progress = session.ReceptionState switch
            {
                AnatomyReceptionState.Connecting => "Connecting...",
                AnatomyReceptionState.Receiving => $"Receiving: {session.ReceivedBytes / 1024} KiB",
                AnatomyReceptionState.Preparing => "Preparing all visualization columns...",
                _ => status
            };
            string content = view.Summary;
            string surface = view.SurfaceHidden ? "A: show brain" : "A: hide brain";
            if (!details && session.IsReady)
            {
                statusText.text = $"B: connection panel | X: recenter\n{surface}";
                return;
            }

            string credentials = pairing == null ? "" : pairing.IsPaired ? (pairing.IsConnected ? "Paired · Desktop connected" : "Paired · waiting for Desktop to reconnect") : pairing.IsPreparing ? "Installing preferences and shared data..." : pairing.IsLocked ? "Pairing expired/locked. Y: new code" : $"Code: {pairing.Code}\nEnter once on Desktop; this Quest will be remembered.";
            statusText.text = Wrap($"HiBoP | Quest connection\n{address}\n{credentials}\n\n{progress}\n{content}\nY: restart pairing | B: hide/show panel\nIndex triggers: move / rotate / scale\nX: recenter | {surface}");
        }

        private static string Wrap(string value)
        {
            var result = new StringBuilder();
            foreach (string paragraph in value.Split('\n'))
            {
                int width = 0;
                foreach (string word in paragraph.Split(' '))
                {
                    if (width > 0 && width + word.Length + 1 > 38)
                    {
                        result.Append('\n');
                        width = 0;
                    }

                    if (width > 0)
                    {
                        result.Append(' ');
                        width++;
                    }

                    result.Append(word);
                    width += word.Length;
                }

                result.Append('\n');
            }

            return result.ToString().TrimEnd('\n');
        }

        private void OnApplicationPause(bool paused)
        {
            // Keep the identity, code attempt budget and globals across normal headset sleep.
            if (!paused && isActiveAndEnabled && running.IsCompleted) _ = RestartAsync();
        }

        private void OnDisable()
        {
            restart?.Dispose();
            toggle?.Dispose();
            lifetime?.Cancel();
        }
    }
}
