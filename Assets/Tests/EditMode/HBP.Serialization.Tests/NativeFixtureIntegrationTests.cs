using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Database;
using HBP.Core.Errors;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using PersistentDataManager = HBP.Core.Preferences.PersistentDataManager;
using LocalizersObjects = HBP.Core.Object3D.LocalizersObjects;

namespace HBP.Tests.Serialization
{
    public class NativeFixtureIntegrationTests
    {
        private NormalizationType m_DefaultNormalization;

        [SetUp]
        public void SetUp()
        {
            m_DefaultNormalization = DataManager.DefaultNormalization;
            DataManager.Clear();
            DataManager.DefaultNormalization = NormalizationType.None;
        }

        [TearDown]
        public void TearDown()
        {
            DataManager.Clear();
            DataManager.DefaultNormalization = m_DefaultNormalization;
        }

        [Test]
        [Category("NativeFixtures")]
        public void NativeFixtures_ContainersPatientTreeAndLocalizers_AreDiscoverable()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            DataContainer[] containers =
            {
                new BrainVision(NativePath("EEG", "BrainVision", "native_brainvision_alpha.vhdr"), Array.Empty<Error>(), Array.Empty<Warning>()),
                new EDF(NativePath("EEG", "EDF", "native_edf.edf"), Array.Empty<Error>(), Array.Empty<Warning>()),
                new Elan(NativePath("EEG", "Elan", "native_from_brainvision.eeg"), NativePath("EEG", "Elan", "native_from_brainvision.pos"), string.Empty, Array.Empty<Error>(), Array.Empty<Warning>()),
                new FIF(NativePath("EEG", "FIF", "native_raw.fif"), Array.Empty<Error>(), Array.Empty<Warning>()),
                new Micromed(NativePath("EEG", "Micromed", "native_from_brainvision.trc"), Array.Empty<Error>(), Array.Empty<Warning>()),
                new Nifti(NativePath("Nifti", "fmri_4d.nii.gz"), Array.Empty<Error>(), Array.Empty<Warning>()),
                new CSV(NativePath("Static", "native_static.csv"), Array.Empty<Error>(), Array.Empty<Warning>())
            };

            foreach (DataContainer container in containers)
            {
                Assert.That(container.GetErrors(), Is.Empty, container.GetType().Name);
            }

            string patientRoot = NativePath("Patients", "synthetic-patient");
            MRI[] mris = MRI.LoadFromDirectory(patientRoot);
            BaseMesh[] meshes = BaseMesh.LoadFromDirectory(patientRoot);
            LeftRightMesh whiteMatter = meshes.OfType<LeftRightMesh>().Single(mesh => mesh.Name == "White matter");

            Assert.That(mris, Has.Length.EqualTo(1));
            Assert.That(mris[0].Name, Is.EqualTo("Preimplantation"));
            Assert.That(mris[0].IsUsable, Is.True);
            Assert.That(meshes.Select(mesh => mesh.Name), Is.SupersetOf(new[] { "Grey matter", "White matter" }));
            Assert.That(whiteMatter.HasMesh, Is.True);
            Assert.That(whiteMatter.HasMarsAtlas, Is.True);
            Assert.That(whiteMatter.HasTransformation, Is.True);

            string localizersTarget = Path.Combine(ApplicationState.DataPath, "Atlases", "Localizers");
            CopyDirectory(NativePath("Localizers"), localizersTarget);
            LocalizersObjects localizers = new();

            Assert.That(localizers.AvailableProtocolNames, Is.EquivalentTo(new[] { "protocol-alpha" }));
            Assert.That(localizers.AvailableDataNames, Is.EquivalentTo(new[] { "signal-alpha" }));
            Assert.That(localizers.GetAvailableBlocNames("protocol-alpha"), Is.EquivalentTo(new[] { "bloc-alpha", "bloc-beta" }));
        }

        [Test]
        [Category("NativeFixtures")]
        public async Task NativeGeneratedProject_LoadsAndResolvesFixtureAlias()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            RegisterNativeFixtureAlias();
            Protocol protocol = SyntheticProjectFactory.CreateProtocol();
            DatabaseManager.Database.SetProtocols(new[] { protocol });

