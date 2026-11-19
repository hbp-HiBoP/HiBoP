using System;
using System.IO;
using System.Text;

namespace HBP.Sync
{
    public enum SceneActionKind : byte
    {
        SelectContext = 1,
        OpenPatient = 2,
        Close = 3,
        Result = 4
    }

    /// <summary>Small reliable UI intentions, explicitly scoped even on the session-control lane.</summary>
    public sealed class SceneAction
    {
        public Guid Operation { get; }
        public SceneId Scene { get; }
        public IncarnationId Incarnation { get; }
        public SceneActionKind Kind { get; }
        public string Column { get; }
        public string Site { get; }
        public string Message { get; }
        public bool Succeeded { get; }

        public SceneAction(SceneId scene, IncarnationId incarnation, SceneActionKind kind, string column = "", string site = "", Guid operation = default, bool succeeded = true, string message = "")
        {
            Scene = scene ?? throw new ArgumentNullException(nameof(scene));
            Incarnation = incarnation ?? throw new ArgumentNullException(nameof(incarnation));
            if (!Enum.IsDefined(typeof(SceneActionKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            Column = column ?? "";
            Site = site ?? "";
            Message = message ?? "";
            if (Column.Length > 1024 || Site.Length > 4096 || Message.Length > 1024) throw new ArgumentException("Scene action exceeds its budget.");
            Kind = kind;
            Operation = operation == Guid.Empty ? Guid.NewGuid() : operation;
            Succeeded = succeeded;
        }

        public byte[] Encode()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(Encoding.ASCII.GetBytes("HBA1"));
            writer.Write((byte)Kind);
            writer.Write(Operation.ToByteArray());
            writer.Write(Scene.Value.ToByteArray());
            writer.Write(Incarnation.Value.ToByteArray());
            writer.Write(Column);
            writer.Write(Site);
            writer.Write(Succeeded);
            writer.Write(Message);
            return stream.ToArray();
        }

        public static bool TryDecode(byte[] bytes, out SceneAction action)
        {
            action = null;
            if (bytes == null || bytes.Length < 4 || Encoding.ASCII.GetString(bytes, 0, 4) != "HBA1") return false;
            if (bytes.Length > 32768) throw new InvalidDataException("Scene action exceeds its budget.");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            reader.ReadBytes(4);
            var kind = (SceneActionKind)reader.ReadByte();
            Guid operation = new(reader.ReadBytes(16));
            var scene = new SceneId(new Guid(reader.ReadBytes(16)));
            var incarnation = new IncarnationId(new Guid(reader.ReadBytes(16)));
            string column = reader.ReadString(), site = reader.ReadString();
            bool succeeded = reader.ReadBoolean();
            string message = reader.ReadString();
            if (stream.Position != stream.Length || operation == Guid.Empty) throw new InvalidDataException("Invalid scene action.");
            action = new(scene, incarnation, kind, column, site, operation, succeeded, message);
            return true;
        }
    }
}
