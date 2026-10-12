# T18 cleanup and qualification — 12 October 2026

## Approved scope and preserved behavior

The approved scope is exhaustive removal of the experimental S1–S3 synchronization and direct renaming of the operation-based runtime. The audit follows commit `518b3f0d4b2f4db45b52df5fbb5f7160b6828c6f` through T17. No compatibility aliases were introduced.

Initial scene delivery, authentication/pairing, session preferences and installed atlases, duplex typed mutations, job cancellation/freshness, multi-scene lifecycle and semantic offline reconciliation remain active. `OnSharedStateChanged`, prepared-resource catalogs, current typed checkpoints and timeline generation handling remain necessary and are preserved. The former Desktop session name is reused only after deleting its experimental implementation.

The wire signatures, enum numeric values and current schema numbers are preserved. The current mutation schema is 2, typed publication checkpoint schema is 7 and correlation checkpoint record schema is 2. Old experimental schemas are rejected, including timeline payload/checkpoint schema 1 and previous publication checkpoint schemas. The correlation float encoding still rejects non-finite values and normalizes negative zero. Protocol/identity strings such as `JPAKE-v1`, `discovery-v1`, `atlas-inventory-v1` and installed resource versions retain their meaning. Transfer-archive legacy format handling also serves pairing and is outside the deleted live-sync protocol.

All 40 renamed scripts retain their original `.meta` GUIDs. Wire enum names that would collide with domain enums are `MutationCutOrientation`, `MutationMeshPart` and `MutationSurfaceRepresentation`; direct replacements avoid aliases.

## Acceptance and evidence

| Acceptance | Evidence | Boundary |
| --- | --- | --- |
| A1 — compile affected assemblies | Unity 6000.5.2f1, `HiBoP@77bfedb67d0f947c`; successful script compilation and console inspection. Architecture gate: 44 assemblies, 162 edges. Checker self-tests: 6/6. | Desktop Editor compilation; no new Android package build or physical installation claimed. |
| A2 — remove obsolete architecture/shims | Full-source/configuration/tool search finds no references to the removed adapter, state graph, delta, merge, wire, capture tracker or legacy receiver. Independent read-only review confirms unchanged current encoders and GUIDs. | Current typed checkpoint/reconciliation code is retained deliberately. |
| A2 — synchronized operation regressions | Fast and full EditMode runs below cover codecs, schedulers, scene boundaries, drivers, transport, job freshness, pairing and preferences. Existing [D1–D34 operation contracts](operation-matrix.md) remain authoritative. | Suite results do not establish new exhaustive USB/Wi-Fi, visual or scientific qualification of every matrix row. Earlier accepted/deferred coverage remains recorded in the [T00–T15 audit](T00-T15-qualification-2026-10-08.md), T16 and T17. |
| A3 — initial delivery and inactive Desktop | Retain current transfer/archive regression suites and prepared/live-delivery PlayMode tests, now bound through `SceneMutationBoundary`. Remove the V1 capture observer and its otherwise unconsumed Core pre-mutation hooks. Analyzer: 24/24 tests. | No new numerical performance budget, instrumentation or hardware timing requirement; approved later decisions remain in force. |
| A3 — real scene and physical qualification | See named PlayMode runs and limitations below. | Physical USB/Wi-Fi milestones require separate human/device evidence. |

## Fresh automated runs

The runs use the existing Editor via MCP; no second Editor was started. Jobs are polled to completion and errors are inspected separately. Local raw evidence is under `.test-results/t18/`.

