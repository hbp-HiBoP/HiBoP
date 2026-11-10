# UI and UX conventions

Read for UI design or review, authored
controls, themes, Quest input, visible error handling and bug reporting.

## Authored controls

AGENTS owns prefab-first and direct prefab editing. Preserve manual asset edits.
Follow the nearest established control/window. Use `Toggle with text` for
comparable preferences. Static tooltips belong in the prefab. User-facing strings
are English; Loading Circle text omits trailing `...`, supplied by its animation.

Quest windows in the existing family are variants of
[`Quest Window.prefab`](../../Assets/Prefabs/Quest/UI/Quest%20Window.prefab).
Reuse its common mechanics and inspect a current sibling for references and
registration. Quest theme elements/states are separate from Desktop; reuse an
existing Quest role before adding one. Appropriate color settings may be shared,
while text/layout suit the headset. Icons are Image settings from the established
pictogram system. Use the UI authoring skill for the editing/verification procedure.

The Quest dialog box is an explicit exception to the window family: its authored
prefab is independent of `Quest Window`, while using the common dialog service.
Ordinary Quest windows start pinned; the Pin toggle uses its active theme color
(blue), rather than switching to a different icon.

Quest window placement is headset-local, keyed by the stable `QuestWindow.Key` in
`QuestWindowLayout.json` under `Application.persistentDataPath`; keep that key when
renaming a title. All ordinary windows use the common window/manager registration
and drag lifecycle, including future variants. Do not add per-panel persistence
or bypass the manager with head-relative placement when opening a window.
`QuestWindowsManager` restores positions and orientations relative to one fixed,
horizontal workspace frame. Initial placement waits for user presence, full head
tracking and settling; holding X establishes a new frame while retaining the
layout, including closed windows when they reopen. `QuestWindowLayoutStore` also
persists completed user resize dimensions, content zoom, pin state, open/closed
state and stacking order through the common window lifecycle, without saving
panel-specific content/layout. The version 2 format reads version 1 placements
with prefab defaults for the added properties. Restoring a closed window defers
zoom application until its layout can measure the unzoomed constraints. Settings
changes save independently of headset tracking and retain the last validated
workspace pose and resize dimensions; cancelled gestures and automatic following
never overwrite them.
Removing/resuming the headset does not automatically establish a new frame.
Transient dialogs keep their separate following behavior. Tests that instantiate
a manager must isolate `QuestWindowLayoutStore.DefaultPath` and explicitly
initialize the workspace when no real worn headset is present.

Quest signal graphs reuse the shared scientific graph widgets inside
`QuestSignalGraphView`; the color picker also reuses `DefaultColor` swatches.
These are intentional shared UI components, not Desktop window implementations.
Their Quest prefabs still use Quest theme elements/states, retaining shared color
and image settings where appropriate. Toolbar tooltip canvases follow their
window's sorting order and clear the full upper edge of the curved bar.
`QuestToolbarArcLayout` sizes and places active direct button children on the
curved toolbar in the editor and at runtime, including unit local scales. Add
buttons under its Background; a flat layout group would conflict with the arc.

Quest `Display Settings` controls the selected visualization through the shared
scene APIs. Its stacked sections are Scene (including colormap), Activity, Site
and Atlas. Atlas parameters are limited to IBC contrast and DiFuMo version/contrast;
Localizer is read-only on Quest. `Visualizations` retains the stable `Display`
window key and owns visualization management and Single/Split presentation;
projection and its removal belong to `DisplaySettings`. Its three color dropdowns
use authored Desktop palette sprites and fixed black titles; do not replace their
image options with text-only options at runtime. Their equal-width captions are
`Functional`, `Brain` and `MRI`, bold and without arrows. Compact toggle/slider
rows use 48-unit heights with centered content and 40-unit icons; keep the
64-unit rows and spacing for ordinary dropdowns and buttons.
Palette option toggles have no selection checkmark, so their textures remain
visible. Unavailable Atlas choices and the opacity slider use
`SelectableStateSetter` with the Quest NotInteractable state. The full-window
settings viewport is a transparent hit surface over the ordinary window body.
Quest dropdowns share the Desktop scrollbar image/color settings through Quest
theme roles, with a 12-unit track. Popup options delegate a primary-trigger drag
to their own ScrollRect after the pointer threshold; releasing a drag must not
select an option or close the list. A stationary click still selects normally.

