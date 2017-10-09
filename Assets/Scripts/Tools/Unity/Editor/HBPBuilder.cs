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
            BuildProjectAndZipIt(@"D:/HBP/HiBoP_builds/");
        }
        public static void BuildProjectAndZipIt(string buildsDirectory, bool development = true)
        {
            string buildName = string.Format("HiBoP_{0}_{1}_{2}", DateTime.Today.Year.ToString("d4"), DateTime.Today.Month.ToString("d2"), DateTime.Today.Day.ToString("d2"));
            string buildDirectory = buildsDirectory + buildName + "/";
            
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.locationPathName = buildDirectory + "HiBoP.exe";
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64; // TODO : allow to change this through editor window
            buildPlayerOptions.scenes = new string[] { "Assets/_Scenes/Main.unity" };
            BuildOptions buildOptions = development ? BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.Development : BuildOptions.None;
            buildPlayerOptions.options = buildOptions;
            BuildPipeline.BuildPlayer(buildPlayerOptions);

            string projectPath = Application.dataPath;
            projectPath = projectPath.Remove(projectPath.Length - 6);

            if (buildPlayerOptions.target == BuildTarget.StandaloneWindows64)
            {
                CopyFilesRecursively(new DirectoryInfo(projectPath + m_Tools + "windows/"), new DirectoryInfo(buildDirectory + m_Tools));
            }
            else if (buildPlayerOptions.target == BuildTarget.StandaloneLinux64)
            {
                CopyFilesRecursively(new DirectoryInfo(projectPath + m_Tools + "linux/"), new DirectoryInfo(buildDirectory + m_Tools));
            }
            else if (buildPlayerOptions.target == BuildTarget.StandaloneOSXIntel64)
            {
                CopyFilesRecursively(new DirectoryInfo(projectPath + m_Tools + "macosx/"), new DirectoryInfo(buildDirectory + m_Tools));
            }
            CopyFilesRecursively(new DirectoryInfo(projectPath + m_Data), new DirectoryInfo(buildDirectory + m_DataBuild));

            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(buildDirectory, "");
                zip.Save(buildsDirectory + buildName + ".zip");
            }
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }
    }

    public class HBPBuilderWindow : EditorWindow
    {
        private string m_BuildDirectory = @"D:/HBP/HiBoP_builds/";
        private bool m_DevelopmentBuild = true;

        [MenuItem("Tools/Build/Development Build")]
        public static void DevelopmentBuild()
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
            if (GUILayout.Button("Build!"))
            {
                if (m_BuildDirectory[m_BuildDirectory.Length - 1] != '/' && m_BuildDirectory[m_BuildDirectory.Length - 1] != '\\')
                {
                    m_BuildDirectory += '/';
                }
                HBPBuilder.BuildProjectAndZipIt(m_BuildDirectory, m_DevelopmentBuild);
                Close();
            }
        }
    }
}
