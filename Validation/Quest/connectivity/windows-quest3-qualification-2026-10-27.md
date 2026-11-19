# Windows 11 / Quest 3 USB and network qualification — October 27, 2026

**Windows 11 / Quest 3 was functionally accepted for the tested builds.**
This completed campaign does not qualify the current checkout, macOS, Linux or
Quest 3S. NCM consent refusal/retry was not exercised and was accepted as
non-blocking. This result documents hardware validation; it does not gate
platform publication in CI.

## Measurement conditions

Windows 11, Quest 3, Android 14, system identifier `52433670048800520`;
commercial Horizon version unconfirmed. The final Player was `WindowsRelease2`,
Release IL2CPP, with the APK already installed. The Desktop
`AutomaticallyConnect` preference remained disabled throughout the campaign.

Local campaign and retest artifacts were under `.artifacts/quest-ncm/`,
with additional evidence without developer mode under `usb-debug-off-20261027/`.
These paths do not guarantee file availability on another clone.
No new build, test or headset trial was performed for this consolidation.

## Observed results

| Workflow | Final result | Scope of observation |
| --- | --- | --- |
| USB only, Quest Wi-Fi off, developer mode disabled through Meta Horizon | Connection without a new code, Small visible, Desktop change received in headset. TLS over NCM IPv6, Desktop scope 54 during the trial. | User confirmation and no ADB-accessible device; no readback of `adb_enabled=0` during the claimed interruption. |
| Manual IPv6 | Connection with full address and correct Desktop interface. | The earlier timeout used a truncated address; it was not evidence of IPv6 failure. |
| USB-to-LAN fallback | First LAN TCP activity approximately **4.20 s** after NCM disappeared, without a new code. | Network delay, not full scene restoration delay. |
| Interruption during transfer | After correction, Wi-Fi TCP recovery in **7.06 s**, Small publication **10,504.53 ms**, initial synchronization **1,232.87 ms**, without Retry/Send/Reconnect or code. | Separate durations; full restoration in under 15 s is not demonstrated. |
| Restarting HiBoP Quest from its menu | Automatic return of Small within **30 s** and bidirectional settings confirmed by the user; Desktop logs: publication **9,881.07 ms**, synchronization **857.76 ms**. | ADB unavailable at this step: no new PID or Quest log claimed. |

Two transfer-recovery failures preceded the admission and heartbeat correction.
Desktop corrections prevent heartbeat from preempting discovery during a
disconnected session and its wait period. The verdict rests on final retests,
without reclassifying initial failures as successes.
The Player check without ADB had passed and the three targeted recovery
variants had been replayed.

## Limits and interpretation

NCM refusal/retry remains unexercised. Disabling USB debugging alone was not
tested independently from disabling developer mode. Pre-existing OpenXR
camera/boundary messages and DontDestroyOnLoad warnings were not declared fixed.
An intermediate manual re-pairing explains code re-entry at that step,
without demonstrating spontaneous secret loss.

Current operation and the acceptance procedure for another OS are described in
[USB and network connectivity](../../../Docs/dev/quest-autonomous/usb-and-network-connectivity.md).
The [provenance of earlier evidence](../retained-results.md#finding-earlier-evidence)
allows the report to be consulted at its former Git path:
`Docs/dev/quest-autonomous/usb/windows-quest3-checklist-2026-10-27.md`.
