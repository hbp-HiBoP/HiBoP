# Development architecture

Reference consolidated on November 2, 2026. It describes responsibilities observable in the code, modification constraints and boundaries that remain coupled. Earlier decomposition proposals do not constitute an approved roadmap.

## Assemblies and dependency direction

The `.asmdef` files are the source of truth. Allowed direct HBP dependencies are versioned in [assembly-dependency-policy.json](../../Tools/assembly-dependency-policy.json). The [check-assembly-dependencies.ps1](../../Tools/check-assembly-dependencies.ps1) gate checks references and rejects cycles or unapproved new edges. During this consolidation, the static gate passed for **44 HBP assemblies and 164 direct edges**; no Unity tests were run.

| Boundary | Current responsibility and constraints |
|---|---|
| `HBP.Core.Runtime` | Models, configuration, serialization, preferences, data management and native wrappers. Foundation with no references to any other `HBP.*` assembly. It nevertheless uses Unity, UniTask and TextMeshPro: it is not a Unity-independent domain. |
| `HBP.Data.Runtime` | Data orchestration, Module3D, graphs and BIDS. Depends on Core, Input, Rendering and Theme. The name Data therefore does not mean "models only". |
| `HBP.Input.Runtime` | Unity Input System input adaptation. |
| `HBP.Rendering.Runtime` | URP infrastructure, dependent on the Render Pipelines Core and Universal packages. Scientific models remain with their existing owners. |
| `HBP.Theme.Runtime` | Shared theme and presentation; depends on Core and Input. |
| `HBP.UI.Runtime` | Windows, controllers and user workflows; composes Core, Data, Rendering, Theme, Input, Transfer and Sync. |
| `HBP.Sync.Runtime` / `HBP.Sync.Scene` | Synchronization contracts and mechanisms / scene adaptation. Runtime depends only on Core among HBP assemblies; Scene also connects to Data and the Transfer adapters. |
| `HBP.Transfer.Codecs`, `.Transport`, `.Scene` | Encoding, transport, then scene adaptation. Transport depends on Core, Codecs and Sync.Runtime; Scene also depends on Data. |
| `HBP.Quest.Runtime` | Standalone Quest composition, UI, XR, rendering, transfer and synchronization. Its dependencies must not flow back into Core. |
| Editor, Dev and Tests assemblies | Editor/build tools, instrumentation and targeted evidence. Their broader references do not justify the same dependencies in the product runtime. |

`noEngineReferences: true` in some Transfer/Sync assemblies describes their own references; their dependency on Core does not establish a Unity-independent transitive dependency closure. The Anatomy/Projection support assemblies and test utilities have their own boundaries in the `.asmdef` files and policy, without being new product entry points.

Before changing a reference, inspect **direct and transitive** dependencies, justify the edge, then run the static gate. A higher-layer feature must observe Core through a domain-neutral event or port. Moving its specialized diagnostics, state or interfaces into Core to bypass this rule would violate the boundary.

## Orchestration and ownership

[ApplicationState](../../Assets/Scripts/HBP/Core/Tools/ApplicationState.cs), `PersistentDataManager`, `DatabaseManager`, `DataManager` and `Module3DMain` remain shared state entry points. They provide access to the current project, paths, preferences, caches and scenes; a test or asynchronous process must make explicit what it borrows and restores. Data paths vary between Editor, Desktop and Android: do not replace ApplicationState properties with a machine-specific path or a relative path assumed to be common.

[DataManager](../../Assets/Scripts/HBP/Core/Data/DataManager.cs) manages caches, reads and data resources, with explicit synchronization. `Clear` and final cleanup are not interchangeable: clearing a project's data must allow the manager to be reused; the lifetimes of its locks and resources must remain consistent. Loading, operation sharing and publication rules are detailed in the [loading](loading/loading-and-persistence.md) domain references.

[Base3DScene](../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs) remains a substantial scene coordinator. Its partial files isolate configuration, operations, lifetime, density, atlases and transfer, among other concerns; they are not independent services. A scene owns its representations and temporary resources. Reference anatomy, displayed mesh and scientific fields have distinct lifetimes; see [projection](activity-projection.md), [inflation](surface-inflation.md) and [mesh](mesh-generation-and-cuts.md).

`SpecificSiteLocationFilterCondition.SceneLocationEvaluator` is still a static adaptation point between a model filter and the scene; its absence produces `false`. This boundary requires controlled installation and restoration, especially in tests. Domain callbacks and events are preferable to a Core reference to the scene or UI, but this observation does not authorize a general filter redesign.

Authored GameObjects are defined in prefabs with serialized references. Runtime construction is reserved for inherently dynamic objects. Windows and controllers still orchestrate some exports: the presence of this responsibility does not mean that the export services proposed in the earlier audit already exist.

## Persistence and project workflows

[ProjectArchiveReader](../../Assets/Scripts/HBP/Core/Data/Project/ProjectArchiveReader.cs) handles archive reading, while the `Project` model retains loading/saving operations. [ProjectWorkflowService](../../Assets/Scripts/HBP/UI/Tools/ProjectWorkflowService.cs) is an effective workflow boundary: confirmations, current project, location, module configurations, scenes and windows pass through controllable dependencies. This service makes cancellation and state restoration testable without opening the entire application.

`.hibop` archives, serialized member names, identities and references are compatibility contracts. Protocols are resolved in the protocol database and must not be recreated as standalone archive entries because an old plan suggested it. Portable paths and aliases must not expose temporary directories in saved data. Cancelling an open operation must not prematurely destroy existing scenes, windows and data; an error must preserve or restore a consistent state. Partially readable archives may be opened in read-only recovery under the current checks, without presenting that recovery as a healthy save.

Moving or renaming a type must account for historical `$type` values, serialization aliases, Unity GUIDs and prefab/scene references. A namespace change is therefore not merely cosmetic. The contract audits and compatibility fixtures in [testing](testing.md) protect this boundary; updating an expected fingerprint without analyzing the difference is not a migration.

## Decisions and limits

The earlier audit proposed a pure domain, a separate Module3D assembly, Persistence/BIDS/Localizer services and several controller extractions. These proposals do not describe the delivered architecture and are not carried forward as tasks to implement. Any future extraction must address a concrete change, preserve contracts and be justified by a testable boundary, without multiplying abstractions.

The historical findings "no tests", "Built-in/Post Processing Stack as the active renderer", "GeneratorSurface in production" and "UserSettings/generated solution versioned" are no longer a valid basis for decisions. Tests are divided by responsibility, the active renderer is URP, projection uses a volumetric field, and this consolidation's Git check found neither `HiBoP.slnx` nor `UserSettings` tracked.

Remaining coupling — Core's Unity dependency, global state, broad Data/UI coordination — is an observed characteristic, not evidence of a functional defect. To make a change: start from the behavior to modify, identify its owner and contracts, choose the narrowest evidence, then broaden only if a real dependency requires it.
