#if UNITY_ANDROID
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;

namespace HBP.Dev
{
    public sealed class HBPAndroidPackaging : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (UnityEditor.PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android) == "fr.crnl.hibop.quest")
            {
                // Associate the Quest player with HiBoP in Meta's developer portal.
                string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
                XDocument manifest = XDocument.Load(manifestPath);
                XNamespace android = "http://schemas.android.com/apk/res/android";
                if (!manifest.Root.Elements("uses-permission").Any(element => (string)element.Attribute(android + "name") == "android.permission.ACCESS_NETWORK_STATE"))
                    manifest.Root.Add(new XElement("uses-permission", new XAttribute(android + "name", "android.permission.ACCESS_NETWORK_STATE")));
                XElement application = manifest.Root.Element("application");
                XElement appId = application.Elements("meta-data")
                    .FirstOrDefault(element => (string)element.Attribute(android + "name") == "com.oculus.app_id");
                if (appId == null)
                {
                    appId = new XElement("meta-data", new XAttribute(android + "name", "com.oculus.app_id"));
                    application.Add(appId);
                }
                appId.SetAttributeValue(android + "value", "1422501847605591");
                manifest.Save(manifestPath);
            }

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
