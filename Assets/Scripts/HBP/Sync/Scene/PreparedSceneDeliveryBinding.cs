using System;
using System.IO;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;

namespace HBP.Sync.Scene
{
    /// <summary>The published initial delivery that owns an S2 synchronization epoch.</summary>
    public sealed class PreparedSceneDeliveryBinding
    {
        public string ManifestHash { get; }
        public string VisualizationId { get; }
        public string TransferId { get; }
        public string SessionId { get; }
        internal PreparedSceneManifest Manifest { get; }

        private PreparedSceneDeliveryBinding(string manifestHash, PreparedSceneManifest manifest)
        {
            if (manifest == null || string.IsNullOrEmpty(manifest.VisualizationId) || string.IsNullOrEmpty(manifest.TransferId) || string.IsNullOrEmpty(manifest.SessionId))
                throw new InvalidDataException("The published scene payload has no stable identity.");
            ManifestHash = manifestHash;
            VisualizationId = manifest.VisualizationId;
            TransferId = manifest.TransferId;
            SessionId = manifest.SessionId;
            Manifest = manifest;
        }

        /// <summary>Bind Desktop only after the receiver acknowledges the exact sent delivery.</summary>
        public static PreparedSceneDeliveryBinding FromSent(SceneDelivery delivery, DeliveryReceipt receipt)
        {
            if (delivery == null) throw new ArgumentNullException(nameof(delivery));
            ValidateReceipt(receipt);
            if (!delivery.CanRetry || delivery.ContentHash != receipt.ContentHash)
                throw new InvalidDataException("The published receipt does not match the sent scene delivery.");
            PreparedSceneManifest manifest = delivery.RequirePreparedManifest();
            if (delivery.TransferId != manifest.TransferId || delivery.SessionId != manifest.SessionId)
                throw new InvalidDataException("The sent scene manifest does not match the delivery identity.");
            return new PreparedSceneDeliveryBinding(receipt.ContentHash, manifest);
        }

        /// <summary>Bind Quest only after its verified receiver publishes the prepared scene.</summary>
        public static PreparedSceneDeliveryBinding FromPublished(DeliveryReceipt receipt, RestoredScene restored)
        {
            ValidateReceipt(receipt);
            if (restored == null || restored.VerifiedContentHash != receipt.ContentHash || restored.PreparedManifest == null)
                throw new InvalidDataException("The published receipt does not match the restored scene content.");
            return new PreparedSceneDeliveryBinding(receipt.ContentHash, restored.PreparedManifest);
        }

        private static void ValidateReceipt(DeliveryReceipt receipt)
        {
            if (receipt == null || receipt.Status is not (DeliveryStatus.Published or DeliveryStatus.AlreadyPublished))
                throw new InvalidDataException("The initial scene delivery has not been published.");
        }
    }
}
