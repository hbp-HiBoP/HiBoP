using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace HBP.Transfer.Transport
{
    /// <summary>Bounded, one-shot TLS transfer. Receipt follows successful application publication. No discovery or persistence.</summary>
    public static class PinnedTlsTransfer
    {
        public const int ChunkBytes = 64 * 1024;
        public const int MaximumPayloadBytes = 64 * 1024 * 1024;
        private const int DeadlineSeconds = 20;

        public static async Task ServeAsync(TcpListener listener, X509Certificate2 identity, byte[] pairingSecret, byte[] payload, CancellationToken stop, Action<string> record, Action<DeliveryReceipt> acknowledged = null, bool corruptChunk = false)
        {
            ValidateSecret(pairingSecret);
            if (payload == null || payload.Length == 0 || payload.Length > MaximumPayloadBytes) throw new ArgumentOutOfRangeException(nameof(payload));
            payload = (byte[])payload.Clone();
            pairingSecret = (byte[])pairingSecret.Clone();
            using var stopAccept = stop.Register(listener.Stop);
            try
            {
                // A single offer accepts at most 16 attempts and expires through its owner token.
                for (int attempt = 0; attempt < 16; attempt++)
                {
                    if (stop.IsCancellationRequested) break;
                    TcpClient peer;
                    try
                    {
                        peer = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    }
                    catch (Exception) when (stop.IsCancellationRequested)
                    {
                        break;
                    }

                    using (peer)
                    using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop))
                    using (deadline.Token.Register(peer.Close))
                    {
                        deadline.CancelAfter(TimeSpan.FromSeconds(DeadlineSeconds));
                        record("accepted");
                        try
                        {
                            using var tls = new SslStream(peer.GetStream(), false);
                            await tls.AuthenticateAsServerAsync(identity, false, SslProtocols.Tls12, false).ConfigureAwait(false);
                            if (!tls.IsEncrypted || !tls.IsAuthenticated) throw new AuthenticationException("TLS not authenticated.");
                            var offered = new byte[32];
                            await ReadExactAsync(tls, offered, 0, offered.Length, deadline.Token).ConfigureAwait(false);
                            bool paired = TransportIdentity.Equal(offered, pairingSecret);
                            Array.Clear(offered, 0, offered.Length);
                            await tls.WriteAsync(new[] { (byte)(paired ? 1 : 0) }, 0, 1, deadline.Token).ConfigureAwait(false);
                            if (!paired)
                            {
                                record("pairing-rejected");
                                continue;
                            }

                            DeliveryReceipt receipt = await SendPayloadAsync(tls, payload, deadline.Token, corruptChunk).ConfigureAwait(false);
                            acknowledged?.Invoke(receipt);
                            record("delivered");
                        }
                        catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is AuthenticationException || exception is ObjectDisposedException || exception is SocketException || exception is OperationCanceledException)
                        {
                            record("connection-ended:" + exception.GetType().Name);
                        }
                        finally
                        {
                            record("released");
                        }
                    }
                }
            }
            finally
            {
                Array.Clear(pairingSecret, 0, pairingSecret.Length);
                listener.Stop();
                record("listener-stopped");
            }
        }

        public static async Task<DeliveryReceipt> ReceiveAsync(string host, int port, byte[] pin, byte[] pairingSecret, CancellationToken stop, Func<byte[], CancellationToken, Task<DeliveryStatus>> publish, Action<int> progress = null)
        {
            if (pin == null || pin.Length != 32) throw new ArgumentException("A full out-of-band certificate pin is required.", nameof(pin));
            ValidateSecret(pairingSecret);
            if (publish == null) throw new ArgumentNullException(nameof(publish));
            // Freeze the caller's trust decision for the entire connection.
            pin = (byte[])pin.Clone();
            pairingSecret = (byte[])pairingSecret.Clone();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop);
            deadline.CancelAfter(TimeSpan.FromSeconds(DeadlineSeconds));
            using var peer = new TcpClient();
            using var cancellation = deadline.Token.Register(peer.Close);
            bool pinRejected = false;
            try
            {
                await peer.ConnectAsync(host, port).ConfigureAwait(false);
                using var tls = new SslStream(peer.GetStream(), false, (_, certificate, __, ___) =>
                {
                    bool accepted = TransportIdentity.Matches(certificate, pin);
                    if (!accepted) pinRejected = true;
                    return accepted;
                });
                try
                {
                    await tls.AuthenticateAsClientAsync("HiBoP-Transport-Probe", null, SslProtocols.Tls12, false).ConfigureAwait(false);
                }
                catch (Exception exception) when (pinRejected)
                {
                    throw new AuthenticationException("Certificate pin rejected.", exception);
                }

                if (!tls.IsEncrypted || !tls.IsAuthenticated) throw new AuthenticationException("TLS not authenticated.");
                await tls.WriteAsync(pairingSecret, 0, pairingSecret.Length, deadline.Token).ConfigureAwait(false);
                var paired = new byte[1];
                await ReadExactAsync(tls, paired, 0, 1, deadline.Token).ConfigureAwait(false);
                if (paired[0] != 1) throw new AuthenticationException("Pairing secret rejected.");
                return await ReceivePayloadAsync(tls, deadline.Token, publish, progress).ConfigureAwait(false);
            }
            finally
            {
                Array.Clear(pairingSecret, 0, pairingSecret.Length);
            }
        }

        public static async Task<DeliveryReceipt> SendPayloadAsync(Stream stream, byte[] payload, CancellationToken stop, bool corruptChunk = false)
        {
            if (payload == null || payload.Length == 0 || payload.Length > MaximumPayloadBytes) throw new InvalidDataException("Payload size out of bounds.");
            using var sha = SHA256.Create();
            var header = new byte[40];
            header[0] = (byte)'H';
            header[1] = (byte)'B';
            header[2] = (byte)'T';
            header[3] = 2; // v2 acknowledges application publication, never just received bytes.
            PutInt(header, 4, payload.Length);
            byte[] digest = sha.ComputeHash(payload);
            Buffer.BlockCopy(digest, 0, header, 8, 32);
            await stream.WriteAsync(header, 0, header.Length, stop).ConfigureAwait(false);
            var chunkHeader = new byte[40];
            int index = 0;
            for (int offset = 0; offset < payload.Length; offset += ChunkBytes)
            {
                int count = Math.Min(ChunkBytes, payload.Length - offset);
                PutInt(chunkHeader, 0, index++);
                PutInt(chunkHeader, 4, count);
                Buffer.BlockCopy(sha.ComputeHash(payload, offset, count), 0, chunkHeader, 8, 32);
                if (corruptChunk && offset == 0) chunkHeader[8] ^= 1; // Explicit adversarial probe injection.
                await stream.WriteAsync(chunkHeader, 0, chunkHeader.Length, stop).ConfigureAwait(false);
                await stream.WriteAsync(payload, offset, count, stop).ConfigureAwait(false);
            }

            var receipt = new byte[33];
            await ReadExactAsync(stream, receipt, 0, receipt.Length, stop).ConfigureAwait(false);
            var receivedDigest = new byte[32];
            Buffer.BlockCopy(receipt, 1, receivedDigest, 0, 32);
            if (!TransportIdentity.Equal(receivedDigest, digest) || receipt[0] < 1 || receipt[0] > 4) throw new InvalidDataException("Invalid publication receipt.");
            return new DeliveryReceipt(digest, (DeliveryStatus)receipt[0]);
        }

        public static async Task<DeliveryReceipt> ReceivePayloadAsync(Stream stream, CancellationToken stop, Func<byte[], CancellationToken, Task<DeliveryStatus>> publish, Action<int> progress = null)
        {
            var header = new byte[40];
            await ReadExactAsync(stream, header, 0, header.Length, stop).ConfigureAwait(false);
            if (header[0] != 'H' || header[1] != 'B' || header[2] != 'T' || header[3] != 2) throw new InvalidDataException("Unknown transport version.");
            int total = GetInt(header, 4);
            if (total <= 0 || total > MaximumPayloadBytes) throw new InvalidDataException("Payload size out of bounds.");
            var expected = new byte[32];
            Buffer.BlockCopy(header, 8, expected, 0, 32);
            var payload = new byte[total];
            var chunkHeader = new byte[40];
            var chunkHash = new byte[32];
            using var sha = SHA256.Create();
            int index = 0;
            for (int offset = 0; offset < total; offset += ChunkBytes)
            {
                int count = Math.Min(ChunkBytes, total - offset);
                await ReadExactAsync(stream, chunkHeader, 0, chunkHeader.Length, stop).ConfigureAwait(false);
                if (GetInt(chunkHeader, 0) != index++ || GetInt(chunkHeader, 4) != count) throw new InvalidDataException("Invalid chunk index or size.");
                await ReadExactAsync(stream, payload, offset, count, stop).ConfigureAwait(false);
                Buffer.BlockCopy(chunkHeader, 8, chunkHash, 0, 32);
                if (!TransportIdentity.Equal(chunkHash, sha.ComputeHash(payload, offset, count))) throw new InvalidDataException("Chunk hash mismatch.");
                progress?.Invoke(offset + count);
                stop.ThrowIfCancellationRequested();
            }

            byte[] digest = sha.ComputeHash(payload);
            if (!TransportIdentity.Equal(expected, digest)) throw new InvalidDataException("Payload hash mismatch.");
            stop.ThrowIfCancellationRequested();
            DeliveryStatus status = await publish(payload, stop).ConfigureAwait(false);
            var result = new DeliveryReceipt(digest, status);
            var receipt = new byte[33];
            receipt[0] = (byte)status;
            Buffer.BlockCopy(digest, 0, receipt, 1, 32);
            // An ACK failure cannot undo a publication that already succeeded.
            await stream.WriteAsync(receipt, 0, receipt.Length, stop).ConfigureAwait(false);
            return result;
        }

        public static async Task ReadExactAsync(Stream stream, byte[] bytes, int offset, int count, CancellationToken stop)
        {
            while (count > 0)
            {
                int read = await stream.ReadAsync(bytes, offset, count, stop).ConfigureAwait(false);
                if (read == 0) throw new EndOfStreamException("Truncated transport frame.");
                offset += read;
                count -= read;
            }
        }

        private static void ValidateSecret(byte[] secret)
        {
            if (secret == null || secret.Length != 32) throw new ArgumentException("A 256-bit pairing secret is required.", nameof(secret));
        }

        private static int GetInt(byte[] bytes, int offset) => bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24;

        private static void PutInt(byte[] bytes, int offset, int value)
        {
            for (int i = 0; i < 4; i++) bytes[offset + i] = (byte)(value >> (i * 8));
        }
    }
}
