using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace HBP.Transfer.Transport
{
    /// <summary>One producer, two queued blocks, and an owned immutable spool for exact retries.</summary>
    public sealed class BlockDelivery
    {
        private readonly FileStream spool;
        private readonly BlockingCollection<byte[]> queue = new(BlockContainer.QueueCapacity);
        private readonly CancellationTokenSource cancellation;
        private readonly Task producer;
        private readonly string timingScope;
        private byte[] digest;
        private long bytes;
        private long rawBytes;
        private bool drained;
        private volatile bool resourcesReady;
        public bool IsPreparingResources => !resourcesReady && !producer.IsCompleted;
        public bool IsPrepared => producer.Status == TaskStatus.RanToCompletion;
        public long EncodedBytes => IsPrepared ? bytes : 0;
        public string ContentHash => IsPrepared ? BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant() : null;

        public BlockDelivery(string file, Func<IReadOnlyList<BlockResource>> prepare, Action release, CancellationToken token, Action<long> prepared = null, string timingScope = null)
        {
            this.timingScope = timingScope;
            spool = new FileStream(file, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 65536, true);
            cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
            producer = Task.Run(() =>
            {
                try
                {
                    var resources = prepare();
                    resourcesReady = true;
                    // Once resources are owned, retries use the spool. Do not retain the
                    // detached JSON graph while the bounded producer waits for a sender.
                    prepare = null;
                    Interlocked.Exchange(ref rawBytes, resources.Sum(resource => resource.Length));
                    using var spoolWrites = PairingTiming.Aggregate("desktop.blocks-spool-write", timingScope);
                    using var enqueue = PairingTiming.Aggregate("desktop.blocks-producer-queue-wait", timingScope);
                    digest = BlockContainer.Produce(resources, packet =>
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        using (PairingTiming.Measure(spoolWrites, packet.Length))
                            spool.Write(packet, 0, packet.Length);
                        bytes += packet.Length;
                        using (PairingTiming.Measure(enqueue))
                            queue.Add(packet, cancellation.Token);
                    }, cancellation.Token, timingScope);
                    using (PairingTiming.Measure(spoolWrites))
                        spool.Flush();
                    prepared?.Invoke(bytes);
                }
                finally
                {
                    try
                    {
                        release();
                    }
                    finally
                    {
                        queue.CompleteAdding();
                    }
                }
            });
        }

        public async Task<DeliveryReceipt> SendAsync(Stream stream, CancellationToken stop, Action<long> progress, Action<long, long> logicalProgress = null, Action payloadWriteStarted = null, Action payloadSent = null, long timingAttempt = 0, Action lastPayloadWrite = null)
        {
            using var tls = PairingTiming.Aggregate("desktop.blocks-tls-write", timingScope, timingAttempt);
            using var dequeue = PairingTiming.Aggregate("desktop.blocks-sender-queue-wait", timingScope, timingAttempt);
            using var spoolReads = PairingTiming.Aggregate("desktop.blocks-retry-spool-read", timingScope, timingAttempt);
            try
            {
                long sent = 0;
                long rawSent = 0;
                bool writeStarted = false;
                if (!drained)
                {
                    while (true)
                    {
                        byte[] packet;
                        using (PairingTiming.Measure(dequeue))
                            packet = await Task.Run(() => queue.TryTake(out var next, Timeout.Infinite, stop) ? next : null).ConfigureAwait(false);
                        if (packet == null)
                            break;
                        using (PairingTiming.Measure(tls, packet.Length))
                            await stream.WriteAsync(packet, 0, packet.Length, stop).ConfigureAwait(false);
                        if (!writeStarted)
                        {
                            writeStarted = true;
                            payloadWriteStarted?.Invoke();
                        }

                        if (packet.Length == 64 && packet[0] == 2)
                            lastPayloadWrite?.Invoke();
                        sent += packet.Length;
                        progress?.Invoke(sent);
                        if (packet.Length >= 64 && packet[0] == 1)
                        {
                            rawSent += packet[20] | packet[21] << 8 | packet[22] << 16 | packet[23] << 24;
                            long totalRaw = Interlocked.Read(ref rawBytes);
                            if (totalRaw > 0) logicalProgress?.Invoke(rawSent, totalRaw);
                        }
                    }

                    await producer.ConfigureAwait(false);
                    drained = true;
                }
                else
                {
                    await producer.ConfigureAwait(false);
                    spool.Position = 0;
                    var buffer = new byte[262144];
                    while (sent < bytes)
                    {
                        int available = (int)Math.Min(buffer.Length, bytes - sent);
                        int read;
                        using (PairingTiming.Measure(spoolReads, available))
                            read = await spool.ReadAsync(buffer, 0, available, stop).ConfigureAwait(false);
                        if (read == 0)
                            throw new EndOfStreamException();
                        using (PairingTiming.Measure(tls, read))
                            await stream.WriteAsync(buffer, 0, read, stop).ConfigureAwait(false);
                        if (!writeStarted)
                        {
                            writeStarted = true;
                            payloadWriteStarted?.Invoke();
                        }

                        sent += read;
                        if (sent == bytes) lastPayloadWrite?.Invoke();
                        progress?.Invoke(sent);
                        logicalProgress?.Invoke(sent, bytes);
                    }
                }

                payloadSent?.Invoke();
                var receipt = new byte[33];
                using (PairingTiming.Measure("desktop.scene-publication-receipt-wait", timingScope, timingAttempt))
                    await PinnedTlsTransfer.ReadExactAsync(stream, receipt, 0, 33, stop).ConfigureAwait(false);
                int diff = 0;
                for (int i = 0; i < 32; i++)
                    diff |= digest[i] ^ receipt[1 + i];
                if (diff != 0 || receipt[0] < 1 || receipt[0] > 4)
                    throw new InvalidDataException("Invalid block publication receipt.");
                return new DeliveryReceipt(digest, (DeliveryStatus)receipt[0]);
            }
            catch
            {
                if (!IsPrepared)
                    cancellation.Cancel();
                try
                {
                    await producer.ConfigureAwait(false);
                }
                catch
                {
                }

                while (queue.TryTake(out _))
                {
                }

                drained = true;
                throw;
            }
        }

        public async Task CloseAsync()
        {
            cancellation.Cancel();
            try
            {
                await producer.ConfigureAwait(false);
            }
            catch
            {
            }

            spool.Dispose();
            queue.Dispose();
            cancellation.Dispose();
        }
    }
}
