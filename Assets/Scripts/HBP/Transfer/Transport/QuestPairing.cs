using System;
using System.Diagnostics;
using System.IO;
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
    /// <summary>One in-memory pairing. Inspect is unauthenticated and sends no credential;
    /// the user must compare the full fingerprint before calling PairAsync.</summary>
    public sealed class QuestPairing : IDisposable
    {
        public const int Port = 45871;
        private readonly X509Certificate2 identity;
        private readonly byte[] secret = RandomBytes(32);
        private readonly Stopwatch age = Stopwatch.StartNew();
        private int attempts;
        private int paired;
        private int ownerClaimed;
        private int preparing;
        public string Code { get; }
        public string Fingerprint => FormatPin(TransportIdentity.Hash(identity.RawData));
        public bool IsPaired => Volatile.Read(ref paired) != 0;
        public bool IsPreparing => Volatile.Read(ref preparing) != 0;
        public bool IsLocked => (Volatile.Read(ref ownerClaimed) != 0 && !IsPaired) || Volatile.Read(ref attempts) >= 5 || age.Elapsed >= TimeSpan.FromMinutes(5);

        public QuestPairing()
        {
            identity = TransportIdentity.Create();
            // Rejection sampling avoids modulo bias in the six-digit code.
            uint value;
            do
            {
                value = BitConverter.ToUInt32(RandomBytes(4), 0);
            } while (value >= 4294000000u);

            Code = (value % 1000000).ToString("D6");
        }

        public async Task ServeAsync(TcpListener listener, CancellationToken stop, Func<Stream, CancellationToken, Task<DeliveryReceipt>> receive, Action<string> state, Func<Stream, CancellationToken, Task<DeliveryReceipt>> receiveGlobals = null)
        {
            using var cancelAccept = stop.Register(listener.Stop);
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

                    using (peer)
                    using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop))
                    using (deadline.Token.Register(peer.Close))
                    {
                        deadline.CancelAfter(TimeSpan.FromSeconds(30));
                        try
                        {
                            using var tls = new SslStream(peer.GetStream(), false);
                            await tls.AuthenticateAsServerAsync(identity, false, SslProtocols.Tls12, false).ConfigureAwait(false);
                            var command = new byte[1];
                            await PinnedTlsTransfer.ReadExactAsync(tls, command, 0, 1, deadline.Token).ConfigureAwait(false);
                            if (command[0] == 0) continue; // Certificate inspection only.
                            if (command[0] == 1 || command[0] == 3)
                            {
                                bool allowed = !IsPaired && !IsLocked && ((command[0] == 3) == (receiveGlobals != null));
                                if (allowed) Interlocked.Increment(ref attempts);
                                var code = new byte[6];
                                await PinnedTlsTransfer.ReadExactAsync(tls, code, 0, code.Length, deadline.Token).ConfigureAwait(false);
                                allowed &= age.Elapsed < TimeSpan.FromMinutes(5) && TransportIdentity.Equal(code, Encoding.ASCII.GetBytes(Code));
                                Array.Clear(code, 0, code.Length);
                                await tls.WriteAsync(new[] { (byte)(allowed ? 1 : 0) }, 0, 1, deadline.Token).ConfigureAwait(false);
                                if (!allowed)
                                {
                                    state(IsLocked ? "Pairing locked. Press Y to restart." : "Pairing refused. Check the code.");
                                    continue;
                                }

                                // Commit before replying: an interrupted reply must not permit another owner.
                                Interlocked.Exchange(ref ownerClaimed, 1);
                                await tls.WriteAsync(secret, 0, secret.Length, deadline.Token).ConfigureAwait(false);
                                if (command[0] == 3)
                                {
                                    state("Receiving global preferences, protocols and tags...");
                                    deadline.CancelAfter(TimeSpan.FromMinutes(30));
                                    Interlocked.Exchange(ref preparing, 1);
                                    try
                                    {
                                        await receiveGlobals(tls, deadline.Token).ConfigureAwait(false);
                                    }
                                    finally
                                    {
                                        Interlocked.Exchange(ref preparing, 0);
                                    }
                                }

                                Interlocked.Exchange(ref paired, 1);
                                state("Paired. Ready to receive.");
                            }
                            else if (command[0] == 2)
                            {
                                var offered = new byte[32];
                                await PinnedTlsTransfer.ReadExactAsync(tls, offered, 0, offered.Length, deadline.Token).ConfigureAwait(false);
                                bool allowed = IsPaired && TransportIdentity.Equal(offered, secret);
                                Array.Clear(offered, 0, offered.Length);
                                await tls.WriteAsync(new[] { (byte)(allowed ? 1 : 0) }, 0, 1, deadline.Token).ConfigureAwait(false);
                                if (!allowed) continue;
                                deadline.CancelAfter(TimeSpan.FromMinutes(30));
                                DeliveryReceipt receipt = await receive(tls, deadline.Token).ConfigureAwait(false);
                                state(receipt.Status == DeliveryStatus.Published || receipt.Status == DeliveryStatus.AlreadyPublished ? "Ready offline. You can explore all visualization columns." : "Old delivery closed/replaced. Send a new snapshot from Desktop.");
                            }
                            else throw new InvalidDataException("Unknown pairing command.");
                        }
                        catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is SocketException || exception is AuthenticationException || exception is OperationCanceledException || exception is ObjectDisposedException || exception is InvalidOperationException)
                        {
                            if (!stop.IsCancellationRequested) state("Connection interrupted. Retry from Desktop; press Y to pair again if global setup was interrupted.");
                        }
                    }
                }
            }
            finally
            {
                listener.Stop();
            }
        }

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

        public static async Task<byte[]> PairAsync(string host, byte[] confirmedPin, string code, CancellationToken stop, Func<Stream, CancellationToken, Task<DeliveryReceipt>> sendGlobals = null)
        {
            RequirePin(confirmedPin);
            if (code == null || code.Length != 6 || Array.Exists(code.ToCharArray(), c => c < '0' || c > '9')) throw new ArgumentException("Enter the six-digit code shown in the headset.");
            byte[] credential = new byte[32];
            try
            {
                await ConnectAsync(host, confirmedPin, stop, async (tls, token) =>
                {
                    byte[] request = Encoding.ASCII.GetBytes((sendGlobals == null ? "\u0001" : "\u0003") + code);
                    try
                    {
                        await tls.WriteAsync(request, 0, request.Length, token).ConfigureAwait(false);
                    }
                    finally
                    {
                        Array.Clear(request, 0, request.Length);
                    }

                    await AcceptedAsync(tls, token).ConfigureAwait(false);
                    await PinnedTlsTransfer.ReadExactAsync(tls, credential, 0, credential.Length, token).ConfigureAwait(false);
                    if (sendGlobals != null) await sendGlobals(tls, token).ConfigureAwait(false);
                }, sendGlobals == null ? null : TimeSpan.FromMinutes(30)).ConfigureAwait(false);
                return credential;
            }
            catch
            {
                Array.Clear(credential, 0, credential.Length);
                throw;
            }
        }

        public static async Task<DeliveryReceipt> SendAsync(string host, byte[] pin, byte[] credential, CancellationToken stop, Func<Stream, CancellationToken, Task<DeliveryReceipt>> send)
        {
            RequirePin(pin);
            if (credential == null || credential.Length != 32) throw new ArgumentException("Pair with the headset first.");
            credential = (byte[])credential.Clone();
            DeliveryReceipt receipt = null;
            try
            {
                await ConnectAsync(host, pin, stop, async (tls, token) =>
                {
                    await tls.WriteAsync(new byte[] { 2 }, 0, 1, token).ConfigureAwait(false);
                    await tls.WriteAsync(credential, 0, credential.Length, token).ConfigureAwait(false);
                    await AcceptedAsync(tls, token).ConfigureAwait(false);
                    receipt = await send(tls, token).ConfigureAwait(false);
                }, TimeSpan.FromMinutes(30)).ConfigureAwait(false);
                return receipt;
            }
            finally
            {
                Array.Clear(credential, 0, credential.Length);
            }
        }

        private static async Task ConnectAsync(string host, byte[] pin, CancellationToken stop, Func<SslStream, CancellationToken, Task> action, TimeSpan? operationTimeout = null)
        {
            if (!IPAddress.TryParse(host, out IPAddress address) || address.AddressFamily != AddressFamily.InterNetwork || address.Equals(IPAddress.Any) || address.Equals(IPAddress.Broadcast)) throw new ArgumentException("Enter the Quest IPv4 address, without a port.");
            pin = pin == null ? null : (byte[])pin.Clone();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            using var peer = new TcpClient();
            using var cancellation = deadline.Token.Register(peer.Close);
            await peer.ConnectAsync(address, Port).ConfigureAwait(false);
            using var tls = new SslStream(peer.GetStream(), false, (_, certificate, __, ___) => pin == null || TransportIdentity.Matches(certificate, pin));
            await tls.AuthenticateAsClientAsync("HiBoP-Quest", null, SslProtocols.Tls12, false).ConfigureAwait(false);
            if (operationTimeout.HasValue) deadline.CancelAfter(operationTimeout.Value);
            await action(tls, deadline.Token).ConfigureAwait(false);
        }

        private static async Task AcceptedAsync(Stream stream, CancellationToken stop)
        {
            var status = new byte[1];
            await PinnedTlsTransfer.ReadExactAsync(stream, status, 0, 1, stop).ConfigureAwait(false);
            if (status[0] != 1) throw new AuthenticationException("Pairing refused. Check the code, or press Y in the headset to pair again.");
        }

        private static void RequirePin(byte[] pin)
        {
            if (pin == null || pin.Length != 32) throw new ArgumentException("Compare and confirm the full headset fingerprint first.");
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

        // The owner must first cancel and await ServeAsync.
        public void Dispose()
        {
            identity.Dispose();
            Array.Clear(secret, 0, secret.Length);
        }
    }
}
