# Long-running jobs and busy scopes

## Common job model

Every job is keyed by scene incarnation, job type, job ID and monotonic generation.

```text
Idle -> Starting -> Computing -> Streaming? -> Applying? -> Ready
                         \-> Cancelling -> Cancelled
                         \-> Failed
```

Only the current generation may publish a result or unlock controls. Cancellation makes all later completion/chunk messages from that generation inert.

A loading visual is delayed by 200 ms to avoid flashing. Progress may describe compute and transfer phases separately. The UI lock is a domain busy scope; it never pauses read/write loops.

## Online filter

1. Either device issues a serializable filter command.
2. Desktop assigns/accepts the job generation and both peers enter the filter busy scope.
3. Desktop alone evaluates the filter.
4. Desktop produces the immutable-roster inclusion bitset and streams it with command generation.
5. Quest validates roster identity and applies the complete bitset atomically.
6. Quest returns job-ready; both peers leave the busy scope.

Quest does not perform a provisional online filter calculation. The Desktop result is canonical. A reset filter is the same job/result path with an all-included result when appropriate.

Offline, Quest runs the command locally only when the initial manifest and runtime report that every filter input/capability required by that command is available. Otherwise disable/reject it with an explicit explanation. An online job interrupted beyond the disconnect grace is cancelled and must be restarted explicitly; it is not resumed offline.

## Online correlations

The lifecycle matches filtering. Desktop alone computes or loads the result while online. The command contains reproducible selection/parameters, not a UI event. A file-loaded result sends only canonical correlation data and provenance, never the Desktop path.

Correlation result encoding must be bounded, versioned and chunked. Partial pairs never become visible. Display enable/disable can remain an ordinary small mutation after a result exists.

Quest computes correlations itself only for an explicitly started offline job and only when the delivered data/capability manifest proves the necessary source samples are present. Rendering-only resources are not assumed sufficient.

## Activity projection

Activity results are too large to transfer and are computed locally on both peers from common inputs/resources.

1. Desktop sequences a projection request generation (including a Quest-origin request).
2. Both peers freeze the generation's scientific inputs and start local compute.
3. Each reports local progress and `Ready`, `Cancelled` or `Failed`.
4. Sensitive controls stay disabled until both peers reach a terminal state for that generation.
5. Safe mutations continue to apply and synchronize.

The network never waits. A late result verifies scene/incarnation, projection generation and input generation immediately before publication.

“Freeze inputs” means acquire immutable prepared buffers or versioned/reference-counted leases and copy only the small parameter/mask records needed by the generator. It must not deep-copy the scene or large functional resources on the Unity thread when a job starts. A mutation that would invalidate a leased input is classified sensitive and cannot commit during the generation.

### Safe during activity calculation

- camera/Quest wrapper movement;
- scene/column/site selection;
- cut create/delete/definition;
- colors, labels, highlight and rendering appearance;
- timeline control/advance;
- thresholds/spans proven to adjust prepared output safely, retaining their newest pending assignment.

### Sensitive during activity calculation

- filters and blacklist;
- active ROI and ROI sphere changes;
- scientific site positions;
- influence distances;
- CCEP source;
- implantation, selected MRI, mesh/topology/representation barriers;
- triangle visibility changes;
- projection-grid/native preferences.

The operation matrix may refine a classification only with evidence from the actual computation dependency. “Current UI happens to disable it” is not sufficient evidence.

## Manual versus automatic activity

`ProjectionRequested`, `AutomaticRecomputeEnabled`, `ProjectionState` and `ProjectionGeneration` are independent.

- Manual mode: invalidating input changes `Ready -> Stale`; no compute starts until requested.
- Automatic mode: invalidation schedules the newest generation after the mutation is accepted.
- Remove activity: sets requested false, cancels active generation and clears/updates derived presentation locally.
- Pairing transfers the Desktop user preference. If it changes during a connected session, Desktop sends the new policy immediately.

## Cancellation and disconnection

A user cancellation from either peer is a reliable job command and cancels both peers for that generation. Confirmed connection loss cancels coordinated jobs locally after 500 ms and unlocks the offline interface. Native work that cannot stop mid-call may finish privately, but its result is discarded and may not hold the network/session lock.

## Failures

- Desktop filter/correlation failure: send failed, discard partial result and unlock both.
- Quest apply failure: keep job failed, show error and do not claim synchronized; missing resource/roster requires full resend.
- One-side activity failure: cancel/mark the generation failed on both and show which peer failed.
- Obsolete completion: discard silently with telemetry.
