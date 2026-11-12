# Scientific rendering and image export

Read for shaders, palettes, cameras, RenderTextures, contours and image exports.
For sampling, coordinates or derived geometry, read the relevant section of
[scientific computing](scientific-computing.md).

## Priorities and pipeline

Scientific correctness takes precedence over functional parity, display/export
parity, platform compatibility, performance, historical appearance and aesthetics,
in that order. Correcting a historical color error can intentionally change an
image, but requires explicit visual validation. Keep URP Universal Forward,
Render Graph and Linear color space as the current foundation. The low light
count and many views do not justify Deferred/Forward+, HDRP or a custom SRP.
Do not impose the old desktop migration's postponed XR scope on current Quest work.

Use dedicated shared HLSL for scientific surfaces. Keep clipping, extrusion and
inflation deformation consistent in color, depth, normals and edge passes.
Keep material properties compatible with SRP Batcher; measure before assuming
`MaterialPropertyBlock` or a new shader arrangement improves performance.
Anatomical lighting is lightweight and camera-relative, without shadow maps;
preserve sulcal readability and avoid unapproved global contrast/saturation drift.
Do not enable global tone mapping/color grading to compensate for incorrect colors.

## Color is data

Separate anatomy, scientific values/classes and visual aids. Palette/UI colors
are sRGB unless their source space explicitly says otherwise. Convert to Linear
exactly once before GPU composition. Texture and vertex paths carrying the same
palette color must agree. Scalar activity, masks, indices and depth are not sRGB
textures; directly colored sRGB images are. Atlas index textures use point
filtering; continuous/anatomical textures use explicitly chosen interpolation.

Share value-to-palette normalization across mesh, cuts, legends and exports.
Atlas selection/hover must be identifiable, reversible visual aids rather than
silently replacing scientific colors. Positive/negative thresholds, out-of-range
values and temporal interpolation have deterministic behavior; temporal
interpolation adds no color-space conversion.

Scientific hue/chroma and palette mapping remain stable. Mesh relief may modulate
luminance independently of scene lights; cuts and legends are nonlit references.
The current composition is:

```text
alpha_palette = 1 - (1 - alpha_scientific)^2
mesh = lerp(lit_anatomy, scientific_linear * controlled_relief, alpha_palette)
cut  = lerp(anatomy, scientific_linear, alpha_palette)
```

Relief is multiplicative, with bounded diffuse highlights to avoid channel
clipping; do not apply ordinary anatomy lighting/specular indiscriminately to
science. The controlled surface model allows a very localized neutral highlight
at its summit; this exception must not change the diffuse palette mapping.
Alpha endpoints stay 0/1; scientific alpha 0.8 has palette weight 0.96.
Shared owners include `Assets/Shaders/HBP/Includes/HBPColor.hlsl` and
`HBPLighting.hlsl` in that directory.

## Activity smoothing and support

Smooth scientific values and weights in the common volumetric field before
the LUT, never separately blur composed RGB images for mesh and cuts. Continuous
reference uses two passes of a separable five-tap binomial kernel per axis followed
by weighted trilinear sampling, cached per timepoint. Nearest remains the discrete
reference. Surface and cuts must sample the same values/support.

Smooth support separately from values. One radius-two pass produces a compact
band: at most two interior layers attenuated and two exterior layers of halo;
deeper interior stays opaque and support is zero beyond the band. Do not make
every nonzero value opaque through an opacity floor. This affects alpha, not
palette position. On the brain mesh, eligibility follows the activity field;
using nearest MRI intensity as another mask creates artificial triangle holes
in geometry that already defines the anatomical domain.

For cuts, `hbp_core/src/volume/anatomy_support.h` defines intrinsic tissue
support independently of palette and display window: a finite nonzero value,
a finite positive robust calibration range, and
`(value - recomputed_cal_min) / range * 254 >= 1`. This supersedes the older
5%-threshold/border-flood-fill rule. `CutGeometryGenerator` caches fractional
coverage from a 4×4 subpixel footprint (binary in Raw mode), with uniform-region
shortcuts. `CutGenerator` removes black-background contamination when reconstructing
edge colors and applies that coverage separately from activity values/weights.
Do not reinstate the old mask or alter scientific influence to fix an edge.

The default-on preferences are independent:

- `Mask activity on MRI background` masks cuts only, not the brain mesh.
- `Smooth activity boundaries` changes mesh/cut support alpha within the compact
  band. Off means binary voxel-exact support without changing values, palette
  or their interpolation.

## Transparency, contours, sites and ROI

