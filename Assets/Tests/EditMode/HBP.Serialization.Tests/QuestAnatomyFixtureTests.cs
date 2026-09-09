using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class QuestAnatomyFixtureTests
    {
        private static string RepositoryRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [Test]
        public async Task ProjectArchive_LoadsOneAnatomicColumnWithoutPatientData()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            string sources = Path.Combine(RepositoryRoot, "Docs/dev/quest-autonomous/fixtures/mni-anatomy");
            string archivePath = Path.Combine(temp.Path, "quest-mni-anatomy.hibop");
            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                foreach (string directory in new[] { "Patients/", "Groups/", "Datasets/", "Visualizations/" }) archive.CreateEntry(directory);
                archive.CreateEntryFromFile(Path.Combine(sources, "quest-mni-anatomy.settings"), "quest-mni-anatomy.settings");
                archive.CreateEntryFromFile(Path.Combine(sources, "MNI Anatomy.visualization"), "Visualizations/MNI Anatomy.visualization");
            }

            ProjectInfo info = new(archivePath);
            Project project = new(info.Name, new ProjectPreferences("fixture-placeholder"));
            ApplicationState.LoadedProject = project;
            ApplicationState.LoadedProjectLocation = temp.Path;
            await project.LoadAsync(info, NoProgress, CancellationToken.None);
            await project.CurrentLoadingOperation.Validated;
            Assert.That(project.StructuralRecoveryReport.HasIssues, Is.False);
            Assert.That(project.Patients, Is.Empty);
            Assert.That(project.Groups, Is.Empty);
            Assert.That(project.Datasets, Is.Empty);
            Assert.That(project.Preferences.ID, Is.EqualTo("quest-001-project"));
            Visualization visualization = project.Visualizations.Single();
            Assert.That(visualization.ID, Is.EqualTo("quest-001-visualization"));
            Assert.That(visualization.Name, Is.EqualTo("MNI Anatomy"));
            Assert.That(visualization.Patients, Is.Empty);
            Assert.That(visualization.IsVisualizable, Is.True);
            AnatomicColumn column = (AnatomicColumn)visualization.Columns.Single();
            Assert.That(column.ID, Is.EqualTo("quest-001-column"));
            Assert.That(column.BaseConfiguration.ID, Is.EqualTo("quest-001-base-configuration"));
            Assert.That(column.AnatomicConfiguration.ID, Is.EqualTo("quest-001-anatomic-configuration"));
            Assert.That(column.BaseConfiguration.ConfigurationBySite, Is.Empty);
            VisualizationConfiguration configuration = visualization.Configuration;
            Assert.That(configuration.ID, Is.EqualTo("quest-001-visualization-configuration"));
            Assert.That(configuration.MeshName, Is.EqualTo("MNI Grey matter"));
            Assert.That(configuration.MRIName, Is.EqualTo("MNI"));
            Assert.That(configuration.MeshPart, Is.EqualTo(MeshPart.Both));
            Assert.That(configuration.SurfaceRepresentation, Is.EqualTo(SurfaceRepresentation.Anatomical));
            Assert.That(configuration.TransparentBrain, Is.False);
            Assert.That(configuration.Cuts, Is.Empty);
            Assert.That(configuration.Views, Is.Empty);
            Assert.That(configuration.RegionsOfInterest, Is.Empty);
        }

        [Test]
        public async Task RepositoryMNI_LoadsBothCompleteHemispheresThroughDesktopLoader()
        {
            MNIObjects mni = new();
            try
            {
                // Await the real loader directly: public Load is fire-and-forget.
                MethodInfo load = typeof(MNIObjects).GetMethod("LoadDataAsync", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(load, Is.Not.Null);
                await (UniTask)load.Invoke(mni, new object[]
                {
                    Path.Combine(RepositoryRoot, "Assets/Data/IRM"),
                    Path.Combine(RepositoryRoot, "Assets/Data/Meshes")
                });
                await UniTask.SwitchToMainThread();
                Assert.That(mni.MRI.Volume.IsLoaded, Is.True);
                Assert.That(mni.GreyMatter.Name, Is.EqualTo("MNI Grey matter"));
                foreach (LeftRightMesh3D mesh in new[] { mni.GreyMatter, mni.WhiteMatter })
                {
                    Assert.That(mesh.Left.IsLoaded, Is.True);
                    Assert.That(mesh.Right.IsLoaded, Is.True);
                    Assert.That(mesh.Left.NumberOfVertices, Is.GreaterThan(10000));
                    Assert.That(mesh.Right.NumberOfVertices, Is.GreaterThan(10000));
                    Assert.That(mesh.Both.NumberOfVertices, Is.EqualTo(mesh.Left.NumberOfVertices + mesh.Right.NumberOfVertices));
                    Assert.That(mesh.Both.NumberOfTriangles, Is.EqualTo(mesh.Left.NumberOfTriangles + mesh.Right.NumberOfTriangles));
                    TestContext.Out.WriteLine($"{mesh.Name}: L={mesh.Left.NumberOfVertices} vertices/{mesh.Left.NumberOfTriangles} triangles; R={mesh.Right.NumberOfVertices} vertices/{mesh.Right.NumberOfTriangles} triangles; Both={mesh.Both.NumberOfVertices} vertices/{mesh.Both.NumberOfTriangles} triangles");
                }
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                mni.Clean();
            }
        }

        [Test]
        public async Task ContactsArchive_LoadsStableSyntheticPatientsAndMniAssociations()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            string sources = Path.Combine(RepositoryRoot, "Docs/dev/quest-autonomous/fixtures/mni-contacts");
            string archivePath = Path.Combine(temp.Path, "quest-mni-contacts.hibop");
            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                foreach (string directory in new[] { "Patients/", "Groups/", "Datasets/", "Visualizations/" }) archive.CreateEntry(directory);
                foreach (string file in Directory.GetFiles(sources))
                {
                    string extension = Path.GetExtension(file);
                    string prefix = extension == ".patient" ? "Patients/" : extension == ".visualization" ? "Visualizations/" : "";
                    if (extension == ".patient" || extension == ".visualization" || extension == ".settings") archive.CreateEntryFromFile(file, prefix + Path.GetFileName(file));
                }
            }

            ProjectInfo info = new(archivePath);
            Project project = new(info.Name, new ProjectPreferences("fixture-placeholder"));
            ApplicationState.LoadedProject = project;
            ApplicationState.LoadedProjectLocation = temp.Path;
            await project.LoadAsync(info, NoProgress, CancellationToken.None);
            await project.CurrentLoadingOperation.Validated;
            Assert.That(project.StructuralRecoveryReport.HasIssues, Is.False);
            Assert.That(project.Patients.Count, Is.EqualTo(2));
            var visualization = project.Visualizations.Single();
            Assert.That(visualization.IsVisualizable, Is.True);
            Assert.That(visualization.Configuration.ImplantationName, Is.EqualTo("MNI"));
            Assert.That(visualization.Configuration.ShowAllSites, Is.True);
            Assert.That(visualization.Configuration.MeshName, Is.EqualTo("MNI Grey matter"));
            var expected = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(sources, "contacts.json")));
            var patients = visualization.Patients;
            for (int i = 0; i < 8; i++)
            {
                var row = expected["sites"][i];
                var patient = patients[i / 4];
                var site = patient.Sites[i % 4];
                Assert.That(patient.ID, Is.EqualTo((string)row["patientId"]));
                Assert.That(site.ID, Is.EqualTo((string)row["siteId"]));
                Assert.That(site.Name, Is.EqualTo((string)row["name"]));
                var coordinate = site.Coordinates.Single();
                Assert.That(coordinate.ReferenceSystem, Is.EqualTo("MNI"));
                Assert.That(new[] { coordinate.Position.x, coordinate.Position.y, coordinate.Position.z }, Is.EqualTo(row["nativeMillimeters"].Values<float>().ToArray()));
            }

            Assert.That(project.Datasets, Is.Empty);
        }

        private static void NoProgress(float progress, float duration, LoadingText text)
        {
        }
    }
}
