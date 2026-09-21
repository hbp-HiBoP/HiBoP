# Prompt template for one implementation task

Replace `<TASK_ID>` with exactly one task from `06-implementation-stages.md`.

```text
Implement task <TASK_ID> of the Desktop–Quest synchronization v2 refactor in the HiBoP repository.

Read AGENTS.md and every document under Docs/dev/quest-autonomous/sync/ in the order given by README.md. Treat those documents as the target contract. The current S1–S3 snapshot/delta/visible-ACK implementation was audited and rejected; do not preserve it merely for compatibility. Protocol v2 is intentionally breaking, although the branch must remain buildable at the task boundary.

Work only on <TASK_ID>. Inspect the results of prerequisite tasks before changing code. If a required product decision is absent or contradictory, report it instead of inventing a broad abstraction or implementing a later task.

Before editing, state:
- the task objective and explicit non-goals;
- the existing components you expect to reuse, replace or leave untouched;
- the exact semantic entry/apply/visible/stable boundaries involved, and which production methods own them;
- the logical identities, attempt identities, owners and lifetimes affected by replacement, retry, cancellation, reconnect or shutdown;
- the current assembly dependency direction, every `.asmdef` edge the task may change, and why each edge points from a higher layer toward a lower one;
- the focused tests and performance/invalidation evidence that will prove completion.

Implementation rules:
- mutations originate at business setters, not toolbar refresh events or whole-scene comparison;
- "business setter" means the domain-owned mutation boundary at the correct layer, not permission to make Core depend on Sync or diagnostics;
- never label a derived event, render callback or later notification as the originating mutation boundary;
- ordinary work is O(change), with no capture/diff/checkpoint in the hot path;
- remote and local paths use the same domain operation with explicit echo suppression;
- invalidation is targeted by operation;
- networking and long-job waits never block the Unity thread;
- logical operations/jobs and their transport/computation attempts have distinct identities wherever retry or replacement is possible;
- diagnostic sinks, locks and correlation formatting stay outside measured intervals, and instrumentation lifecycle is safe with producers in flight;
- an inactive sync or diagnostic path adds no recurring traversal, serialization, allocation or permanent per-frame owner;
- `HBP.Core.Runtime` references no other `HBP.*` assembly; feature-specific state and telemetry stay in higher feature/composition layers;
- lower layers expose only feature-neutral events or ports when a higher layer must observe them; do not add a neutral-looking assembly solely to conceal an inverted dependency;
- every added or changed direct `HBP.*` assembly edge must be explicitly allowed, acyclic and justified;
- preserve unrelated user changes and avoid speculative cleanup;
- do not implement later operation families just because an abstraction could support them.

Tests are part of the short development loop. Follow the cadence and tier-selection table in 07-verification.md. Prefer fake clocks, fake jobs and in-memory transport; do not add real sleeps, broad scene loads or multi-minute suites to fast tiers. When an editor instance is available, use its persistent MCP runner for iterative focused tests; reserve fresh CLI runs and `-AllInAssemblies` for the documented closure or cold-start purpose. Do not rerun an unchanged broad suite without a stated reason. Record test execution and total wall time separately. Follow the repository UniTask/Unity async-test safety rules.

Run `Tools/check-assembly-dependencies.ps1` before Unity tests. If it fails, correct the dependency direction instead of adding another reference or moving feature-specific types into Core. Compilation alone does not satisfy this architecture check.

At completion:
- report the static assembly-dependency gate result and every `.asmdef` edge changed;
- run the focused tests and report exact results/duration;
- run Tools/format-code.cmd for changed C# and git diff --check;
- update only the relevant coverage/evidence entries in operation-matrix.md or the validation report;
- provide a compact table mapping every acceptance claim to its production entry point, deterministic test or measurement, and remaining limitation;
- list known limitations that belong to later tasks;
- do not claim a milestone or matrix row that was not actually demonstrated.
```

## Review prompt

For architecture/concurrency-sensitive tasks, request one independent review focused on races, stale publication, queue bounds, main-thread ownership and unintended scene-sized work. If the task touches telemetry, correlation or retry, the review also checks causal measurement boundaries, observer effect, attempt uniqueness and capture close/flush integrity. The primary agent remains responsible for inspecting and integrating that review.
