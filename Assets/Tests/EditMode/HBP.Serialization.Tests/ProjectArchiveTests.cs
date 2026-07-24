using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using PersistentDataManager = HBP.Core.Preferences.PersistentDataManager;

namespace HBP.Tests.Serialization
{
    public class ProjectArchiveTests
    {
        [Test]
        public async Task SaveLoad_MinimalProject_PreservesArchiveStructureAndState()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateMinimalProject();
            string archivePath = await SaveProject(temp, source);

            Assert.That(Project.IsProject(archivePath), Is.True);
            AssertArchiveContainsProjectDirectories(archivePath);

            Project loaded = await LoadProject(temp, archivePath);
            ProjectSnapshotAssert.AreFunctionallyEquivalent(source, loaded);
        }

        [Test]
        public async Task SaveLoad_CompleteSyntheticProject_PreservesReferencesAndIds()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateCompleteProject();
            string archivePath = await SaveProject(temp, source);
            Project loaded = await LoadProject(temp, archivePath);

            ProjectSnapshotAssert.AreFunctionallyEquivalent(source, loaded);
            Assert.That(await loaded.CheckProjectIDsAsync(), Is.Empty);
        }

        [Test]
        public void NewProject_CreatesDefaultPreferencesAndEmptyCollections()
        {
            Project project = new("project-archive-defaults");

            Assert.That(project.Name, Is.EqualTo("project-archive-defaults"));
            Assert.That(project.FileName, Is.EqualTo("project-archive-defaults" + Project.EXTENSION));
            Assert.That(project.Preferences, Is.Not.Null);
            Assert.That(project.Patients, Is.Empty);
            Assert.That(project.Groups, Is.Empty);
            Assert.That(project.Datasets, Is.Empty);
            Assert.That(project.Visualizations, Is.Empty);
        }

        [Test]
        public async Task ProjectInfo_ReadsArchiveSummaryAndSettings()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateCompleteProject();
            string archivePath = await SaveProject(temp, source);

            ProjectInfo info = new(archivePath);

            Assert.That(info.Path, Is.EqualTo(archivePath));
            Assert.That(info.Name, Is.EqualTo(source.Name));
            Assert.That(info.Settings.ID, Is.EqualTo(source.Preferences.ID));
            Assert.That(info.Settings.CanLoadProject, Is.True);
            Assert.That(info.Patients, Is.EqualTo(source.Patients.Count));
            Assert.That(info.Groups, Is.EqualTo(source.Groups.Count));
            Assert.That(info.Datasets, Is.EqualTo(source.Datasets.Count));
            Assert.That(info.Visualizations, Is.EqualTo(source.Visualizations.Count));
            Assert.That(info.Manifest, Is.Not.Null);
            Assert.That(info.Manifest.SchemaVersion, Is.EqualTo(ProjectManifest.LegacySchemaVersion));
            Assert.That(info.Manifest.ProductVersion, Is.EqualTo(source.Preferences.Version));
        }

        [Test]
        public void ProjectInfo_DefaultConstructor_HasEmptySummary()
        {
            ProjectInfo info = new();

            Assert.That(info.Name, Is.Empty);
            Assert.That(info.Path, Is.Empty);
            Assert.That(info.Settings, Is.Not.Null);
            Assert.That(info.Patients, Is.Zero);
            Assert.That(info.Groups, Is.Zero);
            Assert.That(info.Datasets, Is.Zero);
            Assert.That(info.Visualizations, Is.Zero);
            Assert.That(info.SettingsLoadException, Is.Null);
        }

        [Test]
        public async Task Save_CompleteSyntheticProject_WritesExpectedArchiveEntries()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateCompleteProject();
            string archivePath = await SaveProject(temp, source);

            using ZipArchive zip = ZipFile.OpenRead(archivePath);
            string[] entryNames = zip.Entries.Select(entry => entry.FullName).ToArray();

            Assert.That(entryNames, Does.Contain(source.Name + ProjectPreferences.EXTENSION));
            Assert.That(entryNames, Does.Contain("Patients/"));
            Assert.That(entryNames, Does.Contain("Groups/"));
            Assert.That(entryNames, Does.Contain("Datasets/"));
            Assert.That(entryNames, Does.Contain("Visualizations/"));
            Assert.That(entryNames, Does.Contain($"Patients/{source.Patients[0].ID}{Patient.EXTENSION}"));
            Assert.That(entryNames, Does.Contain($"Groups/{source.Groups[0].Name}{Core.Data.Group.EXTENSION}"));
            Assert.That(entryNames, Does.Contain($"Datasets/{source.Datasets[0].Name}{Dataset.EXTENSION}"));
            Assert.That(entryNames, Does.Contain($"Visualizations/{source.Visualizations[0].Name}{Visualization.EXTENSION}"));
            Assert.That(entryNames.Count(name => name.StartsWith("Patients/") && name.EndsWith(Patient.EXTENSION)), Is.EqualTo(source.Patients.Count));
            Assert.That(entryNames.Count(name => name.StartsWith("Groups/") && name.EndsWith(Core.Data.Group.EXTENSION)), Is.EqualTo(source.Groups.Count));
            Assert.That(entryNames.Count(name => name.StartsWith("Datasets/") && name.EndsWith(Dataset.EXTENSION)), Is.EqualTo(source.Datasets.Count));
            Assert.That(entryNames.Count(name => name.StartsWith("Visualizations/") && name.EndsWith(Visualization.EXTENSION)), Is.EqualTo(source.Visualizations.Count));
        }

        [Test]
        public async Task SaveLoad_UsesDatabaseProtocolsInsteadOfSeparateProtocolArchiveEntries()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateCompleteProject();
            string archivePath = await SaveProject(temp, source);

            using (ZipArchive zip = ZipFile.OpenRead(archivePath))
            {
                string[] entryNames = zip.Entries.Select(entry => entry.FullName).ToArray();
                Assert.That(entryNames.Any(name => name.StartsWith("Protocols/")), Is.False);
                Assert.That(entryNames.Any(name => name.EndsWith(Protocol.EXTENSION)), Is.False);
            }

            Protocol databaseProtocol = SyntheticProjectFactory.CreateProtocol();
            Project loaded = await LoadProject(temp, archivePath, databaseProtocol);

            Dataset loadedDataset = loaded.Datasets.Single();
            Assert.That(loadedDataset.Protocol, Is.SameAs(databaseProtocol));
            Assert.That(loadedDataset.Data.Select(data => data.Protocol), Is.All.SameAs(databaseProtocol));
        }

        [Test]
        public async Task LoadAsync_LegacyProtocolsFolder_IsIgnored()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateCompleteProject();
            string archivePath = await SaveProject(temp, source);
            AddZipEntryContent(archivePath, "Protocols/", string.Empty);
            AddZipEntryContent(archivePath, "Protocols/ignored" + Protocol.EXTENSION, "{ this is deliberately invalid json");

            Protocol databaseProtocol = SyntheticProjectFactory.CreateProtocol();
            ProjectInfo info = new(archivePath);
            Project loaded = await LoadProject(temp, archivePath, databaseProtocol);

            Assert.That(info.Manifest.Entries.Keys.Any(name => name.StartsWith("Protocols/", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(loaded.Datasets.Single().Protocol, Is.SameAs(databaseProtocol));
        }

        [Test]
        public async Task SaveLoadResave_CompleteSyntheticProject_ProducesValidArchive()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateCompleteProject();
            string archivePath = await SaveProject(temp, source);
            Project loaded = await LoadProject(temp, archivePath);

            string resavedArchivePath = await SaveProject(temp, loaded, "resaved");
            Project reloaded = await LoadProject(temp, resavedArchivePath);

            Assert.That(Project.IsProject(resavedArchivePath), Is.True);
            AssertArchiveContainsProjectDirectories(resavedArchivePath);
            ProjectSnapshotAssert.AreFunctionallyEquivalent(loaded, reloaded);
            Assert.That(await reloaded.CheckProjectIDsAsync(), Is.Empty);
        }

        [Test]
        public async Task Save_CompleteSyntheticProject_DoesNotLeakTemporaryAbsolutePaths()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateCompleteProject();
            string archivePath = await SaveProject(temp, source);

            AssertArchiveDoesNotContainAbsolutePath(archivePath, temp.Path);
        }

        [Test]
        public async Task CheckProjectIDsAsync_ReportsDuplicateIds()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateCompleteProject();
            project.Patients[0].ID = project.Preferences.ID;

            var duplicateIDs = await project.CheckProjectIDsAsync();

            Assert.That(duplicateIDs, Contains.Key(project.Preferences.ID));
            Assert.That(duplicateIDs[project.Preferences.ID], Has.Count.EqualTo(2));
        }

        [Test]
        public async Task CheckProjectIDsAsync_ReportsMissingIds()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateCompleteProject();
            project.Patients[0].ID = string.Empty;

            var invalidIDs = await project.CheckProjectIDsAsync();

            Assert.That(invalidIDs, Contains.Key(string.Empty));
            Assert.That(invalidIDs[string.Empty].Select(entry => entry.Item1), Does.Contain(project.Patients[0]));
        }

        [Test, Timeout(5000)]
        public async Task LoadAsync_WhenCancelled_CleansExtractedProjectFolder()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateMinimalProject();
            string archivePath = await SaveProject(temp, source);
            ProjectInfo info = new(archivePath);
            Project loaded = new(info.Name, new ProjectPreferences("cancel-placeholder"));
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();

            Exception exception = await CaptureExceptionAsync(async () => await loaded.LoadAsync(info, NoProgress, cancellation.Token));

            Assert.That(exception, Is.TypeOf<OperationCanceledException>());
            Assert.That(Directory.Exists(ApplicationState.ExtractProjectFolder), Is.False);
        }

        [Test]
        public async Task LoadAsync_DoesNotTouchExtractionFolder()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateCompleteProject();
            string archivePath = await SaveProject(temp, source);
            Directory.CreateDirectory(ApplicationState.ExtractProjectFolder);
            string sentinelPath = Path.Combine(ApplicationState.ExtractProjectFolder, "sentinel.txt");
            File.WriteAllText(sentinelPath, "must remain untouched");

            Project loaded = await LoadProject(temp, archivePath);

            Assert.That(loaded.Patients, Has.Count.EqualTo(source.Patients.Count));
            Assert.That(File.ReadAllText(sentinelPath), Is.EqualTo("must remain untouched"));
        }

        [Test, Timeout(5000)]
        public async Task LoadAsync_WhenCancelledDuringPatientsLoad_CleansExtraction()
        {
            await AssertLoadCancellationFromProgress("Loading patients");
        }

        [Test, Timeout(5000)]
        public async Task LoadAsync_FileValidationRunsSilentlyAfterReady()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateCompleteProject();
            string archivePath = await SaveProject(temp, source);
            ProjectInfo info = new(archivePath);
            Project loaded = new(source.Name, new ProjectPreferences("load-placeholder"));
            ApplicationState.LoadedProject = loaded;
            ApplicationState.LoadedProjectLocation = Path.GetDirectoryName(archivePath);
            DatabaseManager.Database.SetProtocols(new[] { SyntheticProjectFactory.CreateProtocol() });
            bool initialLoaderSawValidation = false;

            await loaded.LoadAsync(
                info,
                (progress, duration, text) =>
                    initialLoaderSawValidation |=
                        text.ToString().StartsWith("Validating"),
                CancellationToken.None);

            Assert.That(initialLoaderSawValidation, Is.False);
            Assert.That(loaded.CurrentLoadingOperation.Ready.IsCompleted, Is.True);
            await loaded.CurrentLoadingOperation.Validated;
            Assert.That(loaded.NeedsValidationWait, Is.False);
        }

        [Test, Timeout(5000)]
        public async Task LoadAsync_WhenCancelledDuringGroupsLoad_CleansExtraction()
        {
            await AssertLoadCancellationFromProgress("Loading groups");
        }

        [Test, Timeout(5000)]
        public async Task LoadAsync_WhenCancelledDuringDatasetsLoad_CleansExtraction()
        {
            await AssertLoadCancellationFromProgress("Loading datasets");
        }

        [Test, Timeout(5000)]
        public async Task LoadAsync_WhenCancelledDuringVisualizationsLoad_CleansExtraction()
        {
            await AssertLoadCancellationFromProgress("Loading visualizations");
        }

        [Test]
        public void IsProject_ReturnsFalseForWrongExtension()
        {
            using TempDirectoryScope temp = new();
            string path = temp.GetPath("not-a-project.txt");
            File.WriteAllText(path, "synthetic");

            Assert.That(Project.IsProject(path), Is.False);
        }

        [Test]
        public void IsProject_ReturnsFalseWhenSettingsAreMissing()
        {
            using TempDirectoryScope temp = new();
            string path = temp.GetPath("missing-settings.hibop");

            using (ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                zip.CreateEntry("Patients/");
                zip.CreateEntry("Groups/");
                zip.CreateEntry("Datasets/");
                zip.CreateEntry("Visualizations/");
            }

            Assert.That(Project.IsProject(path), Is.False);
        }

        [Test]
        public void ProjectInfo_RejectsMalformedArchive()
        {
            using TempDirectoryScope temp = new();
            string path = temp.GetPath("malformed-project.hibop");
            File.WriteAllText(path, "this is not a zip archive");

            Exception exception = Assert.Catch<DirectoryNotProjectException>(() => _ = new ProjectInfo(path));
            Assert.That(exception, Is.Not.Null);
        }

        [Test]
        public void IsProject_ReturnsFalseForMalformedArchive()
        {
            using TempDirectoryScope temp = new();
            string path = temp.GetPath("malformed-project.hibop");
            File.WriteAllText(path, "this is not a zip archive");

            Assert.That(Project.IsProject(path), Is.False);
        }

        [Test, Timeout(5000)]
        public async Task LoadAsync_CorruptedSettingsJson_ThrowsControlledException()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateMinimalProject();
            string archivePath = await SaveProject(temp, source);
            ReplaceZipEntryContent(archivePath, source.Name + ProjectPreferences.EXTENSION, "{ this is not valid json");
            ProjectInfo info = new()
            {
                Name = source.Name,
                Path = archivePath,
                Patients = 0,
                Groups = 0,
                Datasets = 0,
                Visualizations = 0
            };
            Project loaded = new(source.Name, new ProjectPreferences("load-placeholder"));

            Exception exception = await CaptureExceptionAsync(async () => await loaded.LoadAsync(info, NoProgress, CancellationToken.None));

            Assert.That(exception, Is.TypeOf<CanNotReadSettingsFileException>());
            Assert.That(exception.InnerException, Is.Not.Null);
            Assert.That(Directory.Exists(ApplicationState.ExtractProjectFolder), Is.False);
        }

        [Test]
        public async Task ProjectInfo_CorruptedSettingsJson_MarksArchiveAsNotLoadableWithoutTouchingTemporaryFolder()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateMinimalProject();
            string archivePath = await SaveProject(temp, source);
            ReplaceZipEntryContent(archivePath, source.Name + ProjectPreferences.EXTENSION, "{ this is not valid json");
            string sentinelPath = Path.Combine(ApplicationState.TMPFolder, "sentinel.txt");
            File.WriteAllText(sentinelPath, "must remain untouched");

            ProjectInfo info = new(archivePath);

            Assert.That(info.Settings.CanLoadProject, Is.False);
            Assert.That(info.SettingsLoadException, Is.Not.Null);
            Assert.That(info.SettingsLoadException.Message, Is.Not.Empty);
            Assert.That(File.ReadAllText(sentinelPath), Is.EqualTo("must remain untouched"));
        }

        [TestCase("../outside.patient")]
        [TestCase("/absolute.patient")]
        [TestCase("C:/absolute.patient")]
        public async Task ProjectManifest_RejectsUnsafeArchiveEntryNames(string unsafeEntryName)
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateMinimalProject();
            string archivePath = await SaveProject(temp, source);
            AddZipEntryContent(archivePath, unsafeEntryName, "{}");

            Assert.That(Project.IsProject(archivePath), Is.False);
            Assert.Throws<DirectoryNotProjectException>(() => _ = new ProjectInfo(archivePath));
        }

        [Test, Timeout(5000)]
        public async Task LoadAsync_CorruptedPatientJson_ThrowsControlledException()
        {
            await AssertCorruptedProjectEntryThrows(
                $"Patients/{SyntheticProjectFactory.PatientId}{Patient.EXTENSION}",
                typeof(CanNotReadPatientFileException));
        }

        [Test, Timeout(5000)]
        public async Task LoadAsync_CorruptedGroupJson_ThrowsControlledException()
        {
            await AssertCorruptedProjectEntryThrows(
                "Groups/synthetic-group-alpha" + Core.Data.Group.EXTENSION,
                typeof(CanNotReadGroupFileException));
        }

        [Test, Timeout(5000)]
        public async Task LoadAsync_CorruptedDatasetJson_ThrowsControlledException()
        {
            await AssertCorruptedProjectEntryThrows(
                "Datasets/dataset-alpha" + Dataset.EXTENSION,
                typeof(CanNotReadDatasetFileException));
        }

        [Test, Timeout(5000)]
        public async Task LoadAsync_CorruptedVisualizationJson_ThrowsControlledException()
        {
            await AssertCorruptedProjectEntryThrows(
                "Visualizations/visualization-alpha" + Visualization.EXTENSION,
                typeof(CanNotReadVisualizationFileException));
        }

        [Test]
        public async Task ProjectGetProject_ReturnsOnlyValidProjectArchivesInFolder()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project valid = SyntheticProjectFactory.CreateMinimalProject();
            string validArchive = await SaveProject(temp, valid, "project-discovery");
            string folder = Path.GetDirectoryName(validArchive);
            File.WriteAllText(Path.Combine(folder, "notes.txt"), "not a project");
            File.WriteAllText(Path.Combine(folder, "broken.hibop"), "not a zip");

            string[] projects = Project.GetProject(folder).ToArray();

            Assert.That(projects, Is.EquivalentTo(new[] { validArchive }));
        }

        [Test]
        public async Task ProjectGetProjectById_ReturnsMatchingArchive()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            string folder = temp.GetPath("project-id-discovery");
            Directory.CreateDirectory(folder);
            Project first = new("first-project", new ProjectPreferences("test-version", "project-id-alpha"));
            Project second = new("second-project", new ProjectPreferences("test-version", "project-id-beta"));
            await SaveProjectToDirectory(folder, first);
            string secondArchive = await SaveProjectToDirectory(folder, second);

            string found = first.GetProject(folder, "project-id-beta");

            Assert.That(found, Is.EqualTo(secondArchive));
            Assert.That(first.GetProject(folder, "missing-id"), Is.Null);
        }

        [Test]
        public async Task ProjectGetProjectById_IgnoresArchiveWithUnreadableSettings()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            string folder = temp.GetPath("project-id-corrupted-settings");
            Directory.CreateDirectory(folder);
            Project project = new("corrupted-settings-project", new ProjectPreferences("test-version", "project-id-corrupted"));
            string archivePath = await SaveProjectToDirectory(folder, project);
            ReplaceZipEntryContent(archivePath, project.Name + ProjectPreferences.EXTENSION, "{ this is not valid json");

            string found = project.GetProject(folder, "project-id-corrupted");

            Assert.That(found, Is.Null);
        }

        [Test]
        public void ProjectGetProject_ReturnsEmptyForMissingOrEmptyPath()
        {
            using TempDirectoryScope temp = new();

            Assert.That(Project.GetProject(string.Empty), Is.Empty);
            Assert.That(Project.GetProject(temp.GetPath("missing-folder")), Is.Empty);
        }

        [Test, Timeout(5000)]
        public async Task LoadAsync_MultipleSettingsFiles_ThrowsControlledExceptionAndCleansExtraction()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateMinimalProject();
            string archivePath = await SaveProject(temp, source);
            AddZipEntryContent(archivePath, "duplicate" + ProjectPreferences.EXTENSION, "{}");
            Project loaded = new(source.Name, new ProjectPreferences("load-placeholder"));

            Exception exception = await CaptureExceptionAsync(async () => await loaded.LoadAsync(CreateProjectInfo(source, archivePath), NoProgress, CancellationToken.None));

            Assert.That(exception, Is.TypeOf<MultipleSettingsFilesFoundException>());
            Assert.That(Directory.Exists(ApplicationState.ExtractProjectFolder), Is.False);
        }

        [Test, Timeout(5000)]
        public async Task LoadAsync_MissingRequiredArchiveFolder_ThrowsControlledExceptionAndCleansExtraction()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateMinimalProject();
            string archivePath = await SaveProject(temp, source);
            DeleteZipEntry(archivePath, "Datasets/");
            Project loaded = new(source.Name, new ProjectPreferences("load-placeholder"));

            Exception exception = await CaptureExceptionAsync(async () => await loaded.LoadAsync(CreateProjectInfo(source, archivePath), NoProgress, CancellationToken.None));

            Assert.That(exception, Is.TypeOf<FileNotFoundException>());
            Assert.That(Directory.Exists(ApplicationState.ExtractProjectFolder), Is.False);
        }

        [Test]
        public async Task LoadAsync_ReadyProgressIsMonotonicAndExcludesValidation()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateCompleteProject();
            string archivePath = await SaveProject(temp, source);
            ProjectInfo info = new(archivePath);
            Project loaded = new(source.Name, new ProjectPreferences("load-placeholder"));
            ApplicationState.LoadedProject = loaded;
            ApplicationState.LoadedProjectLocation = Path.GetDirectoryName(archivePath);
            float previousProgress = -1;
            LoadingText lastText = null;
            bool validationProgressReported = false;

            void TrackProgress(float progress, float duration, LoadingText text)
            {
                Assert.That(progress, Is.GreaterThanOrEqualTo(previousProgress));
                previousProgress = progress;
                lastText = text;
                validationProgressReported |=
                    text.ToString().StartsWith("Validating patient file references");
            }

            await loaded.LoadAsync(info, TrackProgress, CancellationToken.None);

            Assert.That(previousProgress, Is.EqualTo(1.0f));
            Assert.That(lastText.ToString(), Is.EqualTo("Project loaded successfully"));
            Assert.That(validationProgressReported, Is.False);
        }

        [Test]
        public void RemovePatient_CleansGroupsDatasetsAndVisualizations()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateCompleteProject();
            Patient patient = project.Patients[0];

            project.RemovePatient(patient);

            Assert.That(project.Patients.Contains(patient), Is.False);
            Assert.That(project.Groups.SelectMany(group => group.Patients).Contains(patient), Is.False);
            Assert.That(project.Datasets.SelectMany(dataset => dataset.GetPatientDataInfos()).Any(data => data.Patient == patient), Is.False);
            Assert.That(project.Visualizations.SelectMany(visualization => visualization.Patients).Contains(patient), Is.False);
        }

        [Test]
        public void SetPatients_ReplacesPatientsAndCleansDependentReferences()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateCompleteProject();

            project.SetPatients(Array.Empty<Patient>());

            Assert.That(project.Patients, Is.Empty);
            Assert.That(project.Groups.SelectMany(group => group.Patients), Is.Empty);
            Assert.That(project.Datasets.SelectMany(dataset => dataset.GetPatientDataInfos()), Is.Empty);
            Assert.That(project.Visualizations.SelectMany(visualization => visualization.Patients), Is.Empty);
        }

        [Test]
        public void RemoveDataset_RemovesAllDatasetBackedVisualizationColumns()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateCompleteProject();
            Dataset dataset = project.Datasets[0];
            Visualization visualization = project.Visualizations[0];

            project.RemoveDataset(dataset);

            Assert.That(project.Datasets.Contains(dataset), Is.False);
            Assert.That(visualization.Columns.OfType<IEEGColumn>(), Is.Empty);
            Assert.That(visualization.Columns.OfType<CCEPColumn>(), Is.Empty);
            Assert.That(visualization.Columns.OfType<FMRIColumn>(), Is.Empty);
            Assert.That(visualization.Columns.OfType<MEGColumn>(), Is.Empty);
            Assert.That(visualization.Columns.OfType<StaticColumn>(), Is.Empty);
            Assert.That(visualization.Columns.OfType<AnatomicColumn>(), Is.Not.Empty);
        }

        [Test]
        public void SetDatasets_ReplacesDatasetsAndRemovesMissingDatasetBackedVisualizationColumns()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateCompleteProject();
            Visualization visualization = project.Visualizations[0];

            project.SetDatasets(Array.Empty<Dataset>());

            Assert.That(project.Datasets, Is.Empty);
            Assert.That(visualization.Columns.OfType<IEEGColumn>(), Is.Empty);
            Assert.That(visualization.Columns.OfType<CCEPColumn>(), Is.Empty);
            Assert.That(visualization.Columns.OfType<FMRIColumn>(), Is.Empty);
            Assert.That(visualization.Columns.OfType<MEGColumn>(), Is.Empty);
            Assert.That(visualization.Columns.OfType<StaticColumn>(), Is.Empty);
            Assert.That(visualization.Columns.OfType<AnatomicColumn>(), Is.Not.Empty);
        }

        [Test]
        public void RemoveGroupAndVisualization_RemoveOnlyTargetObjects()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateCompleteProject();
            Core.Data.Group group = project.Groups[0];
            Visualization visualization = project.Visualizations[0];

            project.RemoveGroup(group);
            project.RemoveVisualization(visualization);

            Assert.That(project.Groups.Contains(group), Is.False);
            Assert.That(project.Visualizations.Contains(visualization), Is.False);
            Assert.That(project.Patients, Is.Not.Empty);
            Assert.That(project.Datasets, Is.Not.Empty);
        }

        [Test]
        public async Task SaveAsync_RejectsInvalidDestinationAndCleansExtraction()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateMinimalProject();

            Exception nullPathException = await CaptureExceptionAsync(async () => await project.SaveAsync(null, NoProgress, CancellationToken.None));
            Exception emptyPathException = await CaptureExceptionAsync(async () => await project.SaveAsync(string.Empty, NoProgress, CancellationToken.None));
            Exception missingPathException = await CaptureExceptionAsync(async () => await project.SaveAsync(temp.GetPath("missing-folder"), NoProgress, CancellationToken.None));

            Assert.That(nullPathException, Is.TypeOf<HBP.Core.Exceptions.DirectoryNotFoundException>());
            Assert.That(emptyPathException, Is.TypeOf<HBP.Core.Exceptions.DirectoryNotFoundException>());
            Assert.That(missingPathException, Is.TypeOf<HBP.Core.Exceptions.DirectoryNotFoundException>());
            Assert.That(Directory.Exists(ApplicationState.ExtractProjectFolder), Is.False);
        }

        [Test]
        public async Task SaveAsync_ProjectNameWithInvalidWindowsFileNameChars_FailsWithControlledExceptionAndCleansExtraction()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateMinimalProject();
            project.Name = "invalid:project-name";
            string saveDirectory = temp.GetPath("invalid-project-name");
            Directory.CreateDirectory(saveDirectory);

            Exception exception = await CaptureExceptionAsync(async () => await project.SaveAsync(saveDirectory, NoProgress, CancellationToken.None));

            Assert.That(exception, Is.TypeOf<CanNotSaveSettingsException>());
            Assert.That(Directory.Exists(ApplicationState.ExtractProjectFolder), Is.False);
            Assert.That(Directory.GetFiles(saveDirectory, "*.hibop"), Is.Empty);
        }

        [Test]
        public async Task SaveAsync_InternalEntryNamesWithInvalidWindowsFileNameChars_AreSanitizedButJsonNamesArePreserved()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateCompleteProject();
            project.Groups[0].Name = "invalid:group";
            project.Datasets[0].Name = "invalid:dataset";
            project.Visualizations[0].Name = "invalid:visualization";

            string archivePath = await SaveProject(temp, project);

            using (ZipArchive zip = ZipFile.OpenRead(archivePath))
            {
                string[] entryNames = zip.Entries.Select(entry => entry.FullName).ToArray();
                Assert.That(entryNames, Does.Contain("Groups/invalid_group" + Core.Data.Group.EXTENSION));
                Assert.That(entryNames, Does.Contain("Datasets/invalid_dataset" + Dataset.EXTENSION));
                Assert.That(entryNames, Does.Contain("Visualizations/invalid_visualization" + Visualization.EXTENSION));
                Assert.That(entryNames.Any(name => name.Contains(':')), Is.False);
            }

            Project loaded = await LoadProject(temp, archivePath);
            Assert.That(loaded.Groups[0].Name, Is.EqualTo("invalid:group"));
            Assert.That(loaded.Datasets[0].Name, Is.EqualTo("invalid:dataset"));
            Assert.That(loaded.Visualizations[0].Name, Is.EqualTo("invalid:visualization"));
        }

        [Test]
        public void ConvertToShortPath_UsesProjectTokenForExtractedProjectFolder()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            string fullPath = Path.Combine(ApplicationState.ExtractProjectFolder, "Data", "file.dat");

            string shortPath = fullPath.ConvertToShortPath();

            Assert.That(shortPath, Is.EqualTo(Path.Combine(PathExtension.PROJECT_TOKEN, "Data", "file.dat")));
        }

        [Test]
        public void ConvertToFullPath_ExpandsProjectTokenToExtractedProjectFolder()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            string shortPath = Path.Combine(PathExtension.PROJECT_TOKEN, "Data", "file.dat");

            string fullPath = shortPath.ConvertToFullPath();

            Assert.That(fullPath, Is.EqualTo(Path.Combine(ApplicationState.ExtractProjectFolder, "Data", "file.dat")));
        }

        [Test]
        public void ConvertToShortPath_UsesConfiguredAlias()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            string aliasRoot = temp.GetPath("alias-root");
            Directory.CreateDirectory(aliasRoot);
            PersistentDataManager.Aliases.SetAliases(new[] { new Alias("[SYNTHETIC_ROOT]", aliasRoot, "synthetic-alias-001") }, false);

            string shortPath = Path.Combine(aliasRoot, "nested", "file.dat").ConvertToShortPath();

            Assert.That(shortPath, Is.EqualTo(Path.Combine("[SYNTHETIC_ROOT]", "nested", "file.dat")));
        }

        [Test]
        public void ConvertToFullPath_ExpandsConfiguredAlias()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            string aliasRoot = temp.GetPath("alias-root");
            Directory.CreateDirectory(aliasRoot);
            PersistentDataManager.Aliases.SetAliases(new[] { new Alias("[SYNTHETIC_ROOT]", aliasRoot, "synthetic-alias-001") }, false);

            string fullPath = Path.Combine("[SYNTHETIC_ROOT]", "nested", "file.dat").ConvertToFullPath();

            Assert.That(fullPath, Is.EqualTo(Path.Combine(aliasRoot, "nested", "file.dat")));
        }

        [Test]
        public void ConvertToShortPath_ProjectTokenTakesPrecedenceBeforeAliases()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            PersistentDataManager.Aliases.SetAliases(new[] { new Alias("[SYNTHETIC_ROOT]", ApplicationState.ExtractProjectFolder, "synthetic-alias-001") }, false);

            string shortPath = Path.Combine(ApplicationState.ExtractProjectFolder, "nested", "file.dat").ConvertToShortPath();

            Assert.That(shortPath, Is.EqualTo(Path.Combine(PathExtension.PROJECT_TOKEN, "nested", "file.dat")));
        }

        [TestCase("Saving project")]
        [TestCase("Saving patients")]
        [TestCase("Saving visualizations")]
        [Timeout(5000)]
        public async Task SaveAsync_WhenCancelledFromProgress_CleansExtraction(string cancellationStep)
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateCompleteProject();
            string saveDirectory = temp.GetPath("cancel-save");
            Directory.CreateDirectory(saveDirectory);
            using CancellationTokenSource cancellation = new();

            void CancelOnStep(float progress, float duration, LoadingText text)
            {
                if (text.ToString().StartsWith(cancellationStep))
                {
                    cancellation.Cancel();
                }
            }

            Exception exception = await CaptureExceptionAsync(async () => await project.SaveAsync(saveDirectory, CancelOnStep, cancellation.Token));

            Assert.That(exception, Is.TypeOf<OperationCanceledException>());
            Assert.That(Directory.Exists(ApplicationState.ExtractProjectFolder), Is.False);
        }

        [Test]
        public async Task SaveAsync_ProgressCallbacksAreMonotonicAndReachSuccess()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateCompleteProject();
            string saveDirectory = temp.GetPath("save-progress");
            Directory.CreateDirectory(saveDirectory);
            float previousProgress = -1;
            LoadingText lastText = null;

            void TrackProgress(float progress, float duration, LoadingText text)
            {
                Assert.That(progress, Is.GreaterThanOrEqualTo(previousProgress));
                previousProgress = progress;
                lastText = text;
            }

            await project.SaveAsync(saveDirectory, TrackProgress, CancellationToken.None);

            Assert.That(previousProgress, Is.EqualTo(1.0f));
            Assert.That(lastText.ToString(), Is.EqualTo("Project saved successfully"));
        }

        [Test]
        public async Task SaveAsync_OverExistingArchive_RemovesStaleEntries()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateCompleteProject();
            string archivePath = await SaveProject(temp, project);
            string stalePatientEntry = $"Patients/{SyntheticProjectFactory.PatientId}{Patient.EXTENSION}";
            AssertArchiveContainsEntry(archivePath, stalePatientEntry);

            project.SetPatients(Array.Empty<Patient>());
            project.SetGroups(Array.Empty<Core.Data.Group>());
            project.SetDatasets(Array.Empty<Dataset>());
            project.SetVisualizations(Array.Empty<Visualization>());
            await SaveProject(temp, project);

            using ZipArchive zip = ZipFile.OpenRead(archivePath);
            string[] entryNames = zip.Entries.Select(entry => entry.FullName).ToArray();
            Assert.That(entryNames, Does.Not.Contain(stalePatientEntry));
            Assert.That(entryNames.Count(name => name.StartsWith("Patients/") && name.EndsWith(Patient.EXTENSION)), Is.Zero);
            Assert.That(entryNames.Count(name => name.StartsWith("Groups/") && name.EndsWith(Core.Data.Group.EXTENSION)), Is.Zero);
            Assert.That(entryNames.Count(name => name.StartsWith("Datasets/") && name.EndsWith(Dataset.EXTENSION)), Is.Zero);
            Assert.That(entryNames.Count(name => name.StartsWith("Visualizations/") && name.EndsWith(Visualization.EXTENSION)), Is.Zero);
        }

        [Test]
        public async Task SaveAsync_DuplicateEntityNames_AreStoredAsDistinctArchiveEntries()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateCompleteProject();
            Patient patient = project.Patients[0];
            Protocol protocol = SyntheticProjectFactory.CreateProtocol();
            Dataset duplicateNamedDataset = new(project.Datasets[0].Name, protocol, Array.Empty<DataInfo>(), "synthetic-dataset-duplicate-name-001");
            Visualization duplicateNamedVisualization = new(project.Visualizations[0].Name, new[] { patient }, Array.Empty<Column>(), "synthetic-visualization-duplicate-name-001");
            Core.Data.Group duplicateNamedGroup = new(project.Groups[0].Name, new[] { patient }, "synthetic-group-duplicate-name-001");
            project.AddDataset(duplicateNamedDataset);
            project.AddVisualization(duplicateNamedVisualization);
            project.AddGroup(duplicateNamedGroup);

            string archivePath = await SaveProject(temp, project);

            using ZipArchive zip = ZipFile.OpenRead(archivePath);
            string[] entryNames = zip.Entries.Select(entry => entry.FullName).ToArray();
            Assert.That(entryNames.Count(name => name.StartsWith("Groups/") && name.EndsWith(Core.Data.Group.EXTENSION)), Is.EqualTo(2));
            Assert.That(entryNames.Count(name => name.StartsWith("Datasets/") && name.EndsWith(Dataset.EXTENSION)), Is.EqualTo(2));
            Assert.That(entryNames.Count(name => name.StartsWith("Visualizations/") && name.EndsWith(Visualization.EXTENSION)), Is.EqualTo(2));
        }

        [Test]
        public async Task SaveAsync_UpdatesProjectSettingsVersion()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project project = SyntheticProjectFactory.CreateMinimalProject();
            project.Preferences.Version = "old-test-version";
            string archivePath = await SaveProject(temp, project);

            ProjectInfo info = new(archivePath);

            Assert.That(info.Settings.Version, Is.EqualTo(ApplicationState.Version));
        }

        private static async Task<Exception> CaptureExceptionAsync(Func<Task> action)
        {
            try
            {
                await action();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private static async Task AssertCorruptedProjectEntryThrows(string entryName, Type expectedExceptionType)
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateCompleteProject();
            string archivePath = await SaveProject(temp, source);
            ReplaceZipEntryContent(archivePath, entryName, "{ this is not valid json");
            ProjectInfo info = new(archivePath);
            Project loaded = new(source.Name, new ProjectPreferences("load-placeholder"));
            ApplicationState.LoadedProject = loaded;
            ApplicationState.LoadedProjectLocation = Path.GetDirectoryName(archivePath);

            Exception exception = await CaptureExceptionAsync(async () => await loaded.LoadAsync(info, NoProgress, CancellationToken.None));

            Assert.That(exception, Is.TypeOf(expectedExceptionType));
            Assert.That(exception.InnerException, Is.Not.Null);
            Assert.That(Directory.Exists(ApplicationState.ExtractProjectFolder), Is.False);
            AssertArchiveIsUnlocked(archivePath);
        }

        private static async Task AssertLoadCancellationFromProgress(string cancellationStep)
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateCompleteProject();
            string archivePath = await SaveProject(temp, source);
            ProjectInfo info = new(archivePath);
            Project loaded = new(source.Name, new ProjectPreferences("load-placeholder"));
            ApplicationState.LoadedProject = loaded;
            ApplicationState.LoadedProjectLocation = Path.GetDirectoryName(archivePath);
            DatabaseManager.Database.SetProtocols(new[] { SyntheticProjectFactory.CreateProtocol() });
            using CancellationTokenSource cancellation = new();

            void CancelOnStep(float progress, float duration, LoadingText text)
            {
                if (text.ToString().StartsWith(cancellationStep))
                {
                    cancellation.Cancel();
                }
            }

            Exception exception = await CaptureExceptionAsync(async () => await loaded.LoadAsync(info, CancelOnStep, cancellation.Token));

            Assert.That(exception, Is.TypeOf<OperationCanceledException>());
            Assert.That(Directory.Exists(ApplicationState.ExtractProjectFolder), Is.False);
            AssertArchiveIsUnlocked(archivePath);
        }

        private static async Task<string> SaveProject(TempDirectoryScope temp, Project project, string directoryName = "saved")
        {
            string saveDirectory = temp.GetPath(directoryName);
            Directory.CreateDirectory(saveDirectory);
            ApplicationState.LoadedProject = project;
            ApplicationState.LoadedProjectLocation = saveDirectory;

            await project.SaveAsync(saveDirectory, NoProgress, CancellationToken.None);

            return Path.Combine(saveDirectory, project.FileName);
        }

        private static async Task<string> SaveProjectToDirectory(string saveDirectory, Project project)
        {
            ApplicationState.LoadedProject = project;
            ApplicationState.LoadedProjectLocation = saveDirectory;

            await project.SaveAsync(saveDirectory, NoProgress, CancellationToken.None);

            return Path.Combine(saveDirectory, project.FileName);
        }

        private static async Task<Project> LoadProject(TempDirectoryScope temp, string archivePath, params Protocol[] databaseProtocols)
        {
            ProjectInfo info = new(archivePath);
            Project loaded = new(info.Name, new ProjectPreferences("load-placeholder"));
            ApplicationState.LoadedProject = loaded;
            ApplicationState.LoadedProjectLocation = Path.GetDirectoryName(archivePath);

            DatabaseManager.Database.SetProtocols(databaseProtocols.Length > 0 ? databaseProtocols : new[] { SyntheticProjectFactory.CreateProtocol() });
            await loaded.LoadAsync(info, NoProgress, CancellationToken.None);
            AssertArchiveIsUnlocked(archivePath);
            return loaded;
        }

        private static void AssertArchiveIsUnlocked(string archivePath)
        {
            using FileStream stream = new(archivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.That(stream.CanRead, Is.True);
        }

        private static void AssertArchiveContainsProjectDirectories(string archivePath)
        {
            using ZipArchive zip = ZipFile.OpenRead(archivePath);
            string[] entryNames = zip.Entries.Select(entry => entry.FullName).ToArray();
            Assert.That(entryNames, Does.Contain("Patients/"));
            Assert.That(entryNames, Does.Contain("Groups/"));
            Assert.That(entryNames, Does.Contain("Datasets/"));
            Assert.That(entryNames, Does.Contain("Visualizations/"));
            Assert.That(entryNames.Any(name => name.EndsWith(ProjectPreferences.EXTENSION)), Is.True);
        }

        private static ProjectInfo CreateProjectInfo(Project project, string archivePath)
        {
            return new ProjectInfo
            {
                Name = project.Name,
                Path = archivePath,
                Patients = project.Patients.Count,
                Groups = project.Groups.Count,
                Datasets = project.Datasets.Count,
                Visualizations = project.Visualizations.Count
            };
        }

        private static void AddZipEntryContent(string archivePath, string entryName, string content)
        {
            using ZipArchive zip = ZipFile.Open(archivePath, ZipArchiveMode.Update);
            ZipArchiveEntry entry = zip.CreateEntry(entryName);
            using StreamWriter writer = new(entry.Open());
            writer.Write(content);
        }

        private static void ReplaceZipEntryContent(string archivePath, string entryName, string content)
        {
            using ZipArchive zip = ZipFile.Open(archivePath, ZipArchiveMode.Update);
            zip.GetEntry(entryName)?.Delete();
            ZipArchiveEntry entry = zip.CreateEntry(entryName);
            using StreamWriter writer = new(entry.Open());
            writer.Write(content);
        }

        private static void DeleteZipEntry(string archivePath, string entryName)
        {
            using ZipArchive zip = ZipFile.Open(archivePath, ZipArchiveMode.Update);
            zip.GetEntry(entryName)?.Delete();
        }

        private static void AssertArchiveContainsEntry(string archivePath, string entryName)
        {
            using ZipArchive zip = ZipFile.OpenRead(archivePath);
            Assert.That(zip.Entries.Select(entry => entry.FullName), Does.Contain(entryName));
        }

        private static void AssertArchiveDoesNotContainAbsolutePath(string archivePath, string absolutePath)
        {
            string slashPath = absolutePath.Replace('\\', '/');
            string escapedPath = absolutePath.Replace("\\", "\\\\");

            using ZipArchive zip = ZipFile.OpenRead(archivePath);
            foreach (ZipArchiveEntry entry in zip.Entries.Where(entry => !entry.FullName.EndsWith("/")))
            {
                using StreamReader reader = new(entry.Open());
                string content = reader.ReadToEnd();
                Assert.That(content, Does.Not.Contain(absolutePath), entry.FullName);
                Assert.That(content, Does.Not.Contain(escapedPath), entry.FullName);
                Assert.That(content.Replace('\\', '/'), Does.Not.Contain(slashPath), entry.FullName);
            }
        }

        private static void NoProgress(float progress, float duration, LoadingText text)
        {
        }
    }
}
