using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using Newtonsoft.Json;
using NUnit.Framework;
using ProcessedIEEGData = HBP.Core.Data.Processed.IEEGData;

namespace HBP.Tests.Serialization
{
    public class Stage4TemporalGridTests
    {
        [TestCase(64f, 97)]
        [TestCase(2048f, 3073)]
        public void Inclusive1500MillisecondTimeline_HasExpectedPositionCount(float frequency, int expectedLength)
        {
            Timeline timeline = CreateTimeline(new TimeWindow(0, 1500), frequency, out _, out _);
            Assert.That(timeline.Length, Is.EqualTo(expectedLength));
        }

        [Test]
        public void ExactLowFrequencySample_MapsWithoutInterpolation()
        {
            Timeline navigation = CreateTimeline(new TimeWindow(0, 1500), 2048, out Bloc bloc, out SubBloc subBloc);
            Timeline projection = CreateTimeline(bloc, subBloc, 64);

            TemporalSample sample = navigation.GetProjectionSample(projection, 32, TemporalSamplingPolicy.Interpolate);

            Assert.That(sample.Index, Is.EqualTo(1));
            Assert.That(sample.Alpha, Is.EqualTo(0f).Within(0.000001f));
        }

        [TestCase(TemporalSamplingPolicy.Floor, 0, 0f)]
        [TestCase(TemporalSamplingPolicy.Round, 1, 0f)]
        [TestCase(TemporalSamplingPolicy.Interpolate, 0, 0.75f)]
        public void FractionalSample_UsesRequestedPolicy(TemporalSamplingPolicy policy, int expectedIndex, float expectedAlpha)
        {
            Timeline navigation = CreateTimeline(new TimeWindow(0, 1500), 2048, out Bloc bloc, out SubBloc subBloc);
            Timeline projection = CreateTimeline(bloc, subBloc, 64);

            TemporalSample sample = navigation.GetProjectionSample(projection, 24, policy);

            Assert.That(sample.Index, Is.EqualTo(expectedIndex));
            Assert.That(sample.Alpha, Is.EqualTo(expectedAlpha).Within(0.000001f));
        }

        [Test]
        public void InterpolatedSample_EvaluatesTheTwoNativeNeighbors()
        {
            TemporalSample sample = new(0, 0.75f);
            Assert.That(sample.Evaluate(new[] { 0f, 10f }), Is.EqualTo(7.5f).Within(0.000001f));
        }

        [Test]
        public void ProjectionMapping_ClampsBothWindowBoundaries()
        {
            Timeline navigation = CreateTimeline(new TimeWindow(0, 1500), 2048, out Bloc bloc, out SubBloc subBloc);
            Timeline projection = CreateTimeline(bloc, subBloc, 64);

            TemporalSample first = navigation.GetProjectionSample(projection, 0, TemporalSamplingPolicy.Interpolate);
            TemporalSample last = navigation.GetProjectionSample(projection, navigation.Length - 1, TemporalSamplingPolicy.Interpolate);

            Assert.That(first.Index, Is.Zero);
            Assert.That(first.Alpha, Is.Zero);
            Assert.That(last.Index, Is.EqualTo(96));
            Assert.That(last.Alpha, Is.Zero);
        }

        [Test]
        public void NegativeWindowAndShiftedOrigin_MapInsideNativeGrid()
        {
            Timeline navigation = CreateTimeline(new TimeWindow(-100, 100), 2048, out Bloc bloc, out SubBloc subBloc);
            Timeline projection = CreateTimeline(bloc, subBloc, 64);

            TemporalSample first = navigation.GetProjectionSample(projection, 0, TemporalSamplingPolicy.Interpolate);
            TemporalSample main = navigation.GetProjectionSample(projection, navigation.SubTimelinesBySubBloc[subBloc].GlobalMinIndex + 204, TemporalSamplingPolicy.Interpolate);

            Assert.That(first.Index, Is.Zero);
            Assert.That(main.Index, Is.EqualTo(6));
            Assert.That(main.Alpha, Is.EqualTo(0f).Within(0.000001f));
        }