| Run | Job ID | Result |
| --- | --- | --- |
| Before cleanup: Sync.Fast | `fba74b4fa8ba4247a05807c1f20a068c` | 80/80 passed, 5.290 s. |
| Before cleanup: full Sync, Transfer.Scene, Quest.Anatomy, Serialization EditMode assemblies | `afc4bd9c1ef641de9a033a0c15644442` | 1111 completed; four recorded failures listed below. MCP returned no result summary for this failed run, so no skipped/passed totals are inferred. |
| After cleanup: Sync.Fast | `b30c596c2c4a4a37933ca044fb4f01d2` | 70/70 passed, 5.153 s. Ten old capture-telemetry tests removed. |
| After cleanup: same four complete EditMode assemblies | `fab102db76c84889a229b95e13ccf50b` | 1066 completed; the pre-existing serialization manifest failure and two outdated legacy-schema acceptance assertions failed. 45 obsolete test cases removed overall. MCP returned no result summary; no skipped/passed totals are inferred. |
| Final codec and scene-boundary regression run, after changing the two legacy-schema assertions to rejection | `a9b60a7f6d5c46d584a5b8616690eaf4` | 133/133 passed, 5.818 s; zero console errors. |
| Native-scene PlayMode qualification: six-modality project save/transfer, initial delivery, geometry, cancellation, reconciliation, reset and multi-scene lifecycle | `51d2c8eb117448558439aeb5b3cf06c6` | 12 completed; three stale test-expectation failures described below. MCP returned no result summary. |
| Final focused PlayMode rerun: cold inflation, prepared/live delivery, exact correlation replay/tampering and seven prepared triangle-mask scenarios | `e1389f1a89494fdf874fae20bc40b43b` | Succeeded; all 11 selected cases completed, no failures, zero console errors. MCP returned no result summary/duration after the PlayMode domain reload. |

Baseline failures, observed before any source modifications:

- `SerializationContractAuditTests.SerializedMemberManifest_MatchesApprovedContractSurface`: expected 573 serialized members, actual 577; SHA256 `f5e227afcb525e8b0c97cf663657db1110eae17eac454edb6de9293580e9f1d5`. No Core serialization contract was changed by T18.
- `PersistentTransportLoopbackTests.ActivityProjectionBusyScope_QuestDriverAllowsSafeMutationAndCorrectsSensitiveMutationUntilBothTerminals`: expected Duplicate, observed Rejected before cleanup; passed in the after-cleanup full run. Synchronization semantics were not altered to hide the pre-existing intermittent assertion failure.
- The two `QuestReplicaTelemetryFailureTests` cases expected IOException but observed InvalidOperationException. Both exclusively exercised the removed legacy receiver and are removed with it.

The broad before/after consoles contained the same four native missing-file, EditMode mesh-instantiation and malformed-JSON diagnostics from fixture/error tests. Subsequent compilation, Fast and focused codec/boundary runs had zero errors. PlayMode console observations are recorded below.

The first PlayMode run completed both real-project six-modality save/reload/transfer cases, automatic cuts/prepared inflation, both offline-reconnect scenarios, production reset rejection/replay and both T17 lifecycle scenarios without reported failures. The three failures were old expectations also present in HEAD before T18: cold inflation required an unrelated prefab error that no longer occurred; both initial-delivery tests compared source-empty UVs with generated projection UVs, immediately before asserting those arrays must differ. The tests now require generated projection UVs to match the vertex count while retaining exact geometry, other UV, color and atlas comparisons, and no longer demand an unrelated prefab exception. Production behavior was not altered for these failures.

The final PlayMode rerun succeeded for these three corrected cases plus exact correlation-resource replay/tampering and all seven prepared triangle-mask scenarios. Both PlayMode runs ended with zero console errors. At this first handoff, the pre-existing serialized-member manifest expectation remained unresolved and the full four-assembly suite had not been repeated after the focused reruns. The follow-up below records its correction.

## Follow-up: remaining test corrections — 12 October 2026

The serialized-member manifest reference is updated from 573 to 577 entries and SHA256 `f5e227afcb525e8b0c97cf663657db1110eae17eac454edb6de9293580e9f1d5`. Its only differences are `Cut.ID` (T08), `RegionOfInterest.ID` and `Sphere.ID` (T10), and `VisualizationConfiguration.SitePlacement` (T16). Removing those four lines from the actual manifest reconstructs the previous approved count and SHA256 exactly. No serialization behavior is changed.

The live activity-projection integration test no longer manually replays a proposal after awaiting its correction and assumes the decision still exists. Application-completion progress may already have retired that decision, so the direct replay raced with normal retention. The test still verifies authoritative rollback, zero pending proposals, connected peers, the busy scope until both terminal events, and acceptance of a fresh sensitive mutation after release.

