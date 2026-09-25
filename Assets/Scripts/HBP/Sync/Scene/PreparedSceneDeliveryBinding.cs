using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;

namespace HBP.Sync.Scene
{
    public sealed class V2PreparedSceneIdentity
    {
        public SessionId SessionId { get; }
        public SceneId SceneId { get; }
        public IncarnationId IncarnationId { get; }

        private V2PreparedSceneIdentity(SessionId sessionId, SceneId sceneId, IncarnationId incarnationId)
        {
            if (sessionId.Value == sceneId.Value || sessionId.Value == incarnationId.Value || sceneId.Value == incarnationId.Value)
                throw new InvalidDataException("The prepared scene identities must be distinct.");
            SessionId = sessionId;
            SceneId = sceneId;
            IncarnationId = incarnationId;
        }

        public static V2PreparedSceneIdentity Create(string globalContextId, string visualizationId, string transferId)
        {
            Guid session = ParseGuid(globalContextId, nameof(globalContextId));
            Guid incarnation = ParseGuid(transferId, nameof(transferId));
            Guid scene;
            if (!Guid.TryParse(visualizationId, out scene))
            {
                using SHA256 sha = SHA256.Create();
                byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes("HBP.Sync.Scene:" + (visualizationId ?? throw new ArgumentNullException(nameof(visualizationId)))));
                var sceneBytes = new byte[16];
                Buffer.BlockCopy(digest, 0, sceneBytes, 0, sceneBytes.Length);
                scene = new Guid(sceneBytes);
            }

            if (scene == Guid.Empty) throw new InvalidDataException("The visualization has an empty v2 scene identity.");
            return new V2PreparedSceneIdentity(new SessionId(session), new SceneId(scene), new IncarnationId(incarnation));
        }

        private static Guid ParseGuid(string value, string parameterName)
        {
            if (!Guid.TryParse(value, out Guid parsed) || parsed == Guid.Empty)
                throw new InvalidDataException("The prepared scene has an invalid " + parameterName + ".");
            return parsed;
        }
    }

    /// <summary>The published initial delivery that owns an S2 synchronization epoch.</summary>
    public sealed class PreparedSceneDeliveryBinding
    {
        public string ManifestHash { get; }
        public string VisualizationId { get; }
        public string TransferId { get; }
        public string SessionId { get; }
        public string GlobalContextId => Manifest.GlobalContextId;
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

        public V2PreparedSceneIdentity CreateV2Identity() => V2PreparedSceneIdentity.Create(GlobalContextId, VisualizationId, TransferId);

        private static void ValidateReceipt(DeliveryReceipt receipt)
        {
            if (receipt == null || receipt.Status is not (DeliveryStatus.Published or DeliveryStatus.AlreadyPublished))
                throw new InvalidDataException("The initial scene delivery has not been published.");
        }
    }
}
