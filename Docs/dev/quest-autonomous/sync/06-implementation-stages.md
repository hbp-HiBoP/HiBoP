# Implementation stages and reviewable deliverables

These are engineering stages for the complete synchronization program. The first user-facing gate is full Desktop-to-Quest coverage (S0–S3 plus the relevant S6 checks). The next user-facing gate exposes Quest editing and offline reconciliation (S4–S5 plus the remaining S6 checks). Both use one state and protocol architecture; a cut-only demonstration meets neither gate. Each stage leaves a reviewable artifact and validation result.

## S0 — Operation and dependency inventory

Walk every control and shortcut available after opening a visualization, from the UI callback to live `Base3DScene`/column mutation and rendering consequence. Record modality, target identity, shared value, local-only presentation, resource dependency, invalidation path and conflict atomic group. Include operations triggered by automatic behavior and continuous playback, not only explicit clicks. Review the inventory against `VisualizationConfiguration`, every column configuration, live state and `ScenePayload`/pairing dependencies.

**Deliverable:** `operation-matrix.md` with one row per operation family and concrete code anchors; a checklist of fields to add to state schema version 1. Unknown or unsupported rows block the release rather than becoming undocumented exceptions.

## S1 — State contract and pure merge engine

Define stable entity IDs, canonical value schema, versions, bounds, resource references, deterministic comparison, touched atomic groups and three-way merge rules. Specify presentation exclusions and scientific coordinates. Build unit tests for independent changes, identical assignments, same-group conflict, delete-versus-edit, tombstones, stale base and schema rejection. The merge engine operates on detached values without Unity objects.

**Deliverable:** versioned schema and codec specification, pure implementation and tests, plus an updated operation matrix mapping every row to fields/groups.

## S2 — Capture and apply the whole operation inventory

Add live capture from `Base3DScene`, column navigation copies, sites, cuts, ROI and modality managers. Add the in-place Quest applier using common scene operations and batched invalidation. Introduce stable maps for cuts/ROI/spheres and resource barriers. Before networking, drive both adapters in one process with two scenes and compare canonical values and scientific output after **every** operation-matrix row. Preserve Quest wrapper transforms.

**Deliverable:** full capture/apply tests, no unhandled matrix row, and a cut create/move/delete visual demonstration as one representative test.

## S3 — Active session, duplex transport and Desktop-to-Quest sync

Bind the successfully delivered scene, start an epoch, add persistent authenticated control transport, accepted revisions, bounded queues, resource readiness, snapshots for resumption, and separate received/applied/visible acknowledgements. Gate all work on an active sent-scene session. Test disconnection and retry without replacing the Quest presentation.

**Deliverable:** Desktop edits to all inventory rows reach the Quest; inactive Desktop path has no sync adapter, subscriptions or per-frame work.

## S4 — Quest proposals and immediate local editing

Expose a test driver for every scientific operation before Quest has its final UI. A driver may invoke shared scene methods on Quest, then send state assignments through the same protocol the eventual controls will use. Add optimistic local application, edit IDs, suppression of echo loops and rebase of pending edits. Then add the requested Quest controls, starting with a cut, without changing the state protocol.

**Deliverable:** each operation can originate from either adapter in automated/device tests; Quest input renders locally before a network acknowledgement.

## S5 — Offline persistence, reconciliation and conflict UX

Pin prepared resources needed offline, checkpoint the common baseline, persist pending final edits, reconcile at reconnect, merge disjoint changes, retain conflicts and expose a choice to the user. Exercise same-field cut conflict, delete-versus-edit, resource change and repeated outage. Resolve a conflict into a new accepted revision and verify both visible states converge. Distinguish socket loss, explicit close, scene replacement and process restart.

**Deliverable:** physical Quest outage test with local edits, reconnection and conflict resolution, plus documented storage/restart behavior.

## S6 — Rate control, parity and release gate

Run continuous gestures at device refresh rates, measure bytes/queues/CPU/GC/latency and adjust preview cadence without losing final state. Verify scientific parity for the full operation matrix and all supported modalities, not only anatomy. Prove obsolete native/render jobs cannot publish after a newer state. Check Desktop-only behavior with the adapter absent. Fix every uncovered operation before release.

**Deliverable:** `validation-report.md` with fixture/build identities, device results, known limits and a completed operation matrix. Mark the Desktop-to-Quest and Quest-editing release gates separately using `07-verification.md`.

## Sequencing notes

The pure state and merge engine should be reviewed before transport complexity is added. The architecture must support Quest-origin edits and offline branches from S1, even though Desktop-to-Quest wiring is integrated first. Resource identity and scene lifecycle decisions must be resolved before claiming recovery after an app restart. Avoid modifying existing project serialization solely to carry transient session fields; migration should be explicit only when a field is intended to persist in the Desktop project.
