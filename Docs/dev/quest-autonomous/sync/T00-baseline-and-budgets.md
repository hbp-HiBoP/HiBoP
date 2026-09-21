# T00 baseline instrumentation and provisional budgets

Date: 2026-09-21. Source baseline: `518b3f0d4b2f4db45b52df5fbb5f7160b6828c6f` plus the T00 working-tree changes described here.

## Scope and result

T00 adds measurement and fast-test foundations only. It does not repair the rejected snapshot/delta protocol, change its wire format, or implement a v2 operation family.

The implementation provides:

- `Sync.Fast`, `Sync.Loopback`, `Sync.SceneFocused`, `Sync.Qualification`, and `Sync.Device` category names;
- `Tools/run-sync-tests.cmd` and `Tools/run-sync-tests.ps1` entry points for the automated EditMode tiers;
- an injected monotonic clock, fake clock, and manually advanced async gate with no wall-clock waits;
- opt-in, bounded telemetry for initial transfer, site color, complete cut changes, and timeline seek/playback;
- immutable capture contexts that atomically bind sink, clock, frequency, main thread, and capture-session generation; close stops admission and drains a writer already admitted to the sink;
- separate scope/logical-trace, state-capture generation, and network-attempt columns, so replacement, retry, and reconnect cannot reuse one ambiguous identity;
- user request, capture start/end, queued/replaced, encoded, first/last written, first/last received, apply start/end, next-visible, scientific-stable, initial publication receipt, and legacy received/applied/visible ACK milestones where the current implementation exposes them;
- absolute per-thread managed-allocation counters, thread identity, main-thread classification, payload bytes, and monotonic timestamps in each sample.

With telemetry disabled, every Sync probe exits on a null-context branch. There is no telemetry `Update`/`LateUpdate`, polling owner, or scene scan. Core business entities contain no Sync type, latch, or per-instance telemetry state. They publish feature-neutral, pre-mutation events; the temporary `SyncSetterOriginObserver` subscribes only while a measured Desktop replica session is active. Focused tests execute 10,000 disabled Sync probes after warm-up with zero managed bytes allocated on the calling thread.

## Exact observable boundaries

| Profile / milestone | Exact production observation |
| --- | --- |
| Initial `UserRequest` | Raw Desktop point captured by the first statement of `QuestManager.SendAsync(false, ...)`, before transfer identity allocation, pairing checks, loading UI, frame switches, or capture preparation. Publication to the sink is deferred until the request exits so instrumentation is outside the measured interval. A retry does not create a new logical trace. |
| Initial `CaptureStart` / `CaptureEnd` | Immediately before and after `Base3DScene.CapturePreparedAsync`; resource-readiness checks and the preceding frame switch are therefore request-to-capture work, not capture work. |
| Initial `Encoded` | Non-streaming: archive file and digest completed successfully. Streaming: block producer, spool, digest, and flush completed successfully. Failure emits no `Encoded`. |
| Initial first/last write | First point follows the first successful transport write; last follows the final successful payload write. A partial failure can therefore expose first without last. |
| Initial `PublicationReceipt` | Desktop point captured only after `SceneDelivery.SendAsync` has validated the 33-byte receipt returned after Quest publication. Each send/retry has a distinct attempt ID. |
| Site `Setter` | `SiteState.Color` raises the neutral pre-mutation `ColorChanging` event. `SyncSetterOriginObserver` records the first raw point only for a site in the active scene and `DesktopReplicaSession` consumes it. |
| Cut `Setter` | `Cut` raises one neutral pre-mutation `DefinitionChanging` event for Position, Flip, NumberOfCuts, Orientation, and custom Normal X/Y/Z. The observer records the first point for an active cut; `OnModifyPlanesCuts` consumes it and `OnUpdateCuts` is not an origin. |
| Timeline `Setter` | `BasicTimeline` raises the neutral pre-mutation `AnchorChanging` event for index/play/loop/step. The observer records the first point only for the active timeline; playback and toolbar events consume it. |
| Live capture start/end | Raw points immediately around `LiveGeometryStateAdapter` capture. Sink locking and identity/CSV formatting happen after `CaptureEnd`. |
| `Replaced` | A queued trace displaced by a newer capture before acceptance. The replacement receives a new logical ID and capture generation. |
| Live attempt milestones | `Encoded`, successful first/last writes, and ACK receipts carry the immutable logical ID and capture generation plus a new attempt ID for every retry/reconnect. |

