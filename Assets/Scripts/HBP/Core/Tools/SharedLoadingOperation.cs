using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HBP.Core.Tools
{
    public enum LoadingOperationState
    {
        NotStarted,
        Loading,
        Ready,
        Validating,
        Validated,
        ValidatedWithIssues,
        ValidationFailed,
        Cancelled
    }

    public sealed class LoadingProgress
    {
        public float Value { get; }
        public float Duration { get; }
        public LoadingText Text { get; }
        public LoadingOperationState State { get; }

        public LoadingProgress(
            float value,
            float duration,
            LoadingText text,
            LoadingOperationState state)
        {
            Value = value;
            Duration = duration;
            Text = text;
            State = state;
        }
    }

    /// <summary>
    /// Owns one loading pipeline and exposes shared Ready and Validated barriers.
    /// Starting or awaiting the operation several times never duplicates its work.
    /// </summary>
    public sealed class SharedLoadingOperation<TResult>
    {
        private readonly object m_Lock = new();
        private readonly Func<Action<float, float, LoadingText>, CancellationToken, UniTask<TResult>> m_LoadAsync;
        private readonly Func<TResult, Action<float, float, LoadingText>, CancellationToken, UniTask<bool>> m_ValidateAsync;
        private readonly CancellationTokenSource m_Cancellation = new();
        private readonly TaskCompletionSource<TResult> m_Ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<TResult> m_Validated = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly List<ProgressSubscription> m_ProgressSubscriptions = new();
        private readonly SynchronizationContext m_ProgressSynchronizationContext;

        private LoadingOperationState m_State = LoadingOperationState.NotStarted;
        private LoadingProgress m_LastProgress;
        private TResult m_Result;
        private Exception m_Exception;
        private bool m_HasResult;
        private bool m_Started;
        private long m_ReportedProgressCount;
        private long m_FlushedProgressCount;

        public Guid ID { get; } = Guid.NewGuid();
        public long Generation { get; }
        public Task<TResult> Ready => m_Ready.Task;
        public Task<TResult> Validated => m_Validated.Task;
        public CancellationToken CancellationToken => m_Cancellation.Token;

        public LoadingOperationState State
        {
            get
            {
                lock (m_Lock)
                {
                    return m_State;
                }
            }
        }

        public LoadingProgress LastProgress
        {
            get
            {
                lock (m_Lock)
                {
                    return m_LastProgress;
                }
            }
        }

        public bool HasResult
        {
            get
            {
                lock (m_Lock)
                {
                    return m_HasResult;
                }
            }
        }

        public bool IsTerminal
        {
            get
            {
                lock (m_Lock)
                {
                    return m_State == LoadingOperationState.Validated
                        || m_State == LoadingOperationState.ValidatedWithIssues
                        || m_State == LoadingOperationState.ValidationFailed
                        || m_State == LoadingOperationState.Cancelled;
                }
            }
        }

        public TResult Result
        {
            get
            {
                lock (m_Lock)
                {
                    if (!m_HasResult)
                    {
                        throw new InvalidOperationException("The loading operation has not reached Ready.");
                    }
                    return m_Result;
                }
            }
        }

        public Exception Exception
        {
            get
            {
                lock (m_Lock)
                {
                    return m_Exception;
                }
            }
        }

        /// <param name="generation">Generation of the project or workspace owning the operation.</param>
        /// <param name="loadAsync">Builds and returns the coherent graph.</param>
        /// <param name="validateAsync">
        /// Optionally validates the graph and returns true when normal data issues were found.
        /// A missing validator promotes Ready directly to Validated.
        /// </param>
        public SharedLoadingOperation(
            long generation,
            Func<Action<float, float, LoadingText>, CancellationToken, UniTask<TResult>> loadAsync,
            Func<TResult, Action<float, float, LoadingText>, CancellationToken, UniTask<bool>> validateAsync = null)
        {
            Generation = generation;
            m_LoadAsync = loadAsync ?? throw new ArgumentNullException(nameof(loadAsync));
            m_ValidateAsync = validateAsync;
            m_ProgressSynchronizationContext = SynchronizationContext.Current;
        }

        public Task<TResult> EnsureReadyAsync()
        {
            Start();
            return Ready;
        }

        public Task<TResult> EnsureValidatedAsync()
        {
            Start();
            return Validated;
        }

        /// <summary>
        /// Cancels only this consumer's wait. The shared operation keeps running.
        /// </summary>
        public async Task<TResult> EnsureValidatedAsync(CancellationToken consumerToken)
        {
            Task<TResult> validation = EnsureValidatedAsync();
            if (!consumerToken.CanBeCanceled || validation.IsCompleted)
            {
                return await validation;
            }

            TaskCompletionSource<bool> cancellation =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            using CancellationTokenRegistration registration =
                consumerToken.Register(() => cancellation.TrySetResult(true));
            if (await Task.WhenAny(validation, cancellation.Task) != validation)
            {
                throw new OperationCanceledException(consumerToken);
            }
            return await validation;
        }

        public IDisposable SubscribeProgress(Action<LoadingProgress> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            ProgressSubscription subscription = new(this, listener);
            LoadingProgress progress;
            lock (m_Lock)
            {
                m_ProgressSubscriptions.Add(subscription);
                progress = m_LastProgress;
            }

            if (progress != null)
            {
                subscription.Publish(progress);
            }
            return subscription;
        }

        public void Cancel()
        {
            m_Cancellation.Cancel();
        }

        private void Start()
        {
            lock (m_Lock)
            {
                if (m_Started)
                {
                    return;
                }

                m_Started = true;
                m_State = LoadingOperationState.Loading;
            }

            ExecuteAsync().Forget();
        }

        private async UniTaskVoid ExecuteAsync()
        {
            try
            {
                TResult result = await m_LoadAsync(ReportProgress, m_Cancellation.Token);
                await FlushProgressAsync();
                m_Cancellation.Token.ThrowIfCancellationRequested();

                lock (m_Lock)
                {
                    m_Result = result;
                    m_HasResult = true;
                    m_State = LoadingOperationState.Ready;
                }
                m_Ready.TrySetResult(result);

                bool hasIssues = false;
                if (m_ValidateAsync != null)
                {
                    lock (m_Lock)
                    {
                        m_State = LoadingOperationState.Validating;
                    }
                    hasIssues = await m_ValidateAsync(result, ReportProgress, m_Cancellation.Token);
                    await FlushProgressAsync();
                    m_Cancellation.Token.ThrowIfCancellationRequested();
                }

                lock (m_Lock)
                {
                    m_State = hasIssues
                        ? LoadingOperationState.ValidatedWithIssues
                        : LoadingOperationState.Validated;
                }
                m_Validated.TrySetResult(result);
            }
            catch (OperationCanceledException exception)
            {
                lock (m_Lock)
                {
                    m_Exception = exception;
                    m_State = LoadingOperationState.Cancelled;
                }
                m_Ready.TrySetCanceled();
                m_Validated.TrySetCanceled();
            }
            catch (Exception exception)
            {
                lock (m_Lock)
                {
                    m_Exception = exception;
                    m_State = LoadingOperationState.ValidationFailed;
                }
                m_Ready.TrySetException(exception);
                m_Validated.TrySetException(exception);
            }
        }

        private async UniTask FlushProgressAsync()
        {
            if (m_ProgressSynchronizationContext == null)
            {
                return;
            }

            lock (m_Lock)
            {
                if (m_ReportedProgressCount == m_FlushedProgressCount)
                {
                    return;
                }
                m_FlushedProgressCount = m_ReportedProgressCount;
            }

            TaskCompletionSource<bool> completion =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            m_ProgressSynchronizationContext.Post(
                _ => completion.TrySetResult(true),
                null);
            await completion.Task;
        }

        private void ReportProgress(float value, float duration, LoadingText text)
        {
            ProgressSubscription[] subscriptions;
            LoadingProgress progress;
            lock (m_Lock)
            {
                float previous = m_LastProgress?.Value ?? 0;
                float boundedValue = float.IsNaN(value) ? previous : Math.Min(1, Math.Max(0, value));
                if (m_LastProgress != null && boundedValue < previous)
                {
                    return;
                }

                m_ReportedProgressCount++;
                progress = new LoadingProgress(
                    boundedValue,
                    duration,
                    text ?? new LoadingText(),
                    m_State);
                m_LastProgress = progress;
                subscriptions = m_ProgressSubscriptions.ToArray();
            }

            foreach (ProgressSubscription subscription in subscriptions)
            {
                PublishProgress(subscription, progress);
            }
        }

        private void PublishProgress(
            ProgressSubscription subscription,
            LoadingProgress progress)
        {
            if (m_ProgressSynchronizationContext == null)
            {
                subscription.Publish(progress);
                return;
            }

            m_ProgressSynchronizationContext.Post(
                _ => subscription.Publish(progress),
                null);
        }

        private void Unsubscribe(ProgressSubscription subscription)
        {
            lock (m_Lock)
            {
                m_ProgressSubscriptions.Remove(subscription);
            }
        }

        private sealed class ProgressSubscription : IDisposable
        {
            private readonly object m_SubscriptionLock = new();
            private SharedLoadingOperation<TResult> m_Owner;
            private Action<LoadingProgress> m_Listener;
            private float m_LastValue = -1;

            public ProgressSubscription(SharedLoadingOperation<TResult> owner, Action<LoadingProgress> listener)
            {
                m_Owner = owner;
                m_Listener = listener;
            }

            public void Publish(LoadingProgress progress)
            {
                lock (m_SubscriptionLock)
                {
                    if (m_Listener == null || progress.Value < m_LastValue)
                    {
                        return;
                    }

                    m_LastValue = progress.Value;
                    m_Listener(progress);
                }
            }

            public void Dispose()
            {
                SharedLoadingOperation<TResult> owner;
                lock (m_SubscriptionLock)
                {
                    owner = m_Owner;
                    m_Owner = null;
                    m_Listener = null;
                }
                owner?.Unsubscribe(this);
            }
        }
    }
}
