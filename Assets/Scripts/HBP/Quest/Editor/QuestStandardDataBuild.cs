using System.IO;
using System.Text;
using HBP.Core.Tools;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HBP.Quest.Editor
{
    public sealed class QuestStandardDataBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            string source = Path.Combine(Application.dataPath, "Data"), target = Path.Combine(Application.streamingAssetsPath, "ScientificData");
            var manifest = new StringBuilder();
            foreach (string relative in StandardData.EnumerateFiles(source))
            {
                string input = StandardData.Resolve(source, relative), output = StandardData.Resolve(target, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.Copy(input, output, true);
                manifest.Append(StandardData.HashFile(input)).Append(' ').Append(relative).Append('\n');
            }

            File.WriteAllText(Path.Combine(target, StandardData.ManifestName), manifest.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
        }
    }
}