CSV row order is admission order, not a causal sort. Analysis sorts within an identity by raw timestamp. A success point is captured only after the corresponding operation succeeds; publishing that already-captured point later does not turn a subsequent failure into a success.

## Test tiers and measured development-loop cost

Run the focused suites from the repository root:

```powershell
.\Tools\run-sync-tests.cmd -Tier Fast
.\Tools\run-sync-tests.cmd -Tier Loopback
```

`-AllInAssemblies` removes the category filter and exists only to expose the cost of broad assemblies. Compilation/domain reload and Unity job setup are total time, not loaded test execution time. The final corrective-pass evidence below was produced through the already-open Unity Editor and MCP, using the same assembly/category selection as the script.

| Tier / job | Result | Loaded test execution | Total Unity job time |
| --- | ---: | ---: | ---: |
| `Sync.Fast` / `83ace47c91024357b1246dd8c7a00bd0` | 9 passed, 0 failed, 0 skipped | 1.2303482 s | 7.728 s |
| `Sync.Loopback` / `a2f7b357fed64699b2349b3c742ea57e` | 6 passed, 0 failed, 0 skipped | 1.1630142 s | 7.658 s |
| `Sync.SceneFocused` / `017b0ce10b9146868ee4ee92344e1295` | 7 passed, 0 failed, 0 skipped | 0.7217195 s | 6.436 s |
| **Total** | **22 passed, 0 failed, 0 skipped** | **3.1150819 s** | **21.822 s** |

`Sync.Loopback` meets its `< 5 s` target. This final `Sync.Fast` run does **not** demonstrate its provisional `< 1 s` target: Unity reported 1.2303482 s after the suite was reduced to nine deterministic cases. No obsolete 7-test/1-test XML is used as final proof, and no broad assembly suite was rerun for this corrective pass.

The static assembly gate passed on 44 HBP assemblies and 154 direct edges; the measured command wrapper took 0.372674 s (checker self-reported 0.3109 s). Its six fixture tests passed in 0.503186 s: valid graph, Core inversion, cycle, missing name, ambiguous name, and unapproved edge.

## Available physical baseline

No Desktop/Quest player pair was running for T00, so this task did not invent live-operation device numbers. The available initial-transfer evidence predates T00 but uses the same `visu_full_test / Small` reference and remains useful context.

| Path/run | Click to Desktop completion | Important phase evidence | Status for T00 |
| --- | ---: | --- | --- |
| USB forwarding, first instrumented load, 2026-09-16 | 97.286 s | Desktop preparation 24.096 s; send 7.592 s; Quest receive 7.952 s; Quest post-receive publication 64.138 s; Desktop maximum frame interval 2.851 s; Quest GC0 +1029 | Historical detailed baseline, not a current p95 |
| Wi-Fi LAN, same instrumented session, 2026-09-16 | 105.533 s | Desktop preparation 17.116 s; Quest receive 11.808 s; Quest post-receive publication 75.243 s; Quest maximum frame interval 16.459 s | Historical detailed baseline, caches and thermals differ from USB |
| Final Wi-Fi LAN smoke after transfer optimization | 24.769 s | 125,317,658 encoded bytes; detailed probes removed; neighboring lot-4 observations 20.027/20.241 s | Latest available total; one run, not a percentile |

Sources: `../reports/QUEST-transfer-baseline-01.md`, `../reports/QUEST-transfer-baseline-02-wifi.md`, and `../reports/QUEST-transfer-final.md`.

Live USB/Wi-Fi p50/p95/p99, current main-thread cost, and current GC cost for site color, cut movement, and timeline are **unavailable at T00**. The new probes make those measurements collectable without claiming that historical initial-transfer data measures live synchronization.

## Collecting a live baseline

Enable telemetry before application startup:

- Desktop: add `-syncBaseline` to the player command line, or create `sync-baseline.enabled` in `Application.persistentDataPath`.
- Quest: create `/sdcard/Android/data/fr.crnl.hibop.quest/files/sync-baseline.enabled` before launch.

On a clean application exit, each peer writes a bounded CSV under `Application.persistentDataPath/SyncBaselines` and logs `HBP_SYNC_BASELINE file=...`. The fixed cap is 8192 samples. At capacity the sink retains the oldest samples, drops each newest sample, and increments a saturating `dropped` counter; a valid baseline requires `dropped=0`.

