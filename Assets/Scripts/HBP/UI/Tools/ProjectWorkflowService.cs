using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.UI.Main;
using UnityEngine;

namespace HBP.UI.Tools
{
    public enum ProjectWorkflowStatus
    {
        Success,
        Cancelled,
        Failed,
        Invalid
    }

    public sealed class ProjectWorkflowResult
    {
        public ProjectWorkflowStatus Status { get; }
        public Exception Exception { get; }
        public bool Success => Status == ProjectWorkflowStatus.Success;

        private ProjectWorkflowResult(ProjectWorkflowStatus status, Exception exception = null)
        {
            Status = status;
            Exception = exception;
        }

        public static ProjectWorkflowResult Succeeded() => new(ProjectWorkflowStatus.Success);
        public static ProjectWorkflowResult Cancelled() => new(ProjectWorkflowStatus.Cancelled);
        public static ProjectWorkflowResult Invalid() => new(ProjectWorkflowStatus.Invalid);
        public static ProjectWorkflowResult Failed(Exception exception) => new(ProjectWorkflowStatus.Failed, exception);
    }

    public sealed class ProjectWorkflowSnapshot
    {
        public Project Project { get; }
        public string Location { get; }

        public ProjectWorkflowSnapshot(Project project, string location)
        {
            Project = project;
            Location = location;
        }
    }

    public interface IProjectWorkflowRuntime
    {
        Project LoadedProject { get; set; }
        string LoadedProjectLocation { get; set; }
        bool HasOpenedScenesForLoadedProject { get; }

        void ClearData();
        void SaveModuleConfigurations();
        void RemoveAllScenes();
        void CloseAllWindows();
        void SetInteractables();
        void CheckProjectIDsAndAskForRegeneration();
        void LogException(Exception exception);
        void ShowError(string title, string message);

        UniTask LoadProjectWithProgressAsync(Project project, ProjectInfo info);
        UniTask SaveProjectWithProgressAsync(Project project, string path);
        UniTask LoadVisualizationsWithProgressAsync(IEnumerable<Visualization> visualizations);
    }

    public sealed class UnityProjectWorkflowRuntime : IProjectWorkflowRuntime
    {
        public Project LoadedProject
        {
            get => ApplicationState.LoadedProject;
            set => ApplicationState.LoadedProject = value;
        }

        public string LoadedProjectLocation
        {
            get => ApplicationState.LoadedProjectLocation;
            set => ApplicationState.LoadedProjectLocation = value;
        }

        public bool HasOpenedScenesForLoadedProject
        {
            get { return ApplicationState.LoadedProject != null && ApplicationState.LoadedProject.Visualizations.Any(visualization => Module3DMain.Visualizations.Contains(visualization)); }
        }

        public void ClearData()
        {
            DataManager.Clear();
        }

        public void SaveModuleConfigurations()
        {
            Module3DMain.SaveConfigurations();
        }

        public void RemoveAllScenes()
        {
            Module3DMain.RemoveAllScenes();
        }

        public void CloseAllWindows()
        {
            WindowsManager.CloseAll();
        }

        public void SetInteractables()
        {
            InteractableStateManager.SetInteractables();
        }

        public void CheckProjectIDsAndAskForRegeneration()
        {
            UITools.CheckProjectIDAndAskForRegeneration().Forget();
        }

        public void LogException(Exception exception)
        {
            Debug.LogException(exception);
        }

        public void ShowError(string title, string message)
        {
            DialogBoxManager.OpenScrollable(DialogBoxType.Error, title, message).Forget();
        }

        public async UniTask LoadProjectWithProgressAsync(Project project, ProjectInfo info)
        {
            await LoadingManager.LoadAsync((update, token) => project.LoadAsync(info, update, token));
        }

        public async UniTask SaveProjectWithProgressAsync(Project project, string path)
        {
            await LoadingManager.LoadAsync((update, token) => project.SaveAsync(path, update, token));
        }

        public UniTask LoadVisualizationsWithProgressAsync(IEnumerable<Visualization> visualizations)
        {
            LoadingManager.Load((update, token) => Module3DMain.LoadAsync(visualizations, update, token));
            return UniTask.CompletedTask;
        }
    }

    public sealed class ProjectWorkflowService
    {
        public static ProjectWorkflowService Default { get; } = new(new UnityProjectWorkflowRuntime());

        private readonly IProjectWorkflowRuntime m_Runtime;

        public ProjectWorkflowService(IProjectWorkflowRuntime runtime)
        {
            m_Runtime = runtime;
        }

        public async UniTask<ProjectWorkflowResult> LoadProjectAsync(ProjectInfo info)
        {
            if (info.SettingsLoadException != null)
            {
                m_Runtime.LogException(info.SettingsLoadException);
                m_Runtime.ShowError("Can not load project settings", info.SettingsLoadException.ToString());
                return ProjectWorkflowResult.Failed(info.SettingsLoadException);
            }

            Project previousProject = m_Runtime.LoadedProject;
            string previousLocation = m_Runtime.LoadedProjectLocation;
            Project projectToLoad = new(info.Name, new ProjectPreferences());

            try
            {
                m_Runtime.ClearData();
                await m_Runtime.LoadProjectWithProgressAsync(projectToLoad, info);
                await UniTask.SwitchToMainThread();
                m_Runtime.LoadedProject = projectToLoad;
                m_Runtime.LoadedProjectLocation = Directory.GetParent(info.Path).FullName;
                m_Runtime.SetInteractables();
                m_Runtime.CheckProjectIDsAndAskForRegeneration();
                return ProjectWorkflowResult.Succeeded();
            }
            catch (OperationCanceledException)
            {
                Restore(previousProject, previousLocation);
                return ProjectWorkflowResult.Cancelled();
            }
            catch (Exception exception)
            {
                Restore(previousProject, previousLocation);
                m_Runtime.LogException(exception);
                m_Runtime.ShowError("Unknown error", exception.ToString());
                return ProjectWorkflowResult.Failed(exception);
            }
        }

