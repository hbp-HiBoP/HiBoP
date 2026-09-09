using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HBP.Transfer.Anatomy;
using NUnit.Framework;

namespace HBP.Tests.Transfer.Anatomy
{
    public class AnatomySnapshotTests
    {
        private static float[] Matrix() => new float[] { -1, 0, 0, 12.25f, 0, 2, 0, -3.5f, 0, 0, 1, 7, 0, 0, 0, 1 };
        private static float[] Positions() => new float[] { BitConverter.Int32BitsToSingle(unchecked((int)0x80000000)), 0, float.Epsilon, 1.25f, 0, 0, 0, -2.5f, 0 };
        private static float[] Normals() => new float[] { 0, 0, 1, 0, 0, 1, 0, 0, 1 };
        private static float[] Color() => new float[] { 0.25f, 0.5f, 0.75f, 1 };
        private static AnatomyCoordinateSpace Coordinates() => new AnatomyCoordinateSpace("fixture-brain-XYZ", AnatomyHandedness.Left, AnatomyLengthUnit.Millimeter, 1, Matrix());
        private static AnatomySnapshot Example(bool uvs = true) => AnatomySnapshot.Create("example-transfer-005", "example-session-005", "quest-001-visualization", "quest-001-column", 1, Coordinates(), AnatomyWinding.Clockwise, true, Color(), Positions(), Normals(), new uint[] { 0, 1, 2 }, uvs ? new float[] { 0, 0, 1, 0, 0, 1 } : Array.Empty<float>());

        [TestCase(true)]
        [TestCase(false)]
        public void RoundTripPreservesEveryBitAndIdentity(bool uvs)
        {
            AnatomySnapshot source = Example(uvs);
            byte[] encoded = AnatomySnapshotCodec.Encode(source);
            AnatomySnapshot copy = AnatomySnapshotCodec.Decode(encoded);
            Assert.That(AnatomySnapshotCodec.Encode(copy), Is.EqualTo(encoded));
            Assert.That(copy.TransferId, Is.EqualTo(source.TransferId));
            Assert.That(copy.SessionId, Is.EqualTo(source.SessionId));
            Assert.That(copy.VisualizationId, Is.EqualTo("quest-001-visualization"));
            Assert.That(copy.ColumnId, Is.EqualTo("quest-001-column"));
            Assert.That(copy.ContentRevision, Is.EqualTo(1ul));
            Assert.That(copy.Coordinates.FrameId, Is.EqualTo(source.Coordinates.FrameId));
            Assert.That(copy.Coordinates.Handedness, Is.EqualTo(source.Coordinates.Handedness));
            Assert.That(copy.Coordinates.Unit, Is.EqualTo(source.Coordinates.Unit));
            Assert.That(copy.Coordinates.MappingVersion, Is.EqualTo(1));
            Assert.That(copy.Coordinates.MetersPerUnit, Is.EqualTo(0.001f));
            Assert.That(copy.Visible, Is.True);
            Assert.That(copy.Winding, Is.EqualTo(AnatomyWinding.Clockwise));
            Assert.That(copy.Indices.ToArray(), Is.EqualTo(source.Indices.ToArray()));
            AssertBits(copy.Positions, source.Positions);
            AssertBits(copy.Normals, source.Normals);
            AssertBits(copy.Uvs, source.Uvs);
            AssertBits(copy.Color, source.Color);
            AssertBits(copy.Coordinates.AssetToBrain, source.Coordinates.AssetToBrain);
            Assert.That(BitConverter.SingleToInt32Bits(copy.Positions[0]), Is.EqualTo(unchecked((int)0x80000000)));
            Assert.That(BitConverter.SingleToInt32Bits(copy.Positions[2]), Is.EqualTo(1));
        }

