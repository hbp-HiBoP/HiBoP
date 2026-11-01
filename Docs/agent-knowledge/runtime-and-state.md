# Runtime cost and shared state

Read for loading, I/O, memory, transfer,
scene state, or a UI change that changes shared behavior.

## Avoid redundant work

Scope validation and loading to the requested data. For example, displaying one
patient's trial matrix should not validate every patient in the database.
Avoid a separate disk pass that duplicates the actual read when necessary checks
can be performed during that read. Preserve necessary validation and its result.
The [loading documentation](../dev/loading/README.md) provides subsystem context;
historical timings are not current performance measurements.

Reuse data already available in memory before adding file reads, retained copies
or transfer payloads. For inflation specifically, the accepted choice is to use
the loaded surface's current/transformed coordinates rather than reread the GII
or transfer a second representation just to reconstruct it. Inspect the current
[Surface.InflateAsync implementation](../../Assets/Scripts/HBP/Core/DLL/Surface.cs).
This choice is scoped to inflation, not a rule for all coordinate transformations.

## Desktop and Quest

Keep shared business behavior in shared APIs; adapt presentation and input to each
platform rather than duplicate the behavior in Quest presenters.
Synchronization must not add scene polling or scene-sized work when no scene is
shared, or degrade ordinary Desktop interaction. This does not request a full
benchmark campaign for every change.

Shared intent and local presentation have different ownership. The current
[requirements and boundaries](../dev/quest-autonomous/sync/01-requirements-and-boundaries.md)
own that distinction (including R10 and the shared/local section). Read them when
adding synchronized state: pose is an example of local state, not its only member.
Use the [operation matrix](../dev/quest-autonomous/sync/operation-matrix.md) for the
affected operation; do not infer that every visible value is synchronized.
