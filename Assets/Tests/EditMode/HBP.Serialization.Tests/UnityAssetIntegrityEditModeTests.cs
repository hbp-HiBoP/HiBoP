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
        [Test]
        public void ProjectPrefabs_DoNotContainMissingScripts()
        {
            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs", "Assets/Resources/Prefabs" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .ToArray();

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
            const string scenePath = "Assets/_Scenes/HiBoP.unity";
            Assert.That(File.Exists(scenePath), Is.True);

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
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
                Assert.That(AssetDatabase.LoadAssetAtPath<Object>(path), Is.Not.Null, path);
            }
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
    }
}
