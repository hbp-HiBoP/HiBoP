using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.UI.Tools;
using NUnit.Framework;

namespace HBP.Tests.PlayMode.UI
{
    public sealed class DelayedLoadingTests
    {
        [TestCase(false)]
        [TestCase(true)]
        [Timeout(30000)]
        public async Task DelayedOperationIsStartedOnceAndDrainedAfterTheDelay(bool cancel)
        {
            using var cancellation = new CancellationTokenSource();
            var release = new UniTaskCompletionSource();
            int starts = 0;
            Task loading = LoadingManager.LoadDelayedAsync(async (_, token) =>
            {
                starts++;
                await release.Task;
                token.ThrowIfCancellationRequested();
            }, cancellation.Token, delayMilliseconds: 0).AsTask();
            bool completedBeforeRelease;
            try
            {
                await UniTask.NextFrame();
                if (cancel) cancellation.Cancel();
                completedBeforeRelease = loading.IsCompleted;
            }
            finally
            {
                release.TrySetResult();
            }

            Exception failure = null;
            try
            {
                await loading;
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            Assert.That(completedBeforeRelease, Is.False, "The loading operation must retain its inputs until the worker finishes.");
            if (cancel) Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            else Assert.That(failure, Is.Null);
            Assert.That(starts, Is.EqualTo(1));
        }

        [Test, Timeout(30000)]
        public async Task OperationFinishingBeforeTheDelayIsStillAwaitedSuccessfully()
        {
            int starts = 0;
            await LoadingManager.LoadDelayedAsync(async (_, _) =>
            {
                starts++;
                await UniTask.Yield();
            }, CancellationToken.None, delayMilliseconds: 2000);
            Assert.That(starts, Is.EqualTo(1));
        }
    }
}
