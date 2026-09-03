using System;
using System.Collections.Generic;
using CRNL.HiBoP.Contracts;

namespace CRNL.HiBoP.Protocol
{
    internal enum IdempotenceDisposition : byte
    {
        Execute = 1,
        Replay = 2,
        Expired = 3,
        Mismatch = 4,
        Gap = 5,
    }

    internal readonly struct IdempotenceLookup
    {
        public IdempotenceLookup(IdempotenceDisposition disposition, Optional<CommandOutcome> outcome)
        {
            Disposition = disposition;
            Outcome = outcome;
        }

        public IdempotenceDisposition Disposition { get; }

        public Optional<CommandOutcome> Outcome { get; }
    }

    internal sealed class IdempotenceLedger
    {
        public const int MaximumEntries = 4096;
        public const long MaximumAgeMilliseconds = 15 * 60 * 1000;

        private readonly LinkedList<Entry> m_Order;
        private readonly Dictionary<ContractId, LinkedListNode<Entry>> m_ByCommandId;
        private readonly Dictionary<ulong, LinkedListNode<Entry>> m_BySequence;

        public IdempotenceLedger()
        {
            m_Order = new LinkedList<Entry>();
            m_ByCommandId = new Dictionary<ContractId, LinkedListNode<Entry>>();
            m_BySequence = new Dictionary<ulong, LinkedListNode<Entry>>();
        }

        private IdempotenceLedger(IdempotenceLedger source)
        {
            HighWaterMark = source.HighWaterMark;
            m_Order = new LinkedList<Entry>();
            m_ByCommandId = new Dictionary<ContractId, LinkedListNode<Entry>>(source.m_ByCommandId.Count);
            m_BySequence = new Dictionary<ulong, LinkedListNode<Entry>>(source.m_BySequence.Count);
            foreach (Entry entry in source.m_Order)
            {
                LinkedListNode<Entry> node = m_Order.AddLast(entry);
                m_ByCommandId.Add(entry.CommandId, node);
                m_BySequence.Add(entry.Sequence, node);
            }
        }

        public ulong HighWaterMark { get; private set; }

        public int Count => m_Order.Count;

        public IdempotenceLedger Clone() => new(this);

        public IdempotenceLookup Lookup(SequencedCommand request, long now)
        {
            Prune(now);
            ulong sequence = request.ClientCommandSequence;
            if (m_ByCommandId.TryGetValue(request.Command.CommandId, out LinkedListNode<Entry> commandNode) && commandNode.Value.Sequence != sequence)
                return new IdempotenceLookup(IdempotenceDisposition.Mismatch, Optional<CommandOutcome>.None);
            if (HighWaterMark != ulong.MaxValue && sequence == HighWaterMark + 1)
                return new IdempotenceLookup(IdempotenceDisposition.Execute, Optional<CommandOutcome>.None);
            if (HighWaterMark == ulong.MaxValue || sequence > HighWaterMark + 1)
                return new IdempotenceLookup(IdempotenceDisposition.Gap, Optional<CommandOutcome>.None);

            if (!m_BySequence.TryGetValue(sequence, out LinkedListNode<Entry> node))
                return new IdempotenceLookup(IdempotenceDisposition.Expired, Optional<CommandOutcome>.None);
            if (node.Value.CommandId != request.Command.CommandId)
                return new IdempotenceLookup(IdempotenceDisposition.Mismatch, Optional<CommandOutcome>.None);
            return new IdempotenceLookup(IdempotenceDisposition.Replay, Optional<CommandOutcome>.Some(node.Value.Outcome));
        }

        public void Record(SequencedCommand request, CommandOutcome outcome, long now)
        {
            if (request.ClientCommandSequence != HighWaterMark + 1)
                throw new InvalidOperationException("Only the next command sequence can be recorded.");
            if (outcome.CommandId != request.Command.CommandId)
                throw new ArgumentException("The outcome belongs to another command.", nameof(outcome));

            Entry entry = new(request.ClientCommandSequence, request.Command.CommandId, outcome, now);
            LinkedListNode<Entry> node = m_Order.AddLast(entry);
            m_ByCommandId.Add(entry.CommandId, node);
            m_BySequence.Add(entry.Sequence, node);
            HighWaterMark = entry.Sequence;
            Prune(now);
        }

