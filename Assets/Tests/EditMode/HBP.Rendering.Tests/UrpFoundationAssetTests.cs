using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HBP.Tests.Rendering
{
    public class UrpFoundationAssetTests
    {
        private const string PipelinePath = "Assets/Settings/Rendering/HBP-Desktop-URP.asset";
        private const string RendererPath = "Assets/Settings/Rendering/HBP-Desktop-Renderer.asset";
        private const string GlobalSettingsPath = "Assets/Settings/Rendering/HBP-Desktop-URP-GlobalSettings.asset";

        [Test]
        public void PipelineAsset_IsSerializedAndReferencesTheForwardRenderer()
        {
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);

            Assert.That(pipeline, Is.Not.Null);
            Assert.That(renderer, Is.Not.Null);

            SerializedObject pipelineSettings = new SerializedObject(pipeline);
            UnityEngine.Object referencedRenderer = pipelineSettings.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue;

            Assert.That(referencedRenderer, Is.SameAs(renderer));
            Assert.That(renderer.renderingMode, Is.EqualTo(RenderingMode.Forward));
        }

        [Test]
        public void PipelineAsset_HasTheInitialDesktopConfiguration()
        {
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);

            Assert.That(pipeline.supportsHDR, Is.False);
            Assert.That(pipeline.msaaSampleCount, Is.EqualTo(1));
            Assert.That(pipeline.renderScale, Is.EqualTo(1.0f));
            Assert.That(pipeline.supportsCameraDepthTexture, Is.False);
            Assert.That(pipeline.supportsCameraOpaqueTexture, Is.False);
            Assert.That(pipeline.mainLightRenderingMode, Is.EqualTo(LightRenderingMode.PerPixel));
            Assert.That(pipeline.supportsMainLightShadows, Is.False);
            Assert.That(pipeline.additionalLightsRenderingMode, Is.EqualTo(LightRenderingMode.Disabled));
            Assert.That(pipeline.supportsAdditionalLightShadows, Is.False);
            Assert.That(pipeline.supportsSoftShadows, Is.False);
            Assert.That(pipeline.useSRPBatcher, Is.True);
            Assert.That(pipeline.useFastSRGBLinearConversion, Is.False);
        }

        [Test]
        public void GlobalSettings_AreRegisteredForUrpWithRenderGraphEnabled()
        {
            RenderPipelineGlobalSettings expected = AssetDatabase.LoadAssetAtPath<RenderPipelineGlobalSettings>(GlobalSettingsPath);
            RenderPipelineGlobalSettings registered = EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset(typeof(UniversalRenderPipeline));

            Assert.That(expected, Is.Not.Null);
            Assert.That(registered, Is.SameAs(expected));

            object renderGraphSettings = FindManagedSetting(expected, "RenderGraphSettings");
            PropertyInfo compatibilityMode = renderGraphSettings.GetType().GetProperty("enableRenderCompatibilityMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(compatibilityMode, Is.Not.Null);
            Assert.That((bool)compatibilityMode.GetValue(renderGraphSettings), Is.False);
        }

        [Test]
        public void GlobalPipelineSwitch_UsesUrpAtEveryQualityLevel()
        {
            UniversalRenderPipelineAsset expected = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);

            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(expected));
            for (int index = 0; index < QualitySettings.count; ++index)
                Assert.That(QualitySettings.GetRenderPipelineAssetAt(index), Is.SameAs(expected), $"Quality level {index}");
        }

        [TestCase("HBP/Brain")]
        [TestCase("HBP/Brain/Transparent")]
        [TestCase("HBP/Cut")]
        [TestCase("HBP/Cut/Transparent")]
        [TestCase("HBP/Utility/UnlitColor")]
        [TestCase("HBP/Site")]
        [TestCase("HBP/Site/Selection")]
        [TestCase("HBP/ROI/Wireframe")]
        [TestCase("HBP/UI/Texture")]
        [TestCase("HBP/UI/Mask")]
        public void FoundationShader_ImportsWithoutCompilerErrors(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);

            Assert.That(shader, Is.Not.Null, shaderName);
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False, shaderName);
        }

        [Test]
        public void ScientificAlphaTexture_IsImportedAsLinearData()
        {
            TextureImporter importer = AssetImporter.GetAtPath("Assets/Resources/Textures/alpha.png") as TextureImporter;

            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.sRGBTexture, Is.False);
        }

        private static object FindManagedSetting(RenderPipelineGlobalSettings globalSettings, string typeName)
        {
            SerializedProperty list = new SerializedObject(globalSettings).FindProperty("m_Settings.m_SettingsList.m_List");

            for (int index = 0; index < list.arraySize; ++index)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(index);
                if (element.managedReferenceFullTypename.EndsWith(typeName, StringComparison.Ordinal))
                    return element.managedReferenceValue;
            }

            Assert.Fail($"Global setting {typeName} was not found.");
            return null;
        }
    }
}
