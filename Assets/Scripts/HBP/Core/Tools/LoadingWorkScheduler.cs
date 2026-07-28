using Cysharp.Threading.Tasks;
using HBP.Core.Preferences;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HBP.Core.Tools
{
    public enum LoadingWorkCategory
    {
        JsonAndZip,
        FileSystem,
        Metadata,
        Native
    }

    public enum LoadingWorkPriority
    {
        Background,
        Foreground
    }

    /// <summary>
    /// Computes bounded worker counts for loading and validation phases.
    /// </summary>
    public sealed class LoadingConcurrencyPolicy
    {
        public const string OverrideEnvironmentVariable =
            "HIBOP_LOADING_CONCURRENCY_OVERRIDE";
        public const string BackgroundValidationEnvironmentVariable =
            "HIBOP_BACKGROUND_VALIDATION";

        private readonly int m_ProcessorCount;
        private readonly bool m_MultiThreading;
        private readonly int? m_Override;

        public int GlobalLimit => GetLimit(LoadingWorkCategory.JsonAndZip);

        public LoadingConcurrencyPolicy(
            int processorCount,
            bool multiThreading,
            int? concurrencyOverride = null)
        {
            m_ProcessorCount = Math.Max(1, processorCount);
            m_MultiThreading = multiThreading;
            m_Override = concurrencyOverride > 0
                ? concurrencyOverride
                : null;
        }

        public static LoadingConcurrencyPolicy Current
        {
            get
            {
                bool multiThreading =
                    !PersistentDataManager.IsInitialized ||
                    (PersistentDataManager.UserPreferences?.General?.System
                        ?.MultiThreading ?? true);
                int? concurrencyOverride = null;
                if (int.TryParse(
                    Environment.GetEnvironmentVariable(
                        OverrideEnvironmentVariable),
                    out int parsedOverride) &&
                    parsedOverride > 0)
                {
                    concurrencyOverride = parsedOverride;
                }

                return new LoadingConcurrencyPolicy(
                    Environment.ProcessorCount,
                    multiThreading,
                    concurrencyOverride);
            }
        }

        public static bool BackgroundValidationEnabled
        {
            get
            {
                string value = Environment.GetEnvironmentVariable(
                    BackgroundValidationEnvironmentVariable);
                return !string.Equals(
                        value,
                        "false",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(value, "0", StringComparison.Ordinal);
            }
        }

        public int GetLimit(LoadingWorkCategory category)
        {
            if (!m_MultiThreading)
            {
                return 1;
            }
            if (m_Override.HasValue)
            {
                return m_Override.Value;
            }

            return category switch
            {
                LoadingWorkCategory.JsonAndZip =>
                    Math.Min(8, m_ProcessorCount),
                LoadingWorkCategory.FileSystem =>
                    Math.Min(8, m_ProcessorCount),
                LoadingWorkCategory.Metadata =>
                    Math.Min(4, Math.Max(1, m_ProcessorCount / 2)),
                LoadingWorkCategory.Native =>
                    Math.Min(2, Math.Max(1, m_ProcessorCount / 4)),
                _ => 1
            };
        }

        public int GetWorkerCount(
            LoadingWorkCategory category,
            int itemCount)
        {
            return Math.Min(Math.Max(0, itemCount), GetLimit(category));
        }
    }

    /// <summary>
    /// Shares one bounded budget between database and project work. Foreground
    /// waiters are selected before background waiters without interrupting work
    /// that already owns a slot.
    /// </summary>
    public sealed class LoadingWorkScheduler
    {
        private static readonly LoadingWorkScheduler s_Shared =
            new(() => LoadingConcurrencyPolicy.Current);

        private readonly object m_Lock = new();
        private readonly Func<LoadingConcurrencyPolicy> m_PolicyProvider;
        private readonly List<Waiter> m_Waiters = new();
        private readonly Dictionary<LoadingWorkCategory, int>
            m_ActiveByCategory = new();
        private int m_ActiveCount;

        public static LoadingWorkScheduler Shared => s_Shared;

        internal LoadingWorkScheduler(LoadingConcurrencyPolicy policy)
            : this(() => policy)
        {
        }

        private LoadingWorkScheduler(
            Func<LoadingConcurrencyPolicy> policyProvider)
        {
            m_PolicyProvider = policyProvider ??
                throw new ArgumentNullException(nameof(policyProvider));
        }

        public async UniTask<T[]> RunAsync<T>(
            IEnumerable<Func<UniTask<T>>> tasks,
            LoadingWorkCategory category,
            Func<LoadingWorkPriority> priorityProvider,
            CancellationToken token,
            Action<int, int> updateProgress = null,
            int? localWorkerLimit = null)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            Func<UniTask<T>>[] taskArray = tasks.ToArray();
            T[] results = new T[taskArray.Length];
            updateProgress?.Invoke(0, taskArray.Length);
            if (taskArray.Length == 0)
            {
                return results;
            }

            LoadingConcurrencyPolicy policy = m_PolicyProvider();
            int workerCount = policy.GetWorkerCount(
                category,
                taskArray.Length);
            if (localWorkerLimit > 0)
            {
                workerCount = Math.Min(
                    workerCount,
                    localWorkerLimit.Value);
            }
            int nextIndex = -1;
            int completedCount = 0;
            object progressLock = new();
            SynchronizationContext progressContext =
                SynchronizationContext.Current;
            Func<LoadingWorkPriority> getPriority =
                priorityProvider ?? (() => LoadingWorkPriority.Foreground);

            UniTask[] workers = Enumerable.Range(0, workerCount)
                .Select(_ => RunWorkerAsync())
                .ToArray();
            await UniTask.WhenAll(workers);
            token.ThrowIfCancellationRequested();
            return results;

            async UniTask RunWorkerAsync()
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    int index = Interlocked.Increment(ref nextIndex);
                    if (index >= taskArray.Length)
                    {
                        return;
                    }

                    using (await AcquireAsync(
                        category,
                        getPriority,
                        token))
                    {
                        token.ThrowIfCancellationRequested();
                        results[index] = await taskArray[index]();
                    }

                    await ReportProgressAsync();
                }
            }

            async UniTask ReportProgressAsync()
            {
                if (updateProgress == null)
                {
                    return;
                }

                Task postedUpdate = null;
                lock (progressLock)
                {
                    int completed = ++completedCount;
                    if (progressContext == null ||
                        SynchronizationContext.Current == progressContext)
                    {
                        updateProgress(completed, taskArray.Length);
                    }
                    else
                    {
                        TaskCompletionSource<bool> completion = new(
                            TaskCreationOptions.RunContinuationsAsynchronously);
                        postedUpdate = completion.Task;
                        progressContext.Post(
                            _ =>
                            {
                                try
                                {
                                    updateProgress(
                                        completed,
                                        taskArray.Length);
                                    completion.TrySetResult(true);
                                }
                                catch (Exception exception)
                                {
                                    completion.TrySetException(exception);
                                }
                            },
                            null);
                    }
                }

                if (postedUpdate != null)
                {
                    await postedUpdate;
                }
            }
        }

        public async UniTask RunAsync(
            IEnumerable<Func<UniTask>> tasks,
            LoadingWorkCategory category,
            Func<LoadingWorkPriority> priorityProvider,
            CancellationToken token,
            Action<int, int> updateProgress = null,
            int? localWorkerLimit = null)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            Func<UniTask<bool>>[] wrappedTasks = tasks
                .Select(task => (Func<UniTask<bool>>)(async () =>
                {
                    await task();
                    return true;
                }))
                .ToArray();
            await RunAsync(
                wrappedTasks,
                category,
                priorityProvider,
                token,
                updateProgress,
                localWorkerLimit);
        }

        private async UniTask<IDisposable> AcquireAsync(
            LoadingWorkCategory category,
            Func<LoadingWorkPriority> priorityProvider,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Waiter waiter = new(
                category,
                priorityProvider,
                token);
            IReadOnlyList<Waiter> granted;
            lock (m_Lock)
            {
                m_Waiters.Add(waiter);
                waiter.RegisterCancellation(() => Cancel(waiter));
                granted = DispatchLocked();
            }
            CompleteGranted(granted);
            return await waiter.Completion.Task;
        }

        private void Cancel(Waiter waiter)
        {
            IReadOnlyList<Waiter> granted;
            bool canceled;
            lock (m_Lock)
            {
                canceled = m_Waiters.Remove(waiter);
                granted = canceled
                    ? DispatchLocked()
                    : Array.Empty<Waiter>();
            }
            if (canceled)
            {
                waiter.Cancel();
            }
            CompleteGranted(granted);
        }

        private IReadOnlyList<Waiter> DispatchLocked()
        {
            List<Waiter> granted = new();
            while (true)
            {
                LoadingConcurrencyPolicy policy = m_PolicyProvider();
                if (m_ActiveCount >= policy.GlobalLimit)
                {
                    return granted;
                }

                Waiter next = FindNextEligibleWaiter(policy);
                if (next == null)
                {
                    return granted;
                }

                m_Waiters.Remove(next);
                m_ActiveCount++;
                m_ActiveByCategory.TryGetValue(
                    next.Category,
                    out int activeForCategory);
                m_ActiveByCategory[next.Category] = activeForCategory + 1;
                granted.Add(next);
            }
        }

        private Waiter FindNextEligibleWaiter(
            LoadingConcurrencyPolicy policy)
        {
            Waiter background = null;
            foreach (Waiter waiter in m_Waiters)
            {
                m_ActiveByCategory.TryGetValue(
                    waiter.Category,
                    out int activeForCategory);
                if (activeForCategory >= policy.GetLimit(waiter.Category))
                {
                    continue;
                }

                if (waiter.Priority == LoadingWorkPriority.Foreground)
                {
                    return waiter;
                }
                background ??= waiter;
            }
            return background;
        }

        private void CompleteGranted(IReadOnlyList<Waiter> granted)
        {
            foreach (Waiter waiter in granted)
            {
                waiter.Grant(new Lease(this, waiter.Category));
            }
        }

        private void Release(LoadingWorkCategory category)
        {
            IReadOnlyList<Waiter> granted;
            lock (m_Lock)
            {
                m_ActiveCount--;
                m_ActiveByCategory[category]--;
                granted = DispatchLocked();
            }
            CompleteGranted(granted);
        }

        private sealed class Waiter
        {
            private readonly CancellationToken m_Token;
            private readonly Func<LoadingWorkPriority> m_PriorityProvider;
            private CancellationTokenRegistration m_CancellationRegistration;

            public LoadingWorkCategory Category { get; }
            public LoadingWorkPriority Priority => m_PriorityProvider();
            public TaskCompletionSource<IDisposable> Completion { get; } =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public Waiter(
                LoadingWorkCategory category,
                Func<LoadingWorkPriority> priorityProvider,
                CancellationToken token)
            {
                Category = category;
                m_PriorityProvider = priorityProvider;
                m_Token = token;
            }

            public void RegisterCancellation(Action cancel)
            {
                if (m_Token.CanBeCanceled)
                {
                    m_CancellationRegistration =
                        m_Token.Register(cancel);
                }
            }

            public void Grant(IDisposable lease)
            {
                m_CancellationRegistration.Dispose();
                Completion.TrySetResult(lease);
            }

            public void Cancel()
            {
                Completion.TrySetCanceled(m_Token);
            }
        }

        private sealed class Lease : IDisposable
        {
            private LoadingWorkScheduler m_Scheduler;
            private readonly LoadingWorkCategory m_Category;

            public Lease(
                LoadingWorkScheduler scheduler,
                LoadingWorkCategory category)
            {
                m_Scheduler = scheduler;
                m_Category = category;
            }

            public void Dispose()
            {
                LoadingWorkScheduler scheduler =
                    Interlocked.Exchange(ref m_Scheduler, null);
                scheduler?.Release(m_Category);
            }
        }
    }
}
