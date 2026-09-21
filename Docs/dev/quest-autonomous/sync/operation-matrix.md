# Canonical operation matrix

This matrix is the implementation and release coverage contract for mutations of an already prepared visualization. It replaces the old field-map/capture checklist. Code paths listed are current entry points to inspect, not APIs that must be preserved.

## Legend

- **Reliable:** ordered, retried and never coalesced.
- **Latest:** an unsent value may be replaced only by a newer value with the same explicit key; the newest remaining value is reliable.
- **Batch:** validate and apply the complete set atomically.
- **Job:** follows `09-long-running-jobs.md`.
- **Safe:** remains enabled during activity projection.
- **Sensitive:** disabled during activity projection because it changes scientific inputs or projection target.
- **Derived:** computed locally from the accepted cause and never emitted redundantly.

Every shared row requires Desktop-origin and Quest-driver tests even when the final Quest UI does not yet expose the operation.

| ID | Operation and current entry | Canonical payload | Scheduling / executor | Activity behavior and local derivation |
| --- | --- | --- | --- | --- |
| D1 | Select active column (`Base3DScene.Operations.SelectColumn`, column label/view) | `SetSelectedColumn(columnId)` | Latest by scene | Safe. GUI focus is local; shared selection drives later operations. |
| D2 | Select/clear site (site list, scene/cut click, arrows) | `SetSelectedSite(columnId, siteId?)` | Latest by column | Safe. Automatic cuts derive locally. Reject unknown/masked site consistently. |
| D3 | Create/delete/order cuts (`CutController`, shortcuts) | `CreateCut(cutId, completeDefinition, order)` / `DeleteCut(cutId)` / explicit order if needed | Reliable structural | Safe. Runtime indices are not identity. Non-last deletion cannot renumber network IDs. |
| D4 | Move/orient/flip/custom cut | `SetCutDefinition(cutId, orientation, normal, flip, position, other scientific fields)` | Latest by cut, atomic complete object | Safe. All geometry/texture/collider consequences derive locally. |
| D5 | Strong/soft cut policy | `SetStrongCuts(bool)` | Latest scalar | Safe. Cut recomputation only. |
| D6 | Automatic cut around selected site | `SetAutomaticCutPolicy(bool)` plus D2 cause | Latest scalar | Safe. Three automatic cuts use permanent semantic axis IDs and are derived; do not send recreated cut deltas. |
| D7 | Create/import/delete/rename ROI | `CreateRoi(roiId, name, spheres, order)`, `RenameRoi`, `DeleteRoi` | Reliable structural / atomic import result | Sensitive. File path is local; imported resulting ROI is shared. |
| D8 | Select/clear active ROI | `SetActiveRoi(roiId?)` | Latest scalar | Sensitive. ROI site mask derives locally. |
| D9 | Add/remove/move/resize/select ROI sphere | `CreateSphere`, `DeleteSphere`, `SetSphereDefinition(position, influenceRadius)`, `SetSelectedSphere(sphereId?)` | Structural reliable; definition/selection latest | Geometry is sensitive; selected sphere itself is safe/shared. Animated display radius is local. ROI mask derives locally. |
| D10 | Filter/reset sites | Serializable `FilterCommand(jobGeneration, parameters)` then `SiteInclusionResult(rosterId, bitset)` | Job; Desktop-only online, Quest local offline | Sensitive and globally busy for the job. Desktop result canonical; no Quest online provisional compute. |
| D11a | Blacklist site(s) | `SetSiteBlacklist` or atomic batch | Latest single / reliable batch | Sensitive because blacklist participates in the effective scientific mask. |
| D11b | Highlight site(s) | `SetSiteHighlight` or atomic batch | Latest single / reliable batch | Safe. Must not invalidate activity. |
| D12 | Site requested color and ordered labels | `SetSiteColor`, `SetSiteLabels`, atomic bulk assignments | Latest single / reliable batch | Safe. Render appearance derives locally; must not invalidate activity. |
| D13a | Hide blacklisted sites | `SetHideBlacklisted(bool)` | Latest scalar | Safe display policy. |
| D13b | Show all sites | `SetShowAllSites(bool)` | Latest scalar | Sensitive if it changes ROI/effective masking; implementation must use actual dependency rather than toolbar grouping. |
| D13c | Site gain | `SetSiteGain(value)` | Latest scalar | Safe display parameter. |
| D14 | Move all sites to left/right hemisphere or reset | `MoveSites(command: Left|Right|Reset)` | Reliable command | Sensitive. Send command only, as required. Release requires cross-platform position parity from identical prepared geometry; if parity fails, block the row and revisit the product decision rather than silently changing to a position batch. |
| D15 | Compute/load/reset/display correlations | `CorrelationCommand`, chunked canonical result, `ResetCorrelations`, `SetDisplayCorrelations` | Job for compute/load; small reliable/reset and latest display | Compute/load globally busy; Desktop-only online, Quest local offline. Result data—not file path—is shared. Display toggle is safe. |
| D16 | Select mesh/preview, hemisphere, anatomical/inflated representation | Stable prepared resource IDs plus mesh part/representation | Reliable resource/topology barrier | Sensitive. Resource must exist in initial manifest. Visibility masks bind to exact original topology. Animation is local. |
| D17a | Select MRI | `SetSelectedMri(resourceId)` | Reliable resource barrier | Sensitive; projection grid/geometry derive locally. |
| D17b | MRI contrast | `SetMriCalibration(min,max)` | Latest atomic pair | Safe for activity field; cut textures update locally. |
| D18 | Select implantation | `SetImplantation(resourceId)` plus resulting membership identity contract | Reliable structural/resource barrier | Sensitive. Invalid site/ROI/correlation references are cleared deterministically. |
| D19 | Brain/cut color, colormap, edge mode, transparency/alpha | Targeted scalar assignments using requested values | Latest per property | Safe. Material/shader results derive locally. |
| D20 | Erase/expand/invert/reset/load triangle visibility | `ApplyTriangleMask(topologyId, sparseIds|bitset)` | Reliable atomic batch; adaptive encoding | Sensitive while projection computes, but does not itself require scientific field recomputation. Original topology IDs never change. Tool mode/brush are local. Triangle undo, if retained, is only another resulting mask operation. |
| D21 | Per-column activity opacity | `SetActivityAlpha(columnId,value)` or per-column batch for global UI action | Latest | Safe; toolbar global flag is local. |
| D22 | Anatomy influence distance | `SetInfluenceDistance(columnId,value)` | Latest | Sensitive; activity input. |
| D23 | Static label/resource, span and influence | Stable resource/label ID; atomic span; influence | Resource reliable; span latest; influence latest | Resource/influence sensitive. Span may remain enabled and apply newest pending generator adjustment. |
| D24 | iEEG/CCEP span and influence | Atomic span plus influence | Latest per atomic group | Influence sensitive. Span may remain enabled as prepared-output adjustment. |
| D25 | CCEP source mode/site/MarsAtlas region | `SetCcepSource(columnId, mode, sourceSiteId?, atlasLabel?)` complete object | Latest complete object | Sensitive. Source change is explicit; selected site alone does not imply source unless domain policy says so. |
| D26 | fMRI/MEG selected volume, thresholds, hidden ranges | Stable resource ID; atomic threshold/hide groups | Resource reliable; parameters latest | Selected resource is sensitive during projection; threshold/hide adjustments may retain newest pending safe value when proven. |
| D27 | Timeline seek/step/play/pause/loop | `SetTimelineAnchor(columnId,index,playing,looping,step,monotonicAnchor)` | Latest anchor | Safe. Desktop canonical online; both advance locally and correct only beyond one sample. Automatic ticks are not mutations. |
| D28 | Mars/JuBrain toggles and alpha | Targeted atlas assignments | Latest | Safe; overlay output derives locally. |
| D29 | IBC/DiFuMo selection/display | Stable prepared source/area IDs and display flags | Resource selection reliable; display latest | Safe if resources are prepared; missing resource rejects. |
| D30 | Localizer source, timeline and thresholds | Stable prepared IDs plus time/span values | Resource selection reliable; values latest | Safe if resources are prepared. UI selectors local. |
| D31 | fMRI atlas thresholds and opacity | Atomic span/threshold group and alpha | Latest | Safe; derived overlay/cut rendering local. |
| D32 | Compute/remove/automatic activity | `RequestProjection(generation)`, `SetProjectionRequested`, automatic policy from session preferences, ready/cancel/fail | Dual-executor job | Dedicated sensitive-control busy scope. Safe operations continue. Inputs/results are never transferred. |
| D33 | Load/reset scene or column configuration | Typed configuration transaction composed of the affected operation-family records and expected identities | Reliable atomic batch/barrier | Lock affected scene. Validate all fields/resources first; acquire strongest contained invalidation. Never use a generic snapshot map or replay UI setters one by one visibly. |
| D34 | Site-state import and bulk attribute changes | Resulting per-site assignments keyed by roster/site IDs | Reliable atomic batch | Sensitive iff batch changes filtered/blacklisted/effective mask/positions; highlight/color/labels alone are safe. File/path is local. |
| D35 | Desktop camera/views/layout/minimize/auto-rotate | Local only | No message | Must not alter canonical state/sequence or Quest wrapper. |
| D36 | Quest grab/recenter/head/controller pose/column physical scale | Local only | No message | Must survive every in-place mutation/checkpoint for retained IDs. |
| D37 | Hover, tooltip, panel expansion, edit handles, toolbar global mode, eraser brush | Local only | No message | May select the target of a later committed operation but is not itself shared. Selected ROI sphere is the explicit D9 exception. |
| D38 | Screenshot/video/export/project/source authoring | Out of live-sync scope | No message | Reads current scene or edits external source. A new heavy dependency requires full resend. |
| D39 | Full send, explicit close/replace, socket loss | `SceneLifecycle` messages with scene/incarnation/manifest IDs | Reliable control barrier | Socket loss preserves view; explicit close does not. Offline Desktop close creates Quest orphan. |