The deterministic `ActivityProjectionRejectedProposal_IsDuplicateUntilRetiredThenRejectsOriginalOrigin` test covers the removed replay assertion using the same nonzero origin sequence as production transport: initial `activity_projection_busy` rejection, duplicate with identical correction while retained, `retired_proposal` after explicit retirement and busy-scope release without applying the mutation, and successful acceptance of a fresh operation with a new origin sequence. Production synchronization code is unchanged by this follow-up.

| Run | Job ID | Result |
| --- | --- | --- |
| Serialization contract audit and both activity-projection regression tests | `676a9da0a5b74571afff130d97e0ce53` | 5/5 passed, 4.187 s; zero console errors. |
| Complete Sync, Transfer.Scene, Quest.Anatomy and Serialization EditMode assemblies after corrections | `85326012940748b3b769e260e7897274` | Succeeded; 1067 cases completed, no recorded failures. MCP returned no result summary, so a passed/skipped breakdown and test duration are not inferred. |

The full run ends with the same four console diagnostics seen before T18: the intentionally missing GII fixture, the existing EditMode `MeshFilter.mesh` instantiation warning logged as an error, and two malformed-JSON fixture exceptions. There are no new console diagnostics. Both identified failing tests are corrected; the previously failing full four-assembly selection now succeeds. Raw follow-up evidence is `.test-results/t18/fix-focused.json` and `.test-results/t18/fix-full.json`.

## Deleted files

Each source below and its `.meta` are deleted. Obsolete methods are also removed from `QuestAnatomySession`, `Base3DScene` and old adapter-only PlayMode tests. Initial-delivery and current production-session tests remain.

- `Assets/Scripts/HBP/UI/Quest/DesktopReplicaSession.cs`
- `Assets/Scripts/HBP/Sync/SharedState.cs`
- `Assets/Scripts/HBP/Sync/SharedStateSchema.cs`
- `Assets/Scripts/HBP/Sync/SharedStateCodec.cs`
- `Assets/Scripts/HBP/Sync/ReplicaDelta.cs`
- `Assets/Scripts/HBP/Sync/ThreeWayStateMerge.cs`
- `Assets/Scripts/HBP/Sync/Scene/LiveGeometryStateAdapter.cs`
- `Assets/Scripts/HBP/Sync/Scene/ReplicaClock.cs`
- `Assets/Scripts/HBP/Transfer/Transport/ReplicaWire.cs`
- `Assets/Tests/EditMode/HBP.Sync.Tests/SharedStateTests.cs`
- `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/ReplicaWireTests.cs`
- `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/QuestReplicaTelemetryFailureTests.cs`
- `Assets/Scripts/HBP/Sync/SyncTraceTracking.cs`
- `Assets/Scripts/HBP/Sync/Scene/SyncSetterOriginObserver.cs`
- `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/SyncBusinessSetterTelemetryTests.cs`

The retained `CorrelationResultResource` owns its small canonical-float helper instead of keeping `SharedState.cs`. The development retention diagnostic reads a live per-scene session instead of the never-populated singleton replica field. Adapter-only automatic-cut snapshot marking and the uncalled prepared functional-resource assertion are also removed.

## Renamed files

Every row includes the matching `.meta` move. Other current call sites and tests use the new names directly. Dated historical reports retain old names as evidence.

