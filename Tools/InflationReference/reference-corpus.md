# Inflation reference corpus

This directory groups Python tools and geometric comparison data produced on
August 27, 2026. It is independent of Unity and `hbp_core`: verifying this
corpus does not test HiBoP's current algorithm. Its
[scientific contract and product tests](../../Docs/dev/surface-inflation.md)
are documented separately. Current computation uses already transformed
anatomical buffers, version 2; corpus transformations remain reference cases
and do not prescribe rereading GIfTI/TRM at runtime.

## Content and reproduction

All paths below are relative to `reference-data/`.

| Artifact | Purpose |
|---|---|
| `corpus.json` | Inputs and expected eligibility |
| `reference-metrics.json` | Hashes, topology, bounds, center, area, RMS, edge/area distributions |
| `external-reference-parameters.json` | Oracle parameters and provenance |
| `synthetic-surfaces/` | Deterministic synthetic surfaces |
| `transforms/` | Rigid, uniform and anisotropic oracles |
| `workbench-results/` | Manifest, inflated/very-inflated surfaces and comparison image |

From the HiBoP root, with the script's Python dependencies available:

```powershell
python Tools/InflationReference/analyze_reference_corpus.py --verify
python Tools/InflationReference/render_mni_comparison.py --output <image.png>
```

The collector requires Python 3.10+, NumPy and NiBabel; rendering also requires
Matplotlib. `--verify` regenerates synthetic surfaces and metrics in a temporary
directory, checks consistency and compares the report while neutralizing
machine-specific tool versions/availability. It analyzes retained Workbench
results without recomputing them. The three synthetic TRM files remain LF
through `.gitattributes` to preserve their reference hashes on Windows and Unix.

Without an option, the collector rewrites synthetic surfaces and
`reference-metrics.json`; inspect differences before replacing a reference.
Rendering writes to `workbench-results/mni-comparison.png` if `--output` is
omitted. Use a separate output to inspect a new image without replacing
existing evidence. To explicitly regenerate Workbench results:

```powershell
python Tools/InflationReference/analyze_reference_corpus.py --generate-workbench-references --workbench-executable <path-to-wb_command.exe>
```

The corpus covers two MNI hemispheres and their bundled inflated surfaces,
three nonclinical patient proxies of different densities, and open, degenerate,
non-manifold and multi-component surfaces. It contains no clinical data.
`.trm` classes come from singular values and determinant, not filenames.

## Provenance and historical results

Operational paths in the corpus, metrics and manifest source/outputs fields
follow this organization. Commands recorded in the manifest and internal GIfTI
metadata retain original execution paths: they are provenance traces,
not commands to rerun verbatim. GIfTI files and the reference image were not
rewritten during the move. Commands above use current paths.

Workbench **2.2.1**, commit `01164ffa47f2778088bd6ff472ec9cd9a57f5b42`, iteration
factor **1.0**: seven eligible surfaces produced inflated and very-inflated
outputs. The manifest retains executable version/hash, commands, input hashes,
stdout/stderr and outputs. The original method for bundled MNI inflated
surfaces is not attested: they are not attributed to Workbench or FreeSurfer.

| Hemisphere | Vertices / triangles | Workbench / anatomical area | Historical / anatomical area | Workbench / anatomical XYZ extent |
|---|---:|---:|---:|---|
| Left | 33,036 / 66,068 | 0.511136 | 0.511182 | 1 / 1 / 1 |
| Right | 33,263 / 66,522 | 0.533895 | 0.512362 | 1 / 1 / 1 |

Topology is identical; positions are not interchangeable.
Workbench preserves XYZ extent while bundled references are more contracted.
FreeSurfer was unavailable for this campaign. The baseline qualifies MNI/synthetic
oracles and their historical reproducibility, not clinical quality or current
software performance. No campaign was rerun for the documentation consolidation.
