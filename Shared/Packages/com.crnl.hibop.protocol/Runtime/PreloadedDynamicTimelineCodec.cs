using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;

namespace CRNL.HiBoP.Protocol
{
    public sealed class PreloadedTimelineDescriptor
    {
        public const ushort SchemaVersion = 1;

        public PreloadedTimelineDescriptor(long byteLength, int indexCount, int columnCount, AssetHash payloadHash)
        {
            if (byteLength <= 0 || indexCount <= 0 || columnCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(byteLength));
            if (!payloadHash.IsValid)
                throw new ArgumentException("A valid payload hash is required.", nameof(payloadHash));
            ByteLength = byteLength;
            IndexCount = indexCount;
            ColumnCount = columnCount;
            PayloadHash = payloadHash;
        }

        public long ByteLength { get; }
        public int IndexCount { get; }
        public int ColumnCount { get; }
        public AssetHash PayloadHash { get; }
    }

    public static class PreloadedDynamicTimelineCodec
    {
        private const uint Magic = 0x4c544248;
        private const int HashBufferBytes = 64 * 1024;

        public static PreloadedTimelineDescriptor Write(Stream destination, PreloadedDynamicTimeline timeline)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            EnsureSeekable(destination, true);

            long start = destination.Position;
            using (var writer = new BinaryWriter(destination, System.Text.Encoding.UTF8, true))
            {
                writer.Write(Magic);
                writer.Write(PreloadedTimelineDescriptor.SchemaVersion);
                WriteId(writer, timeline.Session.SessionId);
                writer.Write(timeline.Session.Epoch);
                WriteId(writer, timeline.TimelineId);
                writer.Write(timeline.SourceStateRevision.Value);
                writer.Write(timeline.IndexCount);
                for (int index = 0; index < timeline.IndexCount; index++)
                {
                    writer.Write(timeline.Indices[index].LogicalTime);
                    writer.Write(timeline.Indices[index].Sample.Index);
                    writer.Write(timeline.Indices[index].Sample.TemporalAlpha);
                }

                writer.Write(timeline.Columns.Count);
                for (int index = 0; index < timeline.Columns.Count; index++)
                    WriteColumn(writer, timeline.Columns[index]);
                writer.Flush();
            }

            long end = destination.Position;
            AssetHash hash = ComputeHash(destination, start, end - start);
            destination.Position = end;
            return new PreloadedTimelineDescriptor(end - start, timeline.IndexCount, timeline.Columns.Count, hash);
        }

