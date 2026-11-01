# Testing conventions

Read when creating or renaming tests, diagnosing failures,
or considering skipping or removing an existing test.

Name new tests and test files for the behavior they protect, not a historical
implementation step such as `Step5`. A future reader should understand the purpose
without reopening an old plan. This does not request a bulk rename of existing tests.

When a useful test crashes, diagnose whether its fixture/setup can exercise the
intended behavior correctly. Do not mark it ignored simply to obtain a passing
result. If a meaningful executable test cannot be constructed, explain the cause
and missing coverage so the user can decide its removal or replacement. A justified
environmental limitation is not a pass, nor an instruction to launch unrelated work.

The [existing testing strategy](../dev/testing_strategy.md) owns test layout,
fixtures and technical conventions. Select only the part relevant to the change.
For Unity execution or diagnostics, use the existing
[validation skill](../../.agents/skills/hibop-unity-validation/SKILL.md).
It owns MCP/CLI routing, async-test safety, job/result inspection and device checks.

AGENTS owns final formatting and the essential verification rules; do not copy
them here. Reading this entry during an analysis-only task does not authorize
changing tests, launching Unity, or running a test campaign.
