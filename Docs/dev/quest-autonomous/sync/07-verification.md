# Verification and short development cycles

Automated-test speed is a mandatory product constraint for this refactor. A correct system with a multi-minute feedback loop for every small change is not an acceptable implementation outcome.

## Static architecture gate

Before starting Unity, run `Tools/check-assembly-dependencies.ps1`. This gate parses `.asmdef` files directly and must complete in a development-loop timescale. It enforces at least:

- `HBP.Core.Runtime` has no reference to any other `HBP.*` assembly;
- the `HBP.*` dependency graph is acyclic;
- every newly introduced direct `HBP.*` edge appears in an explicit reviewed allow-list;
- missing or ambiguous assembly names fail visibly instead of being ignored.

The sync test launcher invokes this check first and stops before Unity when it fails. The same gate runs in CI. Any `.asmdef` modification must include the changed-edge rationale in the task report. Do not replace this gate with an EditMode test: its purpose is to reject an invalid architecture in milliseconds, before compilation/domain reload cost.

## Test tiers and initial budgets

Budgets describe test execution after assemblies are loaded; Unity compilation/domain reload is recorded separately. T00 measures the real baseline and may tighten or adjust a budget with documented evidence.

| Tier | Purpose | Dependencies | Initial execution target |
| --- | --- | --- | --- |
| `Sync.Fast` | IDs, codec, validation, scheduler, coalescing, backpressure, generation, fake clock | Pure managed values; no scene, socket or real time | `< 1 s` total |
| `Sync.Loopback` | Framing, duplex order, ACK loss, reconnect, chunks | Fake peers; loopback transport allowed | `< 5 s` total |
| `Sync.SceneFocused` | One small synthetic scene/handler family and invalidation | Minimal EditMode/PlayMode fixture | `< 30 s` per task shard; aggregate tracked separately |
| `Sync.Qualification` | Full modalities, real scene, large data, long outages | Larger Unity fixtures | milestone/nightly only |
| `Sync.Device` | Physical Quest USB/Wi-Fi latency and visual/scientific parity | Desktop + Quest builds | milestone/release only |

An implementation task runs the smallest tiers proving its change. It does not run a full device or six-modality scenario merely to debug a codec branch. T00 records both per-shard and aggregate runtime; adding shards may not make the aggregate invisible.

## Development-loop execution policy

Optimize the feedback path before removing useful tests. Report test execution time separately from Unity compilation, domain reload, editor/process startup and total wall time; a sub-second suite launched through a fresh 30-second process is a launcher problem, not a slow-test problem.

Use this cadence:

1. Run the static architecture gate after any `.asmdef` change and before Unity tests.
2. During implementation, batch a coherent code change before testing. Do not restart an unchanged suite after every file edit.
3. On a failure, rerun the smallest failing test or fixture while diagnosing it. After the fix, rerun its focused tier once.
4. Before review, run every focused tier selected for the task and record exact pass/fail counts plus execution and wall times.
5. Run a broader affected assembly once at task closure only when shared production integration, serialization, transport or scene behavior changed. `-AllInAssemblies` is not part of the inner loop.
6. Run `Sync.Qualification` and `Sync.Device` only at the milestones that require them or for a concrete relevant regression.

When the Unity editor is already available, use its persistent MCP test runner for iterative focused tests instead of starting another Unity process. Use `Tools/run-sync-tests.ps1` for a closed-editor run, CI-equivalent evidence or the deliberate cold-start/aggregate measurement requested by a task. Do not run concurrent Unity test processes against the same project.

Select tiers from the behavior changed, not from the number of files touched:

| Change | Iteration tier | Additional closure evidence when applicable |
| --- | --- | --- |
| Pure IDs, contracts, codecs, validation, scheduling or fake jobs | `Sync.Fast` | None unless a public integration boundary changed |
| Framing, socket order, ACK/retry, reconnect or chunk flow | affected `Sync.Fast` tests, then `Sync.Loopback` | affected transport assembly once |
| Business setter, real handler, invalidation or frame publication | affected `Sync.Fast` tests, then one `Sync.SceneFocused` shard | affected scene assembly once |
| Large modality fixture, long job or full-scene behavior | fastest contract/job tests first | relevant `Sync.Qualification` shard at the designated milestone |
| Physical latency or Desktop/Quest parity | no device run during ordinary edits | `Sync.Device` only when explicitly required |
| Documentation-only change | no Unity test by default | link/consistency review and `git diff --check` |

