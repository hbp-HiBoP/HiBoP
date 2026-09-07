using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.InputSystem;
using XRController = UnityEngine.InputSystem.XR.XRController;
using UnityEngine.XR.Interaction.Toolkit;

namespace HBP.Tests.PlatformConfiguration
{
    public class QuestPackageConfigurationTests
    {
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [Test]
        public void UnifiedPackages_MatchManifestLockAndInstalledVersions()
        {
            JObject manifest = JObject.Parse(File.ReadAllText(Path.Combine(Root, "Packages/manifest.json")));
            JObject locked = JObject.Parse(File.ReadAllText(Path.Combine(Root, "Packages/packages-lock.json")));
            foreach (string name in new[] { "com.unity.inputsystem", "com.unity.xr.interaction.toolkit", "com.unity.xr.management", "com.unity.xr.openxr", "com.unity.xr.meta-openxr" })
            {
                PackageInfo installed = PackageInfo.FindForPackageName(name);
                Assert.That(installed, Is.Not.Null, name);
                Assert.That(installed.version, Is.EqualTo((string)manifest["dependencies"][name]), name);
            }

            foreach (JProperty package in ((JObject)locked["dependencies"]).Properties())
            {
                if ((string)package.Value["source"] != "registry" && (string)package.Value["source"] != "builtin") continue;
                PackageInfo installed = PackageInfo.FindForPackageName(package.Name);
                Assert.That(installed, Is.Not.Null, package.Name);
                Assert.That(installed.version, Is.EqualTo((string)package.Value["version"]), package.Name);
                TestContext.Out.WriteLine($"{package.Name}={installed.version}");
            }

            Assert.That(PackageInfo.FindForPackageName("com.unity.xr.hands"), Is.Null, "Hand tracking is outside QUEST-002.");
            Assert.That(typeof(XRInteractionManager).Assembly.GetName().Name, Is.EqualTo("Unity.XR.Interaction.Toolkit"));
        }

        [Test]
        public void InputBackends_MatchActiveTargetAndCompiledPlayerDefines()
        {
            bool android = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
            Assert.That(android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64, Is.True, "Run this qualification on Windows or Android.");
            SerializedObject settings = new(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            Assert.That(settings.FindProperty("activeInputHandler").intValue, Is.EqualTo(1), "Input System only is required on every target.");
            var core = CompilationPipeline.GetAssemblies(AssembliesType.Player).Single(assembly => assembly.name == "HBP.Core.Runtime");
            Assert.That(core.defines, Does.Contain("ENABLE_INPUT_SYSTEM"));
            Assert.That(core.defines.Contains("ENABLE_LEGACY_INPUT_MANAGER"), Is.False);
        }

        [Test]
        public void Desktop_HasNoActiveXRLoader()
        {
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (settings == null) return;
            Assert.That(settings.InitManagerOnStart, Is.False, "Installing packages must not initialize XR on Desktop.");
            if (settings.Manager != null) Assert.That(settings.Manager.activeLoaders, Is.Empty);
        }

        [Test]
        public void InputSystem_XRSpatialActionBindsToController()
        {
            XRController controller = InputSystem.AddDevice<XRController>();
            try
            {
                using InputAction action = new("QuestPosition", InputActionType.Value, "<XRController>/devicePosition");
                action.Enable();
                Assert.That(action.controls, Does.Contain(controller.devicePosition));
            }
            finally
            {
                InputSystem.RemoveDevice(controller);
            }
        }

        [Test]
        public void PlayerScripts_CompileForActiveTargetWithoutChangingPackages()
        {
            string manifestPath = Path.Combine(Root, "Packages/manifest.json");
            string lockPath = Path.Combine(Root, "Packages/packages-lock.json");
            byte[] manifest = File.ReadAllBytes(manifestPath);
            byte[] locked = File.ReadAllBytes(lockPath);
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string output = Path.Combine(Root, ".test-results/quest-002-a/player-scripts", target.ToString());
            Directory.CreateDirectory(output);
            var result = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings
            {
                target = target,
                group = BuildPipeline.GetBuildTargetGroup(target)
            }, output);
            Assert.That(result.assemblies, Is.Not.Empty, "Player compilation failed; inspect the Unity log.");
            Assert.That(File.ReadAllBytes(manifestPath), Is.EqualTo(manifest));
            Assert.That(File.ReadAllBytes(lockPath), Is.EqualTo(locked));
            TestContext.Out.WriteLine($"Compiled {result.assemblies.Count()} player assemblies for {target} into {output}");
        }
    }
}
