using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;

namespace HBP.Data.Module3D
{
    public partial class Base3DScene
    {
        private UniTask m_ExternalPreparationWork = UniTask.CompletedTask;

        public IDisposable RetainForPreparation()
        {
            if (IsClosing) throw new ObjectDisposedException(Name);
            var completion = new UniTaskCompletionSource();
            m_ExternalPreparationWork = completion.Task;
            return new PreparationLease(completion);
        }

        private sealed class PreparationLease : IDisposable
        {
            private readonly UniTaskCompletionSource completion;

            public PreparationLease(UniTaskCompletionSource completion)
            {
                this.completion = completion;
            }

            public void Dispose() => completion.TrySetResult();
        }

        /// <summary>Wait for all source resources, then copy on Unity's thread without interleaved mutations.</summary>
        public async UniTask<T> CapturePreparedAsync<T>(Func<T> capture, CancellationToken token, Func<string, IDisposable> measure = null)
        {
            using (measure?.Invoke("source.wait-initialization"))
                await m_InitializationWork;
            using (measure?.Invoke("source.wait-anatomy"))
            {
                // Another capture may start preparation while this caller is awaiting
                // Unity. Recheck on Unity before replacing the tracked lifetime task.
                do
                {
                    await m_AnatomyWork;
                    await UniTask.SwitchToMainThread();
                } while (!m_AnatomyWork.Status.IsCompleted());
            }

            token.ThrowIfCancellationRequested();
            if (IsClosing || !SceneInformation.Initialized)
                throw new InvalidOperationException("The visualization is closing or has not finished initialization.");
            using (measure?.Invoke("source.prepare-missing-anatomy"))
            {
                m_AnatomyWork = LoadMissingAnatomyAsync().ToAsyncLazy().Task;
                await m_AnatomyWork;
            }

            await UniTask.SwitchToMainThread();
            token.ThrowIfCancellationRequested();
            if (IsClosing)
                throw new ObjectDisposedException(Name);
            using (measure?.Invoke("source.wait-surface-representation"))
            {
                while (IsSurfaceRepresentationPreparing)
                {
                    token.ThrowIfCancellationRequested();
                    if (IsClosing) throw new ObjectDisposedException(Name);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }

            using (measure?.Invoke("source.wait-capture-gate"))
                await m_SurfaceRepresentationGate.WaitAsync(token);
            try
            {
                await UniTask.SwitchToMainThread();
                using (measure?.Invoke("source.wait-derived-data"))
                {
                    do
                    {
                        using (measure?.Invoke("source.wait-generator")) await m_GeneratorWork;
                        using (measure?.Invoke("source.wait-correlation")) await m_CorrelationWork;
                        using (measure?.Invoke("source.wait-collider")) await m_ColliderWork;
                        await UniTask.SwitchToMainThread();
                        token.ThrowIfCancellationRequested();
                    } while (!m_GeneratorWork.Status.IsCompleted() || !m_CorrelationWork.Status.IsCompleted() || !m_ColliderWork.Status.IsCompleted());
                }

                if (IsClosing)
                    throw new ObjectDisposedException(Name);
                using (measure?.Invoke("source.capture-owned-snapshot"))
                    return capture();
            }
            finally
            {
                m_SurfaceRepresentationGate.Release();
            }
        }

        /// <summary>Resource decoding feeds the same initializer as project loading.</summary>
        public UniTask InitializePreparedAsync(Func<CancellationToken, UniTask> loadResources, Action<float, float, LoadingText> progress, CancellationToken token)
        {
            m_InitializationWork = InitializeContentAsync(Visualization, progress, token, loadResources).ToAsyncLazy().Task;
            return m_InitializationWork;
        }
    }
}
