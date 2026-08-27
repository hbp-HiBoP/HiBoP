using System.Linq;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Tests.Serialization
{
    public class SurfaceInflationPhase6Tests
    {
        [Test]
        public void VisualizationConfiguration_SurfaceRepresentationDefaultsToAnatomicalAndSurvivesCopies()
        {
            VisualizationConfiguration legacy = ClassLoaderSaver.LoadFromJsonString<VisualizationConfiguration>("{\"ID\":\"legacy-inflation-configuration\",\"Mesh\":\"Grey matter\"}");
            Assert.That(legacy.SurfaceRepresentation, Is.EqualTo(SurfaceRepresentation.Anatomical));

            VisualizationConfiguration source = new() { SurfaceRepresentation = SurfaceRepresentation.Inflated };
            VisualizationConfiguration clone = (VisualizationConfiguration)source.Clone();
            VisualizationConfiguration copy = new();
            copy.Copy(source);

            Assert.That(clone.SurfaceRepresentation, Is.EqualTo(SurfaceRepresentation.Inflated));
            Assert.That(copy.SurfaceRepresentation, Is.EqualTo(SurfaceRepresentation.Inflated));
        }

        [Test]
        public void Base3DScene_InflatedCutsInformationIsDeduplicatedPerSceneLifetime()
        {
            GameObject firstObject = new("First inflation information scene");
            GameObject secondObject = new("Second inflation information scene");
            Base3DScene first = firstObject.AddComponent<Base3DScene>();
            Base3DScene second = secondObject.AddComponent<Base3DScene>();
            try
            {
                Assert.That(first.TryMarkInflatedCutsInformationShown(), Is.True);
                Assert.That(first.TryMarkInflatedCutsInformationShown(), Is.False);
                Assert.That(second.TryMarkInflatedCutsInformationShown(), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
            }
        }

        [Test]
        public void BrainShaders_ExposeInflationBlendForOpaqueAndTransparentRendering()
        {
            Shader opaque = Shader.Find("HBP/Brain");
            Shader transparent = Shader.Find("HBP/Brain/Transparent");

            Assert.That(opaque, Is.Not.Null);
            Assert.That(transparent, Is.Not.Null);
            Material opaqueMaterial = new(opaque);
            Material transparentMaterial = new(transparent);
            try
            {
                Assert.That(opaqueMaterial.HasProperty("_InflationBlend"), Is.True);
                Assert.That(transparentMaterial.HasProperty("_InflationBlend"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(opaqueMaterial);
                Object.DestroyImmediate(transparentMaterial);
            }
        }

        [Test]
        public void SceneToolbar_PlacesSerializedRepresentationToggleAfterMeshSelectorWithEmptySprite()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/UI/3D Menu.prefab");
            Assert.That(prefab, Is.Not.Null);
            Transform sceneSettings = prefab.GetComponentsInChildren<Transform>(true).Single(transform => transform.name == "Scene Settings");
            Transform meshSelector = sceneSettings.Cast<Transform>().Single(transform => transform.name == "Brain Selector");
            Transform representationToggle = sceneSettings.Cast<Transform>().Single(transform => transform.name == "Surface Representation Toggle");

            Assert.That(representationToggle.GetSiblingIndex(), Is.EqualTo(meshSelector.GetSiblingIndex() + 1));
            MonoBehaviour tool = representationToggle.GetComponents<MonoBehaviour>().Single(component => component.GetType().FullName == "HBP.UI.Toolbar.SurfaceRepresentationToggle");
            SerializedObject serializedTool = new(tool);
            Assert.That(serializedTool.FindProperty("m_Toggle").objectReferenceValue, Is.Not.Null);
            Assert.That(serializedTool.FindProperty("m_Tooltip").objectReferenceValue, Is.Not.Null);
            Image icon = serializedTool.FindProperty("m_Icon").objectReferenceValue as Image;
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.sprite, Is.Null);

            MonoBehaviour toolbar = sceneSettings.GetComponents<MonoBehaviour>().Single(component => component.GetType().FullName == "HBP.UI.Toolbar.SceneSettingsToolbar");
            Assert.That(new SerializedObject(toolbar).FindProperty("m_SurfaceRepresentationToggle").objectReferenceValue, Is.SameAs(tool));
        }
    }
}
