using System;
using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace CRNL.HiBoP.XR.Bootstrap.Editor
{
    public sealed class P04PassthroughManifestPostprocessor : IPostGenerateGradleAndroidProject
    {
        private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
        private const string InternetPermission = "android.permission.INTERNET";
        private const string PassthroughFeature = "com.oculus.feature.PASSTHROUGH";

        public int callbackOrder => 10000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            bool found = false;
            foreach (string manifestPath in Directory.GetFiles(path, "AndroidManifest.xml", SearchOption.AllDirectories))
            {
                var document = new XmlDocument();
                document.Load(manifestPath);
                var namespaces = new XmlNamespaceManager(document.NameTable);
                namespaces.AddNamespace("android", AndroidNamespace);
                bool changed = false;
                XmlNodeList nodes = document.SelectNodes($"/manifest/uses-feature[@android:name='{PassthroughFeature}']", namespaces);

                XmlNodeList internetPermissions = document.SelectNodes($"/manifest/uses-permission[@android:name='{InternetPermission}']", namespaces);

                foreach (XmlElement element in nodes)
                {
                    element.SetAttribute("required", AndroidNamespace, "false");
                    found = true;
                    changed = true;
                }

                while (internetPermissions.Count > 0)
                {
                    XmlNode internetPermission = internetPermissions.Item(0);
                    internetPermission.ParentNode.RemoveChild(internetPermission);
                    changed = true;
                }

                if (changed)
                {
                    document.Save(manifestPath);
                }
            }

            if (!found)
            {
                throw new InvalidOperationException("P04 build did not declare the Meta passthrough feature.");
            }

            Debug.Log("P04 manifest verified: PASSTHROUGH is Supported (required=false).");
        }
    }
}
