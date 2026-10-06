using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using Newtonsoft.Json;

namespace HBP.Sync.Scene
{
    /// <summary>Prepares the same delivery-bound surface on both peers before publishing its display.</summary>
    public sealed class V2SurfaceInflationCoordinator : IDisposable
    {
        private readonly Base3DScene m_Scene;
        private readonly V2SceneMutationBoundary m_Boundary;
        private readonly V2PreparedSceneIdentity m_Identity;
        private readonly bool m_Desktop;
        private readonly Func<bool> m_Online;
        private readonly Func<bool> m_LocalAllowed;
        private readonly Func<ulong> m_CanonicalSequence;
        private readonly Action<V2SurfaceInflationControl, bool> m_Send;
        private readonly V2JobGenerationRegistry m_Generations = new();
        private readonly HashSet<OperationId> m_Finished = new();
        private readonly Queue<OperationId> m_FinishedOrder = new();
        private readonly Func<SurfaceRepresentation, IProgress<float>, CancellationToken, bool, UniTask> m_Handler;
        private ActiveJob m_Active;
        private ulong m_LastGeneration;
        private bool m_Disposed;

        public event Action<Task> RemoteJobStarted;

        /// <summary>Optional presentation wrapper for preparation only, completed before any transition.</summary>
        public Func<Func<CancellationToken, UniTask>, CancellationToken, UniTask> PreparationHandler { get; set; }

        public float Progress => m_Active == null ? 1 : Math.Min(m_Active.LocalProgress, m_Active.RemoteProgress);
        public bool IsBusy => m_Active != null;

        public V2SurfaceInflationCoordinator(Base3DScene scene, V2SceneMutationBoundary boundary, V2PreparedSceneIdentity identity, bool desktop, Func<bool> online, Func<ulong> canonicalSequence, Action<V2SurfaceInflationControl, bool> send, Func<bool> localAllowed = null)
        {
            m_Scene = scene;
            m_Boundary = boundary;
            m_Identity = identity;
            m_Desktop = desktop;
            m_Online = online;
            m_LocalAllowed = localAllowed ?? (() => true);
            m_CanonicalSequence = canonicalSequence;
            m_Send = send;
            m_Handler = RequestAsync;
            m_Scene.SurfaceRepresentationRequestHandler = m_Handler;
        }

        private async UniTask RequestAsync(SurfaceRepresentation representation, IProgress<float> progress, CancellationToken token, bool animate)
        {
            await UniTask.SwitchToMainThread(token);
            if (m_Disposed) throw new ObjectDisposedException(nameof(V2SurfaceInflationCoordinator));
            if (!m_Online())
            {
                if (!m_LocalAllowed()) throw new IOException("The scene connection is unavailable. Wait for reconnection or offline mode.");
                await RunPreparationAsync(representation, stop => m_Scene.PrepareSurfaceRepresentationAsync(representation, progress, stop), token);
                await m_Scene.SetLocalSurfaceRepresentationAsync(representation, progress, token, animate);
                return;
            }

            if (m_Active != null) throw new InvalidOperationException("Surface preparation is already running.");
            var id = new OperationId(Guid.NewGuid());
            Command command = FreezeCommand(representation, animate);
            ActiveJob active = CreateActive(id, 0, m_CanonicalSequence(), command, progress, token);
            if (m_Desktop)
                StartDesktop(active);
            else
            {
                try
                {
                    Send(V2SurfaceInflationControlKind.Request, active, payload: EncodeCommand(command));
                }
                catch
                {
                    Release(active);
                    throw;
                }

                active.Work = RunQuestAsync(active);
            }

            await active.Completion.Task;
        }

