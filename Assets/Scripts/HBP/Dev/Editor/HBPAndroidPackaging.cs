#if UNITY_ANDROID
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;
using UnityEditor.Build;

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
                foreach (string permission in new[] { "android.permission.INTERNET", "android.permission.ACCESS_NETWORK_STATE", "android.permission.CHANGE_NETWORK_STATE", "android.permission.CHANGE_WIFI_MULTICAST_STATE" })
                    if (!manifest.Root.Elements("uses-permission").Any(element => (string)element.Attribute(android + "name") == permission))
                        manifest.Root.Add(new XElement("uses-permission", new XAttribute(android + "name", permission)));
                XElement application = manifest.Root.Element("application");
                var launchActivities = application.Elements("activity").Where(activity => activity.Elements("intent-filter").Elements("action").Any(action => (string)action.Attribute(android + "name") == "android.intent.action.MAIN")).ToArray();
                if (launchActivities.Length == 0)
                    throw new BuildFailedException("The Quest manifest does not declare a launch activity.");
                foreach (XElement activity in launchActivities)
                    activity.SetAttributeValue(android + "excludeFromRecents", "true");

                XElement appId = application.Elements("meta-data").FirstOrDefault(element => (string)element.Attribute(android + "name") == "com.oculus.app_id");
                if (appId == null)
                {
                    appId = new XElement("meta-data", new XAttribute(android + "name", "com.oculus.app_id"));
                    application.Add(appId);
                }

                appId.SetAttributeValue(android + "value", "1422501847605591");
                NormalizeQuest3Device(application, android);
                manifest.Save(manifestPath);

                // XR Management can emit the device list in a generated Android library.
                string xrManifestPath = Path.Combine(path, "xrmanifest.androidlib", "AndroidManifest.xml");
                if (File.Exists(xrManifestPath))
                {
                    XDocument xrManifest = XDocument.Load(xrManifestPath);
                    if (NormalizeQuest3Device(xrManifest.Root.Element("application"), android))
                        xrManifest.Save(xrManifestPath);
                }
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

        private static bool NormalizeQuest3Device(XElement application, XNamespace android)
        {
            XAttribute devices = application?.Elements("meta-data").FirstOrDefault(element => (string)element.Attribute(android + "name") == "com.oculus.supportedDevices")?.Attribute(android + "value");
            if (devices == null) return false;

            string normalized = string.Join("|", devices.Value.Split('|').Select(device => device == "eureka" ? "quest3" : device).Distinct());
            if (normalized == devices.Value) return false;

            devices.Value = normalized;
            return true;
        }
    }
}
#endif
