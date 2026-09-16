using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Transfer.Transport;

namespace HBP.Transfer.Scene
{
    public sealed class SceneDelivery : IDisposable
    {
        private readonly string file;
        private readonly FileStream source;
        private readonly byte[] digest;
        private readonly BlockDelivery blocks;
        private readonly SemaphoreSlim sendGate = new(1, 1);
        private readonly object lifetime = new();
        private readonly TaskCompletionSource<bool> released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int sends;
        private bool disposed;
        public string TransferId { get; }
        public string SessionId { get; }
        public string Summary { get; }

        private readonly string contentHash;
        private readonly long encodedBytes;
        public string ContentHash => blocks == null ? contentHash : blocks.ContentHash;
        public long EncodedBytes => blocks == null ? encodedBytes : blocks.EncodedBytes;
        public bool CanRetry => blocks == null || blocks.IsPrepared;

        internal SceneDelivery(string file, string transferId, string sessionId, string summary, Func<System.Collections.Generic.IReadOnlyList<BlockResource>> prepare, Action release, CancellationToken token)
        {
            this.file = file;
            TransferId = transferId;
            SessionId = sessionId;
            Summary = summary;
            blocks = new BlockDelivery(file, prepare, release, token);
        }

        public SceneDelivery(string file, string transferId, string sessionId, string summary)
        {
            this.file = file;
            TransferId = transferId;
            SessionId = sessionId;
            Summary = summary;
            source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, PinnedTlsTransfer.ChunkBytes, true);
            try
            {
                encodedBytes = source.Length;
                if (EncodedBytes < 1 || EncodedBytes > PinnedTlsTransfer.MaximumSceneFileBytes)
                    throw new InvalidDataException("Visualization exceeds the transfer budget.");
                using var sha = HBP.Transfer.Codecs.TransferCodec.CreateHash();
                digest = sha.ComputeHash(source);
                contentHash = BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant();
            }
            catch
            {
                source.Dispose();
                throw;
            }
        }

        public async Task<DeliveryReceipt> SendAsync(Stream stream, CancellationToken stop, Action<long> progress = null)
        {
            lock (lifetime)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(SceneDelivery));
                ++sends;
            }

            bool entered = false;
            try
            {
                await sendGate.WaitAsync(stop).ConfigureAwait(false);
                entered = true;
                if (blocks != null)
                    return await blocks.SendAsync(stream, stop, progress).ConfigureAwait(false);
                return await PinnedTlsTransfer.SendPreparedFileAsync(stream, source, digest, stop, progress).ConfigureAwait(false);
            }
            finally
            {
                if (entered)
                    sendGate.Release();
                lock (lifetime)
                {
                    --sends;
                    if (disposed && sends == 0)
                        DeleteFiles();
                }
            }
        }

        public void Dispose()
        {
            lock (lifetime)
            {
                if (disposed) return;
                disposed = true;
                if (sends == 0) DeleteFiles();
            }
        }

        public Task DisposeAsync()
        {
            Dispose();
            return released.Task;
        }

        private void DeleteFiles()
        {
            if (blocks != null)
            {
                _ = CloseBlocksAsync();
                return;
            }

            source.Dispose();
            sendGate.Dispose();
            DeleteArtifact();
            released.TrySetResult(true);
        }

        private async Task CloseBlocksAsync()
        {
            try
            {
                await blocks.CloseAsync().ConfigureAwait(false);
                sendGate.Dispose();
                DeleteArtifact();
                released.TrySetResult(true);
            }
            catch (Exception error)
            {
                released.TrySetException(error);
            }
        }

        private void DeleteArtifact()
        {
            if (File.Exists(file))
                File.Delete(file);
            string directory = Path.GetDirectoryName(file);
            if (Directory.Exists(directory) && Directory.GetFileSystemEntries(directory).Length == 0)
                Directory.Delete(directory);
        }
    }
}
