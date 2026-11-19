using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Transport;
using NUnit.Framework;

namespace HBP.Tests.Transfer
{
    [TestFixture, Category("Sync.Fast")]
    public sealed class SignalBulkReceiverTests
    {
        [Test]
        public void PresentationBulkAllowsSelectionToPassBeforeTheBodyCompletes()
        {
            var session = new SessionId(Guid.NewGuid());
            var scene = new SceneId(Guid.NewGuid());
            var incarnation = new IncarnationId(Guid.NewGuid());
            var scheduler = new OutgoingScheduler(session, scene, incarnation, OriginDevice.Desktop);
            byte[] body = SignalWire.Encode(new SignalMessage { Kind = SignalMessageKind.Resource, Generation = Guid.NewGuid(), Key = new string('A', 64), Total = 100000, Body = new byte[100000] });
            var descriptor = ScheduleDescriptor.ForPresentation(scene, incarnation);
            Assert.That(descriptor.BarrierScope, Is.EqualTo(BarrierScope.None));
            Assert.That(descriptor.TouchedKeys, Is.Empty);
            Assert.That(scheduler.EnqueueSceneOperation(body, descriptor, bodySchema: SignalWire.BodySchema).Accepted, Is.True);
            Assert.That(scheduler.EnqueueMutation(new SetSelectedSite(new ColumnId("column"), new SiteId("site")), canonicalSequence: 1).Accepted, Is.True);
            using var receiver = new SignalBulkReceiver();
            bool sawSelection = false, completed = false;
            while (scheduler.TryGetNextTransmission(out var attempt))
            {
                var f = attempt.Frame;
                var record = new TransportRecord(TransportMessageKind.Application, session, scene, incarnation, f.OperationId, f.StreamId, f.ReliableFrameSequence ?? 0, f.OriginSequence ?? 0, OriginDevice.Desktop, f.Lane, f.BodySchema, f.ChunkIndex, f.GetPayloadCopy(), f.CanonicalSequence, f.ObservedCanonicalSequence);
                record = TransportFrameCodec.Decode(TransportFrameCodec.Encode(record));
                if (receiver.TryReceive(record, out var received, out var application))
                {
                    if (received != null)
                    {
                        CollectionAssert.AreEqual(body, received);
                        Assert.That(application.OriginSequence, Is.GreaterThan(0));
                        Assert.That(sawSelection, Is.True);
                        completed = true;
                    }
                }
                else
                {
                    Assert.That(completed, Is.False);
                    Assert.That(MutationPayloadCodec.Decode(record.GetPayloadCopy()), Is.TypeOf<SetSelectedSite>());
                    sawSelection = true;
                }

                scheduler.Acknowledge(f.StreamId, f.ReliableFrameSequence.Value);
            }

            Assert.That(completed, Is.True);
        }

        [Test]
        public void AbandoningBulkReturnsItsApplicationOriginForRetirement()
        {
            var session = new SessionId(Guid.NewGuid());
            var scene = new SceneId(Guid.NewGuid());
            var incarnation = new IncarnationId(Guid.NewGuid());
            var scheduler = new OutgoingScheduler(session, scene, incarnation, OriginDevice.Desktop);
            scheduler.EnqueueSceneOperation(new byte[10000], ScheduleDescriptor.ForPresentation(scene, incarnation), bodySchema: SignalWire.BodySchema);
            Assert.That(scheduler.TryGetNextTransmission(out var attempt), Is.True);
            var f = attempt.Frame;
            using var receiver = new SignalBulkReceiver();
            var record = new TransportRecord(TransportMessageKind.Application, session, scene, incarnation, f.OperationId, f.StreamId, f.ReliableFrameSequence ?? 0, f.OriginSequence ?? 0, OriginDevice.Desktop, f.Lane, f.BodySchema, payload: f.GetPayloadCopy());
            Assert.That(receiver.TryReceive(record, out _, out var completed), Is.True);
            Assert.That(completed, Is.Null);
            var abandoned = receiver.Clear();
            Assert.That(abandoned.Single().OriginSequence, Is.EqualTo(record.OriginSequence));
            Assert.That(receiver.Clear(), Is.Empty);
        }
    }
}
