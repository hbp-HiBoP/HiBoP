using System;

namespace CRNL.HiBoP.Contracts
{
    /// <summary>
    /// Opaque 128-bit identifier with one canonical byte and text representation.
    /// </summary>
    public readonly struct ContractId : IComparable<ContractId>, IEquatable<ContractId>
    {
        public const int ByteLength = 16;
        public const int TextLength = 32;

        public ContractId(ulong high, ulong low)
        {
            if (high == 0 && low == 0)
                throw new ArgumentException("The zero identifier is reserved and invalid.");

            High = high;
            Low = low;
        }

        public ulong High { get; }

        public ulong Low { get; }

        public bool IsValid => High != 0 || Low != 0;

        public static ContractId FromBytes(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length != ByteLength)
                throw new ArgumentException("An identifier must contain exactly 16 bytes.", nameof(bytes));

            return new ContractId(ReadUInt64BigEndian(bytes, 0), ReadUInt64BigEndian(bytes, 8));
        }

        public static ContractId Parse(string text)
        {
            if (!TryParse(text, out ContractId result))
                throw new FormatException("The identifier must contain 32 hexadecimal characters and must not be zero.");

            return result;
        }

        public static bool TryParse(string text, out ContractId result)
        {
            result = default;
            if (text == null || text.Length != TextLength)
                return false;

            ulong high = 0;
            ulong low = 0;
            for (int index = 0; index < TextLength; index++)
            {
                int digit = HexDigit(text[index]);
                if (digit < 0)
                    return false;

                if (index < 16)
                    high = (high << 4) | (uint)digit;
                else
                    low = (low << 4) | (uint)digit;
            }

            if (high == 0 && low == 0)
                return false;

            result = new ContractId(high, low);
            return true;
        }

        public void WriteBytes(byte[] destination, int offset = 0)
        {
            if (!IsValid)
                throw new InvalidOperationException("The default identifier is invalid.");
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (offset < 0 || destination.Length - offset < ByteLength)
                throw new ArgumentOutOfRangeException(nameof(offset));

            WriteUInt64BigEndian(High, destination, offset);
            WriteUInt64BigEndian(Low, destination, offset + 8);
        }

        public int CompareTo(ContractId other)
        {
            int highComparison = High.CompareTo(other.High);
            return highComparison != 0 ? highComparison : Low.CompareTo(other.Low);
        }

        public bool Equals(ContractId other)
        {
            return High == other.High && Low == other.Low;
        }

        public override bool Equals(object obj)
        {
            return obj is ContractId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (High.GetHashCode() * 397) ^ Low.GetHashCode();
            }
        }

        public override string ToString()
        {
            return High.ToString("x16") + Low.ToString("x16");
        }

        public static bool operator ==(ContractId left, ContractId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContractId left, ContractId right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(ContractId left, ContractId right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(ContractId left, ContractId right)
        {
            return left.CompareTo(right) > 0;
        }

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

    public readonly struct SessionEpoch : IEquatable<SessionEpoch>
    {
        public SessionEpoch(ContractId sessionId, ulong epoch)
        {
            if (!sessionId.IsValid)
                throw new ArgumentException("A valid session identifier is required.", nameof(sessionId));
            if (epoch == 0)
                throw new ArgumentOutOfRangeException(nameof(epoch), "An epoch must be greater than zero.");

            SessionId = sessionId;
            Epoch = epoch;
        }

        public ContractId SessionId { get; }

        public ulong Epoch { get; }

        public bool IsValid => SessionId.IsValid && Epoch != 0;

        public bool Equals(SessionEpoch other)
        {
            return SessionId.Equals(other.SessionId) && Epoch == other.Epoch;
        }

        public override bool Equals(object obj)
        {
            return obj is SessionEpoch other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (SessionId.GetHashCode() * 397) ^ Epoch.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"SessionEpoch(session={SessionId}, epoch={Epoch})";
        }

        public static bool operator ==(SessionEpoch left, SessionEpoch right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SessionEpoch left, SessionEpoch right)
        {
            return !left.Equals(right);
        }
    }
}
