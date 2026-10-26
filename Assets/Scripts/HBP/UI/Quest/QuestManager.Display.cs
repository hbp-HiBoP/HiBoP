using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;

namespace HBP.Quest.Desktop
{
    public sealed partial class QuestManager
    {
        public async UniTask RetryDisplayScenesAsync(Base3DScene[] scenes, CancellationToken token, bool showResults = true)
        {
            var pending = scenes.Where(scene => scene != null && !scene.IsClosing && !IsSceneSynchronized(scene)).Distinct().ToArray();
            if (pending.Length == 0) return;
            if (!IsPaired)
            {
                SetStatus("Reconnect Quest before retrying.");
                return;
            }

            await new HBP.UI.Main.SceneDisplayOperation(pending.Select(scene => scene.Visualization).ToArray(), pending).RunAsync(token, showResults);
        }

        private DisplayReservation displayReservation;
        private readonly HashSet<Visualization> locallyOpening = new();
        private bool IsDisplayReserved(Base3DScene scene) => locallyOpening.Contains(scene.Visualization) || displayReservation?.Contains(scene.Visualization) == true;
        internal static bool HasDisplayPresentation(Base3DScene scene) => IsInitialized && Instance.IsDisplayReserved(scene);

        public IDisposable HoldAutomaticScenes(Visualization[] visualizations)
        {
            if (visualizations.Any(locallyOpening.Contains)) throw new InvalidOperationException("These visualizations are already being displayed.");
            locallyOpening.UnionWith(visualizations);
            return new LocalDisplayScope(this, visualizations);
        }

        private sealed class LocalDisplayScope : IDisposable
        {
            private QuestManager owner;
            private readonly Visualization[] visualizations;

            internal LocalDisplayScope(QuestManager owner, Visualization[] visualizations)
            {
                this.owner = owner;
                this.visualizations = visualizations;
            }

            public void Dispose()
            {
                owner?.locallyOpening.ExceptWith(visualizations);
                owner = null;
            }
        }

        public DisplayReservation ReserveDisplay(IEnumerable<Visualization> visualizations, bool explicitRecovery = false) => ReserveDisplayCore(visualizations, explicitRecovery, false);

        private DisplayReservation ReserveDisplayCore(IEnumerable<Visualization> visualizations, bool explicitRecovery, bool ownsOperation)
        {
            if (!IsPaired || !autoSync && !explicitRecovery || globals == null) return null;
            if (HasPendingDisplayDependencies(ownsOperation))
                throw new InvalidOperationException("Wait for the current Quest operation before displaying visualizations.");
            return displayReservation = new DisplayReservation(this, visualizations.ToArray());
        }

        private bool HasPendingDisplayDependencies(bool ownsOperation) => displayReservation != null || busy && !ownsOperation || restoringScenes || reloadingScenes || !inventoryWork.IsCompleted || publications.Values.Any(value => value.HasPendingWork);

        public async UniTask<DisplayReservation> ReserveDisplayAsync(IEnumerable<Visualization> visualizations, CancellationToken token, bool explicitRecovery = false, CancellationToken operationOwner = default)
        {
            await UniTask.SwitchToMainThread(token);
            if (!IsPaired || globals == null || !autoSync && !explicitRecovery) return null;
            string target = ConnectedId, context = globals.Context.Id;
            long generation = sessionGeneration;
            bool sharing = autoSync;
            bool SameSession() => IsPaired && ConnectedId == target && globals?.Context.Id == context && sessionGeneration == generation && autoSync == sharing;
            bool OwnsOperation() => explicitRecovery && operationOwner.CanBeCanceled && operation != null && operation.Token == operationOwner;
            using var wait = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token);
            // Join shared operations without cancelling them when this Display is cancelled.
            await UniTask.WaitUntil(() => !SameSession() || !HasPendingDisplayDependencies(OwnsOperation()), cancellationToken: wait.Token);
            wait.Token.ThrowIfCancellationRequested();
            if (!SameSession()) throw new InvalidOperationException("The Quest connection changed while Display was waiting. Retry synchronization explicitly.");
            return ReserveDisplayCore(visualizations, explicitRecovery, OwnsOperation());
        }

        public void RequireSceneSynchronizationRetry(Base3DScene scene, string message)
        {
            var entry = GetPublication(scene);
            entry.UserRetryRequired = true;
            entry.Error = message;
            OnChanged.Invoke();
        }

