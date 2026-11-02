# MRI-derived mesh previews

A **single-patient scene without a persistent patient mesh** automatically
generates an approximate envelope for each usable patient MRI. MNI meshes
do not count as patient reconstructions. A declared but unreadable GIfTI
remains a loading error; fallback does not conceal its corruption.
Multi-patient MNI scenes do not generate individual previews.

## Loading, identity and lifetime

After MRIs and before sites, `Base3DScene.EnsureRuntimePatientMeshAsync`
extracts the surface on a worker, then registers the result on the Unity thread.
Cancellation is checked before and after the synchronous native call: it does
not interrupt MC33 in progress, but releases any unpublished output. A failure
is logged and does not prevent other MRIs from producing their preview.

`RuntimeSingleMesh3D` retains its `SourceMRI`, report and two full/simplified
surfaces with distinct handles. MRI identity, rather than its name,
prevents duplicates. The mesh is owned by the scene, released by `Clean`, without
implicit handle cloning. It enters neither `Patient.Meshes`, persistent
preloads nor BIDS. No GIfTI, fictitious path or disk cache is produced;
its transient selection is not saved as a persistent mesh.

Initial selection prefers an explicitly configured loaded mesh, then
the patient preference; without a patient mesh, it uses the configured MRI's
preview, the first successful preview, then MNI. No heuristic uses MRI names.

## Native extraction

`Volume.ExtractPreviewSurface` calls `hbp_volume_extract_preview_surface`.
ABI structs carry `struct_size`; null options mean defaults. Output is
initialized to null, transferred to the caller only on success and released by
`hbp_surface_destroy`. An empty/null volume is `HBP_INVALID_HANDLE`;
invalid options/layout are `HBP_INVALID_ARGUMENT`; a geometry failure with
a native message does not publish a surface.

The current pipeline:

1. Finite intensities, robust 0.5/99.5 percentile bounds; Otsu, absolute
   or normalized threshold within these bounds.
2. Trilinear downsampling preserving outermost centers; default maximum
   dimension 160, target 20,000 triangles.
3. Mask, largest 26-connected component, binary closing, 6-connected cavity
   filling, background border and scalar smoothing.
4. MC33 on the in-memory grid; remove the border, return to source IJK, then
   apply the NIfTI affine **once**. A negative determinant corrects native
   winding before the separate Unity conversion.
5. Index checks, degenerate-triangle cleanup, simplification and normals;
   retain the report of dimensions, threshold, counts and timings.

Active bounds: maximum dimension 32–256, target 1,000–200,000 triangles,
occupancy 0.5–90%, mask at most 256³, bordered grid 258³, at most two million
intermediate MC33 triangles and int32-compatible counts.
These limits belong to the **preview**, not to all scientific data.

MC33++ 5.4 is a private static dependency of the native repository, pinned to commit
`252dabd7ea6dea3d0529e66b047adbe878caf2fa`. Readers and OpenGL functions
are not part of the extraction path. `USE_MM_RSQRT_SS=0` avoids an x86
dependency; `GRD_ORTHOGONAL` is undefined because of an MSVC incompatibility
in the upstream commit. Rationale, license and notices are in
`hbp_core/third_party/mc33/UPSTREAM.md` and `third_party/components.json`.

## Scientific scope and limits

The envelope may represent the head rather than the cortex: it is neither skull
stripping, segmentation nor a qualified cortical reconstruction.
It offers only `Both`, without hemispheres, MarsAtlas or MNI resources.
Selection applies these capabilities and CCEP MarsAtlas returns to Site mode.
Activity, cuts, ROI, eraser and patient fMRI remain subject to actual data
alignment and [projection coverage](activity-projection.md).

After configuration loading, the diagnostic measures site distances to
the nearest **vertex** (not the exact triangular surface), P50/P90/P95.
If at least 25% exceed the smallest active influence radius, a log suggests
`ceil(P90 + 2 mm)`. No radius is changed automatically.

Manual regeneration with thresholds/profiles and a global enable setting
were proposals; this automatic path does not implement them. Their absence
alone does not constitute an accepted backlog.

## Sources and validation

- [Runtime model and diagnostic](../../Assets/Scripts/HBP/Core/Object3D/Mesh3D.cs),
  [manager](../../Assets/Scripts/HBP/Data/Module3D/Modules/MeshManager.cs),
  [loading](../../Assets/Scripts/HBP/Data/Module3D/Base3DScene.cs),
  [wrapper](../../Assets/Scripts/HBP/Core/DLL/Volume.cs).
- Native: `hbp_core/src/meshing/preview_surface_extractor.cpp`,
  `src/api/preview_surface_api.cpp`,
  `tests/native/hbp_core_preview_surface_functional_test.cpp`.
- [Runtime tests](../../Assets/Tests/EditMode/HBP.Serialization.Tests/PreviewSurfaceRuntimeMeshTests.cs)
  and [PlayMode scenes](../../Assets/Tests/PlayMode/HBP.Module3D.PlayModeTests/Module3DScenePlayModeTests.cs).

Oracles cover anisotropic/negative affines, NaN/Inf, topology, errors,
ownership, multi-MRI selection and persistent/runtime separation. For an
independent anatomical inspection, the native `hbp_core_preview_surface_export`
tool produces a `SPACE=RAS` OBJ and a report to overlay on the NIfTI in 3D Slicer;
see `hbp_core/docs/preview_surface_visual_validation.md`.

Historical measurement from July 2026: phantom `160 × 192 × 144`, reduced to
`107 × 128 × 96`, approximately 0.30 s on the Windows development machine,
62,600 → 20,000 triangles. CPU not recorded here: neither a cross-platform budget
nor clinical qualification. Earlier reports announced Windows validations;
current hardware qualification was not rerun for this documentation
consolidation.
