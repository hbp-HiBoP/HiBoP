# S3 active Desktop–Quest session

## Implementation

After a prepared scene is successfully published, `QuestManager` creates one
`DesktopReplicaSession` for that scene. An unsent Desktop scene has no replica
adapter or capture loop. The active session reacts to scene modification events,
keeps only the latest pending state, and sends a full checkpoint followed
by ordered field deltas over an authenticated persistent connection. The Quest
applies the changed state groups to its existing `Base3DScene`; its column pose, scale, and local
surface visibility remain presentation state. The receiver acknowledges receipt,
application, and visibility separately. Reconnection checks the revision and
state hash, then resends a checkpoint without republishing the scene.

The open scene's prepared resources are immutable. Scene transfer uses the MNI
provenance already recorded when the scene opened and does not scan the atlas
directory. The binding checks the published manifest identity and cheap resource
roster; it does not rehash reconstructed meshes. Correlation results used by live
state travel as verified content-addressed resources on the control connection.
Cut edits capture only cut fields, timeline ticks capture only timeline fields,
and Quest applies those two delta types without walking unrelated geometry or
sites. Scene, column, site and ROI operations use change notifications; the
session does no idle full-scene polling. Other notified operations currently
capture the complete canonical state to form their deltas, while Quest applies
only the affected groups. Full D1–D34 event and latency coverage remains a
release gate.

S3 still has an operation rejection limitation: a missing local atlas is rejected
by Quest and shown on Desktop, but Desktop does not yet undo only the rejected
operation and resume the stream. That requires a per-operation pending queue with
conditional inverse patches, so that a late rejection cannot erase a newer user
edit. Add this with Quest-origin operation handling in S4 and three-way
reconciliation in S5. An initial snapshot selecting an unavailable atlas needs
a clear unavailable-resource status rather than an implicit retry.

## Localizers

Localizer IRM and mask files are separately installed local resources. Neither
the Desktop nor Quest application build contains them, and scene delivery never
includes them. Shared localizer selection uses content-derived references. The
Quest accepts that state only when the matching local protocol, data, bloc,
volume, and mask are already available. In the current device environment those
files are not installed on Quest, so D30 localizer rendering cannot be validated
on the device yet. Its S2 in-process state and scientific output checks passed
with the resource installed locally.

## Verification

- Focused delta/codec EditMode tests: 3 passed.
- Persistent pairing heartbeat test: 1 passed.
- S3 local PlayMode socket, snapshot, delta, reconnection, visible acknowledgement,
  and presentation preservation: 1 passed after the localizer correction.
- Live scene capture and localizer-independent delivery PlayMode test: 1 passed
  after the correction, including a direct archive check that loaded localizer
  bytes are absent and local wrapper pose preservation on republish.
- Broader sync and transfer EditMode suites after the correction: 121 passed.
- After the event-driven correction, the S3 local PlayMode case passed with
  cut creation and movement in the existing Quest scene (1/1). The sync and
  transfer EditMode suites passed 121/121 after removing the conflicting ADB
  port forward for the test run; the forward was restored afterward.
- Physical Quest, final signed APK: 1 PlayMode scenario passed (31.7 s). It
  published a prepared scene, applied snapshot and delta, received separate
  received/applied/visible acknowledgements, and resumed from the Quest
  checkpoint after reconnection.
- Final signed APK: SHA-256
  `ebd4f54006bca8677821153809d40590ff9ffb5bcf9fede430c0915034fa49e5`,
  322,476,761 bytes, 9 ARM64 libraries, no localizer entries. The existing
  pairing and app data were preserved by an in-place install.

The user's real scene was transferred to the physical Quest without the previous
mesh-fingerprint rejection. Creating and moving a cut produced visible Quest
acknowledgements (revisions 2 and 3), and the user confirmed both changes were
visible in the headset. Desktop measured about 60 frames/s after transfer. A
follow-up removal of the last cut exposed an unrelated camera overlay indexing
exception; its zero-cut guard was corrected afterward.

The on-device D30 check remains pending until localizer resources are installed
separately on Quest. Full D1–D34 coverage remains a release gate.
S4 Quest-origin edits, S5 offline reconciliation, and S6 throughput and full
device parity remain their own release gates.
