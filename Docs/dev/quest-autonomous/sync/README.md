# Desktop–Quest synchronization v2

Status: **specification reset approved; implementation not started**.

The synchronization work implemented after commit `a4ef93c` does not satisfy the product or performance requirements. Its snapshot/diff hot path, globally blocking acknowledgements, coarse invalidation and unfinished bidirectional model are not the foundation of the next implementation. The replacement is intentionally wire-incompatible and may delete the current S1–S3 runtime once the new vertical slice is active. Keeping the branch buildable between tasks remains mandatory; preserving the old sync protocol does not.

The initial scene delivery remains a separate workflow. Live synchronization starts only after Quest has published that prepared scene. It exchanges typed business operations and bounded job results, never a recaptured scene on each edit.

## Canonical decisions

- One user alternates between one Desktop and one Quest. Desktop orders accepted operations while connected; Quest applies its own interactions optimistically.
- A normal setter must emit work proportional to the modification, not to the scene. No global capture, diff, render fence or acknowledgement may gate the next mutation.
- Continuous values keep the newest unsent preview; structural operations and the newest value after a stream stops are reliable.
- Both directions use the same business setters and targeted invalidations. Remote application suppresses re-emission without bypassing domain behavior.
- Filter and correlation jobs run only on Desktop while online; Quest waits for the canonical result. Activity projection runs locally on both devices.
- A 500 ms disconnection grace queues and retries operations. Beyond it, both peers continue locally. Reconnection chooses one whole state—Desktop or Quest—rather than merging.
- Heavy resources are transferred only with the initial/full scene delivery. Incremental messages reference a verified manifest. A missing resource requires a full resend.
- Local camera, Quest wrapper pose/scale, tracked poses, hover and UI layout remain local. Scientific selections and parameters are shared.
- Future multi-scene support is designed into identifiers and envelopes now, but implemented after the online single-scene core.
- Automated tests are a development-loop feature: the fast sync suite must stay deterministic and short. Real sockets, sleeps, full scene loads and device tests do not belong in the per-edit tier.

## Read in order

1. [Why the current system is being replaced](00-current-system-audit.md)
2. [Requirements and boundaries](01-requirements-and-boundaries.md)
3. [Operation and checkpoint contract](02-state-contract.md)
4. [Authority, disconnection and reconciliation](03-replication-and-offline.md)
5. [Transport and performance](04-transport-and-performance.md)
6. [Integration and invalidation model](05-code-integration.md)
7. [Ordered implementation tasks](06-implementation-stages.md)
8. [Fast-test strategy and release verification](07-verification.md)
9. [Wire protocol v2](08-wire-protocol.md)
10. [Long-running jobs](09-long-running-jobs.md)
11. [Scene lifecycle and future multi-scene support](10-scene-lifecycle.md)
12. [Operation matrix](operation-matrix.md)
13. [Prompt template for implementation agents](prompt.md)

Operator-only aid: [recommended model for each implementation task](model-selection.md). This guide is non-normative and does not change task scope or acceptance criteria.

## Authority of these documents

These documents supersede the previous S1 state codec, S2 live adapter and S3 active-session plans as well as future-sync assumptions in `../05-state-command-and-sync-model.md`. Historical task records remain useful evidence, not requirements.

If code and these documents disagree during the refactor, the documented v2 behavior is the target. An agent must record a genuine blocker or request a specification decision rather than silently preserving legacy behavior.
