# Model selection by implementation task

Status: **operator guidance, not a synchronization requirement**. Last reviewed: 2026-09-21.

Use this table when creating the implementation discussion for one task. It optimizes for a reliable first pass without assigning the most expensive model to every bounded change.

| Task | First model | Reasoning | Why |
| --- | --- | --- | --- |
| T00 | `gpt-5.6-terra` | `medium` | Instrumentation and fast-test foundation are bounded by explicit measurements. |
| T01 | `gpt-5.6-terra` | `medium` | Pure identities, DTOs, validation and codecs have deterministic acceptance tests. |
| T02 | `gpt-5.6-sol` | `high` | Initial-transfer performance crosses resource lifetime, threading, hashing and allocation concerns. |
| T03 | `gpt-5.6-sol` | `high` | Scheduler correctness depends on queue bounds, ordering, fairness, retry and stale-generation races. |
| T04 | `gpt-5.6-terra` | `high` | The slice is small, but echo suppression and targeted invalidation require careful integration. |
| T05 | `gpt-5.6-sol` | `high` | Duplex transport, framing, cancellation, replay and shutdown contain subtle concurrency failures. |
| T06 | `gpt-5.6-sol` | `high` | Authority, optimistic correction, conflict ordering and reconnect determine data integrity. |
| T07 | `gpt-5.6-sol` | `high` | Native-job lifetime, immutable leases and stale publication are concurrency-critical. |
| T08 | `gpt-5.6-sol` | `high` | This is the real-scene cutover and the first point where all architectural layers meet. |
| T09 | `gpt-5.6-terra` | `medium` | Mostly repeated typed-handler coverage on an already proven architecture. |
| T10 | `gpt-5.6-sol` | `high` | Stable identity, topology, structural order and adaptive masks are integrity-sensitive. |
| T11 | `gpt-5.6-terra` | `high` | Broad but specified handler work, with atomic batches and bulk routing to verify. |
| T12 | `gpt-5.6-sol` | `high` | Filtering combines job generations, UI locks, cancellation and atomic remote publication. |
| T13 | `gpt-5.6-sol` | `high` | Correlation transfer adds large-data fairness, provenance and partial-result safety. |
| T14 | `gpt-5.6-sol` | `high` | Two-peer activity coordination is the most race-sensitive runtime workflow. |
| T15 | `gpt-5.6-terra` | `medium` | Quest UI must connect to existing operations without inventing new protocol semantics. |
| T16 | `gpt-5.6-sol` | `high` | Reconciliation staging, user choice, commit failure and orphan behavior affect whole-state integrity. |
| T17 | `gpt-5.6-sol` | `high` | Multi-scene routing/lifecycle adds cross-scene isolation and concurrent failure cases. |
| T18 | `gpt-5.6-terra` | `high` | Cleanup and qualification are broad, but behavior should already be fixed and testable. |

## Escalation rule

- Keep the selected model for normal review and adjustment cycles within the same task.
- If a Terra task exposes an undocumented architectural or concurrency problem, escalate the unresolved analysis to `gpt-5.6-sol` with `high` reasoning instead of broadening the task silently.
- Use `gpt-6-astra` with `high` reasoning for an independent review or a genuinely blocked cross-component design problem. It is not the default implementation model.
- Do not use `gpt-5.6-luna` as the first owner of a complete T00–T18 task. Reserve it for isolated mechanical follow-ups with deterministic verification.
- Do not use `xhigh`, `max` or `ultra` by default. Increase effort only after identifying a concrete reasoning failure that tests or code inspection cannot resolve cheaply.

The task scope and acceptance criteria remain authoritative. A stronger model is not permission to implement later tasks or redesign settled behavior.

## Sources and maintenance

OpenAI currently describes Terra as the intelligence/cost balance, Sol as a flagship model for complex professional work and Astra as the most capable model for the hardest end-to-end work. The general selection guidance is to meet the accuracy target first, then optimize cost and latency.

- [OpenAI model catalog](https://developers.openai.com/api/docs/models)
- [OpenAI model-selection guide](https://developers.openai.com/api/docs/guides/model-selection)

Recheck this page when the available Codex model list or model roles change. Exact API token pricing is not used here because it does not necessarily represent Codex app usage or account limits.
