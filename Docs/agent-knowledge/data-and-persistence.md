# Data loading and persistence

Read for project archives, serialization/migrations, time-series loading or
validation. For tag definitions, imports and repair, also read
[Tags and imports](tags-and-imports.md). This entry owns the durable data contracts;
historical measurements are not performance baselines.

## Project formats and migrations

Keep project JSON human-readable (`Formatting.Indented`), including streamed I/O,
and UTF-8 without BOM. Use stream readers/writers without materializing a complete
JSON string or creating a throwaway object before deserialization. Read and write
settings are separate so a read compatibility option cannot change the output.
Compact and indented historical JSON remain readable.

The `.hibop` archive contains its settings plus `Patients/`, `Groups/`, `Datasets/`
and `Visualizations/`. Preserve explicit folder entries, even empty ones, and
existing names/type contracts. The historical `Protocols/` folder is accepted but
ignored; global protocols are canonical. Safe additional entries are tolerated.
`ProjectManifest` is an in-memory index, not an extra file or new archive format.
Reuse an inspected manifest only while archive length and modification time match.

Read ZIP entries directly. DotNetZip readers on one `ZipFile` share a stream, so
parallel entry reads require a bounded pool of independent ZIP readers. Close
readers on success, failure and cancellation. Validate entry names before loading:
reject absolute/drive/UNC paths, `.` or `..` segments, and duplicates after
separator and case normalization. Loading must not repopulate an extraction folder.

## Serialized types and IL2CPP

Types are resolved through the generated `SerializationTypeRegistry`, never a
runtime assembly scan or `Type.GetType` fallback. The permitted type name is the
resolution key; known historical assembly names remain readable. Unknown types
fail explicitly rather than instantiate an unregistered type.

When renaming a serialized type, namespace, assembly or JSON property, update
`Assets/SerializationTypeAliases.json` and regenerate with the Editor command
`Tools/Serialization/Generate Type Registry`. The generated Core and Data fragments
form one registry while preserving Data → Core dependency direction. Preserve
Unity `.meta` identities when moving assets/scripts.

Property aliases use JSON names and an exact owner type, not a global text rename.
Aliases are read-only compatibility: write only current names. Reject input with
two aliases/current names addressing the same member, independent of order.
Value transformations belong in `SerializationPropertyMigrations`; preserve the
target family's identity semantics. Both project and prepared-transfer JSON use
the alias resolver. The Player consumes generated registrations, not the alias JSON.

Play entry rejects stale generated files. HBPBuilder generates before building;
if generated sources change, allow compilation and restart the build. The prebuild
guard for other build paths validates without modifying scripts during preprocessing.
The registry's `typeof` references and `Assets/link.xml` are complementary:
constructors and Json.NET-reflected members still need IL2CPP preservation. Do not
shrink linker preservation incidentally to a type rename; verify affected Player
round-trips for deliberate changes.

## Configuration and project workflows

For configuration thresholds, persist ratios so normalization changes do not
reuse incompatible absolute values. Legacy absolute fields remain readable until
they can be resolved; once resolved/saved, write the ratio representation. Reuse
[DynamicConfiguration](../../Assets/Scripts/HBP/Core/Data/Visualization/Configurations/DynamicConfiguration.cs),
CCEP's inherited behavior and the corresponding static configuration rather than
multiply configuration classes. Comment Newtonsoft `ShouldSerialize*` hooks:
they are invoked by convention, and their IL2CPP preservation matters.

Open/create/SaveAs workflows restore the previous project/location on failure or
cancellation; QuickStartCancel restores its saved snapshot. Keep display names in
JSON distinct from safe internal ZIP names.
The current owner is [ProjectWorkflowService](../../Assets/Scripts/HBP/UI/Tools/ProjectWorkflowService.cs),
with [workflow tests](../../Assets/Tests/EditMode/HBP.ProjectWorkflow.Tests/ProjectWorkflowServiceTests.cs).

## Time-series and cache ownership

Preserve access to all protocol blocs needed by Trial Matrix and fast reopening.
This does not require retaining every raw recording indefinitely: the current
`DataManager` and [RawRecordingCache](../../Assets/Scripts/HBP/Core/Data/Loaded/RawRecordingCache.cs)
support compact epoch storage, bounded retention and invalidation. Inspect those
owners before changing cache lifetime or protocol dependencies.

Epoch bounds are inclusive. Successive sub-blocs are juxtaposed; aligned sub-blocs
of different duration use the longest segment and retain their event-relative
alignment. `Timeline` is common navigation at the maximum displayed frequency;
`ProjectionTimeline` is each column's discrete grid at its own maximum internal
frequency. A column can itself contain several frequencies. Map through physical
time, including negative windows, shifted origins and noncommensurable rates.

`EEGPreferences.TemporalSampling` selects `Floor`, `Round` or `Interpolate`
(default). The same `(index, alpha)` drives surface, cuts, site values and
implantation. NIfTI export uses the exact projection grid without extra temporal
reconstruction. The obsolete configuration field `Temporal Sampling` is ignored;
do not add a migration/fallback that overrides the current user preference.

Preserve the exact precomputed scientific path and immediate access to protocol
blocs. Avoid persistent duplicate raw/epoch/baseline/normalized arrays when views
or compact backing suffice. Compact epoch backing depends on protocol windows;
it cannot inherit the protocol-independent lifetime of a complete raw recording.
Changes to processing, normalization or aggregation invalidate their actual
derivatives. Reopening should reuse warm compatible data, not bypass invalidation.