        [Test]
        public void PublicConstructionAndDecodedSnapshotOwnTheirBuffers()
        {
            float[] positions = Positions(), normals = Normals(), uvs = { 0, 0, 1, 0, 0, 1 }, color = Color(), matrix = Matrix();
            uint[] indices = { 0, 1, 2 };
            var coordinates = new AnatomyCoordinateSpace("frame", AnatomyHandedness.Right, AnatomyLengthUnit.Meter, 2, matrix);
            AnatomySnapshot source = AnatomySnapshot.Create("t", "s", "v", "c", ulong.MaxValue, coordinates, AnatomyWinding.CounterClockwise, false, color, positions, normals, indices, uvs);
            byte[] before = AnatomySnapshotCodec.Encode(source);
            positions[0] = normals[0] = uvs[0] = color[0] = matrix[0] = 99;
            indices[0] = 99;
            source.Positions.ToArray()[0] = 123;
            source.Coordinates.AssetToBrain.ToArray()[0] = 123;
            Assert.That(AnatomySnapshotCodec.Encode(source), Is.EqualTo(before));
            AnatomySnapshot copy = AnatomySnapshotCodec.Decode(before);
            byte[] expected = (byte[])before.Clone();
            Array.Clear(before, 0, before.Length);
            Assert.That(AnatomySnapshotCodec.Encode(copy), Is.EqualTo(expected));
        }

        [Test]
        public void Utf8IdentifiersArePreservedWithoutGuidParsingOrNormalization()
        {
            AnatomySnapshot source = AnatomySnapshot.Create("é-transfert", "会-session", "визуализация", "colonne-e\u0301", 9, Coordinates(), AnatomyWinding.Clockwise, true, Color(), Positions(), Normals(), new uint[] { 0, 1, 2 }, Array.Empty<float>());
            AnatomySnapshot copy = AnatomySnapshotCodec.Decode(AnatomySnapshotCodec.Encode(source));
            Assert.That(copy.VisualizationId, Is.EqualTo(source.VisualizationId));
            Assert.That(copy.ColumnId, Is.EqualTo(source.ColumnId));
            Assert.That(copy.SessionId, Is.EqualTo(source.SessionId));
        }

        [TestCase("magic")]
        [TestCase("version")]
        [TestCase("totalLength")]
        [TestCase("revision")]
        [TestCase("handedness")]
        [TestCase("unit")]
        [TestCase("mappingVersion")]
        [TestCase("affine")]
        [TestCase("singular")]
        [TestCase("matrixNan")]
        [TestCase("winding")]
        [TestCase("visible")]
        [TestCase("color")]
        [TestCase("colorNan")]
        [TestCase("verticesNegative")]
        [TestCase("verticesMaximum")]
        [TestCase("indicesMaximum")]
        [TestCase("indicesIncomplete")]
        [TestCase("uvCount")]
        [TestCase("surfaceLength")]
        [TestCase("indexOutOfRange")]
        [TestCase("positionNan")]
        [TestCase("normalInfinity")]
        [TestCase("uvNan")]
        [TestCase("textLength")]
        [TestCase("textBlank")]
        [TestCase("textUtf8")]
        public void MalformedContentIsRejectedEvenWithRecomputedHashes(string corruption)
        {
            byte[] bytes = AnatomySnapshotCodec.Encode(Example());
            Dictionary<string, int> offsets = Offsets(bytes);
            switch (corruption)
            {
                case "magic": bytes[0] ^= 1; break;
                case "version": bytes[4] = 99; break;
                case "totalLength": Put(bytes, 6, bytes.Length + 1L); break;
                case "revision": Put(bytes, offsets["revision"], 0L); break;
                case "handedness": bytes[offsets["handedness"]] = 255; break;
                case "unit": bytes[offsets["unit"]] = 255; break;
                case "mappingVersion": Put(bytes, offsets["mappingVersion"], 0); break;
                case "affine": Put(bytes, offsets["matrix"] + 60, 2f); break;
                case "singular": Put(bytes, offsets["matrix"], 0f); break;
                case "matrixNan": Put(bytes, offsets["matrix"], float.NaN); break;
                case "winding": bytes[offsets["winding"]] = 255; break;
                case "visible": bytes[offsets["visible"]] = 2; break;
                case "color": Put(bytes, offsets["color"], 2f); break;
                case "colorNan": Put(bytes, offsets["color"], float.NaN); break;
                case "verticesNegative": Put(bytes, offsets["counts"], -1); break;
                case "verticesMaximum": Put(bytes, offsets["counts"], int.MaxValue); break;
                case "indicesMaximum": Put(bytes, offsets["counts"] + 4, int.MaxValue); break;
                case "indicesIncomplete": Put(bytes, offsets["counts"] + 4, 4); break;
                case "uvCount": Put(bytes, offsets["counts"] + 8, 1); break;
                case "surfaceLength": Put(bytes, offsets["surfaceLength"], long.MaxValue); break;
                case "indexOutOfRange": Put(bytes, offsets["surface"] + 72, -1); break;
                case "positionNan": Put(bytes, offsets["surface"], float.NaN); break;
                case "normalInfinity": Put(bytes, offsets["surface"] + 36, float.PositiveInfinity); break;
                case "uvNan": Put(bytes, offsets["surface"] + 84, float.NaN); break;
                case "textLength": Put(bytes, 14, int.MaxValue); break;
                case "textBlank": Array.Fill(bytes, (byte)' ', 18, BitConverter.ToInt32(bytes, 14)); break;
                case "textUtf8": bytes[18] = 0xff; break;
            }

            Rehash(bytes, offsets["surface"]);
            Assert.Throws<InvalidDataException>(() => AnatomySnapshotCodec.Decode(bytes), corruption);
        }

