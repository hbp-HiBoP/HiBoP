using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Codecs;

namespace HBP.Transfer.Transport
{
    public sealed class BlockResource
    {
        public string Name { get; }
        public long Length { get; }
        public Action<Stream, CancellationToken> Write { get; }

        public BlockResource(string name, long length, Action<Stream, CancellationToken> write = null)
        {
            Name = name;
            Length = length;
            Write = write;
        }
    }

    public interface IBlockSink
    {
        void Begin(IReadOnlyList<BlockResource> resources, CancellationToken token);
        Stream Open(int index);
        void Complete();
    }

    /// <summary>HBT4: bounded independent blocks, ordered resources and a final authenticated transcript.</summary>
    public static class BlockContainer
    {
        public const int QueueCapacity = 2;
        private const int MaxManifest = 16 * 1024 * 1024;
        private const int MaxResources = 100000;
        private const long MaxBytes = PinnedTlsTransfer.MaximumSceneFileBytes;

        public static readonly byte[] Magic =
        {
            (byte)'H',
            (byte)'B',
            (byte)'T',
            4
        };

        public static byte[] Produce(IReadOnlyList<BlockResource> resources, Action<byte[]> emit, CancellationToken token)
        {
            Validate(resources);
            using var transcript = TransferCodec.CreateHash();
            using var manifest = new MemoryStream();
            using (var writer = new BinaryWriter(manifest, Encoding.UTF8, true))
            {
                writer.Write(resources.Count);
                foreach (var item in resources)
                {
                    writer.Write((byte)item.Name.Length);
                    writer.Write(Encoding.ASCII.GetBytes(item.Name));
                    writer.Write(item.Length);
                }
            }

            var intro = new byte[8 + manifest.Length];
            Buffer.BlockCopy(Magic, 0, intro, 0, 4);
            Put32(intro, 4, (int)manifest.Length);
            Buffer.BlockCopy(manifest.GetBuffer(), 0, intro, 8, (int)manifest.Length);
            Hash(transcript, intro);
            emit(intro);
            int sequence = 0;
            long total = 0, encoded = 0;
            using var blockHash = TransferCodec.CreateHash();
            var raw = new byte[TransferCodec.BlockBytes];
            var compressed = new byte[TransferCodec.BlockBytes + 65536];
            for (int file = 0; file < resources.Count; file++)
            {
                token.ThrowIfCancellationRequested();
                long offset = 0;
                int resourceIndex = file;
                using var target = new BlockWriter(raw, (buffer, count) =>
                {
                    token.ThrowIfCancellationRequested();
                    int size;
                    size = TransferCodec.Encode(TransferCodec.Zstd, 3, buffer, count, compressed);
                    bool compress = size < count;
                    if (!compress)
                        size = count;
                    var packet = new byte[64 + size];
                    packet[0] = 1;
                    packet[1] = compress ? TransferCodec.Zstd : (byte)0;
                    Put32(packet, 4, sequence++);
                    Put32(packet, 8, resourceIndex);
                    Put64(packet, 12, offset);
                    Put32(packet, 20, count);
                    Put32(packet, 24, size);
                    Buffer.BlockCopy(compress ? compressed : buffer, 0, packet, 64, size);
                    Buffer.BlockCopy(blockHash.ComputeHash(packet, 64, size), 0, packet, 28, 32);
                    transcript.TransformBlock(packet, 0, 64, null, 0);
                    offset += count;
                    total += count;
                    encoded += size;
                    emit(packet);
                }, resources[file].Length);
                resources[file].Write(target, token);
                target.Finish();
            }

            token.ThrowIfCancellationRequested();
            transcript.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            byte[] digest = transcript.Hash;
            var end = new byte[64];
            end[0] = 2;
            Put32(end, 4, sequence);
            Put64(end, 12, total);
            Put64(end, 20, encoded);
            Buffer.BlockCopy(digest, 0, end, 28, 32);
            emit(end);
            return digest;
        }

