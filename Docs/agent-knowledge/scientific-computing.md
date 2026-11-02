# Scientific computing and native integration

Read the relevant section before changing native wrappers, spatial math,
projection, mesh generation, inflation or NIfTI export. Shader, palette, camera,
RenderTexture and image-export changes require [scientific rendering](scientific-rendering.md).
[Runtime and state](runtime-and-state.md) owns resource preparation/cancellation;
[synchronization](synchronization.md) owns coordinated Desktop/Quest jobs.

## Native object and ABI boundary

Production uses `hbp_core`; `hbp_export` is an editor-only parity oracle, never a
runtime fallback. Resolve the native checkout from the workspace rather than
assuming a machine-specific location. Keep numerical kernels native and presentation
in Unity. Keep surface file I/O separate from in-memory surface operations.

- A native object with identity, mutation or lifetime has a typed opaque C pointer
  and a C# `CppDLLImportBase` wrapper in `HBP.Core.DLL`. The wrapper owns its handle;
  construction, class-specific imports, operations and destruction stay together.
  Do not replace object methods with a broad geometry service.
- The C ABI exposes no C++ classes, STL, templates or exceptions. Use `hbp_*`
  functions, family-specific create/destroy, typed output pointers and `HBP_Status`
  when an operation can fail. No generic product `HBP_Handle` or generic destructor.
  Null object pointers return `HBP_INVALID_HANDLE`; null value/output parameters
  return `HBP_INVALID_ARGUMENT`. Bounded outputs expose capacity and report
  `HBP_BUFFER_TOO_SMALL` rather than writing beyond it.
- Values without native lifetime use structs. Shared interop structs belong in
  `HbpCoreValueTypes.cs`; class-specific structs stay near their owner. Internal
  native objects use `hbp::core` names and object-oriented `.h/.cpp` organization;
  the API layer adapts these objects to C. `AffineTransform3` is affine value math;
  `Transformation3` is its DLL-backed object when managed identity is required.
- `HbpCoreRuntime` owns version, errors, initialization/shutdown and debug callbacks,
  not geometry/business services. Managed mutable setters must update native state.
- Dispose temporary native wrappers deterministically, including elements of
  temporary collections. Finalizers are fallback cleanup. Transfer ownership
  explicitly; do not give two mutable owners the same handle without a lease.
- For a new family, cover dynamic exported symbols, create/destroy/mutation,
  invalid arguments and installed-DLL C# integration; update import inventories.
  Assess license, static packaging and dependency impact before adding a library.
  Preserve the absence of OpenCV/Boost runtime dependencies; avoid large
  frameworks for a narrow numerical operation.

## Coordinates: exactly one boundary conversion

Native neuroimaging geometry is right-handed; Unity scene geometry is left-handed.
At their boundary `R = diag(-1, 1, 1)`, so `p_unity = R * p_native` and the inverse
is the same reflection. This is the HiBoP integration convention, not a universal
claim about every input format: readers normalize formats into native space.

| Data | Conversion |
|---|---|
| Points, directions, normals, mesh vertices | Reflect X exactly once |
| Triangle winding | Reverse once when reflecting a mesh |
| Bounding box | Reflect corners, then recompute component-wise min/max |
| Affine | `A_unity = R * A_native * R` |
| Rotation/quaternion | Use the conjugated matrix, not an arbitrary component sign flip |
| Spacing, dimensions, distances, radii, indices, UV, colors, masks, labels | No reflection |

`Vector3` means Unity space by default; `Vec3`/`hbp_Vec3` mean native space.
Arithmetic must combine values in the same space. `Vec3.FromVector3`/`ToVector3`
normally convert. The current `convertReferenceSystem: false` exceptions are
`RawSiteList.AddSite(nativePosition)` and unsigned `Volume.Spacing`. A native
value temporarily stored in `Vector3` includes `Native` in its name/documentation;
preserve the conversion allowlist tests. Do not add scattered axis flips, negative
scene scales or another transform to compensate for an uncertain boundary.
Verify asymmetric bounds, winding/normals, affine conjugation and site/cut
distances, not only symmetric meshes.

## Volumetric projection and invalidation