Repeated broad green runs without an intervening relevant change add latency but no evidence. If a rerun is needed for suspected nondeterminism, state that hypothesis; never normalize flakiness by retrying until green.

## Rules for fast tests

- Inject monotonic clocks; never wait 500 real milliseconds to test the grace period.
- Use barriers/completion sources and controllable fake jobs; no race assertion based on `Delay`, `WaitForSeconds` or polling.
- Use in-memory/fake transport for scheduler semantics and loopback only for actual framing/socket behavior.
- Construct operation DTOs directly; do not load a Unity scene to test encoding, IDs or coalescing.
- Keep one minimal synthetic scene per handler family instead of rebuilding a complete visualization per case.
- Parameterize equivalent operation cases without hiding which matrix row failed.
- Separate performance/allocation assertions from functional assertions when profiler instrumentation would distort timing.
- Follow the repository async-test rules: await Unity/UniTask work directly; never block the PlayerLoop with `.Wait()`, `.Result`, NUnit async wrappers or busy waits.
- A new regression first receives the fastest deterministic reproduction capable of detecting its mechanism.

Any change that pushes a fast tier over budget must identify the slow tests and either optimize/split them or justify a budget change in the validation report. Adding retries to hide nondeterminism is forbidden.

## Observability and production-hook tests

Instrumentation is production code and requires its own deterministic integrity tests:

- prove that the production hook captures the declared semantic boundary, not merely that the telemetry API can store a point;
- use separate logical trace and attempt identities, and test coalescing, replacement, retry and reconnect without cross-associating samples;
- capture before/after points around measured work and publish them afterwards so sink locks, correlation formatting and serialization do not contaminate the interval;
- test enable/disable or shutdown with a writer in flight so accepted samples are either drained or explicitly reported as incomplete;
- test successful and failed preparation/encoding so completion milestones cannot be emitted from a failure `finally` path;
- assert that disabled instrumentation introduces no allocations, scene traversal or permanent per-frame owner;
- keep incomplete traces visible in the report rather than silently discarding or combining them.

## Contract tests

Pure tests cover:

- deterministic encode/decode and version rejection;
- length/count/numeric/resource validation;
- idempotence by operation ID;
- canonical acceptance and per-key application ordering;
- ordered reliable-frame-gap recovery within each stream at resume, without a bulk-stream gap blocking control/scene-operation streams;
- reconnect with a large unacknowledged bulk followed by cancel and interactive traffic completing within the configured scheduler bound;
- threshold routing for a large atomic configuration/site batch and dense triangle mask: small ordered descriptor, independent bulk body, complete validation then one apply;
- control-lane structural priority without moving create/delete out of scene-operation order;
- decoded operations blocking only overlapping keys, including a multi-key barrier with partial overlap;
- coalescing only unsent matching preview keys, without consuming origin sequence or creating a false gap;
- structural barriers sealing earlier coalescing slots;
- retention and unchanged replay of every written reliable preview until ACK or grace expiry;
- ephemeral ACK/ping/telemetry loss without a continuity gap;
- retransmit pressure stopping new preview commitment while newest unsent values continue to coalesce;
- reliable retention of structural/control/final work;
- queue byte/count bounds and visible reliable-overflow failure;
- bulk chunk interleaving, fairness/no starvation across lanes and scenes, and interactive apply while an earlier accepted job result or large atomic body is incomplete;
- stale generation/result rejection;
- fake-clock timeline anchors and disconnect grace;
- checkpoint deterministic identity and family-owned typed export/apply.
- O(change) incremental checkpoint identity updates under a 30,000-site fixture without whole-scene traversal.

## Race scenarios

Every long-job implementation must test:

1. slow generation A starts;
2. A is cancelled or superseded;
3. generation B starts and completes;
4. A returns after B;
5. only B can publish or unlock UI.

