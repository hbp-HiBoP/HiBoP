using System.Threading;
using System.Threading.Tasks;

namespace HBP.Sync.Testing
{
    /// <summary>A manually advanced async boundary for tests; it never reads wall-clock time.</summary>
    public sealed class DeterministicAsyncGate
    {
        private readonly object m_Gate = new();
        private TaskCompletionSource<bool> m_Completion = NewCompletion();

        public bool IsOpen
        {
            get
            {
                lock (m_Gate)
                    return m_Completion.Task.IsCompleted;
            }
        }

        public Task WaitAsync(CancellationToken cancellationToken = default)
        {
            Task wait;
            lock (m_Gate)
                wait = m_Completion.Task;
            return cancellationToken.CanBeCanceled ? WaitWithCancellationAsync(wait, cancellationToken) : wait;
        }

        public void Open()
        {
            TaskCompletionSource<bool> completion;
            lock (m_Gate)
                completion = m_Completion;
            completion.TrySetResult(true);
        }

        public void Reset()
        {
            lock (m_Gate)
            {
                if (!m_Completion.Task.IsCompleted)
                    throw new System.InvalidOperationException("Open the gate before resetting it.");
                m_Completion = NewCompletion();
            }
        }

        private static TaskCompletionSource<bool> NewCompletion() => new(TaskCreationOptions.RunContinuationsAsynchronously);

        private static async Task WaitWithCancellationAsync(Task wait, CancellationToken cancellationToken)
        {
            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(() => cancelled.TrySetCanceled(cancellationToken)))
                await await Task.WhenAny(wait, cancelled.Task).ConfigureAwait(false);
        }
    }
}
