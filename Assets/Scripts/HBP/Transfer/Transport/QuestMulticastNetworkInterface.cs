using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace HBP.Transfer.Transport
{
    // Supply the interface indices needed by the mDNS client when Mono's managed
    // properties are unavailable or also query unrelated adapter settings.
    internal sealed class QuestMulticastNetworkInterface : NetworkInterface
    {
        private readonly NetworkInterface network;
        private readonly IPInterfaceProperties properties;

        public QuestMulticastNetworkInterface(NetworkInterface network)
        {
            this.network = network;
            properties = new MulticastProperties(network);
        }

        public override string Id => network.Id;
        public override string Name => network.Name;
        public override OperationalStatus OperationalStatus => network.OperationalStatus;
        public override bool SupportsMulticast => network.SupportsMulticast;
        public override IPInterfaceProperties GetIPProperties() => properties;

        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly bool IsMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#if UNITY_EDITOR_LINUX || (UNITY_STANDALONE_LINUX && !UNITY_EDITOR)
        private static readonly bool IsLinuxDesktop = true;
#else
        private static readonly bool IsLinuxDesktop = false;
#endif
        public static bool NeedsAdapter => IsWindows || IsMac || IsLinuxDesktop;

        public static int GetIndex(NetworkInterface network, bool ipv6)
        {
            if (!network.Supports(ipv6 ? NetworkInterfaceComponent.IPv6 : NetworkInterfaceComponent.IPv4)) return 0;
            if (IsMac) return (int)if_nametoindex(network.Name);
            if (IsLinuxDesktop) return (int)LinuxInterfaceIndex(network.Name);
            if (ipv6) return network.GetIPProperties().GetIPv6Properties()?.Index ?? 0;
            if (IsWindows) return GetWindowsIPv4Index(network);
            return network.GetIPProperties().GetIPv4Properties()?.Index ?? 0;
        }

        private static int GetWindowsIPv4Index(NetworkInterface network)
        {
            return Guid.TryParse(network.Id, out var id) && ConvertInterfaceGuidToLuid(ref id, out ulong luid) == 0 &&
                ConvertInterfaceLuidToIndex(ref luid, out int index) == 0 ? index : 0;
        }

        [DllImport("/usr/lib/libSystem.B.dylib", ExactSpelling = true)]
        private static extern uint if_nametoindex(string name);

        [DllImport("libc.so.6", EntryPoint = "if_nametoindex", ExactSpelling = true)]
        private static extern uint LinuxInterfaceIndex(string name);

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern uint ConvertInterfaceGuidToLuid(ref Guid interfaceGuid, out ulong interfaceLuid);

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern uint ConvertInterfaceLuidToIndex(ref ulong interfaceLuid, out int interfaceIndex);

        private sealed class MulticastProperties : IPInterfaceProperties
        {
            private readonly IPInterfaceProperties properties;
            private readonly IPv4InterfaceProperties ipv4;
            private readonly IPv6InterfaceProperties ipv6;

            public MulticastProperties(NetworkInterface network)
            {
                properties = network.GetIPProperties();
                ipv4 = new MulticastIPv4Properties(GetIndex(network, false));
                if (IsMac || IsLinuxDesktop) ipv6 = new MulticastIPv6Properties(GetIndex(network, true));
            }

            public override IPv4InterfaceProperties GetIPv4Properties() => ipv4;
            public override IPv6InterfaceProperties GetIPv6Properties() => ipv6 ?? properties.GetIPv6Properties();
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

        private sealed class MulticastIPv6Properties : IPv6InterfaceProperties
        {
            public MulticastIPv6Properties(int index) => Index = index;

            public override int Index { get; }
            public override int Mtu => throw new NotSupportedException();
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
