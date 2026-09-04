using System;
using System.Collections.Generic;

namespace CRNL.HiBoP.Contracts
{
    public readonly struct Optional<T> : IEquatable<Optional<T>>
    {
        private readonly T m_Value;

        public Optional(T value)
        {
            if (ReferenceEquals(value, null))
                throw new ArgumentNullException(nameof(value));

            m_Value = value;
            HasValue = true;
        }

        public bool HasValue { get; }

        public T Value => HasValue ? m_Value : throw new InvalidOperationException("The optional value is absent.");

        public static Optional<T> None => default;

        public static Optional<T> Some(T value)
        {
            return new Optional<T>(value);
        }

        public bool Equals(Optional<T> other)
        {
            if (HasValue != other.HasValue)
                return false;
            return !HasValue || EqualityComparer<T>.Default.Equals(m_Value, other.m_Value);
        }

        public override bool Equals(object obj)
        {
            return obj is Optional<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HasValue ? EqualityComparer<T>.Default.GetHashCode(m_Value) : 0;
        }

        public override string ToString()
        {
            return HasValue ? $"Optional<{typeof(T).Name}>(present)" : $"Optional<{typeof(T).Name}>(absent)";
        }

        public static bool operator ==(Optional<T> left, Optional<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Optional<T> left, Optional<T> right)
        {
            return !left.Equals(right);
        }
    }
}
