using System;
using System.Collections.Generic;
using System.IO;

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

        internal V2CanonicalMutation(SceneId sceneId, IncarnationId incarnationId, OperationId operationId, ulong canonicalSequence, V2Mutation mutation)
        {
            SceneId = sceneId;
            IncarnationId = incarnationId;
            OperationId = operationId;
            CanonicalSequence = canonicalSequence;
            Mutation = mutation;
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
                current = m_Boundary.ReadCurrentMutation(mutation);
                m_Boundary.ValidateMutation(mutation);
            }
            catch (KeyNotFoundException)
            {
                return Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Rejected, rejectionCode: "missing_target"));
            }
            catch (ArgumentOutOfRangeException)
            {
                ulong keySequence = m_Ledger.TryGetLastAcceptedSequence(descriptor.CoalescingKey, out ulong lastAccepted) ? lastAccepted : m_CanonicalSequence;
                var correction = new V2MutationCorrection(m_SceneId, m_IncarnationId, operationId, keySequence, current, "invalid_mutation_value");
                string rejectionCode = mutation is SetTimelineAnchor ? "timeline_index_out_of_range" : "invalid_mutation_value";
                return Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Rejected, correction: correction, rejectionCode: rejectionCode));
            }

            if (observedCanonicalSequence > m_CanonicalSequence || m_CanonicalSequence == ulong.MaxValue)
                return Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Rejected, rejectionCode: "stale_sequence"));

            ulong acceptedSequence = m_CanonicalSequence + 1;
            V2OperationAdmission admission = m_Ledger.Accept(operationId, descriptor, observedCanonicalSequence, acceptedSequence, payload);
            if (admission == V2OperationAdmission.Accepted)
            {
                m_Boundary.Apply(mutation, V2MutationApplicationOrigin.Remote, operationId);
                m_CanonicalSequence = acceptedSequence;
                var canonical = new V2CanonicalMutation(m_SceneId, m_IncarnationId, operationId, acceptedSequence, mutation);
                var result = Remember(operationId, payload, new V2DesktopProposalResult(V2ProposalOutcome.Accepted, canonical));
                CanonicalReady?.Invoke(canonical);
                return result;
            }

            if (admission == V2OperationAdmission.Conflicting)
            {
                ulong keySequence = m_Ledger.TryGetLastAcceptedSequence(descriptor.CoalescingKey, out ulong lastAccepted) ? lastAccepted : m_CanonicalSequence;
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
        private readonly Queue<V2QuestMutationProposal> m_DeferredProposals = new Queue<V2QuestMutationProposal>();
        private readonly Dictionary<Guid, byte[]> m_Received = new Dictionary<Guid, byte[]>();
        private readonly Dictionary<V2TouchedKey, ulong> m_LastAppliedByKey = new Dictionary<V2TouchedKey, ulong>();
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
            m_Boundary.Apply(mutation, V2MutationApplicationOrigin.LocalQuest, operationId);
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
                m_Pending.Remove(canonical.OperationId.Value);
                RememberReceived(canonical.OperationId, payload);
                m_LastObservedCanonicalSequence = Math.Max(m_LastObservedCanonicalSequence, canonical.CanonicalSequence);
                bool keySuperseded = WasKeySuperseded(descriptor.CoalescingKey, canonical.CanonicalSequence);
                MarkKeyApplied(descriptor.CoalescingKey, canonical.CanonicalSequence);
                if (BytesEqual(optimisticPayload, payload))
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
            if (WasKeySuperseded(descriptor.CoalescingKey, canonical.CanonicalSequence)) return false;

            m_Boundary.Apply(canonical.Mutation, V2MutationApplicationOrigin.Remote, canonical.OperationId);
            MarkKeyApplied(descriptor.CoalescingKey, canonical.CanonicalSequence);
            return true;
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

            m_Pending.Remove(correction.OperationId.Value);
            RememberReceived(correction.OperationId, payload);
            m_LastObservedCanonicalSequence = Math.Max(m_LastObservedCanonicalSequence, correction.CanonicalSequence);
            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(m_SceneId, m_IncarnationId, correction.AuthoritativeMutation);
            bool keySuperseded = WasKeySuperseded(descriptor.CoalescingKey, correction.CanonicalSequence);
            MarkKeyApplied(descriptor.CoalescingKey, correction.CanonicalSequence);
            if (!BytesEqual(optimisticPayload, payload) && !keySuperseded)
            {
                m_Boundary.Apply(correction.AuthoritativeMutation, V2MutationApplicationOrigin.Remote, correction.OperationId);
                AuthoritativeCorrectionApplied?.Invoke(correction.OperationId, correction.AuthoritativeMutation);
                return true;
            }

            return false;
        }

        private void OnLocalMutationProposed(OperationId operationId, V2Mutation mutation, V2OriginDevice device)
        {
            if (m_Disposed || device != V2OriginDevice.Quest) return;
            RefreshConnectionState();
            if (m_OfflineLocal) return;
            if (m_Pending.ContainsKey(operationId.Value) || m_Received.ContainsKey(operationId.Value))
            {
                ProposalNotQueued?.Invoke(operationId, V2EnqueueDisposition.Rejected);
                EnterOfflineLocal(1);
                return;
            }

            var proposal = new V2QuestMutationProposal(m_SceneId, m_IncarnationId, operationId, m_LastObservedCanonicalSequence, mutation);
            if (m_Pending.Count >= MaximumRememberedOperations)
            {
                ProposalNotQueued?.Invoke(operationId, V2EnqueueDisposition.Backpressured);
                EnterOfflineLocal(1);
                return;
            }

            m_Pending.Add(operationId.Value, V2MutationPayloadCodec.Encode(mutation));
            m_LastCreatedProposal = proposal;
            if (m_DeferredProposals.Count > 0)
            {
                DeferProposal(proposal, V2EnqueueDisposition.Backpressured);
                return;
            }

            ScheduleProposal(proposal);
        }

        private void ScheduleProposal(V2QuestMutationProposal proposal)
        {
            V2EnqueueResult queued = m_Scheduler.EnqueueMutation(proposal.Mutation, coalesciblePreview: false, operationId: proposal.OperationId, observedCanonicalSequence: proposal.ObservedCanonicalSequence);
            if (queued.Accepted)
            {
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
            m_DeferredProposals.Enqueue(proposal);
            ProposalDeferred?.Invoke(proposal, disposition);
        }

        private void PumpDeferredProposals()
        {
            while (!m_OfflineLocal && m_DeferredProposals.Count > 0)
            {
                V2QuestMutationProposal proposal = m_DeferredProposals.Peek();
                V2EnqueueResult queued = m_Scheduler.EnqueueMutation(proposal.Mutation, coalesciblePreview: false, operationId: proposal.OperationId, observedCanonicalSequence: proposal.ObservedCanonicalSequence);
                if (queued.Accepted)
                {
                    m_DeferredProposals.Dequeue();
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
            m_Pending.Clear();
            m_DeferredProposals.Clear();
            m_Received.Clear();
            m_LastAppliedByKey.Clear();
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

        private bool WasKeySuperseded(V2TouchedKey key, ulong sequence) => m_LastAppliedByKey.TryGetValue(key, out ulong lastApplied) && lastApplied >= sequence;

        private void MarkKeyApplied(V2TouchedKey key, ulong sequence)
        {
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
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
                if (left[i] != right[i])
                    return false;
            return true;
        }
    }
}
