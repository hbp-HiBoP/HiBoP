using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HBP.Transfer.Transport;

namespace HBP.Sync.Scene
{
    public enum V2ProposalOutcome : byte
    {
        Accepted,
        Duplicate,
        Rejected,
        OperationIdReused,
        Overflow
    }

    public enum V2QuestMutationConnectionState : byte
    {
        Connected,
        OfflineLocal
    }

    public enum V2DesktopMutationAuthorityState : byte
    {
        Active,
        Faulted
    }

    public sealed class V2QuestMutationProposal
    {
        public SceneId SceneId { get; }
        public IncarnationId IncarnationId { get; }
        public OperationId OperationId { get; }
        public ulong ObservedCanonicalSequence { get; }
        public V2Mutation Mutation { get; }

        internal V2QuestMutationProposal(SceneId sceneId, IncarnationId incarnationId, OperationId operationId, ulong observedCanonicalSequence, V2Mutation mutation)
        {
            SceneId = sceneId;
            IncarnationId = incarnationId;
            OperationId = operationId;
            ObservedCanonicalSequence = observedCanonicalSequence;
            Mutation = mutation;
        }
    }

    public sealed class V2CanonicalMutation
    {
        public SceneId SceneId { get; }
        public IncarnationId IncarnationId { get; }
        public OperationId OperationId { get; }
        public ulong CanonicalSequence { get; }
        public V2Mutation Mutation { get; }
        public V2OriginDevice OriginDevice { get; }

        internal V2CanonicalMutation(SceneId sceneId, IncarnationId incarnationId, OperationId operationId, ulong canonicalSequence, V2Mutation mutation, V2OriginDevice originDevice = V2OriginDevice.Desktop)
        {
            SceneId = sceneId;
            IncarnationId = incarnationId;
            OperationId = operationId;
            CanonicalSequence = canonicalSequence;
            Mutation = mutation;
            OriginDevice = originDevice;
        }
    }

    public sealed class V2MutationCorrection
    {
        public SceneId SceneId { get; }
        public IncarnationId IncarnationId { get; }
        public OperationId OperationId { get; }
        public ulong CanonicalSequence { get; }
        public V2Mutation AuthoritativeMutation { get; }
        public string RejectionCode { get; }

        internal V2MutationCorrection(SceneId sceneId, IncarnationId incarnationId, OperationId operationId, ulong canonicalSequence, V2Mutation authoritativeMutation, string rejectionCode)
        {
            SceneId = sceneId;
            IncarnationId = incarnationId;
            OperationId = operationId;
            CanonicalSequence = canonicalSequence;
            AuthoritativeMutation = authoritativeMutation;
            RejectionCode = rejectionCode;
        }
    }

    public sealed class V2DesktopProposalResult
    {
        public V2ProposalOutcome Outcome { get; }
        public V2CanonicalMutation CanonicalMutation { get; }
        public V2MutationCorrection Correction { get; }
        public string RejectionCode { get; }

        internal V2DesktopProposalResult(V2ProposalOutcome outcome, V2CanonicalMutation canonicalMutation = null, V2MutationCorrection correction = null, string rejectionCode = null)
        {
            Outcome = outcome;
            CanonicalMutation = canonicalMutation;
            Correction = correction;
            RejectionCode = rejectionCode;
        }
    }

    public sealed class V2QuestProposalDecision
    {
        public OperationId OperationId { get; }
        public V2MutationCorrection Correction { get; }
        public string RejectionCode { get; }

        internal V2QuestProposalDecision(OperationId operationId, V2MutationCorrection correction, string rejectionCode)
        {
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
            Correction = correction;
            RejectionCode = rejectionCode ?? throw new ArgumentNullException(nameof(rejectionCode));
        }
    }

    /// <summary>Versioned scene-stream body for a Quest proposal rejection or targeted correction.</summary>
    public static class V2QuestProposalDecisionCodec
    {
        public const ushort BodySchema = 3;
        private const ushort SchemaVersion = 1;
        private const int FixedHeaderLength = 37;
        private const int MaximumRejectionCodeBytes = 256;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("HBPD");
        private static readonly UTF8Encoding Utf8 = new(false, true);

        public static byte[] EncodeCorrection(V2MutationCorrection correction)
        {
            if (correction == null) throw new ArgumentNullException(nameof(correction));
            byte[] mutation = V2MutationPayloadCodec.Encode(correction.AuthoritativeMutation);
            return Encode(correction.OperationId, correction.RejectionCode, correction.CanonicalSequence, mutation);
        }

        public static byte[] EncodeRejection(OperationId operationId, string rejectionCode)
        {
            return Encode(operationId, rejectionCode, 0, Array.Empty<byte>());
        }

        public static V2QuestProposalDecision Decode(byte[] bytes, SceneId sceneId, IncarnationId incarnationId)
        {
            if (sceneId == null) throw new ArgumentNullException(nameof(sceneId));
            if (incarnationId == null) throw new ArgumentNullException(nameof(incarnationId));
            if (bytes == null || bytes.Length < FixedHeaderLength || bytes.Length > V2TransportFrameCodec.MaximumPayloadBytes)
                throw new InvalidDataException("Invalid Quest proposal decision length.");

            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Utf8, true);
            try
            {
                if (!reader.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic) || reader.ReadUInt16() != SchemaVersion)
                    throw new InvalidDataException("Unsupported Quest proposal decision signature or schema.");
                byte[] operationBytes = reader.ReadBytes(16);
                if (operationBytes.Length != 16) throw new EndOfStreamException();
                var operationId = new OperationId(new Guid(operationBytes));
                byte hasCorrection = reader.ReadByte();
                if (hasCorrection > 1) throw new InvalidDataException("Invalid Quest proposal decision flags.");
                ulong canonicalSequence = reader.ReadUInt64();
                ushort rejectionLength = reader.ReadUInt16();
                int mutationLength = reader.ReadInt32();
                if (rejectionLength == 0 || rejectionLength > MaximumRejectionCodeBytes || mutationLength < 0 || mutationLength > V2MutationEnvelopeCodec.MaximumPayloadBytes || mutationLength != stream.Length - stream.Position - rejectionLength)
                    throw new InvalidDataException("Invalid Quest proposal decision fields.");
                string rejectionCode = Utf8.GetString(reader.ReadBytes(rejectionLength));
                if (hasCorrection == 0)
                {
                    if (canonicalSequence != 0 || mutationLength != 0)
                        throw new InvalidDataException("A rejection without correction cannot carry canonical state.");
                    return new V2QuestProposalDecision(operationId, null, rejectionCode);
                }

