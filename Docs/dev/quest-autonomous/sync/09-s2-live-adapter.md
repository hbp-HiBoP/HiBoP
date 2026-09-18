# S2 live adapter validation

The live adapter is in `Assets/Scripts/HBP/Sync/Scene`, an assembly depending on
the pure S1 runtime and the existing scene runtime. It does not change the Quest
presentation transform. Its epoch and manifest are fixed at construction. The
receiver binds cut, ROI and sphere IDs from the **initial delivered state** before
applying later snapshots. Tombstones remain in subsequent captures.

## Implemented and exercised

- Stable `ID` on runtime cuts, ROI and spheres. Cut texture and native indices use
  `Cut.Index`; removal and automatic cut recomputation keep surviving IDs.
- Capture and in-place application for selected column/ROI, cut policy and
  geometry, ROI/sphere membership and geometry, site state/positions, scene
  appearance, column opacity, anatomy influence distance, static and dynamic
  spans and influence distances, CCEP source choice, fMRI/MEG calibration and
  selected prepared resource, navigation timeline values, Mars/JuBrain display,
  IBC/DiFuMo prepared selection and display, atlas opacity and fMRI atlas
  calibration, explicit projection intent, and localizer selection, index and
  thresholds, comparison site and correlation result. Localizer selection
  resolves against the exact loaded protocol, data and bloc roster before the
  scene is changed. Correlation values are encoded as canonical, content
  addressed resources; the receiver must stage verified bytes before applying
  a snapshot that names them. A two-column-object PlayMode test verifies exact
  correlation and mean values after resource decoding and rejects changed
  resource bytes.
- A PlayMode test loads the same verified scene archive independently into the
  Desktop scene prefab and the Quest view prefab, preserving the Quest column
  wrapper pose. It stages and replays a correlation result, compares its exact
  pair values, and replays its reset. Sequential state checks cover anatomy
  influence, static source/span, iEEG span/influence, MEG source, iEEG seek/step,
  Mars atlas display and resulting mesh colors, CCEP site-source reset/restore,
  fMRI/MEG calibration and fMRI atlas calibration. CCEP Mars-area selection is
  rejected before mutation when the prepared implantation has no MarsAtlas tag.
  The same test replays filtering, highlight, blacklist, color, labels and moved
  sites, then compares static, CCEP and iEEG activity arrays. It also runs
  independent native projections on Desktop and Quest and compares the iEEG
  surface activity UVs after stabilization; the accepted Quest timeline loop
  flag survives recomputation. The same delivered-scene test now selects IBC,
  DiFuMo and a localizer in sequence using prepared native NIfTI fixtures,
  checks the resolved volumes and mask, and compares the resulting surface
  colors on both scenes. A prepared MarsAtlas tag fixture also replays a CCEP
  area source and compares its area mask and activity output. It replays active
  selection through all six columns, site display and gain, scene colors and
  transparency, and per-column activity opacity. It selects and clears sites,
  changes the strong-cut policy, and compares the IDs and exact fields of
  automatic cuts after both scenes settle. Changing the active column clears a
  previously selected site in another column. Automatic-cut recomputation
  resets the unflipped MRI orientation before deriving cut positions, so a
  reused cut retains the same normal and flip as its first computation. For
  every operation replayed by this test, both scenes now settle before the
  canonical comparison; activity UVs, opacity UVs, surface colors and Quest
  wrapper transforms are checked at that same point.
- Delivery-bound exact references for the prepared mesh, MRI, implantation and
  column resources. The public adapter checks the prepared mesh, MRI and
  functional column roster against the published manifest, then checks local
  resource readiness and selects the exact object. Mesh, MRI and functional
  references include the corresponding descriptor hash from the final encoded
  delivery metadata. Prepared MRI and functional file hashes must agree with
  those descriptors; dashed or uppercase loader hashes are normalized. The
  implantation and static-label references now include their scientific
  content; IBC, DiFuMo and localizer references include loaded atlas content
  and hierarchy instead of a local roster position. The catalog rejects
  changes to prepared implantation, static, IBC and DiFuMo content. The
  manifest retains only immutable values, so modifying a restored payload
  does not change it. A test rejects a changed live mesh name. The real
  Desktop capture path binds directly to its sent delivery, and two project
  fixtures (standard and patient MRI) match the independently restored Quest
  adapter without a second Desktop restoration. Changing
  implantation rebuilds each column's prepared site topology; removed sites
  retain membership tombstones and can reappear when an earlier prepared
  implantation is selected. A new mesh part or representation must have a
  prepared surface variant. Topology changes rebuild geometry before triangle
  masks are applied. Masks use one canonical bit per triangle and are checked
  against both full and simplified topology before mutation.
