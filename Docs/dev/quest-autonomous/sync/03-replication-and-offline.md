# Authority, disconnection and reconciliation

## Connected authority

Desktop is the online sequencer, not a render server. It assigns `canonicalSequence` to accepted operations. Quest applies its own input immediately, sends a proposal with a stable `operationId`, and treats the Desktop echo as acknowledgement rather than applying the same operation twice.

With one alternating user, genuine conflicts should be exceptional. Desktop keeps a `lastAcceptedCanonicalSequence` by touched operation key/group for the lifetime of an incarnation. A Quest proposal declares the last canonical sequence it observed; it is accepted when no touched key has a newer Desktop assignment. If a touched key is newer, Desktop wins and returns its authoritative assignment. The map is bounded by schema/entity limits, not time; deleted-entity tombstone watermarks remain until incarnation close so a stale proposal cannot resurrect them. Automatic producers such as timeline playback follow their dedicated clock rules rather than pretending to be simultaneous user edits.

## Connection state machine

```text
Unsent
  -> InitialPublishing
  -> Connected
  -> GracePeriod (500 ms)
  -> OfflineLocal
  -> Reconciling
  -> Connected

Connected/GracePeriod/OfflineLocal -> Closed for an explicit close
```

`GracePeriod` is silent: the UI still appears connected, outgoing operations remain in memory and the writer retries event-driven. If the same session returns within 500 ms, unacknowledged operations are resent with their existing IDs and deduplicated by the receiver.

After 500 ms:

- show an informational disconnection dialog;
- cancel active filter/correlation/activity coordination on both sides as soon as each side detects confirmed loss;
- abandon the retry journal and retain only current local state;
- keep both applications fully usable;
- do not persist the branch across process close/crash.

The Quest visualization remains usable indefinitely until the user closes it or the process exits.

## Reconnection handshake

Peers exchange session/scene/incarnation identity and an incrementally maintained lightweight checkpoint identity; the handshake never captures/hashes the whole scene. For all common incarnations:

- equal checkpoint identities reconnect automatically;
- divergent state opens one global choice for the reconnection, not one choice per scene;
- affected shared scenes are locked while the choice is open;
- “Keep Desktop” validates every selected checkpoint, then commits each common scene under an interaction lock on Quest;
- “Keep Quest” validates every selected checkpoint, then commits each common scene under an interaction lock on Desktop;
- heavy resources are never included.

“Atomic” here means no partial validated batch is intentionally exposed inside one scene. It is not a transactional rollback across several Unity scenes. If a setter unexpectedly fails after validation, mark that scene reconciliation failed/out-of-sync, keep the UI locked for that scene and require retry or full resend; do not claim that all scenes committed.

Checkpoint transfer is bounded, chunked, cancelable before commit and displayed with progress after 200 ms. If the connection drops during transfer/staging, discard the incomplete staging buffer, remain offline and offer reconciliation again after the next connection. Once a per-scene Unity commit begins it runs to completion or enters the explicit failed state.

The discarded state is not retained as a conflict branch. There is no property-level merge, wall-clock “last writer” algorithm or three-way merge engine.

## Missing and orphaned incarnations

If Desktop closed a scene while connected, Quest closes the matching incarnation.

If Desktop closed it while offline, reconnection reports that the visualization no longer exists on Desktop. The Quest incarnation remains usable but becomes permanently local/orphaned. A small per-scene disconnected indicator is shown. Its edits are never synchronized, even if Desktop later reopens the same source: reopening creates a different `incarnationId`.

A scene opened on Desktop while offline has no prepared counterpart on Quest. After reconnection it requires the normal full-scene delivery before live synchronization can start.

Orphaned/missing incarnations are excluded from the global Desktop/Quest state choice.

## Online rejection and retry

An invalid operation receives a scoped rejection containing operation ID and reason. Independent operations continue. Missing resource/topology/schema errors make the affected operation unavailable and request a full scene resend; they do not kill the socket or unrelated scenes.

Loss of an acknowledgement after application is handled by idempotent retry. A bounded applied-operation cache and cumulative accepted sequence prevent duplicate effects.

## Offline behavior of long jobs

Quest computes filters and correlations locally only when already offline and the user explicitly starts them. A job that began online is cancelled when the 500 ms grace expires; it is not automatically restarted offline. Activity projection follows the same cancellation rule and can be relaunched locally.

## Future multi-scene behavior

The first core may bind one active visualization, but all state and transport owners are keyed by `sceneId` and `incarnationId`. Future behavior will synchronize open/close and selected scene. The last brain/column interacted with on Quest will select the corresponding Desktop scene/column. This is a later implementation stage, not permission to use a global unkeyed session now.
