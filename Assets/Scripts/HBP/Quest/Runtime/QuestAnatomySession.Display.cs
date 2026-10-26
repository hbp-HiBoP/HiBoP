using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.Quest
{
    public sealed partial class QuestAnatomySession
    {
        private SceneDisplayBatchState displayBatch;
        private float displayBatchExpires;
        private float displayReceivingProgress;
        private long displayRevision;
        private Action<float, float, LoadingText> displayProgress;
        private TaskCompletionSource<bool> displayFinished;
        private Task displayPresentation;
        private Task displayDrain = Task.CompletedTask;
        private SceneLoadingStatus displayReceivingStatus;
        private readonly Queue<string> finishedDisplayBatches = new();
        private static readonly HashSet<string> blockedDisplayVisualizations = new(StringComparer.Ordinal);

        public static bool CanInteractWith(Base3DScene scene) => scene != null && !scene.IsClosing && !blockedDisplayVisualizations.Contains(scene.Visualization.ID);

        private async Task<SessionControlResponse> ApplyDisplayBatchAsync(SessionControlRequest request, CancellationToken token)
        {
            var state = SceneSessionState.Decode<SceneDisplayBatchState>(request.GetBody());
            state.Validate();
            if (finishedDisplayBatches.Contains(state.Id))
                return new(request.OperationId, SessionControlStatus.Stale, "Display batch already finished.");
            if (displayBatch != null && displayBatch.Id != state.Id || !displayDrain.IsCompleted)
                return new(request.OperationId, SessionControlStatus.Rejected, "Another Display batch is still active.");
            if (displayBatch != null && request.Revision < displayRevision)
                return new(request.OperationId, SessionControlStatus.Applied, body: SceneSessionState.Encode(displayBatch));
            if (state.Finished)
            {
                if (displayBatch?.Id == state.Id)
                {
                    displayDrain = DrainDisplayBatchAsync(state.IncompleteVisualizations, state.Progress == 1);
                    await displayDrain;
                }
                else RememberDisplayFinished(state.Id);

                return new(request.OperationId, SessionControlStatus.Applied);
            }

            if (displayBatch == null)
            {
                displayRevision = 0;
                displayReceivingProgress = 0;
                displayReceivingStatus = null;
                displayBatch = state;
                blockedDisplayVisualizations.UnionWith(state.Visualizations);
                displayFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);
                displayPresentation = PresentDisplayBatchAsync();
            }
            else if (!displayBatch.Visualizations.SequenceEqual(state.Visualizations))
                return new(request.OperationId, SessionControlStatus.Rejected, "Display batch scenes changed.");

            displayRevision = request.Revision;
            if (displayBatch.ActiveVisualizationId != state.ActiveVisualizationId)
            {
                displayReceivingProgress = 0;
                displayReceivingStatus = null;
            }

            displayBatch.Progress = Math.Max(displayBatch.Progress, state.Progress);
            displayBatch.ActiveVisualizationId = state.ActiveVisualizationId;
            displayBatch.ReceivingProgress = displayReceivingProgress;
            displayBatch.Names = state.Names;
            displayBatch.SourceStatus = state.SourceStatus;
            displayBatch.TransferStatus = state.TransferStatus;
            if (loadingProgress == null && state.TransferStatus is { Transfer: false, Progress: >= .97f }) displayReceivingStatus = null;
            displayBatch.ReceivingStatus = displayReceivingStatus;
            displayBatchExpires = Time.realtimeSinceStartup + 30;
            RenderDisplayProgress();
            return new(request.OperationId, SessionControlStatus.Applied, body: SceneSessionState.Encode(displayBatch));
        }

        private async Task PresentDisplayBatchAsync()
        {
            await LoadingManager.LoadAsync(async update =>
            {
                await UniTask.SwitchToMainThread();
                displayProgress = update;
                RenderDisplayProgress();
                await displayFinished.Task;
            });
        }

        private void RenderDisplayProgress()
        {
            if (displayBatch == null) return;
            var status = displayReceivingStatus ?? displayBatch.TransferStatus ?? displayBatch.SourceStatus;
            var text = status?.ToText(displayBatch.Visualizations, displayBatch.Names, receiving: true) ?? new LoadingText("Preparing scene");
            displayProgress?.Invoke(displayBatch.Progress, .35f, text);
        }

        private async Task DrainDisplayBatchAsync(string[] incomplete = null, bool succeeded = false)
        {
            var batch = displayBatch;
            if (batch == null) return;
            // Expiry/disconnection cancels the owned receive before releasing its interaction gate.
            if (Time.realtimeSinceStartup >= displayBatchExpires || !IsSessionConnected || incomplete?.Contains(batch.ActiveVisualizationId) == true) reception?.Cancel();
            await loadingGate.WaitAsync();
            loadingGate.Release();
            await UniTask.SwitchToMainThread();
            try
            {
                var failed = incomplete ?? batch.Visualizations;
                foreach (var presentation in view.Scenes.Where(value => failed.Contains(value.Scene.Visualization.ID)).ToArray())
                {
                    if (deliveries.TryGetValue(presentation.TransferId, out var entry))
                    {
                        entry.Replica?.Dispose();
                        entry.Replica = null;
                    }

                    try
                    {
                        await presentation.Scene.CapturePreparedAsync(() => 0, CancellationToken.None);
                    }
                    catch (Exception error)
                    {
                        Debug.LogWarning("Quest Display preparation stopped: " + error.Message);
                    }
                }

                if (succeeded) displayProgress?.Invoke(1, 0, new LoadingText(batch.Visualizations.Length == 1 ? "Scene ready" : $"{batch.Visualizations.Length} scenes ready"));
            }
            finally
            {
                displayFinished.TrySetResult(true);
                try
                {
                    await displayPresentation;
                }
                finally
                {
                    blockedDisplayVisualizations.ExceptWith(batch.Visualizations);
                    RememberDisplayFinished(batch.Id);
                    displayBatch = null;
                    displayProgress = null;
                    displayReceivingStatus = null;
                }
            }
        }

        private void RememberDisplayFinished(string id)
        {
            finishedDisplayBatches.Enqueue(id);
            while (finishedDisplayBatches.Count > 64) finishedDisplayBatches.Dequeue();
        }

        private void TickDisplayBatch()
        {
            if (displayBatch != null && displayDrain.IsCompleted && (Time.realtimeSinceStartup >= displayBatchExpires || !IsSessionConnected))
                displayDrain = DrainDisplayBatchAsync();
        }

        private void StopDisplayBatch()
        {
            if (displayBatch != null && displayDrain.IsCompleted)
                displayDrain = DrainDisplayBatchAsync();
        }
    }
}
