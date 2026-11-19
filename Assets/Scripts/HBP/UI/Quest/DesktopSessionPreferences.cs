using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Sync.Scene;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using Newtonsoft.Json;

namespace HBP.UI.Quest
{
    /// <summary>Connected-only Desktop policy changes; no queue survives this owner's connection generation.</summary>
    public sealed class DesktopSessionPreferences : IDisposable
    {
        private readonly Guid m_Context;
        private readonly long m_Generation;
        private readonly UserPreferences m_Preferences;
        private readonly UserPreferences m_PairingPreferences;
        private readonly Func<SessionControlRequest, CancellationToken, Task<SessionControlResponse>> m_Send;
        private readonly IReadOnlyDictionary<string, bool> m_AtlasIntents;
        private readonly Func<CancellationToken, Task<bool>> m_PeerReady;
        private readonly TimeSpan m_ControlTimeout;
        private long m_CatalogGeneration;
        private string m_SynchronizingAtlas;
        private CancellationTokenSource m_AtlasAttempt;
        private readonly CancellationTokenSource m_Stop = new();
        private readonly SemaphoreSlim m_Gate = new(1, 1);
        private Task m_PendingPreferences = Task.CompletedTask;
        private readonly TaskCompletionSource<bool> m_Opened = new();
        private long m_Revision;
        private int m_QueuedSaves;
        public const int MaximumQueuedSaves = 64;
        private bool m_Started, m_Disposed;
        private Task m_InventoryWork = Task.CompletedTask;
        private long m_AtlasChanges;
        private readonly HashSet<string> m_ConfirmedAtlases = new(StringComparer.Ordinal);
        private readonly HashSet<string> m_InventoryRecoveryAttempted = new(StringComparer.Ordinal);
        public bool QuestHasSharedScene { get; private set; }
        public string Status { get; private set; } = "Quest session is connecting.";
        private readonly Dictionary<string, string> m_AtlasStates = new(StringComparer.Ordinal);
        public string AtlasStatus { get; private set; } = "Quest session is connecting.";
        public UnityEvent OnChanged { get; } = new();
        public UnityEvent OnConnectionLost { get; } = new();
        public UnityEvent<string> OnAtlasError { get; } = new();
        private readonly HashSet<string> m_PendingRemoteUnloads = new(StringComparer.Ordinal);

        public string GetAtlasState(string id) => m_AtlasStates.TryGetValue(id, out string state) ? state : "Quest synchronization is pending.";

        public void RetryPendingAtlasUnloads()
        {
            foreach (string id in m_PendingRemoteUnloads.ToArray()) RequestAtlasSynchronization(id);
        }

        public DesktopSessionPreferences(Guid context, long generation, UserPreferences preferences, Func<SessionControlRequest, CancellationToken, Task<SessionControlResponse>> send, UserPreferences pairingPreferences = null, IReadOnlyDictionary<string, bool> atlasIntents = null, Func<CancellationToken, Task<bool>> peerReady = null, TimeSpan? controlTimeout = null)
        {
            m_Context = context;
            m_Generation = generation;
            m_Preferences = preferences;
            m_Send = send;
            m_PairingPreferences = pairingPreferences;
            m_AtlasIntents = atlasIntents;
            m_PeerReady = peerReady;
            m_ControlTimeout = controlTimeout ?? TimeSpan.FromSeconds(10);
            m_Preferences.OnSavePreferences.AddListener(OnPreferencesSaved);
        }

        private SessionControlRequest Request(SessionControlKind kind, string atlas = "", byte[] body = null, long revision = 0) => new(m_Context, m_Generation, Guid.NewGuid(), kind, revision, atlas, body: body);

        private async UniTask<SessionControlResponse> SendAsync(SessionControlRequest request, CancellationToken cancellationToken = default)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(m_Stop.Token, cancellationToken);
            if (request.Kind != SessionControlKind.LoadAtlas) linked.CancelAfter(m_ControlTimeout);
            m_Stop.Token.ThrowIfCancellationRequested();
            SessionControlResponse response;
            try
            {
                // Long native loads keep their own budget, but a sleeping application must not hold the queue.
                Task monitor = request.Kind == SessionControlKind.LoadAtlas && m_PeerReady != null ? MonitorPeerAsync(linked) : Task.CompletedTask;
                try
                {
                    response = await m_Send(request, linked.Token).ConfigureAwait(false);
                    linked.Token.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException) when (!m_Stop.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException("Quest is unavailable or asleep. Wake the headset to resume atlas synchronization.");
                }
                finally
                {
                    linked.Cancel();
                    await monitor.ConfigureAwait(false);
                }
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }

