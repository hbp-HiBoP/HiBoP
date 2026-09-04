using System;
using System.Collections.Generic;
using CRNL.HiBoP.Contracts;

namespace CRNL.HiBoP.Protocol
{
    public sealed class SyntheticSessionHost
    {
        public const long ResumeLeaseMilliseconds = 30_000;
        private readonly object m_Gate = new();
        private readonly HandshakePolicy m_HandshakePolicy;
        private readonly IMonotonicClock m_Clock;
        private readonly SessionDiagnostics m_Diagnostics;
        private HostData m_Data;
        private ProtocolCapabilities m_EffectiveCapabilities;
        private ContractId m_LeaseOwner;
        private long m_LeaseExpiresAt;
        private PairingCoordinator m_Pairing;
        private HostSessionStateMachine m_StateMachine;

        public SyntheticSessionHost(SessionSnapshot initialSnapshot, HandshakePolicy handshakePolicy, string sas, IMonotonicClock clock, Func<byte[]> tokenFactory)
        {
            if (initialSnapshot == null)
                throw new ArgumentNullException(nameof(initialSnapshot));
            m_HandshakePolicy = handshakePolicy ?? throw new ArgumentNullException(nameof(handshakePolicy));
            m_Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            m_Diagnostics = new SessionDiagnostics(clock);
            m_Pairing = new PairingCoordinator(sas, clock, tokenFactory);
            m_StateMachine = new HostSessionStateMachine();
            m_StateMachine.Start();
            m_Data = new HostData(initialSnapshot, new DeltaJournal(), new IdempotenceLedger());
            m_Diagnostics.Record(DiagnosticEventCode.StateChanged);
        }

        public HostSessionState State
        {
            get
            {
                lock (m_Gate)
                    return m_StateMachine.State;
            }
        }

        public SessionSnapshot CurrentSnapshot
        {
            get
            {
                lock (m_Gate)
                    return m_Data.Snapshot;
            }
        }

        public SessionDiagnostics Diagnostics => m_Diagnostics;

        public long AppliedCommandCount { get; private set; }

        public SessionDiagnosticSummary GetDiagnosticSummary()
        {
            lock (m_Gate)
            {
                return new SessionDiagnosticSummary("host", m_StateMachine.State.ToString(), m_Data.Snapshot.Session, m_Data.Snapshot.StateRevision, m_Data.Journal.Count, m_Data.Journal.EvictionCount, m_Data.Idempotence.Count, m_Data.Idempotence.HighWaterMark, AppliedCommandCount);
            }
        }

        public PairingResult Pair(ContractId clientId, string suppliedSas, bool transportIdentityVerified)
        {
            if (!clientId.IsValid)
                throw new ArgumentException("A valid client identifier is required.", nameof(clientId));

            lock (m_Gate)
            {
                ExpireLeaseIfRequired();
                if (m_LeaseOwner.IsValid)
                {
                    m_Diagnostics.Record(DiagnosticEventCode.SessionBusy, error: Optional<ErrorCode>.Some(ErrorCode.SessionBusy));
                    return PairingResult.Reject(ErrorCode.SessionBusy);
                }

                if (m_StateMachine.State != HostSessionState.Pairing)
                    return PairingResult.Reject(ErrorCode.SessionBusy);

                PairingResult result = m_Pairing.TryPair(suppliedSas, transportIdentityVerified);
                if (!result.Accepted)
                {
                    m_Diagnostics.Record(DiagnosticEventCode.PairingRejected, error: result.Error);
                    return result;
                }

                m_LeaseOwner = clientId;
                m_StateMachine.Pair();
                m_Diagnostics.Record(DiagnosticEventCode.PairingAccepted);
                return result;
            }
        }

