# Shared operations and computations

The [mutation boundary](../../../../Assets/Scripts/HBP/Sync/Scene/SceneMutationBoundary.cs) owns validation, application, invalidation and checkpoints for the families below. D1–D39 identifiers serve only to locate earlier evidence; numerical protocol names are defined in [MutationContract](../../../../Assets/Scripts/HBP/Sync/MutationContract.cs) and contracts `T09` through `T13` in the same directory. A handler's existence does not prove hardware qualification: see [validation](synchronization-validation.md).

## Current matrix

"Latest value" allows replacement of an **unsent** value on the same key; the last remaining value remains reliable. Structure, results and transactions are reliable and ordered. "Safe" means allowed during activity projection; targeted rendering can nevertheless be working. A mixed batch takes the strongest scientific constraint.

| Family | Transmitted operation / state | Effect and constraint |
| --- | --- | --- |
| D1–D2 | `SetSelectedColumn`, `SetSelectedSite` | Shared scientific selection; a new site is subject to effective visibility. Automatic cuts derived locally. Safe. |
| D3–D6 | `CreateCut`, `DeleteCut`, `SetCutOrder`, `SetCutDefinition`, Strong/Soft, AutomaticCut policy | Stable identities, three semantic automatic axes. Atomic full definition, latest value per cut; reliable structure. Safe, targeted geometry/textures/colliders. |
| D7–D9 | ROI creation/name/deletion, active ROI, spheres, sphere selection | Import = resulting content, without path. Scientific geometry/radius sensitive; sphere selection safe; animated radius local. ROI mask computed locally. |
| D10 | Filter command then `SetSiteFilterResult` | `IsFiltered` and roster identity + bitset. Canonical Desktop job online; details below. |
| D11–D13 | Blacklist, highlight, requested color, ordered labels, HideBlacklisted, ShowAllSites, gain | Blacklist/ShowAllSites affect the scientific mask; highlight/color/labels/gain and visual masking policy do not invalidate the activity field. |
| D14 | `MoveSites(Left|Right|Reset)` | Reliable scientific command, not a trajectory or substituted position array. Absolute placement retained in configurations/checkpoints. Windows/ARM numerical parity distinct from visual acceptance. |
| D15 | Correlation computation/loading, reset/display | Canonical matrices/provenance, never Desktop path; computation/loading by job, display safe. |
| D16 | `SetMeshDisplay`: prepared resource, hemisphere, representation | Sensitive resource/topology barrier. Inflation computed locally by coordinated job; no new delivery for the derivative. |
| D17–D18 | MRI, min/max calibration, implantation | Sensitive resource choice, prepared identity and cleanup of dependent references. Atomic MRI contrast safe for the activity field, updates cuts. |
| D19–D21 | Brain/cut colors, colormap, edges, alpha/transparency, activity; triangle mask | Safe appearance. `ApplyTriangleMask` sensitive during computation: original topology IDs, sparse list or atomic bitset, not erasure gestures. Possible undo = new resulting mask. |
| D22–D26 | Influence; static label; iEEG/CCEP span; CCEP source; fMRI/MEG resource, thresholds and hidden ranges | Resources/influence/source are sensitive; spans and parameters of an already prepared output can retain the latest safe value during computation. Explicit CCEP source, distinct from merely the selected site. |
| D27 | `SetTimelineAnchor`: index/play/loop/step, monotonic time and intent | Safe, local playback; no message per tick. Clock rules below. |
| D28–D31 | Mars/JuBrain, IBC/DiFuMo, localizer, fMRI atlas calibration | Logical source/area/time/parameter choices. Installed dependencies ready on both devices; overlays derived locally. No atlas transfer. |
| D32 | Activity request/removal/automatic policy | Local computation on each device; coordination of generation and availability, not transfer of the activity result. |
| D33 | `SetConfigurationTransaction` | One typed transaction prevalidated against staged resources and identities; strongest barrier, atomic application, restoration on failure. |
| D34 | `SetSiteConfigurationBatch` | One gesture/import = final blacklist/highlight/color/ordered-label assignments. Filter and positions excluded. Disabled fields and ordered label merging preserved. |
| D35–D38 | Cameras/views, Quest poses, hover, panels, tools/brush, exports and project editing | Local. A source resource change requires new delivery; an export does not become a live mutation. |
| D39 | Delivery, close/replace, network loss | See [lifecycle](synchronization-architecture.md#inventory-visibility-and-patient-scene). |

A toolbar's global mode is not shared state; resulting assignments are. An already retained selection can survive changed visibility through checkpoint/correction, without allowing new selection of an invisible site. Effects derived from a remote event do not re-emit a second operation.

Masks correspond to the two original full/simplified topologies. Validation checks sizes and identity before mutation; a canonical mask can target a prepared topology temporarily inactive because of optimistic Quest selection. Selected geometry is rebuilt before application; the driver retains the latest authoritative mask per topology and rebases remaining proposals. Do not work around an error by skipping the mask or only advancing its watermark.

## Timeline and clock

[TimelineClockEstimator](../../../../Assets/Scripts/HBP/Sync/Scene/TimelineClockEstimator.cs) is consulted only for `Play` intent. `Pause`, `Seek`, `Step`, `Loop` apply exactly the transmitted index, even if playback remains active. Without a usable estimate, Play starts at the received index, including the last index of a non-looping timeline.

Transport probes every 250 ms during playback. The estimator retains at most 32 observations over two seconds, requires three observations with the newest at most one second old, chooses the minimum RTT among fresh observations and rejects uncertainty above half a sample. Playback is corrected only beyond one sample plus that uncertainty. Clocks are monotonic 64-bit with declared frequency, never wall-clock time or a long-lived Unity `float`. Play/Pause/Step/Loop form boundaries stopping seek coalescing.

## Jobs, invalidation and cancellation

Each computation is identified by incarnation, type, ID and monotonic generation. Only the current generation publishes/unlocks. Cancelling A, finishing B, then receiving A late must leave B intact. Preparation retains actually used buffers/handles until native return; cancelling a wait does not release a still-borrowed buffer.

| Job | Execution and result |
| --- | --- |
| Filter | Desktop only online, full roster-bound mask, Quest validates/applies atomically then exact Ready. |
| Correlation | Desktop computes or loads, bounded/versioned result with provenance, Quest publishes all matrices together then Ready. |
| Activity projection | Both compute on their shared inputs; sensitive commands await both terminal states, safe commands and network continue. |
| Inflation | Both prepare the exact cache, then local transitions and coordinated canonical commit. |

Filters/correlations are mutually exclusive. Confirmed loss after 500 ms cancels online jobs without automatically restarting them offline. An explicit local Quest request can then use delivered capabilities. A Desktop error abandons partial results; a Quest application error is not successful synchronization. Missing resource/roster requires resending. A stale result is discarded without being presented as failure of the new generation.

Requested projection, automatic policy, Absent/Stale/Computing/Ready/Failed state and generation are distinct. In manual mode, invalidation marks Stale and awaits a request; in automatic mode it schedules the newest generation. Removing activity cancels and clears its presentation. Sensitive: filter/blacklist, active ROI/geometry, scientific positions, influence, CCEP source, resource/topology, triangles and native settings. Selections, cuts, colors/labels/highlight, timeline and output parameters proven safe remain applicable. Mixed batches are sensitive.

## Filter and correlation contracts

[SiteFilterJob](../../../../Assets/Scripts/HBP/Sync/Scene/SiteFilterJob.cs) retains distinct semantics: Site Filters window = unmasked sites only; toolbar reset = all included, even masked; `FilterChannels` = replacement across all sites by patient/name pairs. The disabled old Basic/Advanced parser is not the active interface contract.

Offline, reset and channel selection are local. Allowed conditions are name, patient, attributes, raw position, resolved and present tags, activity if statistics are available, ROI location, with valid All/Any groups. MRI masks, atlases, mesh/cut geometry and other unsupported conditions are rejected before modifying `IsFiltered`.

[CorrelationResultResource](../../../../Assets/Scripts/HBP/Sync/Scene/CorrelationResultResource.cs) carries the full roster and correlation/mean matrices, plus Computed/Imported/Unknown provenance, patient/column/dataset/protocol/bloc/data-info, normalization, threshold and Bonferroni. Limit: 8 MiB. Values and all dimensions are validated before replacing the first array. The checkpoint independently retains the result (or explicit absence) and `DisplayCorrelations`; loading a live result enables display, restoring a checkpoint restores its captured flag.

## Coordinated inflation

[SurfaceInflationCoordinator](../../../../Assets/Scripts/HBP/Sync/Scene/SurfaceInflationCoordinator.cs) captures resource, hemisphere, exact options, algorithm version and anatomical surface references/GeometryVersion. Quest first drains the previous canonical watermark. The old display remains visible during preparation.

Current algorithm **2** works in anatomical coordinates already transformed in memory. No additional GIFTI/transform is sent for inflation. Only `CurrentSurfaceCoordinates` is accepted for a prepared result; old `NativeGifti`/`NativeGiftiThenTransformed` markers are rejected without silent recomputation or changes to their historical numbers.

When both caches are ready, an animated request performs the existing 0.6 s local transition on each peer. Both completions precede canonical `SetMeshDisplay`; Quest confirms after applying the watermark and checking the representation. Without animation, this barrier is skipped. Peer/barrier waits are bounded to ten minutes. LoadingManager covers only preparation/waiting for the peer and closes **before** animation/commit. Pre-publication cancellation retains the old display; after commit it does not undo it. Masks, UVs/colors and local poses are retained. The [scientific contract](../../surface-inflation.md) describes topology, cache and precision.