## Cross-row rules

1. A cause is sent once. Derived cuts, ROI masks, render meshes, textures, activity display and local material state are not echoed as secondary mutations.
2. Filters/correlations deliberately send command plus canonical result. This is the documented exception, not a pattern for all derived data.
3. “Safe” means safe during activity projection; it does not remove its own normal targeted rendering work.
4. If a batch mixes safe and sensitive fields, the complete batch is sensitive and atomic.
5. If code inspection proves a classification wrong, update this specification explicitly before changing behavior. Do not infer product semantics from accidental current invalidation.
6. Every resource reference is checked against the prepared manifest before domain application.
7. Every row has an O(change) mutation path. Checkpoint capture is never used to implement a missing handler.
8. A body above the protocol's measured inline threshold always uses a small ordered scene-operation descriptor plus an independent reliable bulk stream. This includes D20 dense masks and D33/D34/other large batches; atomic validation/application semantics do not change.

## Coverage record template

Implementation agents append evidence without replacing requirements:

| ID | DTO/handler | Desktop test | Quest-driver test | Perf/invalidation evidence | Status |
| --- | --- | --- | --- | --- | --- |
| Dn | link/type | test name | test name | measurement/assertion | not started / partial / complete |

No row becomes complete from codec round-trip alone. It requires domain application and targeted-effect verification.
