using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CRNL.HiBoP.Protocol
{
    public enum LatestWinsOutcomeKind : byte
    {
        Completed = 1,
        Superseded = 2,
        Failed = 3,
    }

    public sealed class LatestWinsOutcome<TResult>
    {
        private LatestWinsOutcome(LatestWinsOutcomeKind kind, TResult result, Exception error)
        {
            Kind = kind;
            Result = result;
            Error = error;
        }

        public LatestWinsOutcomeKind Kind { get; }
        public TResult Result { get; }
        public Exception Error { get; }

        internal static LatestWinsOutcome<TResult> Complete(TResult result) => new(LatestWinsOutcomeKind.Completed, result, null);
        internal static LatestWinsOutcome<TResult> Superseded() => new(LatestWinsOutcomeKind.Superseded, default, null);
        internal static LatestWinsOutcome<TResult> Fail(Exception error) => new(LatestWinsOutcomeKind.Failed, default, error);
    }

    public sealed class LatestWinsScheduler<TScope, TRequest, TResult>
    {
        private readonly object m_Gate = new();
        private readonly Func<TRequest, CancellationToken, Task<TResult>> m_Worker;
        private readonly Dictionary<TScope, ScopeQueue> m_Scopes = new();
        private long m_Completed;
        private long m_Failed;
        private long m_Superseded;

        public LatestWinsScheduler(Func<TRequest, CancellationToken, Task<TResult>> worker)
        {
            m_Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        }

        public long CompletedCount => Interlocked.Read(ref m_Completed);
        public long FailedCount => Interlocked.Read(ref m_Failed);
        public long SupersededCount => Interlocked.Read(ref m_Superseded);

        public int ScopeCount
        {
            get
            {
                lock (m_Gate)
                    return m_Scopes.Count;
            }
        }

        public Task<LatestWinsOutcome<TResult>> EnqueueAsync(TScope scope, ulong sequence, TRequest request)
        {
            if (sequence == 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));

            var item = new WorkItem(sequence, request);
            WorkItem start = null;
            WorkItem cancelActive = null;
            WorkItem replacedPending = null;
            lock (m_Gate)
            {
                if (!m_Scopes.TryGetValue(scope, out ScopeQueue queue))
                {
                    queue = new ScopeQueue();
                    m_Scopes.Add(scope, queue);
                }

                if (sequence <= queue.LatestSequence)
                {
                    Interlocked.Increment(ref m_Superseded);
                    item.Completion.SetResult(LatestWinsOutcome<TResult>.Superseded());
                    return item.Completion.Task;
                }

                queue.LatestSequence = sequence;
                if (queue.Active == null)
                {
                    queue.Active = item;
                    start = item;
                }
                else
                {
                    replacedPending = queue.Pending;
                    queue.Pending = item;
                    cancelActive = queue.Active;
                }
            }

            if (replacedPending != null)
            {
                Interlocked.Increment(ref m_Superseded);
                replacedPending.Completion.SetResult(LatestWinsOutcome<TResult>.Superseded());
            }

            cancelActive?.RequestCancellation();
            if (start != null)
                _ = RunAsync(scope, start);
            return item.Completion.Task;
        }

        public void GetDepth(TScope scope, out int active, out int pending)
        {
            lock (m_Gate)
            {
                if (!m_Scopes.TryGetValue(scope, out ScopeQueue queue))
                {
                    active = 0;
                    pending = 0;
                    return;
                }

                active = queue.Active == null ? 0 : 1;
                pending = queue.Pending == null ? 0 : 1;
            }
        }

        private async Task RunAsync(TScope scope, WorkItem item)
        {
            TResult result = default;
            Exception failure = null;
            try
            {
                result = await m_Worker(item.Request, item.Cancellation.Token);
            }
            catch (OperationCanceledException) when (item.Cancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            WorkItem next = null;
            bool stale;
            lock (m_Gate)
            {
                ScopeQueue queue = m_Scopes[scope];
                stale = item.Sequence < queue.LatestSequence;
                queue.Active = queue.Pending;
                queue.Pending = null;
                next = queue.Active;
                if (next == null)
                    m_Scopes.Remove(scope);
            }

            item.Cancellation.Dispose();
            if (stale)
            {
                Interlocked.Increment(ref m_Superseded);
                item.Completion.SetResult(LatestWinsOutcome<TResult>.Superseded());
            }
            else if (failure != null)
            {
                Interlocked.Increment(ref m_Failed);
                item.Completion.SetResult(LatestWinsOutcome<TResult>.Fail(failure));
            }
            else
            {
                Interlocked.Increment(ref m_Completed);
                item.Completion.SetResult(LatestWinsOutcome<TResult>.Complete(result));
            }

            if (next != null)
                _ = RunAsync(scope, next);
        }

        private sealed class ScopeQueue
        {
            public ulong LatestSequence;
            public WorkItem Active;
            public WorkItem Pending;
        }

        private sealed class WorkItem
        {
            public WorkItem(ulong sequence, TRequest request)
            {
                Sequence = sequence;
                Request = request;
            }

            public ulong Sequence { get; }
            public TRequest Request { get; }
            public CancellationTokenSource Cancellation { get; } = new();
            public TaskCompletionSource<LatestWinsOutcome<TResult>> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public void RequestCancellation()
            {
                try
                {
                    Cancellation.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Completion won the race after this item left the active slot.
                }
            }
        }
    }
}
