using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace HBP.Core.Tools
{
    /// <summary>Installed reference files, accessible to native readers through filesystem paths.</summary>
    public static class StandardData
    {
        public const string ManifestName = "standard-data.sha256";
        private static AsyncLazy s_Installation;

        // Android's asset merger expands .gz files. Keep their original bytes in
        // the package; installation restores the scientific filename on disk.
        public static string PackagedPath(string relative) => relative.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ? relative + ".bytes" : relative;

        public static IEnumerable<string> EnumerateFiles(string root)
        {
            yield return "IRM/MNI.nii";
            foreach (string name in new[] { "MNI.trm", "MNI_Lhemi.gii", "MNI_Rhemi.gii", "MNI_Lwhite.gii", "MNI_Rwhite.gii" }) yield return "Meshes/" + name;
            foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "Atlases"), "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
                if (!file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    yield return file.Substring(root.Length + 1).Replace('\\', '/');
        }

        public static string HashFile(string path)
        {
            using var input = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
        }

        public static string CompanionFile(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".img" ? Path.ChangeExtension(path, ".hdr") : extension == ".hdr" ? Path.ChangeExtension(path, ".img") : null;
        }

        public static string Resolve(string root, string relative)
        {
            if (string.IsNullOrEmpty(relative) || relative.Contains('\\') || relative.Split('/').Any(p => p.Length == 0 || p == "." || p == "..") || relative.Contains(':')) throw new InvalidDataException("Invalid resource path.");
            string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string result = Path.GetFullPath(Path.Combine(prefix, relative));
            if (!result.StartsWith(prefix, StringComparison.Ordinal)) throw new InvalidDataException("Resource escaped its directory.");
            return result;
        }

        public static UniTask EnsureInstalledAsync()
        {
            s_Installation ??= UniTask.Lazy(InstallAsync);
            return s_Installation.Task;
        }

        private static async UniTask InstallAsync()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            await UniTask.SwitchToMainThread();
            string source = Application.streamingAssetsPath + "/ScientificData/";
            using var manifestRequest = UnityWebRequest.Get(source + ManifestName);
            await manifestRequest.SendWebRequest();
            foreach (string line in manifestRequest.downloadHandler.text.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string hash = line.Substring(0, 64), relative = line.Substring(65).TrimEnd('\r');
                string path = Resolve(ApplicationState.DataPath, relative);
                if (File.Exists(path) && HashFile(path) == hash) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporary = path + ".installing";
                try
                {
                    using var request = UnityWebRequest.Get(source + PackagedPath(relative));
                    request.downloadHandler = new DownloadHandlerFile(temporary);
                    await request.SendWebRequest();
                    if (HashFile(temporary) != hash) throw new InvalidDataException($"Installed reference checksum mismatch: {relative}");
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(temporary, path);
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
#else
            await UniTask.CompletedTask;
#endif
        }
    }
}