        // Caller has consumed and validated the four-byte magic. One worker owns sink/native decoding.
        public static async Task<byte[]> ReceiveAsync(Stream stream, IBlockSink sink, CancellationToken token, Action<long> progress = null, Action<long, long> logicalProgress = null)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token);
            using var queue = new System.Collections.Concurrent.BlockingCollection<byte[]>(QueueCapacity);
            var length = new byte[4];
            await PinnedTlsTransfer.ReadExactAsync(stream, length, 0, 4, token).ConfigureAwait(false);
            int manifestLength = Get32(length, 0);
            if (manifestLength < 4 || manifestLength > MaxManifest)
                throw new InvalidDataException("Invalid block manifest size.");
            var manifest = new byte[manifestLength];
            await PinnedTlsTransfer.ReadExactAsync(stream, manifest, 0, manifest.Length, token).ConfigureAwait(false);
            var resources = new List<BlockResource>();
            using (var input = new MemoryStream(manifest, false))
            using (var reader = new BinaryReader(input, Encoding.ASCII))
            {
                int count = reader.ReadInt32();
                if (count < 1 || count > MaxResources)
                    throw new InvalidDataException("Invalid resource count.");
                for (int i = 0; i < count; i++)
                {
                    int size = reader.ReadByte();
                    if (size < 1 || size > 80)
                        throw new InvalidDataException("Invalid resource name.");
                    byte[] name = reader.ReadBytes(size);
                    if (name.Length != size || name.Any(b => b > 127))
                        throw new InvalidDataException("Invalid ASCII resource name.");
                    resources.Add(new BlockResource(Encoding.ASCII.GetString(name), reader.ReadInt64()));
                }

                if (input.Position != input.Length)
                    throw new InvalidDataException("Trailing manifest data.");
            }

