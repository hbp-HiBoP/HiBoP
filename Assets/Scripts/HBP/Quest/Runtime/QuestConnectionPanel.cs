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
        private bool details = true;
        private float nextRefresh;

        private void OnEnable()
        {
            restart = new InputAction("Restart pairing (Y)", binding: "<XRController>{LeftHand}/secondaryButton");
            toggle = new InputAction("Connection panel (B)", binding: "<XRController>{RightHand}/secondaryButton");
            restart.Enable();
            toggle.Enable();
            _ = RestartAsync(); // All failures observed by RestartAsync/RunAsync.
        }

        public async Task RestartAsync()
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
                running = RunAsync(lifetime.Token);
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

        private async Task RunAsync(CancellationToken stop)
        {
            try
            {
                status = "Preparing secure pairing...";
                using var nextPairing = await Task.Run(() => new QuestPairing(), stop);
                stop.ThrowIfCancellationRequested();
                pairing = nextPairing;
                address = string.Join(" / ", NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up).SelectMany(n => n.GetIPProperties().UnicastAddresses).Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address)).Select(a => a.Address.ToString()).Distinct());
                status = "Open Quest in Desktop, enter this address.";
                var listener = new TcpListener(IPAddress.Any, QuestPairing.Port);
                listener.Start(4);
                var context = SynchronizationContext.Current;
                await nextPairing.ServeAsync(listener, stop, session.ReceiveStreamAsync, message => context.Post(_ =>
                {
                    if (!stop.IsCancellationRequested) status = message;
                }, null));
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

        private void Update()
        {
            if (restart.WasPressedThisFrame()) _ = RestartAsync();
            if (toggle.WasPressedThisFrame()) details = !details;
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;
            string progress = session.ReceptionState switch
            {
                AnatomyReceptionState.Connecting => "Connecting...",
                AnatomyReceptionState.Receiving => $"Receiving: {session.ReceivedBytes / 1024} KiB",
                AnatomyReceptionState.Preparing => "Preparing anatomy...",
                _ => status
            };
            string content = session.IsReady && view.IEEG != null ? view.IEEG.Summary + "\nInputs received; iEEG rendering pending" : session.IsReady ? $"Anatomy ready | {view.Contacts.Sites.Count} contacts\nAvailable offline" : "No anatomy received";
            if (view.DensityComputing) content += "\nCalculating local density...";
            else if (view.DensityError != null) content += "\nDensity failed: " + view.DensityError;
            else if (view.Density != null) content += $"\nDensity ready | max {view.Density.MaxDensity:G5}\nRight stick click: recalculate offline";
            string surface = view.SurfaceHidden ? "A: show brain" : "A: hide brain";
            if (!details && session.IsReady)
            {
                statusText.text = $"B: connection panel | X: recenter\n{surface}";
                return;
            }

            string credentials = pairing == null ? "" : pairing.IsPaired ? "Paired with Desktop" : pairing.IsLocked ? "Pairing expired/locked. Y: new code" : $"Code: {pairing.Code}\nCompare ALL fingerprint groups on Desktop:\n{pairing.Fingerprint}";
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
            if (paused) lifetime?.Cancel();
            else if (isActiveAndEnabled) _ = RestartAsync();
        }

        private void OnDisable()
        {
            restart?.Dispose();
            toggle?.Dispose();
            lifetime?.Cancel();
        }
    }
}
