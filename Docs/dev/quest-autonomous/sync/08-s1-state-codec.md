# S1 — canonical state and merge contract

Implementation: `Assets/Scripts/HBP/Sync` (`HBP.Sync.Runtime`, no Unity references). The contract is independent of the saved project configuration and of the initial `ScenePayload`. S2 must capture every value from the live common scene and must reject a scene whose required fields cannot be produced.

## Identity and coordinates

- The header contains schema version `1`, epoch UUID, visualization ID, the initial prepared-resource manifest SHA-256 and accepted common revision. This manifest hash stays fixed for the epoch; later content-addressed resources are pinned and announced separately before a referencing revision. The epoch changes on explicit scene replacement. A temporary socket loss does not change it. The common revision is a Desktop-assigned history cursor, never a Quest local sequence.
- Keys are `(entity kind, parent ID, entity ID, field ID)`. Scene uses empty IDs. Site identity is `(column ID, Site.FullID)`. Sphere identity is `(ROI ID, sphere ID)`. Column IDs come from existing `Column.ID`. New cut, ROI and sphere IDs must be generated once per epoch and mapped to live objects by S2; runtime cut indices and list positions are never IDs. Auto-generated cuts retain IDs while the same semantic cut persists, or receive new IDs and tombstone the old cuts when regenerated.
- Collection order is an explicit integer field in the entity membership group. `exists=false` is a tombstone and is retained in checkpoints while any peer may refer to that entity. Reusing a tombstoned ID within the epoch is invalid. Parent deletion covers descendants: deleting a column covers its sites; deleting an ROI covers its spheres.
- Vectors describe scientific coordinates inside `Base3DScene`. Quest wrapper position, rotation and scale, Desktop camera/views and input handles have no field IDs. `Sphere.influenceRadius` is scientific; animated `Sphere.Radius` is local display.

## Field and group rules

`SharedStateSchema` is the authoritative field-number registry. Field numbers are scoped to an entity kind and must never be reassigned within schema 1. Group numbers are scoped to the same entity instance. A group is compared and replaced as a unit; absent fields differ from present fields. The S1 mapping in `operation-matrix.md` links every operation row to these numbers.

- Cut geometry `cut:2` contains orientation, normal, flip and position. Cut membership/order is `cut:1`.
- Topology `scene:9` contains mesh, preview MRI, mesh part, representation and both erasure masks. Its resource and mask are one group. S2 validates mask bit lengths against the selected topology; a syntactically valid mask alone is insufficient.
- Sphere geometry `sphere:2` contains position and influence radius. CCEP source `column:10` contains mode, site and atlas label. Timeline `column:14` contains actual navigation index, playing, looping, step, sampling and anchor time.
- A configuration load, global toolbar action or site import is encoded as the resulting assignments to these same groups. The source file path, toolbar global toggle and UI action name are not transmitted.
- Correlation result `scene:7` uses a prepared content-addressed result alongside comparison target. S2 may replace that dependency with reproducible inputs only after parity is demonstrated and a new schema version is negotiated. `scene:27` records manual activity projection intent independently from derived generator freshness.

## Canonical binary snapshot

`SharedStateCodec` writes little-endian `HBS1` magic, UInt16 schema version, 16-byte epoch UUID, length-prefixed UTF-8 visualization/manifest strings, UInt64 common revision and Int32 field count. Fields follow in strict key order: UInt8 entity kind, two length-prefixed UTF-8 IDs, UInt16 field ID, Int32 value byte length and value bytes. The registry fixes the value type for each field; no runtime type name or reflection payload is accepted. UTF-8 is strict. Float32 is unquantized, finite and normalizes negative zero to positive zero. Lists retain their intentional order. Absent optional references use an empty ID/resource string. Resource references have `kind:lowercase-sha256:positive-version`; the header manifest identifies the agreed resource set. Resource availability, topology dimensions and pinning are S2/S3 barriers.

The decoder rejects unknown versions/fields, duplicate or unsorted keys, malformed values, nonfinite floats, trailing data and excessive lengths before application. Limits are 100,000 fields and 16 MiB encoded state, with 8 MiB per value, 4,096 bytes per text value and 4,096 list items. These are safety ceilings, not target packet sizes; S3 sends compact changed-group assignments for ordinary updates.

## Three-way merge

`ThreeWayStateMerge.Merge(B,D,Q,baseHash)` requires a verified canonical hash of `B`, matching epoch/visualization/manifest, `D.revision >= B.revision` and `Q.revision == B.revision`. `D` may have advanced since `B`; that is the ordinary offline case. It validates each input, groups fields by entity and atomic group, then applies the B→D / B→Q table in `03-replication-and-offline.md`. Equal final values are accepted once. A conflict leaves Desktop's candidate in the merged candidate and retains complete B/D/Q group values in `StateConflict` for later user resolution. No conflict is auto-resolved. A tombstone against edits to that entity or descendants creates a delete-versus-edit conflict and restores Desktop's candidate for the affected groups. The merged candidate is validated again, including selected column, active ROI and per-column selected-site references. A dangling result is rejected rather than published.

A mesh/topology transition conflicts with edits to dependent cuts, sites, columns or ROIs on the other branch. An implantation transition has the same conservative dependency set. An MRI transition conflicts with other-branch cut and contrast edits. These conflicts retain both resource and dependent candidates until S5 resolves them; S2 may narrow a dependency only after proving the operation remains valid across that resource transition.

S1 does not assign a new revision or claim a synchronized visible state. S3/S5 must assign accepted revisions, retain conflict records and pending Quest edits durably, validate resource readiness, and distinguish accepted from applied and visible revisions. A transport proposal carries the verified base hash/revision, stable edit ID, device ID and local sequence in its envelope; those are not scientific state fields.
