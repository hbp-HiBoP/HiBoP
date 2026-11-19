# Quest validation and measurements

This reference consolidates **historical** evidence and its limits. The November 2, 2026 documentation revision ran neither Unity, tests, builds nor headset acceptance procedures. Test sources were inspected; no old result is presented as validation of the current checkout.

## Scientific parity: scope of acceptances

Density and iEEG fixtures compared Windows Desktop and Quest 3 using Unity **6000.5.2f1** and `hbp_core` **0.2.1**, on binaries/parameters identified by their manifests.

| Campaign | Domain and historical acceptance |
| --- | --- |
| [Density](../../../Validation/Quest/scientific-parity/density/measurement-context.json) / decision D30 | 18 density projection comparisons. UV: absolute tolerance 5×10⁻⁷, observed maximum ≈2.98×10⁻⁷; alpha: 2×10⁻⁷, maximum ≈1.19×10⁻⁷. Masks, grids and categories remain exact. |
| [iEEG](../../../Validation/Quest/scientific-parity/ieeg/measurement-context.json) / decision D31 | 36 iEEG comparisons. Same UV/alpha tolerances; maxima ≈4.17×10⁻⁷ / 1.79×10⁻⁷. [Numerical acceptance](../../../Validation/Quest/scientific-parity/ieeg/numerical-acceptance.json) is separate from raw evidence. |

These thresholds do not arbitrarily cover every computation, every platform or new native versions. Comparison reports retain exact per-case differences; full buffers were local artifacts, not data distributed in Git. Acceptance is a separate decision. Android compilation, Editor tests, parameter equality and visual observation each have a different scope.

The [density bench](../../../Validation/Quest/scientific-parity/density/comparison-results.json) covers **nine variants × two repetitions on each platform**, giving 18 Windows/Quest comparisons, native source `ffb7686`, MNI **69,104 vertices** for the full surface (some variants use reduced synthetic surfaces), grid **353,600 cells**, eight contacts: maximum absolute and RMS errors (calculated separately) for activity **2.980232238769531×10⁻⁷ / 1.3842878707934336×10⁻⁸**, alpha **1.1920928955078125×10⁻⁷ / 7.538859779568176×10⁻⁹**. The [iEEG bench](../../../Validation/Quest/scientific-parity/ieeg/comparison-results.json) covers **18 fixtures × two repetitions on each platform**, giving 36 Windows/Quest comparisons: maximum absolute and RMS errors for activity **4.172325134277344×10⁻⁷ / 2.113383258724899×10⁻⁸**, alpha **1.7881393432617188×10⁻⁷ / 7.256360437146824×10⁻⁹**. These are numerical buffer comparisons, not proof of pixel-by-pixel equality or latency including NIfTI/export/GPU.

The revision-2 [multimodal scene](../../../Validation/Quest/scientific-parity/multimodal-scenes/measurement-context.json) campaign extends the acceptance procedure to **six modalities**, first MNI then corrected synthetic patient: **72 / 78 comparisons**, **12 exact cut textures per fixture**, activity/alpha maxima **3.5762786865234375×10⁻⁷ / 1.1920928955078125×10⁻⁷**. Acceptance on **September 14, 2026** covers this USB procedure, not Wi-Fi, real patients or endurance. Residual **4 / 6 empty Desktop meshes per cycle** remained without a definitive explanation; no later resolution is inferred here. Early T13/T15 `Passed` results had been invalidated because the Task was cancelled: retain final results and actual draining of native preparation, not just the runner label.

The [retained results](../../../Validation/Quest/retained-results.md) include final warm and post-interruption repetitions, as well as resource counts. The patient acceptance procedure was initially rejected visually: a singular transformation came from a 4×4 identity file interpreted as `.trm`, then triangle orientation had to be corrected. The final procedure actually applies `MNI.trm` before synthetic deformation and retains visual validation separately from numerical parity.

## Small transfer: September 2026

Reference `visu_full_test / Small`, Windows + Quest 3, IL2CPP Players, Unity 6000.5.2f1. Early USB and Wi-Fi trials had different power/cache states; the measurements are not a statistical study.

