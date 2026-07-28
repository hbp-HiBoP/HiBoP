using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace HBP.Tests.Serialization
{
    public class SharedLoadingOperationTests
    {
        [Test]
        public async Task TwoConsumers_AwaitTheSameSingleFlight()
        {
            int executions = 0;
            TaskCompletionSource<bool> release = NewCompletionSource<bool>();
            SharedLoadingOperation<string> operation = new(7, async (progress, token) =>
            {
                executions++;
                await release.Task;
                return "result";
            });

            Task<string> first = operation.EnsureValidatedAsync();
            Task<string> second = operation.EnsureValidatedAsync();

            Assert.That(second, Is.SameAs(first));
            Assert.That(executions, Is.EqualTo(1));
            Assert.That(operation.Generation, Is.EqualTo(7));

            release.SetResult(true);

            Assert.That(await first, Is.EqualTo("result"));
            Assert.That(await second, Is.EqualTo("result"));
            Assert.That(operation.Result, Is.EqualTo("result"));
            Assert.That(operation.State, Is.EqualTo(LoadingOperationState.Validated));
        }

        [Test]
        public async Task LateProgressSubscriber_ImmediatelyReceivesTheLatestMonotoneProgress()
        {
            TaskCompletionSource<bool> release = NewCompletionSource<bool>();
            SharedLoadingOperation<string> operation = new(0, async (progress, token) =>
            {
                progress(0.7f, 0.2f, new LoadingText("seventy"));
                progress(0.3f, 0.2f, new LoadingText("stale"));
                await release.Task;
                return "result";
            });

            Task<string> completion = operation.EnsureValidatedAsync();
            List<LoadingProgress> received = new();
            using IDisposable subscription = operation.SubscribeProgress(received.Add);

            Assert.That(received, Has.Count.EqualTo(1));
            Assert.That(received[0].Value, Is.EqualTo(0.7f));
            Assert.That(received[0].Text.ToString(), Is.EqualTo("seventy"));

            release.SetResult(true);
            await completion;
        }

        [Test]
        public async Task Validation_KeepsReadyAvailableUntilTheSharedValidationCompletes()
        {
            TaskCompletionSource<bool> releaseValidation = NewCompletionSource<bool>();
            SharedLoadingOperation<string> operation = new(1, (progress, token) => UniTask.FromResult("graph"), async (result, progress, token) =>
            {
                await releaseValidation.Task;
                return true;
            });

            Task<string> validated = operation.EnsureValidatedAsync();
            string ready = await operation.Ready;

            Assert.That(ready, Is.EqualTo("graph"));
            Assert.That(validated.IsCompleted, Is.False);
            Assert.That(operation.State, Is.EqualTo(LoadingOperationState.Validating));

            releaseValidation.SetResult(true);

            Assert.That(await validated, Is.EqualTo("graph"));
            Assert.That(operation.State, Is.EqualTo(LoadingOperationState.ValidatedWithIssues));
        }

        [Test]
        public async Task TechnicalFailure_IsStoredAndPropagatedToEveryConsumer()
        {
            InvalidOperationException failure = new("validation failed");
            SharedLoadingOperation<string> operation = new(2, (progress, token) => UniTask.FromResult("graph"), (result, progress, token) => UniTask.FromException<bool>(failure));

            Task<string> first = operation.EnsureValidatedAsync();
            Task<string> second = operation.EnsureValidatedAsync();
            Exception firstException = await CaptureExceptionAsync(async () => await first);
            Exception secondException = await CaptureExceptionAsync(async () => await second);

            Assert.That(firstException, Is.SameAs(failure));
            Assert.That(secondException, Is.SameAs(failure));
            Assert.That(operation.Exception, Is.SameAs(failure));
            Assert.That(operation.Result, Is.EqualTo("graph"));
            Assert.That(operation.State, Is.EqualTo(LoadingOperationState.ValidationFailed));
        }

        [Test]
        public async Task CancellingTheOperation_CancelsBothSharedBarriers()
        {
            SharedLoadingOperation<string> operation = new(3, async (progress, token) =>
            {
                TaskCompletionSource<string> cancelled = NewCompletionSource<string>();
                using CancellationTokenRegistration registration = token.Register(() => cancelled.TrySetCanceled());
                return await cancelled.Task;
            });

            Task<string> ready = operation.EnsureReadyAsync();
            Task<string> validated = operation.EnsureValidatedAsync();
            operation.Cancel();

            Exception readyException = await CaptureExceptionAsync(async () => await ready);
            Exception validatedException = await CaptureExceptionAsync(async () => await validated);

            Assert.That(readyException, Is.InstanceOf<OperationCanceledException>());
            Assert.That(validatedException, Is.InstanceOf<OperationCanceledException>());
            Assert.That(operation.State, Is.EqualTo(LoadingOperationState.Cancelled));
        }

        [Test]
        public async Task CancellingOneConsumer_DoesNotCancelSharedValidation()
        {
            TaskCompletionSource<bool> releaseValidation = NewCompletionSource<bool>();
            SharedLoadingOperation<string> operation = new(4, (progress, token) => UniTask.FromResult("graph"), async (result, progress, token) =>
            {
                await releaseValidation.Task;
                return false;
            });
            Task<string> sharedValidation = operation.EnsureValidatedAsync();
            using CancellationTokenSource consumerCancellation = new();

            Task<string> cancelledConsumer = operation.EnsureValidatedAsync(consumerCancellation.Token);
            consumerCancellation.Cancel();
            Exception exception = await CaptureExceptionAsync(async () => await cancelledConsumer);

            Assert.That(exception, Is.TypeOf<OperationCanceledException>());
            Assert.That(operation.State, Is.EqualTo(LoadingOperationState.Validating));
            Assert.That(operation.CancellationToken.IsCancellationRequested, Is.False);

            releaseValidation.SetResult(true);
            Assert.That(await sharedValidation, Is.EqualTo("graph"));
            Assert.That(operation.State, Is.EqualTo(LoadingOperationState.Validated));
        }

        [Test]
        public async Task CancellingReadyConsumer_DoesNotCancelSharedLoad()
        {
            TaskCompletionSource<bool> releaseLoad = NewCompletionSource<bool>();
            SharedLoadingOperation<string> operation = new(5, async (progress, token) =>
            {
                await releaseLoad.Task;
                return "graph";
            });
            Task<string> sharedReady = operation.EnsureReadyAsync();
            using CancellationTokenSource consumerCancellation = new();

            Task<string> cancelledConsumer = operation.EnsureReadyAsync(consumerCancellation.Token);
            consumerCancellation.Cancel();
            Exception exception = await CaptureExceptionAsync(async () => await cancelledConsumer);

            Assert.That(exception, Is.TypeOf<OperationCanceledException>());
            Assert.That(operation.State, Is.EqualTo(LoadingOperationState.Loading));
            Assert.That(operation.CancellationToken.IsCancellationRequested, Is.False);

            releaseLoad.SetResult(true);
            Assert.That(await sharedReady, Is.EqualTo("graph"));
            Assert.That(operation.State, Is.EqualTo(LoadingOperationState.Validated));
        }

        [Test]
        public async Task ProjectLoad_TwoCallersShareTheCurrentOperation()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope applicationState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            Project source = SyntheticProjectFactory.CreateMinimalProject();
            string saveDirectory = temp.GetPath("project-single-flight");
            Directory.CreateDirectory(saveDirectory);
            ApplicationState.LoadedProject = source;
            ApplicationState.LoadedProjectLocation = saveDirectory;
            await source.SaveAsync(saveDirectory, NoProgress, CancellationToken.None);

            string archivePath = Path.Combine(saveDirectory, source.FileName);
            ProjectInfo info = new(archivePath);
            Project loaded = new(source.Name, new ProjectPreferences("load-placeholder"));
            ApplicationState.LoadedProject = loaded;
            ApplicationState.LoadedProjectLocation = saveDirectory;
            Task secondLoad = null;
            float secondProgress = -1;

            void AttachSecondCaller(float progress, float duration, LoadingText text)
            {
                if (secondLoad != null)
                {
                    return;
                }

                secondLoad = loaded.LoadAsync(info, (value, _, _) => secondProgress = value, CancellationToken.None).AsTask();
            }

            Task firstLoad = loaded.LoadAsync(info, AttachSecondCaller, CancellationToken.None).AsTask();
            SharedLoadingOperation<Project> operation = loaded.CurrentLoadingOperation;

            await firstLoad;
            await secondLoad;

            Assert.That(loaded.CurrentLoadingOperation, Is.SameAs(operation));
            Assert.That(operation.Generation, Is.EqualTo(1));
            Assert.That(operation.Result, Is.SameAs(loaded));
            await operation.Validated;
            Assert.That(operation.State, Is.EqualTo(LoadingOperationState.Validated));
            Assert.That(secondProgress, Is.EqualTo(1));
        }

        [Test]
        public async Task DatabaseLoad_TwoCallersShareTheCurrentOperation()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope applicationState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            GlobalDatabase database = DatabaseManager.Database;
            database.Settings.SetDefaultWorkspace();
            Task secondLoad = null;
            float secondProgress = -1;

            void AttachSecondCaller(float progress, float duration, LoadingText text)
            {
                if (secondLoad != null)
                {
                    return;
                }

                secondLoad = database.LoadDatabaseAsync((value, _, _) => secondProgress = value).AsTask();
            }

            Task firstLoad = database.LoadDatabaseAsync(AttachSecondCaller).AsTask();
            SharedLoadingOperation<GlobalDatabase> operation = database.CurrentLoadingOperation;

            await firstLoad;
            await secondLoad;

            Assert.That(database.CurrentLoadingOperation, Is.SameAs(operation));
            Assert.That(operation.Generation, Is.EqualTo(1));
            Assert.That(operation.Result, Is.SameAs(database));
            Assert.That(operation.State, Is.EqualTo(LoadingOperationState.Validated));
            Assert.That(database.IsLoaded, Is.True);
            Assert.That(secondProgress, Is.EqualTo(1));
        }

        [Test]
        public async Task DatabaseSilentStart_ReachesReadyWithoutVisibleLoadingInfrastructure()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope applicationState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            GlobalDatabase database = DatabaseManager.Database;
            database.Settings.SetDefaultWorkspace();

            await database.StartLoadingSilentlyAsync();
            SharedLoadingOperation<GlobalDatabase> operation = database.CurrentLoadingOperation;

            Assert.That(operation, Is.Not.Null);
            Assert.That(await operation.Ready, Is.SameAs(database));
            Assert.That(database.IsLoaded, Is.True);
            Assert.That(await operation.Validated, Is.SameAs(database));
            Assert.That(operation.State, Is.EqualTo(LoadingOperationState.Validated));
        }

        [Test]
        public async Task DatabaseWorkspaceSwitch_PublishesOnlyTheCurrentGeneration()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope applicationState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            GlobalDatabase database = DatabaseManager.Database;
            Workspace firstWorkspace = new("first", "workspace-first");
            Workspace secondWorkspace = new("second", "workspace-second");
            database.Settings.SetWorkspaces(new[] { firstWorkspace, secondWorkspace });
            database.Settings.SelectedWorkspace = firstWorkspace;

            for (int index = 0; index < 12; index++)
            {
                await SaveWorkspacePatientAsync(firstWorkspace, new Patient("first-" + index, Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), string.Empty, "first-patient-" + index));
            }

            Patient expectedPatient = new("second", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), string.Empty, "second-patient");
            await SaveWorkspacePatientAsync(secondWorkspace, expectedPatient);

            await database.StartLoadingSilentlyAsync();
            SharedLoadingOperation<GlobalDatabase> obsoleteOperation = database.CurrentLoadingOperation;

            database.Settings.SelectedWorkspace = secondWorkspace;
            await database.ReloadSelectedWorkspaceSilentlyAsync();
            SharedLoadingOperation<GlobalDatabase> currentOperation = database.CurrentLoadingOperation;
            await currentOperation.Validated;

            Assert.That(currentOperation, Is.Not.SameAs(obsoleteOperation));
            Assert.That(currentOperation.Generation, Is.GreaterThan(obsoleteOperation.Generation));
            Assert.That(database.IsLoaded, Is.True);
            Assert.That(database.Patients, Has.Count.EqualTo(1));
            Assert.That(database.Patients[0].ID, Is.EqualTo(expectedPatient.ID));
            Assert.That(database.Patients, Has.None.Matches<Patient>(patient => patient.ID.StartsWith("first-patient-")));
        }

        [Test]
        public async Task DatabaseBackgroundFailure_IsPresentedBeforeTheNextRequestRetries()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope applicationState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);
            GlobalDatabase database = DatabaseManager.Database;
            database.Settings.SetDefaultWorkspace();
            InvalidOperationException failure = new("background database failure");
            SharedLoadingOperation<GlobalDatabase> failedOperation = new(1, (progress, token) => UniTask.FromException<GlobalDatabase>(failure));
            await CaptureExceptionAsync(async () => await failedOperation.EnsureReadyAsync());
            SetPrivateField(database, "m_LoadingOperation", failedOperation);
            SetPrivateField(database, "m_LoadingWorkspaceID", database.Settings.SelectedWorkspace.ID);
            SetPrivateField(database, "m_LoadingGeneration", 1L);

            Exception presentedException = await CaptureExceptionAsync(() => database.EnsureDatabaseReadyAsync(NoProgress).AsTask());

            Assert.That(presentedException, Is.SameAs(failure));
            Assert.That(database.CurrentLoadingOperation, Is.SameAs(failedOperation));
            Assert.That(failedOperation.Exception, Is.SameAs(failure));

            await database.EnsureDatabaseReadyAsync(NoProgress);
            SharedLoadingOperation<GlobalDatabase> retryOperation = database.CurrentLoadingOperation;
            await retryOperation.Validated;

            Assert.That(retryOperation, Is.Not.SameAs(failedOperation));
            Assert.That(retryOperation.Generation, Is.EqualTo(2));
            Assert.That(database.IsLoaded, Is.True);
        }

        [Test]
        public void ForegroundLeases_AreReferenceCounted()
        {
            SharedLoadingOperation<int> operation = new(1, (progress, token) => UniTask.FromResult(1));

            Assert.That(operation.Priority, Is.EqualTo(LoadingWorkPriority.Background));
            IDisposable first = operation.AttachForeground();
            IDisposable second = operation.AttachForeground();
            Assert.That(operation.Priority, Is.EqualTo(LoadingWorkPriority.Foreground));

            first.Dispose();
            Assert.That(operation.Priority, Is.EqualTo(LoadingWorkPriority.Foreground));
            second.Dispose();
            Assert.That(operation.Priority, Is.EqualTo(LoadingWorkPriority.Background));
        }

        private static readonly Action<float, float, LoadingText> NoProgress = (_, _, _) => { };

        private static async UniTask SaveWorkspacePatientAsync(Workspace workspace, Patient patient)
        {
            string patientDirectory = Path.Combine(workspace.Path, "Patients");
            Directory.CreateDirectory(patientDirectory);
            await ClassLoaderSaver.SaveToJsonAsync(patient, Path.Combine(patientDirectory, patient.ID + Patient.EXTENSION), true);
        }

        private static TaskCompletionSource<T> NewCompletionSource<T>()
        {
            return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
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

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(target, value);
        }
    }
}
