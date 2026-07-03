using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using ProcessedIEEGData = HBP.Core.Data.Processed.IEEGData;

namespace HBP.Tests.Serialization
{
    public class DataLoadingProcessingCacheTests
    {
        private NormalizationType m_DefaultNormalization;
        private AveragingType m_DefaultAveraging;
        private AveragingType m_DefaultPositionAveraging;

        [SetUp]
        public void SetUp()
        {
            m_DefaultNormalization = DataManager.DefaultNormalization;
            m_DefaultAveraging = DataManager.DefaultAveraging;
            m_DefaultPositionAveraging = DataManager.DefaultPositionAveraging;
            DataManager.Clear();
            DataManager.DefaultNormalization = NormalizationType.None;
            DataManager.DefaultAveraging = AveragingType.Mean;
            DataManager.DefaultPositionAveraging = AveragingType.Mean;
        }

        [TearDown]
        public void TearDown()
        {
            DataManager.Clear();
            DataManager.DefaultNormalization = m_DefaultNormalization;
            DataManager.DefaultAveraging = m_DefaultAveraging;
            DataManager.DefaultPositionAveraging = m_DefaultPositionAveraging;
        }

        [Test]
        public void StaticData_LoadUnloadAndReload_UsesCacheLifecycle()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            StaticDataInfo dataInfo = CreateStaticDataInfo(temp);

            StaticData first = (StaticData)DataManager.GetData(dataInfo);
            StaticData second = (StaticData)DataManager.GetData(dataInfo);

            Assert.That(DataManager.HasData, Is.True);
            Assert.That(second, Is.SameAs(first));
            Assert.That(first.Labels, Is.EquivalentTo(new[] { "alpha", "beta" }));
            Assert.That(first.ValuesByChannel["A1"], Is.EqualTo(new[] { 1.5f, 2.5f }));

            DataManager.UnLoad(dataInfo);

            Assert.That(DataManager.HasData, Is.False);

            StaticData afterUnload = (StaticData)DataManager.GetData(dataInfo);
            Assert.That(afterUnload, Is.Not.SameAs(first));

            DataManager.Reload(dataInfo);
            StaticData afterReload = (StaticData)DataManager.GetData(dataInfo);

            Assert.That(afterReload, Is.Not.SameAs(afterUnload));
            Assert.That(afterReload.ValuesByChannel["B2"], Is.EqualTo(new[] { 3.5f, 4.5f }));
        }

        [Test]
        public void InvalidDataInfo_ReturnsNullAndDoesNotMutateCaches()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Protocol protocol = SyntheticProjectFactory.CreateProtocol();
            Patient patient = new("data-loading-cache-invalid-patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), "", "data-loading-cache-invalid-patient-001");
            StaticDataInfo invalidDataInfo = new(
                "data-loading-cache-invalid-static",
                protocol,
                new CSV("", Array.Empty<Error>(), Array.Empty<Warning>()),
                new Error[] { new RequiredFieldEmptyError("data-loading-cache invalid data") },
                Array.Empty<Warning>(),
                patient,
                "data-loading-cache-db",
                "data-loading-cache-invalid-data-001");

            Assert.That(DataManager.GetData(invalidDataInfo), Is.Null);
            Assert.That(DataManager.GetData(invalidDataInfo, protocol.Blocs[0], "A1"), Is.Null);
            Assert.That(DataManager.GetStatistics(invalidDataInfo, protocol.Blocs[0], "A1"), Is.Null);
            Assert.That(DataManager.HasData, Is.False);
        }

