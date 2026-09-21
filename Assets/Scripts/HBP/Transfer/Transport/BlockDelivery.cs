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
        private byte[] digest;
        private long bytes;
        private long rawBytes;
        private bool drained;
        public bool IsPrepared => producer.Status == TaskStatus.RanToCompletion;
        public long EncodedBytes => IsPrepared ? bytes : 0;
        public string ContentHash => IsPrepared ? BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant() : null;

        public BlockDelivery(string file, Func<IReadOnlyList<BlockResource>> prepare, Action release, CancellationToken token, Action<long> prepared = null)
        {
            spool = new FileStream(file, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read, 65536, true);
            cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
            producer = Task.Run(() =>
            {
                try
                {
                    var resources = prepare();
                    Interlocked.Exchange(ref rawBytes, resources.Sum(resource => resource.Length));
                    digest = BlockContainer.Produce(resources, packet =>
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        spool.Write(packet, 0, packet.Length);
                        bytes += packet.Length;
                        queue.Add(packet, cancellation.Token);
                    }, cancellation.Token);
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

        public async Task<DeliveryReceipt> SendAsync(Stream stream, CancellationToken stop, Action<long> progress, Action<long, long> logicalProgress = null, Action payloadWriteStarted = null, Action payloadSent = null)
        {
            try
            {
                long sent = 0;
                long rawSent = 0;
                bool writeStarted = false;
                if (!drained)
                {
                    while (true)
                    {
                        byte[] packet = await Task.Run(() => queue.TryTake(out var next, Timeout.Infinite, stop) ? next : null).ConfigureAwait(false);
                        if (packet == null)
                            break;
                        await stream.WriteAsync(packet, 0, packet.Length, stop).ConfigureAwait(false);
                        if (!writeStarted)
                        {
                            writeStarted = true;
                            payloadWriteStarted?.Invoke();
                        }

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
                        int read = await spool.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, bytes - sent), stop).ConfigureAwait(false);
                        if (read == 0)
                            throw new EndOfStreamException();
                        await stream.WriteAsync(buffer, 0, read, stop).ConfigureAwait(false);
                        if (!writeStarted)
                        {
                            writeStarted = true;
                            payloadWriteStarted?.Invoke();
                        }

                        sent += read;
                        progress?.Invoke(sent);
                        logicalProgress?.Invoke(sent, bytes);
                    }
                }

                payloadSent?.Invoke();
                var receipt = new byte[33];
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
