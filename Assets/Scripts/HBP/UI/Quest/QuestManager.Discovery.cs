using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Transfer.Transport;
using HBP.UI.Quest;
using UnityEngine;

namespace HBP.Quest.Desktop
{
    public sealed partial class QuestManager
    {
        private PairingAssociations associations;
        private Dictionary<string, long> history = new(StringComparer.Ordinal);
        private readonly HashSet<string> automaticExcluded = new(StringComparer.Ordinal);
        private readonly Dictionary<string, float> automaticRetryAfter = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (QuestDevice Device, float Expires)> presence = new(StringComparer.Ordinal);
        private QuestDeviceNotifications notifications;
        private CancellationTokenSource discoveryWatch;
        private Task listening = Task.CompletedTask;
        private bool automaticallyConnect, autoConnectSuspended, requestWifi, requestUsb, historyReady;
        private float consolidateUntil, nextAutomaticAttempt, nextUsbProbe, debounceUsb;

        private void InitializeDiscovery()
        {
            associations = new PairingAssociations(store);
            automaticallyConnect = sceneLifecyclePreferences.General.Quest.AutomaticallyConnect;
            sceneLifecyclePreferences.OnSavePreferences.AddListener(ApplyConnectionPreferences);
            discovering = ReadHistoryAsync();
            UpdateDiscoveryWatch();
        }

