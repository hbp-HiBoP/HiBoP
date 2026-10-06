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


## Surface inflation

The base mesh remains a delivery-bound prepared resource. Its inflated representation can be computed after publication, without another scene delivery.

1. Either device requests the representation through the scene's representation request port. Desktop assigns a surface-inflation job generation and freezes the delivery-bound mesh resource ID, hemisphere, representation, exact options and algorithm version (2). Each local job captures the references and `GeometryVersion` of anatomical `Both`, `Left` and `Right` when present.
2. Both devices reserve a domain busy scope. Quest first drains the preceding canonical watermark. Read/write loops and safe mutations continue.
3. Each device computes or reuses the exact cache entry. The displayed representation remains unchanged during preparation. Progress is ephemeral; start, ready, cancellation, failure and commit controls are reliable and bounded.
4. When both preparations are ready, an animated request starts the existing 0.6 s local GPU transition on each device. Both transition completions are acknowledged before Desktop publishes the ordinary canonical `SetMeshDisplay`; intermediate animation frames never become scientific mutations. A request without animation skips this barrier. Quest acknowledges the commit only after applying its canonical watermark and checking the selected representation. Controls unlock after completion.
5. Cancellation or failure before publication preserves the previous display. Disconnect, closure and stale job completions release the scope after native work stops; they never turn an interrupted online calculation into an offline calculation. A cancellation received after canonical publication cannot undo that accepted display mutation. Peer/barrier waits have a ten-minute upper bound.

LoadingManager wraps only mesh preparation and waiting for the peer's preparation, with its existing 200 ms delay. That operation is awaited and closed before the transition starts; animation and commit execute outside LoadingManager. Cached switches normally finish preparation before the visual appears. Offline requests retain local behavior. During reconnect grace, Quest rejects new surface jobs until connected or explicitly offline. Pending mesh/hemisphere geometry is rebuilt before a transition; automatic rebuilds are deferred until it finishes. Success and cancellation restore the accepted display without resetting triangle masks or scientific UV/colors.

All meshes inflate from the transformed anatomical buffers already in memory, including MNI and MRI-generated surfaces. The delivery carries the existing prepared buffers and optional prepared inflated representations; no dedicated GIFTI/transform inflation inputs or file fingerprints are transferred. Each job checks the captured surface references/versions before and after preparation and before publication, alongside the selected mesh/hemisphere and roster checks. These guards retain no coordinate copies. Patient anatomy excluded from a multi visualization remains excluded.

Prepared inflated results must use `CurrentSurfaceCoordinates`. Transfer validation and `Mesh3D.FromPrepared` explicitly reject historical `NativeGifti` and `NativeGiftiThenTransformed` markers and request preparation/delivery again, without silent recomputation. Their enum numbers and the transfer container version remain unchanged. Deploy Desktop and Quest together with algorithm version 2.

Full anatomical topology, scientific colors/UVs, triangle masks and the local Quest wrapper placement are preserved. The original anatomical simplification remains the reference for scientific masks; an independently simplified inflated surface does not redefine its topology identity.
