using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using HBP.Transfer.Anatomy;
using NUnit.Framework;

namespace HBP.Tests.Transfer.Anatomy
{
    public class AnatomyProjectionTests
    {
        internal static byte[] Volume()
        {
            byte[] bytes = new byte[352 + 2 * 3 * 4];
            void Put(int offset, byte[] value) => Buffer.BlockCopy(value, 0, bytes, offset, value.Length);
            Put(0, BitConverter.GetBytes(348));
            Put(40, BitConverter.GetBytes((short)3));
            for (int i = 0; i < 3; i++)
            {
                Put(42 + 2 * i, BitConverter.GetBytes((short)(i + 2)));
                Put(80 + 4 * i, BitConverter.GetBytes((float)(i + 1)));
            }

            Put(70, BitConverter.GetBytes((short)2));
            Put(72, BitConverter.GetBytes((short)8));
            Put(108, BitConverter.GetBytes(352f));
            bytes[344] = (byte)'n';
            bytes[345] = (byte)'+';
            bytes[346] = (byte)'1';
            for (int i = 352; i < bytes.Length; i++) bytes[i] = (byte)(i - 352);
            return bytes;
        }

        internal static AnatomySnapshot Snapshot(AnatomyProjection projection, AnatomyContacts contacts = null) => AnatomySnapshot.Create("transfer", "../../session", "v", "c", 1, new AnatomyCoordinateSpace(AnatomyContacts.FrameId, AnatomyHandedness.Left, AnatomyLengthUnit.Millimeter, 1, new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }), AnatomyWinding.Clockwise, true, new float[] { 1, 1, 1, 1 }, new float[] { -2, 1, 4, 5, 2, 1, 1, 6, 9 }, new float[] { 0, 0, 1, 0, 0, 1, 0, 0, 1 }, new uint[] { 0, 1, 2 }, Array.Empty<float>(), contacts, projection);

        [Test]
        public void ProjectionRoundTripOwnsVolumeAndPreservesSettings()
        {
            byte[] volume = Volume();
            var input = new AnatomyProjection(volume, 97, 0, 23.5f, 2, .75f);
            byte[] bytes = AnatomySnapshotCodec.Encode(Snapshot(input));
            Array.Clear(volume, 0, volume.Length);
            var decoded = AnatomySnapshotCodec.Decode(bytes);
            Assert.That(decoded.SchemaVersion, Is.EqualTo(3));
            Assert.That(decoded.Projection.VolumeBytes.ToArray(), Is.EqualTo(Volume()));
            Assert.That(decoded.Projection.Dimensions, Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(decoded.Projection.GridDimension, Is.EqualTo(97));
            Assert.That(decoded.Projection.Interpolation, Is.Zero);
            Assert.That(decoded.Projection.InfluenceDistance, Is.EqualTo(23.5f));
            Assert.That(decoded.Projection.InfluenceByDistance, Is.EqualTo(2));
            Assert.That(decoded.Projection.ActivityAlpha, Is.EqualTo(.75f));
            Assert.That(AnatomySnapshotCodec.Encode(decoded), Is.EqualTo(bytes));
        }

        [TestCase("hash")]
        [TestCase("length")]
        [TestCase("missing")]
        [TestCase("grid")]
        [TestCase("interpolation")]
        [TestCase("influence")]
        public void CorruptProjectionRejectedEvenWithValidEnvelopeHash(string fault)
        {
            byte[] bytes = AnatomySnapshotCodec.Encode(Snapshot(new AnatomyProjection(Volume(), 80, 1, 15, 0, .8f)));
            int section = bytes.Length - 32 - Volume().Length - 56;
            int offset = fault switch { "hash" => section + 24, "length" => section + 20, "missing" => section - 4, "grid" => section, "interpolation" => section + 4, _ => section + 8 };
            Buffer.BlockCopy(BitConverter.GetBytes(fault == "grid" || fault == "missing" ? 0 : -1), 0, bytes, offset, 4);
            using var sha = SHA256.Create();
            Buffer.BlockCopy(sha.ComputeHash(bytes, 0, bytes.Length - 32), 0, bytes, bytes.Length - 32, 32);
            Assert.Throws<InvalidDataException>(() => AnatomySnapshotCodec.Decode(bytes));
        }

        [TestCase(40)]
        [TestCase(42)]
        [TestCase(70)]
        [TestCase(72)]
        [TestCase(108)]
        [TestCase(344)]
        public void InvalidNiftiRejectedBeforeNativeLoading(int offset)
        {
            byte[] volume = Volume();
            volume[offset] = 0;
            if (offset == 108) Buffer.BlockCopy(BitConverter.GetBytes(float.NaN), 0, volume, offset, 4);
            Assert.Throws<ArgumentException>(() => new AnatomyProjection(volume, 80, 1, 15, 0, .8f));
        }

        [Test]
        public void MissingVolumeRejected() => Assert.Throws<ArgumentNullException>(() => new AnatomyProjection(null, 80, 1, 15, 0, .8f));

        [TestCase(false)]
        [TestCase(true)]
        public void AllSourceMaskCombinationsArePreservedAndChecked(bool roi)
        {
            for (int bits = 0; bits < 16; bits++)
            {
                bool masked = (bits & 3) != 0 || (roi && (bits & 4) != 0) || (bits & 8) == 0;
                AnatomyContacts Contacts(bool value) => new AnatomyContacts("MNI", roi, new[] { "patient" }, new[] { new AnatomySite("site", "A1", "A", 0, 0, 3, new float[] { 7, -8, 9 }, new float[] { 1, 1, 1, 1 }, 2, true, (AnatomySiteFlags)bits, value) });
                var projection = new AnatomyProjection(Volume(), 80, 1, 15, 0, .8f);
                var copy = AnatomySnapshotCodec.Decode(AnatomySnapshotCodec.Encode(Snapshot(projection, Contacts(masked))));
                Assert.That(copy.Contacts.Sites[0].EffectiveMasked, Is.EqualTo(masked));
                Assert.That(copy.Contacts.Sites[0].SourceIndex, Is.EqualTo(3));
                Assert.Throws<ArgumentException>(() => Snapshot(projection, Contacts(!masked)));
            }
        }
    }
}