        [Test]
        public void ChannelAndEventStatistics_UseSyntheticTrialsAndClearCleanly()
        {
            EpochCacheFixture fixture = CreateInjectedEpochCache(NormalizationType.None);
            Bloc bloc = fixture.Blocs[0];
            SubBloc subBloc = bloc.MainSubBloc;
            Event mainEvent = subBloc.MainEvent;

            BlocChannelStatistics blocStatistics = DataManager.GetStatistics(fixture.DataInfo, bloc, fixture.Channel);
            BlocEventsStatistics eventStatistics = DataManager.GetEventsStatistics(fixture.DataInfo, bloc);
            ChannelStatistics channelStatistics = DataManager.GetStatistics(fixture.DataInfo, fixture.Channel);

            Assert.That(blocStatistics.Trial.TotalNumberOfTrials, Is.EqualTo(2));
            Assert.That(blocStatistics.Trial.NumberOfValidTrials, Is.EqualTo(2));
            Assert.That(blocStatistics.Trial.ChannelSubTrialBySubBloc[subBloc].Values, Is.EqualTo(new[] { 11f, 12f }));
            Assert.That(eventStatistics.EventsStatisticsBySubBloc[subBloc].StatisticsByEvent[mainEvent].NumberOfOccurences, Is.EqualTo(2));
            Assert.That(eventStatistics.EventsStatisticsBySubBloc[subBloc].StatisticsByEvent[mainEvent].TimeFromStart, Is.EqualTo(20f).Within(0.0001f));
            Assert.That(channelStatistics, Is.Not.Null);

            Assert.DoesNotThrow(() => DataManager.Clear());
            Assert.That(DataManager.HasData, Is.False);
        }

        [TestCase(NormalizationType.None)]
        [TestCase(NormalizationType.SubTrial)]
        [TestCase(NormalizationType.Trial)]
        [TestCase(NormalizationType.SubBloc)]
        [TestCase(NormalizationType.Bloc)]
        [TestCase(NormalizationType.Protocol)]
        public void NormalizeiEEGData_AppliesRequestedModeAndInvalidatesStatistics(NormalizationType normalization)
        {
            EpochCacheFixture fixture = CreateInjectedEpochCache(normalization, blocCount: 2);
            Bloc bloc = fixture.Blocs[0];
            BlocChannelStatistics beforeStatistics = DataManager.GetStatistics(fixture.DataInfo, bloc, fixture.Channel);
            SetNormalizationState(fixture.DataInfo, bloc, normalization == NormalizationType.None ? NormalizationType.SubTrial : NormalizationType.None);

            TryNormalizeOrIgnore();

            float[] values = fixture.FirstSubTrial.ValuesByChannel[fixture.Channel];
            float[] expected = normalization == NormalizationType.None ? new[] { 10f, 11f } : new[] { 0f, 1f };
            Assert.That(values, Is.EqualTo(expected).Within(0.0001f));
            Assert.That(GetNormalizationState(fixture.DataInfo, bloc), Is.EqualTo(normalization));
            Assert.That(DataManager.GetStatistics(fixture.DataInfo, bloc, fixture.Channel), Is.Not.SameAs(beforeStatistics));
        }

        [Test]
        public void NormalizeiEEGData_AutoUsesDefaultNormalization()
        {
            DataManager.DefaultNormalization = NormalizationType.SubTrial;
            EpochCacheFixture fixture = CreateInjectedEpochCache(NormalizationType.Auto);
            Bloc bloc = fixture.Blocs[0];

            TryNormalizeOrIgnore();

            Assert.That(fixture.FirstSubTrial.ValuesByChannel[fixture.Channel], Is.EqualTo(new[] { 0f, 1f }).Within(0.0001f));
            Assert.That(GetNormalizationState(fixture.DataInfo, bloc), Is.EqualTo(NormalizationType.SubTrial));
        }

        [Test]
        public void ConcurrentReads_ReturnSameCachedBlocChannelDataWithoutChangingValues()
        {
            EpochCacheFixture fixture = CreateInjectedEpochCache(NormalizationType.None);
            Bloc bloc = fixture.Blocs[0];
            ConcurrentBag<BlocChannelData> results = new();

            Parallel.For(0, 32, _ =>
            {
                results.Add(DataManager.GetData(fixture.DataInfo, bloc, fixture.Channel));
            });

            Assert.That(results, Has.Count.EqualTo(32));
            Assert.That(results.Distinct().Count(), Is.EqualTo(1));
            Assert.That(fixture.FirstSubTrial.ValuesByChannel[fixture.Channel], Is.EqualTo(new[] { 10f, 11f }));
        }