`MemoryCacheLimit` is a shared cache budget in MiB; zero selects the automatic
policy. Account owned managed/native/GPU buffers once, unpin/remove them with
their lifecycle, and evict inactive reconstructible derivatives before colder
source data. Active data remains pinned; an active-only overrun is reported,
never hidden by scientific downsampling. This accounting is not total process RAM.
Do not reintroduce forced GC on normal cache-clearing paths.
When the existing normalization/preferences workflow reloads scenes, retain warm
sources, suspend cache eviction while clearing derivatives and reloading, then
restore the configured budget in `finally`, including cancellation/failure.
This is the bounded reload transaction, not permission to disable budgets for
ordinary loads.

Trial Matrix tiles width and height to the device texture limit while retaining
global coordinates, events and interpolation continuity across tile boundaries.
Graph rendering may materialize only the visible range, but full-resolution
CSV/SVG exports remain complete. Reuse and explicitly release owned rendering
buffers/textures. Scientific equivalence and visual acceptance are described in
[Scientific computing](scientific-computing.md).

## Loading, validation and publication

Keep raw JSON construction/deserialization separate from external file checks
and reference binding. Callbacks may restore local invariants, but must not
perform file I/O, scan global lists or use singletons to resolve relationships.
An explicit `LoadingContext` builds ordinal canonical tag/protocol/patient/dataset
and per-protocol bloc indexes once, then binds references in a separate pass.
Columns retain resolved dataset/bloc instances; getters must not search the
current project. Database and project contexts use their own captured patients/
datasets, with no implicit fallback between scopes or first-global-protocol choice.

Do not silently select one of several colliding structural IDs. Recovery excludes
all ambiguous objects, reports affected owners and rebuilds dependent context
after exclusions. Preserve read-only recovery for irrecoverable structural data,
as described in [Tags and imports](tags-and-imports.md#repair-on-opening), rather
than importing the old all-or-nothing load behavior. Reference binding precedes
publication; external semantic validation can still be deferred as defined below.

`Ready` means JSON has been read, references resolved and a coherent graph
published atomically. It does not promise external files are present or usable.
`Validated` applies to the requested aspects and data, not an eternal global flag.
Normal invalid/missing data produces diagnostics (`ValidatedWithIssues`); an
unexpected technical failure produces `ValidationFailed`, remains observable,
and blocks dependent operations with an actionable retry path.

Startup/workspace change and project opening check source/asset availability and
lightweight fingerprints, without native EEG reads or full CSV scans. Browse
Database needs `Ready`; Trial Matrix, visualization and export request only their
selected data's availability, readability and semantic prerequisites. Save waits
for relevant validation already triggered by edits, without forcing all stale
semantic checks. Only an explicit full integrity report forces the whole base.

Keep validation aspects separate: structure, source availability/readability,
static content, epoching, channel mapping and patient assets. Serialize states,
diagnostics and dependency signatures; public errors/warnings remain an aggregate.
Merge requests without taking a cross-product of aspect and target IDs.

| Change | Required validation |
|---|---|
| Path/container | Availability, readability/content and dependent rules for changed data |
| Main event/type/codes | Epoching of affected protocol sub-blocs only |
| Secondary code, timing/processing, name/order/appearance | Local structural checks where needed; no unrelated file reads |
| Site added/removed/renamed | iEEG channel and CCEP links for that patient |
| Site coordinates/tags | No data-file validation |
| CCEP stimulated channel | In-memory CCEP link only |
| Mesh/MRI | Changed asset only |
| Alias or external file fingerprint | Recheck changed resolved paths; mark dependent semantic results stale until needed |

Epoch validation checks every sub-bloc's main event; one present alternative code
suffices. Missing secondary events produce no warning. Preserve iEEG warning vs
CCEP error severity. Within one operation share one native source opening for
triggers/channels; do not retain another reusable metadata cache. When real data
loading already reads metadata, publish those validation results instead of
opening the same source again.

One owned, observable operation serves concurrent consumers. An `Ensure...Async`
call joins existing work, propagates failure and supports cancellation. Replacing
project/workspace invalidates its generation; late results must not publish.
Cancelling a consumer's wait does not automatically cancel shared work still needed
by others. Background exceptions are retained and observed, not fire-and-forgotten.

Workers build independent results for an explicitly captured scope; they do not
discover the current project from a singleton midway or mutate UI-visible arrays.
Check cancellation and generation before one atomic publication and UI refresh.
Use the shared bounded scheduler: user-disabled parallelism means one worker,
native checks have conservative limits, visible work has priority, and database
plus project do not each allocate an unrestricted pool. Progress reaches UI on
the main thread.

Database loading and post-publication validation are silent until a user must
wait. Attach the existing loader to current progress without restarting work.
Only its current display owner may update/close it; composed validation/loading
phases keep one display. Already-satisfied prerequisites must not flash an empty
loading circle. Pure snapshot/report work can run off the main thread when it has
no Unity dependency; do not deep-clone an entire graph just for a lightweight report.

## Tags and repair

The complete maintained policy is [Tags and imports](tags-and-imports.md), including
inference, explicit type changes, owner/ID preservation and deferred repair.
