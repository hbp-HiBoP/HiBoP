using System.IO;
using System.Linq;
using HBP.Dev;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.XR.OpenXR;

namespace HBP.Tests.PlatformConfiguration
{
    public class BuildProfileConfigurationTests
    {
        [Test]
        public void QuestNetworkBridge_IsIncludedOnlyOnAndroid()
        {
            var plugin = AssetImporter.GetAtPath("Assets/Plugins/Android/NetworkMonitor.java") as PluginImporter;
            Assert.That(plugin, Is.Not.Null);
            Assert.That(plugin.GetCompatibleWithAnyPlatform(), Is.False);
            Assert.That(plugin.GetCompatibleWithEditor(), Is.False);
            Assert.That(plugin.GetCompatibleWithPlatform(BuildTarget.Android), Is.True);
            foreach (var target in new[] { BuildTarget.StandaloneWindows64, BuildTarget.StandaloneLinux64, BuildTarget.StandaloneOSX })
                Assert.That(plugin.GetCompatibleWithPlatform(target), Is.False);
        }

        [Test]
        public void AndroidNativeCore_IsArm64OnlyAndExcludedFromDesktop()
        {
            var plugin = AssetImporter.GetAtPath("Assets/Plugins/Native/Android/arm64-v8a/libhbp_core.so") as PluginImporter;
            Assert.That(plugin, Is.Not.Null);
            Assert.That(plugin.GetCompatibleWithAnyPlatform(), Is.False);
            Assert.That(plugin.GetCompatibleWithEditor(), Is.False);
            Assert.That(plugin.GetCompatibleWithPlatform(BuildTarget.Android), Is.True);
            Assert.That(plugin.GetPlatformData(BuildTarget.Android, "CPU"), Is.EqualTo("ARM64"));
            foreach (var target in new[] { BuildTarget.StandaloneWindows64, BuildTarget.StandaloneLinux64, BuildTarget.StandaloneOSX })
                Assert.That(plugin.GetCompatibleWithPlatform(target), Is.False);
        }

