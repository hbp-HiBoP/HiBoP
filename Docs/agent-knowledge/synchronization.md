# Desktop–Quest synchronization and session resources

Read the sections relevant to pairing/USB, full delivery, live state, jobs,
reconnection, multi-scene lifecycle or session preferences/atlases. This is the
current behavioral contract; the source files linked below own executable schemas
and limits. For ordinary loading costs use [Runtime and state](runtime-and-state.md);
for UI presentation use [UI and UX](ui-and-ux.md).

Consult selectively: [ownership](#product-and-ownership),
[pairing/USB](#pairing-and-usbwi-fi-discovery),
[full delivery](#full-scene-delivery-and-resource-identity),
[live operations](#live-mutations-scheduling-and-application),
[jobs](#scientific-jobs-and-cancellation),
[reconnect/lifecycle](#reconnection-and-scene-lifecycle),
[settings/atlases](#session-preferences-definitions-and-installed-atlases),
or [verification](#verification-and-observability).

## Product and ownership

One person alternates between one Desktop and one autonomous Quest. They share
the common scientific model. Send a prepared visualization once, then transmit
typed business operations with the lowest practical latency in either direction.
Desktop is the online sequencer, not a render server. A local edit never waits
for a network round trip or derived scientific calculation before being offered
to transport. Remote application should be eligible for the next feasible frame;
this is a statistical objective, not a hard one-frame computation guarantee.

Work is proportional to the changed value. No scene capture/diff, global scan,
hash, deep checkpoint clone, render fence or forced next-frame wait belongs to
an ordinary setter. A paired Quest without a shared scene adds no scene polling
or scene-sized work. Desktop interaction must remain usable with Quest absent,
sleeping, calculating or receiving a scene.

Shared intent includes scene/column/site/ROI/sphere selection, scene visibility,
cut definitions, scientific site positions and state, resource/hemisphere choices,
ROI/filter/triangle masks, timeline intent, colors and scientific parameters.
Local state includes cameras, Quest physical pose/rotation/scale and Single/Split
layout, hover/rays/tooltips, panel layout, tool focus, eraser brush and export/file
dialogs. A shared selection can drive locally derived geometry; that does not
make physical placement shared. Session `UserPreferences` ownership is separate
from these local scene/presentation values; see the settings section.

## Pairing and USB/Wi-Fi discovery

- Normal flow: launch HiBoP on both devices, choose a reachable Quest, enter its
  six-digit code for the first association, then send a visualization. Remembered
  trust permits reconnect without another code. Manual IPv4/IPv6 entry remains
  available even when discovery finds a different headset.
- Discovery is not authentication. Preserve the existing TLS-bound J-PAKE mutual
  confirmation, pinned identity and credential; do not send application data or
  a new credential before confirmation. Codes expire after five minutes and have
  at most five attempts, including malformed/aborted attempts. Do not put codes
  or secrets in discovery, logs or PlayerPrefs, or silently fall back to an
  unauthenticated mode. Normal restart preserves association.
- Product USB is NCM networking and discovery is mDNS/DNS-SD on USB and LAN.
  ADB is development tooling, never a runtime dependency or distributed binary.
  Keep the same application protocol, TLS identity and stored association across
  paths. An expired announcement/interface is not expired trust.
- Group candidate paths by TLS identity. Prefer USB when connecting/reconnecting;
  plugging USB into an active Wi-Fi session does not migrate that TCP connection.
  A manually opened session can resume after a cut even if automatic opening of
  new sessions is disabled. Explicit disconnect/cancel suspends that resumption.
- IPv6 link-local addresses require the local **Desktop** interface scope, not
  the Quest's numeric interface index. The service is `_hibop-quest._tcp.local.`,
  TCP 45871; mDNS uses UDP 5353. Ads contain version, identity, name and path only.
  Android listeners bind to their actual network; losing USB must not close LAN.
- Diagnose interface/address, discovery and authentication separately. If manual
  pairing works, do not replace cryptography to fix discovery. Scope any approved
  network adjustment to the relevant host/interface/application; do not disable
  global firewalls or IPv6. Do not infer OS/device qualification from compilation
  or another platform's result.

Implementation entry points: [QuestPairing](../../Assets/Scripts/HBP/Transfer/Transport/QuestPairing.cs),
[PairingPake](../../Assets/Scripts/HBP/Transfer/Transport/PairingPake.cs),
[QuestDiscovery](../../Assets/Scripts/HBP/Transfer/Transport/QuestDiscovery.cs), and
[QuestConnectionPanel](../../Assets/Scripts/HBP/Quest/Runtime/QuestConnectionPanel.cs).
Packaging and update identity are covered in [Builds and distribution](builds-and-distribution.md).

## Full scene delivery and resource identity

The transfer format is distinct from Desktop project persistence. Capture all
ordered columns and dependencies needed by their existing functionality: useful
patient/site metadata, modality data, trials/statistics, every required time
sample, units/masks, configurations, selections, cuts/ROI and available anatomy.
Do not reduce recordings to means or transfer an unrelated whole project. A
multi-patient scene does not require individual patient anatomy it cannot display;
opening a derived single-patient scene prepares that anatomy separately.

Deduplicate shared resources without aliasing mutable column state. Resources
must not depend on Desktop absolute paths, native handles, callbacks or executable
GameObject serialization. Derived caches may be rebuilt. Optional protocol
illustrations are not scientific buffers: absent/unreadable illustrations must
not block their definitions; received paths are empty and Desktop files remain
unchanged. Image bytes/paths are excluded from definition identity checks.

Wait for actual required preparation, not merely first visibility or a misleading
loaded flag. Capture coherent versions; validate schema, counts, dimensions,
relations and local availability before costly allocation. Prepare the received
rendering before atomic publication. Failure/cancellation preserves previous
published content, and resource release waits for real users/native work to finish.
Stream/stage without requiring a second whole payload in memory. Resource pressure
causes an explicit failure, never silent decimation or lost scientific data.

Identity layers are distinct: pairing/session, logical scene, concrete opening
`incarnationId`, immutable source `resourceManifestId`, stable entity IDs and
operation IDs. Closing/reopening or replacing a scene changes incarnation; a retry
retains the delivery identity required for idempotence. Delayed jobs/frames cannot
attach to a new opening. Collection positions and display names are not identities
for transferred resources; automatic cuts use stable semantic axis IDs.

Transferred user resources carry content integrity and provenance. Compute/cache
fingerprints on owned immutable buffers or while serializing/receiving the same
bytes; never add a second scene traversal/read/copy just to hash. Initial delivery
contains all selectable user source resources needed by live operations. A new
heavy dependency requires full delivery, not an incremental resource mutation.
Installed MNI/atlas resources are the explicit exception described below.

Desktop need not freeze for the whole delivery. At a coherent baseline, retain a
bounded journal of supported mutations while streaming. After Quest publication,
replay it before declaring live; overflow/unprovable baseline uses one current
checkpoint. Unsupported structural/resource/topology changes abort/restart the
delivery. This is a publication journal, not persistent offline history or normal
change detection. Keep in-flight publication separate from heartbeat reconnect
decisions; stale inventory responses cannot replace its current owner.

Entry points: [DesktopSceneCapture](../../Assets/Scripts/HBP/Transfer/Scene/DesktopSceneCapture.cs)
and [PreparedSceneManifest](../../Assets/Scripts/HBP/Transfer/Scene/PreparedSceneManifest.cs).
Preserve [Data and persistence](data-and-persistence.md) when changing transfer
DTOs; a transport refactor does not authorize changing saved project fields.

## Live mutations, scheduling and application

Use versioned absolute business assignments, not UI events, controller trajectories
or replay-sensitive deltas. Local/remote callers use the same handlers and targeted
invalidation. Remote context suppresses re-emission while preserving validation,
derived work and UI refresh. An identical canonical echo confirms the optimistic
Quest proposal; a changed/rejected value follows the authoritative correction path.
Desktop wins genuine concurrent conflicts on overlapping keys, not every Quest edit.

Each operation identifies session/scene/incarnation, operation ID, origin and
origin sequence; Desktop assigns canonical sequence. Retry preserves IDs and
immutable bytes. Assign wire sequence only after unsent coalescing. Handler-owned
validated metadata defines touched keys/barriers; arbitrary UI-provided key lists
cannot decide ordering. Validate identities, schema, finite values, bounds, payload
length and resource/topology dependencies before one atomic apply.
Reject invalid domain operations with their ID and a scoped reason while keeping
independent operations/scenes active. Missing resource, topology or supported
schema dependencies require a full scene resend; never partially apply them or
terminate an otherwise healthy socket merely for a domain rejection.

| Operation family | Required behavior |
| --- | --- |
| Selections | Share scene/column/site/ROI/sphere identity; automatic cuts and ROI masks derive locally from accepted causes. Hover never selects a new context. |
| Cuts and ROI spheres | Send complete small definitions atomically. Create/delete are reliable structural operations; definitions coalesce by object. Never renumber identities after deletion. |
| Site state | A multi-site gesture/import is one final-value batch, preserving targeting/masking rules and skipping unchanged assignments. Blacklist affects scientific masks; highlight/color/ordered labels do not invalidate activity. |
| ROI/filtering | Share ROI definitions and derive its mask locally. Filtering uses command plus canonical result as described under jobs. |
| Scientific site placement | Send the Left/Right/Reset domain command. Do not silently substitute raw position arrays if cross-platform geometry parity fails. |
| Mesh/MRI/implantation and modality resources | Use stable prepared IDs and explicit resource/topology barriers; clear dependent references deterministically. MRI calibration updates cut appearance; resource changes may invalidate projection. |
| Colors/alpha/edges/labels | Send requested parameters; derive material/shader appearance locally. A toolbar's global mode is not itself shared state. |
| Triangle erasure | Send exact original-topology IDs as sparse list or bitset, never gestures or renumbered topology. Undo, if present, is another resulting mask, not a generic application-wide undo contract. |
| Timeline | Send index/play/loop/step and monotonic anchor; advance playback locally. Only Play uses clock estimation. Pause/Seek/Step/Loop preserve transmitted index exactly, including edits during playback; automatic ticks are not mutations. |
| Thresholds/influence/CCEP source | Preserve atomic parameter groups and explicit source intent. Influence/source changes are scientific invalidations; safe prepared-output adjustments keep the newest pending value. |
| Atlases/localizers | Send logical source/contrast/area/time/threshold choices only when dependencies are ready on both peers; derive overlays locally. |
| Configuration load/reset | One validated typed transaction across affected operation families, with the strongest contained invalidation; no visible partial setter replay or generic snapshot dictionary. |
| Export/project authoring | Local operation; exporting current scene does not become a live mutation. New source resources still require full delivery. |

Every local continuous sample is applied and offered to the scheduler. Only an
unsent superseded value with the same key can be replaced; always retain the final
remaining value. Structural, job and transaction barriers seal earlier slots.
Incoming preview reduction is bounded and cannot cross semantic barriers or alter
immutable reliable retry history. Play/Pause/Step/Loop are timeline boundaries.

Use the persistent authenticated duplex transport with independent read/write
loops and event-driven writers. Control, interactive and bulk lanes preserve
semantic per-stream ordering. Large bodies use a small ordered descriptor plus
independent bounded bulk chunks; validate the whole body before apply. Bulk must
yield to interactions without starving. The current scheduler starts with 4 KiB
inline bodies, 16 KiB chunks and an eight-interaction burst; inspect executable
constants before tuning. Never block Unity waiting for socket capacity.

All queues/caches are bounded in bytes/count. Retain written reliable frames
unchanged until acknowledged or grace expiry; required structural/final/cancel work
is not silently dropped on saturation. Transport ACK means receive admission,
not Unity application. Application progress retires only completed work, includes
pending proposals/bulk and does not create ACK-of-ACK loops. Healthy sessions have
no cumulative operation-count limit: the 4096 decision bound applies to unresolved
work. Retire completed state safely; increasing a limit is not a retention design.
Keep conflict keys until Quest proves no pending proposal can predate them;
retirement advances a scoped minimum-observation floor so an older proposal
cannot bypass conflict checks after its key/tombstone was removed. A dequeued
application record or reconstructed bulk body survives a short transport cut
until application succeeds; socket cancellation must not erase its completion.

Entry point: [SceneMutationBoundary](../../Assets/Scripts/HBP/Sync/Scene/SceneMutationBoundary.cs).
Checkpoints use the same typed family records/handlers for initial agreement and
reconnect. They exclude source bytes, Unity objects and local presentation and
must never become a per-interaction scene observer.

## Scientific jobs and cancellation

Key every job by incarnation, type, ID and monotonic generation. Only the current
generation may publish or unlock controls; a late completion cannot overwrite a
new job. Keep network loops running through compute, staging and application.

| Job | Online executor and completion |
| --- | --- |
| Filter | Desktop computes the command, sends complete roster-bound inclusion bitset, Quest validates/applies atomically and confirms readiness. No provisional Quest online calculation. |
| Correlation | Desktop computes or loads canonical result; send bounded versioned/chunked data and provenance, never Desktop file paths. Partial pairs remain invisible. |
| Activity projection | Both devices compute locally from common prepared inputs. Transfer intent/readiness, never full activity result. Sensitive controls wait for the generation's terminal state; safe mutations continue. |
| Surface inflation | Both compute/reuse matching local caches from current transformed anatomical buffers; retain prior display until both prepared. Then local transition and canonical representation commit complete without a new full scene delivery. |

Freeze scientific inputs through immutable buffers or versioned leases, not a
deep scene copy. During projection, safe operations include camera/wrapper motion,
selection, cuts, colors/labels/highlight, timeline and proven prepared-output span
adjustments. Sensitive operations include filters/blacklist, active ROI/sphere
geometry, scientific positions/influence, CCEP source, source resources/topology,
triangle visibility and native projection settings. Mixed batches are sensitive.
Derive this classification from actual computation dependencies, not accidental
toolbar grouping. A local rendering update is not necessarily a field recompute.

Projection requested state, automatic policy, readiness/staleness and generation
are separate. Manual invalidation marks stale and waits for a click; automatic
mode schedules the newest accepted generation. Removing activity cancels it and
clears derived presentation. Ready results recheck input and incarnation identity.

Cancel from either peer cancels the coordinated generation. After the 500 ms
disconnect grace, cancel online jobs and unlock offline use; do not automatically
restart the job offline. Quest may explicitly start local filters/correlations only
if delivered inputs/capabilities suffice. A noninterruptible native call finishes
privately before cleanup; its stale result cannot publish or hold the network lock.
Desktop filter/correlation failure discards partial results and unlocks both peers.
Quest apply failure remains failed and visibly unsynchronized; missing roster or
resource requires full resend. One-peer activity failure fails/cancels the shared
generation and identifies the failing peer. Discard obsolete completions without
presenting them as failures of the current job.

Inflation captures source references/geometry versions, mesh part and exact
options/algorithm version; the [scientific inflation contract](scientific-computing.md#inflation-display-geometry-anatomical-science)
owns buffers, topology and cache semantics. Prepared results require
`CurrentSurfaceCoordinates`; reject historical native-GIFTI markers without silent
recompute or renumbering their enum/container version. Deploy both peers with the
same algorithm version.

Quest drains the preceding canonical watermark before preparing. After both
preparations, animated requests complete the local transitions on **both** peers
before Desktop publishes canonical `SetMeshDisplay`; nonanimated requests skip
that transition barrier. Quest acknowledges commit only after applying its
canonical watermark and verifying the selected representation. Unlock only when
this completion handshake ends; peer/barrier waits are bounded (currently ten
minutes). Rebuild pending mesh/hemisphere geometry before transition, and defer
automatic rebuilds until it finishes. During reconnect grace, Quest rejects new
surface jobs until connected or explicitly offline.

The [UI loading boundary](ui-and-ux.md#progress-and-operation-boundaries) closes
preparation feedback before animation/commit. A canceled pre-publication job
preserves the accepted display; cancel after canonical commit does not undo it.
Preserve local Quest pose throughout.

## Reconnection and scene lifecycle

For a transient cut, keep a silent 500 ms grace and retry stable operation IDs.
After grace, show disconnection, cancel coordinated jobs, discard retry history
and continue with current local state. No process-crash/close persistence is
promised. Reconnecting restores Desktop-authoritative settings before reconciling
common scene incarnations; it does not send source/atlas files.

Keep the last confirmed common typed state in memory. Reconnection performs a
semantic three-way merge: equal changes are silent, independent attributes/entities
combine, and dependent conflicts require Desktop/Quest choices. Natural playback
advance is not an explicit timeline edit. If no common base remains, ask for a
whole-scene choice. Conflict UI has no Cancel choice and explains preservation of
independent changes; transport/application cancellation remains supported.

Guard interaction while choosing/staging/applying, validate the complete candidate
and prepare necessary local representations before commit. Failure before commit
applies nothing. Failure after commit begins stays visibly out of sync and guarded;
retry keeps the exact candidate/guard. A successful handshake establishes a fresh
common base. Never turn reconciliation retry into a fresh delivery that overwrites
retained offline edits. Preserve local pose. There is no cross-scene rollback promise.

Automatic scene membership synchronization is a default-on Desktop setting shared
across headsets. Enabled: Desktop inventory is authoritative; send missing open
scenes and close extra Quest scenes after connect/reconnect or enabling. Connected
closes propagate both ways; sequence a Quest close before comparing inventories.
Disabled: existing common scenes still synchronize mutations/selection/visibility,
but subsequent closes are local. A Quest scene whose Desktop incarnation vanished
becomes permanently local/orphaned, not reattached by reopening the same source.
Re-enabling membership closes orphans to match Desktop. Distinguish a connected
Desktop session from an individual scene that is no longer shared.

Close uses scene-operation/bulk watermarks to apply or discard outstanding work;
delayed frames are fenced. Cancel and await that incarnation's preparation/jobs
without blocking other scenes. Scene visibility hides rendering and interactions,
terminates captures and retains resources/synchronization. Showing it does not
resume stale gestures. Intentional brain/site/cut interaction selects the owning
scene/column; capture that target for the gesture and never redirect on selection
change. Physical pose and layout stay local.

Opening a patient from a Quest multi-scene is online-only and requests Desktop's
existing derivation. Publish it on both devices even with automatic membership
off, through one deduplicated path. Same source opening+patient reuses, shows and
selects an existing child; different source openings may each have one. Preserve
column definitions/anatomy preferences, discard source-topology erasure and transient
MRI preview, and do not couple future parent/child parameters. Closing the parent
does not close children. Capture target IDs before async waits; stale targets fail
explicitly. Progress starts at the request, not only when transfer starts.

Entry points: [SceneCheckpointMerge](../../Assets/Scripts/HBP/Sync/Scene/SceneCheckpointMerge.cs),
[SceneReconciliationProtocol](../../Assets/Scripts/HBP/Sync/Scene/SceneReconciliationProtocol.cs),
and [QuestAnatomySession](../../Assets/Scripts/HBP/Quest/Runtime/QuestAnatomySession.cs).

## Session preferences, definitions and installed atlases

Pairing installs a fully validated in-memory global context: preferences, tags,
protocols with available illustrations, aliases, filter presets and global native
settings. Resolve scene references and trial/timeline dictionary keys against this
context. Missing/ambiguous definitions or another pairing identity fail explicitly.
Do not write Desktop data or the Quest preferences file as a side effect.

Desktop owns **all UserPreferences**, even without a shared scene. A successful
save validates before side effects, applies on Unity, keeps the Quest object and
listeners, and notifies once. Alias/tag/protocol modifications also update the
connected Quest without re-pairing. These are definition snapshots, not patient
recording transfers. Do not resend an unchanged context for every scene. Connection
generation, operation UUID and monotonic revision prevent stale application;
ACK follows apply. UUID reuse with changed payload is invalid. Disconnected pending
saves/atlas operations are abandoned; reconnect installs fresh current state.

Reload-requiring preferences use a guarded cycle: capture current views on Unity,
retire affected incarnations on both peers with awaited cleanup, apply settings,
reload Desktop scenes and deliver fresh incarnations, preserving independent
derived openings. Reject before save/mutation/transmission while shared scenes
are disconnected, transfer/reconciliation is pending or orphan sources cannot be
reloaded. Report per-scene failure/retry; release guards even if initial capture fails.

Installed MNI and atlas/localizer files are local reference resources, not scene
payload files. Their runtime contract is logical identity and native readiness,
not a global content hash or installed-content mismatch comparison. User-managed
content consistency is the user's responsibility. Do not add runtime scans or a
new build-hash scheme. Preserve actual FMRI/Volume load provenance and integrity
of transferred user data. The Android installation/update manifest has its own
role and does not gate use or sync. Localizers are distributed separately; see
[Builds and distribution](builds-and-distribution.md) for packaging boundaries.

Atlas loading is sequential, awaitable and shared per atlas; publish only fully
prepared native candidates and reuse already-loaded objects without disk scans.
Unload then Load adopts changed files. Cancellation awaits native work before
release; partial successes survive. Desktop load/use does not wait for sleeping
Quest; sync resumes automatically when Quest is ready. Shared use requires both
peers' readiness and missing data fails with a useful error, not partial apply.

Unload reserves both peers, excludes new uses and releases only when scene/native
users permit. Release reservations on failed/ambiguous attempts; remote reservations
expire. Existing retries are bounded to three attempts with cancelable 500/1000 ms
pauses and no endless inventory retry after exhaustion. Persistent failures use
one Desktop error dialog, not status/Retry clutter in preferences; cancellation
or disconnect abandons retries. Manual atlas buttons act immediately; preferences
Cancel does not undo them. Auto-load flags apply on Desktop startup/new Quest
session installation, not every save. An unrelated global atlas does not invalidate
delivered mesh/MRI.

Entry points: [DesktopSessionPreferences](../../Assets/Scripts/HBP/UI/Quest/DesktopSessionPreferences.cs),
[QuestManager.Definitions](../../Assets/Scripts/HBP/UI/Quest/QuestManager.Definitions.cs),
and [SessionPreferencesReceiver](../../Assets/Scripts/HBP/Sync/Scene/SessionPreferencesReceiver.cs).

## Verification and observability

Use [Testing](testing.md#determinism-and-focused-regression-evidence) for tiers,
operation-family coverage, controlled clocks and generation-race tests. Sync-specific
scenarios include lost ACK after apply, typed checkpoint round trips, coalescing/
reliability, unavailable resources, shutdown/close during staged native work,
publication overflow and independent interactive work during bulk transfer.
A cut demonstration alone does not verify every operation family.

Measure true setter/request boundaries; receipt, Unity application, visible frame
and scientific stability are distinct. Keep logical trace, capture generation and
network attempt identities separate. Measure at these production boundaries;
[Testing](testing.md#determinism-and-focused-regression-evidence) owns telemetry
verification, disabled-path cost and measurement isolation.
