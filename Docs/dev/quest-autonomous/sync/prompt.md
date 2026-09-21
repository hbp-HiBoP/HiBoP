# Prompt template for one implementation task

Replace `<TASK_ID>` with exactly one task from `06-implementation-stages.md`.

```text
Implement task <TASK_ID> of the Desktop–Quest synchronization v2 refactor in the HiBoP repository.

Read AGENTS.md and every document under Docs/dev/quest-autonomous/sync/ in the order given by README.md. Treat those documents as the target contract. The current S1–S3 snapshot/delta/visible-ACK implementation was audited and rejected; do not preserve it merely for compatibility. Protocol v2 is intentionally breaking, although the branch must remain buildable at the task boundary.

Work only on <TASK_ID>. Inspect the results of prerequisite tasks before changing code. If a required product decision is absent or contradictory, report it instead of inventing a broad abstraction or implementing a later task.

Before editing, state:
- the task objective and explicit non-goals;
- the existing components you expect to reuse, replace or leave untouched;
- the focused tests and performance/invalidation evidence that will prove completion.

Implementation rules:
- mutations originate at business setters, not toolbar refresh events or whole-scene comparison;
- ordinary work is O(change), with no capture/diff/checkpoint in the hot path;
- remote and local paths use the same domain operation with explicit echo suppression;
- invalidation is targeted by operation;
- networking and long-job waits never block the Unity thread;
- preserve unrelated user changes and avoid speculative cleanup;
- do not implement later operation families just because an abstraction could support them.

Tests are part of the short development loop. Use the smallest tier in 07-verification.md. Prefer fake clocks, fake jobs and in-memory transport; do not add real sleeps, broad scene loads or multi-minute suites to fast tiers. Record the focused suite's execution time. Follow the repository UniTask/Unity async-test safety rules.

At completion:
- run the focused tests and report exact results/duration;
- run Tools/format-code.cmd for changed C# and git diff --check;
- update only the relevant coverage/evidence entries in operation-matrix.md or the validation report;
- list known limitations that belong to later tasks;
- do not claim a milestone or matrix row that was not actually demonstrated.
```

## Review prompt

For architecture/concurrency-sensitive tasks, request one independent review focused on races, stale publication, queue bounds, main-thread ownership and unintended scene-sized work. The primary agent remains responsible for inspecting and integrating that review.
