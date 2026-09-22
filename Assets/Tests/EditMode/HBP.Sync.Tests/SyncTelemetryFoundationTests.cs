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
            var origin = new SyncSetterOrigin();
            origin.CaptureFirst();
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 10000; i++)
            {
                SyncTelemetry.Mark(SyncProfile.SiteColor, Correlation, SyncMilestone.Setter);
                origin.CaptureFirst();
            }

            Assert.That(SyncTelemetry.ActiveCaptureGeneration, Is.Zero);
            Assert.That(origin.TryTake(out _), Is.False);
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
        public void SetterOrigin_PreservesTheFirstPointUntilConsumed()
        {
            var clock = new FakeMonotonicClock(1000);
            var sink = new CollectingSink();
            var origin = new SyncSetterOrigin();
            using (SyncTelemetry.BeginCapture(sink, clock))
            {
                origin.CaptureFirst();
                clock.Advance(TimeSpan.FromMilliseconds(9));
                origin.CaptureFirst();
                Assert.That(origin.TryTake(out SyncTelemetryPoint point), Is.True);
                Assert.That(point.Timestamp, Is.Zero);
            }

            var oldClock = new FakeMonotonicClock(1000);
            using (SyncTelemetry.BeginCapture(new CollectingSink(), oldClock))
                origin.CaptureFirst();

            var newClock = new FakeMonotonicClock(1000);
            newClock.Advance(TimeSpan.FromMilliseconds(125));
            using (SyncTelemetry.BeginCapture(new CollectingSink(), newClock))
            {
                origin.CaptureFirst();
                Assert.That(origin.TryTake(out SyncTelemetryPoint point), Is.True);
                Assert.That(point.Timestamp, Is.EqualTo(125));
            }

            var oldSink = new CollectingSink();
            SyncTelemetryPoint oldPoint;
            using (SyncTelemetry.BeginCapture(oldSink, new FakeMonotonicClock(1000)))
                oldPoint = SyncTelemetry.CapturePoint();
            var newSink = new CollectingSink();
            using (SyncTelemetry.BeginCapture(newSink, new FakeMonotonicClock(1000)))
                SyncTelemetry.MarkAt(SyncProfile.SiteColor, new SyncTelemetryIdentity("new", 1, 1), SyncMilestone.Setter, oldPoint);

            Assert.That(newSink.Samples, Is.Empty);
        }

        [Test]
        public void PendingReplacement_DoesNotReuseTheReplacedLogicalTrace()
        {
            var clock = new FakeMonotonicClock(1000);
            var sink = new CollectingSink();
            var tracker = new SyncTraceTracker();
            using (SyncTelemetry.BeginCapture(sink, clock))
            {
                tracker.Begin(SyncProfile.SiteColor, 11, SyncTelemetry.CapturePoint());
                SyncTraceCapture first = tracker.Capture(21, SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint());
                Assert.That(first.Replaced.IsEmpty, Is.True);

                tracker.Begin(SyncProfile.SiteColor, 12, SyncTelemetry.CapturePoint());
                SyncTraceCapture second = tracker.Capture(22, SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint());
                SyncTraceBatch pending = tracker.SnapshotPending();

                Assert.That(second.Replaced.Count, Is.EqualTo(1));
                Assert.That(second.Replaced[0].LogicalTraceId, Is.EqualTo(11));
                Assert.That(second.Replaced[0].CaptureGeneration, Is.EqualTo(21));
                Assert.That(pending.Count, Is.EqualTo(1));
                Assert.That(pending[0].LogicalTraceId, Is.EqualTo(12));
                Assert.That(pending[0].CaptureGeneration, Is.EqualTo(22));
            }
        }

        [Test]
        public void PendingAlreadyAcceptedForAttempt_IsNotReportedAsReplaced()
        {
            var tracker = new SyncTraceTracker();

            using (SyncTelemetry.BeginCapture(new BoundedSyncTelemetrySink(32), new FakeMonotonicClock(1000)))
            {
                SyncTelemetryPoint point = SyncTelemetry.CapturePoint();

                tracker.Begin(SyncProfile.TimelineAnchor, 11, point);
                tracker.Capture(21, point, point, point);

                SyncTraceBatch firstAttempt = tracker.SnapshotPendingForAttempt();

                Assert.That(firstAttempt.Count, Is.EqualTo(1));
                Assert.That(firstAttempt[0].LogicalTraceId, Is.EqualTo(11));

                tracker.Begin(SyncProfile.TimelineAnchor, 12, point);
                SyncTraceCapture next = tracker.Capture(22, point, point, point);

                Assert.That(next.Replaced.IsEmpty, Is.True);

                SyncTraceBatch pending = tracker.SnapshotPending();
                Assert.That(pending.Count, Is.EqualTo(1));
                Assert.That(pending[0].LogicalTraceId, Is.EqualTo(12));

                // Completion of the old in-flight attempt must not clear
                // the newer pending trace.
                tracker.ClearPending(firstAttempt);

                pending = tracker.SnapshotPending();
                Assert.That(pending.Count, Is.EqualTo(1));
                Assert.That(pending[0].LogicalTraceId, Is.EqualTo(12));
            }
        }

        [Test]
        public void Capture_ColorPendingThenCutDirty_PreservesBothProfiles()
        {
            AssertDisjointPendingIsPreserved(SyncProfile.SiteColor, SyncProfile.CutDefinition);
        }

        [Test]
        public void Capture_CutPendingThenColorDirty_PreservesBothProfiles()
        {
            AssertDisjointPendingIsPreserved(SyncProfile.CutDefinition, SyncProfile.SiteColor);
        }

        [Test]
        public void ClearPending_RemovesExactAcceptedIdentitiesAcrossMixedGenerations()
        {
            var clock = new FakeMonotonicClock(1000);
            var tracker = new SyncTraceTracker();
            using (SyncTelemetry.BeginCapture(new CollectingSink(), clock))
            {
                tracker.Begin(SyncProfile.SiteColor, 11, SyncTelemetry.CapturePoint());
                tracker.Capture(21, SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint());
                tracker.Begin(SyncProfile.CutDefinition, 12, SyncTelemetry.CapturePoint());
                tracker.Capture(22, SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint());
                SyncTraceBatch accepted = tracker.SnapshotPending();

                tracker.Begin(SyncProfile.SiteColor, 13, SyncTelemetry.CapturePoint());
                tracker.Capture(23, SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint());
                tracker.ClearPending(accepted);

                SyncTraceBatch remaining = tracker.SnapshotPending();
                Assert.That(remaining.Count, Is.EqualTo(1));
                Assert.That(remaining[0].Profile, Is.EqualTo(SyncProfile.SiteColor));
                Assert.That(remaining[0].LogicalTraceId, Is.EqualTo(13));
                Assert.That(remaining[0].CaptureGeneration, Is.EqualTo(23));
            }
        }

        [Test]
        public void Identity_KeepsExactAttemptsDistinctAndLabelsLegacyProxies()
        {
            var clock = new FakeMonotonicClock(1000);
            var sink = new CollectingSink();
            var tracker = new SyncTraceTracker();
            using (SyncTelemetry.BeginCapture(sink, clock))
            {
                tracker.Begin(SyncProfile.CutDefinition, 31, SyncTelemetry.CapturePoint());
                tracker.Capture(41, SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint());
                SyncLogicalTrace trace = tracker.SnapshotPending()[0];
                SyncTelemetryIdentity firstAttempt = trace.Identity("epoch", 51);
                SyncTelemetryIdentity reconnectAttempt = trace.Identity("epoch", 52);

                Assert.That(firstAttempt.LogicalTraceId, Is.EqualTo(reconnectAttempt.LogicalTraceId));
                Assert.That(firstAttempt.CaptureGeneration, Is.EqualTo(reconnectAttempt.CaptureGeneration));
                Assert.That(firstAttempt.AttemptId, Is.Not.EqualTo(reconnectAttempt.AttemptId));
                Assert.That(firstAttempt, Is.Not.EqualTo(reconnectAttempt));
            }

            SyncTelemetryIdentity proxy = SyncTelemetryIdentity.LegacyRevisionProxy("epoch", 42);
            SyncTelemetryIdentity unknown = SyncTelemetryIdentity.Unknown("transfer");

            Assert.That(proxy.LogicalTraceIdKind, Is.EqualTo(SyncTelemetryIdentityKind.LegacyProxy));
            Assert.That(proxy.CaptureGenerationKind, Is.EqualTo(SyncTelemetryIdentityKind.LegacyProxy));
            Assert.That(proxy.AttemptIdKind, Is.EqualTo(SyncTelemetryIdentityKind.Unavailable));
            Assert.That(unknown.LogicalTraceId, Is.Zero);
            Assert.That(unknown.CaptureGeneration, Is.Zero);
            Assert.That(unknown.LogicalTraceIdKind, Is.EqualTo(SyncTelemetryIdentityKind.Unavailable));
            Assert.That(unknown.CaptureGenerationKind, Is.EqualTo(SyncTelemetryIdentityKind.Unavailable));
        }

        private sealed class CollectingSink : ISyncTelemetrySink
        {
            public List<SyncTelemetrySample> Samples { get; } = new();
            public void Record(SyncTelemetrySample sample) => Samples.Add(sample);
        }

        private static void AssertDisjointPendingIsPreserved(SyncProfile firstProfile, SyncProfile secondProfile)
        {
            var clock = new FakeMonotonicClock(1000);
            var tracker = new SyncTraceTracker();
            using (SyncTelemetry.BeginCapture(new CollectingSink(), clock))
            {
                tracker.Begin(firstProfile, 11, SyncTelemetry.CapturePoint());
                tracker.Capture(21, SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint());
                tracker.Begin(secondProfile, 12, SyncTelemetry.CapturePoint());
                SyncTraceCapture capture = tracker.Capture(22, SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint(), SyncTelemetry.CapturePoint());

                SyncTraceBatch pending = tracker.SnapshotPending();
                Assert.That(capture.Replaced.IsEmpty, Is.True);
                Assert.That(pending.Count, Is.EqualTo(2));
                Assert.That(pending[0].Profile, Is.EqualTo(SyncProfile.SiteColor));
                Assert.That(pending[1].Profile, Is.EqualTo(SyncProfile.CutDefinition));
                Assert.That(pending[0].CaptureGeneration, Is.EqualTo(firstProfile == SyncProfile.SiteColor ? 21 : 22));
                Assert.That(pending[1].CaptureGeneration, Is.EqualTo(firstProfile == SyncProfile.CutDefinition ? 21 : 22));
            }
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
