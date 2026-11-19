using System;

namespace HBP.Transfer.Transport
{
    public enum DeliveryStatus : byte
    {
        Published = 1,
        AlreadyPublished = 2,
        Superseded = 3,
        Closed = 4
    }

    /// <summary>The SHA-256 covers the entire HBNA, including its transfer and session IDs.</summary>
    public sealed class DeliveryReceipt
    {
        public string ContentHash { get; }
        public DeliveryStatus Status { get; }

        public DeliveryReceipt(byte[] digest, DeliveryStatus status)
        {
            if (digest == null || digest.Length != 32) throw new ArgumentException("A SHA-256 digest is required.");
            if (status < DeliveryStatus.Published || status > DeliveryStatus.Closed) throw new ArgumentOutOfRangeException(nameof(status));
            ContentHash = BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant();
            Status = status;
        }
    }
}
