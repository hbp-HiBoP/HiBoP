# Quest reference results

The [validation overview](../../Docs/dev/quest-autonomous/validation.md) explains
the results, technical choices and their limits. This directory contains final
comparisons that allow the figures to be checked. These September 2026 campaigns
do not qualify current code.

## Reading results by topic

| Topic | Context and main results | Scope |
| --- | --- | --- |
| Density | [Context](scientific-parity/density/measurement-context.json), [comparisons](scientific-parity/density/comparison-results.json), [numerical acceptance](scientific-parity/density/numerical-acceptance.json), [headset validation](scientific-parity/density/device-validation.json) | Nine variants, two repetitions per platform; 18 Windows/Quest comparisons. Acceptance on September 9, 2026. |
| iEEG | [Context](scientific-parity/ieeg/measurement-context.json), [comparisons](scientific-parity/ieeg/comparison-results.json), [numerical acceptance](scientific-parity/ieeg/numerical-acceptance.json), [input provenance](scientific-parity/ieeg/fixture-provenance.json), [offline validation](scientific-parity/ieeg/offline-device-validation.json) | 18 fixtures, two repetitions per platform; 36 Windows/Quest comparisons. Acceptance on September 10, 2026. |
| Multimodal scenes | [Context](scientific-parity/multimodal-scenes/measurement-context.json), [MNI comparison](scientific-parity/multimodal-scenes/mni-comparison-results.json), [synthetic patient comparison](scientific-parity/multimodal-scenes/synthetic-patient-comparison-results.json), [numerical acceptance](scientific-parity/multimodal-scenes/numerical-acceptance.json), [headset validation](scientific-parity/multimodal-scenes/device-validation.json) | Six modalities, 72/78 comparisons, 12 cut textures per fixture. Revision-2 binaries, USB only; acceptance on September 14, 2026. |

For multimodal scenes, the [binaries](scientific-parity/multimodal-scenes/player-provenance.json)
and [synthetic patient procedure](scientific-parity/multimodal-scenes/synthetic-patient-provenance.json)
define the exact scope. Final repetitions are retained because they establish
properties distinct from the first comparison:

| Property | MNI | Synthetic patient |
| --- | --- | --- |
| Warm repetition on Quest | [Comparison](scientific-parity/multimodal-scenes/mni-warm-repeat-comparison.json) | [Comparison](scientific-parity/multimodal-scenes/synthetic-patient-warm-repeat-comparison.json) |
| Repetition after interruption | [Comparison](scientific-parity/multimodal-scenes/mni-offline-repeat-comparison.json) | [Comparison](scientific-parity/multimodal-scenes/synthetic-patient-offline-repeat-comparison.json) |
| Resource and memory counts | [MNI and Desktop measurements](scientific-parity/multimodal-scenes/mni-and-desktop-resource-measurements.json) | [Corrected patient measurements](scientific-parity/multimodal-scenes/synthetic-patient-resource-measurements.json) |

Resource measurements also retain the residual Desktop meshes. Three stable
Quest runs do not demonstrate endurance stability. Manual observations and
acceptances remain separate from numerical differences.
The MNI and Desktop record also contains the first Windows patient cohort;
for the corrected patient procedure, use the dedicated patient record.
These cohorts must not be merged.

## Reproducible fixtures

Reusable inputs are retained alongside validation results:

| Fixture | Content and generation |
| --- | --- |
| [MNI anatomy](fixtures/mni-anatomy/anatomy-fixture.md) | Minimal project and anatomical visualization; deterministic archive generation from repository MNI references. |
| [Synthetic MNI contacts](fixtures/mni-contacts/contacts-fixture.md) | Two synthetic patients and eight contacts with known identities and coordinates; inputs used by tests and the iEEG generator. |
| [Synthetic iEEG](fixtures/synthetic-ieeg-fixture.md) | Generation procedure for BrainVision signals, protocol and archive; expected values and loading conditions. |

Fixture manifests describe sources, their fingerprints and archive entries.
They are read by the generator and remain operational data.
Serialized identifiers and artifact names containing old task numbers remain
stable for existing tests and launchers; guides describe their function
without requiring knowledge of those tasks. Generated files are local
and are not added to historically qualified results.

## Android native loading

