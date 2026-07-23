using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HBP.Tests.Serialization
{
    public class AssetReferenceValidatorTests
    {
        [TearDown]
        public void TearDown()
        {
            LoadingDiagnostics.SetOutputDirectoryForTests(null);
        }

        [Test]
        public async Task DeserializationPerformsNoFileIo_ThenValidationPreservesUsability()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            string meshPath = temp.GetPath("valid.gii");
            string missingMeshPath = temp.GetPath("missing.gii");
            string leftPath = temp.GetPath("left.gii");
            string rightPath = temp.GetPath("right.gii");
            string MRIPath = temp.GetPath("valid.nii");
            string missingMRIPath = temp.GetPath("missing.nii");
            string wrongExtensionPath = temp.GetPath("wrong.txt");
            File.WriteAllText(meshPath, "mesh");
            File.WriteAllText(leftPath, "left");
            File.WriteAllText(rightPath, "right");
            File.WriteAllText(MRIPath, "mri");
            File.WriteAllText(wrongExtensionPath, "wrong");

            Patient source = new(
                "validation",
                new BaseMesh[]
                {
                    new SingleMesh("valid", string.Empty, meshPath, string.Empty, "mesh-valid"),
                    new SingleMesh("duplicate", string.Empty, meshPath, string.Empty, "mesh-duplicate"),
                    new SingleMesh("missing", string.Empty, missingMeshPath, string.Empty, "mesh-missing"),
                    new SingleMesh("wrong", string.Empty, wrongExtensionPath, string.Empty, "mesh-wrong"),
                    new LeftRightMesh("pair", string.Empty, leftPath, rightPath, string.Empty, string.Empty, "mesh-pair")
                },
                new[]
                {
                    new MRI("valid", MRIPath, "mri-valid"),
                    new MRI("missing", missingMRIPath, "mri-missing"),
                    new MRI("wrong", wrongExtensionPath, "mri-wrong")
                },
                Array.Empty<Site>(),
                Array.Empty<BaseTagValue>(),
                "database",
                "patient-validation");
            string patientPath = temp.GetPath("patient.patient");
            Assert.That(ClassLoaderSaver.SaveToJSon(source, patientPath, true), Is.True);

            LoadingDiagnostics.SetOutputDirectoryForTests(temp.Path);
            Patient loaded;
            using (LoadingDiagnostics.SessionScope session =
                LoadingDiagnostics.BeginSession(LoadingDiagnostics.Operation.Database))
            {
                loaded = ClassLoaderSaver.LoadFromJson<Patient>(
                    patientPath,
                    LoadingDiagnostics.Phase.None,
                    LoadingDiagnostics.Phase.DatabasePatientsDeserialize);
                session.MarkSucceeded();
            }

            JObject deserializeSummary = JObject.Parse(File.ReadAllText(LoadingDiagnostics.LastSummaryPath));
            Assert.That(
                deserializeSummary["phases"].Sum(phase => (long)phase["fileExistsCalls"]),
                Is.Zero);
            Assert.That(loaded.Meshes.All(mesh => !mesh.WasUsable), Is.True);
            Assert.That(loaded.MRIs.All(MRI => !MRI.WasUsable), Is.True);

            using (LoadingDiagnostics.SessionScope session =
                LoadingDiagnostics.BeginSession(LoadingDiagnostics.Operation.Database))
            {
                using (LoadingDiagnostics.BeginPhase(
                    LoadingDiagnostics.Phase.DatabasePatientsValidateFiles,
                    objectCount: 1,
                    concurrency: 4))
                {
                    await new AssetReferenceValidator().ValidatePatientsAsync(
                        new[] { loaded },
                        4,
                        CancellationToken.None);
                }
                session.MarkSucceeded();
            }
            await UniTask.SwitchToMainThread();

            JObject validationSummary = JObject.Parse(File.ReadAllText(LoadingDiagnostics.LastSummaryPath));
            JToken validationPhase = validationSummary["phases"]
                .Single(phase => (string)phase["name"] == "Loading.Database.Patients.ValidateFiles");
            Assert.That((long)validationPhase["fileExistsCalls"], Is.EqualTo(7));

            foreach (BaseMesh mesh in loaded.Meshes)
            {
                bool validated = mesh.WasUsable;
                Assert.That(validated, Is.EqualTo(mesh.IsUsable), mesh.Name);
            }
            foreach (MRI MRI in loaded.MRIs)
            {
                bool validated = MRI.WasUsable;
                Assert.That(validated, Is.EqualTo(MRI.IsUsable), MRI.Name);
            }
        }

        [Test]
        public async Task ValidationExpandsAliasesAndProjectPathsOnce_WithBoundedConcurrency()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            PersistentDataManager.Aliases.SetAliases(
                new[]
                {
                    new Alias("[WIN]", @"C:\validation-root", "alias-win"),
                    new Alias("[LINUX]", "/mnt/validation-root", "alias-linux"),
                    new Alias("[NETWORK]", @"\\server\share", "alias-network")
                },
                false);

            SingleMesh windows = DeserializeSingleMesh("[WIN]/windows.gii", "windows");
            SingleMesh windowsDuplicate = DeserializeSingleMesh("[WIN]/windows.gii", "windows-duplicate");
            SingleMesh linux = DeserializeSingleMesh("[LINUX]/linux.gii", "linux");
            SingleMesh network = DeserializeSingleMesh("[NETWORK]/network.gii", "network");
            SingleMesh project = DeserializeSingleMesh("./project.gii", "project");
            Patient patient = new(
                "aliases",
                new BaseMesh[] { windows, windowsDuplicate, linux, network, project },
                Array.Empty<MRI>(),
                Array.Empty<Site>(),
                Array.Empty<BaseTagValue>(),
                "database",
                "patient-aliases");

            ConcurrentBag<string> checkedPaths = new();
            int active = 0;
            int maximumActive = 0;
            AssetReferenceValidator validator = new(path =>
            {
                checkedPaths.Add(path);
                int current = Interlocked.Increment(ref active);
                UpdateMaximum(ref maximumActive, current);
                Thread.Sleep(20);
                Interlocked.Decrement(ref active);
                return !path.Contains("network", StringComparison.OrdinalIgnoreCase);
            });

            await validator.ValidatePatientsAsync(new[] { patient }, 2, CancellationToken.None);
            await UniTask.SwitchToMainThread();

            Assert.That(checkedPaths, Has.Count.EqualTo(4));
            Assert.That(maximumActive, Is.LessThanOrEqualTo(2));
            Assert.That(checkedPaths, Does.Contain(@"C:\validation-root\windows.gii".StandardizeToEnvironement()));
            Assert.That(checkedPaths, Does.Contain(@"\mnt\validation-root\linux.gii".StandardizeToEnvironement()));
            Assert.That(checkedPaths, Does.Contain(@"\\server\share\network.gii".StandardizeToEnvironement()));
            Assert.That(checkedPaths, Does.Contain(
                (ApplicationState.ExtractProjectFolder + Path.DirectorySeparatorChar + "project.gii")
                    .StandardizeToEnvironement()));
            Assert.That(windows.WasUsable, Is.True);
            Assert.That(windowsDuplicate.WasUsable, Is.True);
            Assert.That(linux.WasUsable, Is.True);
            Assert.That(project.WasUsable, Is.True);
            Assert.That(network.WasUsable, Is.False);
        }

        [Test]
        public async Task CancellationDoesNotPublishPartialValidation()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            SingleMesh mesh = DeserializeSingleMesh(temp.GetPath("cancel.gii"), "cancel");
            Patient patient = new(
                "cancel",
                new BaseMesh[] { mesh },
                Array.Empty<MRI>(),
                Array.Empty<Site>(),
                Array.Empty<BaseTagValue>(),
                "database",
                "patient-cancel");
            using CancellationTokenSource cancellation = new();
            int calls = 0;
            AssetReferenceValidator validator = new(_ =>
            {
                Interlocked.Increment(ref calls);
                cancellation.Cancel();
                return true;
            });

            Exception exception = await CaptureExceptionAsync(() =>
                validator.ValidatePatientsAsync(new[] { patient }, 1, cancellation.Token).AsTask());
            await UniTask.SwitchToMainThread();

            Assert.That(exception, Is.TypeOf<OperationCanceledException>());
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(mesh.WasUsable, Is.False);
        }

        private static SingleMesh DeserializeSingleMesh(string path, string ID)
        {
            JObject json = new()
            {
                ["Name"] = ID,
                ["Path"] = path,
                ["MarsAtlasPath"] = string.Empty,
                ["Transformation"] = string.Empty,
                ["ID"] = ID
            };
            return ClassLoaderSaver.LoadFromJsonString<SingleMesh>(json.ToString(Formatting.None));
        }

        private static void UpdateMaximum(ref int maximum, int value)
        {
            int observed;
            do
            {
                observed = Volatile.Read(ref maximum);
                if (observed >= value)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref maximum, value, observed) != observed);
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
    }
}
