# T00 baseline instrumentation and provisional budgets

Date: 2026-09-22.  
Implementation reviewed from commit `38ce731580a559bba64f86908e8cae5c0c1bce62`, followed by the final corrective working-tree changes validated on 2026-09-22.

## Scope and result

T00 adds measurement and fast-test foundations only. It does not repair the current snapshot/delta protocol, change its wire format, or implement a v2 operation family.

The final implementation provides:

- `Sync.Fast`, `Sync.Loopback`, `Sync.SceneFocused`, `Sync.Qualification`, and `Sync.Device` category names;
- `Tools/run-sync-tests.cmd` and `Tools/run-sync-tests.ps1` entry points for the automated EditMode tiers;
- an injected monotonic clock, fake clock, and manually advanced async gate with no wall-clock waits in timing semantics tests;
- opt-in, bounded telemetry for initial transfer, site color, complete cut changes, and timeline seek/playback;
- immutable capture contexts that atomically bind sink, clock, frequency, main thread, and capture-session generation;
- capture close semantics that stop admission, wait for an already-admitted writer, and do not detach a newer capture;
- separate scope, logical-trace, capture-generation, recording-generation, and network-attempt identities;
- raw points for user request, setter, capture start/end, queue/replacement, encode, first/last write, first/last receive, apply start/end, next-visible, initial publication receipt, and the existing received/applied/visible ACK boundaries;
- explicit provenance for exact, proxy, and unavailable identity dimensions;
- absolute per-thread managed-allocation counters, thread identity, main-thread classification, payload bytes, and monotonic timestamps;
- checkpoint export on Quest pause and clean-close export on Desktop;
- a Python analyzer that refuses contradictory trace semantics rather than silently incorporating them into percentile results.

With telemetry disabled, Sync probes exit on the inactive capture-context path. There is no telemetry `Update`/`LateUpdate`, polling owner, or scene scan. Core business entities contain no Sync-specific state; they expose feature-neutral pre-mutation events consumed by `SyncSetterOriginObserver` only while a measured Desktop replica session is active. The disabled-probe allocation test executes 10,000 warm probes with zero managed bytes allocated on the calling thread.

The final physical Wi-Fi smoke test passed visual verification for:

- initial scene publication;
- site-color convergence;
- complete-cut convergence;
- timeline convergence.

Both final recordings reported `dropped=0` and `valid-local-observations`.

## Exact observable boundaries

| Profile / milestone | Exact production observation |
| --- | --- |
| Initial `UserRequest` | Raw Desktop point captured at the user-request boundary in `QuestManager.SendAsync(false, ...)`, before pairing checks, loading UI, frame switches, or capture preparation. |
| Initial `CaptureStart` / `CaptureEnd` | Raw points immediately before and after `Base3DScene.CapturePreparedAsync`. |
| Initial `Encoded` | Emitted only after archive/streaming preparation has completed successfully. Failed preparation does not emit `Encoded`. |
| Initial first/last write | Captured after the corresponding successful transport-write stages. A partial failure may therefore expose first-write without last-write. |
| Initial `PublicationReceipt` | Desktop point captured only after the receipt returned after Quest publication has been validated. Each send/retry uses a distinct attempt identity. |
| Site `Setter` | `SiteState.Color` raises the neutral pre-mutation `ColorChanging` event. `SyncSetterOriginObserver` preserves the first raw origin until the active scene session consumes it. |
| Cut `Setter` | `Cut` raises one neutral pre-mutation `DefinitionChanging` event for Position, Flip, NumberOfCuts, Orientation, and custom Normal X/Y/Z. `OnModifyPlanesCuts` consumes the preserved origin; later update notifications are not origins. |
| Timeline `Setter` | `BasicTimeline` raises the neutral pre-mutation `AnchorChanging` event for index/play/loop/step. Playback and toolbar notifications may consume the origin but cannot replace it with a later timestamp. |
| Live `CaptureStart` / `CaptureEnd` | Raw points immediately around `LiveGeometryStateAdapter` capture. Sink publication happens after the measured interval. |
| `Queued` | The captured logical trace has become the currently pending representation for its profile. |
| `Replaced` | A queued logical trace was superseded **before it was accepted for a transmission attempt**. A trace that has been accepted for an attempt can no longer become `Replaced`, even while waiting for ACKs. |
| `Encoded` / first/last write | Attempt-local Desktop points. Logical identity and capture generation remain stable while each retry/reconnect receives a new attempt ID. |
| Quest first/last receive | Local Quest transport observations. They use legacy/proxy identity where the current wire protocol cannot carry the exact Desktop identities. |
| Quest apply start/end | Local Quest apply interval around the current production apply path. |
| `NextVisible` | CPU-side frame eligibility after apply, not GPU completion or photons. |
| `ScientificStable` | **Unavailable** in the final T00 implementation because no independent production boundary was qualified. The analyzer reports `unavailable-no-independent-production-boundary` rather than relabelling another milestone. |