The [elementary `hbp_math` check](native-runtime/math-abi-comparison.json),
its [build context](native-runtime/math-android-build-context.json) and
[provenance](native-runtime/measurement-context.json) supplement scene comparisons.
Retained from the September 11, 2026 campaign, they cover **17 public exports**
and four elementary results: standard error of the mean, Pearson correlation
and two Wilcoxon tests. Observed Windows/Android differences are all zero
on these inputs. This defines neither a universal tolerance, Unity qualification
nor full scientific qualification.
The compared library's hash matches the one in the build context.

## USB and network connectivity

The [October 27, 2026 Windows 11 / Quest 3 qualification](connectivity/windows-quest3-qualification-2026-10-27.md)
retains trial conditions, observed fallback/recovery delays and NCM acceptance
limits. macOS, Linux and Quest 3S are not qualified by this campaign.
NCM consent refusal was not exercised. This report is a documentation reference,
without a blocking publication gate in CI.

## Provenance and historical files

The 21 result, acceptance and provenance files were originally moved and
renamed **without changing their bytes**. Each `measurement-context.json` maps
their current name to their former path and SHA-256 in `retainedFiles`.
Human-readable French text in six acceptance/device-validation JSON files has
since been translated into English, including quoted user feedback. Those
translations preserve the meaning, measurements and technical status values;
they are not the original verbatim statements. The recorded SHA-256 values
still identify the original files before translation, retrievable from Git
as described below; they are not hashes of the translated copies.
For scientific parity campaigns, the context also extracts useful information
from the original manifest: versions, source state (including uncommitted files),
binary identity and scope. For multimodal scenes, it retains the reconciliation
between final and compiled source, with the sole documented line-ending difference.
For the native check, source and toolchain are described directly in the original
build-context file.

Paths and commands inside original records remain historical.
An old filename resolves through `retainedFiles`; it does not necessarily refer
to a neighboring file still bearing that name. Other paths to `.artifacts`,
`.test-results` or a local machine do not guarantee availability of large
buffers, APKs and logs. Retained comparisons allow per-case aggregates to be
checked, not all differences to be recalculated without original buffers.
No measurement was recreated during this organization.

Task identifiers remain provenance keys only: density `QUEST-019`, iEEG
`QUEST-023`, multimodal scenes `QUEST-024`. Superseded trials, compilation reports,
redundant captures and intermediate logs were removed from the checkout.
Their still-useful conclusions are in the documentation.

### Finding earlier evidence

Commit `003edbcf53ac2697525dee688b2a9be082159794` contains all versioned artifacts
before this consolidation. This archive reference is **not** each campaign's
execution commit: that is recorded in its context.
From the repository root, for example:

```powershell
git show 003edbcf53ac2697525dee688b2a9be082159794:Docs/dev/quest-autonomous/evidence/QUEST-009/manifest.json
```

The same command can inspect manifests of the first fixtures (`QUEST-001`,
`QUEST-013`) and TLS delivery (`QUEST-010`), or a report under
`Docs/dev/quest-autonomous/reports/`. Traces that were never versioned do not
become available through this method.

Earlier artifacts under `Docs/dev/quest-autonomous/scene-column-refactor/evidence/`
and the two CSVs in `Docs/dev/quest-autonomous/sync/M2-Small-inputs/` are also
available at that commit. Editor comparisons and first scene publication on
September 10–11 were partial; the retained September 14 multimodal qualification
provides the more complete hardware reference. Pause/resumption states,
deployment backups, intermediate builds and the manifest template add no
qualification to these results.

Synchronization measurements and their limits are in the
[validation reference](../../Docs/dev/quest-autonomous/sync/synchronization-validation.md).
Old reports and task lists under `Docs/dev/quest-autonomous/sync/` remain
available through the same command; historical names serve to locate a source,
not to organize current documentation.

## Adding a new reference

Retain a measurement when it justifies a still-useful conclusion, choice or limit.
Document content, parameters, hardware, versions, transport, cache conditions,
repetitions, units and acceptance criteria. Distinguish measurement,
interpretation and manual validation; also retain failures that explain a limit.
Replace an outdated reference rather than accumulating every development stage.

Transfer and Display measurements are already documented in the
[overview](../../Docs/dev/quest-autonomous/validation.md#small-transfer-september-2026).
This selection does not create raw measurement files from their summaries.
Future campaigns can add these topics with their actual results and context.
