using HBP.Core.Data;
using HBP.Core.Tools;
using Newtonsoft.Json;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEngine;

namespace HBP.Dev
{
    public class HBPBuilder : MonoBehaviour
    {
        private static string m_Data = "Assets/Data/";
        private static string m_DataBuild = "Data/";

        public static void DefaultBuild()
        {
            BuildProjectAndZipIt(@"D:/HBP/HiBoP_builds/", false, BuildTarget.StandaloneWindows64);
            BuildProjectAndZipIt(@"D:/HBP/HiBoP_builds/", false, BuildTarget.StandaloneLinux64);
            BuildProjectAndZipIt(@"D:/HBP/HiBoP_builds/", false, BuildTarget.StandaloneOSX);
        }

        public static void BuildProjectAndZipIt(string buildsDirectory, bool development, BuildTarget target, bool connectProfiler = false)
        {
            SerializationTypeRegistryGenerator.EnsureUpToDateForBuild();
            PrepareBuildTarget(target);

            string os = "";
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
                    UnityEditor.WindowsStandalone.UserBuildSettings.architecture = OSArchitecture.x64;
                    os = "win64";
                    break;
                case BuildTarget.StandaloneLinux64:
                    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
                    os = "linux64";
                    break;
                case BuildTarget.StandaloneOSX:
                    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                    UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.ARM64;
                    os = "macos64";
                    break;
            }

            string buildName = string.Format("{0}.{1}.{2}", Application.productName, Application.version, os);
            string buildDirectory = buildsDirectory + buildName + "/";
            string dataDirectory = buildDirectory;
            string hibopName = "HiBoP";
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    hibopName += ".exe";
                    break;
                case BuildTarget.StandaloneLinux64:
                    hibopName += ".x86_64";
                    break;
                case BuildTarget.StandaloneOSX:
                    hibopName += ".app";
                    dataDirectory += hibopName + "/";
                    break;
            }

            BuildOptions buildOptions = development ? BuildOptions.Development : BuildOptions.None;
            if (connectProfiler)
            {
                buildOptions |= BuildOptions.ConnectWithProfiler;
            }

