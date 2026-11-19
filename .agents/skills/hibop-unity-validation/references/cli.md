# When the editor is closed: use the official Unity CLI

Read the exact editor version from:

```text
ProjectSettings/ProjectVersion.txt
```

Resolve the matching Unity Hub editor rather than hardcoding a version in
instructions. On the current Windows setup the normal editor location is:

```text
C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe
```

Any command that starts `Unity.exe`, directly or through a launcher script, must
run outside the Codex sandbox with escalated execution.

Use PowerShell `Start-Process -Wait -PassThru` so Codex does not regain control
while Unity is still running. A typical pattern is:

```powershell
$Process = Start-Process `
    -FilePath $Unity `
    -ArgumentList $Args `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

exit $Process.ExitCode
```

For Unity Test Framework CLI runs:

- use `-batchmode`;
- do not add `-quit` with `-runTests`; the test runner exits Unity itself;
- pass multiple assembly names as one semicolon-separated value;
- use `-nographics` for EditMode when appropriate;
- be cautious with `-nographics` for PlayMode UI/3D tests that may require a real
  graphics device;
- prefer filtered PlayMode runs while debugging rather than the entire UI suite;
- wait for completion and inspect the exit code, log, and test XML.

Observed Unity Test Framework exit codes for this workflow are:

- `0`: passed;
- `2`: at least one test failed;
- `3`: run error;
- `4`: test platform not found.


## Licensing failure diagnosis

The current Codex sandbox can block Windows named-pipe IPC with
`Unity.Licensing.Client`. Typical secondary symptoms include refused licensing
connections, repeated timeouts, unknown package entitlements, or
`com.unity.editor.headless` errors.

Do not conclude that the Unity Personal license is invalid or incompatible from
those symptoms alone. Do not reactivate the license or remove `-batchmode` as the
first workaround.

First rerun the exact Unity command outside the sandbox with escalated execution.
Then inspect the Unity log. Healthy runs should connect successfully to the
Licensing Client, resolve a license group, and register packages without repeated
connection-loss messages.


## Sync tier runner

Use `Tools/run-sync-tests.ps1` for the established Fast, Loopback, SceneFocused
or Qualification selection; `-TestFilter` narrows it, while `-AllInAssemblies`
explicitly broadens beyond the category. The runner first executes
`Tools/check-assembly-dependencies.ps1`. With an open editor, `-PlanOnly` runs
that static preflight and returns assemblies/category/version/filter without
launching Unity; execute the selection through MCP. Do not delete a Unity lock
to force a CLI run. With the editor closed, the actual runner launches Unity
and therefore needs escalated execution. Inspect its fresh XML, log and summary;
missing/zero/skipped/inconclusive tests are not a fully passed tier.
