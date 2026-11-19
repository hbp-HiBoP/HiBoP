# Time series, grids and memory

Reference for the current code, checked on November 2, 2026. Earlier measurements
are isolated in [Performance](performance.md). Spatial contracts are in
[Projection](../activity-projection.md).

## From files to epochs

[DataManager](../../../Assets/Scripts/HBP/Core/Data/DataManager.cs) owns
loaded data and derivatives. For iEEG, CCEP and continuous MEG,
[RawRecordingCache](../../../Assets/Scripts/HBP/Core/Data/Loaded/RawRecordingCache.cs)
shares raw loading by source key using `ConcurrentDictionary` and
`Lazy<DynamicData>`. An error removes the faulty entry. The key includes format,
paths, sizes and modification times of associated files (including
files referenced by a BrainVision header). It does not include the protocol.
EEG/epoch construction happens outside the DataManager publication lock;
this does not describe all other loaders, notably fMRI/static data.

[EpochedData](../../../Assets/Scripts/HBP/Core/Data/Loaded/EpochedData.cs) prepares
all protocol blocs: Trial Matrix and bloc switching need them.
[SubTrial](../../../Assets/Scripts/HBP/Core/Data/Loaded/SubTrial.cs) copies a
compact range per channel covering the contiguous union of window/baseline,
then exposes `EpochView` instances. Both ranges share this storage, even when
they overlap. This is no longer the earlier proposal for views necessarily
retaining the entire raw recording. The cache keeps only the last unpinned raw
recording after an epoched load; epochs are self-contained. A compatible
reopening can reuse caches, but no universal absence of disk rereads
is guaranteed.

[Descriptors](../../../Assets/Scripts/HBP/Core/Data/Loaded/EpochDescriptor.cs)
retain trials, sub-blocs, events and inclusive bounds. For a window,
the start is rounded to the first admissible sample (`ceil`) and the end to
the last (`floor`). Successive sub-blocs are juxtaposed. Aligned sub-blocs
with different durations use the longest segment and their
`Before`/`After` event-relative alignment.

## Processing and statistics

Processing follows protocol order with reused working buffers.
Parameters and derivatives depend on the protocol and normalization;
compact epoch storage cannot have the protocol-independent lifetime
of a complete recording.

[StreamingStatistics](../../../Assets/Scripts/HBP/Core/Data/Loaded/StreamingStatistics.cs)
uses double-precision Welford accumulation, then produces floats:
sample standard deviation `sqrt(M2/(n-1))`, zero for `n <= 1`, and
`SEM = standard deviation / sqrt(n)`, zero for no observations. The median
leases a single cross-trial buffer, returned in `finally`. It retains SEM
calculation over observations without building a temporary samples × trials
matrix. Analytical tests use, among other tolerances, `1e-6`, and full
normalization paths `1e-4`; these are not universal tolerances for every computation.

`NormalizationType.Auto` on a `DataInfo` follows the user preference;
`Auto` as a preference is reduced to `None`. Aggregation changes
invalidate the corresponding statistics. Preference-driven reloading
preserves compatible sources and temporarily suspends eviction during
reconstruction, then restores the budget, including on failure.

## Two temporal grids

`Timeline` navigation uses the maximum frequency of displayed columns.
Each column retains a `ProjectionTimeline` at its maximum internal frequency.
Thus `[0; 1500 ms]` contains 97 positions at 64 Hz and 3073 at 2048 Hz: two
columns retain their own grids; a mixed column uses 2048 Hz.

[Column3DDynamic](../../../Assets/Scripts/HBP/Data/Module3D/Column3DDynamic.cs)
reads `EEGPreferences.TemporalSampling`: `Floor`, `Round` (`Mathf.RoundToInt`) or
`Interpolate` by default. Mapping uses physical time, including shifted origins,
negative times and noncommensurable frequencies. Sites and implantation
evaluate `TemporalSample.Evaluate`, and therefore interpolate between values
when requested. Surface and cut textures use only
`CurrentProjectionSample.Index`: they do not combine the two time points using
its temporal alpha. `ActivityAlpha` passed to the generator controls opacity,
not temporal interpolation. A configuration's historical
`Temporal Sampling` field is ignored without preference migration.
NIfTI export follows the projection grid exactly.

Interpolation is an explicit reconstruction between measurements, not a new
scientific measurement. No automatic temporal downsampling is used
to fit data within the budget. Exact native precomputation remains
proportional to points × time points; block caching/compression/GPU processing
are not active strategies to infer from earlier plans.

## Preparation and presentation

[ProjectionBufferBuilder](../../../Assets/Scripts/HBP/Core/Data/Loaded/ProjectionBufferBuilder.cs)
fills the contiguous `time × sites` array and computes statistics for
unmasked sites. Prepared per-site values serve tooltips; the flattened matrix
is a separate copy required by native code. Zero series are shared per column.
Limits and histograms do not require a persistent concatenation of all
unmasked values.

[TrialMatrixTileBuilder](../../../Assets/Scripts/HBP/Core/Data/Loaded/TrialMatrixTileBuilder.cs)
tiles both axes according to the maximum texture size. Bilinear smoothing
uses a one-pixel halo cropped through UVs, with global coordinates and events.
`Color32` and `SetPixelData` avoid the `Color[]` array; textures are reused at
equal sizes and released on destruction. All tiles are materialized:
there is no virtualization limited to visible tiles.
The reference comparison allows one RGBA32 level out of 255 per channel due
to rounding, without scientific decimation.

[CurveData](../../../Assets/Scripts/HBP/Data/Informations/Graph/CurveData.cs)
represents a regular series by ordinates, origin and step. `GetPoint` reconstructs
the abscissa; `Points` can materialize a compatibility copy. Irregular curves
remain explicit. The renderer prepares the viewport and reuses its buffers;
CSV/SVG exports traverse all points, independently of the renderer's
visual reduction.

## Budget and limits

[MemoryCacheBudget](../../../Assets/Scripts/HBP/Core/Data/Loaded/MemoryCacheBudget.cs)
interprets `MemoryCacheLimit` in MiB. Zero selects
`min(floor(0.9 × RAM_MiB), max(0, RAM_MiB - 2048))`; unknown RAM gives an
unbounded limit. Eviction targets inactive entries, in the order managed
derivatives, native projections, textures, raw recordings, then LRU within each
category. Active entries remain pinned; overruns are reported without altering
their values. This budget measures buffers known to HiBoP, not all process,
driver or Unity RAM. Normal cleanup does not force GC.

Very long exact grids and all active tiles can exceed the budget.
GPU profiles, memory plateaus and visual acceptance on product data remain
separate checks from numerical tests. The earlier memory-gain estimate
is not a measurement of the current checkout.

## Verifying a change

Tests under
[HBP.Serialization.Tests](../../../Assets/Tests/EditMode/HBP.Serialization.Tests)
include `RawRecordingCacheTests`, `EpochIndexTests`, `DataLoadingProcessingCacheTests`
and `Stage3StreamingStatisticsTests` through `Stage8CurveDataTests`. Retain these
historical names to locate tests; they are not a documentation timeline.
They cover caching, bounds, value identity, temporal behavior, buffers and budget.
Depending on the change, supplement them with graph/Trial Matrix UI scenarios
and [performance](performance.md) measurements. No Unity tests were run
for this documentation migration.