        [Test]
        public void NonCommensurableFrequencies_MapByPhysicalTime()
        {
            Timeline navigation = CreateTimeline(new TimeWindow(0, 1500), 1000, out Bloc bloc, out SubBloc subBloc);
            Timeline projection = CreateTimeline(bloc, subBloc, 333);

            TemporalSample sample = navigation.GetProjectionSample(projection, 501, TemporalSamplingPolicy.Interpolate);

            Assert.That(sample.Index, Is.EqualTo(166));
            Assert.That(sample.Alpha, Is.EqualTo(0.833f).Within(0.002f));
        }

        [Test]
        public void AlignedShortSubBloc_UsesTheLongerSegmentTiming()
        {
            Timeline shortTimeline = CreateAlignedShortTimeline(64);
            Assert.That(shortTimeline.Length, Is.EqualTo(97));
            Assert.That(shortTimeline.SubTimelinesBySubBloc[shortTimeline.CurrentSubtimeline == null ? null : FindCurrentSubBloc(shortTimeline)].After, Is.EqualTo(32));
        }

        [Test]
        public void SeparateLowFrequencyColumn_KeepsCommonNavigationButNativeProjectionGrid()
        {
            Bloc bloc = CreateBloc(new TimeWindow(0, 1500), out SubBloc subBloc);
            ProcessedIEEGData data = new();
            try
            {
                GetFrequencies(data).Add(new Frequency(64));
                const string channel = "patient_A1";
                float[] nativeValues = new float[97];
                for (int i = 0; i < nativeValues.Length; ++i)
                    nativeValues[i] = i;
                BlocChannelStatistics statistics = (BlocChannelStatistics)FormatterServices.GetUninitializedObject(typeof(BlocChannelStatistics));
                statistics.Trial = new ChannelTrialStat(new Dictionary<SubBloc, ChannelSubTrialStat>
                {
                    { subBloc, new ChannelSubTrialStat(nativeValues, new float[nativeValues.Length]) }
                }, 1, 1);
                data.DataByChannelID.Add(channel, null);
                data.StatisticsByChannelID.Add(channel, statistics);
                GetFrequencyByChannel(data).Add(channel, new Frequency(64));

                data.SetTimeline(new Frequency(2048), bloc, new[] { bloc });

                Assert.That(data.Timeline.Length, Is.EqualTo(3073));
                Assert.That(data.ProjectionTimeline.Length, Is.EqualTo(97));
                Assert.That(data.ProcessedValuesByChannel[channel], Has.Length.EqualTo(97));
                Assert.That(data.ProcessedValuesByChannel[channel], Is.EqualTo(nativeValues).Within(0.000001f));
            }
            finally
            {
                data.Unload();
            }
        }

        [Test]
        public void MixedColumn_UsesItsHighestNativeFrequencyForProjection()
        {
            Bloc bloc = CreateBloc(new TimeWindow(0, 1500), out _);
            ProcessedIEEGData data = new();
            try
            {
                GetFrequencies(data).Add(new Frequency(64));
                GetFrequencies(data).Add(new Frequency(2048));

                data.SetTimeline(new Frequency(2048), bloc, new[] { bloc });

                Assert.That(data.ProjectionTimeline.Length, Is.EqualTo(3073));
            }
            finally
            {
                data.Unload();
            }
        }

        [Test]
        public void TemporalPolicy_IsSerializedClonedAndDefaultsToInterpolationInUserPreferences()
        {
            EEGPreferences preferences = new(AveragingType.Mean, NormalizationType.None, 0.05f, true, TemporalSamplingPolicy.Round);
            string json = JsonConvert.SerializeObject(preferences);
            EEGPreferences restored = JsonConvert.DeserializeObject<EEGPreferences>(json);
            EEGPreferences clone = (EEGPreferences)preferences.Clone();
            EEGPreferences defaults = JsonConvert.DeserializeObject<EEGPreferences>("{}");

            Assert.That(restored.TemporalSampling, Is.EqualTo(TemporalSamplingPolicy.Round));
            Assert.That(clone.TemporalSampling, Is.EqualTo(TemporalSamplingPolicy.Round));
            Assert.That(defaults.TemporalSampling, Is.EqualTo(TemporalSamplingPolicy.Interpolate));
            Assert.That(JsonConvert.SerializeObject(new DynamicConfiguration()), Does.Not.Contain("Temporal Sampling"));
        }