On Desktop, transparent brain surfaces use a dedicated `HBPTransparentBrainSurface` pass
with `Cull Back` and `ZWrite On` into isolated color/depth targets. The
`HBPEdgeRendererFeature` composites that surface against scene depth before
ordinary transparents, even when Edges is disabled. Keep this composition when
changing contour settings; do not replace it with the former ordinary
`ZWrite Off` brain blending description. Preserve useful internal geometry,
sites, cuts, clipping and exports. Edges affect only brains/cuts: depth/normals contours for
opaque geometry, outer silhouette for transparent geometry. They do not alter
scientific interiors and remain optional with stable apparent thickness.
Exclude sites, ROI and interactive camera/cut markers from edge buffers while
preserving the markers' normal depth occlusion.

Quest composes brains, cuts, sites, cages and world-space scientific text against
the resolved opaque scene depth for each eye. It peels the four nearest
translucent fragment depths and blends the remaining fragments in draw order
behind them. Edges follow their source depth before the final XR overlays.
The timeline contributes its nearest visible canvas surface to scene depth
composition. Managed windows retain their canvas stack and compare each
fragment with the scene and timeline. Recompose the foreground scene over each
window fragment so a selected window can cover a nearer window while geometry
between them remains visible. Material render queues route both kinds of canvas
to their respective passes.

`HBPEdgeRendererFeature` owns Render Graph edges before post-processing. Keep
settings/pass data isolated per camera; no last-camera global state. Preserve
premultiplied alpha-over composition for transparency. A regular depth slope
such as a nearly edge-on plane must not become repeated false edges.

Sites use their measured specialized rendering path. Do not substitute a generic
URP shader without comparative profiling, including 30,000 sites per column.
Any instancing redesign preserves selection, hover, picking, filters, size,
color and per-column visibility. ROI wireframes work without a geometry shader
on Metal. Multi-view stress includes 8–9 columns × 3 views; the combined extreme
must remain stable, not necessarily fluid. These are validation workloads, not
arbitrary product limits.

## RenderTexture and capture lifetime

Use `HBPRenderTextureDescriptorFactory` and `HBPRenderTextureOwner` in
`Assets/Scripts/HBP/Rendering/`. Specify LDR sRGB color, depth/stencil, MSAA and
disabled unused mipmaps/random-write explicitly. Reuse compatible descriptors;
release replaced allocations. View textures match physical screen size after
Canvas scaling without changing aspect ratio. Do not allocate zero-sized targets.
Hidden/minimized/disabled/destroyed views detach/release targets and stop cameras;
restore on activation. Recreate cached shared meshes if Unity destroyed them.

Default 3D background is `#282828`, subject to explicit theme/preference.
Individual PNG exports share the renderer, shaders and state of an equivalent
display view, except for transparent background (alpha zero). Edges preserve that
alpha. Existing composite exports retain opaque `#282828` unless separately
changed. No export-only grading, tone mapping, platform-specific scientific
mapping or duplicate renderer.

PNG output is straight alpha. For a premultiplied sRGB framebuffer, decode to
Linear, divide RGB by alpha there, clamp/re-encode to sRGB, and zero RGB at alpha
zero. Dividing sRGB bytes directly or unpremultiplying twice creates wrong colors
and halos. Verify recomposition on light and dark backgrounds.

Restore camera target/aspect/background in `finally`; release temporary captures
and video resources even on error. Restore trial-matrix scroll and dynamic-column
time indices after export. Variable-sized encoded video frames copy/write only
their current payload, never stale capacity bytes.

## Verification and build diagnosis

Check palette patches, mesh/cut/legend mapping, activity alphas/thresholds/time,
transparent/opaque anatomy, cuts, selection/hover, sites/ROI, Edges on/off,
individual/composite PNGs and repeated multiview resizing/export resource cleanup.
Compare equivalent offscreen states, not OS color-managed screenshots. Initial
targets: at most one 8-bit channel unit for flat palette patches in one environment,
equal display/export channels away from antialiased edges, and cross-platform
`DeltaE00 <= 2` for scientific patches. These require calibrated captures; they
are not evidence that every platform passed. Record hardware/API, resolution,
views/columns, site load and source state.

Human acceptance remains necessary for anatomical readability, scientific-color
ambiguity, transparency/depth, contours and intentional appearance changes;
record scene, configuration, capture and verdict. Automated comparisons alone do
not establish those qualities. Use `hibop-unity-validation` when executing checks;
distinguish static review from actual rendered evidence.

For build-only defects, inspect stripping/inclusion of shaders actually used.
Do not include every shader asset or change the scientific path on one platform
to hide a missing variant. Keep permanent rendering tests/benchmarks; remove
investigation-only instrumentation once resolved.
