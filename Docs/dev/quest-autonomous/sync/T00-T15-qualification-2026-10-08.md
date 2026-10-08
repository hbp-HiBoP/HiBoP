# Qualification of synchronization tasks T00 through T15

Qualification date: 8 October 2026. This audit compares T00–T15 acceptance criteria with the current production paths, fresh focused tests, and later recorded hardware acceptance. It preserves the approved scope and historical evidence of each task.

## Subsequent user decisions — 8 October 2026

The findings below describe the audit evidence, not additional mandatory release gates. The user confirmed shared hemisphere site placement must survive initial delivery and reconciliation; atlas files are bundled with both builds and must never be transferred at runtime. Preserve current selection rules and adapt stale fixtures. Investigate the temporary-file cleanup failure. Repair the large authoritative-correction receiving path. Existing manual scientific/Quest acceptance remains valid; unproven coverage gaps and the unreproduced remove/reproject report are deferred. Judge performance by feel; do not add instrumentation or enforce numerical timing budgets. Reconciliation must combine independent changes and ask only for conflicting dependent groups, superseding the former global no-merge choice. Local pose is preserved and preferences remain Desktop-authoritative.

Most planned functionality exists. The focused synchronization, integrity and Quest UI suites pass, but the native production PlayMode selection fails seven of twenty-one cases. Complete qualification of every synchronized family is therefore not established. Two current correctness gaps matter before T16 can promise whole-scene reconciliation: v2 checkpoints omit D14 moved-site state, and large authoritative proposal corrections have no supported receiving path. Other open items are failing native fixtures, resource-backed test coverage and performance/manual qualification, rather than demonstrated missing handlers.

## Qualification by task

“Automated qualified” means the relevant existing tests passed in this audit and production integration was inspected. It does not substitute for a separately required hardware, performance, or human acceptance gate.