| Previous path | Standard path |
| --- | --- |
| `Assets/Tests/EditMode/HBP.Sync.Tests/V2ContractCodecTests.cs` | `Assets/Tests/EditMode/HBP.Sync.Tests/ContractCodecTests.cs` |
| `Assets/Tests/EditMode/HBP.Sync.Tests/V2SchedulerTests.cs` | `Assets/Tests/EditMode/HBP.Sync.Tests/SchedulerTests.cs` |
| `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/V2ActivityProjectionTests.cs` | `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/ActivityProjectionTests.cs` |
| `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/V2OnlineMutationDriverTests.cs` | `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/OnlineMutationDriverTests.cs` |
| `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/V2PersistentTransportLoopbackTests.cs` | `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/PersistentTransportLoopbackTests.cs` |
| `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/V2SceneCheckpointMergeTests.cs` | `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/SceneCheckpointMergeTests.cs` |
| `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/V2SceneMutationBoundaryTests.cs` | `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/SceneMutationBoundaryTests.cs` |
| `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/V2TimelineClockEstimatorTests.cs` | `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/TimelineClockEstimatorTests.cs` |
| `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/V2TransportProtocolTests.cs` | `Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/TransportProtocolTests.cs` |
| `Assets/Scripts/HBP/Sync/V2CorrelationControl.cs` | `Assets/Scripts/HBP/Sync/CorrelationControl.cs` |
| `Assets/Scripts/HBP/Sync/V2MutationCodec.cs` | `Assets/Scripts/HBP/Sync/MutationCodec.cs` |
| `Assets/Scripts/HBP/Sync/V2MutationContract.cs` | `Assets/Scripts/HBP/Sync/MutationContract.cs` |
| `Assets/Scripts/HBP/Sync/V2RetentionProgress.cs` | `Assets/Scripts/HBP/Sync/RetentionProgress.cs` |
| `Assets/Scripts/HBP/Sync/V2SceneAction.cs` | `Assets/Scripts/HBP/Sync/SceneAction.cs` |
| `Assets/Scripts/HBP/Sync/V2Scheduler.cs` | `Assets/Scripts/HBP/Sync/Scheduler.cs` |
| `Assets/Scripts/HBP/Sync/V2SchedulerIndexes.cs` | `Assets/Scripts/HBP/Sync/SchedulerIndexes.cs` |
| `Assets/Scripts/HBP/Sync/V2SiteFilterControl.cs` | `Assets/Scripts/HBP/Sync/SiteFilterControl.cs` |
| `Assets/Scripts/HBP/Sync/V2SurfaceInflationControl.cs` | `Assets/Scripts/HBP/Sync/SurfaceInflationControl.cs` |
| `Assets/Scripts/HBP/Sync/V2T09MutationContract.cs` | `Assets/Scripts/HBP/Sync/T09MutationContract.cs` |
| `Assets/Scripts/HBP/Sync/V2T10MutationContract.cs` | `Assets/Scripts/HBP/Sync/T10MutationContract.cs` |
| `Assets/Scripts/HBP/Sync/V2T11MutationContract.cs` | `Assets/Scripts/HBP/Sync/T11MutationContract.cs` |
| `Assets/Scripts/HBP/Sync/V2T12MutationContract.cs` | `Assets/Scripts/HBP/Sync/T12MutationContract.cs` |
| `Assets/Scripts/HBP/Sync/V2T13MutationContract.cs` | `Assets/Scripts/HBP/Sync/T13MutationContract.cs` |
| `Assets/Scripts/HBP/UI/Quest/DesktopV2ReplicaSession.cs` | `Assets/Scripts/HBP/UI/Quest/DesktopReplicaSession.cs` |
| `Assets/Scripts/HBP/Transfer/Transport/V2PersistentTransport.cs` | `Assets/Scripts/HBP/Transfer/Transport/PersistentTransport.cs` |
| `Assets/Scripts/HBP/Transfer/Transport/V2ReplicaRoute.cs` | `Assets/Scripts/HBP/Transfer/Transport/ReplicaRoute.cs` |
| `Assets/Scripts/HBP/Transfer/Transport/V2TransportProtocol.cs` | `Assets/Scripts/HBP/Transfer/Transport/TransportProtocol.cs` |
| `Assets/Scripts/HBP/Sync/Scene/V2CorrelationJob.cs` | `Assets/Scripts/HBP/Sync/Scene/CorrelationJob.cs` |
| `Assets/Scripts/HBP/Sync/Scene/V2OnlineMutationDriver.cs` | `Assets/Scripts/HBP/Sync/Scene/OnlineMutationDriver.cs` |
| `Assets/Scripts/HBP/Sync/Scene/V2PublicationCheckpointBulkReceiver.cs` | `Assets/Scripts/HBP/Sync/Scene/PublicationCheckpointBulkReceiver.cs` |
| `Assets/Scripts/HBP/Sync/Scene/V2PublicationJournal.cs` | `Assets/Scripts/HBP/Sync/Scene/PublicationJournal.cs` |
| `Assets/Scripts/HBP/Sync/Scene/V2SceneCheckpointMerge.cs` | `Assets/Scripts/HBP/Sync/Scene/SceneCheckpointMerge.cs` |
| `Assets/Scripts/HBP/Sync/Scene/V2SceneMutationBoundary.cs` | `Assets/Scripts/HBP/Sync/Scene/SceneMutationBoundary.cs` |
| `Assets/Scripts/HBP/Sync/Scene/V2SceneOperationBulkReceiver.cs` | `Assets/Scripts/HBP/Sync/Scene/SceneOperationBulkReceiver.cs` |
| `Assets/Scripts/HBP/Sync/Scene/V2SceneReconciliationProtocol.cs` | `Assets/Scripts/HBP/Sync/Scene/SceneReconciliationProtocol.cs` |
| `Assets/Scripts/HBP/Sync/Scene/V2SceneReconciliationRecord.cs` | `Assets/Scripts/HBP/Sync/Scene/SceneReconciliationRecord.cs` |
| `Assets/Scripts/HBP/Sync/Scene/V2SiteFilterJob.cs` | `Assets/Scripts/HBP/Sync/Scene/SiteFilterJob.cs` |
| `Assets/Scripts/HBP/Sync/Scene/V2SurfaceInflationCoordinator.cs` | `Assets/Scripts/HBP/Sync/Scene/SurfaceInflationCoordinator.cs` |
| `Assets/Scripts/HBP/Sync/Scene/V2TimelineClockEstimator.cs` | `Assets/Scripts/HBP/Sync/Scene/TimelineClockEstimator.cs` |
| `Assets/Scripts/HBP/Quest/Runtime/QuestV2ReplicaSession.cs` | `Assets/Scripts/HBP/Quest/Runtime/QuestReplicaSession.cs` |