        public ServerHello Handshake(ContractId clientId, PairingToken token, ClientHello hello)
        {
            lock (m_Gate)
            {
                if (!OwnsLease(clientId))
                    return RejectedHello(hello, CompatibilityDecision.SessionBusy);
                if (!m_Pairing.IsAuthorized(token))
                    return RejectedHello(hello, CompatibilityDecision.AuthFailed);
                if (m_StateMachine.State != HostSessionState.AwaitingHello)
                    throw new InvalidOperationException("The host is not awaiting a hello.");

                ServerHello result = HandshakeNegotiator.Negotiate(hello, m_HandshakePolicy, m_Data.Snapshot.Session);
                if (!result.Accepted)
                {
                    m_Diagnostics.Record(DiagnosticEventCode.HandshakeRejected);
                    ReleaseLease();
                    return result;
                }

                m_EffectiveCapabilities = result.Capabilities;
                m_StateMachine.AcceptHello();
                m_Diagnostics.Record(DiagnosticEventCode.HandshakeAccepted);
                return result;
            }
        }

        public SnapshotEnvelope CaptureSnapshot(ContractId clientId, PairingToken token)
        {
            lock (m_Gate)
            {
                EnsureAuthorized(clientId, token);
                if (m_StateMachine.State != HostSessionState.Synchronizing)
                    throw new InvalidOperationException("The host is not synchronizing.");
                int bytes = EstimateSnapshotBytes(m_Data.Snapshot);
                return new SnapshotEnvelope(m_Data.Snapshot, m_EffectiveCapabilities, bytes);
            }
        }

        public void AcknowledgeSynchronization(ContractId clientId, PairingToken token, StateRevision revision)
        {
            lock (m_Gate)
            {
                EnsureAuthorized(clientId, token);
                if (m_StateMachine.State != HostSessionState.Synchronizing || revision != m_Data.Snapshot.StateRevision)
                    throw new InvalidOperationException("The acknowledged revision is not current.");
                m_StateMachine.Activate();
                m_Diagnostics.Record(DiagnosticEventCode.SnapshotCommitted);
            }
        }

        public CommandExecutionResult Execute(ContractId clientId, PairingToken token, SequencedCommand request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            lock (m_Gate)
            {
                EnsureAuthorized(clientId, token);
                if (m_StateMachine.State != HostSessionState.Active)
                    throw new InvalidOperationException("Commands are accepted only while the session is active.");

                long now = m_Clock.Milliseconds;
                IdempotenceLookup lookup = m_Data.Idempotence.Lookup(request, now);
                if (lookup.Disposition == IdempotenceDisposition.Replay)
                {
                    m_Diagnostics.Record(DiagnosticEventCode.CommandReplayed, Optional<ContractId>.Some(request.Command.CorrelationId));
                    return new CommandExecutionResult(lookup.Outcome.Value, Optional<StateDelta>.None, true);
                }

                if (lookup.Disposition != IdempotenceDisposition.Execute)
                {
                    bool retryable = lookup.Disposition == IdempotenceDisposition.Gap;
                    CommandOutcome rejected = CommandOutcome.Reject(request.Command.CommandId, new ContractError(ErrorCode.CommandInvalid, request.Command.CorrelationId, retryable));
                    m_Diagnostics.Record(DiagnosticEventCode.CommandRejected, Optional<ContractId>.Some(request.Command.CorrelationId), Optional<ErrorCode>.Some(ErrorCode.CommandInvalid));
                    return new CommandExecutionResult(rejected, Optional<StateDelta>.None, false);
                }

                Optional<CommandOutcome> prior = Optional<CommandOutcome>.None;
                Optional<ScopeRevision> scopeRevision = FindScopeRevision(m_Data.Snapshot, request.Command.Scope);
                CommandGateResult gate = CommandGate.Evaluate(request.Command, m_Data.Snapshot.Session, m_Data.Snapshot.StateRevision, prior, scopeRevision);
                if (gate.Disposition == CommandGateDisposition.ReturnOutcome)
                    return CommitOutcomeOnly(request, gate.Outcome.Value, now);

                if (!TryBuildDelta(m_Data.Snapshot, request.Command, out StateDelta delta))
                {
                    CommandOutcome invalid = CommandOutcome.Reject(request.Command.CommandId, new ContractError(ErrorCode.CommandInvalid, request.Command.CorrelationId, false));
                    return CommitOutcomeOnly(request, invalid, now);
                }

                SessionSnapshot candidate = SessionStateReducer.Apply(m_Data.Snapshot, delta);
                ScopeRevision resultingScope = delta.Scopes[0].ResultingRevision;
                CommandOutcome accepted = CommandOutcome.Accept(request.Command.CommandId, candidate.StateRevision, resultingScope, Optional<ContractValue>.Some(request.Command.Payload));

                IdempotenceLedger ledger = m_Data.Idempotence.Clone();
                ledger.Record(request, accepted, now);
                DeltaJournal journal = m_Data.Journal.Clone();
                journal.Add(delta, EstimateDeltaBytes(delta), now);
                m_Data = new HostData(candidate, journal, ledger);
                AppliedCommandCount++;
                m_Diagnostics.Record(DiagnosticEventCode.CommandApplied, Optional<ContractId>.Some(request.Command.CorrelationId));
                m_Diagnostics.Record(DiagnosticEventCode.DeltaCommitted, Optional<ContractId>.Some(request.Command.CorrelationId));
                return new CommandExecutionResult(accepted, Optional<StateDelta>.Some(delta), false);
            }
        }

