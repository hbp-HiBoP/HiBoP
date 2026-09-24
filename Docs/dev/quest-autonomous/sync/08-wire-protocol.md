# Wire protocol v2

## Purpose

Protocol v2 carries small typed mutations, job control/results, checkpoints and lifecycle messages over the authenticated pairing connection. It is intentionally incompatible with the experimental snapshot protocol.

The protocol is duplex from its first vertical slice even if Desktop-origin UI coverage ships first.

## Framing

Use a fixed-size bounded header followed by a validated payload. The final binary layout is chosen in T01/T05, but the header must expose enough information to reject an oversized/unknown frame before allocating its declared body.

Required logical fields:

```text
magic, protocolVersion, messageKind, flags
headerLength, payloadLength
sessionId, sceneId, incarnationId
messageId / operationId / jobId as applicable
reliableStreamId, reliableFrameSequence when applicable
originSequence, canonicalSequence as applicable
```

All counts and lengths have explicit maxima and checked arithmetic. Unknown required message kinds/versions reject the scoped session negotiation. Unknown optional extensions can be skipped only when their encoded length is bounded and the version contract permits it.

TLS provides transport integrity/authentication. Resource content fingerprints and checkpoint identities validate semantic dependencies; do not add per-frame cryptographic hashing without measured need.

## Message families

| Family | Examples | Lane |
| --- | --- | --- |
| Session | hello/resume handshake; bind scene, close, error | control; handshake is special, lifecycle/error is reliable |
| Mutation | Desktop accepted operation, Quest proposal, canonical echo | reliable interactive or control for structural |
| ACK/reject | cumulative receive/apply, proposal accepted; scoped rejection/correction | ephemeral control ACK; reliable rejection/correction |
| Job | start, cancel, ready, failed | reliable control |
| Bulk | filter/correlation/checkpoint chunks; large atomic batch/mask bodies | reliable bulk |
| Resource status | manifest availability/mismatch | reliable control |
| Ping | liveness/clock sample | ephemeral control, low rate |

## Operation ordering

Every reliable application frame belongs to an explicit reliable stream and carries a stream-monotonic `reliableFrameSequence`. Stream IDs and sequences remain valid when the socket reconnects inside the same session. The bounded stream classes are:

- one session-control stream per direction;
- one scene-operation stream per `(sceneId, incarnationId, direction)`;
- one independent bulk stream per job result, checkpoint or oversized operation body.

Mutations, lifecycle/job controls, scoped rejections/corrections and resource controls use the appropriate control/scene stream. Structural scene mutations always remain ordered in the scene-operation stream even when the scheduler gives them control-lane priority. Bulk descriptors are announced on the scene-operation stream, then their chunks use the declared bulk stream. ACKs, pings and telemetry are ephemeral and outside reliable continuity; when a not-yet-committed reliable session-control record is blocked on retransmit capacity, a later ephemeral record may pass it without changing the relative order or sequence of reliable records. Losing an ephemeral record cannot create a resume gap. Handshake/resume messages carry their own explicit session fields rather than consuming a reliable sequence.

Each operation origin assigns `originSequence` only when an operation record is committed to its scene-operation stream, after unsent coalescing. Unsent samples that were locally applied but superseded never consume either sequence and therefore create no false gap. Desktop assigns a monotonic `canonicalSequence` to every accepted connected operation/command.

- Desktop mutations are sent with their canonical sequence.
- Quest proposals carry operation ID, Quest origin sequence and last observed canonical sequence.
- Each operation type derives a trusted coalescing key, bounded touched-key set and barrier scope from its validated payload. Desktop compares the proposal's observed sequence with `lastAcceptedCanonicalSequence` for those keys, validates/conflict-checks, assigns canonical sequence and echoes the same operation ID.
- Quest confirms an identical optimistic payload without applying it twice; a different canonical payload/rejection uses the targeted correction path.
- Duplicate IDs are idempotent. A reliable-frame gap blocks only later frames in the same reliable stream until resume/replay fills it. It never blocks another scene stream or a control/operation stream behind a bulk stream. A coalesced unsent sample or lost ephemeral message cannot create a gap.

`canonicalSequence` is the order of acceptance, not a requirement that every derived result finish in that total order. Canonical operation/job descriptors are decoded from the ordered scene-operation stream before their effects are scheduled. The receiver tracks the newest applied canonical sequence per touched key. Independent decoded operations can apply independently; a decoded multi-key operation waits only for incomplete predecessors on overlapping keys, and structure/resource/configuration/lifecycle operations declare explicit barrier scopes. A job result is keyed by job generation and does not consume new canonical mutation slots for every chunk. Thus a job accepted at #10 can keep streaming on its bulk stream while an independent color accepted at #11 applies from the scene-operation stream.

Desktop's per-key accepted index is bounded by schema/entity limits and the current incarnation. Deleted-entity tombstone watermarks remain until incarnation close; they are not discarded merely by checkpoint compaction. Scene-wide barriers also maintain a bounded `(sceneId, incarnationId)` accepted watermark: an all-scene barrier conflicts with any newer accepted sequence in that scope, and later proposals must observe at least the latest accepted all-scene barrier sequence. Both watermarks are removed when the incarnation closes. Independent scenes have independent indexes. One slow scene/job may not block another.