## Input and shared behavior

Use [DesktopInput](../../Assets/Scripts/HBP/Input/DesktopInput.cs) for Desktop
New Input System access and its neutral behavior when a device is absent.
Use [QuestInteractionInput](../../Assets/Scripts/HBP/Quest/Runtime/QuestInteractionInput.cs)
and [QuestInteractionPolicy](../../Assets/Scripts/HBP/Quest/Runtime/QuestInteractionPolicy.cs)
instead of independent button bindings: primary uses the front/index trigger;
secondary uses the side grip. Cut manipulation uses secondary so it can coexist
with primary brain manipulation. For changes to business state, consult
[Runtime and state](runtime-and-state.md).

Quest ROI editing follows the Cuts window lifecycle. Its managed `ROI` window
shows the active ROI's spheres in each presented column; closing hides these
local aids while retaining the scientific ROI selection and masks. Its
Hide/Show guides button follows Cuts and hides the local spheres while suspended.
One grip follows the controller pose, rotating the center about the contact point
like a grabbed brain. A second fresh grip joins the same capture and scales the
sphere by the ratio of controller separation, around their midpoint like brain
manipulation. Prefer grip over aim whenever grip is already within contact and
freeze that choice per hand. Cage rotation stays local to the proxy; the shared
sphere still has only position and radius. Joining/releasing reanchors without
snapping; there is no resize mode. New Quest spheres have a 20 mm radius and start
at the first hit from the headset toward the selected displayed brain's center,
using that column's ray target and converting back through its scientific frame.
If no surface hit is available, use the brain center. Cage renderers draw above
ordinary windows, below loading/dialog overlays, retaining scientific depth
occlusion. The universal pointer arbitrates
cuts/ROI by contact distance, preferring ROI on equal contact, and freezes the
scene/column/ROI/sphere throughout capture. Copies in other columns share one
sphere lock. Scientific-input gestures reserve the existing sensitive-operation
scope until release/cancellation. Import/export remains Desktop-only; imported
ROI/sphere creation propagates through the ordinary shared mutations.
Cut and ROI edits share the reliable scene-object edit lane: one proposal in
flight, latest unsent sphere position/radius intent retained and rebased on the
confirmed fields/watermark. Preserve structural barriers and scientific sphere
identity. Sphere selection changes atomically without an intermediate None;
selected Quest spheres retain the common ROI material instead of a cut hover tint.
ROI sphere rows show their index, plain position/radius text and remove action;
Add sphere is the final list item. Quest input and placeholder text is vertically
centered while preserving its horizontal alignment.

Quest site selection uses right A; right B applies/reverts the local site preset.
`QuestSitePresetPanel` samples B in LateUpdate after pointer selection, allowing
A+B together without mutual exclusion. `Configure preset` is the sole added row
below the site's labels. Its lateral editor keeps its own color picker open to
its right; other site editors close the preset panel. Preset instructions persist
locally in `QuestSitePreset.json`; tests override its lazily resolved DefaultPath.
Labels support Keep/Add/Remove individually, including labels absent from the
scene. Highlight/blacklist use On/Off toggles in an optional-selection group;
labels use Add/Remove columns with one such group per row. Neither selected means
Keep. Color uses an On toggle and a square swatch button. Undo differences are
retained per scene opening/column/site across deselection, with at most one
record per target and no disk persistence. A shared preset definition supplies
the applied values and label names; records retain changed-field flags, previous
color and label-instruction indices/removal positions, not full before/after
configurations. Preset edits clear all records, including edits changed back to
their former values. Overlapping external edits invalidate only that target,
including while deselected; revert preserves unrelated edits. Scene closing,
resource/reload and local or received configuration replacement clear that
scene's records through its lifecycle notifications. Apply/revert use the shared
site configuration batch.