| Historical state | Observed delay | Interpretation |
| --- | ---: | --- |
| Wi-Fi baseline | 105.533 s | Sequential delivery, 48,553 ZIP entries; reception alone ≈11.81 s, including hashes/TLS/storage. |
| Indexed pack, batch 2 | 59.914 s | Seven entries instead of tens of thousands, without scientific removal. |
| Snapshot/caches, batch 3 | 41.374 / 41.982 s | Separate advance initial preparation: 11.536 s. |
| HBT4/Zstd, batch 4 | 20.027 / 20.241 s | Separate initial preparation: 9.314 s Desktop + 5.371 s Quest; 8.4–8.8 s remain after reception. |
| Final version without detailed traces | 24.769 s, one Wi-Fi send | Small usable, but **22–24% slower than batch 4**, cause not established. No absence of regression claimed. |

The isolated codec corpus contained **397,025,397 bytes / seven resources**, including JSON 15,289,823 and pack 139,275,466. Zstd level 3, 256 KiB blocks: **125,219,971 bytes**, PC encoding ≈1,406 ms, Quest decoding ≈608 ms; LZ4 was faster but added approximately 52 MB. This microbenchmark explains the chosen tradeoff, without predicting network latency or another corpus.

Earlier objectives "under 30 s", "10–20 s" or "network +3–5 s" were conditional targets; they are not product guarantees. The pack's gain mainly came from the number of opens/allocations and the container overhead, not proof that all storage or native code was intrinsically slow. Cumulative allocations of several GB were not simultaneous RAM occupancy.

## Small Display: October 14, 2026

The new delay runs from clicking Display to unlocking after preparation and initial synchronization. **It is not directly comparable to September's transfer benchmark.** Over USB, the final campaign uses three new Desktop processes with Quest retained, versus five in the baseline; "cold" means a new Desktop process, not disk-cache clearing or a Quest restart.

| Cohort | Median opening / reopening |
| --- | ---: |
| Display baseline | 13,453 / 11,823 ms |
| Measured final implementation | 13,141 / 10,511 ms |

The atomic snapshot retained approximately **1,844 ms** median JSON time; observed Desktop pauses were **2,247 / 1,578 ms**. Elimination of all freezes is therefore not demonstrated. Process RSS, PSS and `dumpsys` graphics memory reports do not have the same scope and cannot be added together. Memory recorded at the end of a trial is neither a peak nor proof of no leak. Prolonged thermal and power measurements and large workloads remain distinct from a Small acceptance procedure.

## Fixtures and evidence

| Reference | What it reproduces |
| --- | --- |
| [MNI anatomy](../../../Validation/Quest/fixtures/mni-anatomy/anatomy-fixture.md) | One anatomical column, repository MNI references, hashes and deterministic archive. Small native-test GII files named MNI are synthetic and do not represent this full brain. |
| [MNI contacts](../../../Validation/Quest/fixtures/mni-contacts/contacts-fixture.md) | Eight synthetic contacts, two patients, deliberately repeated names and stable IDs; native mm coordinates then Unity X conversion. Eight is not a product limit. |
| [MNI iEEG](../../../Validation/Quest/fixtures/synthetic-ieeg-fixture.md) | Synthetic BrainVision prepared over 151 samples/100 Hz, with missing channels/blacklist and temporal behavior made explicit. |
| [Reference results](../../../Validation/Quest/retained-results.md) | Final comparisons, acceptances and context, organized by topic outside `Docs/dev`. Intermediate traces remain available in Git. |
| [Selection laboratory](selection-lab-guide.md) | Independent harness, six layouts and acquisition metrics; campaign limited to one subject, without general profile ranking. |
| [USB](usb-and-network-connectivity.md) | Dated Windows 11/Quest 3 qualification; macOS ARM64/Linux x64 and Quest 3S not covered by this evidence. |

Some historical logs/XML/APKs are only referenced under ignored `.artifacts`, `.test-results` or `Logs`; their absence from another clone does not authorize reconstructing their results from memory. Versioned manifests identify source, binaries, commands and scope when they exist.

For a new campaign, fix content, parameters, versions and hashes; distinguish process/disk cache, first opening/reopening, USB/Wi-Fi and advance/post-click time. Correlate local durations without subtracting two unsynchronized clocks. Retain errors, repetitions, dispersion, memory and frames; a first CPU frame is not a command-to-photon measurement. Operational procedures remain in the [validation skill](../../../.agents/skills/hibop-unity-validation/SKILL.md).
