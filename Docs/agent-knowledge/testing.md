# Testing conventions

Read when creating or renaming tests, diagnosing failures,
or considering skipping or removing an existing test.

Name new tests and test files for the behavior they protect, not a historical
implementation step such as `Step5`. A future reader should understand the purpose
without reopening an old plan. This does not request a bulk rename of existing tests.

Use synthetic/anonymized fixtures and the existing versioned MNI fixtures where
appropriate. Automated tests must be reconstructible on another workstation,
without private datasets or unexplained local `.artifacts` prerequisites.
Use the existing state scopes to restore paths, statics and singletons after tests,
including failure/teardown: a later Play session must not inherit temporary paths.

Keep fixtures small, with stable synthetic IDs and no real patient/study names or
machine paths. Preserve historical `$type` shapes in compatibility JSON. Add a
binary `.hibop` fixture only when a generated fixture cannot protect the relevant
legacy behavior. Tests must never read or overwrite the user's preferences,
aliases, database, project history or workspaces.

## Choose the test boundary

| Boundary | Appropriate evidence |
|---|---|
| Pure model, parser, serialization, cache or data transform | Deterministic EditMode test |
| Required components/references, materials, prefabs and scenes | EditMode asset integrity test |
| Project/archive workflow with controlled Unity dependencies | Focused integration fixture |
| Start/Update, frame publication, input dispatch, rendering or scene lifecycle | Minimal PlayMode harness |
| Application bootstrap or multi-window user journey | A small number of full workflow smokes |

Use the lowest layer that proves the behavior. A `MonoBehaviour` type alone does
not justify PlayMode, but EditMode cannot prove its runtime lifecycle. Assert
observable state/output, not only absence of exceptions. Use real rendering only
when pixels, layout or GPU behavior matter; screenshots do not replace semantic
assertions. Group equivalent cases without concealing which behavior failed.

Reuse existing assembly boundaries under `Assets/Tests/EditMode` and
`Assets/Tests/PlayMode`; do not create an assembly per historical step. Shared
`HBP.PlayModeTestUtilities` supplies harness mechanics, not assertions of product
behavior. Useful existing scopes include `ApplicationStateTestScope`,
`PersistentDataTestScope`, `TempDirectoryScope` and their PlayMode equivalents,
plus `PlayModeSceneScope`, `PlayModeWindowHarness` and `PlayModeProjectHarness`.
Await cleanup of scenes, objects and operations before restoring global state.

## Determinism and focused regression evidence

Inject clocks, completion sources and barriers to control asynchronous boundaries.
Do not use elapsed real-time delays to force a race. A stale-result test should
start A, supersede/cancel it, complete B, then complete A and prove that only B
publishes or unlocks UI. Exercise cancellation/error cleanup and a repeated run
where they protect resource ownership, not just the successful path.

For loading/validation changes, prove shared work is not duplicated, consumer
cancellation respects other consumers, old generations cannot publish, and an
already-ready operation shows no empty loader. Count injected native/file reads
when validating that an edit only checks affected data. For persistence changes,
cover legacy read, current round-trip, malformed input and rollback using the
contracts in [Data and persistence](data-and-persistence.md).

When a useful test crashes, diagnose whether its fixture/setup can exercise the
intended behavior correctly. Do not mark it ignored simply to obtain a passing
result. If a meaningful executable test cannot be constructed, explain the cause
and missing coverage so the user can decide its removal or replacement. A justified
environmental limitation is not a pass, nor an instruction to launch unrelated work.

For shared behavior, consult [Synchronization](synchronization.md) and select the
affected semantic operations. Verify typed payload/identity, both origins, atomic
application, echo suppression, targeted invalidation, busy/cancellation behavior,
and preserved local presentation. Check stabilized scientific output as well as
state. Derived effects are tested at their cause; each UI gesture does not require
a separate network operation or duplicate test.

Keep sync test tiers distinct: `Sync.Fast` for pure contracts/scheduling with fake
clock/transport, `Sync.Loopback` for framing/retry/order, `Sync.SceneFocused` for
small handler families, and `Sync.Qualification`/`Sync.Device` for relevant milestone
or concrete regression evidence. A codec change does not justify a full headset
campaign. Record test execution separately from compilation, reload and startup.
Do not retry nondeterministic tests until green or repeat unchanged broad suites
without a hypothesis that the rerun resolves.

Instrumentation needs tests of its production boundary, not merely its storage
API: trace/attempt identity must survive coalescing/retries without cross-association,
failure must not emit a success milestone, and shutdown must drain or explicitly
report incomplete traces. Disabled instrumentation must not add allocations or
scene traversal. Keep sink/formatting work outside the measured interval and
separate timing/allocation checks from functional tests when instrumentation
would distort results.

For execution, diagnostics, async-test safety and device checks, use
[hibop-unity-validation](../../.agents/skills/hibop-unity-validation/SKILL.md)
and only its applicable supporting procedure.

Reading this entry during an analysis-only task does not authorize changing tests,
launching Unity, or running a test campaign. Report skipped/unavailable native or
device coverage explicitly; an Editor pass is not a Player or headset acceptance.
