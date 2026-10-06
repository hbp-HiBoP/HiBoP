using System;
using System.IO;
using System.Text;

namespace HBP.Sync
{
    public enum V2SurfaceInflationControlKind : byte
    {
        Request = 1,
        Started = 2,
        Progress = 3,
        Ready = 4,
        Commit = 5,
        Committed = 6,
        Cancel = 7,
        Failed = 8,
        Transition = 9,
        Transitioned = 10
    }

    /// <summary>Session-scoped controls; no native geometry or device-local paths cross the wire.</summary>
    public sealed class V2SurfaceInflationControl
    {
        private readonly byte[] m_Payload;
        public V2SurfaceInflationControlKind Kind { get; }
        public OperationId JobId { get; }
        public ulong Generation { get; }
        public ulong CanonicalSequence { get; }
        public float Progress { get; }
        public byte[] Payload => (byte[])m_Payload.Clone();

        public V2SurfaceInflationControl(V2SurfaceInflationControlKind kind, OperationId jobId, ulong generation, ulong canonicalSequence = 0, byte[] payload = null, float progress = 0)
        {
            if (kind < V2SurfaceInflationControlKind.Request || kind > V2SurfaceInflationControlKind.Transitioned) throw new ArgumentOutOfRangeException(nameof(kind));
            JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
            if (kind == V2SurfaceInflationControlKind.Request ? generation != 0 : generation == 0 && kind != V2SurfaceInflationControlKind.Cancel && kind != V2SurfaceInflationControlKind.Failed) throw new ArgumentOutOfRangeException(nameof(generation));
            if (float.IsNaN(progress) || float.IsInfinity(progress) || progress < 0 || progress > 1 || kind != V2SurfaceInflationControlKind.Progress && progress != 0) throw new ArgumentOutOfRangeException(nameof(progress));
            payload ??= Array.Empty<byte>();
            bool command = kind == V2SurfaceInflationControlKind.Request || kind == V2SurfaceInflationControlKind.Started;
            if (payload.Length > V2SurfaceInflationControlCodec.MaximumPayloadBytes || command != (payload.Length > 0) && kind != V2SurfaceInflationControlKind.Failed) throw new ArgumentException("Invalid inflation control payload.", nameof(payload));
            Kind = kind;
            Generation = generation;
            CanonicalSequence = canonicalSequence;
            Progress = progress;
            m_Payload = (byte[])payload.Clone();
        }
    }

    public static class V2SurfaceInflationControlCodec
    {
        public const int MaximumPayloadBytes = 1536;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("HBSI");

        public static byte[] Encode(V2SurfaceInflationControl control)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(Magic);
            writer.Write((byte)1);
            writer.Write((byte)control.Kind);
            writer.Write(control.JobId.ToByteArray());
            writer.Write(control.Generation);
            writer.Write(control.CanonicalSequence);
            writer.Write(control.Progress);
            byte[] payload = control.Payload;
            writer.Write((ushort)payload.Length);
            writer.Write(payload);
            return stream.ToArray();
        }

        public static bool TryDecode(byte[] bytes, out V2SurfaceInflationControl control)
        {
            control = null;
            if (bytes == null || bytes.Length < 44 || bytes.Length > 44 + MaximumPayloadBytes) return false;
            try
            {
                using var stream = new MemoryStream(bytes, false);
                using var reader = new BinaryReader(stream);
                if (!reader.ReadBytes(4).AsSpan().SequenceEqual(Magic) || reader.ReadByte() != 1) return false;
                var kind = (V2SurfaceInflationControlKind)reader.ReadByte();
                var id = new OperationId(new Guid(reader.ReadBytes(16)));
                ulong generation = reader.ReadUInt64();
                ulong sequence = reader.ReadUInt64();
                float progress = reader.ReadSingle();
                int length = reader.ReadUInt16();
                if (length > MaximumPayloadBytes || length != stream.Length - stream.Position) return false;
                control = new V2SurfaceInflationControl(kind, id, generation, sequence, reader.ReadBytes(length), progress);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException)
            {
                return false;
            }
        }
    }
}
