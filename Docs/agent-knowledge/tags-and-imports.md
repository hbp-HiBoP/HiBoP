# Tags, imports and repair

Read when importing metadata, changing tag definitions/types, editing enum options,
or repairing project/workspace tag values and filters. For archives, serialization
and loading boundaries, read [Data and persistence](data-and-persistence.md).

## Import and inference

`TagCollection` keeps read-only category/AllTags views stable between mutations
and uses an ordinal ID index for average O(1) lookup. AllTags retains patient,
site, then general ordering. Build replacement lists/index before publication;
constructor/deserialization and explicit mutations, including ID regeneration,
rebuild them together. Do not mutate an indexed tag ID behind the collection.
Two distinct tag objects with one ID are rejected without damaging prior state;
the same instance in several categories is permitted. Share immutable valid-ID
sets across a loading batch instead of rebuilding them for each patient/value.

Tags are global definitions; patient/site values resolve to their canonical
definition by ID. Infer a type only for a globally new tag. An import never
silently retypes an existing tag or substitutes `0`, `false` or enum option zero
for an incompatible value. Keep the external source intact and report rejected
cells so corrected data can be imported again.

Pre-scan all configured metadata references of the workspace before creating tags,
including references not selected for that update. Aggregate by scope
(`Patient`/`Site`) plus normalized name: homonymous patient/site tags stay distinct;
an existing `GeneralTag` is resolved explicitly. Input order must not change inference.
BIDS, tags CSV/Excel and other tag-producing importers share the same policy.
Scanning does not mutate global tags or save them: validate a complete draft,
publish tags and data as one batch, then report creations, appended options,
incompatibilities and ignored values. Do not add an import-time type-review window.

Pass an immutable `TagParsingPolicy` built from preferences into pure services.
Do not let parsers query preference singletons independently. Boolean and ignored
token lists are trimmed, case-insensitive, deduplicated and disjoint. `0/1` are
not default boolean tokens. After ignoring configured missing values, infer
Bool → Int → Float → String using all remaining values; an all-zero column is Int.
Enum is a manual type choice, with options populated from known distinct values.

## Enum semantics

Modern enum values and filter values retain both historical integer `Value`
and semantic `StringValue`. The text is authoritative; the index is a cache.
After resolution, `Tag.Values[index] == StringValue`. Repair a divergent index
by exact ordinal label lookup, never clamp it to zero. Labels must be unique.
New options are appended once in deterministic order so existing indices retain
meaning. Report extensions; do not silently select a different option.

Legacy index-only values can recover a label from a valid current enum index,
with a grouped ambiguity warning. Without the old enum definition, conversion to
a non-enum cannot recover the original meaning. Report this limitation rather
than inventing a label. Manual reorder/rename/removal must warn about old files;
renames/removals can change the interpretation of modern labels too.

There is no separate schema-history/version system or stable per-option ID to
consult. Do not pretend such history exists when repairing older files.

## Explicit type changes

Manual tag editing is the type-change entry point. Prepare a plan on clones of
loaded database values, open-project values and affected global filters. Present
counts, conversions with loss/clamping, failures and deferred impact on closed
projects/workspaces before committing the user's chosen change.

Preserve the tag ID/name/category, value/filter IDs, exact patient/site owner and
collection position, and bind results to the new canonical definition. Keep all
valid tag IDs distinct from the subset selected for migration; migrating one tag
must not remove unaffected tags. Use the central conversion/factory services,
not a chain of generic Remove/Add/Copy operations or subclass `Copy` as a parser.

Conversions explicitly succeed or fail. Float-to-int needs a finite integral value;
modern enums convert through their text. Apply min/max/clamp only after semantic
conversion, and report loss. Ambiguous filter transformations (such as text
contains into a non-text type) are removed with a warning in the proposed migration
plan; editing confirms that complete plan, not a separate removal dialog.
Do not extend a proposed enum schema implicitly while converting its filters.

Prepare writes and validate the complete plan before publication. Failure restores
memory, owners and bindings. Persist loaded global/base changes with rollback;
mark the open project dirty without silently saving it. Closed projects/workspaces
are migrated on their next opening. Report a failed file restoration explicitly;
the sequence of saves is not an OS-level atomic multi-file transaction.

Current imports build their policy from `UserPreferences.Data.TagImport`, but
manual and deferred migrations in `TagCollectionModifier`, `Project`,
`GlobalDatabase` and `PersistentDataManager` use `TagParsingPolicy.Default`.
Do not assume customized import tokens also apply to those migrations.

## Repair on opening

Deserialize tag values with their original TagID/type/value before canonical
rebinding. Prepare migration on the unpublished graph before generic tag resolution
can cast against a changed definition. Apply current global definitions:

- Rebind compatible values; convert changed types while preserving value IDs.
- Report accepted lossy/clamped conversions.
- Remove values whose definition/ID is missing or conversion impossible, with an
  explicit runtime removal report. Do not serialize removed raw values elsewhere.
- Modern enums may extend options append-only; valid legacy indices recover with
  warning, invalid ones are removed and reported.

Keep every preset's name and ID, including an empty preset. Recursively remove
invalid leaves. In multiple-site conditions, remove only invalid child filters.
Keep All/Any groups with two or more children; replace a single-child group with
its child and combine negation (`child.IsNot ^= group.IsNot`); remove empty groups.
Do not quarantine/disable the entire preset because one condition is invalid.

Core owns the plan, not the dialog. After publication the UI explains affected
tags, conversions, repaired filters and deleted patient/site-value counts grouped
by patient name. Avoid technical IDs/types in the removed-values summary. Inform
the user that a normal save persists repairs; do not introduce a dedicated `.bak`
or repeatedly announce the same migration after save/reload. A corrected external
source can restore removed imported values through synchronization.

This policy does not authorize overwriting corrupted structural data. Invalid
global tag/preset JSON or irrecoverable protocol/patient/dataset references use
read-only recovery: retain valid objects, preserve original files and prevent saves
that would destroy unrelated data. Restore the source and reload/resynchronize.

Implementation owners are `Core/Data/Tags` (`TagParsingPolicy`,
`TagSchemaMigrationService`, `DeferredTagMigrationService`), `LoadingContext`, and
`UI/Tools/DeferredTagMigrationDialog`. Test conversions, owner/ID preservation,
canonical references, rollback, source-order independence and save/reload repair
idempotence through those owners rather than duplicating migration logic in UI.
