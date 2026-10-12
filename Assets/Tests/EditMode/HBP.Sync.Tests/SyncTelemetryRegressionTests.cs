using System;
using System.Linq;
using HBP.Sync.Testing;
using NUnit.Framework;

namespace HBP.Sync.Tests
{
    [Category(SyncTestCategories.Fast)]
    public class SyncTelemetryRegressionTests
    {
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
                trace.Publish(SyncProfile.SiteColor, new SyncTelemetryIdentity("scene", 1, 1));
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
