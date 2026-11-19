# Synchronization protocol

The wire carries absolute operations and bounded results over the authenticated duplex pairing stream. It carries neither UI events nor differential snapshots per interaction. The codecs cited here are the executable authority; values checked on November 2, 2026.

## Formats and compatibility

| Layer | Current format and source |
| --- | --- |
| Mutation | `HBS2`, protocol 2, 136-byte header; [MutationEnvelopeCodec / MutationPayloadCodec](../../../../Assets/Scripts/HBP/Sync/MutationCodec.cs). Payload schema **2** only, logical bound 16 MiB. |
| Auxiliary transport | `HBT2`, protocol 2, 136-byte header, maximum payload 64 KiB; [TransportFrameCodec](../../../../Assets/Scripts/HBP/Transfer/Transport/TransportProtocol.cs). |
| Publication checkpoint | Schema **7** only; [SceneMutationCheckpointCodec](../../../../Assets/Scripts/HBP/Sync/Scene/PublicationJournal.cs). |
| Correlation in checkpoint | Record schema **2** only; [CorrelationCheckpointRecord](../../../../Assets/Scripts/HBP/Sync/Scene/SceneMutationBoundary.cs). |
| Application progress | `HBRP`, schema **1**, 86 bytes; [RetentionProgress](../../../../Assets/Scripts/HBP/Sync/RetentionProgress.cs). |
| Reconciliation | `HBR3`; [SceneReconciliationProtocol](../../../../Assets/Scripts/HBP/Sync/Scene/SceneReconciliationProtocol.cs). |

Removing the old V2 names did not renumber these protocols. Experimental schemas, old checkpoints and the old timeline payload are no longer accepted; deploy compatible peers together. The [full delivery](../transfer.md) format and Desktop project files are separate contracts.

The mutation header encodes, in order: magic/version/kind/flags/lengths (16 bytes), session/scene/incarnation/operation/stream UUIDs (5 × 16), reliable/origin/canonical/observed sequences (4 × 8), origin (1), then seven zero reserved bytes. Offsets: 16/32/48/64/80 for UUIDs, 96/104/112/120 for sequences, 128 for origin. Flag bits 0/1 indicate canonical/observed; absent value = zero. Desktop sends nonzero canonical, Quest observed (initial zero allowed). `HBT2` uses its lane/chunk/schema fields in bytes 129–135; do not apply the `HBS2` reserved layout to those frames.

Integers and binary32 are little-endian. UUIDs use the codec's `Guid.ToByteArray()` bytes, without implicit conversion to a UUID string's network order. Text identities: strict UTF-8, NFC, nonempty, without control characters, at most 256 bytes. Floats must be finite; relevant contracts reject `-0`. The cut boundary canonicalizes zeros from native normals to `+0`, without relaxing the wire. A cut transmits orientation/flip/number/normalized position/full normal, with nonzero normal and position in [0,1]. Length/version/reserved-field checks occur before body allocation; unexpected trailing bytes are rejected. TLS provides frame integrity; content hashes validate dependencies/bodies, not a systematic second pass per frame.

## Order and scheduling

[Scheduler](../../../../Assets/Scripts/HBP/Sync/Scheduler.cs) distinguishes priority lanes and reliable streams. There is one control stream per direction, one operation stream per incarnation/direction, and one independent bulk stream per body. A structural operation stays in scene-stream order even if it uses control priority. Only an invalidating close can overtake this order by declaring watermarks to apply/abandon.

Coalescing keys, touched keys and barriers come from validated handlers, never a free-form UI list. Replacement affects only an unsent slot with the same key; a structural/job/transaction barrier seals previous slots. Origin sequence is assigned at wire commitment after coalescing, so a replaced preview creates no false gap. An already written frame retains bytes, ID and sequences until ACK or grace expiry.

Large bodies use a small ordered descriptor (identities, schema, length, digest, stream and barriers), then bulk chunks. Full validation precedes one atomic application. Independent already decoded operations can progress; overlapping keys await their barrier. This includes large authoritative corrections, recognized by [SceneOperationBulkReceiver](../../../../Assets/Scripts/HBP/Sync/Scene/SceneOperationBulkReceiver.cs), not just mutations.

