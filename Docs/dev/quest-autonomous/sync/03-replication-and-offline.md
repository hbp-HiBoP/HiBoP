# Authority, disconnection and reconciliation

## Connected authority

Desktop is the online sequencer, not a render server. It assigns `canonicalSequence` to accepted operations. Quest applies its own input immediately, sends a proposal with a stable `operationId`, and treats the Desktop echo as acknowledgement rather than applying the same operation twice.

With one alternating user, genuine conflicts should be exceptional. Desktop keeps a `lastAcceptedCanonicalSequence` by touched operation key/group until Quest proves no outstanding proposal can predate it. A Quest proposal declares the last canonical sequence it observed; it is accepted when no touched key has a newer Desktop assignment. If a touched key is newer, Desktop wins and returns its authoritative assignment. Retiring a key advances a scoped minimum-observation floor: a proposal below that floor is rejected even when its specific key or deleted-entity tombstone has been removed. Automatic producers such as timeline playback follow their dedicated clock rules rather than pretending to be simultaneous user edits.

## Online history retirement

Healthy online sessions have no cumulative operation-count limit. The 4096-operation bound applies to unresolved decisions; accepted and rejected decisions release capacity after application completion. Queue, byte, reliable replay and bulk budgets remain unchanged. Saturating genuinely outstanding work still faults/disconnects rather than silently evicting unresolved decisions.

Transport ACK means bounded receive-queue admission, not Unity application. Both production owners exchange cumulative reliable `HBRP` application progress. One immutable progress frame may be in flight per direction, with only the latest unsent update retained. Progress does not acknowledge itself at application level and therefore cannot create an acknowledgement loop.

Quest retains dequeued application records, preview lookahead and reconstructed bulk bodies at session scope until processing succeeds, including across a short connection interruption. Incoming pumps do not overlap. Once application succeeds, recording its completion on Unity's thread is independent of connection cancellation; disposing the session still abandons retained work. Authoritative deletion corrections may resolve a cut already absent after a later optimistic deletion, while ordinary deletion validation remains strict.

Receipt notifications also occupy one replaceable queue slot, so receipt ACKs cannot flood the main thread with obsolete progress values. Every incoming progress frame is checked for scope, reliability, monotonicity and already-known origin bounds before replacement; application-dependent bounds are checked again by the owners. Queue admission is atomic with respect to backpressure and short reconnect replay. A correction at the version of a matching echo still resolves a later pending preview; only a strictly newer canonical value makes that correction obsolete.

The 86-byte, little-endian body contains magic `HBRP` (4 bytes), schema version 1 (byte), sender device (byte), session/scene/incarnation UUIDs (16 bytes each), then four `u64` values: `AppliedOriginThrough`, `MinimumObservedCanonicalSequence`, `RetiredCanonicalThrough`, `CurrentCanonicalSequence`. Scope, direction, monotonicity and known bounds are validated. Desktop and Quest must be deployed together; older receivers reject this unsupported control rather than providing unlimited-session compatibility.

- Quest's `AppliedOriginThrough` is a contiguous prefix of Desktop scene-origin records completely treated by the application. Bulk descriptors remain open until reconstructed application or explicit abandonment. Checkpoints remain open until Unity commit. Optimized batches complete skipped previews only after their replacements succeed.
- Quest reports the minimum observed canonical sequence among **all** pending proposals, including deferred and already-sent work; without pending proposals, it reports its current observed sequence. Desktop uses this floor to reclaim conflict keys.
- Desktop echoes the applied-origin prefix and reports a safe canonical retirement floor, below every accepted canonical still unresolved. An unsent preview replaced by a different logical operation is terminal; replacing a slot with the same operation ID is not retirement. The applied initial-publication barrier also closes canonicals covered by its checkpoint/journal.
- Desktop removes completed response decisions and their ledger IDs. Quest removes covered received identities and applied-key watermarks, retaining keys needed by pending optimistic operations and transaction replay. Corrections can legitimately reference an old key sequence; they still resolve their pending proposal and retain their identity guard until Desktop acknowledges their application completion.

