using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.UI.Tools;
using NUnit.Framework;

namespace HBP.Tests.ProjectWorkflow
{
    public class ProjectWorkflowServiceTests
    {
        [Test]
        public async Task LoadProjectAsync_WithSettingsLoadException_LogsAndDoesNotChangeApplicationState()
        {
            FakeRuntime runtime = new();
            Project previousProject = new("previous", new ProjectPreferences("test-version", "previous-id"));
            runtime.LoadedProject = previousProject;
            runtime.LoadedProjectLocation = "previous-location";
            ProjectInfo info = CreateProjectInfoWithPath(Path.Combine(CreateTempFolder(), "broken.hibop"));
            InvalidDataException settingsException = new("settings are unreadable");
            SetSettingsLoadException(info, settingsException);
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowResult result = await service.LoadProjectAsync(info);

            Assert.That(result.Status, Is.EqualTo(ProjectWorkflowStatus.Failed));
            Assert.That(runtime.LoggedExceptions, Is.EquivalentTo(new[] { settingsException }));
            Assert.That(runtime.ShowErrorCalls, Is.EqualTo(1));
            Assert.That(runtime.ClearDataCalls, Is.Zero);
            Assert.That(runtime.LoadProjectCalls, Is.Zero);
            Assert.That(runtime.LoadedProject, Is.SameAs(previousProject));
            Assert.That(runtime.LoadedProjectLocation, Is.EqualTo("previous-location"));
        }

        [Test]
        public async Task LoadProjectAsync_OnSuccess_ClearsDataSetsLocationAndInteractables()
        {
            string folder = CreateTempFolder();
            FakeRuntime runtime = new()
            {
                LoadedProject = new Project("previous", new ProjectPreferences("test-version", "previous-id")),
                LoadedProjectLocation = "previous-location"
            };
            ProjectInfo info = CreateProjectInfoWithPath(Path.Combine(folder, "project.hibop"));
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowResult result = await service.LoadProjectAsync(info);

            Assert.That(result.Success, Is.True);
            Assert.That(runtime.ClearDataCalls, Is.EqualTo(1));
            Assert.That(runtime.LoadProjectCalls, Is.EqualTo(1));
            Assert.That(runtime.SetInteractablesCalls, Is.EqualTo(1));
            Assert.That(runtime.CheckProjectIDsCalls, Is.EqualTo(1));
            Assert.That(runtime.LoadedProjectDuringLoad.Name, Is.EqualTo("previous"));
            Assert.That(runtime.LoadedProjectLocationDuringLoad, Is.EqualTo("previous-location"));
            Assert.That(runtime.LoadedProject, Is.SameAs(runtime.LoadProjectArgument));
            Assert.That(runtime.LoadedProjectLocation, Is.EqualTo(folder));
        }

