using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using HBP.Transfer.Anatomy;
using NUnit.Framework;

namespace HBP.Tests.Transfer.Anatomy
{
    public class AnatomyContactsTests
    {
        private static AnatomySite Site(int order = 0, int patient = 0, string id = "stable-site", float x = -31.25f) => new AnatomySite(id, "A" + order, "A", order, patient, order, new[] { x, 18.5f, -26.75f }, new[] { .25f, .5f, .75f, 1f }, 2, true, AnatomySiteFlags.Filtered, false);
        private static AnatomySnapshot Snapshot(AnatomyContacts contacts, string frame = AnatomyContacts.FrameId) => AnatomySnapshot.Create("t", "s", "v", "c", 1, new AnatomyCoordinateSpace(frame, AnatomyHandedness.Left, AnatomyLengthUnit.Millimeter, 1, new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }), AnatomyWinding.Clockwise, true, new float[] { 1, 1, 1, 1 }, new float[] { 0, 0, 0, 100, 0, 0, 0, 100, 0 }, new float[] { 0, 0, 1, 0, 0, 1, 0, 0, 1 }, new uint[] { 0, 1, 2 }, Array.Empty<float>(), contacts);

        [TestCase(0)]
        [TestCase(8)]
        [TestCase(257)]
        public void RoundTrip_PreservesAllRecordsAndAssociationsWithoutFixtureLimit(int count)
        {
            var sites = Enumerable.Range(0, count).Select(i => Site(i, i % 2, "id-" + i)).ToArray();
            string[] patients = { "p1", "p2" };
            var contacts = new AnatomyContacts("MNI", true, patients, sites);
            byte[] encoded = AnatomySnapshotCodec.Encode(Snapshot(contacts));
            patients[0] = "changed";
            if (count > 0) sites[0] = Site();
            AnatomySnapshot copy = AnatomySnapshotCodec.Decode(encoded);
            Assert.That(copy.SchemaVersion, Is.EqualTo(2));
            Assert.That(copy.Contacts.PatientIds, Is.EqualTo(new[] { "p1", "p2" }));
            Assert.That(copy.Contacts.Sites.Count, Is.EqualTo(count));
            Assert.That(copy.Contacts.RoiActive, Is.True);
            for (int i = 0; i < count; i++)
            {
                var site = copy.Contacts.Sites[i];
                Assert.That(site.Id, Is.EqualTo("id-" + i));
                Assert.That(site.Order, Is.EqualTo(i));
                Assert.That(site.SourceIndex, Is.EqualTo(i));
                Assert.That(site.PatientIndex, Is.EqualTo(i % 2));
                Assert.That(site.Position.ToArray(), Is.EqualTo(new[] { -31.25f, 18.5f, -26.75f }));
            }

            Assert.That(AnatomySnapshotCodec.Encode(copy), Is.EqualTo(encoded));
        }

