# Tests and regression evidence

This reference replaces the chronology of the July 2026 campaigns with boundaries and contracts that remain useful. A test's presence in the repository does not mean it has run in the current session. This November 2, 2026 consolidation inspected sources and passed the static assembly gate; it ran neither Unity, a native campaign nor headset validation.

## Choosing evidence

| Behavior | Evidence and location |
|---|---|
| Models, parsing, serialization, deterministic computation, cache | Targeted EditMode, mainly `Assets/Tests/EditMode/HBP.Serialization.Tests`. |
| Archive and project orchestration | `ProjectArchiveTests` and `HBP.ProjectWorkflow.Tests`, with controlled dependencies. |
| Unity references, components, prefabs, materials | Asset integrity tests, without starting the entire application. |
| Lifetime, frame-boundary publication, scene, interaction | Minimal harness in the Module3D, Toolbar or UI PlayMode assemblies. |
| Bootstrap and multi-window workflows | A few Workflow smoke tests with the real bootstrap when it is part of the contract. |
| Native library, pixels, Player, real transport or Quest | Explicit suite/qualification in the appropriate environment; an Editor pass does not prove these boundaries. |

Reuse existing assemblies and helpers, name tests after behavior and assert an observable result. Inheriting from `MonoBehaviour` alone does not justify PlayMode; conversely, EditMode does not prove `Start`, `Update` or deferred destruction. A screenshot checks appearance, not data identity or reference correctness.

## Map of existing contracts

Paths below are relative to `Assets/Tests`. Lists group the main families without fixing test counts or old phase numbers.

| Family | Current entry points and contract to preserve |
|---|---|
| Serialization | `EditMode/HBP.Serialization.Tests/ClassLoaderSaverTests.cs`, `LegacyProjectCompatibilityTests.cs`, `SerializationContractAuditTests.cs`: historical reading, current round-trip, member names/types, constructibility, callbacks and `Clone`, `Copy`, `GenerateID`, `GetAllIdentifiable` contracts. |
| Archives | `ProjectArchiveTests.cs`: structure, identities and references, protocol external to the archive, aliases, portable paths, invalid/duplicate names, repeated saves without stale entries, malformed archives and unsafe entries, cancellation/cleanup, read-only recovery and Ready progress. |
| Project workflow | `EditMode/HBP.ProjectWorkflow.Tests/ProjectWorkflowServiceTests.cs`: success/failure/cancellation, project and location restoration, Save As, confirmations, Quick Start, configuration saving, scene retention when opening is cancelled. |
| Patients/groups/tags/sites | `PatientsGroupsTagsSitesTests.cs`: identities, copying, references, filters and tabular import/export. Preserve scientific keys, not just display labels. |
| Protocols/datasets/containers | `ProtocolDatasetDataContainerTests.cs`: data families, processing and protocol links; UI/Workflow PlayMode supplements interactions and persistence. |
| BIDS/localizer/database | `BidsLocalizerDatabaseWorkflowTests.cs`: reproducible discovery, paths and export; check formats and numeric culture, without private data. |
| Reading/processing/caches | `DataLoadingProcessingCacheTests.cs` and targeted domain suites: values, normalization, key/cache and reuse, invalidation, cancellation and resource ownership. A storage-only test does not prove result publication. |
| Information data | `InformationDataTests.cs`, `EditMode/HBP.ProjectWorkflow.Tests/InformationPresentationTests.cs`, `TrialMatrixValidationScopeTests.cs`: data and validation scope; windows are covered separately. |
| Module3D | `PlayMode/HBP.Module3D.PlayModeTests/Module3DScenePlayModeTests.cs`, `CutsTriangleErasingPlayModeTests.cs`: configuration/scenes, cuts and erasure with small deterministic volumes/meshes. |
| Toolbar | `PlayMode/HBP.Toolbar.PlayModeTests/ToolbarCoveragePlayModeTests.cs`, `CompareSiteToolbarPlayModeTests.cs`: commands and observable state; external actions use controlled adapters. |
| Graphs/UI | `PlayMode/HBP.UI.PlayModeTests/InformationGraphPlayModeTests.cs`, `ProtocolDatasetUiPlayModeTests.cs`: curves, matrices, filters, fields and events; synthetic datasets for values and an explicit native fixture for the NIfTI path. |
| User workflows | `PlayMode/HBP.Workflow.PlayModeTests/MainWorkflowPlayModeTests.cs`, `ProtocolDatasetPlayModeTests.cs`: creation/opening/saving, windows and model integration. |
| Architecture and assets | `UnityAssetIntegrityEditModeTests.cs`, `*PlayModeArchitectureTests.cs`, `HBP.PlatformConfiguration.Tests` and `HBP.Rendering.Tests` assemblies: fixture boundaries, references and configuration. Structural tests do not replace behavioral assertions. |
| Transfer/synchronization/Quest | Dedicated EditMode/PlayMode assemblies and utilities under `Assets/Tests`; select the affected family and evidence level rather than systematically rerunning the headset. |