        public async UniTask<ProjectWorkflowResult> SaveProjectAsync(string path = null)
        {
            Project project = m_Runtime.LoadedProject;
            if (project == null)
            {
                return ProjectWorkflowResult.Invalid();
            }

            string targetPath = string.IsNullOrEmpty(path) ? m_Runtime.LoadedProjectLocation : path;
            try
            {
                m_Runtime.SaveModuleConfigurations();
                m_Runtime.LoadedProjectLocation = targetPath;
                await m_Runtime.SaveProjectWithProgressAsync(project, targetPath);
                return ProjectWorkflowResult.Succeeded();
            }
            catch (OperationCanceledException)
            {
                return ProjectWorkflowResult.Cancelled();
            }
            catch (Exception exception)
            {
                m_Runtime.LogException(exception);
                m_Runtime.ShowError("Unknown error", exception.ToString());
                return ProjectWorkflowResult.Failed(exception);
            }
        }

        public async UniTask<ProjectWorkflowResult> SaveProjectAndReloadAsync()
        {
            ProjectWorkflowResult result = await SaveProjectAsync();
            if (result.Success)
            {
                m_Runtime.SetInteractables();
            }

            return result;
        }

        public async UniTask<ProjectWorkflowResult> CreateNewProjectAsync(string name, string folder, bool overwriteConfirmed)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                m_Runtime.ShowError("Directory not found", "Please select a valid directory to save your project file.");
                return ProjectWorkflowResult.Invalid();
            }

            if (File.Exists(Path.Combine(folder, name + Project.EXTENSION)) && !overwriteConfirmed)
            {
                return ProjectWorkflowResult.Cancelled();
            }

            Project previousProject = m_Runtime.LoadedProject;
            string previousLocation = m_Runtime.LoadedProjectLocation;

            m_Runtime.LoadedProject = new Project(name, new ProjectPreferences());
            m_Runtime.LoadedProjectLocation = folder;

            ProjectWorkflowResult result = await SaveProjectAndReloadAsync();
            if (!result.Success)
            {
                Restore(previousProject, previousLocation);
            }

            return result;
        }

        public async UniTask<ProjectWorkflowResult> SaveProjectAsAsync(string name, string folder, bool overwriteConfirmed)
        {
            Project project = m_Runtime.LoadedProject;
            if (project == null)
            {
                return ProjectWorkflowResult.Invalid();
            }

            if (File.Exists(Path.Combine(folder, name + Project.EXTENSION)) && !overwriteConfirmed)
            {
                return ProjectWorkflowResult.Cancelled();
            }

            string previousName = project.Name;
            ProjectPreferences previousPreferences = project.Preferences;
            string previousLocation = m_Runtime.LoadedProjectLocation;

            project.Name = name;
            project.Preferences = project.Preferences.Clone() as ProjectPreferences;
            ProjectWorkflowResult result = await SaveProjectAsync(folder);
            if (!result.Success)
            {
                project.Name = previousName;
                project.Preferences = previousPreferences;
                m_Runtime.LoadedProjectLocation = previousLocation;
            }

            return result;
        }

        public async UniTask<ProjectWorkflowResult> OpenProjectAsync(ProjectInfo info, bool openedScenesConfirmed)
        {
            if (m_Runtime.HasOpenedScenesForLoadedProject && !openedScenesConfirmed)
            {
                return ProjectWorkflowResult.Cancelled();
            }

            if (m_Runtime.HasOpenedScenesForLoadedProject)
            {
                m_Runtime.RemoveAllScenes();
            }

            m_Runtime.CloseAllWindows();
            return await LoadProjectAsync(info);
        }

        public ProjectWorkflowSnapshot QuickStartBegin(string temporaryLocation)
        {
            ProjectWorkflowSnapshot snapshot = new(m_Runtime.LoadedProject, m_Runtime.LoadedProjectLocation);
            m_Runtime.LoadedProject = new Project("Quick Start", new ProjectPreferences());
            m_Runtime.LoadedProjectLocation = temporaryLocation;
            return snapshot;
        }

        public void QuickStartCancel(ProjectWorkflowSnapshot snapshot)
        {
            if (snapshot == null) return;
            Restore(snapshot.Project, snapshot.Location);
        }

        public async UniTask<ProjectWorkflowResult> QuickStartFinishAsync()
        {
            ProjectWorkflowResult result = await SaveProjectAsync();
            if (!result.Success)
            {
                return result;
            }

            m_Runtime.SetInteractables();
            await m_Runtime.LoadVisualizationsWithProgressAsync(m_Runtime.LoadedProject.Visualizations);
            return ProjectWorkflowResult.Succeeded();
        }

        private void Restore(Project project, string location)
        {
            m_Runtime.LoadedProject = project;
            m_Runtime.LoadedProjectLocation = location;
        }
    }
}
