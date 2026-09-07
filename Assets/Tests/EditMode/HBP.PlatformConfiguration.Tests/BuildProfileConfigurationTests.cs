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
        public void BuildWithoutXR_DropsOnlyOpenXRPreload()
        {
            var original = PlayerSettings.GetPreloadedAssets();
            var xr = ScriptableObject.CreateInstance<OpenXRSettings>();
            var unrelated = new TextAsset("Keep this preloaded asset");
            try
            {
                PlayerSettings.SetPreloadedAssets(new Object[] { unrelated, xr });
                HBPBuildProfiles.RemoveUnusedOpenXRPreload(EditorUserBuildSettings.activeBuildTarget);
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
        public void QuestBootstrap_DependsOnItsPrefabAndNoDesktopManagers()
        {
            var dependencies = AssetDatabase.GetDependencies(HBPBuildProfiles.QuestScene, true);
            Assert.That(dependencies, Does.Contain("Assets/Prefabs/Quest/QuestBootstrap.prefab"));
            Assert.That(dependencies.Any(p => p.StartsWith("Assets/Scripts/HBP/")), Is.False);
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
    }
}
