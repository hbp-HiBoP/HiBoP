# Loading, persistence and validation

Reference for the current code, checked on November 2, 2026.

- [Time series and memory](timeseries.md): epochs, grids, statistics, buffers, Trial Matrix and caches.
- [Tags](tags.md): inference, type changes and repair.
- [Performance](performance.md): useful historical results and reproduction.

Development rules remain in [Data and persistence](../../agent-knowledge/data-and-persistence.md).

## Pipeline owners

| Responsibility | Implementation |
|---|---|
| Database and workspace | [GlobalDatabase](../../../Assets/Scripts/HBP/Core/Database/GlobalDatabase.cs) |
| Archive and project graph | [Project](../../../Assets/Scripts/HBP/Core/Data/Project/Project.cs), [ProjectManifest](../../../Assets/Scripts/HBP/Core/Data/Project/ProjectManifest.cs), [ProjectArchiveReader](../../../Assets/Scripts/HBP/Core/Data/Project/ProjectArchiveReader.cs) |
| JSON and compatibility | [ClassLoaderSaver](../../../Assets/Scripts/HBP/Core/Tools/ClassLoaderSaver.cs), [SerializationTypeRegistry](../../../Assets/Scripts/HBP/Core/Tools/SerializationTypeRegistry.cs) |
| Binding and recovery | [LoadingContext](../../../Assets/Scripts/HBP/Core/Data/LoadingContext.cs), [LoadingRecovery](../../../Assets/Scripts/HBP/Core/Data/LoadingRecovery.cs) |
| Shared work | [SharedLoadingOperation](../../../Assets/Scripts/HBP/Core/Tools/SharedLoadingOperation.cs), [LoadingWorkScheduler](../../../Assets/Scripts/HBP/Core/Tools/LoadingWorkScheduler.cs) |
| Aspects and invalidation | [ValidationModel](../../../Assets/Scripts/HBP/Core/Tools/ValidationModel.cs), [ValidationImpactAnalyzer](../../../Assets/Scripts/HBP/Core/Tools/ValidationImpactAnalyzer.cs) |
| Open/create/SaveAs | [ProjectWorkflowService](../../../Assets/Scripts/HBP/UI/Tools/ProjectWorkflowService.cs) |

The pipeline is: reading/deserialization → detached graph repair → indexing
and binding → atomic publication (`Ready`) → validation of requested aspects.
JSON callbacks restore local invariants, without looking up relationships
in singletons or opening scientific sources.

`LoadingContext` builds ordinal tag/protocol/patient/dataset indexes
and per-protocol bloc indexes. Columns retain resolved instances. Database and
project each use their own captured scope, with no implicit fallback between them.
A structural collision excludes ambiguous objects and their dependencies in
read-only recovery; it does not arbitrarily choose the first object.
The workflow preserves the previous project and location on failure or
cancellation; Quick Start restores its snapshot on cancellation.

## JSON and archive contract

JSON is streamed as UTF-8 without BOM, indented for manual editing, with
separate read/write settings and `TypeNameHandling.Auto`. Historical compact
forms remain readable. `BaseData.ID` is generated on first access if null
(`Volatile`/`Interlocked`), or repaired during deserialization if absent/empty.
An empty string explicitly assigned at runtime remains detectable by validation.

A `.hibop` archive retains `<name>.settings`, `Patients/`, `Groups/`,
`Datasets/`, `Visualizations/`, with directory entries even when empty. The
historical `Protocols/` directory is accepted but ignored: global protocols
are canonical. Safe additional entries remain tolerated.

`ProjectManifest` is an in-memory index (`SchemaVersion == 0`, legacy-v0),
not an additional file. Its reuse depends on the ZIP's length and modification
time. Entries are read directly, without extraction during loading.
Independent ZIP readers are leased from a bounded pool, because DotNetZip
streams on the same instance share their position. All are closed even on
error/cancellation. Absolute names, drive/UNC paths, `.`/`..` segments and
duplicates after separator/case normalization are rejected.

The v2 format with short discriminators proposed in the earlier audit is not
the active format. Streaming and indexing were adopted while preserving
backward reading and manual editing, without imposing that schema migration.

## Serialized types and properties

The generated registry authorizes type names, with compiled historical aliases;
it performs neither runtime assembly scanning nor `Type.GetType` fallback.
Historical assembly names remain accepted for a known type: the assembly
is not the authorization key. An unknown type fails explicitly.