        [Test]
        public void AnatomyOnly_RetainsV1WireBytesAndDecodesWithNoSites()
        {
            byte[] encoded = AnatomySnapshotCodec.Encode(Snapshot(null));
            Assert.That(BitConverter.ToUInt16(encoded, 4), Is.EqualTo(1));
            var copy = AnatomySnapshotCodec.Decode(encoded);
            Assert.That(copy.Contacts.Sites, Is.Empty);
            Assert.That(AnatomySnapshotCodec.Encode(copy), Is.EqualTo(encoded));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SurfaceHashOffset_IsIndependentOfContactSuffix(bool contacts)
        {
            var source = Snapshot(contacts ? new AnatomyContacts("MNI", false, new[] { "patient" }, new[] { Site() }) : null);
            var bytes = AnatomySnapshotCodec.Encode(source);
            var decoded = AnatomySnapshotCodec.Decode(bytes);
            int offset = AnatomySnapshotCodec.GetSurfaceHashOffset(decoded);
            using var sha = SHA256.Create();
            Assert.That(bytes.Skip(offset).Take(32), Is.EqualTo(sha.ComputeHash(bytes, offset + 32, (int)decoded.SurfaceByteLength)));
        }

        [Test]
        public void ZeroSites_PreservesNonDefaultImplantationMetadata()
        {
            var snapshot = Snapshot(new AnatomyContacts("Patient", false, Array.Empty<string>(), Array.Empty<AnatomySite>()));
            byte[] bytes = AnatomySnapshotCodec.Encode(snapshot);
            Assert.That(AnatomySnapshotCodec.Decode(bytes).Contacts.Implantation, Is.EqualTo("Patient"));
            Assert.That(AnatomySnapshotCodec.Encode(AnatomySnapshotCodec.Decode(bytes)), Is.EqualTo(bytes));
        }

        [Test]
        public void ContactArraysAreCopiedAndCoveredByTheContentHash()
        {
            float[] position = { -31.25f, 18.5f, -26.75f }, color = { .25f, .5f, .75f, 1f };
            var site = new AnatomySite("id", "A1", "A", 0, 0, 0, position, color, 2, false, AnatomySiteFlags.Blacklisted, true);
            var snapshot = Snapshot(new AnatomyContacts("MNI", false, new[] { "patient" }, new[] { site }));
            byte[] bytes = AnatomySnapshotCodec.Encode(snapshot);
            position[0] = color[0] = 99;
            site.Position.ToArray()[0] = 55;
            Assert.That(AnatomySnapshotCodec.Encode(snapshot), Is.EqualTo(bytes));
            var copy = AnatomySnapshotCodec.Decode(bytes).Contacts.Sites[0];
            Assert.That(copy.Color.ToArray(), Is.EqualTo(new[] { .25f, .5f, .75f, 1f }));
            Assert.That(copy.Visible, Is.False);
            Assert.That(copy.EffectiveMasked, Is.True);
            bytes[bytes.Length - 33] ^= 1;
            Assert.Throws<InvalidDataException>(() => AnatomySnapshotCodec.Decode(bytes));
        }

        [TestCase("id")]
        [TestCase("patient")]
        [TestCase("order")]
        [TestCase("native-patient")]
        [TestCase("world")]
        [TestCase("nan")]
        public void InvalidContacts_AreRejected(string scenario)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var contacts = new AnatomyContacts("MNI", false, new[] { scenario == "native-patient" ? "a?b" : "patient" }, scenario == "id" ? new[] { Site(), Site(1) } : new[] { Site(scenario == "order" ? 1 : 0, scenario == "patient" ? 1 : 0, x: scenario == "nan" ? float.NaN : 1) });
                Snapshot(contacts, scenario == "world" ? "quest-world" : AnatomyContacts.FrameId);
            });
        }

        [TestCase("count")]
        [TestCase("section")]
        [TestCase("boolean")]
        [TestCase("position")]
        public void MalformedContactPayload_RehashedStillRejected(string scenario)
        {
            byte[] bytes = AnatomySnapshotCodec.Encode(Snapshot(new AnatomyContacts("MNI", false, new[] { "patient" }, new[] { Site() })));
            // Four 1-byte identifiers + frame ID, fixed header and triangle buffers.
            int section = 214 + 4 + AnatomyContacts.FrameId.Length + 84 - 32;
            using var stream = new MemoryStream(bytes, true);
            using var writer = new BinaryWriter(stream);
            if (scenario == "section")
            {
                stream.Position = section;
                writer.Write(int.MaxValue);
            }

            if (scenario == "boolean") bytes[section + 4 + 4 + 3] = 2;
            if (scenario == "count")
            {
                stream.Position = section + 4 + 4 + 3 + 1;
                writer.Write(int.MaxValue);
            }

            if (scenario == "position")
            {
                stream.Position = bytes.Length - 32 - 47 + 12;
                writer.Write(float.NaN);
            }

            using var sha = SHA256.Create();
            Buffer.BlockCopy(sha.ComputeHash(bytes, 0, bytes.Length - 32), 0, bytes, bytes.Length - 32, 32);
            Assert.Throws<InvalidDataException>(() => AnatomySnapshotCodec.Decode(bytes));
        }
    }
}
