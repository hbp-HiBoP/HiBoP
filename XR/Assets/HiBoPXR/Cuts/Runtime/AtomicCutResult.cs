using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;

namespace CRNL.HiBoP.XR.Cuts
{
    public enum CutResultApplyResult : byte
    {
        Committed = 1,
        Stale = 2,
        Rejected = 3,
    }

    public enum CutFeedbackState : byte
    {
        Idle = 1,
        Canonical = 2,
        Pending = 3,
        Error = 4,
    }

    public sealed class CommittedCutResult<TPrepared> where TPrepared : class
    {
        internal CommittedCutResult(CutResultPublication publication, TPrepared prepared)
        {
            Publication = publication;
            Prepared = prepared;
        }

        public CutResultPublication Publication { get; }
        public TPrepared Prepared { get; }
    }

    public sealed class AtomicCutResult<TPrepared> where TPrepared : class
    {
        private readonly object m_Gate = new();
        private readonly Func<CutResultPublication, TPrepared> m_Prepare;
        private readonly Action<TPrepared> m_Release;
        private SessionEpoch m_Session;
        private ContractId m_CutId;
        private ContractId m_RequestedInteractionId;
        private InteractionSequence m_RequestedSequence;
        private CommittedCutResult<TPrepared> m_Current;
        private CutFeedbackState m_Feedback;
        private Exception m_Error;

        public AtomicCutResult(SessionEpoch session, ContractId cutId, Func<CutResultPublication, TPrepared> prepare, Action<TPrepared> release = null)
        {
            m_Prepare = prepare ?? throw new ArgumentNullException(nameof(prepare));
            m_Release = release;
            Reset(session, cutId);
        }

        public CutFeedbackState Feedback
        {
            get
            {
                lock (m_Gate)
                    return m_Feedback;
            }
        }

        public Exception Error
        {
            get
            {
                lock (m_Gate)
                    return m_Error;
            }
        }

        public void Reset(SessionEpoch session, ContractId cutId)
        {
            if (!session.IsValid || !cutId.IsValid)
                throw new ArgumentException("A valid session and cut ID are required.");
            TPrepared previous;
            lock (m_Gate)
            {
                previous = m_Current?.Prepared;
                m_Session = session;
                m_CutId = cutId;
                m_RequestedInteractionId = default;
                m_RequestedSequence = default;
                m_Current = null;
                m_Feedback = CutFeedbackState.Idle;
                m_Error = null;
            }

            Release(previous);
        }

        public bool ObserveRequest(Command command)
        {
            if (!CutCommands.TryRead(command, out _) || command.Session != m_Session || command.Scope.Id != m_CutId)
                throw new ArgumentException("The command does not belong to this cut result scope.", nameof(command));
            lock (m_Gate)
            {
                ContractId interactionId = command.InteractionId.Value;
                InteractionSequence sequence = command.Sequence.Value;
                if (interactionId == m_RequestedInteractionId && m_RequestedSequence.IsValid && sequence.Value <= m_RequestedSequence.Value)
                    return false;
                m_RequestedInteractionId = interactionId;
                m_RequestedSequence = sequence;
                m_Feedback = CutFeedbackState.Pending;
                m_Error = null;
                return true;
            }
        }

        public CutResultApplyResult TryPrepareAndCommit(CutResultPublication publication, out Exception error)
        {
            if (publication == null)
            {
                error = new ArgumentNullException(nameof(publication));
                return CutResultApplyResult.Rejected;
            }

            lock (m_Gate)
            {
                CutResultApplyResult precheck = Check(publication);
                if (precheck != CutResultApplyResult.Committed)
                {
                    error = null;
                    return precheck;
                }
            }

            TPrepared prepared;
            try
            {
                prepared = m_Prepare(publication) ?? throw new InvalidOperationException("Cut preparation returned null.");
            }
            catch (Exception exception)
            {
                MarkError(publication.Result.InteractionId, publication.Result.Sequence, exception);
                error = exception;
                return CutResultApplyResult.Rejected;
            }

            TPrepared previous = null;
            CutResultApplyResult result;
            lock (m_Gate)
            {
                result = Check(publication);
                if (result == CutResultApplyResult.Committed)
                {
                    previous = m_Current?.Prepared;
                    m_Current = new CommittedCutResult<TPrepared>(publication, prepared);
                    m_Feedback = CutFeedbackState.Canonical;
                    m_Error = null;
                }
            }

            if (result != CutResultApplyResult.Committed)
                Release(prepared);
            else
                Release(previous);
            error = null;
            return result;
        }

        public bool MarkError(ContractId interactionId, InteractionSequence sequence, Exception error)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error));
            lock (m_Gate)
            {
                if (interactionId != m_RequestedInteractionId || sequence != m_RequestedSequence)
                    return false;
                m_Feedback = CutFeedbackState.Error;
                m_Error = error;
                return true;
            }
        }

        public bool TryRead(out CommittedCutResult<TPrepared> current)
        {
            lock (m_Gate)
            {
                current = m_Current;
                return current != null;
            }
        }

        private CutResultApplyResult Check(CutResultPublication publication)
        {
            if (publication.Session != m_Session || publication.Result.CutId != m_CutId)
                return CutResultApplyResult.Rejected;
            if (!m_RequestedSequence.IsValid || publication.Result.InteractionId != m_RequestedInteractionId || publication.Result.Sequence != m_RequestedSequence)
                return CutResultApplyResult.Stale;
            if (m_Current != null && (publication.Result.RenderRevision <= m_Current.Publication.Result.RenderRevision || publication.Result.SourceStateRevision < m_Current.Publication.Result.SourceStateRevision))
                return CutResultApplyResult.Stale;
            return CutResultApplyResult.Committed;
        }

        private void Release(TPrepared prepared)
        {
            if (prepared != null)
                m_Release?.Invoke(prepared);
        }
    }
}