        public void Suspend(ContractId clientId)
        {
            lock (m_Gate)
            {
                if (!OwnsLease(clientId))
                    return;
                m_StateMachine.Suspend();
                m_LeaseExpiresAt = checked(m_Clock.Milliseconds + ResumeLeaseMilliseconds);
                m_Diagnostics.Record(DiagnosticEventCode.HeartbeatTimeout);
            }
        }

        public StateDelta ApplyAuthoritativeChange(ScopeKey scope, PropertyKey key, ContractValue value)
        {
            if (!scope.IsValid)
                throw new ArgumentException("A valid scope is required.", nameof(scope));
            if (!key.IsValid)
                throw new ArgumentException("A valid property key is required.", nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            lock (m_Gate)
            {
                if (m_StateMachine.State != HostSessionState.Active && m_StateMachine.State != HostSessionState.Suspended)
                    throw new InvalidOperationException("Authoritative changes require an active or suspended session.");
                Optional<ScopeRevision> revision = FindScopeRevision(m_Data.Snapshot, scope);
                if (!revision.HasValue)
                    throw new ArgumentException("The scope does not exist.", nameof(scope));

                ScopeDelta scopeDelta = new(scope, revision.Value, revision.Value.Next(), new[] { PropertyChange.Set(key, value) });
                StateDelta delta = new(m_Data.Snapshot.Session, m_Data.Snapshot.StateRevision, m_Data.Snapshot.StateRevision.Next(), new[] { scopeDelta }, Array.Empty<AssetChange>());
                SessionSnapshot candidate = SessionStateReducer.Apply(m_Data.Snapshot, delta);
                DeltaJournal journal = m_Data.Journal.Clone();
                journal.Add(delta, EstimateDeltaBytes(delta), m_Clock.Milliseconds);
                m_Data = new HostData(candidate, journal, m_Data.Idempotence);
                m_Diagnostics.Record(DiagnosticEventCode.DeltaCommitted);
                return delta;
            }
        }

        public ResumeResponse Resume(ContractId clientId, PairingToken token, ResumeRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            lock (m_Gate)
            {
                ExpireLeaseIfRequired();
                EnsureAuthorized(clientId, token);
                if (m_StateMachine.State != HostSessionState.Suspended)
                    throw new InvalidOperationException("The host has no suspended lease to resume.");
                m_StateMachine.BeginResume();

                if (request.Session != m_Data.Snapshot.Session)
                {
                    m_Diagnostics.Record(DiagnosticEventCode.NewSession);
                    return ResumeResponse.NewSession(m_Data.Snapshot);
                }

                if (m_Data.Journal.TryGetSince(request.StateRevision, m_Data.Snapshot.StateRevision, m_Clock.Milliseconds, out IReadOnlyList<StateDelta> deltas) && ResumeMetadataMatches(request, m_Data.Snapshot, deltas))
                {
                    m_Diagnostics.Record(DiagnosticEventCode.ResumeWithDeltas);
                    return ResumeResponse.WithDeltas(m_Data.Snapshot.Session, deltas);
                }

                m_Diagnostics.Record(DiagnosticEventCode.FullSnapshotRequired);
                return ResumeResponse.FullSnapshot(m_Data.Snapshot);
            }
        }

        public void ReplaceSession(SessionSnapshot replacement, string sas, Func<byte[]> tokenFactory)
        {
            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            lock (m_Gate)
            {
                if (replacement.Session == m_Data.Snapshot.Session)
                    throw new ArgumentException("A replacement must use a new session epoch.", nameof(replacement));
                m_StateMachine.Replace();
                m_Pairing.Revoke();
                m_LeaseOwner = default;
                m_LeaseExpiresAt = 0;
                m_EffectiveCapabilities = ProtocolCapabilities.None;
                m_Data = new HostData(replacement, new DeltaJournal(), new IdempotenceLedger());
                m_Pairing = new PairingCoordinator(sas, m_Clock, tokenFactory);
                m_StateMachine = new HostSessionStateMachine();
                m_StateMachine.Start();
                m_Diagnostics.Record(DiagnosticEventCode.SessionReplaced);
            }
        }

        public void Close()
        {
            lock (m_Gate)
            {
                m_Pairing.Revoke();
                m_LeaseOwner = default;
                m_Data = new HostData(m_Data.Snapshot, new DeltaJournal(), new IdempotenceLedger());
                m_StateMachine.Close();
                m_Diagnostics.Record(DiagnosticEventCode.StateChanged);
            }
        }

        private CommandExecutionResult CommitOutcomeOnly(SequencedCommand request, CommandOutcome outcome, long now)
        {
            IdempotenceLedger ledger = m_Data.Idempotence.Clone();
            ledger.Record(request, outcome, now);
            m_Data = new HostData(m_Data.Snapshot, m_Data.Journal, ledger);
            ErrorCode error = outcome.Error.HasValue ? outcome.Error.Value.Code : ErrorCode.Unknown;
            m_Diagnostics.Record(DiagnosticEventCode.CommandRejected, Optional<ContractId>.Some(request.Command.CorrelationId), error == ErrorCode.Unknown ? Optional<ErrorCode>.None : Optional<ErrorCode>.Some(error));
            return new CommandExecutionResult(outcome, Optional<StateDelta>.None, false);
        }

        private ServerHello RejectedHello(ClientHello hello, CompatibilityDecision decision)
        {
            ProtocolVersion selected = hello == null || !hello.Protocol.IsValid ? m_HandshakePolicy.Protocol : new ProtocolVersion(m_HandshakePolicy.Protocol.Major, (ushort)Math.Min(m_HandshakePolicy.Protocol.Minor, hello.Protocol.Minor));
            ErrorCode error = decision == CompatibilityDecision.SessionBusy ? ErrorCode.SessionBusy : ErrorCode.AuthFailed;
            m_Diagnostics.Record(decision == CompatibilityDecision.SessionBusy ? DiagnosticEventCode.SessionBusy : DiagnosticEventCode.HandshakeRejected, error: Optional<ErrorCode>.Some(error));
            return new ServerHello(selected, Optional<AssetHash>.None, m_HandshakePolicy.Build, ProtocolCapabilities.None, m_Data.Snapshot.Session, m_HandshakePolicy.CreateNonce(), decision);
        }

        private void EnsureAuthorized(ContractId clientId, PairingToken token)
        {
            if (!OwnsLease(clientId))
                throw new InvalidOperationException("The client does not own the session lease.");
            if (!m_Pairing.IsAuthorized(token))
                throw new InvalidOperationException("The pairing token is not authorized.");
        }

        private bool OwnsLease(ContractId clientId) => clientId.IsValid && m_LeaseOwner == clientId;

        private void ExpireLeaseIfRequired()
        {
            if (m_StateMachine.State == HostSessionState.Suspended && m_Clock.Milliseconds >= m_LeaseExpiresAt)
                ReleaseLease();
        }

        private void ReleaseLease()
        {
            m_Pairing.Revoke();
            m_LeaseOwner = default;
            m_LeaseExpiresAt = 0;
            m_EffectiveCapabilities = ProtocolCapabilities.None;
            m_StateMachine.ReleaseLease();
        }

        private static Optional<ScopeRevision> FindScopeRevision(SessionSnapshot snapshot, ScopeKey key)
        {
            for (int index = 0; index < snapshot.Scopes.Count; index++)
            {
                if (snapshot.Scopes[index].Scope == key)
                    return Optional<ScopeRevision>.Some(snapshot.Scopes[index].Revision);
            }

            return Optional<ScopeRevision>.None;
        }

        private static bool TryBuildDelta(SessionSnapshot snapshot, Command command, out StateDelta delta)
        {
            delta = null;
            PropertyKey key;
            if (command.Kind == CommandKind.SetOpacity && command.Payload.Kind == ContractValueKind.Number && command.Payload.Number >= 0d && command.Payload.Number <= 1d)
            {
                if (command.Scope.Type == ScopeType.Column)
                    key = V1PropertyKeys.ColumnActivityOpacity;
                else if (command.Scope.Type == ScopeType.Visualization)
                    key = V1PropertyKeys.VisualizationBrainOpacity;
                else
                    return false;
            }
            else if (command.Kind == CommandKind.SetTimelinePlayback && command.Scope.Type == ScopeType.Timeline && command.Payload.Kind == ContractValueKind.Boolean)
            {
                key = V1PropertyKeys.TimelinePlaybackState;
            }
            else
            {
                return false;
            }

            Optional<ScopeRevision> current = FindScopeRevision(snapshot, command.Scope);
            if (!current.HasValue)
                return false;
            ScopeDelta scope = new(command.Scope, current.Value, current.Value.Next(), new[] { PropertyChange.Set(key, command.Payload) });
            delta = new StateDelta(snapshot.Session, snapshot.StateRevision, snapshot.StateRevision.Next(), new[] { scope }, Array.Empty<AssetChange>());
            return true;
        }

        private static bool ResumeMetadataMatches(ResumeRequest request, SessionSnapshot current, IReadOnlyList<StateDelta> deltas)
        {
            Dictionary<ScopeKey, ScopeRevision> scopes = new();
            for (int index = 0; index < current.Scopes.Count; index++)
                scopes[current.Scopes[index].Scope] = current.Scopes[index].Revision;
            HashSet<AssetHash> assets = new();
            for (int index = 0; index < current.Assets.Count; index++)
                assets.Add(current.Assets[index].Hash);

            for (int deltaIndex = deltas.Count - 1; deltaIndex >= 0; deltaIndex--)
            {
                StateDelta delta = deltas[deltaIndex];
                for (int scopeIndex = 0; scopeIndex < delta.Scopes.Count; scopeIndex++)
                {
                    ScopeDelta change = delta.Scopes[scopeIndex];
                    if (change.BaseRevision.Value == 0)
                        scopes.Remove(change.Scope);
                    else
                        scopes[change.Scope] = change.BaseRevision;
                }

                for (int assetIndex = 0; assetIndex < delta.Assets.Count; assetIndex++)
                {
                    AssetChange change = delta.Assets[assetIndex];
                    if (change.Kind == AssetChangeKind.Add)
                        assets.Remove(change.Hash);
                    else
                        assets.Add(change.Hash);
                }
            }

            if (scopes.Count != request.ScopeRevisions.Count || assets.Count != request.Assets.Count)
                return false;
            foreach (KeyValuePair<ScopeKey, ScopeRevision> pair in scopes)
            {
                if (!request.ScopeRevisions.TryGetValue(pair.Key, out ScopeRevision revision) || revision != pair.Value)
                    return false;
            }

            for (int index = 0; index < request.Assets.Count; index++)
            {
                if (!assets.Contains(request.Assets[index]))
                    return false;
            }

            return true;
        }

        private static int EstimateSnapshotBytes(SessionSnapshot snapshot)
        {
            int bytes = 128 + (snapshot.Assets.Count * 64);
            for (int scopeIndex = 0; scopeIndex < snapshot.Scopes.Count; scopeIndex++)
            {
                ScopeState scope = snapshot.Scopes[scopeIndex];
                bytes = checked(bytes + 40);
                for (int propertyIndex = 0; propertyIndex < scope.Properties.Count; propertyIndex++)
                    bytes = checked(bytes + 16 + EstimateValueBytes(scope.Properties[propertyIndex].Value));
            }

            return bytes;
        }

        private static int EstimateDeltaBytes(StateDelta delta)
        {
            int bytes = 64 + (delta.Assets.Count * 72);
            for (int scopeIndex = 0; scopeIndex < delta.Scopes.Count; scopeIndex++)
            {
                bytes = checked(bytes + 48);
                for (int changeIndex = 0; changeIndex < delta.Scopes[scopeIndex].Changes.Count; changeIndex++)
                {
                    PropertyChange change = delta.Scopes[scopeIndex].Changes[changeIndex];
                    bytes = checked(bytes + 16 + (change.Value.HasValue ? EstimateValueBytes(change.Value.Value) : 0));
                }
            }

            return bytes;
        }

        private static int EstimateValueBytes(ContractValue value)
        {
            if (value.Kind == ContractValueKind.NumberVector)
                return checked(8 * value.Numbers.Count);
            if (value.Kind == ContractValueKind.IdList)
                return checked(16 * value.Ids.Count);
            return value.Kind == ContractValueKind.Id ? 16 : 8;
        }

        private sealed class HostData
        {
            public HostData(SessionSnapshot snapshot, DeltaJournal journal, IdempotenceLedger idempotence)
            {
                Snapshot = snapshot;
                Journal = journal;
                Idempotence = idempotence;
            }

            public SessionSnapshot Snapshot { get; }

            public DeltaJournal Journal { get; }

            public IdempotenceLedger Idempotence { get; }
        }
    }

