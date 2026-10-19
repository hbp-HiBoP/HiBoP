using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Core.Preferences;
using HBP.Data.Module3D;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using UnityEngine;

namespace HBP.Quest.Desktop
{
    public sealed partial class QuestManager
    {
        private sealed class Publication
        {
            public Base3DScene Scene;
            public string VisualizationId;
            public SceneDelivery Offer;
            public DesktopReplicaSession Owner;
            public PreparedSceneDeliveryBinding Binding;
            public SceneReconciliationRecord Record;
            public CancellationTokenSource Stop;
            public Task<bool> Work;
            public bool HasPendingWork => Work != null && !Work.IsCompleted;
            public string Error;
            public bool UserRetryRequired;
            public bool OfferWasSent;
            public bool PreparedForDisplay;
            public CancellationTokenSource PreparationStop;
            public Task<bool> Preparation;
            public Task<bool> DisplaySend;
            public TaskCompletionSource<bool> DisplayCompletion;
            public DisplayReservation DisplayOwner;
            public readonly HashSet<string> UncertainTransfers = new(StringComparer.Ordinal);
            public bool Removed;
            public readonly CancellationTokenSource Lifetime = new();
            public readonly Dictionary<Guid, Task> Actions = new();
        }

        private readonly Dictionary<Base3DScene, Publication> publications = new();
        private readonly List<(SessionControlKind Kind, SceneSessionClose Close)> pendingCloses = new();
        private readonly Dictionary<string, (HBP.Core.Data.Visualization Visualization, bool Share)> pendingReloads = new();
        public bool CanRetryScenes => pendingReloads.Count != 0 || publications.Values.Any(value => !value.Removed && (value.UserRetryRequired || value.Binding == null && value.Error != null));
        private UserPreferences sceneLifecyclePreferences;
        private bool autoSync, applyingContext, restoringScenes, reloadingScenes, inventoryRequested;
        private Task inventoryWork = Task.CompletedTask;
        private float nextSceneAttempt;
        public bool AutomaticallySynchronizeScenes => autoSync;
        public bool IsSceneSynchronized(Base3DScene scene) => scene != null && publications.TryGetValue(scene, out var entry) && !entry.Removed && !entry.UserRetryRequired && !entry.HasPendingWork && entry.Binding != null && entry.Owner?.IsLive == true && entry.Owner.IsInitialRenderingReady && entry.Error == null;
        public string GetSceneSynchronizationError(Base3DScene scene) => scene != null && publications.TryGetValue(scene, out var entry) ? entry.Error : null;

        public async Task CancelSceneSynchronizationAsync(Base3DScene scene)
        {
            if (scene == null || !publications.TryGetValue(scene, out var entry)) return;
            entry.Stop?.Cancel();
            if (entry.Work != null) await entry.Work;
        }

        private bool NeedsSceneReconnection => publications.Values.Any(value => !value.Removed && !value.UserRetryRequired && !value.HasPendingWork && !IsDisplayReserved(value.Scene) && value.Binding != null && value.Owner?.IsLive != true && value.Error == null);

        // An inventory can predate a send that finishes while its response is in flight.
        // Only reconcile entries that stayed idle and kept the same publication throughout.
        private (Publication Entry, PreparedSceneDeliveryBinding Binding, Task<bool> Work)[] SnapshotInventoryPublications() => publications.Values.Where(value => !value.Removed && !value.UserRetryRequired && !value.HasPendingWork && !IsDisplayReserved(value.Scene)).Select(value => (value, value.Binding, value.Work)).ToArray();

        private static bool CanApplyInventory((Publication Entry, PreparedSceneDeliveryBinding Binding, Task<bool> Work) snapshot) => !snapshot.Entry.Removed && !snapshot.Entry.UserRetryRequired && !snapshot.Entry.HasPendingWork && ReferenceEquals(snapshot.Entry.Binding, snapshot.Binding) && ReferenceEquals(snapshot.Entry.Work, snapshot.Work);

        private void InitializeSceneLifecycle()
        {
            sceneLifecyclePreferences = PersistentDataManager.UserPreferences;
            autoSync = sceneLifecyclePreferences.General.Quest.AutomaticallySynchronizeScenes;
            sceneLifecyclePreferences.OnSavePreferences.AddListener(ApplyScenePreferences);
            Module3DMain.OnRemoveScene.AddListener(SceneRemoved);
            Module3DMain.OnSelectScene.AddListener(SceneSelected);
            Module3DMain.OnSelectColumn.AddListener(ColumnSelected);
        }

