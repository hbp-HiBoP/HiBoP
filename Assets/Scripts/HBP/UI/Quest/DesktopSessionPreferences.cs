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
        public bool QuestHasSharedScene { get; private set; }
        public string Status { get; private set; } = "Quest session is connecting.";
        private readonly Dictionary<string, string> m_AtlasStates = new(StringComparer.Ordinal);
        public string AtlasStatus { get; private set; } = "Quest session is connecting.";
        public string RetryAtlasId { get; private set; }
        private bool m_RetryLoad;
        public event Action Changed;
        public event Action ConnectionLost;

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

        private async Task<SessionControlResponse> SendAsync(SessionControlRequest request)
        {
            m_Stop.Token.ThrowIfCancellationRequested();
            var response = await m_Send(request, m_Stop.Token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread();
            m_Stop.Token.ThrowIfCancellationRequested();
            QuestHasSharedScene = response.HasSharedScene;
            return response;
        }

        public async Task StartAsync()
        {
            using var timing = PairingTiming.Measure("desktop.session-open");
            var response = await SendAsync(Request(SessionControlKind.Open));
            if (!response.Applied) throw new InvalidOperationException(response.Message);
            if (response.Message != SessionControlCodec.AtlasInventoryCapability) throw new InvalidOperationException("Update HiBoP Desktop and Quest together: the Quest session protocol is incompatible.");
            SessionAtlasCatalog.Begin();
            m_Started = true;
            m_Opened.TrySetResult(true);
            SetStatus("Quest connected. Preferences and manual atlas actions are synchronized.");
            AtlasResources.Changed += AtlasChanged;
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
                                if (remote.State == AtlasLoadState.Failed)
                                {
                                    RetryAtlasId = entry.Key;
                                    m_RetryLoad = true;
                                }

                                continue;
                            }

                            if (local.State == AtlasLoadState.Loaded && !m_ConfirmedAtlases.Contains(entry.Key))
                                await ConfirmAsync(entry.Key);
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
                await FailedAsync("Quest atlas preparation: " + exception.Message, exception);
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
                ConnectionLost?.Invoke();
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
                await FailedAsync("Preferences saved on Desktop; Quest: " + exception.Message, exception);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                m_QueuedSaves--;
            }
        }

        public Task FlushPreferencesAsync() => m_PendingPreferences;

        public async Task EnsureAtlasReadyAsync(string id)
        {
            if (SessionAtlasCatalog.CanUse(id)) return;
            await SetAtlasLoadedAsync(id, true);
            if (!SessionAtlasCatalog.CanUse(id)) throw new InvalidOperationException(Status);
        }

        public async Task<string> ValidateNormalizationAsync(NormalizationType requested, bool desktopHasSharedScene)
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
                return "The Quest scene state could not be checked: " + exception.Message;
            }
        }

        public async Task SetAtlasLoadedAsync(string id, bool loaded)
        {
            try
            {
                await m_Gate.WaitAsync(m_Stop.Token);
                try
                {
                    SetAtlasState(id, loaded ? "loading" : "checking usage", loaded ? "loading" : "checking usage");
                    SetStatus(id + ": " + (loaded ? "loading on Desktop and Quest..." : "checking Desktop and Quest usage..."));
                    if (loaded)
                    {
                        // Both loads start before either is awaited. Observe both results, including native completion.
                        var localWork = AtlasResources.LoadAsync(id, m_Stop.Token).AsTask();
                        var remoteWork = SendAsync(Request(SessionControlKind.LoadAtlas, id));
                        var local = await localWork;
                        SessionControlResponse remote = null;
                        try
                        {
                            remote = await remoteWork;
                        }
                        catch (Exception exception)
                        {
                            await FailedAsync(id + ": Desktop " + local.State + "; Quest: " + exception.Message, exception);
                        }

                        SetAtlasState(id, local.State.ToString(), remote == null ? "connection lost" : remote.Applied ? "loaded" : remote.Message);
                        if (remote == null)
                        {
                            RetryAtlasId = id;
                            m_RetryLoad = true;
                            Changed?.Invoke();
                            return;
                        }

                        if (!local.Succeeded || !remote.Applied)
                        {
                            RetryAtlasId = id;
                            m_RetryLoad = true;
                            SetStatus(id + ": Desktop " + local.State + "; Quest " + (remote.Applied ? "loaded" : remote.Message) + "; " + local.Error);
                            return;
                        }

                        await ConfirmAsync(id);
                    }
                    else
                    {
                        using var localReservation = ResourceRetention.ReserveRelease(id);
                        var prepare = Request(SessionControlKind.PrepareUnload, id);
                        var prepared = await SendAsync(prepare);
                        if (!prepared.Applied) throw new InvalidOperationException("Quest: " + prepared.Message);
                        bool committed = false;
                        try
                        {
                            var result = await SendAsync(Request(SessionControlKind.CommitUnload, id, body: prepare.OperationId.ToByteArray()));
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

                    SetAtlasState(id, loaded ? "loaded" : "unloaded", loaded ? "loaded" : "unloaded");
                    RetryAtlasId = null;
                    SetStatus(id + ": Desktop and Quest " + (loaded ? "loaded." : "unloaded."));
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
                RetryAtlasId = id;
                m_RetryLoad = loaded;
                SetAtlasState(id, AtlasResources.Status(id).State.ToString(), exception.Message);
                await FailedAsync(id + ": " + exception.Message, exception);
            }
        }

        public Task RetryAtlasAsync() => RetryAtlasId == null ? Task.CompletedTask : SetAtlasLoadedAsync(RetryAtlasId, m_RetryLoad);

        private async Task ConfirmAsync(string id)
        {
            using var retention = ResourceRetention.Retain(id);
            if (AtlasResources.Status(id).State != AtlasLoadState.Loaded) throw new InvalidOperationException("Desktop atlas is not loaded: " + id);
            var response = await SendAsync(Request(SessionControlKind.ConfirmAtlas, id));
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
            Changed?.Invoke();
        }

        private void SetStatus(string value)
        {
            if (m_Disposed) return;
            Status = value;
            RefreshStatus();
        }

        private async Task FailedAsync(string message, Exception exception)
        {
            await UniTask.SwitchToMainThread();
            if (m_Disposed) return;
            SetStatus(message);
            if (exception is IOException || exception is System.Net.Sockets.SocketException || exception is System.Security.Authentication.AuthenticationException)
            {
                m_Started = false;
                m_Stop.Cancel();
                SessionAtlasCatalog.End();
                ConnectionLost?.Invoke();
            }
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Started = false;
            m_Preferences.OnSavePreferences.RemoveListener(OnPreferencesSaved);
            AtlasResources.Changed -= AtlasChanged;
            m_Stop.Cancel();
            SessionAtlasCatalog.End();
            m_Opened.TrySetCanceled();
            // Pending native jobs observe completion before their request owner releases resources.
        }
    }
}