| Task | Current qualification | Evidence and remaining boundary |
| --- | --- | --- |
| T00 | Historical baseline preserved; foundation checks pass | The approved [baseline](T00-baseline-and-budgets.md) remains historical. Existing telemetry foundation tests pass within Fast; analyzer tests pass 24/24 and architecture-checker self-tests pass 6/6. ScientificStable and full end-to-end budget evidence remain unavailable as explicitly allowed by the baseline record. |
| T01 | Automated qualified; task status is stale | All twelve originally planned `V2ContractCodecTests` exist and pass, together with later family codecs. Its “Ready for implementation” status no longer describes the current code. |
| T02 | Integrity implementation qualified; paired performance gate open | Archive/buffer/file-transport integrity tests pass. Later anatomy-transfer tests cover capture/restoration. Existing hardware reports establish successful publication, but not a controlled paired pre/post comparison against both the 30-second and 20% regression criteria. |
| T03 | Functional scheduler/index/job checks qualified; Fast timing target missed | Fast covers fake-time retry, coalescing, bounds, independent bulk, conflict indexes and incremental identity. The current combined Fast run takes 3.404 s, above the documented one-second target. That is a development-loop qualification gap, not a functional test failure. |
| T04 | Automated qualified | Production-boundary tests cover constant-time site targeting, complete cut application, timeline intent, nested suppression, targeted invalidation and whole-checkpoint prevalidation. |
| T05 | Automated qualified | Transport protocol and loopback suites pass, including fragmentation, origin/reliable sequence separation, retry, stream retirement, bounded shutdown and bulk progress. This does not qualify every later application schema carried by the transport. |
| T06 | Original online authority/driver scope qualified | Both directions, same-key/disjoint-key admission, corrections, deduplication and grace behavior pass. Later retention coverage and successful hardware stress are recorded in [T06](tasks/T06.md). The later large-correction integration gap below remains explicitly recorded there. |
| T07 | Focused concurrency checks qualified; native projection qualification fails | Generation/lease, stale publication, manual/automatic policy and both sensitive-operation race orders pass. The selected native projection test fails because the fixture cannot load the required `mars` atlas; the failure reproduces in isolation. Native projection cannot be freshly qualified from this run. |
| T08 | Original journal/cutover behavior qualified; performance gate open | Current tests cover ordered replay, checkpoint fallback, retained checkpoints across interruption and the initial live barrier. Later M1/M2 and T15 evidence supersedes blanket claims that no headset/real-scene testing occurred. Required qualifying color/cut/timeline p95, GC and main-thread datasets are still absent. |
| T09 | Implemented; complete row qualification remains open | The bidirectional safe-family batches, checkpoint application, timeline clock policy and missing-resource rejection pass. Positive v2 driver/checkpoint evidence is incomplete for D28 atlas toggles and D29–D31 prepared overlays/calibration. The session-atlas milestone supplies later positive DiFuMo hardware evidence, but not the complete per-row bidirectional matrix. |
| T10 | Substantial historical qualification; current native failures and checkpoint gap remain | Current cut-control tests qualify creation/deletion/edit barriers and checkpoints. Seven native mesh/mask cases pass, supported by the later hardware acceptance. Six M2 production geometry cases currently fail: five reject the fixture's hidden-site selections, and cold inflation reports a locked `buffers.pack` during cleanup. Automatic-cut selection rejection reproduces in isolation, so historical acceptance does not establish a fresh full pass. D14 moved-site state is absent from checkpoints. M2 records D14 visually OK on Small, but no numeric cross-platform parity qualification or movement checkpoint regression was found. Dedicated positive v2 success evidence remains incomplete for other prepared-resource/ROI variants. |
| T11 | Bulk/configuration behavior qualified; full modality matrix open | Real 30,000-site checkpoint and interleaved bulk tests pass, as do D33 validation/rollback/replay and D34 persisted assignments. Later T14 tests include an accepted Quest blacklist after busy-scope release, so a blanket “no Quest blacklist driver evidence” claim is stale. Complete bidirectional coverage of the remaining resource/scientific variants, especially D25 MarsAtlas and D23–D26 resource choices, is not established. Large corrections also affect these later families. |
| T12 | Automated qualified; Small filter behavior recorded | Canonical masks, checkpoint preservation, job cancellation/generation fencing, exact Ready and offline capability checks pass. M2 records successful Desktop filter/reset behavior on Quest. This does not claim physical validation of every interruption boundary. |
| T13 | Automated qualified | Result/provenance and display state survive typed checkpoints; invalid later-column data cannot partially publish. Production chunk transfer, interleaved traffic, filter/correlation exclusion, cancellation and stale-generation cases pass. M2 correlation scenarios remain marked NT, so hardware result/progress UX is not established by that recipe. |
| T14 | Automated qualified; scientific busy UX review remains separate | Both terminal orders, stale A after B, safe versus sensitive operations, automatic/manual policy, connection-loss cancellation and removal controls pass. M2 marks long-running busy interaction scenarios NA. The recorded projection/remove/reprojection rendering report has not been explicitly closed by a matching hardware recipe; passing removal-control tests alone do not prove that rendering issue fixed. |
| T15 | Closed and user-accepted for its approved scope | [T15](tasks/T15.md) explicitly records final user acceptance on 8 October. Fresh prefab and Quest control tests pass. That closure covers UX lots 01–11 and does not imply that every scientific handler or T16/T17 behavior is exposed or accepted. |

## Confirmed correctness gaps

### D14 moved-site state is absent from checkpoints

`Base3DScene.MoveSitesToHemisphere` and `ResetSitesPositions` execute the shared movement commands. `Column3D.MoveAllSitesToTheSameSideOfAPlane` writes each site's local position. In contrast, `V2SceneMutationBoundary.CaptureT10Records` exports cuts, ROI state, prepared-resource choices and masks, but no movement/position state. `IsCheckpointT10Mutation` excludes `MoveSites`, and checkpoint application has no equivalent position restoration. D34 deliberately excludes positions.

This affects existing publication fallback as well as future reconnect. `V2PublicationMutationJournal.Complete` replaces the mutation journal with `CaptureCheckpoint` on overflow. A movement accepted after the original capture therefore has no corresponding state in that fallback checkpoint. Desktop/Quest state choice in T16 cannot restore the winning moved-site state using the current checkpoint.

Evidence: [movement implementation](../../../../Assets/Scripts/HBP/Data/Module3D/Column3D.cs), lines 570–589; [checkpoint capture and allowlist](../../../../Assets/Scripts/HBP/Sync/Scene/V2SceneMutationBoundary.cs), lines 1250–1280 and 1398; [publication fallback](../../../../Assets/Scripts/HBP/Sync/Scene/V2PublicationJournal.cs), lines 122–125. Legacy `LiveGeometryStateAdapter` position support does not qualify this v2 path. The command-only D14 contract must be preserved when resolving this gap; this audit does not authorize a position-batch redesign.

