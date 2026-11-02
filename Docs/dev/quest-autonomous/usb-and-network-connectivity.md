# USB NCM and Quest network discovery

Code reference as of November 2, 2026. Product USB uses **NCM (network interface)** and the same application protocol as LAN/Wi-Fi; it depends on neither an ADB tunnel nor developer mode. ADB remains a separate build/diagnostic tool.

## Discovery and connection

The mDNS service is `_hibop-quest._tcp.local` on UDP 5353; the TLS connection uses TCP 45871. Advertisements expose version, identity, name and network path, never pairing code or secret. Discovery does not authenticate the headset: code/J-PAKE pairing, TLS-bound confirmation and pinned identity establish trust.

[QuestDiscovery](../../../Assets/Scripts/HBP/Transfer/Transport/QuestMdnsDiscovery.cs) observes interfaces and rebuilds its listener after network changes. [QuestEndpoint](../../../Assets/Scripts/HBP/Transfer/Transport/QuestEndpoint.cs) retains address, path and interface generation. Multiple paths are grouped under the same headset identity. USB is preferred on connection/reconnection; plugging in USB during a healthy Wi-Fi session does not migrate that session.

A link-local IPv6 address requires the **Desktop interface's** scope, never the one read on Android. Manual entry allows a full address and local interface selection; retain all address groups. Scopes may change after reconnection. An expired advertisement must not replace a current endpoint from a new generation.

Eligible network interruptions automatically recover scenes, unlike user cancellation or data errors: see [Display](transfer.md#a-display-batch). Disabling `AutomaticallyConnect` prevents automatic initial startup; it does not mean interrupting recovery of an already established connection. Explicit Disconnect suspends attempts. New pairing from another Desktop can replace the credential accepted by Quest; locally protected Windows persistence is not a portable file.

In [QuestPairing.ConnectAsync](../../../Assets/Scripts/HBP/Transfer/Transport/QuestPairing.cs), cancellation closes the **captured Socket**, waits for Connect/TLS to finish, then disposes the TcpClient. Closing the TcpClient first can clear its Client property during Unity's Begin/EndConnect pair. Network timeouts remain errors, not cancellation successes.

Current storage limit: [PairingStorage](../../../Assets/Scripts/HBP/Transfer/Transport/PairingStorage.cs) protects files through Windows-user-bound DPAPI, but returns bytes unchanged outside Windows. Functional macOS/Linux persistence therefore does not demonstrate encryption at rest. On Android, callers use private storage without automatic backup. Do not publish pairing files or their secrets.

[TransportIdentity](../../../Assets/Scripts/HBP/Transfer/Transport/TransportIdentity.cs) generates the certificate through BouncyCastle for IL2CPP compatibility, where the spike's `CertificateRequest` path was unusable. The PKCS#12 container's SHA/3DES choice serves Mono reading; it is not the negotiated TLS cipher. The full certificate fingerprint is checked before sending the credential.

## Qualification by platform

The matrix below describes documented hardware qualifications, without constituting a new acceptance procedure for current code. These results do not gate platform publication in CI.

| Platform | Evidence and scope |
| --- | --- |
| Windows 11 + Quest 3 | [October 27, 2026 campaign](../../../Validation/Quest/connectivity/windows-quest3-qualification-2026-10-27.md), identified builds, functional USB without Wi-Fi and with developer mode disabled. NCM consent refusal/retry not exercised, accepted as non-blocking. |
| macOS ARM64 | Not physically qualified; packaging and CI checks are insufficient. |
| Linux x64 | Not physically qualified; packaging and CI checks are insufficient. |
| Quest 3S | Outside the Windows campaign; no extrapolation from Quest 3. |

On macOS, `NSLocalNetworkUsageDescription` and `NSBonjourServices` keys are added to the final bundle before signing. Launch through Finder to exercise user permissions; execution from Terminal is not equivalent. CI ad-hoc signing does not replace Developer ID/notarization. On Linux, check the NCM interface (`cdc_ncm`) and local IPv6; the product requires neither root execution nor an Avahi daemon. Enabling IPv6 globally on the Internet is unnecessary. Do not globally disable the firewall or IPv6 to work around diagnostics.

mDNS dependencies are prepared and pinned by [Tools/QuestMdns](../../../Tools/QuestMdns/README.md) and its lock file; do not substitute an arbitrary package. Checks without ADB and bundle checks are described in [deployment](deployment.md).

## Targeted acceptance procedure on a new OS

Retain models/OS, exact Quest system version, build versions/hashes and local logs. Use the same scenes and distinguish TLS connection delay, scene publication and initial synchronization.

| Case | Expected observation |
| --- | --- |
| C1 — USB only | Without Wi-Fi, pairing then scene and mutation in both directions; no product ADB. |
| C2 — Manual IPv6 | Full address and current Desktop scope; connection then reconnection. |
| C3 — USB + Wi-Fi | USB preferred for a new connection; healthy session not migrated. Removing USB checks fallback, then reconnect without imposing an active migration. |
| C4 — Interruption during Sending | Remove cable, observe recovery for up to 30 s without Retry/Send/Reconnect; check final scene and absence of duplication. |
| C5 — Quest restart | Relaunch the application and check session/scene recovery and mutations, without unjustified re-pairing. |
| C6 — Desktop/Disconnect | Distinguish restart and auto-connect preference; after explicit Disconnect, observe at least 10 s without spontaneous recovery. |

NCM consent refusal must not be forced by resetting the headset. If it is not easily reproducible, record it as unexercised; the Windows decision does not qualify this case on other systems. A standby test must verify actual wake-up and that the headset is being worn before attributing network absence to the protocol.