## Remaining qualification and limitations

- No new physical Quest USB/Wi-Fi run, latency observation, visual/scientific parity acceptance or APK installation is claimed by this code-cleanup report.
- T18 preserves the existing T16 semantic merge and T17 membership policy; the earlier global no-merge plan is superseded.
- Historical T00 measurement vocabulary/analyzer remains available for recorded evidence; removed capture/ACK machinery is not retained as a live synchronization path.
- No commit or push is performed. C# formatting is the final action after implementation, validation and review.

## Application-wide Task / UniTask audit

The follow-up audit covers all HiBoP C# sources, including Editor code, test helpers,
`Tools` and project-owned `Packages`; it is not restricted to changes after
`518b3f0d4b2f4db45b52df5fbb5f7160b6828c6f`. Before this migration, 46 application
files contained actual TPL `Task`/`TaskCompletionSource` usages; 33 still do.
`UniTaskCompletionSource.Task`, `AsyncLazy.Task` and the IBC information field
named `Task` are not TPL usages. No application Task usage was found in third-party
Assets, Tools or project-owned Packages.

### Migrations

Paths below are relative to `Assets/Scripts/HBP`.

| Area | Migrated operations |
| --- | --- |
| `Core/DLL/Surface.cs` | Inflation worker and real-time progress polling; native work is still drained after cancellation. |
| `Data/Module3D/Base3DScene.cs`, `Module3DMain.cs` | Anatomy worker and shared single-patient scene opening. The latter uses a manual UniTask completion source that supports multiple consumers. |
| `Transfer/Scene/DesktopSceneCapture.cs`, `PairingSnapshot.cs`, `NativePreparation.cs` | Unity capture APIs and background resource preparation. Tokens remain checked inside producers so cancellation cannot discard an owned result after creation. |
| `Quest/Runtime/QuestAnatomyView.cs` | Application, closure and retired-scene releases. Shared releases are memoized; every already-started release is drained before an error is propagated. |
| `Quest/Runtime/QuestConnectionPanel.cs`, `QuestNativeDiagnostic.cs`, `QuestAnatomySession.cs` | Connection UI lifecycle, native diagnostic delays and replica-grace expiry. Delays use real time rather than scaled Unity time. |
| `Sync/Scene/SessionAtlasPreparation.cs`, `SceneMutationBoundary.cs`, `SiteFilterJob.cs`, `SurfaceInflationCoordinator.cs` | Atlas preloads, checkpoint preparation, site-filter evaluation/yields and inflation execution/observation. Shared execution is memoized where several consumers need it. |
| `Sync/Scene/SessionPreferencesReceiver.cs`, `UI/Quest/QuestPairingDiagnostic.cs` | Local session cleanup and smoke-test start/close helpers; their network callback contracts remain Task. |
| `UI/Quest/DesktopSessionPreferences.cs`, `QuestManager.cs`, `QuestAdbProcess.cs`, `QuestUsbDiscovery.cs` | Atlas/preference UI commands, session opening and ADB execution. Transport lifetimes retain their Task contracts. |
| `UI/Tools/LoadingCircle/LoadingManager.cs`, `SurfaceInflationLoading.cs` | Removed UniTask-to-Task conversions used only for polling or racing a loading delay. The operation raced against the delay is memoized before the final await. |
| `UI/Module3D/Sites/Site Tools/ImportFromCSVSection.cs`, `Dev/Editor/Rendering/RenderingBaselineMenu.cs` | CSV preparation hook and rendering capture wrapper. |