Every CSV row has `scopeId`, `logicalTraceId`, `captureGeneration`, `attemptId`, and an explicit provenance kind for each numeric identity dimension. On Desktop, initial scope is the transfer ID and live scope is the existing epoch; logical/capture identity survives a retry while the exact attempt changes. The legacy wire format is unchanged. Quest live rows label epoch/revision-derived logical and capture values as `LegacyProxy`, with attempt `0` labelled `Unavailable`. They can correlate only within the Quest legacy epoch/revision view: they cannot be joined to an exact Desktop logical trace, capture generation, or network attempt, and several Desktop profile changes collapsed into one revision cannot be reconstructed. Initial Quest rows retain transfer ID only as scope; logical, capture, and attempt are `0`/`Unavailable`, so the delivering Desktop attempt is unknowable. For each USB and Wi-Fi path, use the same scene and collect after warm-up:

- at least 100 isolated site-color assignments;
- at least 300 continuous cut definitions, followed by an idle final value;
- at least 100 timeline seek/play/pause anchors;
- p50/p95/p99 same-peer stages, plus Desktop setter-to-applied-ACK and setter-to-visible-ACK upper bounds;
- same-thread allocation deltas, main-thread intervals, payload bytes, and incomplete traces.

Perform one unrecorded operation per profile first to warm Unity and serializer caches. Site color no longer depends on a post-change cache: its raw origin is captured directly before `SiteState.Color` assignment.

Allocation counters are absolute per managed thread. Subtract milestones only when their thread IDs match. Monotonic clocks are process-local and are never subtracted across Desktop and Quest. Desktop setter-to-ACK and initial request-to-`PublicationReceipt` are same-process causal upper bounds. Exact cross-peer phase durations require a qualified clock estimate and protocol-carried attempt identity in a later task. Initial-transfer wire milestones are taken at completed transport reads/writes rather than decompression or disk callbacks. `FirstByteReceived` is observed after the bounded four-byte transport prefix; `NextVisible` is CPU-side frame eligibility, not GPU completion or photons. `ScientificStable` currently reflects the legacy initial publication/apply quiescence boundary and must not be reused as a generic v2 send gate.

## Provisional regression budgets

These are initial T08 gates, not measured claims. T08 must publish pass/fail and may tighten them using controlled T00-format USB/Wi-Fi evidence. Allocation limits are total managed bytes attributable to one warm operation path; no allocation or CPU work may scale with unrelated scene size.

| Profile | Setter to applied-ACK receipt p95 USB / Wi-Fi | Setter to visible-ACK receipt p95 USB / Wi-Fi | Local Unity-thread p95 | Remote apply Unity-thread p95 | Managed allocation after warm-up |
| --- | ---: | ---: | ---: | ---: | ---: |
| Site color | 16.7 / 33.3 ms | 33.3 / 50.0 ms | 0.25 ms | 0.50 ms | <= 256 B |
| Complete cut definition | 16.7 / 33.3 ms | 33.3 / 50.0 ms | 0.50 ms | 2.00 ms | <= 512 B |
| Timeline anchor | 16.7 / 33.3 ms | 33.3 / 50.0 ms | 0.25 ms | 0.50 ms | <= 256 B |

The initial-transfer guard remains separate: on the same `Small` fixture and controlled cache/thermal state, a new run must stay within 30 s click-to-publication and within 20% of its paired pre-change run until a multi-run p95 exists. T02 must also report preparation, first byte, last byte, apply, and next-visible phases so an unchanged total cannot hide a larger Unity-thread stall.

## Known limitations assigned to later tasks

- The measured live path is still the rejected snapshot/delta/visible-ACK implementation. T01-T08 replace it; T00 intentionally does not optimize it.
- No operation-matrix row is complete. The three profiles are instrumentation coverage, not v2 DTO/handler or bidirectional-driver evidence.
- Physical live-operation percentiles remain pending a controlled Desktop/Quest run.
- The CSV is flushed on clean exit; abrupt process termination can lose the in-memory trace.
- Desktop traces separate logical change, capture generation, and each attempt. Quest live correlation remains an explicitly labelled legacy epoch/revision proxy because T00 does not change the wire format; initial Quest traces expose no logical/capture/attempt identity, and several logical changes collapsed into one revision cannot be reconstructed remotely.
- The fixed telemetry sink deliberately drops newest samples after 8192 rows. A run with any drop is invalid and must be repeated with a shorter capture window; abrupt process termination can still lose the in-memory trace.
- The active-scene observer keeps sets bounded by current scene membership, and trace/profile batches use three fixed slots. HashSet capacity can retain the maximum concurrent scene cardinality seen during that observer lifetime, but it cannot grow per mutation.
- The measured `Sync.Fast` duration was 1.2303482 s, so the provisional `< 1 s` feedback target remains unmet on this Editor run.
- Device visual parity, scientific parity, and GPU/photon visibility remain qualification work, not T00 claims.

