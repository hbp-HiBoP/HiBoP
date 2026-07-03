using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Tests.PlayMode.Utilities;
using NUnit.Framework;

namespace HBP.Tests.PlayMode.Workflow
{
    public class SerializationPlayModeTests
    {
        [Test]
        [Category("PlayMode.Serialization")]
        public async Task ProjectRoundTrip_InPlayMode_PreservesSerializedReferencesAndIds()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("SerializationSerializationRoundTrip");

            Project source = PlayModeProjectHarness.CreateAndLoadCompleteProject();
            string archivePath = await SaveProjectAsync(temp, source);

            Project loaded = await LoadProjectAsync(archivePath);

            Assert.That(loaded.Preferences.ID, Is.EqualTo(PlayModeProjectHarness.ProjectId));
            Assert.That(loaded.Patients.Single().ID, Is.EqualTo(PlayModeProjectHarness.PatientId));
            Assert.That(loaded.Groups.Single().ID, Is.EqualTo(PlayModeProjectHarness.GroupId));
            Assert.That(loaded.Groups.Single().Patients.Single().ID, Is.EqualTo(PlayModeProjectHarness.PatientId));
            Assert.That(loaded.Datasets.Single().ID, Is.EqualTo(PlayModeProjectHarness.DatasetId));
            Assert.That(loaded.Datasets.Single().Protocol.ID, Is.EqualTo(PlayModeProjectHarness.ProtocolId));
            Assert.That(loaded.Visualizations.Single().ID, Is.EqualTo(PlayModeProjectHarness.VisualizationId));
            Assert.That(loaded.Visualizations.Single().Patients.Single().ID, Is.EqualTo(PlayModeProjectHarness.PatientId));
            Assert.That(loaded.Visualizations.Single().Columns.Select(column => column.ID), Is.EquivalentTo(source.Visualizations.Single().Columns.Select(column => column.ID)));
            Assert.That(await loaded.CheckProjectIDsAsync(), Is.Empty);
            Assert.That(scene.Scene.isLoaded, Is.True);
        }

        [TestCase("Patients/playmode-patient-001.patient", typeof(CanNotReadPatientFileException))]
        [TestCase("Groups/playmode-group-alpha.group", typeof(CanNotReadGroupFileException))]
        [TestCase("Datasets/playmode-dataset-alpha.dataset", typeof(CanNotReadDatasetFileException))]
        [TestCase("Visualizations/playmode-visualization-alpha.visualization", typeof(CanNotReadVisualizationFileException))]
        [Category("PlayMode.Serialization")]
        public async Task ProjectLoadAsync_WithCorruptedSerializedEntryInPlayMode_ThrowsControlledExceptionAndCleansExtraction(string entryName, Type expectedExceptionType)
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("SerializationSerializationCorruptedEntry");

            Project source = PlayModeProjectHarness.CreateAndLoadCompleteProject();
            string archivePath = await SaveProjectAsync(temp, source);
            ReplaceZipEntryContent(archivePath, entryName, "{ this is not valid json");
            Project loaded = new(source.Name, new ProjectPreferences("playmode-load-placeholder"));
            ProjectInfo info = new(archivePath);
            ApplicationState.LoadedProject = loaded;
            ApplicationState.LoadedProjectLocation = Path.GetDirectoryName(archivePath);

            Exception exception = await AsyncPlayModeTestUtilities.CaptureExceptionAsync(async () => await loaded.LoadAsync(info, NoProgress, CancellationToken.None));

            Assert.That(exception, Is.TypeOf(expectedExceptionType));
            Assert.That(exception.InnerException, Is.Not.Null);
            Assert.That(Directory.Exists(ApplicationState.ExtractProjectFolder), Is.False);
            Assert.That(scene.Scene.isLoaded, Is.True);
        }

        private static async Task<string> SaveProjectAsync(PlayModeTempDirectoryScope temp, Project project)
        {
            string saveDirectory = temp.GetPath("serialization-project");
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

        private static void ReplaceZipEntryContent(string archivePath, string entryName, string content)
        {
            using ZipArchive zip = ZipFile.Open(archivePath, ZipArchiveMode.Update);
            zip.GetEntry(entryName)?.Delete();
            ZipArchiveEntry entry = zip.CreateEntry(entryName);
            using StreamWriter writer = new(entry.Open());
            writer.Write(content);
        }

        private static void NoProgress(float progress, float duration, LoadingText text)
        {
        }
    }
}
