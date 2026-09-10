using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using UnityEngine;

namespace HBP.Quest
{
    public enum AnatomyReceptionState
    {
        Idle,
        Connecting,
        Receiving,
        Preparing
    }

    /// <summary>Owns delivery identity independently of connections; all publication/lifecycle changes use Unity's thread.</summary>
    public sealed class QuestAnatomySession : MonoBehaviour
    {
        public const int MaximumDeliveryHistory = 256;
        [SerializeField] private QuestAnatomyView view;
        private readonly Dictionary<string, Entry> deliveries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private SynchronizationContext unityContext;
        private int mainThread;
        private CancellationTokenSource reception;
        private Entry current;
        private bool destroyed;
        private PairingContext globals;
        private SceneArchive globalArchive;
        public AnatomyReceptionState ReceptionState { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsReady => current != null && view != null && view.Scene != null;
        public string TransferId => current?.TransferId;
        public string ContentHash => current?.ContentHash;
        public string SessionId => current?.SessionId;
        public string LastError { get; private set; }
        public long ReceivedBytes { get; private set; }

        private sealed class Entry
        {
            public string TransferId;
            public string SessionId;
            public string ContentHash;
            public DeliveryStatus Status;
        }

        private void Awake()
        {
            mainThread = Thread.CurrentThread.ManagedThreadId;
            unityContext = SynchronizationContext.Current;
        }

        /// <summary>Accept an authenticated incoming stream using the same publication and cancellation owner.</summary>
        public async Task<DeliveryReceipt> ReceiveStreamAsync(Stream stream, CancellationToken stop)
        {
            Task<DeliveryReceipt> receptionTask = await OnUnityThreadAsync(() => ReceiveCoreAsync(async (token, publish, progress) =>
            {
                using var closeStream = token.Register(stream.Dispose);
                return await ReceiveFileAsync(stream, token, publish, progress).ConfigureAwait(false);
            }, stop), stop).ConfigureAwait(false);
            return await receptionTask.ConfigureAwait(false);
        }

        public async Task<DeliveryReceipt> ReceiveGlobalsAsync(Stream stream, CancellationToken stop)
        {
            Task<DeliveryReceipt> work = await OnUnityThreadAsync(() => ReceiveCoreAsync(async (token, publish, progress) =>
            {
                using var close = token.Register(stream.Dispose);
                return await ReceiveFileAsync(stream, token, publish, progress).ConfigureAwait(false);
            }, stop, InstallGlobalsAsync), stop).ConfigureAwait(false);
            return await work.ConfigureAwait(false);
        }

        private async Task<DeliveryStatus> InstallGlobalsAsync(string file, string hash, CancellationToken stop)
        {
            string directory = await OnUnityThreadAsync(() => Path.Combine(Application.temporaryCachePath, "PairingData", Guid.NewGuid().ToString("N")), stop).ConfigureAwait(false);
            var archive = new SceneArchive(directory, true);
            bool consumed = false;
            try
            {
                var candidate = await Task.Run(() =>
                {
                    var context = new PairingContext(archive.ReadGlobalData(file, stop));
                    context.RestoreFilterPresets(archive);
                    return context;
                }, stop).ConfigureAwait(false);
                Task installation = await OnUnityThreadAsync(async () =>
                {
                    if (!HBP.Core.Preferences.PersistentDataManager.IsInitialized || !HBP.Core.Database.DatabaseManager.IsInitialized)
                        throw new InvalidOperationException("The Quest common services must be initialized before pairing.");
                    await view.ClearAsync();
                    stop.ThrowIfCancellationRequested();
                    if (destroyed) throw new ObjectDisposedException(nameof(QuestAnatomySession));
                    HBP.Core.Preferences.PersistentDataManager.ApplySessionData(candidate.Data.Preferences, candidate.Data.Tags, candidate.Data.Aliases, candidate.FilterPresets);
                    HBP.Core.Database.DatabaseManager.Database.SetProtocols(candidate.Data.Protocols, new HBP.Core.Data.ValidationRequest(HBP.Core.Data.ValidationAspect.None));
                    HBP.Core.DLL.ActivityProjectionSettings.VolumeGridDimension = candidate.Data.Grid;
                    HBP.Core.DLL.ActivityProjectionSettings.VolumeInterpolation = candidate.Data.Interpolation;
                    var previous = globalArchive;
                    globals = candidate;
                    globalArchive = archive;
                    consumed = true;
                    current = null;
                    deliveries.Clear();
                    previous?.Dispose();
                }, stop).ConfigureAwait(false);
                await installation.ConfigureAwait(false);
                return DeliveryStatus.Published;
            }
            finally
            {
                if (!consumed) archive.Dispose();
            }
        }

        private async Task<DeliveryReceipt> ReceiveCoreAsync(Func<CancellationToken, Func<string, string, CancellationToken, Task<DeliveryStatus>>, Action<long>, Task<DeliveryReceipt>> receive, CancellationToken stop, Func<string, string, CancellationToken, Task<DeliveryStatus>> prepare = null)
        {
            RequireMainThread();
            if (destroyed || !isActiveAndEnabled) throw new InvalidOperationException("The session receiver is unavailable.");
            if (view == null || unityContext == null) throw new InvalidOperationException("Missing serialized view or Unity synchronization context.");
            if (reception != null) throw new InvalidOperationException("A delivery is already in progress.");
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(stop);
            reception = attempt;
            LastError = null;
            ReceivedBytes = 0;
            ReceptionState = AnatomyReceptionState.Connecting;
            try
            {
                return await receive(attempt.Token, prepare ?? PrepareAsync, count => unityContext.Post(_ =>
                {
                    if (destroyed || reception != attempt || attempt.IsCancellationRequested) return;
                    IsConnected = true;
                    ReceptionState = AnatomyReceptionState.Receiving;
                    ReceivedBytes = count;
                }, null));
            }
            catch (Exception exception)
            {
                // No resource release here: ACK loss can follow a successful publication.
                LastError = exception.Message;
                throw;
            }
            finally
            {
                reception = null;
                IsConnected = false;
                ReceptionState = AnatomyReceptionState.Idle;
            }
        }

        private async Task<DeliveryReceipt> ReceiveFileAsync(Stream stream, CancellationToken stop, Func<string, string, CancellationToken, Task<DeliveryStatus>> publish, Action<long> progress)
        {
            string file = Path.Combine(Application.temporaryCachePath, "scene-" + Guid.NewGuid().ToString("N") + ".hbscene");
            try
            {
                return await PinnedTlsTransfer.ReceiveFileAsync(stream, file, stop, publish, progress).ConfigureAwait(false);
            }
            finally
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }

        private async Task<DeliveryStatus> PrepareAsync(string file, string hash, CancellationToken stop)
        {
            string directory = await OnUnityThreadAsync(() =>
            {
                ReceptionState = AnatomyReceptionState.Preparing;
                return Path.Combine(Application.temporaryCachePath, "SceneRestoration", Guid.NewGuid().ToString("N"));
            }, stop).ConfigureAwait(false);
            var archive = new SceneArchive(directory, true, globals);
            bool consumed = false;
            try
            {
                ScenePayload payload = await Task.Run(() => archive.Read(file, stop), stop).ConfigureAwait(false);
                Task<DeliveryStatus> publication = await OnUnityThreadAsync(() => PublishAsync(payload, archive, hash, stop), stop).ConfigureAwait(false);
                DeliveryStatus result = await publication.ConfigureAwait(false);
                consumed = result == DeliveryStatus.Published;
                return result;
            }
            finally
            {
                if (!consumed) archive.Dispose();
            }
        }

        private async Task<DeliveryStatus> PublishAsync(ScenePayload snapshot, SceneArchive archive, string hash, CancellationToken stop)
        {
            if (deliveries.TryGetValue(snapshot.TransferId, out Entry existing))
            {
                if (existing.ContentHash != hash) throw new InvalidDataException("Transfer identity reused with different content.");
                return existing == current ? DeliveryStatus.AlreadyPublished : existing.Status;
            }

            // Never evict tombstones: that would allow a delayed retry to resurrect old content.
            if (deliveries.Count >= MaximumDeliveryHistory) throw new InvalidOperationException("Delivery history is full; reopen the receiver before sending new identities.");
            var next = new Entry { TransferId = snapshot.TransferId, SessionId = snapshot.SessionId, ContentHash = hash, Status = DeliveryStatus.Published };
            deliveries.Add(next.TransferId, next); // Allocate metadata before the renderer commit.
            try
            {
                await view.ApplyAsync(snapshot, archive, stop); // ACK only after complete common rendering and publication.
            }
            catch
            {
                deliveries.Remove(next.TransferId);
                throw;
            }

            if (current != null) current.Status = DeliveryStatus.Superseded;
            current = next;
            return DeliveryStatus.Published;
        }

        private async Task<T> OnUnityThreadAsync<T>(Func<T> action, CancellationToken stop)
        {
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            int dispatched = 0;
            using var cancelled = stop.Register(() =>
            {
                // Once action starts, its returned task owns cancellation and cleanup. Do not
                // detach an in-flight async publication and release the reception slot early.
                if (Interlocked.CompareExchange(ref dispatched, 1, 0) == 0) completion.TrySetCanceled(stop);
            });
            unityContext.Post(_ =>
            {
                if (Interlocked.CompareExchange(ref dispatched, 1, 0) != 0) return;
                if (stop.IsCancellationRequested || destroyed)
                {
                    completion.TrySetCanceled();
                    return;
                }

                try
                {
                    completion.TrySetResult(action());
                }
                catch (Exception exception)
                {
                    completion.TrySetException(exception);
                }
            }, null);
            return await completion.Task.ConfigureAwait(false);
        }

        public void Disconnect()
        {
            RequireMainThread();
            reception?.Cancel();
            IsConnected = false;
        }

        public void CloseSession()
        {
            RequireMainThread();
            Disconnect(); // Cancels queued publication before freeing resources.
            if (current != null) current.Status = DeliveryStatus.Closed;
            current = null;
            if (view != null) view.Clear();
        }

        private void RequireMainThread()
        {
            if (mainThread != Thread.CurrentThread.ManagedThreadId) throw new InvalidOperationException("Use the session on Unity's main thread.");
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Disconnect();
        }

        private void OnDisable() => Disconnect();

        private void OnDestroy()
        {
            destroyed = true;
            CloseSession();
            deliveries.Clear();
            _ = ReleaseGlobalsAsync();
        }

        private async Task ReleaseGlobalsAsync()
        {
            try
            {
                if (!ReferenceEquals(view, null)) await view.ClearAsync();
                globalArchive?.Dispose();
                globalArchive = null;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
