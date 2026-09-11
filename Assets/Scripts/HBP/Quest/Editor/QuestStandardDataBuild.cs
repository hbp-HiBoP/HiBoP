using System.IO;
using System.Text;
using HBP.Core.Tools;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace HBP.Quest.Editor
{
    public sealed class QuestStandardDataBuild : BuildPlayerProcessor
    {
        public override int callbackOrder => 0;

        public override void PrepareForBuild(BuildPlayerContext context)
        {
            if (context.BuildPlayerOptions.target != BuildTarget.Android) return;
            string source = Path.Combine(Application.dataPath, "Data");
            var manifest = new StringBuilder();
            foreach (string relative in StandardData.EnumerateFiles(source))
            {
                // Localizers are distributed separately on both Desktop and Quest.
                if (relative.StartsWith("Atlases/Localizers/", System.StringComparison.Ordinal)) continue;
                string input = StandardData.Resolve(source, relative);
                context.AddAdditionalPathToStreamingAssets(input, "ScientificData/" + StandardData.PackagedPath(relative));
                manifest.Append(StandardData.HashFile(input)).Append(' ').Append(relative).Append('\n');
            }

            // Only the small generated manifest needs a temporary file. Reference
            // data stays in Assets/Data and is copied directly by the build pipeline.
            string manifestDirectory = Path.GetFullPath("Library/HBP/QuestStandardData");
            Directory.CreateDirectory(manifestDirectory);
            string manifestPath = Path.Combine(manifestDirectory, StandardData.ManifestName);
            File.WriteAllText(manifestPath, manifest.ToString(), new UTF8Encoding(false));
            context.AddAdditionalPathToStreamingAssets(manifestPath, "ScientificData/" + StandardData.ManifestName);
        }
    }
}
