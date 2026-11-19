# Runtime cost and shared state

Read for loading, I/O, memory, transfer,
scene state, or a UI change that changes shared behavior.
For serialization, tags or time-series cache semantics, also use
[Data and persistence](data-and-persistence.md).

## Avoid redundant work

Scope validation and loading to the requested data. For example, displaying one
patient's trial matrix should not validate every patient in the database.
Avoid a separate disk pass that duplicates the actual read when necessary checks
can be performed during that read. Preserve necessary validation and its result.
Historical timings are not current performance measurements. For time-series
loading, cache ownership and project parsing, use [Data and persistence](data-and-persistence.md).

Reuse data already available in memory before adding file reads, retained copies
or transfer payloads. For inflation specifically, the accepted choice is to use
the loaded surface's current/transformed coordinates rather than reread the GII
or transfer a second representation just to reconstruct it. Inspect the current
[Surface.InflateAsync implementation](../../Assets/Scripts/HBP/Core/DLL/Surface.cs).
This choice is scoped to inflation, not a rule for all coordinate transformations.

`MNIObjects` owns MNI resources, not eager loading of every atlas. Preparing a
multi-patient scene must not load individual patient anatomy inaccessible in that
scene; a separately opened single-patient scene owns its required preparation.
Scientific projection settings belong to `ActivityProjectionConfiguration` in the
3D prefab, independently of `DLLDebugManager` and logging switches.

## Desktop and Quest

Keep shared business behavior in shared APIs; adapt presentation and input to each
platform rather than duplicate the behavior in Quest presenters.
Inspect existing base classes and wrappers before extracting a new shared layer.
A forwarding layer needs a distinct responsibility, not just a new home for calls.
Place generic operations at their actual common scope, not in an anatomy/density/
Quest specialization; fix shared preparation or lifetime issues in that common
path without anticipating an unrelated global refactor.
Desktop and autonomous Quest are targets of the same Unity project. Quest runs
the common scientific model and local calculations rather than acting as a
remote video display. Retain complete useful data, not just a rendered snapshot.
Synchronization must not add scene polling or scene-sized work when no scene is
shared, or degrade ordinary Desktop interaction. This does not request a full
benchmark campaign for every change.

For pairing, full scene delivery, live mutations, resource readiness, session
settings, jobs, reconnection or scene lifecycle, read the matching section of
[Desktop–Quest synchronization](synchronization.md). It contains the current
contracts, including the distinction between transferred user data and installed
atlas/MNI references. Shared visibility and selection differ from local pose and
layout; do not infer synchronization solely from whether a value is visible.

For native lifetime, coordinates, projection and scientific rendering, use
[Scientific computing](scientific-computing.md). UI progress and interaction
presentation belong to [UI and UX](ui-and-ux.md).
