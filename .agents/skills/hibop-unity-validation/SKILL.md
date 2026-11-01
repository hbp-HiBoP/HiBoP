---
name: hibop-unity-validation
description: Run or diagnose HiBoP Unity tests, editor checks, Quest builds and device validation, or hbp_core benchmarks that launch Unity. Also use when changing Unity/UniTask tests. Exclude native-only checks, read-only code reviews without validation, and general Quest feature planning.
---

# HiBoP validation

This skill belongs to the HiBoP project. Its native-repository entry points apply
only to integration work that launches or validates HiBoP/Unity.

Choose checks that resolve the current task's risks. Group expensive builds and
device checks around an integrated change; repeat them only for relevant changes,
failures, or unresolved concerns. Honor an explicit request to defer validation.
Editor script compilation needed to edit serialized references is distinct from
building an APK; use a distinction already established in the conversation.

Locate the current HiBoP checkout before resolving project-relative paths. From
a native repository, use its integration instructions to locate HiBoP and read
`.agents/skills/hibop-unity-validation/SKILL.md` in that checkout explicitly.

Read only the applicable procedure:

- Editor open: [MCP validation](references/mcp.md). Inspect/select HiBoP and use
  the existing editor; do not launch another Unity process.
- Writing or exercising Unity/UniTask async tests:
  [Async tests](references/async-tests.md).
- Editor closed, or diagnosing CLI licensing:
  [CLI validation](references/cli.md). Every Windows command launching Unity
  directly or indirectly must run outside the sandbox.
- Building, installing, or testing Quest:
  [Quest validation](references/quest.md), before preparing the APK or device.

Separate implementation, technical results, and manual acceptance. Inspect a
completed job/process and its console/logs before reporting a result. Evidence
must correspond to the tested source/build; an earlier successful run does not
verify later behavioral changes. Use the existing project report template where
the task requires a durable report, without adding a reporting framework.

Follow AGENTS.md for the final C# formatting action. This workflow adds no work
after that action and no formatting step to read-only reviews or audits.