CSV row order is admission order, not a causal sort. Analysis sorts within an identity by monotonic timestamp. Desktop and Quest monotonic clocks are process-local and are never subtracted across processes.

## Replacement bug found during physical validation

The first physical smoke test exposed a telemetry-semantic defect that deterministic tests had not originally covered.

The original tracker kept a trace in the pending slot until `VisibleAck`. If a new timeline capture arrived after the old trace had already been accepted for transmission but before its final ACK, the old trace could incorrectly receive both:

- `Replaced`; and
- a real transmission attempt containing `Encoded`, writes, and ACKs.

That made the telemetry contradictory even though the synchronization behavior itself was correct.

The corrective implementation now distinguishes:

1. pending and still replaceable; from
2. pending but already accepted for a transmission attempt.

`SnapshotPendingForAttempt()` atomically marks the selected trace as attempted while the session lock is held. A later capture can replace only a trace that has not yet crossed that boundary. Completion of an older in-flight attempt cannot clear a newer pending trace.

The Python analyzer was also tightened: `Replaced` plus any non-root transmission attempt is `ambiguous-invalid`.

### Negative validation against the old smoke

Re-analyzing the original smoke with the corrected analyzer intentionally produced:

- Desktop integrity: `invalid`;
- Timeline logical groups: 45;
- completed valid attempts: 7;
- valid `replaced-uncompleted`: 28;
- `ambiguous-invalid`: 10.

All ten invalid traces were cases where `Replaced` coexisted with an exact transmission attempt. This proves that the analyzer now detects the defect that previously passed silently.

### Final corrected smoke

The final corrected run produced:

- Timeline logical groups: 89;
- completed attempts: 25;
- valid `replaced-uncompleted`: 64;
- `ambiguous-invalid`: 0;
- incomplete: 0.

The raw recording was also reviewed: replaced traces contain no transmission attempt, while transmitted traces contain no `Replaced` milestone.

## Test tiers and development-loop evidence

Run the focused suites from the repository root:

```powershell
.\Tools\run-sync-tests.cmd -Tier Fast
.\Tools\run-sync-tests.cmd -Tier Loopback
.\Tools\run-sync-tests.cmd -Tier SceneFocused
```

The static assembly gate runs before Unity through the launcher.

An earlier measured corrective pass recorded:

| Tier | Result | Loaded test execution | Total Unity job time |
| --- | ---: | ---: | ---: |
| `Sync.Fast` | 9 passed, 0 failed, 0 skipped | 1.2303482 s | 7.728 s |
| `Sync.Loopback` | 6 passed, 0 failed, 0 skipped | 1.1630142 s | 7.658 s |
| `Sync.SceneFocused` | 7 passed, 0 failed, 0 skipped | 0.7217195 s | 6.436 s |
| **Total** | **22 passed** | **3.1150819 s** | **21.822 s** |

The static assembly gate passed on 44 HBP assemblies and 154 direct edges; its six fixture cases passed.

After the final `Replaced`-semantics correction:

- the new regression test for an accepted in-flight trace versus a newer pending trace passed;
- `Sync.Fast` passed;
- `Sync.Loopback` passed;
- `Sync.SceneFocused` passed;
- the telemetry-overhead test passed;
- the Python analyzer suite passed with the new `Replaced + attempt` rejection case.

Exact post-correction Unity execution times were not retained as a separate final artifact, so the timing table above is retained only as the measured development-loop reference, not as a claim that those exact durations were reproduced after the last small tracker/analyzer correction.

The measured `Sync.Fast` reference remains above its provisional `< 1 s` target. The loop is still short enough for focused iteration, but the target was not demonstrated on the recorded timing run.

## Final physical Wi-Fi smoke evidence

### Recording provenance

| Peer | Recording | State | Samples | Dropped | Integrity |
| --- | --- | --- | ---: | ---: | --- |
| Desktop | `sync-WindowsEditor-20260922-171423-879-89f8a9d5766748778be535eba43c000a.csv` | `closed` / `editor-menu` | 748 | 0 | `valid-local-observations` |
| Quest | `sync-Android-20260922-171537-260-70346e70942e4e38884049449dae354b.csv` | `checkpoint` / `quest-pause` | 215 | 0 | `valid-local-observations` |

