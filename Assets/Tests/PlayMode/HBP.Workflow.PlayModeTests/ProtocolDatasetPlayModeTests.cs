using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Tools;
using HBP.Tests.PlayMode.Utilities;
using NUnit.Framework;

namespace HBP.Tests.PlayMode.Workflow
{
    public class ProtocolDatasetPlayModeTests
    {
        [Test]
        [Category("PlayMode.ProtocolDataset")]
        public async Task ProjectRoundTrip_InPlayMode_PreservesProtocolDatasetAndDataInfoVariants()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("ProtocolDatasetProtocolDatasetRoundTrip");

            Project source = PlayModeProjectHarness.CreateAndLoadCompleteProject();
            string archivePath = await SaveProjectAsync(temp, source);

            Project loaded = await LoadProjectAsync(archivePath);

            Dataset dataset = loaded.Datasets.Single();
            Protocol protocol = dataset.Protocol;
            Patient patient = loaded.Patients.Single();

            Assert.That(protocol.ID, Is.EqualTo(PlayModeProjectHarness.ProtocolId));
            Assert.That(dataset.ID, Is.EqualTo(PlayModeProjectHarness.DatasetId));
            Assert.That(dataset.Data.Select(dataInfo => dataInfo.Protocol), Is.All.SameAs(protocol));
            Assert.That(dataset.GetPatientDataInfos().Select(dataInfo => dataInfo.Patient), Is.All.SameAs(patient));
            Assert.That(dataset.GetStaticDataInfos(), Has.Length.EqualTo(1));
            Assert.That(dataset.GetIEEGDataInfos(), Has.Length.EqualTo(2));
            Assert.That(dataset.GetCCEPDataInfos(), Has.Length.EqualTo(1));
            Assert.That(dataset.GetFMRIDataInfos(), Has.Length.EqualTo(1));
            Assert.That(dataset.GetSharedFMRIDataInfos(), Has.Length.EqualTo(1));
            Assert.That(dataset.GetMEGDataInfos(), Has.Length.EqualTo(2));
            Assert.That(dataset.Data.Select(dataInfo => dataInfo.DataContainer.GetType()), Is.EquivalentTo(new[]
            {
                typeof(Elan),
                typeof(Micromed),
                typeof(EDF),
                typeof(Nifti),
                typeof(BrainVision),
                typeof(FIF),
                typeof(Nifti),
                typeof(CSV)
            }));
            Assert.That(scene.Scene.isLoaded, Is.True);
        }

        private static async Task<string> SaveProjectAsync(PlayModeTempDirectoryScope temp, Project project)
        {
            string saveDirectory = temp.GetPath("protocol-dataset-project");
            Directory.CreateDirectory(saveDirectory);
            ApplicationState.LoadedProject = project;
            ApplicationState.LoadedProjectLocation = saveDirectory;

            await project.SaveAsync(saveDirectory, NoProgress, CancellationToken.None);

            return Path.Combine(saveDirectory, project.FileName);
        }

        private static async Task<Project> LoadProjectAsync(string archivePath)
        {
            ProjectInfo info = new(archivePath);
            Project loaded = new(info.Name, new ProjectPreferences("playmode-load-placeholder"));
            ApplicationState.LoadedProject = loaded;
            ApplicationState.LoadedProjectLocation = Path.GetDirectoryName(archivePath);
            await loaded.LoadAsync(info, NoProgress, CancellationToken.None);
            return loaded;
        }

        private static void NoProgress(float progress, float duration, LoadingText text)
        {
        }
    }
}
