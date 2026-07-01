using System;
using System.IO;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.Tests.PlayMode.Utilities;
using NUnit.Framework;

namespace HBP.Tests.PlayMode.Workflow
{
    public class Phase6DataManagerPlayModeTests
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
        [Category("PlayMode.Phase6")]
        public async Task StaticCsvData_InPlayMode_ReusesCacheThenReloadsAfterUnload()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("Phase6DataManagerCacheLifecycle");
            await Task.Yield();

            StaticDataInfo dataInfo = CreateStaticDataInfo(temp, "lifecycle");

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
            Assert.That(scene.Scene.isLoaded, Is.True);
        }

        [Test]
        [Category("PlayMode.Phase6")]
        public async Task Clear_InPlayMode_RemovesAllLoadedStaticCsvCaches()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("Phase6DataManagerClear");
            await Task.Yield();

            StaticDataInfo firstInfo = CreateStaticDataInfo(temp, "clear-a");
            StaticDataInfo secondInfo = CreateStaticDataInfo(temp, "clear-b");
            StaticData first = (StaticData)DataManager.GetData(firstInfo);
            StaticData second = (StaticData)DataManager.GetData(secondInfo);

            Assert.That(DataManager.HasData, Is.True);

            DataManager.Clear();

            Assert.That(DataManager.HasData, Is.False);
            Assert.That(DataManager.GetData(firstInfo), Is.Not.SameAs(first));
            Assert.That(DataManager.GetData(secondInfo), Is.Not.SameAs(second));
            Assert.That(scene.Scene.isLoaded, Is.True);
        }

        [Test]
        [Category("PlayMode.Phase6")]
        public async Task InvalidStaticDataInfo_InPlayMode_ReturnsNullWithoutCreatingCache()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("Phase6DataManagerInvalidData");
            await Task.Yield();

            Protocol protocol = PlayModeProjectHarness.CreateProtocol();
            Patient patient = CreatePatient("invalid");
            StaticDataInfo invalidDataInfo = new(
                "phase6-playmode-invalid-static",
                protocol,
                new CSV("", Array.Empty<Error>(), Array.Empty<Warning>()),
                new Error[] { new RequiredFieldEmptyError("phase6 playmode invalid data") },
                Array.Empty<Warning>(),
                patient,
                "phase6-playmode-db",
                "phase6-playmode-invalid-data-001");

            Assert.That(DataManager.GetData(invalidDataInfo), Is.Null);
            Assert.That(DataManager.GetData(invalidDataInfo, protocol.Blocs[0], "A1"), Is.Null);
            Assert.That(DataManager.GetStatistics(invalidDataInfo, protocol.Blocs[0], "A1"), Is.Null);
            Assert.That(DataManager.HasData, Is.False);
            Assert.That(scene.Scene.isLoaded, Is.True);
        }

        private static StaticDataInfo CreateStaticDataInfo(PlayModeTempDirectoryScope temp, string suffix)
        {
            string csvPath = temp.GetPath($"phase6-static-{suffix}.csv");
            File.WriteAllLines(csvPath, new[]
            {
                "channel,alpha,beta",
                "A1,1.5,2.5",
                "B2,3.5,4.5"
            });

            Protocol protocol = PlayModeProjectHarness.CreateProtocol();
            return new StaticDataInfo(
                $"phase6-playmode-static-{suffix}",
                protocol,
                new CSV(csvPath, Array.Empty<Error>(), Array.Empty<Warning>(), $"phase6-playmode-container-{suffix}"),
                Array.Empty<Error>(),
                Array.Empty<Warning>(),
                CreatePatient(suffix),
                "phase6-playmode-db",
                $"phase6-playmode-static-data-{suffix}");
        }

        private static Patient CreatePatient(string suffix)
        {
            return new Patient(
                $"phase6-playmode-patient-{suffix}",
                Array.Empty<BaseMesh>(),
                Array.Empty<MRI>(),
                Array.Empty<Site>(),
                Array.Empty<BaseTagValue>(),
                string.Empty,
                $"phase6-playmode-patient-{suffix}");
        }
    }
}
