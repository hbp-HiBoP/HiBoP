// HiBoP transport adaptation of jdomnitz/net-mdns v0.38 (MIT).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Makaretu.Dns
{
    public sealed class MdnsPacketEventArgs : EventArgs
    {
        public UdpReceiveResult Result { get; set; }
        public int InterfaceIndex { get; set; }
    }

    internal sealed class MulticastClient : IDisposable
    {
        public const int MulticastPort = 5353;
        private readonly List<Socket> sockets = new List<Socket>();
        private readonly List<(Socket Socket, IPAddress Address, int Index, IPEndPoint Target)> destinations = new List<(Socket, IPAddress, int, IPEndPoint)>();
        private readonly List<Task> receivers = new List<Task>();
        private volatile bool disposed;
        public event EventHandler<MdnsPacketEventArgs> MessageReceived;
        public Task Completion => Task.WhenAll(receivers);
        public bool HasReceiverFailure => receivers.Any(task => task.IsFaulted);

        public MulticastClient(bool ipv4, bool ipv6, IEnumerable<NetworkInterface> networks)
        {
            try
            {
                var nics = networks.Where(n => n.OperationalStatus == OperationalStatus.Up && n.SupportsMulticast).ToArray();
                foreach (var family in new[] { AddressFamily.InterNetwork, AddressFamily.InterNetworkV6 })
                {
                    if (family == AddressFamily.InterNetwork ? !ipv4 : !ipv6) continue;
                    var socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
                    sockets.Add(socket);
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    if (family == AddressFamily.InterNetworkV6) socket.DualMode = false;
                    socket.Bind(new IPEndPoint(family == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, MulticastPort));
                    socket.SendTimeout = 1000;
                    var group = IPAddress.Parse(family == AddressFamily.InterNetwork ? "224.0.0.251" : "ff02::fb");
                    foreach (var nic in nics)
                    foreach (var item in nic.GetIPProperties().UnicastAddresses.Where(a => a.Address.AddressFamily == family))
                    {
                        if (family == AddressFamily.InterNetworkV6 && !item.Address.IsIPv6LinkLocal) continue;
                        var properties = nic.GetIPProperties();
                        int index = family == AddressFamily.InterNetwork ? properties.GetIPv4Properties().Index : properties.GetIPv6Properties().Index;
                        try
                        {
                            if (family == AddressFamily.InterNetwork)
                                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(group, item.Address));
                            else socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(group, index));
                            var target = family == AddressFamily.InterNetwork ? group : new IPAddress(group.GetAddressBytes(), index);
                            destinations.Add((socket, item.Address, index, new IPEndPoint(target, MulticastPort)));
                        }
                        catch (SocketException) { /* A disappearing/unsupported interface cannot disable the others. */ }
                    }
                    var receiver = new MdnsNativeReceiver(socket);
                    receivers.Add(Task.Run(() => Receive(socket, receiver)));
                }
            }
            catch { Dispose(); throw; }
        }

        private void Receive(Socket socket, MdnsNativeReceiver receiver)
        {
            using (receiver)
            {
                try
                {
                    while (!disposed)
                    {
                        var packet = receiver.Receive();
                        if (!disposed && packet != null) MessageReceived?.Invoke(this, packet);
                    }
                }
                catch (Exception) when (disposed) { }
            }
        }

        // Only used by the worker-owned discovery service; no Unity main-thread socket waits.
        public Task SendAsync(byte[] message)
        {
            foreach (var destination in destinations)
            {
                try
                {
                    if (destination.Socket.AddressFamily == AddressFamily.InterNetwork)
                    {
                        destination.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, destination.Address.GetAddressBytes());
                        destination.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 255);
                    }
                    else
                    {
                        destination.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, destination.Index);
                        destination.Socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive, 255);
                    }
                    destination.Socket.SendTo(message, destination.Target);
                }
                catch (Exception) when (disposed) { break; }
                catch (SocketException) { }
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            disposed = true;
            MessageReceived = null;
            foreach (var socket in sockets) socket.Dispose();
        }
    }
}
