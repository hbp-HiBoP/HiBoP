using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Data.Tools;
using HBP.Quest.Desktop;
using HBP.Transfer.Transport;
using HBP.Transfer.Scene;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Main
{
    /// <summary>Owns one Display presentation and drains its local and remote work before releasing it.</summary>
    internal sealed class SceneDisplayOperation
    {
        private const long MaximumAheadSnapshotBytes = 512L * 1024 * 1024;
        private readonly Visualization[] requested;
        private readonly Base3DScene[] existingScenes;
        private readonly List<Base3DScene> loaded = new();
        private readonly Dictionary<Base3DScene, string> failures = new();
        private readonly float[] remoteProgress;
        private float localProgress, percentage;
        private string remoteError;
        private Action<float, float, LoadingText> update;
        private CancellationTokenSource remoteStop;
        private Task remoteWork = Task.CompletedTask;
        private QuestManager.DisplayReservation reservation;
        private Visualization loadingVisualization;
        private bool loadingLocally;

        internal SceneDisplayOperation(Visualization[] requested, Base3DScene[] existingScenes = null)
        {
            this.requested = requested;
            this.existingScenes = existingScenes;
            remoteProgress = new float[requested.Length];
        }

        internal async UniTask RunAsync(CancellationToken token, bool showResults = true)
        {
            using var awake = PerformanceManager.IsInitialized ? UnityEngine.Object.FindAnyObjectByType<PerformanceManager>().KeepAwake() : null;
            using var localAdmission = QuestManager.IsInitialized ? QuestManager.Instance.HoldAutomaticScenes(requested) : null;
            LoadingManager.PresentationHandle presentation = null;
            Exception localError = null;
            bool cancelled = false;
            try
            {
                await LoadingManager.PresentAsync(async (progress, stop) =>
                {
                    update = progress;
                    using var remote = CancellationTokenSource.CreateLinkedTokenSource(stop);
                    using var monitorStop = new CancellationTokenSource();
                    remoteStop = remote;
                    Task monitor = Task.CompletedTask;
                    try
                    {
                        if (QuestManager.IsInitialized)
                        {
                            progress(0, 0, new LoadingText("Waiting for current Quest operations"));
                            try
                            {
                                reservation = await QuestManager.Instance.ReserveDisplayAsync(requested, stop, existingScenes != null, operationOwner: token);
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception error)
                            {
                                remoteError = TransferError.Describe(error);
                                remote.Cancel();
                            }

                            if (reservation != null)
                            {
                                reservation.Presentation = presentation;
                                reservation.UserCancellation = stop;
                            }
                        }

                        if (reservation != null)
                        {
                            try
                            {
                                using var begin = CancellationTokenSource.CreateLinkedTokenSource(stop);
                                begin.CancelAfter(TimeSpan.FromSeconds(10));
                                await reservation.UpdateRemoteAsync(0, false, begin.Token);
                                monitor = MonitorRemoteAsync(monitorStop.Token);
                            }
                            catch (Exception error)
                            {
                                reservation.RecoverConnection(error);
                                remoteError = TransferError.Describe(error);
                                remote.Cancel();
                            }
                        }

                        if (existingScenes != null)
                        {
                            localProgress = 1;
                            foreach (var scene in existingScenes) await SceneReadyAsync(scene, stop);
                        }
                        else
                        {
                            loadingLocally = true;
                            await Module3DMain.LoadWithResultsAsync(requested, (value, duration, text) =>
                            {
                                localProgress = Math.Max(localProgress, value);
                                if (reservation != null) reservation.SourceStatus = SceneLoadingStatus.From(value, text, loadingVisualization?.ID);
                                Report(text);
                            }, stop, SceneReadyAsync, visualization => loadingVisualization = visualization);
                            loadingLocally = false;
                        }

                        await remoteWork;
                        stop.ThrowIfCancellationRequested();
                        localProgress = 1;
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled = true;
                    }
                    catch (Exception error)
                    {
                        Debug.LogException(error);
                        localError = error;
                    }
                    finally
                    {
                        loadingLocally = false;
                        remote.Cancel();
                        monitorStop.Cancel();
                        await remoteWork;
                        await monitor;
                        if (reservation != null) await reservation.DrainPreparedAsync();
                        await UniTask.SwitchToMainThread();
                        if (reservation != null)
                        {
                            foreach (var scene in loaded.Where(scene => !QuestManager.Instance.IsSceneSynchronized(scene)))
                            {
                                if (!stop.IsCancellationRequested && reservation.DeferScene(scene))
                                {
                                    failures.Remove(scene);
                                    continue;
                                }

                                string message = failures.TryGetValue(scene, out var failure) ? failure : remoteError ?? "Display interrupted. Retry to synchronize this visualization.";
                                failures[scene] = message;
                                reservation.RequireRetry(scene, message);
                            }

                            if (reservation.CanFinish)
                            {
                                using var end = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                                try
                                {
                                    bool success = !cancelled && localError == null && failures.Count == 0 && remoteError == null;
                                    await reservation.UpdateRemoteAsync(success ? 1 : percentage, true, end.Token, failures.Keys.Select(scene => scene.Visualization.ID).ToArray());
                                }
                                catch (Exception error)
                                {
                                    if (reservation.RecoverConnection(error))
                                    {
                                        foreach (var scene in loaded)
                                            if (reservation.DeferScene(scene))
                                                failures.Remove(scene);
                                    }
                                    else Debug.LogException(error);

                                    remoteError = "Quest completion could not be confirmed: " + TransferError.Describe(error);
                                    foreach (var scene in loaded)
                                    {
                                        if (!stop.IsCancellationRequested && reservation.DeferScene(scene)) continue;
                                        failures[scene] = remoteError;
                                        reservation.RequireRetry(scene, remoteError);
                                    }
                                }
                            }
                        }

                        cancelled |= stop.IsCancellationRequested;
                        if (!cancelled && localError == null && failures.Count == 0 && remoteError == null && reservation?.TransportInterrupted != true)
                            Report(new LoadingText("Visualizations ready"), true);
                    }
                }, token, handle => { presentation = handle; });
            }
            finally
            {
                reservation?.Dispose();
            }

            // Open the modal in the same main-thread continuation that releases the loading
            // blocker. Release admission before any explicit retry acquires a new reservation.
            localAdmission?.Dispose();
            if (!showResults || token.IsCancellationRequested) return;
            if (cancelled && reservation?.NeedsCancellationResolution == true)
            {
                await ResolveCancellationAsync(token);
                return;
            }

            if (localError != null)
            {
                var failure = localError as HBPException ?? new VisualizationLoadException(string.Join(", ", requested.Select(visualization => visualization.Name)), localError);
                string message = failure.Message;
                if (loaded.Count > 0) message += "\n\nVisualizations that finished opening on Desktop remain available.";
                await DialogBoxManager.OpenAsync(DialogBoxType.Error, failure.Title, message, "OK");
            }
            else if (failures.Count != 0 && !cancelled)
            {
                string message = string.Join("\n", failures.Select(item => item.Key.Visualization.Name + ": " + item.Value));
                int answer = await DialogBoxManager.OpenAsync(DialogBoxType.Error, "Quest synchronization incomplete", message + "\nDesktop visualizations remain usable.", "Retry", "Keep Desktop scenes");
                if (answer == 0) await QuestManager.Instance.RetryDisplayScenesAsync(failures.Keys.ToArray(), token);
            }
        }

        private async UniTask ResolveCancellationAsync(CancellationToken token)
        {
            var manager = QuestManager.Instance;
            Base3DScene[] Pending() => loaded.Where(scene => scene != null && !scene.IsClosing && !manager.IsSceneSynchronized(scene)).ToArray();
            var pending = Pending();
            bool attempted = false;
            while (pending.Length != 0 && reservation.NeedsCancellationResolution && !token.IsCancellationRequested)
            {
                string message = "Automatic scene synchronization is enabled, but these visualizations are not ready on Quest:\n" + string.Join("\n", pending.Select(scene => scene.Visualization.Name)) + "\n\nChoose Transfer to synchronize them before continuing, or Close to close these Desktop visualizations and discard their incomplete Quest copies.";
                if (attempted)
                    message += "\n\nThe transfer did not complete. " + (manager.GetSceneSynchronizationError(pending[0]) ?? "Try again or close the visualizations.");
                int answer = await DialogBoxManager.OpenAsync(DialogBoxType.Warning, "Scene synchronization required", message, "Transfer", "Close");
                if (answer == 1)
                {
                    await LoadingManager.LoadAsync(async progress =>
                    {
                        await UniTask.SwitchToMainThread();
                        progress(0, 0, new LoadingText("Closing incomplete visualizations"));
                        foreach (var scene in pending)
                            if (scene != null && !scene.IsClosing)
                                await Module3DMain.RemoveSceneAsync(scene);
                    });
                    return;
                }

                if (!reservation.NeedsCancellationResolution) break;
                await manager.RetryDisplayScenesAsync(pending, token, showResults: false);
                attempted = true;
                pending = Pending();
            }

            if (pending.Length != 0 && !token.IsCancellationRequested)
                await DialogBoxManager.OpenAsync(DialogBoxType.Warning, "Quest synchronization interrupted", "The Quest connection or automatic scene synchronization changed. Desktop visualizations have been kept. Reconnect Quest and use Send visualization to Quest to retry.", "OK");
        }

        private async UniTask SceneReadyAsync(Base3DScene scene, CancellationToken token)
        {
            loaded.Add(scene);
            if (reservation != null) reservation.SourceStatus = SceneLoadingStatus.From(localProgress, new LoadingText("Preparing scene display"), scene.Visualization.ID);
            await scene.PrepareRenderingAsync(token);
            if (reservation == null)
            {
                if (remoteError != null)
                {
                    failures[scene] = remoteError;
                    QuestManager.Instance.RequireSceneSynchronizationRetry(scene, remoteError);
                }

                return;
            }

            int index = Array.IndexOf(requested, scene.Visualization);

            void Progress(float value, float duration, LoadingText text)
            {
                remoteProgress[index] = Math.Max(remoteProgress[index], value);
                reservation.SourceStatus = SceneLoadingStatus.From(value, text, scene.Visualization.ID);
                Report(text);
            }

            if (remoteError == null && !remoteStop.IsCancellationRequested)
            {
                bool prepared;
                try
                {
                    // One snapshot ahead, with an explicit retained-size budget. A larger
                    // visualization falls back to serial preparation after the current send.
                    prepared = await reservation.PrepareAsync(scene, Progress, remoteStop.Token, remoteWork.IsCompleted ? long.MaxValue : MaximumAheadSnapshotBytes);
                }
                catch (HBP.Transfer.Scene.ScenePreparationBudgetExceededException)
                {
                    await remoteWork;
                    await UniTask.SwitchToMainThread(token);
                    prepared = remoteError == null && await reservation.PrepareAsync(scene, Progress, remoteStop.Token, long.MaxValue);
                }

                if (!prepared)
                {
                    remoteError ??= QuestManager.Instance.GetSceneSynchronizationError(scene) ?? "Visualization preparation interrupted.";
                    failures[scene] = remoteError;
                }
            }

            await remoteWork;
            await UniTask.SwitchToMainThread(token);
            if (remoteError != null || remoteStop.IsCancellationRequested)
            {
                failures[scene] = remoteError ?? "Quest synchronization interrupted.";
                return;
            }

            remoteWork = SendAsync(scene, token);
        }

        private async Task SendAsync(Base3DScene scene, CancellationToken token)
        {
            try
            {
                int index = Array.IndexOf(requested, scene.Visualization);
                bool success = await reservation.SendAsync(scene, (value, duration, text) =>
                {
                    remoteProgress[index] = Math.Max(remoteProgress[index], value);
                    Report(text);
                }, remoteStop.Token);
                await UniTask.SwitchToMainThread();
                if (!success)
                {
                    remoteError = QuestManager.Instance.GetSceneSynchronizationError(scene) ?? "Quest synchronization failed.";
                    failures[scene] = remoteError;
                }
            }
            catch (Exception error)
            {
                await UniTask.SwitchToMainThread();
                if (!reservation.RecoverConnection(error)) Debug.LogException(error);
                remoteError = TransferError.Describe(error);
                failures[scene] = remoteError;
            }
        }

        private async Task MonitorRemoteAsync(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    reservation.Validate();
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                    timeout.CancelAfter(TimeSpan.FromSeconds(10));
                    await reservation.UpdateRemoteAsync(percentage, false, timeout.Token);
                    int index = Array.FindIndex(requested, scene => scene.ID == reservation.ReceivingVisualization);
                    if (index >= 0)
                    {
                        float preparation = Mathf.Clamp01((reservation.ReceivingProgress - .8f) / .2f);
                        if (preparation > 0) remoteProgress[index] = Math.Max(remoteProgress[index], .85f + .12f * preparation);
                    }

                    Report(reservation.RemoteText ?? new LoadingText("Preparing scene"));
                    await UniTask.Delay(TimeSpan.FromMilliseconds(250), DelayType.Realtime, cancellationToken: token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception error)
            {
                await UniTask.SwitchToMainThread();
                reservation.RecoverConnection(error);
                remoteError = TransferError.Describe(error);
                remoteStop.Cancel();
            }
        }

        private void Report(LoadingText text, bool complete = false)
        {
            float value = reservation == null ? localProgress : .15f * Mathf.Clamp01(localProgress) + .85f * remoteProgress.Average();
            percentage = complete ? 1 : Math.Max(percentage, Math.Min(.99f, value));
            if (!complete && reservation != null)
                text = loadingLocally ? reservation.SourceText : reservation.RemoteText ?? reservation.SourceText;
            update(percentage, complete ? 0 : .25f, text);
        }
    }
}