Quest Single/Split is local presentation. Hidden columns retain their scientific
state and updates; central presentation predicates control both rendering and
interaction. Shared scene visibility is different: hiding a scene also disables
its interaction. See [QuestAnatomyView presentation](../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomyView.Presentation.cs).
Place a new scene before showing it, in available space in front of the user,
without moving existing scenes. Merge returns to the headset viewport center;
Single/Split controls belong to each visualization row. The current
[QuestSceneLayout](../../Assets/Scripts/HBP/Quest/Runtime/QuestSceneLayout.cs) owns placement.

## Progress and operation boundaries

Loading text describes the stage currently running, set before its work begins.
Use the same English wording/count semantics on Desktop and Quest for equivalent
work, and accurate monotonic progress within the operation. Do not append a
`[scene X/Y]` suffix to every stage. A wording-only request does not authorize
redesigning the Loading Circle or adding extra detail/status panels.

Preserve the precise operation wrapped by `LoadingManager`; orchestration belongs
to the appropriate UI owner. Background work becomes visible when a user must
wait for it, by attaching to the existing operation. Avoid a one-frame empty loader.
Long-job preparation uses the existing delayed loader (200 ms by default) to
avoid flashes; register the operation immediately, delaying only its visual.
For opening a single-patient scene from Quest, feedback starts at the click and
covers Desktop preparation and transfer through the common progress path.

Inflation mesh computation uses loading feedback. Its subsequent visual animation
bypasses `LoadingManager` entirely, not merely its visual component. The
[synchronization and job contract](synchronization.md#scientific-jobs-and-cancellation)
owns coordinated preparation and the transition to local animation.

## User-visible errors and bug reporting

Provide a useful domain explanation when the user can act on a failure; a missing
data file should not produce only a generic error dialog. An expected failure of
the optional version check should not interrupt normal work with a technical error.
These examples do not authorize swallowing unexpected exceptions or hiding a
failed operation. Apply the existing error-handling path for the affected feature.

Keep pairing/preferences UI focused on useful domain controls, without redundant
technical statuses or retry counters. Atlas preparation retries are bounded and
automatic; persistent failures need an actionable Desktop error. Use the
[session resource contract](synchronization.md#session-preferences-definitions-and-installed-atlases)
instead of adding a manual Retry Quest status row to preferences.

The bug-report flow stays inside a native Unity window, without a WebView or
requiring the user to leave the application. This decision is specific to bug
reporting. The relay validates reports before forwarding them to Discord; the
private webhook belongs only in its secret binding, never in the client or Git.
The user's description is optional. Deduplicate repeated exceptions within an
incident without losing distinct exceptions and occurrence counts; compact stacks
while retaining useful class/file/line context. The current owner is
[ExceptionIncidentTracker](../../Assets/Scripts/HBP/UI/Tools/Diagnostics/ExceptionIncidentTracker.cs).

### Relay maintenance

The [relay implementation](../../Services/BugReportRelay/src/index.js) uses
approximate edge flood protection followed by strongly consistent SQLite-backed
Durable Object counters before contacting Discord. Limits are one report per
installation, three per source IP and twenty globally per 60 seconds. The
installation identifier is a rate-limit key, not a credential.

From `Services/BugReportRelay`, use `pnpm install`, `pnpm test` and
`pnpm exec wrangler deploy --dry-run` for local verification. Deployment is a
separate authorized action (`pnpm exec wrangler deploy`, with interactive login
if needed). The public worker `/report` URL is configured by
`BUG_REPORT_RELAY_URL` in `BugReporterWindow`; the Discord webhook is configured
interactively with `pnpm exec wrangler secret put DISCORD_WEBHOOK_URL`.
Confirm the binding with `secret list` without displaying its value. Never
include deployment-specific endpoints or credentials in this knowledge base.