`ActivityProjectionGrid` is defined by the reference volume, dimensions and
interpolation, not by the chosen surface or its vertices. Its sample centers span
the source volume's endpoint centers: each axis scales by
`(sourceDimension - 1) / (gridDimension - 1)`, with dimensions at least two.
Keep `gridToWorld`/`worldToGrid` explicit. Never form a grid by implicitly unioning
surface and volume AABBs in potentially incompatible spaces.

`Base3DScene` owns the grid; it retains its volume. An activity generator retains
the grid and owns its native field; a surface generator retains the activity
generator and surface. Rebind consumers before releasing a replaced grid.
Do not invent an extra `ActivityField` wrapper without a distinct lifetime need.

| Change | Invalidate |
|---|---|
| Reference volume or grid dimension/interpolation | Grid, field and dependent bindings |
| Activity values, sites/implantation, source/mode, ROI/mask, influence radius | Field on the existing grid |
| Surface or hemisphere geometry | Surface binding/UV, preserving field and timeline |
| Triangle visibility/eraser | Mesh/mask and existing UV application, not field/stencils |
| Cut geometry | Cut sampling/textures |
| Time selection, alpha, palette or display calibration | Display sampling/colors, not already computed raw field |

Cache keys cover these inputs. Surface bindings include geometry identity/version
and interpolation. `Surface.GeometryVersion` changes with vertex positions
(replacement, merge, transform), not UV, color, normals, triangle visibility or
appearance. Keep genuine CCEP source/mode invalidation; do not invalidate simply
because a compatible mesh changes. Direct `FMRIManager` overlays are a separate
path; changing activity projection does not authorize unifying them or restoring
the removed `GeneratorSurface` compatibility layer.

### Surface coverage and user decisions

Coverage measures actual sampled vertices, not AABB overlap. Build it in the
cached stencil pass, lazily on validation/projection; do not scan every frame.
States are unavailable, none, partial and complete. Out-of-grid vertices get
neutral UV, never a clamped border sample. Current significant partial coverage
means invalid vertices exceed `max(32, floor(totalVertices * 0.01))`.

Selecting a surface or volume is silent. A manual calculation with zero or
significantly partial coverage offers Continue/Cancel. Automatic calculation
waits silently until compatible geometry or explicit authorization; retain the
stale indication. Continue applies only to the current grid/surface version pair.
If geometry changes while the dialog is open, validate the new pair again.

### NIfTI and Localizer export

Activity and mask exports use the grid's exact affine. Copy it into `sform`, use
its inverse for `sto_ijk`, and derive quaternion/qform/pixdim consistently with
niftilib. `sform` is authoritative: disable `qform_code` if shear cannot be
represented accurately by its quaternion. Do not recreate an affine from
dimension ratios or scale matrix rows instead of source-axis columns.

`LocalizerExportGridSettings` owns export resolution independently of interactive
`ActivityProjectionSettings`. Current limits/defaults: maximum dimension 2–512,
default 80, trilinear sampling. Axis dimensions are
`max(2, truncate(maximumDimension * sourceAxis / largestSourceAxis))`.
Preview dimensions, voxel count and uncompressed activity-per-timepoint plus mask
cost without constructing a native grid. This is not predicted gzip size;
multiple timepoints increase cost. More than 8,000,000 spatial voxels requires
confirmation before closing views; revalidate before any side effects. The window
does not persist this setting between opens. Export must not change interactive
grids/caches or open-scene resolution. Dispose native wrappers when export ends.

## MRI preview meshes

When a single-patient scene has no persistent usable patient mesh, generate an
in-memory approximate preview for each usable patient anatomical MRI. MNI meshes
already in the manager do not count as patient reconstruction; do not use total
mesh count to decide. A broken declared GIfTI is a load error, not an excuse to
silently replace it. Multi-patient MNI scenes do not get this fallback.

Native extraction uses finite intensities, robust thresholding (Otsu, absolute
or normalized), endpoint-preserving trilinear downsampling, a foreground mask,
connected-component/morphological cleanup and MC33 isosurface extraction. Transform
reduced-grid coordinates back to source IJK then through the NIfTI affine once;
normals/winding handle a negative determinant before the separate Unity boundary
conversion. Validate dimensions, occupancy, output indices/counts and degenerate
geometry; retain the diagnostic report. MC33 is a private static numerical
dependency, without its OpenGL or file-reader paths.
Preview extraction has active safety bounds: reduced mask at most 256³, bordered
scalar grid at most 258³, at most two million intermediate MC33 triangles, and
public counts fitting signed 32-bit integers. Preserve rejection/overflow checks;
these are preview-specific limits, not limits on all scientific grids or data.

