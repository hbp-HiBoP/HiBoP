# HiBoP Testing Strategy

This document is the implementation plan for building a broad regression test
suite before the architecture refactor described in
`Docs/dev/architecture_audit.md`.

The target is not only to raise a coverage number. The goal is to characterize
current behavior well enough that project persistence, data loading, 3D
visualization, information panels, toolbar actions and export workflows can be
refactored without relying on manual exploratory testing.

## Objectives

- Protect project and preference formats against accidental breaking changes.
- Cover the main user workflows with deterministic tests and synthetic data.
- Separate fast EditMode characterization tests from slower PlayMode scene/UI
  tests.
- Identify behavior that currently needs PlayMode, global singletons, native
  DLLs or real Unity assets before refactoring it.
- Make every future extraction from `Core`, `Data`, `Module3D` or `UI` start
  from an explicit regression fixture.

## Test Pyramid

HiBoP should use four complementary layers.

| Layer | Unity mode | Purpose | Examples |
| --- | --- | --- | --- |
| Characterization/unit | EditMode | Pure or mostly pure behavior, serialization, data transforms, parsers, configuration objects | `ClassLoaderSaver`, filters, protocols, datasets, `ProjectPreferences`, trial matrix data |
| Asset and prefab integrity | EditMode | Validate required scenes, prefabs, serialized references and assets without entering PlayMode | main scene exists, Module3D prefabs have required components, TextMesh Pro assets are present |
| Integration | EditMode and PlayMode | Workflows that need Unity objects but not full user interaction | project save/load, visualization configuration, scene configuration load/save, graph data generation |
| Functional smoke | PlayMode | End-to-end behavior through instantiated scenes, UI and runtime update loops | open synthetic project, create 3D scene, use toolbar options, display graphs/trial matrices, manipulate cuts |

Prefer the lowest layer that can prove the behavior. Do not put a test in
PlayMode just because the production class is a `MonoBehaviour`; first check
whether the behavior can be extracted or exercised through a plain data object.

## Current Baseline

The first tests already live in:

```text
Assets/Tests/EditMode/HBP.Serialization.Tests/
Assets/Tests/Fixtures/Serialization/
Assets/Tests/Fixtures/Projects/
```

`HBP.Serialization.Tests` is an EditMode-only Unity Test Framework assembly. It
currently references:

- `HBP.Core.Runtime`
- `HBP.Data.Runtime`
- `UniTask`

The current baseline focuses on:

- `ClassLoaderSaver` JSON round trips and legacy `$type` resolution.
- Core data objects used by projects: tags, filters, protocols, datasets, data
  containers, visualizations and 3D configuration data.
- `.hibop` archive structure, save, load and re-save.
- Lightweight Unity asset integrity checks for project prefabs, the main scene
  and required TextMesh Pro assets.

This baseline should remain fast and deterministic. It is milestone 0, not the
final coverage target.

## Proposed Test Assemblies

Keep test assemblies small and named after the behavior they protect.

```text
Assets/Tests/EditMode/HBP.Serialization.Tests/
Assets/Tests/EditMode/HBP.Core.Tests/
Assets/Tests/EditMode/HBP.Persistence.Tests/
Assets/Tests/EditMode/HBP.Data.Tests/
Assets/Tests/EditMode/HBP.Informations.Tests/
Assets/Tests/EditMode/HBP.AssetIntegrity.Tests/

Assets/Tests/PlayMode/HBP.Module3D.PlayModeTests/
Assets/Tests/PlayMode/HBP.Toolbar.PlayModeTests/
Assets/Tests/PlayMode/HBP.UI.PlayModeTests/
Assets/Tests/PlayMode/HBP.Workflow.PlayModeTests/
Assets/Tests/PlayMode/HBP.PlayModeTestUtilities/

Assets/Tests/Fixtures/
```

Suggested boundaries:

- `HBP.Serialization.Tests`: backward compatibility and JSON/archive round
  trips.
- `HBP.Core.Tests`: domain object invariants, filters, preferences, tags,
  protocols, treatments and ID/reference checks.
- `HBP.Persistence.Tests`: project archive, database references, import/export
  helpers, filesystem redirection.
- `HBP.Data.Tests`: BIDS helpers, data containers, `DataManager` behavior that
  can run with fake/synthetic data.
- `HBP.Informations.Tests`: graph and trial matrix data generation independent
  of rendered UI.
- `HBP.AssetIntegrity.Tests`: scenes, prefabs, materials, shaders, asmdef
  references and required serialized fields.
- `HBP.Module3D.PlayModeTests`: scene/view/column/camera/cut behavior.
- `HBP.Toolbar.PlayModeTests`: toolbar actions and their side effects on the
  active scene/configuration.
- `HBP.UI.PlayModeTests`: focused UI components and windows.
- `HBP.Workflow.PlayModeTests`: high-value user workflows spanning several
  systems.
- `HBP.PlayModeTestUtilities`: shared PlayMode-only harness code for temporary
  folders, redirected application state, isolated scenes, minimal UI roots and
  synthetic project setup. It should not contain assertions for production
  behavior.

## Fixture Rules

Fixtures must be synthetic and anonymized:

- no real patient, center, study, protocol or project names;
- no real filesystem paths copied from existing projects;
- stable IDs and names such as `synthetic-...` or `legacy-...`;
- small files only;
- no dependency on local user preferences or machine-specific project paths.

Legacy JSON fixtures should preserve old `$type` shapes exactly enough to
exercise compatibility. Binary `.hibop` fixtures should be added only when they
protect real compatibility behavior that generated fixtures cannot protect.

Recommended fixture layout:

```text
Assets/Tests/Fixtures/Serialization/
Assets/Tests/Fixtures/Projects/
Assets/Tests/Fixtures/BIDS/
Assets/Tests/Fixtures/DataContainers/
Assets/Tests/Fixtures/Module3D/
Assets/Tests/Fixtures/Graphs/
Assets/Tests/Fixtures/TrialMatrix/
```

Generated temporary files must use test scopes similar to the existing
`ApplicationStateTestScope`, `PersistentDataTestScope` and `TempDirectoryScope`.
Tests must never read or overwrite local preferences, aliases, databases,
project history, extraction folders or user workspaces.

## Running Tests

When Unity is open, use Unity MCP rather than launching Unity batchmode. Before
running tests, check the editor state and console errors through MCP resources
and tools.

Typical EditMode run:

```text
run_tests(
  mode="EditMode",
  assembly_names=["HBP.Serialization.Tests"],
  include_failed_tests=true,
  include_details=false,
  init_timeout=30000
)
```

Then poll the job:

```text
get_test_job(
  job_id="<job_id>",
  wait_timeout=60,
  include_failed_tests=true,
  include_details=false
)
```

Before and after the run, check the Unity console:

```text
read_console(
  action="get",
  types=["error"],
  count="10",
  format="detailed",
  include_stacktrace=true
)
```

The PowerShell wrapper remains useful for CI or closed-editor fallback:

```powershell
pwsh Docs/dev/run_unity_editmode_tests.ps1
```

For more filters and CI-oriented commands, see `Docs/dev/unity_test_cli.md`.

## EditMode Guidelines

Use EditMode for:

- JSON/XML serialization and legacy type migration.
- Project archive structure and generated synthetic projects.
- Domain object constructors, cloning, equality, ID/reference validation and
  default values.
- Preference round trips and path redirection.
- Data container metadata and path validation.
- BIDS/localizer helper logic that can operate on synthetic files.
- Graph and trial matrix data structures that do not require rendered UI.
- Asset/prefab integrity checks with `AssetDatabase`.

Avoid in EditMode:

- relying on `Start`, `Update`, coroutines, frame timing or input events;
- asserting behavior that only happens after PlayMode initialization;
- touching `Module3DMain` or scene singletons unless the test builds a controlled
  scene harness;
- native DLL-heavy behavior unless the test can clearly skip or substitute the
  dependency.

If a failing EditMode test needs PlayMode semantics, move it to a PlayMode
assembly or extract the tested behavior into a pure service first.

## PlayMode Guidelines

Use PlayMode for:

- `MonoBehaviour` lifecycle behavior;
- instantiated prefabs and serialized references;
- UI event dispatch, clicks, drags, toggles, sliders and dropdowns;
- scene/view/camera setup;
- cuts, raycasts, selection and runtime update loops;
- toolbar actions that mutate active scenes;
- graph and trial matrix rendering behavior;
- end-to-end smoke workflows.

PlayMode tests should use a small synthetic harness scene whenever possible
instead of the full application scene. The full main scene should still have a
small number of smoke tests to catch broken bootstrapping.

Every PlayMode test must:

- create or load only synthetic data;
- clean up created GameObjects, scenes and static state;
- wait explicitly for async operations, scene load completion and domain reload
  conditions;
- assert observable state, not just "no exception";
- keep screenshots optional and targeted for visual regressions.

### PlayMode Skeleton

The first PlayMode structure lives under `Assets/Tests/PlayMode/` and is
intended to receive both phase 7 coverage and the PlayMode-only smoke cases
identified during phases 1-6.

Shared utilities:

- `PlayModeTempDirectoryScope` creates disposable filesystem roots under the
  machine temp folder.
- `PlayModeApplicationStateScope` redirects `ApplicationState` paths and
  restores the previously loaded project.
