using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Ionic.Zip;

namespace Tools.Unity
{
    public class HBPBuilder : MonoBehaviour
    {
        private static string m_Data = "Assets/Data/";
        private static string m_DataBuild = "Data/";

        private static string m_Tools = "tools/";

        public static void DefaultBuild()
        {
            BuildProjectAndZipIt(@"D:/HBP/HiBoP_builds/", false, BuildTarget.StandaloneWindows64);
            BuildProjectAndZipIt(@"D:/HBP/HiBoP_builds/", false, BuildTarget.StandaloneLinux64);
            BuildProjectAndZipIt(@"D:/HBP/HiBoP_builds/", false, BuildTarget.StandaloneOSXIntel64);
        }

        public static void BuildProjectAndZipIt(string buildsDirectory, bool development, BuildTarget target) // FIXME : some libraries are not copied in linux .so.X.X.X
        {
            string buildName = Application.productName;
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    buildName += " win64";
                    break;
                case BuildTarget.StandaloneLinux64:
                    buildName += " linux64";
                    break;
                case BuildTarget.StandaloneOSXIntel64:
                    buildName += " macos64";
                    break;
            }
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
                case BuildTarget.StandaloneOSXIntel64:
                    hibopName += ".app";
                    dataDirectory += hibopName + "/";
                    break;
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.locationPathName = buildDirectory + hibopName;
            buildPlayerOptions.target = target;
            buildPlayerOptions.scenes = new string[] { "Assets/_Scenes/HiBoP.unity" };
            BuildOptions buildOptions = development ? BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.Development : BuildOptions.None;
            buildPlayerOptions.options = buildOptions;
            BuildPipeline.BuildPlayer(buildPlayerOptions);

            string projectPath = Application.dataPath;
            projectPath = projectPath.Remove(projectPath.Length - 6);

            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    CopyFilesRecursively(new DirectoryInfo(projectPath + m_Tools + "windows/"), new DirectoryInfo(buildDirectory + m_Tools));
                    break;
                case BuildTarget.StandaloneLinux64:
                    CopyFilesRecursively(new DirectoryInfo(projectPath + m_Tools + "linux/"), new DirectoryInfo(buildDirectory + m_Tools));
                    break;
                case BuildTarget.StandaloneOSXIntel64:
                    CopyFilesRecursively(new DirectoryInfo(projectPath + m_Tools + "macos/"), new DirectoryInfo(buildDirectory));
                    break;
            }
            CopyFilesRecursively(new DirectoryInfo(projectPath + m_Data), new DirectoryInfo(dataDirectory + m_DataBuild));
            if (target == BuildTarget.StandaloneOSXIntel64)
            {
                DirectoryInfo pluginsDirectory = new DirectoryInfo(dataDirectory + "Contents/Plugins/");
                CopyFilesRecursively(pluginsDirectory, new DirectoryInfo(dataDirectory + "Contents/Frameworks/MonoEmbedRuntime/osx/"));
                foreach (FileInfo plugin in pluginsDirectory.GetFiles())
                {
                    plugin.Delete();
                }
            }

            FileInfo readme = new FileInfo(projectPath + "README.md");
            readme.CopyTo(buildDirectory + readme.Name);

            FileInfo documentation = new FileInfo(projectPath + "Docs/LaTeX/HiBoP_user_manual.pdf");
            documentation.CopyTo(buildDirectory + documentation.Name);
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            if (!source.Exists) return;

            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }
    }

    public class HBPBuilderWindow : EditorWindow
    {
        private string m_BuildDirectory = @"D:/HBP/HiBoP_builds/";
        private bool m_DevelopmentBuild = false;
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
            m_Windows = GUILayout.Toggle(m_Windows, "Windows");
            m_Linux = GUILayout.Toggle(m_Linux, "Linux");
            m_MacOSX = GUILayout.Toggle(m_MacOSX, "MacOSX");
            if (GUILayout.Button("Build!"))
            {
                if (m_BuildDirectory[m_BuildDirectory.Length - 1] != '/' && m_BuildDirectory[m_BuildDirectory.Length - 1] != '\\')
                {
                    m_BuildDirectory += '/';
                }
                if (m_Windows)
                {
                    HBPBuilder.BuildProjectAndZipIt(m_BuildDirectory, m_DevelopmentBuild, BuildTarget.StandaloneWindows64);
                }
                if (m_Linux)
                {
                    HBPBuilder.BuildProjectAndZipIt(m_BuildDirectory, m_DevelopmentBuild, BuildTarget.StandaloneLinux64);
                }
                if (m_MacOSX)
                {
                    HBPBuilder.BuildProjectAndZipIt(m_BuildDirectory, m_DevelopmentBuild, BuildTarget.StandaloneOSXIntel64);
                }
                Close();
            }
        }
    }
}
