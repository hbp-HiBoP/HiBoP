using System;
using System.IO;
using System.Text;

namespace HBP.Sync
{
    public enum SignalMessageKind : byte
    {
        Subscribe = 1,
        Unsubscribe,
        Begin,
        Resource,
        Manifest,
        Commit,
        Error,
        Acknowledge
    }

    public sealed class SignalMessage
    {
        public SignalMessageKind Kind;
        public Guid Generation;
        public long Revision;
        public string Key = "";
        public int Offset, Total;
        public byte[] Body = Array.Empty<byte>();
    }

    public static class SignalWire
    {
        public const string Capability = "HiBoP-Signals-v2";
        public const ushort BodySchema = 20;
        public const int PartBytes = 1024 * 1024;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("HBSP");
        public static bool IsControl(byte[] bytes) => bytes != null && bytes.Length >= 4 && bytes[0] == Magic[0] && bytes[1] == Magic[1] && bytes[2] == Magic[2] && bytes[3] == Magic[3];

        public static byte[] Encode(SignalMessage message)
        {
            if (message.Generation == Guid.Empty || message.Revision < 0 || !Enum.IsDefined(typeof(SignalMessageKind), message.Kind) || message.Key.Length > 2048 || message.Body.Length > SignalSnapshotCodec.MaximumMetadataBytes) throw new InvalidDataException("Invalid Signals message.");
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(Magic);
            writer.Write((byte)1);
            writer.Write((byte)message.Kind);
            writer.Write(message.Generation.ToByteArray());
            writer.Write(message.Revision);
            byte[] text = Encoding.UTF8.GetBytes(message.Key);
            writer.Write(text.Length);
            writer.Write(text);
            writer.Write(message.Offset);
            writer.Write(message.Total);
            writer.Write(message.Body.Length);
            writer.Write(message.Body);
            return stream.ToArray();
        }

        public static SignalMessage Decode(byte[] bytes)
        {
            if (!IsControl(bytes) || bytes.Length > SignalSnapshotCodec.MaximumMetadataBytes + 4096) throw new InvalidDataException("Invalid Signals message size.");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            reader.ReadBytes(4);
            if (reader.ReadByte() != 1) throw new InvalidDataException("Unsupported Signals version.");
            var message = new SignalMessage { Kind = (SignalMessageKind)reader.ReadByte(), Generation = new Guid(SignalSnapshotCodec.Exact(reader, 16)), Revision = reader.ReadInt64() };
            message.Key = Encoding.UTF8.GetString(SignalSnapshotCodec.Exact(reader, SignalSnapshotCodec.Count(reader, 8192)));
            message.Offset = reader.ReadInt32();
            message.Total = reader.ReadInt32();
            message.Body = SignalSnapshotCodec.Exact(reader, SignalSnapshotCodec.Count(reader, SignalSnapshotCodec.MaximumMetadataBytes));
            if (stream.Position != stream.Length || message.Generation == Guid.Empty || message.Revision < 0 || !Enum.IsDefined(typeof(SignalMessageKind), message.Kind) || message.Offset < 0 || message.Total < 0 || message.Total > SignalSnapshot.DefaultBudgetBytes) throw new InvalidDataException("Invalid Signals message fields.");
            return message;
        }
    }
}