            BuildPlayerOptions buildPlayerOptions = new()
            {
                locationPathName = buildDirectory + hibopName,
                target = target,
                scenes = new string[] { "Assets/_Scenes/HiBoP.unity" },
                options = buildOptions
            };
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Build failed for {target}: {report.summary.result}");
            }

            string projectPath = Application.dataPath;
            projectPath = projectPath.Remove(projectPath.Length - 6);

            DirectoryInfo dataDirectoryInfo = new(dataDirectory + m_DataBuild);
            new DirectoryInfo(projectPath + m_Data).CopyFilesRecursively(dataDirectoryInfo);
            foreach (var file in dataDirectoryInfo.GetFiles("*.meta", SearchOption.AllDirectories))
            {
                file.Delete();
            }

            foreach (var file in dataDirectoryInfo.GetFiles("*.obj", SearchOption.AllDirectories))
            {
                file.Delete();
            }

            DirectoryInfo doNotShipDirectory = new(Path.Join(dataDirectory, "HiBoP_BackUpThisFolder_ButDontShipItWithYourGame"));
            if (doNotShipDirectory.Exists)
            {
                doNotShipDirectory.Delete(true);
            }

            // Remove Localizer atlas if it exists (we do not ship it with the build)
            DirectoryInfo localizerDirectory = new(Path.Combine(dataDirectory, m_DataBuild, "Atlases", "Localizers"));
            if (localizerDirectory.Exists)
            {
                localizerDirectory.Delete(true);
            }

            if (target == BuildTarget.StandaloneOSX && UnityEditor.OSXStandalone.UserBuildSettings.architecture == UnityEditor.Build.OSArchitecture.ARM64)
            {
                string pluginsPath = Path.Join(dataDirectory, "Contents", "PlugIns");
                DirectoryInfo pluginsDirectory = new(pluginsPath);
                DirectoryInfo arm64PluginsDirectory = new(Path.Join(pluginsPath, "ARM64"));
                arm64PluginsDirectory.CopyFilesRecursively(pluginsDirectory);
                arm64PluginsDirectory.Delete(true);
            }

            if (target == BuildTarget.StandaloneLinux64)
            {
                DirectoryInfo pluginsDirectory = new(Application.dataPath + "/Plugins/x86_64/Linux");
                DirectoryInfo newPluginsDirectory = new(dataDirectory + "HiBoP_Data/Plugins");
                pluginsDirectory.CopyFilesRecursively(newPluginsDirectory);
                foreach (var metaFile in newPluginsDirectory.GetFiles("*.meta"))
                {
                    metaFile.Delete();
                }
            }

            FileInfo readme = new(projectPath + "README.md");
            readme.CopyTo(buildDirectory + readme.Name, true);

            FileInfo documentation = new(projectPath + "Docs/LaTeX/HiBoP_user_manual.pdf");
            documentation.CopyTo(buildDirectory + documentation.Name, true);
        }

        private static void PrepareBuildTarget(BuildTarget target)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            {
                throw new BuildFailedException($"Build target {target} is not installed or not supported by this Unity Editor.");
            }

            if (target == BuildTarget.StandaloneLinux64)
            {
                EnsureLinuxIl2CppPackagesResolved();
            }

            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                AssetDatabase.SaveAssets();
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, target))
                {
                    throw new BuildFailedException($"Could not switch the active build target to {target}.");
                }
            }
        }

        private static void EnsureLinuxIl2CppPackagesResolved()
        {
            EnsurePackageResolved("com.unity.sdk.linux-x86_64");

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                EnsurePackageResolved("com.unity.toolchain.win-x86_64-linux");
            }
        }

        private static void EnsurePackageResolved(string packageName)
        {
            if (UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName) == null)
            {
                throw new BuildFailedException($"Package {packageName} is required to build the Linux IL2CPP player. Add it to Packages/manifest.json and let Unity resolve packages before building.");
            }
        }
    }

    public class HBPBuilderWindow : EditorWindow
    {
        private string m_BuildDirectory = @"C:\HBP\Builds\HiBoP";
        private bool m_DevelopmentBuild = false;
        private bool m_ConnectProfiler = false;
        private bool m_Windows = true;
        private bool m_Linux = true;
        private bool m_MacOSX = true;

        [MenuItem("Tools/Build HiBoP")]
        public static void OpenBuildWindow()
        {
            HBPBuilderWindow window = (HBPBuilderWindow)GetWindow(typeof(HBPBuilderWindow));
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("HBP Builder", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            m_BuildDirectory = EditorGUILayout.TextField("Builds Directory", m_BuildDirectory);
            if (GUILayout.Button("Select"))
            {
                m_BuildDirectory = EditorUtility.OpenFolderPanel("Select the builds folder", m_BuildDirectory, "");
            }

            GUILayout.EndHorizontal();
            m_DevelopmentBuild = GUILayout.Toggle(m_DevelopmentBuild, "Development Build");
            m_ConnectProfiler = GUILayout.Toggle(m_ConnectProfiler, "Connect Profiler");
            m_Windows = GUILayout.Toggle(m_Windows, "Windows");
            m_Linux = GUILayout.Toggle(m_Linux, "Linux");
            m_MacOSX = GUILayout.Toggle(m_MacOSX, "MacOSX");
            if (GUILayout.Button("Build!"))
            {
                WriteBuildInfo();
                if (m_BuildDirectory[m_BuildDirectory.Length - 1] != '/' && m_BuildDirectory[m_BuildDirectory.Length - 1] != '\\')
                {
                    m_BuildDirectory += '/';
                }

                if (m_Windows)
                {
                    HBPBuilder.BuildProjectAndZipIt(m_BuildDirectory, m_DevelopmentBuild, BuildTarget.StandaloneWindows64, m_ConnectProfiler);
                }

                if (m_Linux)
                {
                    HBPBuilder.BuildProjectAndZipIt(m_BuildDirectory, m_DevelopmentBuild, BuildTarget.StandaloneLinux64, m_ConnectProfiler);
                }

                if (m_MacOSX)
                {
                    HBPBuilder.BuildProjectAndZipIt(m_BuildDirectory, m_DevelopmentBuild, BuildTarget.StandaloneOSX, m_ConnectProfiler);
                }

                Close();
            }
        }

        void WriteBuildInfo()
        {
            BuildInfo buildInfo = new()
            {
                UnityVersion = Application.unityVersion,
                Version = Application.version,
                BuildDate = System.DateTime.Now
            };
            File.WriteAllText("Assets/Resources/BuildInfo.json", JsonConvert.SerializeObject(buildInfo));
            AssetDatabase.Refresh();
        }
    }
}
