using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;

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

        private static async Task<string> SaveProject(TempDirectoryScope temp, Project project)
        {
            string saveDirectory = temp.GetPath("saved");
            Directory.CreateDirectory(saveDirectory);
            ApplicationState.LoadedProject = project;
            ApplicationState.LoadedProjectLocation = saveDirectory;

            await project.SaveAsync(saveDirectory, NoProgress, CancellationToken.None);

            return Path.Combine(saveDirectory, project.FileName);
        }

        private static async Task<Project> LoadProject(TempDirectoryScope temp, string archivePath)
        {
            ProjectInfo info = new(archivePath);
            Project loaded = new(info.Name, new ProjectPreferences("load-placeholder"));
            ApplicationState.LoadedProject = loaded;
            ApplicationState.LoadedProjectLocation = Path.GetDirectoryName(archivePath);

            DatabaseManager.Database.SetProtocols(new[] { SyntheticProjectFactory.CreateProtocol() });
            await loaded.LoadAsync(info, NoProgress, CancellationToken.None);
            return loaded;
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

        private static void NoProgress(float progress, float duration, LoadingText text)
        {
        }
    }
}