| Default bound | Value |
| --- | --- |
| Inline / chunk / interactive burst | 4 KiB / 16 KiB / 8 operations before bulk service |
| Scene queue | 256 records, 256 KiB; control reserve 16 records/16 KiB |
| Session control queue | 64 records, 64 KiB; reserve 8 records/8 KiB |
| Reliable retry | 128 frames, 256 KiB; reserve 8 frames/16 KiB |
| Retained bulk bodies | 8 transfers; 16 MiB each, 32 MiB total |
| Touched keys per record | 128 |
| Unfinished decisions / conflict keys / scene watermarks | 4096 / 65536 / 4096 |
| Transport reception | 256 records / 4 MiB; 128 out-of-order records |
| Resumption | 128 streams including 126 unretired bulk ordinals per direction |

These caps are admission bounds, not a performance promise or recommended scientific size. Under pressure, unsent previews coalesce and bulk waits; latest state and required control are never silently lost. Inability to retain reliable work becomes an explicit error. Unity does not wait for socket capacity. Independent reader/writer, signal-awakened writer, TCP NoDelay; liveness does not depend on the PlayerLoop.

## Acknowledgments and bounded memory

A network ACK means admission to the bounded queue, **not Unity application**. Ephemeral ACKs/pings consume no reliable sequence and create no ACK-of-ACK loop. Jobs and checkpoints own their separate domain barriers.

HBRP encodes magic (4), schema/origin (1+1), session/scene/incarnation UUIDs (48), then four `u64` values: `AppliedOriginThrough`, `MinimumObservedCanonicalSequence`, `RetiredCanonicalThrough`, `CurrentCanonicalSequence`. Scope, direction, monotonicity and known bounds are checked.

- Quest announces a **completed contiguous prefix**, not the maximum received. Incomplete bulk, correction or an uncommitted checkpoint leaves a gap.
- The observed minimum includes all Quest proposals, deferred or already sent. Desktop retires a conflict key/tombstone only after this proof and retains a floor preventing an old proposal from bypassing the conflict.
- Desktop returns the applied prefix and safe canonical floor. Quest can then release received identities and obsolete keys, retaining what protects rollback and replay.
- Only one immutable progress record is in flight per direction, with the latest unsent value and a replaceable received notification. Each received progress record is checked before replacement.

Thus 4096 bounds **unresolved** work, not a session's total edits. Reuse of a UUID with a different payload is rejected within the active window; no universal registry of every UUID over the process lifetime is promised.

Current records, preview-reduction lookahead and reconstructed bodies belong to the Quest session until success. An interruption after ACK cannot make them disappear. Recording completion of application on Unity is independent of that connection's cancellation; receive pumps do not overlap.

Bulk IDs carry an ordinal per session/origin. After completion or cancellation, the retired prefix is transmitted in ResumeHello/`BulkStreamRetirement`; it allows old duplicates to be ignored without retaining their GUIDs indefinitely. During the 500 ms grace period, each stream replays its unacknowledged frames before new sequences, preserving interactive/control priority over bulk. After grace, the [reconciliation protocol](synchronization-architecture.md#interruption-and-reconciliation) takes over.

## Rejections and session control

A domain rejection contains an ID and bounded reason: invalid target/value, missing dependency, unsupported schema or busy sensitive operation. It does not terminate independent operations. A framing/authentication/negotiation fault can close the session; the [caveat about Quest error classification](synchronization-validation.md#points-to-qualify) remains explicitly documented.

Preferences use authenticated session control, separate from scene operations: pairing command 15, maximum body 1 MiB, result 1024 characters, atlas 128 characters, connection generation/UUID/revision. Quest history and Desktop save queue are bounded to 64. ACK follows application; UUID reuse with changed content is rejected. See [SessionControlProtocol](../../../../Assets/Scripts/HBP/Transfer/Transport/SessionControlProtocol.cs) and the [session contracts](synchronization-architecture.md#session-preferences-and-atlases).
