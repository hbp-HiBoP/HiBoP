# Validation, diagnostics and limits

Current contracts are described in [architecture](synchronization-architecture.md), [operations](synchronized-operations.md) and [wire](wire-protocol.md). This page separates points to verify today from historical results. **No Unity tests, builds or physical trials were run for the November 2, 2026 consolidation.**

## Executable coverage

Tests are entry points to select according to the change, not an attestation of current success.

| Domain | Verification sources |
| --- | --- |
| Codecs, bounds, identities and scheduler | [HBP.Sync.Tests](../../../../Assets/Tests/EditMode/HBP.Sync.Tests), notably `ContractCodecTests`, `SchedulerTests` |
| Application, rebase, publication, merge, clock | [HBP.Transfer.Scene.Tests](../../../../Assets/Tests/EditMode/HBP.Transfer.Scene.Tests): `SceneMutationBoundaryTests`, `OnlineMutationDriverTests`, `SceneCheckpointMergeTests`, `TimelineClockEstimatorTests` |
| Real loopback transport, bulk and sessions | `PersistentTransportLoopbackTests`, `MultiSceneTransportTests`, `SessionControlTransportTests`, `SessionPreferencesTests`, `SessionDefinitionsTests` in the same directory |
| Resources and computations | `PreparedSceneArchiveTests`, `ActivityProjectionTests`, then [NativePreparationTests](../../../../Assets/Tests/PlayMode/HBP.Transfer.Scene.PlayModeTests/NativePreparationTests.cs) |
| Closing, replacement, ownership and presentation | [QuestSceneLifecycleTests](../../../../Assets/Tests/PlayMode/HBP.Transfer.Scene.PlayModeTests/QuestSceneLifecycleTests.cs), [QuestAnatomyLifetimeTests](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/QuestAnatomyLifetimeTests.cs), [SceneDisplayOwnershipTests](../../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/SceneDisplayOwnershipTests.cs) |

To run Unity, follow the repository workflow and `hibop-unity-validation` skill. Tests needing the PlayerLoop directly await their asynchronous operations; an observed timeout does not authorize leaving work active and starting another suite. A native handle remains alive until the started work actually finishes.

A useful network trial distinguishes socket admission, Unity application, completed scientific generation and visual observation. "NextVisible" meant CPU eligibility for rendering, not the first image seen in the headset. Do not subtract timestamps from two devices without clock estimation. The historical `ScientificStable` boundary did not receive full qualification.

## Reference measurements

These dated measurements explain choices and limits; they do not qualify current checkout performance. Samples and measurement boundaries differ: they do not form a directly comparable benchmark series.

| Topic and conditions | Observed result | Scope |
| --- | --- | --- |
| Latency of the old snapshot exchange, September 22, 2026; Windows Editor + Quest Wi-Fi, Unity 6000.5.2f1 | Median setter → visible ACK: color **1130.9862 ms** (n=8), cut **1387.7274 ms** (n=9), timeline **1693.3038 ms** (n=25). One initial transfer: **37.402764 s**. | Architecture since replaced. Insufficient samples for planned p95 qualifications (100/300/100). No current runtime latency is inferred. |
| Scheduler inline/bulk boundary, September 24; in-process, 8 warm-ups then 24 measurements | **4096-byte body: 3.98 µs**; **4097-byte body: 53.08 µs**. | Cost of switching paths, without network or rendering. |
| Checkpoint of 30,000 sites, late September | **3,064,297 bytes**, 13 records, 188 chunks; loopback **1840.4 ms**, another matrix **1725.7 ms**. | Two separate trials; no headset performance guarantee. |
| Inflation delivery, October 6; three columns, left MNI hemisphere used as patient, scale 2/1/0.5 and translation 5/7/11 | Payload **5,901,503 bytes**; removal of **804,940 bytes** of compressed GIFTI/TRM, approximately **12%** of the reconstructed previous total of 6,706,443 bytes. | Fastest resource compression; metadata gain excluded. The 0.001 mm hardware parity target was not qualified by this Windows trial. |

