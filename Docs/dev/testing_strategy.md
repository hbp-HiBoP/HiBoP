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

## Coverage Backlog By Feature Area

### 1. Serialization and Format Compatibility

Goal: every persisted object can round-trip and legacy files remain readable.

Current status:

- `HBP.Serialization.Tests` passes in EditMode through Unity MCP: 30/30 tests
  passing.
- `PersistentDataTestScope` initializes `PersistentDataManager` and
  `DatabaseManager` without invoking PlayMode-only `DontDestroyOnLoad`.
- Synthetic serialization fixtures use stable IDs and controlled
  `ApplicationState`/database context for project object references.
- JSON round trips currently cover `ClassLoaderSaver`, tags/tag values, nested
  filters, protocol, patient, dataset, visualization, all current synthetic data
  info/container variants, visualization columns and column base
  configurations and project/user/visualization preferences.
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

### 2. Project Archive and Project Lifecycle

Goal: `.hibop` projects remain loadable, saveable and internally consistent.

Initial tests:

- minimal project save/load preserves archive structure.
- complete synthetic project save/load preserves references and IDs.
- invalid extension, missing settings and malformed archive are rejected.
- re-saving a loaded project produces a valid archive.

Add tests for:

- project creation defaults;
- project preferences, aliases and extraction paths;
- patients, groups, protocols, datasets and visualizations stored in the
  expected archive folders;
- duplicate or missing IDs reported by `CheckProjectIDsAsync`;
- load cancellation and partial extraction cleanup;
- corrupted JSON entry reports a controlled error;
- archive does not leak absolute machine paths unless intentionally persisted.

### 3. Patients, Groups, Tags and Sites

Goal: patient metadata and site state survive editing and filtering.

Add tests for:

- patient creation with meshes, MRI, sites and tag values;
- group membership and patient references;
- site coordinates, labels, tags, selection flags, blacklisted state and gain;
- site filters by name, tag, data state, data type and scene location;
- CSV import/export for site attributes through a service-level boundary or a
  controlled UI PlayMode test;
- copy/change site attributes from the site tools window;
- selected site and compared site state.

### 4. Protocols, Datasets and Data Containers

Goal: protocol/dataset definitions and data references remain stable.

Add tests for:

- protocol creation with basic and advanced blocs;
- sub-bloc events, icons and treatments (`Abs`, `Clamp`, `Factor`, `Mean`,
  `Median`, `Min`, `Max`, `Offset`, `Rescale`, `Threshold`);
- dataset references to protocols and patients;
- data info variants: static, iEEG, CCEP, fMRI, shared fMRI, MEG;
- data container variants: CSV, BrainVision, EDF, Elan, FIF, Micromed, NIfTI;
- relative/absolute path normalization under redirected test roots;
- missing files and unsupported formats report predictable errors.

### 5. BIDS, Localizer and Database Workflows

Goal: import/export helpers can be refactored out of UI windows safely.

Add EditMode tests for:

- BIDS folder discovery with synthetic participants, sessions, modalities and
  sidecar files;
- BIDS export configuration serialization;
- generated TSV/JSON file names and paths;
- validation of missing required metadata;
- localizer export request building once extracted from UI;
- database reference serialization for BIDS, BrainVisa, Localizer and tags
  references.

Add PlayMode or workflow tests for:

- database browser opens a synthetic database reference;
- BIDS export window builds the expected export request;
- localizer export window handles protocol/data/bloc selections;
- workflows do not require an already opened 3D scene unless documented.

### 6. Data Loading, Processing and Caches

Goal: `DataManager` and data processing behavior can be changed without hidden
global-state regressions.

Add tests for:

- load/unload/reload cache lifecycle;
- `Clear` and `Dispose` reset all static dictionaries;
- normalization modes: none, sub-trial, trial, sub-bloc, bloc and protocol;
- channel statistics and event statistics with tiny synthetic arrays;
- concurrent reads do not mutate cached data unexpectedly;
- missing data returns controlled error/default behavior;
- native-DLL-dependent paths are marked separately and skipped when the
  dependency is unavailable.

### 7. Module3D Scene, Views, Cameras and Columns

Goal: the runtime 3D state can be protected before splitting `Base3DScene` and
`HBP.Data.Module3D`.

Add PlayMode tests for:

- creating a synthetic 3D scene from a synthetic visualization;
- `Base3DScene.InitializeAsync` reaches a loaded state with synthetic assets;
- `Clean` removes columns, cuts, generated objects and event subscriptions;
- adding/removing/selecting columns;
- column variants: anatomy, static, iEEG, CCEP, fMRI, MEG and dynamic;
- `View3D` creation, resizing, focus and visibility;
- `Camera3D` default view, standard views, camera type switching, auto-rotate
  and screenshot path generation;
- mesh/MRI/implantation/fMRI/atlas/ROI managers enable and disable the expected
  displayed objects;
- active scene changes through `Module3DMain`.

Add EditMode characterization where possible for:

- visualization and column configuration objects;
- data parameter containers (`AnatomyDataParameters`,
  `DynamicDataParameters`, `FMRIDataParameters`, `MEGDataParameters`,
  `AtlasInfo`);
- pure calculations extracted from scene managers.

### 8. Cuts and Triangle Erasing

Goal: cut planes, site-centered cuts and triangle erasing remain stable.

Add PlayMode tests for:

- add, remove and update cut plane;
- cut mode toggles the expected scene state;
- cut color changes update materials/preferences;
- cut around selected site creates the expected cut parameters;
- cut parameters UI writes back to scene state;
- triangle erasing mode toggles;
- expand, invert, reset and cancel erasing mutate only the target mesh state;
- saved triangle-erasing state can be loaded back.

Add lower-level tests for:

- `Cut` geometry/configuration serialization;
- `CutTexturesUtility` deterministic behavior on synthetic inputs;
- native cut generator unavailable path.

### 9. Toolbar Coverage

Goal: each toolbar control has at least one behavioral test proving the command
changes the intended state.

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

Add EditMode tests for:

- `Data.Informations.Graph` curve data creation;
- shaped curve data and color/group settings;
- localizer graph worker with synthetic localizer data;
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
| Serialization/core objects | Part 1 implemented: 30/30 `HBP.Serialization.Tests` passing; core JSON round-trips, preferences, legacy `$type` fixtures, missing/unknown field compatibility, ID stability, clone isolation, `Copy` identity behavior, and reflection audit for 515 JSON descriptors plus 376 lifecycle/BaseData descriptors covered | Extend when new persisted types, fields or real legacy project files are discovered |
| Project archive lifecycle | Started | Save/load/re-save, invalid archives, cancellation |
| Patients/groups/sites | Partial through serialization | EditMode plus site UI/tool PlayMode |
| Protocols/datasets/data containers | Partial through serialization | All protocol/data/container variants |
| BIDS/localizer/database | Not covered | Synthetic fixtures and export request tests |
| DataManager/data processing | Not covered | Cache lifecycle and normalization modes |
| Module3D scene/view/camera/columns | Not covered | PlayMode harness coverage |
| Cuts/triangle erasing | Not covered | PlayMode behavior and serialization |
| Toolbar | Not covered | One behavior per tool/group, smoke click paths |
| Graphs/trial matrices | Not covered | Data tests plus UI PlayMode rendering |
| Main UI workflows | Not covered | Focused PlayMode workflow smokes |
| Asset/prefab integrity | Started | All critical prefabs/scenes/assets |
