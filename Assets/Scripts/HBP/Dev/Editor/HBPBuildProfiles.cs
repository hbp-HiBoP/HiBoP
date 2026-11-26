using System.Globalization;
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

        public const string QuestScene = "Assets/_Scenes/HiBoP_Quest.unity";
        public const string QuestUIDemoScene = "Assets/_Scenes/QuestUIDemo.unity";
        public const string QuestUIDemoIdentifier = "fr.crnl.hibop.quest.uidemo";
        public const string QuestSelectionLabScene = "Assets/_Scenes/QuestSelectionLab.unity";
        public const string QuestSelectionLabIdentifier = "fr.crnl.hibop.quest.selectionlab";

        // XRBuildHelper preloads settings at order 0 even when no loader is used.
        public int callbackOrder => 1000;

        public static BuildProfile Load(bool quest)
        {
            string path = quest ? QuestPath : DesktopPath;
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
            if (profile == null) throw new BuildFailedException($"Missing build profile: {path}");
            return profile;
        }

        public static string ReadProductVersion() => ReadProductVersion(out _);

        internal static string ReadProductVersion(out string releaseNotes)
        {
            JObject metadata = ReadCodeMeta();
            JToken versionToken = metadata["version"];
            if (versionToken == null || versionToken.Type != JTokenType.String || string.IsNullOrWhiteSpace(versionToken.Value<string>()))
            {
                throw new BuildFailedException($"{GetCodeMetaPath()} must contain a non-empty string property named 'version'.");
            }

            JToken notesToken = metadata["releaseNotes"];
            releaseNotes = notesToken?.Type == JTokenType.String && !JToken.DeepEquals(notesToken, metadata["downloadUrl"]) ? notesToken.Value<string>() : string.Empty;
            return versionToken.Value<string>();
        }

        public static void WriteProductVersion(string version, string releaseNotes = null)
        {
            if (string.IsNullOrWhiteSpace(version)) throw new System.ArgumentException("The product version cannot be empty.", nameof(version));
            version = version.Trim();
            int questVersionCode = GetQuestVersionCode(version);
            JObject metadata = ReadCodeMeta();
            string repositoryUrl = (string)metadata["codeRepository"];
            if (string.IsNullOrWhiteSpace(repositoryUrl))
                throw new BuildFailedException($"{GetCodeMetaPath()} must contain a repository URL in 'codeRepository'.");

            string releaseUrl = $"{repositoryUrl.Trim().TrimEnd('/')}/releases/tag/{version}";
            metadata["version"] = version;
            metadata["dateModified"] = System.DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            metadata["downloadUrl"] = releaseUrl;
            metadata["releaseNotes"] = string.IsNullOrWhiteSpace(releaseNotes) ? releaseUrl : releaseNotes;

            SynchronizeStoredProductVersion(version, questVersionCode);
            File.WriteAllText(GetCodeMetaPath(), metadata.ToString(Formatting.Indented) + System.Environment.NewLine);
        }

        public static int GetQuestVersionCode(string version)
        {
            if (!System.Version.TryParse(version, out var parsed) || parsed.Build < 0 || parsed.Revision >= 0 || parsed.Minor > 99 || parsed.Build > 99 || parsed.ToString(3) != version)
            {
                throw new BuildFailedException($"Quest requires a version in major.minor.patch format with minor and patch between 0 and 99. Received '{version}'.");
            }

            long code = parsed.Major * 10000L + parsed.Minor * 100 + parsed.Build;
            if (code <= 0 || code > int.MaxValue)
                throw new BuildFailedException($"Product version '{version}' is outside the supported Quest versionCode range.");
            return (int)code;
        }

        public static string ApplyProductVersion()
        {
            string version = ReadProductVersion();
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                PlayerSettings.Android.bundleVersionCode = GetQuestVersionCode(version);
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

        private static void SynchronizeStoredProductVersion(string version, int questVersionCode)
        {
            UpdatePlayerSettingsVersion(GetGlobalPlayerSettings(), version);
            UpdateProfilePlayerSettings(Load(false), version);
            var questProfile = Load(true);
            UpdateProfilePlayerSettings(questProfile, version, questVersionCode);

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
            if (activeProfile == questProfile) PlayerSettings.Android.bundleVersionCode = questVersionCode;
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

        private static void UpdateProfilePlayerSettings(BuildProfile profile, string version, int? androidVersionCode = null)
        {
            PropertyInfo property = typeof(BuildProfile).GetProperty("playerSettings", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo serialize = typeof(BuildProfile).GetMethod("SerializePlayerSettings", BindingFlags.Instance | BindingFlags.NonPublic);
            PlayerSettings settings = property?.GetValue(profile) as PlayerSettings;
            if (settings == null || serialize == null) throw new System.InvalidOperationException($"Could not access or serialize Player Settings for build profile '{profile.name}'.");
            UpdatePlayerSettingsVersion(settings, version, androidVersionCode);
            // Saving the profile alone does not serialize its in-memory Player Settings when it is inactive.
            serialize.Invoke(profile, null);
            EditorUtility.SetDirty(profile);
        }

        private static void UpdatePlayerSettingsVersion(PlayerSettings settings, string version, int? androidVersionCode = null)
        {
            SerializedObject serializedSettings = new(settings);
            SerializedProperty bundleVersion = serializedSettings.FindProperty("bundleVersion");
            if (bundleVersion == null || bundleVersion.propertyType != SerializedPropertyType.String)
            {
                throw new System.InvalidOperationException("Could not find the bundleVersion field in Player Settings.");
            }

            bundleVersion.stringValue = version;
            if (androidVersionCode.HasValue)
            {
                SerializedProperty versionCode = serializedSettings.FindProperty("AndroidBundleVersionCode");
                if (versionCode == null || versionCode.propertyType != SerializedPropertyType.Integer)
                    throw new System.InvalidOperationException("Could not find the AndroidBundleVersionCode field in the Quest Player Settings.");
                versionCode.intValue = androidVersionCode.Value;
            }

            serializedSettings.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if ((HBPBuilder.IsQuestUIDemoBuild || HBPBuilder.IsQuestSelectionLabBuild) && (report.summary.platform != BuildTarget.Android || (report.summary.options & BuildOptions.Development) == 0))
                throw new BuildFailedException("Quest experiment scenes must be Android Development builds.");
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
            if (quest && PlayerSettings.Android.bundleVersionCode != GetQuestVersionCode(expectedVersion))
                throw new BuildFailedException($"{profile.name} has an out-of-date Android Bundle Version Code. Build through HBPBuilder to synchronize it.");
            bool demo = quest && HBPBuilder.IsQuestUIDemoBuild;
            bool lab = quest && HBPBuilder.IsQuestSelectionLabBuild;
            if (demo && PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != QuestUIDemoIdentifier)
                throw new BuildFailedException("The Quest UI demo requires its own application identifier.");
            if (lab && PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != QuestSelectionLabIdentifier)
                throw new BuildFailedException("The Quest selection laboratory requires its own application identifier.");
            string scene = quest ? lab ? QuestSelectionLabScene : demo ? QuestUIDemoScene : QuestScene : DesktopScene;
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
                // Unity also reports Android Java source plugins as native plugins. This bridge is platform-neutral Java, not an ABI-specific library.
                bool networkBridge = plugin.assetPath == "Assets/Plugins/Android/NetworkMonitor.java";
                if (plugin.GetCompatibleWithPlatform(BuildTarget.Android) && (!requiredPlugins.Contains(plugin.assetPath) && !networkBridge || plugin.GetCompatibleWithAnyPlatform() || plugin.GetCompatibleWithEditor() || !networkBridge && plugin.GetPlatformData(BuildTarget.Android, "CPU") != "ARM64"))
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
                androidVersionCode = report.summary.platform == BuildTarget.Android ? (int?)PlayerSettings.Android.bundleVersionCode : null,
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
        private string m_OriginalReleaseNotes;
        private string m_EditedReleaseNotes;
        private Vector2 m_ReleaseNotesScroll;
        private GUIStyle m_ReleaseNotesStyle;

        private string ReleaseNotes => m_EditedReleaseNotes ?? (m_Version?.Trim() == m_OriginalVersion ? m_OriginalReleaseNotes : string.Empty);

        [MenuItem("Tools/Change version")]
        private static void Open()
        {
            try
            {
                HBPVersionEditorWindow window = GetWindow<HBPVersionEditorWindow>("Change version");
                window.m_OriginalVersion = HBPBuildProfiles.ReadProductVersion(out window.m_OriginalReleaseNotes);
                window.m_Version = window.m_OriginalVersion;
                window.m_EditedReleaseNotes = null;
                window.m_ReleaseNotesScroll = Vector2.zero;
                window.minSize = new Vector2(420, 270);
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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Release notes (optional)");
            m_ReleaseNotesStyle ??= new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            m_ReleaseNotesScroll = EditorGUILayout.BeginScrollView(m_ReleaseNotesScroll);
            EditorGUI.BeginChangeCheck();
            string releaseNotes = EditorGUILayout.TextArea(ReleaseNotes ?? string.Empty, m_ReleaseNotesStyle, GUILayout.ExpandHeight(true));
            if (EditorGUI.EndChangeCheck()) m_EditedReleaseNotes = releaseNotes;
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("Leave release notes blank to link to the GitHub release.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.Width(80))) Close();

            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(m_Version));
            if (GUILayout.Button("Save", GUILayout.Width(80))) Save();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private void Save()
        {
            try
            {
                HBPBuildProfiles.WriteProductVersion(m_Version, ReleaseNotes);
                Close();
            }
            catch (System.Exception exception)
            {
                EditorUtility.DisplayDialog("Change version", exception.Message, "OK");
            }
        }
    }
}