        [Test]
        public void BuildWithoutXR_DropsOnlyOpenXRPreload()
        {
            var original = PlayerSettings.GetPreloadedAssets();
            var xr = ScriptableObject.CreateInstance<OpenXRSettings>();
            var unrelated = new TextAsset("Keep this preloaded asset");
            try
            {
                PlayerSettings.SetPreloadedAssets(new Object[] { unrelated, xr });
                HBPBuildProfiles.RemoveUnusedOpenXRPreload(BuildTarget.StandaloneWindows64);
                Assert.That(PlayerSettings.GetPreloadedAssets(), Is.EqualTo(new Object[] { unrelated }));
            }
            finally
            {
                PlayerSettings.SetPreloadedAssets(original);
                Object.DestroyImmediate(xr);
                Object.DestroyImmediate(unrelated);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Profiles_IsolateScenesDefinesAndPreserveNewInput(bool quest)
        {
            var profile = HBPBuildProfiles.Load(quest);
            Assert.That(profile.overrideGlobalScenes, Is.True);
            Assert.That(profile.GetScenesForBuild().Select(s => s.path), Is.EqualTo(new[] { quest ? HBPBuildProfiles.QuestScene : HBPBuildProfiles.DesktopScene }));
            Assert.That(profile.scenes.All(s => s.enabled), Is.True);
            Assert.That(profile.scriptingDefines, Is.EqualTo(new[] { quest ? "HIBOP_QUEST" : "HIBOP_DESKTOP" }));
            var yaml = File.ReadAllText(AssetDatabase.GetAssetPath(profile));
            Assert.That(yaml, Does.Contain("activeInputHandler: 1"), "Each profile must serialize New, including when global settings change.");
            Assert.That(new SerializedObject(profile).FindProperty("m_BuildTarget").intValue, Is.EqualTo((int)(quest ? BuildTarget.Android : BuildTarget.StandaloneWindows64)));
        }

        [Test]
        public void QuestBootstrap_UsesCommonContentWithoutDesktopPresentation()
        {
            var dependencies = AssetDatabase.GetDependencies(HBPBuildProfiles.QuestScene, true);
            Assert.That(dependencies, Does.Contain("Assets/Prefabs/Quest/QuestBootstrap.prefab"));
            Assert.That(dependencies, Does.Contain("Assets/Prefabs/3D/Scenes/Scene 3D Content.prefab"));
            Assert.That(dependencies, Does.Not.Contain("Assets/Scripts/HBP/Data/Module3D/DesktopScenePresentation.cs"));
            Assert.That(dependencies, Does.Not.Contain("Assets/Scripts/HBP/Data/Module3D/View3D.cs"));
            Assert.That(dependencies.Any(p => p.StartsWith("Assets/Tests/Support/")), Is.False);
            Assert.That(dependencies.Any(p => p.StartsWith("Assets/Prefabs/Managers/")), Is.False);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            Assert.That(prefab.GetComponentsInChildren<Camera>(true).Length, Is.EqualTo(1));
        }

        [Test]
        public void ActiveProfile_PassesBuildGuard()
        {
            HBPBuildProfiles.Validate(EditorUserBuildSettings.activeBuildTarget);
        }

        [Test]
        public void BuildGuard_RejectsWrongSceneBeforeBuilding()
        {
            var profile = BuildProfile.GetActiveBuildProfile();
            Assert.That(profile, Is.Not.Null, "Run with -activeBuildProfile.");
            var scenes = profile.scenes;
            try
            {
                profile.scenes = new[] { new EditorBuildSettingsScene(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ? HBPBuildProfiles.DesktopScene : HBPBuildProfiles.QuestScene, true) };
                Assert.Throws<BuildFailedException>(() => HBPBuildProfiles.Validate(EditorUserBuildSettings.activeBuildTarget));
            }
            finally
            {
                profile.scenes = scenes;
            }
        }

        [Test]
        public void QuestBuildGuard_RejectsWrongArchitecture()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) Assert.Ignore("Android profile only.");
            var architecture = PlayerSettings.Android.targetArchitectures;
            try
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
                Assert.Throws<BuildFailedException>(() => HBPBuildProfiles.Validate(BuildTarget.Android));
            }
            finally
            {
                PlayerSettings.Android.targetArchitectures = architecture;
            }
        }