            Validate(resources);
            long expectedRaw = resources.Sum(resource => resource.Length);
            logicalProgress?.Invoke(0, expectedRaw);
            // Closing a failed connection unblocks pending TLS reads even on runtimes which ignore read cancellation.
            using var close = linked.Token.Register(stream.Dispose);
            Task<byte[]> consumer = Task.Run(() =>
            {
                try
                {
                    using var transcript = TransferCodec.CreateHash();
                    Hash(transcript, Magic);
                    Hash(transcript, length);
                    Hash(transcript, manifest);
                    using var sha = TransferCodec.CreateHash();
                    var decoded = new byte[TransferCodec.MaximumBlockBytes];
                    sink.Begin(resources, token);
                    int seq = 0;
                    long total = 0, encoded = 0;
                    for (int file = 0; file < resources.Count; file++)
                    {
                        using Stream target = sink.Open(file);
                        for (long offset = 0; offset < resources[file].Length;)
                        {
                            byte[] packet = queue.Take(linked.Token);
                            int raw = Get32(packet, 20), size = Get32(packet, 24);
                            if (packet[0] != 1 || Get32(packet, 4) != seq++ || Get32(packet, 8) != file || Get64(packet, 12) != offset || raw > resources[file].Length - offset)
                                throw new InvalidDataException("Invalid block order or resource range.");
                            if (!EqualHash(packet, sha.ComputeHash(packet, 64, size)))
                                throw new InvalidDataException("Block checksum mismatch.");
                            transcript.TransformBlock(packet, 0, 64, null, 0);
                            // Native input starts at zero; bounded copy avoids unsafe array pointers across IL2CPP.
                            byte packetCodec = packet[1];
                            Buffer.BlockCopy(packet, 64, packet, 0, size);
                            TransferCodec.Decode(packetCodec, packet, size, decoded, raw);
                            target.Write(decoded, 0, raw);
                            offset += raw;
                            total += raw;
                            encoded += size;
                            logicalProgress?.Invoke(total, expectedRaw);
                        }
                    }

                    byte[] end = queue.Take(linked.Token);
                    transcript.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    if (end[0] != 2 || Get32(end, 4) != seq || Get64(end, 12) != total || Get64(end, 20) != encoded || !EqualHash(end, transcript.Hash))
                        throw new InvalidDataException("Invalid final block transcript.");
                    linked.Token.ThrowIfCancellationRequested();
                    sink.Complete();
                    return transcript.Hash;
                }
                catch
                {
                    linked.Cancel();
                    throw;
                }
            });
            try
            {
                long received = 8L + manifest.Length;
                while (true)
                {
                    var header = new byte[64];
                    await PinnedTlsTransfer.ReadExactAsync(stream, header, 0, 64, linked.Token).ConfigureAwait(false);
                    ValidateHeader(header);
                    int size = header[0] == 1 ? Get32(header, 24) : 0;
                    var packet = new byte[64 + size];
                    Buffer.BlockCopy(header, 0, packet, 0, 64);
                    await PinnedTlsTransfer.ReadExactAsync(stream, packet, 64, size, linked.Token).ConfigureAwait(false);
                    await Task.Run(() => queue.Add(packet, linked.Token)).ConfigureAwait(false);
                    received += packet.Length;
                    progress?.Invoke(received);
                    if (header[0] == 2)
                        break;
                }

                byte[] result = await consumer.ConfigureAwait(false);
                return result;
            }
            catch
            {
                linked.Cancel();
                try
                {
                    await consumer.ConfigureAwait(false);
                }
                catch when (!consumer.IsFaulted || token.IsCancellationRequested)
                {
                }

                throw;
            }
        }

        private static void ValidateHeader(byte[] header)
        {
            if (header[2] != 0 || header[3] != 0 || Get32(header, 60) != 0)
                throw new InvalidDataException("Unknown block flags.");
            if (header[0] == 2)
            {
                if (header[1] != 0 || Get32(header, 8) != 0)
                    throw new InvalidDataException("Invalid terminator.");
                return;
            }

            int raw = Get32(header, 20), size = Get32(header, 24);
            if (header[0] != 1 || raw < 1 || raw > TransferCodec.MaximumBlockBytes || size < 1 || size > raw || header[1] != 0 && header[1] != 2 && header[1] != 3)
                throw new InvalidDataException("Unknown codec or invalid block dimensions.");
        }

        private static void Validate(IReadOnlyList<BlockResource> resources)
        {
            if (resources.Count < 1 || resources.Count > MaxResources)
                throw new InvalidDataException("Invalid resource count.");
            var names = new HashSet<string>(StringComparer.Ordinal);
            long sum = 0;
            foreach (var item in resources)
            {
                if (string.IsNullOrEmpty(item.Name) || item.Name.Length > 80 || item.Name.Any(c => c != '.' && c != '_' && c != '-' && (c < '0' || c > '9') && (c < 'a' || c > 'z')) || !names.Add(item.Name) || item.Length < 0 || item.Length > MaxBytes || (sum += item.Length) > MaxBytes)
                    throw new InvalidDataException("Invalid resource manifest.");
            }
        }

        private static bool EqualHash(byte[] header, byte[] hash)
        {
            int diff = 0;
            for (int i = 0; i < 32; i++)
                diff |= header[28 + i] ^ hash[i];
            return diff == 0;
        }

        private static void Hash(HashAlgorithm sha, byte[] bytes) => sha.TransformBlock(bytes, 0, bytes.Length, null, 0);

        private static void Put32(byte[] b, int p, int v)
        {
            for (int i = 0; i < 4; i++)
                b[p + i] = (byte)(v >> (8 * i));
        }

        private static int Get32(byte[] b, int p)
        {
            int v = 0;
            for (int i = 0; i < 4; i++)
                v |= b[p + i] << (8 * i);
            return v;
        }

        private static void Put64(byte[] b, int p, long v)
        {
            for (int i = 0; i < 8; i++)
                b[p + i] = (byte)(v >> (8 * i));
        }

        private static long Get64(byte[] b, int p)
        {
            long v = 0;
            for (int i = 0; i < 8; i++)
                v |= (long)b[p + i] << (8 * i);
            return v;
        }

        private sealed class BlockWriter : Stream
        {
            private readonly byte[] buffer;
            private readonly Action<byte[], int> emit;
            private readonly long expected;
            private long total;
            private int used;

            internal BlockWriter(byte[] buffer, Action<byte[], int> emit, long expected)
            {
                this.buffer = buffer;
                this.emit = emit;
                this.expected = expected;
            }

            public override void Write(byte[] source, int offset, int count)
            {
                if (count < 0 || offset < 0 || offset > source.Length - count || total + count > expected)
                    throw new InvalidDataException("Resource exceeds its declared size.");
                total += count;
                while (count > 0)
                {
                    int n = Math.Min(buffer.Length - used, count);
                    Buffer.BlockCopy(source, offset, buffer, used, n);
                    used += n;
                    offset += n;
                    count -= n;
                    if (used == buffer.Length)
                        Flush();
                }
            }

            public override void Flush()
            {
                if (used > 0)
                {
                    emit(buffer, used);
                    used = 0;
                }
            }

            internal void Finish()
            {
                if (total != expected)
                    throw new InvalidDataException("Truncated resource.");
                Flush();
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => total;

            public override long Position
            {
                get => total;
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] b, int o, int c) => throw new NotSupportedException();
            public override long Seek(long o, SeekOrigin s) => throw new NotSupportedException();
            public override void SetLength(long v) => throw new NotSupportedException();
        }
    }
}
