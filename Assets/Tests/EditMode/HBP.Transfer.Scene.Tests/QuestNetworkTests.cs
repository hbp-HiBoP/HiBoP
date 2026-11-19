using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Transport;
using Makaretu.Dns;
using NUnit.Framework;

namespace HBP.Tests.Transfer
{
    public sealed class QuestNetworkTests
    {
        [TestCase("127.0.0.1"), TestCase("::1")]
        public async Task CancelledConnectionDoesNotDereferenceTheDisposedTcpClient(string host)
        {
            if (host == "::1" && !Socket.OSSupportsIPv6) Assert.Ignore("IPv6 unavailable on this host.");
            using var stop = new CancellationTokenSource();
            stop.Cancel();
            Exception failure = null;
            try
            {
                await QuestPairing.InspectAsync(host, stop.Token);
            }
            catch (Exception error)
            {
                failure = error;
            }

            Assert.That(failure, Is.InstanceOf<OperationCanceledException>(), failure?.ToString());
            Assert.That(((OperationCanceledException)failure).CancellationToken, Is.EqualTo(stop.Token));
        }

        [TestCase(false), TestCase(true)]
        public async Task CancellationDuringTlsClosesTheSocketAndDrainsTheConnection(bool ipv6)
        {
            if (ipv6 && !Socket.OSSupportsIPv6) Assert.Ignore("IPv6 unavailable on this host.");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
            var listener = new TcpListener(ipv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback, QuestPairing.Port);
            if (ipv6) listener.Server.DualMode = false;
            listener.Start();
            using var cancellation = timeout.Token.Register(listener.Stop);
            Task<byte[]> inspecting = null;
            try
            {
                var accepting = listener.AcceptTcpClientAsync();
                inspecting = QuestPairing.InspectAsync(ipv6 ? "::1" : "127.0.0.1", stop.Token);
                using var peer = await accepting;
                // Receive the TLS ClientHello, but deliberately never send a ServerHello.
                int bytes = await peer.GetStream().ReadAsync(new byte[4096], 0, 4096, timeout.Token);
                Assert.That(bytes, Is.GreaterThan(0));
                stop.Cancel();
                Exception failure = null;
                try
                {
                    await inspecting;
                }
                catch (Exception error)
                {
                    failure = error;
                }

                Assert.That(failure, Is.InstanceOf<OperationCanceledException>(), failure?.ToString());
                Assert.That(((OperationCanceledException)failure).CancellationToken, Is.EqualTo(stop.Token));
                int closed;
                try
                {
                    closed = await peer.GetStream().ReadAsync(new byte[1], 0, 1, timeout.Token);
                }
                catch (System.IO.IOException)
                {
                    closed = 0;
                } // A TCP reset also confirms peer closure.

                Assert.That(closed, Is.Zero, "Cancellation must close the TCP connection, not leave the TLS task running.");
            }
            finally
            {
                stop.Cancel();
                listener.Stop();
                if (inspecting != null)
                    try
                    {
                        await inspecting;
                    }
                    catch (Exception)
                    {
                    }
            }
        }

        private static Message Announcement(string path, TimeSpan ttl)
        {
            var message = new Message { QR = true };
            string instance = "test-" + path + "." + QuestMdnsCache.ServiceType;
            message.Answers.Add(new PTRRecord { Name = QuestMdnsCache.ServiceType, DomainName = instance, TTL = ttl });
            message.AdditionalRecords.Add(new SRVRecord { Name = instance, Target = "test.local", Port = QuestPairing.Port, TTL = ttl });
            var txt = new TXTRecord { Name = instance, TTL = ttl };
            foreach (string value in new[] { "v=1", "id=" + Convert.ToBase64String(new byte[32]), "name=Quest", "path=" + path }) txt.Strings.Add(value);
            message.AdditionalRecords.Add(txt);
            message.AdditionalRecords.Add(new AAAARecord { Name = "test.local", Address = IPAddress.Parse("fe80::1234"), TTL = ttl });
            return message;
        }

