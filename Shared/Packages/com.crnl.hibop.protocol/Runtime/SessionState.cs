using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CRNL.HiBoP.Contracts;

namespace CRNL.HiBoP.Protocol
{
    public enum ResumeDecision : byte
    {
        Unknown = 0,
        ResumeWithDeltas = 1,
        FullSnapshotRequired = 2,
        NewSession = 3,
    }

    public sealed class SequencedCommand
    {
        public SequencedCommand(ulong clientCommandSequence, Command command)
        {
            if (clientCommandSequence == 0)
                throw new ArgumentOutOfRangeException(nameof(clientCommandSequence));
            ClientCommandSequence = clientCommandSequence;
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public ulong ClientCommandSequence { get; }

        public Command Command { get; }

        public override string ToString() => $"SequencedCommand(sequence={ClientCommandSequence}, command={Command})";
    }

    public sealed class CommandExecutionResult
    {
        public CommandExecutionResult(CommandOutcome outcome, Optional<StateDelta> delta, bool replayed)
        {
            Outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            if (replayed && delta.HasValue)
                throw new ArgumentException("A replay cannot publish a new delta.", nameof(delta));
            Delta = delta;
            Replayed = replayed;
        }

        public CommandOutcome Outcome { get; }

        public Optional<StateDelta> Delta { get; }

        public bool Replayed { get; }
    }

    public sealed class ResumeRequest
    {
        private readonly ReadOnlyDictionary<ScopeKey, ScopeRevision> m_ScopeRevisions;
        private readonly ReadOnlyCollection<AssetHash> m_Assets;

        public ResumeRequest(SessionEpoch session, StateRevision stateRevision, IDictionary<ScopeKey, ScopeRevision> scopeRevisions, IEnumerable<AssetHash> assets)
        {
            if (!session.IsValid)
                throw new ArgumentException("A valid session is required.", nameof(session));
            if (scopeRevisions == null)
                throw new ArgumentNullException(nameof(scopeRevisions));
            if (assets == null)
                throw new ArgumentNullException(nameof(assets));

            Dictionary<ScopeKey, ScopeRevision> revisions = new(scopeRevisions);
            foreach (ScopeKey key in revisions.Keys)
            {
                if (!key.IsValid)
                    throw new ArgumentException("Scope revisions contain an invalid scope.", nameof(scopeRevisions));
            }

            List<AssetHash> hashes = new(assets);
            if (hashes.Exists(hash => !hash.IsValid))
                throw new ArgumentException("Assets contain an invalid hash.", nameof(assets));
            hashes.Sort();
            for (int index = 1; index < hashes.Count; index++)
            {
                if (hashes[index - 1] == hashes[index])
                    throw new ArgumentException("Asset hashes must be unique.", nameof(assets));
            }

            Session = session;
            StateRevision = stateRevision;
            m_ScopeRevisions = new ReadOnlyDictionary<ScopeKey, ScopeRevision>(revisions);
            m_Assets = hashes.AsReadOnly();
        }

        public SessionEpoch Session { get; }

        public StateRevision StateRevision { get; }

        public IReadOnlyDictionary<ScopeKey, ScopeRevision> ScopeRevisions => m_ScopeRevisions;

        public IReadOnlyList<AssetHash> Assets => m_Assets;

        public static ResumeRequest FromSnapshot(SessionSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            Dictionary<ScopeKey, ScopeRevision> scopes = new();
            for (int index = 0; index < snapshot.Scopes.Count; index++)
                scopes.Add(snapshot.Scopes[index].Scope, snapshot.Scopes[index].Revision);

            AssetHash[] assets = new AssetHash[snapshot.Assets.Count];
            for (int index = 0; index < assets.Length; index++)
                assets[index] = snapshot.Assets[index].Hash;

            return new ResumeRequest(snapshot.Session, snapshot.StateRevision, scopes, assets);
        }
    }

    public sealed class ResumeResponse
    {
        private readonly ReadOnlyCollection<StateDelta> m_Deltas;

