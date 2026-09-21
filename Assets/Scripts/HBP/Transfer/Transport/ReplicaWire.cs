using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using HBP.Sync;

namespace HBP.Transfer.Transport
{
    public enum ReplicaFrameKind : byte
    {
        Snapshot = 1,
        Delta = 2,
        Resource = 3,
        Received = 4,
        Applied = 5,
        Visible = 6,
        Rejected = 7,
        Checkpoint = 8
    }

    public enum ReplicaReadStage : byte
    {
        FirstBytes,
        Complete
    }

    public enum ReplicaWriteStage : byte
    {
        FirstBytes,
        Complete
    }

    /// <summary>Length-bounded messages carried inside the existing authenticated TLS connection.</summary>
    public static class ReplicaWire
    {
        public const int MaximumFrameBytes = SharedStateSchema.MaxStateBytes + 1024;
        private static readonly UTF8Encoding Utf8 = new(false, true);

        public static async Task WriteAsync(Stream stream, ReplicaFrameKind kind, byte[] body, CancellationToken stop, Action<ReplicaWriteStage> progress = null)
        {
            if (body == null || body.Length > MaximumFrameBytes - 1) throw new InvalidDataException("Invalid replica frame length.");
            byte[] prefix = BitConverter.GetBytes(body.Length + 1);
            await stream.WriteAsync(prefix, 0, prefix.Length, stop).ConfigureAwait(false);
            progress?.Invoke(ReplicaWriteStage.FirstBytes);
            await stream.WriteAsync(new[] { (byte)kind }, 0, 1, stop).ConfigureAwait(false);
            await stream.WriteAsync(body, 0, body.Length, stop).ConfigureAwait(false);
            progress?.Invoke(ReplicaWriteStage.Complete);
        }

        public static async Task<(ReplicaFrameKind Kind, byte[] Body)> ReadAsync(Stream stream, CancellationToken stop, Action<ReplicaReadStage> progress = null)
        {
            byte[] prefix = new byte[4];
            await PinnedTlsTransfer.ReadExactAsync(stream, prefix, 0, prefix.Length, stop).ConfigureAwait(false);
            progress?.Invoke(ReplicaReadStage.FirstBytes);
            int length = BitConverter.ToInt32(prefix, 0);
            if (length < 1 || length > MaximumFrameBytes) throw new InvalidDataException("Invalid replica frame length.");
            byte[] frame = new byte[length];
            await PinnedTlsTransfer.ReadExactAsync(stream, frame, 0, length, stop).ConfigureAwait(false);
            progress?.Invoke(ReplicaReadStage.Complete);
            if (!Enum.IsDefined(typeof(ReplicaFrameKind), frame[0])) throw new InvalidDataException("Unknown replica frame.");
            byte[] body = new byte[length - 1];
            Buffer.BlockCopy(frame, 1, body, 0, body.Length);
            return ((ReplicaFrameKind)frame[0], body);
        }

        public static byte[] Revision(ulong revision) => BitConverter.GetBytes(revision);

        public static byte[] Checkpoint(ulong revision, byte[] snapshotHash)
        {
            if (snapshotHash == null || snapshotHash.Length != 32) throw new InvalidDataException("Invalid checkpoint hash.");
            byte[] body = new byte[40];
            Buffer.BlockCopy(BitConverter.GetBytes(revision), 0, body, 0, 8);
            Buffer.BlockCopy(snapshotHash, 0, body, 8, 32);
            return body;
        }

        public static (ulong Revision, byte[] Hash) ReadCheckpoint(byte[] body)
        {
            if (body == null || body.Length != 40) throw new InvalidDataException("Invalid replica checkpoint.");
            byte[] hash = new byte[32];
            Buffer.BlockCopy(body, 8, hash, 0, 32);
            return (BitConverter.ToUInt64(body, 0), hash);
        }

        public static byte[] Hash(StateSnapshot state)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(SharedStateCodec.Encode(state));
        }

        public static ulong ReadRevision(byte[] body)
        {
            if (body == null || body.Length != 8) throw new InvalidDataException("Invalid replica acknowledgement.");
            return BitConverter.ToUInt64(body, 0);
        }

        public static byte[] Resource(string reference, byte[] data)
        {
            if (data == null || data.Length > 8 * 1024 * 1024) throw new InvalidDataException("Invalid replica resource length.");
            byte[] name = Utf8.GetBytes(reference ?? "");
            if (name.Length < 1 || name.Length > 256) throw new InvalidDataException("Invalid replica resource reference.");
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Utf8, true))
            {
                writer.Write((ushort)name.Length);
                writer.Write(name);
                writer.Write(data.Length);
                writer.Write(data);
            }

            return stream.ToArray();
        }

        public static (string Reference, byte[] Data) ReadResource(byte[] body)
        {
            using var stream = new MemoryStream(body, false);
            using var reader = new BinaryReader(stream, Utf8);
            int nameLength = reader.ReadUInt16();
            if (nameLength < 1 || nameLength > 256) throw new InvalidDataException("Invalid replica resource reference.");
            byte[] name = reader.ReadBytes(nameLength);
            if (name.Length != nameLength) throw new InvalidDataException("Truncated replica resource.");
            int length = reader.ReadInt32();
            if (length < 0 || length > 8 * 1024 * 1024 || length != stream.Length - stream.Position) throw new InvalidDataException("Invalid replica resource length.");
            byte[] data = reader.ReadBytes(length);
            return (Utf8.GetString(name), data);
        }
    }
}
