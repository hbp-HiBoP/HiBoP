# Integration and invalidation model

## Mutation boundary

The source of truth is the business operation/setter at the moment local software accepts a new value. UI callbacks and global toolbar refreshes are not the journal.

"Business setter" means the domain-owned mutation boundary at the correct architectural layer. It does not mean the lowest property setter regardless of dependency direction. A lower layer must never import Sync, telemetry or another feature module so that the observer can be called there. If the existing property setter cannot expose the boundary without an inverted dependency, Core may expose a feature-neutral event or port and the higher composition layer observes it, or the mutation is routed through the existing domain operation boundary.

Each shared operation should have one target-explicit entry point that:

1. validates identity and value;
2. applies local state;
3. emits precise domain invalidations;
4. publishes a typed mutation if an active sync context is observing the scene.

An application context identifies `LocalDesktop`, `LocalQuest` or `Remote`, plus operation ID. `Remote` suppresses outbound echo but executes the same domain behavior.

No active session means the publisher is absent/null. There is no polling timer, snapshot capture or allocation beyond the smallest unavoidable branch/event call.

Diagnostic state is owned by the observing higher layer and exists only while capture is active. Do not embed synchronization trace points, attempt identities or diagnostic latches in long-lived Core entities such as every site, column, cut or timeline value. Disabled instrumentation must not increase the permanent size of those domain objects.

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

Assembly direction is a hard invariant, not a guideline inferred from whether Unity accepts the graph:

```text
HBP.Sync.Scene  ----> HBP.Sync.Runtime
       |                     |
       +---------------------+----> lower-level external/runtime contracts as allowed
       |
       +-----------> HBP.Core.Runtime

HBP.Core.Runtime  ----> no other HBP.* assembly
```

- `HBP.Core.Runtime` may reference Unity and external libraries, but no assembly whose name starts with `HBP.`.
- Sync, Transfer, UI, Data and Quest may depend toward Core according to the repository allow-list; Core never depends back toward those features.
- Pure envelopes, operation DTOs, scheduler and codecs stay in a low-level Sync runtime assembly with no Unity object dependency. Domain/session composition that needs both Sync and Core belongs in `HBP.Sync.Scene` or another existing higher layer.
- Core may define a feature-neutral domain event, value or port when multiple higher layers need to observe a mutation. It must not define or retain Sync-specific traces, attempts, sinks or telemetry state.
- Do not introduce a new neutral assembly merely to hide an inversion. A new assembly requires a demonstrated independent responsibility and an approved dependency edge.
- Every new or changed direct `HBP.*` reference must be explicit in the static dependency allow-list. The checker rejects all cycles and at minimum enforces the zero-HBP-dependency rule for `HBP.Core.Runtime`.

Before editing an `.asmdef`, inspect the actual direct and transitive graph and state the intended edge in the task plan. Run `Tools/check-assembly-dependencies.ps1` before any Unity suite. Successful compilation proves only that there is no compiler-visible cycle; it does not prove that the layer direction is valid.
