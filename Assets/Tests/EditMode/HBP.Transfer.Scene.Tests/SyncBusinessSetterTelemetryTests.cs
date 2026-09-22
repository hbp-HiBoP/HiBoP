using System;
using System.Linq;
using HBP.Core.Enums;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.Sync;
using HBP.Sync.Scene;
using NUnit.Framework;
using UnityEngine;
using SceneCut = HBP.Core.Object3D.Cut;

namespace HBP.Tests.Transfer.Scene
{
    [Category("Sync.SceneFocused")]
    public class SyncBusinessSetterTelemetryTests
    {
        [Test]
        public void SiteColorSetter_PreservesItsFirstPreMutationPoint()
        {
            var clock = new TestClock();
            var state = new SiteState();
            using (SyncTelemetry.BeginCapture(new NullSink(), clock))
            using (var observer = Observe(sites: new[] { state }))
            {
                state.Color = Color.red;
                clock.Advance();
                state.Color = Color.blue;

                AssertOrigin(observer, SyncProfile.SiteColor, 0);
            }
        }

        [Test]
        public void CutPositionSetter_ReportsTheBusinessOrigin()
        {
            AssertCutOrigin(cut => cut.Position = 0.75f);
        }

        [Test]
        public void CutFlipSetter_ReportsTheBusinessOrigin()
        {
            AssertCutOrigin(cut => cut.Flip = true);
        }

        [TestCase(2f, 0f, 0f, TestName = "CutCustomNormalX_ReportsTheBusinessOrigin")]
        [TestCase(1f, 1f, 0f, TestName = "CutCustomNormalY_ReportsTheBusinessOrigin")]
        [TestCase(1f, 0f, 1f, TestName = "CutCustomNormalZ_ReportsTheBusinessOrigin")]
        public void CutNormalSetter_ReportsTheBusinessOrigin(float x, float y, float z)
        {
            AssertCutOrigin(cut => cut.Normal = new Vector3(x, y, z));
        }

        [Test]
        public void TimelineSetter_PreservesItsFirstPointBeforePlaybackWork()
        {
            var clock = new TestClock();
            var timeline = new FMRITimeline();
            using (SyncTelemetry.BeginCapture(new NullSink(), clock))
            using (var observer = Observe(timelines: new BasicTimeline[] { timeline }))
            {
                timeline.IsPlaying = true;
                clock.Advance();
                timeline.Step = 2;

                AssertOrigin(observer, SyncProfile.TimelineAnchor, 0);
            }
        }

        [Test]
        public void CutOrientationSetter_ReportsTheBusinessOrigin()
        {
            AssertCutOrigin(cut => cut.Orientation = Enum.GetValues(typeof(CutOrientation)).Cast<CutOrientation>().First(value => value != cut.Orientation));
        }

        [Test]
        public void CutNumberOfCutsSetter_ReportsTheBusinessOrigin()
        {
            AssertCutOrigin(cut => cut.NumberOfCuts = cut.NumberOfCuts + 1);
        }

        [TestCase("index")]
        [TestCase("play")]
        [TestCase("pause")]
        [TestCase("loop")]
        [TestCase("step")]
        public void EachTimelineInput_ReportsItsOwnPreMutationOrigin(string input)
        {
            var timeline = new TestTimeline();
            if (input == "pause") timeline.IsPlaying = true;
            var clock = new TestClock();
            using (SyncTelemetry.BeginCapture(new NullSink(), clock))
            using (var observer = Observe(timelines: new BasicTimeline[] { timeline }))
            {
                switch (input)
                {
                    case "index": timeline.CurrentIndex = 2; break;
                    case "play": timeline.IsPlaying = true; break;
                    case "pause": timeline.IsPlaying = false; break;
                    case "loop": timeline.IsLooping = true; break;
                    case "step": timeline.Step = 2; break;
                }

                clock.Advance();
                AssertOrigin(observer, SyncProfile.TimelineAnchor, 0);
            }
        }

        private sealed class TestTimeline : BasicTimeline
        {
            public TestTimeline() => Length = 10;
            public override SubTimeline CurrentSubtimeline => null;
        }

        private static void AssertCutOrigin(Action<SceneCut> mutate)
        {
            var clock = new TestClock();
            using var cut = new SceneCut();
            using (SyncTelemetry.BeginCapture(new NullSink(), clock))
            using (var observer = Observe(cuts: new[] { cut }))
            {
                mutate(cut);
                clock.Advance();
                cut.Position = 0.25f;

                AssertOrigin(observer, SyncProfile.CutDefinition, 0);
            }
        }

        private static SyncSetterOriginObserver Observe(SiteState[] sites = null, SceneCut[] cuts = null, BasicTimeline[] timelines = null)
        {
            return new SyncSetterOriginObserver(sites ?? Array.Empty<SiteState>(), cuts ?? Array.Empty<SceneCut>(), timelines ?? Array.Empty<BasicTimeline>());
        }

        private static void AssertOrigin(SyncSetterOriginObserver observer, SyncProfile profile, long timestamp)
        {
            Assert.That(observer.TryTake(profile, out SyncTelemetryPoint point), Is.True);
            Assert.That(point.Timestamp, Is.EqualTo(timestamp));
        }

        private sealed class TestClock : IMonotonicClock
        {
            private long m_Timestamp;
            public long Frequency => 1000;
            public long GetTimestamp() => m_Timestamp;
            public void Advance() => ++m_Timestamp;
        }

        private sealed class NullSink : ISyncTelemetrySink
        {
            public void Record(SyncTelemetrySample sample)
            {
            }
        }
    }
}
