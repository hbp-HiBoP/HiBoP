using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Build;

namespace HBP.Dev
{
    internal static class QuestDistributionValidation
    {
        private static readonly string[] Forbidden = { "adb", "adb.exe", "AdbWinApi.dll", "AdbWinUsbApi.dll" };

        public static void ValidateManagedDependencies()
        {
            string root = Path.GetDirectoryName(UnityEngine.Application.dataPath);
            var pins = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(root, "Tools", "QuestMdns", "ManagedPlugins.lock.json")));
            foreach (var pin in pins.Properties())
            {
                string path = Path.Combine(root, "Assets", "Plugins", "Managed", "QuestMdns", pin.Name);
                using var sha = System.Security.Cryptography.SHA256.Create();
                using var stream = File.OpenRead(path);
                string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                if (actual != pin.Value.ToString()) throw new BuildFailedException("Quest mDNS dependency does not match its tested pin: " + pin.Name);
            }
        }

        public static void CleanAndValidate(string buildDirectory)
        {
            string root = Path.GetFullPath(buildDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (string directory in Directory.GetDirectories(root, "QuestUsb", SearchOption.AllDirectories))
            {
                string resolved = Path.GetFullPath(directory);
                if (!resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new BuildFailedException("Invalid legacy Quest USB output path.");
                if (string.Equals(Path.GetFileName(Path.GetDirectoryName(resolved)), "StreamingAssets", StringComparison.OrdinalIgnoreCase))
                    Directory.Delete(resolved, true);
            }

            string forbidden = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).FirstOrDefault(path => Forbidden.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase));
            if (forbidden != null) throw new BuildFailedException("ADB must not be distributed with HiBoP: " + forbidden);
        }

        public static void ConfigureMacNetworkPrivacy(string applicationDirectory)
        {
            string path = Path.Combine(applicationDirectory, "Contents", "Info.plist");
            var document = XDocument.Load(path);
            var dictionary = document.Root.Element("dict");
            Set(dictionary, "NSLocalNetworkUsageDescription", new XElement("string", "HiBoP connects to your Quest over USB or your local network to synchronize visualizations."));
            Set(dictionary, "NSBonjourServices", new XElement("array", new XElement("string", "_hibop-quest._tcp")));
            document.Save(path);
        }

        private static void Set(XElement dictionary, string key, XElement value)
        {
            var existing = dictionary.Elements("key").FirstOrDefault(element => element.Value == key);
            if (existing == null) dictionary.Add(new XElement("key", key), value);
            else existing.NextNode.ReplaceWith(value);
        }
    }
}