- `PlayModePersistentDataScope` creates isolated `PersistentDataManager` and
  `DatabaseManager` instances backed by synthetic preference/database files.
- `PlayModeSceneScope` creates an additive scene, makes it active and cleans up
  created roots.
- `PlayModeWindowHarness` creates a minimal canvas, graphic raycaster and event
  system for focused UI tests.
- `PlayModeProjectHarness` creates and loads a minimal synthetic project.
- `AsyncPlayModeTestUtilities` provides non-blocking wait/exception helpers for
  async PlayMode tests.

Routing for backlog items:

- phase 1-2 project/window bootstrapping smoke tests go to
  `HBP.Workflow.PlayModeTests`;
- phase 3 site UI, site comparison and toolbar-driven site state tests go to
  `HBP.Toolbar.PlayModeTests` or `HBP.Workflow.PlayModeTests` depending on
  whether the assertion is a single command or a multi-window workflow;
- phase 5 database, BIDS and localizer window smoke tests go to
  `HBP.UI.PlayModeTests` unless they cross project/application state, in which
  case they go to `HBP.Workflow.PlayModeTests`;
- phase 7 visualization, view, camera, column, cut and selection behavior goes
  to `HBP.Module3D.PlayModeTests`;
- graph and trial-matrix rendering tests start in `HBP.UI.PlayModeTests` after
  their data-generation behavior is covered in EditMode.

## Coverage Backlog By Feature Area

### 1. Serialization and Format Compatibility

Goal: every persisted object can round-trip and legacy files remain readable.

Current status:

- Manual EditMode validation from
  `C:/Users/Benjamin BONTEMPS/Desktop/TestResults_20260630_163656.xml`:
  `HBP.Serialization.Tests` passed 40/40 with 0 failed, 0 skipped and 0
  inconclusive tests. Current local changes remove low-level background
  `Debug.LogException` calls from project loading and preserve the original
  JSON exception as `InnerException` on controlled `CanNotRead*FileException`
  errors. `Project.LoadAsync` tests must capture expected exceptions with an
  explicitly awaited `try/catch`, not `Assert.ThrowsAsync` or
  `Assert.CatchAsync`, because NUnit's synchronous async-assert wrappers can
  block Unity's main-thread continuation while `UniTask` is switching
  schedulers. The direct corrupted
  patient/group/dataset/visualization `LoadAsync` path is now covered by
  PlayMode phase 1 tests that exercise the Unity player loop without blocking
  it.
- `PersistentDataTestScope` initializes `PersistentDataManager` and
  `DatabaseManager` without invoking PlayMode-only `DontDestroyOnLoad`.
- Synthetic serialization fixtures use stable IDs and controlled
  `ApplicationState`/database context for project object references.
- JSON round trips currently cover `ClassLoaderSaver`, tags/tag values, nested
  filters, protocol, patient, dataset, visualization, all current synthetic data
  info/container variants, visualization columns and column base
  configurations and project/user/visualization preferences.
- PlayMode phase 1 coverage validates that a complete synthetic project can
  save/load through the runtime player loop while preserving serialized IDs and
  project references, and that corrupted patient/group/dataset/visualization
  archive entries throw controlled exceptions and clean extraction state.
- Legacy compatibility fixtures currently cover old `$type` shapes for
  `BoolTag`, `UserPreferences`, `GlobalDatabaseSettings` and project
  preferences from an old project-version fixture.
- Compatibility tests cover missing optional fields, ignored unknown/obsolete
  fields, stable IDs through repeated JSON save/load cycles, clone isolation
  for mutable serialization collections, and `Copy` behavior for applying
  state without replacing global collection/preference identity.
- Serialization contract audit tests currently lock 515 reflected JSON
  type/member descriptors and 376 lifecycle/BaseData contract descriptors.
  Any added, removed or renamed `[JsonObject]`, `[JsonProperty]`,
  serialization callback, `Clone`, `Copy`, `GenerateID` or
  `GetAllIdentifiable` surface must be explicitly reviewed and the approved
  manifest signature updated.
- Project-level JSON round-trip is intentionally not used because real project
  persistence is archive-based; `.hibop` save/load coverage lives in backlog
  area 2.

Initial tests:

- [x] `ClassLoaderSaver` saves and loads simple, polymorphic and nested objects.
- [x] Legacy `$type` names resolve for renamed or moved types.
- [x] Tags and tag values round-trip.
- [x] Filters round-trip, including nested `AllFilterCondition` and
  `AnyFilterCondition`.
- [x] Protocols, blocs, sub-blocs, events and treatments round-trip.
- [x] Datasets and representative data info/container variants round-trip:
  iEEG/Elan, iEEG/Micromed, CCEP/EDF, fMRI/NIfTI, MEGc/BrainVision, MEGv/FIF,
  static/CSV and shared fMRI/NIfTI.
- [x] Visualizations and all current synthetic column types round-trip:
  anatomy, iEEG, CCEP, fMRI, MEG and static.
- [x] Project, visualization and user preferences round-trip.

Later tests:

- [x] explicit fixtures for old project versions;
- [x] missing optional fields use current defaults;
- [x] unknown or obsolete fields are ignored or migrated safely;
- [x] serialized IDs remain stable through save/load/save;
- [x] clone operations isolate mutable collections, while `Copy` applies state
  without replacing global collection/preference identity.
- [x] reflection audit locks the current serialized field/type surface and the
  serialization lifecycle/identity method surface.
- [x] PlayMode save/load round-trip preserves serialized IDs and references for
  a complete synthetic project.
- [x] PlayMode corrupted patient/group/dataset/visualization project entries
  report controlled errors and clean extracted state without blocking the
  player loop.

### 2. Project Archive and Project Lifecycle

Goal: `.hibop` projects remain loadable, saveable and internally consistent.

Current status:

- `ProjectArchiveTests` covers generated `.hibop` save/load for minimal and
  complete synthetic projects.
- Phase 2 expansion covers project constructor defaults, `ProjectInfo` archive
  summaries, expected ZIP folders and persisted entry names,
  load/save/re-save validity, duplicate ID reporting, cancellation cleanup,
  missing ID reporting, corrupted settings JSON errors and accidental leakage
  of temporary absolute paths.
- Manual EditMode validation from
  `C:/Users/Benjamin BONTEMPS/Desktop/TestResults_20260630_163656.xml`:
  `HBP.Tests.Serialization.ProjectArchiveTests` passed 14/14, including
  `LoadAsync_WhenCancelled_CleansExtractedProjectFolder` and
  `LoadAsync_CorruptedSettingsJson_ThrowsControlledException`, and the full
  `HBP.Serialization.Tests` assembly passed 40/40. After discovering that
  `Debug.LogException` from project-data load failures could leave the Unity
  Test Runner/MCP bridge waiting indefinitely, project loading was changed to
  throw only the controlled `CanNotRead*FileException` errors for corrupted
  project entries while keeping the original JSON exception as
  `InnerException`. The `LoadAsync` cancellation and corrupted-settings tests
  avoid NUnit's `Assert.ThrowsAsync` and `Assert.CatchAsync`; they use an
  awaited local capture helper so Unity's main-thread continuation can run.
  Direct patient/group/dataset/visualization corruption tests through
  `Project.LoadAsync` are explicitly not part of the automatic suite for now:
  even with test timeouts, this scenario can block the editor on unfinished
  UniTask work.
- Current Unity MCP validation on 2026-06-30: targeted EditMode run of
  `HBP.Serialization.Tests` and `HBP.ProjectWorkflow.Tests` passed 93/93 with
  0 failed and 0 skipped. `ProjectArchiveTests` now covers project mutation
  invariants, project discovery, direct corrupted project-data entries through
  an awaited non-blocking harness, `ProjectInfo` fallback behavior, malformed
  archives, save/load cancellation at multiple phases, save progress, archive
  overwrite cleanup, duplicate and invalid persisted entry file names, alias and
  project-token path conversion, and saved settings version.
- `ProjectInfo` preserves unreadable settings diagnostics in
  `SettingsLoadException` without logging from its constructor. The Unity log is
  produced at UI/loader boundaries (`OpenProject` discovery and
  `ProjectLoaderSaver.LoadAsync`) where the error is actionable, which keeps
  low-level project scans and tests quiet while retaining the original failure
  details.
- Coverage verdict as of 2026-07-01: the EditMode portion of phase 2 is
  complete for core archive behavior and the extracted project workflow
  service, and the PlayMode portion now smoke-tests the project-window prefabs
  without booting the full main scene. These PlayMode tests verify prefab
  fields and service wiring rather than duplicating the deterministic business
  logic already covered by `ProjectWorkflowService`.
- The current "complete synthetic project" fixture is representative, not
  exhaustive. It contains one patient, one group, one dataset, one visualization
  and one site with representative data/column variants. Additional tests must
  intentionally cover multi-entity edge cases, reference cleanup and invalid
  names/IDs rather than relying on this single fixture as proof of full
  lifecycle coverage.

Initial tests:

- [x] minimal project save/load preserves archive structure.
- [x] complete synthetic project save/load preserves references and IDs.
- [x] invalid extension and missing settings are rejected.
- [x] malformed archive construction is rejected.
- [x] re-saving a loaded project produces a valid archive.

