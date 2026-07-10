# hbp_core coordinate-system integration

HiBoP applies one project-level spatial convention at the `hbp_core` boundary:

```text
R = diag(-1, 1, 1)

p_unity  = R * p_native
p_native = R * p_unity
```

The convention is based on the following invariants:

- `UnityEngine.Vector3` is in Unity's left-handed scene space by default;
- `HBP.Core.DLL.Vec3` and `hbp_Vec3` are in hbp_core's native right-handed space by default;
- normal `Vector3`/`Vec3` conversion reflects X;
- spatial arithmetic never mixes the two spaces;
- a `Vector3` that intentionally stores native coordinates includes `Native` in its name and documentation.

The only current calls using `convertReferenceSystem: false` are:

- `RawSiteList.AddSite`, because `nativePosition` is explicitly a native position stored in a `Vector3`;
- `Volume.Spacing`, because spacing is an unsigned component magnitude.

Points, directions and normals reflect X. Mesh triangle winding is reversed once because a one-axis reflection changes handedness. Bounding boxes are converted by reflecting their corners and recomputing min/max. Native affine transformations are exposed through `A_unity = R * A_native * R`.

Distances, radii, dimensions, spacing, indices, UVs, colors, masks and labels are not reflected.

Compatibility conversions required by the transitional `hbp_export` backend must be guarded by an explicit legacy branch. They must never run on values already returned in Unity space by `hbp_core`.

The detailed native contract and the permanent test obligations are maintained in `hbp_core/docs/coordinate_system_contract.md`.
