using System;
using System.IO;
using System.Text;

namespace HBP.Sync
{
    public enum SiteFilterControlKind : byte
    {
        Request = 1,
        Started = 2,
        Cancel = 3,
        Ready = 4,
        Failed = 5
    }

    public sealed class SiteFilterControl
    {
        private readonly byte[] m_Payload;

        public SiteFilterControlKind Kind { get; }
        public OperationId JobId { get; }
        public ulong Generation { get; }
        public string FailureCode { get; }
        public byte[] Payload => (byte[])m_Payload.Clone();

        public SiteFilterControl(SiteFilterControlKind kind, OperationId jobId, ulong generation, byte[] payload = null, string failureCode = null)
        {
            if (kind < SiteFilterControlKind.Request || kind > SiteFilterControlKind.Failed) throw new ArgumentOutOfRangeException(nameof(kind));
            JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
            if (kind == SiteFilterControlKind.Request ? generation != 0 : kind is not SiteFilterControlKind.Cancel and not SiteFilterControlKind.Failed && generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            payload ??= Array.Empty<byte>();
            if (payload.Length > SiteFilterControlCodec.MaximumPayloadBytes) throw new ArgumentOutOfRangeException(nameof(payload));
            if ((kind == SiteFilterControlKind.Request) != (payload.Length > 0)) throw new ArgumentException("Only a request control carries command parameters.", nameof(payload));
            if (kind == SiteFilterControlKind.Failed)
            {
                if (string.IsNullOrWhiteSpace(failureCode)) throw new ArgumentException("A failed filter job requires a failure code.", nameof(failureCode));
                byte[] encoded = SiteFilterControlCodec.Utf8.GetBytes(failureCode);
                if (encoded.Length > SiteFilterControlCodec.MaximumFailureCodeBytes) throw new ArgumentOutOfRangeException(nameof(failureCode));
                FailureCode = failureCode;
            }
            else if (failureCode != null)
            {
                throw new ArgumentException("Only a failed filter job carries a failure code.", nameof(failureCode));
            }

            Kind = kind;
            Generation = generation;
            m_Payload = (byte[])payload.Clone();
        }
    }

    /// <summary>Bounded session-control messages for starting, cancelling and acknowledging a T12 job.</summary>
    public static class SiteFilterControlCodec
    {
        public const int MaximumPayloadBytes = 48 * 1024;
        public const int MaximumFailureCodeBytes = 128;
        private const ushort SchemaVersion = 1;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("HBFJ");
        internal static readonly UTF8Encoding Utf8 = new(false, true);

        public static byte[] Encode(SiteFilterControl control)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
            byte[] payload = control.Payload;
            byte[] failure = control.FailureCode == null ? Array.Empty<byte>() : Utf8.GetBytes(control.FailureCode);
            using var stream = new MemoryStream(35 + payload.Length + failure.Length);
            using var writer = new BinaryWriter(stream, Utf8, true);
            writer.Write(Magic);
            writer.Write(SchemaVersion);
            writer.Write((byte)control.Kind);
            writer.Write(control.JobId.ToByteArray());
            writer.Write(control.Generation);
            writer.Write(checked((uint)payload.Length));
            writer.Write(checked((ushort)failure.Length));
            writer.Write(payload);
            writer.Write(failure);
            writer.Flush();
            if (stream.Length > MaximumPayloadBytes) throw new InvalidDataException("T12 filter control exceeds its session-control bound.");
            return stream.ToArray();
        }

        public static bool TryDecode(byte[] bytes, out SiteFilterControl control)
        {
            control = null;
            if (bytes == null || bytes.Length < 35 || bytes.Length > MaximumPayloadBytes) return false;
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Utf8, true);
            try
            {
                if (!reader.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic) || reader.ReadUInt16() != SchemaVersion) return false;
                byte kindValue = reader.ReadByte();
                if (kindValue < (byte)SiteFilterControlKind.Request || kindValue > (byte)SiteFilterControlKind.Failed) return false;
                byte[] jobBytes = reader.ReadBytes(16);
                if (jobBytes.Length != 16) return false;
                ulong generation = reader.ReadUInt64();
                uint payloadLength = reader.ReadUInt32();
                ushort failureLength = reader.ReadUInt16();
                if (payloadLength > MaximumPayloadBytes || failureLength > MaximumFailureCodeBytes || payloadLength + failureLength != stream.Length - stream.Position) return false;
                byte[] payload = reader.ReadBytes(checked((int)payloadLength));
                byte[] failureBytes = reader.ReadBytes(failureLength);
                if (payload.Length != payloadLength || failureBytes.Length != failureLength) return false;
                string failure = failureLength == 0 ? null : Utf8.GetString(failureBytes);
                control = new SiteFilterControl((SiteFilterControlKind)kindValue, new OperationId(new Guid(jobBytes)), generation, payload, failure);
                return true;
            }
            catch (Exception exception) when (exception is EndOfStreamException || exception is ArgumentException || exception is DecoderFallbackException || exception is OverflowException)
            {
                return false;
            }
        }
    }
}
