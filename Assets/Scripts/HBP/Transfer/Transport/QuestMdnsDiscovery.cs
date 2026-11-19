using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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

        public static async Task WatchAsync(Action<QuestDevice, bool> changed, Action<string> error, CancellationToken stop, Func<bool> refresh = null, Action<string> diagnostic = null)
        {
            // Called on a worker: upstream's synchronous query API must never block Unity's PlayerLoop.
            var clock = Stopwatch.StartNew();
            var previous = new Dictionary<string, QuestDevice>();
            string fingerprint = null;
            long generation = 0;
            MulticastService mdns = null;
            QuestMdnsCache cache = null;
            var packets = new ConcurrentQueue<MessageEventArgs>();
            var interfaces = new Dictionary<(AddressFamily, int), NetworkInterface>();
            double inspectAt = 0, queryAt = 0;
            double reportAt = 0;
            int received = 0, questAnswers = 0, missingInterface = 0;
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
                    if (refresh?.Invoke() == true)
                    {
                        received = questAnswers = missingInterface = 0;
                        reportAt = now + 2.5;
                        inspectAt = queryAt = now;
                        interval = 1;
                        cache = new QuestMdnsCache();
                        while (packets.TryDequeue(out _))
                        {
                        }
                    }

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
                            interfaces.Clear();
                            foreach (var network in networks)
                            foreach (var family in new[] { AddressFamily.InterNetwork, AddressFamily.InterNetworkV6 })
                            {
                                int index = QuestEndpoint.GetInterfaceIndex(network, family);
                                if (index > 0) interfaces[(family, index)] = network;
                            }

                            generation = Interlocked.Increment(ref nextGeneration);
                            fingerprint = current;
                            var multicastNetworks = QuestMulticastNetworkInterface.NeedsAdapter
                                ? networks.Select(network => (NetworkInterface)new QuestMulticastNetworkInterface(network)).ToArray()
                                : networks;
                            mdns = new MulticastService(_ => multicastNetworks) { IgnoreDuplicateMessages = false };
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
                    while (packets.TryDequeue(out var packet))
                    {
                        if (reportAt > 0) received++;
                        NetworkInterface network;
                        if (packet.RemoteEndPoint != null)
                            interfaces.TryGetValue((packet.RemoteEndPoint.AddressFamily, packet.InterfaceIndex), out network);
                        else
                        {
                            var matches = interfaces.Where(item => item.Key.Item2 == packet.InterfaceIndex).Select(item => item.Value).Distinct().Take(2).ToArray();
                            network = matches.Length == 1 ? matches[0] : null;
                        }

                        if (network == null)
                        {
                            if (reportAt > 0) missingInterface++;
                            continue;
                        }

                        if (cache.Add(packet.Message, packet.InterfaceIndex, now, network) && reportAt > 0) questAnswers++;
                    }
                    if (reportAt == 0 || now >= reportAt)
                    {
                        var available = cache.Devices(now, generation).ToDictionary(d => d.Id + "|" + d.Endpoint.Key);
                        foreach (var old in previous.Where(p => !available.ContainsKey(p.Key))) changed(old.Value, false);
                        foreach (var device in available.Values)
                            if (!previous.TryGetValue(device.Id + "|" + device.Endpoint.Key, out var old) || old.Name != device.Name)
                                changed(device, true);
                        previous = available;
                    }
                    if (reportAt > 0 && now >= reportAt)
                    {
                        reportAt = 0;
                        string names = string.Join(", ", interfaces.Values.Select(n => n.Name).Distinct().OrderBy(n => n));
                        diagnostic?.Invoke(names.Length == 0 ? "No active multicast-capable network interface was available." :
                            $"Network interfaces checked: {names}.\n" +
                            (questAnswers > 0 ? "HiBoP discovery records arrived, but no usable Quest address was resolved." :
                                received > 0 ? "mDNS traffic arrived, but no usable HiBoP Quest announcement was received." : "No mDNS responses arrived during the search.") +
                            (missingInterface > 0 ? "\nSome responses could not be matched to a Desktop network interface." : ""));
                    }
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
            int ipv4 = QuestEndpoint.GetInterfaceIndex(network, AddressFamily.InterNetwork);
            int ipv6 = QuestEndpoint.GetInterfaceIndex(network, AddressFamily.InterNetworkV6);
            return network.Id + ":" + ipv4 + ":" + ipv6 + ":" + string.Join(",", properties.UnicastAddresses.Select(a => a.Address.ToString()).OrderBy(a => a));
        }
    }
}