        private static Timeline CreateTimeline(TimeWindow window, float frequency, out Bloc bloc, out SubBloc subBloc)
        {
            bloc = CreateBloc(window, out subBloc);
            return CreateTimeline(bloc, subBloc, frequency);
        }

        private static Timeline CreateTimeline(Bloc bloc, SubBloc subBloc, float frequency)
        {
            Dictionary<SubBloc, List<SubBlocEventsStatistics>> statistics = new()
            {
                { subBloc, new List<SubBlocEventsStatistics> { CreateEventStatistics(subBloc) } }
            };
            return new Timeline(bloc, statistics, new Dictionary<SubBloc, int> { { subBloc, 0 } }, new Frequency(frequency));
        }

        private static Timeline CreateAlignedShortTimeline(float frequency)
        {
            Bloc shortBloc = CreateBloc(new TimeWindow(0, 1000), out SubBloc shortSubBloc);
            CreateBloc(new TimeWindow(0, 1500), out SubBloc longSubBloc);
            Dictionary<SubBloc, List<SubBlocEventsStatistics>> statistics = new()
            {
                { shortSubBloc, new List<SubBlocEventsStatistics> { CreateEventStatistics(shortSubBloc) } }
            };
            Dictionary<SubBloc, int> alignment = new() { { shortSubBloc, 0 }, { longSubBloc, 0 } };
            return new Timeline(shortBloc, statistics, alignment, new Frequency(frequency));
        }

        private static SubBloc FindCurrentSubBloc(Timeline timeline)
        {
            foreach (KeyValuePair<SubBloc, SubTimeline> pair in timeline.SubTimelinesBySubBloc)
                return pair.Key;
            return null;
        }

        private static Bloc CreateBloc(TimeWindow window, out SubBloc subBloc)
        {
            Event mainEvent = new("main", new[] { 1 }, MainSecondaryEnum.Main, Guid.NewGuid().ToString("N"));
            subBloc = new SubBloc("main", 0, MainSecondaryEnum.Main, window, window, new[] { mainEvent }, Array.Empty<Icon>(), Array.Empty<Treatment>(), Guid.NewGuid().ToString("N"));
            Bloc bloc = (Bloc)FormatterServices.GetUninitializedObject(typeof(Bloc));
            bloc.ID = Guid.NewGuid().ToString("N");
            bloc.Name = "bloc";
            bloc.Order = 0;
            bloc.Sort = string.Empty;
            bloc.SubBlocs = new List<SubBloc> { subBloc };
            return bloc;
        }

        private static SubBlocEventsStatistics CreateEventStatistics(SubBloc subBloc)
        {
            float timeFromStart = -subBloc.Window.Start;
            EventInformation information = new(new[]
            {
                new EventInformation.EventOccurence(1, 0, 0, 0, 0f, timeFromStart, 0f)
            });
            return new SubBlocEventsStatistics(new Dictionary<Event, EventInformation[]> { { subBloc.MainEvent, new[] { information } } }, AveragingType.Mean);
        }

        private static List<Frequency> GetFrequencies(ProcessedIEEGData data)
        {
            FieldInfo field = typeof(ProcessedIEEGData).GetField("m_Frequencies", BindingFlags.Instance | BindingFlags.NonPublic);
            return (List<Frequency>)field.GetValue(data);
        }

        private static Dictionary<string, Frequency> GetFrequencyByChannel(ProcessedIEEGData data)
        {
            FieldInfo field = typeof(ProcessedIEEGData).GetField("m_FrequencyByChannelID", BindingFlags.Instance | BindingFlags.NonPublic);
            return (Dictionary<string, Frequency>)field.GetValue(data);
        }
    }
}