            m_Stop.Token.ThrowIfCancellationRequested();
            cancellationToken.ThrowIfCancellationRequested();
            QuestHasSharedScene = response.HasSharedScene;
            return response;
        }

        private async Task MonitorPeerAsync(CancellationTokenSource request)
        {
            try
            {
                while (!request.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), request.Token).ConfigureAwait(false);
                    if (!await m_PeerReady(request.Token).ConfigureAwait(false))
                    {
                        request.Cancel();
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                request.Cancel();
            }
        }

        public async UniTask StartAsync(CancellationToken cancellationToken = default)
        {
            using var timing = PairingTiming.Measure("desktop.session-open");
            var response = await SendAsync(Request(SessionControlKind.Open), cancellationToken);
            if (!response.Applied) throw new InvalidOperationException(response.Message);
            if (response.Message != SessionControlCodec.AtlasInventoryCapability) throw new InvalidOperationException("Update HiBoP Desktop and Quest together: the Quest session protocol is incompatible.");
            m_CatalogGeneration = SessionAtlasCatalog.Begin();
            m_Started = true;
            m_Opened.TrySetResult(true);
            SetStatus("Quest connected. Desktop atlases are synchronizing in the background.");
            AtlasResources.OnChanged.AddListener(AtlasChanged);
            m_InventoryWork = RefreshInventoryAsync();
        }