Desktop SHA-256:

`ef0e50a831f7ed758c4c610a63e73168ff742d954792315d0527f9dc6c453ab2`

Quest SHA-256:

`a789ffca0c773eef87f2c60291fd7d52b8915064d005f1ef70fc45dd9fd11eda`

Unity version on both peers: `6000.5.2f1`.

Transport label: `WiFi`.

The Desktop was the Unity Editor; the Quest was the Android player. The Quest export was a pause checkpoint rather than a clean application close.

### Desktop observed values

These are nearest-rank values from this smoke only. They are useful baseline observations, **not qualification p95s**, because the required 100/300/100 completion counts were not collected.

| Profile / metric | n | p50 | p95 | p99 |
| --- | ---: | ---: | ---: | ---: |
| Initial / request → publication receipt | 1 | 37402.7640 ms | 37402.7640 ms | 37402.7640 ms |
| Initial / write span | 1 | 8289.8555 ms | 8289.8555 ms | 8289.8555 ms |
| Site color / setter → applied ACK | 8 | 1074.8349 ms | 1308.7709 ms | 1308.7709 ms |
| Site color / setter → visible ACK | 8 | 1130.9862 ms | 1400.9117 ms | 1400.9117 ms |
| Site color / observed capture main-thread interval | 8 | 65.9943 ms | 67.8022 ms | 67.8022 ms |
| Site color / observed capture same-thread allocated bytes | 8 | 0 B | 0 B | 0 B |
| Site color / write span | 8 | 0.1293 ms | 0.5585 ms | 0.5585 ms |
| Cut / setter → applied ACK | 9 | 1387.4186 ms | 1888.2040 ms | 1888.2040 ms |
| Cut / setter → visible ACK | 9 | 1387.7274 ms | 1888.3943 ms | 1888.3943 ms |
| Cut / observed capture main-thread interval | 9 | 49.4278 ms | 124.6703 ms | 124.6703 ms |
| Cut / observed capture same-thread allocated bytes | 9 | 0 B | 0 B | 0 B |
| Cut / write span | 9 | 0.1329 ms | 0.1719 ms | 0.1719 ms |
| Timeline / setter → applied ACK | 25 | 1311.6145 ms | 1852.4816 ms | 2012.3112 ms |
| Timeline / setter → visible ACK | 25 | 1693.3038 ms | 2295.0102 ms | 2417.2063 ms |
| Timeline / observed capture main-thread interval | 89 | 49.2488 ms | 193.2833 ms | 337.0840 ms |
| Timeline / observed capture same-thread allocated bytes | 89 | 0 B | 0 B | 0 B |
| Timeline / write span | 25 | 0.1177 ms | 0.2195 ms | 1.6668 ms |

Desktop trace counts:

| Profile | Logical groups | Attempts | Completed | Replaced-uncompleted | Incomplete | Ambiguous-invalid |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Site color | 8 | 8 | 8 | 0 | 0 | 0 |
| Cut definition | 9 | 9 | 9 | 0 | 0 | 0 |
| Timeline anchor | 89 | 25 | 25 | 64 | 0 | 0 |

### Quest local observations

Quest rows are local observations only. Current live identities are legacy epoch/revision proxies and cannot be joined to exact Desktop logical traces or attempts.

| Profile / metric | n | p50 | p95 | p99 |
| --- | ---: | ---: | ---: | ---: |
| Initial / receive span | 1 | 8167.9238 ms | 8167.9238 ms | 8167.9238 ms |
| Initial / apply end → next visible | 1 | 127.2299 ms | 127.2299 ms | 127.2299 ms |
| Site color / observed apply main-thread interval | 8 | 283.8569 ms | 334.6521 ms | 334.6521 ms |
| Site color / observed apply same-thread allocated bytes | 8 | 0 B | 0 B | 0 B |
| Site color / receive span | 8 | 51.6743 ms | 58.7719 ms | 58.7719 ms |
| Site color / apply end → next visible | 8 | 11.6182 ms | 379.1909 ms | 379.1909 ms |
| Cut / observed apply main-thread interval | 9 | 266.7202 ms | 288.8067 ms | 288.8067 ms |
| Cut / observed apply same-thread allocated bytes | 9 | 0 B | 0 B | 0 B |
| Cut / receive span | 9 | 55.6693 ms | 57.5882 ms | 57.5882 ms |
| Cut / apply end → next visible | 9 | 35.0598 ms | 421.0462 ms | 421.0462 ms |
| Timeline / observed apply main-thread interval | 25 | 251.3871 ms | 291.1394 ms | 319.4372 ms |
| Timeline / observed apply same-thread allocated bytes | 25 | 0 B | 0 B | 0 B |
| Timeline / receive span | 25 | 51.6020 ms | 62.2893 ms | 64.9116 ms |
| Timeline / apply end → next visible | 25 | 412.5782 ms | 447.6811 ms | 467.9288 ms |

