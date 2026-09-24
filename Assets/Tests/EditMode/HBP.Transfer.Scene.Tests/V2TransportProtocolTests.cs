using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HBP.Sync;
using HBP.Transfer.Transport;
using NUnit.Framework;

namespace HBP.Sync.Tests
{
    [TestFixture]
    public sealed class V2TransportProtocolTests
    {
        [Test]
        [Category("Sync.Fast")]
        public void Frame_RoundTripsApplicationAndBulkMetadata()
        {
            var record = new V2TransportRecord(V2TransportMessageKind.Application, new SessionId(Guid.Parse("10000000-0000-0000-0000-000000000001")), new SceneId(Guid.Parse("20000000-0000-0000-0000-000000000002")), new IncarnationId(Guid.Parse("30000000-0000-0000-0000-000000000003")), new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000004")), new ReliableStreamId(Guid.Parse("50000000-0000-0000-0000-000000000005")), 9, 0, V2OriginDevice.Quest, V2ScheduleLane.Bulk, 17, 3, new byte[] { 4, 5, 6 });

            V2TransportRecord decoded = V2TransportFrameCodec.Decode(V2TransportFrameCodec.Encode(record));

            Assert.That(decoded.SessionId, Is.EqualTo(record.SessionId));
            Assert.That(decoded.SceneId, Is.EqualTo(record.SceneId));
            Assert.That(decoded.IncarnationId, Is.EqualTo(record.IncarnationId));
            Assert.That(decoded.MessageId, Is.EqualTo(record.MessageId));
            Assert.That(decoded.StreamId, Is.EqualTo(record.StreamId));
            Assert.That(decoded.ReliableFrameSequence, Is.EqualTo(9UL));
            Assert.That(decoded.OriginSequence, Is.Zero);
            Assert.That(decoded.OriginDevice, Is.EqualTo(V2OriginDevice.Quest));
            Assert.That(decoded.Lane, Is.EqualTo(V2ScheduleLane.Bulk));
            Assert.That(decoded.BodySchema, Is.EqualTo(17));
            Assert.That(decoded.ChunkIndex, Is.EqualTo(3));
            CollectionAssert.AreEqual(new byte[] { 4, 5, 6 }, decoded.GetPayloadCopy());
        }

        [Test]
        [Category("Sync.Fast")]
        public void MutationFrames_CrossDecodeWithT01AndKeepReservedBytesZero()
        {
            V2Mutation mutation = new SetSiteColor(new ColumnId("column"), new SiteId("site"), 0.1f, 0.2f, 0.3f, 1f);
            var desktop = new V2MutationEnvelope(new SessionId(Guid.Parse("10000000-0000-0000-0000-000000000001")), new SceneId(Guid.Parse("20000000-0000-0000-0000-000000000002")), new IncarnationId(Guid.Parse("30000000-0000-0000-0000-000000000003")), new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000004")), new ReliableStreamId(Guid.Parse("50000000-0000-0000-0000-000000000005")), 3, 7, V2OriginDevice.Desktop, 11, null, mutation);
            var questInitialProposal = new V2MutationEnvelope(new SessionId(Guid.Parse("10000000-0000-0000-0000-000000000011")), new SceneId(Guid.Parse("20000000-0000-0000-0000-000000000012")), new IncarnationId(Guid.Parse("30000000-0000-0000-0000-000000000013")), new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000014")), new ReliableStreamId(Guid.Parse("50000000-0000-0000-0000-000000000015")), 1, 1, V2OriginDevice.Quest, null, 0, mutation);

            AssertMutationCrossCodec(desktop);
            AssertMutationCrossCodec(questInitialProposal);
        }

        [Test]
        [Category("Sync.Loopback")]
        public async Task FrameReader_ReassemblesFragmentedHeaderAndPayloadAndReadsCombinedFrames()
        {
            V2TransportRecord first = CreateEphemeralControl(new byte[] { 1, 2, 3, 4, 5 });
            V2TransportRecord second = CreateEphemeralControl(new byte[] { 6, 7 });
            byte[] firstBytes = V2TransportFrameCodec.Encode(first);
            byte[] secondBytes = V2TransportFrameCodec.Encode(second);
            byte[] combined = new byte[firstBytes.Length + secondBytes.Length];
            Buffer.BlockCopy(firstBytes, 0, combined, 0, firstBytes.Length);
            Buffer.BlockCopy(secondBytes, 0, combined, firstBytes.Length, secondBytes.Length);

            using var fragmented = new FragmentingReadStream(combined, 3);
            V2TransportRecord decodedFirst = await V2TransportFrameCodec.ReadAsync(fragmented, CancellationToken.None);
            V2TransportRecord decodedSecond = await V2TransportFrameCodec.ReadAsync(fragmented, CancellationToken.None);
            V2TransportRecord end = await V2TransportFrameCodec.ReadAsync(fragmented, CancellationToken.None);

            CollectionAssert.AreEqual(first.GetPayloadCopy(), decodedFirst.GetPayloadCopy());
            CollectionAssert.AreEqual(second.GetPayloadCopy(), decodedSecond.GetPayloadCopy());
            Assert.That(end, Is.Null);
        }

        [Test]
        [Category("Sync.Loopback")]
        public async Task FrameReader_RejectsOversizedLengthBeforeReadingOrAllocatingTheBody()
        {
            byte[] bytes = V2TransportFrameCodec.Encode(CreateEphemeralControl(Array.Empty<byte>()));
            WriteUInt32(bytes, 12, uint.MaxValue);
            using var headerOnly = new HeaderOnlyStream(bytes);

            Exception failure = null;
            try
            {
                await V2TransportFrameCodec.ReadAsync(headerOnly, CancellationToken.None);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            Assert.That(failure, Is.TypeOf<InvalidDataException>());
            Assert.That(headerOnly.ReadCalls, Is.EqualTo(1));
        }

        [Test]
        [Category("Sync.Fast")]
        public void ResumeWatermarks_RoundTripAndRejectDuplicateStreams()
        {
            var streamId = new ReliableStreamId(Guid.Parse("50000000-0000-0000-0000-000000000005"));
            var values = new System.Collections.Generic.Dictionary<ReliableStreamId, ulong> { [streamId] = 12 };

            var decoded = V2ResumeWatermarkCodec.Decode(V2ResumeWatermarkCodec.Encode(values));

            Assert.That(decoded[streamId], Is.EqualTo(12UL));
            byte[] oneEntry = V2ResumeWatermarkCodec.Encode(values);
            byte[] malformed = new byte[52];
            malformed[0] = oneEntry[0];
            malformed[1] = oneEntry[1];
            malformed[2] = 2;
            Buffer.BlockCopy(oneEntry, 4, malformed, 4, 24);
            Buffer.BlockCopy(oneEntry, 4, malformed, 28, 24);
            Assert.Throws<InvalidDataException>(() => V2ResumeWatermarkCodec.Decode(malformed));
        }

        [Test]
        [Category("Sync.Fast")]
        public void BulkStreamRetirement_RoundTripsAndBulkIdsCarryDirectionalOrdinals()
        {
            var session = new SessionId(Guid.Parse("10000000-0000-0000-0000-000000000001"));
            ReliableStreamId desktopStream = V2BulkStreamIdentityCodec.Create(session, V2OriginDevice.Desktop, 129);
            ReliableStreamId questStream = V2BulkStreamIdentityCodec.Create(session, V2OriginDevice.Quest, 129);
            Assert.That(V2BulkStreamIdentityCodec.TryGetOrdinal(session, V2OriginDevice.Desktop, desktopStream, out ulong ordinal), Is.True);
            Assert.That(ordinal, Is.EqualTo(129UL));
            Assert.That(V2BulkStreamIdentityCodec.TryGetOrdinal(session, V2OriginDevice.Quest, desktopStream, out _), Is.False);
            Assert.That(questStream, Is.Not.EqualTo(desktopStream));

            var retirement = new V2TransportRecord(V2TransportMessageKind.BulkStreamRetirement, session, originDevice: V2OriginDevice.Desktop, bulkStreamRetiredThrough: 129);
            V2TransportRecord decoded = V2TransportFrameCodec.Decode(V2TransportFrameCodec.Encode(retirement));

            Assert.That(decoded.Kind, Is.EqualTo(V2TransportMessageKind.BulkStreamRetirement));
            Assert.That(decoded.BulkStreamRetiredThrough, Is.EqualTo(129UL));
            Assert.That(decoded.ReliableFrameSequence, Is.Zero);
        }

        [Test]
        [Category("Sync.Fast")]
        public void ScopedRejection_RoundTripsBoundedDiagnostic()
        {
            var operationId = new OperationId(Guid.Parse("40000000-0000-0000-0000-000000000004"));
            byte[] payload = V2ScopedRejectionCodec.Encode(operationId, "wrong-scene", "Scene incarnation is closed.");

            Assert.That(V2ScopedRejectionCodec.TryDecode(payload, out OperationId decodedId, out string code, out string diagnostic), Is.True);
            Assert.That(decodedId, Is.EqualTo(operationId));
            Assert.That(code, Is.EqualTo("wrong-scene"));
            Assert.That(diagnostic, Is.EqualTo("Scene incarnation is closed."));
            Assert.That(V2ScopedRejectionCodec.TryDecode(new byte[] { 1, 2, 3 }, out _, out _, out _), Is.False);
        }

        private static V2TransportRecord CreateEphemeralControl(byte[] payload)
        {
            return new V2TransportRecord(V2TransportMessageKind.Application, new SessionId(Guid.Parse("10000000-0000-0000-0000-000000000001")), streamId: new ReliableStreamId(Guid.Parse("50000000-0000-0000-0000-000000000005")), originDevice: V2OriginDevice.Desktop, payload: payload);
        }

        private static void AssertMutationCrossCodec(V2MutationEnvelope envelope)
        {
            byte[] t01Frame = V2MutationEnvelopeCodec.Encode(envelope);
            V2TransportRecord decodedByTransport = V2TransportFrameCodec.Decode(t01Frame);
            Assert.That(decodedByTransport.Kind, Is.EqualTo(V2TransportMessageKind.Application));
            Assert.That(decodedByTransport.SessionId, Is.EqualTo(envelope.SessionId));
            Assert.That(decodedByTransport.SceneId, Is.EqualTo(envelope.SceneId));
            Assert.That(decodedByTransport.IncarnationId, Is.EqualTo(envelope.IncarnationId));
            Assert.That(decodedByTransport.MessageId, Is.EqualTo(envelope.OperationId));
            Assert.That(decodedByTransport.StreamId, Is.EqualTo(envelope.ReliableStreamId));
            Assert.That(decodedByTransport.ReliableFrameSequence, Is.EqualTo(envelope.ReliableFrameSequence));
            Assert.That(decodedByTransport.OriginSequence, Is.EqualTo(envelope.OriginSequence));
            Assert.That(decodedByTransport.CanonicalSequence, Is.EqualTo(envelope.CanonicalSequence));
            Assert.That(decodedByTransport.ObservedCanonicalSequence, Is.EqualTo(envelope.ObservedCanonicalSequence));
            Assert.That(decodedByTransport.OriginDevice, Is.EqualTo(envelope.OriginDevice));

            byte[] transportFrame = V2TransportFrameCodec.Encode(decodedByTransport);
            Assert.That(transportFrame[0], Is.EqualTo((byte)'H'));
            Assert.That(transportFrame[1], Is.EqualTo((byte)'B'));
            Assert.That(transportFrame[2], Is.EqualTo((byte)'S'));
            Assert.That(transportFrame[3], Is.EqualTo((byte)'2'));
            for (int i = 129; i < V2MutationEnvelopeCodec.HeaderLength; i++)
                Assert.That(transportFrame[i], Is.Zero, $"Reserved byte {i} must remain zero.");

            V2MutationEnvelope decodedByT01 = V2MutationEnvelopeCodec.Decode(transportFrame);
            Assert.That(decodedByT01.OriginDevice, Is.EqualTo(envelope.OriginDevice));
            Assert.That(decodedByT01.CanonicalSequence, Is.EqualTo(envelope.CanonicalSequence));
            Assert.That(decodedByT01.ObservedCanonicalSequence, Is.EqualTo(envelope.ObservedCanonicalSequence));
            Assert.That(decodedByT01.Mutation, Is.TypeOf<SetSiteColor>());
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            for (int i = 0; i < 4; i++)
                bytes[offset + i] = (byte)(value >> (8 * i));
        }

        private sealed class FragmentingReadStream : Stream
        {
            private readonly byte[] m_Bytes;
            private readonly int m_MaximumRead;
            private int m_Offset;

            public FragmentingReadStream(byte[] bytes, int maximumRead)
            {
                m_Bytes = bytes;
                m_MaximumRead = maximumRead;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => throw new NotSupportedException();

            public override int Read(byte[] buffer, int offset, int count)
            {
                int available = Math.Min(Math.Min(count, m_MaximumRead), m_Bytes.Length - m_Offset);
                if (available <= 0)
                    return 0;
                Buffer.BlockCopy(m_Bytes, m_Offset, buffer, offset, available);
                m_Offset += available;
                return available;
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(Read(buffer, offset, count));
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        private sealed class HeaderOnlyStream : Stream
        {
            private readonly byte[] m_Header;
            private int m_Offset;
            public int ReadCalls { get; private set; }

            public HeaderOnlyStream(byte[] frame) => m_Header = frame;
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() => throw new NotSupportedException();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ReadCalls++;
                if (m_Offset >= V2TransportFrameCodec.HeaderLength)
                    throw new AssertionException("The oversized body must be rejected before the stream reads it.");
                int available = Math.Min(count, V2TransportFrameCodec.HeaderLength - m_Offset);
                Buffer.BlockCopy(m_Header, m_Offset, buffer, offset, available);
                m_Offset += available;
                return Task.FromResult(available);
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
