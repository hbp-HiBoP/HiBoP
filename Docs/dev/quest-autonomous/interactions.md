# Quest interactions

Reference for the current code as of November 2, 2026. Ergonomic observations and measured performance remain dated results, described in [validation.md](validation.md) and the [selection laboratory](selection-lab-guide.md).

## Presentation and shared commands

Physical placement and scale of brains, windows and cursors belong to Quest. Selection, cuts and scientific parameters go through shared setters and commands. Manipulating a brain selects its scene/column context; it does not transport its room pose to Desktop. Locally grouped/separated modes and column changes are managed by [QuestAnatomyView.Presentation](../../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyView.Presentation.cs): captures are invalidated during transitions, without resetting other columns' scientific parameters. The [scene contract](shared-scientific-scenes.md) details visibility and selection.

[QuestPointerInput](../../../Assets/Scripts/HBP/Quest/Runtime/QuestPointerInput.cs), [QuestInteractionInput](../../../Assets/Scripts/HBP/Quest/Runtime/QuestInteractionInput.cs) and [QuestInteractionPolicy](../../../Assets/Scripts/HBP/Quest/Runtime/QuestInteractionPolicy.cs) centralize arbitration: index trigger for UI/anatomical manipulation, grip for cuts. A press captures a single target per hand until release. Tracking loss or invalidation ends the gesture; a new press is required. The default UI ray reaches 3 m, with proximity priority. Site selection uses its own physical profile described below, not the old 0.5 mm probe radius from early trials.

## Windows and signals

[QuestWindowsManager](../../../Assets/Scripts/HBP/Quest/Runtime/QuestWindowsManager.cs) instantiates catalog prefabs and prohibits two windows with the same key. [QuestWindow](../../../Assets/Scripts/HBP/Quest/Runtime/QuestWindow.cs) separates opening, pinning, hover and manipulation; closing hides content/raycast/canvas without interrupting the connection. Windows are **pinned by default**. When following is enabled, it respects hover and manipulation, with hysteresis; a recall places the window in front of the head.

Movement, resizing and content zoom are local operations. Resizing changes the `RectTransform` while preserving the opposite edge; it does not enlarge glyphs through GameObject scale. Prefabs and serialized references are the source of UI structure. Quest theme elements/states are specific to this presentation; common graphics parameters can be shared without coupling Desktop control state.

Signal panels use shared data and presentations, with their own computation and resource ownership. The Small acceptance procedure of October 28, 2026 provides a targeted observation of rendering in the headset; it does not qualify every protocol, every curve density or every usage duration. Earlier plans stating that the signal window or multi-scene context remain to be implemented are outdated.

Hiding the surface can retain sites and anatomical grabbing; this presentation mask is not the scientific mask. On Desktop, a pre-existing asymmetry remains visible in `Camera3D`: `HorizontalStrafe` multiplies speed by 0.2, `VerticalStrafe` by 1. This fivefold ratio does not come from the Input System migration; no correction is claimed here.

## Site selection

Selection sizes are expressed in **physical world meters**, independently of displayed anatomical scale. [QuestSelectionSettings](../../../Assets/Scripts/HBP/Quest/Runtime/QuestSelectionSettings.cs) defines Contact, Depth and Ray profiles. The current default is profile 5:

| Parameter | Value |
| --- | --- |
| Mode | Depth |
| Contact / marker radius | 3 mm / 1 mm |
| Front and back depth | 10 mm on each side |
| Attraction | Enabled, 3 mm radius |
| Stabilization | Enabled, score margin 0.15 |
| Cursor | White halo, 4 mm radius |
| Offset in the aim coordinate system | X=0, Y=0, Z=+30 mm |
| Visual cue | Name enabled, label scale 0.4; other visual cues disabled |

[QuestSiteAssistance](../../../Assets/Scripts/HBP/Quest/Runtime/QuestSiteAssistance.cs) prioritizes direct contact. Depth uses a capsule centered on the probe and oriented along the head–probe axis: the segment runs from −10 to +10 mm around the probe with the current profile, excluding rounded ends. Ray uses the bounded ray and optionally its attraction cone. A candidate is ranked by normalized distance, with a distance term and stable IDs for tie-breaking. The previous site is retained only if it remains inside the volume and score margin. **0.15 is a score margin**, not a probability, hold duration or error rate. There is no swept selection between frames or indefinite retention outside the volume. At 50 cm, a 1° half-angle adds approximately 8.7 mm to the radius: the setting must be interpreted physically.

Eligibility follows shared appearance: masked/invisible sites do not become new targets. An already retained canonical selection can remain selected after becoming invisible. Repeated names across patients are not identifiers. Shared routing and the boundary replicate a site intent consistent with its column; immediate presentation remains local, and the driver serializes proposals with a single latest deferred intent. Canonical checkpoints and corrections take precedence over old responses.

## Cuts

[QuestCutCommands](../../../Assets/Scripts/HBP/Quest/Runtime/QuestCutCommands.cs) modifies only the control's intent: each operation rereads current orientation, flip, position and normal to preserve fields modified elsewhere. A custom normal must be finite and nonzero; position is bounded to [0,1]. Commands go through `SetCutDefinition`, with stable cut identity and scene scope.

The window exposes cuts and can suspend aids without deleting data. Closing the window stops the gesture and aids. [QuestCutHandles](../../../Assets/Scripts/HBP/Quest/Runtime/QuestCutHandles.cs) captures the **bounded plane surface from either face**, using grip; it constrains movement to the anatomical normal. Proximity is measured from grip/aim and a single candidate receives capture. Profile thresholds: contact 35 mm, appearance 180 mm, disappearance 240 mm. These are not anatomical distances after enlarging the brain.

Two hands do not modify the same cut simultaneously. A grabbed cut locks its anatomical representation; a representation already being manipulated refuses to start this gesture. Deletion, closing, transition or an incompatible change invalidates capture. Guides and their pose remain local; the latest valid definitions are already published during the gesture, without republishing a frozen copy on release that would overwrite a remote change.
