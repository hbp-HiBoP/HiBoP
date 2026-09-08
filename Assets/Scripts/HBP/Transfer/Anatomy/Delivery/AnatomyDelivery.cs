using System;
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
        public string TransferId { get; }
        public string SessionId { get; }
        public string ContentHash { get; }
        public int EncodedBytes => payload.Length;

        public AnatomyDelivery(AnatomySnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            payload = AnatomySnapshotCodec.Encode(snapshot);
            if (payload.Length > PinnedTlsTransfer.MaximumPayloadBytes) throw new ArgumentException("Delivery exceeds the transport limit of 64 MiB.");
            TransferId = snapshot.TransferId;
            SessionId = snapshot.SessionId;
            ContentHash = new DeliveryReceipt(TransportIdentity.Hash(payload), DeliveryStatus.Published).ContentHash;
        }

        /// <summary>Listener must already be started. Keep the certificate alive until this task ends.</summary>
        public Task ServeAsync(TcpListener listener, X509Certificate2 identity, byte[] pairingSecret, CancellationToken stop, Action<string> record, Action<DeliveryReceipt> acknowledged = null) => PinnedTlsTransfer.ServeAsync(listener, identity, pairingSecret, payload, stop, record, acknowledged);
    }
}
