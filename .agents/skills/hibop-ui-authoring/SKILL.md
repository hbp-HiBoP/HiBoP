---
name: hibop-ui-authoring
description: Create or modify HiBoP Desktop or Quest UI prefabs, windows, controls, tooltips, and theme assets. Use for authored UI structure or presentation changes; not for backend-only changes, scientific rendering, or device/build validation alone.
---

# HiBoP UI authoring

Read [UI and UX conventions](../../../Docs/agent-knowledge/ui-and-ux.md).
If the change affects shared behavior or synchronization, also read
[Runtime and state](../../../Docs/agent-knowledge/runtime-and-state.md)
and [Synchronization](../../../Docs/agent-knowledge/synchronization.md) when applicable.

1. Identify whether the request changes structure/interaction or only displayed
   text. Inspect the nearest existing control/window, prefab inheritance, serialized
   references and theme role; identify manual edits to preserve.
2. Apply the project authoring rules and the knowledge entry. Use the Unity
   MCP skill for editor operations when needed; do not duplicate its tool schemas.
3. Inspect the changed prefab preview and required references. Use existing
   `QuestUIAssetTests`, `QuestInformationWindowAssetTests`, or relevant window
   tests where they prove the change, without duplicating their assertions.
4. If validation requires Unity execution or a device, use
   `hibop-unity-validation`. Distinguish editor preview from headset interaction.
