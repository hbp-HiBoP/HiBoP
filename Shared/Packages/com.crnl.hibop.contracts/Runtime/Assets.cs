using System;

namespace CRNL.HiBoP.Contracts
{
    /// <summary>
    /// Opaque SHA-256 asset identity with canonical big-endian bytes and lowercase hexadecimal text.
    /// </summary>
    public readonly struct AssetHash : IComparable<AssetHash>, IEquatable<AssetHash>
    {
        public const int ByteLength = 32;
        public const int TextLength = 64;

        private readonly ulong m_First;
        private readonly ulong m_Second;
        private readonly ulong m_Third;
        private readonly ulong m_Fourth;

        public AssetHash(ulong first, ulong second, ulong third, ulong fourth)
        {
            if ((first | second | third | fourth) == 0)
                throw new ArgumentException("The zero asset hash is reserved and invalid.");

            m_First = first;
            m_Second = second;
            m_Third = third;
            m_Fourth = fourth;
        }

        public bool IsValid => (m_First | m_Second | m_Third | m_Fourth) != 0;

        public static AssetHash FromBytes(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length != ByteLength)
                throw new ArgumentException("An asset hash must contain exactly 32 bytes.", nameof(bytes));

            return new AssetHash(ReadUInt64BigEndian(bytes, 0), ReadUInt64BigEndian(bytes, 8), ReadUInt64BigEndian(bytes, 16), ReadUInt64BigEndian(bytes, 24));
        }

        public static AssetHash Parse(string text)
        {
            if (!TryParse(text, out AssetHash result))
                throw new FormatException("The asset hash must contain 64 hexadecimal characters and must not be zero.");

            return result;
        }

        public static bool TryParse(string text, out AssetHash result)
        {
            result = default;
            if (text == null || text.Length != TextLength)
                return false;

            ulong[] parts = new ulong[4];
            for (int index = 0; index < TextLength; index++)
            {
                int digit = HexDigit(text[index]);
                if (digit < 0)
                    return false;

                int partIndex = index / 16;
                parts[partIndex] = (parts[partIndex] << 4) | (uint)digit;
            }

            if ((parts[0] | parts[1] | parts[2] | parts[3]) == 0)
                return false;

            result = new AssetHash(parts[0], parts[1], parts[2], parts[3]);
            return true;
        }

        public void WriteBytes(byte[] destination, int offset = 0)
        {
            if (!IsValid)
                throw new InvalidOperationException("The default asset hash is invalid.");
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (offset < 0 || destination.Length - offset < ByteLength)
                throw new ArgumentOutOfRangeException(nameof(offset));

            WriteUInt64BigEndian(m_First, destination, offset);
            WriteUInt64BigEndian(m_Second, destination, offset + 8);
            WriteUInt64BigEndian(m_Third, destination, offset + 16);
            WriteUInt64BigEndian(m_Fourth, destination, offset + 24);
        }

        public int CompareTo(AssetHash other)
        {
            int comparison = m_First.CompareTo(other.m_First);
            if (comparison != 0)
                return comparison;
            comparison = m_Second.CompareTo(other.m_Second);
            if (comparison != 0)
                return comparison;
            comparison = m_Third.CompareTo(other.m_Third);
            return comparison != 0 ? comparison : m_Fourth.CompareTo(other.m_Fourth);
        }

        public bool Equals(AssetHash other)
        {
            return m_First == other.m_First && m_Second == other.m_Second && m_Third == other.m_Third && m_Fourth == other.m_Fourth;
        }

        public override bool Equals(object obj) => obj is AssetHash other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = m_First.GetHashCode();
                hash = (hash * 397) ^ m_Second.GetHashCode();
                hash = (hash * 397) ^ m_Third.GetHashCode();
                return (hash * 397) ^ m_Fourth.GetHashCode();
            }
        }

        public override string ToString()
        {
            return m_First.ToString("x16") + m_Second.ToString("x16") + m_Third.ToString("x16") + m_Fourth.ToString("x16");
        }

        public static bool operator ==(AssetHash left, AssetHash right) => left.Equals(right);

        public static bool operator !=(AssetHash left, AssetHash right) => !left.Equals(right);

        private static int HexDigit(char character)
        {
            if (character >= '0' && character <= '9')
                return character - '0';
            if (character >= 'a' && character <= 'f')
                return character - 'a' + 10;
            if (character >= 'A' && character <= 'F')
                return character - 'A' + 10;
            return -1;
        }

        private static ulong ReadUInt64BigEndian(byte[] bytes, int offset)
        {
            ulong value = 0;
            for (int index = 0; index < 8; index++)
                value = (value << 8) | bytes[offset + index];
            return value;
        }

        private static void WriteUInt64BigEndian(ulong value, byte[] destination, int offset)
        {
            for (int index = 7; index >= 0; index--)
            {
                destination[offset + index] = (byte)value;
                value >>= 8;
            }
        }
    }

    public sealed class AssetReference : IComparable<AssetReference>, IEquatable<AssetReference>
    {
        public AssetReference(ContractId assetId, AssetHash hash, ushort schemaVersion)
        {
            if (!assetId.IsValid)
                throw new ArgumentException("A valid asset identifier is required.", nameof(assetId));
            if (!hash.IsValid)
                throw new ArgumentException("A valid asset hash is required.", nameof(hash));
            if (schemaVersion == 0)
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));

            AssetId = assetId;
            Hash = hash;
            SchemaVersion = schemaVersion;
        }

        public ContractId AssetId { get; }

        public AssetHash Hash { get; }

        public ushort SchemaVersion { get; }

        public int CompareTo(AssetReference other)
        {
            if (ReferenceEquals(other, null))
                return 1;
            int comparison = Hash.CompareTo(other.Hash);
            return comparison != 0 ? comparison : AssetId.CompareTo(other.AssetId);
        }

        public bool Equals(AssetReference other)
        {
            return !ReferenceEquals(other, null) && AssetId.Equals(other.AssetId) && Hash.Equals(other.Hash) && SchemaVersion == other.SchemaVersion;
        }

        public override bool Equals(object obj) => Equals(obj as AssetReference);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = AssetId.GetHashCode();
                hash = (hash * 397) ^ Hash.GetHashCode();
                return (hash * 397) ^ SchemaVersion.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"AssetReference(id={AssetId}, hash={Hash}, schemaVersion={SchemaVersion})";
        }
    }
}
