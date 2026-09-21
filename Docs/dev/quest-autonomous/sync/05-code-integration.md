# Integration and invalidation model

## Mutation boundary

The source of truth is the business operation/setter at the moment local software accepts a new value. UI callbacks and global toolbar refreshes are not the journal.

Each shared operation should have one target-explicit entry point that:

1. validates identity and value;
2. applies local state;
3. emits precise domain invalidations;
4. publishes a typed mutation if an active sync context is observing the scene.

An application context identifies `LocalDesktop`, `LocalQuest` or `Remote`, plus operation ID. `Remote` suppresses outbound echo but executes the same domain behavior.

No active session means the publisher is absent/null. There is no polling timer, snapshot capture or allocation beyond the smallest unavoidable branch/event call.

## Main-thread and worker ownership

- Unity objects and live scene collections are read/written on the Unity thread.
- A mutation payload is detached before worker encoding.
- Network read/write, hashing immutable transfer buffers, compression and pure codec work run off the Unity thread.
- The receiver validates pure envelope/payload data before enqueueing an apply record.
- Quest drains ready interactive records once at the earliest configured PlayerLoop phase. It does not post through multiple independent main-thread callbacks.
- A bounded per-frame apply budget may defer bulk work, but must not defer a tiny interactive operation behind it.

Shared queues use explicit thread-safe primitives. Session accepted/pending state is never inspected unsafely from both network and Unity threads.

## Targeted invalidation taxonomy

Replace generic state invalidation with explicit effects such as:

```text
SelectionOnly
SitePresentation
SiteScientificMask
CutGeometry
RoiMask
ActivityInputs
ActivityDisplayParameters
SurfaceVisibility
ProjectionGrid
ResourceTopology
TimelineDisplay
```

One operation may declare several effects, but only those effects run.

Important corrections:

- highlight, color and labels do not invalidate the activity field;
- filter inclusion, blacklist, ROI mask and influence distance do;
- cut changes do not invalidate the activity field;
- triangle visibility changes the projection target/display and is locked during projection, but does not automatically require recomputing the scientific field;
- selection alone is safe unless a specific policy derives a scientific change (automatic cuts derive locally; CCEP source selection is its own operation).

The broad `Column3D.OnChangeSiteState -> InvalidateActivityField` listener must be split by changed property or replaced with specific operations.

## Activity state model

Separate these values:

```text
ProjectionRequested: bool
AutomaticRecomputeEnabled: bool
ProjectionState: Absent | Stale | Computing | Ready | Failed
ProjectionGeneration: ulong
```

When automatic recompute is off, an invalidating mutation moves `Ready -> Stale` and waits for an explicit request. It must not restart merely because projection remains requested/visible. When automatic recompute is on, schedule the newest generation.

While computing, ordinary scene updates continue. Remove the broad early return that freezes all `Base3DScene.Update` work. A generation result publishes only when its scene/incarnation and input generation are current.

## Operation safety during projection

Allowed and immediately synchronized:

- local camera/wrapper manipulation;
- selected scene/column/site;
- cuts;
- colors, labels, highlight, opacity, transparency, colormap and edges;
- timeline controls;
- other presentation-only parameters.

Temporarily disabled on both peers:

- filter and blacklist;
- active ROI and ROI sphere geometry;
- scientific site positions;
- influence distances;
- CCEP source;
- implantation, MRI, mesh and topology-affecting representation;
- triangle erasure;
- projection-grid preferences.

Span/threshold operations that adjust an already prepared generator may remain enabled and retain their newest pending value. The exact matrix classification is authoritative.

The network always continues. Ordering resolves a sensitive-operation race deterministically: if the sensitive operation is canonically accepted before projection start, it applies first and the projection freezes the newer inputs; if projection start is accepted first, the later sensitive proposal is rejected as busy and Desktop returns the authoritative value for targeted Quest rollback. Sensitive operations are not queued ambiguously across a generation boundary.

## Long-job coordinator

Introduce one per-scene coordinator keyed by job type and generation. It owns UI busy scope, cancellation, progress, result freshness and ready messages. The transport carries its commands/results but does not infer scientific dependencies.

Filtering and correlations are Desktop-executed while online. Activity projection is dual-executed. Detailed states are in `09-long-running-jobs.md`.

## Existing component disposition

| Existing component | v2 disposition |
| --- | --- |
| `DesktopSceneCapture` / archive | Keep for full delivery; remove duplicate fingerprint/copy work. Never call per live mutation. |
| `QuestManager` / pairing | Keep authentication/preferences; create v2 session owner only after successful publish. |
| `QuestAnatomySession` | Keep scene lifetime; delegate live operations to a v2 replica owner keyed by scene/incarnation. |
| `QuestAnatomyView.ApplyAsync` | Full publish/replacement only. Never use for normal mutation. Preserve Quest wrappers. |
| `Base3DScene.Operations` | Preferred domain boundary; extend with IDs, context and targeted invalidation. |
| `LiveGeometryStateAdapter` | Remove from hot path; checkpoint-only code may be rewritten, not assumed reusable. |
| `SharedState` / `StateDelta` / merge | Do not preserve as v2 live contract. Retain only proven low-level utilities. No three-way merge. |
| `DesktopReplicaSession` | Replace with event-driven v2 session and scheduler. |
| `ReplicaWire` | Replace/version explicitly for typed duplex envelopes and multiplexed chunks. |
| `BeginSynchronizedStateApplication` | Remove; application is operation-aware. |

## Assembly direction

Keep the pure envelope, operation DTOs, scheduler and codecs in a low-level runtime assembly with no Unity object dependency. Domain handlers live beside common 3D runtime code. Desktop/Quest session adapters depend on both. Do not create a circular reference merely to reuse existing snapshot types.

Before creating asmdefs, inspect the actual dependency graph and choose the smallest split. Avoid a new abstraction layer if existing lower-level assemblies can host pure contracts cleanly.