Quest trace counts:

| Profile | Local logical groups | Incomplete | Ambiguous-invalid |
| --- | ---: | ---: | ---: |
| Site color | 8 | 0 | 0 |
| Cut definition | 9 | 0 | 0 |
| Timeline anchor | 25 | 0 | 0 |

### Interpretation

The final smoke demonstrates that the instrumentation is capable of producing coherent physical-device evidence without sample loss.

It also exposes the poor latency of the current protocol. In this run:

- site-color setter → visible-ACK was roughly 1.13 s at the observed median;
- cut setter → visible-ACK was roughly 1.39 s at the observed median;
- timeline setter → visible-ACK was roughly 1.69 s at the observed median.

Those values are intentionally reported rather than repaired in T00. The current snapshot/delta/ACK path is the baseline that later tasks replace.

The network write calls themselves are not the dominant cost in the Desktop observations: warm live write spans are typically sub-millisecond. Quest local receive spans are roughly 50–65 ms, while observed Quest apply and next-visible intervals are much larger.

No cross-process subtraction is performed.

## Historical initial-transfer context

Earlier transfer reports remain useful context but are not mixed into the final live-operation percentile calculations.

| Path/run | Click to Desktop completion | Important phase evidence | Status |
| --- | ---: | --- | --- |
| USB forwarding, detailed load, 2026-09-16 | 97.286 s | Desktop preparation 24.096 s; send 7.592 s; Quest receive 7.952 s; Quest post-receive publication 64.138 s | Historical context only |
| Wi-Fi LAN, same instrumented session, 2026-09-16 | 105.533 s | Desktop preparation 17.116 s; Quest receive 11.808 s; Quest post-receive publication 75.243 s | Historical context only |
| Wi-Fi LAN smoke after earlier transfer optimization | 24.769 s | 125,317,658 encoded bytes; neighboring observations 20.027/20.241 s | Historical context only |
| **Final T00 Wi-Fi smoke, 2026-09-22** | **37.403 s request → publication receipt** | Desktop write span 8.290 s; Quest receive span 8.168 s; Quest apply-end → next-visible 0.127 s | Final T00 instrumented smoke; n=1, not a percentile |

Historical sources:

- `../reports/QUEST-transfer-baseline-01.md`
- `../reports/QUEST-transfer-baseline-02-wifi.md`
- `../reports/QUEST-transfer-final.md`

The final 37.403 s initial-transfer observation is above the provisional 30 s future regression guard described below. T00 does not hide or waive that result. Because this was not a controlled paired pre-change/post-change experiment and T00 intentionally does not optimize the protocol, the value is recorded as baseline evidence rather than treated as a T00 implementation repair task.

## Collecting a live baseline

### Enable capture

Desktop Editor:

1. use `Tools → HiBoP → Sync Baseline → Enable for next Play`;
2. enter Play Mode;
3. create/send the measured session only after capture has been enabled.

Desktop player:

- pass `-syncBaseline`; or
- create `sync-baseline.enabled` under `Application.persistentDataPath`.

Quest:

```text
/sdcard/Android/data/fr.crnl.hibop.quest/files/sync-baseline.enabled
```

must exist before application startup.

### Export

Desktop Editor:

`Tools → HiBoP → Sync Baseline → Stop and export`

Quest:

- a normal pause creates a checkpoint export;
- a clean application exit creates the final close export.

Exports are written below `Application.persistentDataPath/SyncBaselines` and log `HBP_SYNC_BASELINE file=...`.

The fixed sink capacity is 8192 samples. When full, newest samples are dropped and `dropped` increments. A valid baseline requires `dropped=0`.

### Analyze

Example:

```powershell
python .\Tools\analyze-sync-baseline.py `
    .\.test-results\t00-device\Smoke `
    --transport WiFi `
    --output .\.test-results\t00-device\smoke-analysis