Add tests for:

- [x] project creation defaults;
- [x] project preferences and archive summary counts through `ProjectInfo`;
- [x] patients, groups, datasets and visualizations stored in the expected
  archive folders and file names;
- [x] duplicate IDs reported by `CheckProjectIDsAsync`;
- [x] missing IDs reported by `CheckProjectIDsAsync`;
- [x] load cancellation and partial extraction cleanup;
- [x] corrupted settings JSON entry reports a controlled error;
- [x] corrupted patient/group/dataset/visualization entries report controlled
  errors with preserved inner exceptions through an awaited non-blocking
  harness;
- [x] archive does not leak redirected temporary absolute paths.
- [x] `Project.SetPatients`, `RemovePatient`, `SetDatasets`,
  `RemoveDataset`, `SetGroups`, `RemoveGroup`, `SetVisualizations` and
  `RemoveVisualization` preserve project invariants and clean dependent
  references in datasets, groups and visualizations.
- [x] removing or replacing a patient removes patient data infos, group
  memberships and visualization patient references that point to the removed
  object.
- [x] removing or replacing a dataset removes every visualization column that
  references the removed dataset, including all dataset-backed column types.
- [x] project discovery through `Project.GetProject(path)` returns only valid
  `.hibop` archives, handles empty/missing folders predictably and ignores
  non-project files.
- [x] project discovery by ID returns the archive whose settings ID matches the
  requested ID and behaves predictably for missing or unreadable settings.
- [x] `ProjectInfo` default construction, corrupted settings fallback
  (`CanLoadProject = false`), preserved `SettingsLoadException`, temporary
  settings extraction cleanup and count calculation are covered directly.
- [x] `ProjectInfo` and `Project.IsProject` malformed ZIP behavior is specified:
  either controlled exception or false, but not an undocumented editor log or
  hang.
- [x] missing archive folders (`Patients/`, `Groups/`, `Datasets/`,
  `Visualizations/`) and multiple project settings files produce documented
  controlled errors.
- [x] load cancellation is covered at more than one phase, not only with a token
  cancelled before load starts.
- [x] load progress callbacks are monotonic enough for UI consumers and finish
  with the expected success state on valid projects.
- [x] save rejects empty, null and non-existent destinations with a controlled
  error and always cleans `ApplicationState.ExtractProjectFolder`.
- [x] save cancellation cleanup is covered before JSON writing, during entity
  writing and before the final ZIP is produced.
- [x] saving over an existing archive replaces stale entries instead of leaving
  removed patients/groups/datasets/visualizations in the ZIP.
- [x] saving updates project settings version from `ApplicationState.Version`.
- [x] project/entity names used as archive entry file names are tested for
  collisions.
- [x] invalid filename characters in project/entity names are either rejected
  before save or converted to controlled save errors.
- [x] aliases and extraction paths beyond temporary path redirection;
- [x] copied/embedded project data paths are no longer separate cases:
  `CopyIcons` and `EmbedDataIntoProjectFileAsync` were unused and removed.
- [x] UI lifecycle wrappers are covered with PlayMode or extracted service
  tests: `ProjectLoaderSaver.LoadAsync`, `ProjectLoaderSaver.SaveAsync`,
  `NewProject`, `OpenProject`, `SaveProjectAs`, project list display and the
  QuickStart project finalization flow. The extracted `ProjectWorkflowService`
  covers the business decisions; project-list display remains a PlayMode smoke
  candidate.
- [x] `ProjectLoaderSaver.LoadAsync` restores the previously loaded project and
  location on failure, clears data manager state at the right time and updates
  interactable/menu state on success.
- [x] save-as and new-project overwrite prompts preserve or change project name,
  preferences and loaded location exactly as the UI contract expects.
- [x] project archives reference database protocols instead of persisting
  separate protocol entries; this is covered by
  `SaveLoad_UsesDatabaseProtocolsInsteadOfSeparateProtocolArchiveEntries`.

Phase 2 core/project-workflow coverage is complete on the extracted EditMode
boundary and now has focused PlayMode prefab smoke coverage:

- [x] `NewProjectWindow_SetFields_UsesUserDefaultNameAndLocation`;
- [x] `SaveProjectAsWindow_Initialize_UsesLoadedProjectNameAndLocation`;
- [x] `OpenProjectWindow_DisplayProjects_PopulatesValidProjectsAndDisablesInvalidSettingsProject`.

The PlayMode tests instantiate the real window prefabs inside a synthetic UI
harness with a local `SelectionManager`, redirected persistent data and
synthetic project archives. They intentionally stop at prefab field/state
wiring; `ProjectWorkflowService` remains the source of deterministic coverage
for creation, save-as, open-project and QuickStart business decisions.

### 3. Patients, Groups, Tags and Sites

Goal: patient metadata and site state survive editing and filtering.

Current status:

- EditMode phase 3 characterization covers patient creation and JSON
  round-trip with mesh, MRI, site coordinates and patient/site tag values.
- Group persistence now verifies that serialized patient IDs resolve back to
  the loaded project patient objects.
- Site state coverage includes labels, highlighted/blacklisted flags,
  selection event state, configuration save/load and visualization site gain.
- Site filters are covered at the lowest practical layer for name, site tag,
  patient tag, raw position, data state, data type and scene-location delegate
  behavior.
- Synthetic CSV import/tag-generation coverage exists for site attributes
  through `Site.LoadSitesFromCSVFile` and `TagCollection.GenerateSiteTagsFromCSV`.
- PlayMode phase 3 coverage now instantiates the real Site Tools prefab with a
  synthetic `Base3DScene`/column/site harness. It covers change attributes,
  copy attributes and CSV export paths while keeping file browser/loading
  services outside the assertion boundary.
- Compare-site toolbar state is covered through a controlled PlayMode toolbar
  harness with a selected synthetic site.

Add tests for:

- [x] patient creation with meshes, MRI, sites and tag values;
- [x] group membership and patient references;
- [x] site coordinates, labels, tags, selection flags, blacklisted state and gain;
- [x] site filters by name, tag, data state, data type and scene location;
- [x] CSV import for site attributes through a service-level boundary;
- [x] CSV export for site attributes through a service-level boundary or a
  controlled UI PlayMode test;
- [x] copy/change site attributes through the underlying `SiteState`
  application boundary;
- [x] copy/change site attributes from the site tools window;
- [x] selected site state;
- [x] compared site state through a controlled scene/toolbar PlayMode harness.

### 4. Protocols, Datasets and Data Containers

Goal: protocol/dataset definitions and data references remain stable.

Current status:

- EditMode phase 4 characterization covers basic protocol normalization through
  `SetBasicProtocolFeatures` and advanced protocol round-trip with multiple
  blocs/sub-blocs, events, icons and all current treatment types.
- Dataset coverage now asserts protocol propagation, patient reference
  resolution after attaching a loaded dataset to a project, and typed accessors
  for static, iEEG, CCEP, fMRI, shared fMRI and MEG data infos.
- Data container coverage explicitly locks the current CSV, BrainVision, EDF,
  Elan, FIF, Micromed and NIfTI metadata variants through synthetic data infos.
- Path handling is covered through alias short-path serialization and
  conversion back to full paths under redirected test roots.
- Missing files, empty paths and unsupported file extensions are covered through
  deterministic `GetErrors` checks without invoking native data readers.
- PlayMode phase 4 coverage now extends the shared complete-project harness to
  all current data info/container metadata variants. It verifies project
  save/load in PlayMode and smoke-tests the real Protocol, Dataset and DataInfo
  selector window prefabs with synthetic phase 4 objects.

Add tests for:

- [x] protocol creation with basic and advanced blocs;
- [x] sub-bloc events, icons and treatments (`Abs`, `Clamp`, `Factor`, `Mean`,
  `Median`, `Min`, `Max`, `Offset`, `Rescale`, `Threshold`);
- [x] dataset references to protocols and patients;
- [x] data info variants: static, iEEG, CCEP, fMRI, shared fMRI, MEG;
- [x] data container variants: CSV, BrainVision, EDF, Elan, FIF, Micromed, NIfTI;
- [x] relative/absolute path normalization under redirected test roots;
- [x] missing files and unsupported formats report predictable errors.

### 5. BIDS, Localizer and Database Workflows

Goal: import/export helpers can be refactored out of UI windows safely.

Current status:

- EditMode phase 5 characterization covers BIDS discovery with synthetic
  `participants.tsv`, `sub-*`, `ses-*`, `anat`, `ieeg`, electrode TSV and JSON
  sidecar files through `BIDSParser`, `Patient.LoadFromBIDSDatabase` and
  `DataInfo.LoadFromBIDSDatabase`.
- BIDS export configuration now round-trips through `ClassLoaderSaver`, and
  `BIDSUtility.ExportPatient` is covered for generated anatomical, electrodes
  TSV and coordsystem JSON paths.
- BIDS electrode TSV coordinates are now emitted with invariant-culture decimal
  separators so generated exports are stable outside an en-US locale.
- Missing mandatory BIDS metadata is covered by a deterministic
  `participants.tsv` validation test.
- Database reference JSON coverage includes BIDS, BrainVisa, Localizer and Tags
  references, including Localizer parameters.
