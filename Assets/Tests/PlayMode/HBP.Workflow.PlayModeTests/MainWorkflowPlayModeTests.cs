using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Enums;
using HBP.Core.Interfaces;
using HBP.Core.Tools;
using HBP.Tests.PlayMode.Utilities;
using HBP.UI.Database;
using HBP.UI.Main;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using UserPersistentDataManager = HBP.Core.Preferences.PersistentDataManager;

namespace HBP.Tests.PlayMode.Workflow
{
    public class MainWorkflowPlayModeTests
    {
        private const string PatientGestionResource = "Prefabs/UI/Windows/Patient gestion window";
        private const string GroupGestionResource = "Prefabs/UI/Windows/Group gestion window";
        private const string ProtocolGestionResource = "Prefabs/UI/Windows/Protocol gestion window";
        private const string DatasetGestionResource = "Prefabs/UI/Windows/Dataset gestion window";
        private const string VisualizationGestionResource = "Prefabs/UI/Windows/Visualization gestion window";
        private const string UserPreferencesResource = "Prefabs/UI/Windows/User preferences window";
        private const string ProjectPreferencesResource = "Prefabs/UI/Windows/Project preferences window";
        private const string GlobalDatabaseSettingsResource = "Prefabs/UI/Windows/Global Database Settings modifier window";
        private const string DatabaseReferenceGestionResource = "Prefabs/UI/Windows/Database Reference gestion window";

        [Test]
        [Category("PlayMode.MainWorkflow")]
        public async Task ManagementWindows_CommitAddEditDeleteWorkflowsToProjectModel()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope applicationState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSelectionManagerScope selectionManager = new();
            using PlayModeInteractableStateManagerScope interactables = new();
            using PlayModeSceneScope scene = new("MainWorkflowManagementWindows");
            PlayModeWindowHarness window = new(scene.Scene, "MainWorkflow Management Windows Harness");
            ApplicationState.LoadedProjectLocation = temp.Path;
            ApplicationState.LoadedProject = null;
            DatabaseManager.Database.SetProtocols(new[] { PlayModeProjectHarness.CreateProtocol() });
            await UniTask.Yield();

            ProtocolGestion protocols = InstantiateWindow<ProtocolGestion>(ProtocolGestionResource, window.Root.transform);
            await UniTask.Yield();
            Protocol editedProtocol = CloneWithName(DatabaseManager.Database.Protocols.Single(), "main-workflow-protocol-edited");
            Protocol createdProtocol = new("main-workflow-protocol-created", Array.Empty<Bloc>(), "main-workflow-protocol-created-id");
            Protocol deletedProtocol = new("main-workflow-protocol-deleted", Array.Empty<Bloc>(), "main-workflow-protocol-deleted-id");
            ActionableList<Protocol> protocolList = protocols.ListGestion.List;
            protocolList.UpdateObject(editedProtocol);
            protocolList.Add(createdProtocol);
            protocolList.Add(deletedProtocol);
            protocolList.Remove(deletedProtocol);
            protocols.OK();
            string createdProtocolPath = Path.Combine(ApplicationState.DatabasePath, "Protocols", createdProtocol.Name + Protocol.EXTENSION);
            await WaitUntilAsync(() => File.Exists(createdProtocolPath));
            await UniTask.DelayFrame(10);

            Assert.That(DatabaseManager.Database.Protocols.Select(protocol => protocol.Name), Does.Contain("main-workflow-protocol-edited"));
            Assert.That(DatabaseManager.Database.Protocols.Any(protocol => protocol.ID == createdProtocol.ID), Is.True);
            Assert.That(DatabaseManager.Database.Protocols.Any(protocol => protocol.ID == deletedProtocol.ID), Is.False);

            Project project = PlayModeProjectHarness.CreateAndLoadCompleteProject();
            ApplicationState.LoadedProjectLocation = temp.Path;
            await UniTask.Yield();

            PatientGestion patients = InstantiateWindow<PatientGestion>(PatientGestionResource, window.Root.transform);
            await UniTask.Yield();
            Patient editedPatient = CloneWithName(project.Patients.Single(), "main-workflow-patient-edited");
            Patient createdPatient = CreatePatient("main-workflow-patient-created", "main-workflow-patient-created-id");
            Patient deletedPatient = CreatePatient("main-workflow-patient-deleted", "main-workflow-patient-deleted-id");
            ActionableList<Patient> patientList = patients.ListGestion.List;
            patientList.UpdateObject(editedPatient);
            patientList.Add(createdPatient);
            patientList.Add(deletedPatient);
            patientList.Remove(deletedPatient);
            patients.OK();
            await UniTask.Yield();

