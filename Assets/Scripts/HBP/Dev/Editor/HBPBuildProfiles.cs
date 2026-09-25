using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public static string ReadProductVersion()
        {
            JObject metadata = ReadCodeMeta();
            JToken versionToken = metadata["version"];
            if (versionToken == null || versionToken.Type != JTokenType.String || string.IsNullOrWhiteSpace(versionToken.Value<string>()))
            {
                throw new BuildFailedException($"{GetCodeMetaPath()} must contain a non-empty string property named 'version'.");
            }

            return versionToken.Value<string>();
        }

        public static void WriteProductVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) throw new System.ArgumentException("The product version cannot be empty.", nameof(version));
            version = version.Trim();
            JObject metadata = ReadCodeMeta();
            metadata["version"] = version;
            File.WriteAllText(GetCodeMetaPath(), metadata.ToString(Formatting.Indented) + System.Environment.NewLine);

            SynchronizeStoredProductVersion(version);
        }

        public static string ApplyProductVersion()
        {
            string version = ReadProductVersion();
            PlayerSettings.bundleVersion = version;
            if (PlayerSettings.bundleVersion != version)
            {
                throw new BuildFailedException($"Could not apply product version '{version}' to the active Player Settings.");
            }

            BuildProfile activeProfile = BuildProfile.GetActiveBuildProfile();
            if (activeProfile != null) EditorUtility.SetDirty(activeProfile);
            AssetDatabase.SaveAssets();
            return version;
        }

        private static JObject ReadCodeMeta()
        {
            string path = GetCodeMetaPath();
            try
            {
                return JObject.Parse(File.ReadAllText(path));
            }
            catch (System.Exception exception)
            {
                throw new BuildFailedException($"Could not read product version from {path}: {exception.Message}");
            }
        }

        private static string GetCodeMetaPath()
        {
            return Path.Combine(GetProjectRoot(), "codemeta.json");
        }

        private static string GetProjectRoot()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        private static void SynchronizeStoredProductVersion(string version)
        {
            UpdatePlayerSettingsVersion(GetGlobalPlayerSettings(), version);
            UpdateProfilePlayerSettings(Load(false), version);
            UpdateProfilePlayerSettings(Load(true), version);

            PlayerSettings.bundleVersion = version;
            if (PlayerSettings.bundleVersion != version)
            {
                throw new BuildFailedException($"Could not apply product version '{version}' to the active Player Settings.");
            }

            string buildInfoAssetPath = "Assets/Resources/BuildInfo.json";
            string buildInfoPath = Path.Combine(GetProjectRoot(), buildInfoAssetPath);
            try
            {
                JObject buildInfo = JObject.Parse(File.ReadAllText(buildInfoPath));
                buildInfo["Version"] = version;
                File.WriteAllText(buildInfoPath, buildInfo.ToString(Formatting.Indented) + System.Environment.NewLine);
            }
            catch (System.Exception exception)
            {
                throw new System.InvalidOperationException($"Could not synchronize the product version in {buildInfoPath}: {exception.Message}", exception);
            }

            BuildProfile activeProfile = BuildProfile.GetActiveBuildProfile();
            if (activeProfile != null) EditorUtility.SetDirty(activeProfile);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(buildInfoAssetPath, ImportAssetOptions.ForceUpdate);
        }

        private static PlayerSettings GetGlobalPlayerSettings()
        {
            MethodInfo getter = typeof(BuildProfile).GetMethod("GetGlobalPlayerSettings", BindingFlags.Static | BindingFlags.NonPublic);
            PlayerSettings settings = getter?.Invoke(null, null) as PlayerSettings;
            if (settings == null) throw new System.InvalidOperationException("Could not access the global Player Settings object.");
            return settings;
        }

        private static void UpdateProfilePlayerSettings(BuildProfile profile, string version)
        {
            PropertyInfo property = typeof(BuildProfile).GetProperty("playerSettings", BindingFlags.Instance | BindingFlags.NonPublic);
            PlayerSettings settings = property?.GetValue(profile) as PlayerSettings;
            if (settings == null) throw new System.InvalidOperationException($"Could not access Player Settings for build profile '{profile.name}'.");
            UpdatePlayerSettingsVersion(settings, version);
            EditorUtility.SetDirty(profile);
        }

        private static void UpdatePlayerSettingsVersion(PlayerSettings settings, string version)
        {
            SerializedObject serializedSettings = new(settings);
            SerializedProperty bundleVersion = serializedSettings.FindProperty("bundleVersion");
            if (bundleVersion == null || bundleVersion.propertyType != SerializedPropertyType.String)
            {
                throw new System.InvalidOperationException("Could not find the bundleVersion field in Player Settings.");
            }

            bundleVersion.stringValue = version;
            serializedSettings.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
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
            string expectedVersion = ReadProductVersion();
            if (PlayerSettings.bundleVersion != expectedVersion)
                throw new BuildFailedException($"{profile.name} uses bundle version '{PlayerSettings.bundleVersion}', but codemeta.json declares '{expectedVersion}'. Build through HBPBuilder to synchronize it.");
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
            string[] requiredPlugins = { "Assets/Plugins/Native/Android/arm64-v8a/libhbp_core.so", "Assets/Plugins/Native/Android/arm64-v8a/libhbp_math.so" };
            foreach (string path in requiredPlugins)
            {
                var plugin = AssetImporter.GetAtPath(path) as PluginImporter;
                if (plugin == null || !plugin.GetCompatibleWithPlatform(BuildTarget.Android))
                    throw new BuildFailedException($"Quest requires the Android native plugin: {path}");
            }

            foreach (var plugin in PluginImporter.GetAllImporters().Where(p => p.isNativePlugin && p.assetPath.StartsWith("Assets/")))
            {
                if (plugin.GetCompatibleWithPlatform(BuildTarget.Android) && (!requiredPlugins.Contains(plugin.assetPath) || plugin.GetCompatibleWithAnyPlatform() || plugin.GetCompatibleWithEditor() || plugin.GetPlatformData(BuildTarget.Android, "CPU") != "ARM64"))
                    throw new BuildFailedException($"Quest must not include an incompatible native plugin: {plugin.assetPath}");
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

    internal sealed class HBPVersionEditorWindow : EditorWindow
    {
        private string m_Version;
        private string m_OriginalVersion;

        [MenuItem("Tools/Change version")]
        private static void Open()
        {
            try
            {
                HBPVersionEditorWindow window = GetWindow<HBPVersionEditorWindow>("Change version");
                window.m_Version = HBPBuildProfiles.ReadProductVersion();
                window.m_OriginalVersion = window.m_Version;
                window.minSize = new Vector2(340, 145);
                window.Show();
            }
            catch (System.Exception exception)
            {
                EditorUtility.DisplayDialog("Change version", exception.Message, "OK");
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Version in codemeta.json");
            m_Version = EditorGUILayout.TextField("Version", m_Version);
            EditorGUILayout.HelpBox("Saving updates global Player Settings, both build profiles, and BuildInfo. HBPBuilder also reapplies this version before each build.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.Width(80))) Close();

            bool changed = !string.IsNullOrWhiteSpace(m_Version) && m_Version.Trim() != m_OriginalVersion;
            EditorGUI.BeginDisabledGroup(!changed);
            if (GUILayout.Button("Save", GUILayout.Width(80))) Save();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private void Save()
        {
            try
            {
                HBPBuildProfiles.WriteProductVersion(m_Version);
                Close();
            }
            catch (System.Exception exception)
            {
                EditorUtility.DisplayDialog("Change version", exception.Message, "OK");
            }
        }
    }
}
