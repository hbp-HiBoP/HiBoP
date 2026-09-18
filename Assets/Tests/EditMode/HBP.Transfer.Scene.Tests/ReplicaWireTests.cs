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
    }
}
