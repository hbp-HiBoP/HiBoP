# Codex Notes For HiBoP

## C# Formatting

Before reviewing or handing off C# changes, run:

```powershell
.\Tools\format-code.cmd
```

By default, this formats only staged, unstaged, and untracked C# files. Use
`-Base origin/develop` to include committed branch changes, or `-All` to format
all C# files under `Assets`.

## Prefab-First GameObject Workflow

When adding or changing UI elements or other GameObjects, edit or create the
appropriate prefab and serialize the required references there. Runtime
`new GameObject(...)` construction should be exceptional and used only when the
object is inherently dynamic and a prefab is not appropriate. Do not use
runtime object creation to compensate for a missing element or reference in a
prefab.

## Unity MCP Workflow

This Unity project is expected to be driven through MCP for Unity when Unity is open.

Before using Unity tools, discover the Unity MCP tools if they are not already visible:

```text
tool_search: unity MCP run_tests get_test_job editor state instances
```

The expected MCP server is:

```toml
[mcp_servers.unityMCP]
url = "http://127.0.0.1:8080/mcp"
```

If the Unity MCP package/configuration was just added, Codex may need to be restarted before the `mcp__unityMCP` tools appear.

### Resource-First Checks

Always read Unity resources before acting:

```text
mcpforunity://custom-tools
mcpforunity://instances
mcpforunity://editor/state
mcpforunity://project/info
```

If multiple Unity instances are connected, use:

```text
set_active_instance(instance="HiBoP@...")
```

For this project, the expected Unity instance is named `HiBoP` and currently uses Unity `6000.5.2f1`.

### Running Tests With MCP

Use Unity MCP instead of launching Unity batchmode when the editor is open.

Typical EditMode serialization run:

```text
run_tests(
  mode="EditMode",
  assembly_names=["HBP.Serialization.Tests"],
  include_failed_tests=true,
  include_details=false,
  init_timeout=30000
)
```

Then poll:

```text
get_test_job(
  job_id="<job_id>",
  wait_timeout=60,
  include_failed_tests=true,
  include_details=false
)
```

### Async Test Safety

In Unity tests, be extremely suspicious of anything that turns async work into a
synchronous wait. UniTask operations such as `Yield`, `NextFrame`, `Delay`,
`DelayFrame`, `WaitUntil`, `SwitchToMainThread`, `SwitchToSynchronizationContext`,
`ToUniTask`, and UnityWebRequest async flows often need the Unity PlayerLoop to
continue running. If the test blocks the main thread, those continuations may
never resume and the Unity Editor can freeze.

Never wrap UniTask or Unity async code in blocking NUnit async assertions:

```csharp
Assert.ThrowsAsync<T>(...);
Assert.CatchAsync<T>(...);
Assert.DoesNotThrowAsync(...);
```

Also avoid sync assertions with async lambdas, which can create false positives,
miss exceptions, or let exceptions escape after the test has already completed:

```csharp
Assert.Throws<T>(async () => await SomeUniTask());
Assert.Catch(async () => await SomeUniTask());
Assert.DoesNotThrow(async () => await SomeUniTask());
Assert.That(async () => await SomeUniTask(), Throws.TypeOf<T>());
```

Do not block on `Task` or `UniTask` from Unity tests:

```csharp
task.Wait();
task.Result;
task.GetAwaiter().GetResult();
uniTask.AsTask().Wait();
uniTask.AsTask().Result;
uniTask.GetAwaiter().GetResult();
Task.WaitAll(...);
Task.WaitAny(...);
Task.Run(...).Wait();
```

Do not busy-wait or sleep the main thread while async Unity work is expected to
progress:

```csharp
while (!done) { }
Thread.Sleep(...);
manualResetEvent.WaitOne();
```

Avoid fire-and-forget async in tests:

```csharp
async void SomeTestOrHelper() { ... }
async UniTaskVoid SomeTestOrHelper() { ... }
SomeUniTask().Forget();
```

All tests that touch UniTask or Unity async APIs should be `async Task` tests and
should `await` the code directly. For expected exceptions, use an explicitly
awaited `try/catch` helper instead of NUnit async exception assertions:

