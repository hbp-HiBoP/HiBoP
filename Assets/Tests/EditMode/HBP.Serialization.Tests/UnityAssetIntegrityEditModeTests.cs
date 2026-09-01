using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HBP.Tests.Serialization
{
    public class UnityAssetIntegrityEditModeTests
    {
        private const string MainScenePath = "Assets/_Scenes/HiBoP.unity";

        [Test]
        public void ProjectPrefabs_DoNotContainMissingScripts()
        {
            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs", "Assets/Resources/Prefabs" }).Select(AssetDatabase.GUIDToAssetPath).Distinct().ToArray();

            List<string> failures = new();
            foreach (string path in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                int missing = CountMissingScripts(prefab);
                if (missing > 0)
                {
                    failures.Add($"{path}: {missing} missing script(s)");
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void MainScene_DoesNotContainMissingScripts()
        {
            Assert.That(File.Exists(MainScenePath), Is.True);

            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
            try
            {
                List<string> failures = new();
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    int missing = CountMissingScripts(root);
                    if (missing > 0)
                    {
                        failures.Add($"{root.name}: {missing} missing script(s)");
                    }
                }

                Assert.That(failures, Is.Empty, string.Join("\n", failures));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void MainScene_IsListedInBuildSettingsAndWiresBootstrapComponents()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath), Is.Not.Null, MainScenePath);
            Assert.That(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == MainScenePath), Is.True, $"{MainScenePath} must be enabled in EditorBuildSettings");

            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
            try
            {
                AssertSceneContainsComponent(scene, "HBP.UI.Tools.ApplicationManager");
                AssertSceneContainsComponent(scene, "HBP.UI.Tools.WindowsManager");
                AssertSceneContainsComponent(scene, "HBP.UI.Tools.DialogBoxManager");
                AssertSceneContainsComponent(scene, "HBP.UI.Tools.LoadingManager");
                AssertSceneContainsComponent(scene, "HBP.Data.Module3D.Module3DMain");
                AssertSceneContainsComponent(scene, "HBP.UI.Main.MainMenu");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void CriticalPrefabs_LoadAndExposeExpectedComponents()
        {
            AssertPrefabHasComponents("Assets/Prefabs/3D/3D.prefab", "HBP.Data.Module3D.Module3DMain");
            AssertPrefabHasComponents("Assets/Prefabs/3D/Scenes/Scene 3D.prefab", "HBP.Data.Module3D.Base3DScene");
            AssertPrefabHasComponents("Assets/Prefabs/3D/UI/3D Menu.prefab", "HBP.UI.Toolbar.ToolbarMenu", "HBP.UI.Toolbar.ToolbarSelector");
            AssertPrefabHasComponents("Assets/Prefabs/Informations/Informations.prefab", "HBP.UI.Informations.GraphZone", "HBP.UI.Informations.Graphs.GraphsGrid", "HBP.UI.Informations.TrialMatrixZone", "HBP.UI.Informations.TrialMatrix.TrialMatrixGrid");
            AssertPrefabHasComponents("Assets/Prefabs/Informations/TrialMatrix/Grid/pref_TrialMatrixGrid.prefab", "HBP.UI.Informations.TrialMatrix.TrialMatrixGrid");
            AssertPrefabHasComponents("Assets/Resources/Prefabs/UI/Windows/New project window.prefab", "HBP.UI.Main.NewProject");
            AssertPrefabHasComponents("Assets/Resources/Prefabs/UI/Windows/Open project window.prefab", "HBP.UI.Main.OpenProject");
            AssertPrefabHasComponents("Assets/Resources/Prefabs/UI/Windows/Save project as window.prefab", "HBP.UI.Main.SaveProjectAs");
            AssertPrefabHasComponents("Assets/Resources/Prefabs/UI/Windows/Patient gestion window.prefab", "HBP.UI.Main.PatientGestion");
            AssertPrefabHasComponents("Assets/Resources/Prefabs/UI/Windows/Protocol gestion window.prefab", "HBP.UI.Main.ProtocolGestion");
            AssertPrefabHasComponents("Assets/Resources/Prefabs/UI/Windows/Dataset gestion window.prefab", "HBP.UI.Main.DatasetGestion");
            AssertPrefabHasComponents("Assets/Resources/Prefabs/UI/Windows/Visualization gestion window.prefab", "HBP.UI.Main.VisualizationGestion");
            AssertPrefabHasComponents("Assets/Resources/Prefabs/UI/Windows/Database browser window.prefab", "HBP.UI.Database.DatabaseBrowserWindow");
            AssertPrefabHasComponents("Assets/Resources/Prefabs/UI/Windows/Graph settings window.prefab", "HBP.UI.Informations.GraphSettingsWindow");
            AssertPrefabHasComponents("Assets/Resources/Prefabs/UI/Windows/Trial matrix explorer window.prefab", "HBP.UI.Database.TrialMatrixExplorerWindow");
        }

        [Test]
        public void CriticalPrefabs_WireBootstrapSerializedReferences()
        {
            GameObject module3D = LoadPrefab("Assets/Prefabs/3D/3D.prefab");
            AssertSerializedReferences(FindComponent(module3D, "HBP.Data.Module3D.Module3DMain"), "m_SharedMaterials", "m_SharedDirectionalLight", "m_SharedSpotlight", "m_ScenesParent", "m_ScenePrefab");

            GameObject toolbar = LoadPrefab("Assets/Prefabs/3D/UI/3D Menu.prefab");
            AssertSerializedReferences(FindComponent(toolbar, "HBP.UI.Toolbar.ToolbarMenu"), "m_ConfigurationToolbar", "m_SceneSettingsToolbar", "m_DisplaySettingsToolbar", "m_ActivitySettingsToolbar", "m_TimelineToolbar", "m_SiteToolbar", "m_AtlasToolbar", "m_ROIToolbar", "m_TriangleToolbar");
            AssertSerializedReferences(FindComponent(toolbar, "HBP.UI.Toolbar.ToolbarSelector"), "m_ToolbarMenu", "m_ConfigurationToggle", "m_SceneToggle", "m_DisplayToggle", "m_IEEGToggle", "m_TimelineToggle", "m_SiteToggle", "m_IBCToggle", "m_ROIToggle", "m_TriangleToggle");

            AssertSerializedReferences(FindComponent(LoadPrefab("Assets/Prefabs/Managers/Windows Manager.prefab"), "HBP.UI.Tools.WindowsManager"), "m_ParentContainer", "m_ContainerPrefab");
            AssertSerializedReferences(FindComponent(LoadPrefab("Assets/Prefabs/Managers/Dialog Box Manager.prefab"), "HBP.UI.Tools.DialogBoxManager"), "m_DialogBoxPrefab", "m_ScrollableDialogBoxPrefab", "m_Canvas");
            AssertSerializedReferences(FindComponent(LoadPrefab("Assets/Prefabs/LoadingCircle/Loading Manager.prefab"), "HBP.UI.Tools.LoadingManager"), "m_LoadingCircle");
            AssertSerializedReferences(FindComponent(LoadPrefab("Assets/Prefabs/General/Main menu.prefab"), "HBP.UI.Main.MainMenu"), "m_FileMenu", "m_EditMenu", "m_ProjectMenu", "m_DatabaseMenu", "m_HelpMenu");
        }

        [Test]
        public void RenderingResources_ExposeRequiredShadersMaterialsColormapsAndIcons()
        {
            string[] shaderPaths =
            {
                "Assets/Resources/Shaders/MeshShader.shader",
                "Assets/Resources/Shaders/SimplifiedMeshShader.shader",
                "Assets/Resources/Shaders/TransparentMeshShader.shader",
                "Assets/Resources/Shaders/SiteShader.shader",
                "Assets/Resources/Shaders/SiteSelectionShader.shader",
                "Assets/Resources/Shaders/ROIShader.shader",
                "Assets/Resources/Shaders/Mask.shader",
                "Assets/Resources/Shaders/PlotInstance.shader"
            };
            foreach (string path in shaderPaths)
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<Shader>(path), Is.Not.Null, path);
            }

            string[] materialPaths =
            {
                "Assets/Resources/Materials/Brain/Brain.mat",
                "Assets/Resources/Materials/Brain/TransparentBrain.mat",
                "Assets/Resources/Materials/Brain/Simplified.mat",
                "Assets/Resources/Materials/Brain/Cut.mat",
                "Assets/Resources/Materials/Sites/Basic.mat",
                "Assets/Resources/Materials/Sites/Blacklisted.mat",
                "Assets/Resources/Materials/ROI/ROI.mat",
                "Assets/Resources/Materials/Rings/Selected.mat",
                "Assets/Resources/Materials/UI/UIMask.mat"
            };
            foreach (string path in materialPaths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.That(material, Is.Not.Null, path);
                Assert.That(material.shader, Is.Not.Null, $"{path} has no shader");
            }

            for (int index = 0; index <= 17; index++)
            {
                string path = $"Assets/Resources/Colormaps/colormap_{index}.png";
                Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(path), Is.Not.Null, path);
            }

            string[] iconPaths =
            {
                "Assets/Resources/Themes/Main/Settings/Images/Icons/HBP Icon.asset",
                "Assets/Resources/Themes/Main/Settings/Images/Icons/Save Icon.asset",
                "Assets/Resources/Themes/Main/Settings/Images/Icons/Load Icon.asset",
                "Assets/Resources/Themes/Main/Settings/Images/Icons/Graph Icon.asset",
                "Assets/Resources/Themes/Main/Settings/Images/Icons/Matrice Icon.asset",
                "Assets/Resources/Themes/Main/Settings/Images/Icons/Site Icon.asset",
                "Assets/Resources/Themes/Main/Settings/Images/Icons/ROI Icon.asset",
                "Assets/Resources/Themes/Main/Settings/Images/Icons/Configuration Icon.asset",
                "Assets/Resources/Themes/Main/Settings/Images/Icons/Photo Icon.asset"
            };
            foreach (string path in iconPaths)
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path), Is.Not.Null, path);
            }

            Assert.That(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Resources/Objects/Shared Materials.asset"), Is.Not.Null);
        }

        [Test]
        public void TestAssemblyReferences_KeepEditModeAndPlayModeBoundariesExplicit()
        {
            AsmdefDefinition serialization = LoadAsmdef("Assets/Tests/EditMode/HBP.Serialization.Tests/HBP.Serialization.Tests.asmdef");
            Assert.That(serialization.references, Is.EquivalentTo(new[]
            {
                "HBP.Core.Runtime",
                "HBP.Data.Runtime",
                "HBP.Dev.Editor",
                "HBP.RenderModelAdapters.Runtime",
                "CRNL.HiBoP.Contracts",
                "CRNL.HiBoP.RenderModel",
                "UniTask"
            }));
            Assert.That(serialization.includePlatforms, Is.EquivalentTo(new[] { "Editor" }));

            AsmdefDefinition projectWorkflow = LoadAsmdef("Assets/Tests/EditMode/HBP.ProjectWorkflow.Tests/HBP.ProjectWorkflow.Tests.asmdef");
            Assert.That(projectWorkflow.references, Is.EquivalentTo(new[] { "HBP.Core.Runtime", "HBP.Data.Runtime", "HBP.UI.Runtime", "UniTask" }));
            Assert.That(projectWorkflow.includePlatforms, Is.EquivalentTo(new[] { "Editor" }));

            foreach (string path in Directory.GetFiles("Assets/Tests/PlayMode", "*.asmdef", SearchOption.AllDirectories))
            {
                AsmdefDefinition asmdef = LoadAsmdef(path);
                Assert.That(asmdef.includePlatforms, Is.Empty, $"{asmdef.name} should remain runnable as PlayMode tests");
                Assert.That(asmdef.references, Does.Not.Contain("HBP.Serialization.Tests"), $"{asmdef.name} must not depend on EditMode tests");
                Assert.That(asmdef.references, Does.Not.Contain("HBP.ProjectWorkflow.Tests"), $"{asmdef.name} must not depend on EditMode tests");
            }

            foreach (string path in Directory.GetFiles("Assets/Scripts", "*.asmdef", SearchOption.AllDirectories))
            {
                AsmdefDefinition asmdef = LoadAsmdef(path);
                Assert.That(asmdef.references.Where(reference => reference.Contains(".Tests")), Is.Empty, $"{asmdef.name} must not reference test assemblies");
            }
        }

        [Test]
        public void ReferencedTextMeshProFonts_Exist()
        {
            string[] requiredAssets =
            {
                "Assets/TextMesh Pro/Fonts/Roboto/Roboto-Regular SDF.asset",
                "Assets/TextMesh Pro/Fonts/Roboto/Roboto-Bold SDF.asset",
                "Assets/TextMesh Pro/Resources/TMP Settings.asset"
            };

            foreach (string path in requiredAssets)
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path), Is.Not.Null, path);
            }
        }

        private static GameObject LoadPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            return prefab;
        }

        private static void AssertPrefabHasComponents(string path, params string[] componentTypeNames)
        {
            GameObject prefab = LoadPrefab(path);
            foreach (string componentTypeName in componentTypeNames)
            {
                Assert.That(FindComponent(prefab, componentTypeName), Is.Not.Null, $"{path} must contain {componentTypeName}");
            }
        }

        private static void AssertSceneContainsComponent(Scene scene, string componentTypeName)
        {
            bool found = scene.GetRootGameObjects().Any(root => HasComponent(root, componentTypeName));
            Assert.That(found, Is.True, $"{scene.path} must contain {componentTypeName}");
        }

        private static Component FindComponent(GameObject root, string componentTypeName)
        {
            return root.GetComponentsInChildren<Component>(true).Where(component => component != null).FirstOrDefault(component => component.GetType().FullName == componentTypeName);
        }

        private static bool HasComponent(GameObject root, string componentTypeName)
        {
            return FindComponent(root, componentTypeName) != null;
        }

        private static void AssertSerializedReferences(Component component, params string[] propertyNames)
        {
            Assert.That(component, Is.Not.Null);
            SerializedObject serializedObject = new(component);
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyName);
                Assert.That(property, Is.Not.Null, $"{component.GetType().FullName}.{propertyName}");
                Assert.That(property.propertyType, Is.EqualTo(SerializedPropertyType.ObjectReference), $"{component.GetType().FullName}.{propertyName}");
                Assert.That(property.objectReferenceValue, Is.Not.Null, $"{component.GetType().FullName}.{propertyName}");
            }
        }

        private static AsmdefDefinition LoadAsmdef(string path)
        {
            AsmdefDefinition definition = JsonUtility.FromJson<AsmdefDefinition>(File.ReadAllText(path));
            Assert.That(definition, Is.Not.Null, path);
            return definition;
        }

        private static int CountMissingScripts(GameObject root)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
            foreach (Transform child in root.transform)
            {
                count += CountMissingScripts(child.gameObject);
            }

            return count;
        }

        [Serializable]
        private sealed class AsmdefDefinition
        {
            public string name;
            public string[] references = Array.Empty<string>();
            public string[] includePlatforms = Array.Empty<string>();
        }
    }
}
