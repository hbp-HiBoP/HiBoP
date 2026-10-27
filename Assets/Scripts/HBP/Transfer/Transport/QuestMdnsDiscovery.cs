using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Makaretu.Dns;

namespace HBP.Transfer.Transport
{
    public static class QuestDiscovery
    {
        private static long nextGeneration;

        public static async Task<List<QuestDevice>> FindAsync(CancellationToken stop, Action<QuestDevice> discovered = null, Action<string> error = null)
        {
            var found = new ConcurrentDictionary<string, QuestDevice>();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop);
            timeout.CancelAfter(2000);
            await WatchAsync((device, present) =>
            {
                string key = device.Id + "|" + device.Endpoint.Key;
                if (present)
                {
                    found[key] = device;
                    discovered?.Invoke(device);
                }
                else if (!timeout.IsCancellationRequested) found.TryRemove(key, out _);
            }, error ?? (_ => { }), timeout.Token).ConfigureAwait(false);
            stop.ThrowIfCancellationRequested();
            return found.Values.ToList();
        }

        public static async Task WatchAsync(Action<QuestDevice, bool> changed, Action<string> error, CancellationToken stop)
        {
            // Called on a worker: upstream's synchronous query API must never block Unity's PlayerLoop.
            var clock = Stopwatch.StartNew();
            var previous = new Dictionary<string, QuestDevice>();
            string fingerprint = null;
            long generation = 0;
            MulticastService mdns = null;
            QuestMdnsCache cache = null;
            var packets = new ConcurrentQueue<MessageEventArgs>();
            double inspectAt = 0, queryAt = 0;
            int interval = 1;
            int refreshSignal = 1;
            bool subscribed = false;
            NetworkAddressChangedEventHandler networkChanged = (_, __) => Interlocked.Exchange(ref refreshSignal, 1);
            try
            {
                try
                {
                    NetworkChange.NetworkAddressChanged += networkChanged;
                    subscribed = true;
                }
                catch (PlatformNotSupportedException)
                {
                    /* Interface snapshots below remain available on IL2CPP. */
                }
                catch (NotImplementedException)
                {
                }

                while (!stop.IsCancellationRequested)
                {
                    double now = clock.Elapsed.TotalSeconds;
                    if (now >= inspectAt || Interlocked.Exchange(ref refreshSignal, 0) != 0)
                    {
                        inspectAt = now + 2;
                        var networks = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.SupportsMulticast && n.NetworkInterfaceType != NetworkInterfaceType.Loopback).ToArray();
                        string current = string.Join("|", networks.Select(InterfaceFingerprint).OrderBy(s => s));
                        if (mdns == null || current != fingerprint)
                        {
                            if (mdns != null)
                            {
                                await mdns.StopAsync().ConfigureAwait(false);
                                mdns.Dispose();
                                mdns = null;
                            }

                            foreach (var device in previous.Values) changed(device, false);
                            previous.Clear();
                            while (packets.TryDequeue(out _))
                            {
                            }

                            cache = new QuestMdnsCache();
                            generation = Interlocked.Increment(ref nextGeneration);
                            fingerprint = current;
                            mdns = new MulticastService(_ => networks) { IgnoreDuplicateMessages = false };
                            mdns.AnswerReceived += (_, packet) =>
                            {
                                if (packets.Count < 256) packets.Enqueue(packet);
                            };
                            mdns.Start();
                            interval = 1;
                            queryAt = now;
                        }
                    }

                    if (mdns.HasReceiverFailure) throw new System.IO.IOException("The network interface stopped receiving mDNS packets.");
                    while (packets.TryDequeue(out var packet)) cache.Add(packet.Message, packet.InterfaceIndex, now);
                    var available = cache.Devices(now, generation).ToDictionary(d => d.Id + "|" + d.Endpoint.Key);
                    foreach (var old in previous.Where(p => !available.ContainsKey(p.Key))) changed(old.Value, false);
                    foreach (var device in available.Values)
                        if (!previous.TryGetValue(device.Id + "|" + device.Endpoint.Key, out var old) || old.Name != device.Name)
                            changed(device, true);
                    previous = available;
                    if (now >= queryAt)
                    {
                        mdns.SendQuery(QuestMdnsCache.ServiceType, type: DnsType.PTR);
                        foreach (var name in cache.Unresolved(now)) mdns.SendQuery(name);
                        queryAt = now + interval;
                        interval = Math.Min(30, interval * 2);
                    }

                    await Task.Delay(250, stop).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
            }
            catch (Exception failure) when (!stop.IsCancellationRequested)
            {
                error("Quest mDNS discovery unavailable: " + failure.Message);
            }
            finally
            {
                if (subscribed) NetworkChange.NetworkAddressChanged -= networkChanged;
                try
                {
                    if (mdns != null) await mdns.StopAsync().ConfigureAwait(false);
                }
                catch (Exception failure)
                {
                    if (!stop.IsCancellationRequested) error("Quest discovery is restarting: " + failure.Message);
                }
                finally
                {
                    mdns?.Dispose();
                    foreach (var device in previous.Values) changed(device, false);
                }
            }
        }

        private static string InterfaceFingerprint(NetworkInterface network)
        {
            var properties = network.GetIPProperties();
            int ipv4 = network.Supports(NetworkInterfaceComponent.IPv4) ? properties.GetIPv4Properties().Index : 0;
            int ipv6 = network.Supports(NetworkInterfaceComponent.IPv6) ? properties.GetIPv6Properties().Index : 0;
            return network.Id + ":" + ipv4 + ":" + ipv6 + ":" + string.Join(",", properties.UnicastAddresses.Select(a => a.Address.ToString()).OrderBy(a => a));
        }
    }
}