This is an approximate isosurface, not skull stripping, cortical segmentation or
a source of inferred MarsAtlas labels. `RuntimeSingleMesh3D` owns distinct complete
and simplified surfaces, is tied to its source `MRI3D`, and is scene-owned. Add it
only to runtime meshes, not `Patient.Meshes` or preloaded persistent meshes. No
fictional GIfTI path, BIDS export, persisted preview selection or disk cache.
Duplicate detection uses the source MRI instance, not a display name. Runtime
preview cloning is unsupported; cleanup disposes surfaces and derived variants.

It exposes only `Both`, no hemispheres, MarsAtlas or MNI-specific resources.
Preserve these capability checks rather than treating its patient mesh type as an
anatomical-registration guarantee. Extraction runs on a worker; registration and
Unity publication return to the main thread. Cancel/failure disposes unpublished
results; a failed preview can be skipped with a diagnostic while other usable
MRIs continue. Do not import unimplemented manual-regeneration or global-preference
proposals as existing product behavior.

## Inflation: display geometry, anatomical science

Inflation is a derived representation of its `Mesh3D`, never a second independent
mesh resource. Allow geometrically admissible meshes regardless of name/type;
do not imply anatomical validity for arbitrary geometry. `BrainSurface` is the
displayed representation; `ReferenceSurface` remains anatomical. Activity, atlas,
fMRI, sites and all measurements use that reference. Reuse scientific UV/labels
through preserved vertex indices, never sample at inflated coordinates. Changing
representation alone preserves grid, field and timeline.
Camera target/centering follows the displayed `BrainSurface`, while scientific
positions and measurements remain on `ReferenceSurface`. Persist
`SurfaceRepresentation` in visualization configuration; older projects without
the field default to `Anatomical`.

Compute from currently loaded, transformed anatomical buffers, including MNI and
MRI previews. Inflate complete `Both`, or `Left`/`Right` independently; publish
hemispheres only when both succeed, then merge/simplify. Never inflate an already
inflated or visibility-truncated mesh, reread source files, apply the transform
twice, or keep a permanent native-coordinate duplicate. New results use
`CurrentSurfaceCoordinates`, algorithm version 2; geometry identity/version and
all algorithm options belong in cache keys. Cache variants in the owning mesh's
memory; disk caching needs measured justification.

Native jobs snapshot input, execute once on a worker and transfer one owned result
once. Cancellation publishes no partial surface. Request cancellation and await
native completion before destruction. Preserve vertex order, triangle indices,
UV/colors and triangle visibility; only positions/normals change. Reject invalid
or non-manifold topology with diagnostics. Process connected components
independently, preserving small components and isolated vertices; use configured
boundary/rescaling policy, not silent vertex deletion. This is geometric
inflation, not an assertion of exact Workbench equivalence.

Cuts and their colliders remain anatomical and editable in inflated mode. They
continue to update but do not clip the inflated envelope or visual transition;
returning to anatomy uses current cuts, not an old snapshot. Sites/electrodes
remain visible/selectable at anatomical positions. Explain this once per
visualization, with one informational OK dialog shared by first inflation with
cuts and first cut created while inflated.

Animations interpolate prepared positions/normals in all relevant shader passes;
they do not mutate scientific state. Preserve UV/colors, update the collider at
completion and suspend geometry-dependent interactions during transition. Full
surface masks transfer only with identical topology; never transfer a mask blindly
to an independently simplified surface. Transactional publication, stale source
checks, shared busy scopes, acknowledgements and loading-modal timing belong in
[synchronization](synchronization.md) and [runtime and state](runtime-and-state.md).

## Performance evidence

Preserve numerical meaning while measuring CPU cost and retained/peak memory.
Reuse buffers/native objects where ownership permits; avoid redundant copies
and size/data calls. Use reproducible workloads and record platform, data sizes
and options; historical timings are not product hardcaps. Keep permanent
benchmarks; remove investigation-only instrumentation when its purpose ends.
Time-series mapping/caches belong in [data and persistence](data-and-persistence.md).
