using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;

namespace CRNL.HiBoP.Protocol
{
    public enum DynamicFrameEncoding : byte
    {
        Float32LittleEndian = 1,
    }

    public sealed class DynamicFrameDescriptor
    {
        public const ushort SchemaVersion = 1;

        public DynamicFrameDescriptor(DynamicFrameEncoding encoding, int byteLength, int columnCount, AssetHash payloadHash)
        {
            if (encoding != DynamicFrameEncoding.Float32LittleEndian)
                throw new ArgumentOutOfRangeException(nameof(encoding));
            if (byteLength <= 0 || columnCount <= 0)
                throw new ArgumentOutOfRangeException(byteLength <= 0 ? nameof(byteLength) : nameof(columnCount));
            if (!payloadHash.IsValid)
                throw new ArgumentException("A valid payload hash is required.", nameof(payloadHash));
            Encoding = encoding;
            ByteLength = byteLength;
            ColumnCount = columnCount;
            PayloadHash = payloadHash;
        }

        public DynamicFrameEncoding Encoding { get; }
        public int ByteLength { get; }
        public int ColumnCount { get; }
        public AssetHash PayloadHash { get; }
    }

    public sealed class EncodedDynamicFrameBundle
    {
        private readonly byte[] m_Payload;

        internal EncodedDynamicFrameBundle(DynamicFrameDescriptor descriptor, byte[] payload)
        {
            Descriptor = descriptor;
            m_Payload = payload;
        }

        public DynamicFrameDescriptor Descriptor { get; }
        public byte[] CopyPayload() => (byte[])m_Payload.Clone();
    }

    public static class DynamicFrameBundleCodec
    {
        private const uint Magic = 0x31424644;

