# Standalone HiBoP on Quest

Development reference checked against the code on November 2, 2026. Dated hardware results are grouped in [validation.md](validation.md); they do not constitute new qualification of the current code.

## Division of responsibilities

Desktop prepares and opens visualizations. Quest receives prepared data and locally uses the shared scene model, configurations, generators and rendering. A network interruption does not turn Quest into a video player: already published scenes remain local. Scene transport, mutation replication and project files are three distinct contracts.

The Unity repository contains Desktop and Quest profiles. `HBP.Core.Runtime` remains the foundation without dependencies on other `HBP.*` assemblies. Data owns scenes; Transfer their capture and restoration; Sync commands; Quest and UI platform adapters. Quest composition starts from [QuestBootstrap](../../../Assets/Scripts/HBP/Quest/Runtime/QuestBootstrap.cs) and [QuestAnatomySession](../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomySession.cs). Assembly and Unity workflow rules are in [AGENTS.md](../../../AGENTS.md).

| State | Owner and scope |
| --- | --- |
| Scenes, columns, patients, prepared data, cuts, selection, scientific parameters | Shared model; mutations replicated according to the [Sync contract](sync/synchronization-architecture.md). |
| Physical placement, brain size in the room, camera, windows, guides, controller feedback | Local Quest presentation. |
| MNI/atlas installation, caches and native handles | Owned local resources; a logical identity is not a universal fingerprint of all installed files. |
| Connection state and publications | Authenticated session, identified incarnations and deliveries; nothing inferred from the visualization name alone. |

The [scene/column reference](shared-scientific-scenes.md) details multi-patient scenes, single-patient views, columns and visibility. A multi-patient scene does not carry individual anatomies simply because they were preloaded on Desktop. Global protocol/tag definitions are transmitted during pairing, then their saves are synchronized during the session.

## Data and local computation

[SceneRestoration](../../../Assets/Scripts/HBP/Transfer/Scene/SceneRestoration.cs) reconstructs prepared data and private native resources, initializes the shared scene, applies its configuration and awaits `PrepareRenderingAsync` before publication. Derived results are rebuilt from data and parameters; delivery does not replace this preparation with a frozen image. Nor does it impose unrequested scientific computation to satisfy a performance measurement.

Current capture supports modalities represented by shared columns, their trials and prepared series. Code and restoration tests do not prove exhaustive hardware acceptance of every protocol. Raw EEG import on Quest, full Desktop editing and opening a portable project from the headset must not be inferred from scene transfer. A resource cache is not a session save; no general guarantee of preserving unsaved work after process termination is provided here.

Scientific coordinates are in millimeters. Wrappers at the `hbp_core` boundary perform the native/Unity coordinate conversion (X and winding) described in [the coordinate contract](../hbp_core_coordinate_system.md). Quest presentation then applies mm→m conversion and its local pose, without another native reflection. Matrices, normals, indices, linear colors and surface states must not undergo a second conversion during transport. Entry points: [ReferenceSystemConversion](../../../Assets/Scripts/HBP/Core/DLL/HbpCoreValueTypes.cs), [QuestColumnPresentation](../../../Assets/Scripts/HBP/Quest/Runtime/QuestColumnPresentation.cs).

`SpanMin/Middle/SpanMax` calibration applies to prepared data, without renormalization at the selected time point. For unaligned temporal navigation, surface and sites follow their shared rules: the site evaluates `TemporalSample`, the surface uses the projection index defined by its policy. Temporal interpolation alpha must not be confused with `ActivityAlpha`, which controls opacity. Scientific masks and presentation visibility are also distinct.

## Lifetime and concurrency

Unity objects, live-graph mutations and publication remain on the Unity thread. Workers receive detached data or explicitly retained resources. Cancelling a task does not interrupt a synchronous native call: joining the work precedes any destruction of files, buffers or handles.

GIFTI geometry and MarsAtlas reads share [Surface](../../../Assets/Scripts/HBP/Core/DLL/Surface.cs)'s static lock, because the parser has global state. This lock does not replace scientific job scheduling. [MRI3D](../../../Assets/Scripts/HBP/Core/Object3D/MRI3D.cs) serializes loading, lazy reading and cleanup of its volume; this protection does not exempt owners from draining their readers before closing.

See [transfer and Display](transfer.md), [interactions](interactions.md), [deployment](deployment.md) and [USB/network](usb-and-network-connectivity.md) for their contracts. Earlier prototype stages and task lists have been absorbed into these references; they do not constitute a current backlog.