        [Test]
        public void CorruptHashesTruncationAndTrailingBytesAreRejected()
        {
            byte[] encoded = AnatomySnapshotCodec.Encode(Example());
            int surfaceOffset = Offsets(encoded)["surface"];
            byte[] badContentHash = (byte[])encoded.Clone();
            badContentHash[badContentHash.Length - 1] ^= 1;
            Assert.Throws<InvalidDataException>(() => AnatomySnapshotCodec.Decode(badContentHash));
            byte[] badSurfaceHash = (byte[])encoded.Clone();
            badSurfaceHash[surfaceOffset - 1] ^= 1;
            Rehash(badSurfaceHash);
            Assert.Throws<InvalidDataException>(() => AnatomySnapshotCodec.Decode(badSurfaceHash));
            for (int length = 0; length < encoded.Length; length++)
                Assert.Throws<InvalidDataException>(() => AnatomySnapshotCodec.Decode(encoded.Take(length).ToArray()));
            byte[] trailing = new byte[encoded.Length + 1];
            encoded.CopyTo(trailing, 0);
            Put(trailing, 6, (long)trailing.Length);
            Rehash(trailing);
            Assert.Throws<InvalidDataException>(() => AnatomySnapshotCodec.Decode(trailing));
        }

        [Test]
        public void PublicFactoryRejectsInvalidDimensionsAndValues()
        {
            foreach (float bad in new[] { float.NaN, float.NegativeInfinity, float.PositiveInfinity })
            {
                float[] positions = Positions();
                positions[0] = bad;
                Assert.Throws<ArgumentException>(() => AnatomySnapshot.Create("t", "s", "v", "c", 1, Coordinates(), AnatomyWinding.Clockwise, true, Color(), positions, Normals(), new uint[] { 0, 1, 2 }, Array.Empty<float>()));
            }

            Assert.Throws<ArgumentException>(() => AnatomySnapshot.Create("t", "s", "v", "c", 1, Coordinates(), AnatomyWinding.Clockwise, true, Color(), Positions(), new float[6], new uint[] { 0, 1, 2 }, Array.Empty<float>()));
            Assert.Throws<ArgumentException>(() => AnatomySnapshot.Create("t", "s", "v", "c", 1, Coordinates(), AnatomyWinding.Clockwise, true, Color(), Positions(), Normals(), new uint[] { 0, 1 }, Array.Empty<float>()));
            Assert.Throws<ArgumentException>(() => new AnatomyCoordinateSpace(new string('é', 513), AnatomyHandedness.Left, AnatomyLengthUnit.Millimeter, 1, Matrix()));
        }

