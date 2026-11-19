# Desktop–Quest synchronization

The runtime exchanges typed domain operations between a Desktop and a standalone Quest. Desktop sequences accepted operations; Quest immediately applies its own gestures, then receives their confirmation or correction. Both devices execute the same scientific model. Initial resource delivery and ongoing synchronization have distinct owners.

State checked by reading the checkout on November 2, 2026. The implementation is present; [qualification limits](synchronization-validation.md) distinguish tests, historical observations and hardware not requalified.

- [Operations, invalidation and coordinated computation](synchronized-operations.md).
- [Protocol, scheduling and retention](wire-protocol.md).
- [Verification, measurements and limits](synchronization-validation.md).
- [Shared scenes and columns](../shared-scientific-scenes.md), [full delivery](../transfer.md).

## Owners and current path

| Owner | Responsibility |
| --- | --- |
| [SceneMutationBoundary](../../../../Assets/Scripts/HBP/Sync/Scene/SceneMutationBoundary.cs) | Observes targeted operations, validates/applies typed families, exports and restores their checkpoints. |
| [DesktopReplicaSession](../../../../Assets/Scripts/HBP/UI/Quest/DesktopReplicaSession.cs) / [QuestReplicaSession](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestReplicaSession.cs) | Sequencing/proposals, reception, application confirmations and job coordination for an incarnation. |
| [OnlineMutationDriver](../../../../Assets/Scripts/HBP/Sync/Scene/OnlineMutationDriver.cs) | Desktop authority, optimistic Quest state, corrections and rebasing of subsequent changes. |
| [PersistentTransport](../../../../Assets/Scripts/HBP/Transfer/Transport/PersistentTransport.cs) | Reading, writing, resumption and liveness of the authenticated stream; no Unity manipulation from network loops. |
| [QuestManager.SceneLifecycle](../../../../Assets/Scripts/HBP/UI/Quest/QuestManager.SceneLifecycle.cs) / [QuestAnatomySession](../../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomySession.cs) | Inventory, publication, multi-scene routing, closing, selection and recovery. |

The domain boundary produces a small detached payload. Remote application calls the same handlers and retains their invalidations, with a context suppressing re-emission. An equivalent echo confirms the optimistic proposal; a different value follows authoritative correction. An ordinary change does not capture the entire scene or await stable rendering or a visible acknowledgment. The key registry detects actual concurrent conflicts: Desktop does not systematically reject an old Quest proposal targeting an independent key.

DTOs/codecs and the scheduler are in `HBP.Sync.Runtime`, scientific composition in `HBP.Sync.Scene`. Core events remain neutral. Dependency constraints and development rules are owned by [AGENTS](../../../../AGENTS.md) and [agent knowledge](../../../agent-knowledge/synchronization.md); this directory explains their implementation.

## Identities and publication

`sessionId` identifies the session, `sceneId` the logical visualization, `incarnationId` its concrete opening, `resourceManifestId` the prepared resources. Columns, sites, cuts, ROIs and spheres have stable identities; a list index or display name does not replace that identity. Closing/reopening or replacement creates a new incarnation. Late messages and computations from the old one cannot apply to it.

[PublicationJournal](../../../../Assets/Scripts/HBP/Sync/Scene/PublicationJournal.cs) starts at consistent capture. During delivery, representable mutations are retained, then replayed after Quest publication. Overflow requires a current checkpoint; an unrepresentable resource/topology mutation restarts delivery. The scene is not declared live until replay/checkpoint and acknowledgment of its initial barrier are complete. Heartbeat must not replace the owner of a publication still in progress; a stale inventory response is reevaluated against its identity and task.

The checkpoint contains shared typed states, masks, available canonical results and scientific Original/Left/Right placement. It excludes source files, GameObjects and local poses. Capture is allowed at publication/reconnection boundaries, never as an ordinary change detector. New user resources require a new full delivery; installed atlases follow the exception below.

## Interruption and reconciliation

During the **500 ms** grace period, the view remains usable and reliable frames are retried with the same identities/bytes. After expiry: disconnection warning, retry-history abandonment, coordinated-job cancellation and local continuation. There is no persistent recovery after process shutdown/crash.

