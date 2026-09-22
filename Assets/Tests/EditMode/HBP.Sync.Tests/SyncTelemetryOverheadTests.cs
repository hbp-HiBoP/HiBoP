using System;
using System.Diagnostics;
using System.Globalization;
using HBP.Sync.Testing;
using NUnit.Framework;

namespace HBP.Sync.Tests
{
    [Category(SyncTestCategories.Qualification)]
    public class SyncTelemetryOverheadTests
    {
        [Test]
        public void ReportProbeAndSinkCostSeparatelyFromApplicationCost()
        {
            const int Iterations = 10000;
            Assert.That(SyncTelemetry.Enabled, Is.False);
            // Warm each path independently. Do not include NUnit/report formatting in the interval.
            for (int i = 0; i < 1000; i++) SyncTelemetry.CapturePoint();
            Measure("disabled-raw-point", Iterations, false);
            var sink = new BoundedSyncTelemetrySink(Iterations + 1000);
            using (SyncTelemetry.BeginCapture(sink))
            {
                for (int i = 0; i < 1000; i++) SyncTelemetry.CapturePoint();
                Measure("enabled-raw-point", Iterations, false);
                var identity = new SyncTelemetryIdentity("overhead-only", 1, 1);
                for (int i = 0; i < 1000; i++) SyncTelemetry.Mark(SyncProfile.SiteColor, identity, SyncMilestone.Setter);
                Measure("enabled-point-and-sink", Iterations, true);
            }

            Assert.That(sink.Dropped, Is.Zero);
        }

        private static void Measure(string label, int count, bool publish)
        {
            var identity = new SyncTelemetryIdentity("overhead-only", 1, 1);
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < count; i++)
            {
                if (publish) SyncTelemetry.Mark(SyncProfile.SiteColor, identity, SyncMilestone.Setter);
                else SyncTelemetry.CapturePoint();
            }

            long ticks = Stopwatch.GetTimestamp() - start;
            long bytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            double nanoseconds = 1e9 * ticks / Stopwatch.Frequency / count;
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture, "HBP_SYNC_OVERHEAD path={0} iterations={1} ticks={2} frequency={3} allocatedBytes={4} nsPerProbe={5:F2}", label, count, ticks, Stopwatch.Frequency, bytes, nanoseconds));
        }
    }
}