        public static PreloadedDynamicTimeline Read(Stream source, PreloadedTimelineDescriptor descriptor, long maximumUniquePayloadBytes)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            if (maximumUniquePayloadBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumUniquePayloadBytes));
            EnsureSeekable(source, false);
            long start = source.Position;
            if (descriptor.ByteLength > source.Length - start || ComputeHash(source, start, descriptor.ByteLength) != descriptor.PayloadHash)
                throw new InvalidDataException("The preloaded timeline archive does not match its descriptor.");
            source.Position = start;

            using var reader = new BinaryReader(source, System.Text.Encoding.UTF8, true);
            if (reader.ReadUInt32() != Magic || reader.ReadUInt16() != PreloadedTimelineDescriptor.SchemaVersion)
                throw new InvalidDataException("The preloaded timeline archive header is incompatible.");
            SessionEpoch session = new(ReadId(reader), reader.ReadUInt64());
            ContractId timelineId = ReadId(reader);
            StateRevision stateRevision = new(reader.ReadUInt64());
            int indexCount = ReadCount(reader, int.MaxValue);
            if (indexCount != descriptor.IndexCount)
                throw new InvalidDataException("The timeline index count does not match its descriptor.");
            EnsureRemaining(reader, checked((long)indexCount * (sizeof(double) + sizeof(int) + sizeof(float)) + sizeof(int)));
            var indices = new PreloadedTimelineIndex[indexCount];
            for (int index = 0; index < indexCount; index++)
                indices[index] = new PreloadedTimelineIndex(reader.ReadDouble(), new RenderTemporalSample(reader.ReadInt32(), reader.ReadSingle()));

            int columnCount = ReadCount(reader, int.MaxValue);
            if (columnCount != descriptor.ColumnCount)
                throw new InvalidDataException("The timeline column count does not match its descriptor.");
            EnsureRemaining(reader, checked((long)columnCount * (ContractId.ByteLength + 1 + AssetHash.ByteLength + sizeof(ulong) + sizeof(int))));
            var columns = new PreloadedColumnTimeline[columnCount];
            long uniquePayloadBytes = 0;
            for (int index = 0; index < columnCount; index++)
                columns[index] = ReadColumn(reader, indexCount, maximumUniquePayloadBytes, ref uniquePayloadBytes);
            if (source.Position - start != descriptor.ByteLength)
                throw new InvalidDataException("The preloaded timeline archive contains trailing or missing bytes.");
            return new PreloadedDynamicTimeline(session, timelineId, stateRevision, indices, columns);
        }

        private static void WriteColumn(BinaryWriter writer, PreloadedColumnTimeline column)
        {
            WriteId(writer, column.ColumnId);
            writer.Write((byte)column.Content);
            WriteHash(writer, column.SurfaceAssetHash);
            writer.Write(column.VisualParametersRevision.Value);
            if ((column.Content & DynamicColumnContent.Surface) != 0)
            {
                WriteChannel(writer, column.SurfaceActivity, sizeof(float));
                WriteChannel(writer, column.SurfaceOpacity, sizeof(float));
                WriteChannel(writer, column.SurfaceMask, sizeof(byte));
            }

            if ((column.Content & DynamicColumnContent.Sites) != 0)
            {
                WriteHash(writer, column.SiteAssetHash);
                WriteChannel(writer, column.SitePositions, 12);
                WriteChannel(writer, column.SiteColors, 4);
                WriteChannel(writer, column.SiteSizes, sizeof(float));
                WriteChannel(writer, column.SiteVisibility, sizeof(byte));
                WriteChannel(writer, column.SiteFlags, sizeof(byte));
            }

            writer.Write(column.Cuts.Count);
            for (int index = 0; index < column.Cuts.Count; index++)
            {
                PreloadedCutTimeline cut = column.Cuts[index];
                WriteId(writer, cut.CutId);
                writer.Write(cut.Width);
                writer.Write(cut.Height);
                writer.Write(cut.MappingRevision.Value);
                WriteChannel(writer, cut.Pixels, 4);
            }
        }

        private static PreloadedColumnTimeline ReadColumn(BinaryReader reader, int indexCount, long maximumUniquePayloadBytes, ref long uniquePayloadBytes)
        {
            ContractId columnId = ReadId(reader);
            DynamicColumnContent content = (DynamicColumnContent)reader.ReadByte();
            if ((content & ~(DynamicColumnContent.Surface | DynamicColumnContent.Sites)) != 0)
                throw new InvalidDataException("The preloaded column content flags are invalid.");
            AssetHash surfaceHash = ReadHash(reader);
            ScopeRevision visualRevision = new(reader.ReadUInt64());
            PreloadedTimelineChannel<float> surfaceActivity = null;
            PreloadedTimelineChannel<float> surfaceOpacity = null;
            PreloadedTimelineChannel<byte> surfaceMask = null;
            if ((content & DynamicColumnContent.Surface) != 0)
            {
                surfaceActivity = ReadChannel<float>(reader, indexCount, sizeof(float), maximumUniquePayloadBytes, ref uniquePayloadBytes);
                surfaceOpacity = ReadChannel<float>(reader, indexCount, sizeof(float), maximumUniquePayloadBytes, ref uniquePayloadBytes);
                surfaceMask = ReadChannel<byte>(reader, indexCount, sizeof(byte), maximumUniquePayloadBytes, ref uniquePayloadBytes);
            }

            AssetHash siteHash = default;
            PreloadedTimelineChannel<Float3> sitePositions = null;
            PreloadedTimelineChannel<Rgba32> siteColors = null;
            PreloadedTimelineChannel<float> siteSizes = null;
            PreloadedTimelineChannel<byte> siteVisibility = null;
            PreloadedTimelineChannel<SiteRenderFlags> siteFlags = null;
            if ((content & DynamicColumnContent.Sites) != 0)
            {
                siteHash = ReadHash(reader);
                sitePositions = ReadChannel<Float3>(reader, indexCount, 12, maximumUniquePayloadBytes, ref uniquePayloadBytes);
                siteColors = ReadChannel<Rgba32>(reader, indexCount, 4, maximumUniquePayloadBytes, ref uniquePayloadBytes);
                siteSizes = ReadChannel<float>(reader, indexCount, sizeof(float), maximumUniquePayloadBytes, ref uniquePayloadBytes);
                siteVisibility = ReadChannel<byte>(reader, indexCount, sizeof(byte), maximumUniquePayloadBytes, ref uniquePayloadBytes);
                siteFlags = ReadChannel<SiteRenderFlags>(reader, indexCount, sizeof(byte), maximumUniquePayloadBytes, ref uniquePayloadBytes);
            }

            int cutCount = ReadCount(reader, int.MaxValue, true);
            EnsureRemaining(reader, checked((long)cutCount * (ContractId.ByteLength + (sizeof(int) * 2) + sizeof(ulong) + (sizeof(int) * 2))));
            var cutIds = new ContractId[cutCount];
            var cuts = new PreloadedCutTimeline[cutCount];
            for (int index = 0; index < cutCount; index++)
            {
                ContractId cutId = ReadId(reader);
                int width = reader.ReadInt32();
                int height = reader.ReadInt32();
                ScopeRevision mappingRevision = new(reader.ReadUInt64());
                PreloadedTimelineChannel<Rgba32> pixels = ReadChannel<Rgba32>(reader, indexCount, 4, maximumUniquePayloadBytes, ref uniquePayloadBytes);
                cutIds[index] = cutId;
                cuts[index] = new PreloadedCutTimeline(cutId, width, height, mappingRevision, pixels);
            }

            return new PreloadedColumnTimeline(columnId, content, surfaceHash, visualRevision, siteHash, cutIds, surfaceActivity, surfaceOpacity, surfaceMask, sitePositions, siteColors, siteSizes, siteVisibility, siteFlags, cuts);
        }

        private static void WriteChannel<T>(BinaryWriter writer, PreloadedTimelineChannel<T> channel, int expectedElementByteSize) where T : struct
        {
            if (channel.ElementByteSize != expectedElementByteSize)
                throw new InvalidDataException("A timeline channel has an unexpected element layout.");
            writer.Write(channel.ElementCount);
            writer.Write(channel.UniqueSliceCount);
            int[] sliceIndices = channel.CopySliceIndices();
            for (int index = 0; index < sliceIndices.Length; index++)
                writer.Write(sliceIndices[index]);
            for (int index = 0; index < channel.UniqueSlices.Count; index++)
                writer.BaseStream.Write(MemoryMarshal.AsBytes(channel.UniqueSlices[index].AsReadOnlySpan()));
        }

        private static PreloadedTimelineChannel<T> ReadChannel<T>(BinaryReader reader, int indexCount, int elementByteSize, long maximumUniquePayloadBytes, ref long uniquePayloadBytes) where T : struct
        {
            int elementCount = ReadCount(reader, int.MaxValue);
            int uniqueCount = ReadCount(reader, indexCount);
            long channelBytes;
            long uniqueSliceBytes;
            try
            {
                uniqueSliceBytes = checked((long)elementCount * elementByteSize * uniqueCount);
                channelBytes = checked(((long)indexCount * sizeof(int)) + uniqueSliceBytes);
                uniquePayloadBytes = checked(uniquePayloadBytes + uniqueSliceBytes);
            }
            catch (OverflowException exception)
            {
                throw new InvalidDataException("A preloaded timeline channel size overflows.", exception);
            }

            if (uniquePayloadBytes > maximumUniquePayloadBytes)
                throw new InvalidDataException($"The timeline unique payload exceeds the explicit {maximumUniquePayloadBytes}-byte memory budget.");

            EnsureRemaining(reader, channelBytes);
            var sliceIndices = new int[indexCount];
            for (int index = 0; index < indexCount; index++)
            {
                int sliceIndex = reader.ReadInt32();
                if (sliceIndex < 0 || sliceIndex >= uniqueCount)
                    throw new InvalidDataException("A timeline channel references an invalid unique slice.");
                sliceIndices[index] = sliceIndex;
            }

            var slices = new RenderBuffer<T>[uniqueCount];
            for (int index = 0; index < uniqueCount; index++)
            {
                T[] values = new T[elementCount];
                ReadExactly(reader.BaseStream, MemoryMarshal.AsBytes(values.AsSpan()));
                slices[index] = RenderBuffer<T>.TakeOwnership(values);
            }

            return new PreloadedTimelineChannel<T>(elementCount, elementByteSize, slices, sliceIndices);
        }

        private static AssetHash ComputeHash(Stream stream, long start, long count)
        {
            long previous = stream.Position;
            try
            {
                stream.Position = start;
                using SHA256 sha256 = SHA256.Create();
                byte[] buffer = new byte[HashBufferBytes];
                long remaining = count;
                while (remaining > 0)
                {
                    int requested = (int)Math.Min(buffer.Length, remaining);
                    int read = stream.Read(buffer, 0, requested);
                    if (read == 0)
                        throw new EndOfStreamException();
                    sha256.TransformBlock(buffer, 0, read, buffer, 0);
                    remaining -= read;
                }

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return AssetHash.FromBytes(sha256.Hash);
            }
            finally
            {
                stream.Position = previous;
            }
        }

        private static int ReadCount(BinaryReader reader, int maximum, bool allowZero = false)
        {
            int value = reader.ReadInt32();
            if (value < 0 || (!allowZero && value == 0) || value > maximum)
                throw new InvalidDataException("A preloaded timeline count is outside its supported envelope.");
            return value;
        }

        private static void EnsureRemaining(BinaryReader reader, long requiredBytes)
        {
            if (requiredBytes < 0 || reader.BaseStream.Length - reader.BaseStream.Position < requiredBytes)
                throw new InvalidDataException("A preloaded timeline count exceeds the remaining archive length.");
        }

        private static void EnsureSeekable(Stream stream, bool write)
        {
            if (!stream.CanSeek || !stream.CanRead || (write && !stream.CanWrite))
                throw new ArgumentException("Preloaded timeline archives require a seekable readable stream.", nameof(stream));
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
            ReadExactly(reader.BaseStream, bytes);
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
            ReadExactly(reader.BaseStream, bytes);
            return AssetHash.FromBytes(bytes);
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
