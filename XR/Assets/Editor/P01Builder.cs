using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

namespace CRNL.HiBoP.XR.Editor
{
    public static class P01Builder
    {
        private const string TEMPORARY_SCENE_PATH = "Assets/P01BuildScene.unity";

        public static void BuildAndroid()
        {
            string outputPath = GetArgument("-p01BuildOutput");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException("Missing -p01BuildOutput.");
            }

            outputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), TEMPORARY_SCENE_PATH))
            {
                throw new InvalidOperationException("Unable to create the temporary P01 build scene.");
            }

            try
            {
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { TEMPORARY_SCENE_PATH },
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });

                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException($"Android build failed with result {report.summary.result}.");
                }
            }
            finally
            {
                AssetDatabase.DeleteAsset(TEMPORARY_SCENE_PATH);
            }
        }

        private static string GetArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (string.Equals(arguments[i], name, StringComparison.Ordinal))
                {
                    return arguments[i + 1];
                }
            }

            return null;
        }
    }
}