- Localizer protocol/data/bloc discovery is covered under redirected
  `ApplicationState.DataPath`; the test asserts that selection metadata can be
  built from synthetic localizer files without loading a 3D scene or volume.
- PlayMode phase 5 coverage now smoke-tests the real Database Browser, BIDS
  export and Localizer atlas export window prefabs with a seeded synthetic
  database. These tests verify list population, advanced export action
  visibility and export-button enablement after controlled selections without
  invoking the actual export jobs.
- `HBP.Serialization.Tests` passed 92/92 via Unity MCP EditMode after these
  additions. The post-run console still contains pre-existing Unity analyzer
  warnings under `Assets/Scripts/HBP/Data/Module3D`, but no phase 5 test
  failures or compile errors.

Add EditMode tests for:

- [x] BIDS folder discovery with synthetic participants, sessions, modalities and
  sidecar files;
- [x] BIDS export configuration serialization;
- [x] generated TSV/JSON file names and paths;
- [x] validation of missing required metadata;
- [x] localizer protocol/data/bloc selection discovery under the extracted
  non-UI `LocalizersObjects` boundary;
- [x] database reference serialization for BIDS, BrainVisa, Localizer and tags
  references.

Add PlayMode or workflow tests for:

- [x] database workflow opens synthetic BIDS references through the lower-level
  database loaders;
- [x] BIDS export service builds the expected exported file layout;
- [x] localizer workflow handles protocol/data/bloc selections through
  filesystem discovery;
- [x] covered workflow services do not require an already opened 3D scene;
- [x] focused UI PlayMode smoke tests for the database browser, BIDS export
  window and localizer export window once a small window harness exists.

### 6. Data Loading, Processing and Caches

Goal: `DataManager` and data processing behavior can be changed without hidden
global-state regressions.

Current status:

- EditMode phase 6 characterization covers `DataManager` cache lifecycle with a
  synthetic CSV-backed `StaticDataInfo`: load uses the cache, unload removes it
  and reload creates fresh data.
- Invalid `DataInfo` requests now assert controlled default behavior: public
  getters return `null` and do not mutate caches when `IsOk` is false.
- Synthetic in-memory iEEG bloc/channel fixtures cover channel statistics,
  event statistics, concurrent bloc-channel reads and processed iEEG
  `Unload()` cleanup without reading native EEG files.
- Normalization modes `None`, `SubTrial`, `Trial`, `SubBloc`, `Bloc`,
  `Protocol` and `Auto` are covered through `DataManager.NormalizeiEEGData`.
  The tests skip with an explicit reason if the native math DLL entry point is
  unavailable.
- Phase 6 tests found and fixed two cache/processing defects:
  `ChannelStatistics` did not store its generated per-bloc statistics, and
  `NormalizeiEEGData` recursively acquired the write lock while invalidating
  statistics.
- `DataManager.Clear()` is covered as the shared test-safe reset boundary.
  `DataManager.Cleanup()` remains an application-shutdown hook because it
  disposes the static lock and would make later EditMode tests unusable.
- PlayMode phase 6 coverage now verifies the same `DataManager` static CSV
  cache boundary under the Unity player loop: cached reads are reused,
  `UnLoad`/`Reload` create fresh data, `Clear` removes multiple loaded entries
  and invalid `DataInfo` requests do not mutate caches while the isolated
  application/persistent-data scopes are active.
- `HBP.Serialization.Tests` passed 104/104 via Unity MCP EditMode after these
  additions, with a clean post-run error console.

Add tests for:

- [x] load/unload/reload cache lifecycle;
- [x] `Clear` resets static dictionaries/caches; application shutdown cleanup
  is documented as not safe to exercise inside the shared EditMode process;
- [x] normalization modes: none, sub-trial, trial, sub-bloc, bloc, protocol and
  auto/default;
- [x] channel statistics and event statistics with tiny synthetic arrays;
- [x] concurrent reads do not mutate cached data unexpectedly;
- [x] missing or invalid data returns controlled error/default behavior;
- [x] native-DLL-dependent paths are marked separately and skipped when the
  dependency is unavailable.
- [x] PlayMode cache smoke tests for `DataManager` lifecycle, `Clear` and
  invalid static CSV data under isolated runtime scopes.

### 7. Module3D Scene, Views, Cameras and Columns

Goal: the runtime 3D state can be protected before splitting `Base3DScene` and
`HBP.Data.Module3D`.

Current status:

- EditMode phase 7 coverage now characterizes `VisualizationConfiguration`
  clone/copy for scene, camera, cut, view and ROI state, all current
  visualization column variants, their clone/round-trip/compatibility behavior,
  Module3D parameter containers (`AnatomyDataParameters`,
  `DynamicDataParameters`, `FMRIDataParameters`, `MEGDataParameters`) and
  `AtlasInfo` metadata without entering PlayMode.
- EditMode asset-contract tests load the real `Scene 3D` and `View 3D` prefabs
  and assert their serialized manager, displayed-object, container,
  column-prefab, camera and line-renderer references are present, including the
  manager back-references to the scene graph.
- EditMode asset-contract tests also validate each current `Column 3D` prefab
  has the required mesh/cut/site containers, `Views` child and `View3D` prefab
  reference used by `Column3D.AddView`.
- PlayMode phase 7 coverage now exercises a controlled Module3D scene graph:
  `Module3DMain.SelectedScene`/`SelectedColumn`/`SelectedView`, `Base3DScene`
  column variant aggregation and selection, scene selection events through
  isolated `Module3DMain` static events, observable `Column3D`
  minimized/activity-alpha state, `View3D` minimized/selected/camera-circle
  state, view viewport/render-target assignment, line removal across columns
  with selected-view fallback, controlled scene visibility/focus events, column
  configuration load/save for activity alpha, `Camera3D` zoom/strafe distance
  limits, standard/default view state, camera type and auto-rotation
  propagation, export-directory generation, `ROIManager` ROI-mask invalidation,
  `ImplantationManager` compare-site state, awaitable `CleanAsync` cleanup of
  generated scene objects, columns and cuts, the non-blocking controlled
  `CanNotLoadMNI` path of `Base3DScene.InitializeAsync` without native asset
  loads, a positive `InitializeAsync` path using an in-memory OBJ-backed
  synthetic MNI plus anatomy columns, initialized multi-column view-line/cut
  synchronization, initialized scene `LoadConfiguration`/`SaveConfiguration`
  round-trip behavior, and `TriangleEraser` enable/disable behavior against
  the generated invisible mesh.
- Unity MCP validation on 2026-07-01: `HBP.Serialization.Tests` passed 114/114,
  `HBP.Module3D.PlayModeTests` passed 22/22 with a clean post-run error
  console, and the local PlayMode assemblies (`HBP.Workflow.PlayModeTests`,
  `HBP.UI.PlayModeTests`, `HBP.Toolbar.PlayModeTests`,
  `HBP.Module3D.PlayModeTests`) passed 45/45. The broader PlayMode run still
  emits the known phase 1 corrupted-project `JsonReaderException` through
  `OpenProject.cs:97`; the targeted Module3D run does not.
- `SceneInformation` flag cascades are covered in EditMode so geometry/cut/base
  texture invalidation dependencies are explicit before manager refactors.
- Generated native MRI, mesh, GIFTI and TRM discovery is covered by
  `NativeFixtureIntegrationTests`; phase 7 remains the deterministic synthetic
  scene harness for runtime state and UI lifecycle behavior.

Add PlayMode tests for:

- [x] creating a synthetic 3D scene from a synthetic visualization;
- [x] `Base3DScene.InitializeAsync` reaches a loaded state with synthetic
  assets;
- [x] initialized multi-column view-line and cut add/remove synchronization;
- [x] initialized scene `LoadConfiguration`/`SaveConfiguration` round-trip;
- [x] `TriangleEraser` enable/disable controls the generated invisible mesh;
- [x] `CleanAsync` removes columns, cuts and generated scene objects through an
  awaitable cleanup path;
- [x] adding/selecting columns and view-line removal across all columns;
- [x] column variants: anatomy, static, iEEG, CCEP, fMRI, MEG and dynamic;
- [x] `View3D` creation, resizing, focus and visibility;
- [x] `Camera3D` default view, standard views, camera type switching,
  auto-rotate and export path generation;
- [x] mesh/MRI/implantation/ROI managers and displayed-object references are
  covered through prefab contracts plus focused runtime manager behavior;
- [x] active scene changes through `Module3DMain`.

Add EditMode characterization where possible for:

- visualization and column configuration objects;
- data parameter containers (`AnatomyDataParameters`,
  `DynamicDataParameters`, `FMRIDataParameters`, `MEGDataParameters`,
  `AtlasInfo`);
- pure calculations extracted from scene managers.

### 8. Cuts and Triangle Erasing

Goal: cut planes, site-centered cuts and triangle erasing remain stable.

Implemented on 2026-07-01 with EditMode and PlayMode coverage. The PlayMode
tests use isolated Module3D scene harnesses for cut and triangle-erasing command
paths so they do not depend on the full MNI/`InitializeAsync` loading pipeline.
Native full-scene cut-around-site integration should be reintroduced only with a
stable real-fixture policy.

Added PlayMode tests for:

