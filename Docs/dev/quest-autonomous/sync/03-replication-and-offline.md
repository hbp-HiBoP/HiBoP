# Live replication, offline editing, and conflicts

## Authority and immediate feedback

Desktop assigns accepted common revisions. Both applications apply their own user input locally immediately. On Quest, a manipulation updates local common scene state and its interaction preview on the input frame; it does not wait for a Desktop response. If native cut or projection calculation cannot finish within that frame, show the live handle/plane preview while retaining the last coherent scientific result until a new one is ready. Mark the new state **provisional** until Desktop accepts it. This preserves interaction responsiveness without pretending the network or scientific computation has zero latency.

The state model has one accepted baseline `B`, zero or more pending Quest edits `Q`, and a visible state produced by replaying `Q` over `B`. Desktop has its accepted state `D`; when connected, Quest proposals are validated against `D`, accepted or reconciled, and echoed back with an increasing common revision. Echoes acknowledge edit IDs and advance the Quest baseline; they must not snap an ongoing Quest gesture back to an older value. All visible local application uses the same scientific setters/operations as Desktop, not a second Quest implementation.

```text
Quest input -> local scientific apply -> next Quest render
            -> queue state proposal -> Desktop validates/merges
            -> common revision -> Quest acknowledges/rebases pending branch
```

The wire proposal is a **state assignment**, for example `{cutId, geometry: complete desired cut definition}`, with edit ID, local sequence, base revision and gesture ID. It is not an enum of every UI action. An operation such as create/delete still requires explicit entity membership changes and tombstones; a delta is always derived from the same complete state contract.

## Session state machine

`Unsent -> PreparingInitial -> Live -> DisconnectedEditable -> Reconciling -> Live`.
An explicit close or replacement enters `Closed`; network loss does not. Pairing may remain active without an open visualization, but that is `Unsent` and does not activate capture. On restart, a persisted editable session may enter `OfflineRecovered` after its resources and journal are verified; this needs its own validation, not an assumption from the current temporary archive.

The connection state, accepted revision, local edit durability and visible/rendered revision are separate indicators. Never show “in sync” merely because the TLS socket is connected.

## Online ordering

Each epoch has a Desktop common revision and per-device monotonic proposal sequence. Each edit carries a stable ID reused on retry. Desktop deduplicates `(epoch, device, editId)`, validates schema and target IDs, and serializes accepted state assignments. The response includes the accepted revision and normalized values. A stale proposal can be rebased automatically only when its touched atomic groups are unchanged since its base. Otherwise it becomes a conflict. Retries never execute a second time. Old render jobs and packets cannot supersede a newer accepted or locally visible state.

For a continuously dragged cut, individual samples may be accepted at up to the negotiated rate. The outgoing queue retains only the newest unsent preview for that gesture. The final pose is a separate reliable terminal proposal and must survive reconnect. Create, delete, topology switches and other dependency changes are ordered barriers: do not coalesce them away or send dependent values before their resources and IDs exist.

## Offline branch and recovery

When the link drops, Quest retains the last accepted baseline, all required resources, IDs, pending edits and local sequence. Local operations continue and are applied immediately. Record their semantic changes, not controller motion or 90-Hz presentation poses. Coalesce consecutive assignments to the same group within a gesture, while preserving creation/deletion boundaries and the final value. Queue storage has explicit size limits and an actionable full-storage error; never silently evict a pending edit.

Persist an accepted checkpoint and session/manifest identity on both devices, plus Quest tombstones and pending final edits, using atomic replace or append/commit records. A visible intermediate drag sample may be newer than its durable checkpoint; the UI must not claim crash durability until the final edit is committed. The initial prepared resources needed for offline use must be pinned in a session store rather than relying on temporary `SceneArchive` cleanup. Validate hashes and compatibility before recovering after a process restart. If Desktop loses its accepted baseline, do not guess a three-way merge base from Quest's claims; recover the checkpoint or stop with a repairable session error.

Desktop need not capture every frame during an outage merely because the old session exists. Its current scene continues to work normally. On reconnection, capture the live Desktop state and compare it with the retained baseline. A checkpoint before explicit Desktop scene closure is necessary if a closed scene is expected to reconcile later; otherwise closing/replacing the scene must explicitly end the epoch and tell the user pending Quest edits need export/resolution. This lifecycle decision is a stage gate.

## Three-way reconciliation

After reconnect, obtain `B` (last mutually accepted baseline), `D` (Desktop current state) and `Q` (Quest current branch plus pending edit provenance). Compare by stable entity ID and **atomic property group**, not by entire scene and not by wall-clock timestamps.

| B -> D | B -> Q | Result |
| --- | --- | --- |
| unchanged | changed | Apply Quest change to Desktop and publish common revision. |
| changed | unchanged | Apply Desktop change to Quest after rebasing its remaining pending branch. |
| same final value | same final value | Accept once; deduplicate. |
| different, disjoint groups | different, disjoint groups | Merge both after validation of dependencies. |
| different values in one atomic group | different values in that group | Conflict; retain both candidates and request a choice. |
| delete entity | modify same entity | Conflict; keep a tombstone and the edited candidate, with no automatic resurrection. |

A cut's orientation, normal, flip and position are one coherent **geometry group**; mixing half of Desktop's cut with half of Quest's is invalid. Other groups should be as small as their invariants permit: color can merge independently from a cut's geometry, but a selected resource and its topology-dependent mask must be resolved together. ROI membership, selected ROI and sphere edits require referential checks. The inventory must define groups for every field.

During a conflict, Desktop retains its currently accepted state; Quest retains the locally visible pending alternative and labels it unsynchronized. The conflict record stores baseline, Desktop value and Quest value plus affected IDs. A conflict UI should be available on both devices after reconnect so the user can select Desktop, Quest, or an explicitly edited value without having to leave the headset. The selection becomes a new accepted revision. The UI must not silently discard a Quest branch, and Quest must not claim convergence while it still displays a conflicting alternative. A conflict in one group need not block unrelated groups. If a parent entity is deleted, dependent edits are held together until that conflict is resolved.

Conflict records and pending edits must survive another disconnection. “Keep both” means retaining both values for a later decision, not duplicating a scientific entity whose identity would then be ambiguous. A separate duplicate/copy operation may be offered by the UI if meaningful.

## Example: the same cut moves while disconnected

At common revision 42, cut `c7` has geometry `G0`. Quest loses network and locally moves it to `GQ`, showing `GQ` immediately. Desktop moves `c7` to `GD`. Reconnection compares `(G0, GD, GQ)` and finds a geometry conflict. Desktop continues showing `GD`; Quest continues showing `GQ` with pending status. Choosing `GQ` publishes revision 43 with `GQ`, which Desktop applies; choosing `GD` acknowledges and clears Quest's pending edit after Quest applies `GD`. In neither case does reconnect itself overwrite a user's work.
