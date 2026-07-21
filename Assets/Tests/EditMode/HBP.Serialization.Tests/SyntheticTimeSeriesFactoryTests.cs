using System;
using System.Linq;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class SyntheticTimeSeriesFactoryTests
    {
        [Test]
        public void ValueAt_IsDeterministicBoundedAndTupleDependent()
        {
            float expected = SyntheticTimeSeriesFactory.ValueAt(2, 3, 4, 5);
            float[] values = Enumerable.Range(0, 1000)
                .Select(index => SyntheticTimeSeriesFactory.ValueAt(2, 3, 4, index))
                .ToArray();

            Assert.That(SyntheticTimeSeriesFactory.ValueAt(2, 3, 4, 5), Is.EqualTo(expected));
            Assert.That(expected, Is.InRange(SyntheticTimeSeriesFactory.MinimumValue, SyntheticTimeSeriesFactory.MaximumValue));
            Assert.That(values, Is.All.InRange(SyntheticTimeSeriesFactory.MinimumValue, SyntheticTimeSeriesFactory.MaximumValue));
            Assert.That(new[]
            {
                SyntheticTimeSeriesFactory.ValueAt(1, 3, 4, 5),
                SyntheticTimeSeriesFactory.ValueAt(2, 2, 4, 5),
                SyntheticTimeSeriesFactory.ValueAt(2, 3, 3, 5),
                SyntheticTimeSeriesFactory.ValueAt(2, 3, 4, 4)
            }, Has.None.EqualTo(expected));
        }

        [Test]
        public void CreateTrial_UsesInclusiveBoundsAndRejectsInvalidCoordinates()
        {
            SyntheticTimeSeriesDefinition definition = new(2, 3, 4, 20, 6, 2, 1000);

            float[] values = SyntheticTimeSeriesFactory.CreateTrial(definition, 1, 2, 3);

            Assert.That(values, Has.Length.EqualTo(6));
            Assert.That(values[0], Is.EqualTo(SyntheticTimeSeriesFactory.ValueAt(1, 2, 3, 0)));
            Assert.That(values[^1], Is.EqualTo(SyntheticTimeSeriesFactory.ValueAt(1, 2, 3, 5)));
            Assert.Throws<ArgumentOutOfRangeException>(() => SyntheticTimeSeriesFactory.CreateTrial(definition, 2, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => SyntheticTimeSeriesFactory.CreateTrial(definition, 0, 3, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => SyntheticTimeSeriesFactory.CreateTrial(definition, 0, 0, 4));
        }

        [TestCase(1500, 64, 97)]
        [TestCase(1500, 2048, 3073)]
        [TestCase(0, 2048, 1)]
        public void InclusiveSampleCount_IncludesBothWindowBounds(int durationMilliseconds, int frequencyHz, int expected)
        {
            Assert.That(
                SyntheticTimeSeriesFactory.InclusiveSampleCount(durationMilliseconds, frequencyHz),
                Is.EqualTo(expected));
        }

        [Test]
        public void ComputeChecksum_IsStableAcrossTenOpenCloseCycles()
        {
            SyntheticTimeSeriesDefinition definition = new(2, 3, 4, 20, 11, 3, 512);
            ulong[] checksums = new ulong[10];

            for (int repetition = 0; repetition < checksums.Length; ++repetition)
            {
                checksums[repetition] = SyntheticTimeSeriesFactory.ComputeChecksum(definition);
            }

            Assert.That(checksums.Distinct().Count(), Is.EqualTo(1));
            Assert.That(checksums[0], Is.EqualTo(0x5D23C01F03510B43UL));
        }

        [Test]
        public void MemoryLayers_DescribeCurrentRawEpochAndDerivedCopiesSeparately()
        {
            SyntheticTimeSeriesDefinition definition = new(2, 3, 4, 20, 6, 2, 1000);

            Assert.That(definition.ManagedRawSignalBytes, Is.EqualTo(2L * 3 * 20 * sizeof(float)));
            Assert.That(definition.ManagedEpochBytes, Is.EqualTo(2L * 3 * 4 * (6 + 2) * sizeof(float)));
            Assert.That(definition.ManagedDerivedBytes, Is.EqualTo(2L * 3 * 4 * 6 * sizeof(float)));
        }

        [Test]
        public void ProjectionProfiles_ContainProductAndControlledHighFrequencyReferences()
        {
            var product = NativeProjectionLoadBenchmarkScenarios.Build(
                "Product", 12, false, HBP.Core.Enums.VolumeInterpolation.Nearest, null);
            var extreme = NativeProjectionLoadBenchmarkScenarios.Build(
                "Extreme", 12, false, HBP.Core.Enums.VolumeInterpolation.Nearest, null);

            Assert.That(product, Has.Some.Matches<NativeProjectionLoadScenarioDefinition>(scenario =>
                scenario.SiteCount == 30000 && scenario.TimelineLength == 100));
            Assert.That(extreme, Has.Some.Matches<NativeProjectionLoadScenarioDefinition>(scenario =>
                scenario.SamplingFrequencyHz == 64 && scenario.TimelineLength == 97));
            Assert.That(extreme, Has.Some.Matches<NativeProjectionLoadScenarioDefinition>(scenario =>
                scenario.SamplingFrequencyHz == 2048 && scenario.TimelineLength == 3073));
        }
    }
}