        [Test]
        public void LinkLocalNeedsALocalInterfaceAndManualIpUsesFixedPort()
        {
            Assert.Throws<QuestInterfaceRequiredException>(() => QuestEndpoint.Parse("fe80::1234", manual: true));
            Assert.Throws<ArgumentException>(() => QuestEndpoint.Parse("127.0.0.1:1234", manual: true));
            Assert.That(QuestEndpoint.Parse("192.168.1.4", manual: true).Port, Is.EqualTo(45871));
            Assert.That(QuestEndpoint.Parse("::1", manual: true).Address.AddressFamily, Is.EqualTo(AddressFamily.InterNetworkV6));
            Assert.Throws<ArgumentException>(() => QuestEndpoint.Parse("ff02::fb"));
            Assert.Throws<ArgumentException>(() => QuestEndpoint.Parse("0.0.0.0"));
            Assert.Throws<QuestRouteUnavailableException>(() => QuestEndpoint.Parse("fe80::1234%2147483647"));
            Assert.Throws<ArgumentException>(() => QuestEndpoint.Parse("fe80::1234%2147483647", manual: true));
        }

        [Test]
        public void ScopeComesFromReceptionAndGenerationsMakeReusedInterfacesDistinct()
        {
            var cache = new QuestMdnsCache();
            cache.Add(Announcement("usb", TimeSpan.FromSeconds(5)), 17, 0);
            cache.Add(Announcement("usb", TimeSpan.FromSeconds(5)), 22, 0);
            var first = cache.Devices(1, 1);
            Assert.That(first.Count, Is.EqualTo(2));
            Assert.That(first.Select(d => d.Endpoint.Address.ScopeId), Is.EquivalentTo(new long[] { 17, 22 }));
            Assert.That(first.Select(d => d.Id).Distinct().Count(), Is.EqualTo(1));
            Assert.That(cache.Devices(1, 2).Select(d => d.Endpoint.Key), Is.Not.EquivalentTo(first.Select(d => d.Endpoint.Key)));
        }

        [Test]
        public void WithdrawalAndExpiryAffectOnlyTheirInterfaceAndRediscoveryKeepsIdentity()
        {
            var cache = new QuestMdnsCache();
            cache.Add(Announcement("usb", TimeSpan.FromSeconds(5)), 17, 0);
            cache.Add(Announcement("lan", TimeSpan.FromSeconds(20)), 22, 0);
            string identity = cache.Devices(1, 1)[0].Id;
            cache.Add(Announcement("usb", TimeSpan.Zero), 17, 1);
            Assert.That(cache.Devices(3, 1).Count, Is.EqualTo(1));
            Assert.That(cache.Devices(3, 1)[0].Endpoint.Path, Is.EqualTo(QuestNetworkPath.LocalNetwork));
            cache.Add(Announcement("usb", TimeSpan.FromSeconds(5)), 17, 3);
            Assert.That(cache.Devices(4, 1).All(d => d.Id == identity), Is.True);
            Assert.That(cache.Devices(30, 1), Is.Empty);
        }

        [Test]
        public void UnrelatedBonjourRecordsCannotFillTheHibopCache()
        {
            var cache = new QuestMdnsCache();
            for (int packet = 0; packet < 20; packet++)
            {
                var message = new Message { QR = true };
                for (int i = 0; i < 128; i++) message.Answers.Add(new ARecord { Name = "printer" + packet + "-" + i + ".local", Address = IPAddress.Parse("192.168.1.7"), TTL = TimeSpan.FromHours(1) });
                cache.Add(message, 17, 0);
            }

            cache.Add(Announcement("usb", TimeSpan.FromSeconds(30)), 17, 1);
            Assert.That(cache.Devices(2, 1).Count, Is.EqualTo(1));
        }