- A synchronized apply generation prevents older activity or collider work from
  publishing after a newer accepted state. A targeted two-scene PlayMode test
  holds a native collider result immediately before publication, accepts a
  newer cut position, proves the old result was discarded, then verifies that
  the current collider result publishes. Site updates emit one change event
  only when the canonical value changed. An activity recomputation initiated by
  a synchronized state preserves the accepted timeline playback values.
- Preflight checks for epoch, manifest, visualization, supported fields, prepared
  column/site topology, cut ordering and bounds, required fields and deleted ID
  reuse. A selected site's identity is checked against the prepared column;
  its previous derived mask cannot veto selection from a newer state.
  Unsupported state is rejected before the adapter's visible mutations.
- PlayMode replay between two prepared scenes verifies canonical snapshots for
  cut create/move/delete, ROI/sphere create/delete, ROI rename and active
  selection/clear, sphere move and radius, anatomy distance, triangle
  masking and atlas appearance. It rejects an incomplete cut, unknown resource
  and wrong-topology mask without mutation. A second two-scene test switches
  exact prepared mesh and MRI resources. Other focused tests check automatic
  cut IDs and timeline clock-anchor restoration. The replay test also checks
  that invalid cut values leave the target unchanged and that a stale site
  mask does not block an incoming selection. It also replays an implantation
  change and return with matching site membership. A native MRI fixture lets
  the cut test compare the generated base cut texture pixels after both scenes
  settle; full and simplified triangle visibility masks also match.

## S2 traceability, 2026-09-18

`S` is `SceneRestorationPlayModeTests.S2_ReplaysCorrelationsAcrossDeliveredSixModalityScenesWithoutReplacingQuestPresentation`:
after each replay it compares canonical bytes, all column activity/alpha UVs,
surface colors, ROI site masks, and Quest wrapper pose. `G` is
`Module3DScenePlayModeTests.LiveGeometryStateAdapter_ReplaysCutCreateMoveAndDeleteBetweenPreparedScenes`;
`R` is its exact mesh/MRI switch companion. UI paths in one family are represented
by their resulting state assignment, not by a separate test for each gesture.
The short `S2_PreparedMeshManifestBindsAfterTwoSceneOpening` test checks prepared
mesh identity, D20 mask coloring, and D33 reset/load in one two-scene setup.
`S2_LiveDesktopCaptureBindsDeliveredQuestScene` reuses that setup and additionally
captures the live Desktop scene through the production capture path.

| Row | Effect | S2 evidence and remaining boundary |
| --- | --- | --- |
| D1 | shared selection | S visits all six columns. |
| D2 | shared site selection | S selects and clears a site, including automatic-cut consequence. |
| D3 | shared cut membership | G creates and removes a stable-ID cut; texture appears/disappears. |
| D4 | shared cut geometry | G moves a cut and compares resulting cut pixels; orientation/flip/normal are one atomic group. |
| D5 | shared cut policy | S toggles strong cuts. |
| D6 | shared automatic-cut policy | S compares generated cut IDs and every field after recomputation. |
| D7 | shared ROI membership/name | S creates, renames and deletes; G also checks membership. |
| D8 | shared active ROI | S selects and clears; ROI site masks are compared. |
| D9 | shared sphere membership/geometry | S creates, moves and resizes; G checks deletion. |
| D10 | shared site inclusion | S changes inclusion; canonical and scientific site masks are compared. |
| D11 | shared site flags | S changes highlight and blacklist. |
| D12 | shared site color/labels | S changes color and ordered labels. |
| D13 | shared display policy | S changes blacklisted visibility, all-sites mode and gain. |
| D14 | shared scientific position | S moves a site and compares resulting activity UVs. |
| D15 | shared correlation result/display | S stages verified result bytes, checks pair and mean values, then resets; loading the same bytes has the same state effect. |
| D16 | shared mesh/topology | R switches exact prepared mesh and compares vertices; surface masks remain topology-checked. |
| D17 | shared MRI/calibration | R switches exact MRI; S changes contrast; G compares native cut pixels. |
| D18 | shared implantation/site topology | G switches and returns, checking site identities and tombstones. |
| D19 | shared scene appearance | S changes scene colors, edge mode and transparency. |
| D20 | shared triangle mask | G compares full/simplified masks and rejects wrong topology; S compares surface output. |
| D21 | shared column opacity | S checks static-column opacity and every column alpha UV. |
| D22 | shared anatomy projection | S changes influence distance and compares activity UVs. |
| D23 | shared static source/span | S changes label, span and influence, comparing activity UVs. |
| D24 | shared dynamic span | S changes iEEG and CCEP spans/influence, comparing activity UVs. |
| D25 | shared CCEP source | S replays site and Mars-area sources and compares output. |
| D26 | shared fMRI/MEG source and calibration | S changes MEG source and both calibrations, comparing output. |
| D27 | shared timeline state | S replays seek, step and loop; focused clock-anchor test covers playback restoration. Transport clock conversion is S3. |
| D28 | shared brain atlas display | S toggles Mars and alpha and compares surface colors; JuBrain uses the same display/indices path. |
| D29 | shared IBC/DiFuMo display | S selects each prepared fixture and compares native volume and surface colors. |
| D30 | shared localizer display | S selects protocol/data/bloc/time/span and compares volume, mask and colors. |
| D31 | shared fMRI atlas calibration | S changes range/alpha and compares colors. |
| D32 | shared projection intent | S removes/recomputes activity; G rejects stale native publication. |
| D33 | shared config assignment | S resets and reloads configuration, comparing canonical and scientific output after each; both passed with the localizer selected. The short two-scene test also checks reset/load after a masked MarsAtlas surface. |
| D34 | shared resulting site attributes | S applies a bulk change; imported file/dialog is local. |
| D35 | local Desktop presentation | No schema field or adapter mutation; camera/views/layout stay local. |
| D36 | local Quest presentation | S verifies wrapper position, rotation and scale after every shared replay. |
| D37 | local interaction state | No schema field; hover, handle, panel and tool mode are excluded. |
| D38 | local/outside session | Export and source editing produce no open-scene state field or S2 revision. Changed source dependencies need a later session barrier. |
| D39 | lifecycle boundary | Adapter rejects wrong epoch/manifest/visualization. Close, replacement and link loss belong to the S3 session owner. |

