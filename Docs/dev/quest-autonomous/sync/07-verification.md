# Verification and release gates

## Operation matrix as the acceptance contract

Create `operation-matrix.md` in S0 with these columns:

`operation | UI/code entry | scientific target | canonical field/group | capture | apply | resource/invalidation dependency | Desktop -> Quest test | Quest -> Desktop test | offline/conflict case | presentation effect`.

The first Desktop-to-Quest release does not pass with any **Desktop** operation of the open visualization marked “later,” “manual resend,” or “Quest cannot reproduce.” A source-database operation may be marked out of scope with a reason. Presentation-only operations must be explicitly marked local and tested for non-propagation. Quest-to-Desktop and offline columns are completed by test drivers at the architecture gate and by real controls at the later Quest-editing gate.

## Required scenario families

1. **Capture completeness:** mutate each operation once on Desktop, capture state before/after, assert the expected canonical group changed and unrelated presentation groups did not. Repeat for Quest-origin test-driver operations. Cover all column modalities.
2. **In-place application:** apply successive states to one Quest scene; compare canonical state and scientific outputs with an independently initialized target state. Preserve Quest column positions, rotations and scales and Desktop camera state.
3. **Cut baseline:** create, drag, flip, change orientation and remove several cuts including a non-last cut; retain stable identities despite runtime reindexing. Check final cut geometry, textures and site-dependent automatic cuts.
4. **Other mutable inputs:** active ROI and spheres; filter inclusion results; selected site/column; moved sites; all modality parameters, resource selections, erasure masks and atlas/fMRI overlays; timeline seek/play/loop/step. Measure output parity rather than only JSON equality.
5. **Online order:** duplicates, lost ACK, stale revision, out-of-order previews, old render completion, slow bulk resource transfer and scene replacement. No stale result becomes visible and no edit executes twice.
6. **Offline:** disconnect for at least one minute, edit Quest and Desktop in disjoint groups, reconnect and merge; edit the same cut geometry on both, verify a visible conflict and user choice; delete on one side while editing on the other, verify tombstone behavior. Repeat reconnection and interruption during reconciliation.
7. **Durability:** persist final Quest edits before the queued status, restart the process if supported by the staged persistence contract, validate recovered hashes/resources and reconcile. An incomplete/corrupt journal must fail visibly without corrupting the accepted state.
8. **Performance:** idle scene sends no state samples; continuous drag bounds preview queue and delivers final state; record observed local-feedback, remote-visible latency, bytes/s, CPU, GC and scientific calculation timing at device refresh rates. Do not claim a measured 90-Hz rate from a unit test.
9. **Inactive Desktop:** no sent scene means no sync adapter, capture timer, serialization or subscriptions, including when a Quest was merely paired. A simple profiler smoke check catches accidental work. The old Desktop behavior and saved project format remain unchanged.

## Acceptance semantics

“Same state” means identical canonical inputs and agreed resource hashes. Numerical/render parity is evaluated with operation-specific tolerances and fixture/build provenance; previous density/iEEG tolerances are not automatically universal. “Live” means local feedback without network wait and eventual remote visibility while connected; continuous scientific recalculation can complete below the input sampling rate but must use the newest valid state and display its actual visible revision. “Synchronized” requires no unresolved local branch or conflict and both peers acknowledging the same accepted revision.

## Evidence and honest limits

Record Unity version, Desktop/Quest build commits, native library versions, network path, headset refresh rate, fixtures and observed results. Keep passing unit/integration tests distinct from physical device observations. If an operation cannot meet parity or offline correctness, state that blocker explicitly; do not relabel it a presentation-only exception. No claim of a zero-overhead inactive path or 90-Hz support is made by these planning documents alone.