        /// <summary>Called on Unity's thread. Native calculation never blocks the transport reader.</summary>
        public void Receive(V2SurfaceInflationControl control)
        {
            if (m_Disposed || !m_Online()) return;
            if (control.Kind == V2SurfaceInflationControlKind.Request && m_Desktop)
            {
                if (m_Finished.Contains(control.JobId) || m_Active?.Id.Equals(control.JobId) == true) return;
                try
                {
                    if (m_Active != null || control.CanonicalSequence > m_CanonicalSequence()) throw new InvalidOperationException("Scene busy or request watermark unavailable.");
                    Command requested = DecodeCommand(control.Payload);
                    AssertSelected(requested);
                    // Desktop owns settings, including an already prepared custom cache.
                    ActiveJob active = CreateActive(control.JobId, 0, m_CanonicalSequence(), FreezeCommand(requested.Representation, requested.Animate), null, default);
                    StartDesktop(active);
                    NotifyRemoteStarted(active);
                }
                catch (Exception)
                {
                    SendFailure(control.JobId, 0);
                }

                return;
            }

            if (control.Kind == V2SurfaceInflationControlKind.Started && !m_Desktop)
            {
                if (control.Generation <= m_LastGeneration) return;
                try
                {
                    Command command = DecodeCommand(control.Payload);
                    bool pending = m_Active != null && m_Active.Id.Equals(control.JobId) && m_Active.Generation == 0;
                    if (m_Active != null && !pending) throw new InvalidOperationException("Scene busy.");
                    ActiveJob active = pending ? m_Active : CreateActive(control.JobId, 0, control.CanonicalSequence, command, null, default);
                    active.Command = command;
                    active.Sequence = control.CanonicalSequence;
                    active.Generation = control.Generation;
                    m_LastGeneration = control.Generation;
                    active.Started.TrySetResult(true);
                    if (!pending)
                    {
                        active.Work = RunQuestAsync(active);
                        NotifyRemoteStarted(active);
                    }
                }
                catch (Exception)
                {
                    SendFailure(control.JobId, control.Generation);
                }

                return;
            }

            ActiveJob current = m_Active;
            if (current == null || !current.Id.Equals(control.JobId) || control.Generation != current.Generation && control.Kind != V2SurfaceInflationControlKind.Cancel && control.Kind != V2SurfaceInflationControlKind.Failed) return;
            switch (control.Kind)
            {
                case V2SurfaceInflationControlKind.Progress:
                    current.RemoteProgress = Math.Max(current.RemoteProgress, control.Progress);
                    current.Progress?.Report(Progress);
                    break;
                case V2SurfaceInflationControlKind.Ready:
                    if (m_Desktop)
                    {
                        current.RemoteProgress = 1;
                        current.Ready.TrySetResult(true);
                    }

                    break;
                case V2SurfaceInflationControlKind.Commit:
                    if (!m_Desktop) current.Commit.TrySetResult(control.CanonicalSequence);
                    break;
                case V2SurfaceInflationControlKind.Transition:
                    if (!m_Desktop && current.Command.Animate) current.Transition.TrySetResult(true);
                    break;
                case V2SurfaceInflationControlKind.Transitioned:
                    if (m_Desktop && current.Command.Animate) current.Transitioned.TrySetResult(true);
                    break;
                case V2SurfaceInflationControlKind.Committed:
                    if (m_Desktop && current.CommitSent && control.CanonicalSequence == current.Sequence) current.Committed.TrySetResult(true);
                    break;
                case V2SurfaceInflationControlKind.Cancel:
                    if (m_Desktop && current.CommitSent) current.Committed.TrySetResult(true);
                    else current.Cancellation.Cancel();
                    break;
                case V2SurfaceInflationControlKind.Failed:
                    current.RemoteFailed = true;
                    current.Cancellation.Cancel();
                    break;
            }
        }

