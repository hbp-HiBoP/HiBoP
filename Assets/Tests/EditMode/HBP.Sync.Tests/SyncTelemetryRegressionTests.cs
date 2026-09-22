using System;
using System.Linq;
using HBP.Sync.Testing;
using NUnit.Framework;

namespace HBP.Sync.Tests
{
    [Category(SyncTestCategories.Fast)]
    public class SyncTelemetryRegressionTests
    {
        [Test]
        public void CaptureRestart_ReplacesOldDirtyOriginButPreservesFirstNewSetter()
        {
            var tracker = new SyncTraceTracker();
            using (SyncTelemetry.BeginCapture(new BoundedSyncTelemetrySink(16), new FakeMonotonicClock(1000)))
                tracker.Begin(SyncProfile.SiteColor, 1, SyncTelemetry.CapturePoint());

            var clock = new FakeMonotonicClock(1000);
            clock.Advance(TimeSpan.FromMilliseconds(100));
            long expectedSetterTimestamp = clock.GetTimestamp();
            using (SyncTelemetry.BeginCapture(new BoundedSyncTelemetrySink(16), clock))
            {
                tracker.Begin(SyncProfile.SiteColor, 2, SyncTelemetry.CapturePoint());
                clock.Advance(TimeSpan.FromMilliseconds(10));
                tracker.Begin(SyncProfile.SiteColor, 3, SyncTelemetry.CapturePoint());
                SyncTelemetryPoint point = SyncTelemetry.CapturePoint();
                SyncTraceCapture result = tracker.Capture(7, point, point, point);
                Assert.That(result.Captured.Count, Is.EqualTo(1));
                Assert.That(result.Captured[0].LogicalTraceId, Is.EqualTo(2));
                Assert.That(result.Captured[0].Setter.Timestamp, Is.EqualTo(expectedSetterTimestamp));
            }
        }

        [Test]
        public void CaptureRestart_DoesNotExposeOldPendingOrConsumeNewDirtyWithMixedPoints()
        {
            var tracker = new SyncTraceTracker();
            SyncTelemetryPoint oldPoint;
            using (SyncTelemetry.BeginCapture(new BoundedSyncTelemetrySink(16), new FakeMonotonicClock(1000)))
            {
                oldPoint = SyncTelemetry.CapturePoint();
                tracker.Begin(SyncProfile.CutDefinition, 1, oldPoint);
                tracker.Capture(1, oldPoint, oldPoint, oldPoint);
            }

            using (SyncTelemetry.BeginCapture(new BoundedSyncTelemetrySink(16), new FakeMonotonicClock(1000)))
            {
                Assert.That(tracker.SnapshotPending().IsEmpty, Is.True);
                SyncTelemetryPoint current = SyncTelemetry.CapturePoint();
                tracker.Begin(SyncProfile.TimelineAnchor, 2, current);
                Assert.That(tracker.Capture(2, oldPoint, current, current).Captured.IsEmpty, Is.True);
                SyncTraceCapture result = tracker.Capture(3, current, current, current);
                Assert.That(result.Captured.Count, Is.EqualTo(1));
                Assert.That(result.Captured[0].LogicalTraceId, Is.EqualTo(2));
                Assert.That(tracker.SnapshotPending().Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void CaptureRestart_OldAcknowledgementCannotClearNewRecordingWithReusedIds()
        {
            var tracker = new SyncTraceTracker();
            SyncTraceBatch previous;
            using (SyncTelemetry.BeginCapture(new BoundedSyncTelemetrySink(4)))
            {
                SyncTelemetryPoint point = SyncTelemetry.CapturePoint();
                tracker.Begin(SyncProfile.SiteColor, 1, point);
                tracker.Capture(1, point, point, point);
                previous = tracker.SnapshotPending();
            }

            using (SyncTelemetry.BeginCapture(new BoundedSyncTelemetrySink(4)))
            {
                SyncTelemetryPoint point = SyncTelemetry.CapturePoint();
                tracker.Begin(SyncProfile.SiteColor, 1, point);
                tracker.Capture(1, point, point, point);
                tracker.ClearPending(previous);
                Assert.That(tracker.SnapshotPending().Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void CaptureRestart_RejectsStaleUnconsumedObserverPoint()
        {
            var origin = new SyncSetterOrigin();
            using (SyncTelemetry.BeginCapture(new BoundedSyncTelemetrySink(1))) origin.CaptureFirst();
            using (SyncTelemetry.BeginCapture(new BoundedSyncTelemetrySink(1)))
                Assert.That(origin.TryTake(out _), Is.False);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void ReceiveFacts_PreserveOnlyReachedBoundariesOnFailure(bool applied, bool visible)
        {
            var clock = new FakeMonotonicClock(1000);
            var sink = new BoundedSyncTelemetrySink(16);
            using (SyncTelemetry.BeginCapture(sink, clock))
            {
                SyncTelemetryPoint first = SyncTelemetry.CapturePoint();
                clock.Advance(TimeSpan.FromMilliseconds(1));
                var trace = new SyncReceiveTelemetry(first, SyncTelemetry.CapturePoint(), 128);
                clock.Advance(TimeSpan.FromMilliseconds(1));
                trace.CaptureApplyStart();
                if (applied)
                {
                    clock.Advance(TimeSpan.FromMilliseconds(1));
                    trace.CaptureApplyEnd();
                }

                if (visible)
                {
                    clock.Advance(TimeSpan.FromMilliseconds(1));
                    trace.CaptureNextVisible();
                }

                // Publication is the same operation used by the receiver's failure/success finally.
                clock.Advance(TimeSpan.FromMilliseconds(100));
                trace.Publish(SyncProfile.SiteColor, SyncTelemetryIdentity.LegacyRevisionProxy("epoch", 1));
            }

            SyncTelemetrySample[] samples = Enumerable.Range(0, sink.Count).Select(index =>
            {
                sink.TryGet(index, out SyncTelemetrySample sample);
                return sample;
            }).ToArray();
            Assert.That(samples.Length, Is.EqualTo(3 + (applied ? 1 : 0) + (visible ? 1 : 0)));
            Assert.That(samples.Select(sample => sample.Timestamp), Is.EqualTo(Enumerable.Range(0, samples.Length).Select(value => (long)value)));
            Assert.That(samples.Any(sample => sample.Milestone == SyncMilestone.ScientificStable), Is.False);
            Assert.That(samples.Any(sample => sample.Milestone == SyncMilestone.PublicationReceipt), Is.False);
        }
    }
}