[SceneReconciliationProtocol](../../../../Assets/Scripts/HBP/Sync/Scene/SceneReconciliationProtocol.cs) first restores Desktop preferences, then reconciles common incarnations with their manifest. [SceneCheckpointMerge](../../../../Assets/Scripts/HBP/Sync/Scene/SceneCheckpointMerge.cs) compares current states to the last retained common base:

- identical changes: no question; independent attributes/entities: combination;
- conflict on the same field, deletion versus dependent modification/selection, resource versus dependent parameters: Desktop/Quest choice for the coherent group;
- common base unavailable: whole-scene choice.

Natural timeline progression is not an explicit edit. Poses remain local. The conflict dialog requires Desktop or Quest, without a Cancel button; transport/application cancellation remains possible.

Interactions are guarded during choice, preparation and application. The entire candidate is validated and its local representations prepared before commit. A pre-commit failure changes nothing; a failure after commit begins retains the exact candidate and guard, with desynchronized state and explicit recovery. Recovery does not become a new send overwriting offline edits. Success establishes a new common base. There is no transactional rollback across multiple scenes.

## Inventory, visibility and patient scene

The Desktop `AutomaticallySynchronizeScenes` setting, enabled by default and shared across headsets, governs only scene membership:

| Setting | Behavior |
| --- | --- |
| Enabled | Desktop inventory is authoritative on connection/reconnection: delivery of missing scenes, closing extra Quest copies, connected closes in both directions. A Quest close is handled before comparing inventories. |
| Disabled | Common scenes continue synchronizing mutations, selection and visibility; subsequent closes remain local. A Quest copy whose Desktop source disappears becomes orphaned, never attached to a new opening of the same source. |
| Re-enabled | Desktop inventory is reapplied, including closing orphans. |

A close carries operation/bulk watermarks to finish or abandon and awaits users of that incarnation. It does not block other scenes. A hidden scene retains resources and synchronization, but suspends rendering/interactions and ends gestures; showing it again does not resume an old capture.

An intentional interaction with a brain/site/cut selects its scene/column context; hover does not. The gesture retains its initial target even if selection changes. Pose, rotation, scale and Single/Split layout remain local. A new scene is placed in front of the headset before becoming visible, without moving previous scenes.

From a site in a shared multi-patient scene, Quest can request **online** that Desktop open the patient. The shared path reuses an existing child opening for `(source opening, patient)`, shows and selects it; otherwise it creates and delivers it to both devices, even with automatic membership disabled. Column definitions and anatomical preferences are retained, but source-topology erasure masks and transient MRI preview selection are discarded. Parent and child then evolve independently; closing the parent does not close its children.

## Session preferences and atlases

[DesktopSessionPreferences](../../../../Assets/Scripts/HBP/UI/Quest/DesktopSessionPreferences.cs) and [SessionPreferencesReceiver](../../../../Assets/Scripts/HBP/Sync/Scene/SessionPreferencesReceiver.cs) synchronize **all** `UserPreferences`, even without a scene. Tag/protocol/alias definitions are also updated after pairing. The snapshot is validated before effects, applied on Unity while preserving the Quest object/listeners, then acknowledged; no implicit save of the Quest file. Connection generation, operation UUID and monotonic revision prevent stale applications. An operation abandoned on disconnection is not replayed: reconnection installs current Desktop state.

Preferences requiring reload capture views on Unity, close/await old incarnations on both sides, apply settings, then reload and deliver new incarnations. Independent patient openings are preserved. An absent connection, ongoing transfer/reconciliation or a non-reloadable local/orphan Quest scene requires rejection before application/saving. Errors remain per scene with retry; a failed capture also releases the guard.

Installed atlases/MNI are local references: logical identity and native availability, **without global hash comparison of installed contents**. The user ensures their consistency. This exception removes neither provenance during FMRI/Volume loading nor integrity of transferred user data. No atlas file travels through synchronization; the Android manifest serves installation, not runtime authorization.

Loads are sequenced/shared per atlas, with publication after full native preparation; Load on an already ready atlas does not rescan disk. Unload then Load accounts for changed files. Partial successes remain available. Unload reserves both sides and waits for the absence of users; the remote reservation expires after 30 s. Retries are bounded to three attempts, cancellable 500/1000 ms pauses, then one Desktop error. Disconnection/cancellation abandons retries. Buttons act immediately and the window's Cancel does not undo them; auto-load flags apply at startup/new session, not on every save.