- add, remove and update cut plane;
- cut mode toggles the expected scene state;
- cut color changes update materials/preferences;
- cut-around-site toolbar state marks the scene cut state dirty;
- cut parameters UI writes back to scene state;
- triangle erasing mode toggles;
- reset, cancel and mask reload mutate only the target mesh state;
- expand and invert toolbar interactability is covered without invoking the
  native ray-based erasing path from a synthetic zero-ray harness;
- saved triangle-erasing state can be loaded back.

Added lower-level tests for:

- `Cut` geometry/configuration serialization;
- `CutTexturesUtility` deterministic behavior on synthetic inputs;
- cut plane fallback behavior when no native brain surface is available.

### 9. Toolbar Coverage

Goal: each toolbar control has at least one behavioral test proving the command
changes the intended state.

Current status:

- Phase 9 PlayMode coverage added for isolated toolbar command behavior in
  `HBP.Toolbar.PlayModeTests`. The synthetic toolbar scene harness now covers
  display controls (auto-rotate, camera type and reset-view events), scene
  display controls (brain color, colormap, transparent brain, edge mode, cut
  mode and cut color), site controls (selected site label, show-all-sites,
  blacklisted-site display, site gain and cut-around-site), activity controls
  (compute request, stale-generator reset, global toggle, transparency, static
  label selection and site-correlation gating), CCEP mode/source selection,
  timeline play/loop/slider/step/record/global behavior, ROI manager
  add/rename/remove plus ROI export interactivity, brain selector and left/right
  mesh toggles, MRI/implantation selector status, view/standard-view
  availability, screenshot availability, export-activity availability,
  configuration/copy command availability, atlas unavailable-state gating plus
  IBC/DiFuMo/localizer selector gating, load-patient gating, interactive-viewer
  availability, site filters/open-site-tools availability, move-site reset, and
  site-state import/export availability including CSV import into selected-only
  and all-column modes. Additional safe-state coverage gates DefaultView,
  fMRI/MEG selectors, DynamicParameters for iEEG/static columns, FMRIParameters
  when unavailable, MRIContrast, CCEP MarsAtlas area selection,
  FMRIAtlasParameters, LocalizersParameters, LocalizersTimeline and
  TriangleErasingLoaderSaver safe availability. Other triangle-erasing toolbar
  controls remain covered by the Phase 8 cut/triangle-erasing PlayMode tests.
  Commands that leave the isolated scene harness now route through
  `ToolbarExternalActions`, which keeps default runtime behavior unchanged
  while allowing PlayMode tests to click the command path without opening
  native file browsers, global windows, loading UI, native viewers or
  screenshot/video exporters.
- Unity MCP validation on 2026-07-02: `HBP.Toolbar.PlayModeTests` passed 16/16
  in PlayMode with a clean post-run error console.
- Adapter-backed smoke clicks cover ROI import/export, site-state import/export,
  triangle-mask save/load, configuration load selector, export-activity window
  opening, screenshot/video requests, site-correlation compute/load,
  load-patient request, interactive-viewer URL generation, site-filter window
  opening and site-tools window opening. The tests intentionally do not invoke
  the real OS/native UI endpoints.
- Coverage audit verdict: isolated PlayMode coverage now exercises all toolbar
  `Tool` classes either in Phase 9 or in earlier focused toolbar tests
  (CompareSite in Phase 3, triangle-erasing mode/reset/cancel/invert/expand in
  Phase 8). Phase 9 is considered complete for automated toolbar behavior
  coverage; native OS/browser/window integration remains a manual or dedicated
  integration-test concern, not a unit/isolated PlayMode requirement.

Toolbar areas to cover:

- Activity: compute activity, global activity toggle, transparency, static
  label, fMRI/MEG selectors, CCEP source/mode selectors, site correlations,
  export activity to NIfTI.
- Atlas: atlas state, DiFuMo/IBC/localizer selectors, localizer timeline and
  parameters, fMRI atlas parameters.
- Configuration: copy visualization, save/load configuration.
- Display: default view, views, standard views, reset views, camera types,
  auto-rotate, screenshot.
- ROI: ROI manager and export.
- Scene: brain selector, brain meshes, brain color, transparent brain, edge
  mode, colormap, MRI selector/contrast, implantation selector, cut mode and
  cut color.
- Site: selected site, show all sites, site filters, site gain, move sites,
  load patient, compare site, blacklisted sites display, open site tools,
  open interactive viewer, cut around site, site state export.
- Timeline: play, loop, slider, step, record, global/sub timeline behavior.
- Triangle erasing: mode, expand, invert, reset, cancel, loader/saver.

Implementation pattern:

1. Instantiate a minimal scene harness with the toolbar and fake/synthetic scene
   state.
2. Trigger the tool through its public method or Unity UI event.
3. Assert the scene/configuration/preference side effect.
4. Keep one full UI click-path smoke test per toolbar group after the lower-level
   tests exist.

### 10. Information Panels, Graphs and Trial Matrices

Goal: graphs and trial matrices are covered both as data products and as UI
rendering surfaces.

Current status:

- `Phase10InformationDataTests` covers curve data creation, shaped curve data
  from non-array enumerables, graph/trial matrix preference clone and JSON
  round-trip, trial matrix data struct equality, missing-channel fallback
  sub-blocs, populated iEEG and CCEP trial matrix data from injected synthetic
  epoch caches, sub-bloc filler state, and serialized prefab wiring for the
  information graph/trial matrix rendering surfaces, graph settings window,
  trial matrix explorer window and site-tool graph/trial-matrix actions.
- `Phase10InformationGraphPlayModeTests` covers graph UI data conversion through
  `StructWrapper`, nested legends, enabled/disabled curve filtering, synthetic
  `Graph` curve state, CSV export, SVG export smoke output, and localizer graph
  worker empty-selection returns for voxel and region modes without requiring a
  selected Module3D scene. It also covers synthetic localizer graph generation
  for voxel, region and atlas modes, including mask filtering, rescaling,
  region/atlas mean values and SEM output without loading native NIfTI volumes.
  It also covers `GraphsGrid` creation from a synthetic
  iEEG column/channel, graph selection, display requests and filter requests,
  synthetic `TrialMatrixGrid` rendering of channels/blocs/sub-blocs/cell
  textures/selection masks, and `GraphZone` curve generation from trial matrix
  selections with live curve updates when selected trials change. It also covers
  `TrialMatrixZone` building visible-column grid data and preserving custom
  display limits across redisplay, plus trial matrix explorer rendering of
  titled synthetic matrix data and patient/site information panels honoring tag
  display settings. It also covers the site-tools "display graph" action
  publishing the requested graph name and filtered site set to the selected
  Module3D scene, the selected-site "open trial matrix explorer" action passing
  filtered sites and selected data into the explorer, the trial matrix explorer
  context action error path when no current patient is selected, and Graph
  settings localizer rescaling controls updating parameters, formula text and
  invalid gain fallback.
- `ShapedCurveData` accepts any `IEnumerable<float>` shapes collection instead
  of only preserving arrays. `Graph` and `StructWrapper` now initialize their
  serialized UnityEvents so dynamically created information graph components do
  not log null-reference exceptions on validation or event emission. `SimpleGraph`
  now follows the same event-initialization pattern for dynamic graph-grid items.
  Trial matrix UI components now initialize serialized UnityEvents and ignore
  selection validation until trial selection data exists, so focused runtime
  harnesses can instantiate them safely outside prefabs. The Graph settings
  window prefab now assigns the `LocalizersPanel` generate button field.
  `LocalizersGraphsWorker` now exposes protected data-access hooks so the same
  runtime behavior can be covered with synthetic localizer and atlas data
  without invoking native volume loading in PlayMode tests.
- Unity MCP validation on 2026-07-02: `HBP.Serialization.Tests` passed 128/128,
  `HBP.UI.PlayModeTests` passed 25/25, and the post-run console error check was
  clean apart from existing Unity serialization analyzer warnings.
- Remaining Phase 10 coverage: none identified; future additions should follow
  the same focused synthetic-data pattern.

Add EditMode tests for:

- `Data.Informations.Graph` curve data creation;
- shaped curve data and color/group settings;
- localizer graph worker pure data helpers when they are split from the PlayMode
  worker harness;
- trial matrix grid data equality and grouping;
- CCEP and iEEG trial matrix data cases;
- graph/trial matrix preferences round-trip.

Add PlayMode tests for:

- graph zone displays one or more synthetic curves;
- simplified graph grid creates, reorders and removes graph items;
- graph settings window updates preferences;
- site action "display graph" opens/updates the expected graph;
- trial matrix zone/grid renders expected channels/blocs/cells;
- trial matrix explorer filters, tag display settings and context actions;
- opening trial matrix from selected site.

### 11. UI Windows and Main Workflows

Goal: UI-level regressions are caught without requiring exhaustive brittle
pixel tests.

Current status:

- Project lifecycle decisions and project-window prefab smoke coverage already
  protect create/open/save/save-as through the phase 2 PlayMode tests and the
  extracted `ProjectWorkflowService` EditMode tests.
- `Phase11MainWorkflowPlayModeTests` adds focused PlayMode workflow coverage for
  patient, group, protocol, dataset and visualization gestion windows, including
  add/edit/remove commits to the loaded project or database model. The protocol
  workflow waits for the saved synthetic `.prov` file before loading a project,
  avoiding accidental Module3D reload paths in a management-window test.
