using System;
using System.IO;
using NUnit.Framework;

namespace HBP.Sync.Tests
{
    [TestFixture, Category("Sync.Fast")]
    public sealed class SignalPresentationTests
    {
        private const string Key = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        private static readonly Guid Generation = Guid.Parse("7e6c6826-b92f-4bf1-bac5-39e832e40001");

        private static SignalSnapshot Snapshot(long revision = 1, string key = Key, int rows = 1, int columns = 2) =>
            new()
            {
                Revision = revision, Title = "Patient A / A1", Format = 2, SmoothingFactor = 1,
                Colormap = new byte[] { 0, 0, 0, 255, 255, 255, 255, 255 },
                Matrices = new[] { new SignalMatrixRow { Title = "Recording / Bloc / A1", Group = "data", Min = -1, Max = 1, Selected = new bool[rows], Cells = new[] { new SignalMatrix { Resource = key, Rows = rows, Columns = columns, Start = -100, End = 100, WindowStart = -100, WindowEnd = 100 } } } }
            };

        private static SignalMessage Message(SignalMessageKind kind, long revision = 1, byte[] body = null, string key = "") => new() { Kind = kind, Generation = Generation, Revision = revision, Body = body ?? Array.Empty<byte>(), Key = key };

        private static void Begin(SignalPresentationBuffer buffer, SignalSnapshot snapshot, string kind = "content")
        {
            buffer.Receive(Message(SignalMessageKind.Begin, snapshot.Revision, key: kind));
            buffer.Receive(Message(SignalMessageKind.Manifest, snapshot.Revision, SignalSnapshotCodec.Encode(snapshot)));
        }

        private static void Resource(SignalPresentationBuffer buffer, string key = Key, long revision = 1)
        {
            var body = new byte[8];
            Buffer.BlockCopy(new[] { 10f, 20f }, 0, body, 0, body.Length);
            var message = Message(SignalMessageKind.Resource, revision, body, key);
            message.Total = body.Length;
            buffer.Receive(message);
        }

        [Test]
        public void Metadata_RoundTripsLongSampleDimensionsWithoutAllocatingSamples()
        {
            var expected = Snapshot(rows: 1, columns: 1000000);
            expected.Matrices[0].GroupTitle = "Recording";
            expected.Matrices[0].BlocId = "block-id";
            expected.Matrices[0].BlocTitle = "Repeated name";
            expected.Matrices[0].ChannelId = "patient|A1";
            expected.Matrices[0].ChannelTitle = "A1 (Patient A)";
            var encoded = SignalSnapshotCodec.Encode(expected);
            Assert.That(encoded.Length, Is.LessThan(1000));
            var actual = SignalSnapshotCodec.Decode(encoded);
            Assert.That(actual.Matrices[0].Cells[0].Columns, Is.EqualTo(1000000));
            Assert.That(actual.Matrices[0].Group, Is.EqualTo("data"));
            Assert.That(actual.Matrices[0].GroupTitle, Is.EqualTo("Recording"));
            Assert.That(actual.Matrices[0].BlocId, Is.EqualTo("block-id"));
            Assert.That(actual.Matrices[0].BlocTitle, Is.EqualTo("Repeated name"));
            Assert.That(actual.Matrices[0].ChannelId, Is.EqualTo("patient|A1"));
            Assert.That(actual.Matrices[0].ChannelTitle, Is.EqualTo("A1 (Patient A)"));
        }

        [Test]
        public void Publication_IsAtomicAndSettingsReuseExactSamples()
        {
            var buffer = new SignalPresentationBuffer(Generation);
            Begin(buffer, Snapshot());
            Resource(buffer);
            Assert.That(buffer.Current, Is.Null);
            buffer.Receive(Message(SignalMessageKind.Commit));
            var samples = buffer.Resources[Key];
            CollectionAssert.AreEqual(new[] { 10f, 20f }, samples);
            var settings = Snapshot(2);
            settings.Matrices[0].Min = -10;
            Begin(buffer, settings, "settings");
            Assert.That(buffer.Current.Revision, Is.EqualTo(1));
            buffer.Receive(Message(SignalMessageKind.Commit, 2));
            Assert.That(buffer.Current.Revision, Is.EqualTo(2));
            Assert.That(buffer.Resources[Key], Is.SameAs(samples));
        }

        [Test]
        public void SupersededAndWrongGenerationMessagesCannotReplaceCurrentContent()
        {
            var buffer = new SignalPresentationBuffer(Generation);
            Begin(buffer, Snapshot(2));
            Resource(buffer, revision: 2);
            buffer.Receive(Message(SignalMessageKind.Commit, 2));
            buffer.Receive(Message(SignalMessageKind.Begin, 1, key: "content"));
            var alien = Message(SignalMessageKind.Begin, 3, key: "content");
            alien.Generation = Guid.NewGuid();
            buffer.Receive(alien);
            Assert.That(buffer.Current.Revision, Is.EqualTo(2));
        }

