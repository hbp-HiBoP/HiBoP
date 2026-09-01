using System;

namespace CRNL.HiBoP.Contracts
{
    public readonly struct ContractVersion : IComparable<ContractVersion>, IEquatable<ContractVersion>
    {
        public ContractVersion(ushort major, ushort minor)
        {
            if (major == 0)
                throw new ArgumentOutOfRangeException(nameof(major));

            Major = major;
            Minor = minor;
        }

        public ushort Major { get; }

        public ushort Minor { get; }

        public bool IsValid => Major != 0;

        public static ContractVersion V1 => new(1, 0);

        public int CompareTo(ContractVersion other)
        {
            int majorComparison = Major.CompareTo(other.Major);
            return majorComparison != 0 ? majorComparison : Minor.CompareTo(other.Minor);
        }

        public bool Equals(ContractVersion other) => Major == other.Major && Minor == other.Minor;

        public override bool Equals(object obj) => obj is ContractVersion other && Equals(other);

        public override int GetHashCode() => (Major << 16) | Minor;

        public override string ToString() => $"ContractVersion({Major}.{Minor})";

        public static bool operator ==(ContractVersion left, ContractVersion right) => left.Equals(right);

        public static bool operator !=(ContractVersion left, ContractVersion right) => !left.Equals(right);
    }
}
