using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Tests.PlayMode.Utilities;
using HBP.UI.Main;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using UserPreferencesManager = HBP.Core.Preferences.PersistentDataManager;
using UserProjectPreferences = HBP.Core.Preferences.ProjectPreferences;

namespace HBP.Tests.PlayMode.Workflow
{
    public class ProjectWindowPlayModeTests
    {
        private const string NewProjectWindowResource = "Prefabs/UI/Windows/New project window";
        private const string SaveProjectAsWindowResource = "Prefabs/UI/Windows/Save project as window";
        private const string OpenProjectWindowResource = "Prefabs/UI/Windows/Open project window";

        [Test]
        [Category("PlayMode.ProjectWindow")]
        public async Task NewProjectWindow_SetFields_UsesUserDefaultNameAndLocation()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSelectionManagerScope selectionManager = new();
            using PlayModeSceneScope scene = new("ProjectWindowNewProjectWindow");
            PlayModeWindowHarness window = new(scene.Scene, "ProjectWindow New Project Window Harness");
            string defaultLocation = temp.GetPath("default-projects");
            Directory.CreateDirectory(defaultLocation);
            UserPreferencesManager.UserPreferences.General.Project = new UserProjectPreferences("playmode-default-project", defaultLocation, temp.GetPath("exports"));
            await UniTask.Yield();

            NewProject newProjectWindow = InstantiateWindow<NewProject>(NewProjectWindowResource, window.Root.transform);

            await UniTask.Yield();

            InputField nameInput = GetPrivateField<InputField>(newProjectWindow, "m_NameInputField");
            FolderSelector projectLocation = GetPrivateField<FolderSelector>(newProjectWindow, "m_ProjectLocationFolderSelector");
            Assert.That(nameInput.text, Is.EqualTo("playmode-default-project"));
            Assert.That(projectLocation.Folder, Is.EqualTo(defaultLocation));

            Object.Destroy(newProjectWindow.gameObject);
            await UniTask.Yield();
        }

        [Test]
        [Category("PlayMode.ProjectWindow")]
        public async Task SaveProjectAsWindow_Initialize_UsesLoadedProjectNameAndLocation()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSelectionManagerScope selectionManager = new();
            using PlayModeSceneScope scene = new("ProjectWindowSaveProjectAsWindow");
            PlayModeWindowHarness window = new(scene.Scene, "ProjectWindow Save Project As Window Harness");
            string projectLocation = temp.GetPath("loaded-project-location");
            Directory.CreateDirectory(projectLocation);
            ApplicationState.LoadedProject = new Project("loaded-playmode-project", new ProjectPreferences("playmode-project-window", "loaded-playmode-project-id"));
            ApplicationState.LoadedProjectLocation = projectLocation;
            await UniTask.Yield();

            SaveProjectAs saveProjectAsWindow = InstantiateWindow<SaveProjectAs>(SaveProjectAsWindowResource, window.Root.transform);

            await UniTask.Yield();

            InputField nameInput = GetPrivateField<InputField>(saveProjectAsWindow, "m_NameInputField");
            FolderSelector location = GetPrivateField<FolderSelector>(saveProjectAsWindow, "m_LocationFolderSelector");
            Assert.That(nameInput.text, Is.EqualTo("loaded-playmode-project"));
            Assert.That(location.Folder, Is.EqualTo(projectLocation));