        private ResumeResponse(ResumeDecision decision, SessionEpoch session, IEnumerable<StateDelta> deltas, Optional<SessionSnapshot> snapshot)
        {
            Decision = decision;
            Session = session;
            m_Deltas = new List<StateDelta>(deltas ?? throw new ArgumentNullException(nameof(deltas))).AsReadOnly();
            Snapshot = snapshot;
        }

        public ResumeDecision Decision { get; }

        public SessionEpoch Session { get; }

        public IReadOnlyList<StateDelta> Deltas => m_Deltas;

        public Optional<SessionSnapshot> Snapshot { get; }

        public static ResumeResponse WithDeltas(SessionEpoch session, IEnumerable<StateDelta> deltas) => new(ResumeDecision.ResumeWithDeltas, session, deltas, Optional<SessionSnapshot>.None);

        public static ResumeResponse FullSnapshot(SessionSnapshot snapshot) => new(ResumeDecision.FullSnapshotRequired, snapshot.Session, Array.Empty<StateDelta>(), Optional<SessionSnapshot>.Some(snapshot));

        public static ResumeResponse NewSession(SessionSnapshot snapshot) => new(ResumeDecision.NewSession, snapshot.Session, Array.Empty<StateDelta>(), Optional<SessionSnapshot>.Some(snapshot));
    }

    public sealed class SnapshotEnvelope
    {
        public const int MaximumEncodedBytes = 64 * 1024;

        public SnapshotEnvelope(SessionSnapshot snapshot, ProtocolCapabilities effectiveCapabilities, int encodedBytes)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (encodedBytes <= 0 || encodedBytes > MaximumEncodedBytes)
                throw new ArgumentOutOfRangeException(nameof(encodedBytes), "A synthetic snapshot must fit one P06 control envelope.");

            Snapshot = snapshot;
            EffectiveCapabilities = effectiveCapabilities;
            EncodedBytes = encodedBytes;
        }

        public SessionSnapshot Snapshot { get; }

        public ProtocolCapabilities EffectiveCapabilities { get; }

        public int EncodedBytes { get; }
    }

