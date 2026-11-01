# HiBoP agent knowledge

Use this index for substantive project analysis, implementation and review.
Choose entries by the behavior being changed as well as the affected files.
Read only matching entries, before design choices; revisit when the scope changes.

| Task | Read |
|---|---|
| C# copy/clone, production async, model identity | [C# conventions](csharp-conventions.md) |
| Loading, I/O, memory, inflation, transfer or shared scene state | [Runtime and state](runtime-and-state.md) |
| UI design, prefabs, themes, Quest interactions, visible errors or bug reporting | [UI and UX](ui-and-ux.md) |
| Test creation/naming, failures, skips or removal | [Testing](testing.md) |
| UI authoring procedure | [.agents/skills/hibop-ui-authoring](../../.agents/skills/hibop-ui-authoring/SKILL.md) |
| Unity tests/editor checks, Quest build/device checks, licensing | [.agents/skills/hibop-unity-validation](../../.agents/skills/hibop-unity-validation/SKILL.md) |
| CI workflow changes | [.github/AGENTS.md](../../.github/AGENTS.md) |
| Native plugin acquisition/build integration | [Existing updater](../../Tools/Update-NativePlugins.ps1), its help and repository-specific build instructions |

AGENTS retains essential instructions. These entries hold scoped conventions;
linked technical contracts own their detailed definitions and exceptions.
Consult a linked procedure only when needed; reading it does not authorize execution.
If an explicit current decision conflicts with an older document, identify the
conflict and apply the current decision without silently extending its scope.

Maintain each entry as current knowledge: replace superseded text rather than
append conversation logs. Merge equivalent rules; keep one owner for each rule.
Keep unvalidated hypotheses in the audit. Known issues require current evidence
and a tracking reference, and leave the entry when resolved. Do not import old
bug reports as current defects. Routine status replies do not require this index.