```

The analyzer:

- never joins Desktop and Quest clocks;
- reports proxy/unavailable identity honestly;
- keeps incomplete traces visible;
- marks contradictory trace semantics as invalid;
- refuses to claim a latency-budget pass when the required completion count is unavailable.

### Qualification-sized collection

For a controlled USB or Wi-Fi qualification campaign, collect after warm-up:

- at least 100 isolated site-color assignments;
- at least 300 continuous cut definitions followed by an idle final value;
- at least 100 timeline seek/play/pause anchors.

The final T00 smoke did **not** collect those counts, so its nearest-rank p95/p99 values remain descriptive smoke statistics only.

## Provisional regression budgets

These are future T08 gates, not measured T00 pass claims. T08 may tighten or revise them using controlled T00-format evidence.

| Profile | Setter → applied-ACK receipt p95 USB / Wi-Fi | Setter → visible-ACK receipt p95 USB / Wi-Fi | Local Unity-thread p95 | Remote apply Unity-thread p95 | Managed allocation after warm-up |
| --- | ---: | ---: | ---: | ---: | ---: |
| Site color | 16.7 / 33.3 ms | 33.3 / 50.0 ms | 0.25 ms | 0.50 ms | <= 256 B |
| Complete cut definition | 16.7 / 33.3 ms | 33.3 / 50.0 ms | 0.50 ms | 2.00 ms | <= 512 B |
| Timeline anchor | 16.7 / 33.3 ms | 33.3 / 50.0 ms | 0.25 ms | 0.50 ms | <= 256 B |

The final Wi-Fi smoke is far above these latency and observed local-interval targets, but the completion counts are intentionally insufficient for qualification. This is baseline evidence about the current protocol, not a T00 requirement to optimize it.

The initial-transfer guard remains separate. The provisional future target is:

- same `Small` fixture;
- controlled cache/thermal state;
- within 30 s click/request-to-publication;
- and within 20% of a paired pre-change run until a multi-run p95 exists.

The final T00 smoke observed 37.403 s request-to-publication and therefore would not meet the literal 30 s target. T02 must treat this as visible baseline evidence and must not claim the guard passes without a controlled paired measurement.

## Known limitations assigned to later tasks

- The measured live path is still the current snapshot/delta/visible-ACK implementation. T00 intentionally does not optimize or redesign it.
- No operation-matrix row is complete. The three live profiles are instrumentation coverage, not v2 DTO/handler or bidirectional-driver evidence.
- The final current-code physical smoke is Wi-Fi only. A final-code USB live campaign was not collected.
- Qualification-sized 100/300/100 completion counts were not collected, so live p95/p99 budget pass/fail remains unavailable.
- Desktop and Quest process clocks are independent and cannot be subtracted.
- Quest live identity remains an explicitly labelled legacy epoch/revision proxy because T00 does not change the wire format.
- Initial Quest traces expose no exact Desktop logical/capture/attempt identity.
- Several Desktop logical changes may collapse into one legacy Quest revision and cannot be reconstructed remotely.
- `ScientificStable` is unavailable because no independent scientific-stability production boundary was qualified.
- `NextVisible` means CPU-side next-frame eligibility, not GPU completion or photons.
- The sink deliberately drops newest samples after 8192 rows. Any run with `dropped > 0` is invalid.
- Abrupt process termination can still lose in-memory telemetry that was never checkpointed/exported.
- The Quest final smoke artifact is a pause checkpoint, not a clean-close export; the analyzer labels checkpoint-only budget status accordingly.
- Absolute managed-allocation counters are meaningful only when subtraction occurs on the same managed thread.
- Observed capture/apply intervals are adapter segments, not total business CPU cost.
- The active-scene observer remains bounded by active scene membership, but retained `HashSet` capacity may reflect the maximum concurrent scene cardinality observed during its lifetime.
- Exact final-code Unity tier execution times were not separately captured after the last tracker/analyzer correction. The functional focused tiers were rerun green; the earlier measured timing table remains the development-loop reference.
- The recorded `Sync.Fast` reference of 1.2303482 s does not demonstrate the provisional `< 1 s` target.
- Full visual parity, scientific parity, and GPU/photon visibility remain qualification work rather than T00 claims.

## Closure table

| Acceptance criterion | Production point | Deterministic test or final measurement | Remaining limitation |
| --- | --- | --- | --- |
| Initial origin is the real user request and capture start is distinct | `QuestManager.SendAsync(false)` plus the `DesktopSceneCapture` capture boundary | Raw-point ordering plus final physical initial-transfer trace | Publication receipt is an upper bound, not photons |
| Desktop observes Quest publication receipt | `SceneDelivery.SendAsync` records receipt only after validation | Retry/success deterministic test plus final physical `PublicationReceipt` | Exact Quest-side attempt cannot be reconstructed on the legacy wire |
| Site/cut/timeline origins come from the business setter | Neutral pre-mutation Core events observed in `HBP.Sync.Scene` | Focused setter tests for color, cut Position/Flip/NumberOfCuts/Orientation/Normal X/Y/Z, and timeline; final physical convergence | Later render/update events remain intentionally excluded as origins |
| First setter survives capture restart | `SyncSetterOrigin` generation-aware first-point retention | `CaptureRestart_ReplacesOldDirtyOriginButPreservesFirstNewSetter` | None observed in final smoke |
| Pending replacement does not merge disjoint profiles | Fixed profile slots in `SyncTraceTracker` | Deterministic mixed-profile capture/clear tests | Legacy Quest revision may still collapse multiple Desktop profiles |
| A trace accepted for transmission is no longer replaceable | `SnapshotPendingForAttempt()` under the session lock | Regression test plus final Timeline smoke: 25 completed, 64 replaced-uncompleted, 0 ambiguous-invalid | Current protocol still serializes/coalesces slowly |
| `Replaced` semantics are mechanically checked offline | `analyze-sync-baseline.py` | Old smoke becomes invalid with 10 `Replaced + attempt` contradictions; final smoke is valid | Analyzer can only validate evidence that was exported |
| Retry/reconnect identity is honest | Stable logical/capture identity plus new exact attempt identity | Identity tests and transport retry tests | Quest attempt identity unavailable on legacy wire |
| Receive facts survive ACK/rejection write failure | Quest receiver publishes already-acquired receive/apply facts in `finally` | `QuestReceiver_ReceivedAckWriteFailureRetainsReceiveFacts` and rejection-write companion test | Failed transport may legitimately produce only a prefix of milestones |
| Instrumentation is outside measured intervals | Raw points captured around work, sink publication afterwards | Timestamp-order, failure, and overhead tests | Observed segment is not total business CPU |
| Capture close is race-safe | Capture session stops admission and drains admitted writer | deterministic close-with-writer-in-flight regression test | Abrupt process death before export remains outside this guarantee |
| Disabled telemetry is bounded | No recurring telemetry owner; inactive probes return immediately | 10,000-probe zero-allocation overhead test | Constant inactive branch still exists |
| Physical Desktop recording is lossless for the smoke | bounded Desktop sink | final smoke: 748 samples, `dropped=0`, valid integrity | Not qualification-sized |
| Physical Quest recording is lossless for the smoke | bounded Quest sink + pause checkpoint | final smoke: 215 samples, `dropped=0`, valid integrity | Checkpoint, not clean-close artifact |
| Quest identity limitations are explicit | legacy-proxy/unavailable factories | final analyzer reports local-only proxy groups instead of cross-peer joins | Requires later wire protocol for exact end-to-end identity |
| `ScientificStable` is honest | no substitute production milestone | final analyzer reports unavailable | Requires a later qualified scientific-stability boundary |
| Assembly direction is mechanically enforced | `check-assembly-dependencies.ps1` before Unity tests | 44 assemblies / 154 edges pass; six checker fixtures pass | Policy must be reviewed when intentional direct edges change |
| Focused test tiers exist and remain short | category-filtered launcher | measured reference table plus final green reruns | Exact final post-correction timing artifact not retained |
| Provisional budgets are reviewable without false passes | analyzer completion thresholds and explicit unavailable states | final smoke reports `insufficient-samples` / `checkpoint-only`, never a false pass | Controlled 100/300/100 USB/Wi-Fi campaigns remain future qualification work |

## T00 closure

The final T00 implementation is considered functionally and observationally complete for its intended scope:

- focused deterministic tests are green;
- the Python analyzer regression suite is green;
- the assembly-direction gate is green;
- physical Desktop → Quest Wi-Fi transfer and live synchronization are visually correct for the three instrumented profiles;
- final Desktop and Quest recordings are valid and lossless;
- replacement/coalescing semantics are unambiguous in the final physical trace;
- known unavailable boundaries and identity limitations are explicitly represented rather than inferred.

The poor latency exposed by these measurements belongs to the protocol and later synchronization tasks. T00's responsibility is to make that behavior observable, attributable, and regression-testable without silently inventing stronger evidence than the production system provides.