```csharp
private static async Task<Exception> CaptureExceptionAsync(Func<Task> action)
{
    try
    {
        await action();
        return null;
    }
    catch (Exception exception)
    {
        return exception;
    }
}
```

Use timeouts only as guardrails around already non-blocking tests, not as a way
to make blocking patterns acceptable.

Before and after the run, check the console:

```text
read_console(action="get", types=["error"], count="10", format="detailed", include_stacktrace=true)
```

### Preferred Tools

Prefer MCP for Unity when the editor is open:

```text
mcp__unityMCP.run_tests
mcp__unityMCP.get_test_job
mcp__unityMCP.read_console
```

Batchmode Unity test scripts are only a fallback for CI or closed-editor runs, not the preferred local workflow.

## Unity CLI Workflow When The Editor Is Closed

When Unity is not open, run tests through the official Unity CLI instead of MCP.
Read the project version from `ProjectSettings/ProjectVersion.txt` and use the
matching Unity Hub editor, currently:

```powershell
C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe
```

### Mandatory Sandbox Rule

Any command that starts `Unity.exe`, either directly or through a PowerShell
launcher, must run outside the Codex sandbox with escalated execution. The Unity
Personal license itself is valid in CLI and `-batchmode` is supported.

This was verified on 2026-07-17: inside the sandbox, Windows named-pipe IPC with
`Unity.Licensing.Client` was blocked, causing refused connections, repeated
60-second timeouts, unknown package entitlements, and secondary
`com.unity.editor.headless` messages. The same CLI command outside the sandbox
initialized the Personal license and completed successfully.

Do not reactivate the license, remove `-batchmode`, or conclude that Unity
Personal is incompatible based on those symptoms. First rerun the exact command
outside the sandbox. A healthy log contains
`Successfully connected to LicensingClient`, resolves the license group, and
registers packages without repeated connection-loss messages.

Use `Start-Process -Wait -PassThru` from PowerShell. A direct call with `&` can
return control to Codex while the Unity process is still running in the
background, which makes the test result look complete too early.

Typical closed-editor EditMode run:

```powershell
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe"
$ResultRoot = "C:\HBP\Software\HiBoP\.test-results\unity-cli"
New-Item -ItemType Directory -Force -Path $ResultRoot | Out-Null

$Args = @(
  "-batchmode",
  "-nographics",
  "-accept-apiupdate",
  "-projectPath", "C:\HBP\Software\HiBoP",
  "-runTests",
  "-testPlatform", "EditMode",
  "-assemblyNames", "HBP.Serialization.Tests;HBP.ProjectWorkflow.Tests",
  "-testResults", (Join-Path $ResultRoot "editmode-results.xml"),
  "-logFile", (Join-Path $ResultRoot "editmode.log"),
  "-forgetProjectPath"
)

$Process = Start-Process -FilePath $Unity -ArgumentList $Args -Wait -PassThru -NoNewWindow
exit $Process.ExitCode
```

Important CLI details:

- Do not pass `-quit` with `-runTests`; Unity Test Framework exits the editor
  itself when the run completes.
- Multiple test assemblies are passed as one semicolon-separated value, for
  example `HBP.Serialization.Tests;HBP.ProjectWorkflow.Tests`.
- Unity Test Framework exit codes observed here: `0` means passed, `2` means at
  least one test failed, `3` means run error, and `4` means test platform not
  found.
- The first CLI run after a Unity version change can spend about a minute on
  asset import and script compilation before tests start.
- Use `-nographics` for EditMode. Be careful with `-nographics` for PlayMode
  UI or 3D tests because those tests may need a real graphics device.
- Closed-editor PlayMode runs are possible, but the full HiBoP PlayMode UI set
  can create very large logs/XML when UI tests time out. Prefer filtered
  PlayMode runs while debugging.

## XR Development

For Quest deployment/debugging, use Tools/Connect-QuestAdbWifi.ps1 to establish ADB connectivity. Do not manually configure ADB unless the script reports an error.