        public static EncodedDynamicFrameBundle Encode(DynamicFrameBundle bundle)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));
            byte[] payload;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic);
                writer.Write(DynamicFrameDescriptor.SchemaVersion);
                WriteId(writer, bundle.Session.SessionId);
                writer.Write(bundle.Session.Epoch);
                WriteId(writer, bundle.TimelineId);
                writer.Write(bundle.PlaybackRevision.Value);
                writer.Write(bundle.FrameSequence);
                writer.Write(bundle.LogicalTime);
                writer.Write(bundle.Sample.Index);
                writer.Write(bundle.Sample.TemporalAlpha);
                writer.Write(bundle.SourceStateRevision.Value);
                writer.Write(bundle.Expectations.Count);
                for (int index = 0; index < bundle.Expectations.Count; index++)
                    WriteColumn(writer, bundle.Expectations[index], FindFrame(bundle, bundle.Expectations[index].ColumnId));
                writer.Flush();
                payload = stream.ToArray();
            }

            AssetHash hash = ComputeHash(payload);
            return new EncodedDynamicFrameBundle(new DynamicFrameDescriptor(DynamicFrameEncoding.Float32LittleEndian, payload.Length, bundle.ColumnFrames.Count, hash), payload);
        }

        public static DynamicFrameBundle Decode(DynamicFrameDescriptor descriptor, byte[] payload)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (descriptor.Encoding != DynamicFrameEncoding.Float32LittleEndian || payload.Length != descriptor.ByteLength || ComputeHash(payload) != descriptor.PayloadHash)
                throw new InvalidDataException("The dynamic frame payload does not match its descriptor.");

            using var stream = new MemoryStream(payload, false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadUInt32() != Magic || reader.ReadUInt16() != DynamicFrameDescriptor.SchemaVersion)
                throw new InvalidDataException("The dynamic frame payload header is incompatible.");
            SessionEpoch session = new(ReadId(reader), reader.ReadUInt64());
            ContractId timelineId = ReadId(reader);
            ScopeRevision playbackRevision = new(reader.ReadUInt64());
            ulong frameSequence = reader.ReadUInt64();
            double logicalTime = reader.ReadDouble();
            RenderTemporalSample sample = new(reader.ReadInt32(), reader.ReadSingle());
            StateRevision stateRevision = new(reader.ReadUInt64());
            int columnCount = ReadCount(reader, 1);
            if (columnCount != descriptor.ColumnCount)
                throw new InvalidDataException("The descriptor column count does not match the payload.");
            var expectations = new DynamicColumnExpectation[columnCount];
            var frames = new ColumnFrame[columnCount];
            for (int index = 0; index < columnCount; index++)
                ReadColumn(reader, sample, stateRevision, out expectations[index], out frames[index]);
            if (stream.Position != stream.Length)
                throw new InvalidDataException("The dynamic frame payload contains trailing bytes.");
            return new DynamicFrameBundle(session, timelineId, playbackRevision, frameSequence, logicalTime, sample, stateRevision, expectations, frames);
        }

        private static ColumnFrame FindFrame(DynamicFrameBundle bundle, ContractId columnId)
        {
            for (int index = 0; index < bundle.ColumnFrames.Count; index++)
            {
                if (bundle.ColumnFrames[index].ColumnId == columnId)
                    return bundle.ColumnFrames[index];
            }

            throw new InvalidDataException("The bundle manifest and frames differ.");
        }

        private static AssetHash ComputeHash(byte[] payload)
        {
            using SHA256 sha256 = SHA256.Create();
            return AssetHash.FromBytes(sha256.ComputeHash(payload));
        }

        private static void WriteColumn(BinaryWriter writer, DynamicColumnExpectation expectation, ColumnFrame frame)
        {
            WriteId(writer, expectation.ColumnId);
            writer.Write((byte)expectation.Content);
            writer.Write(expectation.CutIds.Count);
            for (int index = 0; index < expectation.CutIds.Count; index++)
                WriteId(writer, expectation.CutIds[index]);
            WriteHash(writer, frame.SurfaceAssetHash);
            writer.Write(frame.VisualParametersRevision.Value);
            if ((expectation.Content & DynamicColumnContent.Surface) != 0)
                WriteSurface(writer, frame.Surface.Value);
            if ((expectation.Content & DynamicColumnContent.Sites) != 0)
                WriteSites(writer, frame.Sites.Value);
            for (int index = 0; index < expectation.CutIds.Count; index++)
                WriteOverlay(writer, FindOverlay(frame, expectation.CutIds[index]));
        }

        private static void WriteSurface(BinaryWriter writer, SurfaceFrame frame)
        {
            writer.Write((byte)frame.TemporalApplication);
            writer.Write(frame.VertexCount);
            WriteFloat32(writer, frame.ActivityValues);
            WriteFloat32(writer, frame.OpacityValues);
            WriteRaw(writer, frame.ActiveMask.AsReadOnlySpan());
        }

        private static void WriteSites(BinaryWriter writer, SiteRenderFrame frame)
        {
            WriteHash(writer, frame.SiteAssetHash);
            writer.Write((byte)frame.TemporalApplication);
            writer.Write(frame.SiteCount);
            if (BitConverter.IsLittleEndian)
                WriteRaw(writer, frame.Positions.AsReadOnlySpan());
            else
            {
                for (int index = 0; index < frame.SiteCount; index++)
                {
                    writer.Write(frame.Positions[index].X);
                    writer.Write(frame.Positions[index].Y);
                    writer.Write(frame.Positions[index].Z);
                }
            }

            WriteRaw(writer, frame.Colors.AsReadOnlySpan());
            WriteFloat32(writer, frame.Sizes);
            WriteRaw(writer, frame.Visibility.AsReadOnlySpan());
            WriteRaw(writer, frame.Flags.AsReadOnlySpan());
        }

        private static void WriteOverlay(BinaryWriter writer, CutOverlayFrame overlay)
        {
            writer.Write(overlay.Width);
            writer.Write(overlay.Height);
            writer.Write((byte)overlay.TemporalApplication);
            writer.Write(overlay.MappingRevision.Value);
            WriteRaw(writer, overlay.Pixels.AsReadOnlySpan());
        }

        private static CutOverlayFrame FindOverlay(ColumnFrame frame, ContractId cutId)
        {
            for (int index = 0; index < frame.CutOverlays.Count; index++)
            {
                if (frame.CutOverlays[index].CutId == cutId)
                    return frame.CutOverlays[index];
            }

            throw new InvalidDataException("The bundle is missing an expected cut overlay.");
        }

        private static void ReadColumn(BinaryReader reader, RenderTemporalSample sample, StateRevision stateRevision, out DynamicColumnExpectation expectation, out ColumnFrame frame)
        {
            ContractId columnId = ReadId(reader);
            DynamicColumnContent content = (DynamicColumnContent)reader.ReadByte();
            int cutCount = ReadCount(reader, ContractId.ByteLength, true);
            ContractId[] cutIds = new ContractId[cutCount];
            for (int index = 0; index < cutCount; index++)
                cutIds[index] = ReadId(reader);
            expectation = new DynamicColumnExpectation(columnId, content, cutIds);
            AssetHash surfaceHash = ReadHash(reader);
            ScopeRevision visualRevision = new(reader.ReadUInt64());
            Optional<SurfaceFrame> surface = (content & DynamicColumnContent.Surface) != 0 ? Optional<SurfaceFrame>.Some(ReadSurface(reader, surfaceHash, sample, stateRevision)) : Optional<SurfaceFrame>.None;
            Optional<SiteRenderFrame> sites = (content & DynamicColumnContent.Sites) != 0 ? Optional<SiteRenderFrame>.Some(ReadSites(reader, sample, stateRevision)) : Optional<SiteRenderFrame>.None;
            CutOverlayFrame[] overlays = new CutOverlayFrame[cutCount];
            for (int index = 0; index < cutCount; index++)
                overlays[index] = ReadOverlay(reader, cutIds[index], columnId, sample, stateRevision);
            frame = new ColumnFrame(columnId, surfaceHash, visualRevision, surface, sites, overlays);
        }

        private static SurfaceFrame ReadSurface(BinaryReader reader, AssetHash surfaceHash, RenderTemporalSample sample, StateRevision stateRevision)
        {
            TemporalApplication temporal = ReadTemporalApplication(reader, TemporalApplication.SampleAndHold);
            int count = ReadCount(reader, (sizeof(float) * 2) + sizeof(byte));
            float[] activity = ReadFloat32(reader, count);
            float[] opacity = ReadFloat32(reader, count);
            byte[] mask = ReadRaw<byte>(reader, count);
            return new SurfaceFrame(surfaceHash, stateRevision, sample, temporal, RenderBuffer<float>.TakeOwnership(activity), RenderBuffer<float>.TakeOwnership(opacity), RenderBuffer<byte>.TakeOwnership(mask));
        }

        private static SiteRenderFrame ReadSites(BinaryReader reader, RenderTemporalSample sample, StateRevision stateRevision)
        {
            AssetHash siteHash = ReadHash(reader);
            TemporalApplication temporal = ReadTemporalApplication(reader, TemporalApplication.Linear);
            int count = ReadCount(reader, 22);
            Float3[] positions;
            if (BitConverter.IsLittleEndian)
            {
                positions = ReadRaw<Float3>(reader, count);
            }
            else
            {
                positions = new Float3[count];
                for (int index = 0; index < count; index++)
                    positions[index] = new Float3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }

            Rgba32[] colors = ReadRaw<Rgba32>(reader, count);
            float[] sizes = ReadFloat32(reader, count);
            byte[] visibility = ReadRaw<byte>(reader, count);
            SiteRenderFlags[] flags = ReadRaw<SiteRenderFlags>(reader, count);
            return new SiteRenderFrame(siteHash, stateRevision, sample, temporal, RenderBuffer<Float3>.TakeOwnership(positions), RenderBuffer<Rgba32>.TakeOwnership(colors), RenderBuffer<float>.TakeOwnership(sizes), RenderBuffer<byte>.TakeOwnership(visibility), RenderBuffer<SiteRenderFlags>.TakeOwnership(flags));
        }

        private static CutOverlayFrame ReadOverlay(BinaryReader reader, ContractId cutId, ContractId columnId, RenderTemporalSample sample, StateRevision stateRevision)
        {
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            if (width <= 0 || height <= 0)
                throw new InvalidDataException("Cut overlay dimensions must be positive.");
            TemporalApplication temporal = ReadTemporalApplication(reader, TemporalApplication.SampleAndHold);
            ScopeRevision mappingRevision = new(reader.ReadUInt64());
            int count;
            try
            {
                count = checked(width * height);
            }
            catch (OverflowException exception)
            {
                throw new InvalidDataException("Cut overlay dimensions overflow.", exception);
            }

            EnsureRemaining(reader, count, sizeof(uint));
            Rgba32[] pixels = ReadRaw<Rgba32>(reader, count);
            return new CutOverlayFrame(cutId, columnId, stateRevision, width, height, sample, temporal, mappingRevision, RenderBuffer<Rgba32>.TakeOwnership(pixels));
        }

        private static TemporalApplication ReadTemporalApplication(BinaryReader reader, TemporalApplication expected)
        {
            TemporalApplication value = (TemporalApplication)reader.ReadByte();
            if (value != expected)
                throw new InvalidDataException("The dynamic frame violates P03 temporal semantics.");
            return value;
        }

        private static int ReadCount(BinaryReader reader, int minimumItemBytes, bool allowZero = false)
        {
            int count = reader.ReadInt32();
            if (count < 0 || (!allowZero && count == 0))
                throw new InvalidDataException("A dynamic frame contains an invalid element count.");
            EnsureRemaining(reader, count, minimumItemBytes);
            return count;
        }

        private static void EnsureRemaining(BinaryReader reader, int count, int minimumItemBytes)
        {
            long required;
            try
            {
                required = checked((long)count * minimumItemBytes);
            }
            catch (OverflowException exception)
            {
                throw new InvalidDataException("A dynamic frame element count overflows.", exception);
            }

            if (reader.BaseStream.Length - reader.BaseStream.Position < required)
                throw new InvalidDataException("A dynamic frame element count exceeds its payload.");
        }

        private static void WriteId(BinaryWriter writer, ContractId id)
        {
            byte[] bytes = new byte[ContractId.ByteLength];
            id.WriteBytes(bytes);
            writer.Write(bytes);
        }

        private static ContractId ReadId(BinaryReader reader)
        {
            byte[] bytes = new byte[ContractId.ByteLength];
            ReadExactly(reader, bytes);
            return ContractId.FromBytes(bytes);
        }

        private static void WriteHash(BinaryWriter writer, AssetHash hash)
        {
            byte[] bytes = new byte[AssetHash.ByteLength];
            hash.WriteBytes(bytes);
            writer.Write(bytes);
        }

        private static AssetHash ReadHash(BinaryReader reader)
        {
            byte[] bytes = new byte[AssetHash.ByteLength];
            ReadExactly(reader, bytes);
            return AssetHash.FromBytes(bytes);
        }

        private static void ReadExactly(BinaryReader reader, byte[] destination)
        {
            ReadExactly(reader.BaseStream, destination);
        }

        private static void WriteFloat32(BinaryWriter writer, RenderBuffer<float> values)
        {
            if (BitConverter.IsLittleEndian)
            {
                WriteRaw(writer, values.AsReadOnlySpan());
                return;
            }

            for (int index = 0; index < values.Count; index++)
                writer.Write(values[index]);
        }

        private static float[] ReadFloat32(BinaryReader reader, int count)
        {
            if (!BitConverter.IsLittleEndian)
            {
                float[] slowValues = new float[count];
                for (int index = 0; index < count; index++)
                    slowValues[index] = reader.ReadSingle();
                return slowValues;
            }

            return ReadRaw<float>(reader, count);
        }

        private static void WriteRaw<T>(BinaryWriter writer, ReadOnlySpan<T> values) where T : struct
        {
            writer.BaseStream.Write(MemoryMarshal.AsBytes(values));
        }

        private static T[] ReadRaw<T>(BinaryReader reader, int count) where T : struct
        {
            T[] values = new T[count];
            ReadExactly(reader.BaseStream, MemoryMarshal.AsBytes(values.AsSpan()));
            return values;
        }

        private static void ReadExactly(Stream stream, Span<byte> destination)
        {
            int offset = 0;
            while (offset < destination.Length)
            {
                int read = stream.Read(destination.Slice(offset));
                if (read == 0)
                    throw new EndOfStreamException();
                offset += read;
            }
        }
    }
}