        [Test]
        public void ProcessedIEEGData_UnloadClearsDerivedCaches()
        {
            EpochCacheFixture fixture = CreateInjectedEpochCache(NormalizationType.None);
            Bloc bloc = fixture.Blocs[0];
            ProcessedIEEGData processed = new();

            processed.EventStatistics.Add(DataManager.GetEventsStatistics(fixture.DataInfo, bloc));
            processed.DataByChannelID.Add("patient_A1", DataManager.GetData(fixture.DataInfo, bloc, fixture.Channel));
            processed.StatisticsByChannelID.Add("patient_A1", DataManager.GetStatistics(fixture.DataInfo, bloc, fixture.Channel));
            processed.UnitByChannelID.Add("patient_A1", "uV");
            processed.ProcessedValuesByChannel.Add("patient_A1", new[] { 1f, 2f });

            processed.Unload();

            Assert.That(processed.EventStatistics, Is.Empty);
            Assert.That(processed.DataByChannelID, Is.Empty);
            Assert.That(processed.StatisticsByChannelID, Is.Empty);
            Assert.That(processed.UnitByChannelID, Is.Empty);
            Assert.That(processed.ProcessedValuesByChannel, Is.Empty);
            Assert.That(processed.Timeline, Is.Null);
            Assert.That(processed.IconicScenario, Is.Null);
        }

