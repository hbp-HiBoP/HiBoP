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

        public DesktopSessionPreferences(Guid context, long generation, UserPreferences preferences, Func<SessionControlRequest, CancellationToken, Task<SessionControlResponse>> send, UserPreferences pairingPreferences = null)
        {
            m_Context = context;
            m_Generation = generation;
            m_Preferences = preferences;
            m_Send = send;
            m_PairingPreferences = pairingPreferences;
            m_Preferences.OnSavePreferences.AddListener(OnPreferencesSaved);
        }

        private SessionControlRequest Request(SessionControlKind kind, string atlas = "", byte[] body = null, long revision = 0) => new(m_Context, m_Generation, Guid.NewGuid(), kind, revision, atlas, body: body);

        private async UniTask<SessionControlResponse> SendAsync(SessionControlRequest request, CancellationToken cancellationToken = default)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(m_Stop.Token, cancellationToken);
            m_Stop.Token.ThrowIfCancellationRequested();
            SessionControlResponse response;
            try
            {
                response = await m_Send(request, linked.Token).ConfigureAwait(false);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }

            linked.Token.ThrowIfCancellationRequested();
            QuestHasSharedScene = response.HasSharedScene;
            return response;
        }

        public async UniTask StartAsync(CancellationToken cancellationToken = default)
        {
            using var timing = PairingTiming.Measure("desktop.session-open");
            var response = await SendAsync(Request(SessionControlKind.Open), cancellationToken);
            if (!response.Applied) throw new InvalidOperationException(response.Message);
            if (response.Message != SessionControlCodec.AtlasInventoryCapability) throw new InvalidOperationException("Update HiBoP Desktop and Quest together: the Quest session protocol is incompatible.");
            SessionAtlasCatalog.Begin();
            m_Started = true;
            m_Opened.TrySetResult(true);
            SetStatus("Quest connected. Preferences and manual atlas actions are synchronized.");
            AtlasResources.OnChanged.AddListener(AtlasChanged);
            m_InventoryWork = RefreshInventoryAsync();
        }

        private void AtlasChanged(AtlasLoadResult result)
        {
            if (!m_Started || m_Disposed) return;
            m_AtlasChanges++;
            if (result.State == AtlasLoadState.Unloaded)
            {
                m_ConfirmedAtlases.Remove(result.Id);
                SessionAtlasCatalog.Remove(result.Id);
            }

            if (m_InventoryWork.IsCompleted) m_InventoryWork = RefreshInventoryAsync();
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
                        foreach (var entry in inventory.Atlases)
                        {
                            var local = AtlasResources.Status(entry.Key);
                            var remote = entry.Value;
                            if (m_ConfirmedAtlases.Contains(entry.Key) && (local.State != AtlasLoadState.Loaded || remote.State != AtlasLoadState.Loaded))
                            {
                                m_ConfirmedAtlases.Remove(entry.Key);
                                SessionAtlasCatalog.Remove(entry.Key);
                            }

                            if (remote.Preload || remote.State != AtlasLoadState.Unloaded || local.State != AtlasLoadState.Unloaded)
                                SetAtlasState(entry.Key, local.State.ToString(), remote.Error ?? remote.State.ToString());
                            if (remote.State != AtlasLoadState.Loaded)
                            {
                                if (m_ConfirmedAtlases.Remove(entry.Key)) SessionAtlasCatalog.Remove(entry.Key);
                                if (remote.State == AtlasLoadState.Failed && m_InventoryRecoveryAttempted.Add(entry.Key))
                                    await SetAtlasLoadedCoreAsync(entry.Key, true, m_Stop.Token);
                                m_Stop.Token.ThrowIfCancellationRequested();
                                continue;
                            }

                            if (local.State == AtlasLoadState.Failed && m_InventoryRecoveryAttempted.Add(entry.Key))
                                await SetAtlasLoadedCoreAsync(entry.Key, true, m_Stop.Token);
                            else if (local.State == AtlasLoadState.Loaded && !m_ConfirmedAtlases.Contains(entry.Key) && m_InventoryRecoveryAttempted.Add(entry.Key))
                                await SetAtlasLoadedCoreAsync(entry.Key, true, m_Stop.Token, confirmOnly: true);
                            m_Stop.Token.ThrowIfCancellationRequested();
                            if (m_ConfirmedAtlases.Contains(entry.Key)) m_InventoryRecoveryAttempted.Remove(entry.Key);
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
            if (SessionAtlasCatalog.CanUse(id)) return;
            await SetAtlasLoadedAsync(id, true, token, reportError: reportError);
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

        public async UniTask SetAtlasLoadedAsync(string id, bool loaded, CancellationToken cancellationToken = default, bool reportError = true)
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
                    await SetAtlasLoadedCoreAsync(id, loaded, token, reportError: reportError);
                }
                finally
                {
                    m_Gate.Release();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        // The caller owns m_Gate throughout recovery, including the delays between attempts.
        private async UniTask SetAtlasLoadedCoreAsync(string id, bool loaded, CancellationToken token, bool confirmOnly = false, bool reportError = true)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    if (confirmOnly && attempt == 0) await ConfirmAsync(id, token);
                    else await ApplyAtlasLoadedAsync(id, loaded, token);
                    SetAtlasState(id, loaded ? "loaded" : "unloaded", loaded ? "loaded" : "unloaded");
                    SetStatus(id + ": Desktop and Quest " + (loaded ? "loaded." : "unloaded."));
                    return;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    await UniTask.SwitchToMainThread();
                    token.ThrowIfCancellationRequested();
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

        private async UniTask ApplyAtlasLoadedAsync(string id, bool loaded, CancellationToken token)
        {
            SetAtlasState(id, loaded ? "loading" : "checking usage", loaded ? "loading" : "checking usage");
            SetStatus(id + ": " + (loaded ? "loading on Desktop and Quest..." : "checking Desktop and Quest usage..."));
            if (loaded)
            {
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
                using var localReservation = ResourceRetention.ReserveRelease(id);
                var prepare = Request(SessionControlKind.PrepareUnload, id);
                bool committed = false;
                try
                {
                    var prepared = await SendAsync(prepare, token);
                    if (!prepared.Applied) throw new InvalidOperationException("Quest: " + prepared.Message);
                    var result = await SendAsync(Request(SessionControlKind.CommitUnload, id, body: prepare.OperationId.ToByteArray()), token);
                    if (!result.Applied) throw new InvalidOperationException("Quest: " + result.Message);
                    AtlasResources.Unload(id, localReservation);
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
            using var retention = ResourceRetention.Retain(id);
            if (AtlasResources.Status(id).State != AtlasLoadState.Loaded) throw new InvalidOperationException("Desktop atlas is not loaded: " + id);
            var response = await SendAsync(Request(SessionControlKind.ConfirmAtlas, id), token);
            if (!response.Applied) throw new InvalidOperationException(response.Message);
            SessionAtlasCatalog.SetReady(id);
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
            SetStatus(message);
            if (exception is IOException || exception is System.Net.Sockets.SocketException || exception is System.Security.Authentication.AuthenticationException)
            {
                m_Started = false;
                m_Stop.Cancel();
                SessionAtlasCatalog.End();
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
            SessionAtlasCatalog.End();
            m_Opened.TrySetCanceled();
            // Pending native jobs observe completion before their request owner releases resources.
        }
    }
}