        private void ApplyScenePreferences()
        {
            bool value = sceneLifecyclePreferences.General.Quest.AutomaticallySynchronizeScenes;
            if (autoSync == value) return;
            autoSync = value;
            if (value) inventoryRequested = true;
            OnChanged.Invoke();
        }

        private Publication GetPublication(Base3DScene scene)
        {
            if (publications.TryGetValue(scene, out var result)) return result;
            if (publications.Count(value => !value.Value.Removed) >= SceneSessionState.MaximumScenes) throw new InvalidOperationException("The Quest session supports at most 64 visualizations.");
            result = new Publication { Scene = scene, VisualizationId = scene.Visualization.ID };
            publications.Add(scene, result);
            return result;
        }

        private void TickSceneLifecycle()
        {
            if (!IsPaired || busy || displayReservation != null || restoringScenes || reloadingScenes || !inventoryWork.IsCompleted || !Module3DMain.IsInitialized || Time.unscaledTime < nextSceneAttempt) return;
            if (inventoryRequested)
            {
                inventoryRequested = false;
                inventoryWork = SynchronizeInventorySafelyAsync();
                return;
            }

            if (!autoSync) return;
            foreach (var scene in Module3DMain.Scenes.ToArray())
            {
                if (scene == null || scene.IsClosing || IsDisplayReserved(scene)) continue;
                if (!publications.TryGetValue(scene, out var entry))
                {
                    _ = SendAsync(false, scene);
                    continue;
                }

                if (!entry.Removed && !entry.UserRetryRequired && entry.Binding == null && entry.Error == null && (entry.Work == null || entry.Work.IsCompleted)) _ = SendAsync(false, scene);
            }
        }

        private async Task<bool> SendSceneAsync(Publication entry, bool retry)
        {
            var context = SynchronizationContext.Current;
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, entry.Lifetime.Token);
            entry.Stop = attempt;
            entry.Error = null;
            try
            {
                return await HBP.UI.Tools.LoadingManager.LoadAsync<bool>(async (update, uiStop) =>
                {
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(attempt.Token, uiStop);
                    await UniTask.SwitchToMainThread(linked.Token);
                    return await SendSceneCoreAsync(entry, retry, linked.Token, (value, duration, message) => context.Post(_ => update(value, duration, message), null));
                });
            }
            catch (OperationCanceledException)
            {
                await UniTask.SwitchToMainThread();
                if (!entry.Removed)
                {
                    entry.Error = "Visualization transfer cancelled. Retry to continue.";
                    entry.UserRetryRequired = true;
                    SetStatus(entry.Error);
                }

                return false;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                entry.Stop = null;
                OnChanged.Invoke();
            }
        }

