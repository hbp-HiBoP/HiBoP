# Codex Notes For HiBoP

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

For this project, the expected Unity instance is named `HiBoP` and currently uses Unity `6000.5.1f1`.

### Running Tests

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
