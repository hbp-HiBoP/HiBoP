using System;
using System.Diagnostics;
using System.IO;
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
    public class DatabaseUpdateAccessTests
    {
        [Test]
        public async Task UpdateReadsMetadataFromSelectedSourcesOnly()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            var database = DatabaseManager.Database;
            database.Settings.SetDefaultWorkspace();
            var selected = CreateBrainvisa(temp, "selected");
            var unselected = CreateBrainvisa(temp, "unselected");
            CreatePatient(selected, "selected-patient");
            CreatePatient(unselected, "previously-imported");
            database.SetDatabaseReferences(new[] { selected, unselected });
            await UpdateAsync(database, unselected);
            var lastUpdated = unselected.LastUpdated;
            unselected.Path = "invalid\0path";

            var report = await UpdateAsync(database, selected);

            Assert.That(report.AccessIssues, Is.Empty);
            Assert.That(report.AddedPatients.Select(patient => patient.ID), Is.EqualTo(new[] { "selected-patient" }));
            Assert.That(database.Patients.Select(patient => patient.ID), Is.EquivalentTo(new[] { "selected-patient", "previously-imported" }));
            Assert.That(database.DatabaseReferences, Has.Count.EqualTo(2));
            Assert.That(unselected.LastUpdated, Is.EqualTo(lastUpdated));
        }

        [TestCase("implantation")]
        [TestCase("t1mri")]
        [Platform(Include = "Win")]
        public async Task BrainvisaKeepsThePreviousPatientWhenASubfolderIsDenied(string subfolder)
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            var database = DatabaseManager.Database;
            database.Settings.SetDefaultWorkspace();
            var selected = CreateBrainvisa(temp, "brainvisa");
            var patientPath = CreatePatient(selected, "skipped-patient");
            File.WriteAllText(Path.Combine(patientPath, "implantation", "skipped-patient.pts"), "ptsfile\nA1 1 2 3\n");
            var deniedPath = Path.Combine(patientPath, subfolder);
            Directory.CreateDirectory(deniedPath);
            database.SetDatabaseReferences(new[] { selected });
            await UpdateAsync(database, selected);
            var lastUpdated = selected.LastUpdated;
            CreatePatient(selected, "available-patient");

            var report = await UpdateWithDeniedDirectoryAsync(database, selected, deniedPath);

            var issue = report.AccessIssues.Single();
            Assert.That(issue.SourceName, Is.EqualTo(selected.Name));
            Assert.That(issue.PatientID, Is.EqualTo("skipped-patient"));
            Assert.That(issue.FolderPath, Is.EqualTo(patientPath));
            Assert.That(report.AddedPatients.Select(patient => patient.ID), Is.EqualTo(new[] { "available-patient" }));
            Assert.That(report.RemovedPatients, Is.Empty);
            Assert.That(report.UpdatedPatients, Is.Empty);
            Assert.That(database.Patients.Single(patient => patient.ID == "skipped-patient").Sites.Single().Name, Is.EqualTo("A1"));
            Assert.That(selected.LastUpdated, Is.EqualTo(lastUpdated));
        }

        [TestCase("recording")]
        [TestCase("group")]
        [TestCase("source")]
        [Platform(Include = "Win")]
        public async Task LocalizerKeepsDataFromDeniedFoldersAndUpdatesReadablePatients(string deniedLevel)
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            var database = DatabaseManager.Database;
            database.Settings.SetDefaultWorkspace();
            var protocol = SyntheticProjectFactory.CreateProtocol();
            database.SetProtocols(new[] { protocol });
            var brainvisa = CreateBrainvisa(temp, "brainvisa");
            CreatePatient(brainvisa, "skipped-patient");
            CreatePatient(brainvisa, "available-patient");
            DatabaseReference localizer = new("localizer", DatabaseType.Localizer, temp.GetPath("localizer"), new LocalizerDatabaseParameters { IncludeRaw = true }, DateTime.MinValue);
            var blockedGroup = Path.Combine(localizer.Path, "blocked-group");
            var recordingPath = CreateLocalizerRecording(blockedGroup, "skipped-patient", "first", protocol.Name);
            CreateLocalizerRecording(Path.Combine(localizer.Path, "available-group"), "available-patient", "first", protocol.Name);
            database.SetDatabaseReferences(new[] { brainvisa, localizer });
            var selected = new[] { brainvisa, localizer };
            await UpdateAsync(database, selected);
            var previousData = database.DataInfos.OfType<PatientDataInfo>().ToDictionary(data => data.Patient.ID, data => data.ID);
            var lastUpdated = localizer.LastUpdated;
            CreateLocalizerRecording(blockedGroup, "skipped-patient", "second", protocol.Name);
            CreateLocalizerRecording(Path.Combine(localizer.Path, "available-group"), "available-patient", "second", protocol.Name);

            var deniedPath = deniedLevel == "recording" ? recordingPath : deniedLevel == "group" ? blockedGroup : localizer.Path;
            var report = await UpdateWithDeniedDirectoryAsync(database, localizer, deniedPath);

            Assert.That(report.HasChanges, Is.True);
            Assert.That(report.AccessIssues.Any(issue => issue.SourceName == localizer.Name && issue.PatientID == "skipped-patient"), Is.True);
            Assert.That(report.AccessIssues.Any(issue => issue.PatientID == null), Is.EqualTo(deniedLevel != "recording"));
            var dataByPatient = database.DataInfos.OfType<PatientDataInfo>().ToLookup(data => data.Patient.ID);
            Assert.That(dataByPatient["skipped-patient"].Single().ID, Is.EqualTo(previousData["skipped-patient"]));
            Assert.That(dataByPatient["available-patient"].Count(), Is.EqualTo(deniedLevel == "source" ? 1 : 2));
            Assert.That(dataByPatient["available-patient"].Any(data => data.ID == previousData["available-patient"]), Is.EqualTo(deniedLevel == "source"));
            Assert.That(localizer.LastUpdated, Is.EqualTo(lastUpdated));
        }

        private static async Task<DatabaseUpdateReport> UpdateAsync(GlobalDatabase database, params DatabaseReference[] selected)
        {
            var transaction = await database.UpdateDatabasesAsync(selected, NoProgress, CancellationToken.None);
            await database.SaveDatabaseUpdateAsync(transaction, NoProgress);
            return transaction.Report;
        }

        private static void NoProgress(float progress, float duration, LoadingText text) { }

        private static DatabaseReference CreateBrainvisa(TempDirectoryScope temp, string name)
        {
            return new DatabaseReference(name, DatabaseType.Brainvisa, temp.GetPath(name), new BrainvisaDatabaseParameters(), DateTime.MinValue);
        }

        private static string CreatePatient(DatabaseReference reference, string patientID)
        {
            var path = Path.Combine(reference.Path, patientID);
            Directory.CreateDirectory(Path.Combine(path, "implantation"));
            return path;
        }

        private static string CreateLocalizerRecording(string groupPath, string patientID, string session, string protocolName)
        {
            var sessionName = $"recording_{session}_task_{protocolName}";
            var sessionPath = Path.Combine(groupPath, patientID, sessionName);
            Directory.CreateDirectory(sessionPath);
            File.WriteAllText(Path.Combine(sessionPath, sessionName + ".eeg"), string.Empty);
            File.WriteAllText(Path.Combine(sessionPath, sessionName + ".pos"), string.Empty);
            return sessionPath;
        }

        private static async Task<DatabaseUpdateReport> UpdateWithDeniedDirectoryAsync(GlobalDatabase database, DatabaseReference reference, string directory)
        {
            await SetDirectoryReadDenialAsync(directory, true);
            try
            {
                Assert.Throws<UnauthorizedAccessException>(() => Directory.GetDirectories(directory));
                return await UpdateAsync(database, reference);
            }
            finally
            {
                await SetDirectoryReadDenialAsync(directory, false);
            }
        }

        private static async Task SetDirectoryReadDenialAsync(string directory, bool deny)
        {
            // Unity's .NET runtime does not implement the managed Windows ACL methods.
            var rule = deny ? "/deny *S-1-1-0:(RD)" : "/remove:d *S-1-1-0";
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "icacls.exe"), $"\"{directory}\" {rule}")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };
            TaskCompletionSource<int> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            process.Exited += (_, _) => completion.TrySetResult(process.ExitCode);
            Assert.That(process.Start(), Is.True);
            Assert.That(await completion.Task, Is.Zero);
        }
    }
}