- The same Phase 11 PlayMode file covers all current visualization column types
  from synthetic project data, user/project preference windows, database
  references and workspace settings.
- Module3D open/close/reopen lifecycle behavior remains covered by the phase 7
  Module3D PlayMode tests rather than duplicated in the main workflow assembly.
- Unity MCP validation on 2026-07-02: `PlayMode.Phase11` passed 3/3 with a
  clean post-run error console, and `HBP.Workflow.PlayModeTests` passed 16/16.
  The full workflow run still emits the expected phase 2 invalid-project JSON
  exception that is asserted with `LogAssert`.

Add PlayMode tests for high-value workflows:

- create a new project with synthetic defaults;
- open a synthetic project archive;
- save and save-as project;
- create/edit/delete patient, group, protocol, dataset and visualization;
- create each visualization column type;
- edit project and user preferences;
- configure database references and workspaces;
- open Module3D scene from a visualization;
- close scene and reopen without leaking state.

Prefer assertions on model state, instantiated components and window state.
Use screenshots only for smoke checks or visual regressions that cannot be
expressed as state.

### 12. Asset, Scene and Prefab Integrity

Goal: broken serialized references are caught before runtime tests fail
indirectly.

Current status:

- `UnityAssetIntegrityEditModeTests` now covers the phase 12 asset-integrity
  surface in EditMode. It checks project prefabs and the main scene for missing
  scripts, verifies that the main scene exists and is enabled in
  `EditorBuildSettings`, and asserts bootstrap components in the scene.
- The same suite verifies that critical Module3D, toolbar, graph, trial matrix
  and main workflow window prefabs load with the expected components, and that
  bootstrap serialized references are assigned for `Module3DMain`,
  `ToolbarMenu`, `ToolbarSelector`, `WindowsManager`, `DialogBoxManager`,
  `LoadingManager` and `MainMenu`.
- Rendering resource checks now lock the current shader, material, colormap,
  icon and shared-material assets that the Module3D/UI surfaces depend on.
- Assembly definition checks make EditMode/PlayMode test boundaries explicit:
  `HBP.Serialization.Tests` stays UI-free, the project workflow EditMode test
  assembly intentionally references UI runtime, PlayMode test assemblies remain
  PlayMode-runnable, and runtime asmdefs do not reference test assemblies.
- Unity MCP validation on 2026-07-02: the targeted
  `UnityAssetIntegrityEditModeTests` run passed 8/8, then
  `HBP.Serialization.Tests` passed 133/133 with a clean post-run error console.

Add EditMode tests for:

- main scene path exists and is listed where expected;
- Module3D, toolbar, graph, trial matrix and main window prefabs load;
- required components are present on prefabs;
- serialized fields required for bootstrapping are assigned;
- materials, shaders, colormaps, TMP assets and icons referenced by prefabs
  exist;
- asmdef references match the intended layering;
- no test assembly references UI or PlayMode-only assemblies accidentally.

## Milestones

### Milestone 0: Existing Serialization Safety Net

Status: stabilized for the current EditMode suite.

Done when:

- [x] current `HBP.Serialization.Tests` pass through Unity MCP;
- [x] synthetic project factory covers the current core persisted object
  categories used by serialization tests;
- [x] path and preference redirection scopes are reliable;
- [x] archive save/load/re-save tests exist for minimal and complete synthetic
  projects.

### Milestone 1: Complete EditMode Characterization

Focus:

- core objects;
- preferences;
- tags and filters;
- protocols/datasets/data containers;
- BIDS/localizer helper logic;
- graph/trial matrix data;
- asset integrity.

Done when:

- all persisted model variants have round-trip tests;
- all data container metadata variants have tests;
- synthetic BIDS/localizer fixtures exist;
- asset integrity failures point to the broken prefab/field directly.

### Milestone 2: First PlayMode Harnesses

Focus:

- minimal scene harness;
- Module3D scene lifecycle;
- view/camera/column creation;
- cuts and selection;
- toolbar command side effects.

Done when:

- a synthetic visualization can open in PlayMode without the full application
  scene;
- scene setup, cleanup and active scene switching are covered;
- at least one behavioral test exists for each toolbar group.

### Milestone 3: Information and Analysis Views

Focus:

- graph UI;
- simplified graph grids;
- trial matrix UI;
- site-to-graph and site-to-trial-matrix actions.

Done when:

- graph and trial matrix data tests cover representative iEEG/CCEP cases;
- UI PlayMode tests prove rendering containers populate from synthetic data;
- preferences changes update graph/trial matrix behavior.

### Milestone 4: End-to-End Workflow Smokes

Focus:

- project open/save;
- create/edit entities;
- open 3D visualization;
- toolbar usage;
- export request building.

Done when:

- a small set of PlayMode workflows covers the main user journey;
- full-scene tests are few, stable and named as smoke tests;
- failures identify which workflow step regressed.

### Milestone 5: Refactor Gate

Focus:

- use tests as a gate before moving namespaces, assemblies and responsibilities.

Done when:

- every module targeted by a refactor has tests at the closest practical layer;
- known uncovered behavior is tracked explicitly;
- native or external-data gaps have skip conditions and manual verification
  notes;
- CI can run fast EditMode tests on every change and slower PlayMode tests on
  scheduled or pre-refactor runs.

## Definition of Done for New Tests

A test is ready when:

- it uses synthetic/anonymized data;
- it can run repeatedly in a clean or dirty local Unity editor;
- it redirects user paths and global state;
- it cleans up static state, GameObjects, scenes and temporary files;
- it has a deterministic assertion on behavior;
- it fails with a clear message tied to one feature;
- it is in the lowest appropriate test layer;
- it is included in the documented MCP command or test assembly list.

## Known Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| EditMode tests accidentally depend on PlayMode lifecycle | Move to PlayMode or extract pure behavior before testing |
| Static managers leak state between tests | Use disposable test scopes and add reset helpers where missing |
| Tests use real preferences or paths | Redirect all paths through temp directories and assert no local path usage |
| Native DLL dependencies make tests platform-specific | Isolate native integration tests and skip with a clear reason when unavailable |
| UI tests become brittle | Prefer state assertions and limited full-click smoke tests |
| Full application scene tests are slow | Use harness scenes for most behavior and keep full-scene tests as smoke coverage |
| Legacy fixtures become unreadable | Keep small JSON fixtures with comments in test names and factory helpers |

## Coverage Tracking

Track progress with a simple table in this document or a follow-up issue list.

