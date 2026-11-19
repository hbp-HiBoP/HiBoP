# C# conventions

Read for copy/clone semantics, events, namespaces,
production async APIs, or changes to model/entity identity.

## Namespaces and serialized models

Keep the namespace's layer prefix aligned with its HBP folder (`HBP.Core`,
`HBP.Data`, `HBP.UI`, etc.); this does not require one namespace per subfolder.
Before renaming a serialized type/member or moving its assembly, read
[Data and persistence](data-and-persistence.md) for legacy JSON and IL2CPP.
For data constructors in the existing configuration families, pass the fields
through the constructor and keep `ID` last, following neighboring types.

## Events

For ordinary HiBoP Unity-side notifications, use `UnityEvent` without arguments
and the project's `GenericEvent<T...>` with arguments; name notifications `On...`.
Prefer direct subscription/invocation over forwarding layers with no additional
responsibility. Preserve legitimate callbacks and return-valued delegates.
Transport/background-thread events and assemblies with `noEngineReferences`
may retain C# events; do not add Unity dependencies to apply this convention.
Static events are appropriate for truly global owners, not a default for each
entity. [Plane](../../Assets/Scripts/HBP/Core/DLL/Plane.cs) illustrates a lazy
instance event without per-entity overhead until observed.

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
Established deliberately detached production entries may use `UniTaskVoid`
invoked with `.Forget()`; preserve their error/lifetime ownership. Operations
whose callers need completion remain awaitable. Do not apply the test-only ban
on detached execution as a blanket production rewrite.
This preference does not change the `async Task` convention for NUnit tests in
AGENTS and the validation skill's async-test procedure.
After worker-side loading/calculation, return to the main thread before Unity
operations that require it (`await UniTask.SwitchToMainThread()` when necessary).

## Identity

Use established stable IDs and their interfaces to identify entities, rather than
collection positions. Correct unintended ID changes instead of inventing index
identities to compensate. `BaseData.Equals` and `GetHashCode` express logical
identity by ID; do not substitute reference equality incidentally in that family.
New `BaseData` objects materialize an ID lazily and thread-safely on first access
or serialization; do not create a temporary GUID before reading an existing JSON
ID. The getter generates only for `null`: an explicit business assignment `ID = ""`
must remain visible as invalid data. Deserialization repairs missing/empty legacy
IDs separately. `GenerateID()` remains the explicit replacement operation; it is
not an incidental effect of loading or copying an already identified object.
For synchronized entities, the authoritative detailed
rules are in [Synchronization](synchronization.md#full-scene-delivery-and-resource-identity).
Read its relevant identity section when changing serialization or synchronization
identity; this entry does not redefine wire schemas or assign new IDs to clones.