        [TestCase("hbp_core")]
        [TestCase("hbp_math")]
        public void QuestBuildGuard_RejectsMissingNativeLibrary(string name)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) Assert.Ignore("Android profile only.");
            var plugin = AssetImporter.GetAtPath($"Assets/Plugins/Native/Android/arm64-v8a/lib{name}.so") as PluginImporter;
            Assert.That(plugin, Is.Not.Null);
            bool included = plugin.GetCompatibleWithPlatform(BuildTarget.Android);
            try
            {
                plugin.SetCompatibleWithPlatform(BuildTarget.Android, false);
                Assert.Throws<BuildFailedException>(() => HBPBuildProfiles.Validate(BuildTarget.Android));
            }
            finally
            {
                plugin.SetCompatibleWithPlatform(BuildTarget.Android, included);
            }
        }

        [TestCase("keystore path")]
        [TestCase("existing keystore file")]
        [TestCase("key alias")]
        [TestCase("keystore password")]
        [TestCase("key alias password")]
        public void QuestSigning_IncompleteConfigurationStopsBeforeBuild(string missing)
        {
            WithQuestSigning(path =>
            {
                switch (missing)
                {
                    case "keystore path": PlayerSettings.Android.keystoreName = ""; break;
                    case "existing keystore file": File.Delete(path); break;
                    case "key alias": PlayerSettings.Android.keyaliasName = ""; break;
                    case "keystore password": PlayerSettings.Android.keystorePass = ""; break;
                    case "key alias password": PlayerSettings.Android.keyaliasPass = ""; break;
                }

                var before = CaptureSigningSettings();
                var profile = BuildProfile.GetActiveBuildProfile();
                string version = PlayerSettings.bundleVersion;
                string buildInfo = File.ReadAllText("Assets/Resources/BuildInfo.json");
                string output = path + ".build";
                var exception = Assert.Throws<BuildFailedException>(() => HBPBuilder.BuildQuest(output));
                Assert.That(exception.Message, Does.Contain(missing).And.Contain("Quest Build Profile / Android Publishing Settings"));
                Assert.That(exception.Message, Does.Not.Contain("test-store-password").And.Not.Contain("test-key-password").And.Not.Contain(path));
                Assert.That(Directory.Exists(output), Is.False, "An incomplete signing configuration must fail before build preparation.");
                Assert.That(PlayerSettings.bundleVersion, Is.EqualTo(version));
                Assert.That(File.ReadAllText("Assets/Resources/BuildInfo.json"), Is.EqualTo(buildInfo));
                Assert.That(BuildProfile.GetActiveBuildProfile(), Is.SameAs(profile));
                Assert.That(CaptureSigningSettings() == before, Is.True, "The guard must not mutate signing settings.");
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void QuestSigning_CompleteConfigurationPassesWithoutMutation(bool relativePath)
        {
            WithQuestSigning(path =>
            {
                if (relativePath) PlayerSettings.Android.keystoreName = "Temp/" + Path.GetFileName(path);
                var before = CaptureSigningSettings();
                Assert.DoesNotThrow(HBPBuilder.ValidateQuestSigning);
                Assert.That(CaptureSigningSettings() == before, Is.True, "The guard must preserve the configured signing identity and credentials.");
            });
        }

        [Test]
        public void QuestSigning_DebugSigningDoesNotRequireCustomCredentials()
        {
            WithQuestSigning(path =>
            {
                PlayerSettings.Android.useCustomKeystore = false;
                PlayerSettings.Android.keystoreName = "";
                PlayerSettings.Android.keyaliasName = "";
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasPass = "";
                var before = CaptureSigningSettings();
                Assert.DoesNotThrow(HBPBuilder.ValidateQuestSigning);
                Assert.That(CaptureSigningSettings() == before, Is.True);
            });
        }

        private static (bool custom, string path, string alias, string storePassword, string keyPassword) CaptureSigningSettings()
        {
            return (PlayerSettings.Android.useCustomKeystore, PlayerSettings.Android.keystoreName, PlayerSettings.Android.keyaliasName, PlayerSettings.Android.keystorePass, PlayerSettings.Android.keyaliasPass);
        }

        private static void WithQuestSigning(System.Action<string> test)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android || BuildProfile.GetActiveBuildProfile() != HBPBuildProfiles.Load(true))
                Assert.Ignore("Run with the Quest Build Profile active.");
            var original = CaptureSigningSettings();
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "quest-signing-" + System.Guid.NewGuid().ToString("N") + ".keystore");
            try
            {
                // A placeholder proves readiness checks do not claim cryptographic validation.
                File.WriteAllText(path, "Signing readiness test placeholder; not a private key.");
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = path;
                PlayerSettings.Android.keyaliasName = "test-alias";
                PlayerSettings.Android.keystorePass = "test-store-password";
                PlayerSettings.Android.keyaliasPass = "test-key-password";
                test(path);
            }
            finally
            {
                PlayerSettings.Android.useCustomKeystore = original.custom;
                PlayerSettings.Android.keystoreName = original.path;
                PlayerSettings.Android.keyaliasName = original.alias;
                PlayerSettings.Android.keystorePass = original.storePassword;
                PlayerSettings.Android.keyaliasPass = original.keyPassword;
                File.Delete(path);
            }
        }
    }
}
