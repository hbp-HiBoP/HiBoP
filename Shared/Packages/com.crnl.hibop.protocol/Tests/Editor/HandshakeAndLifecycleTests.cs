using System;
using CRNL.HiBoP.Contracts;
using NUnit.Framework;

namespace CRNL.HiBoP.Protocol.Tests
{
    public class HandshakeAndLifecycleTests
    {
        [Test]
        public void HandshakeNegotiatesMinorAndCapabilities()
        {
            ProtocolVersion hostVersion = new(1, 3);
            ProtocolVersion clientVersion = new(1, 1);
            HandshakePolicy policy = SessionTestFixture.Policy(hostVersion);

            ServerHello result = HandshakeNegotiator.Negotiate(SessionTestFixture.Hello(clientVersion), policy, SessionTestFixture.Snapshot().Session);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Protocol, Is.EqualTo(clientVersion));
            Assert.That(result.SelectedSchemaHash.Value, Is.EqualTo(SessionTestFixture.Schema));
            Assert.That(result.Capabilities, Is.EqualTo(SessionTestFixture.AllCapabilities));
        }

        [TestCase(2, 0, CompatibilityDecision.ProtocolIncompatible)]
        [TestCase(1, 0, CompatibilityDecision.SchemaIncompatible)]
        public void HandshakeRejectsMajorOrSchema(int major, int minor, CompatibilityDecision expected)
        {
            ClientHello hello = expected == CompatibilityDecision.SchemaIncompatible ? SessionTestFixture.Hello(new ProtocolVersion((ushort)major, (ushort)minor), new AssetHash(9, 9, 9, 9)) : SessionTestFixture.Hello(new ProtocolVersion((ushort)major, (ushort)minor));

            ServerHello result = HandshakeNegotiator.Negotiate(hello, SessionTestFixture.Policy(), SessionTestFixture.Snapshot().Session);

            Assert.That(result.Decision, Is.EqualTo(expected));
            Assert.That(result.Accepted, Is.False);
        }

        [Test]
        public void HandshakeRejectsMissingRequiredCapability()
        {
            ProtocolCapabilities incomplete = SessionTestFixture.AllCapabilities & ~ProtocolCapabilities.Resume;
            ServerHello result = HandshakeNegotiator.Negotiate(SessionTestFixture.Hello(capabilities: incomplete), SessionTestFixture.Policy(), SessionTestFixture.Snapshot().Session);

            Assert.That(result.Decision, Is.EqualTo(CompatibilityDecision.CapabilitiesIncompatible));
        }

        [Test]
        public void PairingRequiresTlsIdentityAndRateLimitsAttempts()
        {
            ManualClock clock = new();
            PairingCoordinator pairing = new(SessionTestFixture.Sas, clock, SessionTestFixture.TokenFactory);

            Assert.That(pairing.TryPair(SessionTestFixture.Sas, false).Error.Value, Is.EqualTo(ErrorCode.AuthFailed));
            for (int index = 1; index < PairingCoordinator.MaximumAttemptsPerMinute; index++)
                Assert.That(pairing.TryPair("000000", true).Accepted, Is.False);

            Assert.That(pairing.TryPair(SessionTestFixture.Sas, true).Error.Value, Is.EqualTo(ErrorCode.RateLimited));
            clock.Advance(60_000);
            PairingResult accepted = pairing.TryPair(SessionTestFixture.Sas, true);
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(pairing.IsAuthorized(accepted.Token.Value), Is.True);
            Assert.That(accepted.Token.Value.ToString(), Does.Not.Contain(SessionTestFixture.Sas));
        }

        [Test]
        public void SecondClientCannotStealLease()
        {
            SyntheticSessionHost host = SessionTestFixture.Host();
            _ = SessionTestFixture.ConnectedClient(host);
            SyntheticSessionClient second = new(host, new ContractId(11, 11), SessionTestFixture.Hello(nonce: new ContractId(201, 1)));

            ServerHello hello = second.PairAndConnect(SessionTestFixture.Sas);

            Assert.That(hello, Is.Null);
            Assert.That(second.State, Is.EqualTo(ClientSessionState.Refused));
            Assert.That(second.UserMessage, Is.EqualTo(SyntheticSessionClient.BusyMessage));
            Assert.That(host.State, Is.EqualTo(HostSessionState.Active));
        }

        [Test]
        public void StateMachinesRejectIllegalTransitions()
        {
            HostSessionStateMachine host = new();
            ClientSessionStateMachine client = new();

            Assert.Throws<InvalidOperationException>(() => host.Activate());
            Assert.Throws<InvalidOperationException>(() => client.Connected());

            host.Start();
            host.Pair();
            host.AcceptHello();
            host.Activate();
            host.Suspend();
            host.BeginResume();
            host.Activate();
            Assert.That(host.State, Is.EqualTo(HostSessionState.Active));

            client.BeginPairing();
            client.PairingAccepted();
            client.Connected();
            client.BeginSynchronization();
            client.Activate();
            client.ConnectionLost();
            client.RetryConnecting();
            client.BeginSynchronization();
            client.Activate();
            Assert.That(client.State, Is.EqualTo(ClientSessionState.Active));
        }

        [Test]
        public void RetryPolicyIsBoundedAndJittered()
        {
            ReconnectPolicy policy = new();

            Assert.That(policy.GetDelayMilliseconds(0, 0), Is.Zero);
            Assert.That(policy.GetDelayMilliseconds(0, 1), Is.EqualTo(250));
            Assert.That(policy.GetDelayMilliseconds(20, 1), Is.EqualTo(4000));
            Assert.Throws<ArgumentOutOfRangeException>(() => policy.GetDelayMilliseconds(-1, 0.5));
            Assert.Throws<ArgumentOutOfRangeException>(() => policy.GetDelayMilliseconds(0, 1.1));
        }

        [Test]
        public void HeartbeatUsesAnyReceivedTrafficAndThreeSecondTimeout()
        {
            ManualClock clock = new();
            HeartbeatMonitor heartbeat = new(clock);

            clock.Advance(999);
            Assert.That(heartbeat.ShouldSend, Is.False);
            Assert.That(heartbeat.IsTimedOut, Is.False);
            clock.Advance(1);
            Assert.That(heartbeat.ShouldSend, Is.True);
            heartbeat.MarkSent();
            clock.Advance(1_999);
            heartbeat.MarkReceived();
            Assert.That(heartbeat.IsTimedOut, Is.False);
            clock.Advance(2_999);
            Assert.That(heartbeat.IsTimedOut, Is.False);
            clock.Advance(1);
            Assert.That(heartbeat.IsTimedOut, Is.True);
        }
    }
}
