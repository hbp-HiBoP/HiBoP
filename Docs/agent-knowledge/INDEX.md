# HiBoP agent knowledge

Use this index for substantive project analysis, implementation and review.
Choose entries by the behavior being changed as well as the affected files.
Read only matching entries, before design choices; revisit when the scope changes.

| Task | Read |
|---|---|
| C# copy/clone, events, namespaces, production async, model identity | [C# conventions](csharp-conventions.md) |
| Project JSON/archives, serialized type or property changes, thresholds, time-series loading/validation | [Data and persistence](data-and-persistence.md) |
| Tag inference/import, enum options, type changes or repair | [Tags and imports](tags-and-imports.md) |
| Loading, I/O, memory, inflation, transfer or shared scene state | [Runtime and state](runtime-and-state.md) |
| Native wrappers/ownership, coordinates, meshes, projection or inflation | [Scientific computing and native integration](scientific-computing.md) |
| Shaders, scientific colors, cameras, contours or image exports | [Scientific rendering](scientific-rendering.md) |
| UI design, prefabs, themes, Quest interactions, visible errors or bug reporting | [UI and UX](ui-and-ux.md) |
| Test creation/naming, failures, skips or removal | [Testing](testing.md) |
| Product version, player packaging, release or native plugin updates | [Builds and distribution](builds-and-distribution.md) |
| Shared operations, coordinated calculations, reconnect or scene lifecycle | [Synchronization](synchronization.md), only the matching sections |
| Session preferences, definitions, atlas/MNI readiness | [Session resources](synchronization.md#session-preferences-definitions-and-installed-atlases) |
| Discovery, pairing, USB or Wi-Fi transport | [Pairing and transport](synchronization.md#pairing-and-usbwi-fi-discovery) |
| UI authoring procedure | Use [hibop-ui-authoring](../../.agents/skills/hibop-ui-authoring/SKILL.md); [UI and UX](ui-and-ux.md) owns its conventions |
| Unity tests/editor checks, Quest build/device checks, licensing | Use [hibop-unity-validation](../../.agents/skills/hibop-unity-validation/SKILL.md), which selects its relevant supporting procedure |
| CI workflow changes | [Builds and distribution](builds-and-distribution.md) |

AGENTS retains essential instructions. These entries own durable conventions,
technical contracts and their exceptions, without depending on temporary plans,
test reports, benchmark results or documentation elsewhere in the repository.
Keep declarative knowledge here. Skills may keep their workflow references
alongside their own entry point; links between those maintained locations are
allowed. Neither should depend on general project documentation outside them.
Code, asset, configuration and executable-tool references
are allowed; locate their current owners if names change. When incorporating
another document, consolidate its useful current knowledge here rather than
replace the missing content with a link or copy historical milestones/results.
Consult a linked procedure only when needed; reading it does not authorize execution.
If an explicit current decision conflicts with an older document, identify the
conflict and apply the current decision without silently extending its scope.

Maintain each entry as current knowledge: replace superseded text rather than
append conversation logs. Merge equivalent rules; keep one owner for each rule.
Keep unvalidated hypotheses in the audit. Known issues require current evidence
and a tracking reference, and leave the entry when resolved. Do not import old
bug reports as current defects. Routine status replies do not require this index.

For substantial staged work, keep stages independently verifiable and respect
the requested task's boundary. Check dependency readiness in code and evidence,
not just a status label. Do not extend a single-task request to the next task;
when the whole multi-stage effort is authorized, complete its stages without
inventing an approval gate between them. Separate
implementation, technical verification and manual acceptance in durable reports.
Record the source/build, relevant commands/results, user-visible before/after,
remaining decisions and limitations. Recheck evidence after affected code changes.
A small correction does not need a task/report framework.