        /// <summary>One Display owns admission before scenes enter Module3DMain's collection.</summary>
        public sealed class DisplayReservation : IDisposable
        {
            private readonly QuestManager owner;
            private readonly Visualization[] visualizations;
            private readonly string target, context;
            private readonly long generation;
            private readonly bool automaticSharing;
            private readonly string batch = Guid.NewGuid().ToString("N");
            private bool disposed;
            private string activeVisualization;
            private long nextDisplayRevision, acceptedDisplayRevision;
            private readonly HashSet<Publication> owned = new();
            public string ReceivingVisualization { get; private set; }
            public float ReceivingProgress { get; private set; }
            public SceneLoadingStatus SourceStatus { get; set; } = SceneLoadingStatus.From(0, new LoadingText("Preparing scene"));
            public SceneLoadingStatus TransferStatus { get; private set; }
            public SceneLoadingStatus ReceivingStatus { get; private set; }
            public LoadingText RemoteText => (ReceivingStatus ?? TransferStatus)?.ToText(visualizations.Select(value => value.ID).ToArray(), visualizations.Select(value => value.Name).ToArray());
            public LoadingText SourceText => SourceStatus.ToText(visualizations.Select(value => value.ID).ToArray(), visualizations.Select(value => value.Name).ToArray());
            internal HBP.UI.Tools.LoadingManager.PresentationHandle Presentation { get; set; }

            internal DisplayReservation(QuestManager owner, Visualization[] visualizations)
            {
                this.owner = owner;
                this.visualizations = visualizations;
                target = owner.ConnectedId;
                context = owner.globals.Context.Id;
                generation = owner.sessionGeneration;
                automaticSharing = owner.autoSync;
            }

            internal bool Contains(Visualization visualization) => !disposed && visualizations.Contains(visualization);

            public bool CanFinish => !disposed && owner.IsPaired && owner.ConnectedId == target && owner.globals?.Context.Id == context && owner.sessionGeneration == generation;

            // The cancellation decision outlives the reservation, but is pinned to its session.
            internal bool NeedsCancellationResolution => automaticSharing && owner.autoSync && owner.IsPaired && owner.ConnectedId == target && owner.globals?.Context.Id == context && owner.sessionGeneration == generation;
            public bool IsCurrent => CanFinish && owner.autoSync == automaticSharing;

            public void Validate()
            {
                if (!IsCurrent) throw new InvalidOperationException("The Quest connection or scene-sharing preference changed. Retry the incomplete visualizations explicitly.");
            }

            public void RequireRetry(Base3DScene scene, string message) => owner.RequireSceneSynchronizationRetry(scene, message);

            public Task<bool> SendAsync(Base3DScene scene, Action<float, float, LoadingText> progress, CancellationToken token)
            {
                Validate();
                var entry = owner.GetPublication(scene);
                if (entry.HasPendingWork && entry.DisplayOwner != this) return entry.Work;
                if (entry.DisplayOwner == this && entry.DisplayCompletion != null)
                    return entry.DisplaySend ??= CompleteSendAsync(entry, progress, token);
                entry.Work = SendOwnedAsync(entry, progress, token);
                return entry.Work;
            }

            public Task<bool> PrepareAsync(Base3DScene scene, Action<float, float, LoadingText> progress, CancellationToken token, long maximumBytes)
            {
                Validate();
                var entry = owner.GetPublication(scene);
                bool serialFallback = entry.HasPendingWork && entry.DisplayOwner == this && entry.PreparationStop == null && entry.DisplaySend == null;
                if (entry.HasPendingWork && !serialFallback)
                    return entry.DisplayOwner == this ? entry.Preparation : Task.FromException<bool>(new InvalidOperationException("Another operation owns this visualization."));
                entry.DisplayOwner = this;
                if (!serialFallback) entry.DisplayCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
                entry.DisplaySend = null;
                entry.Work = entry.DisplayCompletion.Task;
                entry.PreparationStop = CancellationTokenSource.CreateLinkedTokenSource(token, owner.lifetime.Token, entry.Lifetime.Token);
                entry.Stop = entry.PreparationStop;
                owned.Add(entry);
                return entry.Preparation = PrepareOwnedAsync(entry, progress, maximumBytes);
            }

