using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Transfer.Scene;
using NUnit.Framework;

namespace HBP.Tests.SceneTransfer
{
    public sealed class NativePreparationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        [Timeout(30000)]
        public async Task WorkerIsAwaitedBeforeCancellationReleasesPrivateResult(bool cancel)
        {
            using var cancellation = new CancellationTokenSource();
            using var finishWorker = new ManualResetEventSlim();
            var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int mainThread = Thread.CurrentThread.ManagedThreadId, workerThread = 0, released = 0;
            object owned = new();
            Task<object> work = NativePreparation.RunAsync(() =>
            {
                workerThread = Thread.CurrentThread.ManagedThreadId;
                started.TrySetResult(true);
                // Only the fake native worker blocks; the Unity loop keeps running.
                if (!finishWorker.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
                return owned;
            }, result =>
            {
                Assert.That(result, Is.SameAs(owned));
                released++;
            }, cancellation.Token).AsTask();
            try
            {
                await started.Task;
                if (cancel) cancellation.Cancel();
                await UniTask.NextFrame();
                Assert.That(work.IsCompleted, Is.False);
                Assert.That(released, Is.Zero);
                Assert.That(workerThread, Is.Not.EqualTo(mainThread));
            }
            finally
            {
                finishWorker.Set();
            }

            Exception failure = null;
            object actual = null;
            try
            {
                actual = await work;
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            Assert.That(Thread.CurrentThread.ManagedThreadId, Is.EqualTo(mainThread));
            if (cancel)
            {
                Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
                Assert.That(released, Is.EqualTo(1));
            }
            else
            {
                Assert.That(failure, Is.Null);
                Assert.That(actual, Is.SameAs(owned));
                Assert.That(released, Is.Zero);
            }
        }
    }
}
