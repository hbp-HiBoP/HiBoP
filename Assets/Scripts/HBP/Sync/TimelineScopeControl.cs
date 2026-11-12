using System;
using System.IO;
using System.Text;

namespace HBP.Sync
{
    /// <summary>Quest's effective Global mode for one live visualization.</summary>
    public readonly struct TimelineScopeControl
    {
        public const string Capability = "timeline-scope-v1";
        private const string Magic = "HBL1";
        private const int PayloadLength = 37;

        public SceneId Scene { get; }
        public IncarnationId Incarnation { get; }
        public bool Global { get; }

        public TimelineScopeControl(SceneId scene, IncarnationId incarnation, bool global)
        {
            Scene = scene ?? throw new ArgumentNullException(nameof(scene));
            Incarnation = incarnation ?? throw new ArgumentNullException(nameof(incarnation));
            Global = global;
        }

        public byte[] Encode()
        {
            using var stream = new MemoryStream(PayloadLength);
            using var writer = new BinaryWriter(stream);
            writer.Write(Encoding.ASCII.GetBytes(Magic));
            writer.Write(Scene.Value.ToByteArray());
            writer.Write(Incarnation.Value.ToByteArray());
            writer.Write(Global);
            return stream.ToArray();
        }

        public static bool TryDecode(byte[] bytes, out TimelineScopeControl control)
        {
            control = default;
            if (bytes == null || bytes.Length < Magic.Length || Encoding.ASCII.GetString(bytes, 0, Magic.Length) != Magic) return false;
            if (bytes.Length != PayloadLength || bytes[36] > 1) throw new InvalidDataException("Invalid timeline scope control.");
            using var reader = new BinaryReader(new MemoryStream(bytes, false));
            reader.ReadBytes(Magic.Length);
            control = new(new SceneId(new Guid(reader.ReadBytes(16))), new IncarnationId(new Guid(reader.ReadBytes(16))), reader.ReadBoolean());
            return true;
        }

        public bool Matches(SceneId scene, IncarnationId incarnation) => Scene.Equals(scene) && Incarnation.Equals(incarnation);
    }
}
