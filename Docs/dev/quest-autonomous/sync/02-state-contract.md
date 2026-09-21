# Operation and checkpoint contract

## Primary abstraction

The hot path is a stream of versioned, typed, absolute business mutations. It is not a diff of a captured dictionary and not a serialization of UI events.

Examples:

```text
SetCutDefinition(cutId, completeDefinition)
SetSiteColor(columnId, siteId, requestedColor)
SetTimelineAnchor(columnId, index, playing, looping, step, anchor)
SetRoiSphere(roiId, sphereId, completeDefinition)
ApplyTriangleMask(topologyId, adaptiveMask)
```

An operation describes intended domain state. “Move cut by delta” and raw controller trajectories are not canonical because replay can apply them twice. A complete cut definition is small and idempotent.

## Envelope identity

Every operation carries:

```text
schemaVersion
sessionId
sceneId
incarnationId
operationId
originDevice
originSequence
canonicalSequence?  // assigned by Desktop while online
operationType
payload
```

- `sceneId` identifies the logical visualization/data relationship.
- `incarnationId` identifies one opening of that scene. Closing and reopening creates a new incarnation even for the same source.
- `operationId` is stable across retries and echoes.
- `originSequence` orders records committed to the wire from one device. It is assigned after unsent coalescing, not at setter invocation.
- `canonicalSequence` orders accepted online operations per scene incarnation.

Each typed operation definition also owns deterministic scheduling metadata derived from its validated payload: optional coalescing key, bounded touched-key set and optional barrier scope. This metadata is not supplied as an untrusted arbitrary list by UI code. Batch/configuration handlers enumerate all touched keys before apply; overlap is defined by these keys.

Identifiers are not collection indices. Cuts, ROI, spheres, columns and resources require stable IDs. Sites use stable column identity plus site full identity. Automatic cuts use permanent semantic IDs for their three axes.

## Mutation classes

| Class | Reliability and scheduling |
| --- | --- |
| Structural | Create/delete/open/close and collection membership. Reliable, ordered, never coalesced. |
| Scalar assignment | Latest unsent value may replace an older value for the same scene/entity/property key. |
| Complete small object | Complete cut or sphere definition. Coalescable by object; application is atomic. |
| Atomic batch | Site imports/configuration results. Reliable as one validated assignment; a large body uses descriptor + independent bulk stream. |
| Job command | Reliable start/cancel with a generation ID. Result has its own chunk stream. |
| Timeline anchor | Latest anchor wins; playback advances locally. |
| Checkpoint | Full syncable state used only for initial agreement/reconnection/diagnosis. Never emitted per interaction. |

“Final” is a stream property. There is no required UI-specific final flag: an unsent preview may be superseded, but the newest remaining value is never discarded merely because input stopped.

## Checkpoint contract

A `SceneCheckpoint` is a detached, bounded representation of all synchronized properties for one incarnation. It exists for:

- fallback initial live-state agreement when the normal capture-boundary journal cannot prove/replay continuity;
- user-selected reconciliation after a confirmed disconnection;
- optional diagnostics that detect missed mutation routes during development.

It is forbidden as the normal change detector. Creating one may be O(scene); ordinary mutations may not.

The checkpoint includes current identities, collections, parameters, site state, masks, selections, timeline anchors and available canonical filter/correlation results. It excludes mesh/MRI/functional source bytes, Unity objects, derived render meshes/textures and local presentation.

A checkpoint is composed from the same typed state records and validation/apply handlers that own each operation family. Each family adds its checkpoint export/apply coverage when its handler is implemented. A generic `StateKey -> byte[]` map, reflection capture, post-hoc whole-scene diff or global invalidation is forbidden. Checkpoint support must not be deferred into a second monolithic adapter at reconciliation time.

Checkpoint identity is maintained incrementally by accepted typed handlers (for example a versioned aggregate/Merkle-style composition chosen in T03), with O(change) update cost. Reconnect handshake must not rebuild or hash a 30,000-site checkpoint by traversing the scene.

## Atomicity and validation

Before applying an operation, validate schema, scene/incarnation, entity identity, finite values, bounds, expected resource/topology identity and payload length. Related fields apply atomically and emit one targeted invalidation batch.

Atomicity is semantic, not a requirement to inline all bytes in the scene-operation stream. Any operation body above the measured inline threshold is represented by a small ordered descriptor containing operation ID, touched keys/barrier scope, schema, length and digest; its bytes travel on an independent reliable bulk stream. The receiver stages and validates the complete body before one apply. This applies to large configuration/site batches and dense triangle masks as well as job results and checkpoints.

Examples:

- all fields of a cut geometry apply together;
- a batch of site assignments becomes visible together;
- a triangle mask must match the original topology and exact triangle count;
- a bulk checkpoint validates completely before any shared setter is called.

A failed operation does not terminate the session. It is rejected with a scoped reason. An operation whose resource dependency is unavailable is never partially applied.

## Resource references

All selectable heavy resources are part of the initial prepared-scene manifest. Live operations use stable resource IDs plus cached content fingerprints. Resource list positions, paths and display names are not identities.

Fingerprints are calculated once after resource preparation on immutable buffers, preferably on a worker. If unavailable when send begins, the digest is accumulated over the exact outgoing bytes and verified over the same incoming bytes. There is no extra traversal, Unity-main-thread geometry read or duplicate copy solely for hashing.

## Special representations

- Site inclusion uses a bit per site in the immutable transferred site order plus the roster identity. At 30,000 sites this is about 3.75 KiB before compression.
- Triangle visibility addresses the immutable original topology. The wire chooses a sparse ID list or dense bitset without changing semantics. Runtime deletion must not renumber network identities.
- Bulk site changes use one atomic batch. Single-site changes use a single-site operation.
- Requested colors and parameters cross the wire; local appearance rules derive final rendered values.
- Filter and correlation results carry the generation/command ID that produced them. A result for an obsolete generation is discarded.

## Local application and echo suppression

Local and remote callers use the same business operations. A scoped application context carries origin and operation ID so a remote apply:

- performs normal validation and invalidation;
- may participate in local UI refresh;
- does not emit a new proposal back to its sender;
- does not get applied twice when Desktop echoes an identical Quest proposal.

Canonical echo handling has an explicit confirmation/correction path. If Desktop accepted byte-equivalent canonical payload, Quest only confirms it. If Desktop canonicalized a different value (notably a timeline anchor) or rejected a proposal, Quest applies the authoritative correction through the same targeted handler without repeating already-equivalent effects.

No generic “begin synchronized state” method may invalidate unrelated systems before the actual operation is known.