        private ActiveJob CreateActive(OperationId id, ulong generation, ulong sequence, Command command, IProgress<float> progress, CancellationToken token)
        {
            Mesh3D anatomicalMesh = m_Boundary.ResourceCatalog.ResolveMesh(command.Mesh);
            if (!m_Scene.TryBeginSurfaceRepresentationPreparation(out IDisposable busy)) throw new InvalidOperationException("The visualization is busy.");
            IDisposable retention;
            try
            {
                retention = m_Scene.RetainForPreparation();
            }
            catch
            {
                busy.Dispose();
                throw;
            }

            var active = new ActiveJob(id, generation, sequence, command, busy, retention, progress, token, anatomicalMesh);
            m_Active = active;
            return active;
        }

        private void StartDesktop(ActiveJob active)
        {
            try
            {
                active.Generation = m_Generations.BeginJob(m_Identity.SceneId, m_Identity.IncarnationId, V2JobType.SurfaceInflation, active.Id).Generation;
                Send(V2SurfaceInflationControlKind.Started, active, payload: EncodeCommand(active.Command));
                active.Work = RunDesktopAsync(active);
            }
            catch
            {
                Release(active);
                throw;
            }
        }

        private async Task RunDesktopAsync(ActiveJob active)
        {
            try
            {
                await PreparePeersAsync(active);
                if (active.Command.Animate)
                {
                    Send(V2SurfaceInflationControlKind.Transition, active);
                    active.PreviewStarted = true;
                    await m_Scene.PreviewSurfaceRepresentationAsync(active.Command.Representation, active.Cancellation.Token);
                    await WaitAsync(active.Transitioned.Task, active);
                }

                AssertInput(active);
                active.Cancellation.Token.ThrowIfCancellationRequested();
                // The synchronous publication is the only scientific mutation admitted by our busy scope.
                // Both caches are ready before this ordinary SetMeshDisplay canonical mutation exists.
                await m_Scene.SetLocalSurfaceRepresentationAsync(active.Command.Representation, cancellationToken: active.Cancellation.Token, animate: false);
                active.Sequence = m_CanonicalSequence();
                active.CommitSent = true;
                Send(V2SurfaceInflationControlKind.Commit, active);
                await WaitAsync(active.Committed.Task, active);
                active.Progress?.Report(1);
                active.Completion.TrySetResult(true);
            }
            catch (Exception exception)
            {
                await FailAsync(active, exception);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                try
                {
                    RestorePreview(active);
                }
                finally
                {
                    Release(active);
                }
            }
        }

        private async Task RunQuestAsync(ActiveJob active)
        {
            try
            {
                await PreparePeersAsync(active);
                if (active.Command.Animate)
                {
                    active.PreviewStarted = true;
                    await m_Scene.PreviewSurfaceRepresentationAsync(active.Command.Representation, active.Cancellation.Token);
                    Send(V2SurfaceInflationControlKind.Transitioned, active);
                }

                await WaitAsync(active.Commit.Task, active);
                ulong sequence = await active.Commit.Task;
                while (m_CanonicalSequence() < sequence)
                    await NextFrameAsync(active);
                AssertInput(active);
                if (m_Scene.MeshManager.SelectedMesh.Representation != active.Command.Representation)
                    throw new InvalidOperationException("The prepared representation was not applied.");
                active.Sequence = sequence;
                Send(V2SurfaceInflationControlKind.Committed, active);
                active.Completion.TrySetResult(true);
            }
            catch (Exception exception)
            {
                await FailAsync(active, exception);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                try
                {
                    RestorePreview(active);
                }
                finally
                {
                    Release(active);
                }
            }
        }

        private async UniTask PreparePeersAsync(ActiveJob active)
        {
            CancellationTokenRegistration cancellation = default;
            try
            {
                await RunPreparationAsync(active.Command.Representation, async token =>
                {
                    cancellation = token.Register(active.Cancellation.Cancel);
                    if (!m_Desktop)
                    {
                        await WaitAsync(active.Started.Task, active);
                        while (m_CanonicalSequence() < active.Sequence)
                            await NextFrameAsync(active);
                    }

                    await PrepareAsync(active);
                    if (m_Desktop)
                        await WaitAsync(active.Ready.Task, active);
                    else
                    {
                        Send(V2SurfaceInflationControlKind.Ready, active);
                        if (active.Command.Animate)
                            await WaitAsync(active.Transition.Task, active);
                    }
                }, active.Cancellation.Token);
                active.Cancellation.Token.ThrowIfCancellationRequested();
            }
            finally
            {
                cancellation.Dispose();
            }
        }