        private async Task<bool> SendSceneCoreAsync(Publication entry, bool retry, CancellationToken token, Action<float, float, LoadingText> update, Action validate = null, bool reportAtlasErrors = true)
        {
            using var preparation = PairingTiming.Measure("desktop.scene-publication-total");
            try
            {
                validate?.Invoke();
                if (!IsPaired || globals == null) throw new InvalidOperationException("Pair a Quest first.");
                if (entry.Removed || entry.Scene == null || entry.Scene.IsClosing) throw new InvalidOperationException("The visualization has been closed.");
                if (sessionPreferences != null)
                {
                    using var preferences = PairingTiming.Measure("desktop.scene-preferences-and-atlas");
                    await sessionPreferences.FlushPreferencesAsync().AsUniTask().AttachExternalCancellation(token);
                    foreach (string atlas in entry.Scene.RequiredAtlasIds()) await sessionPreferences.EnsureAtlasReadyAsync(atlas, token, reportError: reportAtlasErrors);
                }

                await UniTask.SwitchToMainThread(token);

                for (int restart = 0;; restart++)
                {
                    token.ThrowIfCancellationRequested();
                    validate?.Invoke();
                    if (restart > 0 && sessionPreferences != null)
                        foreach (string atlas in entry.Scene.RequiredAtlasIds())
                            await sessionPreferences.EnsureAtlasReadyAsync(atlas, token, reportError: reportAtlasErrors);
                    await PrepareSceneOfferAsync(entry, retry, token, update);
                    entry.PreparedForDisplay = false;

                    var owner = entry.Owner;
                    validate?.Invoke();
                    using var deliveryStop = CancellationTokenSource.CreateLinkedTokenSource(token, owner.PublicationAbortToken);
                    try
                    {
                        SetStatus("Sending " + entry.Scene.Visualization.Name + " to Quest");
                        entry.OfferWasSent = true;
                        var receipt = await QuestPairing.SendAsync(endpoint, pin, credential, deliveryStop.Token, (stream, stop) => entry.Offer.SendAsync(stream, stop, null, (sent, total) =>
                        {
                            float fraction = total == 0 ? 0 : Mathf.Clamp01((float)sent / total);
                            update(.20f + .65f * fraction, 0, new LoadingText("Sending " + entry.Scene.Visualization.Name + $": {fraction:P0}"));
                        }));
                        await UniTask.SwitchToMainThread(token);
                        validate?.Invoke();
                        if (entry.Removed) return false;
                        if (retry && restart < 2 && receipt.Status is DeliveryStatus.Closed or DeliveryStatus.Superseded)
                        {
                            await RetireInterruptedPublicationAsync(entry, token);
                            continue;
                        }

                        entry.Binding = PreparedSceneDeliveryBinding.FromSent(entry.Offer, receipt);
                        entry.UncertainTransfers.Remove(entry.Offer.TransferId);
                        entry.Record = SceneReconciliationRecord.Retain(entry.Scene, entry.Binding);
                        using (PairingTiming.Measure("desktop.scene-initial-sync", entry.Offer.TransferId))
                        {
                            await owner.StartAfterPublicationAsync(entry.Binding, endpoint, pin, credential, deliveryStop.Token);
                            await owner.WaitInitialRenderingAsync(deliveryStop.Token);
                        }

                        await UniTask.SwitchToMainThread(token);
                        validate?.Invoke();
                        update(1, 0, new LoadingText("Visualization ready on Quest"));
                        SetStatus(entry.Scene.Visualization.Name + " ready on Quest.");
                        entry.UserRetryRequired = false;
                        return true;
                    }
                    catch (Exception error) when (restart < 2 && !entry.Removed && (owner.RequiresPublicationRestart || error is DesktopReplicaSession.PublicationRestartException))
                    {
                        retry = false;
                    }
                }
            }
            catch (Exception error)
            {
                await UniTask.SwitchToMainThread();
                if (!entry.Removed)
                {
                    bool cancelled = token.IsCancellationRequested || error is OperationCanceledException;
                    entry.Error = cancelled ? "Visualization transfer cancelled. Retry to continue." : TransferError.Describe(error);
                    entry.UserRetryRequired = true;
                    if (entry.Binding != null && entry.Owner?.IsInitialRenderingReady != true)
                    {
                        entry.Owner?.Dispose();
                        entry.Owner = null;
                        if (entry.Scene != null && !entry.Scene.IsClosing)
                            try
                            {
                                await entry.Scene.CapturePreparedAsync(() => 0, CancellationToken.None);
                            }
                            catch (Exception cleanup)
                            {
                                Debug.LogWarning("Desktop Display preparation stopped: " + cleanup.Message);
                            }
                    }

                    if (!cancelled) Debug.LogException(error);
                    if (entry.Binding != null) reconciliationNeedsRetry = true;
                    SetStatus(entry.Error);
                }

                return false;
            }
        }

        private async Task PrepareSceneOfferAsync(Publication entry, bool retry, CancellationToken token, Action<float, float, LoadingText> update, long maximumBytes = long.MaxValue)
        {
            await FlushDefinitionsAsync(token);
            bool reuse = entry.Offer != null && entry.Owner?.CanRetryTransfer == true && (entry.PreparedForDisplay && !entry.OfferWasSent || retry && entry.Offer.CanRetry);
            if (!reuse && (entry.OfferWasSent || entry.UncertainTransfers.Count != 0) && entry.Binding == null)
            {
                if (!retry) throw new InvalidOperationException("The interrupted Quest transfer needs an explicit retry.");
                await RetireInterruptedPublicationAsync(entry, token);
            }

            if (!reuse)
            {
                if (entry.OfferWasSent && entry.Offer != null) entry.UncertainTransfers.Add(entry.Offer.TransferId);
                entry.OfferWasSent = false;
                entry.Owner?.Dispose();
                entry.Owner = null;
                entry.Binding = null;
                entry.Record?.Dispose();
                entry.Record = null;
                if (entry.Offer != null) await entry.Offer.DisposeAsync();
                entry.Offer = null;
                string transferId = Guid.NewGuid().ToString("N");
                long generation = Interlocked.Increment(ref nextTransferCaptureGeneration);
                entry.Offer = await DesktopSceneCapture.CaptureForQuestAsync(globals.Context, token, update, entry.Scene, transferId, generation, (scene, incarnation, session) =>
                {
                    entry.Owner = new DesktopReplicaSession(scene, session, incarnation);
                    entry.Owner.OnActionReceived.AddListener(action => ReceiveSceneAction(entry, action));
                }, maximumOwnedSnapshotBytes: maximumBytes);
            }
        }

