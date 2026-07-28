using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HBP.Tests.Serialization
{
    public class LoadingWorkSchedulerTests
    {
        [Test]
        public void Policy_DisabledMultithreadingUsesOneWorker()
        {
            LoadingConcurrencyPolicy policy = new(20, false, 20);

            foreach (LoadingWorkCategory category in
                Enum.GetValues(typeof(LoadingWorkCategory)))
            {
                Assert.That(policy.GetLimit(category), Is.EqualTo(1));
            }
        }

        [Test]
        public void Policy_OverrideAppliesToEveryCategory()
        {
            LoadingConcurrencyPolicy policy = new(20, true, 4);

            foreach (LoadingWorkCategory category in
                Enum.GetValues(typeof(LoadingWorkCategory)))
            {
                Assert.That(policy.GetLimit(category), Is.EqualTo(4));
            }
            Assert.That(policy.GlobalLimit, Is.EqualTo(4));
        }

        [Test]
        public void Policy_BackgroundValidationCanBeDisabled()
        {
            string name =
                LoadingConcurrencyPolicy
                    .BackgroundValidationEnvironmentVariable;
            string previous = Environment.GetEnvironmentVariable(name);
            try
            {
                Environment.SetEnvironmentVariable(name, "false");
                Assert.That(
                    LoadingConcurrencyPolicy.BackgroundValidationEnabled,
                    Is.False);

                Environment.SetEnvironmentVariable(name, null);
                Assert.That(
                    LoadingConcurrencyPolicy.BackgroundValidationEnabled,
                    Is.True);
            }
            finally
            {
                Environment.SetEnvironmentVariable(name, previous);
            }
        }

        [Test]
        public async Task RunAsync_PreservesInputOrder()
        {
            LoadingWorkScheduler scheduler = new(
                new LoadingConcurrencyPolicy(20, true, 4));
            TaskCompletionSource<bool>[] gates = Enumerable.Range(0, 4)
                .Select(_ => new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously))
                .ToArray();
            Func<UniTask<int>>[] tasks = Enumerable.Range(0, 4)
                .Select(index => (Func<UniTask<int>>)(async () =>
                {
                    await gates[index].Task;
                    return index;
                }))
                .ToArray();

            UniTask<int[]> run = scheduler.RunAsync(
                tasks,
                LoadingWorkCategory.JsonAndZip,
                () => LoadingWorkPriority.Foreground,
                CancellationToken.None);
            gates[3].SetResult(true);
            gates[1].SetResult(true);
            gates[2].SetResult(true);
            gates[0].SetResult(true);

            Assert.That(await run, Is.EqualTo(new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public async Task RunAsync_SharesGlobalBudgetAcrossCategories()
        {
            LoadingWorkScheduler scheduler = new(
                new LoadingConcurrencyPolicy(20, true, 2));
            int active = 0;
            int maximumActive = 0;
            TaskCompletionSource<bool> release =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            Func<UniTask<int>> CreateTask()
            {
                return async () =>
                {
                    int current = Interlocked.Increment(ref active);
                    SetMaximum(ref maximumActive, current);
                    await release.Task;
                    Interlocked.Decrement(ref active);
                    return 1;
                };
            }

            UniTask<int[]> json = scheduler.RunAsync(
                new[] { CreateTask(), CreateTask() },
                LoadingWorkCategory.JsonAndZip,
                () => LoadingWorkPriority.Background,
                CancellationToken.None);
            UniTask<int[]> files = scheduler.RunAsync(
                new[] { CreateTask(), CreateTask() },
                LoadingWorkCategory.FileSystem,
                () => LoadingWorkPriority.Background,
                CancellationToken.None);

            await UniTask.Yield();
            release.SetResult(true);
            await json;
            await files;

            Assert.That(maximumActive, Is.EqualTo(2));
        }

        [Test]
        public async Task RunAsync_ForegroundWaiterRunsBeforeQueuedBackground()
        {
            LoadingWorkScheduler scheduler = new(
                new LoadingConcurrencyPolicy(1, true, 1));
            TaskCompletionSource<bool> firstStarted =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> releaseFirst =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            List<string> order = new();

            UniTask<string[]> first = scheduler.RunAsync(
                new[]
                {
                    (Func<UniTask<string>>)(async () =>
                    {
                        firstStarted.SetResult(true);
                        await releaseFirst.Task;
                        lock (order)
                        {
                            order.Add("first");
                        }
                        return "first";
                    })
                },
                LoadingWorkCategory.JsonAndZip,
                () => LoadingWorkPriority.Background,
                CancellationToken.None);
            await firstStarted.Task;

            UniTask<string[]> background = scheduler.RunAsync(
                new[]
                {
                    (Func<UniTask<string>>)(() =>
                    {
                        lock (order)
                        {
                            order.Add("background");
                        }
                        return UniTask.FromResult("background");
                    })
                },
                LoadingWorkCategory.JsonAndZip,
                () => LoadingWorkPriority.Background,
                CancellationToken.None);
            UniTask<string[]> foreground = scheduler.RunAsync(
                new[]
                {
                    (Func<UniTask<string>>)(() =>
                    {
                        lock (order)
                        {
                            order.Add("foreground");
                        }
                        return UniTask.FromResult("foreground");
                    })
                },
                LoadingWorkCategory.JsonAndZip,
                () => LoadingWorkPriority.Foreground,
                CancellationToken.None);

            releaseFirst.SetResult(true);
            await first;
            await foreground;
            await background;

            Assert.That(
                order,
                Is.EqualTo(new[] { "first", "foreground", "background" }));
        }

        [Test]
        public async Task RunAsync_ReevaluatesPriorityWhileQueued()
        {
            LoadingWorkScheduler scheduler = new(
                new LoadingConcurrencyPolicy(1, true, 1));
            TaskCompletionSource<bool> firstStarted =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> releaseFirst =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            List<string> order = new();
            bool promoted = false;

            UniTask<string[]> first = scheduler.RunAsync(
                new[]
                {
                    (Func<UniTask<string>>)(async () =>
                    {
                        firstStarted.SetResult(true);
                        await releaseFirst.Task;
                        return "first";
                    })
                },
                LoadingWorkCategory.JsonAndZip,
                () => LoadingWorkPriority.Background,
                CancellationToken.None);
            await firstStarted.Task;

            UniTask<string[]> background = scheduler.RunAsync(
                new[]
                {
                    (Func<UniTask<string>>)(() =>
                    {
                        order.Add("background");
                        return UniTask.FromResult("background");
                    })
                },
                LoadingWorkCategory.JsonAndZip,
                () => LoadingWorkPriority.Background,
                CancellationToken.None);
            UniTask<string[]> changingPriority = scheduler.RunAsync(
                new[]
                {
                    (Func<UniTask<string>>)(() =>
                    {
                        order.Add("promoted");
                        return UniTask.FromResult("promoted");
                    })
                },
                LoadingWorkCategory.JsonAndZip,
                () => promoted
                    ? LoadingWorkPriority.Foreground
                    : LoadingWorkPriority.Background,
                CancellationToken.None);

            promoted = true;
            releaseFirst.SetResult(true);
            await first;
            await changingPriority;
            await background;

            Assert.That(
                order,
                Is.EqualTo(new[] { "promoted", "background" }));
        }

        [Test]
        public async Task RunAsync_CancellationWhileQueuedDoesNotRunTask()
        {
            LoadingWorkScheduler scheduler = new(
                new LoadingConcurrencyPolicy(1, true, 1));
            TaskCompletionSource<bool> firstStarted =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<bool> releaseFirst =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            bool canceledTaskRan = false;

            UniTask<int[]> first = scheduler.RunAsync(
                new[]
                {
                    (Func<UniTask<int>>)(async () =>
                    {
                        firstStarted.SetResult(true);
                        await releaseFirst.Task;
                        return 1;
                    })
                },
                LoadingWorkCategory.JsonAndZip,
                () => LoadingWorkPriority.Background,
                CancellationToken.None);
            await firstStarted.Task;

            using CancellationTokenSource cancellation = new();
            UniTask<int[]> canceled = scheduler.RunAsync(
                new[]
                {
                    (Func<UniTask<int>>)(() =>
                    {
                        canceledTaskRan = true;
                        return UniTask.FromResult(2);
                    })
                },
                LoadingWorkCategory.JsonAndZip,
                () => LoadingWorkPriority.Foreground,
                cancellation.Token);
            cancellation.Cancel();

            Exception exception = await CaptureExceptionAsync(
                async () => await canceled);
            releaseFirst.SetResult(true);
            await first;

            Assert.That(
                exception,
                Is.InstanceOf<OperationCanceledException>());
            Assert.That(canceledTaskRan, Is.False);
        }

        private static async Task<Exception> CaptureExceptionAsync(
            Func<Task> action)
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

        private static void SetMaximum(ref int maximum, int value)
        {
            int observed;
            do
            {
                observed = maximum;
                if (value <= observed)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(
                ref maximum,
                value,
                observed) != observed);
        }
    }
}