                if (mutationLength == 0) throw new InvalidDataException("A correction must carry an authoritative mutation.");
                byte[] mutationBytes = reader.ReadBytes(mutationLength);
                if (mutationBytes.Length != mutationLength || stream.Position != stream.Length)
                    throw new InvalidDataException("Truncated Quest proposal correction.");
                V2Mutation mutation = V2MutationPayloadCodec.Decode(mutationBytes);
                var correction = new V2MutationCorrection(sceneId, incarnationId, operationId, canonicalSequence, mutation, rejectionCode);
                return new V2QuestProposalDecision(operationId, correction, rejectionCode);
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException("Truncated Quest proposal decision.", exception);
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException("Invalid Quest proposal decision text.", exception);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Invalid Quest proposal decision identity.", exception);
            }
        }

        private static byte[] Encode(OperationId operationId, string rejectionCode, ulong canonicalSequence, byte[] mutation)
        {
            if (operationId == null) throw new ArgumentNullException(nameof(operationId));
            if (string.IsNullOrEmpty(rejectionCode)) throw new ArgumentException("A rejection code is required.", nameof(rejectionCode));
            byte[] text = Utf8.GetBytes(rejectionCode);
            if (text.Length > MaximumRejectionCodeBytes) throw new InvalidDataException("Quest proposal rejection code exceeds its bound.");
            if (mutation == null || mutation.Length > V2MutationEnvelopeCodec.MaximumPayloadBytes)
                throw new InvalidDataException("Quest proposal correction exceeds its payload bound.");

            using var stream = new MemoryStream(FixedHeaderLength + text.Length + mutation.Length);
            using var writer = new BinaryWriter(stream, Utf8, true);
            writer.Write(Magic);
            writer.Write(SchemaVersion);
            writer.Write(operationId.ToByteArray());
            writer.Write(mutation.Length == 0 ? (byte)0 : (byte)1);
            writer.Write(canonicalSequence);
            writer.Write(checked((ushort)text.Length));
            writer.Write(mutation.Length);
            writer.Write(text);
            writer.Write(mutation);
            writer.Flush();
            if (stream.Length > V2TransportFrameCodec.MaximumPayloadBytes)
                throw new InvalidDataException("Quest proposal decision exceeds the inline transport bound.");
            return stream.ToArray();
        }
    }

    /// <summary>Desktop acceptance-order authority for one prepared scene incarnation.</summary>
    public sealed class V2DesktopMutationAuthority : IDisposable
    {
        public const int MaximumRememberedOperations = 4096;

        private readonly SceneId m_SceneId;
        private readonly IncarnationId m_IncarnationId;
        private readonly V2SceneMutationBoundary m_Boundary;
        private readonly V2OperationLedger m_Ledger;
        private readonly Dictionary<Guid, Decision> m_Decisions = new Dictionary<Guid, Decision>();
        private ulong m_CanonicalSequence;
        private V2DesktopMutationAuthorityState m_State;
        private bool m_Disposed;

        public ulong CanonicalSequence => m_CanonicalSequence;
        public V2DesktopMutationAuthorityState State => m_State;
        public string FailureCode { get; private set; }
        public event Action<V2CanonicalMutation> CanonicalReady;

        /// <summary>Session owners must close the connected replica session when this event fires.</summary>
        public event Action<string> SessionMustDisconnect;

        public V2DesktopMutationAuthority(SceneId sceneId, IncarnationId incarnationId, V2SceneMutationBoundary boundary, int maximumOperations = MaximumRememberedOperations, int maximumKeys = 65536)
        {
            m_SceneId = sceneId ?? throw new ArgumentNullException(nameof(sceneId));
            m_IncarnationId = incarnationId ?? throw new ArgumentNullException(nameof(incarnationId));
            m_Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
            m_Ledger = new V2OperationLedger(maximumOperations, maximumKeys);
            m_Boundary.MutationProposed += OnLocalMutationProposed;
        }

        /// <summary>Accepts a Quest proposal, applies it once on Desktop, and returns its echo or targeted correction.</summary>
        public V2DesktopProposalResult AcceptQuestProposal(V2QuestMutationProposal proposal)
        {
            ThrowIfDisposed();
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));
            if (m_State == V2DesktopMutationAuthorityState.Faulted)
                return new V2DesktopProposalResult(V2ProposalOutcome.Overflow, rejectionCode: "authority_faulted");
            if (!m_SceneId.Equals(proposal.SceneId) || !m_IncarnationId.Equals(proposal.IncarnationId))
                return new V2DesktopProposalResult(V2ProposalOutcome.Rejected, rejectionCode: "wrong_incarnation");

            return AcceptQuestProposal(proposal.OperationId, proposal.Mutation, proposal.ObservedCanonicalSequence);
        }

        public V2DesktopProposalResult AcceptQuestProposal(SceneId sceneId, IncarnationId incarnationId, OperationId operationId, V2Mutation mutation, ulong observedCanonicalSequence)
        {
            if (sceneId == null) throw new ArgumentNullException(nameof(sceneId));
            if (incarnationId == null) throw new ArgumentNullException(nameof(incarnationId));
            if (!m_SceneId.Equals(sceneId) || !m_IncarnationId.Equals(incarnationId))
                return new V2DesktopProposalResult(V2ProposalOutcome.Rejected, rejectionCode: "wrong_incarnation");
            return AcceptQuestProposal(operationId, mutation, observedCanonicalSequence);
        }

        /// <summary>Accepts a decoded proposal when its transport metadata is already available to the receiver.</summary>
        public V2DesktopProposalResult AcceptQuestProposal(OperationId operationId, V2Mutation mutation, ulong observedCanonicalSequence)
        {
            ThrowIfDisposed();
            if (operationId == null) throw new ArgumentNullException(nameof(operationId));
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));
            if (m_State == V2DesktopMutationAuthorityState.Faulted)
                return new V2DesktopProposalResult(V2ProposalOutcome.Overflow, rejectionCode: "authority_faulted");

            byte[] payload = V2MutationPayloadCodec.Encode(mutation);
            if (TryGetPriorDecision(operationId, payload, out V2DesktopProposalResult prior))
                return PublishPriorCanonical(prior);
            if (m_Decisions.Count >= MaximumRememberedOperations)
            {
                FaultAuthority("operation_history_full");
                return new V2DesktopProposalResult(V2ProposalOutcome.Overflow, rejectionCode: "operation_history_full");
            }

            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(m_SceneId, m_IncarnationId, mutation);
            V2Mutation current = null;
            try
            {
                current = ReadCurrentMutationForCorrection(mutation);
                m_Boundary.ValidateMutation(mutation);
            }
            catch (KeyNotFoundException)
            {
                return Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Rejected, rejectionCode: "missing_target"));
            }
            catch (ArgumentOutOfRangeException)
            {
                string rejectionCode = mutation is SetTimelineAnchor ? "timeline_index_out_of_range" : "invalid_mutation_value";
                if (current == null)
                    return Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Rejected, rejectionCode: rejectionCode));
                ulong keySequence = GetLastAcceptedSequence(descriptor);
                var correction = new V2MutationCorrection(m_SceneId, m_IncarnationId, operationId, keySequence, current, rejectionCode);
                return Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Rejected, correction: correction, rejectionCode: rejectionCode));
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is InvalidDataException)
            {
                string rejectionCode = mutation is ApplyTriangleMask ? "topology_unavailable" : "prepared_resource_unavailable";
                if (current != null && V2MutationPayloadCodec.Encode(current).Length <= V2TransportFrameCodec.MaximumPayloadBytes / 2)
                {
                    ulong keySequence = GetLastAcceptedSequence(descriptor);
                    var correction = new V2MutationCorrection(m_SceneId, m_IncarnationId, operationId, keySequence, current, rejectionCode);
                    return Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Rejected, correction: correction, rejectionCode: rejectionCode));
                }

                return Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Rejected, rejectionCode: rejectionCode));
            }

            if (observedCanonicalSequence > m_CanonicalSequence || m_CanonicalSequence == ulong.MaxValue)
            {
                if (current == null)
                    return Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Rejected, rejectionCode: "stale_sequence"));
                ulong keySequence = GetLastAcceptedSequence(descriptor);
                var correction = new V2MutationCorrection(m_SceneId, m_IncarnationId, operationId, keySequence, current, "stale_sequence");
                return Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Rejected, correction: correction, rejectionCode: correction.RejectionCode));
            }

            ulong acceptedSequence = m_CanonicalSequence + 1;
            V2OperationAdmission admission = m_Ledger.Accept(operationId, descriptor, observedCanonicalSequence, acceptedSequence, payload);
            if (admission == V2OperationAdmission.Accepted)
            {
                m_Boundary.Apply(mutation, V2MutationApplicationOrigin.Remote, operationId);
                m_CanonicalSequence = acceptedSequence;
                var canonical = new V2CanonicalMutation(m_SceneId, m_IncarnationId, operationId, acceptedSequence, mutation, V2OriginDevice.Quest);
                var result = Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Accepted, canonical));
                CanonicalReady?.Invoke(canonical);
                return result;
            }

            if (admission == V2OperationAdmission.Conflicting)
            {
                if (current == null)
                    return Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Rejected, rejectionCode: "stale_sequence"));
                ulong keySequence = GetLastAcceptedSequence(descriptor);
                var correction = new V2MutationCorrection(m_SceneId, m_IncarnationId, operationId, keySequence, current, "stale_sequence");
                return Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Rejected, correction: correction, rejectionCode: correction.RejectionCode));
            }

            if (admission == V2OperationAdmission.OperationIdReused)
                return new V2DesktopProposalResult(V2ProposalOutcome.OperationIdReused, rejectionCode: "operation_id_reused");
            if (admission == V2OperationAdmission.Overflow)
            {
                FaultAuthority("operation_history_full");
                return Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Overflow, rejectionCode: "operation_history_full"));
            }

            return Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Rejected, rejectionCode: "invalid_proposal"));
        }

        private V2Mutation ReadCurrentMutationForCorrection(V2Mutation mutation)
        {
            // A transaction can create staged identities, so no single current-value mutation can
            // correct it. A plain rejection lets Quest restore its captured checkpoint.
            if (mutation is SetConfigurationTransaction) return null;
            return m_Boundary.ReadCurrentMutation(mutation);
        }

        private ulong GetLastAcceptedSequence(V2ScheduleDescriptor descriptor)
        {
            ulong latest = 0;
            foreach (V2TouchedKey key in descriptor.TouchedKeys)
                if (m_Ledger.TryGetLastAcceptedSequence(key, out ulong sequence) && sequence > latest)
                    latest = sequence;
            return latest == 0 ? m_CanonicalSequence : latest;
        }

        private void OnLocalMutationProposed(OperationId operationId, V2Mutation mutation, V2OriginDevice device)
        {
            if (m_Disposed || m_State == V2DesktopMutationAuthorityState.Faulted || device != V2OriginDevice.Desktop) return;
            ThrowIfDisposed();
            byte[] payload = V2MutationPayloadCodec.Encode(mutation);
            if (TryGetPriorDecision(operationId, payload, out V2DesktopProposalResult prior))
            {
                PublishPriorCanonical(prior);
                return;
            }

            if (m_Decisions.Count >= MaximumRememberedOperations)
            {
                FaultAuthority("operation_history_full");
                return;
            }

            if (m_CanonicalSequence == ulong.MaxValue)
            {
                FaultAuthority("canonical_sequence_exhausted");
                return;
            }

            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(m_SceneId, m_IncarnationId, mutation);
            ulong acceptedSequence = m_CanonicalSequence + 1;
            V2OperationAdmission admission = m_Ledger.Accept(operationId, descriptor, m_CanonicalSequence, acceptedSequence, payload);
            if (admission != V2OperationAdmission.Accepted)
            {
                FaultAuthority(admission == V2OperationAdmission.Overflow ? "operation_history_full" : "local_mutation_not_sequenced");
                return;
            }

            m_CanonicalSequence = acceptedSequence;
            var canonical = new V2CanonicalMutation(m_SceneId, m_IncarnationId, operationId, acceptedSequence, mutation);
            Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Accepted, canonical));
            CanonicalReady?.Invoke(canonical);
        }

        private void FaultAuthority(string failureCode)
        {
            if (m_State == V2DesktopMutationAuthorityState.Faulted) return;
            m_State = V2DesktopMutationAuthorityState.Faulted;
            FailureCode = failureCode;
            SessionMustDisconnect?.Invoke(failureCode);
        }

        private bool TryGetPriorDecision(OperationId operationId, byte[] payload, out V2DesktopProposalResult result)
        {
            if (!m_Decisions.TryGetValue(operationId.Value, out Decision decision))
            {
                result = null;
                return false;
            }

            if (!BytesEqual(decision.Payload, payload))
            {
                result = new V2DesktopProposalResult(V2ProposalOutcome.OperationIdReused, rejectionCode: "operation_id_reused");
                return true;
            }

            result = new V2DesktopProposalResult(V2ProposalOutcome.Duplicate, decision.Result.CanonicalMutation, decision.Result.Correction, decision.Result.RejectionCode);
            return true;
        }

        private V2DesktopProposalResult PublishPriorCanonical(V2DesktopProposalResult result)
        {
            if (result.CanonicalMutation != null) CanonicalReady?.Invoke(result.CanonicalMutation);
            return result;
        }

        private V2DesktopProposalResult Remember(OperationId operationId, byte[] payload, V2DesktopProposalResult result)
        {
            if (m_Decisions.Count < MaximumRememberedOperations)
                m_Decisions.Add(operationId.Value, new Decision(payload, result));
            return result;
        }

        private void ThrowIfDisposed()
        {
            if (m_Disposed) throw new ObjectDisposedException(nameof(V2DesktopMutationAuthority));
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Boundary.MutationProposed -= OnLocalMutationProposed;
            m_Ledger.CloseIncarnation(m_SceneId, m_IncarnationId);
            m_Decisions.Clear();
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
                if (left[i] != right[i])
                    return false;
            return true;
        }

        private sealed class Decision
        {
            public byte[] Payload { get; }
            public V2DesktopProposalResult Result { get; }

            public Decision(byte[] payload, V2DesktopProposalResult result)
            {
                Payload = payload;
                Result = result;
            }
        }
    }

    /// <summary>Quest-side optimistic driver with canonical echo deduplication and bounded reconnect history.</summary>
    public sealed class V2QuestMutationDriver : IDisposable
    {
        public const int MaximumRememberedOperations = V2DesktopMutationAuthority.MaximumRememberedOperations;

        private readonly SceneId m_SceneId;
        private readonly IncarnationId m_IncarnationId;
        private readonly V2SceneMutationBoundary m_Boundary;
        private readonly V2OutgoingScheduler m_Scheduler;
        private readonly Dictionary<Guid, byte[]> m_Pending = new Dictionary<Guid, byte[]>();
        private readonly LinkedList<V2QuestMutationProposal> m_DeferredProposals = new LinkedList<V2QuestMutationProposal>();
        private readonly Dictionary<V2TouchedKey, LinkedListNode<V2QuestMutationProposal>> m_DeferredByKey = new Dictionary<V2TouchedKey, LinkedListNode<V2QuestMutationProposal>>();
        private readonly Dictionary<Guid, byte[]> m_Received = new Dictionary<Guid, byte[]>();
        private readonly Dictionary<V2TouchedKey, ulong> m_LastAppliedByKey = new Dictionary<V2TouchedKey, ulong>();
        private readonly Dictionary<Guid, List<ReplayMutation>> m_TransactionReplayJournal = new Dictionary<Guid, List<ReplayMutation>>();
        private readonly HashSet<Guid> m_OverflowedTransactionReplayJournals = new HashSet<Guid>();
        private ulong m_LastObservedCanonicalSequence;
        private bool m_Disposed;
        private bool m_OfflineLocal;
        private V2QuestMutationProposal m_LastCreatedProposal;

        public ulong LastObservedCanonicalSequence => m_LastObservedCanonicalSequence;
        public int PendingProposalCount => m_Pending.Count;
        public int DeferredProposalCount => m_DeferredProposals.Count;
        public int AbandonedProposalCount { get; private set; }

        public V2QuestMutationConnectionState ConnectionState
        {
            get
            {
                RefreshConnectionState();
                return m_OfflineLocal ? V2QuestMutationConnectionState.OfflineLocal : V2QuestMutationConnectionState.Connected;
            }
        }

        public event Action<V2QuestMutationProposal> ProposalQueued;
        public event Action<V2QuestMutationProposal, V2EnqueueDisposition> ProposalDeferred;
        public event Action<OperationId, V2EnqueueDisposition> ProposalNotQueued;
        public event Action<OperationId> ProposalConfirmed;
        public event Action<OperationId, V2Mutation> AuthoritativeCorrectionApplied;
        public event Action<OperationId, string> ProposalRejected;
        public event Action OfflineLocalEntered;

        public V2QuestMutationDriver(SceneId sceneId, IncarnationId incarnationId, V2SceneMutationBoundary boundary, V2OutgoingScheduler scheduler, ulong lastObservedCanonicalSequence = 0)
        {
            m_SceneId = sceneId ?? throw new ArgumentNullException(nameof(sceneId));
            m_IncarnationId = incarnationId ?? throw new ArgumentNullException(nameof(incarnationId));
            m_Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
            m_Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            if (scheduler.OriginDevice != V2OriginDevice.Quest || !scheduler.SceneId.Equals(sceneId) || !scheduler.IncarnationId.Equals(incarnationId))
                throw new ArgumentException("The Quest mutation scheduler must belong to this scene incarnation.", nameof(scheduler));
            m_LastObservedCanonicalSequence = lastObservedCanonicalSequence;
            m_Boundary.MutationProposed += OnLocalMutationProposed;
        }

        /// <summary>Applies one Quest-origin value through the prepared scene boundary and queues its stable proposal.</summary>
        public V2QuestMutationProposal ApplyOptimistic(V2Mutation mutation, OperationId operationId)
        {
            ThrowIfDisposed();
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));
            if (operationId == null) throw new ArgumentNullException(nameof(operationId));
            m_LastCreatedProposal = null;
            bool trackTransaction = mutation is SetConfigurationTransaction && !m_TransactionReplayJournal.ContainsKey(operationId.Value);
            if (trackTransaction) m_TransactionReplayJournal.Add(operationId.Value, new List<ReplayMutation>());
            try
            {
                m_Boundary.Apply(mutation, V2MutationApplicationOrigin.LocalQuest, operationId);
            }
            catch
            {
                if (trackTransaction)
                {
                    m_TransactionReplayJournal.Remove(operationId.Value);
                    m_OverflowedTransactionReplayJournals.Remove(operationId.Value);
                }

                throw;
            }

            if (trackTransaction && m_LastCreatedProposal == null)
            {
                m_TransactionReplayJournal.Remove(operationId.Value);
                m_OverflowedTransactionReplayJournals.Remove(operationId.Value);
            }

            return m_LastCreatedProposal;
        }

        public void BeginDisconnectGrace()
        {
            ThrowIfDisposed();
            m_Scheduler.BeginDisconnectGrace();
        }

        /// <summary>Returns false after the scheduler's fake-clock 500 ms grace has expired.</summary>
        public bool TryReconnect()
        {
            ThrowIfDisposed();
            RefreshConnectionState();
            if (m_Scheduler.State != V2SchedulerState.DisconnectedGrace) return false;
            bool resumed = m_Scheduler.TryResume();
            if (RefreshConnectionState() != V2QuestMutationConnectionState.OfflineLocal)
                PumpDeferredProposals();
            return resumed;
        }

        public bool TryGetNextTransmission(out V2TransmissionAttempt transmission)
        {
            ThrowIfDisposed();
            if (RefreshConnectionState() == V2QuestMutationConnectionState.OfflineLocal)
            {
                transmission = null;
                return false;
            }

            PumpDeferredProposals();
            if (m_OfflineLocal)
            {
                transmission = null;
                return false;
            }

            bool available = m_Scheduler.TryGetNextTransmission(out transmission);
            RefreshConnectionState();
            return available;
        }

        /// <summary>Applies an accepted Desktop mutation once, or only confirms its matching optimistic Quest value.</summary>
        public bool ReceiveCanonical(V2CanonicalMutation canonical)
        {
            ThrowIfDisposed();
            if (canonical == null) throw new ArgumentNullException(nameof(canonical));
            if (RefreshConnectionState() == V2QuestMutationConnectionState.OfflineLocal) return false;
            ValidateScope(canonical.SceneId, canonical.IncarnationId);
            if (canonical.CanonicalSequence == 0) throw new InvalidDataException("A canonical mutation must have a nonzero sequence.");

            byte[] payload = V2MutationPayloadCodec.Encode(canonical.Mutation);
            if (TryGetReceived(canonical.OperationId, payload)) return false;

            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(m_SceneId, m_IncarnationId, canonical.Mutation);
            if (m_Pending.TryGetValue(canonical.OperationId.Value, out byte[] optimisticPayload))
            {
                bool keySuperseded = WasKeySuperseded(descriptor.CoalescingKey, canonical.CanonicalSequence);
                bool matchesOptimistic = BytesEqual(optimisticPayload, payload);
                RecordCanonicalForPendingTransactions(canonical, keySuperseded, matchesOptimistic);
                RemovePendingProposal(canonical.OperationId);
                RememberReceived(canonical.OperationId, payload);
                m_LastObservedCanonicalSequence = Math.Max(m_LastObservedCanonicalSequence, canonical.CanonicalSequence);
                MarkKeyApplied(descriptor.CoalescingKey, canonical.CanonicalSequence);
                if (matchesOptimistic)
                {
                    ProposalConfirmed?.Invoke(canonical.OperationId);
                    return false;
                }

                if (!keySuperseded)
                {
                    m_Boundary.Apply(canonical.Mutation, V2MutationApplicationOrigin.Remote, canonical.OperationId);
                    AuthoritativeCorrectionApplied?.Invoke(canonical.OperationId, canonical.Mutation);
                    return true;
                }

                return false;
            }

            RememberReceived(canonical.OperationId, payload);
            m_LastObservedCanonicalSequence = Math.Max(m_LastObservedCanonicalSequence, canonical.CanonicalSequence);
            bool superseded = WasKeySuperseded(descriptor.CoalescingKey, canonical.CanonicalSequence);
            RecordCanonicalForPendingTransactions(canonical, superseded, preserveExistingOrder: false);
            if (superseded) return false;

            m_Boundary.Apply(canonical.Mutation, V2MutationApplicationOrigin.Remote, canonical.OperationId);
            MarkKeyApplied(descriptor.CoalescingKey, canonical.CanonicalSequence);
            return true;
        }

        public bool ReceiveCanonical(SceneId sceneId, IncarnationId incarnationId, OperationId operationId, ulong canonicalSequence, V2Mutation mutation)
        {
            if (sceneId == null) throw new ArgumentNullException(nameof(sceneId));
            if (incarnationId == null) throw new ArgumentNullException(nameof(incarnationId));
            if (operationId == null) throw new ArgumentNullException(nameof(operationId));
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));
            return ReceiveCanonical(new V2CanonicalMutation(sceneId, incarnationId, operationId, canonicalSequence, mutation));
        }

        public void AdvanceCanonicalWatermark(ulong canonicalSequence)
        {
            ThrowIfDisposed();
            if (canonicalSequence < m_LastObservedCanonicalSequence)
                throw new InvalidDataException("A canonical checkpoint cannot move the observed sequence backwards.");
            m_LastObservedCanonicalSequence = canonicalSequence;
        }

        /// <summary>Applies a rejection's current authoritative value through the same targeted setter handler.</summary>
        public bool ReceiveCorrection(V2MutationCorrection correction)
        {
            ThrowIfDisposed();
            if (correction == null) throw new ArgumentNullException(nameof(correction));
            if (RefreshConnectionState() == V2QuestMutationConnectionState.OfflineLocal) return false;
            ValidateScope(correction.SceneId, correction.IncarnationId);

            byte[] payload = V2MutationPayloadCodec.Encode(correction.AuthoritativeMutation);
            if (TryGetReceived(correction.OperationId, payload)) return false;
            if (!m_Pending.TryGetValue(correction.OperationId.Value, out byte[] optimisticPayload)) return false;

            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(m_SceneId, m_IncarnationId, correction.AuthoritativeMutation);
            bool keySuperseded = WasKeySuperseded(descriptor.CoalescingKey, correction.CanonicalSequence);
            bool matchesOptimistic = BytesEqual(optimisticPayload, payload);
            RecordCanonicalForPendingTransactions(correction.OperationId, correction.AuthoritativeMutation, keySuperseded, matchesOptimistic);
            RemovePendingProposal(correction.OperationId);
            RememberReceived(correction.OperationId, payload);
            m_LastObservedCanonicalSequence = Math.Max(m_LastObservedCanonicalSequence, correction.CanonicalSequence);
            MarkKeyApplied(descriptor.CoalescingKey, correction.CanonicalSequence);
            if (!matchesOptimistic && !keySuperseded)
            {
                m_Boundary.Apply(correction.AuthoritativeMutation, V2MutationApplicationOrigin.Remote, correction.OperationId);
                AuthoritativeCorrectionApplied?.Invoke(correction.OperationId, correction.AuthoritativeMutation);
                return true;
            }

            return false;
        }

        /// <summary>Closes the online proposal set when the Desktop cannot provide a usable correction.</summary>
        public bool ReceiveRejection(OperationId operationId, string rejectionCode)
        {
            ThrowIfDisposed();
            if (operationId == null) throw new ArgumentNullException(nameof(operationId));
            if (string.IsNullOrEmpty(rejectionCode)) throw new ArgumentException("A rejection code is required.", nameof(rejectionCode));
            if (!m_Pending.ContainsKey(operationId.Value)) return false;
            List<ReplayMutation> replay = m_TransactionReplayJournal.TryGetValue(operationId.Value, out List<ReplayMutation> entries) ? new List<ReplayMutation>(entries) : null;
            bool replayOverflowed = m_OverflowedTransactionReplayJournals.Contains(operationId.Value);
            V2SceneMutationCheckpoint liveCheckpoint = !replayOverflowed && replay != null && replay.Count > 0 ? m_Boundary.CaptureCheckpoint() : null;
            bool restored = !replayOverflowed && m_Boundary.TryRollbackOptimisticOperation(operationId);
            if (restored && replay != null && replay.Count > 0)
                ReplayAfterRejectedTransaction(operationId, replay, liveCheckpoint);

            RemoveDeferredProposalForOperation(operationId);
            RemovePendingProposal(operationId);
            RemoveReplayEntriesForOperation(operationId.Value);
            ProposalRejected?.Invoke(operationId, rejectionCode);
            if (!restored) EnterOfflineLocal();
            return true;
        }

        private void OnLocalMutationProposed(OperationId operationId, V2Mutation mutation, V2OriginDevice device)
        {
            if (m_Disposed || device != V2OriginDevice.Quest) return;
            bool createdTransactionJournal = mutation is SetConfigurationTransaction && !m_TransactionReplayJournal.ContainsKey(operationId.Value);
            if (createdTransactionJournal)
                m_TransactionReplayJournal.Add(operationId.Value, new List<ReplayMutation>());

            RecordOptimisticForPendingTransactions(operationId, mutation);
            RefreshConnectionState();
            if (m_OfflineLocal)
            {
                RemoveCreatedTransactionJournal(operationId, createdTransactionJournal);
                return;
            }

            if (m_Pending.ContainsKey(operationId.Value) || m_Received.ContainsKey(operationId.Value))
            {
                ProposalNotQueued?.Invoke(operationId, V2EnqueueDisposition.Rejected);
                EnterOfflineLocal(1);
                RemoveCreatedTransactionJournal(operationId, createdTransactionJournal);
                return;
            }

            var proposal = new V2QuestMutationProposal(m_SceneId, m_IncarnationId, operationId, m_LastObservedCanonicalSequence, mutation);
            V2TouchedKey key = V2ScheduleDescriptor.ForMutation(m_SceneId, m_IncarnationId, mutation).CoalescingKey;
            bool replacesDeferred = key != null && m_DeferredByKey.ContainsKey(key);
            if (m_Pending.Count >= MaximumRememberedOperations && !replacesDeferred)
            {
                ProposalNotQueued?.Invoke(operationId, V2EnqueueDisposition.Backpressured);
                EnterOfflineLocal(1);
                RemoveCreatedTransactionJournal(operationId, createdTransactionJournal);
                return;
            }

            m_Pending.Add(operationId.Value, V2MutationPayloadCodec.Encode(mutation));
            m_LastCreatedProposal = proposal;
            ScheduleProposal(proposal);
        }

        private void ScheduleProposal(V2QuestMutationProposal proposal)
        {
            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(m_SceneId, m_IncarnationId, proposal.Mutation);
            V2EnqueueResult queued = m_Scheduler.EnqueueMutation(proposal.Mutation, coalesciblePreview: descriptor.CoalescingKey != null, operationId: proposal.OperationId, observedCanonicalSequence: proposal.ObservedCanonicalSequence);
            if (queued.Accepted)
            {
                RemovePendingProposal(queued.ReplacedOperationId);
                if (descriptor.CoalescingKey != null) RemoveDeferredProposalForKey(descriptor.CoalescingKey);
                ProposalQueued?.Invoke(proposal);
                return;
            }

            if (queued.Disposition == V2EnqueueDisposition.Backpressured)
            {
                DeferProposal(proposal, queued.Disposition);
                return;
            }

            ProposalNotQueued?.Invoke(proposal.OperationId, queued.Disposition);
            EnterOfflineLocal();
        }

        private void DeferProposal(V2QuestMutationProposal proposal, V2EnqueueDisposition disposition)
        {
            V2TouchedKey key = V2ScheduleDescriptor.ForMutation(m_SceneId, m_IncarnationId, proposal.Mutation).CoalescingKey;
            if (key != null && m_DeferredByKey.TryGetValue(key, out LinkedListNode<V2QuestMutationProposal> existing))
            {
                RemovePendingProposal(existing.Value.OperationId);
                existing.Value = proposal;
            }
            else
            {
                LinkedListNode<V2QuestMutationProposal> node = m_DeferredProposals.AddLast(proposal);
                if (key != null) m_DeferredByKey.Add(key, node);
            }

            ProposalDeferred?.Invoke(proposal, disposition);
        }

        private void PumpDeferredProposals()
        {
            while (!m_OfflineLocal && m_DeferredProposals.Count > 0)
            {
                LinkedListNode<V2QuestMutationProposal> node = m_DeferredProposals.First;
                V2QuestMutationProposal proposal = node.Value;
                V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(m_SceneId, m_IncarnationId, proposal.Mutation);
                V2EnqueueResult queued = m_Scheduler.EnqueueMutation(proposal.Mutation, coalesciblePreview: descriptor.CoalescingKey != null, operationId: proposal.OperationId, observedCanonicalSequence: proposal.ObservedCanonicalSequence);
                if (queued.Accepted)
                {
                    RemovePendingProposal(queued.ReplacedOperationId);
                    RemoveDeferredProposal(node, removePending: false);
                    ProposalQueued?.Invoke(proposal);
                    continue;
                }

                if (queued.Disposition == V2EnqueueDisposition.Backpressured)
                    return;

                ProposalNotQueued?.Invoke(proposal.OperationId, queued.Disposition);
                EnterOfflineLocal();
                return;
            }
        }

        private void RemovePendingProposal(OperationId operationId)
        {
            if (operationId == null) return;
            m_Pending.Remove(operationId.Value);
            m_Boundary.ForgetOptimisticOperation(operationId);
            m_TransactionReplayJournal.Remove(operationId.Value);
            m_OverflowedTransactionReplayJournals.Remove(operationId.Value);
        }

        private void RecordOptimisticForPendingTransactions(OperationId operationId, V2Mutation mutation)
        {
            foreach (KeyValuePair<Guid, List<ReplayMutation>> pending in m_TransactionReplayJournal)
            {
                if (pending.Key == operationId.Value) continue;
                if (pending.Value.Count >= MaximumRememberedOperations)
                {
                    m_OverflowedTransactionReplayJournals.Add(pending.Key);
                    continue;
                }

                pending.Value.Add(new ReplayMutation(operationId, mutation, isOptimistic: true));
            }
        }

        private void RemoveCreatedTransactionJournal(OperationId operationId, bool created)
        {
            if (!created) return;
            m_TransactionReplayJournal.Remove(operationId.Value);
            m_OverflowedTransactionReplayJournals.Remove(operationId.Value);
        }

        private void RecordCanonicalForPendingTransactions(V2CanonicalMutation canonical, bool superseded, bool preserveExistingOrder) => RecordCanonicalForPendingTransactions(canonical.OperationId, canonical.Mutation, superseded, preserveExistingOrder);

        private void RecordCanonicalForPendingTransactions(OperationId operationId, V2Mutation mutation, bool superseded, bool preserveExistingOrder)
        {
            foreach (KeyValuePair<Guid, List<ReplayMutation>> pending in m_TransactionReplayJournal)
            {
                if (pending.Key == operationId.Value) continue;
                ReplayMutation existing = pending.Value.Find(entry => entry.OperationId.Equals(operationId));
                if (superseded)
                {
                    if (existing != null) pending.Value.Remove(existing);
                    continue;
                }

                if (existing != null && preserveExistingOrder)
                {
                    existing.Mutation = mutation;
                    existing.IsOptimistic = false;
                    continue;
                }

                // A correction is applied at receipt time. Move it after intervening
                // authoritative mutations instead of rewriting the optimistic entry's
                // earlier position in this journal.
                if (existing != null) pending.Value.Remove(existing);

                if (pending.Value.Count >= MaximumRememberedOperations)
                {
                    m_OverflowedTransactionReplayJournals.Add(pending.Key);
                    continue;
                }

                pending.Value.Add(new ReplayMutation(operationId, mutation, isOptimistic: false));
            }
        }

        private void ReplayAfterRejectedTransaction(OperationId rejectedOperationId, IReadOnlyList<ReplayMutation> replay, V2SceneMutationCheckpoint liveCheckpoint)
        {
            try
            {
                foreach (ReplayMutation entry in replay)
                {
                    if (entry.IsOptimistic)
                    {
                        m_Boundary.ApplyOptimisticReplay(entry.Mutation, entry.OperationId);
                    }
                    else
                    {
                        m_Boundary.Apply(entry.Mutation, V2MutationApplicationOrigin.Remote, entry.OperationId);
                    }
                }
            }
            catch (Exception replayFailure)
            {
                try
                {
                    m_Boundary.ApplyCheckpoint(liveCheckpoint, new OperationId(Guid.NewGuid()));
                }
                catch (Exception restoreFailure)
                {
                    EnterOfflineLocal();
                    throw new AggregateException("Rejected configuration transaction replay and live-scene restoration failed; pending proposals were abandoned and a fresh scene publish is required.", replayFailure, restoreFailure);
                }

                EnterOfflineLocal();
                throw new InvalidOperationException($"Rejected configuration transaction {rejectedOperationId} could not replay newer scene mutations; the pre-rejection scene state was restored, pending proposals were abandoned, and a fresh scene publish is required.", replayFailure);
            }
        }

        private void RemoveReplayEntriesForOperation(Guid operationId)
        {
            foreach (List<ReplayMutation> entries in m_TransactionReplayJournal.Values)
                entries.RemoveAll(entry => entry.OperationId.Value == operationId);
        }

        private void RemoveDeferredProposalForKey(V2TouchedKey key)
        {
            if (key == null) return;
            if (!m_DeferredByKey.TryGetValue(key, out LinkedListNode<V2QuestMutationProposal> node)) return;
            RemoveDeferredProposal(node, removePending: true);
        }

        private void RemoveDeferredProposalForOperation(OperationId operationId)
        {
            LinkedListNode<V2QuestMutationProposal> node = m_DeferredProposals.First;
            while (node != null)
            {
                LinkedListNode<V2QuestMutationProposal> next = node.Next;
                if (node.Value.OperationId.Equals(operationId)) RemoveDeferredProposal(node, removePending: false);
                node = next;
            }
        }

        private void RemoveDeferredProposal(LinkedListNode<V2QuestMutationProposal> node, bool removePending)
        {
            if (node == null) return;
            V2TouchedKey key = V2ScheduleDescriptor.ForMutation(m_SceneId, m_IncarnationId, node.Value.Mutation).CoalescingKey;
            if (key != null && m_DeferredByKey.TryGetValue(key, out LinkedListNode<V2QuestMutationProposal> indexed) && ReferenceEquals(indexed, node))
                m_DeferredByKey.Remove(key);
            m_DeferredProposals.Remove(node);
            if (removePending)
                RemovePendingProposal(node.Value.OperationId);
        }

        private V2QuestMutationConnectionState RefreshConnectionState()
        {
            if (m_Disposed || m_OfflineLocal) return m_OfflineLocal ? V2QuestMutationConnectionState.OfflineLocal : V2QuestMutationConnectionState.Connected;
            if (m_Scheduler.State == V2SchedulerState.DisconnectedGrace)
                m_Scheduler.TryGetNextTransmission(out _);
            if (m_Scheduler.State == V2SchedulerState.Offline || m_Scheduler.State == V2SchedulerState.Faulted)
            {
                EnterOfflineLocal();
                return V2QuestMutationConnectionState.OfflineLocal;
            }

            return V2QuestMutationConnectionState.Connected;
        }

        private void EnterOfflineLocal(int additionalAbandoned = 0)
        {
            if (m_OfflineLocal) return;
            AbandonedProposalCount += m_Pending.Count + additionalAbandoned;
            foreach (Guid operationId in m_Pending.Keys)
                m_Boundary.ForgetOptimisticOperation(new OperationId(operationId));
            m_Pending.Clear();
            m_DeferredProposals.Clear();
            m_DeferredByKey.Clear();
            m_Received.Clear();
            m_LastAppliedByKey.Clear();
            m_TransactionReplayJournal.Clear();
            m_OverflowedTransactionReplayJournals.Clear();
            m_OfflineLocal = true;
            OfflineLocalEntered?.Invoke();
        }

        private bool TryGetReceived(OperationId operationId, byte[] payload)
        {
            if (!m_Received.TryGetValue(operationId.Value, out byte[] previous)) return false;
            if (!BytesEqual(previous, payload)) throw new InvalidDataException("A canonical operation ID was reused with a different payload.");
            return true;
        }

        private void RememberReceived(OperationId operationId, byte[] payload)
        {
            if (m_Received.ContainsKey(operationId.Value)) return;
            if (m_Received.Count >= MaximumRememberedOperations)
                throw new InvalidOperationException("The bounded canonical operation history is full.");
            m_Received.Add(operationId.Value, payload);
        }

        private bool WasKeySuperseded(V2TouchedKey key, ulong sequence) => key != null && m_LastAppliedByKey.TryGetValue(key, out ulong lastApplied) && lastApplied >= sequence;

        private void MarkKeyApplied(V2TouchedKey key, ulong sequence)
        {
            if (key == null) return;
            if (!m_LastAppliedByKey.TryGetValue(key, out ulong lastApplied) || sequence > lastApplied)
                m_LastAppliedByKey[key] = sequence;
        }

        private void ValidateScope(SceneId sceneId, IncarnationId incarnationId)
        {
            if (!m_SceneId.Equals(sceneId) || !m_IncarnationId.Equals(incarnationId))
                throw new InvalidDataException("A canonical mutation belongs to a different scene incarnation.");
        }

        private void ThrowIfDisposed()
        {
            if (m_Disposed) throw new ObjectDisposedException(nameof(V2QuestMutationDriver));
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Boundary.MutationProposed -= OnLocalMutationProposed;
            m_Pending.Clear();
            m_DeferredProposals.Clear();
            m_Received.Clear();
            m_LastAppliedByKey.Clear();
            m_TransactionReplayJournal.Clear();
            m_OverflowedTransactionReplayJournals.Clear();
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
                if (left[i] != right[i])
                    return false;
            return true;
        }

        private sealed class ReplayMutation
        {
            public OperationId OperationId { get; }
            public V2Mutation Mutation { get; set; }
            public bool IsOptimistic { get; set; }

            public ReplayMutation(OperationId operationId, V2Mutation mutation, bool isOptimistic)
            {
                OperationId = operationId;
                Mutation = mutation;
                IsOptimistic = isOptimistic;
            }
        }
    }
}
