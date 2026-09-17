# Requirements and boundaries

## User journey

1. Open a visualization on Desktop; pair and send it to Quest through the current workflow.
2. Quest validates and publishes the prepared scene. The applications establish one synchronization epoch for that visualization and agree on a common scientific revision.
3. A Desktop edit updates Desktop immediately and then Quest. A future Quest edit updates Quest immediately and then Desktop. Each side keeps its own presentation transforms.
4. If the connection fails, the existing Quest scene remains usable. Quest edits continue locally. Desktop can continue editing the sent visualization. Reconnection reconciles the two branches.
5. Explicitly sending a different visualization starts a new epoch. Closing or replacing a session is distinct from losing the network.

## Release boundaries

The first user-facing release must mirror all Desktop manipulations available **after the visualization is open** that can affect the scientific scene or visible content: cuts; ROI and active ROI; site selection, filtering, highlighting, blacklist, colors, labels and moved positions; column selection and modality-specific settings; selected resources and anatomical representation; atlas/fMRI overlays; thresholds, gain, opacity and color; surface erasure; timelines; and other live controls found by the operation inventory. This list is a starting classification, not an exhaustive acceptance list. The inventory in `06-implementation-stages.md` is the release contract.

Quest-origin editing and offline Quest edits are the next user-facing capability. The state schema, identities, protocol and merge rules must be designed for them from the start; a Quest-side test driver should exercise both directions before adding full Quest controls. Once Quest editing is exposed, every control offered there must work locally while disconnected and reconcile afterward. This does not imply that the first release must expose every Desktop control as a Quest UI control.

Source-project/database edits, data import/export, patient or protocol authoring, and synchronization of several unrelated visualizations are outside this session. Existing scene data may still depend on definitions captured at pairing; changing those source definitions is not implicitly part of live visualization sync.

## Observable invariants

| ID | Invariant |
| --- | --- |
| R1 | Local interaction feedback never waits for a round trip. Quest can show and calculate from a provisional local state. |
| R2 | Every accepted common revision means the same canonical scientific inputs on both devices; rendering can lag that revision but must report it distinctly. |
| R3 | Reapplying a revision or an edit ID has no second effect. Older work cannot become visible after newer work. |
| R4 | A temporary disconnect never destroys the received scene; once Quest editing is exposed, it also never discards pending local edits. |
| R5 | Conflict resolution preserves both alternatives until a user decision; equal and disjoint changes merge without a prompt. |
| R6 | Quest presentation survives live updates, reconnection and resource changes for IDs that still exist. It never changes Desktop scientific coordinates. |
| R7 | Without an active sent-scene session, synchronization adds no per-frame work to Desktop. |
| R8 | A user-visible error identifies a missing resource, incompatible schema or unresolved conflict; the application never reports synchronized while branches differ. |

## Three distinct states

- **Accepted common state:** the most recent revision assigned by Desktop and confirmed by both sides. Desktop coordinates this history.
- **Local pending branch:** changes made after that revision which have not yet been accepted, including all offline Quest edits. The branch has its own monotonic local sequence and stable edit IDs; it is never confused with a common revision.
- **Visible state:** the scientific result each device currently renders, plus any clearly marked local interaction preview. Quest can immediately show its pending manipulation, while expensive cut/projection output may still show the last completed state. This is expected to differ temporarily from Desktop during computation, latency, disconnection, or conflict. Status and diagnostics must expose the distinction.

Desktop is the coordinator of common revisions, not the sole place allowed to run an operation. Quest applies the same shared operation locally first. This distinction is essential for instant and offline interaction.

## Presentation boundary

The scientific frame uses the coordinates expected by the common `Base3DScene` and native calculations. A cut normal, ROI sphere, site position or mesh mask is expressed in that frame. The local Quest wrapper applies position/rotation/scale to the entire scientific frame; its transform is stored separately by local column ID. A Desktop camera move, Quest grab, recenter or head move changes presentation only. Selection is shared **when it affects scientific output**, for example an automatic cut around the selected site; UI focus and hover are local.

## Explicit limitations requiring qualification

The same source code and native library on both devices make local reproduction plausible, but do not prove identical inputs, numerical output, shader behavior, resource versions or timing. The qualification matrix must establish parity per operation and modality. If an operation cannot be reproduced on Quest, the release must either make that common path work or explicitly change the product requirement; quietly substituting an approximate visual result does not meet R2.

Offline continuity during a live Quest process is mandatory. Durable recovery of edits after process death is included in the target design: record the edit and its base identity before claiming it is safely queued, and retain the prepared resources while the pending branch depends on them. Exact storage format, size policy and Android restart behavior are implementation decisions to verify in the persistence stage.
