using HBP.Core.Data;
using HBP.Core.Tools;
using Newtonsoft.Json;
using System;
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

        public static void BuildFromCommandLine()
        {
            try
            {
                string buildsDirectory = GetCommandLineArgument("-buildOutput");
                if (string.IsNullOrWhiteSpace(buildsDirectory))
                {
                    throw new BuildFailedException("Missing required -buildOutput argument.");
                }

                if (buildsDirectory[buildsDirectory.Length - 1] != '/' && buildsDirectory[buildsDirectory.Length - 1] != '\\')
                {
                    buildsDirectory += Path.DirectorySeparatorChar;
                }

                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                ScriptingImplementation scriptingBackend = GetCommandLineScriptingBackend(target);
                bool development = HasCommandLineArgument("-developmentBuild");
                WriteBuildInfo();
                BuildProjectAndZipIt(buildsDirectory, development, target, scriptingBackend);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void DefaultBuild()
        {
            BuildProjectAndZipIt(@"D:/HBP/HiBoP_builds/", false, BuildTarget.StandaloneWindows64);
            BuildProjectAndZipIt(@"D:/HBP/HiBoP_builds/", false, BuildTarget.StandaloneLinux64);
            BuildProjectAndZipIt(@"D:/HBP/HiBoP_builds/", false, BuildTarget.StandaloneOSX);
        }

        public static void BuildProjectAndZipIt(string buildsDirectory, bool development, BuildTarget target, bool connectProfiler = false)
        {
            BuildProjectAndZipIt(buildsDirectory, development, target, GetDefaultScriptingBackend(target), connectProfiler);
        }

        public static void BuildProjectAndZipIt(string buildsDirectory, bool development, BuildTarget target, ScriptingImplementation scriptingBackend, bool connectProfiler = false)
        {
            SerializationTypeRegistryGenerator.EnsureUpToDateForBuild();
            PrepareBuildTarget(target);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, scriptingBackend);

            string os = "";
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
#if UNITY_EDITOR_WIN
                    UnityEditor.WindowsStandalone.UserBuildSettings.architecture = OSArchitecture.x64;
#endif
                    os = "win64";
                    break;
                case BuildTarget.StandaloneLinux64:
                    os = "linux64";
                    break;
                case BuildTarget.StandaloneOSX:
#if UNITY_EDITOR_OSX
                    UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.ARM64;
#endif
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

            if (connectProfiler && !development)
            {
                throw new BuildFailedException("The profiler can only be connected to a development build.");
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
            using DiagnosticBuildSettingsScope diagnosticSettings = new(development, scriptingBackend);
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Build failed for {target}: {report.summary.result}");
            }

            string projectPath = Application.dataPath;
            projectPath = projectPath.Remove(projectPath.Length - 6);

            string dataBuildDirectory = target == BuildTarget.StandaloneOSX ? Path.Combine(dataDirectory, "Contents", "Resources", m_DataBuild) : Path.Combine(dataDirectory, m_DataBuild);
            DirectoryInfo dataDirectoryInfo = new(dataBuildDirectory);
            new DirectoryInfo(projectPath + m_Data).CopyFilesRecursively(dataDirectoryInfo);
            foreach (var file in dataDirectoryInfo.GetFiles("*.meta", SearchOption.AllDirectories))
            {
                file.Delete();
            }

            foreach (var file in dataDirectoryInfo.GetFiles("*.obj", SearchOption.AllDirectories))
            {
                file.Delete();
            }

            string[] doNotShipDirectoryNames =
            {
                $"{Application.productName}_BackUpThisFolder_ButDontShipItWithYourGame",
                $"{Application.productName}_BurstDebugInformation_DoNotShip"
            };
            foreach (string doNotShipDirectoryName in doNotShipDirectoryNames)
            {
                DirectoryInfo doNotShipDirectory = new(Path.Combine(buildDirectory, doNotShipDirectoryName));
                if (doNotShipDirectory.Exists)
                {
                    doNotShipDirectory.Delete(true);
                }
            }

            // Remove Localizer atlas if it exists (we do not ship it with the build)
            DirectoryInfo localizerDirectory = new(Path.Combine(dataBuildDirectory, "Atlases", "Localizers"));
            if (localizerDirectory.Exists)
            {
                localizerDirectory.Delete(true);
            }

#if UNITY_EDITOR_OSX
            if (target == BuildTarget.StandaloneOSX && UnityEditor.OSXStandalone.UserBuildSettings.architecture == OSArchitecture.ARM64)
            {
                string pluginsPath = Path.Join(dataDirectory, "Contents", "PlugIns");
                DirectoryInfo pluginsDirectory = new(pluginsPath);
                DirectoryInfo arm64PluginsDirectory = new(Path.Join(pluginsPath, "ARM64"));
                arm64PluginsDirectory.CopyFilesRecursively(pluginsDirectory);
                arm64PluginsDirectory.Delete(true);
            }
#endif

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

        private static string GetCommandLineArgument(string argumentName)
        {
            string[] arguments = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == argumentName)
                {
                    return arguments[i + 1];
                }
            }

            return null;
        }

        private static bool HasCommandLineArgument(string argumentName)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            foreach (string argument in arguments)
            {
                if (string.Equals(argument, argumentName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static ScriptingImplementation GetCommandLineScriptingBackend(BuildTarget target)
        {
            string value = GetCommandLineArgument("-scriptingBackend");
            if (string.IsNullOrWhiteSpace(value))
            {
                return GetDefaultScriptingBackend(target);
            }

            if (string.Equals(value, "IL2CPP", System.StringComparison.OrdinalIgnoreCase))
            {
                return ScriptingImplementation.IL2CPP;
            }

            if (string.Equals(value, "Mono", System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, "Mono2x", System.StringComparison.OrdinalIgnoreCase))
            {
                return ScriptingImplementation.Mono2x;
            }

            throw new BuildFailedException($"Unsupported -scriptingBackend value: {value}. Use Mono or IL2CPP.");
        }

        private static ScriptingImplementation GetDefaultScriptingBackend(BuildTarget target)
        {
            return target == BuildTarget.StandaloneOSX ? ScriptingImplementation.Mono2x : ScriptingImplementation.IL2CPP;
        }

        internal static void WriteBuildInfo()
        {
            BuildInfo buildInfo = new()
            {
                UnityVersion = Application.unityVersion,
                Version = Application.version,
                BuildDate = DateTime.Now,
                Commit = GetBuildCommit()
            };
            File.WriteAllText("Assets/Resources/BuildInfo.json", JsonConvert.SerializeObject(buildInfo));
            AssetDatabase.Refresh();
        }

        private static string GetBuildCommit()
        {
            string commit = GetCommandLineArgument("-buildCommit");
            if (string.IsNullOrWhiteSpace(commit))
            {
                commit = Environment.GetEnvironmentVariable("GITHUB_SHA");
            }

            if (string.IsNullOrWhiteSpace(commit))
            {
                commit = GetLocalGitCommit();
            }

            commit = commit?.Trim() ?? string.Empty;
            return commit.Length > 12 ? commit[..12] : commit;
        }

        private static string GetLocalGitCommit()
        {
            try
            {
                System.Diagnostics.ProcessStartInfo startInfo = new()
                {
                    FileName = "git",
                    Arguments = "rev-parse HEAD",
                    WorkingDirectory = Path.GetDirectoryName(Application.dataPath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo);
                if (process == null)
                {
                    return string.Empty;
                }

                if (!process.WaitForExit(2000))
                {
                    process.Kill();
                    return string.Empty;
                }

                return process.ExitCode == 0 ? process.StandardOutput.ReadToEnd() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private sealed class DiagnosticBuildSettingsScope : IDisposable
        {
            private readonly bool m_UsePlayerLog;
            private readonly StackTraceLogType m_ErrorStackTrace;
            private readonly StackTraceLogType m_AssertStackTrace;
            private readonly StackTraceLogType m_WarningStackTrace;
            private readonly StackTraceLogType m_LogStackTrace;
            private readonly StackTraceLogType m_ExceptionStackTrace;
            private readonly bool m_UsesIl2Cpp;
            private readonly Il2CppStacktraceInformation m_Il2CppStacktraceInformation;
            private readonly Il2CppCompilerConfiguration m_Il2CppCompilerConfiguration;
            private readonly Il2CppCodeGeneration m_Il2CppCodeGeneration;

            public DiagnosticBuildSettingsScope(bool development, ScriptingImplementation scriptingBackend)
            {
                m_UsePlayerLog = PlayerSettings.usePlayerLog;
                m_ErrorStackTrace = PlayerSettings.GetStackTraceLogType(LogType.Error);
                m_AssertStackTrace = PlayerSettings.GetStackTraceLogType(LogType.Assert);
                m_WarningStackTrace = PlayerSettings.GetStackTraceLogType(LogType.Warning);
                m_LogStackTrace = PlayerSettings.GetStackTraceLogType(LogType.Log);
                m_ExceptionStackTrace = PlayerSettings.GetStackTraceLogType(LogType.Exception);
                m_UsesIl2Cpp = scriptingBackend == ScriptingImplementation.IL2CPP;

                PlayerSettings.usePlayerLog = true;
                PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
                PlayerSettings.SetStackTraceLogType(LogType.Assert, StackTraceLogType.ScriptOnly);
                PlayerSettings.SetStackTraceLogType(LogType.Warning, development ? StackTraceLogType.ScriptOnly : StackTraceLogType.None);
                PlayerSettings.SetStackTraceLogType(LogType.Log, development ? StackTraceLogType.ScriptOnly : StackTraceLogType.None);
                PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);

                if (!m_UsesIl2Cpp)
                {
                    return;
                }

                m_Il2CppStacktraceInformation = PlayerSettings.GetIl2CppStacktraceInformation(NamedBuildTarget.Standalone);
                m_Il2CppCompilerConfiguration = PlayerSettings.GetIl2CppCompilerConfiguration(NamedBuildTarget.Standalone);
                m_Il2CppCodeGeneration = PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.Standalone);
                PlayerSettings.SetIl2CppStacktraceInformation(NamedBuildTarget.Standalone, Il2CppStacktraceInformation.MethodFileLineNumber);
                if (!development)
                {
                    PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Standalone, Il2CppCompilerConfiguration.Release);
                    PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Standalone, Il2CppCodeGeneration.OptimizeSpeed);
                }
            }

            public void Dispose()
            {
                PlayerSettings.usePlayerLog = m_UsePlayerLog;
                PlayerSettings.SetStackTraceLogType(LogType.Error, m_ErrorStackTrace);
                PlayerSettings.SetStackTraceLogType(LogType.Assert, m_AssertStackTrace);
                PlayerSettings.SetStackTraceLogType(LogType.Warning, m_WarningStackTrace);
                PlayerSettings.SetStackTraceLogType(LogType.Log, m_LogStackTrace);
                PlayerSettings.SetStackTraceLogType(LogType.Exception, m_ExceptionStackTrace);

                if (m_UsesIl2Cpp)
                {
                    PlayerSettings.SetIl2CppStacktraceInformation(NamedBuildTarget.Standalone, m_Il2CppStacktraceInformation);
                    PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Standalone, m_Il2CppCompilerConfiguration);
                    PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Standalone, m_Il2CppCodeGeneration);
                }
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
        private bool m_WindowsIL2CPP = true;
        private bool m_LinuxIL2CPP = true;
        private bool m_MacOSXIL2CPP = false;

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
            if (m_Windows)
            {
                m_WindowsIL2CPP = GUILayout.Toggle(m_WindowsIL2CPP, "Windows IL2CPP");
            }

            m_Linux = GUILayout.Toggle(m_Linux, "Linux");
            if (m_Linux)
            {
                m_LinuxIL2CPP = GUILayout.Toggle(m_LinuxIL2CPP, "Linux IL2CPP");
            }

            m_MacOSX = GUILayout.Toggle(m_MacOSX, "MacOSX");
            if (m_MacOSX)
            {
                m_MacOSXIL2CPP = GUILayout.Toggle(m_MacOSXIL2CPP, "MacOSX IL2CPP");
            }

            if (GUILayout.Button("Build!"))
            {
                HBPBuilder.WriteBuildInfo();
                if (m_BuildDirectory[m_BuildDirectory.Length - 1] != '/' && m_BuildDirectory[m_BuildDirectory.Length - 1] != '\\')
                {
                    m_BuildDirectory += '/';
                }

                if (m_Windows)
                {
                    HBPBuilder.BuildProjectAndZipIt(m_BuildDirectory, m_DevelopmentBuild, BuildTarget.StandaloneWindows64, GetScriptingBackend(m_WindowsIL2CPP), m_ConnectProfiler);
                }

                if (m_Linux)
                {
                    HBPBuilder.BuildProjectAndZipIt(m_BuildDirectory, m_DevelopmentBuild, BuildTarget.StandaloneLinux64, GetScriptingBackend(m_LinuxIL2CPP), m_ConnectProfiler);
                }

                if (m_MacOSX)
                {
                    HBPBuilder.BuildProjectAndZipIt(m_BuildDirectory, m_DevelopmentBuild, BuildTarget.StandaloneOSX, GetScriptingBackend(m_MacOSXIL2CPP), m_ConnectProfiler);
                }

                Close();
            }
        }

        private static ScriptingImplementation GetScriptingBackend(bool il2cpp)
        {
            return il2cpp ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x;
        }
    }
}
