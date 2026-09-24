# HiBoP agent instructions

Global agent instructions also apply. Keep this file focused on HiBoP invariants
and high-frequency workflow rules.

## C# formatting

After **all implementation, testing, validation, and review work is complete**, run this **once as the final action before responding to the user**:

```powershell
.\Tools\format-code.cmd
```

Do not run it earlier or perform further work afterward. Its output does not need to be reviewed.

Use `-Base origin/develop` when committed branch changes must also be included, and `-All` only when the full `Assets` tree is intentionally targeted.

## Assembly dependency architecture

`HBP.Core.Runtime` is the foundational runtime assembly and must not reference any
other `HBP.*` assembly. Feature modules such as Sync, Transfer, UI, Data, and
Quest depend toward Core, never the reverse.

Do not move feature-specific types, diagnostics, interfaces, or state into Core to
bypass this rule. If a higher layer must observe a Core mutation, expose only a
feature-neutral domain event or port from Core.

Before changing an `.asmdef` reference:

1. inspect direct and transitive dependency direction;
2. justify the new edge;
3. run `Tools/check-assembly-dependencies.ps1`;
4. reject cycles and disallowed new `HBP.*` edges.

The task is not complete until this static gate passes.

## Prefab-first GameObject workflow

For UI and other authored GameObjects, edit/create the appropriate prefab and
serialize required references there. Runtime `new GameObject(...)` construction is
for inherently dynamic objects, not a workaround for missing prefab content.

Edit prefabs directly in Unity; do not generate or rebuild them with temporary
editor scripts.

## Unity workflow

When the HiBoP editor is open, use Unity MCP instead of starting another Unity
process. Inspect connected instances and editor/project state first; select the
`HiBoP` instance explicitly if multiple editors are connected.

When Unity must be started from the CLI, read its version from
`ProjectSettings/ProjectVersion.txt`, follow the global outside-sandbox rule, wait
for completion, and inspect exit code plus produced logs/results.

Use the `hibop-unity-validation` skill for detailed MCP, CLI, licensing, and async
test procedures.

Unity/UniTask tests must remain non-blocking: use `async Task` and direct `await`.
Do not use `.Wait()`, `.Result`, `GetAwaiter().GetResult()`, sleep/busy-wait, or
fire-and-forget while PlayerLoop progress is required. Capture expected async
exceptions with explicitly awaited `try/catch` rather than blocking NUnit helpers.

## Quest ADB workflow

For Quest ADB-over-Wi-Fi, use `Tools/Connect-QuestAdbWifi.ps1` instead of manual
ADB configuration unless the script itself fails. If the headset is not found,
first check that it is awake and being worn.

## Verification

- Start with the narrowest relevant tests and expand only when risk or failures
  justify it.
- Inspect Unity console errors as well as test results.
- Run the assembly dependency gate for `.asmdef` reference changes.
- Run the C# formatter before handoff when C# files changed.
