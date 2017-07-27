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
        private static List<string> m_DLLs = new List<string> {
        "concrt140.dll",
        "hbp_export.dll",
        "msvcp140.dll",
        "opencv_core320.dll",
        "opencv_highgui320.dll",
        "opencv_imgcodecs320.dll",
        "opencv_imgproc320.dll",
        "ucrtbase.dll",
        "vccorlib140.dll",
        "vcomp140.dll",
        "vcruntime140.dll"
    };

        private static string m_Data = "Assets/Data/";
        private static string m_DataBuild = "Data/";

        private static string m_Tools = "tools/";

        [MenuItem("Build/Development Build")]
        public static void DevelopmentBuild()
        {
            string buildName = string.Format("HiBoP_{0}_{1}_{2}", DateTime.Today.Year.ToString("d4"), DateTime.Today.Month.ToString("d2"), DateTime.Today.Day.ToString("d2"));
            string buildDirectory = @"D:/HBP/HiBoP_builds/" + buildName + "/";

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.locationPathName = buildDirectory + "HiBoP.exe";
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            BuildOptions buildOptions = BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler | BuildOptions.Development | BuildOptions.ShowBuiltPlayer;
            buildPlayerOptions.options = buildOptions;
            BuildPipeline.BuildPlayer(buildPlayerOptions);

            string projectPath = Application.dataPath;
            projectPath = projectPath.Remove(projectPath.Length - 6);
            foreach (string dll in m_DLLs)
            {
                File.Copy(projectPath + dll, buildDirectory + dll, true);
            }
            CopyFilesRecursively(new DirectoryInfo(projectPath + m_Tools), new DirectoryInfo(buildDirectory + m_Tools));
            CopyFilesRecursively(new DirectoryInfo(projectPath + m_Data), new DirectoryInfo(buildDirectory + m_DataBuild));

            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(buildDirectory, "");
                zip.Save(buildDirectory + buildName + ".zip");
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
}
