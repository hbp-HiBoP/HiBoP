using System;
using System.IO;
using HBP.Core.Database;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class LoadingDiagnosticsTests
    {
        [TearDown]
        public void TearDown()
        {
            LoadingDiagnostics.SetOutputDirectoryForTests(null);
        }

        [Test]
        public void SuccessfulSession_WritesAggregateSummaryWithoutSourcePaths()
        {
            using TempDirectoryScope temp = new();
            string sourcePath = Path.Combine(temp.Path, "sensitive-patient-name.json");
            LoadingDiagnostics.SetOutputDirectoryForTests(temp.Path);

            using (LoadingDiagnostics.SessionScope session = LoadingDiagnostics.BeginSession(LoadingDiagnostics.Operation.Database))
            {
                using (LoadingDiagnostics.BeginPhase(
                    LoadingDiagnostics.Phase.DatabasePatientsRead,
                    fileCount: 1,
                    byteCount: 123,
                    objectCount: 1,
                    concurrency: 4))
                {
                    LoadingDiagnostics.FileExists(sourcePath);
                    LoadingDiagnostics.RecordTagLookups(3);
                    LoadingDiagnostics.RecordReferenceLookups(2);
                    LoadingDiagnostics.RecordObjects("Patient", 1);
                }
                session.MarkSucceeded();
            }

            string json = File.ReadAllText(LoadingDiagnostics.LastSummaryPath);
            JObject summary = JObject.Parse(json);

            Assert.That((string)summary["status"], Is.EqualTo("Succeeded"));
            Assert.That((string)summary["runtime"], Is.EqualTo("Editor"));
            Assert.That((string)summary["operation"], Is.EqualTo("Database"));
            Assert.That(json, Does.Contain("Loading.Database.Patients.Read"));
            Assert.That(json, Does.Contain("\"files\": 1"));
            Assert.That(json, Does.Contain("\"bytes\": 123"));
            Assert.That(json, Does.Not.Contain("sensitive-patient-name"));
            Assert.That(json, Does.Not.Contain(sourcePath));
        }

        [Test]
        public void FailedAndCanceledSessions_ArePersistedWithoutExceptionMessages()
        {
            using TempDirectoryScope temp = new();
            LoadingDiagnostics.SetOutputDirectoryForTests(temp.Path);

            using (LoadingDiagnostics.SessionScope session = LoadingDiagnostics.BeginSession(LoadingDiagnostics.Operation.Project))
            {
                using (LoadingDiagnostics.BeginPhase(LoadingDiagnostics.Phase.ProjectArchiveRead))
                {
                }
                session.MarkFailed(new InvalidDataException("sensitive-patient-name"));
            }

            string failedJson = File.ReadAllText(LoadingDiagnostics.LastSummaryPath);
            Assert.That(failedJson, Does.Contain("\"status\": \"Failed\""));
            Assert.That(failedJson, Does.Contain(typeof(InvalidDataException).FullName));
            Assert.That(failedJson, Does.Not.Contain("sensitive-patient-name"));

            using (LoadingDiagnostics.SessionScope session = LoadingDiagnostics.BeginSession(LoadingDiagnostics.Operation.Project))
            {
                using (LoadingDiagnostics.BeginPhase(LoadingDiagnostics.Phase.ProjectPatientsDeserialize))
                {
                }
                session.MarkCanceled();
            }

            string canceledJson = File.ReadAllText(LoadingDiagnostics.LastSummaryPath);
            Assert.That(canceledJson, Does.Contain("\"status\": \"Canceled\""));
            Assert.That(canceledJson, Does.Contain("Loading.Project.Patients.Deserialize"));
        }

        [Test]
        public void ProfiledJsonLoad_PreservesDeserializedValues()
        {
            using TempDirectoryScope temp = new();
            string fixturePath = TestPathUtility.FixturePath("Serialization", "legacy_database_settings_namespace.json");
            GlobalDatabaseSettings expected = ClassLoaderSaver.LoadFromJson<GlobalDatabaseSettings>(fixturePath);
            LoadingDiagnostics.SetOutputDirectoryForTests(temp.Path);

            GlobalDatabaseSettings actual;
            using (LoadingDiagnostics.SessionScope session = LoadingDiagnostics.BeginSession(LoadingDiagnostics.Operation.Database))
            {
                actual = ClassLoaderSaver.LoadFromJson<GlobalDatabaseSettings>(
                    fixturePath,
                    LoadingDiagnostics.Phase.DatabaseSettings,
                    LoadingDiagnostics.Phase.DatabaseSettings);
                session.MarkSucceeded();
            }

            Assert.That(actual.ID, Is.EqualTo(expected.ID));
            Assert.That(actual.IsFirstUse, Is.EqualTo(expected.IsFirstUse));
            Assert.That(actual.Workspaces.Count, Is.EqualTo(expected.Workspaces.Count));
            Assert.That(actual.SelectedWorkspace?.ID, Is.EqualTo(expected.SelectedWorkspace?.ID));
        }
    }
}
