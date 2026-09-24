using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace HBP.Sync
{
    public enum V2OriginDevice : byte
    {
        Desktop = 1,
        Quest = 2
    }

    public enum V2OperationType : ushort
    {
        SetSiteColor = 1,
        SetCutDefinition = 2,
        SetTimelineAnchor = 3
    }

    public enum V2CutOrientation : byte
    {
        Axial = 0,
        Coronal = 1,
        Sagittal = 2,
        Custom = 3
    }

    public enum V2BarrierScope : byte
    {
        None = 0
    }

    public abstract class V2BinaryIdentity : IEquatable<V2BinaryIdentity>
    {
        public Guid Value { get; }
        public bool IsValid => Value != Guid.Empty;

        protected V2BinaryIdentity(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("Identity must be nonzero.", nameof(value));
            Value = value;
        }

        public byte[] ToByteArray() => Value.ToByteArray();

        public bool Equals(V2BinaryIdentity other) => other != null && GetType() == other.GetType() && Value == other.Value;
        public override bool Equals(object obj) => Equals(obj as V2BinaryIdentity);
        public override int GetHashCode() => unchecked((Value.GetHashCode() * 397) ^ GetType().GetHashCode());
        public override string ToString() => Value.ToString("D");
    }

    public sealed class SessionId : V2BinaryIdentity
    {
        public SessionId(Guid value) : base(value)
        {
        }

        public static SessionId FromBytes(byte[] bytes) => new SessionId(V2IdentityCodec.ReadGuid(bytes));
    }

    public sealed class SceneId : V2BinaryIdentity
    {
        public SceneId(Guid value) : base(value)
        {
        }

        public static SceneId FromBytes(byte[] bytes) => new SceneId(V2IdentityCodec.ReadGuid(bytes));
    }

    public sealed class IncarnationId : V2BinaryIdentity
    {
        public IncarnationId(Guid value) : base(value)
        {
        }

        public static IncarnationId FromBytes(byte[] bytes) => new IncarnationId(V2IdentityCodec.ReadGuid(bytes));
    }

    public sealed class OperationId : V2BinaryIdentity
    {
        public OperationId(Guid value) : base(value)
        {
        }

        public static OperationId FromBytes(byte[] bytes) => new OperationId(V2IdentityCodec.ReadGuid(bytes));
    }

    public sealed class ReliableStreamId : V2BinaryIdentity
    {
        public ReliableStreamId(Guid value) : base(value)
        {
        }

        public static ReliableStreamId FromBytes(byte[] bytes) => new ReliableStreamId(V2IdentityCodec.ReadGuid(bytes));
    }

    public abstract class V2TextIdentity : IEquatable<V2TextIdentity>
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public string Value { get; }

        protected V2TextIdentity(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            string normalized;
            try
            {
                normalized = value.Normalize(NormalizationForm.FormC);
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException("Identity must contain valid Unicode text.", nameof(value), exception);
            }

            if (normalized.Length == 0)
                throw new ArgumentException("Identity must not be empty.", nameof(value));
            foreach (char character in normalized)
            {
                if (char.IsControl(character))
                    throw new ArgumentException("Identity must not contain control characters.", nameof(value));
            }

            byte[] encoded;
            try
            {
                encoded = StrictUtf8.GetBytes(normalized);
            }
            catch (EncoderFallbackException exception)
            {
                throw new ArgumentException("Identity must contain valid Unicode text.", nameof(value), exception);
            }

            if (encoded.Length > 256)
                throw new ArgumentException("Identity exceeds 256 UTF-8 bytes.", nameof(value));
            Value = normalized;
        }

        public bool Equals(V2TextIdentity other) => other != null && GetType() == other.GetType() && StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) => Equals(obj as V2TextIdentity);
        public override int GetHashCode() => unchecked((StringComparer.Ordinal.GetHashCode(Value) * 397) ^ GetType().GetHashCode());
        public override string ToString() => Value;
    }

    public sealed class ResourceId : V2TextIdentity
    {
        public ResourceId(string value) : base(value)
        {
        }
    }

    public sealed class TopologyId : V2TextIdentity
    {
        public TopologyId(string value) : base(value)
        {
        }
    }

    public sealed class ColumnId : V2TextIdentity
    {
        public ColumnId(string value) : base(value)
        {
        }
    }

    public sealed class SiteId : V2TextIdentity
    {
        public SiteId(string value) : base(value)
        {
        }
    }

    public sealed class CutId : V2TextIdentity
    {
        public CutId(string value) : base(value)
        {
        }
    }

    public abstract class V2Mutation
    {
        public abstract V2OperationType Type { get; }

        internal V2Mutation()
        {
        }
    }

    public sealed class SetSiteColor : V2Mutation
    {
        public ColumnId ColumnId { get; }
        public SiteId FullSiteId { get; }
        public float Red { get; }
        public float Green { get; }
        public float Blue { get; }
        public float Alpha { get; }
        public override V2OperationType Type => V2OperationType.SetSiteColor;

        public SetSiteColor(ColumnId columnId, SiteId fullSiteId, float red, float green, float blue, float alpha)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            FullSiteId = fullSiteId ?? throw new ArgumentNullException(nameof(fullSiteId));
            V2ValueValidation.ValidateFloat(red, nameof(red));
            V2ValueValidation.ValidateFloat(green, nameof(green));
            V2ValueValidation.ValidateFloat(blue, nameof(blue));
            V2ValueValidation.ValidateFloat(alpha, nameof(alpha));
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }
    }

    public sealed class SetCutDefinition : V2Mutation
    {
        public CutId CutId { get; }
        public V2CutOrientation Orientation { get; }
        public bool Flip { get; }
        public uint NumberOfCuts { get; }
        public float Position { get; }
        public float NormalX { get; }
        public float NormalY { get; }
        public float NormalZ { get; }
        public override V2OperationType Type => V2OperationType.SetCutDefinition;

        public SetCutDefinition(CutId cutId, V2CutOrientation orientation, bool flip, uint numberOfCuts, float position, float normalX, float normalY, float normalZ)
        {
            CutId = cutId ?? throw new ArgumentNullException(nameof(cutId));
            if ((byte)orientation > (byte)V2CutOrientation.Custom)
                throw new ArgumentOutOfRangeException(nameof(orientation));
            if (numberOfCuts < 1 || numberOfCuts > 65535)
                throw new ArgumentOutOfRangeException(nameof(numberOfCuts));
            V2ValueValidation.ValidateFloat(position, nameof(position));
            if (position < 0f || position > 1f)
                throw new ArgumentOutOfRangeException(nameof(position));
            V2ValueValidation.ValidateFloat(normalX, nameof(normalX));
            V2ValueValidation.ValidateFloat(normalY, nameof(normalY));
            V2ValueValidation.ValidateFloat(normalZ, nameof(normalZ));
            if (normalX == 0f && normalY == 0f && normalZ == 0f)
                throw new ArgumentException("Cut normal must be nonzero.", nameof(normalX));

            Orientation = orientation;
            Flip = flip;
            NumberOfCuts = numberOfCuts;
            Position = position;
            NormalX = normalX;
            NormalY = normalY;
            NormalZ = normalZ;
        }
    }

    public sealed class SetTimelineAnchor : V2Mutation
    {
        public ColumnId ColumnId { get; }
        public int Index { get; }
        public bool Playing { get; }
        public bool Looping { get; }
        public int Step { get; }
        public long MonotonicAnchorTicks { get; }
        public ulong TickFrequency { get; }
        public override V2OperationType Type => V2OperationType.SetTimelineAnchor;

        public SetTimelineAnchor(ColumnId columnId, int index, bool playing, bool looping, int step, long monotonicAnchorTicks, ulong tickFrequency)
        {
            ColumnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (step < 1 || step > 1000000)
                throw new ArgumentOutOfRangeException(nameof(step));
            if (monotonicAnchorTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(monotonicAnchorTicks));
            if (tickFrequency == 0 || tickFrequency > 1000000000000UL)
                throw new ArgumentOutOfRangeException(nameof(tickFrequency));

            Index = index;
            Playing = playing;
            Looping = looping;
            Step = step;
            MonotonicAnchorTicks = monotonicAnchorTicks;
            TickFrequency = tickFrequency;
        }
    }

    public enum V2TouchedKeyKind : byte
    {
        SiteColor = 1,
        CutDefinition = 2,
        TimelineAnchor = 3
    }

    public sealed class V2TouchedKey : IEquatable<V2TouchedKey>
    {
        public SceneId SceneId { get; }
        public IncarnationId IncarnationId { get; }
        public V2TouchedKeyKind Kind { get; }
        public ColumnId ColumnId { get; }
        public SiteId SiteId { get; }
        public CutId CutId { get; }

        internal V2TouchedKey(SceneId sceneId, IncarnationId incarnationId, V2TouchedKeyKind kind, ColumnId columnId, SiteId siteId, CutId cutId)
        {
            SceneId = sceneId;
            IncarnationId = incarnationId;
            Kind = kind;
            ColumnId = columnId;
            SiteId = siteId;
            CutId = cutId;
        }

        public bool Equals(V2TouchedKey other) => other != null && SceneId.Equals(other.SceneId) && IncarnationId.Equals(other.IncarnationId) && Kind == other.Kind && Equals(ColumnId, other.ColumnId) && Equals(SiteId, other.SiteId) && Equals(CutId, other.CutId);

        public override bool Equals(object obj) => Equals(obj as V2TouchedKey);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = SceneId.GetHashCode();
                hash = (hash * 397) ^ IncarnationId.GetHashCode();
                hash = (hash * 397) ^ (int)Kind;
                hash = (hash * 397) ^ (ColumnId?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ (SiteId?.GetHashCode() ?? 0);
                return (hash * 397) ^ (CutId?.GetHashCode() ?? 0);
            }
        }
    }

    public sealed class V2MutationDescriptor
    {
        public V2TouchedKey CoalescingKey { get; }
        public IReadOnlyList<V2TouchedKey> TouchedKeys { get; }
        public bool SupportsLatestUnsentCoalescing => true;
        public V2BarrierScope BarrierScope => V2BarrierScope.None;

        internal V2MutationDescriptor(SceneId sceneId, IncarnationId incarnationId, V2Mutation mutation)
        {
            V2TouchedKey key;
            if (mutation is SetSiteColor siteColor)
                key = new V2TouchedKey(sceneId, incarnationId, V2TouchedKeyKind.SiteColor, siteColor.ColumnId, siteColor.FullSiteId, null);
            else if (mutation is SetCutDefinition cutDefinition)
                key = new V2TouchedKey(sceneId, incarnationId, V2TouchedKeyKind.CutDefinition, null, null, cutDefinition.CutId);
            else if (mutation is SetTimelineAnchor timelineAnchor)
                key = new V2TouchedKey(sceneId, incarnationId, V2TouchedKeyKind.TimelineAnchor, timelineAnchor.ColumnId, null, null);
            else
                throw new ArgumentException("Unsupported mutation type.", nameof(mutation));

            CoalescingKey = key;
            TouchedKeys = new ReadOnlyCollection<V2TouchedKey>(new[] { key });
        }
    }

    public sealed class V2MutationEnvelope
    {
        public SessionId SessionId { get; }
        public SceneId SceneId { get; }
        public IncarnationId IncarnationId { get; }
        public OperationId OperationId { get; }
        public ReliableStreamId ReliableStreamId { get; }
        public ulong ReliableFrameSequence { get; }
        public ulong OriginSequence { get; }
        public ulong? CanonicalSequence { get; }
        public ulong? ObservedCanonicalSequence { get; }
        public V2OriginDevice OriginDevice { get; }
        public V2Mutation Mutation { get; }
        public V2MutationDescriptor Descriptor { get; }

        public V2MutationEnvelope(SessionId sessionId, SceneId sceneId, IncarnationId incarnationId, OperationId operationId, ReliableStreamId reliableStreamId, ulong reliableFrameSequence, ulong originSequence, V2OriginDevice originDevice, ulong? canonicalSequence, ulong? observedCanonicalSequence, V2Mutation mutation)
        {
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            SceneId = sceneId ?? throw new ArgumentNullException(nameof(sceneId));
            IncarnationId = incarnationId ?? throw new ArgumentNullException(nameof(incarnationId));
            OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
            ReliableStreamId = reliableStreamId ?? throw new ArgumentNullException(nameof(reliableStreamId));
            Mutation = mutation ?? throw new ArgumentNullException(nameof(mutation));
            if (reliableFrameSequence == 0)
                throw new ArgumentOutOfRangeException(nameof(reliableFrameSequence));
            if (originSequence == 0)
                throw new ArgumentOutOfRangeException(nameof(originSequence));
            if (originDevice != V2OriginDevice.Desktop && originDevice != V2OriginDevice.Quest)
                throw new ArgumentOutOfRangeException(nameof(originDevice));
            if (canonicalSequence.HasValue && canonicalSequence.Value == 0)
                throw new ArgumentOutOfRangeException(nameof(canonicalSequence));
            if (originDevice == V2OriginDevice.Desktop && !canonicalSequence.HasValue)
                throw new ArgumentException("Desktop mutations must carry a canonical sequence.", nameof(canonicalSequence));
            if (originDevice == V2OriginDevice.Desktop && observedCanonicalSequence.HasValue)
                throw new ArgumentException("Desktop-origin mutations cannot carry an observed canonical sequence.", nameof(observedCanonicalSequence));
            if (originDevice == V2OriginDevice.Quest && !observedCanonicalSequence.HasValue)
                throw new ArgumentException("Quest proposals must carry an observed canonical sequence.", nameof(observedCanonicalSequence));
            EnsureDistinctIdentities(sessionId, sceneId, incarnationId, operationId, reliableStreamId);

            ReliableFrameSequence = reliableFrameSequence;
            OriginSequence = originSequence;
            OriginDevice = originDevice;
            CanonicalSequence = canonicalSequence;
            ObservedCanonicalSequence = observedCanonicalSequence;
            Descriptor = new V2MutationDescriptor(sceneId, incarnationId, mutation);
        }

        private static void EnsureDistinctIdentities(params V2BinaryIdentity[] identities)
        {
            for (int i = 0; i < identities.Length; i++)
            {
                if (identities[i] == null || !identities[i].IsValid)
                    throw new ArgumentException("Envelope identities must be nonzero.", nameof(identities));
                for (int j = 0; j < i; j++)
                {
                    if (identities[i].Value == identities[j].Value)
                        throw new ArgumentException("Envelope identities must be distinct.", nameof(identities));
                }
            }
        }
    }

    public sealed class SiteColorCheckpointRecord
    {
        public SetSiteColor Value { get; }
        public ushort RecordType => (ushort)V2OperationType.SetSiteColor;
        public ushort SchemaVersion => 1;

        public SiteColorCheckpointRecord(SetSiteColor value) => Value = value ?? throw new ArgumentNullException(nameof(value));
        public byte[] Encode() => V2CheckpointRecordCodec.Encode(RecordType, Value);
        public static SiteColorCheckpointRecord Decode(byte[] bytes) => new SiteColorCheckpointRecord((SetSiteColor)V2CheckpointRecordCodec.Decode(bytes, V2OperationType.SetSiteColor));
    }

    public sealed class CutDefinitionCheckpointRecord
    {
        public SetCutDefinition Value { get; }
        public ushort RecordType => (ushort)V2OperationType.SetCutDefinition;
        public ushort SchemaVersion => 1;

        public CutDefinitionCheckpointRecord(SetCutDefinition value) => Value = value ?? throw new ArgumentNullException(nameof(value));
        public byte[] Encode() => V2CheckpointRecordCodec.Encode(RecordType, Value);
        public static CutDefinitionCheckpointRecord Decode(byte[] bytes) => new CutDefinitionCheckpointRecord((SetCutDefinition)V2CheckpointRecordCodec.Decode(bytes, V2OperationType.SetCutDefinition));
    }

    public sealed class TimelineAnchorCheckpointRecord
    {
        public SetTimelineAnchor Value { get; }
        public ushort RecordType => (ushort)V2OperationType.SetTimelineAnchor;
        public ushort SchemaVersion => 1;

        public TimelineAnchorCheckpointRecord(SetTimelineAnchor value) => Value = value ?? throw new ArgumentNullException(nameof(value));
        public byte[] Encode() => V2CheckpointRecordCodec.Encode(RecordType, Value);
        public static TimelineAnchorCheckpointRecord Decode(byte[] bytes) => new TimelineAnchorCheckpointRecord((SetTimelineAnchor)V2CheckpointRecordCodec.Decode(bytes, V2OperationType.SetTimelineAnchor));
    }

    internal static class V2IdentityCodec
    {
        internal static Guid ReadGuid(byte[] bytes)
        {
            if (bytes == null || bytes.Length != 16)
                throw new ArgumentException("Binary identity must be exactly 16 bytes.", nameof(bytes));
            return new Guid(bytes);
        }
    }

    internal static class V2ValueValidation
    {
        internal static void ValidateFloat(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || (value == 0f && 1f / value < 0f))
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and must not be negative zero.");
        }
    }
}
