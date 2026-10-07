using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using Newtonsoft.Json;

namespace HBP.Sync.Scene
{
    /// <summary>One in-memory pairing policy owner, independent of scene replica connections.</summary>
    public sealed class SessionPreferencesReceiver
    {
        private readonly Func<Guid> m_Context;
        private readonly Func<UserPreferences> m_Preferences;
        private readonly Func<bool> m_HasScene;
        private readonly Func<SessionAtlasInventory> m_Inventory;
        private readonly SemaphoreSlim m_Gate = new(1, 1);
        private CancellationTokenSource m_Epoch = new();
        private readonly Dictionary<string, PendingRelease> m_Releases = new(StringComparer.Ordinal);
        private readonly Dictionary<Guid, (byte[] Request, SessionControlResponse Response)> m_Completed = new();
        private readonly Queue<Guid> m_History = new();
        private Guid m_ContextId;
        private long m_Generation, m_Revision;
        private readonly Stopwatch m_Clock = Stopwatch.StartNew();

        private sealed class PendingRelease
        {
            public Guid Operation;
            public ResourceRetention.Reservation Reservation;
            public double Expires;
        }

        public SessionPreferencesReceiver(Func<Guid> context, Func<UserPreferences> preferences, Func<bool> hasScene, Func<SessionAtlasInventory> inventory = null)
        {
            m_Context = context;
            m_Preferences = preferences;
            m_HasScene = hasScene;
            m_Inventory = inventory;
        }

        public void Tick()
        {
            foreach (var entry in m_Releases.Where(entry => entry.Value.Expires <= m_Clock.Elapsed.TotalSeconds).ToArray())
            {
                entry.Value.Reservation.Dispose();
                m_Releases.Remove(entry.Key);
            }
        }

        public async Task CloseAsync()
        {
            m_Epoch.Cancel();
            await m_Gate.WaitAsync();
            try
            {
                await UniTask.SwitchToMainThread();
                Reset();
                m_ContextId = Guid.Empty;
                m_Generation = 0;
                SessionAtlasCatalog.End();
                m_Epoch = new();
            }
            finally
            {
                m_Gate.Release();
            }
        }

        private void Reset()
        {
            foreach (var pending in m_Releases.Values) pending.Reservation.Dispose();
            m_Releases.Clear();
            m_Completed.Clear();
            m_History.Clear();
            m_Revision = 0;
        }

        public async Task<SessionControlResponse> HandleAsync(SessionControlRequest request, CancellationToken token)
        {
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token, m_Epoch.Token);
            token = lifetime.Token;
            PreferencesSnapshot preferences = null;
            try
            {
                if (request.Kind == SessionControlKind.Preferences) preferences = await Task.Run(() => PreferencesSnapshot.Read(request.GetBody()), token).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                return new(request.OperationId, SessionControlStatus.Rejected, exception.Message);
            }

            await m_Gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await UniTask.SwitchToMainThread();
                token.ThrowIfCancellationRequested();
                Tick();
                if (request.ContextId != m_Context()) return Reply(request, SessionControlStatus.Stale, "The pairing context has changed.");
                if (request.Kind == SessionControlKind.Open)
                {
                    if (request.ContextId == m_ContextId && request.Generation < m_Generation) return Reply(request, SessionControlStatus.Stale, "Old connection generation.");
                    if (request.ContextId != m_ContextId || request.Generation != m_Generation)
                    {
                        Reset();
                        m_ContextId = request.ContextId;
                        m_Generation = request.Generation;
                        SessionAtlasCatalog.Begin();
                    }
                }
                else if (request.ContextId != m_ContextId || request.Generation != m_Generation) return Reply(request, SessionControlStatus.Stale, "Old connection generation.");