        private UniTask RunPreparationAsync(SurfaceRepresentation representation, Func<CancellationToken, UniTask> prepare, CancellationToken token)
        {
            return representation == SurfaceRepresentation.Inflated && PreparationHandler != null ? PreparationHandler(prepare, token) : prepare(token);
        }

        private async Task PrepareAsync(ActiveJob active)
        {
            AssertInput(active);
            var progress = new InlineProgress(value =>
            {
                // Native progress may originate on a worker; only store it there.
                active.NativeProgress = value;
            });
            Task preparation = m_Scene.PrepareSurfaceRepresentationAsync(active.Command.Representation, progress, active.Cancellation.Token, new Mesh3DInflationSettings(active.Command.Preset, active.Command.Options)).AsTask();
            bool prepared = false;
            try
            {
                while (!preparation.IsCompleted)
                {
                    await NextFrameAsync(active);
                    float value = Math.Max(active.LocalProgress, active.NativeProgress);
                    active.LocalProgress = value;
                    int percent = (int)(value * 100);
                    if (percent > active.LastReportedPercent)
                    {
                        active.LastReportedPercent = percent;
                        Send(V2SurfaceInflationControlKind.Progress, active, progress: value, ephemeral: true);
                        active.Progress?.Report(Progress);
                    }
                }

                await preparation;
                await UniTask.SwitchToMainThread();
                AssertInput(active);
                prepared = true;
                active.LocalProgress = 1;
                Send(V2SurfaceInflationControlKind.Progress, active, progress: 1, ephemeral: true);
            }
            finally
            {
                // Keep the scene/native inputs alive until the cancelled worker has actually stopped.
                if (!prepared) active.Cancellation.Cancel();
                try
                {
                    await preparation;
                }
                catch
                {
                }
            }
        }

        private async Task FailAsync(ActiveJob active, Exception exception)
        {
            await UniTask.SwitchToMainThread();
            if (!m_Disposed && m_Online())
            {
                try
                {
                    Send(exception is OperationCanceledException && !active.RemoteFailed ? V2SurfaceInflationControlKind.Cancel : V2SurfaceInflationControlKind.Failed, active);
                }
                catch (Exception)
                {
                    /* Preserve the original failure even when the transport is closing. */
                }
            }

            if (exception is OperationCanceledException && !active.RemoteFailed) active.Completion.TrySetCanceled();
            else active.Completion.TrySetException(active.RemoteFailed && exception is OperationCanceledException ? new InvalidOperationException("The other device could not prepare the inflated surface.", exception) : exception);
        }

        private async Task WaitAsync(Task task, ActiveJob active)
        {
            var waitClock = System.Diagnostics.Stopwatch.StartNew();
            while (!task.IsCompleted)
            {
                if (waitClock.Elapsed > TimeSpan.FromMinutes(10)) throw new TimeoutException("The other device did not complete surface preparation.");
                await NextFrameAsync(active);
            }

            active.Cancellation.Token.ThrowIfCancellationRequested();
            await task;
        }

