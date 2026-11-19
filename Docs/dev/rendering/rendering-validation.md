# Verifying rendering and interpreting measurements

This reference describes reusable checks and a historical comparison that
explains renderer choices. It does not certify the current build.
To run Unity, follow the
[validation workflow](../../../.agents/skills/hibop-unity-validation/SKILL.md).

## Reproducible campaign

The permanent collector is
[RenderingBaselineCapture](../../../Assets/Scripts/HBP/Dev/Rendering/RenderingBaselineCapture.cs).
It loads `visu_full_test.hibop` / `Small`; another path can be supplied through
`HIBOP_RENDERING_BASELINE_PROJECT`. It writes captures, a manifest and
`latest-run.txt` under `.test-results/rendering/`, outside versioned documentation.
These local results may be absent from another checkout.

Fix source/build, dataset, camera, timeline, scientific parameters, physical
view size, resolution and theme. Wait for loading/animations, then check two
stable captures. The manifest must identify Unity, OS, GPU, driver/API,
quality, columns/views/sites, VSync/focus and human verdict.
Measure on raw SDR PNGs, without external color management. The collector suspends
idle mode and restores state; an `IdleThrottled` or non-`Normative` series
cannot support performance conclusions.

| Check | Required cases |
|---|---|
| Data/color | Uniform/vertex/texture sRGB/Linear patches, LUT, positive/negative thresholds, alpha, temporal interpolation |
| Geometry | Opaque/transparent anatomy, extrusion, clipping 0/1/20, inflation and cuts |
| Interaction | Atlas hover/selection, filtered/blacklisted sites, picking, normal/selected ROI |
| Outlines | Off/on, opaque relief/transparent silhouette, exclusion of sites/ROI/markers |
| Export | Individual/composite/video PNG, background alpha, recomposition over white and `#282828`, state restoration |
| Resources | 100 resize/export cycles; 1×1, 8×3, 9×3 views; return to an owned-resource plateau |

Reference tolerances: flat scientific patches at most one unit per 8-bit channel
in the same environment; export/equivalent offscreen view identical outside
anti-aliased edges; cross-platform target `DeltaE00 <= 2`. Controlled mesh relief
does not require RGB equality with an unlit cut under every normal.
Do not use pixel-by-pixel comparison of the entire anatomy as the sole verdict.
Background/anatomy/science/edge masks prevent a good average from hiding
a small incorrect scientific region.

Human validation covers anatomical readability, color ambiguity,
depth/transparency, outlines and intentional differences. Record capture,
configuration, validator/date and verdict. Test presence, shader compilation
and hardware qualification are three different forms of evidence.

Tests: `HBP.Rendering.Tests` (color, shaders, material inventory, ROI/sites,
textures, Edges/exports), `HBP.Module3D.PlayModeTests`, native generator tests.
Historical `Phase*` test names remain current entry points.

## Performance protocol

Measure main/render CPU/GPU, median/P95/P99, SetPass/batches, geometry, culling,
GC allocations and live resources. Same machine, state, resolution and warm-up;
FPS under VSync is insufficient. The historical comparison called for
investigation above 5% and rejection above 10% sustained P95 regression,
unless explicitly accepted. These relative criteria are not product limits.

The isolated 30,000 sites × 1 view case measures the renderer: temporary sites
do not have the real model's colliders. The 9 columns × 3 views case, possibly
with 30,000 sites per column, is a stability stress test, without a smoothness promise.

## Final historical comparison of August 7, 2026

Windows 11, RTX 2070 SUPER, D3D11, Unity `6000.5.2f1`, Fantastic quality, Linear;
120 warm-up frames followed by 900 measured frames, focus retained, idle disabled.
`Small`: 3 × 348×516 pixels; 30,000 sites: 1 × 348×516;
9×3: 27 × 112×200. Local Built-in `20260807-191903` and URP
`20260807-211822` runs, under `baseline-birp-phase5` / `urp-phase5`.

| Scenario | BIRP / URP median (ms) | BIRP / URP P95 (ms) |
|---|---:|---:|
| Control with 3D cameras off | 3.249 / 2.850 | 25.757 / 3.455 |
| Opaque, Edges off | 4.546 / 3.872 | 60.256 / 4.577 |
| Opaque, Edges on | 4.493 / 4.040 | 15.704 / 4.910 |
| Transparent, Edges off | 3.975 / 3.859 | 16.806 / 4.472 |
| Transparent, Edges on | 4.473 / 4.104 | 16.635 / 4.958 |
| Rotation | 4.521 / 4.223 | 17.374 / 5.183 |
| Temporal activity | 16.581 / 19.512 | 20.053 / 20.785 |
| Atlas hover | 4.717 / 4.605 | 28.125 / 5.630 |
| Cut movement | 13.308 / 14.355 | 16.260 / 16.584 |
| 30,000 sites, 1 view | 38.075 / 17.573 | 58.872 / 19.501 |
| 9×3 views | 10.129 / 10.011 | 23.104 / 11.068 |

These measurements justified retaining simple sites and Windows acceptance.
They do not show a uniform gain: activity median +17.7%, cut +7.9%,
but respective P95 +3.7% and +2.0%. They do not predict current timings
or those of another GPU/API or Quest.

Reusing three native buffers and merging smoothing traversals had reduced
activity P95 from an intermediate candidate's 38.816 to 20.785 ms without
changing kernels. A global support cache would be incorrect because it depends
on time points and thresholds. Static views added no GC over the control
(10,513 bytes/frame common to application/editor/collector);
this result does not mean zero total GC.

After the 9×3 stress and return to Small, owned textures went from 27→3 and
graphics memory from 354.6→318.7 MB (322.3 before stress); Unity reserved memory
remains a high-water mark. The historical report indicates Windows visual
acceptance and passing suites of 79 rendering, 43 PlayMode and 13 native tests.
None of these campaigns was rerun for the documentation overhaul. macOS Apple
Silicon/Metal and Linux/Vulkan validations were still deferred in that report;
their current hardware status is not established here. Linux OpenGL Core was
only a fallback to qualify, not an implicitly validated platform.
