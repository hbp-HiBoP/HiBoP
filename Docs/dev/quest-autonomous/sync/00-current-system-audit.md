# Audit and architectural reset

## Scope of the audit

The reviewed range starts at commit `a4ef93c` and includes the current S1–S3 implementation on `feature/xr-autonomous`. The audit was static: source, history and synchronization documents were inspected; no claim in this document is based on a successful build or device run.

The central conclusion is not that the implementation merely needs optimization. Its hot-path abstraction is wrong for the requested product. A breaking v2 refactor is required.

## Product mismatch

The required product is an operation stream:

```text
business setter -> local effect -> small typed mutation -> remote same setter
```

The implemented product is primarily a replicated state graph:

```text
generic change notification -> whole live capture -> snapshot diff
-> one in-flight revision -> whole-state apply -> render/quiescence acknowledgement
```

That design makes simple interactions pay for unrelated scene size and derived work. It also makes Quest-origin edits, job cancellation and multi-scene multiplexing harder rather than easier.

## Critical findings

1. `DesktopReplicaSession` permits only one revision in flight and waits for received, applied and visible acknowledgements before sending the next revision.
2. Quest's visible acknowledgement waits for `PrepareRenderingAsync` and another frame. `PrepareRenderingAsync` waits for broad scene quiescence, including geometry, activity, textures, sites and cuts.
3. The receiver performs multiple Unity-main-thread hops before a state becomes visible. The protocol therefore cannot make next-frame application its normal path.
4. The Desktop loop can sleep for 100 ms while idle; a setter does not directly wake a persistent writer.
5. `LiveGeometryStateAdapter.Capture` walks broad scene state after mutations. `StateSnapshot.Fields` and delta application clone dictionaries and byte arrays repeatedly.
6. `BeginSynchronizedStateApplication` invalidates activity and colliders for every remote state, including changes that are only selections or colors.
7. New mutations wait for an active generator instead of invalidating/cancelling an obsolete generation. The current generation guard prevents some stale publication but does not create a responsive scheduler.
8. Mutation detection relies on manually assembled event subscriptions and broad toolbar refresh events. It can miss a route and can recapture on unrelated UI changes.
9. The transport does not implement the required preview/reliable scheduling. TCP latency is aggravated by small writes and the absence of an explicit low-latency writer contract.
10. A rejection stops the active session rather than rejecting or rolling back one operation.
11. The live protocol has no complete Quest proposal path, optimistic edit identity, canonical echo suppression or per-scene multiplexing. The three-way merge implementation is not the runtime behavior.
12. Reconnect currently tends toward delayed full snapshots rather than a 500 ms retry window followed by an explicit whole-state choice.

## Newly introduced behavior that is actively harmful

- `Base3DScene.BeginSynchronizedStateApplication` increments a global state generation, invalidates activity and requests collider work for every synchronized apply.
- A listener on `Column3D.OnChangeSiteState` invalidates activity for filtering and blacklist changes, but also for highlight, color and label changes. These presentation/metadata edits do not all affect the scientific mask.
- `m_ProjectionIntent` mixes “projection should be displayed” with “projection should automatically recompute.” Once true, it can trigger recomputation even when the automatic preference is disabled.
- `Base3DScene.Update` returns while generators are running, preventing unrelated scene updates. The current `CanApplyPreparedState` also rejects all synchronized application during that work.

These behaviors must not be encoded as v2 compatibility requirements.

## Initial-transfer regressions to remove

The current transfer path may perform scene-sized work before bytes start flowing:

- `MeshGeometryFingerprint` traverses/copies several surface variants on the Unity thread, then archive construction copies data again.
- `MegContentFingerprint` walks all MEG samples before transfer.
- some expensive assertions/hashes do not prove runtime parity against the published Quest scene;
- `QuestAnatomyView.LateUpdate` allocates through `columns.ToArray()` every frame in addition to hierarchy queries.

Resource fingerprints remain required, but they must be cached after resource preparation or accumulated over the exact transmitted byte stream. They may not add a second traversal or delay first-byte delivery.

## What may be reused

- the prepared-scene initial delivery and resource lifetime model, after profiling and fingerprint changes;
- stable IDs already introduced where their lifetime and collision rules are correct;
- common `Base3DScene` operations and setters;
- primitive codecs that fit the v2 envelope without forcing the snapshot model;
- generation checks that prevent obsolete native work from publishing;
- existing pairing authentication and user-preference delivery.

Reuse is earned component by component. Existing code is not accepted merely because it is already present.

## Explicitly rejected foundations

- whole-scene capture and diff as the live mutation detector;
- polling as the primary publication trigger;
- a generic map of field IDs as the normal interaction protocol;
- one-revision-at-a-time visible acknowledgements;
- scene-wide invalidation before knowing the operation type;
- automatic three-way offline merge;
- durable offline edit recovery after process death;
- retransmission of heavy resources as an incremental sync side effect;
- a cut-only demonstration presented as feature completion.

## Refactor policy

Protocol v2 has no backward-compatibility obligation toward the current experimental sync protocol or snapshot schema. New components should be introduced behind a separate owner until a buildable vertical slice exists, then the legacy runtime and obsolete tests should be removed deliberately. Temporary coexistence is an implementation technique, not a permanent compatibility layer.
