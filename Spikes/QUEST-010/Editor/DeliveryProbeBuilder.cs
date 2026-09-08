using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DeliveryProbeBuilder
{
    public static void Build()
    {
        bool android = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
        PlayerSettings.companyName = "CRNL";
        PlayerSettings.productName = "HiBoP Delivery Qualification";
        PlayerSettings.bundleVersion = "0.0.10";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "fr.crnl.hibop.deliveryprobe");
        PlayerSettings.SetScriptingBackend(android ? NamedBuildTarget.Android : NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetApiCompatibilityLevel(android ? NamedBuildTarget.Android : NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.usePlayerLog = true;
        Directory.CreateDirectory("Assets/Generated");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var root = new GameObject("Transport Probe", typeof(DeliveryServer));
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Generated/DeliveryServer.prefab");
        UnityEngine.Object.DestroyImmediate(root);
        PrefabUtility.InstantiatePrefab(prefab, scene);
        EditorSceneManager.SaveScene(scene, "Assets/Generated/DeliveryServer.unity");
        var arguments = Environment.GetCommandLineArgs();
        string output = arguments[Array.IndexOf(arguments, "-probeOutput") + 1];
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Generated/DeliveryServer.unity" },
            target = android ? BuildTarget.Android : BuildTarget.StandaloneWindows64,
            locationPathName = output,
            options = BuildOptions.Development | BuildOptions.DetailedBuildReport
        });
        File.WriteAllText(output + ".build.txt", $"Unity={Application.unityVersion}\nBackend=IL2CPP\nTarget={report.summary.platform}\nResult={report.summary.result}\nBytes={report.summary.totalSize}\nSeconds={report.summary.totalTime.TotalSeconds}\nErrors={report.summary.totalErrors}\n");
        if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Transport probe build failed.");
    }
}
