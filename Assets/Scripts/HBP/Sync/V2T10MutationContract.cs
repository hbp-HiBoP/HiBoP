using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HBP.Sync
{
    public enum V2SiteMoveCommand : byte
    {
        Left = 1,
        Right = 2,
        Reset = 3
    }

    public enum V2MeshPart : byte
    {
        Both = 0,
        Left = 1,
        Right = 2
    }

    public enum V2SurfaceRepresentation : byte
    {
        Anatomical = 0,
        Inflated = 1
    }

    public enum V2TriangleMaskEncoding : byte
    {
        SparseInvisibleIndices = 1,
        VisibilityBitset = 2
    }

    public readonly struct V2RoiSphereDefinition
    {
        public SphereId SphereId { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float InfluenceRadius { get; }

        public V2RoiSphereDefinition(SphereId sphereId, float x, float y, float z, float influenceRadius)
        {
            SphereId = sphereId ?? throw new ArgumentNullException(nameof(sphereId));
            V2ValueValidation.ValidateFloat(x, nameof(x));
            V2ValueValidation.ValidateFloat(y, nameof(y));
            V2ValueValidation.ValidateFloat(z, nameof(z));
            V2ValueValidation.ValidateFloat(influenceRadius, nameof(influenceRadius));
            if (influenceRadius < 0.5f || influenceRadius > 100f) throw new ArgumentOutOfRangeException(nameof(influenceRadius));
            X = x;
            Y = y;
            Z = z;
            InfluenceRadius = influenceRadius;
        }
    }

    public sealed class V2RoiSelectionSnapshot
    {
        public RoiId ActiveRoiId { get; }
        public SphereId SelectedSphereId { get; }

        public V2RoiSelectionSnapshot(RoiId activeRoiId, SphereId selectedSphereId)
        {
            ActiveRoiId = activeRoiId;
            SelectedSphereId = selectedSphereId;
        }
    }

    public sealed class CreateCut : V2Mutation
    {
        public CutId CutId { get; }
        public SetCutDefinition Definition { get; }
        public int Order { get; }
        public override V2OperationType Type => V2OperationType.CreateCut;

        public CreateCut(CutId cutId, SetCutDefinition definition, int order)
        {
            CutId = cutId ?? throw new ArgumentNullException(nameof(cutId));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (!CutId.Equals(definition.CutId)) throw new ArgumentException("Cut identity must match its complete definition.", nameof(definition));
            if (order < 0 || order > 255) throw new ArgumentOutOfRangeException(nameof(order));
            Order = order;
        }
    }

    public sealed class DeleteCut : V2Mutation
    {
        public CutId CutId { get; }
        public override V2OperationType Type => V2OperationType.DeleteCut;
        public DeleteCut(CutId cutId) => CutId = cutId ?? throw new ArgumentNullException(nameof(cutId));
    }

    public sealed class SetCutOrder : V2Mutation
    {
        private readonly CutId[] m_CutIds;
        public IReadOnlyList<CutId> CutIds => Array.AsReadOnly(m_CutIds);
        public override V2OperationType Type => V2OperationType.SetCutOrder;

        public SetCutOrder(IEnumerable<CutId> cutIds)
        {
            m_CutIds = (cutIds ?? throw new ArgumentNullException(nameof(cutIds))).ToArray();
            if (m_CutIds.Length > 256 || m_CutIds.Any(id => id == null) || m_CutIds.Distinct().Count() != m_CutIds.Length)
                throw new ArgumentException("Cut order must contain at most 256 unique identities.", nameof(cutIds));
        }
    }

    public sealed class CreateRoi : V2Mutation
    {
        private readonly V2RoiSphereDefinition[] m_Spheres;
        public RoiId RoiId { get; }
        public string Name { get; }
        public IReadOnlyList<V2RoiSphereDefinition> Spheres => Array.AsReadOnly(m_Spheres);
        public int Order { get; }
        public V2RoiSelectionSnapshot SelectionSnapshot { get; }
        public override V2OperationType Type => V2OperationType.CreateRoi;

        public CreateRoi(RoiId roiId, string name, IEnumerable<V2RoiSphereDefinition> spheres, int order, V2RoiSelectionSnapshot selectionSnapshot = null)
        {
            RoiId = roiId ?? throw new ArgumentNullException(nameof(roiId));
            Name = V2T09ValueValidation.ValidateRequiredText(name, nameof(name));
            m_Spheres = (spheres ?? throw new ArgumentNullException(nameof(spheres))).ToArray();
            if (m_Spheres.Length > 64 || m_Spheres.Select(sphere => sphere.SphereId).Distinct().Count() != m_Spheres.Length)
                throw new ArgumentException("ROI sphere identities must be unique and bounded.", nameof(spheres));
            if (order < 0 || order > 255) throw new ArgumentOutOfRangeException(nameof(order));
            Order = order;
            SelectionSnapshot = selectionSnapshot;
        }
    }

    public sealed class RenameRoi : V2Mutation
    {
        public RoiId RoiId { get; }
        public string Name { get; }
        public override V2OperationType Type => V2OperationType.RenameRoi;

        public RenameRoi(RoiId roiId, string name)
        {
            RoiId = roiId ?? throw new ArgumentNullException(nameof(roiId));
            Name = V2T09ValueValidation.ValidateRequiredText(name, nameof(name));
        }
    }

    public sealed class DeleteRoi : V2Mutation
    {
        public RoiId RoiId { get; }
        public override V2OperationType Type => V2OperationType.DeleteRoi;
        public DeleteRoi(RoiId roiId) => RoiId = roiId ?? throw new ArgumentNullException(nameof(roiId));
    }

    public sealed class SetActiveRoi : V2Mutation
    {
        public RoiId RoiId { get; }
        public override V2OperationType Type => V2OperationType.SetActiveRoi;
        public SetActiveRoi(RoiId roiId) => RoiId = roiId;
    }

    public sealed class CreateRoiSphere : V2Mutation
    {
        public RoiId RoiId { get; }
        public V2RoiSphereDefinition Definition { get; }
        public int Order { get; }
        public V2RoiSelectionSnapshot SelectionSnapshot { get; }
        public override V2OperationType Type => V2OperationType.CreateRoiSphere;

        public CreateRoiSphere(RoiId roiId, V2RoiSphereDefinition definition, int order, V2RoiSelectionSnapshot selectionSnapshot = null)
        {
            RoiId = roiId ?? throw new ArgumentNullException(nameof(roiId));
            Definition = definition;
            if (definition.SphereId == null) throw new ArgumentException("Sphere identity is required.", nameof(definition));
            if (order < 0 || order > 255) throw new ArgumentOutOfRangeException(nameof(order));
            Order = order;
            SelectionSnapshot = selectionSnapshot;
        }
    }

    public sealed class DeleteRoiSphere : V2Mutation
    {
        public RoiId RoiId { get; }
        public SphereId SphereId { get; }
        public override V2OperationType Type => V2OperationType.DeleteRoiSphere;

        public DeleteRoiSphere(RoiId roiId, SphereId sphereId)
        {
            RoiId = roiId ?? throw new ArgumentNullException(nameof(roiId));
            SphereId = sphereId ?? throw new ArgumentNullException(nameof(sphereId));
        }
    }

    public sealed class SetRoiSphereDefinition : V2Mutation
    {
        public RoiId RoiId { get; }
        public V2RoiSphereDefinition Definition { get; }
        public override V2OperationType Type => V2OperationType.SetRoiSphereDefinition;

        public SetRoiSphereDefinition(RoiId roiId, V2RoiSphereDefinition definition)
        {
            RoiId = roiId ?? throw new ArgumentNullException(nameof(roiId));
            Definition = definition;
            if (definition.SphereId == null) throw new ArgumentException("Sphere identity is required.", nameof(definition));
        }
    }

    public sealed class MoveSites : V2Mutation
    {
        public V2SiteMoveCommand Command { get; }
        public override V2OperationType Type => V2OperationType.MoveSites;

        public MoveSites(V2SiteMoveCommand command)
        {
            if (!Enum.IsDefined(typeof(V2SiteMoveCommand), command)) throw new ArgumentOutOfRangeException(nameof(command));
            Command = command;
        }
    }

    public sealed class SetMeshDisplay : V2Mutation
    {
        public ResourceId MeshId { get; }
        public V2MeshPart Part { get; }
        public V2SurfaceRepresentation Representation { get; }
        public override V2OperationType Type => V2OperationType.SetMeshDisplay;

        public SetMeshDisplay(ResourceId meshId, V2MeshPart part, V2SurfaceRepresentation representation)
        {
            MeshId = meshId ?? throw new ArgumentNullException(nameof(meshId));
            if (!Enum.IsDefined(typeof(V2MeshPart), part)) throw new ArgumentOutOfRangeException(nameof(part));
            if (!Enum.IsDefined(typeof(V2SurfaceRepresentation), representation)) throw new ArgumentOutOfRangeException(nameof(representation));
            Part = part;
            Representation = representation;
        }
    }

    public sealed class SetSelectedMri : V2Mutation
    {
        public ResourceId ResourceId { get; }
        public override V2OperationType Type => V2OperationType.SetSelectedMri;
        public SetSelectedMri(ResourceId resourceId) => ResourceId = resourceId ?? throw new ArgumentNullException(nameof(resourceId));
    }

    public sealed class SetMriCalibration : V2Mutation
    {
        public float Minimum { get; }
        public float Maximum { get; }
        public override V2OperationType Type => V2OperationType.SetMriCalibration;

        public SetMriCalibration(float minimum, float maximum)
        {
            V2T09ValueValidation.ValidateRange(minimum, 0f, 1f, nameof(minimum));
            V2T09ValueValidation.ValidateRange(maximum, minimum, 1f, nameof(maximum));
            Minimum = minimum;
            Maximum = maximum;
        }
    }

    public sealed class SetImplantation : V2Mutation
    {
        public ResourceId ResourceId { get; }
        public string MembershipHash { get; }
        public override V2OperationType Type => V2OperationType.SetImplantation;

        public SetImplantation(ResourceId resourceId, string membershipHash)
        {
            ResourceId = resourceId ?? throw new ArgumentNullException(nameof(resourceId));
            if (membershipHash == null || membershipHash.Length != 64 || membershipHash.Any(character => !(character is >= '0' and <= '9' or >= 'a' and <= 'f')))
                throw new ArgumentException("Implantation membership must be identified by a lowercase SHA-256 digest.", nameof(membershipHash));
            MembershipHash = membershipHash;
        }
    }

    public sealed class V2TriangleMask
    {
        private readonly int[] m_InvisibleTriangleIds;
        private readonly byte[] m_VisibilityBits;
        public TopologyId TopologyId { get; }
        public int TriangleCount { get; }
        public V2TriangleMaskEncoding Encoding { get; }
        public IReadOnlyList<int> InvisibleTriangleIds => Array.AsReadOnly(m_InvisibleTriangleIds);
        public byte[] GetVisibilityBitsCopy() => (byte[])m_VisibilityBits.Clone();

        public V2TriangleMask(TopologyId topologyId, int triangleCount, IEnumerable<int> invisibleTriangleIds)
        {
            TopologyId = topologyId ?? throw new ArgumentNullException(nameof(topologyId));
            if (triangleCount <= 0) throw new ArgumentOutOfRangeException(nameof(triangleCount));
            TriangleCount = triangleCount;
            m_InvisibleTriangleIds = (invisibleTriangleIds ?? throw new ArgumentNullException(nameof(invisibleTriangleIds))).ToArray();
            if (m_InvisibleTriangleIds.Length > triangleCount || m_InvisibleTriangleIds.Any(id => id < 0 || id >= triangleCount) || !m_InvisibleTriangleIds.SequenceEqual(m_InvisibleTriangleIds.Distinct().OrderBy(id => id)))
                throw new ArgumentException("Sparse triangle ids must be sorted, unique, and inside the original topology.", nameof(invisibleTriangleIds));
            Encoding = V2TriangleMaskEncoding.SparseInvisibleIndices;
            m_VisibilityBits = null;
        }

        public V2TriangleMask(TopologyId topologyId, int triangleCount, byte[] visibilityBits)
        {
            TopologyId = topologyId ?? throw new ArgumentNullException(nameof(topologyId));
            if (triangleCount <= 0) throw new ArgumentOutOfRangeException(nameof(triangleCount));
            if (visibilityBits == null || visibilityBits.Length != (triangleCount + 7) / 8) throw new ArgumentException("Visibility bitset length does not match the original topology.", nameof(visibilityBits));
            if (triangleCount % 8 != 0 && (visibilityBits[^1] & ~((1 << (triangleCount % 8)) - 1)) != 0) throw new ArgumentException("Visibility bitset has noncanonical trailing bits.", nameof(visibilityBits));
            TopologyId = topologyId;
            TriangleCount = triangleCount;
            Encoding = V2TriangleMaskEncoding.VisibilityBitset;
            m_VisibilityBits = (byte[])visibilityBits.Clone();
            m_InvisibleTriangleIds = Array.Empty<int>();
        }

        public int[] ToVisibilityMask()
        {
            var mask = Enumerable.Repeat(1, TriangleCount).ToArray();
            if (Encoding == V2TriangleMaskEncoding.SparseInvisibleIndices)
                foreach (int index in m_InvisibleTriangleIds)
                    mask[index] = 0;
            else
                for (int i = 0; i < TriangleCount; i++)
                    mask[i] = (m_VisibilityBits[i >> 3] & (1 << (i & 7))) != 0 ? 1 : 0;
            return mask;
        }

        public static V2TriangleMask FromVisibilityMask(TopologyId topologyId, IReadOnlyList<int> visibility)
        {
            if (visibility == null || visibility.Count == 0) throw new ArgumentException("Visibility mask is empty.", nameof(visibility));
            var invisible = new List<int>();
            var bits = new byte[(visibility.Count + 7) / 8];
            for (int i = 0; i < visibility.Count; i++)
            {
                if (visibility[i] == 1) bits[i >> 3] |= (byte)(1 << (i & 7));
                else if (visibility[i] == 0) invisible.Add(i);
                else throw new ArgumentException("Visibility masks must contain only zero and one values.", nameof(visibility));
            }

            return invisible.Count * sizeof(int) < bits.Length ? new V2TriangleMask(topologyId, visibility.Count, invisible) : new V2TriangleMask(topologyId, visibility.Count, bits);
        }
    }

    public sealed class ApplyTriangleMask : V2Mutation
    {
        private readonly V2TriangleMask[] m_Masks;
        public IReadOnlyList<V2TriangleMask> Masks => Array.AsReadOnly(m_Masks);
        public override V2OperationType Type => V2OperationType.ApplyTriangleMask;

        public ApplyTriangleMask(IEnumerable<V2TriangleMask> masks)
        {
            m_Masks = (masks ?? throw new ArgumentNullException(nameof(masks))).ToArray();
            if (m_Masks.Length != 2 || m_Masks.Any(mask => mask == null) || m_Masks.Select(mask => mask.TopologyId).Distinct().Count() != m_Masks.Length)
                throw new ArgumentException("A mask operation requires unique complete and simplified topology masks.", nameof(masks));
        }

        public static string HashMembership(IEnumerable<string> siteIds)
        {
            if (siteIds == null) throw new ArgumentNullException(nameof(siteIds));
            byte[] bytes = Encoding.UTF8.GetBytes(string.Join("\n", siteIds.OrderBy(id => id, StringComparer.Ordinal)));
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }

    /// <summary>A T10-family checkpoint record that retains the concrete operation type.</summary>
    public sealed class V2T10CheckpointRecord
    {
        private const ushort RecordMagic = 0x5431;
        private const ushort RecordSchema = 1;
        public V2Mutation Value { get; }

        public V2T10CheckpointRecord(V2Mutation value)
        {
            if (value == null || (ushort)value.Type < (ushort)V2OperationType.CreateCut)
                throw new ArgumentException("A T10 checkpoint record requires a T10 mutation.", nameof(value));
            Value = value;
        }

        public byte[] Encode()
        {
            byte[] mutation = V2MutationPayloadCodec.Encode(Value);
            using var stream = new MemoryStream(mutation.Length + 8);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.Write(RecordMagic);
            writer.Write(RecordSchema);
            writer.Write(checked((uint)mutation.Length));
            writer.Write(mutation);
            writer.Flush();
            return stream.ToArray();
        }

        public static V2T10CheckpointRecord Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 12 || bytes.Length > V2MutationPayloadCodec.MaximumPayloadBytes + 8)
                throw new InvalidDataException("Invalid T10 checkpoint record length.");
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);
            try
            {
                if (reader.ReadUInt16() != RecordMagic || reader.ReadUInt16() != RecordSchema)
                    throw new InvalidDataException("Unsupported T10 checkpoint record signature or schema.");
                uint length = reader.ReadUInt32();
                if (length > V2MutationPayloadCodec.MaximumPayloadBytes || length != stream.Length - stream.Position)
                    throw new InvalidDataException("Invalid T10 checkpoint mutation length.");
                var record = new V2T10CheckpointRecord(V2MutationPayloadCodec.Decode(reader.ReadBytes(checked((int)length))));
                if (stream.Position != stream.Length)
                    throw new InvalidDataException("Trailing T10 checkpoint record bytes.");
                return record;
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException("Truncated T10 checkpoint record.", exception);
            }
        }
    }

    internal static class V2T10MutationCodec
    {
        private static readonly UTF8Encoding StrictUtf8 = new(false, true);

        internal static V2Mutation ReadBody(BinaryReader reader, V2OperationType type)
        {
            switch (type)
            {
                case V2OperationType.CreateCut:
                    {
                        CutId id = new(ReadText(reader, false));
                        var definition = new SetCutDefinition(id, ReadEnum<V2CutOrientation>(reader), ReadBoolean(reader), reader.ReadUInt32(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        return new CreateCut(id, definition, reader.ReadInt32());
                    }
                case V2OperationType.DeleteCut: return new DeleteCut(new CutId(ReadText(reader, false)));
                case V2OperationType.SetCutOrder:
                    {
                        int count = reader.ReadUInt16();
                        if (count > 256) throw new InvalidDataException("Cut order exceeds its bound.");
                        return new SetCutOrder(Enumerable.Range(0, count).Select(_ => new CutId(ReadText(reader, false))));
                    }
                case V2OperationType.CreateRoi:
                    {
                        RoiId id = new(ReadText(reader, false));
                        string name = ReadText(reader, false);
                        int count = reader.ReadUInt16();
                        if (count > 64) throw new InvalidDataException("ROI sphere count exceeds its bound.");
                        var spheres = new V2RoiSphereDefinition[count];
                        for (int i = 0; i < count; i++) spheres[i] = ReadSphere(reader);
                        int order = reader.ReadInt32();
                        return new CreateRoi(id, name, spheres, order, ReadRoiSelectionSnapshot(reader));
                    }
                case V2OperationType.RenameRoi: return new RenameRoi(new RoiId(ReadText(reader, false)), ReadText(reader, false));
                case V2OperationType.DeleteRoi: return new DeleteRoi(new RoiId(ReadText(reader, false)));
                case V2OperationType.SetActiveRoi:
                    {
                        string id = ReadText(reader);
                        return new SetActiveRoi(string.IsNullOrEmpty(id) ? null : new RoiId(id));
                    }
                case V2OperationType.CreateRoiSphere:
                    {
                        RoiId roiId = new(ReadText(reader, false));
                        V2RoiSphereDefinition sphere = ReadSphere(reader);
                        int order = reader.ReadInt32();
                        return new CreateRoiSphere(roiId, sphere, order, ReadRoiSelectionSnapshot(reader));
                    }
                case V2OperationType.DeleteRoiSphere: return new DeleteRoiSphere(new RoiId(ReadText(reader, false)), new SphereId(ReadText(reader, false)));
                case V2OperationType.SetRoiSphereDefinition: return new SetRoiSphereDefinition(new RoiId(ReadText(reader, false)), ReadSphere(reader));
                case V2OperationType.MoveSites: return new MoveSites(ReadEnum<V2SiteMoveCommand>(reader));
                case V2OperationType.SetMeshDisplay: return new SetMeshDisplay(new ResourceId(ReadText(reader, false)), ReadEnum<V2MeshPart>(reader), ReadEnum<V2SurfaceRepresentation>(reader));
                case V2OperationType.SetSelectedMri: return new SetSelectedMri(new ResourceId(ReadText(reader, false)));
                case V2OperationType.SetMriCalibration: return new SetMriCalibration(reader.ReadSingle(), reader.ReadSingle());
                case V2OperationType.SetImplantation: return new SetImplantation(new ResourceId(ReadText(reader, false)), ReadText(reader, false));
                case V2OperationType.ApplyTriangleMask: return ReadMasks(reader);
                default: throw new InvalidDataException("Unsupported T10 mutation type.");
            }
        }

        internal static void WriteBody(BinaryWriter writer, V2Mutation mutation)
        {
            switch (mutation)
            {
                case CreateCut value:
                    WriteText(writer, value.CutId.Value);
                    WriteCutDefinition(writer, value.Definition);
                    writer.Write(value.Order);
                    break;
                case DeleteCut value: WriteText(writer, value.CutId.Value); break;
                case SetCutOrder value:
                    writer.Write(checked((ushort)value.CutIds.Count));
                    foreach (CutId id in value.CutIds) WriteText(writer, id.Value);
                    break;
                case CreateRoi value:
                    WriteText(writer, value.RoiId.Value);
                    WriteText(writer, value.Name);
                    writer.Write(checked((ushort)value.Spheres.Count));
                    foreach (V2RoiSphereDefinition sphere in value.Spheres) WriteSphere(writer, sphere);
                    writer.Write(value.Order);
                    WriteRoiSelectionSnapshot(writer, value.SelectionSnapshot);
                    break;
                case RenameRoi value:
                    WriteText(writer, value.RoiId.Value);
                    WriteText(writer, value.Name);
                    break;
                case DeleteRoi value: WriteText(writer, value.RoiId.Value); break;
                case SetActiveRoi value: WriteText(writer, value.RoiId?.Value ?? string.Empty); break;
                case CreateRoiSphere value:
                    WriteText(writer, value.RoiId.Value);
                    WriteSphere(writer, value.Definition);
                    writer.Write(value.Order);
                    WriteRoiSelectionSnapshot(writer, value.SelectionSnapshot);
                    break;
                case DeleteRoiSphere value:
                    WriteText(writer, value.RoiId.Value);
                    WriteText(writer, value.SphereId.Value);
                    break;
                case SetRoiSphereDefinition value:
                    WriteText(writer, value.RoiId.Value);
                    WriteSphere(writer, value.Definition);
                    break;
                case MoveSites value: writer.Write((byte)value.Command); break;
                case SetMeshDisplay value:
                    WriteText(writer, value.MeshId.Value);
                    writer.Write((byte)value.Part);
                    writer.Write((byte)value.Representation);
                    break;
                case SetSelectedMri value: WriteText(writer, value.ResourceId.Value); break;
                case SetMriCalibration value:
                    writer.Write(value.Minimum);
                    writer.Write(value.Maximum);
                    break;
                case SetImplantation value:
                    WriteText(writer, value.ResourceId.Value);
                    WriteText(writer, value.MembershipHash);
                    break;
                case ApplyTriangleMask value: WriteMasks(writer, value); break;
                default: throw new ArgumentException("Unsupported T10 mutation type.", nameof(mutation));
            }
        }

        private static void WriteCutDefinition(BinaryWriter writer, SetCutDefinition value)
        {
            writer.Write((byte)value.Orientation);
            writer.Write((byte)(value.Flip ? 1 : 0));
            writer.Write(value.NumberOfCuts);
            writer.Write(value.Position);
            writer.Write(value.NormalX);
            writer.Write(value.NormalY);
            writer.Write(value.NormalZ);
        }

        private static V2RoiSphereDefinition ReadSphere(BinaryReader reader) => new(new SphereId(ReadText(reader, false)), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        private static void WriteSphere(BinaryWriter writer, V2RoiSphereDefinition value)
        {
            WriteText(writer, value.SphereId.Value);
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
            writer.Write(value.InfluenceRadius);
        }

        private static V2RoiSelectionSnapshot ReadRoiSelectionSnapshot(BinaryReader reader)
        {
            if (reader.BaseStream.Position == reader.BaseStream.Length) return null;
            if (!ReadBoolean(reader)) return null;
            string activeRoiId = ReadText(reader);
            string selectedSphereId = ReadText(reader);
            return new V2RoiSelectionSnapshot(string.IsNullOrEmpty(activeRoiId) ? null : new RoiId(activeRoiId), string.IsNullOrEmpty(selectedSphereId) ? null : new SphereId(selectedSphereId));
        }

        private static void WriteRoiSelectionSnapshot(BinaryWriter writer, V2RoiSelectionSnapshot selectionSnapshot)
        {
            if (selectionSnapshot == null) return;
            writer.Write((byte)1);
            WriteText(writer, selectionSnapshot.ActiveRoiId?.Value ?? string.Empty);
            WriteText(writer, selectionSnapshot.SelectedSphereId?.Value ?? string.Empty);
        }

        private static ApplyTriangleMask ReadMasks(BinaryReader reader)
        {
            int count = reader.ReadByte();
            if (count < 1 || count > 2) throw new InvalidDataException("Invalid topology mask count.");
            var masks = new V2TriangleMask[count];
            for (int i = 0; i < count; i++)
            {
                var topology = new TopologyId(ReadText(reader, false));
                int triangleCount = reader.ReadInt32();
                V2TriangleMaskEncoding encoding = ReadEnum<V2TriangleMaskEncoding>(reader);
                if (triangleCount <= 0 || triangleCount > 128 * 1024 * 1024) throw new InvalidDataException("Triangle count exceeds its bound.");
                if (encoding == V2TriangleMaskEncoding.SparseInvisibleIndices)
                {
                    int invisibleCount = reader.ReadInt32();
                    if (invisibleCount < 0 || invisibleCount > triangleCount || invisibleCount > (reader.BaseStream.Length - reader.BaseStream.Position) / sizeof(int)) throw new InvalidDataException("Sparse triangle mask length is invalid.");
                    var invisible = new int[invisibleCount];
                    for (int j = 0; j < invisibleCount; j++) invisible[j] = reader.ReadInt32();
                    masks[i] = new V2TriangleMask(topology, triangleCount, invisible);
                }
                else
                {
                    int byteCount = reader.ReadInt32();
                    if (byteCount != (triangleCount + 7) / 8 || byteCount > reader.BaseStream.Length - reader.BaseStream.Position) throw new InvalidDataException("Visibility bitset length is invalid.");
                    byte[] bits = reader.ReadBytes(byteCount);
                    if (bits.Length != byteCount) throw new EndOfStreamException();
                    masks[i] = new V2TriangleMask(topology, triangleCount, bits);
                }
            }

            return new ApplyTriangleMask(masks);
        }

        private static void WriteMasks(BinaryWriter writer, ApplyTriangleMask value)
        {
            writer.Write(checked((byte)value.Masks.Count));
            foreach (V2TriangleMask mask in value.Masks)
            {
                WriteText(writer, mask.TopologyId.Value);
                writer.Write(mask.TriangleCount);
                writer.Write((byte)mask.Encoding);
                if (mask.Encoding == V2TriangleMaskEncoding.SparseInvisibleIndices)
                {
                    writer.Write(mask.InvisibleTriangleIds.Count);
                    foreach (int id in mask.InvisibleTriangleIds) writer.Write(id);
                }
                else
                {
                    byte[] bits = mask.GetVisibilityBitsCopy();
                    writer.Write(bits.Length);
                    writer.Write(bits);
                }
            }
        }

        private static T ReadEnum<T>(BinaryReader reader) where T : struct, Enum
        {
            byte value = reader.ReadByte();
            if (!Enum.IsDefined(typeof(T), value)) throw new InvalidDataException("Invalid T10 enum value.");
            return (T)Enum.ToObject(typeof(T), value);
        }

        private static bool ReadBoolean(BinaryReader reader)
        {
            byte value = reader.ReadByte();
            if (value > 1) throw new InvalidDataException("Invalid T10 boolean value.");
            return value == 1;
        }

        private static string ReadText(BinaryReader reader, bool allowEmpty = true)
        {
            int length = reader.ReadUInt16();
            if (length > 256 || (!allowEmpty && length == 0) || length > reader.BaseStream.Length - reader.BaseStream.Position) throw new InvalidDataException("Invalid T10 text length.");
            byte[] bytes = reader.ReadBytes(length);
            try
            {
                string value = StrictUtf8.GetString(bytes);
                if (!StringComparer.Ordinal.Equals(value, value.Normalize(NormalizationForm.FormC))) throw new InvalidDataException("T10 text must be NFC-normalized.");
                return value;
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException("Malformed T10 UTF-8 text.", exception);
            }
        }

        private static void WriteText(BinaryWriter writer, string value)
        {
            byte[] bytes = StrictUtf8.GetBytes(value.Normalize(NormalizationForm.FormC));
            if (bytes.Length > 256) throw new InvalidDataException("T10 text exceeds its identity bound.");
            writer.Write(checked((ushort)bytes.Length));
            writer.Write(bytes);
        }
    }
}