    public sealed class SyntheticSessionClient
    {
        public const string ConflictMessage = "L’état a changé sur le Desktop. HiBoP XR se resynchronise ; réessayez votre action.";
        public const string BusyMessage = "Une autre session XR utilise ce Desktop. Fermez-la ou choisissez “Remplacer le casque” sur le Desktop.";

        private readonly ContractId m_ClientId;
        private readonly ClientHello m_Hello;
        private readonly SyntheticSessionHost m_Host;
        private readonly ClientSessionStateMachine m_StateMachine = new();
        private ulong m_NextSequence = 1;
        private PairingToken m_Token;

        public SyntheticSessionClient(SyntheticSessionHost host, ContractId clientId, ClientHello hello)
        {
            m_Host = host ?? throw new ArgumentNullException(nameof(host));
            if (!clientId.IsValid)
                throw new ArgumentException("A valid client identifier is required.", nameof(clientId));
            m_ClientId = clientId;
            m_Hello = hello ?? throw new ArgumentNullException(nameof(hello));
            Mirror = new AtomicSessionMirror();
        }

        public ClientSessionState State => m_StateMachine.State;

        public AtomicSessionMirror Mirror { get; }

        public string UserMessage { get; private set; } = string.Empty;

        public SessionDiagnosticSummary GetDiagnosticSummary()
        {
            SessionSnapshot snapshot = Mirror.HasState ? Mirror.Current : null;
            return new SessionDiagnosticSummary("client", m_StateMachine.State.ToString(), snapshot == null ? default : snapshot.Session, snapshot == null ? default : snapshot.StateRevision, 0, 0, 0, m_NextSequence - 1, 0);
        }

