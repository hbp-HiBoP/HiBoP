using System;

namespace CRNL.HiBoP.Contracts
{
    public readonly struct StateRevision : IComparable<StateRevision>, IEquatable<StateRevision>
    {
        public StateRevision(ulong value)
        {
            Value = value;
        }

        public ulong Value { get; }

        public StateRevision Next()
        {
            if (Value == ulong.MaxValue)
                throw new OverflowException("The state revision cannot advance past UInt64.MaxValue.");
            return new StateRevision(Value + 1);
        }

        public int CompareTo(StateRevision other) => Value.CompareTo(other.Value);

        public bool Equals(StateRevision other) => Value == other.Value;

        public override bool Equals(object obj) => obj is StateRevision other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => $"StateRevision({Value})";

        public static bool operator ==(StateRevision left, StateRevision right) => left.Equals(right);

        public static bool operator !=(StateRevision left, StateRevision right) => !left.Equals(right);

        public static bool operator <(StateRevision left, StateRevision right) => left.Value < right.Value;

        public static bool operator >(StateRevision left, StateRevision right) => left.Value > right.Value;

        public static bool operator <=(StateRevision left, StateRevision right) => left.Value <= right.Value;

        public static bool operator >=(StateRevision left, StateRevision right) => left.Value >= right.Value;
    }

    public readonly struct ScopeRevision : IComparable<ScopeRevision>, IEquatable<ScopeRevision>
    {
        public ScopeRevision(ulong value)
        {
            Value = value;
        }

        public ulong Value { get; }

        public ScopeRevision Next()
        {
            if (Value == ulong.MaxValue)
                throw new OverflowException("The scope revision cannot advance past UInt64.MaxValue.");
            return new ScopeRevision(Value + 1);
        }

        public int CompareTo(ScopeRevision other) => Value.CompareTo(other.Value);

        public bool Equals(ScopeRevision other) => Value == other.Value;

        public override bool Equals(object obj) => obj is ScopeRevision other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => $"ScopeRevision({Value})";

        public static bool operator ==(ScopeRevision left, ScopeRevision right) => left.Equals(right);

        public static bool operator !=(ScopeRevision left, ScopeRevision right) => !left.Equals(right);

        public static bool operator <(ScopeRevision left, ScopeRevision right) => left.Value < right.Value;

        public static bool operator >(ScopeRevision left, ScopeRevision right) => left.Value > right.Value;

        public static bool operator <=(ScopeRevision left, ScopeRevision right) => left.Value <= right.Value;

        public static bool operator >=(ScopeRevision left, ScopeRevision right) => left.Value >= right.Value;
    }

    public readonly struct InteractionSequence : IComparable<InteractionSequence>, IEquatable<InteractionSequence>
    {
        public InteractionSequence(ulong value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public ulong Value { get; }

        public bool IsValid => Value != 0;

        public InteractionSequence Next()
        {
            if (Value == ulong.MaxValue)
                throw new OverflowException("The interaction sequence cannot advance past UInt64.MaxValue.");
            return new InteractionSequence(Value + 1);
        }

        public int CompareTo(InteractionSequence other) => Value.CompareTo(other.Value);

        public bool Equals(InteractionSequence other) => Value == other.Value;

        public override bool Equals(object obj) => obj is InteractionSequence other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => $"InteractionSequence({Value})";

        public static bool operator ==(InteractionSequence left, InteractionSequence right) => left.Equals(right);

        public static bool operator !=(InteractionSequence left, InteractionSequence right) => !left.Equals(right);
    }
}
