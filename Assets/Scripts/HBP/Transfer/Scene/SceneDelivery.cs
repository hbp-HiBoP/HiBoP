using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Sync;
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
        private readonly SyncTelemetryIdentity telemetryIdentity;
        private long nextAttemptId;
        private SyncTelemetryPoint encodedTelemetryPoint;
        private long encodedTelemetryBytes;
        private int encodedTelemetryPublished;
        private SyncTelemetryPoint captureStartTelemetryPoint;
        private SyncTelemetryPoint captureEndTelemetryPoint;
        private SyncTelemetryPoint queuedTelemetryPoint;
        private int captureTelemetryPublished;
        private int sends;
        private bool disposed;
        public string TransferId { get; }
        public string SessionId { get; }
        public string Summary { get; }
        public Base3DScene SourceScene { get; internal set; }
        public PreparedSceneManifest PreparedManifest { get; private set; }

        private readonly string contentHash;
        private readonly long encodedBytes;
        public string ContentHash => blocks == null ? contentHash : blocks.ContentHash;
        public long EncodedBytes => blocks == null ? encodedBytes : blocks.EncodedBytes;
        public bool CanRetry => blocks == null || blocks.IsPrepared;

        internal SceneDelivery(string file, string transferId, string sessionId, string summary, Func<System.Collections.Generic.IReadOnlyList<BlockResource>> prepare, Action release, CancellationToken token, SyncTelemetryIdentity telemetryIdentity = default)
        {
            this.file = file;
            TransferId = transferId;
            SessionId = sessionId;
            Summary = summary;
            this.telemetryIdentity = telemetryIdentity;
            blocks = new BlockDelivery(file, prepare, release, token, bytes =>
            {
                if (!telemetryIdentity.IsValid) return;
                encodedTelemetryPoint = SyncTelemetry.CapturePoint();
                encodedTelemetryBytes = bytes;
            });
        }

        public SceneDelivery(string file, string transferId, string sessionId, string summary, SyncTelemetryIdentity telemetryIdentity = default)
        {
            this.file = file;
            TransferId = transferId;
            SessionId = sessionId;
            Summary = summary;
            this.telemetryIdentity = telemetryIdentity;
            source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, PinnedTlsTransfer.ChunkBytes, true);
            try
            {
                encodedBytes = source.Length;
                if (EncodedBytes < 1 || EncodedBytes > PinnedTlsTransfer.MaximumSceneFileBytes)
                    throw new InvalidDataException("Visualization exceeds the transfer budget.");
                using var sha = HBP.Transfer.Codecs.TransferCodec.CreateHash();
                digest = sha.ComputeHash(source);
                contentHash = BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant();
                if (telemetryIdentity.IsValid)
                {
                    encodedTelemetryPoint = SyncTelemetry.CapturePoint();
                    encodedTelemetryBytes = encodedBytes;
                }
            }
            catch
            {
                source.Dispose();
                throw;
            }
        }

        internal void SetPreparedManifest(PreparedSceneManifest manifest)
        {
            if (manifest == null || PreparedManifest != null) throw new InvalidOperationException("The delivery manifest is unavailable or already set.");
            PreparedManifest = manifest;
        }

        internal void SetCaptureTelemetry(SyncTelemetryPoint captureStart, SyncTelemetryPoint captureEnd, SyncTelemetryPoint queued)
        {
            captureStartTelemetryPoint = captureStart;
            captureEndTelemetryPoint = captureEnd;
            queuedTelemetryPoint = queued;
        }

        public PreparedSceneManifest RequirePreparedManifest()
        {
            if (PreparedManifest != null) return PreparedManifest;
            if (blocks != null) throw new InvalidOperationException("The block delivery is not prepared.");
            return PreparedManifest = PreparedSceneManifest.FromArchiveFile(file);
        }

        public async Task<DeliveryReceipt> SendAsync(Stream stream, CancellationToken stop, Action<long> progress = null, Action<long, long> detailedProgress = null)
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
                long attemptId = Interlocked.Increment(ref nextAttemptId);
                SyncTelemetryIdentity attemptIdentity = telemetryIdentity.WithAttempt(attemptId);
                SyncTelemetryPoint firstWritten = default;
                SyncTelemetryPoint lastWritten = default;
                DeliveryReceipt receipt;
                if (blocks != null)
                {
                    try
                    {
                        receipt = await blocks.SendAsync(stream, stop, progress, detailedProgress, () => firstWritten = SyncTelemetry.CapturePoint(), () => lastWritten = SyncTelemetry.CapturePoint()).ConfigureAwait(false);
                    }
                    catch
                    {
                        PublishCapture();
                        PublishEncoded();
                        PublishAttempt(attemptIdentity, firstWritten, lastWritten, default, blocks.EncodedBytes);
                        throw;
                    }
                }
                else
                {
                    try
                    {
                        receipt = await PinnedTlsTransfer.SendPreparedFileAsync(stream, source, digest, stop, count =>
                        {
                            progress?.Invoke(count);
                            detailedProgress?.Invoke(count, encodedBytes);
                            if (count >= encodedBytes) lastWritten = SyncTelemetry.CapturePoint();
                        }, () => firstWritten = SyncTelemetry.CapturePoint()).ConfigureAwait(false);
                    }
                    catch
                    {
                        PublishCapture();
                        PublishEncoded();
                        PublishAttempt(attemptIdentity, firstWritten, lastWritten, default, encodedBytes);
                        throw;
                    }
                }

                SyncTelemetryPoint publicationReceipt = SyncTelemetry.CapturePoint();
                PublishCapture();
                PublishEncoded();
                PublishAttempt(attemptIdentity, firstWritten, lastWritten, publicationReceipt, EncodedBytes);
                return receipt;
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

        private void PublishEncoded()
        {
            if (!encodedTelemetryPoint.IsValid || Interlocked.Exchange(ref encodedTelemetryPublished, 1) != 0) return;
            SyncTelemetry.MarkAt(SyncProfile.InitialTransfer, telemetryIdentity, SyncMilestone.Encoded, encodedTelemetryPoint, encodedTelemetryBytes);
        }

        private void PublishCapture()
        {
            if (!telemetryIdentity.IsValid || Interlocked.Exchange(ref captureTelemetryPublished, 1) != 0) return;
            SyncTelemetry.MarkAt(SyncProfile.InitialTransfer, telemetryIdentity, SyncMilestone.CaptureStart, captureStartTelemetryPoint);
            SyncTelemetry.MarkAt(SyncProfile.InitialTransfer, telemetryIdentity, SyncMilestone.CaptureEnd, captureEndTelemetryPoint);
            SyncTelemetry.MarkAt(SyncProfile.InitialTransfer, telemetryIdentity, SyncMilestone.Queued, queuedTelemetryPoint);
        }

        private static void PublishAttempt(SyncTelemetryIdentity identity, SyncTelemetryPoint firstWritten, SyncTelemetryPoint lastWritten, SyncTelemetryPoint publicationReceipt, long payloadBytes)
        {
            if (!identity.IsValid) return;
            SyncTelemetry.MarkAt(SyncProfile.InitialTransfer, identity, SyncMilestone.FirstByteWritten, firstWritten, payloadBytes);
            SyncTelemetry.MarkAt(SyncProfile.InitialTransfer, identity, SyncMilestone.LastByteWritten, lastWritten, payloadBytes);
            SyncTelemetry.MarkAt(SyncProfile.InitialTransfer, identity, SyncMilestone.PublicationReceipt, publicationReceipt, payloadBytes);
        }

        public void Dispose()
        {
            PublishCapture();
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
