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
            if (source.Length < 1 || source.Length > MaximumSceneFileBytes)
                throw new InvalidDataException("Visualization exceeds the transfer file budget.");
            byte[] digest = HBP.Core.Tools.Sha256.ComputeHash(source);
            return await SendPreparedFileAsync(stream, source, digest, stop, progress).ConfigureAwait(false);
        }

        // Only the owning SceneDelivery may reuse a digest while keeping the source immutable.
        internal static async Task<DeliveryReceipt> SendPreparedFileAsync(Stream stream, Stream source, byte[] digest, CancellationToken stop, Action<long> progress, Action writeStarted = null, Action payloadSent = null, string timingScope = null, long timingAttempt = 0)
        {
            using var sourceRead = PairingTiming.Aggregate("desktop.archive-spool-read", timingScope, timingAttempt);
            using var tls = PairingTiming.Aggregate("desktop.archive-tls-write", timingScope, timingAttempt);
            using var hashing = PairingTiming.Aggregate("desktop.archive-chunk-sha256", timingScope, timingAttempt);
            stop.ThrowIfCancellationRequested();
            using var sha = HBP.Transfer.Codecs.TransferCodec.CreateHash();
            source.Position = 0;
            byte[] header = new byte[44];
            header[0] = (byte)'H';
            header[1] = (byte)'B';
            header[2] = (byte)'T';
            header[3] = 3;
            PutLong(header, 4, source.Length);
            Buffer.BlockCopy(digest, 0, header, 12, 32);
            using (PairingTiming.Measure(tls, header.Length))
                await stream.WriteAsync(header, 0, header.Length, stop).ConfigureAwait(false);
            writeStarted?.Invoke();
            var chunk = new byte[ChunkBytes];
            var chunkHeader = new byte[40];
            int index = 0;
            for (long offset = 0; offset < source.Length;)
            {
                int count = (int)Math.Min(ChunkBytes, source.Length - offset);

                using (PairingTiming.Measure(sourceRead, count))
                    await ReadExactAsync(source, chunk, 0, count, stop).ConfigureAwait(false);
                PutInt(chunkHeader, 0, index++);
                PutInt(chunkHeader, 4, count);
                using (PairingTiming.Measure(hashing, count))
                    Buffer.BlockCopy(sha.ComputeHash(chunk, 0, count), 0, chunkHeader, 8, 32);
                using (PairingTiming.Measure(tls, chunkHeader.Length + count))
                {
                    await stream.WriteAsync(chunkHeader, 0, chunkHeader.Length, stop).ConfigureAwait(false);
                    await stream.WriteAsync(chunk, 0, count, stop).ConfigureAwait(false);
                }

                offset += count;
                progress?.Invoke(offset);
            }

            payloadSent?.Invoke();
            var receipt = new byte[33];
            using (timingScope == null ? null : PairingTiming.Measure("desktop.scene-publication-receipt-wait", timingScope, timingAttempt))
                await ReadExactAsync(stream, receipt, 0, receipt.Length, stop).ConfigureAwait(false);
            var returnedHash = new byte[32];
            Buffer.BlockCopy(receipt, 1, returnedHash, 0, 32);
            if (!TransportIdentity.Equal(returnedHash, digest) || receipt[0] < 1 || receipt[0] > 4)
                throw new InvalidDataException("Invalid publication receipt.");
            return new DeliveryReceipt(digest, (DeliveryStatus)receipt[0]);
        }

        public static async Task<DeliveryReceipt> ReceiveFileAsync(Stream stream, string file, CancellationToken stop, Func<string, string, CancellationToken, Task<DeliveryStatus>> publish, Action<long> progress = null, Func<Stream, CancellationToken, Task<DeliveryReceipt>> receiveBlocks = null, Action<long, long> detailedProgress = null, Action<long> wireProgress = null, Action receiptSent = null, string timingScope = null)
        {
            var header = new byte[44];
            using (timingScope == null ? null : PairingTiming.Measure("quest.transport-prefix-read", timingScope))
                await ReadExactAsync(stream, header, 0, 4, stop).ConfigureAwait(false);
            wireProgress?.Invoke(4);
            if (header[0] == 'H' && header[1] == 'B' && header[2] == 'T' && header[3] == 4 && receiveBlocks != null)
                return await receiveBlocks(stream, stop).ConfigureAwait(false);
            if (header[0] != 'H' || header[1] != 'B' || header[2] != 'T' || header[3] != 3)
                throw new InvalidDataException("Unknown visualization transport version. Rebuild Desktop and Quest together.");
            using var tls = PairingTiming.Aggregate("quest.archive-tls-read", timingScope);
            using var hashing = PairingTiming.Aggregate("quest.archive-chunk-and-file-sha256", timingScope);
            using var staging = PairingTiming.Aggregate("quest.archive-spool-write", timingScope);
            using (PairingTiming.Measure(tls, 40))
                await ReadExactAsync(stream, header, 4, 40, stop).ConfigureAwait(false);
            long wireReceived = 44;
            wireProgress?.Invoke(wireReceived);
            long total = GetLong(header, 4);
            if (total < 1 || total > MaximumSceneFileBytes)
                throw new InvalidDataException("Visualization exceeds the transfer file budget.");
            detailedProgress?.Invoke(0, total);
            var expected = new byte[32];
            Buffer.BlockCopy(header, 12, expected, 0, 32);
            var chunk = new byte[ChunkBytes];
            var chunkHeader = new byte[40];
            var expectedChunk = new byte[32];
            using var sha = HBP.Transfer.Codecs.TransferCodec.CreateHash();
            using var whole = HBP.Transfer.Codecs.TransferCodec.CreateHash();
            byte[] digest;
            var target = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None, ChunkBytes, true);
            try
            {
                int index = 0;
                for (long offset = 0; offset < total;)
                {
                    int count = (int)Math.Min(ChunkBytes, total - offset);

                    using (PairingTiming.Measure(tls, chunkHeader.Length))
                        await ReadExactAsync(stream, chunkHeader, 0, chunkHeader.Length, stop).ConfigureAwait(false);
                    if (GetInt(chunkHeader, 0) != index++ || GetInt(chunkHeader, 4) != count)
                        throw new InvalidDataException("Invalid chunk dimensions.");
                    using (PairingTiming.Measure(tls, count))
                        await ReadExactAsync(stream, chunk, 0, count, stop).ConfigureAwait(false);
                    wireReceived += chunkHeader.Length + count;
                    wireProgress?.Invoke(wireReceived);
                    Buffer.BlockCopy(chunkHeader, 8, expectedChunk, 0, 32);
                    using (PairingTiming.Measure(hashing, 2L * count))
                    {
                        if (!TransportIdentity.Equal(expectedChunk, sha.ComputeHash(chunk, 0, count)))
                            throw new InvalidDataException("Chunk hash mismatch.");
                        whole.TransformBlock(chunk, 0, count, null, 0);
                    }

                    using (PairingTiming.Measure(staging, count))
                        await target.WriteAsync(chunk, 0, count, stop).ConfigureAwait(false);
                    offset += count;
                    progress?.Invoke(offset);
                    detailedProgress?.Invoke(offset, total);
                }

                whole.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                digest = whole.Hash;
                if (!TransportIdentity.Equal(expected, digest))
                    throw new InvalidDataException("Visualization hash mismatch.");
            }
            finally
            {
                using (PairingTiming.Measure(staging))
                    target.Dispose();
            }

            stop.ThrowIfCancellationRequested();
            string hash = new DeliveryReceipt(digest, DeliveryStatus.Published).ContentHash;
            DeliveryStatus status;
            status = await publish(file, hash, stop).ConfigureAwait(false);
            var receipt = new byte[33];
            receipt[0] = (byte)status;
            Buffer.BlockCopy(digest, 0, receipt, 1, 32);
            using (timingScope == null ? null : PairingTiming.Measure("quest.scene-publication-receipt-write", timingScope))
                await stream.WriteAsync(receipt, 0, receipt.Length, stop).ConfigureAwait(false);
            receiptSent?.Invoke();
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