        private async Task RetireInterruptedPublicationAsync(Publication entry, CancellationToken token)
        {
            var expectedContext = globals;
            long expectedGeneration = sessionGeneration;
            var transfers = entry.UncertainTransfers.ToList();
            if (entry.OfferWasSent && entry.Offer != null) transfers.Add(entry.Offer.TransferId);
            if (transfers.Count == 0) throw new InvalidOperationException("The interrupted transfer identity is unavailable. Close the Desktop visualization before sending it again.");
            foreach (string transfer in transfers.Distinct())
            {
                await SceneControlAsync(SessionControlKind.CloseScene, new SceneSessionClose { VisualizationId = entry.VisualizationId, TransferId = transfer }, token);
                await UniTask.SwitchToMainThread(token);
                if (!IsPaired || !ReferenceEquals(globals, expectedContext) || sessionGeneration != expectedGeneration || entry.Removed)
                    throw new InvalidOperationException("The Quest connection changed during recovery. Retry after reconnecting.");
            }

            // Only the exact, acknowledged old incarnations may be replaced. A Quest tombstone
            // also prevents an abandoned receive from publishing after this acknowledgement.
            entry.Owner?.Dispose();
            entry.Owner = null;
            if (entry.Offer != null) await entry.Offer.DisposeAsync();
            await UniTask.SwitchToMainThread(token);
            entry.Offer = null;
            entry.OfferWasSent = false;
            entry.PreparedForDisplay = false;
            entry.UncertainTransfers.Clear();
        }

        private async Task<SessionControlResponse> SceneControlAsync(SessionControlKind kind, SceneSessionClose close, CancellationToken token)
        {
            var request = new SessionControlRequest(Guid.ParseExact(globals.Context.Id, "N"), sessionGeneration, Guid.NewGuid(), kind, body: close == null ? null : SceneSessionState.Encode(close));
            // Retrying the same operation is safe even if its acknowledgement was lost.
            SessionControlResponse response;
            try
            {
                response = await QuestPairing.SendSessionControlAsync(endpoint, pin, credential, request, token);
            }
            catch (System.IO.IOException) when (!token.IsCancellationRequested)
            {
                response = await QuestPairing.SendSessionControlAsync(endpoint, pin, credential, request, token);
            }

            if (!response.Applied) throw new InvalidOperationException(response.Message);
            await UniTask.SwitchToMainThread(token);
            return response;
        }

        private static SceneSessionClose Closing(Publication entry) =>
            new()
            {
                VisualizationId = entry.VisualizationId,
                TransferId = entry.Binding?.TransferId ?? entry.Offer?.TransferId,
                FinalSequence = entry.Owner?.FinalSequence ?? 0,
                BulkThrough = entry.Owner?.BulkThrough ?? 0
            };

        private void SceneRemoved(Base3DScene scene)
        {
            if (!publications.TryGetValue(scene, out var entry) || entry.Removed) return;
            var close = Closing(entry);
            Retire(entry);
            if (reloadingScenes) return;
            if (close.TransferId != null) pendingCloses.Add((autoSync ? SessionControlKind.CloseScene : SessionControlKind.DetachScene, close));
            foreach (string transfer in entry.UncertainTransfers.Where(value => value != close.TransferId))
                pendingCloses.Add((autoSync ? SessionControlKind.CloseScene : SessionControlKind.DetachScene, new SceneSessionClose { VisualizationId = entry.VisualizationId, TransferId = transfer }));
            inventoryRequested = true;
        }

        private static void Retire(Publication entry)
        {
            entry.Removed = true;
            entry.Lifetime.Cancel();
            entry.Stop?.Cancel();
            entry.Owner?.Dispose();
            entry.Owner = null;
            entry.Record?.Dispose();
            entry.Record = null;
            entry.Offer?.Dispose();
        }

        private async Task FlushClosesAsync(CancellationToken token)
        {
            async Task FlushAsync((SessionControlKind Kind, SceneSessionClose Close) item)
            {
                await SceneControlAsync(item.Kind, item.Close, token);
                pendingCloses.Remove(item);
            }

            await Task.WhenAll(pendingCloses.ToArray().Select(FlushAsync));
        }

        private async Task SynchronizeInventorySafelyAsync()
        {
            try
            {
                await FlushClosesAsync(lifetime.Token);
                if (autoSync) await SynchronizeInventoryAsync(lifetime.Token);
            }
            catch (Exception error)
            {
                await UniTask.SwitchToMainThread();
                inventoryRequested = true;
                nextSceneAttempt = Time.unscaledTime + 5;
                SetStatus("Scene synchronization interrupted: " + TransferError.Describe(error));
            }
        }

