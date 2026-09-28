# M1 physical Windows / Quest test — 28 September 2026

Follow-up: the [editor-signed cut-ID fix retest](M1-cut-id-fix-retest-2026-09-28.md) used new 6.2.0 Players. The large cut-position move still failed on Quest, while live color synchronization passed.

## Verdict

**M1 remains open.** `visu_full_test / Small` was published three times, and the operator saw its three columns on Quest. A Desktop site-color change appeared on Quest, and the local Quest controls behaved as expected. The axial cut, however, did not follow Desktop position or flip changes. Several discrete `−` clicks after a second publication reproduced the failure. Activity projection was visible on Desktop but absent on Quest, including when projection was active before a new full send; this made the timeline impossible to judge visually with this fixture.

The Desktop interface intentionally blocks interaction during any load. The former S1/S1b attempts are therefore **not manual acceptance tests or product defects**. T08 requires publication replay and structural abort/restart to be verified by automated tests or an explicit test harness. Their absence from this physical session does not count against the loading UI.

Operator observations and machine measurements are kept separate below. `NextVisible` is the first CPU-side frame eligibility point, not proof that pixels were displayed.

## Configuration and preparation

| Item | Value |
| --- | --- |
| HiBoP source for both Players | `a7bbab8ef39641b69c38542550275111e4fe1318`, Unity `6000.5.2f1`, HiBoP `6.2.0` Release |
| Windows EXE | SHA-256 `AAB370FC59ED240ED50AF0C9461D0D3E83548253AC11C820420D4D37BBA64250` |
| Quest APK | SHA-256 `2FC42F07CB1B9D90CAC9264C63F7B8051C1AE2260D73C8CC350B7DBD3B28B06A` |
| M1 APK signing certificate | SHA-256 `0CF833382569E08B3FB6353A26913195519FD8990DA94B8DCD228B8C69378190` |
| Fixture | `visu_full_test.hibop`, `Small` view, SHA-256 `3700B7093CDAFAF20B1055DE40E55F7A2F57E02E7B801F96EB78CEC58C87DF13` |
| Headset | Quest 3, Android 14, USB ADB serial `2G0YC5ZHB20370`, Wi-Fi ADB `192.168.1.18:5555` |
| Observed HiBoP transport | **USB**: `adb forward tcp:62970 tcp:45871` and a Player connection to `127.0.0.1:62970`. Enabling ADB over Wi-Fi did not make HiBoP use Wi-Fi. |
| Android battery / temperature | 39% / 44 °C before; 16% / 48 °C after, from `dumpsys battery` |

The previously installed 6.1.0 APK had a different certificate (SHA-256 `D5DCF67868BD08A3C7028A8495CD39550DAFAA7936286989D0E42253B081ACF`), causing `INSTALL_FAILED_UPDATE_INCOMPATIBLE`. Before the user-authorized uninstall, its APK and **643,186,299 bytes (613.4 MiB) of accessible external data** were backed up with hashes under `preinstall-backup`. Its private data was inaccessible through ADB. The M1 6.2.0 APK was then installed and launched. Future APK updates need a consistent signing certificate or an explicit migration path.

## Functional results

| Scenario | Result | Evidence and limit |
| --- | --- | --- |
| S0 — initial publication | **Basic display passed** | The operator saw three columns, and a Quest screenshot confirms them. Three `QUEST_TRANSFER_PUBLISHED` identifiers match Desktop transfer records. A complete comparison of all initial patients, colors and cuts was not performed. |
| S2 — site color | **Passed for the observed edit** | Site X12 for LEMl changed to green without a full resend; the operator saw green on Desktop and Quest. One `SiteColor` operation was received and applied in the first Quest capture. |
| S2 — cut definition | **Failed twice** | The sole axial cut was present in the initial scene. Desktop position and flip changes produced no visible Quest change. After the second full send, several discrete `−` clicks still did not move the Quest cut. Neither Quest capture contains a `CutDefinition` receive event. This narrows the fault to a point before the instrumented receive boundary; it does not prove whether the event was never published, rejected or lost in transit. |
| S2 — timeline | **Visually unqualified** | A seek was attempted. Activity projection appeared on Desktop while the Quest scene remained unchanged. The first Quest capture contains 34 received/applied `TimelineAnchor` operations, but there was no visible activity on Quest to check seek, play or pause. Dual-device activity projection is addressed later in T14. |
| S3 — local Quest controls | **Passed by operator observation** | The operator reported that triggers, X and A worked, and that local position/scale survived another Desktop color change. No useful before/after wrapper images were captured. |
| S4 — idle stability | **Passed by operator observation** | Nothing changed spontaneously for more than 30 seconds. Normal Windows Player closure produced a `closed` export. |
| Former S1/S1b — edits during loading | **Not applicable to manual testing** | The loading UI correctly blocks interaction. T08 publication replay, checkpoint fallback and unsupported structural abort/restart require focused automated evidence; they cannot be exercised through the normal Player interface. |