Unity-facing UniTask APIs explicitly return to the main thread before cleanup or
error presentation, including failures raised on workers. PlayMode validation
identified and corrected the previous implicit SynchronizationContext assumption
at the Quest view boundary. `QuestPairingDiagnostic` also explicitly returns to
Unity before writing its Unity JSON report and quitting after ADB work.

### Task usages deliberately retained

Multi-consumer waiting alone does not require Task: manual UniTask completion
sources and AsyncLazy support it. The decisions below additionally account for
continuation scheduling, thread affinity, identity-based ownership, .NET contracts
and complete draining on failure. A conversion of these internal contracts is
technically possible, but would require a broader lifecycle change or repeated
Task/UniTask adapters rather than a useful direct replacement.

| Files / usages | Reason retained |
| --- | --- |
| `Core/Tools/SharedLoadingOperation.cs` | Ready/Validated barriers, independent consumer cancellation, captured SynchronizationContext and asynchronously scheduled continuations. |
| `Core/Tools/LoadingWorkScheduler.cs` | Worker admission and progress dispatch use `RunContinuationsAsynchronously`; inline continuation changes would alter worker threads and reentrance during grant/release. Its public execution API already uses UniTask. |
| `Sync/SyncTelemetry.cs`, `Transfer/Scene/SceneDelivery.cs` | Shared capture/delivery close barriers retain inputs until all borrowers/producers have stopped. |
| `Transfer/Transport/BlockContainer.cs`, `BlockDelivery.cs` | Blocking-queue worker pipelines and producer shutdown, independent of the Unity PlayerLoop. |
| `Transfer/Transport/PersistentTransport.cs` | Socket reader/writer/liveness lifetimes, repeated wait races and complete shutdown draining. Network liveness timers must not depend on Unity frames. |
| `Transfer/Transport/QuestPairing.cs`, `QuestDiscovery.cs` | .NET TLS/UDP operations, concurrent handlers, discovery races and callback contracts. |
| `Transfer/Transport/PairingPake.cs`, `PinnedTlsTransfer.cs`, `ReplicaRoute.cs`, `SceneFileTransfer.cs`, `SessionControlProtocol.cs`, `TransportProtocol.cs` | Pure .NET stream/crypto protocols and their existing Task callback contracts. No Unity operation is hidden behind these APIs. |
| `Sync/Scene/SceneReconciliationProtocol.cs`, `SessionPreferencesReceiver.cs` | Stream/session-control callback contracts connecting those protocols to application-side preparation. |
| `Quest/Runtime/QuestAnatomySession.cs` | Session-control deduplication, shared installations/closures, transport callbacks and main-thread dispatch barriers with asynchronous continuations. `Stream.ReadAsync`/`WriteAsync` overrides must return Task. |
| `Quest/Runtime/QuestAnatomyView.cs` | The preparation-completion TCS remains Task with asynchronously scheduled continuations: Clear/globals replacement must not resume inline inside ApplyAsync's final cleanup. Its application/release APIs use UniTask. |
| `Quest/Runtime/QuestReplicaSession.cs`, `UI/Quest/DesktopReplicaSession.cs` | Shared incoming/connection/job lifetimes, race winners subsequently awaited again, cancellation independent of job ownership and reconnect shutdown. |
| `UI/Quest/QuestManager.cs`, `QuestManager.SceneLifecycle.cs` | Shared publication/action/discovery lifetimes, identity checks against inventory snapshots and full shutdown/reconciliation draining. |
| `UI/Quest/DesktopSessionPreferences.cs` | Shared pending-save chain, open barrier and inventory lifetime. Atlas/UI commands now use UniTask. |
| `UI/Quest/QuestUsbDiscovery.cs` | Discovery provider result participates in QuestManager's Task-keyed Wi-Fi/USB race; individual ADB calls use UniTask. |
| `UI/Quest/QuestPairingDiagnostic.cs` | Smoke-test server lifetime and .NET transport callbacks. |
| `Sync/Scene/SiteFilterJob.cs`, `CorrelationJob.cs`, `SceneMutationBoundary.cs`, `UI/Module3D/Sites/SiteFilterUiRequestRunner.cs`, `Sites/Site Filters/SiteFiltersWindow.cs` | Router delegates expose the existing shared job owner's Task directly, including rejected requests. Evaluation itself now uses UniTask. |
| `Sync/Scene/SurfaceInflationCoordinator.cs`, `UI/Tools/SurfaceInflationLoading.cs` | Shared protocol-step TCS and remote completion notification/observation preserve deferred continuations. Execution and preparation use UniTask. |
| `Assets/Tests/**` | NUnit entry points remain `async Task` with direct awaits as required by AGENTS.md; Task test helpers and TPL transport fixtures retain their contracts. Test-only adapters exist where races or reflection need the exact Task boundary. |