        /// <summary>Resume the same application session without cancelling an atlas already loading on Quest.</summary>
        public async UniTask ReopenAsync(CancellationToken cancellationToken)
        {
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(m_Stop.Token, cancellationToken);
            try
            {
                await m_Gate.WaitAsync(operation.Token);
                try
                {
                    var response = await SendAsync(Request(SessionControlKind.Open), operation.Token);
                    if (!response.Applied || response.Message != SessionControlCodec.AtlasInventoryCapability)
                        throw new InvalidOperationException("The Quest session could not resume: " + response.Message);
                }
                finally
                {
                    m_Gate.Release();
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new IOException("Quest became unavailable while resuming its atlas session.");
            }
        }

        private void AtlasChanged(AtlasLoadResult result)
        {
            if (!m_Started || m_Disposed) return;
            m_AtlasChanges++;
            if (result.State == AtlasLoadState.Unloaded)
            {
                if (m_SynchronizingAtlas == result.Id) m_AtlasAttempt?.Cancel();
                m_ConfirmedAtlases.Remove(result.Id);
                SessionAtlasCatalog.Remove(result.Id);
            }

            if (m_InventoryWork.IsCompleted) m_InventoryWork = RefreshInventoryAsync();
        }

        public void RequestAtlasSynchronization(string id)
        {
            if (!m_Started || m_Disposed) return;
            m_InventoryRecoveryAttempted.Remove(id);
            AtlasChanged(AtlasResources.Status(id));
        }

        private async Task RefreshInventoryAsync()
        {
            // Give StartAsync and pending saves their completion boundary before background requests.
            await UniTask.NextFrame();
            try
            {
                bool preparing, changed;
                do
                {
                    m_Stop.Token.ThrowIfCancellationRequested();
                    await m_Gate.WaitAsync(m_Stop.Token);
                    long observedChanges = m_AtlasChanges;
                    try
                    {
                        var response = await SendAsync(Request(SessionControlKind.AtlasInventory));
                        if (!response.Applied) throw new InvalidOperationException(response.Message);
                        var inventory = JsonConvert.DeserializeObject<SessionAtlasInventory>(Encoding.UTF8.GetString(response.GetBody())) ?? throw new InvalidDataException("Missing Quest atlas inventory.");
                        preparing = inventory.Preparing;
                        foreach (string id in inventory.Atlases.Keys.Concat(AtlasResources.Definitions.Select(value => value.Id)).Distinct(StringComparer.Ordinal))
                        {
                            var local = AtlasResources.Status(id);
                            inventory.Atlases.TryGetValue(id, out var remote);
                            var remoteState = remote?.State ?? AtlasLoadState.Unloaded;
                            if (m_ConfirmedAtlases.Contains(id) && (local.State != AtlasLoadState.Loaded || remoteState != AtlasLoadState.Loaded))
                            {
                                m_ConfirmedAtlases.Remove(id);
                                SessionAtlasCatalog.Remove(id);
                            }

                            if (remoteState != AtlasLoadState.Unloaded || local.State != AtlasLoadState.Unloaded)
                                SetAtlasState(id, local.State.ToString(), remote?.Error ?? remoteState.ToString());
                            bool unload = m_AtlasIntents != null && m_AtlasIntents.TryGetValue(id, out bool desired) && !desired && local.State == AtlasLoadState.Unloaded;
                            if (unload && remoteState != AtlasLoadState.Unloaded) m_PendingRemoteUnloads.Add(id);
                            else m_PendingRemoteUnloads.Remove(id);
                            if ((local.State == AtlasLoadState.Loaded && !m_ConfirmedAtlases.Contains(id) || unload && remoteState != AtlasLoadState.Unloaded) && m_InventoryRecoveryAttempted.Add(id))
                            {
                                using var attempt = CancellationTokenSource.CreateLinkedTokenSource(m_Stop.Token);
                                m_AtlasAttempt = attempt;
                                m_SynchronizingAtlas = id;
                                try
                                {
                                    await SetAtlasLoadedCoreAsync(id, !unload, attempt.Token, confirmOnly: !unload && remoteState == AtlasLoadState.Loaded, reportError: false, remoteOnly: true);
                                }
                                catch (OperationCanceledException) when (!m_Stop.IsCancellationRequested)
                                {
                                    m_InventoryRecoveryAttempted.Remove(id);
                                }
                                finally
                                {
                                    m_AtlasAttempt = null;
                                    m_SynchronizingAtlas = null;
                                }
                            }

                            m_Stop.Token.ThrowIfCancellationRequested();
                            if (m_ConfirmedAtlases.Contains(id)) m_InventoryRecoveryAttempted.Remove(id);
                        }
                    }
                    finally
                    {
                        m_Gate.Release();
                    }

                    if (preparing) await Task.Delay(2000, m_Stop.Token);
                    await UniTask.SwitchToMainThread();
                    changed = observedChanges != m_AtlasChanges;
                } while ((preparing || changed) && !m_Disposed);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                await FailedAsync("Quest atlas preparation: " + TransferError.Describe(exception), exception);
            }
        }

        private void OnPreferencesSaved()
        {
            if (m_Disposed) return;
            m_PairingPreferences?.Copy(m_Preferences.Clone());
            if (m_QueuedSaves >= MaximumQueuedSaves)
            {
                SetStatus("Preferences saved on Desktop; Quest queue is full. Pending operations were abandoned.");
                Dispose();
                OnConnectionLost.Invoke();
                return;
            }

            var snapshot = PreferencesSnapshot.CaptureSerialization(m_Preferences);
            long revision = ++m_Revision;
            m_QueuedSaves++;
            m_PendingPreferences = SendPreferencesAsync(m_PendingPreferences, snapshot, revision);
        }

        private async Task SendPreferencesAsync(Task previous, Func<byte[]> captured, long revision)
        {
            try
            {
                await previous;
                await m_Opened.Task;
                m_Stop.Token.ThrowIfCancellationRequested();
                byte[] snapshot = await Task.Run(captured, m_Stop.Token);
                await m_Gate.WaitAsync(m_Stop.Token);
                try
                {
                    var response = await SendAsync(Request(SessionControlKind.Preferences, body: snapshot, revision: revision));
                    SetStatus(response.Applied ? "Preferences applied on Desktop and Quest." : "Preferences saved on Desktop; Quest: " + response.Message);
                }
                finally
                {
                    m_Gate.Release();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                await FailedAsync("Preferences saved on Desktop; Quest: " + TransferError.Describe(exception), exception);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                m_QueuedSaves--;
            }
        }

        public Task FlushPreferencesAsync() => m_PendingPreferences;

        public Task SynchronizeCurrentPreferencesAsync()
        {
            OnPreferencesSaved();
            return FlushPreferencesAsync();
        }

        public async UniTask EnsureAtlasReadyAsync(string id, CancellationToken token = default, bool reportError = true)
        {
            m_Stop.Token.ThrowIfCancellationRequested();
            token.ThrowIfCancellationRequested();
            if (SessionAtlasCatalog.CanUse(id)) return;
            await SetAtlasLoadedAsync(id, true, token, reportError: reportError, remoteOnly: AtlasResources.IsLoaded(id));
            token.ThrowIfCancellationRequested();
            m_Stop.Token.ThrowIfCancellationRequested();
            if (!SessionAtlasCatalog.CanUse(id)) throw new InvalidOperationException(Status);
        }

        public async UniTask<string> ValidateNormalizationAsync(NormalizationType requested, bool desktopHasSharedScene)
        {
            if (requested == m_Preferences.Data.EEG.Normalization) return null;
            if (desktopHasSharedScene) return "Close the shared visualizations before changing EEG normalization. Automatic reload and retransfer will be added with multi-scene support.";
            try
            {
                await m_Gate.WaitAsync(m_Stop.Token);
                try
                {
                    var response = await SendAsync(Request(SessionControlKind.Inspect));
                    if (!response.Applied) return response.Message;
                    return response.HasSharedScene ? "Close the Quest visualization before changing EEG normalization." : null;
                }
                finally
                {
                    m_Gate.Release();
                }
            }
            catch (Exception exception)
            {
                return "The Quest scene state could not be checked: " + TransferError.Describe(exception);
            }
        }

        public async UniTask SetAtlasLoadedAsync(string id, bool loaded, CancellationToken cancellationToken = default, bool reportError = true, bool remoteOnly = false)
        {
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(m_Stop.Token, cancellationToken);
            var token = operation.Token;
            // Record intent before waiting: a stale inventory must not reverse a queued unload.
            m_InventoryRecoveryAttempted.Add(id);
            try
            {
                await m_Gate.WaitAsync(token);
                try
                {
                    m_InventoryRecoveryAttempted.Add(id);
                    if (loaded)
                    {
                        m_SynchronizingAtlas = id;
                        m_AtlasAttempt = operation;
                    }

                    await SetAtlasLoadedCoreAsync(id, loaded, token, reportError: reportError, remoteOnly: remoteOnly);
                }
                finally
                {
                    m_AtlasAttempt = null;
                    m_SynchronizingAtlas = null;
                    m_Gate.Release();
                }
            }
            catch (OperationCanceledException)
            {
                if (!AtlasResources.IsLoaded(id)) m_InventoryRecoveryAttempted.Remove(id);
            }
        }

        // The caller owns m_Gate throughout recovery, including the delays between attempts.
        private async UniTask SetAtlasLoadedCoreAsync(string id, bool loaded, CancellationToken token, bool confirmOnly = false, bool reportError = true, bool remoteOnly = false)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    if (confirmOnly && attempt == 0) await ConfirmAsync(id, token);
                    else await ApplyAtlasLoadedAsync(id, loaded, token, remoteOnly);
                    SetAtlasState(id, loaded ? "loaded" : "unloaded", loaded ? "loaded" : "unloaded");
                    SetStatus(id + ": Desktop and Quest " + (loaded ? "loaded." : "unloaded."));
                    return;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    await UniTask.SwitchToMainThread();
                    token.ThrowIfCancellationRequested();
                    if (exception is TimeoutException)
                    {
                        await FailedAsync("Desktop remains usable. " + exception.Message, exception);
                        return;
                    }

                    if (attempt < 2)
                    {
                        await UniTask.Delay(500 * (attempt + 1), DelayType.Realtime, cancellationToken: token);
                        continue;
                    }

                    string message = id + ": " + TransferError.Describe(exception);
                    SetAtlasState(id, AtlasResources.Status(id).State.ToString(), TransferError.Describe(exception));
                    if (reportError) OnAtlasError.Invoke(message);
                    await FailedAsync(message, exception);
                }
            }
        }

        private async UniTask ApplyAtlasLoadedAsync(string id, bool loaded, CancellationToken token, bool remoteOnly = false)
        {
            SetAtlasState(id, loaded ? "loading" : "checking usage", loaded ? "loading" : "checking usage");
            SetStatus(id + ": " + (loaded ? "loading on Desktop and Quest..." : "checking Desktop and Quest usage..."));
            if (loaded)
            {
                if (remoteOnly)
                {
                    if (!AtlasResources.IsLoaded(id)) throw new OperationCanceledException(token);
                    var response = await SendAsync(Request(SessionControlKind.LoadAtlas, id), token);
                    if (!response.Applied) throw new InvalidOperationException(response.Message);
                    await ConfirmAsync(id, token);
                    return;
                }

                using var attempt = CancellationTokenSource.CreateLinkedTokenSource(token);
                // Already loaded resources are reused; a partial success is never unloaded to retry.
                var localWork = AtlasResources.LoadAsync(id, attempt.Token).AsTask();
                var remoteWork = SendAsync(Request(SessionControlKind.LoadAtlas, id), attempt.Token).AsTask();
                AtlasLoadResult local;
                try
                {
                    local = await localWork;
                }
                catch
                {
                    attempt.Cancel();
                    try
                    {
                        await remoteWork;
                    }
                    catch (Exception)
                    {
                    }

                    throw;
                }

                var remote = await remoteWork;
                token.ThrowIfCancellationRequested();
                SetAtlasState(id, local.State.ToString(), remote.Applied ? "loaded" : remote.Message);
                if (!local.Succeeded || !remote.Applied)
                    throw new InvalidOperationException("Desktop " + local.State + "; Quest " + (remote.Applied ? "loaded" : remote.Message) + "; " + local.Error);
                await ConfirmAsync(id, token);
            }
            else
            {
                using var localReservation = remoteOnly ? null : ResourceRetention.ReserveRelease(id);
                var prepare = Request(SessionControlKind.PrepareUnload, id);
                bool committed = false;
                try
                {
                    var prepared = await SendAsync(prepare, token);
                    if (!prepared.Applied) throw new InvalidOperationException("Quest: " + prepared.Message);
                    var result = await SendAsync(Request(SessionControlKind.CommitUnload, id, body: prepare.OperationId.ToByteArray()), token);
                    if (!result.Applied) throw new InvalidOperationException("Quest: " + result.Message);
                    if (!remoteOnly) AtlasResources.Unload(id, localReservation);
                    m_PendingRemoteUnloads.Remove(id);
                    SessionAtlasCatalog.Remove(id);
                    committed = true;
                }
                finally
                {
                    if (!committed && !m_Stop.IsCancellationRequested)
                        try
                        {
                            await SendAsync(Request(SessionControlKind.AbortUnload, id, body: prepare.OperationId.ToByteArray()));
                        }
                        catch (Exception)
                        {
                            /* Remote reservation also expires. */
                        }
                }
            }
        }

        private async UniTask ConfirmAsync(string id, CancellationToken token = default)
        {
            if (AtlasResources.Status(id).State != AtlasLoadState.Loaded) throw new InvalidOperationException("Desktop atlas is not loaded: " + id);
            var response = await SendAsync(Request(SessionControlKind.ConfirmAtlas, id), token);
            if (!response.Applied) throw new InvalidOperationException(response.Message);
            SessionAtlasCatalog.SetReady(id, m_CatalogGeneration);
            m_ConfirmedAtlases.Add(id);
        }

        private void SetAtlasState(string id, string desktop, string quest)
        {
            if (m_Disposed) return;
            m_AtlasStates[id] = id + ": Desktop " + desktop + "; Quest " + quest;
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            AtlasStatus = Status + (m_AtlasStates.Count == 0 ? "" : "\n" + string.Join("\n", m_AtlasStates.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => entry.Value)));
            OnChanged.Invoke();
        }

        private void SetStatus(string value)
        {
            if (m_Disposed) return;
            Status = value;
            RefreshStatus();
        }

        private async UniTask FailedAsync(string message, Exception exception)
        {
            await UniTask.SwitchToMainThread();
            if (m_Disposed) return;
            if (exception is TimeoutException) message = "Quest is unavailable or asleep. Desktop atlases remain usable and will synchronize automatically when the headset is awake.";
            SetStatus(message);
            if (exception is TimeoutException || exception is IOException || exception is System.Net.Sockets.SocketException || exception is System.Security.Authentication.AuthenticationException)
            {
                m_Started = false;
                m_Stop.Cancel();
                SessionAtlasCatalog.End(m_CatalogGeneration);
                OnConnectionLost.Invoke();
            }
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Started = false;
            m_Preferences.OnSavePreferences.RemoveListener(OnPreferencesSaved);
            AtlasResources.OnChanged.RemoveListener(AtlasChanged);
            m_Stop.Cancel();
            SessionAtlasCatalog.End(m_CatalogGeneration);
            m_Opened.TrySetCanceled();
            // Pending native jobs observe completion before their request owner releases resources.
        }
    }
}
