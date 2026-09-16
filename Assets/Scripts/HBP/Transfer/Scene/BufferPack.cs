using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace HBP.Transfer.Scene
{
    // Version 1: little-endian HBPI, version, count, then SHA256[32], offset[8], length[8].
    // Offsets address the separate uncompressed buffers.pack and must cover it contiguously.
    internal sealed class BufferPack
    {
        internal const string IndexName = "buffers.index", DataName = "buffers.pack";
        internal const int MaximumCount = 100000, MaximumIndexBytes = 12 + 48 * MaximumCount;
        internal readonly List<Entry> Entries = new();
        private readonly Dictionary<string, int> byName = new(StringComparer.Ordinal);
        private readonly object gate = new();
        private FileStream file;
        private bool retired;
        private int readers;
        private Action cleanup;
        internal long Length { get; private set; }

        internal sealed class Entry
        {
            internal string Name;
            internal byte[] Hash;
            internal long Offset, Length;
        }

        internal void Add(string name, long length)
        {
            if (Entries.Count >= MaximumCount || length < 0 || length > SceneArchive.MaximumExpandedBytes - Length)
                throw new InvalidDataException("Buffer pack exceeds its budget.");
            var hash = new byte[32];
            for (int i = 0; i < 32; i++) hash[i] = Convert.ToByte(name.Substring(i * 2, 2), 16);
            byName.Add(name, Entries.Count);
            Entries.Add(new Entry { Name = name, Hash = hash, Offset = Length, Length = length });
            Length += length;
        }

        internal bool Contains(string name) => name != null && byName.ContainsKey(name);
        internal int IndexOf(string name) => byName.TryGetValue(name, out int index) ? index : throw new InvalidDataException("Unknown packed buffer.");

        internal void WriteIndex(Stream output)
        {
            using var writer = new BinaryWriter(output, Encoding.UTF8, true);
            writer.Write(0x49504248);
            writer.Write(1);
            writer.Write(Entries.Count);
            foreach (var entry in Entries)
            {
                writer.Write(entry.Hash);
                writer.Write(entry.Offset);
                writer.Write(entry.Length);
            }
        }

        internal static BufferPack ReadIndex(Stream input, long indexLength, long dataLength, CancellationToken token)
        {
            if (indexLength < 12 || indexLength > MaximumIndexBytes || dataLength < 0 || dataLength > SceneArchive.MaximumExpandedBytes)
                throw new InvalidDataException("Invalid buffer pack dimensions.");
            using var reader = new BinaryReader(input, Encoding.UTF8, true);
            if (reader.ReadInt32() != 0x49504248 || reader.ReadInt32() != 1) throw new InvalidDataException("Unknown buffer pack format.");
            int count = reader.ReadInt32();
            if (count < 0 || count > MaximumCount || indexLength != 12L + count * 48L) throw new InvalidDataException("Invalid buffer pack index size.");
            var pack = new BufferPack();
            for (int i = 0; i < count; i++)
            {
                token.ThrowIfCancellationRequested();
                byte[] hash = reader.ReadBytes(32);
                if (hash.Length != 32) throw new EndOfStreamException();
                long offset = reader.ReadInt64(), length = reader.ReadInt64();
                string name = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant() + ".bin";
                if (offset != pack.Length || length < 0 || length > dataLength - offset || pack.Contains(name))
                    throw new InvalidDataException("Invalid, repeated or overlapping buffer range.");
                pack.Add(name, length);
            }

            if (pack.Length != dataLength || input.ReadByte() != -1) throw new InvalidDataException("Unassigned or truncated pack data.");
            return pack;
        }

        internal void Attach(string path)
        {
            file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
        }

        internal Stream Open(int index)
        {
            lock (gate)
            {
                if (retired) throw new ObjectDisposedException(nameof(BufferPack));
                if (file == null || index < 0 || index >= Entries.Count) throw new InvalidDataException("Invalid packed buffer reference.");
                readers++;
                return new Range(this, Entries[index]);
            }
        }

        internal Stream Open(string name) => Open(IndexOf(name));

        internal void Retire(Action afterReaders)
        {
            lock (gate)
            {
                retired = true;
                cleanup = afterReaders;
                CloseIfIdle();
            }
        }

        private void CloseIfIdle()
        {
            if (!retired || readers != 0) return;
            file?.Dispose();
            file = null;
            var action = cleanup;
            cleanup = null;
            action?.Invoke();
        }

        // The verifier consumes uncompressed copy blocks, including boundaries and empty resources.
        internal sealed class Verifier : IDisposable
        {
            private readonly BufferPack pack;
            private readonly SHA256 sha = HBP.Transfer.Codecs.TransferCodec.CreateHash();
            private int index;
            private long position;

            internal Verifier(BufferPack pack)
            {
                this.pack = pack;
                try
                {
                    FinishBoundaries();
                }
                catch
                {
                    sha.Dispose();
                    throw;
                }
            }

            private void FinishBoundaries()
            {
                while (index < pack.Entries.Count && position == pack.Entries[index].Length)
                {
                    sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    byte[] actual = sha.Hash, expected = pack.Entries[index].Hash;
                    for (int i = 0; i < 32; i++)
                        if (actual[i] != expected[i])
                            throw new InvalidDataException("Packed buffer checksum mismatch.");
                    index++;
                    position = 0;
                    sha.Initialize();
                }
            }

            internal void Append(byte[] buffer, int count, CancellationToken token)
            {
                for (int offset = 0; offset < count;)
                {
                    token.ThrowIfCancellationRequested();
                    if (index == pack.Entries.Count) throw new InvalidDataException("Extra pack bytes.");
                    int take = (int)Math.Min(count - offset, pack.Entries[index].Length - position);
                    sha.TransformBlock(buffer, offset, take, null, 0);
                    position += take;
                    offset += take;
                    FinishBoundaries();
                }
            }

            internal void Complete()
            {
                if (index != pack.Entries.Count) throw new InvalidDataException("Truncated buffer pack.");
            }

            public void Dispose() => sha.Dispose();
        }

        private sealed class Range : Stream
        {
            private BufferPack owner;
            private readonly Entry entry;
            private long position;

            internal Range(BufferPack owner, Entry entry)
            {
                this.owner = owner;
                this.entry = entry;
            }

            public override bool CanRead => owner != null;
            public override bool CanSeek => owner != null;
            public override bool CanWrite => false;
            public override long Length => entry.Length;

            public override long Position
            {
                get => position;
                set => Seek(value, SeekOrigin.Begin);
            }

            public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

            public override int Read(Span<byte> buffer)
            {
                var pack = owner ?? throw new ObjectDisposedException(nameof(Range));
                lock (pack.gate)
                {
                    if (owner == null) throw new ObjectDisposedException(nameof(Range));
                    int count = (int)Math.Min(buffer.Length, entry.Length - position);
                    pack.file.Position = entry.Offset + position;
                    int read = pack.file.Read(buffer.Slice(0, count));
                    position += read;
                    return read;
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                long next = checked((origin == SeekOrigin.Begin ? 0 : origin == SeekOrigin.Current ? position : origin == SeekOrigin.End ? Length : throw new ArgumentOutOfRangeException(nameof(origin))) + offset);
                if (next < 0 || next > Length) throw new IOException("Read escaped its buffer range.");
                return position = next;
            }

            protected override void Dispose(bool disposing)
            {
                var pack = owner;
                if (pack != null)
                    lock (pack.gate)
                    {
                        if (owner != null)
                        {
                            owner = null;
                            pack.readers--;
                            pack.CloseIfIdle();
                        }
                    }

                base.Dispose(disposing);
            }

            public override void Flush()
            {
            }

            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
