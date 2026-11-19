using System;
using System.IO;
using System.Text;

namespace HBP.Sync
{
    public enum CorrelationControlKind : byte
    {
        Request = 1,
        Started = 2,
        Cancel = 3,
        Ready = 4,
        Failed = 5
    }

    public sealed class CorrelationControl
    {
        private readonly byte[] m_Command;
        public CorrelationControlKind Kind { get; }
        public OperationId JobId { get; }
        public ulong Generation { get; }
        public string FailureCode { get; }
        public byte[] Command => (byte[])m_Command.Clone();

        public CorrelationControl(CorrelationControlKind kind, OperationId jobId, ulong generation, byte[] command = null, string failureCode = null)
        {
            if (kind < CorrelationControlKind.Request || kind > CorrelationControlKind.Failed) throw new ArgumentOutOfRangeException(nameof(kind));
            JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
            if (kind == CorrelationControlKind.Request ? generation != 0 : kind is not CorrelationControlKind.Cancel and not CorrelationControlKind.Failed && generation == 0) throw new ArgumentOutOfRangeException(nameof(generation));
            command ??= Array.Empty<byte>();
            if (command.Length > CorrelationControlCodec.MaximumCommandBytes) throw new ArgumentOutOfRangeException(nameof(command));
            if ((kind == CorrelationControlKind.Request) != (command.Length > 0)) throw new ArgumentException("Only a correlation request control carries a command.", nameof(command));
            if (kind == CorrelationControlKind.Failed)
            {
                if (string.IsNullOrWhiteSpace(failureCode)) throw new ArgumentException("A failed correlation job requires a failure code.", nameof(failureCode));
                if (CorrelationControlCodec.Utf8.GetByteCount(failureCode) > CorrelationControlCodec.MaximumFailureCodeBytes) throw new ArgumentOutOfRangeException(nameof(failureCode));
                FailureCode = failureCode;
            }
            else if (failureCode != null) throw new ArgumentException("Only a failed correlation control carries a failure code.", nameof(failureCode));

            Kind = kind;
            Generation = generation;
            m_Command = (byte[])command.Clone();
        }
    }

    /// <summary>Bounded control records; large result bytes travel only as bulk-capable scene mutations.</summary>
    public static class CorrelationControlCodec
    {
        public const int MaximumCommandBytes = 64;
        public const int MaximumFailureCodeBytes = 128;
        public const int MaximumPayloadBytes = 256;
        private const ushort SchemaVersion = 1;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("HBCJ");
        internal static readonly UTF8Encoding Utf8 = new(false, true);

        public static byte[] Encode(CorrelationControl control)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
            byte[] command = control.Command;
            byte[] failure = control.FailureCode == null ? Array.Empty<byte>() : Utf8.GetBytes(control.FailureCode);
            using var stream = new MemoryStream(35 + command.Length + failure.Length);
            using var writer = new BinaryWriter(stream, Utf8, true);
            writer.Write(Magic);
            writer.Write(SchemaVersion);
            writer.Write((byte)control.Kind);
            writer.Write(control.JobId.ToByteArray());
            writer.Write(control.Generation);
            writer.Write(checked((uint)command.Length));
            writer.Write(checked((ushort)failure.Length));
            writer.Write(command);
            writer.Write(failure);
            writer.Flush();
            if (stream.Length > MaximumPayloadBytes) throw new InvalidDataException("T13 correlation control exceeds its session-control bound.");
            return stream.ToArray();
        }

        public static bool TryDecode(byte[] bytes, out CorrelationControl control)
        {
            control = null;
            if (bytes == null || bytes.Length < 37 || bytes.Length > MaximumPayloadBytes) return false;
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Utf8, true);
            try
            {
                if (!reader.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic) || reader.ReadUInt16() != SchemaVersion) return false;
                byte kind = reader.ReadByte();
                if (kind < (byte)CorrelationControlKind.Request || kind > (byte)CorrelationControlKind.Failed) return false;
                byte[] jobBytes = reader.ReadBytes(16);
                if (jobBytes.Length != 16) return false;
                ulong generation = reader.ReadUInt64();
                uint commandLength = reader.ReadUInt32();
                ushort failureLength = reader.ReadUInt16();
                if (commandLength > MaximumCommandBytes || failureLength > MaximumFailureCodeBytes || commandLength + failureLength != stream.Length - stream.Position) return false;
                byte[] command = reader.ReadBytes(checked((int)commandLength));
                byte[] failureBytes = reader.ReadBytes(failureLength);
                if (command.Length != commandLength || failureBytes.Length != failureLength) return false;
                string failure = failureLength == 0 ? null : Utf8.GetString(failureBytes);
                control = new CorrelationControl((CorrelationControlKind)kind, new OperationId(new Guid(jobBytes)), generation, command, failure);
                return true;
            }
            catch (Exception exception) when (exception is EndOfStreamException || exception is ArgumentException || exception is DecoderFallbackException || exception is OverflowException)
            {
                return false;
            }
        }
    }
}
