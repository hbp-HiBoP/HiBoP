using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace HBP.Transfer.Transport
{
    // The Windows Unity/Mono GetIPv4Properties implementation also queries adapter settings.
    // That extra native call can throw even on an active interface. The mDNS client needs only its index.
    internal sealed class QuestWindowsNetworkInterface : NetworkInterface
    {
        private readonly NetworkInterface network;
        private readonly IPInterfaceProperties properties;

        public QuestWindowsNetworkInterface(NetworkInterface network)
        {
            this.network = network;
            properties = new MulticastProperties(network.GetIPProperties(), GetIPv4Index(network));
        }

        public override string Id => network.Id;
        public override string Name => network.Name;
        public override OperationalStatus OperationalStatus => network.OperationalStatus;
        public override bool SupportsMulticast => network.SupportsMulticast;
        public override IPInterfaceProperties GetIPProperties() => properties;

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static int GetIPv4Index(NetworkInterface network)
        {
            if (!network.Supports(NetworkInterfaceComponent.IPv4)) return 0;
            return Guid.TryParse(network.Id, out var id) && ConvertInterfaceGuidToLuid(ref id, out ulong luid) == 0 &&
                ConvertInterfaceLuidToIndex(ref luid, out int index) == 0 ? index : 0;
        }

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern uint ConvertInterfaceGuidToLuid(ref Guid interfaceGuid, out ulong interfaceLuid);

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern uint ConvertInterfaceLuidToIndex(ref ulong interfaceLuid, out int interfaceIndex);

        private sealed class MulticastProperties : IPInterfaceProperties
        {
            private readonly IPInterfaceProperties properties;
            private readonly IPv4InterfaceProperties ipv4;

            public MulticastProperties(IPInterfaceProperties properties, int index)
            {
                this.properties = properties;
                ipv4 = new MulticastIPv4Properties(index);
            }

            public override IPv4InterfaceProperties GetIPv4Properties() => ipv4;
            public override IPv6InterfaceProperties GetIPv6Properties() => properties.GetIPv6Properties();
            public override UnicastIPAddressInformationCollection UnicastAddresses => properties.UnicastAddresses;
            public override MulticastIPAddressInformationCollection MulticastAddresses => properties.MulticastAddresses;
            public override IPAddressInformationCollection AnycastAddresses => properties.AnycastAddresses;
            public override IPAddressCollection DnsAddresses => properties.DnsAddresses;
            public override GatewayIPAddressInformationCollection GatewayAddresses => properties.GatewayAddresses;
            public override IPAddressCollection DhcpServerAddresses => properties.DhcpServerAddresses;
            public override IPAddressCollection WinsServersAddresses => properties.WinsServersAddresses;
            public override bool IsDnsEnabled => properties.IsDnsEnabled;
            public override bool IsDynamicDnsEnabled => properties.IsDynamicDnsEnabled;
            public override string DnsSuffix => properties.DnsSuffix;
        }

        private sealed class MulticastIPv4Properties : IPv4InterfaceProperties
        {
            public MulticastIPv4Properties(int index) => Index = index;

            public override int Index { get; }
            public override bool UsesWins => throw new NotSupportedException();
            public override bool IsDhcpEnabled => throw new NotSupportedException();
            public override bool IsAutomaticPrivateAddressingActive => throw new NotSupportedException();
            public override bool IsAutomaticPrivateAddressingEnabled => throw new NotSupportedException();
            public override bool IsForwardingEnabled => throw new NotSupportedException();
            public override int Mtu => throw new NotSupportedException();
        }
    }
}