    public static class SessionStateReducer
    {
        public static SessionSnapshot Apply(SessionSnapshot current, StateDelta delta)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));
            if (delta == null)
                throw new ArgumentNullException(nameof(delta));
            if (delta.Session != current.Session)
                throw new InvalidOperationException("A delta from another session cannot be applied.");
            if (delta.BaseStateRevision != current.StateRevision)
                throw new InvalidOperationException("The delta base state revision is not current.");

            Dictionary<ScopeKey, ScopeState> scopes = new();
            for (int index = 0; index < current.Scopes.Count; index++)
                scopes.Add(current.Scopes[index].Scope, current.Scopes[index]);

            for (int index = 0; index < delta.Scopes.Count; index++)
                ApplyScope(scopes, delta.Scopes[index]);

            Dictionary<AssetHash, AssetReference> assets = new();
            for (int index = 0; index < current.Assets.Count; index++)
                assets.Add(current.Assets[index].Hash, current.Assets[index]);

            for (int index = 0; index < delta.Assets.Count; index++)
            {
                AssetChange change = delta.Assets[index];
                if (change.Kind == AssetChangeKind.Add)
                {
                    if (assets.ContainsKey(change.Hash))
                        throw new InvalidOperationException("The delta adds an existing asset.");
                    assets.Add(change.Hash, change.Asset.Value);
                }
                else if (change.Kind == AssetChangeKind.Remove)
                {
                    if (!assets.Remove(change.Hash))
                        throw new InvalidOperationException("The delta removes an unknown asset.");
                }
                else
                {
                    throw new InvalidOperationException("The delta contains an unknown asset change.");
                }
            }

            return new SessionSnapshot(current.ContractVersion, current.Session, delta.ResultingStateRevision, scopes.Values, assets.Values);
        }

        private static void ApplyScope(IDictionary<ScopeKey, ScopeState> scopes, ScopeDelta delta)
        {
            bool exists = scopes.TryGetValue(delta.Scope, out ScopeState current);
            ScopeRevision expectedBase = exists ? current.Revision : new ScopeRevision(0);
            if (delta.BaseRevision != expectedBase)
                throw new InvalidOperationException("The delta base scope revision is not current.");

            if (delta.Kind == ScopeDeltaKind.Remove)
            {
                if (!exists)
                    throw new InvalidOperationException("The delta removes an unknown scope.");
                scopes.Remove(delta.Scope);
                return;
            }

            if (delta.Kind != ScopeDeltaKind.Update)
                throw new InvalidOperationException("The delta contains an unknown scope change.");

            Dictionary<PropertyKey, StateProperty> properties = new();
            if (exists)
            {
                for (int index = 0; index < current.Properties.Count; index++)
                    properties.Add(current.Properties[index].Key, current.Properties[index]);
            }

            for (int index = 0; index < delta.Changes.Count; index++)
            {
                PropertyChange change = delta.Changes[index];
                if (change.Kind == PropertyChangeKind.Set)
                    properties[change.Key] = new StateProperty(change.Key, change.Value.Value);
                else if (change.Kind == PropertyChangeKind.Remove)
                {
                    if (!properties.Remove(change.Key))
                        throw new InvalidOperationException("The delta removes an unknown property.");
                }
                else
                    throw new InvalidOperationException("The delta contains an unknown property change.");
            }

            scopes[delta.Scope] = new ScopeState(delta.Scope, delta.ResultingRevision, properties.Values);
        }
    }

    public sealed class AtomicSessionMirror
    {
        private readonly object m_Gate = new();
        private SessionSnapshot m_Current;
        private long m_Generation;

        public bool HasState
        {
            get
            {
                lock (m_Gate)
                    return m_Current != null;
            }
        }

        public SessionSnapshot Current
        {
            get
            {
                lock (m_Gate)
                    return m_Current ?? throw new InvalidOperationException("No snapshot has been committed.");
            }
        }

        public MirrorTransaction PrepareSnapshot(SessionSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            lock (m_Gate)
                return new MirrorTransaction(this, m_Generation, snapshot);
        }

        public MirrorTransaction PrepareDeltas(IEnumerable<StateDelta> deltas)
        {
            if (deltas == null)
                throw new ArgumentNullException(nameof(deltas));

            lock (m_Gate)
            {
                if (m_Current == null)
                    throw new InvalidOperationException("A snapshot is required before deltas.");

                SessionSnapshot candidate = m_Current;
                foreach (StateDelta delta in deltas)
                    candidate = SessionStateReducer.Apply(candidate, delta);
                return new MirrorTransaction(this, m_Generation, candidate);
            }
        }

        public void Purge()
        {
            lock (m_Gate)
            {
                m_Current = null;
                m_Generation++;
            }
        }

        private void Commit(long expectedGeneration, SessionSnapshot candidate)
        {
            lock (m_Gate)
            {
                if (m_Generation != expectedGeneration)
                    throw new InvalidOperationException("The mirror changed while the transaction was being prepared.");
                m_Current = candidate;
                m_Generation++;
            }
        }

        public sealed class MirrorTransaction
        {
            private readonly SessionSnapshot m_Candidate;
            private readonly long m_ExpectedGeneration;
            private AtomicSessionMirror m_Owner;

            internal MirrorTransaction(AtomicSessionMirror owner, long expectedGeneration, SessionSnapshot candidate)
            {
                m_Owner = owner;
                m_ExpectedGeneration = expectedGeneration;
                m_Candidate = candidate;
            }

            public SessionSnapshot Candidate => m_Candidate;

            public void Commit()
            {
                AtomicSessionMirror owner = m_Owner ?? throw new InvalidOperationException("The transaction has already been committed.");
                owner.Commit(m_ExpectedGeneration, m_Candidate);
                m_Owner = null;
            }
        }
    }
}