        private async Task<SceneSessionEntry[]> InventoryAsync(CancellationToken token)
        {
            var response = await SceneControlAsync(SessionControlKind.SceneInventory, null, token);
            var scenes = SceneSessionState.Decode<SceneSessionEntry[]>(response.GetBody());
            if (scenes.Length > SceneSessionState.MaximumScenes || scenes.Any(value => value == null || !Guid.TryParseExact(value.TransferId, "N", out _)) || scenes.Select(value => value.TransferId).Distinct().Count() != scenes.Length)
                throw new System.IO.InvalidDataException("Invalid Quest scene inventory.");
            return scenes;
        }

        private async Task SynchronizeInventoryAsync(CancellationToken token)
        {
            if (Module3DMain.IsInitialized)
                foreach (var scene in Module3DMain.Scenes.Where(value => value != null && !value.IsClosing).ToArray())
                    GetPublication(scene);
            var candidates = SnapshotInventoryPublications();
            var inventory = await InventoryAsync(token);
            var local = Module3DMain.IsInitialized ? Module3DMain.Scenes.Where(value => value != null && !value.IsClosing).ToArray() : Array.Empty<Base3DScene>();
            var closes = new List<Task<SessionControlResponse>>();
            foreach (var remote in inventory)
            {
                var retained = publications.Values.FirstOrDefault(value => !value.Removed && value.VisualizationId == remote.VisualizationId && ((value.Binding?.TransferId ?? value.Offer?.TransferId) == remote.TransferId || value.UncertainTransfers.Contains(remote.TransferId)));
                // An uncertain receipt or cancelled Display remains a manual recovery.
                // Inventory must not erase its remote copy to make a fresh send possible.
                if (retained?.UserRetryRequired == true || retained?.UncertainTransfers.Contains(remote.TransferId) == true) continue;
                if (remote.Orphan || retained == null || !local.Contains(retained.Scene))
                    closes.Add(SceneControlAsync(SessionControlKind.CloseScene, new SceneSessionClose { VisualizationId = remote.VisualizationId, TransferId = remote.TransferId }, token));
            }

            await Task.WhenAll(closes);
            var sends = new List<Task<bool>>();
            foreach (var candidate in candidates)
            {
                token.ThrowIfCancellationRequested();
                if (!CanApplyInventory(candidate)) continue;
                var entry = candidate.Entry;
                var scene = entry.Scene;
                if (scene == null || scene.IsClosing || !local.Contains(scene) || !Module3DMain.Scenes.Contains(scene)) continue;
                bool present = inventory.Any(value => !value.Orphan && value.VisualizationId == entry.VisualizationId && value.TransferId == entry.Binding?.TransferId);
                if (present) continue;
                entry.Owner?.Dispose();
                entry.Owner = null;
                entry.Binding = null;
                entry.Record?.Dispose();
                entry.Record = null;
                sends.Add(SendAsync(false, scene));
            }

            await Task.WhenAll(sends);
        }

