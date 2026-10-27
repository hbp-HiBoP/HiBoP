using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace HBP.Transfer.Transport
{
    public enum QuestNetworkPath
    {
        LocalNetwork,
        Usb
    }

    /// <summary>An address on a local interface. Neither the address nor its scope is a device identity.</summary>
    public sealed class QuestEndpoint
    {
        public IPAddress Address { get; }
        public int Port { get; }
        public QuestNetworkPath Path { get; }
        public int InterfaceIndex { get; }
        public long Generation { get; }
        public string Key => Address + "|" + Port + "|" + InterfaceIndex + "|" + Generation;
        public string AddressText => Port == QuestPairing.Port ? Address.ToString() : Address + ":" + Port;

        public QuestEndpoint(IPAddress address, int port = QuestPairing.Port, QuestNetworkPath path = QuestNetworkPath.LocalNetwork, int interfaceIndex = 0, long generation = 0)
        {
            if (address == null || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.Broadcast) || address.IsIPv6Multicast || address.AddressFamily == AddressFamily.InterNetwork && address.GetAddressBytes()[0] >= 224)
                throw new ArgumentException("Enter a unicast IPv4 or IPv6 address.");
            if (port < 1 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));
            Address = new IPAddress(address.GetAddressBytes());
            if (Address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                Address.ScopeId = interfaceIndex != 0 && Address.IsIPv6LinkLocal ? interfaceIndex : address.ScopeId;
                if (Address.IsIPv6LinkLocal && Address.ScopeId == 0) throw new QuestInterfaceRequiredException(address.ToString());
            }

            Port = port;
            Path = path;
            InterfaceIndex = interfaceIndex;
            Generation = generation;
        }

        public static QuestEndpoint Parse(string text, bool manual = false)
        {
            text = (text ?? "").Trim();
            if (text.StartsWith("[") && text.EndsWith("]")) text = text.Substring(1, text.Length - 2);
            int port = QuestPairing.Port;
            // Non-default ports are retained only for loopback tests/diagnostics, never manual UI.
            if (!IPAddress.TryParse(text, out var address) && !manual && text.Count(c => c == ':') == 1)
            {
                string[] parts = text.Split(':');
                if (!IPAddress.TryParse(parts[0], out address) || !IPAddress.IsLoopback(address) || !int.TryParse(parts[1], out port))
                    throw new ArgumentException("Enter the Quest IPv4 or IPv6 address without a port.");
                return new QuestEndpoint(address, port);
            }

            int zone = text.LastIndexOf('%');
            if (zone >= 0)
            {
                string name = text.Substring(zone + 1);
                int index;
                var interfaces = LocalIPv6Interfaces();
                if (!int.TryParse(name, out index))
                {
                    var network = interfaces.FirstOrDefault(n => n.Name == name || n.Id == name);
                    if (network == null) throw new ArgumentException("Select an available local IPv6 interface.");
                    index = network.GetIPProperties().GetIPv6Properties().Index;
                }

                if (!interfaces.Any(n => n.GetIPProperties().GetIPv6Properties().Index == index))
                {
                    if (manual) throw new ArgumentException("The selected local IPv6 interface is unavailable.");
                    throw new QuestRouteUnavailableException();
                }

                text = text.Substring(0, zone) + "%" + index;
            }

            if (!IPAddress.TryParse(text, out address)) throw new ArgumentException("Enter the Quest IPv4 or IPv6 address without a port.");
            return new QuestEndpoint(address, port, interfaceIndex: address.AddressFamily == AddressFamily.InterNetworkV6 ? (int)address.ScopeId : 0);
        }

        public static NetworkInterface[] LocalIPv6Interfaces() => NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.Supports(NetworkInterfaceComponent.IPv6)).Where(n => n.GetIPProperties().UnicastAddresses.Any(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)).ToArray();
    }

    public sealed class QuestInterfaceRequiredException : ArgumentException
    {
        public string AddressText { get; }
        public QuestInterfaceRequiredException(string address) : base("Select the Desktop interface for this link-local IPv6 address.") => AddressText = address;
    }

    public sealed class QuestRouteUnavailableException : System.IO.IOException
    {
        public QuestRouteUnavailableException() : base("The selected local IPv6 interface is unavailable.")
        {
        }
    }

    /// <summary>A stale/disguised discovery route is not a rejection of a saved pairing credential.</summary>
    public sealed class QuestIdentityMismatchException : System.IO.IOException
    {
        public QuestIdentityMismatchException(Exception inner) : base("The address no longer belongs to the expected Quest. Rediscovering its network paths.", inner)
        {
        }
    }
}
