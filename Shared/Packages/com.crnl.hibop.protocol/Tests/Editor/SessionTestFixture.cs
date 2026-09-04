using System;
using System.Collections.Generic;
using CRNL.HiBoP.Contracts;

namespace CRNL.HiBoP.Protocol.Tests
{
    internal sealed class ManualClock : IMonotonicClock
    {
        public long Milliseconds { get; private set; }

        public void Advance(long milliseconds)
        {
            if (milliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(milliseconds));
            Milliseconds = checked(Milliseconds + milliseconds);
        }
    }

    internal static class SessionTestFixture
    {
        public const string Sas = "123456";
        public static readonly AssetHash Schema = new(1, 2, 3, 4);
        public static readonly ContractId ClientId = new(10, 10);
        public static readonly ScopeKey ColumnScope = new(ScopeType.Column, new ContractId(20, 20));
        public static readonly ScopeKey TimelineScope = new(ScopeType.Timeline, new ContractId(30, 30));
        public const ProtocolCapabilities AllCapabilities = ProtocolCapabilities.TransactionalSnapshot | ProtocolCapabilities.OrderedDeltas | ProtocolCapabilities.SequencedCommands | ProtocolCapabilities.Resume | ProtocolCapabilities.RedactedDiagnostics;

        public static SessionSnapshot Snapshot(ulong epoch = 1, ulong stateRevision = 1, double opacity = 0.25, bool playing = false)
        {
            SessionEpoch session = new(new ContractId(1, epoch), epoch);
            ScopeState column = new(ColumnScope, new ScopeRevision(stateRevision), new[]
            {
                new StateProperty(V1PropertyKeys.ColumnActivityOpacity, ContractValue.FromNumber(opacity)),
            });
            ScopeState timeline = new(TimelineScope, new ScopeRevision(stateRevision), new[]
            {
                new StateProperty(V1PropertyKeys.TimelinePlaybackState, ContractValue.FromBoolean(playing)),
            });
            return new SessionSnapshot(ContractVersion.V1, session, new StateRevision(stateRevision), new[] { column, timeline }, Array.Empty<AssetReference>());
        }

        public static HandshakePolicy Policy(ProtocolVersion? version = null, AssetHash? schema = null, ProtocolCapabilities capabilities = AllCapabilities, ProtocolCapabilities requiredCapabilities = AllCapabilities)
        {
            int nonce = 0;
            return new HandshakePolicy(version ?? ProtocolVersion.V1, new[] { schema ?? Schema }, Build("desktop"), capabilities, requiredCapabilities, () => new ContractId(100, (ulong)++nonce));
        }

        public static ClientHello Hello(ProtocolVersion? version = null, AssetHash? schema = null, ProtocolCapabilities capabilities = AllCapabilities, ContractId? nonce = null)
        {
            return new ClientHello(version ?? ProtocolVersion.V1, new[] { schema ?? Schema }, Build("xr"), capabilities, DeviceClass.Quest, nonce ?? new ContractId(200, 1));
        }

        public static SyntheticSessionHost Host(ManualClock clock = null, SessionSnapshot snapshot = null)
        {
            clock ??= new ManualClock();
            return new SyntheticSessionHost(snapshot ?? Snapshot(), Policy(), Sas, clock, TokenFactory);
        }

        public static SyntheticSessionClient ConnectedClient(SyntheticSessionHost host, ContractId? clientId = null)
        {
            SyntheticSessionClient client = new(host, clientId ?? ClientId, Hello());
            ServerHello hello = client.PairAndConnect(Sas);
            if (hello == null || !hello.Accepted)
                throw new InvalidOperationException("The test client could not connect.");
            return client;
        }

        public static Command OpacityCommand(SessionSnapshot basis, ContractId commandId, ContractId correlationId, double opacity)
        {
            ScopeState scope = FindScope(basis, ColumnScope);
            return new Command(basis.Session, commandId, correlationId, ColumnScope, scope.Revision, CommandKind.SetOpacity, ContractValue.FromNumber(opacity));
        }

        public static ScopeState FindScope(SessionSnapshot snapshot, ScopeKey key)
        {
            for (int index = 0; index < snapshot.Scopes.Count; index++)
            {
                if (snapshot.Scopes[index].Scope == key)
                    return snapshot.Scopes[index];
            }

            throw new InvalidOperationException("Scope not found.");
        }

        public static double Opacity(SessionSnapshot snapshot)
        {
            ScopeState scope = FindScope(snapshot, ColumnScope);
            for (int index = 0; index < scope.Properties.Count; index++)
            {
                if (scope.Properties[index].Key == V1PropertyKeys.ColumnActivityOpacity)
                    return scope.Properties[index].Value.Number;
            }

            throw new InvalidOperationException("Opacity not found.");
        }

        public static byte[] TokenFactory()
        {
            byte[] token = new byte[PairingToken.ByteLength];
            for (int index = 0; index < token.Length; index++)
                token[index] = (byte)(index + 1);
            return token;
        }

        private static BuildIdentity Build(string name) => new("1.0.0-" + name, "abcdef", "hbp-core-1");
    }
}
