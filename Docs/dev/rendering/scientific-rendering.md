# Scientific rendering

The project uses URP 17.5, Universal Forward, Render Graph and Linear color space.
The Built-in migration is implemented; its earlier audits described a superseded
state. PPv2/AGM packages are no longer active dependencies.

## Pipeline and responsibilities

Assets in `Assets/Settings/Rendering/` separate profiles: Desktop uses
MSAA 1, with HDR/shadows/additional lights disabled; Quest uses, among other
settings, MSAA 4 and enables main-light shadows/additional lights. Desktop
settings are therefore not a universal description of the XR profile. The
scientific brain uses its own camera-relative lighting, independently of
scene lights.

| Owner | Responsibility |
|---|---|
| `BrainMaterial.cs`, `Assets/Shaders/HBP/Brain/` shaders | Anatomy, overlays, extrusion, clipping, inflation |
| `Includes/HBPColor.hlsl`, `HBPLighting.hlsl` | Conversion, composition and scientific relief |
| `HBPEdgeRendererFeature` | Transparent brain composite and Render Graph outlines |
| `HBPRenderTextureDescriptorFactory`, `HBPRenderTextureOwner` | View target descriptors and lifetime |
| `View3D`, `View3DUI`, `Scene3DWindow` | View sizing, captures and exports |
| Native activity/cut generators | Values, support and scientific composition |

The low light count and cost multiplied across views explain the Forward
choice; Deferred/Forward+, HDRP and a complete custom SRP offered no demonstrated
benefit. Dedicated HLSL shaders retain the material API and 20 clipping planes.
Extrusion, clipping and inflation deformation must remain consistent across
all relevant passes.

## Colors and activity

Palettes/UI are sRGB; convert to Linear **once** before composition.
Scalar, mask, index and alpha textures are not sRGB. A color carried by
vertex, texture or palette must have the same mapping. Atlases and
selection/hover remain semantically distinct. Current composition is:

```text
paletteAlpha = 1 - (1 - clamp(scientificAlpha, 0, 1))²
mesh = lerp(litAnatomy, scientificLinear * controlledRelief, paletteAlpha)
cut  = lerp(anatomy, scientificLinear, paletteAlpha)
```

At alpha 0.8, palette weight is 0.96. Cuts and legends are unlit references.
Diffuse scientific relief multiplies color, bounded by 1.08 and channel
headroom; a very localized neutral highlight is an intentional visual exception.
It changes neither normalization, LUT nor thresholds. Tone mapping, grading
or palette changes are not used to compensate for a color error.

In continuous interpolation, the shared volumetric field undergoes two passes
of a separable five-tap binomial kernel per axis for values/weights, followed
by weighted trilinear interpolation. The cache is shared per time point between
mesh and cuts. Support separately receives a single radius-two pass:
attenuation over two inner layers and a halo of at most two outer cells.
Nearest mode remains the discrete reference. RGB is not separately blurred
after the LUT. The mesh does not add a mask based on the nearest MRI voxel.

The `Mask activity on MRI background` (cuts only) and
`Smooth activity boundaries` (support alpha) preferences are independent,
enabled by default; disabling smoothing changes neither values nor palette mapping.

### Current anatomical support for cuts

Current native code has replaced the former "5% threshold then border-connected
flood-fill" rule. `anatomy_value_is_supported` uses intrinsic calibration,
independent of the display window and palette: finite nonzero value,
finite positive robust range `calMax - calMin` and
`(value - calMin) / range * 254 >= 1`.

`CutGeometryGenerator` integrates this predicate over a **4 × 4** footprint
per pixel, with a shortcut for uniform regions; Raw mode retains binary
coverage. This geometric coverage is cached independently of activity.
`CutGenerator` reconstructs edge colors to remove the black-background
contribution, then applies fractional coverage. Activity values/weights
and their smoothing remain separate. Native references:
`src/volume/anatomy_support.h`, `src/generators/cut_geometry_generator.cpp`,
`cut_generator.cpp`, test `test_calibrated_anatomy_support`.

## Transparency, outlines, sites and ROI

The transparent brain is no longer just a classic alpha-blended material.
`HBPTransparentBrainSurface` writes the visible surface and its depth into
dedicated targets (`Cull Back`, `ZWrite On`), then a composite compares
brain/scene depth before ordinary transparents. This `HBPEdgeRendererFeature`
pass runs for Game cameras even when Edges is off.
It avoids indiscriminately compositing every brain layer. Do not remove
the feature on the assumption that it only handles outlines.

The optional Edges pass runs before post-processing: depth/normal
discontinuities on opaque brain/cuts and the outer silhouette of transparents.
`HBPEdgeData`/`HBPEdgeMask` tags exclude sites, ROI and interactive markers.
Markers retain normal occlusion. A change in slope, rather than a uniform
slope, triggers depth outlines. Pass data is isolated per camera;
the composite preserves alpha.

The actual ROI is an **analytical cage**, despite the filename
`ROI/HBPROIWireframe.shader`: seven circles, silhouette, contrasting understroke
and thickness stabilized by `fwidth`, without a geometry shader or need for
barycentric coordinates. It uses a compact indexed mesh.

Sites retain a small shared mesh, specialized RGBA shader, per-state materials
and individual objects/colliders for picking. The shader supports an instancing
variant, which does not mean the product renderer uses instancing.
A redesign must preserve selection, hover, filters, size and per-column
visibility; historical measurements explain retaining the simple path.

## Views, resources and export

View RenderTextures have an explicit RGBA8 sRGB, D24/S8 descriptor,
MSAA 1, without mipmaps or random-write. Size follows physical pixels after
Canvas scaling. Reuse a compatible descriptor; release/destroy on
replacement, hiding, minimization or destruction. An inactive view stops
its camera; no zero-sized target is allocated.

Export uses the same cameras, shaders and state as an equivalent view.
Individual PNG: zero-alpha background, requested resolution (2048² by default),
straight alpha. For a premultiplied sRGB target, decode to Linear, divide
RGB by alpha, clamp/re-encode sRGB; zero RGB when alpha is zero. Dividing
sRGB bytes directly or unpremultiplying twice creates halos. Historical
composites retain the opaque `#282828` background.

`finally` blocks restore camera target/aspect/background, matrix scroll and
time indices; they release captures and video. A JPEG frame writes its
current length, not an older buffer's capacity.

## Sources and qualification

[Rendering code](../../../Assets/Scripts/HBP/Rendering/),
[shaders](../../../Assets/Shaders/HBP/),
[rendering tests](../../../Assets/Tests/EditMode/HBP.Rendering.Tests/),
[protocol and historical measurements](rendering-validation.md).
The [projection](../activity-projection.md) and
[inflation](../surface-inflation.md) contracts explain scientific geometry.

Windows qualification of the migration dates from August 2026. It does not
prove current qualification of every desktop or headset; Metal compilation
is not a hardware trial. Earlier VR deferrals concern the past Desktop
migration, not the current Quest scope.
