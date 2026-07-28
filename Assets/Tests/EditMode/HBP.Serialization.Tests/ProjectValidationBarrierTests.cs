using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HBP.Tests.Serialization
{
    public class ProjectValidationBarrierTests
    {
        [Test]
        public async Task ProjectMutation_RestartsValidationInBackground()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope applicationState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            Project project = SyntheticProjectFactory.CreateMinimalProject();

            await project.EnsureProjectValidatedAsync(NoProgress);
            SharedLoadingOperation<Project> firstOperation = project.CurrentLoadingOperation;

            Assert.That(project.NeedsValidationWait, Is.False);
            project.SetDatasets(Array.Empty<Dataset>());
            SharedLoadingOperation<Project> secondOperation = project.CurrentLoadingOperation;
            Assert.That(secondOperation, Is.Not.Null);
            Assert.That(secondOperation.Generation, Is.GreaterThan(firstOperation.Generation));

            await secondOperation.Validated;
            Assert.That(project.NeedsValidationWait, Is.False);
        }

        [Test]
        public async Task SaveAsync_WaitsForProjectValidation()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope applicationState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            ValidationGateProject project = new();
            string saveDirectory = temp.GetPath("validation-save");
            Directory.CreateDirectory(saveDirectory);
            ApplicationState.LoadedProject = project;
            ApplicationState.LoadedProjectLocation = saveDirectory;

            Task save = project.SaveAsync(saveDirectory, NoProgress, CancellationToken.None).AsTask();
            await project.ValidationStarted.Task;

            Assert.That(File.Exists(Path.Combine(saveDirectory, project.FileName)), Is.False);

            project.ReleaseValidation.SetResult(true);
            await save;

            Assert.That(project.ValidationCalls, Is.EqualTo(1));
            Assert.That(File.Exists(Path.Combine(saveDirectory, project.FileName)), Is.True);
        }

        [Test]
        public async Task Module3DLoadAsync_UsesTheCentralProjectBarrier()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope applicationState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            ValidationGateProject project = new();
            ApplicationState.LoadedProject = project;

            Task load = Module3DMain.LoadAsync(Array.Empty<Visualization>(), NoProgress, CancellationToken.None).AsTask();
            await project.ValidationStarted.Task;

            Assert.That(load.IsCompleted, Is.False);
            project.ReleaseValidation.SetResult(true);
            await load;

            Assert.That(project.ValidationCalls, Is.EqualTo(1));
        }

        private static readonly Action<float, float, LoadingText> NoProgress = (_, _, _) => { };

        private sealed class ValidationGateProject : Project
        {
            public TaskCompletionSource<bool> ValidationStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> ReleaseValidation { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public int ValidationCalls { get; private set; }

            public ValidationGateProject() : base("validation-gate", new ProjectPreferences("test-version"))
            {
            }

            public override async UniTask EnsureProjectValidatedAsync(Action<float, float, LoadingText> updateProgress, CancellationToken token = default)
            {
                ValidationCalls++;
                ValidationStarted.TrySetResult(true);
                await ReleaseValidation.Task;
                token.ThrowIfCancellationRequested();
            }
        }
    }
}
