#if UNITY_ANDROID
using System.IO;
using UnityEditor.Android;

namespace HBP.Dev
{
    public sealed class HBPAndroidPackaging : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // Unity passes the unityLibrary directory. Configure the application module,
            // including projects exported from the Editor and builds invoked by CI.
            string launcher = Path.Combine(path, "..", "launcher", "build.gradle");
            const string marker = "// HiBoP: always assemble APKs from scratch.";
            string contents = File.ReadAllText(launcher);
            if (contents.Contains(marker)) return;

            // A non-incremental packaging task makes AGP discard its old APK. Otherwise
            // Zipflinger can retain gigabytes of virtual entries after assets change.
            // Only packaging is invalidated; compilation and resource caches remain usable.
            File.AppendAllText(launcher, "\n" + marker + @"
tasks.withType(com.android.build.gradle.tasks.PackageApplication).configureEach {
    outputs.upToDateWhen { false }
}
");
        }
    }
}
#endif
