---
name: hibop-ui-authoring
description: Create or modify HiBoP Desktop or Quest UI prefabs, windows, controls, tooltips, and theme assets. Use for authored UI structure or presentation changes; not for backend-only changes, scientific rendering, or device/build validation alone.
---

# HiBoP UI authoring

Inspect the closest existing window/control, its prefab inheritance, serialized
references, and ThemeElement before choosing an implementation. Preserve manual
edits in the affected assets.

Apply the project's prefab-first rule. Use the Unity MCP skill for editor
operations; this skill supplies HiBoP conventions rather than tool schemas.

## Desktop and shared behavior

Reuse the established window/modifier and control conventions, including
`Toggle with text` for comparable preference toggles. Put static tooltip text in
the prefab, not in code that overwrites the authored value. User-facing strings
are English. Loading-circle messages omit trailing `...`, supplied by its animation.

Keep business behavior in the existing shared APIs. A Quest presentation should
invoke the same behavior as Desktop; adapt layout and input to the headset.
Do not duplicate business logic from Desktop tools in Quest presenters.

## Quest windows and themes

Use `Assets/Prefabs/Quest/UI/Quest Window.prefab` as the base variant for windows
belonging to that family. Inspect a current sibling for registration, serialized
references, footer, and preview setup. Reuse the existing shared window mechanics.

Quest ThemeElements and states are distinct from Desktop. Reuse existing Quest
roles before creating a new one; use a new role only for a distinct visual purpose.
Shared color settings can remain shared. Text/layout settings must suit Quest.
Icons are Image settings; reuse the established pictogram library.

Relevant current examples: `Quest Connection Window`, `Quest Site Window`,
`Quest Signals Window`, and `Assets/Resources/Themes/Quest/`.

## Focused verification

Inspect the changed prefab preview and serialized references. Use existing
`QuestUIAssetTests`, `QuestInformationWindowAssetTests`, or the relevant window
tests where they prove the change; do not duplicate their variant/theme assertions.
An editor preview proves layout only. When interaction needs a headset check,
route that check through `hibop-unity-validation`, respecting the user's chosen
build/test cadence.
