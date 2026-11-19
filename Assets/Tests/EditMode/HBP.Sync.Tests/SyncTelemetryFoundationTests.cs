using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBP.Sync.Testing;
using NUnit.Framework;

namespace HBP.Sync.Tests
{
    [Category(SyncTestCategories.Fast)]
    public class SyncTelemetryFoundationTests
    {
        [Test]
        public async Task AsyncGate_OpensResetsAndCancelsWithoutWallClockTime()
        {
            var clock = new FakeMonotonicClock(1000);
            clock.Advance(TimeSpan.FromMilliseconds(125));
            Assert.That(clock.GetTimestamp(), Is.EqualTo(125));
            Assert.That(clock.Frequency, Is.EqualTo(1000));

            var gate = new DeterministicAsyncGate();
            Task waiting = gate.WaitAsync();
            Assert.That(waiting.IsCompleted, Is.False);

            gate.Open();
            await waiting;
            Assert.That(gate.IsOpen, Is.True);

            gate.Reset();
            using var cancellation = new CancellationTokenSource();
            waiting = gate.WaitAsync(cancellation.Token);
            Assert.That(waiting.IsCompleted, Is.False);

            cancellation.Cancel();

            Exception observed = null;
            try
            {
                await waiting;
            }
            catch (Exception exception)
            {
                observed = exception;
            }

            Assert.That(observed, Is.InstanceOf<OperationCanceledException>());

            var sink = new BlockingSink();
            ISyncTelemetryCapture capture = SyncTelemetry.BeginCapture(sink, new FakeMonotonicClock(1000));
            SyncTelemetryPoint point = SyncTelemetry.CapturePoint();
            Task writer = Task.Run(() => SyncTelemetry.MarkAt(SyncProfile.SiteColor, new SyncTelemetryIdentity("scope", 1, 1), SyncMilestone.Setter, point));
            try
            {
                await sink.Entered;
                // CloseAsync stops admission before returning. This is not a thread-scheduling assertion.
                Task close = capture.CloseAsync();
                Assert.That(SyncTelemetry.Enabled, Is.False);
                Assert.That(close.IsCompleted, Is.False);
                Assert.That(capture.CloseAsync(), Is.SameAs(close));
                SyncTelemetry.MarkAt(SyncProfile.SiteColor, "late", SyncMilestone.Setter, point);
                Assert.That(sink.Count, Is.EqualTo(1));
                var nextSink = new BoundedSyncTelemetrySink(4);
                using (SyncTelemetry.BeginCapture(nextSink, new FakeMonotonicClock(1000)))
                {
                    SyncTelemetry.MarkAt(SyncProfile.SiteColor, "stale", SyncMilestone.Setter, point);
                    SyncTelemetry.Mark(SyncProfile.SiteColor, "new", SyncMilestone.Setter);
                    sink.Release();
                    await writer;
                    await close;
                    Assert.That(SyncTelemetry.Enabled, Is.True, "An older close must not detach the newer capture.");
                    Assert.That(nextSink.Count, Is.EqualTo(1));
                }
            }
            finally
            {
                // A failed assertion must not leave a blocked worker or active process-global capture.
                sink.Release();
                await writer;
                await capture.CloseAsync();
            }

            Assert.That(sink.Count, Is.EqualTo(1));
            Assert.That(SyncTelemetry.Enabled, Is.False);
        }

        [Test]
        public void Telemetry_RecordsEveryRequiredMilestoneAndReplaysCapturedPoints()
        {
            var clock = new FakeMonotonicClock(1000);
            var sink = new CollectingSink();
            SyncMilestone[] milestones = Enum.GetValues(typeof(SyncMilestone)).Cast<SyncMilestone>().ToArray();
            using (SyncTelemetry.BeginCapture(sink, clock, Thread.CurrentThread.ManagedThreadId))
            {
                foreach (SyncMilestone milestone in milestones)
                {
                    SyncTelemetry.Mark(SyncProfile.SiteColor, "scene-a/7/site-color", milestone, 24);
                    clock.Advance(TimeSpan.FromMilliseconds(1));
                }
            }

            Assert.That(sink.Samples.Select(sample => sample.Milestone), Is.EqualTo(milestones));
            Assert.That(sink.Samples.Select(sample => sample.Timestamp), Is.EqualTo(Enumerable.Range(0, milestones.Length).Select(value => (long)value)));
            Assert.That(sink.Samples.All(sample => sample.IsMainThread), Is.True);
            Assert.That(sink.Samples.All(sample => sample.PayloadBytes == 24), Is.True);

            clock = new FakeMonotonicClock(1000);
            sink = new CollectingSink();
            using (SyncTelemetry.BeginCapture(sink, clock))
            {
                SyncTelemetryPoint setter = SyncTelemetry.CapturePoint();
                clock.Advance(TimeSpan.FromMilliseconds(8));
                SyncTelemetry.MarkAt(SyncProfile.CutDefinition, "epoch/4/cut", SyncMilestone.Setter, setter);
                SyncTelemetry.Mark(SyncProfile.CutDefinition, "epoch/4/cut", SyncMilestone.Queued);
            }

            Assert.That(sink.Samples[0].Timestamp, Is.EqualTo(0));
            Assert.That(sink.Samples[1].Timestamp, Is.EqualTo(8));
            Assert.That(sink.Samples[0].CorrelationId, Is.EqualTo(sink.Samples[1].CorrelationId));
        }

