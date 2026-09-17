# S2 live adapter progress

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

## Required before S2 can be called complete

- Bind every catalog entry to an exact prepared resource. The immutable
  manifest now comes from the final encoded scene metadata on Desktop and the
  verified metadata on Quest. `FromSent` checks the receipt against the sent
  delivery without restoring a second Desktop scene; `FromPublished` compares
  the receipt with the restored archive hash. File hash and ZIP extraction use
  the same read-only handle. Mesh, MRI and functional descriptors are included
  in references. Implantation, static and loaded atlas references now include
  content fingerprints, but individual mesh bytes and MEG channel values are
  not yet checked against the live Desktop object at binding. Atlas resources
  added after scene capture are not part of the initial manifest and cannot
  yet be claimed as delivered resources.
- Complete scientific-output comparisons after stabilization for all matrix
  rows, especially functional projection and atlas output.
- CCEP Mars-area, IBC, DiFuMo and localizer replay have positive two-scene
  output tests with prepared fixtures. The current Quest standard-data
  packaging excludes `Atlases/Localizers/`, so localizer readiness on a Quest
  build is not yet established.
  Timeline anchors currently use Unity's local monotonic clock; transport will
  need a clock-offset conversion before cross-device playback can be claimed.
- Expand the two-scene tests to every shared operation-matrix row and all
  modalities. Check Quest wrapper preservation and exact scientific output, in
  addition to canonical snapshot equality.

`LiveGeometryStateAdapter` deliberately rejects unsupported incoming fields. It
must not be connected to the session transport as a complete S2 applier yet.
