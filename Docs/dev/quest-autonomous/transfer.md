# Scene transfer and synchronized Display

Contracts checked against the code on November 2, 2026. Scene, transport and HBNA prototype formats are independent. [Historical measurements](validation.md) do not establish a guaranteed delay.

## Capture and ownership

[Base3DScene.Transfer](../../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.Transfer.cs) awaits scene resources and protects its representation before capture. [DesktopSceneCapture](../../../Assets/Scripts/HBP/Transfer/Scene/DesktopSceneCapture.cs) detaches the graph on Unity without an intervening mutation; serialization, hashing and compression then work on this owned snapshot. Moving live-graph reading into `Task.Run`, or spreading it across `await` calls, does not guarantee a consistent snapshot.

Surface caches retain immutable geometry/bytes under versions and budget (128 MiB per capture/reception cache), with references retained during use. They do not implicitly share mutable masks or a native object between columns. A repeated pack reference produces independent arrays for consumers that can modify them. User resources retain their provenance; a verified private resource can reuse that proof during its lifetime, not solely on the strength of a SHA name or timestamp.

Native preparation remains private until full archive validation. The current pipeline does not publish an object from partially received metadata. GIFTI retains its serialization and Unity operations remain on the main thread. See the [lifetime rules](architecture.md#lifetime-and-concurrency).

## Scene archive

[ScenePayload](../../../Assets/Scripts/HBP/Transfer/Scene/ScenePayload.cs) uses format **4**, retaining reading of format **3**. [SceneArchive](../../../Assets/Scripts/HBP/Transfer/Scene/SceneArchive.cs) and [BufferPack](../../../Assets/Scripts/HBP/Transfer/Scene/BufferPack.cs) are the validation references.

| Element | Contract |
| --- | --- |
| Metadata | Visualization and prepared data; allowed types/references, duplicate JSON properties rejected. |
| Global references | Compact table against the owned context; two objects with the same ID must not conceal incompatible definitions. |
| `buffers.index` | Magic `HBPI`, version 1, little-endian: 12-byte header, then 48-byte entries: SHA-256 (32), offset (uint64), length (uint64). |
| `buffers.pack` | Bounded ranges with exact contiguous coverage; no overlap, gaps or extra bytes. Coordinated reads, no unlocked shared cursor. |
| Native files | Resources needed by path-based APIs, with verified identities and companions; no arbitrary path accepted from the network. |

Current caps are 4 GiB decompressed, 128 MiB metadata and 100,000 resources/entries depending on the container. Wire names are ASCII bounded to 80 characters; native resource names are validated by the archive contract. These input caps are not RAM budgets: old scene, candidate, converted volumes, caches and GPU resources may coexist.

Standard MNI/atlas references are installed locally and resolved through logical identity and availability. Do not replace this contract with an assumed global comparison of hashes between the two installations. Unity packaging of compressed resources uses, among other extensions, `.gz.bytes`.

## HBT4 streaming

[BlockContainer](../../../Assets/Scripts/HBP/Transfer/Transport/BlockContainer.cs) starts with bytes `H`, `B`, `T`, `4` (last byte numeric), then a manifest bounded to 16 MiB. [SceneArchive.Blocks](../../../Assets/Scripts/HBP/Transfer/Scene/SceneArchive.Blocks.cs) imposes the order visualization metadata, global references, index, pack, then external resources.

DATA/END records have a 64-byte header. Sequences, resource indices, offsets, sizes and reserved bytes are checked; block hashes and the final transcript bind content and order. Corruption, truncation, out-of-bounds expansion or inconsistency prevent publication. TLS and authenticated pairing remain necessary: a SHA does not prove sender identity.

Production uses **Zstd level 3 in independent 256 KiB blocks**, with a raw block if compression is not smaller; queues are bounded to two blocks. The reader accepts allowed raw/LZ4/Zstd codecs with output bounded to 1 MiB. The native Zstd decoder rejects dictionaries, windows above 1 MiB, unexpectedly sized content and additional frames. [TransferCodec](../../../Assets/Scripts/HBP/Transfer/Codecs/TransferCodec.cs) calls `hbp_codec_encode/decode` from `hbp_core`; SHA primitives are also integrated there. An older standalone transfer library is not required.

[BlockDelivery](../../../Assets/Scripts/HBP/Transfer/Transport/BlockDelivery.cs) produces a finalized spool to resume **the same bytes and identity**. An incomplete producer must be cancelled and joined; the mutable graph is not rerun under an old identity. [SceneDelivery](../../../Assets/Scripts/HBP/Transfer/Scene/SceneDelivery.cs) retains active users, serializes sends and defers deletion until draining. A lost receipt does not prove remote publication failed; incarnations, receipts and tombstones prevent duplication and late resurrection. The publication marker is not a measurement of photons displayed in the headset.

## A Display batch

[SceneDisplayOperation](../../../Assets/Scripts/HBP/UI/Main/Visualization/SceneDisplayOperation.cs) owns the visible lifecycle and [QuestManager.Display](../../../Assets/Scripts/HBP/UI/Quest/QuestManager.Display.cs) reserves publications with auto-synchronization. Admission precedes sends; multiple consumers can await shared work without all becoming owners.

Success waits for local data/rendering readiness, publication of the correct incarnation on Quest, initial synchronization and confirmed batch release. Scenes are processed in requested order, with a single remote publication at a time. One capture can be prepared ahead, under a **512 MiB estimate**; a larger scene falls back to sequential preparation. This bound does not limit all process memory.

Progress is monotonic, currently weighted 15% local loading and 85% publications, capped at 99% before actual completion. It indicates estimated progress, not remaining time. A request started without Quest remains local; a headset connected afterwards does not reopen that lifecycle. Target, context and connection generation are fixed for the batch.

On remote failure, completed Desktop scenes remain usable. Explicit retries target incomplete scenes and validate their current state, without systematically reloading data. **User cancellation and network interruption are distinct**: cancellation can present `Transfer` / `Close` to resolve incomplete copies while automatic sharing remains active; an eligible transport interruption retains automatic connection recovery. A data error requires action. Closing and recapture await acknowledgments for the relevant incarnations; an old callback must neither publish success nor close a new scene.

Earlier plans imposing manual recovery after every interruption are therefore outdated. Source tests [SceneDisplayOwnershipTests](../../../Assets/Tests/PlayMode/HBP.Quest.PlayModeTests/SceneDisplayOwnershipTests.cs), [QuestNetworkTests](../../../Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/QuestNetworkTests.cs) and [BufferPackTests](../../../Assets/Tests/EditMode/HBP.Transfer.Scene.Tests/BufferPackTests.cs) describe, among other things, cancellation, admission, generation, recovery and buffer independence. They were not run for this documentation consolidation.

## HBNA: prototype harness contract

The codec remains in [Assets/Tests/Support/TransferPrototype](../../../Assets/Tests/Support/TransferPrototype/Anatomy/AnatomySnapshotCodec.cs), with v1/v2/v3/v4 readers. It serves parity fixtures/diagnostics, not the current scene workflow: v1 anatomy, v2 contacts, v3 density projection, v4 iEEG. Its little-endian format carries float32/uint32, explicit units and coordinate systems, row-major matrices, linear color and surface/content hashes. It bounds files to **128 MiB**, vertices to **2,000,000**, indices to **12,000,000** and each UTF-8 identifier to **1,024 bytes**. Index count must be a multiple of three; UVs are absent or match vertices.

A harness limitation, such as HBNA v3 requiring `Smooth activity boundaries`, does not become a general limitation of the current product. [Fixtures](validation.md#fixtures-and-evidence) and their numerical evidence retain their original formats and binaries.