The [analyze-sync-baseline.py](../../../../Tools/analyze-sync-baseline.py) analyzer reads earlier exports. It does not reintroduce snapshot/visible ACK telemetry removed from the runtime. Its former collector, bounded to 8192 records, invalidated a measurement on loss. Numerical performance thresholds were deferred on October 8, 2026; no new guarantee is defined here.

## Verified behaviors and limits

| Behavior | Historical observation | What remains separate |
| --- | --- | --- |
| Retention and topology change | On October 6, hardware session of **8192 colors and 64 cut cycles** accepted; topology correction reviewed over two eight-state paths. | Not all operations, modalities and interruptions are covered. |
| Quest controls | User acceptance of the interface scope on October 8. | It qualifies neither every scientific operation nor multi-scene routing added later. |
| Reconnection and offline merge | On October 8, initial site placement, unchanged reconnection, combination of independent edits, Desktop/Quest conflict choice and recovery paths accepted on headset. Local poses were to remain preserved. | Final dialog text changed after the tested build; dialog cancellation had been tested before replacement by a mandatory choice. The status of a scene closed on Desktop remained to be clarified in the multi-scene acceptance procedure. |
| Scene inventory and reloading | On October 12, manual sending and part of the hardware acceptance procedure accepted. Reloading after a normalization change exposed Unity access from a worker, corrected with automated regression tests. | Physical normalization acceptance needed repeating after correction; not all latest reload paths were validated. No full hardware acceptance is inferred from tests. |
| Automated verification after sequential resumption | On October 12: **1067 passed, 1 ignored, 337.25 s** in CLI; **16 passed, 92.36 s** in PlayMode. | Dated results, not a current run. The ignored test depended on an external HBNA fixture. |

The Small acceptance procedure did not cover CCEP, static, fMRI or MEG; correlations were not tested there and several resource operations had only partial bidirectional coverage. Site movement had been visually accepted on Small, without demonstrating Windows/ARM numerical parity. Raw positions must not replace the command contract to conceal a discrepancy.

## Points to qualify

- **Application error boundary.** In [QuestAnatomySession](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomySession.cs), `IsTransientReplicaInterruption` still groups `InvalidOperationException` with IO/socket/disposal interruptions. Qualify this path before promising that a domain error never triggers reconnection. This code finding is not a reproduction of a current user defect.
- **Hardware workflows.** Bidirectional resources, long interruptions/reconnection, preferences during native work, inventory, closing and patient opening require their own trials. Codec or loopback tests replace neither these workflows nor scientific buffer comparisons.
- **Symptoms to isolate if they recur.** A scene becoming invisible after activity removal and reprojection was reported on October 5, without isolated reproduction or demonstrated global correction. The disabled historical statistical parser in [SiteConditions](../../../../Assets/Scripts/HBP/Data/Module3D/SiteConditions.cs) also retains a time-range TODO: its helpers traverse `Trial.AllValues`. This last finding does not concern the active filter job.

A native Mono access violation on October 12 occurred during `RestoreSceneManagerSetup` / `UnloadUnusedAssets`, in a context of concurrent or cancelled suites. Root cause is not established and successful sequential resumption does not prove a native fix. For similar diagnostics, historical local traces were under `.test-results/unitask/crash-2026-10-12/`, report `Crash_2026-10-12_134308403`: read `0x100000000`, code `0xC0000005`, stack `mono_add_process_object`, without a managed heap in the dump. Absence of a plugin from that stack does not exclude earlier corruption.

## Provenance

The [Quest reference results](../../../../Validation/Quest/retained-results.md) contain scientific comparisons and instructions for consulting old reports in Git. Task lists, resumption states and Small acceptance CSVs are no longer part of current documentation. Some linked reports and large local artifacts were never available in the repository; no result was reconstructed from their absence.
