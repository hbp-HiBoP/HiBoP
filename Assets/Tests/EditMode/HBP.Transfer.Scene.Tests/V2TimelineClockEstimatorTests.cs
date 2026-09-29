using System;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Transport;
using NUnit.Framework;

namespace HBP.Tests.Transfer.Scene
{
    [Category("Sync.Fast")]
    public sealed class V2TimelineClockEstimatorTests
    {
        private const long Frequency = 1000;
        private const long LongUptimeTicks = 5000000000L;

        [Test]
        public void Estimate_RequiresThreeSamplesAndHandlesAsymmetricLatencyAfterLongUptime()
        {
            var clock = new TestClock(Frequency);
            var estimator = new V2TimelineClockEstimator(clock);

            AddProbe(estimator, clock, LongUptimeTicks, outboundTicks: 80, processingTicks: 10, returnTicks: 80);
            Assert.That(estimator.TryEstimate(LongUptimeTicks + 60000, Frequency, 5, out _), Is.False);
            AddProbe(estimator, clock, LongUptimeTicks + 250, outboundTicks: 50, processingTicks: 10, returnTicks: 40);
            Assert.That(estimator.TryEstimate(LongUptimeTicks + 60250, Frequency, 5, out _), Is.False);
            AddProbe(estimator, clock, LongUptimeTicks + 500, outboundTicks: 20, processingTicks: 5, returnTicks: 30);

            Assert.That(estimator.TryEstimate(LongUptimeTicks + 60025, Frequency, 5, out V2TimelineAnchorTimingEstimate estimate), Is.True);
            Assert.That(estimate.AgeSeconds, Is.EqualTo(0.525d).Within(0.002d));
            Assert.That(estimate.UncertaintySeconds, Is.LessThanOrEqualTo(0.5d / 5d));
            Assert.That(estimator.RecentSampleCount, Is.EqualTo(3));
        }

        [Test]
        public void AddSampleIfPlaying_IgnoresPausedLivenessSamplesAndAcceptsPlaybackSamples()
        {
            var clock = new TestClock(Frequency);
            var estimator = new V2TimelineClockEstimator(clock);
            V2ClockProbeSample sample = CreateProbe(clock, LongUptimeTicks, outboundTicks: 20, processingTicks: 5, returnTicks: 30);

            Assert.That(estimator.AddSampleIfPlaying(sample, timelinePlaying: false), Is.False);
            Assert.That(estimator.RecentSampleCount, Is.Zero);
            Assert.That(estimator.AddSampleIfPlaying(sample, timelinePlaying: true), Is.True);
            Assert.That(estimator.RecentSampleCount, Is.EqualTo(1));
        }

        [Test]
        public void Estimate_RejectsInvalidStaleAndOutOfOrderSamples()
        {
            var clock = new TestClock(Frequency);
            var estimator = new V2TimelineClockEstimator(clock);
            AddProbe(estimator, clock, LongUptimeTicks, 20, 5, 30);
            AddProbe(estimator, clock, LongUptimeTicks + 250, 20, 5, 30);
            AddProbe(estimator, clock, LongUptimeTicks + 500, 20, 5, 30);

            Assert.That(estimator.AddSample(new V2ClockProbeSample(LongUptimeTicks + 500, LongUptimeTicks + 520, Frequency, LongUptimeTicks + 60000, LongUptimeTicks + 60030, Frequency)), Is.False);
            Assert.That(estimator.AddSample(new V2ClockProbeSample(LongUptimeTicks + 400, LongUptimeTicks + 600, Frequency, LongUptimeTicks + 60400, LongUptimeTicks + 60405, Frequency)), Is.False);

            clock.Advance(TimeSpan.FromSeconds(1.01d));
            Assert.That(estimator.TryEstimate(LongUptimeTicks + 60025, Frequency, 5, out _), Is.False);
        }

        [Test]
        public void Estimate_RejectsUncertaintyAboveHalfSample()
        {
            var clock = new TestClock(Frequency);
            var estimator = new V2TimelineClockEstimator(clock);
            AddProbe(estimator, clock, LongUptimeTicks, 60, 10, 60);
            AddProbe(estimator, clock, LongUptimeTicks + 250, 60, 10, 60);
            AddProbe(estimator, clock, LongUptimeTicks + 500, 60, 10, 60);

            Assert.That(estimator.TryEstimate(LongUptimeTicks + 60070, Frequency, 20, out _), Is.False);
        }

        [Test]
        public void Estimate_DoesNotReuseLowestRttOffsetAfterItBecomesStale()
        {
            var clock = new TestClock(Frequency);
            var estimator = new V2TimelineClockEstimator(clock);

            AddProbe(estimator, clock, LongUptimeTicks, outboundTicks: 1, processingTicks: 1, returnTicks: 1, remoteOffsetTicks: 120000);
            for (int i = 1; i <= 5; i++)
                AddProbe(estimator, clock, LongUptimeTicks + i * 250, outboundTicks: 30, processingTicks: 1, returnTicks: 30);

            Assert.That(estimator.TryEstimate(LongUptimeTicks + 60500, Frequency, 5, out V2TimelineAnchorTimingEstimate estimate), Is.True);
            Assert.That(estimate.AgeSeconds, Is.EqualTo(0.81d).Within(0.02d));
        }

        private static void AddProbe(V2TimelineClockEstimator estimator, TestClock clock, long localSendTicks, long outboundTicks, long processingTicks, long returnTicks, long remoteOffsetTicks = 60000)
        {
            V2ClockProbeSample sample = CreateProbe(clock, localSendTicks, outboundTicks, processingTicks, returnTicks, remoteOffsetTicks);
            Assert.That(estimator.AddSample(sample), Is.True);
        }

        private static V2ClockProbeSample CreateProbe(TestClock clock, long localSendTicks, long outboundTicks, long processingTicks, long returnTicks, long remoteOffsetTicks = 60000)
        {
            long localReceiveTicks = localSendTicks + outboundTicks + processingTicks + returnTicks;
            long elapsedTicks = localReceiveTicks - clock.GetTimestamp();
            if (elapsedTicks > 0) clock.Advance(TimeSpan.FromSeconds((double)elapsedTicks / Frequency));
            return new V2ClockProbeSample(localSendTicks, localReceiveTicks, Frequency, localSendTicks + remoteOffsetTicks + outboundTicks, localSendTicks + remoteOffsetTicks + outboundTicks + processingTicks, Frequency);
        }

        private sealed class TestClock : IMonotonicClock
        {
            private long m_Timestamp;
            public long Frequency { get; }
            public long GetTimestamp() => m_Timestamp;

            public TestClock(long frequency) => Frequency = frequency;
            public void Advance(TimeSpan duration) => m_Timestamp = checked(m_Timestamp + (long)(duration.TotalSeconds * Frequency));
        }
    }
}
