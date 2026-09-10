using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Transport;

namespace HBP.Transfer.Anatomy.Delivery
{
    /// <summary>Immutable offer: retain this object to retry the exact capture and identity.</summary>
    public sealed class AnatomyDelivery
    {
        private readonly byte[] payload;
        public string IEEGSummary { get; }
        public string TransferId { get; }
        public string SessionId { get; }
        public string ContentHash { get; }
        public int EncodedBytes => payload.Length;

        public AnatomyDelivery(AnatomySnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            payload = AnatomySnapshotCodec.Encode(snapshot);
            if (payload.Length > PinnedTlsTransfer.MaximumPayloadBytes) throw new ArgumentException($"Delivery requires {payload.Length} bytes (surface {snapshot.SurfaceByteLength}, volume {snapshot.Projection?.VolumeBytes.Count ?? 0}); transport limit is {PinnedTlsTransfer.MaximumPayloadBytes} bytes (64 MiB). Keep all scientific inputs; select a smaller source dataset or qualify a larger transfer/memory budget.");
            IEEGSummary = snapshot.IEEG?.Summary;
            TransferId = snapshot.TransferId;
            SessionId = snapshot.SessionId;
            ContentHash = new DeliveryReceipt(TransportIdentity.Hash(payload), DeliveryStatus.Published).ContentHash;
        }

        /// <summary>Listener must already be started. Keep the certificate alive until this task ends.</summary>
        public Task ServeAsync(TcpListener listener, X509Certificate2 identity, byte[] pairingSecret, CancellationToken stop, Action<string> record, Action<DeliveryReceipt> acknowledged = null) => PinnedTlsTransfer.ServeAsync(listener, identity, pairingSecret, payload, stop, record, acknowledged);

        public Task<DeliveryReceipt> SendAsync(Stream stream, CancellationToken stop, Action<int> progress = null) => PinnedTlsTransfer.SendPayloadAsync(stream, payload, stop, progress: progress);
    }
}