The retained `using System.Threading.Tasks` in `Core/Data/DataManager.cs` supplies
`Parallel`/`ParallelOptions`; it is not an asynchronous Task usage. No assembly
references or package versions were changed and no compatibility aliases were
introduced.

### Migration validation

- Focused EditMode preparation/inflation/lifetime run: **24/24 passed** (job `1882a765169b4953a46510866a07ebba`).
- General EditMode CLI run: **1,067 passed, zero failed, one ignored**, 337.25 s, Unity exit code 0. The ignored test, `ActualDesktopFixture_PreservesEveryComponentAndFullSurface`, requires the external QUEST-006 Desktop HBNA export (`-questAnatomyFixture`). Evidence: `.test-results/unitask/editmode-cli.xml` and `.test-results/unitask/editmode-cli.log`.
- The general run logged the existing missing-GII fixture diagnostic, EditMode `MeshFilter.mesh` diagnostic and two intentionally malformed-JSON exceptions. No compilation error was found.
- MCP general-suite attempts were discarded: the first job was reported as initialization-failed although Unity continued running it. A second run then overlapped it; inspecting Unity's live TestRunner job holder confirmed two active jobs. Resource-retention failures and counters exceeding the selected test count made those results unreliable. Cancelling both runs was followed by an editor crash during scene restoration/Mono liveness traversal. The successful CLI run above started only after confirming that no Unity process remained and that its orphaned lock file was not held; no lock file was deleted.
- An earlier PlayMode run reported an intermittent `buffers.pack` file-lock failure in `M2_InflationWithDesktopOnlyCache_ConvergesThroughProductionSessions`; its isolated rerun completed without failure. An independent ownership review found no demonstrated early release in the migrated path. The final serial PlayMode result is recorded below.
- Final serial PlayMode CLI run: **16/16 passed**, zero skipped or failed, 92.36 s, Unity exit code 0. Coverage includes both complete-scene restoration cases, cold/cache inflation, opening an individual patient, Quest closure, five preferences-owner cases, two native-preparation cancellation cases and three delayed-loading cases. The file-lock failure did not recur. Evidence: `.test-results/unitask/playmode-cli.xml` and `.test-results/unitask/playmode-cli.log`. The two invalid-topology exceptions in the log belong to deliberate malformed-scene rejection checks; no main-thread cleanup exception remained.
- Assembly dependency gate: **passed**, 44 HBP assemblies and 162 direct edges. `git diff --check` passed before final formatting. No Android device/IL2CPP build was executed for this follow-up.

