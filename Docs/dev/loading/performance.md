# Performance: protocols and historical measurements

These figures document decisions, not the performance of the current checkout.
No Unity measurements were run during the November 2, 2026 consolidation.
Product data and earlier local outputs are not redistributable fixtures;
the series below are not interchangeable.

## Metadata: index before changing format

Campaign of **July 23, 2026**, Unity 6000.5.2f1 Editor Mono Windows,
concurrency 20: database of 240 patients / 370918 tag values, project of
218 patients / 340783 values. Before indexing, approximately 71.4 million
comparisons and 742076 `AllTags` reconstructions were estimated for the
database. Only one database baseline and two project openings precede
the three warm runs after optimization: the differences are not statistical
intervals.

| Wall time | Database | Project |
|---|---:|---:|
| Before indexing | 9992 ms | 8134 ms (interpolated median of two runs) |
| Stable tag index | 5101 ms | 4926 ms |
| Lazy IDs | 4153 ms | 4805 ms |
| Separate file validation (six warm runs) | 4787 ms | 5166 ms |

Separating validation increased wall time (+15.3% / +7.5%), despite only
32/19 ms of explicit validation. It is retained for architecture and
cancellation, not as a standalone gain. Scheduling/I/O variability
prevents attributing every increase to probes.

Campaign of **July 24**, different computer and data: `Default` database
with 419 patients / 577632 tag values / 3561 DataInfo, `full_test` project
with 245 patients, archive 6912292 bytes, patient JSON ~39029233 bytes.
Windows Editor Mono 6000.5.2f1 runtime. Compare only within this series.

| Change | Database, warm median | Project, warm median |
|---|---:|---:|
| Binding context | 10125.6 → 9614.3 ms | 4379.2 → 3628.9 ms |
| Streamed indented JSON | 9614.3 → 8625.6 ms | 3628.9 → 2929.8 ms |
| Direct ZIP | Not applicable | 2929.8 → 1792.5 ms |

The pre-binding reference contains one run; subsequent results and comparisons
use three warm runs. Streaming reduces session GCs from 55 to 32 for the
database and 8 to 6 for the project, without shrinking JSON.
Direct ZIP reduces the archive phase from 1082.9 to 105.2 ms.
Streamed `Read`/`Deserialize` phases are nested; they do not add together.
Cumulative worker times, global CPU/GC observed by each scope and memory
deltas provide neither wall time nor a real memory peak.

## Scheduler

Campaign of **July 28**, Editor Mono 6000.5.2f1, 20 logical processors,
`Default` and `visu_full_test` (not `full_test`). One discarded warm-up, three
repetitions per combination, for 30 warm measurements.

| Workers | Database Ready / Validated | Project Ready / Validated |
|---:|---:|---:|
| 1 | 4.09 / 124.31 s | 4.18 / 113.15 s |
| 2 | 3.69 / 64.01 s | 3.49 / 58.17 s |
| 4 | 2.98 / 24.72 s | 2.29 / 16.04 s |
| 8 | 3.02 / 10.82 s | 2.74 / 9.69 s |
| 20 | 4.72 / 11.30 s | 4.01 / 9.80 s |

Twenty workers did not improve Validated and degraded Ready/CPU. This motivates
the 8/4/2 caps and shared budget of the [current pipeline](loading-and-persistence.md).
The native cap of 2 is conservative: this campaign did not cover a representative
native corpus. Temporary loading instrumentation was removed;
its old commands must not be advertised as available.
For comparisons: fix data/build/machine, separate cold/warm runs, measure Ready,
targeted validation and visible wait, CPU, allocations and actual peak; do not
reuse full-validation times as a startup baseline.

## Synthetic projection benchmark still available

Entry points under
[HBP.Serialization.Tests](../../../Assets/Tests/EditMode/HBP.Serialization.Tests):
`NativeProjectionLoadBenchmarkCli`, `NativeProjectionLoadBenchmarkScenarios` and
`NativeProjectionLoadBenchmarkModel`. Deterministic values are calculated from
patient/channel/trial/index, without adding a large recording to the repository.

Profiles: short `Smoke`; `Product` (alias `Typical`) with 30000 sites × 100
time points and a three-column variant; `Extreme` including 97/3073 positions over
1500 ms at 64/2048 Hz. The CLI accepts, among other arguments:

```text
-executeMethod HBP.Tests.Serialization.NativeProjectionLoadBenchmarkCli.Run
-hbpProjectionOutput <report.json>
-hbpProjectionProfile Smoke
-hbpProjectionTimeline 100
-hbpProjectionRepetitions 10
-hbpProjectionWorkers 0
-hbpProjectionBatchSites 0
```

`-hbpProjectionFilter` selects a scenario. The CLI also provides surface,
volume, volumetric interpolation and export inclusion options: inspect its
arguments before comparing. It exits Unity through `EditorApplication.Exit`;
do not add `-quit`. Use the version from `ProjectVersion.txt` and the
[Unity procedure](../../../.agents/skills/hibop-unity-validation/SKILL.md), without
starting a second Editor when HiBoP is already open.

Reference from **July 21, 2026**, Unity 6000.5.2f1, Windows 11, 16 logical
processors, ten repetitions, former projection geometry:

| Scenario | Checksum | Total P50 / P95 | Computation P50 / P95 | Cut P50 / P95 |
|---|---|---:|---:|---:|
| Smoke 1000 sites × 100 | `F72D0F4CEE55D5F3` | 200.22 / 224.07 ms | 58.72 / 62.70 ms | 0.227 / 0.270 ms |
| Product 30000 sites × 100 | `2FDA5CD13773B845` | 4949.91 / 5806.37 ms | 4828.66 / 5568.66 ms | 0.193 / 0.370 ms |

Product: 250 patients × 120 channels, 100 trials, 2001-sample windows,
501-sample baselines; 422704 points and 42270400 projected values. Actually
observed memory: column array 12000000 bytes, native projection 170772416,
estimated texture 109068, private peak 191606784, maximum retained private memory 6475776.
Smoke: 400000, 31602496, 109068, 40972288 and 4222976 bytes respectively.
Product raw/epoch/derivative sizes (24.012 / 30.024 / 24.012 billion bytes)
were **logical estimates not allocated** by this benchmark.
One checksum per scenario and a return to zero private delta over the next nine
repetitions were reported; this does not prove a plateau under every product workload.

Native grid and storage changes make these checksums unsuitable as a universal
current oracle. For a new measurement, retain source and plugin versions,
OS, runtime, CPU, data, parameters, memory sampling frequency and full report;
check scientific equivalence, latency and memory separately.
Theoretical gains from earlier plans are not results.