            string archivePath = TestPathUtility.FixturePath("Projects", "Generated", "native-fixture-reference.hibop");
            ProjectInfo info = new(archivePath);
            Project loaded = new(info.Name, new HBP.Core.Data.ProjectPreferences("native-load-placeholder"));
            ApplicationState.LoadedProject = loaded;
            ApplicationState.LoadedProjectLocation = Path.GetDirectoryName(archivePath);

            await loaded.LoadAsync(info, NoProgress, CancellationToken.None);

            Dataset dataset = loaded.Datasets.Single();
            Assert.That(loaded.Patients, Has.Count.EqualTo(1));
            Assert.That(loaded.Groups, Has.Count.EqualTo(1));
            Assert.That(dataset.Data, Has.Count.EqualTo(9));
            Assert.That(dataset.Protocol, Is.SameAs(protocol));

            foreach (DataInfo dataInfo in dataset.Data)
            {
                Assert.That(dataInfo.DataContainer.GetErrors(), Is.Empty, dataInfo.Name);
                if (dataInfo is FMRIDataInfo fmriDataInfo)
                {
                    Assert.That(fmriDataInfo.MaskDataContainer.GetErrors(), Is.Empty, dataInfo.Name);
                }
                if (dataInfo is MEGvDataInfo megvDataInfo)
                {
                    Assert.That(megvDataInfo.MaskDataContainer.GetErrors(), Is.Empty, dataInfo.Name);
                }
                if (dataInfo is SharedFMRIDataInfo sharedFMRIDataInfo)
                {
                    Assert.That(sharedFMRIDataInfo.MaskDataContainer.GetErrors(), Is.Empty, dataInfo.Name);
                }
            }

            IEEGDataInfo brainVisionInfo = dataset.GetIEEGDataInfos().Single(dataInfo => dataInfo.Name == "signal-alpha");
            BrainVision brainVision = (BrainVision)brainVisionInfo.DataContainer;
            Assert.That(brainVision.SavedHeader, Does.StartWith("[HIBOP_NATIVE_FIXTURES]"));
            Assert.That(brainVision.Header, Does.StartWith(NativeRoot()));