| Area | Current status | Target |
| --- | --- | --- |
| Serialization/core objects | Part 1 implemented; current manual EditMode XML validation passed full `HBP.Serialization.Tests` 40/40; core JSON round-trips, preferences, legacy `$type` fixtures, missing/unknown field compatibility, ID stability, clone isolation, `Copy` identity behavior, reflection audit for 515 JSON descriptors plus 376 lifecycle/BaseData descriptors, and PlayMode runtime save/load/corrupted-entry coverage are covered | Extend when new persisted types, fields or real legacy project files are discovered |
| Project archive lifecycle | Phase 2 core + workflow service implemented and validated on 2026-06-30: targeted Unity MCP EditMode run passed `HBP.Serialization.Tests` and `HBP.ProjectWorkflow.Tests` 93/93. Coverage includes `.hibop` save/load/re-save, archive entries, `ProjectInfo`, duplicate/missing IDs, corrupted settings and project-data entries, mutation reference cleanup, discovery by folder/ID, malformed archives, load/save cancellation at multiple phases, progress, overwrite cleanup, duplicate/invalid internal file names, settings version, temporary path leakage, alias/project-token paths, database-protocol archive references and extracted project workflow decisions for load/save/new/open/save-as/QuickStart. Phase 2 PlayMode smoke coverage added on 2026-07-01 for the New Project, Save Project As and Open Project prefabs. Native fixture project archives are generated by `NativeFixtureProjectGenerator` and loaded by `NativeFixtureIntegrationTests`. | Keep PlayMode project-window tests as smoke-only wiring checks; extend when archive semantics change |
| Patients/groups/sites | Phase 3 EditMode coverage added for patient/group/site models, site filters, selected-site/configuration state and synthetic CSV site import/tag generation. Phase 3 PlayMode coverage added on 2026-07-01 for Site Tools prefab change/copy/export behavior and CompareSite toolbar state through synthetic scene/UI harnesses; Unity MCP PlayMode run passed the local PlayMode assemblies 16/16. | Extend only when new site tools, filters or toolbar state transitions are introduced |
| Protocols/datasets/data containers | Phase 4 EditMode coverage added for basic/advanced protocols, all treatment types, dataset protocol/patient references, all current data info/container variants, alias path normalization and container error reporting. Phase 4 PlayMode coverage added on 2026-07-01 for complete-project save/load with all current data info/container metadata variants and Protocol/Dataset/DataInfo selector window prefabs; Unity MCP PlayMode run passed the local PlayMode assemblies 18/18. | Extend when new protocols, treatments, data info variants, container formats or selector display states are introduced |
| BIDS/localizer/database | Phase 5 EditMode coverage added for synthetic BIDS discovery/import, BIDS export config and generated TSV/JSON paths, missing metadata validation, database reference serialization for BrainVisa/Localizer/BIDS/Tags, and localizer protocol/data/bloc discovery without a 3D scene. Phase 5 PlayMode coverage added on 2026-07-01 for the Database Browser, BIDS export and Localizer atlas export window prefabs with seeded synthetic database state; Unity MCP PlayMode run passed the local PlayMode assemblies 21/21. | Extend when new database import/export workflows, export selections or database window states are introduced |
| DataManager/data processing | Phase 6 EditMode coverage added for `DataManager` load/unload/reload cache lifecycle, invalid data defaults, `Clear`, channel/event statistics, concurrent cached reads, processed iEEG unload cleanup and all iEEG normalization modes. Phase 6 PlayMode coverage added on 2026-07-01 for static CSV cache lifecycle, `Clear` across multiple loaded entries and invalid data defaults under isolated runtime scopes. Unity MCP EditMode run passed `HBP.Serialization.Tests` 104/104. `NativeFixtureIntegrationTests` adds native BrainVision, EDF, ELAN, FIF, Micromed and NIfTI fixture metadata coverage; the DLL-backed reader execution tests are `Explicit` and categorized `NativeDll`. | Extend when new native formats, statistics or normalization modes are introduced |
| Module3D scene/view/camera/columns | Phase 7 coverage expanded on 2026-07-01: EditMode `HBP.Serialization.Tests` passed 114/114 with visualization configuration/column variants, Module3D parameter containers, atlas metadata, `SceneInformation` invalidation cascades and Scene/View/Column/DisplayedObjects/manager prefab asset-contract tests; PlayMode `HBP.Module3D.PlayModeTests` passed 22/22 with controlled `Module3DMain` selected scene/column/view state, Base3DScene column aggregation/selection, visibility/focus events and export directory generation, Module3D selection events, Column3D state and configuration load/save, initialized scene configuration load/save, view-line removal fallback, initialized multi-column view-line/cut synchronization, View3D minimized/selection/camera-circle/viewport/render-target state, Camera3D zoom/strafe limits, standard/default views, camera type and auto-rotation propagation, ROI mask invalidation, compare-site manager state, TriangleEraser invisible-mesh toggle behavior, awaitable `CleanAsync` generated-object cleanup, non-blocking `CanNotLoadMNI` coverage and a positive synthetic OBJ-backed MNI/anatomy-column path for `InitializeAsync`. `NativeFixtureIntegrationTests` adds generated patient MRI, mesh, GIFTI, TRM and localizer discovery coverage. Local PlayMode assemblies passed 45/45 before native fixture expansion. | Extend when new MNI, mesh, MRI or column initialization behavior is introduced |
| Cuts/triangle erasing | Phase 8 coverage added on 2026-07-01: EditMode `HBP.Serialization.Tests` passed 117/117 with `Cut` JSON/runtime defaults and deterministic `CutTexturesUtility` synthetic-input behavior; PlayMode `HBP.Module3D.PlayModeTests` passed 28/28, including Phase 8 6/6 for isolated cut add/update/remove, cut toolbar mode/color/cut-around-site state, cut parameter UI writeback/removal, triangle-erasing mask load/reset/cancel target isolation, triangle erasing toolbar mode/degrees/reset/cancel/interactability, and no-native-surface cut fallback. Additional native-surface PlayMode coverage exercises cut geometry creation, ray-hit triangle erasing and native expand/invert mask modes without requiring a full activity-texture column. Local PlayMode assemblies passed 51/51 before native fixture expansion. | Extend when cut geometry or erasing algorithms change |
| Toolbar | Phase 9 complete on 2026-07-02: `HBP.Toolbar.PlayModeTests` passed 16/16 with display, scene, site, activity, CCEP, timeline, ROI, brain mesh/selector, MRI/implantation selector status, static labels, site-state CSV import, move-site reset, view/default-view/standard-view availability, atlas selector/parameter/timeline gating, site-correlation gating, configuration availability, triangle-mask load/save availability and adapter-backed smoke clicks for file-browser, selector, global-window, screenshot/video, native-viewer and loading command paths. | Direct OS dialogs are intentionally not automated; keep adapter smoke coverage unless platform-specific UI automation is explicitly required |
| Graphs/trial matrices | Phase 10 complete on 2026-07-02: `Phase10InformationDataTests` covers graph/trial-matrix data products, preferences, serialization and prefab contracts; `Phase10InformationGraphPlayModeTests` covers graph rendering/export, synthetic localizer graph generation, native NIfTI-backed voxel localizer generation, `GraphsGrid`, `TrialMatrixGrid`, `GraphZone`, `TrialMatrixZone`, trial matrix explorer rendering/actions, patient/site information panels and graph settings controls. Unity MCP validation passed `HBP.UI.PlayModeTests` 25/25 before native fixture expansion. | Extend when new information panels, graph renderers, localizer modes, trial matrix modes or export paths are introduced |
| Main UI workflows | Phase 11 PlayMode workflow coverage added on 2026-07-02: project lifecycle decisions remain covered by `ProjectWorkflowService` and phase 2 project-window smokes; `Phase11MainWorkflowPlayModeTests` covers gestion-window add/edit/remove commits for patients, groups, protocols, datasets and visualizations, all visualization column types, preference windows, database references and workspace settings. Unity MCP `PlayMode.Phase11` passed 3/3 with clean console, and `HBP.Workflow.PlayModeTests` passed 16/16 with the expected phase 2 invalid-project JSON log asserted by `LogAssert`. Module3D open/close/reopen remains covered by phase 7 Module3D lifecycle tests. | Extend only when new top-level windows or end-to-end workflows are introduced |
| Asset/prefab integrity | Phase 12 coverage added on 2026-07-02 in `UnityAssetIntegrityEditModeTests`: missing-script checks, main scene existence/build-settings/bootstrap components, critical Module3D/toolbar/graph/trial-matrix/main-window prefab component contracts, bootstrap serialized references, shader/material/colormap/icon/shared-material resources, and asmdef boundary checks. Unity MCP targeted integrity run passed 8/8; the latest full local MCP run passed EditMode 148/148. | Extend when new critical prefabs, resources, scenes or test assemblies are introduced |

Latest local MCP validation on 2026-07-02:

- EditMode: `HBP.Serialization.Tests` plus `HBP.ProjectWorkflow.Tests` passed 148/148.
- PlayMode: `HBP.Module3D.PlayModeTests` passed 28/28.
- PlayMode: `HBP.Toolbar.PlayModeTests` passed 16/16.
- PlayMode: `HBP.UI.PlayModeTests` passed 25/25.
- PlayMode: `HBP.Workflow.PlayModeTests` passed 16/16.
- The final PlayMode console contained the expected invalid-project
  `JsonReaderException` emitted by the phase 2 Open Project test and asserted
  with `LogAssert`; it is not a failing or unexpected console error.

## User Feature Traceability Matrix

This matrix is the source of truth for the current known user-facing feature
surface. A feature is considered covered when the listed automated tests prove
the intended user-visible behavior, not when every implementation line is
executed. Rows marked "Synthetic automated" are covered with deterministic
synthetic/anonymized fixtures. Rows marked "Smoke automated" cover wiring and
high-value user-visible state while relying on lower-level tests for detailed
behavior. Rows marked "Synthetic + native fixture automated" use deterministic
synthetic fixtures plus generated native files. Tests that execute the native
DLL reader layer are marked `Explicit` and categorized `NativeDll`, because a
failing native DLL can crash the Unity editor instead of throwing a managed test
failure. Direct operating-system dialogs are covered by adapter smoke tests only
because they are platform-shell behavior rather than deterministic application
logic.