## Closure table

| Acceptance criterion | Production point | Deterministic test or measurement | Remaining limitation |
| --- | --- | --- | --- |
| Initial origin is the real user request and capture start is distinct | `QuestManager.SendAsync(false)` captures request; `DesktopSceneCapture.CaptureDeliveryAsync` surrounds `CapturePreparedAsync` | Raw-point ordering is visible in the initial trace; `Sync.Fast` validates injected timestamps and generation isolation | No physical request-to-Quest percentile was collected in T00 |
| Desktop observes the receipt returned after Quest publication | `SceneDelivery.SendAsync` captures `PublicationReceipt` only after receipt validation | `SceneDelivery_RetryUsesANewAttemptAndRecordsOnlySuccessfulPublicationReceipt` | Receipt is a publication upper bound, not photons |
| Site/cut/timeline use the first business setter point without a Core-to-Sync edge | Neutral Core pre-mutation events are observed by `SyncSetterOriginObserver` in `HBP.Sync.Scene`; `DesktopReplicaSession` consumes the bounded origins | `SiteColorSetter_PreservesItsFirstPreMutationPoint`; Position, Flip, Normal X/Y/Z, and Timeline `Sync.SceneFocused` cases; `SetterOrigin_PreservesTheFirstPointUntilConsumed` | Later render/update events are intentionally not origins |
| Pending replacement does not merge traces | `SyncTraceTracker.Capture` replaces only the dirty profile's fixed pending slot; `ClearPending` matches profile + logical ID + generation | `PendingReplacement_DoesNotReuseTheReplacedLogicalTrace`; both `Capture_*PendingThen*Dirty_PreservesBothProfiles` directions; `ClearPending_RemovesExactAcceptedIdentitiesAcrossMixedGenerations` | A legacy revision may still combine profiles on Quest; Desktop identities remain separate |
| Retry/reconnect and legacy identity are honest | `SyncLogicalTrace.Identity` keeps exact Desktop logical/capture IDs and allocates a new exact attempt; Quest factories label revision proxies and unavailable dimensions | `Identity_KeepsExactAttemptsDistinctAndLabelsLegacyProxies`; SceneDelivery retry test | Legacy wire carries no Desktop attempt and no exact end-to-end identity |
| Instrumentation work is outside measured intervals | Setters and transport callbacks capture raw points; publication to the sink occurs after capture/write/apply intervals | Timestamp-order and success/failure tests; disabled allocation tests | Absolute allocation deltas are valid only on the same managed thread |
| Capture context closes atomically and stale origins cannot cross generations | `SyncTelemetry.CaptureSession.Dispose` clears admission then drains admitted writers; `SyncSetterOrigin` replaces a point whose capture generation is no longer active | `AsyncGate_OpensResetsAndCancelsWithoutWallClockTime`; `SetterOrigin_PreservesTheFirstPointUntilConsumed` also restarts capture and rejects an old raw point | CSV still requires clean application exit |
| Success milestones are failure-symmetric | Block producer sets encoded point only after successful spool/digest/flush; write stages follow completed writes | Successful/failed streaming-preparation tests and ReplicaWire completed/failed-write tests | A partial transport legitimately has first-write without last-write or receipt |
| Disabled telemetry and observation are bounded | No telemetry `Update`/`LateUpdate`; no Core telemetry state; fixed three-profile batches; bounded sink drops newest explicitly | `DisabledTelemetryAndBoundedSink_HaveExplicitBounds`; seven `Sync.SceneFocused` setter cases | Observer HashSets retain at most the active observer's maximum concurrent scene cardinality |
| Assembly direction is mechanically enforced before Unity | `check-assembly-dependencies.ps1` validates unique names, HBP references, Core isolation, cycles, and the versioned exact edge allow-list; `run-sync-tests.ps1` invokes it first | Six PowerShell fixtures plus final 44-assembly/154-edge pass | The allow-list must be reviewed whenever an intentional direct HBP edge changes |
| Focused development tiers and budgets are reviewable | `Tools/run-sync-tests.ps1` selects category-filtered assemblies and records XML/log/wall time | Final `Sync.Fast`, `Sync.Loopback`, and `Sync.SceneFocused` jobs are recorded above | Cold Unity startup remains slower; this Fast run is above its provisional target |