            Assert.That(project.Patients.Select(patient => patient.Name), Does.Contain("main-workflow-patient-edited"));
            Assert.That(project.Patients.Any(patient => patient.ID == createdPatient.ID), Is.True);
            Assert.That(project.Patients.Any(patient => patient.ID == deletedPatient.ID), Is.False);

            GroupGestion groups = InstantiateWindow<GroupGestion>(GroupGestionResource, window.Root.transform);
            await UniTask.Yield();
            Group editedGroup = CloneWithName(project.Groups.Single(), "main-workflow-group-edited");
            Group createdGroup = new("main-workflow-group-created", new[] { createdPatient }, "main-workflow-group-created-id");
            Group deletedGroup = new("main-workflow-group-deleted", new[] { createdPatient }, "main-workflow-group-deleted-id");
            ActionableList<Group> groupList = groups.ListGestion.List;
            groupList.UpdateObject(editedGroup);
            groupList.Add(createdGroup);
            groupList.Add(deletedGroup);
            groupList.Remove(deletedGroup);
            groups.OK();
            await UniTask.Yield();

            Assert.That(project.Groups.Select(group => group.Name), Does.Contain("main-workflow-group-edited"));
            Assert.That(project.Groups.Any(group => group.ID == createdGroup.ID), Is.True);
            Assert.That(project.Groups.Any(group => group.ID == deletedGroup.ID), Is.False);

            DatasetGestion datasets = InstantiateWindow<DatasetGestion>(DatasetGestionResource, window.Root.transform);
            await UniTask.Yield();
            Dataset editedDataset = CloneWithName(project.Datasets.Single(), "main-workflow-dataset-edited");
            Dataset createdDataset = new("main-workflow-dataset-created", project.Datasets.Single().Protocol, Array.Empty<DataInfo>(), "main-workflow-dataset-created-id");
            Dataset deletedDataset = new("main-workflow-dataset-deleted", project.Datasets.Single().Protocol, Array.Empty<DataInfo>(), "main-workflow-dataset-deleted-id");
            ActionableList<Dataset> datasetList = datasets.ListGestion.List;
            datasetList.UpdateObject(editedDataset);
            datasetList.Add(createdDataset);
            datasetList.Add(deletedDataset);
            datasetList.Remove(deletedDataset);
            datasets.OK();
            await UniTask.Yield();

            Assert.That(project.Datasets.Select(dataset => dataset.Name), Does.Contain("main-workflow-dataset-edited"));
            Assert.That(project.Datasets.Any(dataset => dataset.ID == createdDataset.ID), Is.True);
            Assert.That(project.Datasets.Any(dataset => dataset.ID == deletedDataset.ID), Is.False);

            VisualizationGestion visualizations = InstantiateWindow<VisualizationGestion>(VisualizationGestionResource, window.Root.transform);
            await UniTask.Yield();
            Visualization editedVisualization = CloneWithName(project.Visualizations.Single(), "main-workflow-visualization-edited");
            Visualization createdVisualization = CreateVisualization("main-workflow-visualization-created", createdPatient, createdDataset, "main-workflow-visualization-created-id");
            Visualization deletedVisualization = CreateVisualization("main-workflow-visualization-deleted", createdPatient, createdDataset, "main-workflow-visualization-deleted-id");
            ActionableList<Visualization> visualizationList = visualizations.ListGestion.List;
            visualizationList.UpdateObject(editedVisualization);
            visualizationList.Add(createdVisualization);
            visualizationList.Add(deletedVisualization);
            visualizationList.Remove(deletedVisualization);
            visualizations.OK();
            await UniTask.Yield();

            Assert.That(project.Visualizations.Select(visualization => visualization.Name), Does.Contain("main-workflow-visualization-edited"));
            Assert.That(project.Visualizations.Any(visualization => visualization.ID == createdVisualization.ID), Is.True);
            Assert.That(project.Visualizations.Any(visualization => visualization.ID == deletedVisualization.ID), Is.False);

            DestroyWindowHarness(window);
            await UniTask.Yield();
        }

