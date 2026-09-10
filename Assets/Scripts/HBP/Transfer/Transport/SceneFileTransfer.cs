using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace HBP.Transfer.Transport
{
    public static partial class PinnedTlsTransfer
    {
        public const long MaximumSceneFileBytes = 4L * 1024 * 1024 * 1024;

        public static async Task<DeliveryReceipt> SendFileAsync(Stream stream, string file, CancellationToken stop, Action<long> progress = null)
        {
            using var source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkBytes, true);
            if (source.Length < 1 || source.Length > MaximumSceneFileBytes) throw new InvalidDataException("Visualization exceeds the transfer file budget.");
            using var sha = SHA256.Create();
            byte[] digest = sha.ComputeHash(source);
            source.Position = 0;
            byte[] header = new byte[44];
            header[0] = (byte)'H';
            header[1] = (byte)'B';
            header[2] = (byte)'T';
            header[3] = 3;
            PutLong(header, 4, source.Length);
            Buffer.BlockCopy(digest, 0, header, 12, 32);
            await stream.WriteAsync(header, 0, header.Length, stop).ConfigureAwait(false);
            var chunk = new byte[ChunkBytes];
            var chunkHeader = new byte[40];
            int index = 0;
            for (long offset = 0; offset < source.Length;)
            {
                int count = (int)Math.Min(ChunkBytes, source.Length - offset);
                await ReadExactAsync(source, chunk, 0, count, stop).ConfigureAwait(false);
                PutInt(chunkHeader, 0, index++);
                PutInt(chunkHeader, 4, count);
                Buffer.BlockCopy(sha.ComputeHash(chunk, 0, count), 0, chunkHeader, 8, 32);
                await stream.WriteAsync(chunkHeader, 0, chunkHeader.Length, stop).ConfigureAwait(false);
                await stream.WriteAsync(chunk, 0, count, stop).ConfigureAwait(false);
                offset += count;
                progress?.Invoke(offset);
            }

            var receipt = new byte[33];
            await ReadExactAsync(stream, receipt, 0, receipt.Length, stop).ConfigureAwait(false);
            var returnedHash = new byte[32];
            Buffer.BlockCopy(receipt, 1, returnedHash, 0, 32);
            if (!TransportIdentity.Equal(returnedHash, digest) || receipt[0] < 1 || receipt[0] > 4) throw new InvalidDataException("Invalid publication receipt.");
            return new DeliveryReceipt(digest, (DeliveryStatus)receipt[0]);
        }

        public static async Task<DeliveryReceipt> ReceiveFileAsync(Stream stream, string file, CancellationToken stop, Func<string, string, CancellationToken, Task<DeliveryStatus>> publish, Action<long> progress = null)
        {
            var header = new byte[44];
            await ReadExactAsync(stream, header, 0, header.Length, stop).ConfigureAwait(false);
            if (header[0] != 'H' || header[1] != 'B' || header[2] != 'T' || header[3] != 3) throw new InvalidDataException("Unknown visualization transport version. Rebuild Desktop and Quest together.");
            long total = GetLong(header, 4);
            if (total < 1 || total > MaximumSceneFileBytes) throw new InvalidDataException("Visualization exceeds the transfer file budget.");
            var expected = new byte[32];
            Buffer.BlockCopy(header, 12, expected, 0, 32);
            var chunk = new byte[ChunkBytes];
            var chunkHeader = new byte[40];
            var expectedChunk = new byte[32];
            using var sha = SHA256.Create();
            using var whole = SHA256.Create();
            byte[] digest;
            using (var target = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None, ChunkBytes, true))
            {
                int index = 0;
                for (long offset = 0; offset < total;)
                {
                    int count = (int)Math.Min(ChunkBytes, total - offset);
                    await ReadExactAsync(stream, chunkHeader, 0, chunkHeader.Length, stop).ConfigureAwait(false);
                    if (GetInt(chunkHeader, 0) != index++ || GetInt(chunkHeader, 4) != count) throw new InvalidDataException("Invalid chunk dimensions.");
                    await ReadExactAsync(stream, chunk, 0, count, stop).ConfigureAwait(false);
                    Buffer.BlockCopy(chunkHeader, 8, expectedChunk, 0, 32);
                    if (!TransportIdentity.Equal(expectedChunk, sha.ComputeHash(chunk, 0, count))) throw new InvalidDataException("Chunk hash mismatch.");
                    whole.TransformBlock(chunk, 0, count, null, 0);
                    await target.WriteAsync(chunk, 0, count, stop).ConfigureAwait(false);
                    offset += count;
                    progress?.Invoke(offset);
                }

                whole.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                digest = whole.Hash;
                if (!TransportIdentity.Equal(expected, digest)) throw new InvalidDataException("Visualization hash mismatch.");
            }

            stop.ThrowIfCancellationRequested();
            string hash = new DeliveryReceipt(digest, DeliveryStatus.Published).ContentHash;
            DeliveryStatus status = await publish(file, hash, stop).ConfigureAwait(false);
            var receipt = new byte[33];
            receipt[0] = (byte)status;
            Buffer.BlockCopy(digest, 0, receipt, 1, 32);
            await stream.WriteAsync(receipt, 0, receipt.Length, stop).ConfigureAwait(false);
            return new DeliveryReceipt(digest, status);
        }

        private static void PutLong(byte[] bytes, int offset, long value)
        {
            for (int i = 0; i < 8; i++) bytes[offset + i] = (byte)(value >> (8 * i));
        }

        private static long GetLong(byte[] bytes, int offset)
        {
            long value = 0;
            for (int i = 0; i < 8; i++) value |= (long)bytes[offset + i] << (8 * i);
            return value;
        }
    }
}
