using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HBP.Transfer.Transport
{
    /// <summary>Serialized authenticated receiver. Discovery is never an authentication decision.</summary>
    public sealed class QuestPairing : IDisposable
    {
        public const int Port = 45871;
        private readonly X509Certificate2 identity;
        private byte[] secret;
        private readonly string storagePath;
        private readonly string deviceName;
        private readonly Stopwatch age = Stopwatch.StartNew();
        private readonly SemaphoreSlim pairingGate = new(1, 1);
        private long lastContact;
        private int attempts, paired, ownerClaimed, preparing, receiving;
        private string globalsId;
        public string Code { get; }
        public byte[] Pin => TransportIdentity.Hash(identity.RawData);
        public string Fingerprint => FormatPin(Pin);
        public string Announcement => QuestDiscovery.Encode(Pin, deviceName);
        public bool IsPaired => Volatile.Read(ref paired) != 0;
        public bool IsConnected => IsPaired && (Volatile.Read(ref receiving) != 0 || DateTime.UtcNow.Ticks - Interlocked.Read(ref lastContact) < TimeSpan.FromSeconds(15).Ticks);
        public bool IsPreparing => Volatile.Read(ref preparing) != 0;
        public bool IsLocked => Volatile.Read(ref ownerClaimed) != 0 || Volatile.Read(ref attempts) >= 5 || age.Elapsed >= TimeSpan.FromMinutes(5);

        public QuestPairing(string storagePath = null, string deviceName = "Quest", bool renew = false)
        {
            this.storagePath = storagePath;
            this.deviceName = deviceName;
            byte[] stored = storagePath == null ? null : PairingStorage.Read(storagePath);
            if (stored == null)
            {
                identity = TransportIdentity.Create();
                secret = RandomBytes(32);
                Save();
            }
            else
            {
                try
                {
                    using var reader = new BinaryReader(new MemoryStream(stored, false));
                    if (reader.ReadInt32() != 1) throw new InvalidDataException("Unsupported saved Quest identity.");
                    secret = reader.ReadBytes(32);
                    int length = reader.ReadInt32();
                    if (secret.Length != 32 || length < 1 || length > 32768) throw new InvalidDataException("Invalid saved Quest identity.");
                    byte[] pfx = reader.ReadBytes(length);
                    try
                    {
                        identity = new X509Certificate2(pfx, "", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
                    }
                    finally
                    {
                        Array.Clear(pfx, 0, pfx.Length);
                    }
                }
                finally
                {
                    Array.Clear(stored, 0, stored.Length);
                }
            }

            if (renew)
            {
                Array.Clear(secret, 0, secret.Length);
                secret = RandomBytes(32);
                Save();
            }

            uint value;
            do
            {
                value = BitConverter.ToUInt32(RandomBytes(4), 0);
            } while (value >= 4294000000u);

            Code = (value % 1000000).ToString("D6");
        }

        private void Save()
        {
            if (storagePath == null) return;
            byte[] pfx = identity.Export(X509ContentType.Pkcs12, "");
            using var buffer = new MemoryStream();
            using (var writer = new BinaryWriter(buffer, Encoding.UTF8, true))
            {
                writer.Write(1);
                writer.Write(secret);
                writer.Write(pfx.Length);
                writer.Write(pfx);
            }

            byte[] bytes = buffer.ToArray();
            try
            {
                PairingStorage.Write(storagePath, bytes);
            }
            finally
            {
                Array.Clear(pfx, 0, pfx.Length);
                Array.Clear(bytes, 0, bytes.Length);
            }
        }

        public async Task ServeAsync(TcpListener listener, CancellationToken stop, Func<Stream, CancellationToken, Task<DeliveryReceipt>> receive, Action<string> state, Func<Stream, CancellationToken, Task<DeliveryReceipt>> receiveGlobals = null, Func<Stream, CancellationToken, Task> receiveReplica = null)
        {
            using var cancelAccept = stop.Register(listener.Stop);
            using var slots = new SemaphoreSlim(4, 4);
            var handlers = new List<Task>();
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    TcpClient peer;
                    try
                    {
                        peer = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    }
                    catch (Exception) when (stop.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        await slots.WaitAsync(stop).ConfigureAwait(false);
                        handlers.Add(HandleAndReleaseAsync(peer, slots, stop, receive, state, receiveGlobals, receiveReplica));
                        handlers.RemoveAll(task => task.Status == TaskStatus.RanToCompletion);
                    }
                    catch
                    {
                        peer.Dispose();
                        throw;
                    }
                }
            }
            finally
            {
                listener.Stop();
                await Task.WhenAll(handlers).ConfigureAwait(false);
            }
        }

        private async Task HandleAndReleaseAsync(TcpClient peer, SemaphoreSlim slots, CancellationToken stop, Func<Stream, CancellationToken, Task<DeliveryReceipt>> receive, Action<string> state, Func<Stream, CancellationToken, Task<DeliveryReceipt>> receiveGlobals, Func<Stream, CancellationToken, Task> receiveReplica)
        {
            try
            {
                await HandlePeerAsync(peer, stop, receive, state, receiveGlobals, receiveReplica).ConfigureAwait(false);
            }
            finally
            {
                slots.Release();
            }
        }

        private async Task HandlePeerAsync(TcpClient peer, CancellationToken stop, Func<Stream, CancellationToken, Task<DeliveryReceipt>> receive, Action<string> state, Func<Stream, CancellationToken, Task<DeliveryReceipt>> receiveGlobals, Func<Stream, CancellationToken, Task> receiveReplica)
        {
            using (peer)
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop))
            using (deadline.Token.Register(peer.Close))
            {
                peer.NoDelay = true;
                deadline.CancelAfter(TimeSpan.FromSeconds(15));
                try
                {
                    using var tls = new SslStream(peer.GetStream(), false);
                    await tls.AuthenticateAsServerAsync(identity, false, SslProtocols.Tls12, false).ConfigureAwait(false);
                    int command = await ReadByteAsync(tls, deadline.Token).ConfigureAwait(false);
                    if (command == 0) return;
                    if (command == 20)
                    {
                        byte[] info = Encoding.UTF8.GetBytes(Announcement);
                        await tls.WriteAsync(info, 0, info.Length, deadline.Token).ConfigureAwait(false);
                        return;
                    }

                    // Versions that send a raw code are deliberately rejected.
                    if (command == 1 || command == 3)
                    {
                        await ReplyAsync(tls, false, deadline.Token).ConfigureAwait(false);
                        return;
                    }

                    if (command == 10 || command == 11)
                    {
                        await pairingGate.WaitAsync(deadline.Token).ConfigureAwait(false);
                        try
                        {
                            bool allowed = !IsLocked && !IsPaired && ((command == 11) == (receiveGlobals != null));
                            if (allowed) Interlocked.Increment(ref attempts); // Includes abandoned/malformed exchanges.
                            await ReplyAsync(tls, allowed, deadline.Token).ConfigureAwait(false);
                            if (!allowed) return;
                            await Task.Run(() => PairingPake.AuthenticateAsync(tls, Code, Pin, true, deadline.Token), deadline.Token).ConfigureAwait(false);
                            if (age.Elapsed >= TimeSpan.FromMinutes(5)) throw new AuthenticationException("Pairing code expired.");
                            Interlocked.Exchange(ref ownerClaimed, 1);
                            Array.Clear(secret, 0, secret.Length);
                            secret = RandomBytes(32);
                            Save(); // Durable ownership before acknowledging authentication.
                            await tls.WriteAsync(secret, 0, secret.Length, deadline.Token).ConfigureAwait(false);
                            if (receiveGlobals == null) Interlocked.Exchange(ref paired, 1);
                            Touch();
                            state(receiveGlobals == null ? "Paired. Ready to receive." : "Desktop recognized. Waiting for shared data...");
                        }
                        finally
                        {
                            pairingGate.Release();
                        }
                    }
                    else if (command == 2 || command == 12 || command == 13 || command == 14)
                    {
                        byte[] offered = new byte[32];
                        await PinnedTlsTransfer.ReadExactAsync(tls, offered, 0, offered.Length, deadline.Token).ConfigureAwait(false);
                        bool allowed = TransportIdentity.Equal(offered, secret) && (command == 12 || command == 13 || IsPaired) && (command != 14 || receiveReplica != null);
                        Array.Clear(offered, 0, offered.Length);
                        await ReplyAsync(tls, allowed, deadline.Token).ConfigureAwait(false);
                        if (!allowed) return;
                        Touch();
                        if (command == 12)
                        {
                            await ReplyAsync(tls, IsPaired, deadline.Token).ConfigureAwait(false);
                            return;
                        }

                        if (command == 14)
                        {
                            deadline.CancelAfter(Timeout.InfiniteTimeSpan);
                            await receiveReplica(tls, deadline.Token).ConfigureAwait(false);
                            return;
                        }

                        deadline.CancelAfter(TimeSpan.FromMinutes(30));
                        if (command == 13)
                        {
                            byte[] id = new byte[32];
                            await PinnedTlsTransfer.ReadExactAsync(tls, id, 0, id.Length, deadline.Token).ConfigureAwait(false);
                            string nextId = Encoding.ASCII.GetString(id);
                            if (!Guid.TryParseExact(nextId, "N", out _)) throw new InvalidDataException("Invalid global snapshot identity.");
                            bool needsGlobals = !IsPaired || globalsId != nextId;
                            await ReplyAsync(tls, needsGlobals, deadline.Token).ConfigureAwait(false);
                            if (needsGlobals)
                            {
                                if (receiveGlobals == null) throw new InvalidOperationException("Shared data receiver unavailable.");
                                Interlocked.Exchange(ref preparing, 1);
                                try
                                {
                                    await receiveGlobals(tls, deadline.Token).ConfigureAwait(false);
                                }
                                finally
                                {
                                    Interlocked.Exchange(ref preparing, 0);
                                }

                                globalsId = nextId;
                                Interlocked.Exchange(ref paired, 1);
                            }

                            Touch();
                            state("Paired. Ready to receive.");
                            await ReplyAsync(tls, true, deadline.Token).ConfigureAwait(false);
                        }
                        else
                        {
                            DeliveryReceipt receipt;
                            Interlocked.Exchange(ref receiving, 1);
                            try
                            {
                                receipt = await receive(tls, deadline.Token).ConfigureAwait(false);
                            }
                            finally
                            {
                                Interlocked.Exchange(ref receiving, 0);
                            }

                            Touch();
                            state(receipt.Status == DeliveryStatus.Published || receipt.Status == DeliveryStatus.AlreadyPublished ? "Visualization ready. Desktop connected." : "Old delivery closed/replaced. Send a new snapshot from Desktop.");
                        }
                    }
                    else throw new InvalidDataException("Unsupported pairing version. Update both applications.");
                }
                catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is V2TransportProtocolException || exception is SocketException || exception is AuthenticationException || exception is OperationCanceledException || exception is ObjectDisposedException || exception is InvalidOperationException || exception is ArgumentException)
                {
                    if (!stop.IsCancellationRequested) state("Connection interrupted. Desktop will reconnect; check the code if pairing was refused.");
                }
            }
        }

        private void Touch() => Interlocked.Exchange(ref lastContact, DateTime.UtcNow.Ticks);

        public static async Task<byte[]> InspectAsync(string host, CancellationToken stop)
        {
            byte[] observed = null;
            await ConnectAsync(host, null, stop, async (tls, token) =>
            {
                observed = TransportIdentity.Hash(tls.RemoteCertificate.GetRawCertData());
                await tls.WriteAsync(new byte[] { 0 }, 0, 1, token).ConfigureAwait(false);
            }).ConfigureAwait(false);
            return observed;
        }

        public static async Task<QuestDevice> DescribeAsync(string host, CancellationToken stop)
        {
            QuestDevice result = null;
            await ConnectAsync(host, null, stop, async (tls, token) =>
            {
                await tls.WriteAsync(new byte[] { 20 }, 0, 1, token).ConfigureAwait(false);
                using var buffer = new MemoryStream();
                byte[] chunk = new byte[512];
                int count;
                while ((count = await tls.ReadAsync(chunk, 0, chunk.Length, token).ConfigureAwait(false)) > 0)
                {
                    if (buffer.Length + count > 512) throw new InvalidDataException("Invalid Quest announcement.");
                    buffer.Write(chunk, 0, count);
                }

                result = QuestDiscovery.Decode(Encoding.UTF8.GetString(buffer.ToArray()), host);
                if (result == null || !TransportIdentity.Equal(result.Pin, TransportIdentity.Hash(tls.RemoteCertificate.GetRawCertData()))) throw new InvalidDataException("Unsupported Quest receiver.");
            }).ConfigureAwait(false);
            return result;
        }

        // Kept for transport clients; new UI persists the credential before starting global installation.
        public static async Task<byte[]> PairAsync(string host, byte[] confirmedPin, string code, CancellationToken stop, Func<Stream, CancellationToken, Task<DeliveryReceipt>> sendGlobals = null)
        {
            byte[] credential = await AuthenticateAsync(host, confirmedPin, code, sendGlobals != null, stop).ConfigureAwait(false);
            if (sendGlobals != null) await ResumeAsync(host, confirmedPin, credential, Guid.NewGuid().ToString("N"), stop, sendGlobals).ConfigureAwait(false);
            return credential;
        }

        public static async Task<byte[]> AuthenticateAsync(string host, byte[] pin, string code, bool globals, CancellationToken stop)
        {
            RequirePin(pin); // Observed pin is bound by PAKE, not trusted from discovery.
            byte[] credential = new byte[32];
            try
            {
                await ConnectAsync(host, pin, stop, async (tls, token) =>
                {
                    await tls.WriteAsync(new[] { (byte)(globals ? 11 : 10) }, 0, 1, token).ConfigureAwait(false);
                    await AcceptedAsync(tls, token).ConfigureAwait(false);
                    await Task.Run(() => PairingPake.AuthenticateAsync(tls, code, TransportIdentity.Hash(tls.RemoteCertificate.GetRawCertData()), false, token), token).ConfigureAwait(false);
                    await PinnedTlsTransfer.ReadExactAsync(tls, credential, 0, credential.Length, token).ConfigureAwait(false);
                }).ConfigureAwait(false);
                return credential;
            }
            catch
            {
                Array.Clear(credential, 0, credential.Length);
                throw;
            }
        }

        public static async Task<bool> PingAsync(string host, byte[] pin, byte[] credential, CancellationToken stop)
        {
            bool ready = false;
            await AuthorizedAsync(host, pin, credential, 12, stop, async (tls, token) => ready = await ReadByteAsync(tls, token).ConfigureAwait(false) == 1, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            return ready;
        }

        public static Task ResumeAsync(string host, byte[] pin, byte[] credential, string contextId, CancellationToken stop, Func<Stream, CancellationToken, Task<DeliveryReceipt>> sendGlobals)
        {
            if (!Guid.TryParseExact(contextId, "N", out _)) throw new ArgumentException("Invalid global snapshot identity.");
            return AuthorizedAsync(host, pin, credential, 13, stop, async (tls, token) =>
            {
                byte[] id = Encoding.ASCII.GetBytes(contextId);
                await tls.WriteAsync(id, 0, id.Length, token).ConfigureAwait(false);
                if (await ReadByteAsync(tls, token).ConfigureAwait(false) == 1) await sendGlobals(tls, token).ConfigureAwait(false);
                await AcceptedAsync(tls, token).ConfigureAwait(false);
            });
        }

        public static async Task<DeliveryReceipt> SendAsync(string host, byte[] pin, byte[] credential, CancellationToken stop, Func<Stream, CancellationToken, Task<DeliveryReceipt>> send)
        {
            DeliveryReceipt receipt = null;
            await AuthorizedAsync(host, pin, credential, 2, stop, async (tls, token) => receipt = await send(tls, token).ConfigureAwait(false)).ConfigureAwait(false);
            return receipt;
        }

        public static Task OpenReplicaAsync(string host, byte[] pin, byte[] credential, CancellationToken stop, Func<Stream, CancellationToken, Task> exchange) => AuthorizedAsync(host, pin, credential, 14, stop, (stream, token) => exchange(stream, token), Timeout.InfiniteTimeSpan);

        public static Task OpenV2ReplicaAsync(string host, byte[] pin, byte[] credential, CancellationToken stop, V2PersistentTransport session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            return OpenReplicaAsync(host, pin, credential, stop, session.RunConnectionAsync);
        }

        private static async Task AuthorizedAsync(string host, byte[] pin, byte[] credential, byte command, CancellationToken stop, Func<SslStream, CancellationToken, Task> action, TimeSpan? timeout = null)
        {
            RequirePin(pin);
            if (credential == null || credential.Length != 32)
                throw new ArgumentException("Pair with the headset first.");
            credential = (byte[])credential.Clone();
            try
            {
                await ConnectAsync(host, pin, stop, async (tls, token) =>
                {
                    {
                        await tls.WriteAsync(new[] { command }, 0, 1, token).ConfigureAwait(false);
                        await tls.WriteAsync(credential, 0, credential.Length, token).ConfigureAwait(false);
                        await AcceptedAsync(tls, token).ConfigureAwait(false);
                    }

                    await action(tls, token).ConfigureAwait(false);
                }, timeout ?? TimeSpan.FromMinutes(30)).ConfigureAwait(false);
            }
            finally
            {
                Array.Clear(credential, 0, credential.Length);
            }
        }

        private static async Task ConnectAsync(string host, byte[] pin, CancellationToken stop, Func<SslStream, CancellationToken, Task> action, TimeSpan? operationTimeout = null)
        {
            // USB forwarding uses a per-device local port; manual UI still accepts IPv4 only.
            string[] parts = (host ?? "").Split(':');
            if (parts.Length > 2 || !IPAddress.TryParse(parts[0], out IPAddress address) || address.AddressFamily != AddressFamily.InterNetwork || address.Equals(IPAddress.Any) || address.Equals(IPAddress.Broadcast)) throw new ArgumentException("Enter the Quest IPv4 address.");
            int port = Port;
            if (parts.Length == 2 && (!IPAddress.IsLoopback(address) || !int.TryParse(parts[1], out port) || port < 1 || port > 65535)) throw new ArgumentException("Enter the Quest IPv4 address, without a port.");
            pin = pin == null ? null : (byte[])pin.Clone();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            using var peer = new TcpClient();
            peer.NoDelay = true;
            using var cancellation = deadline.Token.Register(peer.Close);
            await peer.ConnectAsync(address, port).ConfigureAwait(false);
            using var tls = new SslStream(peer.GetStream(), false, (_, certificate, __, ___) => pin == null || TransportIdentity.Matches(certificate, pin));
            await tls.AuthenticateAsClientAsync("HiBoP-Quest", null, SslProtocols.Tls12, false).ConfigureAwait(false);
            deadline.CancelAfter(operationTimeout ?? TimeSpan.FromSeconds(15));
            await action(tls, deadline.Token).ConfigureAwait(false);
        }

        private static async Task<int> ReadByteAsync(Stream stream, CancellationToken stop)
        {
            var value = new byte[1];
            await PinnedTlsTransfer.ReadExactAsync(stream, value, 0, 1, stop).ConfigureAwait(false);
            return value[0];
        }

        private static Task ReplyAsync(Stream stream, bool accepted, CancellationToken stop) => stream.WriteAsync(new[] { (byte)(accepted ? 1 : 0) }, 0, 1, stop);

        private static async Task AcceptedAsync(Stream stream, CancellationToken stop)
        {
            if (await ReadByteAsync(stream, stop).ConfigureAwait(false) != 1) throw new AuthenticationException("Pairing refused. Check the code; Y on Quest renews pairing.");
        }

        private static void RequirePin(byte[] pin)
        {
            if (pin == null || pin.Length != 32) throw new ArgumentException("Missing headset identity.");
        }

        public static string FormatPin(byte[] pin)
        {
            RequirePin(pin);
            string hex = BitConverter.ToString(pin).Replace("-", "");
            var result = new StringBuilder();
            for (int i = 0; i < hex.Length; i += 8)
            {
                if (i != 0) result.Append(i == 32 ? '\n' : ' ');
                result.Append(hex, i, 8);
            }

            return result.ToString();
        }

        private static byte[] RandomBytes(int length)
        {
            var bytes = new byte[length];
            using var random = RandomNumberGenerator.Create();
            random.GetBytes(bytes);
            return bytes;
        }

        // Cancel and await ServeAsync before disposal.
        public void Dispose()
        {
            identity.Dispose();
            Array.Clear(secret, 0, secret.Length);
        }
    }
}