            private async Task<bool> PrepareOwnedAsync(Publication entry, Action<float, float, LoadingText> progress, long maximumBytes)
            {
                try
                {
                    Validate();
                    if (entry.Binding != null) return true;
                    await owner.PrepareSceneDependenciesAsync(entry.Scene, progress, entry.PreparationStop.Token, reportAtlasErrors: false);

                    await UniTask.SwitchToMainThread(entry.PreparationStop.Token);
                    Validate();
                    await owner.PrepareSceneOfferAsync(entry, true, entry.PreparationStop.Token, progress, maximumBytes, () => UpdateRemoteAsync(0, false, entry.PreparationStop.Token));
                    await UniTask.SwitchToMainThread(entry.PreparationStop.Token);
                    Validate();
                    entry.PreparedForDisplay = true;
                    return true;
                }
                catch (ScenePreparationBudgetExceededException)
                {
                    await AbandonPreparationAsync(entry, false);
                    throw;
                }
                catch (Exception error)
                {
                    await AbandonPreparationAsync(entry);
                    RequireRetry(entry.Scene, TransferError.Describe(error));
                    return false;
                }
            }

            private async Task<bool> CompleteSendAsync(Publication entry, Action<float, float, LoadingText> progress, CancellationToken token)
            {
                bool success = false;
                try
                {
                    if (entry.Preparation != null && !await entry.Preparation) return false;
                    success = await SendOwnedAsync(entry, progress, token);
                    return success;
                }
                finally
                {
                    await UniTask.SwitchToMainThread();
                    entry.PreparationStop?.Dispose();
                    entry.PreparationStop = null;
                    entry.DisplayCompletion.TrySetResult(success);
                    owner.OnChanged.Invoke();
                }
            }

            private async Task AbandonPreparationAsync(Publication entry, bool finishPublication = true)
            {
                await UniTask.SwitchToMainThread();
                entry.PreparationStop?.Cancel();
                if (entry.Offer != null && !entry.OfferWasSent)
                {
                    entry.Owner?.Dispose();
                    entry.Owner = null;
                    if (entry.Offer != null) await entry.Offer.DisposeAsync();
                    entry.Offer = null;
                }
                else if (entry.Offer == null)
                {
                    entry.Owner?.Dispose();
                    entry.Owner = null;
                }

                entry.PreparedForDisplay = false;
                entry.PreparationStop?.Dispose();
                entry.PreparationStop = null;
                entry.Stop = null;
                if (finishPublication) entry.DisplayCompletion?.TrySetResult(false);
            }

            public async Task DrainPreparedAsync()
            {
                foreach (var entry in owned.Where(value => value.DisplayCompletion?.Task.IsCompleted == false).ToArray())
                    await AbandonPreparationAsync(entry);
            }

            private async Task<bool> SendOwnedAsync(Publication entry, Action<float, float, LoadingText> progress, CancellationToken token)
            {
                using var stop = CancellationTokenSource.CreateLinkedTokenSource(token, owner.lifetime.Token, entry.Lifetime.Token, entry.PreparationStop?.Token ?? CancellationToken.None);
                var unity = SynchronizationContext.Current;
                bool succeeded = false;
                entry.Stop = stop;
                activeVisualization = entry.VisualizationId;
                TransferStatus = SceneLoadingStatus.From(0, new LoadingText(entry.Binding != null ? "Synchronizing scene" : entry.Offer != null ? entry.Offer.IsPreparingResources ? "Encoding scene data" : "Scene ready to send" : "Preparing scene for transfer"), entry.VisualizationId);
                ReceivingStatus = null;
                entry.Error = null;
                try
                {
                    Validate();
                    await UpdateRemoteAsync(0, false, stop.Token);
                    if (entry.Binding != null)
                    {
                        if (entry.Owner?.IsLive == true && entry.Owner.IsInitialRenderingReady)
                            await entry.Owner.WaitInitialRenderingAsync(stop.Token);
                        else
                        {
                            entry.UserRetryRequired = false;
                            await owner.RestoreScenesAsync(stop.Token, entry.Scene);
                        }

                        await UniTask.SwitchToMainThread(stop.Token);
                        Validate();
                        if (entry.Owner?.IsLive != true || !entry.Owner.IsInitialRenderingReady)
                            throw new InvalidOperationException(entry.Error ?? "The published Quest visualization needs explicit recovery.");
                        entry.UserRetryRequired = false;
                        entry.Error = null;
                        progress(1, 0, new LoadingText("Visualization ready on Quest"));
                        succeeded = true;
                        return true;
                    }

                    void Update(float value, float duration, LoadingText text)
                    {
                        if (SynchronizationContext.Current == unity)
                        {
                            if (!stop.IsCancellationRequested && !disposed) progress(value, duration, text);
                        }
                        else
                            unity.Post(_ =>
                            {
                                if (!stop.IsCancellationRequested && !disposed) progress(value, duration, text);
                            }, null);
                    }

                    bool result = await owner.SendSceneCoreAsync(entry, true, stop.Token, Update, Validate, reportAtlasErrors: false, reportStatus: status =>
                    {
                        TransferStatus = status;
                        if (!status.Transfer && status.Progress >= .97f) ReceivingStatus = null;
                    }, announcePreparation: () => UpdateRemoteAsync(0, false, stop.Token));
                    await UniTask.SwitchToMainThread();
                    Validate();
                    succeeded = result;
                    return result;
                }
                catch (Exception error)
                {
                    await UniTask.SwitchToMainThread();
                    RequireRetry(entry.Scene, stop.IsCancellationRequested || error is OperationCanceledException ? "Display cancelled. Retry to synchronize this visualization." : TransferError.Describe(error));
                    return false;
                }
                finally
                {
                    await UniTask.SwitchToMainThread();
                    if (succeeded && SourceStatus?.VisualizationId == entry.VisualizationId)
                        SourceStatus = SceneLoadingStatus.From(1, new LoadingText("Scene ready"), entry.VisualizationId);
                    TransferStatus = null;
                    ReceivingStatus = null;
                    activeVisualization = null;
                    entry.Stop = null;
                    owner.OnChanged.Invoke();
                }
            }

