using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.BIDS;
using HBP.Data.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HBP.Dev
{
    public static class SerializationTypeRegistryGenerator
    {
        public const string GenerateMenuPath = "Tools/Serialization/Generate Type Registry";

        private const string AliasConfigurationPath = "Assets/SerializationTypeAliases.json";
        private const string CoreOutputPath = "Assets/Scripts/HBP/Core/Tools/GeneratedCoreSerializationTypes.cs";
        private const string DataOutputPath = "Assets/Scripts/HBP/Data/Tools/GeneratedDataSerializationTypes.cs";
        private static readonly UTF8Encoding s_Utf8WithoutBom = new(false);

        [MenuItem(GenerateMenuPath)]
        public static void GenerateFromMenu()
        {
            bool changed = Generate();
            Debug.Log(changed ? "HiBoP serialization type registry generated. Unity will recompile the updated sources." : "HiBoP serialization type registry is already up to date.");
        }

        public static bool Generate()
        {
            IReadOnlyDictionary<string, string> expectedFiles = BuildExpectedFiles();
            bool changed = false;
            foreach (KeyValuePair<string, string> expectedFile in expectedFiles)
            {
                string absolutePath = GetAbsolutePath(expectedFile.Key);
                string current = File.Exists(absolutePath) ? File.ReadAllText(absolutePath) : null;
                if (string.Equals(current, expectedFile.Value, StringComparison.Ordinal)) continue;

                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                File.WriteAllText(absolutePath, expectedFile.Value, s_Utf8WithoutBom);
                AssetDatabase.ImportAsset(expectedFile.Key, ImportAssetOptions.ForceUpdate);
                changed = true;
            }

            return changed;
        }

        public static bool TryValidate(out string error)
        {
            try
            {
                IReadOnlyDictionary<string, string> expectedFiles = BuildExpectedFiles();
                List<string> staleFiles = new();
                List<string> missingTypes = new();

                foreach (KeyValuePair<string, string> expectedFile in expectedFiles)
                {
                    string absolutePath = GetAbsolutePath(expectedFile.Key);
                    string current = File.Exists(absolutePath) ? File.ReadAllText(absolutePath) : null;
                    if (string.Equals(current, expectedFile.Value, StringComparison.Ordinal)) continue;

                    staleFiles.Add(expectedFile.Key);
                    foreach (SerializableTypeEntry entry in DiscoverSerializableTypes().Where(entry => OutputPathFor(entry.Type) == expectedFile.Key))
                    {
                        if (current == null || !current.Contains($"typeof(global::{GetCSharpTypeName(entry.Type)})"))
                        {
                            missingTypes.Add(entry.Type.FullName);
                        }
                    }
                }

                if (staleFiles.Count == 0)
                {
                    error = null;
                    return true;
                }

                string missingTypeDetails = missingTypes.Count == 0 ? string.Empty : $"\nMissing types: {string.Join(", ", missingTypes.OrderBy(value => value))}.";
                error = $"The generated HiBoP serialization type registry is out of date: " + $"{string.Join(", ", staleFiles)}.{missingTypeDetails}\n" + $"Run '{GenerateMenuPath}' and wait for Unity to finish compiling.";
                return false;
            }
            catch (Exception exception)
            {
                error = $"The HiBoP serialization type registry could not be validated: {exception.Message}";
                return false;
            }
        }

        public static void EnsureUpToDateForBuild()
        {
            if (Generate())
            {
                throw new BuildFailedException("The HiBoP serialization type registry was updated. Wait for Unity to finish compiling, " + "then launch the build again.");
            }

            if (!TryValidate(out string error))
            {
                throw new BuildFailedException(error);
            }
        }

        private static IReadOnlyDictionary<string, string> BuildExpectedFiles()
        {
            List<SerializableTypeEntry> entries = DiscoverSerializableTypes().ToList();
            AliasConfiguration configuration = LoadAliasConfiguration();
            ApplyAliases(entries, configuration);

            return new Dictionary<string, string>
            {
                [CoreOutputPath] = GenerateCoreSource(entries.Where(entry => OutputPathFor(entry.Type) == CoreOutputPath)),
                [DataOutputPath] = GenerateDataSource(entries.Where(entry => OutputPathFor(entry.Type) == DataOutputPath))
            };
        }

        private static IEnumerable<SerializableTypeEntry> DiscoverSerializableTypes()
        {
            Assembly[] assemblies =
            {
                typeof(BaseData).Assembly,
                typeof(BIDSExportConfiguration).Assembly
            };

            return assemblies.Distinct().SelectMany(assembly => assembly.GetTypes()).Where(type => type.IsClass && !type.IsAbstract).Where(type => type.GetCustomAttributes(typeof(JsonObjectAttribute), true).Length > 0).OrderBy(type => type.Assembly.GetName().Name).ThenBy(type => type.FullName).Select(type => new SerializableTypeEntry(type));
        }

        private static AliasConfiguration LoadAliasConfiguration()
        {
            string absolutePath = GetAbsolutePath(AliasConfigurationPath);
            if (!File.Exists(absolutePath))
            {
                throw new InvalidOperationException($"Alias configuration not found at '{AliasConfigurationPath}'.");
            }

            AliasConfiguration configuration = JsonConvert.DeserializeObject<AliasConfiguration>(File.ReadAllText(absolutePath));
            if (configuration == null || configuration.SchemaVersion != 1)
            {
                throw new InvalidOperationException("SerializationTypeAliases.json must use schemaVersion 1.");
            }

            configuration.NamespaceAliases ??= new List<NamespaceAlias>();
            configuration.TypeAliases ??= new List<TypeAlias>();
            return configuration;
        }

        private static void ApplyAliases(IReadOnlyCollection<SerializableTypeEntry> entries, AliasConfiguration configuration)
        {
            Dictionary<string, SerializableTypeEntry> entriesByCurrentName = entries.ToDictionary(entry => entry.Type.FullName, StringComparer.Ordinal);
            Dictionary<string, SerializableTypeEntry> entriesBySerializedName = entries.ToDictionary(entry => entry.Type.FullName, StringComparer.Ordinal);

            foreach (NamespaceAlias namespaceAlias in configuration.NamespaceAliases)
            {
                if (string.IsNullOrEmpty(namespaceAlias.SerializedPrefix) || string.IsNullOrEmpty(namespaceAlias.CurrentPrefix))
                {
                    throw new InvalidOperationException("Namespace aliases require serializedPrefix and currentPrefix.");
                }

                SerializableTypeEntry[] targets = entries.Where(entry => entry.Type.FullName.StartsWith(namespaceAlias.CurrentPrefix, StringComparison.Ordinal)).ToArray();
                if (targets.Length == 0)
                {
                    throw new InvalidOperationException($"Namespace alias current prefix '{namespaceAlias.CurrentPrefix}' does not match any serializable type.");
                }

                foreach (SerializableTypeEntry target in targets)
                {
                    string serializedName = namespaceAlias.SerializedPrefix + target.Type.FullName[namespaceAlias.CurrentPrefix.Length..];
                    AddAlias(entriesBySerializedName, target, serializedName);
                }
            }

            foreach (TypeAlias typeAlias in configuration.TypeAliases)
            {
                if (string.IsNullOrEmpty(typeAlias.SerializedType) || string.IsNullOrEmpty(typeAlias.CurrentType))
                {
                    throw new InvalidOperationException("Type aliases require serializedType and currentType.");
                }

                if (!entriesByCurrentName.TryGetValue(typeAlias.CurrentType, out SerializableTypeEntry target))
                {
                    throw new InvalidOperationException($"Type alias target '{typeAlias.CurrentType}' is not a current serializable type.");
                }

                AddAlias(entriesBySerializedName, target, typeAlias.SerializedType);
            }
        }

        private static void AddAlias(IDictionary<string, SerializableTypeEntry> entriesBySerializedName, SerializableTypeEntry target, string serializedName)
        {
            if (entriesBySerializedName.TryGetValue(serializedName, out SerializableTypeEntry existing))
            {
                if (existing.Type != target.Type)
                {
                    throw new InvalidOperationException($"Serialized type alias '{serializedName}' targets both " + $"'{existing.Type.FullName}' and '{target.Type.FullName}'.");
                }

                return;
            }

            entriesBySerializedName.Add(serializedName, target);
            target.Aliases.Add(serializedName);
        }

        private static string GenerateCoreSource(IEnumerable<SerializableTypeEntry> entries)
        {
            StringBuilder source = new();
            source.AppendLine("// <auto-generated />");
            source.AppendLine();
            source.AppendLine("namespace HBP.Core.Tools");
            source.AppendLine("{");
            source.AppendLine("    internal static class GeneratedCoreSerializationTypes");
            source.AppendLine("    {");
            source.AppendLine("        internal static void Register()");
            source.AppendLine("        {");
            AppendRegistrations(source, entries, "SerializationTypeRegistry");
            source.AppendLine("        }");
            source.AppendLine("    }");
            source.AppendLine("}");
            return source.ToString();
        }

        private static string GenerateDataSource(IEnumerable<SerializableTypeEntry> entries)
        {
            StringBuilder source = new();
            source.AppendLine("// <auto-generated />");
            source.AppendLine();
            source.AppendLine("using UnityEngine;");
            source.AppendLine("using UnityEngine.Scripting;");
            source.AppendLine();
            source.AppendLine("namespace HBP.Data.Tools");
            source.AppendLine("{");
            source.AppendLine("    [Preserve]");
            source.AppendLine("    public static class GeneratedDataSerializationTypes");
            source.AppendLine("    {");
            source.AppendLine("        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]");
            source.AppendLine("        public static void Register()");
            source.AppendLine("        {");
            AppendRegistrations(source, entries, "global::HBP.Core.Tools.SerializationTypeRegistry");
            source.AppendLine("        }");
            source.AppendLine("    }");
            source.AppendLine("}");
            return source.ToString();
        }

        private static void AppendRegistrations(StringBuilder source, IEnumerable<SerializableTypeEntry> entries, string registryTypeName)
        {
            foreach (SerializableTypeEntry entry in entries.OrderBy(value => value.Type.FullName))
            {
                IEnumerable<string> serializedNames = new[] { entry.Type.FullName }.Concat(entry.Aliases.OrderBy(value => value));
                source.Append("            ");
                source.Append(registryTypeName);
                source.Append(".RegisterGenerated(typeof(global::");
                source.Append(GetCSharpTypeName(entry.Type));
                source.Append("), ");
                source.Append(string.Join(", ", serializedNames.Select(JsonConvert.ToString)));
                source.AppendLine(");");
            }
        }

        private static string OutputPathFor(Type type)
        {
            string assemblyName = type.Assembly.GetName().Name;
            return assemblyName switch
            {
                "HBP.Core.Runtime" => CoreOutputPath,
                "HBP.Data.Runtime" => DataOutputPath,
                _ => throw new InvalidOperationException($"Serializable type '{type.FullName}' belongs to unsupported assembly '{assemblyName}'.")
            };
        }

        private static string GetCSharpTypeName(Type type)
        {
            if (type.FullName.Contains("`"))
            {
                throw new InvalidOperationException($"Generic serializable type '{type.FullName}' requires an explicit generator implementation.");
            }

            return type.FullName.Replace("+", ".");
        }

        private static string GetAbsolutePath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private sealed class SerializableTypeEntry
        {
            public Type Type { get; }
            public List<string> Aliases { get; } = new();

            public SerializableTypeEntry(Type type)
            {
                Type = type;
            }
        }

        private sealed class AliasConfiguration
        {
            [JsonProperty("schemaVersion")] public int SchemaVersion { get; set; }

            [JsonProperty("namespaceAliases")] public List<NamespaceAlias> NamespaceAliases { get; set; }

            [JsonProperty("typeAliases")] public List<TypeAlias> TypeAliases { get; set; }
        }

        private sealed class NamespaceAlias
        {
            [JsonProperty("serializedPrefix")] public string SerializedPrefix { get; set; }

            [JsonProperty("currentPrefix")] public string CurrentPrefix { get; set; }
        }

        private sealed class TypeAlias
        {
            [JsonProperty("serializedType")] public string SerializedType { get; set; }

            [JsonProperty("currentType")] public string CurrentType { get; set; }
        }
    }

    [InitializeOnLoad]
    internal static class SerializationTypeRegistryEditorGuard
    {
        static SerializationTypeRegistryEditorGuard()
        {
            GeneratedDataSerializationTypes.Register();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            if (SerializationTypeRegistryGenerator.TryValidate(out string error)) return;

            Debug.LogError(error);
            EditorApplication.ExitPlaymode();
        }
    }

    internal sealed class SerializationTypeRegistryBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!SerializationTypeRegistryGenerator.TryValidate(out string error))
            {
                throw new BuildFailedException(error);
            }
        }
    }
}
