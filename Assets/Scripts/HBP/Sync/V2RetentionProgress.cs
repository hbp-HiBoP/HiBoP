using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HBP.Sync
{
    /// <summary>Application completion, distinct from transport receipt acknowledgements.</summary>
    public sealed class V2RetentionProgress
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("HBRP");
        public const int EncodedLength = 86;
        public SessionId SessionId { get; }
        public SceneId SceneId { get; }
        public IncarnationId IncarnationId { get; }
        public V2OriginDevice OriginDevice { get; }
        public ulong AppliedOriginThrough { get; }
        public ulong MinimumObservedCanonicalSequence { get; }
        public ulong RetiredCanonicalThrough { get; }
        public ulong CurrentCanonicalSequence { get; }

        public V2RetentionProgress(SessionId sessionId, SceneId sceneId, IncarnationId incarnationId, V2OriginDevice originDevice, ulong appliedOriginThrough, ulong minimumObservedCanonicalSequence, ulong retiredCanonicalThrough, ulong currentCanonicalSequence)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            SceneId = sceneId ?? throw new ArgumentNullException(nameof(sceneId));
            IncarnationId = incarnationId ?? throw new ArgumentNullException(nameof(incarnationId));
            if (originDevice != V2OriginDevice.Desktop && originDevice != V2OriginDevice.Quest || minimumObservedCanonicalSequence > currentCanonicalSequence || retiredCanonicalThrough > currentCanonicalSequence || originDevice == V2OriginDevice.Quest && retiredCanonicalThrough != 0)
                throw new InvalidDataException("Invalid application retention progress.");
            OriginDevice = originDevice;
            AppliedOriginThrough = appliedOriginThrough;
            MinimumObservedCanonicalSequence = minimumObservedCanonicalSequence;
            RetiredCanonicalThrough = retiredCanonicalThrough;
            CurrentCanonicalSequence = currentCanonicalSequence;
        }

        public byte[] Encode()
        {
            using var stream = new MemoryStream(EncodedLength);
            using var writer = new BinaryWriter(stream);
            writer.Write(Magic);
            writer.Write((byte)1);
            writer.Write((byte)OriginDevice);
            writer.Write(SessionId.ToByteArray());
            writer.Write(SceneId.ToByteArray());
            writer.Write(IncarnationId.ToByteArray());
            writer.Write(AppliedOriginThrough);
            writer.Write(MinimumObservedCanonicalSequence);
            writer.Write(RetiredCanonicalThrough);
            writer.Write(CurrentCanonicalSequence);
            return stream.ToArray();
        }

        public static bool TryDecode(byte[] bytes, out V2RetentionProgress progress)
        {
            progress = null;
            if (bytes == null || bytes.Length < 4) return false;
            for (int i = 0; i < Magic.Length; i++)
                if (bytes[i] != Magic[i])
                    return false;
            if (bytes.Length != EncodedLength || bytes[4] != 1) throw new InvalidDataException("Invalid retention progress frame.");
            try
            {
                using var stream = new MemoryStream(bytes, false);
                using var reader = new BinaryReader(stream);
                stream.Position = 5;
                var device = (V2OriginDevice)reader.ReadByte();
                var session = new SessionId(new Guid(reader.ReadBytes(16)));
                var scene = new SceneId(new Guid(reader.ReadBytes(16)));
                var incarnation = new IncarnationId(new Guid(reader.ReadBytes(16)));
                progress = new V2RetentionProgress(session, scene, incarnation, device, reader.ReadUInt64(), reader.ReadUInt64(), reader.ReadUInt64(), reader.ReadUInt64());
                return true;
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("Invalid retention progress identity.", exception);
            }
        }

        public void ValidateScope(SessionId session, SceneId scene, IncarnationId incarnation, V2OriginDevice device)
        {
            if (!SessionId.Equals(session) || !SceneId.Equals(scene) || !IncarnationId.Equals(incarnation) || OriginDevice != device)
                throw new InvalidDataException("Retention progress belongs to another session, incarnation or direction.");
        }
    }

    /// <summary>A maximum is insufficient: bulk and optimized batches may finish out of order.</summary>
    public sealed class V2ApplicationCompletionWatermark
    {
        private readonly SortedSet<ulong> m_Completed = new SortedSet<ulong>();
        private readonly int m_MaximumGaps;
        public ulong CompletedThrough { get; private set; }
        public int GapCount => m_Completed.Count;

        public V2ApplicationCompletionWatermark(int maximumGaps = 4096)
        {
            if (maximumGaps <= 0) throw new ArgumentOutOfRangeException(nameof(maximumGaps));
            m_MaximumGaps = maximumGaps;
        }

        public void Complete(ulong originSequence)
        {
            if (originSequence == 0 || originSequence <= CompletedThrough) return;
            if (originSequence == CompletedThrough + 1)
            {
                CompletedThrough = originSequence;
                while (CompletedThrough != ulong.MaxValue && m_Completed.Remove(CompletedThrough + 1)) CompletedThrough++;
            }
            else
            {
                if (!m_Completed.Contains(originSequence) && m_Completed.Count >= m_MaximumGaps)
                    throw new InvalidDataException("Application completion gaps exceed their budget.");
                m_Completed.Add(originSequence);
            }
        }
    }
}
