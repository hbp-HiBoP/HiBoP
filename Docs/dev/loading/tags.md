# Tags: import, conversion and repair

Definitions are global; patient/site values and filters reference them
by ID. This reference describes code checked on November 2, 2026.
Operational conventions remain in
[Tags and imports](../../agent-knowledge/tags-and-imports.md).

## Import

`GlobalDatabase.UpdateDatabaseAsync` uses a scan of all configured metadata
references, even those not selected for the update, before constructing
a draft. [TagImportScanner and importers](../../../Assets/Scripts/HBP/Core/Database)
feed the services in [Core/Data/Tags](../../../Assets/Scripts/HBP/Core/Data/Tags).
The key combines patient/site scope and normalized name; an existing `GeneralTag`
is explicitly resolved. Source order must not change the result.

`TagInferenceService` ignores empty cells and excluded tokens, then chooses
Bool → Int → Float → String. Without a meaningful value, it chooses String.
Zero alone is an integer, and Enum remains a manual choice. An import never
retypes an existing tag: it reports incompatible values and leaves the external
source intact. It can extend an enum by ordered appending to the list.

The immutable policy trims tokens, ignores their case, deduplicates them and
rejects overlaps. Defaults: `true, yes`; `false, no`;
ignored values `n/a, na, nan, null, none, -, not found`. `0/1` are not
booleans by default. Scanning neither publishes nor saves tags; the draft and
database are validated before publication, with restoration on persistence errors.

## Enums and identity

`EnumTagValue` and `EnumTagFilterValue` retain `Value` (historical index) and
`StringValue` (authoritative label). After resolution,
`Tag.Values[Value] == StringValue`. A mismatch is repaired by exact ordinal
label comparison, never by clamping to option zero. Labels are unique;
append-only additions preserve existing indices.

A historical enum without text can recover its label through a valid index in
the current definition, with an ambiguity warning. If the definition has changed
to a non-enum type, the old meaning is unrecoverable: no schema history or
stable option identifier exists. Reordering, renaming or deleting options must
therefore inform users of the consequences for older files.

`TagCollection` maintains stable views and an average O(1) ordinal index,
rebuilt together after mutation. `AllTags` order remains patient, site,
general. Two different objects with the same ID are rejected without altering
the previous collection; the same instance can appear in multiple categories.

## Manual changes

[TagCollectionModifier](../../../Assets/Scripts/HBP/UI/Main/Tags/TagCollectionModifier.cs)
prepares a `TagSchemaMigrationPlan` on clones of the loaded database, open project
and global filters, then requests confirmation of the plan. Context changes
during confirmation are detected and require preparation again.

Central conversion preserves definition ID, value/filter IDs,
exact owner and position. References are rebound to the canonical definition.
Float → Int requires a finite integral value; a modern enum converts through
its text; Min/Max/Clamp constraints apply after semantic conversion
and losses are reported.

Impossible values are removed and reported; an incompatible filter
(for example text `contains` to integer) is removed in the plan with
a warning. It does not by itself block the plan or require a second
deletion dialog. Rollback restores collections and bindings.
Saving definitions, filters and database follows confirmation; the open project
is not silently saved. If restoring files also fails, the UI exposes that error:
in-memory atomicity is not a guarantee of a file transaction resilient to
every system incident.

## Repair on opening

`DeferredTagMigrationService` operates on the unpublished graph, before generic
`LoadingContext` resolution. It retains compatible values, converts others
where possible and reports removed values by patient and site.
Removed raw values are not serialized in a recovery store.
Modern enums can extend options; invalid legacy indices
are removed with a report.

Presets retain their name and ID, even when empty. Repair descends into
conditions: only invalid leaves are removed; All/Any with one child becomes
that child with `child.IsNot ^= group.IsNot`; an empty group is removed.
The UI informs users after publication. A normal save persists the repair,
without a dedicated `.bak` and without repeating the report on the next load.

Corrupt structural data receives different treatment: read-only recovery,
preservation of the source and prohibition on saving an incomplete subgraph.
This also applies to an unreadable global Tags/presets file.

## Observed limit and verification

Import constructs its policy from `UserPreferences.Data.TagImport`.
However, current migration calls in `Project`, `GlobalDatabase`,
`PersistentDataManager` and `TagCollectionModifier` pass `TagParsingPolicy.Default`.
Custom import preferences are therefore not guaranteed during migration;
the earlier plan promising them on opening went beyond the code.
This documentation finding does not modify the product.

Existing tests: `TagInferenceTests`, `EnumTagReferenceTests`,
`TagCollectionIndexTests`, `TagSchemaMigrationTests`, `DeferredTagMigrationTests`
in [HBP.Serialization.Tests](../../../Assets/Tests/EditMode/HBP.Serialization.Tests).
They check, among other things, source order, owners/IDs, rollback,
targeted filter removal and idempotent repair. Their presence is neither
recent execution nor Player qualification.
