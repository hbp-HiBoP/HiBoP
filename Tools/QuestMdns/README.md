# Embedded mDNS

Upstream: jdomnitz/net-mdns tag `v0.38`, package `Makaretu.Dns.Multicast.New` 0.38.0,
MIT. DNS model: `Makaretu.Dns.New` 3.1.2, MIT. Logging: `Common.Logging` and
`Common.Logging.Core` 3.4.1, Apache-2.0. The selected binaries target .NET Standard.

Run `python Tools/Prepare-QuestMdns.py` with .NET SDK **10.0.201** installed to restore
hash-pinned inputs and build the adapted managed assembly. No .NET runtime,
Bonjour installer or Avahi daemon is required by a distributed IL2CPP player.
The DLL importers deliberately disable automatic references: Common.Logging's
`Common` namespace otherwise conflicts with Unity Collections/Burst.
The script refuses another SDK and verifies every resulting DLL against
`ManagedPlugins.lock.json`; changing a pin requires rebuilding and qualification.

Local changes are reproduced by that script, with replacement transport sources
in this directory:

- Preserve receiving interface on every packet and expose it on MessageEventArgs.
- Use Windows WSARecvMsg and POSIX recvmsg on the existing sockets. Unity's
  managed PacketInformation option is unavailable in the tested editor runtime.
  Linux and Darwin layouts/constants are handled separately; both still require
  hardware qualification. Only x64 Desktop and Apple ARM64 are targeted.
- Scope multicast sends to each local interface, retain port 5353, share it with
  other responders, and observe receiver shutdown. Two worker receivers have a
  bounded socket timeout; Unity's main thread never performs their receive waits.
- Application owns interface monitoring/generations and discovery cache.
- Expose initialization/receiver failures for application-level recovery.

The transport is for browsing, not an embedded DNS server. Android advertises
through NsdManager on the selected Network. Public discovery metadata never
authorizes a connection: the application still verifies its pinned TLS identity.

Licenses are stored beside the DLLs and included in StreamingAssets/Licenses.
Do not replace the patched DLL with the upstream NuGet DLL: its event API lacks
the receiving interface and its socket implementation has not been qualified here.
