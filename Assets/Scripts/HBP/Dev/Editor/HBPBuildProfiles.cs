using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.OpenXR;

namespace HBP.Dev
{
    public sealed class HBPBuildProfiles : IPreprocessBuildWithReport
    {
        public const string DesktopPath = "Assets/Settings/BuildProfiles/DesktopWindows.asset";
        public const string QuestPath = "Assets/Settings/BuildProfiles/Quest.asset";
        public const string DesktopScene = "Assets/_Scenes/HiBoP.unity";

        public const string QuestScene = "Assets/_Scenes/QuestBootstrap.unity";

        // XRBuildHelper preloads settings at order 0 even when no loader is used.
        public int callbackOrder => 1000;

        public static BuildProfile Load(bool quest)
        {
            string path = quest ? QuestPath : DesktopPath;
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
            if (profile == null) throw new BuildFailedException($"Missing build profile: {path}");
            return profile;
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android || report.summary.platform == BuildTarget.StandaloneWindows64)
            {
                Validate(report.summary.platform);
                RemoveUnusedOpenXRPreload(report.summary.platform);
            }
        }

        public static void RemoveUnusedOpenXRPreload(BuildTarget target)
        {
            var xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildPipeline.GetBuildTargetGroup(target));
            if (xr != null && xr.Manager != null && xr.Manager.activeLoaders.Any(loader => loader is OpenXRLoader)) return;
            PlayerSettings.SetPreloadedAssets(PlayerSettings.GetPreloadedAssets().Where(asset => asset is not OpenXRSettings).ToArray());
        }

        public static void Validate(BuildTarget target)
        {
            bool quest = target == BuildTarget.Android;
            var profile = Load(quest);
            if (BuildProfile.GetActiveBuildProfile() != profile)
                throw new BuildFailedException($"Activate {profile.name} before building {target}.");
            string scene = quest ? QuestScene : DesktopScene;
            if (!profile.overrideGlobalScenes || !profile.GetScenesForBuild().Where(s => s.enabled).Select(s => s.path).SequenceEqual(new[] { scene }))
                throw new BuildFailedException($"{profile.name} must build only {scene}.");
            string expectedDefine = quest ? "HIBOP_QUEST" : "HIBOP_DESKTOP";
            string forbiddenDefine = quest ? "HIBOP_DESKTOP" : "HIBOP_QUEST";
            if (!profile.scriptingDefines.Contains(expectedDefine) || profile.scriptingDefines.Contains(forbiddenDefine))
                throw new BuildFailedException($"Incorrect composition defines in {profile.name}.");
            var core = CompilationPipeline.GetAssemblies(AssembliesType.Player).Single(a => a.name == "HBP.Core.Runtime");
            if (!core.defines.Contains("ENABLE_INPUT_SYSTEM") || core.defines.Contains("ENABLE_LEGACY_INPUT_MANAGER"))
                throw new BuildFailedException("Input System only (New) is required for both Players.");
            var desktopXR = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (desktopXR != null && (desktopXR.InitManagerOnStart || (desktopXR.Manager != null && desktopXR.Manager.activeLoaders.Count > 0)))
                throw new BuildFailedException("Desktop must not initialize an XR loader.");
            if (!quest) return;
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP || PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
                throw new BuildFailedException("Quest requires IL2CPP and ARM64 only.");
            foreach (var plugin in PluginImporter.GetAllImporters().Where(p => p.isNativePlugin && p.assetPath.StartsWith("Assets/")))
            {
                if (plugin.GetCompatibleWithPlatform(BuildTarget.Android) && (plugin.assetPath != "Assets/Plugins/Native/Android/arm64-v8a/libhbp_core.so" || plugin.GetCompatibleWithAnyPlatform() || plugin.GetCompatibleWithEditor() || plugin.GetPlatformData(BuildTarget.Android, "CPU") != "ARM64"))
                    throw new BuildFailedException($"Quest bootstrap must not include Desktop native plugin: {plugin.assetPath}");
            }
        }

        public static void WriteReport(BuildReport report, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var profile = BuildProfile.GetActiveBuildProfile();
            var plugins = PluginImporter.GetAllImporters().Where(p => p.isNativePlugin).Select(p => new
            {
                path = p.assetPath,
                included = p.GetCompatibleWithPlatform(report.summary.platform)
            }).ToArray();
            var packed = report.packedAssets.SelectMany(p => p.contents).GroupBy(p => p.sourceAssetPath).Select(g => new { path = g.Key, bytes = g.Sum(p => (long)p.packedSize) }).OrderByDescending(p => p.bytes).ToArray();
            var value = new
            {
                profile = AssetDatabase.GetAssetPath(profile),
                unity = Application.unityVersion,
                target = report.summary.platform.ToString(),
                result = report.summary.result.ToString(),
                bytes = report.summary.totalSize,
                seconds = report.summary.totalTime.TotalSeconds,
                errors = report.summary.totalErrors,
                warnings = report.summary.totalWarnings,
                scenes = profile == null ? new[] { DesktopScene } : profile.GetScenesForBuild().Where(s => s.enabled).Select(s => s.path).ToArray(),
                plugins,
                files = report.GetFiles().Select(f => new { f.path, f.role, f.size }),
                packedAssets = packed,
                resourcesBytes = packed.Where(p => p.path.Contains("/Resources/")).Sum(p => p.bytes),
                compiledAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player).Select(a => a.name).OrderBy(n => n).ToArray()
            };
            File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented));
        }
    }
}