        public ServerHello PairAndConnect(string sas, bool transportIdentityVerified = true)
        {
            m_StateMachine.BeginPairing();
            PairingResult pairing = m_Host.Pair(m_ClientId, sas, transportIdentityVerified);
            if (!pairing.Accepted)
            {
                UserMessage = pairing.Error.Value == ErrorCode.SessionBusy ? BusyMessage : "Appairage refusé.";
                m_StateMachine.Refuse();
                return null;
            }

            m_Token = pairing.Token.Value;
            m_StateMachine.PairingAccepted();
            m_StateMachine.Connected();
            ServerHello hello = m_Host.Handshake(m_ClientId, m_Token, m_Hello);
            if (!hello.Accepted)
            {
                UserMessage = "Versions ou capacités incompatibles.";
                m_StateMachine.Refuse();
                return hello;
            }

            m_StateMachine.BeginSynchronization();
            SnapshotEnvelope envelope = m_Host.CaptureSnapshot(m_ClientId, m_Token);
            AtomicSessionMirror.MirrorTransaction transaction = Mirror.PrepareSnapshot(envelope.Snapshot);
            transaction.Commit();
            m_Host.AcknowledgeSynchronization(m_ClientId, m_Token, Mirror.Current.StateRevision);
            m_StateMachine.Activate();
            UserMessage = string.Empty;
            return hello;
        }