### Native editor crash investigation, 12 October 2026

The earlier description of an incident during MCP tests identifies the context,
not a proven root cause. The native failure must not be dismissed as an MCP-only
problem or treated as disproved by subsequent passing tests.

The preserved crash report is `Crash_2026-10-12_134308403` (approximately 15:43
Europe/Paris). Copies of its dump and editor log are retained under
`.test-results/unitask/crash-2026-10-12/`.

- The minidump and Windows debugger agree on **access violation `0xC0000005`**, a read at `0x100000000`, on the Unity main thread (18232). This is not a stack-overflow exception.
- The faulting instruction is `mono_add_process_object+0x38`, `mono-2.0-bdwgc.dll+0x26CC48` (`unity-liveness.c:264`). Mono reads an invalid object-vtable pointer. The object argument is `0x19E1809C240`; the preceding array argument is `0x19F9AEA8000`.
- The call chain is `RestoreSceneManagerSetup → UnloadUnusedAssetsOperation → GarbageCollectSharedAssets → mono_unity_liveness_calculation_from_statics → mono_traverse_array/objects → mono_add_process_object`. No HiBoP native-plugin call appears in this faulting chain. This does **not** exclude an earlier native write corrupting memory.
- The object and array heap pages are absent from this minidump. Their types, owning static root and the writer of the invalid value cannot be recovered from it. Repeated traversal frames alone do not establish excessive graph depth.

Two concrete orchestration hazards are visible in the installed source:

1. Unity Test Framework `TaskList.cs` sends `RunFinished` before `RestoreSceneSetupTask`; MCP `TestRunnerService.RunFinished` resolves its completion source at that callback. An MCP-completed job can therefore still have Unity cleanup pending.
2. Unity Test Framework `TestJobRunner.CancelRun` clears the runner enumerator immediately. It does not await the asynchronous test's Task before cancellation cleanup restores scenes. Running continuations or workers can consequently overlap restoration. Neither behavior was introduced by this Task/UniTask migration.

The duplicate runs were observed directly before cancellation, so this is a
credible triggering context. It is not proof of how the invalid object header
originated. Reviewing the migration found no demonstrated new ABI, pointer or
early-native-release defect. UniTask pools and diagnostic snippet assemblies
change the managed graph, but no faulty object could be attributed to them.

[Unity issue UUM-140954](https://issuetracker.unity.com/issues/22564/crash-on-monoaddprocessobject-when-exiting-play-mode-on-a-specific-project)
is open and has a closely matching Mono traversal stack, reproduced in several
Unity families including 6000.5. This is a candidate engine issue, not evidence
that HiBoP has the same cause.

After the user restarted and reconnected the editor, inspection found zero test
jobs, zero retained resource keys, no console errors and a clean HiBoP scene. One
serial MCP run of `SurfaceInflationPhase4Tests` and `PairingLifetimeTests` then
passed **13/13** in 6.80 s (job `ad53e925b5654e3eb8417e749e63cdf1`). A subsequent
inspection confirmed **zero remaining Unity TestRunner jobs** and the restored
`Assets/_Scenes/HiBoP.unity` scene. Only the intentional missing-GII fixture
diagnostic remained. No crash recurred. This is a bounded check, not exclusion of
an intermittent corruption defect.

For further validation, wait for the actual TestRunner job holder to become empty
before starting another run, even after MCP reports completion. If an MCP
initialization timeout occurs, check Unity's actual jobs before retrying. Avoid
using cancellation of active async tests as a cleanup mechanism. If the crash
recurs naturally, a dump including the managed heap is the missing evidence for
identifying the offending object; no speculative pooling or native-lifetime
workaround has been introduced.
