# Transport and performance

## Two traffic classes

The initial scene and new large resources use the existing authenticated, integrity-checked transfer path. Live scientific state uses a persistent bidirectional control stream on the same paired trust relationship. A control stream should carry schema/epoch negotiation, state proposals, accepted revisions, snapshots for resumption, acknowledgements, conflicts, heartbeat and resource references. Bulk data must not block the latest control state: use independent bounded I/O and an explicit resource-ready barrier.

The current Quest `TcpListener` accepts and fully handles one connection before the next, with an initial 15-second deadline. A persistent stream therefore requires a bounded concurrent accept/auth layer and a **single serialized scientific publication owner**; simply adding a never-ending command branch to `QuestPairing.ServeAsync` would block pairing, heartbeat and scene delivery. Keep the current pin and credential checks. Negotiate protocol/schema/capabilities before changing the scene.

## Rate is a ceiling, not an obligation

Quest tracking and rendering may run at 60/72/90 Hz; local feedback is produced on the local frame. State transport runs only when a shared value changes. For continuous manipulation, support a negotiated **up-to-90-Hz** sample ceiling, with 60 Hz and lower adaptive rates permitted when CPU, native computation or network queues cannot sustain 90. A stationary scene sends no scientific frames. The final value of every gesture is reliable and ordered even when intermediate previews are dropped.

An illustrative 160-byte state payload at 90 Hz is 14.4 KB/s in one direction before framing, TLS and IP overhead. Two-way echo and headers increase that figure. A 1-MB whole-scene snapshot at 90 Hz would be 90 MB/s and repeated encoding/allocation. These are arithmetic examples, **not measurements of HiBoP**. The principal risk is often rebuilding cut geometry, projections and textures at the receiving rate rather than raw link bandwidth.

## Hot-path encoding and queues

- Encode compact typed binary assignments with stable field IDs, fixed-width floats where science uses float32, bounded lengths and no per-frame JSON graph walk. Keep numbers unquantized unless a parity test explicitly approves quantization.
- One writer owns stream order. Separate bounded queues for reliable barriers/finals and replaceable previews. Per gesture or atomic group, at most one unsent preview is retained. Never let a slow receiver create an unbounded backlog.
- Do not wait for a rendered acknowledgement before sending the next preview. Track `received`, `accepted/applied` and `visible` separately; use credits/backpressure based on queue occupancy and measured processing time.
- On receive, validate epoch, IDs, sequence and bounds before dispatch to Unity's thread. Coalesce stale previews, apply the latest state, and invalidate only affected scientific outputs. A render job publishes only if its input revision is still current.
- A full **state** snapshot is a recovery mechanism. It references unchanged scientific resources by hash and preserves Quest presentation. Full `ScenePayload` replacement is reserved for a different scene/epoch or a resource transition that cannot be reconciled in place.

## Activation and Desktop cost

Create the synchronization adapter, subscriptions, capture scheduling, serializer and journal only after a scene has been successfully sent and a live session is established. Pairing alone and merely opening a Desktop visualization do not activate them. When no session has ever been sent, no added `Update`/`LateUpdate` polling, state hashing, scanning of sites/cuts or background worker should run. An optional nullable hook at a shared mutation point must return without allocation when inactive.

For an active session, callbacks mark changed groups; a session-scoped capture coalesces them at a frame boundary. Periodic full-state comparison can run at a **low diagnostic rate only while active** to detect missed mutation paths during rollout; it is not the 90-Hz hot path. During disconnection, Quest journaling continues. Desktop can defer its full comparison to reconnect if it retains the baseline and scene lifecycle guarantees, avoiding unnecessary offline polling.

## Measurements and acceptance

Measure on real Windows Desktop and Quest: input-to-local-visible latency, Desktop-to-Quest and Quest-to-Desktop visible latency, state bytes/s, control queue depth, dropped previews, terminal proposal delivery, CPU/GC per frame, scientific recomputation duration, and recovery time after one minute and longer outages. Test both an easy cut and expensive operations such as large site filters or topology-dependent masks. Record display refresh rate and whether the session was idle, dragging, or reconnecting. Choose numerical latency/throughput targets only after a baseline measurement; do not equate a 90-Hz transport ceiling with a guaranteed 90-Hz scientific recomputation rate.

For the inactive Desktop path, verify structurally that the adapter and its timers/subscriptions are absent and use a simple profiler smoke comparison to detect accidental allocations or work. The user did not request an elaborate no-headset benchmark suite.