        public SequencedCommand PrepareCommand(Command command)
        {
            if (m_StateMachine.State != ClientSessionState.Active)
                throw new InvalidOperationException("Commands can be prepared only while active.");
            return new SequencedCommand(m_NextSequence++, command);
        }

        public CommandExecutionResult Send(SequencedCommand request)
        {
            if (m_StateMachine.State != ClientSessionState.Active)
                throw new InvalidOperationException("Commands can be sent only while active.");

            CommandExecutionResult result = m_Host.Execute(m_ClientId, m_Token, request);
            if (result.Delta.HasValue)
            {
                AtomicSessionMirror.MirrorTransaction transaction = Mirror.PrepareDeltas(new[] { result.Delta.Value });
                transaction.Commit();
            }
            else if (result.Outcome.Error.HasValue && result.Outcome.Error.Value.Code == ErrorCode.StateConflict)
            {
                UserMessage = ConflictMessage;
                Disconnect();
                Resume();
            }

            return result;
        }

        public void Disconnect()
        {
            if (m_StateMachine.State != ClientSessionState.Active)
                throw new InvalidOperationException("Only an active client can disconnect.");
            m_Host.Suspend(m_ClientId);
            m_StateMachine.ConnectionLost();
        }

        public ResumeResponse Resume()
        {
            if (m_StateMachine.State != ClientSessionState.ReconnectWait)
                throw new InvalidOperationException("The client is not waiting to reconnect.");
            m_StateMachine.RetryConnecting();
            ResumeRequest request = ResumeRequest.FromSnapshot(Mirror.Current);
            ResumeResponse response = m_Host.Resume(m_ClientId, m_Token, request);
            m_StateMachine.BeginSynchronization();

            if (response.Decision == ResumeDecision.ResumeWithDeltas)
            {
                AtomicSessionMirror.MirrorTransaction transaction = Mirror.PrepareDeltas(response.Deltas);
                transaction.Commit();
            }
            else
            {
                if (response.Decision == ResumeDecision.NewSession)
                {
                    Mirror.Purge();
                    m_NextSequence = 1;
                }

                AtomicSessionMirror.MirrorTransaction transaction = Mirror.PrepareSnapshot(response.Snapshot.Value);
                transaction.Commit();
            }

            m_Host.AcknowledgeSynchronization(m_ClientId, m_Token, Mirror.Current.StateRevision);
            m_StateMachine.Activate();
            return response;
        }

        public void HandleSessionReplaced()
        {
            Mirror.Purge();
            m_Token = null;
            UserMessage = "La session Desktop a été remplacée. Reconnectez le casque.";
            m_StateMachine.Refuse();
        }

        public void Close()
        {
            Mirror.Purge();
            m_Token = null;
            m_StateMachine.Close();
        }
    }
}
