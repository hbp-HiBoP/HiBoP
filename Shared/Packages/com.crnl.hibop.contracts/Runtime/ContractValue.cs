using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CRNL.HiBoP.Contracts
{
    public enum ContractValueKind : byte
    {
        None = 0,
        Boolean = 1,
        SignedInteger = 2,
        UnsignedInteger = 3,
        Number = 4,
        Id = 5,
        NumberVector = 6,
        IdList = 7,
    }

    /// <summary>
    /// Serializer-independent logical value used by commands, snapshots and deltas.
    /// </summary>
    public sealed class ContractValue : IEquatable<ContractValue>
    {
        private static readonly ReadOnlyCollection<double> s_EmptyNumbers = Array.AsReadOnly(Array.Empty<double>());
        private static readonly ReadOnlyCollection<ContractId> s_EmptyIds = Array.AsReadOnly(Array.Empty<ContractId>());

        private readonly bool m_Boolean;
        private readonly ContractId m_Id;
        private readonly ReadOnlyCollection<ContractId> m_Ids;
        private readonly double m_Number;
        private readonly ReadOnlyCollection<double> m_Numbers;
        private readonly long m_SignedInteger;
        private readonly ulong m_UnsignedInteger;

        private ContractValue(ContractValueKind kind, bool boolean, long signedInteger, ulong unsignedInteger, double number, ContractId id, ReadOnlyCollection<double> numbers, ReadOnlyCollection<ContractId> ids)
        {
            Kind = kind;
            m_Boolean = boolean;
            m_SignedInteger = signedInteger;
            m_UnsignedInteger = unsignedInteger;
            m_Number = number;
            m_Id = id;
            m_Numbers = numbers ?? s_EmptyNumbers;
            m_Ids = ids ?? s_EmptyIds;
        }

        public static ContractValue None { get; } = new(ContractValueKind.None, false, 0, 0, 0, default, null, null);

        public ContractValueKind Kind { get; }

        public bool Boolean
        {
            get
            {
                EnsureKind(ContractValueKind.Boolean);
                return m_Boolean;
            }
        }

        public long SignedInteger
        {
            get
            {
                EnsureKind(ContractValueKind.SignedInteger);
                return m_SignedInteger;
            }
        }

        public ulong UnsignedInteger
        {
            get
            {
                EnsureKind(ContractValueKind.UnsignedInteger);
                return m_UnsignedInteger;
            }
        }

        public double Number
        {
            get
            {
                EnsureKind(ContractValueKind.Number);
                return m_Number;
            }
        }

        public ContractId Id
        {
            get
            {
                EnsureKind(ContractValueKind.Id);
                return m_Id;
            }
        }

        public IReadOnlyList<double> Numbers
        {
            get
            {
                EnsureKind(ContractValueKind.NumberVector);
                return m_Numbers;
            }
        }

        public IReadOnlyList<ContractId> Ids
        {
            get
            {
                EnsureKind(ContractValueKind.IdList);
                return m_Ids;
            }
        }

        public static ContractValue FromBoolean(bool value)
        {
            return new ContractValue(ContractValueKind.Boolean, value, 0, 0, 0, default, null, null);
        }

        public static ContractValue FromSignedInteger(long value)
        {
            return new ContractValue(ContractValueKind.SignedInteger, false, value, 0, 0, default, null, null);
        }

        public static ContractValue FromUnsignedInteger(ulong value)
        {
            return new ContractValue(ContractValueKind.UnsignedInteger, false, 0, value, 0, default, null, null);
        }

        public static ContractValue FromNumber(double value)
        {
            EnsureFinite(value, nameof(value));
            return new ContractValue(ContractValueKind.Number, false, 0, 0, value, default, null, null);
        }

        public static ContractValue FromId(ContractId value)
        {
            if (!value.IsValid)
                throw new ArgumentException("A valid identifier is required.", nameof(value));
            return new ContractValue(ContractValueKind.Id, false, 0, 0, 0, value, null, null);
        }

        public static ContractValue FromNumbers(IEnumerable<double> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            List<double> copy = new();
            foreach (double value in values)
            {
                EnsureFinite(value, nameof(values));
                copy.Add(value);
            }

            return new ContractValue(ContractValueKind.NumberVector, false, 0, 0, 0, default, copy.AsReadOnly(), null);
        }

        public static ContractValue FromIds(IEnumerable<ContractId> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            List<ContractId> copy = new();
            foreach (ContractId value in values)
            {
                if (!value.IsValid)
                    throw new ArgumentException("Every identifier must be valid.", nameof(values));
                copy.Add(value);
            }

            return new ContractValue(ContractValueKind.IdList, false, 0, 0, 0, default, null, copy.AsReadOnly());
        }

        public bool Equals(ContractValue other)
        {
            if (ReferenceEquals(other, null) || Kind != other.Kind)
                return false;

            switch (Kind)
            {
                case ContractValueKind.None:
                    return true;
                case ContractValueKind.Boolean:
                    return m_Boolean == other.m_Boolean;
                case ContractValueKind.SignedInteger:
                    return m_SignedInteger == other.m_SignedInteger;
                case ContractValueKind.UnsignedInteger:
                    return m_UnsignedInteger == other.m_UnsignedInteger;
                case ContractValueKind.Number:
                    return m_Number.Equals(other.m_Number);
                case ContractValueKind.Id:
                    return m_Id.Equals(other.m_Id);
                case ContractValueKind.NumberVector:
                    return SequenceEqual(m_Numbers, other.m_Numbers);
                case ContractValueKind.IdList:
                    return SequenceEqual(m_Ids, other.m_Ids);
                default:
                    return false;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ContractValue);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                switch (Kind)
                {
                    case ContractValueKind.Boolean:
                        return (hash * 397) ^ m_Boolean.GetHashCode();
                    case ContractValueKind.SignedInteger:
                        return (hash * 397) ^ m_SignedInteger.GetHashCode();
                    case ContractValueKind.UnsignedInteger:
                        return (hash * 397) ^ m_UnsignedInteger.GetHashCode();
                    case ContractValueKind.Number:
                        return (hash * 397) ^ m_Number.GetHashCode();
                    case ContractValueKind.Id:
                        return (hash * 397) ^ m_Id.GetHashCode();
                    case ContractValueKind.NumberVector:
                        return SequenceHash(hash, m_Numbers);
                    case ContractValueKind.IdList:
                        return SequenceHash(hash, m_Ids);
                    default:
                        return hash;
                }
            }
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case ContractValueKind.NumberVector:
                    return $"ContractValue(kind={Kind}, count={m_Numbers.Count})";
                case ContractValueKind.IdList:
                    return $"ContractValue(kind={Kind}, count={m_Ids.Count})";
                default:
                    return $"ContractValue(kind={Kind})";
            }
        }

        private static void EnsureFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName, "Contract numbers must be finite.");
        }

        private void EnsureKind(ContractValueKind expected)
        {
            if (Kind != expected)
                throw new InvalidOperationException($"A {Kind} contract value cannot be read as {expected}.");
        }

        private static bool SequenceEqual<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
        {
            if (left.Count != right.Count)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int index = 0; index < left.Count; index++)
            {
                if (!comparer.Equals(left[index], right[index]))
                    return false;
            }

            return true;
        }

        private static int SequenceHash<T>(int seed, IReadOnlyList<T> values)
        {
            unchecked
            {
                int hash = seed;
                EqualityComparer<T> comparer = EqualityComparer<T>.Default;
                for (int index = 0; index < values.Count; index++)
                    hash = (hash * 397) ^ comparer.GetHashCode(values[index]);
                return hash;
            }
        }
    }
}
