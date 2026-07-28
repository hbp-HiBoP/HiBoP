using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Core.Tools;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class Stage3StreamingStatisticsTests
    {
        private static readonly TimeWindow FullWindow = new(0, 3);
        private static readonly TimeWindow FullBaseline = new(0, 1);
        private static readonly Frequency Frequency = new(1000);

        [Test]
        public void RunningStatistics_MatchesNativeMeanStandardDeviationAndSem()
        {
            float[] values = { -3f, 1f, 2f, 7f, 9f };
            RunningStatistics statistics = new();
            statistics.Add(values);

            Assert.That(statistics.Count, Is.EqualTo(values.Length));
            Assert.That(statistics.Mean, Is.EqualTo(values.Mean()).Within(0.000001f));
            Assert.That(statistics.StandardDeviation, Is.EqualTo(values.StandardDeviation()).Within(0.000001f));
            Assert.That(statistics.StandardError, Is.EqualTo(values.SEM()).Within(0.000001f));
        }

        [TestCase(AveragingType.Mean)]
        [TestCase(AveragingType.Median)]
        public void StreamingCalculation_ReturnsAnalyticValuesAndSem(AveragingType averaging)
        {
            float[][] series =
            {
                new[] { 1f, 10f },
                new[] { 3f, 14f },
                new[] { 5f, 18f }
            };

            StreamingStatistics.Calculate(series, averaging, out float[] values, out float[] sem);

            Assert.That(values, Is.EqualTo(new[] { 3f, 14f }).Within(0.000001f));
            Assert.That(sem, Is.EqualTo(new[] { 1.1547005f, 2.309401f }).Within(0.000001f));
        }

        [Test]
        public void StreamingCalculation_RejectsMismatchedSeriesLengths()
        {
            float[][] series = { new[] { 1f }, new[] { 1f, 2f } };
            Assert.Throws<ArgumentException>(() => StreamingStatistics.Calculate(series, AveragingType.Mean, out _, out _));
        }

        [Test]
        public void MedianSelection_IsBitwiseEquivalentToSortedMedian()
        {
            Random random = new(73021);
            for (int count = 1; count <= 64; ++count)
            {
                for (int iteration = 0; iteration < 100; ++iteration)
                {
                    float[] input = Enumerable.Range(0, count).Select(_ => (float)(random.NextDouble() * 2000d - 1000d)).ToArray();
                    if (iteration % 10 == 0)
                        input[random.Next(count)] = float.NaN;
                    if (iteration % 13 == 0)
                        input[random.Next(count)] = float.PositiveInfinity;

                    float[] sorted = (float[])input.Clone();
                    Array.Sort(sorted);
                    int middle = count / 2;
                    float expected = count % 2 == 0 ? (sorted[middle - 1] + sorted[middle]) * 0.5f : sorted[middle];

                    float actual = StreamingStatistics.Median((float[])input.Clone(), count);
                    Assert.That(BitConverter.SingleToInt32Bits(actual), Is.EqualTo(BitConverter.SingleToInt32Bits(expected)), $"count={count}, iteration={iteration}");
                }
            }
        }

        [Test]
        public void ChannelSubTrialStat_UsesOnlyValidSeriesAndTracksFoundCounts()
        {
            ChannelSubTrial[] subTrials =
            {
                CreateChannelSubTrial(new[] { 1f, 10f }, true),
                CreateChannelSubTrial(new[] { 100f, 100f }, false),
                CreateChannelSubTrial(new[] { 5f, 18f }, true)
            };

            ChannelSubTrialStat statistics = new(subTrials, new[] { true, false, true }, AveragingType.Mean);

            Assert.That(statistics.TotalNumberOfSubTrials, Is.EqualTo(3));
            Assert.That(statistics.NumberOfFoundSubTrials, Is.EqualTo(2));
            Assert.That(statistics.Values, Is.EqualTo(new[] { 3f, 14f }).Within(0.000001f));
            Assert.That(statistics.SEM, Is.EqualTo(new[] { 2f, 4f }).Within(0.000001f));
        }

        [Test]
        public void EventStatistics_MeanMatchesHistoricIntegerOccurrenceAverage()
        {
            EventInformation[] information =
            {
                CreateEventInformation(10f),
                CreateEventInformation(20f, 30f)
            };

            EventStatistics statistics = new(information, AveragingType.Mean);

            Assert.That(statistics.NumberOfOccurences, Is.EqualTo(3));
            Assert.That(statistics.TimeFromStart, Is.EqualTo(20f).Within(0.000001f));
            Assert.That(statistics.NumberOfOccurenceBySubTrial, Is.EqualTo(1f));
            Assert.That(statistics.RoundedTimeFromStart, Is.EqualTo(20));
        }

        [Test]
        public void EventStatistics_MedianUsesOneLogicalBufferForTimesAndCounts()
        {
            EventInformation[] information =
            {
                CreateEventInformation(3f),
                CreateEventInformation(9f, 21f),
                CreateEventInformation(15f, 27f, 33f)
            };

            EventStatistics statistics = new(information, AveragingType.Median);

            Assert.That(statistics.NumberOfOccurences, Is.EqualTo(6));
            Assert.That(statistics.TimeFromStart, Is.EqualTo(18f).Within(0.000001f));
            Assert.That(statistics.NumberOfOccurenceBySubTrial, Is.EqualTo(2f));
        }

        [Test]
        public void Treatments_AreClassifiedByExecutionMemoryShape()
        {
            Treatment[] pointwise =
            {
                new AbsTreatment(), new ClampTreatment(), new FactorTreatment(),
                new OffsetTreatment(), new RescaleTreatment(), new ThresholdTreatment()
            };
            Treatment[] scalar = { new MeanTreatment(), new MinTreatment(), new MaxTreatment() };

            Assert.That(pointwise.Select(treatment => treatment.ExecutionKind), Is.All.EqualTo(TreatmentExecutionKind.Pointwise));
            Assert.That(scalar.Select(treatment => treatment.ExecutionKind), Is.All.EqualTo(TreatmentExecutionKind.Scalar));
            Assert.That(new MedianTreatment().ExecutionKind, Is.EqualTo(TreatmentExecutionKind.Buffer));
        }

        [Test]
        public void EveryTreatment_AppliedIndividually_ProducesExpectedWindow()
        {
            AssertTreatment(new AbsTreatment(), new[] { 2f, 1f, 1f, 4f });
            AssertTreatment(new ClampTreatment(true, FullWindow, false, FullBaseline, true, -1f, true, 2f, 0), new[] { -1f, -1f, 1f, 2f });
            AssertTreatment(new FactorTreatment(true, FullWindow, false, FullBaseline, 2f, 0, "factor"), new[] { -4f, -2f, 2f, 8f });
            AssertTreatment(new OffsetTreatment(true, FullWindow, false, FullBaseline, 3f, 0, "offset"), new[] { 1f, 2f, 4f, 7f });
            AssertTreatment(new RescaleTreatment(true, FullWindow, false, FullBaseline, -2f, 4f, 0f, 6f, 0, "rescale"), new[] { 0f, 1f, 3f, 6f });
            AssertTreatment(new ThresholdTreatment(true, FullWindow, false, FullBaseline, true, -1f, true, 2f, 0, "threshold"), new[] { -2f, -1f, 0f, 4f });
            AssertTreatment(new MinTreatment(true, FullWindow, false, FullBaseline, 0, "min"), new[] { -2f, -2f, -2f, -2f });
            AssertTreatment(new MaxTreatment(true, FullWindow, false, FullBaseline, 0, "max"), new[] { 4f, 4f, 4f, 4f });
            AssertTreatment(new MeanTreatment(true, FullWindow, false, FullBaseline, 0, "mean"), new[] { 0.5f, 0.5f, 0.5f, 0.5f });
            AssertTreatment(new MedianTreatment(true, FullWindow, false, FullBaseline, 0, "median"), new[] { 0f, 0f, 0f, 0f });
        }

        [TestCase(true, 1.6666666f)]
        [TestCase(false, 1.5f)]
        public void AggregateTreatments_CombineWindowAndBaselineWithoutTemporarySubarrays(bool useMean, float expected)
        {
            Treatment treatment = useMean ? new MeanTreatment(true, FullWindow, true, FullBaseline, 0, "mean") : new MedianTreatment(true, FullWindow, true, FullBaseline, 0, "median");
            float[] values = { -2f, -1f, 1f, 4f };
            float[] baseline = { 2f, 6f };

            treatment.Apply(ref values, ref baseline, 0, 0, Frequency, new float[values.Length + baseline.Length]);

            Assert.That(values, Is.All.EqualTo(expected).Within(0.000001f));
            Assert.That(baseline, Is.All.EqualTo(expected).Within(0.000001f));
        }

        [Test]
        public void OrderedTreatmentPipeline_HonorsOrderAndReusesWorkspace()
        {
            Treatment[] treatments =
            {
                new MeanTreatment(true, FullWindow, false, FullBaseline, 2, "mean"),
                new OffsetTreatment(true, FullWindow, false, FullBaseline, 1f, 1, "offset"),
                new FactorTreatment(true, FullWindow, false, FullBaseline, 2f, 0, "factor")
            };
            float[] values = { -2f, -1f, 1f, 4f };
            float[] baseline = { 2f, 6f };
            float[] workspace = new float[6];

            foreach (Treatment treatment in treatments.OrderBy(treatment => treatment.Order))
                treatment.Apply(ref values, ref baseline, 0, 0, Frequency, workspace);

            Assert.That(values, Is.All.EqualTo(2f).Within(0.000001f));
            Assert.That(baseline, Is.EqualTo(new[] { 2f, 6f }));
        }

        private static void AssertTreatment(Treatment treatment, float[] expected)
        {
            treatment.UseOnWindow = true;
            treatment.Window = FullWindow;
            treatment.UseOnBaseline = false;
            treatment.Baseline = FullBaseline;
            float[] values = { -2f, -1f, 1f, 4f };
            float[] baseline = { 2f, 6f };
            treatment.Apply(ref values, ref baseline, 0, 0, Frequency, new float[6]);
            Assert.That(values, Is.EqualTo(expected).Within(0.000001f), treatment.GetType().Name);
        }

        private static ChannelSubTrial CreateChannelSubTrial(float[] values, bool found)
        {
            return new ChannelSubTrial(values, "uV", found, new Dictionary<Event, EventInformation>());
        }

        private static EventInformation CreateEventInformation(params float[] times)
        {
            EventInformation.EventOccurence[] occurrences = times.Select((time, index) => new EventInformation.EventOccurence(1, index, index, index, time, time, time)).ToArray();
            return new EventInformation(occurrences);
        }
    }
}