        [Test]
        [Category("PlayMode.MainWorkflow")]
        public async Task PreferencesAndDatabaseWindows_DisplayProjectUserReferenceAndWorkspaceState()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope applicationState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSelectionManagerScope selectionManager = new();
            using PlayModeInteractableStateManagerScope interactables = new();
            using PlayModeSceneScope scene = new("MainWorkflowPreferencesDatabaseWindows");
            PlayModeWindowHarness window = new(scene.Scene, "MainWorkflow Preferences Database Harness");
            Project project = PlayModeProjectHarness.CreateAndLoadCompleteProject();
            ApplicationState.LoadedProjectLocation = temp.Path;

            Workspace workspace = new("main-workflow-workspace", "main-workflow-workspace-id");
            DatabaseManager.Database.Settings.SetWorkspaces(new[] { workspace });
            DatabaseManager.Database.Settings.SelectedWorkspace = workspace;
            DatabaseReference reference = new("main-workflow-reference", DatabaseType.Brainvisa, temp.GetPath("brainvisa"), new BrainvisaDatabaseParameters(), DateTime.UtcNow, "main-workflow-reference-id");
            DatabaseManager.Database.SetDatabaseReferences(new[] { reference });
            await UniTask.Yield();

            NormalizationType initialNormalization = DataManager.DefaultNormalization;
            UserPreferencesModifier userPreferences = InstantiateWindow<UserPreferencesModifier>(UserPreferencesResource, window.Root.transform);
            userPreferences.Object = UserPersistentDataManager.UserPreferences;
            userPreferences.Interactable = false;
            userPreferences.Interactable = true;
            Assert.That(GetPrivateField<object>(userPreferences, "m_ProjectPreferencesSubModifier"), Is.Not.Null);
            Assert.That(GetPrivateField<object>(userPreferences, "m_GraphPreferencesSubModifier"), Is.Not.Null);
            EEGPreferencesSubModifier eegPreferences = GetPrivateField<EEGPreferencesSubModifier>(userPreferences, "m_EEGPreferencesSubModifier");
            Dropdown normalizationDropdown = GetPrivateField<Dropdown>(eegPreferences, "m_EEGNormalizationDropdown");
            Dropdown temporalSamplingDropdown = GetPrivateField<Dropdown>(eegPreferences, "m_TemporalSamplingDropdown");
            Assert.That(normalizationDropdown.options.Select(option => option.text), Does.Not.Contain("Auto"));
            Assert.That(temporalSamplingDropdown, Is.Not.Null);
            Assert.That(temporalSamplingDropdown.options.Select(option => option.text), Is.EqualTo(new[] { "Floor", "Round", "Interpolate" }));
            normalizationDropdown.value = normalizationDropdown.options.FindIndex(option => option.text == "Protocol");
            Assert.That(DataManager.DefaultNormalization, Is.EqualTo(NormalizationType.Protocol));
            userPreferences.Close();
            await UniTask.Yield();
            Assert.That(DataManager.DefaultNormalization, Is.EqualTo(initialNormalization));

            ProjectPreferencesModifier projectPreferences = InstantiateWindow<ProjectPreferencesModifier>(ProjectPreferencesResource, window.Root.transform);
            projectPreferences.Object = project.Preferences;
            projectPreferences.Interactable = false;
            projectPreferences.Interactable = true;
            Assert.That(GetPrivateField<object>(projectPreferences, "m_GeneralSubModifier"), Is.Not.Null);

            GlobalDatabaseSettingsModifier databaseSettings = InstantiateWindow<GlobalDatabaseSettingsModifier>(GlobalDatabaseSettingsResource, window.Root.transform);
            databaseSettings.Object = DatabaseManager.Database.Settings;
            WorkspaceListGestion workspaceListGestion = GetPrivateField<WorkspaceListGestion>(databaseSettings, "m_WorkspaceListGestion");
            Button switchWorkspaceButton = GetPrivateField<Button>(databaseSettings, "m_SwitchWorkspaceButton");
            Assert.That(workspaceListGestion.List.Objects, Does.Contain(workspace));
            Assert.That(switchWorkspaceButton.interactable, Is.False);

            DatabaseReferenceGestion referenceGestion = InstantiateWindow<DatabaseReferenceGestion>(DatabaseReferenceGestionResource, window.Root.transform);
            await UniTask.Yield();
            DatabaseReferenceListGestion referenceListGestion = GetPrivateField<DatabaseReferenceListGestion>(referenceGestion, "m_ListGestion");
            Button updateButton = GetPrivateField<Button>(referenceGestion, "m_UpdateButton");
            Assert.That(referenceListGestion.List.Objects.Single().Name, Is.EqualTo("main-workflow-reference"));
            Assert.That(updateButton.interactable, Is.False);