Expected fingerprints and counts in `SerializationContractAuditTests` evolve with approved migrations; their current source is the test, not an earlier count of 515 members or 376 descriptors. On a difference, inspect the manifest, explain additions/removals/renames and cover compatibility before approving the new fingerprint. Historical members can be supported through the alias registry rather than remaining on the model.

## Fixtures and isolation

Fixtures must be small, anonymized/synthetic, with stable identifiers. Retain historical `$type` forms needed for compatibility. Generate an archive when that suffices; add a `.hibop` binary only if its binary form protects a case the generator does not represent. Versioned MNI fixtures are reusable; a personal path or unexplained `.artifacts` file is not.

`ApplicationStateTestScope`, `PersistentDataTestScope` and `TempDirectoryScope` isolate EditMode tests. `HBP.PlayModeTestUtilities` provides, among others, `PlayModeApplicationStateScope`, `PlayModePersistentDataScope`, `PlayModeTempDirectoryScope`, `PlayModeSceneScope`, `PlayModeWindowHarness`, `PlayModeProjectHarness` and `PlayModeModule3DTestHarness`. These helpers organize lifetime; product assertions remain in the relevant suite. Wait for operations, scenes and objects to close before restoring global state. Restoration must also work after failure or cancellation, so a later Play session does not inherit temporary directories.

Never read/modify personal preferences, aliases, histories, databases or workspaces to simplify a fixture. For a cache, also check cleanup followed by reuse: confusing `Clear` with final manager shutdown can invalidate a subsequent test or the user session.

OS dialogs, file opening and external actions are substituted in targeted tests. This proves the request and its parameters; manual verification remains necessary when actual system integration is the behavior being changed. Full bootstrap remains reserved for workflows that depend on it: otherwise it obscures responsibilities and adds unnecessary suite overhead.

## Asynchrony and evidence level

Unity/UniTask tests use `async Task` and direct `await`. Do not block the PlayerLoop with `.Wait()`, `.Result`, `GetAwaiter().GetResult()`, sleep or a wait loop; capture an expected asynchronous exception with an actually awaited `try/catch`. Exceptions and cancellations must be observed by the test. Historical Quest T13/T15 reports were invalidated because a displayed `Passed` result concealed a cancelled Task: these results are not evidence of success. The current validation procedure explicitly protects against this case.

Control races through an injected clock, barriers and completion sources: start A, replace/cancel it, complete B, then A, and prove that only B publishes or releases the UI. Check operation sharing, cancellation of a single consumer, stale generations, error cleanup and absence of an empty loader when a result is already ready. Do not make races depend on an arbitrary real delay or repeat a suite until it turns green.

Synchronization suites distinguish contracts/scheduling (`Sync.Fast`), local transport (`Sync.Loopback`), scene handlers (`Sync.SceneFocused`) and qualification/device (`Sync.Qualification`, `Sync.Device`). Check both origins, identities, atomic application, echo suppression, targeted invalidations and retained local presentation. A derived effect is proven at its cause; every UI gesture does not require an additional network operation.

`NativeFixtureIntegrationTests` separates `NativeFixtures` and `NativeDll` categories. Native graph paths also carry `NativeDll`. Some unavailable native dependencies explicitly trigger `Assert.Ignore`: count these cases as not executed, never as successful scientific qualification. Synthetic tests remain useful, but prove neither ABI, plugin loading nor final rendering. Similarly, a numerical comparison or historical frame median does not qualify current hardware; see [rendering validation](rendering/rendering-validation.md).

## Running and reporting

Current operational instructions are in the [hibop-unity-validation](../../.agents/skills/hibop-unity-validation/SKILL.md) skill: Editor instance selection, MCP/CLI, results, async safety and device validation. Earlier launchers mentioned in the July strategy are no longer the reference entry point. This page does not authorize an additional campaign when the task is documentation-only.

Choose tests proportionate to the modified boundary, inspect their actual results and distinguish compilation, reload, startup, execution and Player/device qualification. Report passed, failed, ignored and unexecuted tests and environment limitations. A relevant crash must be diagnosed; marking it ignored to obtain success would conceal missing coverage. Current GitHub workflows are manually triggered: the earlier proposal to run "all suites on every change" is not a delivered CI policy.

For an assembly change, also run [check-assembly-dependencies.ps1](../../Tools/check-assembly-dependencies.ps1). For rendering or scientific changes, retain parameters, input, native version, platform and metric so comparisons are reproducible. Additional validation follows from a change, failure or concrete uncertainty, without rerunning unchanged suites by default.