An accidental Quest **Y** press renewed the pairing code and disconnected the session. The new code was entered only in the Player, but its dialog said `Quest pairing failed` and `Quest disconnected. Reconnecting automatically...`. Desktop logs contain `Pairing refused` and `Truncated transport frame`. After a Quest pause that exported telemetry and an application restart, Quest showed `Quest paired / Waiting for visualization`, and another send succeeded. `QuestAnatomySession.ReceiveGlobalsAsync` refuses to replace shared data while a visualization is active; that is a plausible explanation for the first recovery failure, but the exact cause was not logged on Quest. The generic Player dialog hid the distinction. This incident is separate from the normal M1 flow.

## Measurements and limits

- Windows `closed` export: 24 samples, none dropped, covering three full transfers of about 125.3 MB each. Local `UserRequest` → `PublicationReceipt` took **16.7–25.4 s** across these three trials. The local write span was about 3.1–3.2 s.
- First Quest `checkpoint` export: 180 samples, none dropped; one transfer, one `SiteColor`, 34 `TimelineAnchor`, no `CutDefinition`.
- Second Quest `checkpoint` export: 10 samples, none dropped; two transfers and no mutation profile despite the cut clicks.
- All three transfer identifiers correlate across Desktop and Quest. The Quest exports are **partial** pause checkpoints. The Windows capture contains only `InitialTransfer` on the sender side. These traces do not provide qualifying end-to-end T00/T08 p95, GC or main-thread decisions for color, cut and timeline. The analyzer marks the corresponding budgets unavailable; none is a pass.

## Work needed for complete M1 sign-off

1. **Fix and retest the real-scene cut path.** Trace one Desktop `Cut.Position` or `Cut.Flip` setter through `DefinitionChanged`, the v2 boundary, validation/admission, transport and Quest receive/apply. Investigate silent rejection and the cut-object identity captured at publication. Add a focused regression test using the loaded `Small` fixture or an equivalent prepared scene. Rebuild both Players, then verify one discrete position change, flip, and a continuous move on Quest without a full resend.
2. **Provide an observable timeline fixture.** Use a precomputed or otherwise available timeline that can be displayed on Quest without the later T14 activity-projection job. If none exists, record timeline physical validation as dependent on T14; do not claim that this M1 run visually passed timeline. The current Quest trace proves anchor reception/application only.
3. **Complete quantitative qualification if full device budgets are required for sign-off.** Preserve a shared operation identity and the sender-to-ACK boundaries needed by the T00 analyzer, then collect warmed-up, isolated sets of at least 100 colors, 300 continuous cut definitions with a stable final value, and 100 timeline anchors **per transport**. Test the HiBoP USB tunnel and LAN Wi-Fi separately, record GC/main-thread cost, and issue explicit pass/fail results. The written T08 contract permits an honest `unavailable` decision; it does not turn the current three-transfer smoke test into a performance pass.
4. **Re-run the physical smoke test after the fix** on same-commit Windows/Android builds and preserve `closed`/`checkpoint` provenance. Review the existing automated T08 replay, overflow and structural-abort tests, and rerun the focused suite after any code change. Quest-origin driver behavior remains an automated criterion until product controls arrive in T15; the loading UI should remain blocked.

The signing mismatch should be resolved before repeated upgrade testing. The pairing-recovery dialog deserves a separate issue, but the accidental Y incident did not prevent completion of the normal publication and color checks after restart.

## Evidence

The local `.test-results/sync-m1-device/20260928-essai-01` folder contains `observations.csv`, the three raw CSV exports, Desktop and Quest logs, `analysis-m1.md` / `.json`, the S0 Quest screenshot, battery/network/thermal captures, the pre-install backup and `evidence-sha256.csv`. Quest screenshots can contain the real room and remain local. The build manifest is `.artifacts/quest-040/manifest.json`. The [physical-test procedure](M1-physical-test.md) explains preparation and collection. Its S1/S1b guidance was corrected after the build; the procedure hash in the immutable build manifest identifies the earlier version.
