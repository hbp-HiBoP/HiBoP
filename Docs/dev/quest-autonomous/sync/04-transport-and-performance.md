# Transport and performance contract

## Latency definition

Latency starts when the local business setter records the value. Record separate monotonic timestamps for:

```text
setter -> queued -> encoded -> first/last byte written
-> first/last byte received -> Unity apply start/end
-> next visible frame -> scientific result stable (when applicable)
```

Ordinary operations target remote application before the next feasible render frame. Desktop and Quest refresh phases are not synchronized, so this is a best-effort statistical target. Report p50/p95/p99 and missed-frame counts for USB and Wi-Fi; do not claim a hard one-frame guarantee for arbitrary scientific jobs.

Received, applied, next-frame visible and scientifically stable are distinct milestones. A normal mutation does not wait for any of them before allowing later independent mutations.

## Hot-path invariants

For a one-entity operation:

- no scene capture or scene-wide comparison;
- no iteration over unrelated sites/triangles/columns;
- no resource fingerprint calculation;
- no deep clone of a checkpoint or field dictionary;
- no polling delay;
- no mandatory next-frame delay before send;
- no render/quiescence fence before the next send;
- no allocation proportional to scene size;
- targeted domain invalidation only.

The fast path should aim for zero managed allocation after warm-up where practical. Any deliberate allocation must be bounded by payload size and visible in profiling.

## Connection and writer

Use one persistent authenticated duplex stream after pairing. For TCP/TLS:

- enable `NoDelay`;
- wake the writer through a signal/channel when a setter enqueues work;
- frame a message into a single contiguous/reused buffer or vectored write rather than separate tiny writes;
- keep read and write loops independent;
- do not couple liveness heartbeat waits to application acknowledgements.

USB and Wi-Fi use the same correctness and scheduling semantics. Different observed latency is measured, not represented by separate product behavior.

## Logical queues

The connection multiplexes three logical lanes. Lane priority and delivery class are distinct: the control lane contains both reliable application records and ephemeral ACK/ping traffic.

1. **control:** lifecycle, job start/cancel, structural operations, rejection, acknowledgement and liveness;
2. **interactive:** scalar/complete-object assignments, with coalescing by explicit key;
3. **bulk:** every payload body above the inline threshold, including checkpoints, job results, large atomic configuration/site batches and dense masks.

Bulk payloads are divided into bounded chunks (initially tune around 16–32 KiB). Between chunks the scheduler rechecks higher-priority lanes. A multi-megabyte correlation result may not block a color or selection behind one write.

The inline threshold is chosen and measured in T03, then enforced by the codec. A large operation places only a small reliable descriptor on its scene-operation stream; the descriptor carries operation ID, touched keys/barrier scope, body schema, length, digest and bulk-stream ID. The body uses an independent reliable bulk stream and applies only after complete validation. No large byte array is allowed on a session-control or scene-operation stream.

Priority is not starvation: after a bounded configurable burst of interactive records, a pending bulk stream receives at least one chunk unless urgent control work is pending. T03 chooses the initial burst from benchmarks and tests continuous interactive traffic with guaranteed bulk progress.

Coalescing removes only an unsent replaceable value with the same key. It never removes create/delete, a job transition, a checkpoint barrier or the newest value remaining for a key.

Reliable frame sequences are scoped to explicit session-control, per-scene operation or per-transfer bulk streams. Origin sequence numbers are assigned when an operation is committed to its scene-operation stream, after unsent coalescing. ACK/ping/telemetry messages are ephemeral and do not consume a reliable sequence. A structural/control barrier seals earlier coalescing slots: a later value cannot replace a slot positioned before that barrier.

Lane priority never changes semantic ordering. Create/delete and every other structural scene mutation remain records in the scene-operation reliable stream even though the scheduler serves them through the control lane. Only connection/session lifecycle uses the session-control stream. An invalidating scene close may overtake scene traffic only by declaring the final scene-operation watermark and bulk generations to apply or discard.

## Backpressure

Queues are bounded by count and bytes. On saturation:

- obsolete unsent previews are replaced/dropped first;
- an unacknowledged written reliable frame is retained unchanged until ACK or grace expiry;
- when the retransmit log approaches its bound, new previews remain unsent and coalesce by key;
- structural/final/control work is retained;
- bulk streams can pause and resume between chunks;
- capacity is reserved for required structural/lifecycle/cancellation control;
- inability to retain reliable work becomes a visible session error rather than silent loss;
- queue depth, replacements and drops are instrumented.

No producer may block the Unity thread waiting for socket capacity.

## Acknowledgements

Normal acknowledgements are asynchronous and cumulative where possible. They support retry, diagnostics and optimistic Quest proposal confirmation. They do not serialize the mutation stream.

`canonicalSequence` orders Desktop acceptance, not completion of derived work. A bulk job command may be accepted before a later color mutation while its result continues on an independent reliable bulk stream; its chunks carry the job generation rather than occupying canonical mutation slots. Ordinary inline mutations apply in canonical acceptance order per dependency key. Resource/configuration/lifecycle messages declare explicit barriers for the keys they affect. Receipt ACKs are cumulative per reliable stream; targeted apply/job ACKs are correlated by ID and may complete out of order. Ephemeral ACK/ping loss never creates a reliable-stream gap. After reconnect, missing bulk chunks cannot block the session-control or scene-operation streams.

Targeted barriers are allowed only for defined business workflows:

- filter/correlation UI unlock after Quest applies the canonical result;
- activity-sensitive UI unlock after both peers complete/cancel the generation;
- reconciliation unlock after checkpoint application.

There is no generic “visible ACK” that waits for every subsystem in the scene.

## Timeline clock

Timeline playback sends an index, play/loop state, step and a monotonic anchor. Desktop is the connected canonical clock. Quest seeks optimistically, then adopts the Desktop anchor. Both advance locally; automatic sample changes are not network mutations. Correct only when drift exceeds one sample. End/loop behavior is deterministic from the anchor and does not require a message if both sides reach the same state.

Do not serialize Unity `float` realtime as a durable clock. Use a 64-bit monotonic representation and account for observed transit/offset when adopting an anchor. Clock samples carry quality/age; the estimator uses multiple recent ping samples and rejects stale/high-uncertainty estimates. Without a valid estimate, apply the received seek immediately and start playback on receipt, then re-anchor when clock quality becomes valid. Tests include asymmetric latency and long-running counters.

## Initial scene delivery is a separate performance path

Protect click-to-first-visible-scene independently. Measure preparation on the Unity thread, serialization, first byte, network, decode, publish and first visible frame.

- Compute/cache resource fingerprints silently after scene preparation on workers that own immutable buffers.
- If the user sends before a digest is ready, hash the same byte stream during serialization/reception.
- Never scan geometry or MEG twice for fingerprint then serialization.
- Start streaming as soon as a coherent resource block is available; avoid waiting for unrelated hashes.
- Every selectable heavy resource required during the session is present in the initial manifest.

No live-sync change may regress this path without a recorded measurement and explicit decision.

## Required telemetry

Per operation/job record IDs, timestamps above, payload/chunk bytes, queue depths, coalesced previews, retries, rejections, cancellation/stale discard, main-thread time and GC. Telemetry must be cheap or disabled in production builds; it must not recreate whole-scene observation.

Numerical latency budgets are established from the first instrumented baseline and then treated as per-stage regression gates. “As fast as possible” does not excuse missing measurements.
