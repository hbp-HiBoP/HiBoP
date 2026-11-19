using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using HBP.Transfer.Anatomy;
using NUnit.Framework;

namespace HBP.Tests.Transfer.Anatomy
{
    public class IEEGInstantTests
    {
        private static IEEGInstant Instant(float alpha = 0, float[] values = null, byte[] available = null, string[] ids = null) => new("dataset", "synthetic uV", "bloc", "subbloc", new string('a', 64), 3, 7, 200, 15, 1, 4, 100, 2, alpha, -10, 0, 10, ids ?? new[] { "patient_A", "patient_B", "patient_C", "patient_D", "patient_E" }, new[] { "uV", "uV", "uV", "", "uV" }, available ?? new byte[] { 2, 2, 2, 0, 1 }, values ?? new float[] { -1, 0, 1, 0, 0 }, alpha == 0 ? values ?? new float[] { -1, 0, 1, 0, 0 } : new float[] { -2, .5f, 2, 0, 0 });

        private static AnatomySnapshot Snapshot(IEEGInstant instant)
        {
            var original = AnatomyProjectionTests.Snapshot(new AnatomyProjection(AnatomyProjectionTests.Volume(), 80, 1, 15, 2, .8f));
            var sites = Enumerable.Range(0, 5).Select(i => new AnatomySite("site" + i, ((char)('A' + i)).ToString(), "E", i, 0, i, new float[3], new float[] { 1, 1, 1, 1 }, 2, true, AnatomySiteFlags.Filtered | (i >= 3 ? AnatomySiteFlags.Masked : i == 2 ? AnatomySiteFlags.Blacklisted : 0), i >= 2)).ToArray();
            return AnatomySnapshot.Create("t", "s", "v", "c", 1, original.Coordinates, original.Winding, true, original.Color.ToArray(), original.Positions.ToArray(), original.Normals.ToArray(), original.Indices.ToArray(), original.Uvs.ToArray(), new AnatomyContacts("MNI", false, new[] { "patient" }, sites), original.Projection, instant);
        }

        [TestCase(0f)]
        [TestCase(.5f)]
        public void RoundTripPreservesValuesUnitsMasksIdentityAndPreparedSpans(float alpha)
        {
            var original = Instant(alpha);
            var bytes = AnatomySnapshotCodec.Encode(Snapshot(original));
            var decoded = AnatomySnapshotCodec.Decode(bytes);
            Assert.That(decoded.SchemaVersion, Is.EqualTo(4));
            Assert.That(AnatomySnapshotCodec.Encode(decoded), Is.EqualTo(bytes));
            Assert.That(decoded.IEEG.SurfaceValues.ToArray(), Is.EqualTo(new float[] { -1, 0, 1, 0, 0 }));
            Assert.That(decoded.IEEG.SiteValues.ToArray(), Is.EqualTo(original.SiteValues.ToArray()));
            Assert.That(decoded.IEEG.SpanMin, Is.EqualTo(-10));
            Assert.That(decoded.IEEG.Middle, Is.Zero);
            Assert.That(decoded.IEEG.SpanMax, Is.EqualTo(10));
            Assert.That(decoded.IEEG.Availability.ToArray(), Is.EqualTo(new byte[] { 2, 2, 2, 0, 1 }));
            Assert.That(decoded.IEEG.NavigationIndex, Is.EqualTo(3));
            Assert.That(decoded.IEEG.ProjectionIndex, Is.EqualTo(1));
            Assert.That(decoded.Contacts.Sites[2].EffectiveMasked, Is.True);
            Assert.That(decoded.IEEG.Summary, Does.Contain("15 ms").And.Contain("uV"));
        }

        [Test]
        public void SourceArraysAreCopied()
        {
            var values = new float[] { -1, 0, 1, 0, 0 };
            var instant = Instant(values: values);
            values[0] = 999;
            Assert.That(instant.SurfaceValues[0], Is.EqualTo(-1));
            Assert.That(instant.SiteValues[0], Is.EqualTo(-1));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void NonFiniteRejected(float value) => Assert.Throws<ArgumentException>(() => Instant(values: new[] { value, 0, 1, 0, 0 }));

        [Test]
        public void MissingWithValueRejected() => Assert.Throws<ArgumentException>(() => Instant(available: new byte[] { 0, 2, 2, 0, 1 }));

        [Test]
        public void ReorderedChannelsRejected() => Assert.Throws<ArgumentException>(() => Snapshot(Instant(ids: new[] { "patient_B", "patient_A", "patient_C", "patient_D", "patient_E" })));

        [TestCase(-.1f)]
        [TestCase(1f)]
        public void InvalidAlphaRejected(float alpha) => Assert.Throws<ArgumentException>(() => Instant(alpha));

        [TestCase(false)]
        [TestCase(true)]
        public void CorruptChannelValueRejected(bool repairHash)
        {
            byte[] bytes = AnatomySnapshotCodec.Encode(Snapshot(Instant()));
            Buffer.BlockCopy(BitConverter.GetBytes(float.NaN), 0, bytes, bytes.Length - 36, 4);
            if (repairHash)
            {
                using var sha = SHA256.Create();
                Buffer.BlockCopy(sha.ComputeHash(bytes, 0, bytes.Length - 32), 0, bytes, bytes.Length - 32, 32);
            }

            Assert.Throws<InvalidDataException>(() => AnatomySnapshotCodec.Decode(bytes));
        }
    }
}
