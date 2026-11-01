# C# conventions

Read for copy/clone semantics,
production async APIs, or changes to model/entity identity.

## Copy and Clone

In HiBoP, `Copy(source)` fills the receiving instance from another instance.
`Clone()` creates an instance following the type's established cloning contract;
use `ICloneable` where appropriate to that family. Inspect nearby implementations
before naming a new operation. Neither name alone decides deep/shallow ownership
or whether an ID changes. Do not alter identity semantics incidentally.
See [BaseData](../../Assets/Scripts/HBP/Core/Data/BaseData.cs).

## Production async

Prefer UniTask for production Unity async paths where practical. Avoid converting
UniTask to Task and back without a boundary that needs it. An API requiring Task
is a legitimate boundary, not a reason for a project-wide conversion.
This preference does not change the `async Task` convention for NUnit tests in
AGENTS and the validation skill's async-test procedure.

## Identity

Use established stable IDs and their interfaces to identify entities, rather than
collection positions. Correct unintended ID changes instead of inventing index
identities to compensate. For synchronized entities, the authoritative detailed
rules are in the [state contract](../dev/quest-autonomous/sync/02-state-contract.md).
Read its relevant identity section when changing serialization or synchronization
identity; this entry does not redefine wire schemas or assign new IDs to clones.
