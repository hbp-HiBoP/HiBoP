using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HBP.Transfer.Transport
{
    public sealed class QuestDevice
    {
        public byte[] Pin;
        public string Name, Host, UsbSerial;
        public string Id => Convert.ToBase64String(Pin);
        public string Label => Name + " · " + (UsbSerial == null ? Host : "USB") + " · " + BitConverter.ToString(Pin, 0, 3).Replace("-", "");
    }

    /// <summary>Local IPv4 UDP query/reply. Announcements contain no secret and grant no trust.</summary>
    public static class QuestDiscovery
    {
        public const int Port = 45872;
        public const int AnnouncementPort = 45873;
        private const string Query = "HiBoP-Quest-discover-v1";
        private const string Prefix = "HiBoP-Quest-v1|";

        public static string Encode(byte[] pin, string name)
        {
            string safe = new string((name ?? "Quest").Where(c => !char.IsControl(c) && c != '|').Take(64).ToArray());
            return Prefix + Convert.ToBase64String(pin) + "|" + safe;
        }

        public static QuestDevice Decode(string message, string host)
        {
            if (message == null || message.Length > 256 || !message.StartsWith(Prefix, StringComparison.Ordinal)) return null;
            string[] parts = message.Split('|');
            if (parts.Length != 3 || parts[2].Length > 64 || parts[2].Any(char.IsControl)) return null;
            try
            {
                byte[] pin = Convert.FromBase64String(parts[1]);
                return pin.Length == 32 ? new QuestDevice { Pin = pin, Name = parts[2], Host = host } : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        public static async Task ListenAsync(Action<QuestDevice> discovered, CancellationToken stop)
        {
            using var socket = new UdpClient();
            socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Client.Bind(new IPEndPoint(IPAddress.Any, AnnouncementPort));
            using var close = stop.Register(socket.Close);
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    var packet = await socket.ReceiveAsync().ConfigureAwait(false);
                    if (packet.Buffer.Length > 512) continue;
                    var device = Decode(Encoding.UTF8.GetString(packet.Buffer), packet.RemoteEndPoint.Address.ToString());
                    if (device != null) discovered(device);
                }
            }
            catch (Exception) when (stop.IsCancellationRequested)
            {
            }
        }

        public static async Task AdvertiseAsync(Func<string> announcement, CancellationToken stop, Func<bool> waiting = null, SemaphoreSlim wake = null)
        {
            using var socket = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
            using var close = stop.Register(socket.Close);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(stop);
            Task announcing = AnnounceAsync(announcement, waiting, wake, linked.Token);
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    var packet = await socket.ReceiveAsync().ConfigureAwait(false);
                    if (packet.Buffer.Length != Query.Length || Encoding.ASCII.GetString(packet.Buffer) != Query) continue;
                    byte[] response = Encoding.UTF8.GetBytes(announcement());
                    await socket.SendAsync(response, response.Length, packet.RemoteEndPoint).ConfigureAwait(false);
                }
            }
            catch (Exception) when (stop.IsCancellationRequested)
            {
            }
            finally
            {
                linked.Cancel();
                await announcing.ConfigureAwait(false);
            }
        }

        private static async Task AnnounceAsync(Func<string> announcement, Func<bool> waiting, SemaphoreSlim wake, CancellationToken stop)
        {
            using var socket = new UdpClient { EnableBroadcast = true };
            try
            {
                bool changed = true;
                while (!stop.IsCancellationRequested)
                {
                    if (changed || waiting?.Invoke() != false)
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(announcement());
                        foreach (var destination in Destinations())
                            try
                            {
                                await socket.SendAsync(bytes, bytes.Length, new IPEndPoint(destination, AnnouncementPort)).ConfigureAwait(false);
                            }
                            catch (SocketException) when (!stop.IsCancellationRequested)
                            {
                            }
                    }

                    if (wake != null) changed = await wake.WaitAsync(5000, stop).ConfigureAwait(false);
                    else
                    {
                        await Task.Delay(5000, stop).ConfigureAwait(false);
                        changed = false;
                    }
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
            }
        }

        private static IEnumerable<IPAddress> Destinations()
        {
            var result = new HashSet<IPAddress> { IPAddress.Broadcast };
            foreach (var network in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up))
            foreach (var unicast in network.GetIPProperties().UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address)))
            {
                if (unicast.IPv4Mask == null) continue;
                byte[] address = unicast.Address.GetAddressBytes(), mask = unicast.IPv4Mask.GetAddressBytes();
                result.Add(new IPAddress(address.Select((b, i) => (byte)(b | ~mask[i])).ToArray()));
            }

            return result;
        }

        public static async Task<List<QuestDevice>> FindAsync(CancellationToken stop, Action<QuestDevice> discovered = null)
        {
            using var socket = new UdpClient(new IPEndPoint(IPAddress.Any, 0)) { EnableBroadcast = true };
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop);
            deadline.CancelAfter(1500);
            using var close = deadline.Token.Register(socket.Close);
            var found = new Dictionary<string, QuestDevice>();
            var destinations = await Task.Run(Destinations, stop).ConfigureAwait(false);

            try
            {
                byte[] query = Encoding.ASCII.GetBytes(Query);
                foreach (var address in destinations)
                {
                    try
                    {
                        await socket.SendAsync(query, query.Length, new IPEndPoint(address, Port)).ConfigureAwait(false);
                    }
                    catch (SocketException) when (!deadline.IsCancellationRequested)
                    {
                    }
                }

                while (!deadline.IsCancellationRequested)
                {
                    var packet = await socket.ReceiveAsync().ConfigureAwait(false);
                    if (packet.Buffer.Length > 512) continue;
                    var candidate = Decode(Encoding.UTF8.GetString(packet.Buffer), packet.RemoteEndPoint.Address.ToString());
                    if (candidate != null && found.Count < 64)
                    {
                        found[candidate.Id + "|" + candidate.Host] = candidate;
                        discovered?.Invoke(candidate);
                    }
                }
            }
            catch (Exception) when (deadline.IsCancellationRequested && !stop.IsCancellationRequested)
            {
            }

            stop.ThrowIfCancellationRequested();
            return found.Values.ToList();
        }
    }
}
