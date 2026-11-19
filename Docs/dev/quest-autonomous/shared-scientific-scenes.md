# Shared Desktop and Quest scientific scene

Desktop and Quest execute the same scientific model, `Base3DScene` / `Column3D`. The platform changes presentation and interactions, not the definition of a column or its computations. The [retained multimodal comparisons](../../../Validation/Quest/retained-results.md) document historical qualification and its limits. The delivery protocol is described in [transfer](transfer.md), mutations in [synchronization](sync/synchronization-architecture.md).

## Owners

| Element | Responsibility |
| --- | --- |
| [Base3DScene](../../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs) and its partials | Scientific model, initialization, resources, configurations, computations, closing and consistent capture. |
| [Column3D](../../../Assets/Scripts/HBP/Data/Module3D/Column3D.cs) and six column prefabs | Anatomy/density, iEEG, CCEP, MEG, fMRI and static; mutable scientific state specific to each column. |
| [DesktopScenePresentation](../../../Assets/Scripts/HBP/Data/Module3D/DesktopScenePresentation.cs) | Desktop views, cameras and wrappers; no requirement to create them for a Quest scene. |
| [QuestAnatomyView](../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyView.cs) | Shared content restoration and Quest presentation. Wrapper poses and their visual scale remain local. |
| [SceneRestoration](../../../Assets/Scripts/HBP/Transfer/Scene/SceneRestoration.cs) | Scene construction from prepared resources, full state and publication after preparation. |

The `Scene3DContent` prefab does not simulate a set of hidden Desktop windows. All six modalities remain accessible to shared code. Scientific coordinates, native data, meshes/cuts, ROI, sources and activity state depend on neither a Desktop camera nor a Quest pose.

Immutable prepared resources can be shared across columns. Mutable objects — selections, cuts, filters, masks, timelines and outputs — have an explicit owner; changing one column must not alter its neighbors through aliasing. Resource and column keys also serve delivery and operations, rather than transient Unity object names.

## Preparation, capture and publication

[Base3DScene.Transfer](../../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.Transfer.cs) exposes `CapturePreparedAsync`. Capture awaits initialization and anatomy, loads missing resources from the scene's **current lists**, awaits the surface barrier and relevant generators/correlations/colliders, then copies state on the Unity thread without an intervening mutation. The preparation lock protects the exact resource/representation choice; waiting only for the scene's first load is insufficient.

Delivery retains useful recordings, trials, statistics and resources of all columns; it does not change Desktop project persistence format. The delivered scene retains its prepared alternatives: mesh/hemisphere choices, MRI, data sources and column state. Restoration prepares before publication; an error does not publish partial content. Resending produces a new incarnation and terminates the old one before replacing the view. Live operations are not an implicit mechanism for shipping an unknown resource.

The initial experimental format described by earlier batches has evolved: refer to the codec and [transfer](transfer.md) for accepted versions. Likewise, typed operations have replaced the live snapshot from early trials; do not reimplement old adapters as a second scientific model.

## Asynchronous work and native lifetime

[Base3DScene.Lifetime](../../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.Lifetime.cs) is the closing boundary. Closing, replacing or cancelling prohibits new publication, then drains **all** work already started before releasing resources. Failure of the first awaited task does not authorize forgetting subsequent ones. Presentation coroutines, generators and late callbacks must no longer publish into a closed scene.

A cancelled token interrupts an intent or wait; it does not prove that a native call in progress has finished. Borrowed buffers and handles remain retained until actual return, including when a newer generation exists. Late results are discarded by identity/generation without freeing inputs still in use.

Sockets/.NET, transport continuations and shared barriers retain `Task` when that contract is required; Unity paths can use `UniTask` with explicit thread transitions. Consumer count alone does not justify a type migration. `Task` and `UniTask` are not mechanically interchangeable: shared work with multiple waiters must retain a representation that can be awaited repeatedly; a continuation accessing Unity must explicitly return to the main thread. Cancelling a single observer must not destroy shared work. PlayerLoop waits are asynchronous, without `.Wait()`, `.Result` or blocking loops. Observed issues and dated trials are in [validation](sync/synchronization-validation.md).

## Verifying a scene change

[SceneQualification](../../../Assets/Scripts/HBP/Transfer/Scene/SceneQualification.cs), [SceneQualificationDiagnostic](../../../Assets/Scripts/HBP/Dev/SceneQualificationDiagnostic.cs) and [QuestSceneDiagnostic](../../../Assets/Scripts/HBP/Quest/Runtime/QuestSceneDiagnostic.cs) export comparable scene data. `SceneRestorationPlayModeTests` generate their projects using [SyntheticSceneProjectFixture](../../../Assets/Tests/PlayMode/HBP.PlayModeTestUtilities/SyntheticSceneProjectFixture.cs), then directly run the Desktop diagnostic and restored scene. No old build or manually prepared `.artifacts` directory is needed. [Run-QuestSceneQualification.ps1](../../../Tools/Run-QuestSceneQualification.ps1) and [Compare-SceneQualification.py](../../../Tools/Compare-SceneQualification.py) remain reserved for explicitly selected hardware measurements and platform comparisons.

A useful comparison covers all six modalities in MNI and patient contexts, resource alternatives, geometric and scientific buffers, restoration and recomputation. Measure sizes/identities, maximum and RMS differences; do not invent a global tolerance from a visually correct capture. Unity memory sampled by these diagnostics is neither peak Windows RSS nor peak Android PSS. Artifacts must name the configuration, binaries and data actually used.

[SceneRestorationPlayModeTests](../../../Assets/Tests/PlayMode/HBP.Transfer.Scene.PlayModeTests/SceneRestorationPlayModeTests.cs) and [Module3DScenePlayModeTests](../../../Assets/Tests/PlayMode/HBP.Module3D.PlayModeTests/Module3DScenePlayModeTests.cs) supplement capture/restoration and lifetime tests. Their presence does not automatically qualify stereo rendering, interactions or Windows/ARM parity.