        [Test]
        public void ReturningToAnEvictedPresentationRequiresItsSamplesAgain()
        {
            var buffer = new SignalPresentationBuffer(Generation);
            Begin(buffer, Snapshot());
            Resource(buffer);
            buffer.Receive(Message(SignalMessageKind.Commit));
            Begin(buffer, Snapshot(2, new string('B', 64)));
            Begin(buffer, Snapshot(3));
            Assert.Throws<InvalidDataException>(() => buffer.Receive(Message(SignalMessageKind.Commit, 3)));
            Resource(buffer, revision: 3);
            buffer.Receive(Message(SignalMessageKind.Commit, 3));
            Assert.That(buffer.Current.Revision, Is.EqualTo(3));
        }

        [Test]
        public void TextureBudgetIsCheckedBeforeSampleAllocation()
        {
            var buffer = new SignalPresentationBuffer(Generation, 32 * 1024 * 1024);
            var s = Snapshot(rows: 1000, columns: 2000);
            s.Smooth = s.Smooth2D = true;
            s.SmoothingFactor = 4;
            Assert.Throws<InvalidDataException>(() => Begin(buffer, s));
            Assert.That(buffer.ResourceBytes, Is.Zero);
        }

        [Test]
        public void ResourceBudgetIncludesDecodedAndWorkingCopiesBeforeAllocation()
        {
            var buffer = new SignalPresentationBuffer(Generation, 32 * 1024 * 1024);
            var snapshot = Snapshot();
            snapshot.Matrices = Array.Empty<SignalMatrixRow>();
            snapshot.Graphs = new[] { new SignalGraph { Curves = new[] { new SignalCurve { Resource = Key } } } };
            Begin(buffer, snapshot);
            var message = Message(SignalMessageKind.Resource, body: new byte[4], key: Key);
            message.Total = 9 * 1024 * 1024;
            Assert.Throws<InvalidDataException>(() => buffer.Receive(message));
            Assert.That(buffer.ResourceBytes, Is.Zero);
        }

        [Test]
        public void ResourcePartsMustBeAlignedAndOrdered()
        {
            var buffer = new SignalPresentationBuffer(Generation);
            Begin(buffer, Snapshot());
            var message = Message(SignalMessageKind.Resource, body: new byte[4], key: Key);
            message.Offset = 4;
            message.Total = 8;
            Assert.Throws<InvalidDataException>(() => buffer.Receive(message));
            message.Offset = 0;
            message.Total = 7;
            Assert.Throws<InvalidDataException>(() => buffer.Receive(message));
        }

        [Test]
        public void ClosingReleasesSamplesAndPublishedSnapshot()
        {
            var buffer = new SignalPresentationBuffer(Generation);
            Begin(buffer, Snapshot());
            Resource(buffer);
            buffer.Receive(Message(SignalMessageKind.Commit));
            buffer.ClearData();
            Assert.That(buffer.Current, Is.Null);
            Assert.That(buffer.Resources, Is.Empty);
            Assert.That(buffer.ResourceBytes, Is.Zero);
        }

        [Test]
        public void InvalidDimensionsAndTruncatedMetadataAreRejected()
        {
            var invalid = Snapshot(rows: 1, columns: int.MaxValue);
            Assert.Throws<InvalidDataException>(() => SignalSnapshotCodec.Decode(SignalSnapshotCodec.Encode(invalid)));
            byte[] bytes = SignalSnapshotCodec.Encode(Snapshot());
            Array.Resize(ref bytes, bytes.Length - 2);
            Assert.Throws<EndOfStreamException>(() => SignalSnapshotCodec.Decode(bytes));
        }

        [Test]
        public void WirePreservesGenerationRevisionAndApplicationFailureAcknowledgement()
        {
            var message = Message(SignalMessageKind.Acknowledge, 17, key: Guid.NewGuid().ToString("N"));
            message.Total = 1;
            var copy = SignalWire.Decode(SignalWire.Encode(message));
            Assert.That(copy.Generation, Is.EqualTo(Generation));
            Assert.That(copy.Revision, Is.EqualTo(17));
            Assert.That(copy.Total, Is.EqualTo(1));
            var bytes = SignalWire.Encode(message);
            bytes[4] = 99;
            Assert.Throws<InvalidDataException>(() => SignalWire.Decode(bytes));
        }
    }
}