            Object.Destroy(saveProjectAsWindow.gameObject);
            await UniTask.Yield();
        }

        [Test]
        [Category("PlayMode.ProjectWindow")]
        public async Task OpenProjectWindow_DisplayProjects_PopulatesValidProjectsAndDisablesInvalidSettingsProject()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSelectionManagerScope selectionManager = new();
            using PlayModeSceneScope scene = new("ProjectWindowOpenProjectWindow");
            PlayModeWindowHarness window = new(scene.Scene, "ProjectWindow Open Project Window Harness");
            string projectFolder = temp.GetPath("project-list");
            string emptyDefaultFolder = temp.GetPath("empty-project-list");
            Directory.CreateDirectory(projectFolder);
            Directory.CreateDirectory(emptyDefaultFolder);
            UserPreferencesManager.UserPreferences.General.Project = new UserProjectPreferences("unused", emptyDefaultFolder, temp.GetPath("exports"));

            string validArchive = await SaveProjectToDirectoryAsync(projectFolder, new Project("valid-project", new ProjectPreferences("playmode-project-window", "valid-project-id")));
            string invalidArchive = await SaveProjectToDirectoryAsync(projectFolder, new Project("invalid-settings-project", new ProjectPreferences("playmode-project-window", "invalid-project-id")));
            ReplaceZipEntryContent(invalidArchive, "invalid-settings-project" + ProjectPreferences.EXTENSION, "{ this is not valid json");
            await UniTask.Yield();

            OpenProject openProjectWindow = InstantiateWindow<OpenProject>(OpenProjectWindowResource, window.Root.transform);
            FolderSelector location = GetPrivateField<FolderSelector>(openProjectWindow, "m_LocationFolderSelector");
            ProjectList projectList = GetPrivateField<ProjectList>(openProjectWindow, "m_ProjectList");
            Button okButton = GetPrivateField<Button>(openProjectWindow, "m_OKButton", typeof(DialogWindow));
            InputField locationInput = GetPrivateField<InputField>(location, "m_Inputfield");
            await UniTask.Yield();
            await UniTask.Yield();
            locationInput.SetTextWithoutNotify(projectFolder);
            LogAssert.Expect(LogType.Exception, new Regex(".*"));
            location.onValueChanged.Invoke(projectFolder);

            await WaitUntilAsync(() => projectList.Objects.Count >= 2);
            await UniTask.Yield();
            await UniTask.Yield();

            ProjectInfo validProject = projectList.Objects.Single(project => project.Path == validArchive);
            ProjectInfo invalidProject = projectList.Objects.Single(project => project.Path == invalidArchive);
            Assert.That(validProject.Settings.CanLoadProject, Is.True);
            Assert.That(invalidProject.Settings.CanLoadProject, Is.False);
            Assert.That(invalidProject.SettingsLoadException, Is.Not.Null);
            Assert.That(okButton.interactable, Is.False);

            ProjectItem invalidItem = projectList.Items.OfType<ProjectItem>().FirstOrDefault(item => item.Object == invalidProject);
            Assert.That(invalidItem, Is.Not.Null);
            Assert.That(invalidItem.Interactable, Is.False);

            Object.Destroy(openProjectWindow.gameObject);
            await UniTask.Yield();
        }

        private sealed class PlayModeSelectionManagerScope : IDisposable
        {
            private readonly GameObject m_GameObject;

            public PlayModeSelectionManagerScope()
            {
                ResetSingleton();
                m_GameObject = new GameObject("SelectionManager_PlayModeTest");
                m_GameObject.AddComponent<SelectionManager>();
            }

            public void Dispose()
            {
                if (m_GameObject != null)
                {
                    Object.Destroy(m_GameObject);
                }
                ResetSingleton();
            }

            private static void ResetSingleton()
            {
                FieldInfo field = typeof(Singleton<SelectionManager>).GetField("m_Instance", BindingFlags.NonPublic | BindingFlags.Static);
                field.SetValue(null, null);
            }
        }

        private static T InstantiateWindow<T>(string resourcePath, Transform parent) where T : Component
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            Assert.That(prefab, Is.Not.Null, resourcePath);

            GameObject instance = Object.Instantiate(prefab, parent);
            T component = instance.GetComponent<T>();
            Assert.That(component, Is.Not.Null, resourcePath);
            return component;
        }

        private static async Task<string> SaveProjectToDirectoryAsync(string saveDirectory, Project project)
        {
            ApplicationState.LoadedProject = project;
            ApplicationState.LoadedProjectLocation = saveDirectory;

            await project.SaveAsync(saveDirectory, NoProgress, CancellationToken.None);

            return Path.Combine(saveDirectory, project.FileName);
        }

        private static void ReplaceZipEntryContent(string archivePath, string entryName, string content)
        {
            using ZipArchive zip = ZipFile.Open(archivePath, ZipArchiveMode.Update);
            zip.GetEntry(entryName)?.Delete();
            ZipArchiveEntry entry = zip.CreateEntry(entryName);
            using StreamWriter writer = new(entry.Open());
            writer.Write(content);
        }

        private static async Task WaitUntilAsync(Func<bool> predicate, float timeoutSeconds = 5f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    throw new TimeoutException("Timed out while waiting for PlayMode project window state.");
                }
                await UniTask.Yield();
            }
        }

        private static T GetPrivateField<T>(object target, string fieldName, Type declaringType = null)
        {
            Type type = declaringType ?? target.GetType();
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"{type.FullName}.{fieldName}");
            object value = field.GetValue(target);
            Assert.That(value, Is.TypeOf<T>().Or.AssignableTo<T>(), $"{type.FullName}.{fieldName}");
            return (T)value;
        }

        private static void NoProgress(float progress, float duration, LoadingText text)
        {
        }
    }
}
