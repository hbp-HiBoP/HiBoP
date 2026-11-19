# HiBoP development

Technical references for the `feature/xr-autonomous` checkout, consolidated on
November 2, 2026 from the available code, tests and evidence. Start with
[the architecture](architecture.md), then choose the relevant domain.

| Domain | Reference and topics covered |
|---|---|
| Loading and persistence | [Pipeline](loading/loading-and-persistence.md): archives, JSON, binding, availability, validation and concurrency |
| Scientific data | [Time series](loading/timeseries.md): epochs, statistics, grids, buffers and memory |
| Metadata | [Tags](loading/tags.md): import, types, enums, migration and recovery |
| Loading performance | [Measurements and reproduction](loading/performance.md), with configurations and comparison limits |
| Projection | [Computation and presentation](activity-projection.md), calibration, coverage and resource ownership |
| Geometry | [Native/Unity coordinate systems](hbp_core_coordinate_system.md), [meshes and cuts](mesh-generation-and-cuts.md), [inflation](surface-inflation.md) |
| Rendering | [URP and scientific rendering](rendering/scientific-rendering.md), [visual validation](rendering/rendering-validation.md) |
| Quest | [Architecture, transfer, interactions and deployment](quest-autonomous/README.md) |
| Synchronization | [Shared state and reconnection](quest-autonomous/sync/synchronization-architecture.md), [operations](quest-autonomous/sync/synchronized-operations.md), [protocol](quest-autonomous/sync/wire-protocol.md) |
| Validation | [Tests and evidence](testing.md), then the procedures specific to the domain |

## Reading the references

Implementation descriptions refer to the code checked during this consolidation.
Dated results qualify only their own sources, binaries, fixtures and devices.
The presence of a test does not establish that it has run recently; a numerical
comparison is neither visual validation nor qualification of every platform.
No Unity build or hardware trial was run for this documentation migration.

Limits are documented alongside the relevant contract: memory and grids in
time series, compatibility in persistence, transport and recovery in Quest/sync,
and scientific and hardware coverage in the validation references. Earlier audit
suggestions do not constitute an active backlog.

## Instructions, procedures and evidence

[AGENTS.md](../../AGENTS.md) holds the repository invariants;
[agent-knowledge](../agent-knowledge/INDEX.md) provides self-contained operational
conventions. Existing skills hold execution procedures, notably
[Unity validation](../../.agents/skills/hibop-unity-validation/SKILL.md) and
[UI editing](../../.agents/skills/hibop-ui-authoring/SKILL.md). These references
explain the subsystems and their technical decisions.

Geometric inflation references are grouped with their tools in
[Tools/InflationReference](../../Tools/InflationReference/reference-corpus.md).
Retained Quest results are grouped by topic in
[Validation/Quest](../../Validation/Quest/retained-results.md).
The [Quest fixtures](../../Validation/Quest/retained-results.md#reproducible-fixtures)
are grouped with validation; their guides describe their scope. Manifests may
identify large local artifacts that are not distributed in Git.
Paths and reports from an earlier campaign are provenance records,
not commands to rerun without checking the current tools.

Logs, completed plans and successive reports have been merged into these
references. Their original text remains available in Git history at its former
path; no parallel documentation archive directory is needed.
When behavior changes, update the reference that owns the contract and add
useful evidence, without recreating a task log in the general documentation.
