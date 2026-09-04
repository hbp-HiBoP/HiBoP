using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CRNL.HiBoP.Contracts;

namespace CRNL.HiBoP.Protocol
{
    public interface IMonotonicClock
    {
        long Milliseconds { get; }
    }

    public sealed class SystemMonotonicClock : IMonotonicClock
    {
        public long Milliseconds => checked((long)(Stopwatch.GetTimestamp() * 1000d / Stopwatch.Frequency));
    }

    public enum HostSessionState : byte
    {
        Stopped = 0,
        Pairing = 1,
        AwaitingHello = 2,
        Synchronizing = 3,
        Active = 4,
        Suspended = 5,
        Replaced = 6,
        Closed = 7,
    }

    public enum ClientSessionState : byte
    {
        Idle = 0,
        Pairing = 1,
        Connecting = 2,
        Handshaking = 3,
        Synchronizing = 4,
        Active = 5,
        ReconnectWait = 6,
        Refused = 7,
        Closed = 8,
    }

    public sealed class HostSessionStateMachine
    {
        public HostSessionState State { get; private set; } = HostSessionState.Stopped;

        public void Start() => Move(HostSessionState.Stopped, HostSessionState.Pairing);

        public void Pair() => Move(HostSessionState.Pairing, HostSessionState.AwaitingHello);

        public void AcceptHello() => Move(HostSessionState.AwaitingHello, HostSessionState.Synchronizing);

        public void Activate() => Move(HostSessionState.Synchronizing, HostSessionState.Active);

        public void Suspend()
        {
            if (State != HostSessionState.Active && State != HostSessionState.Synchronizing)
                throw Invalid(HostSessionState.Suspended);
            State = HostSessionState.Suspended;
        }

        public void BeginResume() => Move(HostSessionState.Suspended, HostSessionState.Synchronizing);

        public void ReleaseLease()
        {
            if (State != HostSessionState.AwaitingHello && State != HostSessionState.Synchronizing && State != HostSessionState.Suspended)
                throw Invalid(HostSessionState.Pairing);
            State = HostSessionState.Pairing;
        }

        public void Replace()
        {
            if (State == HostSessionState.Stopped || State == HostSessionState.Closed || State == HostSessionState.Replaced)
                throw Invalid(HostSessionState.Replaced);
            State = HostSessionState.Replaced;
        }

        public void Close()
        {
            if (State == HostSessionState.Closed)
                return;
            if (State == HostSessionState.Replaced)
                throw Invalid(HostSessionState.Closed);
            State = HostSessionState.Closed;
        }

        private void Move(HostSessionState expected, HostSessionState target)
        {
            if (State != expected)
                throw Invalid(target);
            State = target;
        }

        private InvalidOperationException Invalid(HostSessionState target)
        {
            return new InvalidOperationException($"Host transition {State} -> {target} is not allowed.");
        }
    }

    public sealed class ClientSessionStateMachine
    {
        public ClientSessionState State { get; private set; } = ClientSessionState.Idle;

        public void BeginPairing() => Move(ClientSessionState.Idle, ClientSessionState.Pairing);

        public void PairingAccepted() => Move(ClientSessionState.Pairing, ClientSessionState.Connecting);

        public void Connected() => Move(ClientSessionState.Connecting, ClientSessionState.Handshaking);

        public void BeginSynchronization()
        {
            if (State != ClientSessionState.Handshaking && State != ClientSessionState.ReconnectWait && State != ClientSessionState.Connecting)
                throw Invalid(ClientSessionState.Synchronizing);
            State = ClientSessionState.Synchronizing;
        }

        public void Activate() => Move(ClientSessionState.Synchronizing, ClientSessionState.Active);

        public void ConnectionLost()
        {
            if (State != ClientSessionState.Active && State != ClientSessionState.Handshaking && State != ClientSessionState.Synchronizing && State != ClientSessionState.Connecting)
                throw Invalid(ClientSessionState.ReconnectWait);
            State = ClientSessionState.ReconnectWait;
        }

        public void RetryConnecting() => Move(ClientSessionState.ReconnectWait, ClientSessionState.Connecting);

        public void Refuse()
        {
            if (State == ClientSessionState.Refused || State == ClientSessionState.Closed)
                return;
            State = ClientSessionState.Refused;
        }

        public void Close()
        {
            if (State == ClientSessionState.Closed)
                return;
            State = ClientSessionState.Closed;
        }

        private void Move(ClientSessionState expected, ClientSessionState target)
        {
            if (State != expected)
                throw Invalid(target);
            State = target;
        }

        private InvalidOperationException Invalid(ClientSessionState target)
        {
            return new InvalidOperationException($"Client transition {State} -> {target} is not allowed.");
        }
    }

    public sealed class PairingToken : IEquatable<PairingToken>
    {
        public const int ByteLength = 32;
        private readonly byte[] m_Bytes;

