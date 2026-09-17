# Shared scientific state contract

## Design rule

`SharedVisualizationState` is a versioned, detached value graph for the **current open scene**. It is not the project-file format, a copy of Unity objects, or a sequence of UI clicks. The existing `VisualizationConfiguration` and per-column configurations provide fields and migration behavior, but runtime values must be captured from `Base3DScene`, its managers, columns, sites and timelines.

The wire schema uses fixed field IDs, explicit versions, bounded collections and a whitelist of supported types. A deterministic canonical encoding or explicit equality implementation supplies hashes and comparisons. Do not use a generic reflection serializer over `MonoBehaviour` or `BaseData` as the sync contract.

## Proposed shape

```text
SharedVisualizationState {
  schemaVersion, epochId, visualizationId, commonRevision,
  resourceManifestId,
  scene: SceneState,
  columns: ordered ColumnState[] keyed by existing Column.ID,
  cuts: ordered CutState[] keyed by session-stable CutId,
  rois: ordered RoiState[] keyed by session-stable RoiId,
  activeRoiId?, selectedColumnId?,
  topologyAndMaskReferences,
  optional prepared outputs with provenance
}

ColumnState {
  columnId, modality, shared parameters, selected resource,
  sites: SiteState[] keyed by columnId + full site identity,
  selectedSiteId?, navigation: TimelineState?,
  modality-specific shared parameters
}

TimelineState { index, playing, looping, step, sampling, anchorTime? }
CutState { cutId, orientation, normal, flip, position, other scientific settings }
RoiState { roiId, name, ordered spheres with stable IDs and scientific coordinates }
```

These are design sketches, not a claim that the named types already exist. The inventory must add any omitted visible operation before freezing schema version 1. IDs must survive list reordering. In particular, the current cut runtime index is not a network identity. Stable IDs for ROI and spheres are also required if edits to one object must be merged while others change.

## Value classification

| Family | Shared value or input | Derived locally or separate resource | Local presentation |
| --- | --- | --- | --- |
| Anatomy | selected mesh/MRI/implantation, representation, mesh part, visibility, color, erasure mask identity | native surfaces/volume and geometry identified by content hash | Quest column pose and scale; Desktop camera |
| Cuts | definition, stable identity, order, automatic-cut policy and dependent selection | meshes, textures, clipping results produced by common code | gizmo pose before scientific commit, hover |
| Sites | blacklist, highlight, color, labels, filtered inclusion, selected site when scientifically relevant, moved scientific position | prepared data mask, ROI membership and appearance from shared inputs | controller pointer and local tooltip placement |
| ROI | identity, ordered spheres, name, active ROI and activation that changes scientific output | ROI membership and projection results | editing handle pose |
| Columns | modality settings and selected resource from existing configurations; ordered identity | activity buffers/functional files from initial delivery; calculated projection | local layout/minimize when it does not change science |
| Timeline | actual navigation index, play/loop/step/sampling state and synchronization anchor | current sample and calculated frames | local control widget |
| Overlays | atlas/fMRI selection, opacity, thresholds and scientifically relevant enable flags | local output if parity is proven; otherwise versioned prepared result | panel visibility |

`SiteState.IsFiltered` is an input to shared science and is sent as the **resulting inclusion set** of a Desktop filter, not as rendering instructions. `IsMasked` and `IsOutOfROI` may be derived if the relevant source data and active ROI are identical; validate this in the inventory. If not, promote the minimum missing input into the contract. An output buffer is transferred only when local derivation cannot satisfy the parity gate.

## Resources and dependency identity

The initial `ScenePayload` contains prepared anatomy, columns and heavy data. During live synchronization, ordinary state messages reference resources already installed by content hash. A newly selected or generated resource is prepared, validated and sent before the revision referring to it can become visible. Triangle erasure masks carry the selected surface/topology hash and exact expected length; a mask for a previous topology is invalid. Resource ownership must outlive pending work and offline edits. Resource changes are barriers for dependent state, not a reason to reset every Quest presentation wrapper.

## Capture and application

Capture a coherent state on the Unity thread without yielding across live graph reads. Encoding and network I/O use a detached copy on workers. The Quest validates schema, identities, finite numbers, bounds, resource versions and dependencies before calling common operations. Apply related values as a transaction with one invalidation batch and a render/publication fence. `LoadConfiguration()` is not this reconciler: it resets and recreates content.

For a gesture, publish absolute desired values. `Set cut C to position P` is idempotent; `move cut C by delta D` is not. The network may coalesce intermediate previews while retaining the final value and gesture identity.

## Completeness discipline

Every Desktop mutation route and later Quest control must map to: (1) a shared field or explicitly local presentation field, (2) capture logic, (3) application through common code, (4) invalidation and resource dependencies, and (5) a parity test. A field present only in a saved configuration does not prove that the live value is captured. A source signal timeline does not prove the column's copied navigation timeline is captured.
