using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Transport;
using NUnit.Framework;

namespace HBP.Tests.Transfer.Scene
{
    public class ReplicaWireTests
    {
        [Test]
        public async Task BoundedFrameAndResourceRoundTrip()
        {
            using var stream = new MemoryStream();
            byte[] resource = { 1, 2, 3, 4 };
            await ReplicaWire.WriteAsync(stream, ReplicaFrameKind.Resource, ReplicaWire.Resource("corr:abc", resource), CancellationToken.None);
            await ReplicaWire.WriteAsync(stream, ReplicaFrameKind.Visible, ReplicaWire.Revision(12), CancellationToken.None);
            stream.Position = 0;
            var first = await ReplicaWire.ReadAsync(stream, CancellationToken.None);
            var decoded = ReplicaWire.ReadResource(first.Body);
            Assert.That(first.Kind, Is.EqualTo(ReplicaFrameKind.Resource));
            Assert.That(decoded.Reference, Is.EqualTo("corr:abc"));
            Assert.That(decoded.Data, Is.EqualTo(resource));
            var second = await ReplicaWire.ReadAsync(stream, CancellationToken.None);
            Assert.That(second.Kind, Is.EqualTo(ReplicaFrameKind.Visible));
            Assert.That(ReplicaWire.ReadRevision(second.Body), Is.EqualTo(12));
        }

        [Test]
        public void OversizedLengthIsRejectedBeforeAllocation()
        {
            using var stream = new MemoryStream(BitConverter.GetBytes(ReplicaWire.MaximumFrameBytes + 1));
            Assert.ThrowsAsync<InvalidDataException>(async () => await ReplicaWire.ReadAsync(stream, CancellationToken.None));
        }

        [Test]
        [Category("Sync.Loopback")]
        public async Task ReadProgress_ReportsDeterministicFrameBoundaries()
        {
            using var stream = new MemoryStream();
            await ReplicaWire.WriteAsync(stream, ReplicaFrameKind.Applied, ReplicaWire.Revision(9), CancellationToken.None);
            stream.Position = 0;
            var stages = new System.Collections.Generic.List<ReplicaReadStage>();

            var frame = await ReplicaWire.ReadAsync(stream, CancellationToken.None, stages.Add);

            Assert.That(frame.Kind, Is.EqualTo(ReplicaFrameKind.Applied));
            Assert.That(stages, Is.EqualTo(new[] { ReplicaReadStage.FirstBytes, ReplicaReadStage.Complete }));
        }

        [Test]
        [Category("Sync.Loopback")]
        public async Task WriteProgress_ReportsOnlyCompletedWrites()
        {
            using var stream = new MemoryStream();
            var stages = new System.Collections.Generic.List<ReplicaWriteStage>();

            await ReplicaWire.WriteAsync(stream, ReplicaFrameKind.Applied, ReplicaWire.Revision(9), CancellationToken.None, stages.Add);

            Assert.That(stages, Is.EqualTo(new[] { ReplicaWriteStage.FirstBytes, ReplicaWriteStage.Complete }));
        }

        [Test]
        [Category("Sync.Loopback")]
        public async Task FailedWrite_DoesNotReportCompletion()
        {
            using var stream = new FailAfterFirstWriteStream();
            var stages = new System.Collections.Generic.List<ReplicaWriteStage>();
            Exception failure = null;
            try
            {
                await ReplicaWire.WriteAsync(stream, ReplicaFrameKind.Applied, ReplicaWire.Revision(9), CancellationToken.None, stages.Add);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            Assert.That(failure, Is.TypeOf<IOException>());
            Assert.That(stages, Is.EqualTo(new[] { ReplicaWriteStage.FirstBytes }));
        }

        private sealed class FailAfterFirstWriteStream : MemoryStream
        {
            private int writes;

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (++writes > 1) return Task.FromException(new IOException("disconnected"));
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }
        }
    }
}
