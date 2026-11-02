# hbp_core / Unity coordinate boundary

Native readers normalize their formats into the right-handed native scientific
coordinate system. The HiBoP boundary to the left-handed Unity scene applies a
single reflection: `R = diag(-1, 1, 1)`, so `p_unity = R * p_native` and
`p_native = R * p_unity`. This is not a universal statement about the raw
coordinates of every input format.

| Data | Conversion |
|---|---|
| Points, directions, normals, vertices | Reflect X once |
| Triangles | Reverse winding once along with the reflection |
| Bounding box | Convert, then recompute min/max |
| Affine | `A_unity = R * A_native * R` |
| Rotation | Conjugated matrix; not an arbitrary quaternion component change |
| Distances, radii, dimensions, spacing, indices, UVs, colors, labels | No reflection |

`Vector3` uses Unity coordinates by default; `Vec3`/`hbp_Vec3` use native coordinates.
The `Vec3.FromVector3` and `ToVector3` conversions reflect X. Current explicit
exceptions to `convertReferenceSystem` are `RawSiteList.AddSite(nativePosition)`,
`RawSiteList.GetNativePositions()` (native scientific coordinates) and
`Volume.Spacing` (unsigned magnitudes). A `Vector3` temporarily holding native
coordinates must be identifiable as such; calculations must never mix the two systems.

Production loads `hbp_core` from `Assets/Plugins/Native/` per platform.
`Assets/Plugins/Editor/LegacyNative/` retains `hbp_export` as a test oracle,
with no runtime fallback. Do not apply a legacy adaptation to a result
already converted by the current wrapper.

References: [value types](../../Assets/Scripts/HBP/Core/DLL/HbpCoreValueTypes.cs),
[transformations](../../Assets/Scripts/HBP/Core/DLL/Transformation3.cs),
[bounds](../../Assets/Scripts/HBP/Core/DLL/BBox.cs),
[native sites](../../Assets/Scripts/HBP/Core/DLL/Electrodes.cs),
[neighboring native repository contract](../../../hbp_core/docs/coordinate_system_contract.md).
Check asymmetric bounds, winding/normals, oblique affines and site/cut distances;
a symmetric sphere does not detect a double reflection.
