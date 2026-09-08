using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Anatomy;
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
        public AnatomyReceptionState ReceptionState { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsReady => current != null && view != null && view.SharedMesh != null;
        public string TransferId => current?.TransferId;
        public string ContentHash => current?.ContentHash;
        public string SessionId => current?.SessionId;
        public string LastError { get; private set; }
        public int ReceivedBytes { get; private set; }

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

        /// <summary>Call on the main thread. One attempt at a time; interruption restarts from byte zero.</summary>
        public async Task<DeliveryReceipt> ReceiveAsync(string host, int port, byte[] pin, byte[] secret, CancellationToken stop = default)
        {
            return await ReceiveCoreAsync((token, publish, progress) => PinnedTlsTransfer.ReceiveAsync(host, port, pin, secret, token, publish, progress), stop);
        }

        /// <summary>Accept an authenticated incoming stream using the same publication and cancellation owner.</summary>
        public async Task<DeliveryReceipt> ReceiveStreamAsync(Stream stream, CancellationToken stop)
        {
            Task<DeliveryReceipt> receptionTask = await OnUnityThreadAsync(() => ReceiveCoreAsync(async (token, publish, progress) =>
            {
                using var closeStream = token.Register(stream.Dispose);
                return await PinnedTlsTransfer.ReceivePayloadAsync(stream, token, publish, progress).ConfigureAwait(false);
            }, stop), stop).ConfigureAwait(false);
            return await receptionTask.ConfigureAwait(false);
        }

        private async Task<DeliveryReceipt> ReceiveCoreAsync(Func<CancellationToken, Func<byte[], CancellationToken, Task<DeliveryStatus>>, Action<int>, Task<DeliveryReceipt>> receive, CancellationToken stop)
        {
            RequireMainThread();
            if (destroyed || !isActiveAndEnabled) throw new InvalidOperationException("The session receiver is unavailable.");
            if (view == null || unityContext == null) throw new InvalidOperationException("Missing serialized view or Unity synchronization context.");
            if (reception != null) throw new InvalidOperationException("A delivery is already in progress.");
            // Local file injection and network delivery cannot both own this view.
            var diagnostic = GetComponentInParent<QuestAnatomyDiagnostic>();
            if (diagnostic != null && diagnostic.enabled) diagnostic.enabled = false;
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(stop);
            reception = attempt;
            LastError = null;
            ReceivedBytes = 0;
            ReceptionState = AnatomyReceptionState.Connecting;
            try
            {
                return await receive(attempt.Token, (bytes, token) => PrepareAsync(bytes, token), count => unityContext.Post(_ =>
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

        private async Task<DeliveryStatus> PrepareAsync(byte[] bytes, CancellationToken stop)
        {
            await OnUnityThreadAsync(() =>
            {
                ReceptionState = AnatomyReceptionState.Preparing;
                return true;
            }, stop).ConfigureAwait(false);
            // The transport owns this complete, hash-checked buffer. Neither decoder nor renderer mutates it.
            AnatomySnapshot snapshot = await Task.Run(() => AnatomySnapshotCodec.Decode(bytes), stop).ConfigureAwait(false);
            string hash = new DeliveryReceipt(TransportIdentity.Hash(bytes), DeliveryStatus.Published).ContentHash;
            return await OnUnityThreadAsync(() => Publish(snapshot, hash), stop).ConfigureAwait(false);
        }

        private DeliveryStatus Publish(AnatomySnapshot snapshot, string hash)
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
                view.ApplySnapshot(snapshot); // Unique publication point, synchronous; failed staging preserves the previous view.
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
            using var cancelled = stop.Register(() => completion.TrySetCanceled(stop));
            unityContext.Post(_ =>
            {
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
        }
    }
}