| User-facing feature/workflow | Evidence | Status | Remaining non-automated scope |
| --- | --- | --- | --- |
| Create a new project with defaults and redirected user paths | `ProjectWorkflowServiceTests`; `Phase2ProjectWindowPlayModeTests`; `Phase11MainWorkflowPlayModeTests` | Synthetic automated | None identified for current project-window behavior |
| Open existing projects from the project list, including valid and invalid entries | `ProjectArchiveTests`; `ProjectWorkflowServiceTests`; `Phase2ProjectWindowPlayModeTests` | Synthetic automated | Native file-picker behavior is not exercised directly |
| Save, Save As, re-save and overwrite project archives | `ProjectArchiveTests`; `ProjectWorkflowServiceTests`; `Phase2ProjectWindowPlayModeTests`; `Phase11MainWorkflowPlayModeTests` | Synthetic automated | None identified for archive semantics |
| Load projects with progress, cancellation, malformed archives and corrupted entries | `ProjectArchiveTests`; `Phase1SerializationPlayModeTests`; `Phase2ProjectWindowPlayModeTests` | Synthetic automated | Direct automatic `Project.LoadAsync` coverage for every persisted child corruption remains scoped to safe non-blocking cases |
| QuickStart/extracted-project workflow decisions | `ProjectWorkflowServiceTests` | Synthetic automated | Full application-scene click path remains smoke-level only |
| Core persisted objects, preferences, legacy descriptors and migration compatibility | `CoreDataSerializationTests`; `ClassLoaderSaverTests`; `LegacyProjectCompatibilityTests`; `SerializationContractAuditTests`; `ProjectArchiveTests` | Synthetic automated | Add real legacy project archives when discovered |
| Patient, group, site, tag, filter and selected-site state management | `PatientsGroupsTagsSitesTests`; `Phase3SiteToolsPlayModeTests`; `Phase11MainWorkflowPlayModeTests` | Synthetic automated | Extend when new site filters or state transitions are introduced |
| Site Tools change/copy/export operations | `Phase3SiteToolsPlayModeTests`; `PatientsGroupsTagsSitesTests` | Synthetic automated | Native destination picker/export shell behavior remains outside the automated harness |
| Compare-site toolbar selected-site behavior | `Phase3CompareSiteToolbarPlayModeTests`; `Phase7Module3DScenePlayModeTests` | Synthetic automated | None identified for current synthetic scene state |
| CSV site-state import and generated tag behavior | `PatientsGroupsTagsSitesTests`; `Phase9ToolbarCoveragePlayModeTests` | Synthetic automated | Real user file-picker integration is adapter/smoke only |
| Protocol creation, treatment variants and protocol selector UI | `ProtocolDatasetDataContainerTests`; `Phase4ProtocolDatasetUiPlayModeTests`; `Phase11MainWorkflowPlayModeTests` | Synthetic automated | Extend when new protocol/treatment variants are added |
| Dataset creation, protocol/patient references and dataset selector UI | `ProtocolDatasetDataContainerTests`; `Phase4ProtocolDatasetPlayModeTests`; `Phase4ProtocolDatasetUiPlayModeTests`; `Phase11MainWorkflowPlayModeTests` | Synthetic automated | Extend when new dataset display states are added |
| Data info/container metadata, alias paths and container error reporting | `ProtocolDatasetDataContainerTests`; `Phase4ProtocolDatasetPlayModeTests`; `NativeFixtureIntegrationTests` | Synthetic + native fixture automated | Extend when new data containers are added |
| DataManager cache lifecycle, unload/reload, clear and invalid data defaults | `DataLoadingProcessingCacheTests`; `Phase6DataManagerPlayModeTests` | Synthetic automated | None identified for synthetic/static data paths |
| Channel/event statistics and iEEG normalization modes | `DataLoadingProcessingCacheTests`; `NativeFixtureIntegrationTests` | Synthetic + native fixture automated | DLL-backed native reader assertions are `Explicit` and categorized `NativeDll` |
| Processed iEEG unload cleanup and concurrent cached reads | `DataLoadingProcessingCacheTests` | Synthetic automated | None identified for current cache behavior |
| BIDS discovery/import/export configuration and generated sidecar paths | `BidsLocalizerDatabaseWorkflowTests`; `Phase5DatabaseWindowsPlayModeTests` | Synthetic automated | Real external dataset scale/performance remains fixture-dependent |
| Localizer protocol/data/bloc discovery and atlas export window behavior | `BidsLocalizerDatabaseWorkflowTests`; `Phase5DatabaseWindowsPlayModeTests`; `Phase10InformationGraphPlayModeTests`; `NativeFixtureIntegrationTests` | Synthetic + native fixture automated | DLL-backed native reader assertions are `Explicit` and categorized `NativeDll` |
| Database references for BrainVisa, Localizer, BIDS and Tags | `BidsLocalizerDatabaseWorkflowTests`; `Phase5DatabaseWindowsPlayModeTests`; `Phase11MainWorkflowPlayModeTests` | Synthetic automated | Real external application launch/browser integration remains manual or adapter-smoke |
| Visualization configuration and all current visualization column types | `Phase7Module3DConfigurationTests`; `Phase11MainWorkflowPlayModeTests`; `UnityAssetIntegrityEditModeTests` | Synthetic automated | Extend when new column types are introduced |
| Module3D scene creation, selected scene/column/view state and lifecycle | `Phase7Module3DScenePlayModeTests`; `Module3DPlayModeArchitectureTests`; `Phase11MainWorkflowPlayModeTests` | Synthetic automated | Full main-scene open/close is intentionally not duplicated beyond smoke/lifecycle tests |
| Base3DScene column aggregation, selection, visibility and focus events | `Phase7Module3DScenePlayModeTests` | Synthetic automated | None identified for synthetic scene behavior |
| View3D camera state, standard/default views, zoom/strafe and auto-rotation | `Phase7Module3DScenePlayModeTests`; `Phase9ToolbarCoveragePlayModeTests` | Synthetic automated | Real rendering fidelity is not visually snapshotted |
| Scene/view/column/displayed-object prefab contracts and configuration persistence | `Phase7Module3DConfigurationTests`; `Phase7Module3DScenePlayModeTests`; `UnityAssetIntegrityEditModeTests` | Synthetic automated | Extend when prefab contracts change |
| MNI/anatomy-column initialization and no-load failure paths | `Phase7Module3DScenePlayModeTests`; `NativeFixtureIntegrationTests` | Synthetic + native fixture automated | Full MNI bundle is not tested separately because it uses the same NIfTI/GIFTI/TRM loader paths |
| ROI mask invalidation and compare-site manager state | `Phase7Module3DScenePlayModeTests`; `Phase9ToolbarCoveragePlayModeTests`; `NativeFixtureIntegrationTests` | Synthetic + native fixture automated | Extend when atlas/MRI masking behavior changes |
| Cut creation, update, remove, color and cut-around-site toolbar state | `Phase8CutsTriangleErasingTests`; `Phase8CutsTriangleErasingPlayModeTests`; `Phase9ToolbarCoveragePlayModeTests` | Synthetic + native surface automated | Site-centered cut behavior is covered in the synthetic harness; native-surface coverage is limited to cut geometry creation |
| Triangle erasing mask load/reset/cancel, mode, degrees, expand/invert and toolbar commands | `Phase8CutsTriangleErasingPlayModeTests`; `Phase9ToolbarCoveragePlayModeTests` | Synthetic + native surface automated | Extend when native erasing algorithms change |
| Toolbar display/scene/site/activity/CCEP/timeline/ROI groups | `Phase9ToolbarCoveragePlayModeTests`; `ToolbarPlayModeArchitectureTests` | Synthetic automated | Extend when toolbar groups or gating rules change |
| Toolbar file-browser, selector, global-window, screenshot/video, native-viewer and loading command paths | `Phase9ToolbarCoveragePlayModeTests` | Smoke automated | Direct native OS/browser/window dialogs are intentionally not automated on the cross-platform test harness |
| Graph data creation, graph/trial-matrix preferences and serialization | `Phase10InformationDataTests`; `UnityAssetIntegrityEditModeTests` | Synthetic automated | Extend when data shapes or preferences change |
| Graph rendering, curve filtering, nested legends, CSV/SVG export and graph zones | `Phase10InformationGraphPlayModeTests` | Synthetic automated | Pixel-perfect visual export comparison is not covered |
| Localizer graph generation for voxel, region and atlas modes | `Phase10InformationGraphPlayModeTests` | Synthetic + native fixture automated | Native NIfTI-backed PlayMode coverage is `Explicit` and categorized `NativeDll` |
| GraphsGrid creation, selection, display and filter requests | `Phase10InformationGraphPlayModeTests`; `UnityAssetIntegrityEditModeTests` | Synthetic automated | None identified for current graph-grid behavior |
| TrialMatrixGrid, TrialMatrixZone, trial selection masks and live curve updates | `Phase10InformationGraphPlayModeTests`; `UnityAssetIntegrityEditModeTests` | Synthetic automated | Extend when new matrix modes or cell renderers are introduced |
| Trial matrix explorer, context actions and patient/site information panels | `Phase10InformationGraphPlayModeTests` | Synthetic automated | None identified for current synthetic matrix data |
| Graph settings localizer rescaling controls, formula text and invalid gain fallback | `Phase10InformationGraphPlayModeTests` | Synthetic + native fixture automated | Extend when localizer graph settings change |
| Main gestion windows add/edit/remove commits for patients, groups, protocols, datasets and visualizations | `Phase11MainWorkflowPlayModeTests` | Synthetic automated | Extend when top-level gestion workflows are added |
| Preference windows, database references and workspace settings | `Phase11MainWorkflowPlayModeTests` | Synthetic automated | Native OS settings dialogs are not part of the current harness |
| Main scene, build settings, bootstrap components and serialized bootstrap references | `UnityAssetIntegrityEditModeTests` | Synthetic automated | Extend when scene/bootstrap ownership changes |
| Critical prefabs for Module3D, toolbar, graph, trial-matrix and main windows | `UnityAssetIntegrityEditModeTests`; phase-specific PlayMode tests | Synthetic automated | Extend when new critical prefabs are introduced |
| Shader, material, colormap, icon and shared-material resources | `UnityAssetIntegrityEditModeTests` | Synthetic automated | Visual correctness of the assets is not image-compared |
| Test assembly boundaries and missing-script detection | `UnityAssetIntegrityEditModeTests` | Synthetic automated | Extend when asmdef/test assembly topology changes |

Current traceability conclusion: the automated suite covers all known
top-level user-facing feature families with deterministic synthetic, smoke,
native fixture or native-surface tests. Direct OS dialogs remain intentionally
adapter-smoke only, and DLL-backed native reader tests are `Explicit` and
categorized `NativeDll` to avoid editor crashes during standard suite runs.