### Authoritative corrections above 4 KiB cannot be reassembled

`DesktopV2ReplicaSession.EnqueueQuestProposalDecision` queues schema-three correction/decision bytes using `EnqueueSceneOperation`. Above 4,096 bytes the scheduler sends a bulk descriptor and independent chunks. With this caller's default flags, the descriptor is on the Interactive lane with decision schema three.

Quest's `V2SceneOperationBulkReceiver` only recognizes SceneControl/schema-one mutation descriptors. Quest instead reaches its inline decision decoder and treats the bulk descriptor bytes as the correction itself. Large mask/ROI authoritative corrections consequently cannot resolve through the current production path. The existing small schema-three rejection test does not exercise this boundary.

Evidence: [Desktop decision enqueue](../../../../Assets/Scripts/HBP/UI/Quest/DesktopV2ReplicaSession.cs), lines 1243–1248; [scheduler bulk routing](../../../../Assets/Scripts/HBP/Sync/V2Scheduler.cs), lines 704–735; [bulk receiver admission](../../../../Assets/Scripts/HBP/Sync/Scene/V2SceneOperationBulkReceiver.cs), line 70; [Quest decision decode](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestV2ReplicaSession.cs), lines 504–506. The later T06 retention report already preserves this follow-up. It needs production coverage on both sides of the inline threshold, including interruption and completion.

### Quest application failure handling needs a focused regression

Quest's general application-exception path starts reconnect grace whenever stream teardown leaves the transport in `DisconnectedGrace`, even for an application failure that is not classified as transient. A retained bad application record may be retried. This is a fault-boundary concern, not a reproduced standalone T16 blocker in this audit.

`InvalidDataException` does **not** inherit `IOException`; that explanation would be incorrect. Transport-level invalid data faults the transport, and Desktop's incoming application observer terminates on exceptions. The concern is specifically [Quest's general exception handler](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomySession.cs), lines 566–584. The successful later topology fix must not be described as a remaining reproduced topology failure.

## Fresh verification

Environment: existing HiBoP Unity `6000.5.2f1` editor, Windows Desktop platform. No APK build or new physical-device recipe was performed. Test jobs ran sequentially through Unity MCP, following the static assembly gate. The gate passed for 44 HBP assemblies and 158 direct edges; six checker self-tests passed. `git diff --check` passed before the audit edits.

| Run | Result | Unity test duration | Job |
| --- | --- | --- | --- |
| EditMode Sync.Fast, Sync and Transfer scene assemblies | 80 passed, 0 failed/skipped | 3.4042195 s | `a33450345e1f43c68c54057f25a09812` |
| EditMode Sync.SceneFocused, same assemblies | 213 passed, 0 failed/skipped | 86.2090038 s | `50aae3e7cfb64264b8218e33cdc4140b` |
| EditMode Sync.Loopback, same assemblies | 69 passed, 0 failed/skipped | 138.1129179 s | `14c20832805945adbf52053d0d3b3945` |
| EditMode archive/buffer/file transport, preferences and Quest prefab/layout filters | 82 passed, 0 failed/skipped | 37.9031350 s | `02a1a7a9d64c47ebb5992c873f1ca559` |
| PlayMode Quest control/input/pairing/loading and preferences filters | 64 passed, 0 failed/skipped | 11.2683127 s | `0243ed66598649fe97416bdbb49ea3b0` |
| PlayMode native masks, production geometry/anatomy, reset and rendering readiness filters | 14 passed, 7 failed, 0 skipped | 52.6536160 s | `258f5d8fe6b14082b3fa039d8d4f8ae0` |
| PlayMode isolated diagnostic rerun: native projection and M2 automatic cuts | 0 passed, 2 failed, 0 skipped | 3.8699926 s | `eab04150686c4c6caeab6438869bdb09` |

MCP domain reload discarded the detailed PlayMode results. The matching freshly written `Application.persistentDataPath/TestResults.xml` was inspected and copied after each run to `quest-ui-playmode.xml`, `native-playmode.xml` and `native-isolated-playmode.xml`; these establish exact pass/fail/skip counts and test names. Raw job results are retained under `.test-results/t00-t15-qualification-20261008/`. Categories overlap, and the last run repeats two cases, so these counts describe test executions rather than distinct cases.

The native run's fourteen passing cases comprise seven prepared triangle-mask variants, four current-scene anatomy transfer variants, transferred cut handles before/after replacement, production T11 reset rejection/replay, and explicit projection readiness awaiting. Its seven failures are:

- `Base3DScene_HbpCoreComputesRuntimeCutTexturesAndSurfaceActivity`: `InvalidDataException: Atlas is not installed: mars`. This reproduces in isolation. It is a resource/fixture qualification failure; the audit does not establish a runtime geometry defect from it.
- Five M2 cases (automatic cuts/prepared inflation, Left and Right hemisphere mutation, Desktop-only cache, transformed single inflation): `InvalidOperationException: A site hidden in the prepared scene cannot become a new shared selection`. The common fixture selects every `!site.State.IsMasked` site at `SceneRestorationPlayModeTests.cs:404–406`, while the current production rule also checks ROI, filtering and blacklist visibility at `V2SceneMutationBoundary.cs:3418–3420`. The automatic-cut case reproduces in isolation. The fixture's selection assumption is stale; these scenarios still require correction and requalification.
- `M2_ColdInflation_ConvergesAndCancelsThroughProductionV2Sessions`: cleanup throws `IOException` because `buffers.pack` is still in use, through `PlayModeTempDirectoryScope.Dispose` at line 31. This may conceal an earlier failure; it does not establish successful cold inflation. The remaining lease/cleanup cause was not resolved by this audit.

The final console snapshot contains the same missing-atlas exception; no clean-console claim is made. No runtime or test source changes were made. The test-induced Enter Play Mode option change was restored in the live editor and on disk.

The Python analyzer's first attempt encountered sandbox temporary-directory permissions in three cases. Repeating the unchanged suite with `tempfile.tempdir` under the workspace passed all 24; this was an environment-path issue, not an analyzer defect.

## T16 prerequisites

The current checkpoint model identifies the relevant family owners: T03's identity foundation; T04/T06's original mutation/authority boundary; T09 presentation/selection/timeline; T10 structural/resources/masks; T11 scientific/configuration/persisted sites; T12 filter state; and T13 correlation data/display. T07/T14 own job cancellation and busy lifetimes. Session preferences remain independently Desktop-authoritative through the completed session-preferences/atlas milestone.

Thus there is no need to ask the user to invent the family list. T16 still needs complete family identity integration, the reconciliation protocol/controller and authored UX, fresh Desktop preferences reconciliation with reload guards, and its own failure tests. Those are T16 work, not evidence that T00–T15 handlers do not exist. The moved-site checkpoint and large-correction defects above require resolution before claiming whole-scene convergence for all current synchronized operations.

T15 remains closed. Existing historical reports and operation-matrix entries should be read with the later evidence in this audit; this report does not silently mark unsupported rows complete or rewrite earlier measurements as fresh results.


## Implementation follow-up — 8 October 2026

The audit above is preserved as historical evidence. The user-approved corrections and T16 implementation now supersede its open prerequisite findings:

- Hemisphere placement is an absolute original/left/right shared state. Configuration clone/copy, initial delivery, checkpoint fallback, restored resource geometry and reconciliation preserve it; scene/column pose remains local.
- Large authoritative corrections use the same bounded bulk reassembly/validation path as large mutations, with the authoritative payload's touched keys. Inline and large dense-mask cases pass.
- Native selection fixtures now use the current visibility rule and explicitly show/unfilter their test contacts. A burst-selection check now waits for the preceding selection to arrive before testing the burst, avoiding a false early match. Production visibility rules are preserved.
- The native projection fixture installs its bundled Mars atlas into its isolated temporary data folder; no runtime atlas transfer was added. The previous `buffers.pack` cleanup failure did not recur in the six passing geometry/inflation variants; no speculative file-cleanup workaround was added.
- Reconnection performs semantic three-way merging and choices per incompatible dependency group. Fresh preferences remain Desktop-authoritative. Cancellation, commit interruption, failed retry, lost progress, resumed live edits and orphaned Quest use are covered.
- Existing manual scientific/Quest acceptance remains valid. Numerical performance gates/instrumentation and the unreproduced remove/reproject report remain deferred by user decision.

The final implementation runs pass: 128 checkpoint/merge/boundary tests, 63 production loopback tests and 30 native/UI/preferences PlayMode tests. The final small filter-state follow-up passes three targeted regression tests. The assembly dependency gate passes (44 assemblies, 158 edges). See [T16's completion report](tasks/T16.md) for job identities and limitations. Physical-headset UX/perceived performance have not been freshly accepted by these editor runs.