        private async Task RestoreScenesAsync(CancellationToken token, Base3DScene manualScene = null)
        {
            restoringScenes = true;
            reconciliationNeedsRetry = false;
            Task<ConflictChoice> choice = null;
            try
            {
                await FlushClosesAsync(token);
                var candidates = manualScene == null ? SnapshotInventoryPublications() : new[] { (Entry: GetPublication(manualScene), Binding: GetPublication(manualScene).Binding, Work: GetPublication(manualScene).Work) };
                var inventory = await InventoryAsync(token);

                async Task ReconnectAsync((Publication Entry, PreparedSceneDeliveryBinding Binding, Task<bool> Work) candidate)
                {
                    if (manualScene == null && !CanApplyInventory(candidate) || candidate.Binding == null || candidate.Entry.Removed) return;
                    var entry = candidate.Entry;
                    var remote = inventory.FirstOrDefault(value => value.TransferId == entry.Binding.TransferId && value.VisualizationId == entry.VisualizationId && !value.Orphan);
                    if (remote == null)
                    {
                        if (manualScene != null)
                        {
                            entry.Error = "The published visualization is unavailable on Quest. Close it explicitly before sending a new copy.";
                            entry.UserRetryRequired = true;
                            return;
                        }

                        entry.Owner?.Dispose();
                        entry.Owner = null;
                        entry.Binding = null;
                        entry.Record?.Dispose();
                        entry.Record = null;
                        return;
                    }

                    if (entry.Owner?.IsLive == true && (manualScene == null || entry.Owner.IsInitialRenderingReady)) return;
                    try
                    {
                        entry.Owner?.Dispose();
                        entry.Owner = null;
                        var identity = entry.Binding.CreateIdentity();
                        using var sceneStop = CancellationTokenSource.CreateLinkedTokenSource(token, entry.Lifetime.Token);
                        foreach (string atlas in entry.Scene.RequiredAtlasIds())
                            await sessionPreferences.EnsureAtlasReadyAsync(atlas, sceneStop.Token, reportError: false);
                        await QuestPairing.OpenReplicaAsync(endpoint, pin, credential, sceneStop.Token, async (stream, stop) =>
                        {
                            await ReplicaRoute.WriteAsync(stream, identity.SessionId, identity.SceneId, identity.IncarnationId, stop);
                            await SceneReconciliationProtocol.ReconcileDesktopAsync(stream, entry.Record, (conflict, choiceStop) => choice ??= ChooseConflictAsync(conflict, token), stop);
                        });
                        await UniTask.SwitchToMainThread(token);
                        if (entry.Removed || entry.Record?.Status == ReconciliationStatus.Orphan) return;
                        entry.Owner = new DesktopReplicaSession(entry.Scene, entry.Binding.GlobalContextId, entry.Binding.TransferId);
                        entry.Owner.OnActionReceived.AddListener(action => ReceiveSceneAction(entry, action));
                        await entry.Owner.StartAfterPublicationAsync(entry.Binding, endpoint, pin, credential, sceneStop.Token);
                        await entry.Owner.WaitInitialRenderingAsync(sceneStop.Token);
                        entry.Error = null;
                    }
                    catch (Exception error) when (!token.IsCancellationRequested)
                    {
                        await UniTask.SwitchToMainThread(token);
                        if (entry.Removed) return;
                        RecordSceneReconnectionFailure(entry, error);
                    }
                }

                await Task.WhenAll(candidates.Where(value => value.Binding != null).Select(ReconnectAsync));
                await FlushClosesAsync(token);
                if (autoSync && manualScene == null) await SynchronizeInventoryAsync(token);
                // An atlas may have remained in use by the old Quest scene until reconciliation.
                foreach (var intent in atlasIntents.Where(value => !value.Value)) sessionPreferences?.RequestAtlasSynchronization(intent.Key);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                restoringScenes = false;
                OnChanged.Invoke();
            }
        }

        private void RecordSceneReconnectionFailure(Publication entry, Exception error)
        {
            if (IsQuestConnectionInterruption(error))
            {
                // Sleeping again is recoverable on the next heartbeat. Keep actual merge or
                // data failures on the explicit Retry path.
                entry.Error = null;
                entry.UserRetryRequired = false;
                SessionConnectionLost();
                return;
            }

            entry.Error = "Reconnection failed for " + entry.VisualizationId + ": " + TransferError.Describe(error);
            entry.UserRetryRequired = true;
            reconciliationNeedsRetry = true;
        }

        private void SceneSelected(Base3DScene scene) => SendContext(scene, scene?.SelectedColumn);

        private void ColumnSelected(Column3D column)
        {
            if (column == null || !Module3DMain.IsInitialized) return;
            var scene = Module3DMain.Scenes.FirstOrDefault(value => value.Columns.Contains(column));
            SendContext(scene, column);
        }

        private void SendContext(Base3DScene scene, Column3D column)
        {
            if (applyingContext || scene == null || column == null || !scene.IsVisible || !publications.TryGetValue(scene, out var entry) || entry.Owner?.IsLive != true || entry.Binding == null) return;
            var identity = entry.Binding.CreateIdentity();
            try
            {
                entry.Owner.SendAction(new(identity.SceneId, identity.IncarnationId, SceneActionKind.SelectContext, column.ColumnData.ID));
            }
            catch (System.IO.IOException error)
            {
                SetStatus(TransferError.Describe(error));
            }
        }

        private void ReceiveSceneAction(Publication entry, SceneAction action)
        {
            if (entry.Removed || entry.Scene == null || entry.Scene.IsClosing || entry.Binding == null) return;
            if (action.Kind == SceneActionKind.SelectContext)
            {
                var column = entry.Scene.Columns.FirstOrDefault(value => value.ColumnData.ID == action.Column);
                applyingContext = true;
                try
                {
                    Module3DMain.SelectSceneContext(entry.Scene, column);
                }
                finally
                {
                    applyingContext = false;
                }

                return;
            }

            if (entry.Actions.ContainsKey(action.Operation)) return;
            if (entry.Actions.Count >= 256)
            {
                foreach (var key in entry.Actions.Where(value => value.Value.IsCompleted).Select(value => value.Key).Take(128).ToArray()) entry.Actions.Remove(key);
            }

            if (entry.Actions.Count >= 256) return;
            entry.Actions.Add(action.Operation, ApplySceneActionAsync(entry, action));
        }

