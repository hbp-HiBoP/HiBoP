# Requirements and boundaries

## Product scenario

One person uses one Desktop and one Quest, alternating between them. A visualization is prepared and sent once. Thereafter, edits to its shared scientific/visible intent propagate with the lowest practical latency in both directions.

The target for ordinary operations is:

1. the local business setter records and applies the change;
2. transport work begins without waiting for derived calculation or rendering;
3. the remote receives and applies the same business operation at its earliest Unity update opportunity;
4. the result is eligible for the following rendered frame.

This is a statistical latency objective, not a promise that network and arbitrary scientific computation always complete within one refresh interval. The system must avoid self-inflicted idle frames, polling delays and global barriers.

## Functional requirements

| ID | Requirement |
| --- | --- |
| R1 | Local application never waits for a network round trip. |
| R2 | An ordinary connected mutation performs work proportional to the changed value, not the number of sites, triangles, columns or other scene entities. |
| R3 | Desktop orders accepted operations while connected. A non-conflicting Quest proposal is accepted; Desktop wins a genuinely concurrent conflict. |
| R4 | Every sample from a continuous setter is applied locally and offered to the scheduler. The remote need not receive superseded unsent previews: the newest unsent value may replace them; structural operations are never coalesced. |
| R5 | Once a value stream stops, its latest value is retained until delivered or superseded by a later user value. |
| R6 | Immutable/selectable source resources such as meshes, MRI and functional datasets cross the wire only during a full scene delivery. Live operations reference the initial manifest. Bounded derived job results such as correlations are not source resources and may be streamed live. |
| R7 | Calculations are local unless an operation explicitly declares a canonical result transfer. Filters and correlations are the confirmed exceptions while online. |
| R8 | Remote application invokes the same domain operation as local interaction and performs only its targeted invalidations. |
| R9 | Selection of column, site, ROI and ROI sphere is shared in the single-scene core. Selected-scene synchronization is added with multi-scene T17. Hover, pointer state, camera and physical Quest wrapper transforms are local. |
| R10 | Pairing without an active delivered scene adds no scene polling or scene-sized work. A disconnected or connected Quest must not degrade normal Desktop interaction. |
| R11 | Transport remains responsive while calculations and bulk results are active. Small interactive messages can overtake bulk chunks. |
| R12 | A transient disconnect does not destroy the Quest scene. Offline process state is not durable across application crash or close. |
| R13 | Reconnection selects one current state; it does not merge branches. |
| R14 | A missing/incompatible resource rejects the dependent operation visibly and requires a full scene resend. Partial application is forbidden. |
| R15 | The implementation and its tests preserve short development cycles. Fast tests may not depend on real time, real sockets, device availability or full scene construction. |

## Scale assumptions

The live operation design must remain predictable with approximately:

- 30,000 sites;
- a 300,000-triangle base mesh;
- eight columns;
- three cuts;
- one ROI with three spheres;
- large functional datasets already installed by the initial delivery.

These sizes do not authorize the sync layer to manage scientific computation. They constrain encoding and algorithms: a one-site color change cannot scan 30,000 sites, while a deliberate 30,000-site batch may legitimately be O(30,000).

## Shared intent versus local presentation

Shared intent includes scientific inputs and visible choices that should describe the same visualization: selections, cut definitions, ROI geometry/activation, filters, site states, resource selection, hemisphere, representation, triangle visibility, timeline controls, colors and overlay parameters.

Local presentation includes Desktop cameras and panels; Quest pose, grab, recentering, physical column layout and scale; hover, tooltip and pointer; file dialogs; progress-widget layout; and local tool focus. The removed Quest “hide surface” feature is not part of the contract. The selected anatomical hemisphere is shared.

## Calculation boundary

Operations normally send inputs before local derived work finishes. Cuts send their full definition, ROI sends sphere parameters, activity sends projection intent, and both peers calculate locally.

Confirmed canonical-result exceptions:

- a filter sends its command, then the final site-inclusion bitset calculated by Desktop while online;
- a correlation sends its command, then the final correlation data calculated or loaded by Desktop while online;
- a triangle erase sends the exact affected original triangle identities/mask rather than replaying the gesture.

## Explicit non-goals for the first refactor

- automatic three-way merge of offline edits;
- persistent recovery after either process crashes;
- multiple simultaneous Quest devices or users;
- live synchronization of source project, patients, protocols or imported source databases;
- loading a new heavy resource incrementally after initial delivery;
- initial implementation of multi-scene UI lifecycle, although protocol identity must support it;
- guaranteeing identical frame rates or shader pixels between devices.

## Release boundary

The online core is not complete until every applicable D1–D34 family in `operation-matrix.md` has an explicit operation, apply path and verification result. A demonstration limited to cuts is a vertical slice, not a release.
