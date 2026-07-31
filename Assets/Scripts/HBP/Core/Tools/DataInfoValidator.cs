using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    public sealed class DataInfoValidationResult
    {
        private readonly IReadOnlyList<Entry> m_Entries;
        private readonly ValidationRequest m_Request;

        public long Generation { get; }
        public bool HasIssues => m_Entries.Any(entry => entry.ValidatedSnapshot.Errors.Count > 0 || entry.ValidatedSnapshot.Warnings.Count > 0);

        internal DataInfoValidationResult(long generation, ValidationRequest request, IReadOnlyList<Entry> entries)
        {
            Generation = generation;
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
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
                entry.Target.ApplyValidationState(entry.ValidatedSnapshot, m_Request);
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
        private readonly IEEGValidationMetadataReader m_MetadataReader;

        public DataInfoValidator(IEEGValidationMetadataReader metadataReader = null)
        {
            m_MetadataReader = metadataReader;
        }

        public async UniTask<DataInfoValidationResult> ValidateAsync(IEnumerable<DataInfo> dataInfos, bool force, int maxConcurrency, CancellationToken token, Action<int, int> updateProgress = null, long generation = 0, Func<LoadingWorkPriority> priorityProvider = null)
        {
            return await ValidateAsync(dataInfos, new ValidationRequest(ValidationAspect.DataInfoAll, force: force), maxConcurrency, token, updateProgress, generation, priorityProvider);
        }

        public async UniTask<DataInfoValidationResult> ValidateAsync(IEnumerable<DataInfo> dataInfos, ValidationRequest request, int maxConcurrency, CancellationToken token, Action<int, int> updateProgress = null, long generation = 0, Func<LoadingWorkPriority> priorityProvider = null)
        {
            if (dataInfos == null)
            {
                throw new ArgumentNullException(nameof(dataInfos));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            token.ThrowIfCancellationRequested();
            DataInfo[] snapshot = dataInfos.Where(request.Matches).ToArray();
            Func<UniTask<DataInfoValidationResult.Entry>>[] tasks = snapshot.Select(target => (Func<UniTask<DataInfoValidationResult.Entry>>)(async () =>
            {
                await UniTask.SwitchToThreadPool();
                token.ThrowIfCancellationRequested();
                DataInfo validatedSnapshot = target.CreateValidationSnapshot(request, request.Force, m_MetadataReader);
                return validatedSnapshot == null ? null : new DataInfoValidationResult.Entry(target, validatedSnapshot);
            })).ToArray();
            LoadingWorkCategory category = GetWorkCategory(request);
            DataInfoValidationResult.Entry[] results = await LoadingWorkScheduler.Shared.RunAsync(tasks, category, priorityProvider, token, updateProgress, maxConcurrency);
            token.ThrowIfCancellationRequested();
            return new DataInfoValidationResult(generation, request, results.Where(result => result != null).ToArray());
        }

        internal static LoadingWorkCategory GetWorkCategory(ValidationRequest request)
        {
            if (request.Includes(ValidationAspect.SourceReadability) || request.Includes(ValidationAspect.Epoching) || request.Includes(ValidationAspect.ChannelMapping))
            {
                return LoadingWorkCategory.Native;
            }

            return request.Includes(ValidationAspect.StaticContent) ? LoadingWorkCategory.Metadata : LoadingWorkCategory.FileSystem;
        }
    }
}
