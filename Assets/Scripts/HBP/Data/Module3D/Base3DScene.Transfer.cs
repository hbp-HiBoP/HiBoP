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
        private Exception m_PreparationError;

        public static UniTask PrepareStandardResourcesAsync()
        {
            UniTask work = Core.Object3D.Object3DManager.MNI.Load();
            TrackStandardPreparation(work);
            return work;
        }

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
        public async UniTask<T> CapturePreparedAsync<T>(Func<T> capture, CancellationToken token)
        {
            await m_InitializationWork;
            await m_AnatomyWork;
            await UniTask.SwitchToMainThread();
            token.ThrowIfCancellationRequested();
            if (IsClosing || !SceneInformation.Initialized) throw new InvalidOperationException("The visualization is closing or has not finished initialization.");
            // Export expands preload coverage; ordinary Desktop opening retains its preference.
            m_AnatomyWork = LoadMissingAnatomyAsync(includeAllPatients: true).ToAsyncLazy().Task;
            await m_AnatomyWork;
            await UniTask.SwitchToMainThread();
            token.ThrowIfCancellationRequested();
            if (IsClosing) throw new ObjectDisposedException(Name);
            await m_SurfaceRepresentationGate.WaitAsync(token);
            try
            {
                await UniTask.SwitchToMainThread();
                do
                {
                    await m_GeneratorWork;
                    await m_CorrelationWork;
                    await m_ColliderWork;
                    await UniTask.SwitchToMainThread();
                    token.ThrowIfCancellationRequested();
                } while (!m_GeneratorWork.Status.IsCompleted() || !m_CorrelationWork.Status.IsCompleted() || !m_ColliderWork.Status.IsCompleted());

                if (IsClosing) throw new ObjectDisposedException(Name);
                return capture();
            }
            finally
            {
                m_SurfaceRepresentationGate.Release();
            }
        }

        /// <summary>Consumes already prepared data and scene-owned resources, without project loading or Desktop views.</summary>
        public UniTask InitializePreparedAsync(Action<float, float, LoadingText> progress, CancellationToken token)
        {
            m_InitializationWork = InitializePreparedContentAsync(progress, token).ToAsyncLazy().Task;
            return m_InitializationWork;
        }

        private async UniTask InitializePreparedContentAsync(Action<float, float, LoadingText> progress, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.SwitchToMainThread();
            m_MeshManager.InitializeMeshes();
            await LoadSitesAsync(Visualization.Patients);
            await LoadColumnsAsync();
            await UniTask.SwitchToMainThread();
            token.ThrowIfCancellationRequested();
            if (IsClosing) throw new ObjectDisposedException(Name);
            foreach (var column in Columns) column.InitializeColumnMeshes(m_DisplayedObjects.Brain);
            FinalizeInitialization();
            LoadConfiguration();
            // Resolve selected topology and reset its eraser before the transfer applies
            // current masks. Waiting until Update would discard those restored masks.
            UpdateGeometry();
            progress?.Invoke(1, 0, new LoadingText("Prepared visualization restored"));
        }

        public async UniTask PrepareRenderingAsync(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (IsClosing) throw new ObjectDisposedException(Name);
                if (m_PreparationError != null) throw new InvalidOperationException("Common scene preparation failed.", m_PreparationError);
                bool geometryReady = SceneInformation.CompletelyLoaded && !SceneInformation.GeometryNeedsUpdate && !SceneInformation.ProjectionGridNeedsUpdate && !SceneInformation.SurfaceProjectionNeedsUpdate;
                if (geometryReady && !IsGeneratorUpToDate && CanComputeFunctionalValues && !m_UpdatingGenerators) UpdateGenerator();
                if (geometryReady && (IsGeneratorUpToDate || !CanComputeFunctionalValues) && !m_UpdatingGenerators && !SceneInformation.SitesNeedUpdate && !SceneInformation.CutsNeedUpdate && !SceneInformation.BaseCutTexturesNeedUpdate && !SceneInformation.FunctionalCutTexturesNeedUpdate && !SceneInformation.GUICutTexturesNeedUpdate && !SceneInformation.FunctionalSurfaceNeedsUpdate) return;
                await UniTask.Yield();
            }
        }
    }
}