                byte[] encoded = SessionControlCodec.Encode(request);
                if (m_Completed.TryGetValue(request.OperationId, out var completed))
                    return encoded.SequenceEqual(completed.Request) ? completed.Response : Reply(request, SessionControlStatus.Rejected, "Operation identity was reused with different data.");
                SessionControlResponse response;
                switch (request.Kind)
                {
                    case SessionControlKind.Inspect:
                        response = Reply(request, SessionControlStatus.Applied, body: PreferencesSnapshot.Capture(m_Preferences()));
                        break;
                    case SessionControlKind.Open:
                        var loaded = new Dictionary<string, string>(StringComparer.Ordinal);
                        foreach (var definition in AtlasResources.Definitions)
                            if (AtlasResources.IsLoaded(definition.Id))
                            {
                                var result = AtlasResources.Status(definition.Id);
                                if (result.State == AtlasLoadState.Loaded && result.Fingerprint != null) loaded.Add(definition.Id, result.Fingerprint);
                            }

                        response = Reply(request, SessionControlStatus.Applied, SessionControlCodec.AtlasInventoryCapability, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(loaded)));
                        break;
                    case SessionControlKind.AtlasInventory:
                        var inventory = m_Inventory?.Invoke() ?? new SessionAtlasInventory();
                        response = Reply(request, SessionControlStatus.Applied, body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(inventory)));
                        break;
                    case SessionControlKind.Preferences:
                        if (request.Revision <= m_Revision) return Reply(request, SessionControlStatus.Stale, "A newer preferences revision is already applied.");
                        preferences.Apply(m_Preferences(), m_HasScene());
                        m_Revision = request.Revision;
                        response = Reply(request, SessionControlStatus.Applied);
                        break;
                    case SessionControlKind.LoadAtlas:
                        var load = await AtlasResources.LoadAsync(request.AtlasId, token);
                        await UniTask.SwitchToMainThread();
                        response = Reply(request, load.Succeeded ? SessionControlStatus.Applied : SessionControlStatus.Failed, load.Error, load.Fingerprint);
                        break;
                    case SessionControlKind.ConfirmAtlas:
                        if (!AtlasResources.IsLoaded(request.AtlasId) || AtlasResources.Status(request.AtlasId).Fingerprint != request.Fingerprint) return Reply(request, SessionControlStatus.Rejected, "Atlas content differs between devices.");
                        SessionAtlasCatalog.SetReady(request.AtlasId, request.Fingerprint);
                        response = Reply(request, SessionControlStatus.Applied, fingerprint: request.Fingerprint);
                        break;
                    case SessionControlKind.PrepareUnload:
                        if (m_Releases.ContainsKey(request.AtlasId)) throw new InvalidOperationException("An unload is already pending.");
                        var reservation = ResourceRetention.ReserveRelease(request.AtlasId);
                        m_Releases.Add(request.AtlasId, new PendingRelease { Operation = request.OperationId, Reservation = reservation, Expires = m_Clock.Elapsed.TotalSeconds + 30 });
                        response = Reply(request, SessionControlStatus.Applied);
                        break;
                    case SessionControlKind.CommitUnload:
                    case SessionControlKind.AbortUnload:
                        byte[] owner = request.GetBody();
                        if (owner.Length != 16 || !m_Releases.TryGetValue(request.AtlasId, out var release) || release.Operation != new Guid(owner)) return Reply(request, SessionControlStatus.Rejected, "Unload reservation is missing or expired.");
                        if (request.Kind == SessionControlKind.CommitUnload)
                        {
                            AtlasResources.Unload(request.AtlasId, release.Reservation);
                            SessionAtlasCatalog.Remove(request.AtlasId);
                        }
                        else release.Reservation.Dispose();

                        m_Releases.Remove(request.AtlasId);
                        response = Reply(request, SessionControlStatus.Applied);
                        break;
                    default: throw new InvalidDataException("Unsupported session request.");
                }

                token.ThrowIfCancellationRequested();
                if (request.ContextId != m_Context()) return Reply(request, SessionControlStatus.Stale, "The pairing context has changed.");
                m_Completed.Add(request.OperationId, (encoded, response));
                m_History.Enqueue(request.OperationId);
                while (m_History.Count > 64) m_Completed.Remove(m_History.Dequeue());
                return response;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await UniTask.SwitchToMainThread();
                return Reply(request, SessionControlStatus.Failed, exception.Message);
            }
            finally
            {
                m_Gate.Release();
            }
        }

        private SessionControlResponse Reply(SessionControlRequest request, SessionControlStatus status, string message = "", string fingerprint = "", byte[] body = null) => new(request.OperationId, status, message, fingerprint, m_HasScene(), body);
    }
}
