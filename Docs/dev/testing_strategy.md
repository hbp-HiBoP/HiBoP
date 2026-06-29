# HiBoP Testing Strategy

## Scope

The first test milestone focuses on EditMode serialization and project archive safety:

- `ClassLoaderSaver` JSON round trips and legacy `$type` resolution.
- Core data objects used by projects: tags, filters, protocols, datasets, data containers, visualizations and 3D configuration data.
- `.hibop` archive structure, save, load and re-save.
- Lightweight Unity asset integrity checks for project prefabs, the main scene and required TextMesh Pro assets.

PlayMode and full 3D behavior tests are intentionally deferred to the next milestone.

## Layout

```text
Assets/Tests/EditMode/HBP.Serialization.Tests/
Assets/Tests/Fixtures/Serialization/
Assets/Tests/Fixtures/Projects/
```

`HBP.Serialization.Tests` is an EditMode-only Unity Test Framework assembly. It references runtime assemblies directly and avoids UI references unless a test genuinely needs them.

## Fixture Rules

Fixtures must be synthetic and anonymized:

- no real patient, center, study, protocol or project names;
- no real filesystem paths copied from existing projects;
- stable IDs and names such as `synthetic-...` or `legacy-...`;
- small files only.

Legacy JSON fixtures should preserve old `$type` shapes exactly enough to exercise compatibility. Binary `.hibop` fixtures are avoided in the first milestone; tests generate archives at runtime from synthetic objects so fixture contents remain readable.

## Running Tests

From PowerShell, use the repository wrapper:

```powershell
pwsh Docs/dev/run_unity_editmode_tests.ps1
```

For more filters and CI-oriented commands, see `Docs/dev/unity_test_cli.md`.

The tests redirect HiBoP preference, tag, alias, database and extraction paths into temporary folders. They must not read or overwrite local user preferences.

## Next Milestones

1. Add PlayMode tests for Module3D scene/view/column configuration load-save behavior.
2. Add targeted UI workflow tests for project open/save and visualization setup.
3. Split fast EditMode and slower PlayMode suites for CI.