Transport/session tests cover loss at queued, written, received, applied and ACK-return boundaries. An ACK lost after application causes retry without a second effect. One rejected operation does not stop independent operations. Shutdown is exercised with read blocked, writer empty, writer full and bulk partial.

Additional deterministic races cover:

- projection start accepted before/after a sensitive proposal, including authoritative rollback after rejection;
- a nested/derived setter under remote context (for example automatic cuts) that must not re-emit;
- tiny interactive apply with an already queued bulk backlog and a bounded per-frame apply budget;
- initial-publication mutation replay, structural mutation, journal overflow checkpoint fallback and topology-change abort.
- activity input lease lifetime across mutation, cancellation, scene close and stale native completion.

## Operation-matrix tests

Each D1–D34 handler needs focused evidence for:

- correct typed payload and identity;
- Desktop-origin and Quest-driver origin;
- atomic apply and echo suppression;
- exact targeted invalidation;
- coalescing/reliability class;
- projection safety/busy behavior;
- resource/topology failure when applicable;
- preservation of local presentation;
- typed checkpoint export/apply through the same handler, without a generic state map.

Derived behavior is tested at the cause. Selecting a site with automatic cuts verifies that both peers derive the same cuts; it does not require redundant cut messages.

## Performance regression gates

Measure representative operations after warm-up:

- O(1) site color/selection;
- continuous complete cut definitions;
- timeline play/seek anchor;
- 30,000-site inclusion bitset/batch;
- sparse and dense triangle masks;
- correlation/checkpoint/large-batch bulk transfer with simultaneous interactive traffic.

Record p50/p95/p99 setter-to-apply and setter-to-next-visible where observable, bytes, queue depth, replaced previews, main-thread CPU and GC. No operation may wait for global render quiescence. A one-site change must not allocate/copy proportionally to the scene. T08 must turn T00 measurements into an explicit pass/fail p95, GC and main-thread decision for color/cut/timeline; “hot-path invariant respected” alone is insufficient.

The inactive path is inspected and profiled: without an active sent-scene session there is no capture timer, scene traversal, serialization or per-frame sync owner.

## Initial-transfer gate

Maintain a separate click-to-first-visible trace:

- resource readiness/fingerprint cache state;
- Unity-main-thread preparation;
- serialization and number of source traversals/copies;
- first/last byte;
- Quest decode/publish;
- first visible brain.

Tests/harnesses must detect a reintroduced fingerprint pass followed by serialization of the same data. Initial transfer cannot regress silently because live synchronization changed the manifest.

## Long-job UX tests

- Loading UI remains hidden for jobs completing before 200 ms and appears after the injected threshold.
- Online filter/correlation execute only on Desktop, transfer one canonical generation and lock both interfaces until Quest application.
- Confirmed connection loss cancels without automatic offline restart.
- Activity permits safe operations, blocks sensitive operations and unlocks only after both peers finish/cancel.
- Manual stale activity does not auto-recompute; automatic mode does.
- Network framing and acknowledgements remain alive under every UI busy scope.

## Reconciliation and lifecycle tests

- reconnect within 500 ms retries/deduplicates without a dialog;
- equal checkpoint identities reconnect silently;
- each Desktop/Quest choice prevalidates then commits the common scene without intentional partial exposure; multi-scene T17 reports success/failure per scene rather than claiming rollback across scenes;
- interrupted checkpoint staging applies nothing and can be retried after reconnect;
- connection loss during staging and failure during per-scene commit produce distinct retry/out-of-sync states;
- closing Desktop offline makes the old Quest incarnation permanently local;
- reopening the same source does not reuse the old incarnation;
- no offline state survives process death by contract;
- multi-scene operations and jobs never cross scene/incarnation keys.

## Physical qualification

At milestones, record Unity/build commits, native library versions, Quest model/refresh rate, USB or Wi-Fi path, fixtures and profiler configuration. Exercise representative cheap, continuous, structural and long operations in both directions. Separate observed device results from unit/integration claims.

No document or green unit suite alone proves one-frame device latency or scientific parity.