        private async Task ReadHistoryAsync()
        {
            try
            {
                var loaded = await Task.Run(associations.ReadHistory, lifetime.Token);
                foreach (var item in loaded)
                    if (!history.TryGetValue(item.Key, out long date) || date < item.Value)
                        history[item.Key] = item.Value;
                historyReady = true;
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
            {
            }
            catch (Exception error)
            {
                Debug.LogWarning("Quest connection history unavailable: " + error.Message);
                historyReady = true;
            }
        }

        private void ApplyConnectionPreferences()
        {
            bool enabled = sceneLifecyclePreferences.General.Quest.AutomaticallyConnect;
            if (enabled == automaticallyConnect) return;
            automaticallyConnect = enabled;
            if (!enabled && automaticOperation && busy && !reconnect) operation?.Cancel();
            UpdateDiscoveryWatch();
            if (enabled) RequestDiscovery();
        }

        private void UpdateDiscoveryWatch()
        {
            bool watch = automaticallyConnect || discoveryActive || reconnect && !IsPaired;
            if (watch && discoveryWatch == null)
            {
                notifications = new QuestDeviceNotifications();
                discoveryWatch = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                var context = SynchronizationContext.Current;
                listening = ListenAnnouncementsAsync(context, discoveryWatch.Token);
                RequestDiscovery();
            }
            else if (!watch && discoveryWatch != null)
            {
                discoveryWatch.Cancel();
                discoveryWatch.Dispose();
                discoveryWatch = null;
                notifications?.Dispose();
                notifications = null;
            }
        }

        private async Task ListenAnnouncementsAsync(SynchronizationContext context, CancellationToken token)
        {
            try
            {
                await Task.Run(() => QuestDiscovery.ListenAsync(device => context.Post(_ =>
                {
                    if (!this || token.IsCancellationRequested) return;
                    Observe(device);
                    consolidateUntil = Math.Max(consolidateUntil, Time.unscaledTime + 1.5f);
                    PublishPresence();
                }, null), token), token);
            }
            catch (Exception error) when (!token.IsCancellationRequested)
            {
                Debug.LogWarning("Quest announcements unavailable: " + error.Message);
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
            }
        }

        private void RequestDiscovery()
        {
            requestWifi = requestUsb = true;
            consolidateUntil = Time.unscaledTime + 1.5f;
        }

        private void TickDiscovery()
        {
            UpdateDiscoveryWatch();
            if (!historyReady) return;
            if (notifications?.TakeNetworkChange() == true) requestWifi = true;
            if (notifications?.TakeUsbChange() == true)
            {
                requestUsb = true;
                debounceUsb = Time.unscaledTime + 0.75f;
            }

            if (discoveryWatch == null) return;
            if (!scanning && (!IsPaired || discoveryActive) && (requestWifi || requestUsb && Time.unscaledTime >= debounceUsb || Time.unscaledTime >= nextUsbProbe))
            {
                bool wifi = requestWifi, inventory = requestUsb && Time.unscaledTime >= debounceUsb;
                requestWifi = false;
                if (inventory) requestUsb = false;
                discovering = DiscoverAsync(wifi, inventory, discoveryWatch.Token);
            }

            // Presence expiration is timer driven, never a network scan per frame.
            if (Time.unscaledTime >= nextDiscovery)
            {
                nextDiscovery = Time.unscaledTime + 1;
                if (presence.Values.Any(item => item.Expires <= Time.unscaledTime)) PublishPresence();
            }

            if (busy || scanning || IsPaired || !(automaticallyConnect || reconnect) || autoConnectSuspended || Time.unscaledTime < consolidateUntil || Time.unscaledTime < nextAutomaticAttempt) return;
            nextAutomaticAttempt = Time.unscaledTime + 1;
            if (devices.Count == 0 || history.Count == 0) return;
            var available = devices.Where(item => !automaticRetryAfter.TryGetValue(item.Id, out float retry) || Time.unscaledTime >= retry);
            var device = reconnect ? available.FirstOrDefault(item => item.Id == ConnectedId && !automaticExcluded.Contains(item.Id)) : PairingAssociations.Choose(available, history, automaticExcluded);
            if (device == null) return;
            Select(device);
            activeRequest = device.Id;
            _ = RunAsync(async token =>
            {
                var endpoints = presence.Values.Where(item => item.Device.Id == device.Id && item.Expires > Time.unscaledTime).OrderBy(item => item.Device.UsbSerial == null).Select(item => item.Device).ToArray();
                Exception failure = null;
                foreach (var candidate in endpoints)
                {
                    selectedEndpoint = candidate.Host;
                    try
                    {
                        await ConnectSelectedAsync(null, true, token);
                        return;
                    }
                    catch (Exception error) when (error is not System.Security.Authentication.AuthenticationException && error is not QuestContextException && !token.IsCancellationRequested)
                    {
                        failure = error;
                        if (BlockingError != null) break;
                    }
                }

                throw failure ?? new InvalidOperationException("Quest is no longer available.");
            }, true);
        }

        private async Task DiscoverAsync(bool wifi, bool inventory, CancellationToken token)
        {
            scanning = true;
            try
            {
                var wifiTask = wifi ? Task.Run(() => QuestDiscovery.FindAsync(token), token) : Task.FromResult(new List<QuestDevice>());
                var usbTask = Task.Run(() => inventory ? usb.FindAsync(token) : usb.ProbeAsync(token), token);

                async Task<(List<QuestDevice> Devices, string Error)> CollectAsync(Task<List<QuestDevice>> source, string transport)
                {
                    try
                    {
                        return (await source, null);
                    }
                    catch (Exception error) when (!token.IsCancellationRequested)
                    {
                        return (new List<QuestDevice>(), transport + " discovery unavailable: " + TransferError.Describe(error));
                    }
                }

                var results = await Task.WhenAll(CollectAsync(wifiTask, "Wi-Fi"), CollectAsync(usbTask, "USB"));
                if (token.IsCancellationRequested) return;
                // A complete USB probe retires old mappings whose application or cable disappeared.
                if (results[1].Error == null)
                    foreach (string key in presence.Where(item => item.Value.Device.UsbSerial != null).Select(item => item.Key).ToArray())
                        presence.Remove(key);
                foreach (var device in results.SelectMany(items => items.Devices)) Observe(device);
                PublishPresence();
                DiscoveryMessages = results.Where(item => item.Error != null).Select(item => item.Error).Append(devices.Count == 0 ? "No Quest found. Try Manual IP." : $"{devices.Count} Quest headset(s) found").ToArray();
            }
            catch (Exception error) when (!token.IsCancellationRequested)
            {
                DiscoveryMessages = new[] { "Quest discovery unavailable: " + TransferError.Describe(error) };
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
            }
            finally
            {
                scanning = false;
                nextUsbProbe = usb.HasForwards ? Time.unscaledTime + 10 : float.PositiveInfinity;
                consolidateUntil = Time.unscaledTime + 0.25f;
                if (this) OnChanged.Invoke();
            }
        }

        private void Observe(QuestDevice device)
        {
            string key = device.Id + "|" + device.Host;
            if (presence.Count >= 128 && !presence.ContainsKey(key)) return;
            presence[key] = (device, Time.unscaledTime + (device.UsbSerial == null ? 15 : 30));
        }

        private void PublishPresence()
        {
            foreach (string key in presence.Where(item => item.Value.Expires <= Time.unscaledTime && item.Value.Device.Id != (IsPaired ? ConnectedId : null)).Select(item => item.Key).ToArray()) presence.Remove(key);
            devices = presence.Values.Select(item => item.Device).GroupBy(device => device.Id).Select(group => group.OrderBy(device => device.UsbSerial == null).First()).OrderBy(device => device.Label).ToList();
            if (!manualSelected && SelectedId == null && devices.Count != 0 && !busy) Select(devices[0]);
            OnChanged.Invoke();
        }

        private void DisposeDiscovery()
        {
            sceneLifecyclePreferences?.OnSavePreferences.RemoveListener(ApplyConnectionPreferences);
            discoveryWatch?.Cancel();
            discoveryWatch?.Dispose();
            discoveryWatch = null;
            notifications?.Dispose();
            notifications = null;
        }
    }
}