        private async Task ApplySceneActionAsync(Publication entry, SceneAction action)
        {
            try
            {
                if (reloadingScenes) throw new InvalidOperationException("Visualizations are reloading after a preferences change. Try again when reloading finishes.");
                if (action.Kind == SceneActionKind.Close)
                {
                    var close = Closing(entry);
                    // Acknowledge the Quest close before comparing inventories or retiring its transport.
                    await SceneControlAsync(SessionControlKind.CloseScene, close, lifetime.Token);
                    Retire(entry);
                    if (autoSync && entry.Scene != null && !entry.Scene.IsClosing) Module3DMain.RemoveScene(entry.Scene);
                    else
                    {
                        publications.Remove(entry.Scene);
                    }

                    return;
                }

                if (action.Kind != SceneActionKind.OpenPatient) return;
                if (!entry.Scene.IsVisible) throw new InvalidOperationException("Show the source visualization first.");
                var column = entry.Scene.Columns.FirstOrDefault(value => value.ColumnData.ID == action.Column);
                var site = column?.Sites.FirstOrDefault(value => value.Information.FullID == action.Site);
                if (site == null) throw new InvalidOperationException("The selected patient site is no longer available.");
                Module3DMain.SelectSceneContext(entry.Scene, column);
                entry.Scene.SelectSite(column, site);
                var child = await HBP.UI.Tools.LoadingManager.LoadAsync<Base3DScene>(async (progress, token) => await Module3DMain.OpenSinglePatientVisualizationAsync(entry.Scene, site.Information.Patient, progress, token));
                await UniTask.SwitchToMainThread(lifetime.Token);
                if (!await SendAsync(false, child)) throw new InvalidOperationException(GetPublication(child).Error ?? "The patient visualization could not be delivered. Retry from Display.");
                SendContext(child, child.SelectedColumn);
                entry.Owner?.SendAction(new(action.Scene, action.Incarnation, SceneActionKind.Result, operation: action.Operation, message: "Patient visualization ready on both devices."));
            }
            catch (Exception error)
            {
                await UniTask.SwitchToMainThread();
                try
                {
                    entry.Owner?.SendAction(new(action.Scene, action.Incarnation, SceneActionKind.Result, operation: action.Operation, succeeded: false, message: TransferError.Describe(error)));
                }
                catch (System.IO.IOException)
                {
                }

                SetStatus(TransferError.Describe(error));
            }
        }

        private void ClearSceneLifecycle()
        {
            foreach (var entry in publications.Values) Retire(entry);
            publications.Clear();
            pendingCloses.Clear();
            inventoryRequested = false;
        }

        private async Task RetryFailedScenesAsync(CancellationToken token)
        {
            var failures = new List<string>();
            await HBP.UI.Tools.LoadingManager.LoadAsync<bool>(async (progress, uiStop) =>
            {
                using var stop = CancellationTokenSource.CreateLinkedTokenSource(token, uiStop);
                await UniTask.SwitchToMainThread(stop.Token);
                foreach (var pending in pendingReloads.Values.ToArray())
                {
                    try
                    {
                        await ReloadOpeningAsync(pending.Visualization, pending.Share, progress, stop.Token);
                    }
                    catch (Exception error) when (error is not OperationCanceledException)
                    {
                        failures.Add(pending.Visualization.Name + ": " + TransferError.Describe(error));
                    }
                }

                return failures.Count == 0;
            });
            await RetryDisplayScenesAsync(publications.Values.Where(value => !value.Removed && value.UserRetryRequired).Select(value => value.Scene).ToArray(), token);
            failures.AddRange(publications.Values.Where(value => !value.Removed && value.UserRetryRequired).Select(value => value.Scene.Visualization.Name + ": " + value.Error));
            SetStatus(failures.Count == 0 ? "Visualizations ready." : "Some visualizations need retry: " + string.Join("; ", failures));
        }

