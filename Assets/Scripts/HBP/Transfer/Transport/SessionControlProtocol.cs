using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HBP.Transfer.Transport
{
    public enum SessionControlKind : byte
    {
        Open = 1,
        Preferences = 2,
        LoadAtlas = 3,
        PrepareUnload = 4,
        CommitUnload = 5,
        AbortUnload = 6,
        ConfirmAtlas = 7,
        Inspect = 8,
        AtlasInventory = 9
    }

    public enum SessionControlStatus : byte
    {
        Applied,
        Rejected,
        Failed,
        Stale
    }

    public sealed class SessionControlRequest
    {
        public Guid ContextId { get; }
        public long Generation { get; }
        public Guid OperationId { get; }
        public SessionControlKind Kind { get; }
        public long Revision { get; }
        public string AtlasId { get; }
        public string Fingerprint { get; }
        private readonly byte[] m_Body;
        public byte[] GetBody() => (byte[])m_Body.Clone();

        public SessionControlRequest(Guid contextId, long generation, Guid operationId, SessionControlKind kind, long revision = 0, string atlasId = "", string fingerprint = "", byte[] body = null)
        {
            if (contextId == Guid.Empty || operationId == Guid.Empty || generation < 1 || revision < 0 || !Enum.IsDefined(typeof(SessionControlKind), kind)) throw new ArgumentException("Invalid session control identity.");
            if ((atlasId?.Length ?? 0) > 128 || (fingerprint?.Length ?? 0) > 64 || (body?.Length ?? 0) > SessionControlCodec.MaximumBodyBytes) throw new ArgumentException("Session control exceeds its budget.");
            ContextId = contextId;
            Generation = generation;
            OperationId = operationId;
            Kind = kind;
            Revision = revision;
            AtlasId = atlasId ?? "";
            Fingerprint = fingerprint ?? "";
            m_Body = body == null ? Array.Empty<byte>() : (byte[])body.Clone();
        }
    }

    public sealed class SessionControlResponse
    {
        public Guid OperationId { get; }
        public SessionControlStatus Status { get; }
        public string Message { get; }
        public string Fingerprint { get; }
        public bool HasSharedScene { get; }
        private readonly byte[] m_Body;
        public byte[] GetBody() => (byte[])m_Body.Clone();
        public bool Applied => Status == SessionControlStatus.Applied;

        public SessionControlResponse(Guid operationId, SessionControlStatus status, string message = "", string fingerprint = "", bool hasSharedScene = false, byte[] body = null)
        {
            if (operationId == Guid.Empty || !Enum.IsDefined(typeof(SessionControlStatus), status) || (body?.Length ?? 0) > SessionControlCodec.MaximumBodyBytes) throw new ArgumentException("Invalid session control result.");
            OperationId = operationId;
            Status = status;
            Message = (message ?? "").Length > 1024 ? message.Substring(0, 1024) : message ?? "";
            Fingerprint = fingerprint ?? "";
            HasSharedScene = hasSharedScene;
            m_Body = body == null ? Array.Empty<byte>() : (byte[])body.Clone();
        }
    }

    /// <summary>Bounded request/response framing, carried only by the authenticated pairing endpoint.</summary>
    public static class SessionControlCodec
    {
        public const string AtlasInventoryCapability = "HiBoP-atlas-inventory-v1";
        public const int MaximumBodyBytes = 1024 * 1024;
        private const int Version = 1;

        public static byte[] Encode(SessionControlRequest request)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(Version);
            writer.Write(request.ContextId.ToByteArray());
            writer.Write(request.Generation);
            writer.Write(request.OperationId.ToByteArray());
            writer.Write((byte)request.Kind);
            writer.Write(request.Revision);
            writer.Write(request.AtlasId);
            writer.Write(request.Fingerprint);
            byte[] body = request.GetBody();
            writer.Write(body.Length);
            writer.Write(body);
            return stream.ToArray();
        }

        public static SessionControlRequest DecodeRequest(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            if (reader.ReadInt32() != Version) throw new InvalidDataException("Unsupported session control version. Update both applications.");
            Guid context = ReadGuid(reader);
            long generation = reader.ReadInt64();
            Guid operation = ReadGuid(reader);
            var kind = (SessionControlKind)reader.ReadByte();
            long revision = reader.ReadInt64();
            string atlas = ReadText(reader, 128), fingerprint = ReadText(reader, 64);
            byte[] body = ReadBody(reader);
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing session control bytes.");
            return new(context, generation, operation, kind, revision, atlas, fingerprint, body);
        }

        public static byte[] Encode(SessionControlResponse response)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(Version);
            writer.Write(response.OperationId.ToByteArray());
            writer.Write((byte)response.Status);
            writer.Write(response.Message);
            writer.Write(response.Fingerprint);
            writer.Write(response.HasSharedScene);
            byte[] body = response.GetBody();
            writer.Write(body.Length);
            writer.Write(body);
            return stream.ToArray();
        }

        public static SessionControlResponse DecodeResponse(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            if (reader.ReadInt32() != Version) throw new InvalidDataException("Unsupported session control response.");
            Guid operation = ReadGuid(reader);
            var status = (SessionControlStatus)reader.ReadByte();
            string message = ReadText(reader, 1024), fingerprint = ReadText(reader, 64);
            bool scene = reader.ReadBoolean();
            byte[] body = ReadBody(reader);
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing session response bytes.");
            return new(operation, status, message, fingerprint, scene, body);
        }

        private static Guid ReadGuid(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(16);
            if (bytes.Length != 16) throw new EndOfStreamException();
            return new(bytes);
        }

        private static string ReadText(BinaryReader reader, int maximum)
        {
            string value = reader.ReadString();
            if (value.Length > maximum) throw new InvalidDataException("Session text exceeds its budget.");
            return value;
        }

        private static byte[] ReadBody(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length < 0 || length > MaximumBodyBytes) throw new InvalidDataException("Session body exceeds its budget.");
            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length != length) throw new EndOfStreamException();
            return bytes;
        }

        public static async Task WriteAsync(Stream stream, byte[] body, CancellationToken token)
        {
            if (body.Length < 1 || body.Length > MaximumBodyBytes + 4096) throw new InvalidDataException("Session frame exceeds its budget.");
            byte[] length = BitConverter.GetBytes(body.Length);
            await stream.WriteAsync(length, 0, 4, token).ConfigureAwait(false);
            await stream.WriteAsync(body, 0, body.Length, token).ConfigureAwait(false);
        }

        public static async Task<byte[]> ReadAsync(Stream stream, CancellationToken token)
        {
            byte[] header = new byte[4];
            await PinnedTlsTransfer.ReadExactAsync(stream, header, 0, 4, token).ConfigureAwait(false);
            int length = BitConverter.ToInt32(header, 0);
            if (length < 1 || length > MaximumBodyBytes + 4096) throw new InvalidDataException("Session frame exceeds its budget.");
            byte[] body = new byte[length];
            await PinnedTlsTransfer.ReadExactAsync(stream, body, 0, length, token).ConfigureAwait(false);
            return body;
        }
    }
}
