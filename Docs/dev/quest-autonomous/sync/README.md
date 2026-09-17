# Desktop–Quest visualization synchronization

Status: architecture and implementation plan, 2026-09-17. No production implementation is claimed by these documents.

This plan applies after the user opens a visualization on Desktop and sends it to Quest. Both applications then use the same scientific scene implementation and may eventually edit that scene. The initial delivery remains the existing prepared-scene transfer. Synchronization concerns the open visualization, not edits to the source project, patients, protocols, or imports.

## Product decisions

1. Desktop coordinates and persists the accepted shared state. Quest renders and edits its local copy immediately, including while disconnected. Desktop authority does **not** put the network in the Quest interaction-to-photon path.
2. After reconnection, independent edits merge. Concurrent edits to the same atomic property group, and delete-versus-edit cases, remain visible as conflicts until the user chooses a result. Neither device silently discards the Quest branch.
3. Continuous gestures may generate 60/90 input samples per second. The network scheduler must bound work and favor the newest preview; every completed gesture gets a reliable final state. No claim that every rendered frame requires a packet.
4. A Desktop installation that has never sent a scene to Quest has no new per-frame capture, comparison, serialization, allocation, or scientific computation. Pairing alone does not activate replication. Once a scene has been sent, its session remains active through temporary disconnection so both sides can record edits.
5. Scientific inputs and intended state cross the wire. Each side runs the common scene operations. A filtered site's inclusion is an input/result of filtering; rendering instructions or meshes are transferred only when a particular derived result cannot be reproduced locally to the accepted parity.
6. Presentation stays local: Desktop camera/views and Quest head pose, column pose/scale, recentering and controller tracking do not modify shared scientific coordinates.
7. The first user-facing synchronization release covers **every Desktop manipulation of an already open visualization** on Quest. Quest-origin controls and offline editing are the next user-facing capability, designed into the same state/transport model and exercised through test drivers before those controls ship. A cut-plane demonstration is not permission to ship a cut-only Desktop-to-Quest feature.

## Read in order

1. [Requirements and scope](01-requirements-and-boundaries.md)
2. [Shared state contract](02-state-contract.md)
3. [Live replication, offline editing, and conflicts](03-replication-and-offline.md)
4. [Transport and performance](04-transport-and-performance.md)
5. [Integration into the current code](05-code-integration.md)
6. [Implementation stages](06-implementation-stages.md)
7. [Verification and release gate](07-verification.md)

S0 inventory: [open-visualization operation matrix and schema checklist](operation-matrix.md). Its D/Q/O checks are planned validation cases, not completed tests.

S1 contract: [canonical schema, codec and merge rules](08-s1-state-codec.md).

These documents supersede the future-sync assumptions in `../05-state-command-and-sync-model.md` for this new work. They do not alter the completed QUEST-001–031 task records or retrospectively claim those tasks tested synchronization. The historical `feature/xr` contracts are references for revision and idempotence invariants, not a codebase or command catalogue to merge wholesale.

## Current baseline

- `DesktopSceneCapture.CaptureForQuestAsync` currently creates a fresh session ID and revision 1 on every send; `ScenePayload.Revision` is not a live stream cursor.
- `QuestAnatomySession` publishes complete deliveries by transfer ID and has a bounded delivery history. It does not order incremental scene updates.
- `QuestAnatomyView.ApplyAsync` rebuilds the scene and column wrappers. Using it on every change would reset Quest presentation.
- `Base3DScene.CaptureConfiguration` reads live scientific values. Stored configurations alone are not the entire live state.

The detailed source anchors and consequences are in [the integration audit](05-code-integration.md). All design choices below require a real operation inventory and device validation before they can be called complete.