        [Test]
        public void MaximumVertexAndIndexCountsRoundTripWithoutTruncation()
        {
            // Full supported payload, also proves uint32 indices beyond Unity's 16-bit mesh range.
            float[] positions = new float[AnatomySnapshotCodec.MaximumVertexCount * 3];
            uint[] indices = new uint[AnatomySnapshotCodec.MaximumIndexCount];
            indices[indices.Length - 1] = AnatomySnapshotCodec.MaximumVertexCount - 1;
            AnatomySnapshot snapshot = AnatomySnapshot.Create("t", "s", "v", "c", 1, Coordinates(), AnatomyWinding.Clockwise, true, Color(), positions, positions, indices, new float[AnatomySnapshotCodec.MaximumVertexCount * 2]);
            byte[] bytes = AnatomySnapshotCodec.Encode(snapshot);
            AnatomySnapshot decoded = AnatomySnapshotCodec.Decode(bytes);
            Assert.That(decoded.VertexCount, Is.EqualTo(AnatomySnapshotCodec.MaximumVertexCount));
            Assert.That(decoded.Indices.Count, Is.EqualTo(AnatomySnapshotCodec.MaximumIndexCount));
            Assert.That(decoded.Indices[decoded.Indices.Count - 1], Is.EqualTo(1_999_999u));
            Assert.That(decoded.SurfaceByteLength, Is.EqualTo(112_000_000));
        }

        [Test]
        public void ContractAssemblyHasOnlySystemDependencies()
        {
            foreach (var reference in typeof(AnatomySnapshot).Assembly.GetReferencedAssemblies())
                Assert.That(reference.Name == "mscorlib" || reference.Name == "netstandard" || reference.Name == "System" || reference.Name.StartsWith("System."), Is.True, reference.FullName);
        }

        [Test]
        public void WriteReviewExample()
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), ".test-results", "quest-005");
            Directory.CreateDirectory(directory);
            byte[] encoded = AnatomySnapshotCodec.Encode(Example());
            File.WriteAllBytes(Path.Combine(directory, "example.hbna"), encoded);
            Assert.That(encoded.Length, Is.EqualTo(417));
        }

        private static void AssertBits(AnatomyBuffer<float> actual, AnatomyBuffer<float> expected) => Assert.That(actual.ToArray().Select(BitConverter.SingleToInt32Bits), Is.EqualTo(expected.ToArray().Select(BitConverter.SingleToInt32Bits)));

        private static Dictionary<string, int> Offsets(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            stream.Position = 14;
            for (int i = 0; i < 5; i++)
            {
                int length = reader.ReadInt32();
                stream.Position += length;
            }

            int start = (int)stream.Position;
            return new Dictionary<string, int>
            {
                { "revision", start }, { "handedness", start + 8 }, { "unit", start + 9 },
                { "mappingVersion", start + 10 }, { "matrix", start + 14 }, { "winding", start + 78 }, { "visible", start + 79 },
                { "color", start + 80 }, { "counts", start + 96 }, { "surfaceLength", start + 108 }, { "surface", start + 148 }
            };
        }

        private static void Put(byte[] bytes, int offset, int value) => BitConverter.GetBytes(value).CopyTo(bytes, offset);
        private static void Put(byte[] bytes, int offset, long value) => BitConverter.GetBytes(value).CopyTo(bytes, offset);
        private static void Put(byte[] bytes, int offset, float value) => BitConverter.GetBytes(value).CopyTo(bytes, offset);

        private static void Rehash(byte[] bytes, int surfaceOffset = -1)
        {
            using SHA256 sha = SHA256.Create();
            if (surfaceOffset >= 0) sha.ComputeHash(bytes, surfaceOffset, bytes.Length - 32 - surfaceOffset).CopyTo(bytes, surfaceOffset - 32);
            sha.ComputeHash(bytes, 0, bytes.Length - 32).CopyTo(bytes, bytes.Length - 32);
        }
    }
}
