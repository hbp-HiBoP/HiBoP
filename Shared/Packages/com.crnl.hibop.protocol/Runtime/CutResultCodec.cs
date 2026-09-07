using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;

namespace CRNL.HiBoP.Protocol
{
    public sealed class CutResultDescriptor
    {
        public const ushort SchemaVersion = 1;

        public CutResultDescriptor(int byteLength, AssetHash payloadHash)
        {
            if (byteLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(byteLength));
            if (!payloadHash.IsValid)
                throw new ArgumentException("A valid payload hash is required.", nameof(payloadHash));
            ByteLength = byteLength;
            PayloadHash = payloadHash;
        }

        public int ByteLength { get; }
        public AssetHash PayloadHash { get; }
    }

    public sealed class EncodedCutResult
    {
        internal EncodedCutResult(CutResultDescriptor descriptor, byte[] payload)
        {
            Descriptor = descriptor;
            Payload = payload;
        }

        public CutResultDescriptor Descriptor { get; }
        public byte[] Payload { get; }
    }

    public sealed class DecodedCutResult
    {
        internal DecodedCutResult(SessionEpoch session, CutRenderResult result, CutResultManifest manifest)
        {
            Session = session;
            Result = result;
            Manifest = manifest;
        }

        public SessionEpoch Session { get; }
        public CutRenderResult Result { get; }
        public CutResultManifest Manifest { get; }
    }

    public static class CutResultCodec
    {
        private const uint Magic = 0x32545543; // CUT2

        public static EncodedCutResult Encode(SessionEpoch session, CutRenderResult result, CutResultManifest manifest)
        {
            if (!session.IsValid)
                throw new ArgumentException("A valid session is required.", nameof(session));
            if (result == null || manifest == null)
                throw new ArgumentNullException(result == null ? nameof(result) : nameof(manifest));
            manifest.Validate(result);

            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                writer.Write(Magic);
                writer.Write(CutResultDescriptor.SchemaVersion);
                WriteId(writer, session.SessionId);
                writer.Write(session.Epoch);
                writer.Write(manifest.Width);
                writer.Write(manifest.Height);
                writer.Write(manifest.ColumnIds.Count);
                for (int index = 0; index < manifest.ColumnIds.Count; index++)
                    WriteId(writer, manifest.ColumnIds[index]);

                WriteId(writer, result.CutId);
                WriteId(writer, result.InteractionId);
                writer.Write(result.Sequence.Value);
                writer.Write(result.CutRevision.Value);
                writer.Write(result.RenderRevision.Value);
                writer.Write(result.SourceStateRevision.Value);
                writer.Write(result.Sample.Index);
                writer.Write(result.Sample.TemporalAlpha);
                WriteFloat3(writer, result.Plane.Normal);
                writer.Write(result.Plane.Distance);
                WriteHash(writer, result.GeometryHash);
                writer.Write(result.Geometry.HasValue);
                if (result.Geometry.HasValue)
                    WriteGeometry(writer, result.Geometry.Value);
                WriteHash(writer, result.BaseTextureHash);
                writer.Write(result.BaseTexture.HasValue);
                if (result.BaseTexture.HasValue)
                    WriteTexture(writer, result.BaseTexture.Value);
                writer.Write(result.Overlays.Count);
                for (int index = 0; index < result.Overlays.Count; index++)
                    WriteOverlay(writer, result.Overlays[index]);
            }

            byte[] payload = stream.ToArray();
            AssetHash hash = ComputeHash(payload);
            return new EncodedCutResult(new CutResultDescriptor(payload.Length, hash), payload);
        }

