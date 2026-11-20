using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Codecs;
using HBP.Transfer.Transport;
using HBP.Core.Tools;

namespace HBP.Transfer.Scene
{
    public sealed partial class SceneArchive : IBlockSink
    {
        internal IReadOnlyList<BlockResource> CaptureBlockResources(byte[] metadata)
        {
            EnsureWritable();
            if (!deferResourceWrites || !packedBuffers)
                throw new InvalidOperationException("Detached packed capture required.");
            byte[] references = GlobalReferences.Encode();
            long indexBytes = pack.IndexLength;
            ValidatePackedMetadataBudget(metadata.LongLength, references.LongLength, indexBytes);
            var result = new List<BlockResource>();

            void AddMemory(string name, byte[] bytes) =>
                result.Add(new BlockResource(name, bytes.Length, (target, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    target.Write(bytes, 0, bytes.Length);
                }));

            AddMemory("visualization.json", metadata);
            AddMemory(SceneGlobalReferences.FileName, references);
            result.Add(new BlockResource(BufferPack.IndexName, indexBytes, (target, token) => pack.WriteIndex(target, token)));
            result.Add(new BlockResource(BufferPack.DataName, pack.Length, (target, token) =>
            {
                var buffer = new byte[65536];
                foreach (var entry in pack.Entries)
                {
                    using Stream source = capturedBuffers.TryGetValue(entry.Name, out byte[] bytes) ? new MemoryStream(bytes, false) : File.OpenRead(Resolve(entry.Name));
                    CopyCaptured(source, target, entry.Length, buffer, token, null);
                }
            }));
            foreach (var entry in capturedBuffers.Where(p => !p.Key.EndsWith(".bin", StringComparison.Ordinal)).OrderBy(p => p.Key, StringComparer.Ordinal))
                AddMemory(entry.Key, entry.Value);
            foreach (string path in Directory.EnumerateFiles(directory))
                if (Path.GetFileName(path) != "visualization.json" && !path.EndsWith(".bin", StringComparison.Ordinal))
                    capturedFiles.TryAdd(Path.GetFileName(path), path);
            foreach (var entry in capturedFiles.Where(p => !capturedBuffers.ContainsKey(p.Key)).OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                string name = entry.Key, path = entry.Value;
                long size = new FileInfo(path).Length;
                result.Add(new BlockResource(name, size, (target, token) =>
                {
                    using var source = File.OpenRead(path);
                    using var sha = TransferCodec.CreateHash();
                    CopyCaptured(source, target, size, new byte[65536], token, sha);
                    sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    if (Hex(sha.Hash) != name.Substring(0, 64))
                        throw new IOException("A source resource changed since capture: " + name);
                }));
            }

            PairingTiming.Count("scene.packed-entry-count", pack.Entries.Count, TimingScope);
            PairingTiming.Count("scene.packed-unique-buffer-bytes", pack.Length, TimingScope);
            PairingTiming.Count("scene.packed-metadata-bytes", metadata.LongLength + references.LongLength + indexBytes, TimingScope);
            ValidateIndexForSceneVersion(indexBytes, result.Count);
            return result;
        }

        private void CopyCaptured(Stream source, Stream target, long remaining, byte[] buffer, CancellationToken token, HashAlgorithm sha)
        {
            while (remaining > 0)
            {
                token.ThrowIfCancellationRequested();
                int count;
                using (PairingTiming.Measure(sourceFileRead))
                    count = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                if (count == 0)
                    throw new InvalidDataException("Truncated source resource.");
                if (sha != null)
                    using (PairingTiming.Measure(sourceFileHash, count))
                        sha.TransformBlock(buffer, 0, count, null, 0);
                target.Write(buffer, 0, count);
                remaining -= count;
            }

            if (source.ReadByte() != -1)
                throw new InvalidDataException("Source resource size changed.");
        }

        private IReadOnlyList<BlockResource> blockResources;
        private int blocksOpened, blocksVerified;
        private bool attachedPack;
        private Task<(ScenePayload payload, PreparedSceneManifest manifest)> privateGraph;
        public bool StageGraphDuringBlocks { get; set; }

        public async Task<ScenePayload> ReadStagedOrPreparedAsync(string hash, Action<float, float, LoadingText> progress)
        {
            EnsureVerified();
            if (privateGraph == null)
                return await Task.Run(() => ReadPrepared(hash, progress), readToken).ConfigureAwait(false);
            using var waiting = PairingTiming.Measure("scene.decode-await-private-graph", TimingScope);
            var graph = await privateGraph.ConfigureAwait(false);
            return CompletePrepared(graph.payload, graph.manifest, hash, progress);
        }

        // Cancel the receive token first on failure. Joining here keeps the file handles
        // and workspace alive until private preparation has stopped.
        public async Task DrainPrivateGraphAsync()
        {
            if (privateGraph == null) return;
            try
            {
                await privateGraph.ConfigureAwait(false);
            }
            catch
            {
                /* The block receiver or the public read owns the error. */
            }
            finally
            {
                privateGraph = null;
            }
        }

        private void PreparePrivateGraph()
        {
            AttachPack();
            privateGraph = Task.Run(() =>
            {
                using var timing = PairingTiming.Measure("scene.decode-private-graph", TimingScope);
                return ReadSceneGraph(null);
            }, readToken);
        }

        private void AttachPack()
        {
            if (attachedPack) return;
            pack.Attach(Path.Combine(directory, BufferPack.DataName));
            attachedPack = true;
        }

