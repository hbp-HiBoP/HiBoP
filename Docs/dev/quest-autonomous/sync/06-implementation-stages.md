# Ordered implementation tasks

## How to use this backlog

These tasks are sequential and intended for different agents to execute one at a time. An agent receives exactly one task, starts from its completed prerequisites and must not implement later tasks opportunistically.

Every task must:

- read this corpus and `AGENTS.md`;
- state assumptions, objective and non-goals before editing;
- leave the branch compiling at its declared boundary;
- add the smallest deterministic tests that prove its behavior;
- run the fast task-specific tier and record its execution time;
- avoid lengthening fast tiers without measurement and justification;
- run `Tools/format-code.cmd` for changed C# and `git diff --check`;
- update matrix/validation evidence only for behavior actually demonstrated.

Protocol v2 may temporarily coexist in source with experimental code, but one live scene must never have both owners active. Wire/schema backward compatibility is not required.

## T00 — Baseline instrumentation and fast-test foundation

**Goal:** expose latency, allocations and development-loop cost before choosing implementations.

**Work:**

- establish the test tiers/categories in `07-verification.md`;
- provide fake monotonic time and deterministic async synchronization utilities;
- instrument initial transfer and three profiles: site color, continuous cut movement and timeline playback/seek;
- record setter, queue/send where available, receive/apply, next-visible and scientific-stable milestones;
- record current test durations, main-thread/GC costs and available USB/Wi-Fi baselines;
- publish provisional p95, allocation and main-thread regression budgets for the three profiles, with unavailable device measurements clearly marked.

**Do not:** redesign or repair the current protocol.

**Acceptance:** focused fast commands exist, timing tests use no real sleeps, and a baseline/budget report is reviewable.

## T01 — Minimal pure v2 contract and codec

**Depends on:** T00.

**Goal:** define only the common envelope plus the three vertical-slice operation contracts.

**Work:**

- implement bounded session/scene/incarnation/operation/resource/topology identities;
- implement versioned envelopes and validation;
- implement DTOs/codecs for site color, complete cut definition and timeline anchor;
- define typed descriptors that derive coalescing key, touched-key set and barrier scope for those DTOs;
- define typed checkpoint records for those same three handlers;
- leave an explicit versioned extension mechanism for later operation families.

**Do not:** predefine every D1–D34 DTO, add Unity objects/sockets, create a generic field map or implement merge.

**Acceptance:** deterministic pure round-trip/malformed/version/finite/bounds tests complete under the fast-tier target.

## T02 — Resource manifest and initial-transfer performance repair

**Depends on:** T01.

**Goal:** establish trustworthy resource identity before live scene binding, without delaying click-to-visible transfer.

**Work:**

- define stable IDs and bounded metadata for every selectable prepared resource/topology/roster;
- calculate/cache fingerprints after preparation using immutable buffers on workers;
- fall back to hashing the exact serialization stream;
- verify received fingerprints in production paths;
- remove demonstrated duplicate surface/MEG fingerprint traversals and copies;
- remove the per-frame Quest `columns.ToArray()` allocation if still present;
- retain T00 first-byte/first-visible instrumentation.

**Acceptance:** no dedicated second resource traversal for hashing, mismatch rejection works, and transfer remains within the T00 regression budget.

## T03 — Pure scheduler, backpressure and job freshness

**Depends on:** T01.

**Goal:** prove scheduling and concurrency before networking.

**Work:**

- implement control, interactive/coalescing and bulk lanes with reliability declared per record;
- use independent reliable sequences/retransmit logs for session control, each scene-operation stream and each bulk transfer;
- assign origin sequence only at wire commitment and seal slots at barriers;
- implement bounded queues/retransmit log, reserved reliable-control capacity and preview admission pressure;
- route every body above the measured inline threshold through descriptor + independent bulk stream, including generic large atomic batches/masks;
- implement bulk chunk interleaving/fairness;
- implement operation dedup, per-key accepted indexes and job generation/cancellation primitives;
- implement incremental checkpoint identity composition from family records;
- expose queue/replace/drop/retry metrics using fake time/jobs.

**Acceptance:** tests cover unsent coalescing without false gaps, per-stream reliable-frame/origin sequence separation, overlapping-key and multi-key barrier ordering, structural records remaining ordered in the scene-operation stream despite control-lane priority, unchanged written-preview retry until ACK/grace expiry, preview admission pressure, reliable retention/visible overflow, fairness/no starvation, tiny interactive work ahead of bulk backlog, large atomic body routing, reconnect with unacknowledged bulk followed by bounded cancel/interactive progress, ACK loss, same-key/disjoint-key conflict indexes, incremental checkpoint identity and stale job publication.

## T04 — In-process mutation boundary vertical slice

**Depends on:** T01 and T03.

**Goal:** validate setter-originated operation/apply semantics without networking.

**Work:**