        public PairingToken(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length != ByteLength)
                throw new ArgumentException($"A pairing token must contain {ByteLength} bytes.", nameof(bytes));
            m_Bytes = (byte[])bytes.Clone();
        }

        public bool Equals(PairingToken other)
        {
            if (ReferenceEquals(other, null))
                return false;

            int difference = 0;
            for (int index = 0; index < ByteLength; index++)
                difference |= m_Bytes[index] ^ other.m_Bytes[index];
            return difference == 0;
        }

        public override bool Equals(object obj) => Equals(obj as PairingToken);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                for (int index = 0; index < 4; index++)
                    hash = (hash * 31) ^ m_Bytes[index];
                return hash;
            }
        }

        public override string ToString() => "PairingToken(redacted)";
    }

    public sealed class PairingResult
    {
        private PairingResult(Optional<PairingToken> token, Optional<ErrorCode> error)
        {
            Token = token;
            Error = error;
        }

        public Optional<PairingToken> Token { get; }

        public Optional<ErrorCode> Error { get; }

        public bool Accepted => Token.HasValue;

        public static PairingResult Accept(PairingToken token) => new(Optional<PairingToken>.Some(token), Optional<ErrorCode>.None);

        public static PairingResult Reject(ErrorCode error) => new(Optional<PairingToken>.None, Optional<ErrorCode>.Some(error));
    }

    public sealed class PairingCoordinator
    {
        public const int MaximumAttemptsPerMinute = 10;
        public const long PairingWindowMilliseconds = 120_000;
        private readonly Queue<long> m_Attempts = new();
        private readonly IMonotonicClock m_Clock;
        private readonly string m_Sas;
        private readonly Func<byte[]> m_TokenFactory;
        private readonly long m_StartedAt;
        private PairingToken m_Token;

        public PairingCoordinator(string sas, IMonotonicClock clock, Func<byte[]> tokenFactory)
        {
            if (sas == null || sas.Length != 6)
                throw new ArgumentException("The SAS must contain exactly six decimal digits.", nameof(sas));
            for (int index = 0; index < sas.Length; index++)
            {
                if (sas[index] < '0' || sas[index] > '9')
                    throw new ArgumentException("The SAS must contain exactly six decimal digits.", nameof(sas));
            }

            m_Sas = sas;
            m_Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            m_TokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
            m_StartedAt = clock.Milliseconds;
        }

        public PairingResult TryPair(string suppliedSas, bool transportIdentityVerified)
        {
            long now = m_Clock.Milliseconds;
            PruneAttempts(now);
            if (now - m_StartedAt > PairingWindowMilliseconds || m_Attempts.Count >= MaximumAttemptsPerMinute)
                return PairingResult.Reject(ErrorCode.RateLimited);

            m_Attempts.Enqueue(now);
            if (!transportIdentityVerified || !SasEquals(suppliedSas))
                return PairingResult.Reject(ErrorCode.AuthFailed);

            byte[] bytes = m_TokenFactory();
            m_Token = new PairingToken(bytes);
            return PairingResult.Accept(m_Token);
        }

        public bool IsAuthorized(PairingToken token)
        {
            return m_Token != null && m_Token.Equals(token);
        }

        public void Revoke()
        {
            m_Token = null;
        }

        private void PruneAttempts(long now)
        {
            while (m_Attempts.Count > 0 && now - m_Attempts.Peek() >= 60_000)
                m_Attempts.Dequeue();
        }

        private bool SasEquals(string suppliedSas)
        {
            int suppliedLength = suppliedSas?.Length ?? 0;
            int difference = suppliedLength ^ m_Sas.Length;
            for (int index = 0; index < m_Sas.Length; index++)
            {
                char supplied = index < suppliedLength ? suppliedSas[index] : '\0';
                difference |= supplied ^ m_Sas[index];
            }

            return difference == 0;
        }
    }

    public enum DiagnosticEventCode : ushort
    {
        Unknown = 0,
        StateChanged = 1,
        PairingAccepted = 2,
        PairingRejected = 3,
        HandshakeAccepted = 4,
        HandshakeRejected = 5,
        SnapshotCommitted = 6,
        DeltaCommitted = 7,
        CommandApplied = 8,
        CommandReplayed = 9,
        CommandRejected = 10,
        ResumeWithDeltas = 11,
        FullSnapshotRequired = 12,
        NewSession = 13,
        HeartbeatTimeout = 14,
        ReconnectAttempt = 15,
        SessionBusy = 16,
        SessionReplaced = 17,
        JournalEvicted = 18,
    }

    public sealed class DiagnosticEvent
    {
        public DiagnosticEvent(long monotonicMilliseconds, DiagnosticEventCode code, Optional<ContractId> correlationId = default, Optional<ErrorCode> error = default)
        {
            if (monotonicMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(monotonicMilliseconds));
            if (code <= DiagnosticEventCode.Unknown || code > DiagnosticEventCode.JournalEvicted)
                throw new ArgumentOutOfRangeException(nameof(code));

            MonotonicMilliseconds = monotonicMilliseconds;
            Code = code;
            CorrelationId = correlationId;
            Error = error;
        }

        public long MonotonicMilliseconds { get; }

        public DiagnosticEventCode Code { get; }

        public Optional<ContractId> CorrelationId { get; }

        public Optional<ErrorCode> Error { get; }

        public override string ToString() => $"DiagnosticEvent(time={MonotonicMilliseconds}, code={Code}, correlation={CorrelationId.HasValue}, error={(Error.HasValue ? Error.Value.ToString() : "None")})";
    }

    public sealed class SessionDiagnostics
    {
        public const int Capacity = 256;
        private readonly object m_Gate = new();
        private readonly Queue<DiagnosticEvent> m_Events = new(Capacity);
        private readonly IMonotonicClock m_Clock;

        public SessionDiagnostics(IMonotonicClock clock)
        {
            m_Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void Record(DiagnosticEventCode code, Optional<ContractId> correlationId = default, Optional<ErrorCode> error = default)
        {
            lock (m_Gate)
            {
                if (m_Events.Count == Capacity)
                    m_Events.Dequeue();
                m_Events.Enqueue(new DiagnosticEvent(m_Clock.Milliseconds, code, correlationId, error));
            }
        }

        public IReadOnlyList<DiagnosticEvent> Snapshot()
        {
            lock (m_Gate)
                return new ReadOnlyCollection<DiagnosticEvent>(m_Events.ToArray());
        }
    }

    public sealed class ReconnectPolicy
    {
        private static readonly int[] s_Ceilings = { 250, 500, 1_000, 2_000, 4_000 };

        public const long BudgetMilliseconds = 30_000;

        public int GetDelayMilliseconds(int attempt, double jitterUnit)
        {
            if (attempt < 0)
                throw new ArgumentOutOfRangeException(nameof(attempt));
            if (jitterUnit < 0d || jitterUnit > 1d || double.IsNaN(jitterUnit))
                throw new ArgumentOutOfRangeException(nameof(jitterUnit));

            int ceiling = s_Ceilings[Math.Min(attempt, s_Ceilings.Length - 1)];
            return (int)Math.Floor(ceiling * jitterUnit);
        }
    }

    public sealed class HeartbeatMonitor
    {
        public const long IntervalMilliseconds = 1_000;
        public const long TimeoutMilliseconds = 3_000;
        private readonly IMonotonicClock m_Clock;
        private long m_LastReceived;
        private long m_LastSent;

        public HeartbeatMonitor(IMonotonicClock clock)
        {
            m_Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Reset();
        }

        public bool ShouldSend => m_Clock.Milliseconds - m_LastSent >= IntervalMilliseconds;

        public bool IsTimedOut => m_Clock.Milliseconds - m_LastReceived >= TimeoutMilliseconds;

        public void MarkSent()
        {
            m_LastSent = m_Clock.Milliseconds;
        }

        public void MarkReceived()
        {
            m_LastReceived = m_Clock.Milliseconds;
        }

        public void Reset()
        {
            m_LastReceived = m_Clock.Milliseconds;
            m_LastSent = m_Clock.Milliseconds;
        }
    }

    public sealed class SessionDiagnosticSummary
    {
        public SessionDiagnosticSummary(string role, string state, SessionEpoch session, StateRevision revision, int deltaJournalDepth, long deltaEvictions, int idempotenceDepth, ulong commandHighWaterMark, long appliedCommands)
        {
            Role = role ?? throw new ArgumentNullException(nameof(role));
            State = state ?? throw new ArgumentNullException(nameof(state));
            Session = session;
            Revision = revision;
            DeltaJournalDepth = deltaJournalDepth;
            DeltaEvictions = deltaEvictions;
            IdempotenceDepth = idempotenceDepth;
            CommandHighWaterMark = commandHighWaterMark;
            AppliedCommands = appliedCommands;
        }

        public string Role { get; }

        public string State { get; }

        public SessionEpoch Session { get; }

        public StateRevision Revision { get; }

        public int DeltaJournalDepth { get; }

        public long DeltaEvictions { get; }

        public int IdempotenceDepth { get; }

        public ulong CommandHighWaterMark { get; }

        public long AppliedCommands { get; }

        public override string ToString()
        {
            return $"SessionDiagnosticSummary(role={Role}, state={State}, session={Session}, revision={Revision}, deltaDepth={DeltaJournalDepth}, deltaEvictions={DeltaEvictions}, idempotenceDepth={IdempotenceDepth}, commandHighWater={CommandHighWaterMark}, appliedCommands={AppliedCommands})";
        }
    }
}