            DestroyWindowHarness(window);
            await UniTask.Yield();
        }

        [Test]
        [Category("PlayMode.MainWorkflow")]
        public void VisualizationWorkflow_CreatesEveryColumnTypeFromSyntheticProjectData()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope applicationState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            Project project = PlayModeProjectHarness.CreateAndLoadCompleteProject();
            Patient patient = project.Patients.Single();
            Dataset dataset = project.Datasets.Single();
            Protocol protocol = dataset.Protocol;
            Bloc bloc = protocol.OrderedBlocs.Single();

            Column[] columns =
            {
                new AnatomicColumn("main-workflow-anatomic", new BaseConfiguration(), new AnatomicConfiguration()),
                new IEEGColumn("main-workflow-ieeg", new BaseConfiguration(), dataset, "playmode-signal-alpha", bloc, new DynamicConfiguration()),
                new CCEPColumn("main-workflow-ccep", new BaseConfiguration(), dataset, "playmode-response-alpha", bloc, new DynamicConfiguration()),
                new FMRIColumn("main-workflow-fmri", new BaseConfiguration(), dataset, new FMRIConfiguration()),
                new MEGColumn("main-workflow-meg", new BaseConfiguration(), dataset, new MEGConfiguration()),
                new StaticColumn("main-workflow-static", new BaseConfiguration(), dataset, "playmode-static-alpha", new StaticConfiguration())
            };

            Visualization visualization = new("main-workflow-all-columns", new[] { patient }, columns, new VisualizationConfiguration(), "main-workflow-all-columns-id");

            Assert.That(visualization.Columns.Select(column => column.GetType()), Is.EquivalentTo(new[]
            {
                typeof(AnatomicColumn),
                typeof(IEEGColumn),
                typeof(CCEPColumn),
                typeof(FMRIColumn),
                typeof(MEGColumn),
                typeof(StaticColumn)
            }));
            Assert.That(visualization.IsVisualizable, Is.True);
            Assert.That(columns.OfType<IEEGColumn>().Single().Dataset, Is.SameAs(dataset));
            Assert.That(columns.OfType<CCEPColumn>().Single().Bloc, Is.SameAs(bloc));
            Assert.That(columns.OfType<StaticColumn>().Single().DataName, Is.EqualTo("playmode-static-alpha"));
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

        private static Patient CreatePatient(string name, string id)
        {
            return new Patient(name, Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), string.Empty, id);
        }

        private static Visualization CreateVisualization(string name, Patient patient, Dataset dataset, string id)
        {
            return new Visualization(name, new[] { patient }, new Column[] { new AnatomicColumn(name + "-column", new BaseConfiguration(), new AnatomicConfiguration()) }, new VisualizationConfiguration(), id);
        }

        private static void DestroyWindowHarness(PlayModeWindowHarness window)
        {
            if (window.EventSystem != null)
            {
                Object.Destroy(window.EventSystem.gameObject);
            }

            if (window.Root != null)
            {
                Object.Destroy(window.Root);
            }
        }

        private static async Task WaitUntilAsync(Func<bool> predicate, float timeoutSeconds = 5f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    throw new TimeoutException("Timed out while waiting for main workflow workflow state.");
                }

                await UniTask.Yield();
            }
        }

        private static T CloneWithName<T>(T source, string name) where T : BaseData, INameable
        {
            T clone = (T)source.Clone();
            clone.Name = name;
            return clone;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = GetFieldInHierarchy(target.GetType(), fieldName);
            Assert.That(field, Is.Not.Null, $"{target.GetType().FullName}.{fieldName}");
            object value = field.GetValue(target);
            Assert.That(value, Is.TypeOf<T>().Or.AssignableTo<T>(), $"{target.GetType().FullName}.{fieldName}");
            return (T)value;
        }

        private static FieldInfo GetFieldInHierarchy(Type type, string fieldName)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) return field;
            }

            return null;
        }

        private sealed class PlayModeInteractableStateManagerScope : IDisposable
        {
            private readonly GameObject m_GameObject;

            public PlayModeInteractableStateManagerScope()
            {
                ResetSingleton();
                m_GameObject = new GameObject("InteractableStateManager_PlayModeTest");
                m_GameObject.AddComponent<InteractableStateManager>();
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
                FieldInfo field = typeof(Singleton<InteractableStateManager>).GetField("m_Instance", BindingFlags.NonPublic | BindingFlags.Static);
                field.SetValue(null, null);
            }
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
    }
}
