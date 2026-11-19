# Activity projection and NIfTI export

The activity field is volumetric. The surface is a sampling target,
independent of the field geometry. The current code uses
`ActivityProjectionGrid`; the former `GeneratorSurface` and its production ABI
have been removed. The `LegacyNative` test adapters remain oracles.

## Owners and geometry

`Base3DScene` owns the grid, which retains the reference volume.
`ActivityGenerator` retains the grid and owns the native field.
`SurfaceGenerator` retains the generator and an explicit surface. When a grid
is replaced, consumers are rebound before the old grid is released.
A separate managed `ActivityField` object is not needed.

For each axis of a volume whose dimensions are all at least two:

```text
gridAxis = max(2, truncate(maximumDimension * sourceAxis / largestSourceAxis))
stepAxis = (sourceAxis - 1) / (gridAxis - 1)
gridToWorld = sourceVoxelToWorld × diag(stepX, stepY, stepZ, 1)
```

The outermost voxel centers are preserved, including with an oblique or
anisotropic affine. Scales multiply the affine's **columns**. The grid
contains no surface vertices and performs no bounding-box union.
Native storage is `i * dimY * dimZ + j * dimZ + k`.
The [coordinate convention](hbp_core_coordinate_system.md) applies at
the Unity boundary; the export affine remains in the native coordinate system.

## Invalidation

| Change | Resources to update |
|---|---|
| Reference volume, grid dimension or interpolation | Grid, field and bindings |
| Activity, sites/implantation, source or mode, ROI/mask, radius | Field on the existing grid |
| Surface or hemisphere | Binding and UVs; field and timeline retained |
| Triangle visibility/erasure | Mesh and application of existing UVs |
| Cut geometry | Cut sampling/textures |
| Time, alpha, palette, display calibration | Sampling/colors, without recomputing the raw field |

Entry points are `InvalidateProjectionGrid`, `InvalidateActivityField`,
`InvalidateSurfaceProjection`, `InvalidateSurfaceMesh` and
`UpdateProjectionResources`. The `ProjectionGridVersion`,
`ActivityFieldVersion`, `SurfaceProjectionVersion` counters make responsibilities
observable. An actual CCEP source/mode change invalidates the field;
a compatible mesh change does not. Direct `FMRIManager` overlays
remain a separate path.

## Surface coverage

The native binding is keyed by grid and surface identity/version,
plus interpolation. `Surface.GeometryVersion` tracks positions (replacement,
transformation, merge), not UVs, colors, normals or triangle visibility.
The first validation or projection builds nearest indices/trilinear stencils
and counts valid vertices **in the same pass**. The result
is reused, without a per-frame scan.

The report exposes total/valid/invalid counts, ratio, classification
`Unavailable`, `None`, `Partial`, `Complete`, version and build duration.
Partial coverage becomes significant when:

```text
invalidVertexCount > max(32, floor(totalVertexCount * 0.01))
```

Vertices outside the grid receive neutral UVs, never a clamped edge sample.
Selecting a volume or surface remains silent. An incompatible manual request
offers Continue/Cancel before computation; authorization applies only to
the current pair of versions. A change during the dialog requires a new check.
Automatic computation silently waits for compatible geometry or explicit
authorization and retains its stale state. Cuts and export remain usable
even without surface coverage.

## Localizer export

`LocalizerExportGridSettings` belongs to the export operation, independently
of `ActivityProjectionSettings` and open scenes: default maximum dimension
**80**, range **2–512**, **trilinear** interpolation. The window does not
remember input between openings.

Without a native grid, the preview computes dimensions, voxel count and
uncompressed sizes: float activity per time point and mask. It does not predict
gzip size. Above **8,000,000 spatial voxels**, confirmation precedes
closing visualizations. Input is revalidated before any side effect;
memory also increases with the timeline.

Native export copies the exact grid affine into `sform`, computes `sto_ijk`
by inversion and derives the quaternion, `qform` and `pixdim` using niftilib.
If shear cannot be represented faithfully, `qform_code` is disabled;
`sform` remains the reference. Activity and mask share dimensions and affine.
Dedicated wrappers are released at the end of export.

Example: MNI `208 × 256 × 219`, maximum dimension 80 → `65 × 80 × 68`,
353,600 voxels, approximately 1.35 MiB of activity per time point and 0.34 MiB of mask.

## Implementations and verification

- [C# grid and settings](../../Assets/Scripts/HBP/Core/DLL/Generators/ActivityProjectionGrid.cs),
  [export settings](../../Assets/Scripts/HBP/Core/DLL/Generators/LocalizerExportGridSettings.cs),
  [scene lifecycle](../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs).
- In the `hbp_core` checkout: `src/generators/activity_projection_grid.cpp`,
  `surface_projection_binding.cpp`, `activity_generator_nifti_io.cpp`.
- [Coverage tests](../../Assets/Tests/EditMode/HBP.Serialization.Tests/ActivityProjectionPhase3Tests.cs),
  [settings](../../Assets/Tests/EditMode/HBP.Serialization.Tests/ActivityProjectionSettingsTests.cs),
  [PlayMode lifecycle](../../Assets/Tests/PlayMode/HBP.Module3D.PlayModeTests/Module3DScenePlayModeTests.cs)
  and `hbp_core/tests/native/hbp_core_generator_functional_test.cpp`.

Check outermost centers and oblique axes, activity/mask readback,
zero/partial coverage, cache retention after triangle erasure and representation
changes, and recomputation after scientific mutations. These tests are entry
points; their presence does not prove execution against the current build.

## Useful historical measurement

On August 13, 2026, the native A/B comparison in **the same process and DLL** used
MNI `208 × 256 × 219` (0.72 mm), 69,104 vertices, dimension 80, 30,000
Halton sites, 100 time points, linear radius 15 and three repetitions. Reference
environment: Windows 11, i7-12700K, approximately 32 GB.

| Measurement | Former hybrid | Grid only |
|---|---:|---:|
| Computed points | 422,704 | 353,600 |
| Values + weights | 170,772,416 bytes | 142,854,400 bytes |
| Median A/B computation | 4,703.06 ms | 3,516.82 ms |

Removing vertices from the field accounts for the −16.35% storage reduction and
the observed gain. Isolated Unity timings are not comparable to this A/B test.
The MNI binding measured afterwards covered 100% of vertices in 1.89 ms and
retained its version on the second sampling. These are historical results,
not guaranteed current performance.

Reproduction: `NativeProjectionLoadBenchmarkCli.Run` in
[the permanent benchmark](../../Assets/Tests/EditMode/HBP.Serialization.Tests/NativeProjectionLoadBenchmarkCli.cs)
(Product profile, dimension 80, timeline 100, repetitions 3, Trilinear
interpolation), or native target `hbp_core_activity_projection_grid_benchmark`.
Follow the [validation workflow](../../.agents/skills/hibop-unity-validation/SKILL.md)
for any Unity execution.