- add scoped local/remote origin and echo suppression at business setters;
- emit/apply site color, complete cut definition and timeline anchor between two fixtures;
- introduce targeted invalidation for those operations;
- support their typed checkpoint export/apply using the same handlers;
- ensure nested derived setters/events under `Remote` context do not re-emit.

**Acceptance:** color is O(1), cut previews preserve newest value, timeline advances from an anchor, remote apply does not echo, automatic/derived callbacks do not escape context, and unrelated activity/collider flags remain unchanged.

## T05 — Persistent duplex transport v2

**Depends on:** T01 and T03.

**Goal:** transport v2 records bidirectionally without Unity scene dependencies.

**Work:** independent persistent authenticated read/write loops, TCP `NoDelay`, event-driven writer, bounded framing, lane multiplexing, chunk streams, scoped rejection, one liveness owner and clean cancellation.

**Acceptance:** loopback fake-peer tests cover both directions, fragmented/combined frames, malformed lengths, per-stream reliable resume/replay, reconnect with a large unacknowledged job or atomic-batch bulk where cancel/independent interactive traffic passes within the configured scheduler bound, ephemeral ACK/ping loss without continuity gaps, origin gaps, bulk-interactive fairness, partial bulk cancellation, read blocked at shutdown and writer empty/full at shutdown. No arbitrary sleep.

## T06 — Online sequencing, transient reconnect and early Quest proposal driver

**Depends on:** T03–T05.

**Goal:** prove bidirectional authority/failure semantics for the three profiles before broad coverage.

**Work:**

- bind one fake/prepared scene incarnation;
- implement Desktop canonical acceptance and per-key last-accepted indexes;
- implement Quest optimistic proposal/canonical echo deduplication and authoritative correction after rejection;
- add a Quest-origin driver for color, cut and timeline;
- implement fake-clock 500 ms grace, bounded retry and transition to offline-local;
- implement same-key Desktop-win and disjoint-key acceptance.

**Acceptance:** both directions converge; no duplicate apply after lost ACK; reconnect within grace is silent; expiry abandons retry history without destroying the view; stale/same-key/disjoint-key proposals behave as specified.

## T07 — Activity/invalidation concurrency foundation

**Depends on:** T04.

**Goal:** remove current global concurrency defects before real-scene v2 handlers expand.

**Work:**

- split projection requested, automatic policy, state and generation;
- remove the broad `Base3DScene.Update` freeze while native generators run;
- split `OnChangeSiteState` invalidation so highlight/color/labels do not invalidate activity;
- establish immutable/versioned input leases and stale-generation publish guards;
- implement deterministic projection-start versus sensitive-operation ordering/rejection hooks;
- keep full two-peer projection job/UI coordination for T14.

**Acceptance:** safe scene work progresses during a fake/controlled generator job, manual stale state does not auto-restart, old generations cannot publish, and sensitive race orders are deterministic.

## T08 — Real-scene v2 vertical slice, publication journal and cutover

**Depends on:** T02 and T04–T07.

**Goal:** activate the three-profile v2 path on a real prepared Desktop/Quest scene.

**Work:**

- wire real setters, session and early single-point Quest PlayerLoop apply;
- decode/validate off-thread and enqueue detached records;
- implement capture-boundary-to-Quest-publication mutation journal, ordered replay and overflow checkpoint fallback;
- abort/restart publication on unsupported resource/topology mutation;
- ensure a v2 scene never has the experimental `DesktopReplicaSession`/adapter owner active simultaneously;
- remove artificial next-frame/quiescence gates from this path;
- preserve Quest wrapper transforms and collect received/applied/visible telemetry.

**Acceptance:** color/cut/timeline work both directions without snapshot capture; mutation/structural change during initial transfer replays correctly; journal overflow falls back once; no double owner/send exists; T00 provisional p95/GC/main-thread budgets receive an explicit pass/fail decision.

## T09 — Selection, appearance and timeline coverage

**Depends on:** T08.

**Goal:** implement non-structural safe matrix families.

**Work:** D1–D2, D5, visual parts of D11–D13, D19, D21, safe parts of D23–D24/D26 and D27–D31, plus selected ROI sphere. Add DTO/handler/checkpoint record together for every family.

**Acceptance:** focused Desktop/Quest-driver tests prove payload, per-key order, invalidation, echo suppression and local-presentation exclusion. Timeline tests cover invalid/stale clock estimate, asymmetric latency and long uptime; correction occurs only beyond one sample.

## T10 — Structural and geometry coverage

**Depends on:** T09.

**Goal:** implement identity/topology-sensitive families.

**Work:** D3–D9, D14, D16–D18 and D20, each with typed checkpoint records. Add stable IDs, semantic automatic-cut IDs, command-only site movement parity qualification, resource barriers and original-topology sparse/bitset masks. Route any mask above the inline threshold through an independent bulk body without moving its descriptor/barrier out of scene-operation order.