        private void Prune(long now)
        {
            while (m_Order.First != null && (m_Order.Count > MaximumEntries || now - m_Order.First.Value.CommittedAt >= MaximumAgeMilliseconds))
            {
                m_ByCommandId.Remove(m_Order.First.Value.CommandId);
                m_BySequence.Remove(m_Order.First.Value.Sequence);
                m_Order.RemoveFirst();
            }
        }

        private readonly struct Entry
        {
            public Entry(ulong sequence, ContractId commandId, CommandOutcome outcome, long committedAt)
            {
                Sequence = sequence;
                CommandId = commandId;
                Outcome = outcome;
                CommittedAt = committedAt;
            }

            public ulong Sequence { get; }

            public ContractId CommandId { get; }

            public CommandOutcome Outcome { get; }

            public long CommittedAt { get; }
        }
    }

    internal sealed class DeltaJournal
    {
        public const int MaximumEntries = 512;
        public const int MaximumLogicalBytes = 4 * 1024 * 1024;
        public const long MaximumAgeMilliseconds = 5 * 60 * 1000;

        private readonly LinkedList<Entry> m_Entries;
        private int m_LogicalBytes;

        public DeltaJournal()
        {
            m_Entries = new LinkedList<Entry>();
        }

        private DeltaJournal(DeltaJournal source)
        {
            m_Entries = new LinkedList<Entry>(source.m_Entries);
            m_LogicalBytes = source.m_LogicalBytes;
            EvictionCount = source.EvictionCount;
        }

        public int Count => m_Entries.Count;

        public int LogicalBytes => m_LogicalBytes;

        public long EvictionCount { get; private set; }

        public DeltaJournal Clone() => new(this);

        public void Add(StateDelta delta, int logicalBytes, long now)
        {
            if (delta == null)
                throw new ArgumentNullException(nameof(delta));
            if (logicalBytes <= 0 || logicalBytes > MaximumLogicalBytes)
                throw new ArgumentOutOfRangeException(nameof(logicalBytes));
            if (m_Entries.Last != null && m_Entries.Last.Value.Delta.ResultingStateRevision != delta.BaseStateRevision)
                throw new InvalidOperationException("Delta journal entries must be contiguous.");

            m_Entries.AddLast(new Entry(delta, logicalBytes, now));
            m_LogicalBytes = checked(m_LogicalBytes + logicalBytes);
            Prune(now);
        }

        public bool TryGetSince(StateRevision revision, StateRevision current, long now, out IReadOnlyList<StateDelta> deltas)
        {
            Prune(now);
            if (revision > current)
            {
                deltas = Array.Empty<StateDelta>();
                return false;
            }

            if (revision == current)
            {
                deltas = Array.Empty<StateDelta>();
                return true;
            }

            List<StateDelta> result = new();
            StateRevision expected = revision;
            foreach (Entry entry in m_Entries)
            {
                if (entry.Delta.ResultingStateRevision <= revision)
                    continue;
                if (entry.Delta.BaseStateRevision != expected)
                {
                    deltas = Array.Empty<StateDelta>();
                    return false;
                }

                result.Add(entry.Delta);
                expected = entry.Delta.ResultingStateRevision;
            }

            if (expected != current)
            {
                deltas = Array.Empty<StateDelta>();
                return false;
            }

            deltas = result.AsReadOnly();
            return true;
        }

        private void Prune(long now)
        {
            while (m_Entries.First != null && (m_Entries.Count > MaximumEntries || m_LogicalBytes > MaximumLogicalBytes || now - m_Entries.First.Value.CommittedAt >= MaximumAgeMilliseconds))
            {
                m_LogicalBytes -= m_Entries.First.Value.LogicalBytes;
                m_Entries.RemoveFirst();
                EvictionCount++;
            }
        }

        private readonly struct Entry
        {
            public Entry(StateDelta delta, int logicalBytes, long committedAt)
            {
                Delta = delta;
                LogicalBytes = logicalBytes;
                CommittedAt = committedAt;
            }

            public StateDelta Delta { get; }

            public int LogicalBytes { get; }

            public long CommittedAt { get; }
        }
    }
}