        [Test]
        public async Task LoadProjectAsync_OnFailure_RestoresPreviousProjectAndLocation()
        {
            FakeRuntime runtime = CreateRuntimeWithLoadedProject();
            InvalidOperationException failure = new("load failed");
            runtime.LoadProjectException = failure;
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowResult result = await service.LoadProjectAsync(CreateProjectInfoWithPath(Path.Combine(CreateTempFolder(), "project.hibop")));

            Assert.That(result.Status, Is.EqualTo(ProjectWorkflowStatus.Failed));
            Assert.That(runtime.LoadedProject.Name, Is.EqualTo("previous"));
            Assert.That(runtime.LoadedProjectLocation, Is.EqualTo("previous-location"));
            Assert.That(runtime.LoggedExceptions, Is.EquivalentTo(new[] { failure }));
            Assert.That(runtime.ShowErrorCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task LoadProjectAsync_OnCancellation_RestoresPreviousProjectAndLocation()
        {
            FakeRuntime runtime = CreateRuntimeWithLoadedProject();
            runtime.LoadProjectException = new OperationCanceledException();
            Project previousProject = runtime.LoadedProject;
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowResult result = await service.LoadProjectAsync(CreateProjectInfoWithPath(Path.Combine(CreateTempFolder(), "project.hibop")));

            Assert.That(result.Status, Is.EqualTo(ProjectWorkflowStatus.Cancelled));
            Assert.That(runtime.LoadedProject, Is.SameAs(previousProject));
            Assert.That(runtime.LoadedProjectLocation, Is.EqualTo("previous-location"));
            Assert.That(runtime.LoggedExceptions, Is.Empty);
        }

        [Test]
        public async Task SaveProjectAsync_SavesModuleConfigurationsBeforeProjectSave()
        {
            FakeRuntime runtime = CreateRuntimeWithLoadedProject();
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowResult result = await service.SaveProjectAsync("save-folder");

            Assert.That(result.Success, Is.True);
            Assert.That(runtime.Calls, Is.EqualTo(new[] { "SaveModuleConfigurations", "SaveProject" }));
        }

        [Test]
        public async Task SaveProjectAsync_UsesCurrentLoadedProjectLocationByDefault()
        {
            FakeRuntime runtime = CreateRuntimeWithLoadedProject();
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowResult result = await service.SaveProjectAsync();

            Assert.That(result.Success, Is.True);
            Assert.That(runtime.SavedPath, Is.EqualTo("previous-location"));
        }

        [Test]
        public async Task CreateNewProjectAsync_InvalidFolder_DoesNotChangeState()
        {
            FakeRuntime runtime = CreateRuntimeWithLoadedProject();
            Project previousProject = runtime.LoadedProject;
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowResult result = await service.CreateNewProjectAsync("new-project", Path.Combine(CreateTempFolder(), "missing"), true);

            Assert.That(result.Status, Is.EqualTo(ProjectWorkflowStatus.Invalid));
            Assert.That(runtime.LoadedProject, Is.SameAs(previousProject));
            Assert.That(runtime.LoadedProjectLocation, Is.EqualTo("previous-location"));
            Assert.That(runtime.SaveProjectCalls, Is.Zero);
        }

        [Test]
        public async Task CreateNewProjectAsync_ExistingArchiveAndCancel_DoesNotChangeState()
        {
            string folder = CreateTempFolder();
            File.WriteAllText(Path.Combine(folder, "new-project.hibop"), string.Empty);
            FakeRuntime runtime = CreateRuntimeWithLoadedProject();
            Project previousProject = runtime.LoadedProject;
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowResult result = await service.CreateNewProjectAsync("new-project", folder, false);

            Assert.That(result.Status, Is.EqualTo(ProjectWorkflowStatus.Cancelled));
            Assert.That(runtime.LoadedProject, Is.SameAs(previousProject));
            Assert.That(runtime.LoadedProjectLocation, Is.EqualTo("previous-location"));
            Assert.That(runtime.SaveProjectCalls, Is.Zero);
        }

        [Test]
        public async Task CreateNewProjectAsync_ExistingArchiveAndConfirm_ReplacesStateAndSaves()
        {
            string folder = CreateTempFolder();
            File.WriteAllText(Path.Combine(folder, "new-project.hibop"), string.Empty);
            FakeRuntime runtime = CreateRuntimeWithLoadedProject();
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowResult result = await service.CreateNewProjectAsync("new-project", folder, true);

            Assert.That(result.Success, Is.True);
            Assert.That(runtime.LoadedProject.Name, Is.EqualTo("new-project"));
            Assert.That(runtime.LoadedProjectLocation, Is.EqualTo(folder));
            Assert.That(runtime.SavedPath, Is.EqualTo(folder));
            Assert.That(runtime.SetInteractablesCalls, Is.EqualTo(1));
        }

        [Test]
        public async Task SaveProjectAsAsync_ExistingArchiveAndCancel_DoesNotRenameProject()
        {
            string folder = CreateTempFolder();
            File.WriteAllText(Path.Combine(folder, "renamed.hibop"), string.Empty);
            FakeRuntime runtime = CreateRuntimeWithLoadedProject();
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowResult result = await service.SaveProjectAsAsync("renamed", folder, false);

            Assert.That(result.Status, Is.EqualTo(ProjectWorkflowStatus.Cancelled));
            Assert.That(runtime.LoadedProject.Name, Is.EqualTo("previous"));
            Assert.That(runtime.SaveProjectCalls, Is.Zero);
        }

        [Test]
        public async Task SaveProjectAsAsync_Confirm_ClonesPreferencesRenamesProjectAndSaves()
        {
            string folder = CreateTempFolder();
            FakeRuntime runtime = CreateRuntimeWithLoadedProject();
            ProjectPreferences previousPreferences = runtime.LoadedProject.Preferences;
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowResult result = await service.SaveProjectAsAsync("renamed", folder, true);

            Assert.That(result.Success, Is.True);
            Assert.That(runtime.LoadedProject.Name, Is.EqualTo("renamed"));
            Assert.That(runtime.LoadedProject.Preferences, Is.Not.SameAs(previousPreferences));
            Assert.That(runtime.LoadedProject.Preferences.ID, Is.EqualTo(previousPreferences.ID));
            Assert.That(runtime.SavedPath, Is.EqualTo(folder));
        }

        [Test]
        public async Task OpenProjectAsync_WithOpenedScenesAndCancel_DoesNotRemoveScenesOrLoad()
        {
            FakeRuntime runtime = CreateRuntimeWithLoadedProject();
            runtime.HasOpenedScenesForLoadedProject = true;
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowResult result = await service.OpenProjectAsync(CreateProjectInfoWithPath(Path.Combine(CreateTempFolder(), "project.hibop")), false);

            Assert.That(result.Status, Is.EqualTo(ProjectWorkflowStatus.Cancelled));
            Assert.That(runtime.RemoveAllScenesCalls, Is.Zero);
            Assert.That(runtime.CloseAllWindowsCalls, Is.Zero);
            Assert.That(runtime.LoadProjectCalls, Is.Zero);
        }

        [Test]
        public async Task OpenProjectAsync_WithOpenedScenesAndConfirm_RemovesScenesClosesWindowsAndLoads()
        {
            FakeRuntime runtime = CreateRuntimeWithLoadedProject();
            runtime.HasOpenedScenesForLoadedProject = true;
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowResult result = await service.OpenProjectAsync(CreateProjectInfoWithPath(Path.Combine(CreateTempFolder(), "project.hibop")), true);

            Assert.That(result.Success, Is.True);
            Assert.That(runtime.RemoveAllScenesCalls, Is.EqualTo(1));
            Assert.That(runtime.CloseAllWindowsCalls, Is.EqualTo(1));
            Assert.That(runtime.LoadProjectCalls, Is.EqualTo(1));
        }

        [Test]
        public void QuickStartCancel_RestoresPreviousProjectAndLocation()
        {
            FakeRuntime runtime = CreateRuntimeWithLoadedProject();
            Project previousProject = runtime.LoadedProject;
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowSnapshot snapshot = service.QuickStartBegin("quickstart-temp");
            service.QuickStartCancel(snapshot);

            Assert.That(runtime.LoadedProject, Is.SameAs(previousProject));
            Assert.That(runtime.LoadedProjectLocation, Is.EqualTo("previous-location"));
        }

        [Test]
        public async Task QuickStartFinish_SavesNewProjectSetsInteractablesAndLoadsVisualizations()
        {
            FakeRuntime runtime = new()
            {
                LoadedProject = new Project("quickstart", new ProjectPreferences("test-version", "quickstart-id")),
                LoadedProjectLocation = "quickstart-folder"
            };
            ProjectWorkflowService service = new(runtime);

            ProjectWorkflowResult result = await service.QuickStartFinishAsync();

            Assert.That(result.Success, Is.True);
            Assert.That(runtime.SaveProjectCalls, Is.EqualTo(1));
            Assert.That(runtime.SetInteractablesCalls, Is.EqualTo(1));
            Assert.That(runtime.LoadVisualizationsCalls, Is.EqualTo(1));
            Assert.That(runtime.SavedPath, Is.EqualTo("quickstart-folder"));
        }

        private static FakeRuntime CreateRuntimeWithLoadedProject()
        {
            return new FakeRuntime
            {
                LoadedProject = new Project("previous", new ProjectPreferences("test-version", "previous-id")),
                LoadedProjectLocation = "previous-location"
            };
        }

        private static ProjectInfo CreateProjectInfoWithPath(string path)
        {
            return new ProjectInfo
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path
            };
        }

        private static string CreateTempFolder()
        {
            string folder = Path.Combine(Path.GetTempPath(), "hibop-workflow-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static void SetSettingsLoadException(ProjectInfo info, Exception exception)
        {
            PropertyInfo property = typeof(ProjectInfo).GetProperty(nameof(ProjectInfo.SettingsLoadException));
            property.GetSetMethod(true).Invoke(info, new object[] { exception });
        }

        private sealed class FakeRuntime : IProjectWorkflowRuntime
        {
            public Project LoadedProject { get; set; }
            public string LoadedProjectLocation { get; set; }
            public bool HasOpenedScenesForLoadedProject { get; set; }

            public int ClearDataCalls { get; private set; }
            public int LoadProjectCalls { get; private set; }
            public int SaveProjectCalls { get; private set; }
            public int RemoveAllScenesCalls { get; private set; }
            public int CloseAllWindowsCalls { get; private set; }
            public int SetInteractablesCalls { get; private set; }
            public int CheckProjectIDsCalls { get; private set; }
            public int ShowErrorCalls { get; private set; }
            public int LoadVisualizationsCalls { get; private set; }
            public List<string> Calls { get; } = new();
            public List<Exception> LoggedExceptions { get; } = new();
            public Exception LoadProjectException { get; set; }
            public Exception SaveProjectException { get; set; }
            public Project LoadProjectArgument { get; private set; }
            public Project LoadedProjectDuringLoad { get; private set; }
            public string LoadedProjectLocationDuringLoad { get; private set; }
            public string SavedPath { get; private set; }

            public void ClearData()
            {
                ClearDataCalls++;
            }

            public void SaveModuleConfigurations()
            {
                Calls.Add("SaveModuleConfigurations");
            }

            public void RemoveAllScenes()
            {
                RemoveAllScenesCalls++;
            }

            public void CloseAllWindows()
            {
                CloseAllWindowsCalls++;
            }

            public void SetInteractables()
            {
                SetInteractablesCalls++;
            }

            public void CheckProjectIDsAndAskForRegeneration()
            {
                CheckProjectIDsCalls++;
            }

            public void LogException(Exception exception)
            {
                LoggedExceptions.Add(exception);
            }

            public void ShowError(string title, string message)
            {
                ShowErrorCalls++;
            }

            public async UniTask LoadProjectWithProgressAsync(Project project, ProjectInfo info)
            {
                LoadProjectCalls++;
                LoadProjectArgument = project;
                LoadedProjectDuringLoad = LoadedProject;
                LoadedProjectLocationDuringLoad = LoadedProjectLocation;
                if (LoadProjectException != null)
                {
                    throw LoadProjectException;
                }
                await UniTask.CompletedTask;
            }

            public async UniTask SaveProjectWithProgressAsync(Project project, string path)
            {
                Calls.Add("SaveProject");
                SaveProjectCalls++;
                SavedPath = path;
                if (SaveProjectException != null)
                {
                    throw SaveProjectException;
                }
                await UniTask.CompletedTask;
            }

            public async UniTask LoadVisualizationsWithProgressAsync(IEnumerable<Visualization> visualizations)
            {
                LoadVisualizationsCalls++;
                _ = visualizations.ToArray();
                await UniTask.CompletedTask;
            }
        }
    }
}
