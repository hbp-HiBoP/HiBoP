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
        private CancellationTokenSource discoveryWatch;
        private Task listening = Task.CompletedTask;
        private bool automaticallyConnect, autoConnectSuspended, historyReady;
        private float consolidateUntil, nextAutomaticAttempt;

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
            bool watch = automaticallyConnect || discoveryActive || reconnect;
            if (watch && discoveryWatch == null)
            {
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
                presence.Clear();
                devices.Clear();
            }
        }

        private async Task ListenAnnouncementsAsync(SynchronizationContext context, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Run(() => QuestDiscovery.WatchAsync((device, present) => context.Post(_ =>
                    {
                        if (!this || token.IsCancellationRequested) return;
                        string key = device.Id + "|" + device.Endpoint.Key;
                        bool added = !presence.ContainsKey(key);
                        if (present) Observe(device);
                        else presence.Remove(key);
                        if (present && added)
                        {
                            consolidateUntil = Math.Max(consolidateUntil, Time.unscaledTime + 0.25f);
                            automaticRetryAfter.Remove(device.Id);
                        }

                        PublishPresence();
                    }, null), message => context.Post(_ =>
                    {
                        if (!this || token.IsCancellationRequested) return;
                        DiscoveryMessages = new[] { message };
                        OnChanged.Invoke();
                    }, null), token), token);
                    if (!token.IsCancellationRequested) await Task.Delay(2000, token);
                }
            }
            catch (Exception error) when (!token.IsCancellationRequested)
            {
                Debug.LogWarning("Quest announcements unavailable: " + error.Message);
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
            }
        }

        private void RequestDiscovery() => consolidateUntil = Time.unscaledTime + 0.25f;

        private void TickDiscovery()
        {
            UpdateDiscoveryWatch();
            if (!historyReady) return;
            if (busy || scanning || IsPaired || !(automaticallyConnect || reconnect) || autoConnectSuspended || Time.unscaledTime < consolidateUntil || Time.unscaledTime < nextAutomaticAttempt) return;
            nextAutomaticAttempt = Time.unscaledTime + 1;
            if (history.Count == 0) return;
            var available = devices.Where(item => !automaticRetryAfter.TryGetValue(item.Id, out float retry) || Time.unscaledTime >= retry);
            var device = reconnect ? available.FirstOrDefault(item => item.Id == ConnectedId && !automaticExcluded.Contains(item.Id)) : PairingAssociations.Choose(available, history, automaticExcluded);
            if (device == null)
            {
                // A remembered Manual IP also remains usable on networks that suppress mDNS.
                // Honor the same retry cooldown; do not race the old heartbeat against it.
                if (reconnect && manualSelected && selectedPin != null && !automaticExcluded.Contains(ConnectedId) && (!automaticRetryAfter.TryGetValue(ConnectedId, out float retry) || Time.unscaledTime >= retry))
                {
                    activeRequest = ConnectedId;
                    _ = RunAsync(token => ConnectSelectedAsync(null, true, token), true);
                }

                return;
            }

            Select(device);
            activeRequest = device.Id;
            _ = RunAsync(token => ConnectCandidatesAsync(null, true, token), true);
        }

        private async Task ConnectCandidatesAsync(Func<CancellationToken, UniTask<HBP.UI.Tools.InputDialogResult>> requestCode, bool automatic, CancellationToken token)
        {
            if (manualSelected || IsSelectedConnected)
            {
                await ConnectSelectedAsync(requestCode, automatic, token);
                return;
            }

            var endpoints = presence.Values.Where(item => item.Device.Id == SelectedId && item.Expires > Time.unscaledTime).OrderBy(item => item.Device.Endpoint.Path != QuestNetworkPath.Usb).Select(item => item.Device).ToArray();
            Exception failure = null;
            foreach (var candidate in endpoints)
            {
                if (!presence.ContainsKey(candidate.Id + "|" + candidate.Endpoint.Key)) continue;
                selectedEndpoint = candidate.Endpoint.AddressText;
                try
                {
                    await ConnectSelectedAsync(requestCode, automatic, token);
                    return;
                }
                catch (Exception error) when (error is not System.Security.Authentication.AuthenticationException && error is not QuestContextException && !token.IsCancellationRequested)
                {
                    failure = error;
                    if (BlockingError != null) break;
                }
            }

            throw failure ?? new InvalidOperationException("Quest is no longer available.");
        }

        private void Observe(QuestDevice device)
        {
            string key = device.Id + "|" + device.Endpoint.Key;
            if (presence.Count >= 128 && !presence.ContainsKey(key)) return;
            presence[key] = (device, float.PositiveInfinity);
        }

        private void PublishPresence()
        {
            foreach (string key in presence.Where(item => item.Value.Expires <= Time.unscaledTime && item.Value.Device.Id != (IsPaired ? ConnectedId : null)).Select(item => item.Key).ToArray()) presence.Remove(key);
            devices = presence.Values.Select(item => item.Device).GroupBy(device => device.Id).Select(group => group.OrderBy(device => device.Endpoint.Path != QuestNetworkPath.Usb).First()).OrderBy(device => device.Label).ToList();
            DiscoveryMessages = new[] { devices.Count == 0 ? "No Quest found. Check USB or Wi-Fi, or use Manual IP." : $"{devices.Count} Quest headset(s) found" };
            if (!manualSelected && SelectedId == null && devices.Count != 0 && !busy) Select(devices[0]);
            OnChanged.Invoke();
        }

        private void DisposeDiscovery()
        {
            sceneLifecyclePreferences?.OnSavePreferences.RemoveListener(ApplyConnectionPreferences);
            discoveryWatch?.Cancel();
            discoveryWatch?.Dispose();
            discoveryWatch = null;
            presence.Clear();
            devices.Clear();
        }
    }
}