The delivery manifest now carries a fingerprint of each prepared mesh's vertex,
triangle, normal and atlas-capability buffers across all variants. Projection UVs,
display colors and live visibility masks can change after scene opening; masks
are checked by the separate canonical field. MEG channel
values, units and frequency are compared with the immutable delivered metadata.
Negative tests substitute different mesh vertices and MEG values under the same
name. Implantation, static and loaded atlas content retain their fingerprints.

S2 can validate locally prepared IBC, DiFuMo and localizer resources after they
are loaded on both scenes. The fixture adds them after archive capture, so it
does **not** demonstrate their distribution to Quest. The Quest build currently
excludes `Atlases/Localizers/`; packaging and readiness on device are S3 gates.
An atlas added after capture cannot be claimed as part of the initial delivery.

MNI preparation now hashes only its six MRI/mesh reference files. A Desktop
delivery hashes the Quest-packaged atlas files when capture is requested, on a
worker thread; separately distributed localizers are excluded. The EditMode
`TransferHashesKeepMniAndPackagedAtlasContentWithoutLocalizers` check passed
(1/1 on 2026-09-18), including different atlas bytes under one filename.
On the final code, the short D20/D33 PlayMode scenario passed (1/1, 16.7 s of
test execution), and the six-modality scenario passed (1/1, 132.3 s), including
the localizer D33 reset and load. Changing a triangle mask now recomputes
surface colors on both Desktop and Quest. Unity startup and compilation add to
these test durations. During development, use the short scenario for resource
binding, masks and configuration, and reserve the six-modality replay for a
final coverage check.

After formatting, the transfer EditMode assembly passed 104/104, including the
MNI/packaged-atlas hash scope and prepared-surface round trip. One filtered
PlayMode launch passed 3/3: the short delivered-scene test (15.5 s), the
cut create/move/delete test (0.8 s), and the prepared mesh/MRI switch test
(0.1 s). This lets routine geometry changes use the subsecond synthetic tests,
while delivered-resource or configuration changes use the short scenario.
The focused live-capture scenario invokes `DesktopSceneCapture.CaptureDeliveryAsync`
on the Desktop scene, opens that exact delivery on Quest, then binds and applies
the initial state. This initial application is necessary: scene restoration
selects a default anatomical site, while the live Desktop scene can have no
site selected. After application, every canonical field, surface colors and
Quest wrapper pose match. The capture scenario passed 1/1 through the open-editor
MCP on 2026-09-18 in about 57 s of test execution. The two full project
load/save/capture fixtures were not repeated: their earlier runs took 251 s
and 600 s. They are broader project qualification, while the targeted live
capture closes the S2 delivery-binding check.
After separating the fast and live-capture cases, the final formatted-code MCP
run passed both tests (2/2, 61.0 s combined). The production capture and apply
code is unchanged since the passing 104/104 EditMode, 3/3 focused PlayMode and
1/1 six-modality runs above.

All D1–D34 shared effects in the matrix have an in-process capture/apply proof
with canonical state and scientific output checks. D35–D38 are explicitly
local or outside the open-scene state, and D39 is the session lifecycle boundary.
The representative cut create/move/delete proof passes. S2 is validated for
locally available prepared resources; device distribution and active-session
transport remain S3 work.

`LiveGeometryStateAdapter` is an in-process S2 adapter. S3 must attach it to a
live session, deliver later resources, convert timeline clock anchors between
devices, and own explicit close/replacement/link-loss transitions.
