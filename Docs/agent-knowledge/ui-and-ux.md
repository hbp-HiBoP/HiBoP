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

## Input and shared behavior

Use [DesktopInput](../../Assets/Scripts/HBP/Input/DesktopInput.cs) for Desktop
New Input System access and its neutral behavior when a device is absent.
Use [QuestInteractionInput](../../Assets/Scripts/HBP/Quest/Runtime/QuestInteractionInput.cs)
and [QuestInteractionPolicy](../../Assets/Scripts/HBP/Quest/Runtime/QuestInteractionPolicy.cs)
instead of independent button bindings: primary uses the front/index trigger;
secondary uses the side grip. Cut manipulation uses secondary so it can coexist
with primary brain manipulation. For changes to business state, consult
[Runtime and state](runtime-and-state.md).

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