        private async Task ReloadOpeningAsync(HBP.Core.Data.Visualization visualization, bool share, Action<float, float, LoadingText> progress, CancellationToken token)
        {
            await UniTask.SwitchToMainThread(token);
            var scene = Module3DMain.Scenes.FirstOrDefault(value => value.Visualization.ID == visualization.ID && !value.IsClosing);
            if (scene == null)
            {
                await Module3DMain.LoadAsync(new[] { visualization }, progress, token);
                await UniTask.SwitchToMainThread(token);
                scene = Module3DMain.Scenes.Single(value => value.Visualization == visualization);
            }

            pendingReloads.Remove(visualization.ID);
            if (IsPaired && (autoSync || share) && !await SendAsync(false, scene)) throw new InvalidOperationException(GetPublication(scene).Error);
        }

        /// <summary>The settings mutation runs only after both devices have retired their old native users.</summary>
        public async Task ReloadForPreferencesAsync(Action applyPreferences, Action<float, float, LoadingText> progress, CancellationToken token)
        {
            await UniTask.SwitchToMainThread(token);
            if (reloadingScenes) throw new InvalidOperationException("A preferences reload is already running.");
            int memoryLimit = PersistentDataManager.UserPreferences.General.System.MemoryCacheLimit;
            reloadingScenes = true;
            try
            {
                var openings = Module3DMain.Scenes.Where(scene => scene != null && !scene.IsClosing).ToArray();
                var shared = publications.Values.Where(entry => !entry.Removed && entry.Binding != null).ToArray();
                var sharedIds = new HashSet<string>(shared.Select(entry => entry.VisualizationId));
                var visualizations = openings.Select(scene =>
                {
                    scene.SaveConfiguration();
                    return scene.Visualization;
                }).ToArray();
                var failures = new List<string>();
                if (shared.Length != 0 && !IsPaired) throw new InvalidOperationException("Reconnect Quest before reloading shared visualizations.");
                if (IsPaired)
                {
                    await FlushClosesAsync(token);
                    var inventory = await InventoryAsync(token);
                    if (inventory.Any(remote => remote.Orphan || !shared.Any(entry => entry.Binding.TransferId == remote.TransferId)))
                        throw new InvalidOperationException("Close local/orphan visualizations on Quest before changing EEG normalization.");
                    foreach (var entry in shared)
                    {
                        token.ThrowIfCancellationRequested();
                        var close = Closing(entry);
                        entry.Stop?.Cancel();
                        entry.Owner?.Dispose();
                        entry.Owner = null;
                        if (entry.Work != null) await entry.Work;
                        await SceneControlAsync(SessionControlKind.CloseScene, close, token);
                    }
                }

                foreach (var visualization in visualizations) pendingReloads[visualization.ID] = (visualization, sharedIds.Contains(visualization.ID));
                foreach (var scene in openings)
                {
                    token.ThrowIfCancellationRequested();
                    await Module3DMain.RemoveSceneAsync(scene);
                }

                foreach (var entry in shared)
                {
                    Retire(entry);
                    if (entry.Offer != null) await entry.Offer.DisposeAsync();
                }

                await UniTask.SwitchToMainThread(token);
                applyPreferences();
                memoryLimit = PersistentDataManager.UserPreferences.General.System.MemoryCacheLimit;
                HBP.Core.Data.DataManager.ConfigureMemoryBudget(0, 0);
                HBP.Core.Data.DataManager.ClearDerivedData();
                if (IsPaired && sessionPreferences != null) await sessionPreferences.FlushPreferencesAsync();
                foreach (var visualization in visualizations)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        await ReloadOpeningAsync(visualization, sharedIds.Contains(visualization.ID), progress, token);
                    }
                    catch (Exception error) when (error is not OperationCanceledException)
                    {
                        failures.Add(visualization.Name + ": " + TransferError.Describe(error));
                    }
                }

                SetStatus(failures.Count == 0 ? "Visualizations reloaded with the new preferences." : "Some visualizations need retry: " + string.Join("; ", failures));
                if (failures.Count != 0) throw new InvalidOperationException(Status);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                HBP.Core.Data.DataManager.ConfigureMemoryBudget(memoryLimit, SystemInfo.systemMemorySize);
                reloadingScenes = false;
                OnChanged.Invoke();
            }
        }

        private void DisposeSceneLifecycle()
        {
            sceneLifecyclePreferences?.OnSavePreferences.RemoveListener(ApplyScenePreferences);
            Module3DMain.OnRemoveScene.RemoveListener(SceneRemoved);
            Module3DMain.OnSelectScene.RemoveListener(SceneSelected);
            Module3DMain.OnSelectColumn.RemoveListener(ColumnSelected);
            foreach (var entry in publications.Values)
            {
                entry.Stop?.Cancel();
                entry.Owner?.Dispose();
            }
        }
    }
}