        [Test]
        public void DisabledTelemetryAndBoundedSink_HaveExplicitBounds()
        {
            Assert.That(SyncTelemetry.Enabled, Is.False);
            Assert.That(SyncTelemetry.CapturePoint().IsValid, Is.False);
            const string Correlation = "disabled";
            SyncTelemetry.Mark(SyncProfile.SiteColor, Correlation, SyncMilestone.Setter);
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 10000; i++)
            {
                SyncTelemetry.Mark(SyncProfile.SiteColor, Correlation, SyncMilestone.Setter);
            }

            Assert.That(SyncTelemetry.ActiveCaptureGeneration, Is.Zero);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);

            var sink = new BoundedSyncTelemetrySink(2);
            using (SyncTelemetry.BeginCapture(sink, new FakeMonotonicClock(1000)))
            {
                SyncTelemetry.Mark(SyncProfile.SiteColor, "scope", SyncMilestone.Setter);
                SyncTelemetry.Mark(SyncProfile.CutDefinition, "scope", SyncMilestone.Setter);
                SyncTelemetry.Mark(SyncProfile.TimelineAnchor, "scope", SyncMilestone.Setter);
            }

            Assert.That(sink.Capacity, Is.EqualTo(2));
            Assert.That(sink.Count, Is.EqualTo(2));
            Assert.That(sink.Dropped, Is.EqualTo(1));
            Assert.That(sink.TryGet(0, out SyncTelemetrySample first), Is.True);
            Assert.That(first.Profile, Is.EqualTo(SyncProfile.SiteColor));
            Assert.That(sink.TryGet(2, out _), Is.False);
        }

        [Test]
        public void Identity_KeepsExactAttemptsDistinctAndInitialTransferUnknown()
        {
            var first = new SyncTelemetryIdentity("scene", 31, 41, 51);
            var retry = first.WithAttempt(52);
            Assert.That(first.LogicalTraceId, Is.EqualTo(retry.LogicalTraceId));
            Assert.That(first.CaptureGeneration, Is.EqualTo(retry.CaptureGeneration));
            Assert.That(first, Is.Not.EqualTo(retry));
            Assert.That(first.AttemptIdKind, Is.EqualTo(SyncTelemetryIdentityKind.Exact));
            var unknown = SyncTelemetryIdentity.Unknown("transfer");
            Assert.That(unknown.LogicalTraceIdKind, Is.EqualTo(SyncTelemetryIdentityKind.Unavailable));
            Assert.That(unknown.CaptureGenerationKind, Is.EqualTo(SyncTelemetryIdentityKind.Unavailable));
        }

        private sealed class CollectingSink : ISyncTelemetrySink
        {
            public List<SyncTelemetrySample> Samples { get; } = new();
            public void Record(SyncTelemetrySample sample) => Samples.Add(sample);
        }

        private sealed class BlockingSink : ISyncTelemetrySink
        {
            private readonly object gate = new();
            private readonly TaskCompletionSource<bool> entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private bool released;

            public Task Entered => entered.Task;
            public int Count { get; private set; }

            public void Record(SyncTelemetrySample sample)
            {
                lock (gate)
                {
                    ++Count;
                    entered.TrySetResult(true);
                    while (!released) Monitor.Wait(gate);
                }
            }

            public void Release()
            {
                lock (gate)
                {
                    released = true;
                    Monitor.PulseAll(gate);
                }
            }
        }
    }
}
