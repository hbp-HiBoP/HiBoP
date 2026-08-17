using System;
using HBP.Core.Data;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class Stage5ProjectionBufferTests
    {
        [Test]
        public void FlattenTimeMajor_ProducesNativeProjectionLayout()
        {
            float[][] values = { new[] { 1f, 2f, 3f }, new[] { 10f, 20f, 30f } };

            float[] flattened = ProjectionBufferBuilder.FlattenTimeMajor(values, new[] { false, false }, 3, out _, out _, out _);

            Assert.That(flattened, Is.EqualTo(new[] { 1f, 10f, 2f, 20f, 3f, 30f }));
        }

        [Test]
        public void FlattenTimeMajor_ExcludesMaskedSitesFromStreamingStatistics()
        {
            float[][] values = { new[] { 1f, 3f }, new[] { -100f, 100f }, new[] { 5f, 7f } };

            ProjectionBufferBuilder.FlattenTimeMajor(values, new[] { false, true, false }, 2, out RunningStatistics statistics, out float minimum, out float maximum);

            Assert.That(statistics.Count, Is.EqualTo(4));
            Assert.That(statistics.Mean, Is.EqualTo(4f).Within(0.000001f));
            Assert.That(statistics.StandardDeviation, Is.EqualTo(2.5819888f).Within(0.000001f));
            Assert.That(minimum, Is.EqualTo(1f));
            Assert.That(maximum, Is.EqualTo(7f));
        }

        [Test]
        public void FlattenTimeMajor_AllMasked_UsesStableAmplitudeFallback()
        {
            ProjectionBufferBuilder.FlattenTimeMajor(new[] { new[] { 0f, 0f } }, new[] { true }, 2, out RunningStatistics statistics, out float minimum, out float maximum);

            Assert.That(statistics.Count, Is.Zero);
            Assert.That(minimum, Is.EqualTo(-1f));
            Assert.That(maximum, Is.EqualTo(1f));
        }

        [Test]
        public void FlattenTimeMajor_RejectsMaskCountMismatch()
        {
            Assert.Throws<ArgumentException>(() => ProjectionBufferBuilder.FlattenTimeMajor(new[] { new[] { 1f } }, Array.Empty<bool>(), 1, out _, out _, out _));
        }

        [Test]
        public void FlattenTimeMajor_RejectsSeriesLengthMismatch()
        {
            Assert.Throws<ArgumentException>(() => ProjectionBufferBuilder.FlattenTimeMajor(new[] { new[] { 1f } }, new[] { false }, 2, out _, out _, out _));
        }
    }
}
