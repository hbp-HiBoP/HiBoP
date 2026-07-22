using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class EpochIndexTests
    {
        private TempDirectoryScope m_Temp;
        private ApplicationStateTestScope m_ApplicationState;
        private PersistentDataTestScope m_PersistentData;

        [SetUp]
        public void SetUp()
        {
            m_Temp = new TempDirectoryScope();
            m_ApplicationState = new ApplicationStateTestScope(m_Temp.Path);
            m_PersistentData = new PersistentDataTestScope(m_Temp.Path);
        }

        [TearDown]
        public void TearDown()
        {
            m_PersistentData.Dispose();
            m_ApplicationState.Dispose();
            m_Temp.Dispose();
        }

        [TestCase(0, 2, -1, 0, 4, 6, 3, 4)]
        [TestCase(-2, 0, 1, 2, 2, 4, 5, 6)]
        [TestCase(-2, 2, -4, -3, 2, 6, 0, 1)]
        public void WindowAndBaselineViews_PreserveInclusiveBounds(
            int windowStart,
            int windowEnd,
            int baselineStart,
            int baselineEnd,
            int expectedWindowStart,
            int expectedWindowEnd,
            int expectedBaselineStart,
            int expectedBaselineEnd)
        {
            SubBloc subBloc = CreateSubBloc(
                "main",
                0,
                MainSecondaryEnum.Main,
                new TimeWindow(windowStart, windowEnd),
                new TimeWindow(baselineStart, baselineEnd),
                new Event("main", new[] { 1 }, MainSecondaryEnum.Main, "main-event"));
            BlocData blocData = CreateBlocData(new[] { subBloc }, (1, 4));
            SubTrial subTrial = blocData.Trials.Single().SubTrialBySubBloc[subBloc];

            Assert.That(subTrial.Descriptor.Window.StartIndex, Is.EqualTo(expectedWindowStart));
            Assert.That(subTrial.Descriptor.Window.EndIndex, Is.EqualTo(expectedWindowEnd));
            Assert.That(subTrial.Descriptor.Baseline.StartIndex, Is.EqualTo(expectedBaselineStart));
            Assert.That(subTrial.Descriptor.Baseline.EndIndex, Is.EqualTo(expectedBaselineEnd));
            Assert.That(subTrial.GetWindow("A1").ToArray(), Is.EqualTo(InclusiveValues(expectedWindowStart, expectedWindowEnd)));
            Assert.That(subTrial.GetBaseline("A1").ToArray(), Is.EqualTo(InclusiveValues(expectedBaselineStart, expectedBaselineEnd)));
        }

        [Test]
        public void ViewsUseCompactEpochCopy_AndDescriptorIsSharedAcrossChannels()
        {
            Event mainEvent = new("main", new[] { 1 }, MainSecondaryEnum.Main, "main-event");
            SubBloc subBloc = CreateSubBloc("main", 0, MainSecondaryEnum.Main, new TimeWindow(-1, 1), new TimeWindow(-1, 0), mainEvent);
            TestDynamicData recording = new(
                new Dictionary<string, float[]>
                {
                    { "A1", InclusiveValues(0, 9) },
                    { "A2", InclusiveValues(10, 19) }
                },
                (1, 4));
            BlocData blocData = new(recording, new Bloc("bloc", 0, string.Empty, string.Empty, new[] { subBloc }, "bloc"));
            SubTrial subTrial = blocData.Trials.Single().SubTrialBySubBloc[subBloc];

            recording.ValuesByChannel["A1"][4] = 123f;

            Assert.That(subTrial.GetWindow("A1")[1], Is.EqualTo(4f));
            Assert.That(subTrial.GetWindow("A2").Count, Is.EqualTo(subTrial.Descriptor.Window.Length));
            Assert.That(typeof(SubTrial).GetProperty("RawValuesByChannel"), Is.Null);
            Assert.That(typeof(SubTrial).GetProperty("BaselineValuesByChannel"), Is.Null);
        }

        [Test]
        public void TreatmentsUseReusableMaterialization_WithoutChangingCanonicalViews()
        {
            Event mainEvent = new("main", new[] { 1 }, MainSecondaryEnum.Main, "main-event");
            TimeWindow window = new(-1, 1);
            TimeWindow baseline = new(-2, -1);
            FactorTreatment treatment = new()
            {
                UseOnWindow = true,
                Window = window,
                UseOnBaseline = true,
                Baseline = baseline,
                Factor = 2f
            };
            SubBloc subBloc = new(
                "main",
                0,
                MainSecondaryEnum.Main,
                window,
                baseline,
                new[] { mainEvent },
                Array.Empty<Icon>(),
                new Treatment[] { treatment },
                "main-subbloc");
            BlocData blocData = CreateBlocData(new[] { subBloc }, (1, 4));
            SubTrial subTrial = blocData.Trials.Single().SubTrialBySubBloc[subBloc];
            EpochCompatibilityBuffer compatibilityBuffer = new();

            subTrial.GetBaselineStatistics("A1", compatibilityBuffer, out float baselineAverage, out _);
            subTrial.Normalize(0f, 1f, compatibilityBuffer);

            Assert.That(subTrial.GetWindow("A1").ToArray(), Is.EqualTo(new[] { 3f, 4f, 5f }));
            Assert.That(baselineAverage, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(subTrial.ValuesByChannel["A1"], Is.EqualTo(new[] { 6f, 8f, 10f }).Within(0.0001f));
        }

        [Test]
        public void EventsOnBothWindowBounds_AreIndexedInclusively()
        {
            Event mainEvent = new("main", new[] { 1 }, MainSecondaryEnum.Main, "main-event");
            Event secondaryEvent = new("secondary", new[] { 2 }, MainSecondaryEnum.Secondary, "secondary-event");
            SubBloc subBloc = CreateSubBloc(
                "main",
                0,
                MainSecondaryEnum.Main,
                new TimeWindow(-2, 2),
                new TimeWindow(-1, 0),
                mainEvent,
                secondaryEvent);
            BlocData blocData = CreateBlocData(new[] { subBloc }, (1, 4), (2, 2), (2, 6), (2, 7));
            SubTrial subTrial = blocData.Trials.Single().SubTrialBySubBloc[subBloc];

            EventInformation.EventOccurence[] occurrences = subTrial.Descriptor.InformationsByEvent[secondaryEvent].Occurences;

            Assert.That(occurrences.Select(occurrence => occurrence.Index), Is.EqualTo(new[] { 2, 6 }));
            Assert.That(occurrences.Select(occurrence => occurrence.IndexFromStart), Is.EqualTo(new[] { 0, 4 }));
        }

        [Test]
        public void SuccessiveSubBlocs_AreIndexedImmediatelyInProtocolOrder()
        {
            SubBloc before = CreateSubBloc("before", 0, MainSecondaryEnum.Secondary, new TimeWindow(-1, 0), new TimeWindow(-1, 0), new Event("before", new[] { 2 }, MainSecondaryEnum.Main, "before-event"));
            SubBloc main = CreateSubBloc("main", 1, MainSecondaryEnum.Main, new TimeWindow(-2, 2), new TimeWindow(-1, 0), new Event("main", new[] { 1 }, MainSecondaryEnum.Main, "main-event"));
            SubBloc after = CreateSubBloc("after", 2, MainSecondaryEnum.Secondary, new TimeWindow(0, 3), new TimeWindow(0, 1), new Event("after", new[] { 3 }, MainSecondaryEnum.Main, "after-event"));
            BlocData blocData = CreateBlocData(new[] { main, after, before }, (1, 10), (2, 5), (3, 15));
            Trial trial = blocData.Trials.Single();

            Assert.That(trial.SubTrialBySubBloc[before].Descriptor.SubTrialIndex, Is.EqualTo(0));
            Assert.That(trial.SubTrialBySubBloc[main].Descriptor.SubTrialIndex, Is.EqualTo(1));
            Assert.That(trial.SubTrialBySubBloc[after].Descriptor.SubTrialIndex, Is.EqualTo(2));
            Assert.That(trial.SubTrialBySubBloc[before].GetWindow("A1").ToArray(), Is.EqualTo(new[] { 4f, 5f }));
            Assert.That(trial.SubTrialBySubBloc[main].GetWindow("A1").ToArray(), Is.EqualTo(new[] { 8f, 9f, 10f, 11f, 12f }));
            Assert.That(trial.SubTrialBySubBloc[after].GetWindow("A1").ToArray(), Is.EqualTo(new[] { 15f, 16f, 17f, 18f }));
        }

        [Test]
        public void MultiBlocProtocol_IndexesBlocThatWasNeverDisplayed()
        {
            SubBloc firstSubBloc = CreateSubBloc("first", 0, MainSecondaryEnum.Main, new TimeWindow(0, 1), new TimeWindow(0, 0), new Event("first", new[] { 1 }, MainSecondaryEnum.Main, "first-event"));
            SubBloc secondSubBloc = CreateSubBloc("second", 0, MainSecondaryEnum.Main, new TimeWindow(-1, 1), new TimeWindow(-1, 0), new Event("second", new[] { 2 }, MainSecondaryEnum.Main, "second-event"));
            Bloc firstBloc = new("first", 0, string.Empty, string.Empty, new[] { firstSubBloc }, "first-bloc");
            Bloc secondBloc = new("second", 1, string.Empty, string.Empty, new[] { secondSubBloc }, "second-bloc");
            Protocol protocol = new("protocol", new[] { firstBloc, secondBloc }, "protocol");
            IEEGDataInfo dataInfo = new(
                "data",
                protocol,
                new HBP.Core.Data.Container.Elan(),
                Array.Empty<HBP.Core.Errors.Error>(),
                Array.Empty<HBP.Core.Errors.Warning>(),
                null,
                NormalizationType.None,
                string.Empty,
                "data");
            TestDynamicData recording = new(new Dictionary<string, float[]> { { "A1", InclusiveValues(0, 19) } }, (1, 3), (2, 12));

            IEEGData data = new(dataInfo, recording);
            SubTrial secondSubTrial = data.DataByBloc[secondBloc].Trials.Single().SubTrialBySubBloc[secondSubBloc];

            Assert.That(data.DataByBloc, Has.Count.EqualTo(2));
            Assert.That(secondSubTrial.GetWindow("A1").ToArray(), Is.EqualTo(new[] { 11f, 12f, 13f }));
            Assert.That(secondSubTrial.Descriptor.TrialIndex, Is.Zero);
        }

        private static BlocData CreateBlocData(IEnumerable<SubBloc> subBlocs, params (int code, int index)[] occurrences)
        {
            return new BlocData(
                new TestDynamicData(new Dictionary<string, float[]> { { "A1", InclusiveValues(0, 31) } }, occurrences),
                new Bloc("bloc", 0, string.Empty, string.Empty, subBlocs, "bloc"));
        }

        private static SubBloc CreateSubBloc(
            string name,
            int order,
            MainSecondaryEnum type,
            TimeWindow window,
            TimeWindow baseline,
            params Event[] events)
        {
            return new SubBloc(name, order, type, window, baseline, events, Array.Empty<Icon>(), Array.Empty<Treatment>(), $"{name}-subbloc");
        }

        private static float[] InclusiveValues(int start, int end)
        {
            return Enumerable.Range(start, end - start + 1).Select(value => (float)value).ToArray();
        }

        private sealed class TestDynamicData : DynamicData
        {
            public TestDynamicData(Dictionary<string, float[]> valuesByChannel, params (int code, int index)[] occurrences)
                : base(
                    valuesByChannel,
                    valuesByChannel.Keys.ToDictionary(channel => channel, _ => "uV"),
                    new Frequency(1000))
            {
                m_OccurencesByCode = occurrences
                    .GroupBy(occurrence => occurrence.code)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(occurrence => new EventOccurence(occurrence.code, occurrence.index, occurrence.index)).ToList());
            }
        }
    }
}
