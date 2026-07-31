using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class RawRecordingCacheTests
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
            DataManager.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            DataManager.ResetRawRecordingLoader();
            DataManager.Clear();
            DataManager.ConfigureMemoryBudget(1024 * 1024, 0);
            m_PersistentData.Dispose();
            m_ApplicationState.Dispose();
            m_Temp.Dispose();
        }

        [Test]
        public void SameRecordingAcrossVisualizationsAndProtocolVersions_IsReadOnce()
        {
            int loadCount = 0;
            DataManager.RawRecordingLoader = _ =>
            {
                Interlocked.Increment(ref loadCount);
                return new StubDynamicData();
            };

            IEEGDataInfo firstInfo = CreateDataInfo("recording.edf", CreateProtocol("protocol-a"), "data-a");
            IEEGDataInfo secondInfo = CreateDataInfo("recording.edf", CreateProtocol("protocol-b"), "data-b");

            IEEGData first = (IEEGData)DataManager.GetData(firstInfo);
            IEEGData second = (IEEGData)DataManager.GetData(secondInfo);

            Assert.That(loadCount, Is.EqualTo(1));
            Assert.That(DataManager.RawRecordingCacheCount, Is.EqualTo(1));
            Assert.That(first.DataByBloc[firstInfo.Protocol.Blocs[0]].Trials[0].SubTrialBySubBloc[firstInfo.Protocol.Blocs[0].MainSubBloc].GetWindow("A1").ToArray(), Is.EqualTo(new[] { 12f, 13f }));
            Assert.That(second, Is.Not.SameAs(first));

            firstInfo.Protocol = CreateProtocol("protocol-c");
            DataManager.ClearDerivedData();
            IEEGData rebuilt = (IEEGData)DataManager.GetData(firstInfo);

            Assert.That(rebuilt, Is.Not.SameAs(first));
            Assert.That(loadCount, Is.EqualTo(1));
            Assert.That(DataManager.RawRecordingCacheCount, Is.EqualTo(1));
        }

        [Test]
        public void ChangedSource_LoadsASecondCanonicalRecording()
        {
            int loadCount = 0;
            DataManager.RawRecordingLoader = _ =>
            {
                Interlocked.Increment(ref loadCount);
                return new StubDynamicData();
            };
            IEEGDataInfo dataInfo = CreateDataInfo("recording-a.edf", CreateProtocol("protocol"), "data");

            DataManager.GetData(dataInfo);
            dataInfo.DataContainer = new EDF("recording-b.edf", Array.Empty<Error>(), Array.Empty<Warning>());
            DataManager.Reload(dataInfo);

            Assert.That(loadCount, Is.EqualTo(2));
            Assert.That(DataManager.RawRecordingCacheCount, Is.EqualTo(1));
        }

        [Test]
        public void CompactEpochs_DoNotDependOnRawArraysAfterConstruction()
        {
            StubDynamicData raw = new();
            DataManager.RawRecordingLoader = _ => raw;
            IEEGDataInfo dataInfo = CreateDataInfo("recording.edf", CreateProtocol("protocol"), "data");

            IEEGData data = (IEEGData)DataManager.GetData(dataInfo);
            SubTrial subTrial = data.DataByBloc[dataInfo.Protocol.Blocs[0]].Trials[0].SubTrialBySubBloc[dataInfo.Protocol.Blocs[0].MainSubBloc];
            raw.ValuesByChannel["A1"][2] = 999f;
            raw.ValuesByChannel["A1"][3] = 1000f;

            Assert.That(subTrial.GetWindow("A1").ToArray(), Is.EqualTo(new[] { 12f, 13f }));
            Assert.That(subTrial.ValuesByChannel["A1"], Is.EqualTo(new[] { 12f, 13f }));
        }

        [Test]
        public void EvictedRawRecording_IsReloadedWhenProtocolRequiresNewEpochs()
        {
            int loadCount = 0;
            DataManager.RawRecordingLoader = _ =>
            {
                Interlocked.Increment(ref loadCount);
                return new StubDynamicData();
            };
            IEEGDataInfo first = CreateDataInfo("recording-a.edf", CreateProtocol("protocol-a"), "data-a");
            IEEGDataInfo second = CreateDataInfo("recording-b.edf", CreateProtocol("protocol-b"), "data-b");

            DataManager.GetData(first);
            DataManager.GetData(second);
            Assert.That(loadCount, Is.EqualTo(2));
            Assert.That(DataManager.RawRecordingCacheCount, Is.EqualTo(1));

            first.Protocol = CreateProtocol("protocol-c");
            DataManager.ClearDerivedData();
            IEEGData rebuilt = (IEEGData)DataManager.GetData(first);

            Assert.That(loadCount, Is.EqualTo(3));
            Assert.That(rebuilt.DataByBloc[first.Protocol.Blocs[0]].Trials[0].SubTrialBySubBloc[first.Protocol.Blocs[0].MainSubBloc].GetWindow("A1").ToArray(), Is.EqualTo(new[] { 12f, 13f }));
        }

        [Test]
        public void NormalizationCompletion_ReleasesLastCompactRecordingRawCache()
        {
            int loadCount = 0;
            DataManager.RawRecordingLoader = _ =>
            {
                Interlocked.Increment(ref loadCount);
                return new StubDynamicData();
            };
            IEEGDataInfo dataInfo = CreateDataInfo("recording.edf", CreateProtocol("protocol"), "data");

            DataManager.GetData(dataInfo);
            Assert.That(DataManager.RawRecordingCacheCount, Is.EqualTo(1));

            DataManager.NormalizeiEEGData();
            Assert.That(DataManager.RawRecordingCacheCount, Is.Zero);

            DataManager.ClearDerivedData();
            DataManager.GetData(dataInfo);
            Assert.That(loadCount, Is.EqualTo(2));
        }

        [Test]
        public void ActiveDataPin_KeepsRawRecordingUntilVisualizationReleasesIt()
        {
            DataManager.RawRecordingLoader = _ => new LargeStubDynamicData();
            IEEGDataInfo dataInfo = CreateDataInfo("large-recording.edf", CreateProtocol("large-protocol"), "large-data");
            DataManager.ConfigureMemoryBudget(1, 0);

            DataManager.PinData(dataInfo);
            try
            {
                DataManager.GetData(dataInfo);

                Assert.That(DataManager.RawRecordingCacheCount, Is.EqualTo(1));
                Assert.That(DataManager.MemoryCacheSnapshot.IsOverBudget, Is.True);
            }
            finally
            {
                DataManager.UnpinData(dataInfo);
            }

            Assert.That(DataManager.RawRecordingCacheCount, Is.Zero);
        }

        [Test]
        public void ConcurrentRequestsForSameSource_RunOneLoader()
        {
            RawRecordingCache cache = new();
            RawRecordingSourceKey key = new("source");
            int loadCount = 0;
            ConcurrentBag<DynamicData> results = new();

            Parallel.For(0, 32, _ =>
            {
                results.Add(cache.GetOrLoad(key, () =>
                {
                    Interlocked.Increment(ref loadCount);
                    Thread.Sleep(25);
                    return new StubDynamicData();
                }));
            });

            Assert.That(loadCount, Is.EqualTo(1));
            Assert.That(results.Distinct().Count(), Is.EqualTo(1));
            Assert.That(cache.Count, Is.EqualTo(1));
        }

        [Test]
        public void FailedLoad_DoesNotPublishEntryAndCanBeRetried()
        {
            RawRecordingCache cache = new();
            RawRecordingSourceKey key = new("source");
            StubDynamicData expected = new();

            Assert.Throws<InvalidOperationException>(() => cache.GetOrLoad(key, () => throw new InvalidOperationException("read failed")));
            Assert.That(cache.Count, Is.Zero);

            Assert.That(cache.GetOrLoad(key, () => expected), Is.SameAs(expected));
            Assert.That(cache.Count, Is.EqualTo(1));
        }

        [Test]
        public void CancelledLoad_DoesNotPublishEntry()
        {
            RawRecordingCache cache = new();
            RawRecordingSourceKey key = new("source");

            Assert.Throws<OperationCanceledException>(() => cache.GetOrLoad(key, () => throw new OperationCanceledException()));

            Assert.That(cache.Count, Is.Zero);
        }

        private static IEEGDataInfo CreateDataInfo(string path, Protocol protocol, string id)
        {
            Patient patient = new($"patient-{id}", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), "", $"patient-{id}");
            return new IEEGDataInfo(id, protocol, new EDF(path, Array.Empty<Error>(), Array.Empty<Warning>()), Array.Empty<Error>(), Array.Empty<Warning>(), patient, NormalizationType.None, "database", id);
        }

        private static Protocol CreateProtocol(string id)
        {
            Event mainEvent = new("main", new[] { 1 }, MainSecondaryEnum.Main, $"event-{id}");
            SubBloc subBloc = new("main", 0, MainSecondaryEnum.Main, new TimeWindow(0, 1), new TimeWindow(0, 0), new[] { mainEvent }, Array.Empty<Icon>(), Array.Empty<Treatment>(), $"subbloc-{id}");
            Bloc bloc = new("bloc", 0, "", "", new[] { subBloc }, $"bloc-{id}");
            return new Protocol(id, new[] { bloc }, id);
        }

        private sealed class StubDynamicData : DynamicData
        {
            public StubDynamicData() : base(new Dictionary<string, float[]> { { "A1", new[] { 10f, 11f, 12f, 13f, 14f } } }, new Dictionary<string, string> { { "A1", "uV" } }, new Frequency(1000))
            {
                m_OccurencesByCode = new Dictionary<int, List<EventOccurence>>
                {
                    { 1, new List<EventOccurence> { new(1, 2, 2) } }
                };
            }
        }

        private sealed class LargeStubDynamicData : DynamicData
        {
            public LargeStubDynamicData() : base(new Dictionary<string, float[]> { { "A1", new float[400000] } }, new Dictionary<string, string> { { "A1", "uV" } }, new Frequency(1000))
            {
                m_OccurencesByCode = new Dictionary<int, List<EventOccurence>>
                {
                    { 1, new List<EventOccurence> { new(1, 2, 2) } }
                };
            }
        }
    }
}
