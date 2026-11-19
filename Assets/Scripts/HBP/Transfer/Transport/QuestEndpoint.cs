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
        public string InterfaceId { get; }
        public string InterfaceName { get; }
        public long Generation { get; }
        public string InterfaceKey => InterfaceId ?? InterfaceIndex.ToString();
        public string Key => Address + "|" + Port + "|" + Path + "|" + InterfaceKey + "|" + InterfaceIndex + "|" + Generation;
        public string AddressText => Port == QuestPairing.Port ? Address.ToString() : Address + ":" + Port;
        public string ConnectionAddress => InterfaceIndex != 0 && Port == QuestPairing.Port ? new IPAddress(Address.GetAddressBytes()) + "%" + InterfaceKey : AddressText;
        public string RouteKey(string questId) => questId + "|" + Path + "|" + InterfaceKey;

        public QuestEndpoint(IPAddress address, int port = QuestPairing.Port, QuestNetworkPath path = QuestNetworkPath.LocalNetwork, int interfaceIndex = 0, long generation = 0, string interfaceId = null, string interfaceName = null)
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
            InterfaceId = interfaceId;
            InterfaceName = interfaceName;
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
                if (!IPAddress.TryParse(text.Substring(0, zone), out address)) throw new ArgumentException("Enter the Quest IPv4 or IPv6 address without a port.");
                string name = text.Substring(zone + 1);
                bool numeric = int.TryParse(name, out int index);
                var network = LocalInterfaces(address.AddressFamily).FirstOrDefault(n => numeric ? GetInterfaceIndex(n, address.AddressFamily) == index : n.Name == name || n.Id == name);
                if (network == null)
                {
                    if (manual) throw new ArgumentException("The selected Desktop network interface is unavailable.");
                    throw new QuestRouteUnavailableException();
                }

                return new QuestEndpoint(address, interfaceIndex: GetInterfaceIndex(network, address.AddressFamily), interfaceId: network.Id, interfaceName: network.Name);
            }

            if (!IPAddress.TryParse(text, out address)) throw new ArgumentException("Enter the Quest IPv4 or IPv6 address without a port.");
            return new QuestEndpoint(address, port, interfaceIndex: address.AddressFamily == AddressFamily.InterNetworkV6 ? (int)address.ScopeId : 0);
        }

        public static NetworkInterface[] LocalInterfaces(AddressFamily family) => NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback && GetInterfaceIndex(n, family) > 0)
            .Where(n => n.GetIPProperties().UnicastAddresses.Any(a => a.Address.AddressFamily == family)).ToArray();

        public static int GetInterfaceIndex(NetworkInterface network, AddressFamily family) =>
            QuestMulticastNetworkInterface.GetIndex(network, family == AddressFamily.InterNetworkV6);

        internal IPAddress GetLocalAddress()
        {
            if (InterfaceIndex == 0 || Address.IsIPv6LinkLocal) return null;
            var network = LocalInterfaces(Address.AddressFamily).FirstOrDefault(n => GetInterfaceIndex(n, Address.AddressFamily) == InterfaceIndex && n.Id == InterfaceId);
            return network?.GetIPProperties().UnicastAddresses.Select(item => item.Address)
                .FirstOrDefault(a => a.AddressFamily == Address.AddressFamily && !a.IsIPv6LinkLocal) ?? throw new QuestRouteUnavailableException();
        }
    }

    public sealed class QuestInterfaceRequiredException : ArgumentException
    {
        public string AddressText { get; }
        public QuestInterfaceRequiredException(string address) : base("Select the Desktop interface for this link-local IPv6 address.") => AddressText = address;
    }

    public sealed class QuestRouteUnavailableException : System.IO.IOException
    {
        public QuestRouteUnavailableException() : base("The selected Desktop network interface is unavailable.")
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
