using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.DLL;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Tests.PlayMode.Utilities;
using HBP.UI.Database;
using HBP.UI.Main;
using HBP.UI.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.PlayMode.UI
{
    public class DatabaseWindowsPlayModeTests
    {
        private const string DatabaseBrowserResource = "Prefabs/UI/Windows/Database browser window";
        private const string ExportBidsResource = "Prefabs/UI/Windows/Export BIDS window";
        private const string ExportLocalizerAtlasResource = "Prefabs/UI/Windows/Export Localizer atlas window";

        [Test]
        [Category("PlayMode.DatabaseWindows")]
        public async Task DatabaseBrowserWindow_DisplaysSeededDatabasePatientsAndAdvancedExportActions()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSelectionManagerScope selectionManager = new();
            using PlayModeSceneScope scene = new("DatabaseWindowsDatabaseBrowserWindow");
            PlayModeWindowHarness window = new(scene.Scene, "DatabaseWindows Database Browser Harness");
            Project project = SeedDatabase(temp);

            DatabaseBrowserWindow databaseBrowserWindow = InstantiateWindow<DatabaseBrowserWindow>(DatabaseBrowserResource, window.Root.transform);
            DatabasePatientList patientList = GetPrivateField<DatabasePatientList>(databaseBrowserWindow, "m_PatientList");
            Button localizerExportButton = GetPrivateField<Button>(databaseBrowserWindow, "m_OpenExportLocalizerAtlasWindowButton");
            Button bidsExportButton = GetPrivateField<Button>(databaseBrowserWindow, "m_OpenExportBIDSWindowButton");

            await WaitForWindowLayoutAsync();

            Assert.That(patientList.Objects.Single(), Is.SameAs(project.Patients.Single()));
            Assert.That(patientList.Items, Is.Not.Empty);
            Assert.That(GetTexts(databaseBrowserWindow.gameObject), Does.Contain(project.Patients.Single().Name));
            Assert.That(localizerExportButton.gameObject.activeSelf, Is.True);
            Assert.That(bidsExportButton.gameObject.activeSelf, Is.True);

            Object.Destroy(databaseBrowserWindow.gameObject);
            await WaitForWindowLayoutAsync();
        }

        [Test]
        [Category("PlayMode.DatabaseWindows")]
        public async Task ExportBidsWindow_PopulatesSyntheticDatabaseAndEnablesExportAfterSelections()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSelectionManagerScope selectionManager = new();
            using PlayModeSceneScope scene = new("DatabaseWindowsExportBidsWindow");
            PlayModeWindowHarness window = new(scene.Scene, "DatabaseWindows Export BIDS Harness");
            Project project = SeedDatabase(temp);

            ExportBIDSWindow exportWindow = InstantiateWindow<ExportBIDSWindow>(ExportBidsResource, window.Root.transform);
            InputField datasetName = GetPrivateField<InputField>(exportWindow, "m_DatasetNameInputField");
            FolderSelector exportFolder = GetPrivateField<FolderSelector>(exportWindow, "m_ExportFolderSelector");
            Text patientsSelectedText = GetPrivateField<Text>(exportWindow, "m_PatientsSelectedText");
            Text patientTagsSelectedText = GetPrivateField<Text>(exportWindow, "m_PatientTagsSelectedText");
            Text siteTagsSelectedText = GetPrivateField<Text>(exportWindow, "m_SiteTagsSelectedText");
            Button okButton = GetPrivateField<Button>(exportWindow, "m_OKButton", typeof(DialogWindow));
            List<BIDSProtocolItem> protocolItems = GetPrivateField<List<BIDSProtocolItem>>(exportWindow, "m_ProtocolItems");
            List<BIDSDataItem> dataItems = GetPrivateField<List<BIDSDataItem>>(exportWindow, "m_DataItems");

            await WaitForWindowLayoutAsync();

            Assert.That(datasetName.text, Is.EqualTo("BIDS_Dataset"));
            Assert.That(exportFolder.Folder, Is.EqualTo(PersistentDataManager.UserPreferences.General.Project.DefaultExportLocation));
            Assert.That(protocolItems.Select(item => item.Name), Does.Contain(project.Datasets.Single().Protocol.Name));
            Assert.That(dataItems.Select(item => item.DataName), Does.Contain("playmode-signal-alpha"));
            Assert.That(patientsSelectedText.text, Is.EqualTo("No patients selected"));
            Assert.That(patientTagsSelectedText.text, Does.Contain("patient tag"));
            Assert.That(siteTagsSelectedText.text, Does.Contain("site tags"));
            Assert.That(okButton.interactable, Is.False);

            InvokePrivate(exportWindow, "OnPatientsSelected", new object[] { project.Patients.ToArray() });
            protocolItems.Single(item => item.Name == project.Datasets.Single().Protocol.Name).SetSelected(true);
            dataItems.Single(item => item.DataName == "playmode-signal-alpha").SetSelected(true);
            await UniTask.Yield();

            Assert.That(patientsSelectedText.text, Is.EqualTo("1 patient selected"));
            Assert.That(okButton.interactable, Is.True);

            Object.Destroy(exportWindow.gameObject);
            await WaitForWindowLayoutAsync();
        }

        [Test]
        [Category("PlayMode.DatabaseWindows")]
        public async Task ExportLocalizerAtlasWindow_PopulatesSyntheticDatabaseAndEnablesExportAfterSelections()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSelectionManagerScope selectionManager = new();
            using ActivityProjectionSettingsScope projectionSettings = new(96);
            using PlayModeSceneScope scene = new("DatabaseWindowsExportLocalizerAtlasWindow");
            PlayModeWindowHarness window = new(scene.Scene, "DatabaseWindows Export Localizer Harness");
            Project project = SeedDatabase(temp);

            ExportLocalizerAtlasWindow exportWindow = InstantiateWindow<ExportLocalizerAtlasWindow>(ExportLocalizerAtlasResource, window.Root.transform);
            FolderSelector exportFolder = GetPrivateField<FolderSelector>(exportWindow, "m_ExportFolderSelector");
            Text patientsSelectedText = GetPrivateField<Text>(exportWindow, "m_PatientsSelectedText");
            InputField maximumGridDimension = GetPrivateField<InputField>(exportWindow, "m_MaximumGridDimensionInputField");
            Text exportGridPreview = GetPrivateField<Text>(exportWindow, "m_ExportGridPreviewText");
            Button okButton = GetPrivateField<Button>(exportWindow, "m_OKButton", typeof(DialogWindow));
            List<ExportProtocolItem> protocolItems = GetPrivateField<List<ExportProtocolItem>>(exportWindow, "m_ProtocolItems");
            List<ExportDataNameItem> dataNameItems = GetPrivateField<List<ExportDataNameItem>>(exportWindow, "m_DataNameItems");

            await WaitForWindowLayoutAsync();

            ExportProtocolItem protocolItem = protocolItems.Single(item => item.Name == project.Datasets.Single().Protocol.Name);
            Assert.That(exportFolder.Folder, Is.EqualTo(PersistentDataManager.UserPreferences.General.Project.DefaultExportLocation));
            Assert.That(protocolItems.Select(item => item.Name), Does.Contain(project.Datasets.Single().Protocol.Name));
            Assert.That(protocolItem.GetComponentsInChildren<ExportBlocItem>(true).Select(item => item.Name), Does.Contain("playmode-bloc-alpha"));
            Assert.That(dataNameItems.Select(item => item.DataName), Does.Contain("playmode-signal-alpha"));
            Assert.That(patientsSelectedText.text, Is.EqualTo("No patients selected"));
            Assert.That(maximumGridDimension.text, Is.EqualTo("80"));
            Assert.That(exportGridPreview.text, Is.Not.Empty);
            Assert.That(okButton.interactable, Is.False);

            InvokePrivate(exportWindow, "OnPatientsSelected", new object[] { project.Patients.ToArray() });
            ToggleFirstChildToggle(protocolItem.gameObject);
            ToggleFirstChildToggle(dataNameItems.Single(item => item.DataName == "playmode-signal-alpha").gameObject);
            await UniTask.Yield();

            Assert.That(patientsSelectedText.text, Is.EqualTo("1 patient selected"));
            Assert.That(okButton.interactable, Is.True);

            maximumGridDimension.text = "1";
            await UniTask.Yield();

            Assert.That(exportGridPreview.text, Does.Contain("between 2 and 512"));
            Assert.That(okButton.interactable, Is.False);

            maximumGridDimension.text = "96";
            await UniTask.Yield();

            Assert.That(okButton.interactable, Is.True);

            Object.Destroy(exportWindow.gameObject);
            await WaitForWindowLayoutAsync();
        }

        private static Project SeedDatabase(PlayModeTempDirectoryScope temp)
        {
            Project project = PlayModeProjectHarness.CreateAndLoadCompleteProject();
            string exportRoot = temp.GetPath("exports");
            Directory.CreateDirectory(exportRoot);
            PersistentDataManager.UserPreferences.General.Project.DefaultExportLocation = exportRoot;
            PersistentDataManager.UserPreferences.General.Misc.AdvancedFeatures = true;
            DatabaseManager.Database.SetProtocols(project.Datasets.Select(dataset => dataset.Protocol));
            SetPrivateField(DatabaseManager.Database, "m_Patients", project.Patients.ToList());
            SetPrivateField(DatabaseManager.Database, "m_DataInfos", project.Datasets.SelectMany(dataset => dataset.Data).ToList());
            return project;
        }

        private static async UniTask WaitForWindowLayoutAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            Canvas.ForceUpdateCanvases();
            await UniTask.Yield();
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

        private static IEnumerable<string> GetTexts(GameObject root)
        {
            return root.GetComponentsInChildren<Text>(true).Select(text => text.text);
        }

        private static T GetPrivateField<T>(object target, string fieldName, Type declaringType = null)
        {
            Type type = declaringType ?? target.GetType();
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"{type.FullName}.{fieldName}");
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"{target.GetType().FullName}.{fieldName}");
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName, object[] parameters)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, $"{target.GetType().FullName}.{methodName}");
            method.Invoke(target, parameters);
        }

        private static void ToggleFirstChildToggle(GameObject root)
        {
            Toggle toggle = root.GetComponentsInChildren<Toggle>(true).FirstOrDefault();
            Assert.That(toggle, Is.Not.Null, root.name);
            toggle.isOn = true;
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
            }

            private static void ResetSingleton()
            {
                FieldInfo field = typeof(Singleton<SelectionManager>).GetField("m_Instance", BindingFlags.NonPublic | BindingFlags.Static);
                field.SetValue(null, null);
            }
        }

        private sealed class ActivityProjectionSettingsScope : IDisposable
        {
            private readonly int m_MaximumDimension = ActivityProjectionSettings.VolumeGridDimension;
            private readonly HBP.Core.Enums.VolumeInterpolation m_Interpolation = ActivityProjectionSettings.VolumeInterpolation;

            public ActivityProjectionSettingsScope(int maximumDimension)
            {
                ActivityProjectionSettings.VolumeGridDimension = maximumDimension;
            }

            public void Dispose()
            {
                ActivityProjectionSettings.VolumeGridDimension = m_MaximumDimension;
                ActivityProjectionSettings.VolumeInterpolation = m_Interpolation;
            }
        }
    }
}