        private static StaticDataInfo CreateStaticDataInfo(TempDirectoryScope temp)
        {
            string csvPath = temp.GetPath("data-loading-cache-static.csv");
            File.WriteAllLines(csvPath, new[]
            {
                "channel,alpha,beta",
                "A1,1.5,2.5",
                "B2,3.5,4.5"
            });

            Protocol protocol = SyntheticProjectFactory.CreateProtocol();
            Patient patient = new("data-loading-cache-static-patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), "", "data-loading-cache-static-patient-001");
            return new StaticDataInfo(
                "data-loading-cache-static",
                protocol,
                new CSV(csvPath, Array.Empty<Error>(), Array.Empty<Warning>(), "data-loading-cache-static-container-001"),
                Array.Empty<Error>(),
                Array.Empty<Warning>(),
                patient,
                "data-loading-cache-db",
                "data-loading-cache-static-data-001");
        }

        private static EpochCacheFixture CreateInjectedEpochCache(NormalizationType normalization, int blocCount = 1)
        {
            Protocol protocol = CreateProtocol(blocCount);
            Patient patient = new("data-loading-cache-ieeg-patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), "", "data-loading-cache-ieeg-patient-001");
            IEEGDataInfo dataInfo = new(
                "data-loading-cache-ieeg",
                protocol,
                new Elan(),
                Array.Empty<Error>(),
                Array.Empty<Warning>(),
                patient,
                normalization,
                "data-loading-cache-db",
                "data-loading-cache-ieeg-data-001");
            Dictionary<Bloc, BlocData> blocDataByBloc = protocol.Blocs.ToDictionary(bloc => bloc, CreateBlocData);

            IEEGData data = (IEEGData)FormatterServices.GetUninitializedObject(typeof(IEEGData));
            data.DataByBloc = blocDataByBloc;
            data.UnitByChannel = new Dictionary<string, string> { { "A1", "uV" } };
            data.Frequency = new Frequency(1000);

            AddCacheEntry("m_DataByRequest", CreateRequest("Request", dataInfo), data);
            foreach (var pair in blocDataByBloc)
            {
                AddCacheEntry("m_BlocDataByRequest", CreateRequest("BlocRequest", dataInfo, pair.Key), pair.Value);
                AddCacheEntry("m_NormalizeByRequest", CreateRequest("BlocRequest", dataInfo, pair.Key), NormalizationType.None);
            }

            return new EpochCacheFixture
            {
                DataInfo = dataInfo,
                Blocs = protocol.Blocs.ToArray(),
                Data = data,
                BlocDataByBloc = blocDataByBloc,
                FirstSubTrial = blocDataByBloc[protocol.Blocs[0]].Trials[0].SubTrialBySubBloc[protocol.Blocs[0].MainSubBloc],
                Channel = "A1"
            };
        }

        private static Protocol CreateProtocol(int blocCount)
        {
            Bloc[] blocs = Enumerable.Range(0, blocCount).Select(index =>
            {
                Event mainEvent = new($"event-{index}", new[] { 10 + index }, MainSecondaryEnum.Main, $"data-loading-cache-event-{index:000}");
                SubBloc subBloc = new(
                    $"subbloc-{index}",
                    0,
                    MainSecondaryEnum.Main,
                    new TimeWindow(0, 1),
                    new TimeWindow(0, 1),
                    new[] { mainEvent },
                    Array.Empty<Icon>(),
                    Array.Empty<Treatment>(),
                    $"data-loading-cache-subbloc-{index:000}");
                Bloc bloc = (Bloc)FormatterServices.GetUninitializedObject(typeof(Bloc));
                bloc.ID = $"data-loading-cache-bloc-{index:000}";
                bloc.Name = $"bloc-{index}";
                bloc.Order = index;
                bloc.Sort = $"subbloc-{index}_event-{index}_CODE";
                bloc.SubBlocs = new List<SubBloc> { subBloc };
                return bloc;
            }).ToArray();

            return new Protocol("data-loading-cache-protocol", blocs, "data-loading-cache-protocol-001");
        }

        private static BlocData CreateBlocData(Bloc bloc)
        {
            BlocData blocData = (BlocData)FormatterServices.GetUninitializedObject(typeof(BlocData));
            SubBloc subBloc = bloc.MainSubBloc;
            blocData.Frequency = new Frequency(1000);
            blocData.Trials = new[]
            {
                new Trial(new Dictionary<SubBloc, SubTrial> { { subBloc, CreateSubTrial(subBloc, new[] { 10f, 11f }, 10f, 10f) } }),
                new Trial(new Dictionary<SubBloc, SubTrial> { { subBloc, CreateSubTrial(subBloc, new[] { 12f, 13f }, 10f, 30f) } })
            };
            return blocData;
        }

        private static SubTrial CreateSubTrial(SubBloc subBloc, float[] rawValues, float baselineValue, float eventTimeFromStart)
        {
            var rawByChannel = new Dictionary<string, float[]> { { "A1", rawValues.ToArray() } };
            var baselineByChannel = new Dictionary<string, float[]> { { "A1", new[] { baselineValue, baselineValue } } };
            var subTrial = new SubTrial(
                new Dictionary<Event, EventInformation>
                {
                    {
                        subBloc.MainEvent,
                        new EventInformation(new[]
                        {
                            new EventInformation.EventOccurence(subBloc.MainEvent.Codes[0], 0, 0, 0, eventTimeFromStart, eventTimeFromStart, 0)
                        })
                    }
                },
                new Dictionary<string, string> { { "A1", "uV" } },
                rawByChannel,
                baselineByChannel,
                true);
            subTrial.ValuesByChannel = rawByChannel.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            return subTrial;
        }

        private static void TryNormalizeOrIgnore()
        {
            try
            {
                DataManager.NormalizeiEEGData();
            }
            catch (DllNotFoundException exception)
            {
                Assert.Ignore($"Native math DLL unavailable for normalization path: {exception.Message}");
            }
            catch (EntryPointNotFoundException exception)
            {
                Assert.Ignore($"Native math DLL entry point unavailable for normalization path: {exception.Message}");
            }
        }

        private static void SetNormalizationState(DataInfo dataInfo, Bloc bloc, NormalizationType normalization)
        {
            IDictionary normalizeByRequest = GetCache("m_NormalizeByRequest");
            normalizeByRequest[CreateRequest("BlocRequest", dataInfo, bloc)] = normalization;
        }

        private static NormalizationType GetNormalizationState(DataInfo dataInfo, Bloc bloc)
        {
            IDictionary normalizeByRequest = GetCache("m_NormalizeByRequest");
            return (NormalizationType)normalizeByRequest[CreateRequest("BlocRequest", dataInfo, bloc)];
        }

        private static void AddCacheEntry(string fieldName, object key, object value)
        {
            GetCache(fieldName).Add(key, value);
        }

        private static IDictionary GetCache(string fieldName)
        {
            FieldInfo field = typeof(DataManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            return (IDictionary)field.GetValue(null);
        }

        private static object CreateRequest(string nestedTypeName, params object[] args)
        {
            Type requestType = typeof(DataManager).GetNestedType(nestedTypeName, BindingFlags.NonPublic);
            return Activator.CreateInstance(requestType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);
        }

        private sealed class EpochCacheFixture
        {
            public IEEGDataInfo DataInfo;
            public Bloc[] Blocs;
            public IEEGData Data;
            public Dictionary<Bloc, BlocData> BlocDataByBloc;
            public SubTrial FirstSubTrial;
            public string Channel;
        }
    }
}