            public async Task UpdateRemoteAsync(float progress, bool finished, CancellationToken token, string[] incomplete = null)
            {
                if (finished ? !CanFinish : !IsCurrent) throw new InvalidOperationException("The Display target connection has changed.");
                var sourceStatus = SourceStatus;
                if (sourceStatus?.Prefix == "Encoding scene data" && owned.FirstOrDefault(entry => entry.VisualizationId == sourceStatus.VisualizationId)?.Offer is { IsPreparingResources: false } && ReferenceEquals(sourceStatus, SourceStatus))
                    SourceStatus = SceneLoadingStatus.From(.20f, new LoadingText("Scene ready to send"), sourceStatus.VisualizationId);
                var state = new SceneDisplayBatchState { Id = batch, Visualizations = visualizations.Select(value => value.ID).ToArray(), Names = visualizations.Select(value => value.Name).ToArray(), Progress = progress, Finished = finished, ActiveVisualizationId = activeVisualization ?? SourceStatus?.VisualizationId, IncompleteVisualizations = incomplete ?? Array.Empty<string>(), SourceStatus = SourceStatus, TransferStatus = TransferStatus };
                state.Validate();
                long revision = Interlocked.Increment(ref nextDisplayRevision);
                var request = new SessionControlRequest(Guid.ParseExact(context, "N"), generation, Guid.NewGuid(), SessionControlKind.DisplayBatch, revision: revision, body: SceneSessionState.Encode(state));
                var response = await QuestPairing.SendSessionControlAsync(owner.endpoint, owner.pin, owner.credential, request, token);
                await UniTask.SwitchToMainThread(token);
                if (finished ? !CanFinish : !IsCurrent) throw new InvalidOperationException("The Display target connection has changed.");
                if (!response.Applied) throw new InvalidOperationException(response.Message);
                if (!finished)
                {
                    if (revision < acceptedDisplayRevision) return;
                    acceptedDisplayRevision = revision;
                    var remote = SceneSessionState.Decode<SceneDisplayBatchState>(response.GetBody());
                    remote.Validate();
                    if (remote.Id != batch || !remote.Visualizations.SequenceEqual(state.Visualizations)) throw new InvalidOperationException("Wrong Quest Display progress identity.");
                    ReceivingVisualization = remote.ActiveVisualizationId;
                    ReceivingProgress = remote.ReceivingProgress;
                    bool currentPhase = state.ActiveVisualizationId == (activeVisualization ?? SourceStatus?.VisualizationId) && ReferenceEquals(state.TransferStatus, TransferStatus);
                    ReceivingStatus = currentPhase && remote.ActiveVisualizationId == state.ActiveVisualizationId ? remote.ReceivingStatus : null;
                }
            }

            public void Dispose()
            {
                disposed = true;
                if (ReferenceEquals(owner.displayReservation, this)) owner.displayReservation = null;
                foreach (var entry in owned)
                    if (entry.DisplayOwner == this && entry.DisplayCompletion?.Task.IsCompleted == true)
                    {
                        entry.DisplayOwner = null;
                        entry.DisplayCompletion = null;
                        entry.Preparation = null;
                        entry.DisplaySend = null;
                    }

                owned.Clear();
            }
        }
    }
}
