# Integration with the current HiBoP code

This is a source audit of `feature/xr-autonomous` on 2026-09-17. Paths are relative to the repository root. The existing scene and native calculations should remain the shared implementation on both devices.

## Current lifecycle and changes required

| Existing component | Current behavior | Integration point |
| --- | --- | --- |
| `Assets/Scripts/HBP/UI/Quest/QuestManager.cs` | Desktop owns pairing, reconnect heartbeat and explicit `SendAsync`. | After a successful scene delivery, bind the sent scene ID and establish a sync epoch/adapter. Keep discovery/pairing independent. Expose common/pending/visible status and conflicts. |
| `Assets/Scripts/HBP/Transfer/Scene/DesktopSceneCapture.cs` | Captures prepared resources, awaits the next frame, produces a whole scene and currently creates a fresh session GUID with revision 1. | Keep as initial/resource delivery. Add a separate small `DesktopStateCapture` for coherent live state. Never invoke full scene capture per gesture sample. |
| `Assets/Scripts/HBP/Transfer/Scene/ScenePayload.cs` | Contains `TransferId`, `SessionId`, `Revision`, model and resources. | Establish a real epoch/revision handshake after initial publication; do not assume the existing `Revision` is enforced by the receiver. |
| `Assets/Scripts/HBP/Transfer/Transport/QuestPairing.cs` | Authenticates a Quest-hosted listener and processes each accepted connection serially. | Add a bounded duplex channel with the same trust. Decouple accepts from long-lived sessions; serialize scene publication explicitly. |
| `Assets/Scripts/HBP/Quest/Runtime/QuestAnatomySession.cs` | Whole-delivery publication deduplicated by `TransferId`; `Disconnect` preserves the view, `CloseSession` removes it. | Retain the whole-delivery path for initial/replacement scenes. Add a separate replica owner with epoch/order, offline branch and visible-revision tracking. Network loss keeps that owner and scene. |
| `Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyView.cs` | `ApplyAsync` prepares a new `Base3DScene`, creates all `QuestColumnPresentation` wrappers at default positions and swaps it in. | Do not use this for normal updates. Reconcile existing `Base3DScene` and keep wrapper transforms by stable column ID. |
| `Assets/Scripts/HBP/Quest/Runtime/QuestColumnPresentation.cs` | Places a column under `scientificFrame`, with a local manipulator. | Keep local spatial transform here. It must never be serialized into `SharedVisualizationState`. |
| `Assets/Scripts/HBP/Data/Module3D/Base3DScene.Configuration.cs` | `CaptureConfiguration` reads live values; `LoadConfiguration` resets/recreates cuts/ROI and loads columns. | Reuse field mapping where correct, but create a non-destructive capture and state applier. Do not call `LoadConfiguration` for each update. |
| `Assets/Scripts/HBP/Data/Module3D/Base3DScene.Operations.cs` | Contains target-explicit selection, timeline, filters and site operations usable by both platforms. | Prefer these common operations from the applier; extend them where needed for stable IDs/atomic application rather than duplicating rules in Quest UI. |
| `Assets/Scripts/HBP/Transfer/Scene/SceneArchive*.cs` | Owns temporary resources and cleanup; some blocks are content-addressed. | Retain for delivery. Introduce a pinned editable-session resource lifetime for offline recovery; do not treat temporary archive ownership as durable storage. |

## Known gaps the inventory must close

1. `Visualization.Configuration` becomes current through `SaveConfiguration`/`CaptureConfiguration`, not on every setter (`Base3DScene.Configuration.cs:136-232`). Observe the live scene.
2. `VisualizationConfiguration` includes Desktop camera/view fields and `JsonIgnore` initial selections (`VisualizationConfiguration.cs:114-134`). Define scientific selection and local presentation separately.
3. Cuts are captured as value structs without persistent IDs (`Core/Data/Objects/Cut.cs`); `Base3DScene.cs:1558-1562,1596-1600` renumbers runtime IDs on structural changes. Introduce stable session IDs and a map to runtime objects.
4. `Column3DIEEG.cs:36-40` and `Column3DCCEP.cs:33-39` use navigation timeline copies. `DesktopSceneCapture.cs` exports processed data, so capture the **navigation** timeline from each live column. Include FMRI/MEG navigation and playback parameters.
5. `Base3DScene.Operations.cs:84-95,142-159` changes `SiteState.IsFiltered`; `Column3D.CaptureConfiguration` does not capture it. Include resulting inclusion by stable column/site identity.
6. `ROIManager.cs:26-55,170-198` uses selected ROI to compute masks. `Base3DScene.Configuration.cs:223-229` records ROI geometry but not active selection. Capture and apply both.
7. `Column3D.cs:568-589` moves site transforms in scientific coordinates without a corresponding site configuration field. Decide its canonical position representation and capture it.
8. `Base3DScene.Operations.cs:107-135` can compute/display correlations, while `Base3DScene.cs:497-509` holds display state. Inventory whether Quest can reproduce the result from transferred inputs or needs a prepared result.
9. `PairingContext.cs:119-122` rejects source protocol/tag definitions changed after pairing. Source editing is outside this sync session, but live scene features dependent on newly required resources need an explicit refresh/re-pairing error or versioned dependency transfer. Never let a state revision point to unavailable dependencies.
10. `QuestAnatomySession.cs:323-348` ignores payload `Revision` for ordering and caps delivery identities at 256. Do not implement one delivery per state change.

## Proposed assembly ownership

Place the detached state schema, validation, comparison/merge and wire codec in a small shared runtime assembly without a Quest UI dependency. Put live capture and apply adapters alongside `Base3DScene` in the common 3D runtime; they must compile for both builds. Desktop session activation belongs to `QuestManager` or a session owner it creates. Quest session/persistence/connection integration belongs beside `QuestAnatomySession`, but the receiver must not implement cut/ROI/timeline rules itself. Local UI on either side calls shared scene operations.

The final assembly references need a dependency check before files are created: `HBP.Transfer.Scene` already references `HBP.Data.Runtime`, while a state schema used by `HBP.Data.Runtime` must not create a circular assembly reference. A viable split is a pure state contract under `HBP.Core.Runtime` or a new lower-level assembly, with capture/apply adapters in `HBP.Data.Runtime` and transport adapters in `HBP.Transfer.*`. Choose the smallest split that satisfies Unity asmdefs.

## Activation hooks

Use `Module3DMain.OnRemoveScene` or an equivalent scene lifecycle signal to terminate or checkpoint an active epoch. Attach mutation notifications only to the scene that was sent, not whichever scene is currently selected later. The active adapter may use existing events where they accurately mark completion; add a narrow `MarkScientificStateChanged(group)` at mutation points lacking events. It must be guarded by an inactive-null observer so Desktop-only use allocates and computes nothing. A low-rate active-only canonical comparison is a safety net, not the main publication mechanism.

For Quest-first editing, the same capture/apply adapters need an injected edit origin and suppression scope: applying a remote accepted state must not be recaptured as a new local proposal. Conversely a local Quest edit must be captured after its common operation has produced its final canonical values, not from raw controller coordinates.

## Historical branch

`feature/xr` contains session epochs, revisions, idempotence and conflict gates under `Shared/Packages/com.crnl.hibop.*`. Its `CommandKind` names a finite set of actions and its earlier renderer/projection choices differ from the current shared-scene approach. Review its invariants selectively; do not merge the full branch or require an action-specific command for each Desktop control.