        private async UniTask NextFrameAsync(ActiveJob active)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, active.Cancellation.Token);
            if (m_Scene == null || m_Scene.IsClosing || !m_Online() || !ReferenceEquals(active, m_Active))
                throw new OperationCanceledException("The surface job's scene or connection is no longer available.");
        }

        private Command FreezeCommand(SurfaceRepresentation representation, bool animate)
        {
            Mesh3D mesh = m_Scene.MeshManager.SelectedMesh;
            Mesh3DInflationSettings settings = Mesh3DInflationSettings.Inflated;
            if (mesh.HasInflatedRepresentation)
            {
                SurfaceInflationCacheKey key = mesh.ActiveInflatedRepresentation.CacheKey;
                settings = new Mesh3DInflationSettings(key.Preset, new SurfaceInflationOptions
                {
                    Method = key.Method, Rescale = key.Rescale, IterationCount = key.IterationCount,
                    SmoothingStrength = key.SmoothingStrength, MetricStrength = key.MetricStrength,
                    MaximumStepFraction = key.MaximumStepFraction, ConvergenceTolerance = key.ConvergenceTolerance,
                    MaximumBacktrackingSteps = key.MaximumBacktrackingSteps, FixBoundaryVertices = key.FixBoundaryVertices
                });
            }

            return new Command { Mesh = m_Boundary.ResourceCatalog.MeshReference(mesh), Part = (int)m_Scene.MeshManager.MeshPartToDisplay, Representation = representation, Animate = animate, AlgorithmVersion = Mesh3DInflationSettings.AlgorithmVersion, Preset = settings.Preset, Options = settings.Options };
        }

        private void AssertSelected(Command command)
        {
            if (!ReferenceEquals(m_Boundary.ResourceCatalog.ResolveMesh(command.Mesh), m_Scene.MeshManager.SelectedMesh) || command.Part != (int)m_Scene.MeshManager.MeshPartToDisplay)
                throw new InvalidOperationException("The selected mesh changed during surface preparation.");
        }

        private void AssertInput(ActiveJob active)
        {
            m_Boundary.ResourceCatalog.AssertPreparedRoster();
            AssertSelected(active.Command);
            Mesh3D mesh = m_Scene.MeshManager.SelectedMesh;
            AssertSurface(mesh.Both, active.AnatomicalSurface, active.GeometryVersion);
            if (mesh is LeftRightMesh3D hemispheres)
            {
                AssertSurface(hemispheres.Left, active.LeftSurface, active.LeftGeometryVersion);
                AssertSurface(hemispheres.Right, active.RightSurface, active.RightGeometryVersion);
            }
        }

        private static void AssertSurface(Surface current, Surface expected, long geometryVersion)
        {
            if (!ReferenceEquals(current, expected) || current == null || !current.IsLoaded || current.GeometryVersion != geometryVersion)
                throw new InvalidOperationException("The anatomical geometry changed during surface preparation.");
        }

        private static byte[] EncodeCommand(Command command) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(command));

        private static Command DecodeCommand(byte[] payload)
        {
            Command command = JsonConvert.DeserializeObject<Command>(new UTF8Encoding(false, true).GetString(payload), new JsonSerializerSettings { MaxDepth = 8 });
            if (command == null || string.IsNullOrEmpty(command.Mesh) || command.Mesh.Length > 256 || command.AlgorithmVersion != Mesh3DInflationSettings.AlgorithmVersion || !Enum.IsDefined(typeof(SurfaceRepresentation), command.Representation) || !Enum.IsDefined(typeof(SurfaceInflationPreset), command.Preset) || !Enum.IsDefined(typeof(SurfaceInflationMethod), command.Options.Method) || !Enum.IsDefined(typeof(SurfaceInflationRescale), command.Options.Rescale) || command.Options.IterationCount < 1 || command.Options.IterationCount > 4096)
                throw new InvalidDataException("Invalid or incompatible inflation command.");
            foreach (double value in new[] { command.Options.SmoothingStrength, command.Options.MetricStrength, command.Options.MaximumStepFraction, command.Options.ConvergenceTolerance })
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                    throw new InvalidDataException("Invalid inflation parameters.");
            return command;
        }

        private void Send(V2SurfaceInflationControlKind kind, ActiveJob active, byte[] payload = null, float progress = 0, bool ephemeral = false) => m_Send(new V2SurfaceInflationControl(kind, active.Id, kind == V2SurfaceInflationControlKind.Request ? 0 : active.Generation, active.Sequence, payload, progress), ephemeral);

        private void SendFailure(OperationId id, ulong generation) => m_Send(new V2SurfaceInflationControl(V2SurfaceInflationControlKind.Failed, id, generation), false);

        private void NotifyRemoteStarted(ActiveJob active)
        {
            RemoteJobStarted?.Invoke(active.Completion.Task);
            _ = ObserveRemoteCompletionAsync(active.Completion.Task);
        }

        private static async Task ObserveRemoteCompletionAsync(Task completion)
        {
            try
            {
                await completion;
            }
            catch
            {
                /* The requester/UI owns the reported failure. */
            }
        }

        private void RestorePreview(ActiveJob active)
        {
            if (active.PreviewStarted && m_Scene != null) m_Scene.RestoreSurfaceRepresentationPreview();
        }

        private void Release(ActiveJob active)
        {
            if (ReferenceEquals(m_Active, active)) m_Active = null;
            active.Retention.Dispose();
            active.Busy.Dispose();
            active.Cancellation.Dispose();
            if (m_Finished.Add(active.Id)) m_FinishedOrder.Enqueue(active.Id);
            while (m_FinishedOrder.Count > 64) m_Finished.Remove(m_FinishedOrder.Dequeue());
        }

        public void Cancel() => m_Active?.Cancellation.Cancel();

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            if (m_Scene != null && m_Scene.SurfaceRepresentationRequestHandler == m_Handler) m_Scene.SurfaceRepresentationRequestHandler = null;
            Cancel();
        }

        [UnityEngine.Scripting.Preserve]
        private sealed class Command
        {
            public string Mesh;
            public int Part;
            public SurfaceRepresentation Representation;
            public bool Animate;
            public int AlgorithmVersion;
            public SurfaceInflationPreset Preset;
            public SurfaceInflationOptions Options;

            [UnityEngine.Scripting.Preserve]
            public Command()
            {
            }
        }

        private sealed class InlineProgress : IProgress<float>
        {
            private readonly Action<float> m_Report;
            public InlineProgress(Action<float> report) => m_Report = report;
            public void Report(float value) => m_Report(value);
        }

        private sealed class ActiveJob
        {
            public readonly OperationId Id;
            public ulong Generation, Sequence;
            public Command Command;
            public readonly IDisposable Busy, Retention;
            public readonly CancellationTokenSource Cancellation;
            public readonly IProgress<float> Progress;
            public readonly Surface AnatomicalSurface, LeftSurface, RightSurface;
            public readonly long GeometryVersion, LeftGeometryVersion, RightGeometryVersion;
            public readonly TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource<bool> Ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource<bool> Transition = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource<bool> Transitioned = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource<ulong> Commit = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource<bool> Committed = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource<bool> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public Task Work;
            public volatile float NativeProgress;
            public float LocalProgress, RemoteProgress;
            public int LastReportedPercent = -1;
            public bool RemoteFailed, CommitSent, PreviewStarted;

            public ActiveJob(OperationId id, ulong generation, ulong sequence, Command command, IDisposable busy, IDisposable retention, IProgress<float> progress, CancellationToken token, Mesh3D anatomicalMesh)
            {
                AnatomicalSurface = anatomicalMesh.Both;
                GeometryVersion = AnatomicalSurface.GeometryVersion;
                if (anatomicalMesh is LeftRightMesh3D hemispheres)
                {
                    LeftSurface = hemispheres.Left;
                    RightSurface = hemispheres.Right;
                    LeftGeometryVersion = LeftSurface.GeometryVersion;
                    RightGeometryVersion = RightSurface.GeometryVersion;
                }

                Id = id;
                Generation = generation;
                Sequence = sequence;
                Command = command;
                Busy = busy;
                Retention = retention;
                Progress = progress;
                Cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
            }
        }
    }
}
