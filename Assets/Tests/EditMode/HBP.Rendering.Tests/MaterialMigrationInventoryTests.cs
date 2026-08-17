using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HBP.Tests.Rendering
{
    public class MaterialMigrationInventoryTests
    {
        private const string InventoryPath = "Assets/Settings/Rendering/HBP-Material-Migration-Inventory.json";

        [Test]
        public void EveryActiveMaterialHasAnUpToDateMigrationStrategy()
        {
            MaterialMigrationInventory inventory = LoadInventory();
            Dictionary<string, MaterialMigrationEntry> entryByMaterial = inventory.entries.ToDictionary(entry => entry.material, StringComparer.Ordinal);
            IReadOnlyCollection<string> activeMaterials = FindActiveMaterialPaths();

            CollectionAssert.AreEquivalent(activeMaterials, entryByMaterial.Keys, "The material inventory must exactly match the active-material discovery scope.");

            foreach (string materialPath in activeMaterials)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                MaterialMigrationEntry entry = entryByMaterial[materialPath];

                Assert.That(material, Is.Not.Null, materialPath);
                Assert.That(material.shader, Is.Not.Null, materialPath);
                Assert.That(entry.currentShader, Is.EqualTo(material.shader.name), materialPath);
                Assert.That(entry.targetShader, Is.EqualTo(material.shader.name), materialPath);
                Assert.That(material.shader.isSupported, Is.True, materialPath);
                Assert.That(ShaderUtil.ShaderHasError(material.shader), Is.False, materialPath);
                Assert.That(entry.targetShader, Is.Not.Null.And.Not.Empty, materialPath);
                Assert.That(entry.phase, Is.InRange(2, 3), materialPath);
            }
        }

        private static MaterialMigrationInventory LoadInventory()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(InventoryPath);
            Assert.That(asset, Is.Not.Null, InventoryPath);

            MaterialMigrationInventory inventory = JsonUtility.FromJson<MaterialMigrationInventory>(asset.text);
            Assert.That(inventory, Is.Not.Null);
            Assert.That(inventory.entries, Is.Not.Null.And.Not.Empty);
            Assert.That(inventory.entries.Select(entry => entry.material).Distinct().Count(), Is.EqualTo(inventory.entries.Length), "The inventory contains duplicate material paths.");
            return inventory;
        }

        private static IReadOnlyCollection<string> FindActiveMaterialPaths()
        {
            HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    AddMaterialDependencies(scene.path, paths);
            }

            foreach (string prefabGuid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
                AddMaterialDependencies(AssetDatabase.GUIDToAssetPath(prefabGuid), paths);

            foreach (string materialGuid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Resources" }))
            {
                string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
                if (Path.GetExtension(materialPath).Equals(".mat", StringComparison.OrdinalIgnoreCase))
                    paths.Add(materialPath);
            }

            return paths;
        }

        private static void AddMaterialDependencies(string assetPath, ISet<string> paths)
        {
            foreach (string dependency in AssetDatabase.GetDependencies(assetPath, true))
            {
                if (Path.GetExtension(dependency).Equals(".mat", StringComparison.OrdinalIgnoreCase))
                    paths.Add(dependency);
            }
        }

        [Serializable]
        private class MaterialMigrationInventory
        {
            public MaterialMigrationEntry[] entries;
        }

        [Serializable]
        private class MaterialMigrationEntry
        {
            public string material;
            public string currentShader;
            public string targetShader;
            public int phase;
        }
    }
}