## Coalescing and reliability

Only the sender's scheduler coalesces, before a value is committed to the wire. The explicit key is `(scene, incarnation, operation type, entity, property/group)`. A structural/control barrier seals previous slots, so a later assignment cannot replace a value ordered before that barrier. Once written, a reliable frame enters its stream's bounded count/byte retransmit log until acknowledged or the 500 ms grace expires. Every entry retains its encoded bytes or an immutable source from which exactly the same stream, ID, sequence and payload can be reconstructed.

Structural membership, lifecycle, job transitions, checkpoint barriers and bulk completion are reliable and never coalesced. Continuous absolute assignments can replace older unsent assignments with the same key. The latest remaining assignment is reliable even without an explicit end-of-gesture signal.

An unacknowledged reliable frame is never removed or rewritten, including a preview. Under retransmit-log pressure, the scheduler stops committing additional previews and coalesces only the newest unsent value per key. Bulk emission pauses between chunks. Capacity/headroom is reserved for structural, lifecycle, cancellation and other control records. If even required reliable work cannot be retained, the session enters a visible error state rather than silently losing continuity. Queue/retransmit byte limits and this behavior are covered by deterministic tests.

## Bulk streams

Every encoded body above the measured inline threshold uses a bulk stream, regardless of operation family. This includes job/checkpoint data, large D33/D34 configuration/site batches and dense triangle masks. The small descriptor remains in the scene-operation stream and declares operation/job/checkpoint identity, generation when applicable, touched keys and barrier scope, reliable bulk-stream identity, total length, chunk count/size bounds, content digest and body schema. Chunks may be interleaved with interactive messages. The receiver writes into a bounded detached buffer/stream and validates completion before one atomic Unity apply. Independent touched keys may continue while the body is incomplete; overlapping keys wait behind the descriptor barrier.

An obsolete/cancelled generation discards remaining chunks without publishing. Cancellation remains processable while chunks are arriving.

## ACK semantics

- Receipt ACK: cumulative per `reliableStreamId`. The ACK itself is ephemeral; if lost, a later cumulative ACK or idempotent retry recovers without a second effect.
- Apply ACK: correlated by operation/job/checkpoint ID; confirms a domain handler accepted it and may complete out of total canonical order for independent keys.
- Job ready: targeted business barrier for UI unlock.
- Next-visible timestamp: telemetry only, never a send gate.
- Scientifically stable timestamp: telemetry/job state only.

The sender may pipeline many operations/frames. It never waits for a generic visible ACK before writing the next independent mutation.

## Rejection

Rejections contain the relevant ID, code and bounded diagnostic text. Codes distinguish malformed payload, wrong scene/incarnation, stale sequence, missing resource/topology, busy-sensitive operation and unsupported feature.

One rejected operation does not close the connection. Fatal framing/authentication/schema-negotiation errors may close the protocol session.

## Reconnect

Within the 500 ms grace, reconnect proves the same session/incarnations and exchanges each active reliable stream's cumulative receive watermark, origin sequences and accepted per-key/checkpoint identity. Each stream replays every unacknowledged frame with unchanged IDs and sequences; within that stream, replay precedes newer sequence values.

Resume scheduling keeps the normal lane priorities: session-control and scene-operation replay/new urgent control run ahead of bulk replay, and bulk streams remain fairly interleaved. A new cancel can therefore invalidate an old bulk generation before its remaining chunks are replayed; a new independent mutation does not wait for a missing bulk chunk or oversized atomic body. Non-bulk reliable streams contain only bounded descriptors/control/operation payloads, never large bodies. Lost ACK/ping/telemetry frames are not replayed and do not create gaps. No checkpoint is sent when the identities agree.

An invalidating scene close is a session-control record carrying the target incarnation, the final assigned scene-operation sequence and the set/generation watermark of bulk streams to complete or discard. This is the only way it may overtake scene-operation/bulk replay. The receiver closes atomically, records the discard boundary and rejects all delayed frames for that incarnation. Ordinary structural mutations such as create/delete never use this shortcut.

After grace expiry, operation journals are abandoned and the reconciliation flow in `03-replication-and-offline.md` starts. It uses checkpoint bulk messages only after the user's choice.

## Liveness and time

Use one liveness owner. Ping samples estimate RTT/clock offset and report sample age/uncertainty for timeline anchors but do not gate mutations. The estimator requires several recent samples and prefers low-RTT observations; exact count/staleness thresholds are set from T00/T09 measurement and tested with an injected clock. If quality is invalid, timeline uses seek-on-receipt fallback. Use monotonic 64-bit ticks plus declared frequency/conversion; wall-clock time is not conflict authority.

## Threading

Network loops own framing buffers and never touch Unity objects. Valid decoded records enter bounded queues. The Unity apply loop returns outcomes to the network owner asynchronously. Shutdown/cancellation must wake reads/writes and complete queues without blocking the Unity thread.
