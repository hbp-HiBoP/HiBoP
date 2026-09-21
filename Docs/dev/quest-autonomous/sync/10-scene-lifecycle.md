# Scene lifecycle, resources and future multi-scene support

## Identities

- `sessionId`: one paired live protocol session.
- `sceneId`: logical visualization relationship used for routing.
- `incarnationId`: one concrete opening/publication lifetime.
- `resourceManifestId`: immutable set of heavy resources available to that incarnation.

Closing and reopening always changes incarnation. A retry of the same interrupted full delivery retains its delivery identity only according to the existing reliable-transfer contract; it does not create two live incarnations.

## Initial full delivery boundary

All selectable meshes, MRI, implantations, functional resources, atlas/localizer dependencies and site/topology rosters required by live operations are included/prepared in the initial delivery. Ordinary live sync cannot introduce a new heavy resource.

Desktop interaction should not be frozen for the whole transfer. At a coherent initial capture boundary:

1. allocate scene/incarnation and baseline marker;
2. start a bounded reliable mutation journal for that incarnation;
3. stream/publish the prepared scene without waiting for background fingerprint passes;
4. after Quest publication, replay the accepted journal in order;
5. if the journal overflowed or its baseline cannot be proven, send one current checkpoint before declaring live.

This journal is only for the initial-publication window and transient reconnect grace. It is not offline history or snapshot polling.

T08 owns this journal and its replay/overflow/topology-abort tests. Full delivery must not be declared live before replay or fallback checkpoint completion.

A topology/resource change during publication that cannot be represented by already transferred resources aborts/restarts the full delivery rather than creating an incremental resource message.

## Resource readiness

The manifest contains stable resource IDs, type, dimensions/counts and content fingerprints. A dependent operation validates manifest/resource/topology identity before application. Runtime list index or filename is not sufficient.

Fingerprints are cached from resource preparation or accumulated over serialization. Quest verifies them while receiving. Production code must actually compare the fingerprints it paid to compute.

## Online close and replacement

Desktop close sends a reliable scene-close barrier on the session-control stream. It carries the final assigned scene-operation sequence and the bulk stream/generation watermark to finish or discard. Quest closes the corresponding incarnation after accepting the declared prefix or atomically recording its discard boundary; delayed frames through that boundary are ignored. Ordinary structural mutations remain in the scene-operation stream and cannot use close's invalidating shortcut. Closing one scene must not stop the transport or other scenes.

Replacing/resending a visualization creates a new incarnation. Late operations/jobs for the old incarnation are rejected/discarded and cannot mutate the new scene.

Quest may close a scene locally once Quest controls exist. While connected, that proposal is sequenced by Desktop and removes the matching Desktop scene only if product UI explicitly authorizes Quest-origin close; until then Quest close can remain a local UI capability marked out of shared scope.

## Offline close

Desktop closing an incarnation offline makes any surviving Quest counterpart permanently orphaned at reconciliation. It remains usable and receives a discrete broken-link indicator. Reopening the source creates a new incarnation and never reattaches the orphan automatically.

Quest closing offline simply removes its local instance. At reconciliation Desktop keeps its scene and may require a fresh full delivery to recreate it on Quest.

## First implementation boundary

The first v2 vertical slice may expose one active scene, but no type or singleton may assume there can only ever be one:

- sequences, queues, jobs, checkpoints and resource manifests are keyed by scene/incarnation;
- selection messages name their scene and column;
- a bulk stream in one scene cannot block another scene's control lane;
- local Quest wrapper state is stored per scene/column and survives in-place operations.

## Later multi-scene behavior

T17 adds:

- full-delivery registration of multiple scenes;
- Desktop open/close propagation;
- selected-scene synchronization;
- selection of the corresponding Desktop scene/column when the user last interacts with a Quest brain;
- per-scene connection/job indicators;
- global reconnection choice applied only to common incarnations.

This later UI stage must not require a wire-protocol redesign.

## User preferences

Pairing already transfers user preferences. Preferences that affect deterministic shared scientific behavior, notably automatic activity recomputation, are session policy owned by Desktop while connected. If mutable after pairing, changes are reliable small policy mutations. Pure presentation preferences remain local.