**Acceptance:** non-last deletion/reorder preserves identity; derived effects do not re-emit; configuration/resource barriers are explicit; D14 position parity passes or the row remains blocked for a product decision; missing topology/resource rejects only the operation.

## T11 — Modality, bulk site and typed configuration coverage

**Depends on:** T10.

**Goal:** finish ordinary D1–D34 coverage outside long jobs/activity.

**Work:** remaining scientific D11–D13/D22–D26, D33 typed configuration transactions and D34 bulk assignments. Route large transaction bodies through independent bulk streams while their descriptors/touched-key barriers remain in scene-operation order. Add family-owned checkpoint export/apply; never add a generic state dictionary/capture adapter.

**Acceptance:** batches prevalidate and appear atomically per scene; 30,000-site/config bodies do not block an independent interactive operation during emission or resume; a one-site presentation edit remains O(1); mixed batches acquire the strongest lock; checkpoint/config application composes existing handlers without global invalidation.

## T12 — Online filter job

**Depends on:** T11.

**Goal:** implement Desktop-only online filtering with canonical result transfer and typed checkpoint result.

**Work:** command/generation, global busy scope, 200 ms delayed UI, Desktop compute, roster bitset, chunking, Quest atomic apply, offline capability gate, cancellation and stale rejection.

**Acceptance:** Quest does not compute online; both unlock after correct apply; confirmed loss cancels without offline resume; old/partial results cannot publish; network stays live internally.

## T13 — Online correlation job

**Depends on:** T12.

**Goal:** implement Desktop-only online correlation compute/load and typed checkpoint result.

**Work:** serialize command and bounded data/provenance, never file path; chunk/interleave; offline capability gate; atomic apply/cancel/failure.

**Acceptance:** compute/load share one result contract; partial pairs never publish; interaction/control traffic is not starved by large data.

## T14 — Dual activity-projection coordination

**Depends on:** T07 and T11–T13.

**Goal:** coordinate local calculation on both peers while allowing safe operations.

**Work:** start/progress/ready/cancel/fail generations, two-peer sensitive busy scope, automatic/manual policy propagation, newest safe pending parameter application and complete stale-result protection.

**Acceptance:** safe operations continue; sensitive operations are disabled/rejected with authoritative correction; unlock waits for both terminal states but transport never waits; disconnect cancels; no scene-sized input copy occurs on Unity thread.

## T15 — Product Quest controls

**Depends on:** T09–T14.

**Goal:** connect actual requested Quest interactions to the already-tested shared operations without changing protocol semantics.

**Work:** add controls incrementally according to product UI scope, using existing handlers/jobs and local optimistic application. Presentation-only Quest gestures stay local.

**Acceptance:** every exposed control passes the same focused behavior as its driver; adding UI does not introduce new operation types or alternate domain logic.

## T16 — Single-scene checkpoint reconciliation UX

**Depends on:** completed handler/checkpoint families and T06.

**Goal:** implement the confirmed no-merge policy for one common incarnation.

**Work:** checkpoint identity, automatic equal reconnect, Desktop/Quest choice, 200 ms delayed progress, bounded/cancelable staging, interaction lock, prevalidation, per-scene commit/failure, orphan indicator and retry after interrupted transfer.

**Acceptance:** either choice converges without source resources; Desktop-closed offline scene becomes local; interrupted staging applies nothing; commit failure is explicit/out-of-sync; no persistence survives process death.

## T17 — Multi-scene lifecycle, selection and global reconciliation choice

**Depends on:** T16.

**Goal:** add multiplexed scene lifecycle without changing protocol fundamentals.

**Work:** multiple scene/incarnation routing, open/full-delivery registration, online close, per-scene jobs/presentation, last-Quest-brain selected scene/column, and one global Desktop/Quest choice that invokes per-scene reconciliation for common incarnations.

**Acceptance:** no cross-scene operation; one slow/closing scene does not block another; one user choice is applied per scene with individual success/failure status; missing/orphan scenes are excluded.

## T18 — Legacy removal and full qualification

**Depends on:** all tasks required by the intended release.

**Goal:** remove dead experimental architecture and prove release gates.

**Work:** delete unused snapshot/delta/merge/hot-capture/session code and obsolete tests; complete matrix evidence; run fast/integration shards, slow real-scene qualification and physical USB/Wi-Fi milestones; compare live and initial-transfer performance with T00.

**Acceptance:** no active compatibility shim remains; applicable D1–D34 rows pass both driver directions; no stale job publishes; inactive Desktop and initial transfer meet gates; known limitations are explicit.

## Milestones

- **M1 architecture proof:** T00–T08. No full-coverage claim.
- **M2 complete online core:** T09–T14.
- **M3 real Quest editing:** T15.
- **M4 single-scene reconnect product:** T16.
- **M5 multi-scene lifecycle:** T17.
- **M6 cleanup/release qualification:** T18.
