# Unity async test safety

Unity and UniTask continuations often require the PlayerLoop to keep running.
Never turn that work into a synchronous wait on the main thread.

Avoid all equivalent forms of:

```csharp
task.Wait();
var value = task.Result;
task.GetAwaiter().GetResult();
uniTask.AsTask().Wait();
uniTask.GetAwaiter().GetResult();
Thread.Sleep(...);
while (!done) { }
manualResetEvent.WaitOne();
```

Do not use fire-and-forget async in tests when completion matters:

```csharp
async void SomeTestOrHelper() { ... }
async UniTaskVoid SomeTestOrHelper() { ... }
SomeUniTask().Forget();
```

Tests that touch Unity async APIs or UniTask should normally be `async Task` tests
and directly `await` the operation.

Be cautious with NUnit async exception assertions such as `Assert.ThrowsAsync`,
`Assert.CatchAsync`, or `Assert.DoesNotThrowAsync` around Unity/UniTask work. They
can introduce blocking behavior. For expected exceptions, prefer an explicitly
awaited `try/catch`, for example:

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

Timeouts are guardrails around already non-blocking tests, not a way to make
blocking patterns safe.