        void IBlockSink.Begin(IReadOnlyList<BlockResource> resources, CancellationToken token)
        {
            EnsureWritable();
            if (Directory.EnumerateFileSystemEntries(directory).Any() || Globals == null)
                throw new InvalidOperationException("Empty paired workspace required.");
            if (resources.Count < 4 || resources[0].Name != "visualization.json" || resources[1].Name != SceneGlobalReferences.FileName || resources[2].Name != BufferPack.IndexName || resources[3].Name != BufferPack.DataName)
                throw new InvalidDataException("Invalid packed resource order.");
            ValidatePackedMetadataBudget(resources[0].Length, resources[1].Length, resources[2].Length);
            foreach (var resource in resources.Skip(4))
            {
                Resolve(resource.Name);
                if (resource.Name.EndsWith(".bin", StringComparison.Ordinal))
                    throw new InvalidDataException("Mixed packed and legacy resources.");
            }

            readingStarted = packedBuffers = true;
            readToken = token;
            blockResources = resources;
        }

        Stream IBlockSink.Open(int index)
        {
            readToken.ThrowIfCancellationRequested();
            if (index != blocksOpened++ || blocksVerified != index)
                throw new InvalidDataException("Invalid resource order.");
            var resource = blockResources[index];
            string path = Path.Combine(directory, resource.Name);
            if (index == 1)
            {
                using var json = File.OpenRead(Path.Combine(directory, "visualization.json"));
                ContentVersion = ReadSceneVersion(json);
                if (ContentVersion is not (ScenePayload.FormatVersion or ScenePayload.CompactSignalFormatVersion or ScenePayload.GroupedBufferFormatVersion))
                    throw new InvalidDataException("HBT4 requires a supported packed scene format.");
                ValidateIndexForSceneVersion(blockResources[2].Length, blockResources.Count);
            }

            if (index == 3)
            {
                using var input = File.OpenRead(Path.Combine(directory, BufferPack.IndexName));
                pack = BufferPack.ReadIndex(input, input.Length, resource.Length, readToken);
            }

            return new VerifiedBlockOutput(this, path, resource, index);
        }

        void IBlockSink.Complete()
        {
            readToken.ThrowIfCancellationRequested();
            if (blocksVerified != blockResources.Count)
                throw new InvalidDataException("Missing verified resources.");
            foreach (var item in blockResources.Where(r => r.Name.EndsWith(".pair", StringComparison.Ordinal)))
            {
                string[] members = ReadNativePair(item.Name);
                string path = ResolveNativeFile(item.Name);
                verifiedResources.Register(path, members[0].Substring(0, 64));
                verifiedResources.Register(StandardData.CompanionFile(path), members[1].Substring(0, 64));
                nativePaths.Add(item.Name, path);
            }

            AttachPack();
            verifiedResources.Seal();
            verifiedContent = true;
        }

        private static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

        private sealed class VerifiedBlockOutput : Stream
        {
            private readonly SceneArchive owner;
            private readonly FileStream file;
            private readonly BlockResource resource;
            private readonly HashAlgorithm sha;
            private readonly BufferPack.Verifier verifier;
            private long written;
            private bool closed;

            internal VerifiedBlockOutput(SceneArchive owner, string path, BlockResource resource, int index)
            {
                this.owner = owner;
                this.resource = resource;
                sha = index >= 4 ? TransferCodec.CreateHash() : null;
                verifier = index == 3 ? new BufferPack.Verifier(owner.pack, owner.stagedPackHash) : null;
                try
                {
                    file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536);
                }
                catch
                {
                    sha?.Dispose();
                    verifier?.Dispose();
                    throw;
                }
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                owner.readToken.ThrowIfCancellationRequested();
                if (offset != 0 || written + count > resource.Length)
                    throw new InvalidDataException("Invalid decoded range.");
                if (sha != null)
                    using (PairingTiming.Measure(owner.stagedResourceHash, count))
                        sha.TransformBlock(buffer, 0, count, null, 0);
                verifier?.Append(buffer, count, owner.readToken);
                using (PairingTiming.Measure(owner.stagedFileWrite, count))
                    file.Write(buffer, 0, count);
                written += count;
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposing || closed)
                    return;
                closed = true;
                try
                {
                    using (PairingTiming.Measure(owner.stagedFileWrite))
                        file.Dispose();
                    if (written != resource.Length)
                        return; // Failed/cancelled transfers never seal the owner.
                    verifier?.Complete();
                    if (sha != null)
                    {
                        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                        string hash = Hex(sha.Hash);
                        if (hash != resource.Name.Substring(0, 64))
                            throw new InvalidDataException("Resource checksum mismatch.");
                        if (!resource.Name.EndsWith(".pair", StringComparison.Ordinal))
                        {
                            string path = owner.Resolve(resource.Name);
                            owner.verifiedResources.Register(path, hash);
                            owner.nativePaths.Add(resource.Name, path);
                        }
                    }

                    owner.blocksVerified++;
                    if (verifier != null && owner.StageGraphDuringBlocks && owner.blockResources.Count > 4)
                        owner.PreparePrivateGraph();
                }
                finally
                {
                    sha?.Dispose();
                    verifier?.Dispose();
                }
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => !closed;
            public override long Length => written;

            public override long Position
            {
                get => written;
                set => throw new NotSupportedException();
            }

            public override void Flush() => file.Flush();
            public override int Read(byte[] b, int o, int c) => throw new NotSupportedException();
            public override long Seek(long o, SeekOrigin s) => throw new NotSupportedException();
            public override void SetLength(long v) => throw new NotSupportedException();
        }
    }
}
