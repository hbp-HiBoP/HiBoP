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

## Input and shared behavior

Use [QuestInteractionInput](../../Assets/Scripts/HBP/Quest/Runtime/QuestInteractionInput.cs)
and [QuestInteractionPolicy](../../Assets/Scripts/HBP/Quest/Runtime/QuestInteractionPolicy.cs)
instead of independent button bindings: primary uses the front/index trigger;
secondary uses the side grip. Cut manipulation uses secondary so it can coexist
with primary brain manipulation. For changes to business state, consult
[Runtime and state](runtime-and-state.md).

## User-visible errors and bug reporting

Provide a useful domain explanation when the user can act on a failure; a missing
data file should not produce only a generic error dialog. An expected failure of
the optional version check should not interrupt normal work with a technical error.
These examples do not authorize swallowing unexpected exceptions or hiding a
failed operation. Apply the existing error-handling path for the affected feature.

The bug-report flow stays inside a native Unity window, without a WebView or
requiring the user to leave the application. This decision is specific to bug
reporting. The [existing relay documentation](../../Services/BugReportRelay/README.md)
owns its technical setup; this entry contains no endpoint or secret.