Reliable receive-stream fences survive the existing 500 ms reconnect grace. The authority additionally fences retired Quest origin identities, and Quest suppresses canonicals behind its retirement floor. New logical mutations require fresh operation IDs; retries preserve their immutable frame identity. Changed-payload UUID reuse is detected within the active retention window; unlimited lifetime UUID auditing is not retained.

Optimistic rollback order contains only live entries and removes confirmed/rejected/replaced identities. Memory therefore follows live scene state and unresolved work, rather than elapsed session duration. T16 reconciles the retained single scene after grace expiry and adds no persistent offline journal; multi-scene behavior remains T17 work.

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

The implemented T16 flow reconciles one retained session/scene/incarnation and prepared-manifest identity. It runs after fresh Desktop-authoritative session preferences are restored. It never transfers atlas files or heavy source resources; both builds must include their atlases.

Each peer retains the last confirmed common typed state in memory while its incarnation exists. Online confirmations update its semantic digest incrementally. A bounded confirmation undo window allows peers to recover a shared base when the last application-progress message was lost. This is transient confirmation bookkeeping, not a persistent offline operation journal.

At reconnect, the peers capture their current typed checkpoints after pending scientific/resource work settles. Local scene/column pose is excluded. They find a retained common base, ignore natural playback advancement when there was no explicit timeline edit, and perform a semantic three-way merge:

- equal changes are silent;
- independent attributes and independent entities combine;
- conflicting fields, deletion versus dependent edits/selections, and changed resources versus their dependent values form coherent choice groups;
- each conflict group requires a Desktop or Quest choice using the existing authored dialog, with no Cancel button; the English message explains which version each button keeps and that independent changes are preserved;
- if no common base remains, one explicit whole-scene choice is required.

Capture and reconciliation may traverse the current scene once at reconnect. They are not ordinary interaction change detectors. Performance is assessed by feel for now; the user explicitly deferred precise instrumentation and numerical gates on 8 October 2026.

Both peers hold an interaction guard while choosing, staging and applying. They validate the complete merged checkpoint and prepare missing locally computed representations before commit. The authenticated replica connection carries bounded, cancelable packets with a candidate identity/digest; full typed checkpoint bytes are allowed, source files are not. Existing delayed loading UI covers preparation/application after conflict choices.

Cancellation or a connection failure before commit applies nothing and returns to local use. Once commit begins, failure leaves the scene explicitly out of sync and guarded. A retry retains the exact candidate and guard, including if that retry fails before staging again. A completed handshake establishes a fresh common base, then fresh live owners restore ordinary bidirectional synchronization. There is no cross-process recovery or transaction spanning multiple scenes; T17 remains separate.

## Missing and orphaned incarnations

If Desktop closed a scene while connected, Quest closes the matching incarnation.

If Desktop closed it while offline, reconnection reports that the visualization no longer exists on Desktop. The Quest incarnation remains usable but becomes permanently local/orphaned. A small per-scene disconnected indicator is shown. Its edits are never synchronized, even if Desktop later reopens the same source: reopening creates a different `incarnationId`.

A scene opened on Desktop while offline has no prepared counterpart on Quest. After reconnection it requires the normal full-scene delivery before live synchronization can start.

Orphaned/missing incarnations are excluded from reconciliation choices.

## Online rejection and retry

An invalid operation receives a scoped rejection containing operation ID and reason. Independent operations continue. Missing resource/topology/schema errors make the affected operation unavailable and request a full scene resend; they do not kill the socket or unrelated scenes.

Loss of an acknowledgement after application is handled by idempotent retry. A bounded applied-operation cache and cumulative accepted sequence prevent duplicate effects.

## Offline behavior of long jobs

Quest computes filters and correlations locally only when already offline and the user explicitly starts them. A job that began online is cancelled when the 500 ms grace expires; it is not automatically restarted offline. Activity projection follows the same cancellation rule and can be relaunched locally.

## Future multi-scene behavior

The first core may bind one active visualization, but all state and transport owners are keyed by `sceneId` and `incarnationId`. Future behavior will synchronize open/close and selected scene. The last brain/column interacted with on Quest will select the corresponding Desktop scene/column. This is a later implementation stage, not permission to use a global unkeyed session now.