        [Test]
        public async Task AdmissionLimitIsSharedAcrossIpv4AndIpv6Listeners()
        {
            if (!Socket.OSSupportsIPv6) Assert.Ignore("IPv6 unavailable on this host.");
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var pairing = new QuestPairing();
            var ipv4 = new TcpListener(IPAddress.Loopback, QuestPairing.Port);
            var ipv6 = new TcpListener(IPAddress.IPv6Loopback, QuestPairing.Port);
            ipv6.Server.DualMode = false;
            ipv4.Start();
            ipv6.Start();
            var first = pairing.ServeAsync(ipv4, stop.Token, (_, __) => throw new InvalidOperationException(), _ => { });
            var second = pairing.ServeAsync(ipv6, stop.Token, (_, __) => throw new InvalidOperationException(), _ => { });
            var clients = new System.Collections.Generic.List<TcpClient>();
            try
            {
                for (int i = 0; i < 72; i++)
                {
                    var client = new TcpClient();
                    clients.Add(client);
                    await client.ConnectAsync(IPAddress.Loopback, QuestPairing.Port);
                }

                await Task.Delay(300, stop.Token);
                var inspecting = QuestPairing.InspectAsync("::1", stop.Token);
                await Task.Delay(300, stop.Token);
                Assert.That(inspecting.IsCompleted, Is.False, "A second listener must not create another admission budget.");
                clients[0].Dispose();
                Assert.That(await inspecting, Is.EqualTo(pairing.Pin));
            }
            finally
            {
                stop.Cancel();
                foreach (var client in clients) client.Dispose();
                await Task.WhenAll(first, second);
            }
        }

        [Test]
        public void AddressRecordsCanArriveAfterServiceRecords()
        {
            var cache = new QuestMdnsCache();
            var message = Announcement("usb", TimeSpan.FromSeconds(30));
            var address = message.AdditionalRecords.OfType<AAAARecord>().Single();
            message.AdditionalRecords.Remove(address);
            cache.Add(message, 17, 0);
            Assert.That(cache.Devices(1, 1), Is.Empty);
            Assert.That(cache.Unresolved(1).Select(n => n.ToString().TrimEnd('.')), Does.Contain("test.local"));
            var later = new Message { QR = true };
            later.Answers.Add(address);
            cache.Add(later, 17, 1);
            Assert.That(cache.Devices(2, 1).Count, Is.EqualTo(1));
        }

        [Test]
        public async Task DiscoverySnapshotSurvivesWatcherShutdown()
        {
            var addresses = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.SupportsMulticast && n.NetworkInterfaceType != NetworkInterfaceType.Loopback).SelectMany(n => n.GetIPProperties().UnicastAddresses).Select(a => a.Address).Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToArray();
            if (addresses.Length == 0) Assert.Ignore("No multicast IPv4 interface on this host.");
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var errors = new System.Collections.Concurrent.ConcurrentQueue<string>();
            var finding = Task.Run(() => QuestDiscovery.FindAsync(stop.Token, error: errors.Enqueue), stop.Token);
            byte[] bytes = Announcement("usb", TimeSpan.FromSeconds(30)).ToByteArray();
            for (int attempt = 0; attempt < 4; attempt++)
            {
                await Task.Delay(300, stop.Token);
                foreach (var address in addresses)
                {
                    using var sender = new UdpClient(new IPEndPoint(address, 0));
                    sender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, address.GetAddressBytes());
                    await sender.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353));
                }
            }

            var found = await finding;
            Assert.That(found.Any(device => device.Name == "Quest" && device.Endpoint.Path == QuestNetworkPath.Usb), Is.True, "Closing the discovery window must retain its final result. " + string.Join("; ", errors));
        }

        [Test]
        public async Task Ipv6TlsAndIdentityMismatchDoNotBecomeCredentialRejection()
        {
            if (!Socket.OSSupportsIPv6) Assert.Ignore("IPv6 is unavailable on this host.");
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var pairing = new QuestPairing();
            var listener = new TcpListener(IPAddress.IPv6Loopback, QuestPairing.Port);
            listener.Server.DualMode = false;
            listener.Start();
            var serving = pairing.ServeAsync(listener, stop.Token, (_, __) => throw new InvalidOperationException(), _ => { });
            try
            {
                byte[] observed = await QuestPairing.InspectAsync("::1", stop.Token);
                Assert.That(observed, Is.EqualTo(pairing.Pin));
                var wrong = (byte[])pairing.Pin.Clone();
                wrong[0] ^= 1;
                Exception failure = null;
                try
                {
                    await QuestPairing.PingAsync("::1", wrong, new byte[32], stop.Token);
                }
                catch (Exception error)
                {
                    failure = error;
                }

                Assert.That(failure, Is.TypeOf<QuestIdentityMismatchException>());
                Assert.That(pairing.GetStatus().AttemptsRemaining, Is.EqualTo(5));
            }
            finally
            {
                stop.Cancel();
                await serving;
            }
        }
    }
}