The two Core/Data fragments preserve Data → Core. The Editor generator
`Tools/Serialization/Generate Type Registry` consumes
[SerializationTypeAliases.json](../../../Assets/SerializationTypeAliases.json).
Property aliases use the JSON name and exact owner, are read but never written,
and reject two names targeting the same member. Value transformations go through
`SerializationPropertyMigrations`; the CCEP alias from `DynamicConfiguration` to
`CCEPConfiguration` preserves identity without globally renaming all dynamic
configurations.

Play rejects a stale registry; HBPBuilder regenerates before building and requests
recompilation if necessary. The prebuild guard for other paths validates without
rewriting during preprocessing. `typeof` references and
[link.xml](../../../Assets/link.xml) are complementary: members reflected by
Json.NET must also survive stripping. Configuration thresholds are persisted
as ratios; historical absolute values remain readable until resolved,
after which only the current representation is written.

## Availability and targeted validation

`Ready` guarantees a coherent published graph, without guaranteeing that its
files exist. `Validated` applies to a precise scope and specific aspects.
`ValidatedWithIssues` describes missing/invalid data; `ValidationFailed`
retains a technical exception, blocks the dependent operation and allows retry.

The shared operation provides observable progress, tasks and exceptions. Multiple
consumers join the same work. Cancelling a wait does not necessarily destroy
work still useful to others. Replacing the project/workspace invalidates
the generation; an old generation can no longer publish. Workers
prepare separate results before applying them together.

At startup/opening, checks cover availability and lightweight fingerprints,
without native EEG reading or a full CSV scan. Browse Database requires
`Ready`; Trial Matrix, visualization and export require prerequisites for their
selected data. Save waits for relevant validation already triggered,
without forcing all stale semantic checks. The explicit integrity report
is the consumer of full validation.

Aspects are Structure, SourceAvailability, SourceReadability, StaticContent,
Epoching, ChannelMapping and PatientAssets, with serialized states, diagnostics
and signatures. Request merging preserves each aspect's targets;
it does not create a cross-product. Changing a main event revalidates epoching
for the affected sub-blocs; a secondary code, site coordinates or tags do not
justify another EEG read. Changing site names revalidates their links;
changing the CCEP stimulated channel requires an in-memory binding.
An operation shares its native opening for triggers/channels and reuses
metadata from actual data loading when available.

The loader attaches to the ongoing operation without restarting it. Only its
owner can close it; a composed action retains its display between validation
and loading. Already-satisfied prerequisites do not open an empty loader.

## Concurrency and diagnostics

Without an override, with `P = max(1, Environment.ProcessorCount)`:

| Category | Limit |
|---|---:|
| JSON/ZIP and shared global budget | `min(8, P)` |
| Paths | `min(8, P)` |
| Metadata | `min(4, max(1, P/2))` with integer division |
| Native | `min(2, max(1, P/4))` with integer division |

Disabling multithreading imposes 1. A positive `HIBOP_LOADING_CONCURRENCY_OVERRIDE`
forces the diagnostic limit (without a CPU cap); `HIBOP_BACKGROUND_VALIDATION=false`
or `0` waits for validation in the same pipeline. The scheduler shares its
slots between database and project and prioritizes visible waits without
interrupting already active tasks. Results retain input order.

## Validation and limits

The [serialization](../../../Assets/Tests/EditMode/HBP.Serialization.Tests) tests
cover `ClassLoaderSaverTests`, `LoadingContextTests`, `ProjectArchiveTests`,
`SerializationTypeRegistryTests`, `SharedLoadingOperationTests`,
`LoadingWorkSchedulerTests`, `TargetedValidationTests` and project barriers.
The [workflow tests](../../../Assets/Tests/EditMode/HBP.ProjectWorkflow.Tests)
check publication and restoration; the Player registry round-trip exists in
[SerializationTypeRegistryPlayModeTests](../../../Assets/Tests/PlayMode/HBP.Workflow.PlayModeTests/SerializationTypeRegistryPlayModeTests.cs).

The historical July campaign reported a Windows IL2CPP round-trip,
without an equivalent Linux IL2CPP run established in this corpus. This does
not qualify the current checkout. Network-share diagnostics and Player
qualification require their own environments; see the
[Unity procedure](../../../.agents/skills/hibop-unity-validation/SKILL.md).
