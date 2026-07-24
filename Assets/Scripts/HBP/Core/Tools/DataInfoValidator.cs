using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HBP.Core.Data
{
    public sealed class DataInfoValidationResult
    {
        private readonly IReadOnlyList<Entry> m_Entries;

        public long Generation { get; }
        public bool HasIssues => m_Entries.Any(entry =>
            entry.ValidatedSnapshot.Errors.Count > 0 ||
            entry.ValidatedSnapshot.Warnings.Count > 0);

        internal DataInfoValidationResult(long generation, IReadOnlyList<Entry> entries)
        {
            Generation = generation;
            m_Entries = entries;
        }

        public bool TryApply(long currentGeneration)
        {
            if (Generation != currentGeneration)
            {
                return false;
            }

            foreach (Entry entry in m_Entries)
            {
                entry.Target.ApplyValidationState(entry.ValidatedSnapshot);
            }
            return true;
        }

        internal sealed class Entry
        {
            public DataInfo Target { get; }
            public DataInfo ValidatedSnapshot { get; }

            public Entry(DataInfo target, DataInfo validatedSnapshot)
            {
                Target = target;
                ValidatedSnapshot = validatedSnapshot;
            }
        }
    }

    /// <summary>
    /// Computes DataInfo validation on private clones so the published graph is
    /// unchanged until the complete result is applied.
    /// </summary>
    public sealed class DataInfoValidator
    {
        public async UniTask<DataInfoValidationResult> ValidateAsync(
            IEnumerable<DataInfo> dataInfos,
            bool force,
            int maxConcurrency,
            CancellationToken token,
            Action<int, int> updateProgress = null,
            long generation = 0)
        {
            if (dataInfos == null)
            {
                throw new ArgumentNullException(nameof(dataInfos));
            }

            token.ThrowIfCancellationRequested();
            DataInfo[] snapshot = dataInfos.Where(dataInfo => dataInfo != null).ToArray();
            DataInfoValidationResult.Entry[] results =
                new DataInfoValidationResult.Entry[snapshot.Length];
            updateProgress?.Invoke(0, snapshot.Length);
            if (snapshot.Length == 0)
            {
                return new DataInfoValidationResult(generation, results);
            }

            int nextIndex = -1;
            int completedCount = 0;
            object progressLock = new();
            int workerCount = Math.Min(Math.Max(1, maxConcurrency), snapshot.Length);
            UniTask[] workers = Enumerable.Range(0, workerCount)
                .Select(_ => ValidateWorkerAsync(
                    snapshot,
                    results,
                    force,
                    () => Interlocked.Increment(ref nextIndex),
                    () =>
                    {
                        if (updateProgress == null)
                        {
                            return;
                        }

                        lock (progressLock)
                        {
                            updateProgress(++completedCount, snapshot.Length);
                        }
                    },
                    token))
                .ToArray();

            await UniTask.WhenAll(workers);
            token.ThrowIfCancellationRequested();
            return new DataInfoValidationResult(
                generation,
                results.Where(result => result != null).ToArray());
        }

        private static async UniTask ValidateWorkerAsync(
            IReadOnlyList<DataInfo> snapshot,
            DataInfoValidationResult.Entry[] results,
            bool force,
            Func<int> nextIndex,
            Action validationCompleted,
            CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();
            while (true)
            {
                token.ThrowIfCancellationRequested();
                int index = nextIndex();
                if (index >= snapshot.Count)
                {
                    return;
                }

                DataInfo target = snapshot[index];
                DataInfo validatedSnapshot = target.CreateValidationSnapshot(force);
                if (validatedSnapshot != null)
                {
                    results[index] = new DataInfoValidationResult.Entry(target, validatedSnapshot);
                }
                validationCompleted();
            }
        }
    }
}