            StaticDataInfo staticInfo = dataset.GetStaticDataInfos().Single();
            StaticData staticData = (StaticData)DataManager.GetData(staticInfo);
            Assert.That(staticData.Labels, Is.EquivalentTo(new[] { "baseline", "response" }));
            Assert.That(staticData.ValuesByChannel["A1"], Is.EqualTo(new[] { 0.1f, 1.5f }).Within(0.0001f));
        }

        [Test]
        [Category("NativeFixtures")]
        [Category("NativeDll")]
        public void NativeEegFixtures_LoadThroughDataManager_WhenNativeDllsAreAvailable()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            foreach (IEEGDataInfo dataInfo in CreateNativeIEEGDataInfos())
            {
                IEEGData data = ExecuteNativeOrIgnore(
                    () => (IEEGData)DataManager.GetData(dataInfo),
                    dataInfo.Name);

                Assert.That(data.Frequency.Value, Is.EqualTo(200), dataInfo.Name);
                Assert.That(data.UnitByChannel.Keys, Is.SupersetOf(new[] { "A1", "A2", "A3" }), dataInfo.Name);
                Assert.That(data.DataByBloc[dataInfo.Protocol.Blocs[0]].Trials, Is.Not.Empty, dataInfo.Name);
                Assert.That(DataManager.GetData(dataInfo, dataInfo.Protocol.Blocs[0], "A1").Trials, Is.Not.Empty, dataInfo.Name);
            }
        }

        [Test]
        [Category("NativeFixtures")]
        [Category("NativeDll")]
        public async Task NativeNiftiFixture_LoadsVolumeAndMask_WhenNativeDllsAreAvailable()
        {
            HBP.Core.Object3D.FMRI fmri = new(
                "native-fmri",
                NativePath("Nifti", "fmri_4d.nii.gz"),
                NativePath("Nifti", "mask_binary.nii"),
                false);

            try
            {
                await ExecuteNativeOrIgnoreAsync(async () => await fmri.LoadAsync(), "native NIfTI fMRI");

                Assert.That(fmri.Loaded, Is.True);
                Assert.That(fmri.Volumes, Is.Not.Empty);
                Assert.That(fmri.MaskVolume, Is.Not.Null);
            }
            finally
            {
                fmri.Clean();
            }
        }

        private static IEnumerable<IEEGDataInfo> CreateNativeIEEGDataInfos()
        {
            Protocol protocol = SyntheticProjectFactory.CreateProtocol();
            Patient patient = new(
                "native-eeg-patient",
                Array.Empty<BaseMesh>(),
                Array.Empty<MRI>(),
                new[]
                {
                    new Site("A1", Array.Empty<Coordinate>(), Array.Empty<BaseTagValue>(), "native-eeg-site-a1"),
                    new Site("A2", Array.Empty<Coordinate>(), Array.Empty<BaseTagValue>(), "native-eeg-site-a2"),
                    new Site("A3", Array.Empty<Coordinate>(), Array.Empty<BaseTagValue>(), "native-eeg-site-a3")
                },
                Array.Empty<BaseTagValue>(),
                string.Empty,
                "native-eeg-patient-001");

            yield return CreateIEEGDataInfo("native-brainvision", protocol, patient, new BrainVision(NativePath("EEG", "BrainVision", "native_brainvision_alpha.vhdr"), Array.Empty<Error>(), Array.Empty<Warning>(), "native-brainvision-container"));
            yield return CreateIEEGDataInfo("native-edf", protocol, patient, new EDF(NativePath("EEG", "EDF", "native_edf.edf"), Array.Empty<Error>(), Array.Empty<Warning>(), "native-edf-container"));
            yield return CreateIEEGDataInfo("native-elan", protocol, patient, new Elan(NativePath("EEG", "Elan", "native_from_brainvision.eeg"), NativePath("EEG", "Elan", "native_from_brainvision.pos"), string.Empty, Array.Empty<Error>(), Array.Empty<Warning>(), "native-elan-container"));
            yield return CreateIEEGDataInfo("native-fif", protocol, patient, new FIF(NativePath("EEG", "FIF", "native_raw.fif"), Array.Empty<Error>(), Array.Empty<Warning>(), "native-fif-container"));
            yield return CreateIEEGDataInfo("native-micromed", protocol, patient, new Micromed(NativePath("EEG", "Micromed", "native_from_brainvision.trc"), Array.Empty<Error>(), Array.Empty<Warning>(), "native-micromed-container"));
        }

        private static IEEGDataInfo CreateIEEGDataInfo(string name, Protocol protocol, Patient patient, DataContainer container)
        {
            return new IEEGDataInfo(
                name,
                protocol,
                container,
                Array.Empty<Error>(),
                Array.Empty<Warning>(),
                patient,
                NormalizationType.None,
                "native-fixture-db",
                $"{name}-data-info");
        }

        private static T ExecuteNativeOrIgnore<T>(Func<T> action, string context)
        {
            try
            {
                return action();
            }
            catch (Exception exception) when (IsMissingNativeDependency(exception))
            {
                Assert.Ignore($"Native dependency unavailable for {context}: {exception.Message}");
                throw;
            }
        }

        private static async Task ExecuteNativeOrIgnoreAsync(Func<Task> action, string context)
        {
            try
            {
                await action();
            }
            catch (Exception exception) when (IsMissingNativeDependency(exception))
            {
                Assert.Ignore($"Native dependency unavailable for {context}: {exception.Message}");
            }
        }

        private static bool IsMissingNativeDependency(Exception exception)
        {
            for (Exception current = exception; current != null; current = current.InnerException)
            {
                if (current is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
                {
                    return true;
                }
            }
            return false;
        }

        private static void RegisterNativeFixtureAlias()
        {
            PersistentDataManager.Aliases.SetAliases(
                new[] { new Alias("[HIBOP_NATIVE_FIXTURES]", NativeRoot(), "native-fixture-test-alias-001") },
                false);
        }

        private static string NativeRoot()
        {
            return TestPathUtility.FixturePath("Native");
        }

        private static string NativePath(params string[] parts)
        {
            string path = NativeRoot();
            foreach (string part in parts)
            {
                path = Path.Combine(path, part);
            }
            return path;
        }

        private static void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);
            foreach (string directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(directory.Replace(sourceDirectory, targetDirectory));
            }
            foreach (string file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(sourceDirectory, targetDirectory), true);
            }
        }

        private static void NoProgress(float progress, float duration, LoadingText text)
        {
        }
    }
}
