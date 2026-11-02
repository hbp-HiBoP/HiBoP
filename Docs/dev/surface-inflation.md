# Inflation: displayed representation and anatomical reference

Inflation is implemented as a derived representation of the selected `Mesh3D`,
generated on demand and retained in memory by that mesh. It does not create
a second anatomy in the resource list. Eligibility is geometric;
the mesh name does not guarantee its cortical nature.

## Scientific contract

| Property | Anatomical | Inflated / transition |
|---|---|---|
| `BrainSurface`, camera centering | Anatomical surface | Derived displayed geometry |
| `ReferenceSurface`, activity, atlases, fMRI, measurements | Anatomical | Anatomical |
| Sites/electrodes | Anatomical coordinates | Same coordinates, visible and selectable |
| Cuts and cut colliders | Anatomical | Anatomical and editable |
| Brain envelope clipping | Current cuts | None |

Scientific UVs/labels/colors are carried by the same vertex indices;
data is not resampled at inflated coordinates. Switching representation
preserves the grid, activity field and timeline. Anatomical cross-section
meshes are hidden in the main inflated view, but remain available in dedicated
cut views. On return, the **current cuts**, including their recent changes,
clip the anatomy.

Full-resolution masks may be retained only with identical topology;
an independently simplified surface does not blindly inherit their indices.

## Input, algorithm and cache

Computation version **2** consumes the full anatomical buffers already loaded
and transformed (`CurrentSurfaceCoordinates`): MNI, patient GIfTI or MRI preview.
It does not reread the file, inflate an already inflated or truncated surface,
reapply the `.trm`, or retain a permanent copy in an older coordinate system.
An anisotropic transformation changes the numerical result; this choice is
accepted to share the Desktop/Quest path.

For a left/right mesh, hemispheres are computed separately; they are
published only together, then merged and simplified. The cache includes source
identity and `GeometryVersion`, algorithm version and all parameters.
A result that becomes stale during the work is not published.

The current `Inflated` preset uses `MetricRegularized`, isotropic RMS radius
preservation, 160 iterations, smoothing 0.35, regularization 0.12, maximum step
0.20, tolerance `1e-5`, at most eight backtracking steps and fixed boundaries.
The `NonShrinkingSmoothing` prototype remains available as a technical baseline.
Connected components are processed around their own center; small components
and isolated vertices are not silently removed. Non-finite input, invalid
indices, degeneracies and non-manifold topologies are rejected with diagnostics.
Only positions and normals change: vertex order, triangles, UVs, colors
and compatible visibility are preserved.

The geometric implementation is independent; Workbench is an external oracle,
not a runtime dependency. Topological correspondence demonstrates neither
numerical equivalence with Workbench/FreeSurfer nor anatomical validity of
an arbitrary mesh. Interpolated states are purely visual.

## Execution, interaction and persistence

The native job takes a snapshot, runs once on a worker and exposes thread-safe
progress/cancellation. `take_result` transfers an owned result exactly once.
Cancellation does not publish a partial result; the wrapper waits for the worker
to finish before destroying the job. Representations remain attached to their
owner and are released with it.

`SurfaceRepresentation` is serialized; old projects default to `Anatomical`.
The first coexistence of inflated representation and cuts displays a single
informational OK message per visualization, whether caused by inflation with
cuts or by the first cut created while inflated. This marker belongs to the
scene instance and is not serialized. It does not prohibit cuts.

The GPU transition interpolates prepared positions and normals in the color,
depth, normals and outline passes. It preserves scientific attributes,
suspends interactions that depend on precise geometry and updates the final
collider. Shared jobs and their coordinated publication belong to
[synchronization](quest-autonomous/sync/synchronization-architecture.md).

A transition cancelled before publication retains the previously accepted
representation. Clipping remains disabled during the transition; restoring it
rereads the current cut list, including cuts added or removed since the
animation began. The six states of the earlier specification corresponded to
representation/cut combinations and the two transition directions,
not to six persisted representation values.

The historical `NativeGifti`/`NativeGiftiThenTransformed` enum values remain
recognized to explicitly reject an incompatible prepared representation and
request preparation/delivery again. The scene container remains at version 4;
Desktop and Quest must use computation contract version 2.

Electrode projection onto the inflated surface, disk caching and GIfTI export
with arbitrary metadata remain extensions not delivered by this path.

## Sources and verification

- [Surface / job / options](../../Assets/Scripts/HBP/Core/DLL/Surface.cs),
  [Mesh3D / cache](../../Assets/Scripts/HBP/Core/Object3D/Mesh3D.cs),
  [MeshManager](../../Assets/Scripts/HBP/Data/Module3D/Modules/MeshManager.cs),
  [Base3DScene](../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs).
- Native: `hbp_core/src/surface/surface_inflation.cpp`,
  `src/api/surface_inflation_api.cpp` and native inflation tests.
- EditMode tests `SurfaceInflationPhase3Tests` through `SurfaceInflationPhase7Tests`
  under `Assets/Tests/EditMode/HBP.Serialization.Tests/`: layouts, ownership,
  cancellation, transformed source without files, cache, atomic publication,
  reference surfaces, serialization, transition and cut visibility.

Product behavior coverage belongs to these tests, independently of the
geometric corpus: `SurfaceInflationPhase5Tests` checks anatomical reference
and clipping, `SurfaceInflationPhase6Tests` the default value and one-time
message, `SurfaceInflationPhase7Tests` UVs/colors and cut views. The inflation
scenario in
[SceneRestorationPlayModeTests](../../Assets/Tests/PlayMode/HBP.Transfer.Scene.PlayModeTests/SceneRestorationPlayModeTests.cs)
checks, among other things, return to Anatomical and mask retention after
cancellation of a synchronized animation. Their presence does not establish
recent execution of these tests.

The [retained historical corpus](../../Tools/InflationReference/reference-corpus.md) documents MNI, fixtures and
reproducible Workbench comparisons. It does not establish current qualification
on a real patient corpus. Earlier phase plans are no longer implementation
instructions or proof that all hardware has been tested.