        public static DecodedCutResult Decode(CutResultDescriptor descriptor, byte[] payload, int maximumPayloadBytes)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (maximumPayloadBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumPayloadBytes));
            if (descriptor.ByteLength != payload.Length || payload.Length > maximumPayloadBytes)
                throw new InvalidDataException("The cut result violates its descriptor or explicit payload budget.");
            if (ComputeHash(payload) != descriptor.PayloadHash)
                throw new InvalidDataException("The cut result payload hash is invalid.");

            using var stream = new MemoryStream(payload, false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadUInt32() != Magic || reader.ReadUInt16() != CutResultDescriptor.SchemaVersion)
                throw new InvalidDataException("Unsupported cut result schema.");
            SessionEpoch session = new(ReadId(reader), reader.ReadUInt64());
            int width = ReadPositive(reader, "width");
            int height = ReadPositive(reader, "height");
            int columnCount = ReadCount(reader, 65_536, "column count");
            var columnIds = new ContractId[columnCount];
            for (int index = 0; index < columnIds.Length; index++)
                columnIds[index] = ReadId(reader);
            var manifest = new CutResultManifest(columnIds, width, height);

            ContractId cutId = ReadId(reader);
            ContractId interactionId = ReadId(reader);
            InteractionSequence sequence = new(reader.ReadUInt64());
            ScopeRevision cutRevision = new(reader.ReadUInt64());
            ScopeRevision renderRevision = new(reader.ReadUInt64());
            StateRevision stateRevision = new(reader.ReadUInt64());
            RenderTemporalSample sample = new(reader.ReadInt32(), reader.ReadSingle());
            Plane3F plane = new(ReadFloat3(reader), reader.ReadSingle());
            AssetHash geometryHash = ReadHash(reader);
            Optional<CutGeometryAsset> geometry = reader.ReadBoolean() ? Optional<CutGeometryAsset>.Some(ReadGeometry(reader, geometryHash)) : Optional<CutGeometryAsset>.None;
            AssetHash baseHash = ReadHash(reader);
            Optional<TextureAsset> baseTexture = reader.ReadBoolean() ? Optional<TextureAsset>.Some(ReadTexture(reader, baseHash)) : Optional<TextureAsset>.None;
            int overlayCount = ReadCount(reader, columnCount, "overlay count");
            var overlays = new CutOverlayFrame[overlayCount];
            for (int index = 0; index < overlays.Length; index++)
                overlays[index] = ReadOverlay(reader, cutId, stateRevision, sample, width, height);
            if (stream.Position != stream.Length)
                throw new InvalidDataException("The cut result contains trailing bytes.");

            var result = new CutRenderResult(cutId, interactionId, sequence, cutRevision, renderRevision, stateRevision, sample, plane, geometryHash, geometry, baseHash, baseTexture, overlays);
            manifest.Validate(result);
            return new DecodedCutResult(session, result, manifest);
        }

        private static void WriteGeometry(BinaryWriter writer, CutGeometryAsset geometry)
        {
            if (geometry.CoordinateSpace.Handedness != CoordinateHandedness.Left || geometry.CoordinateSpace.AxisOrder != CoordinateAxisOrder.Xyz || geometry.CoordinateSpace.Unit != LengthUnit.Millimeter || geometry.CoordinateSpace.MetersPerUnit != 0.001f || geometry.CoordinateSpace.MappingVersion != 1 || !geometry.CoordinateSpace.AssetToBrain.Equals(Matrix4x4F.Identity))
                throw new ArgumentException("Only the canonical P03 cut coordinate space can be transported.", nameof(geometry));
            WriteFloat3(writer, geometry.Bounds.Minimum);
            WriteFloat3(writer, geometry.Bounds.Maximum);
            writer.Write(geometry.Positions.Count);
            writer.Write(geometry.Indices.Count);
            for (int index = 0; index < geometry.Positions.Count; index++)
            {
                WriteFloat3(writer, geometry.Positions[index]);
                WriteFloat3(writer, geometry.Normals[index]);
                writer.Write(geometry.Uvs[index].X);
                writer.Write(geometry.Uvs[index].Y);
            }

            for (int index = 0; index < geometry.Indices.Count; index++)
                writer.Write(geometry.Indices[index]);
        }

        private static CutGeometryAsset ReadGeometry(BinaryReader reader, AssetHash hash)
        {
            Bounds3F bounds = new(ReadFloat3(reader), ReadFloat3(reader));
            int vertexCount = ReadCount(reader, 16_777_216, "cut vertex count");
            int indexCount = ReadCount(reader, 50_331_648, "cut index count");
            var positions = new Float3[vertexCount];
            var normals = new Float3[vertexCount];
            var uvs = new Float2[vertexCount];
            for (int index = 0; index < vertexCount; index++)
            {
                positions[index] = ReadFloat3(reader);
                normals[index] = ReadFloat3(reader);
                uvs[index] = new Float2(reader.ReadSingle(), reader.ReadSingle());
            }

            var indices = new uint[indexCount];
            for (int index = 0; index < indexCount; index++)
                indices[index] = reader.ReadUInt32();
            return new CutGeometryAsset(hash, CoordinateSpace.DesktopUnityMillimetersV1, bounds, RenderBuffer<Float3>.TakeOwnership(positions), RenderBuffer<Float3>.TakeOwnership(normals), RenderBuffer<Float2>.TakeOwnership(uvs), RenderBuffer<uint>.TakeOwnership(indices));
        }

        private static void WriteTexture(BinaryWriter writer, TextureAsset texture)
        {
            writer.Write(texture.Width);
            writer.Write(texture.Height);
            writer.Write((byte)texture.ColorSpace);
            WritePixels(writer, texture.Pixels);
        }

        private static TextureAsset ReadTexture(BinaryReader reader, AssetHash hash)
        {
            int width = ReadPositive(reader, "base width");
            int height = ReadPositive(reader, "base height");
            TextureColorSpace colorSpace = (TextureColorSpace)reader.ReadByte();
            return new TextureAsset(hash, width, height, colorSpace, ReadPixels(reader, width, height));
        }

        private static void WriteOverlay(BinaryWriter writer, CutOverlayFrame overlay)
        {
            WriteId(writer, overlay.ColumnId);
            writer.Write(overlay.MappingRevision.Value);
            WritePixels(writer, overlay.Pixels);
        }

        private static CutOverlayFrame ReadOverlay(BinaryReader reader, ContractId cutId, StateRevision stateRevision, RenderTemporalSample sample, int width, int height)
        {
            ContractId columnId = ReadId(reader);
            ScopeRevision mappingRevision = new(reader.ReadUInt64());
            return new CutOverlayFrame(cutId, columnId, stateRevision, width, height, sample, TemporalApplication.SampleAndHold, mappingRevision, ReadPixels(reader, width, height));
        }

        private static void WritePixels(BinaryWriter writer, RenderBuffer<Rgba32> pixels)
        {
            writer.Write(pixels.Count);
            writer.BaseStream.Write(MemoryMarshal.AsBytes(pixels.AsReadOnlySpan()));
        }

        private static RenderBuffer<Rgba32> ReadPixels(BinaryReader reader, int width, int height)
        {
            int expected = checked(width * height);
            if (reader.ReadInt32() != expected)
                throw new InvalidDataException("The pixel count does not match its declared dimensions.");
            var pixels = new Rgba32[expected];
            ReadExactly(reader.BaseStream, MemoryMarshal.AsBytes(pixels.AsSpan()));
            return RenderBuffer<Rgba32>.TakeOwnership(pixels);
        }

        private static void WriteId(BinaryWriter writer, ContractId id)
        {
            var bytes = new byte[ContractId.ByteLength];
            id.WriteBytes(bytes);
            writer.Write(bytes);
        }

        private static ContractId ReadId(BinaryReader reader) => ContractId.FromBytes(ReadExact(reader, ContractId.ByteLength));

        private static void WriteHash(BinaryWriter writer, AssetHash hash)
        {
            var bytes = new byte[AssetHash.ByteLength];
            hash.WriteBytes(bytes);
            writer.Write(bytes);
        }

        private static AssetHash ReadHash(BinaryReader reader) => AssetHash.FromBytes(ReadExact(reader, AssetHash.ByteLength));

        private static void WriteFloat3(BinaryWriter writer, Float3 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        private static Float3 ReadFloat3(BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        private static int ReadPositive(BinaryReader reader, string name)
        {
            int value = reader.ReadInt32();
            if (value <= 0) throw new InvalidDataException($"Invalid {name}.");
            return value;
        }

        private static int ReadCount(BinaryReader reader, int maximum, string name)
        {
            int value = reader.ReadInt32();
            if (value < 0 || value > maximum) throw new InvalidDataException($"Invalid {name}.");
            return value;
        }

        private static byte[] ReadExact(BinaryReader reader, int count)
        {
            byte[] bytes = reader.ReadBytes(count);
            if (bytes.Length != count) throw new EndOfStreamException();
            return bytes;
        }

        private static AssetHash ComputeHash(byte[] bytes)
        {
            using SHA256 algorithm = SHA256.Create();
            return AssetHash.FromBytes(algorithm.ComputeHash(bytes));
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
