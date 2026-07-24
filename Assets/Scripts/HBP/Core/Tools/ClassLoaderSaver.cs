using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Cysharp.Threading.Tasks;

namespace HBP.Core.Tools
{
    public static class ClassLoaderSaver
    {
        private const int STREAM_BUFFER_SIZE = 64 * 1024;
        private static readonly LegacyAssemblySerializationBinder m_Binder = new();
        private static readonly UTF8Encoding m_Utf8WithoutBom = new(false);

        private static readonly JsonSerializerSettings m_ReadSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            SerializationBinder = m_Binder,
        };

        private static readonly JsonSerializerSettings m_WriteSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Formatting = Formatting.Indented,
            SerializationBinder = m_Binder,
        };

        public static T LoadFromJson<T>(string path)
        {
            return LoadFromJson<T>(path, LoadingDiagnostics.Phase.None, LoadingDiagnostics.Phase.None);
        }

        public static T LoadFromJson<T>(string path, LoadingDiagnostics.Phase readPhase, LoadingDiagnostics.Phase deserializePhase, int concurrency = 0)
        {
            return LoadFromJsonFile<T>(path, readPhase, deserializePhase, concurrency);
        }

        public static T LoadFromJson<T>(Stream stream, long byteCount, LoadingDiagnostics.Phase readPhase, LoadingDiagnostics.Phase deserializePhase, int concurrency = 0)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            return LoadFromJsonStream<T>(stream, byteCount, readPhase, deserializePhase, concurrency);
        }

        public static async UniTask<T> LoadFromJsonAsync<T>(string path)
        {
            return await LoadFromJsonAsync<T>(path, LoadingDiagnostics.Phase.None, LoadingDiagnostics.Phase.None);
        }

        public static async UniTask<T> LoadFromJsonAsync<T>(string path, LoadingDiagnostics.Phase readPhase, LoadingDiagnostics.Phase deserializePhase, int concurrency = 0)
        {
            await UniTask.SwitchToThreadPool();
            return LoadFromJsonFile<T>(path, readPhase, deserializePhase, concurrency);
        }

        public static bool SaveToJSon<T>(T instance, string path, bool overwrite = false)
        {
            return SaveToJsonStream(instance, path, overwrite);
        }

        public static async UniTask<bool> SaveToJsonAsync<T>(T instance, string path, bool overwrite = false)
        {
            await UniTask.SwitchToThreadPool();
            return SaveToJsonStream(instance, path, overwrite);
        }

        public static T LoadFromJsonString<T>(string jsonString)
        {
            using StringReader stringReader = new(jsonString);
            using JsonTextReader jsonReader = new(stringReader);
            return Deserialize<T>(jsonReader);
        }

        private static T LoadFromJsonFile<T>(string path, LoadingDiagnostics.Phase readPhase, LoadingDiagnostics.Phase deserializePhase, int concurrency)
        {
            long fileLength = LoadingDiagnostics.GetFileLength(path);
            using (FileStream fileStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, STREAM_BUFFER_SIZE, FileOptions.SequentialScan))
            {
                return LoadFromJsonStream<T>(fileStream, fileLength, readPhase, deserializePhase, concurrency);
            }
        }

        private static T LoadFromJsonStream<T>(Stream stream, long byteCount, LoadingDiagnostics.Phase readPhase, LoadingDiagnostics.Phase deserializePhase, int concurrency)
        {
            int readObjectCount = readPhase == deserializePhase ? 1 : 0;

            // Streaming makes file reads part of deserialization. The outer read phase
            // retains file/byte accounting; distinct read/deserialize timings overlap.
            // TEMP-LOADING-PROFILING
            using (LoadingDiagnostics.BeginPhase(readPhase, 1, byteCount, readObjectCount, concurrency))
            using (StreamReader streamReader = new(stream, Encoding.UTF8, true, STREAM_BUFFER_SIZE, true))
            using (JsonTextReader jsonReader = new(streamReader))
            {
                if (readPhase == deserializePhase)
                {
                    return Deserialize<T>(jsonReader);
                }

                // TEMP-LOADING-PROFILING
                using (LoadingDiagnostics.BeginPhase(deserializePhase, objectCount: 1, concurrency: concurrency))
                {
                    return Deserialize<T>(jsonReader);
                }
            }
        }

        private static T Deserialize<T>(JsonReader jsonReader)
        {
            JsonSerializer serializer = JsonSerializer.Create(m_ReadSettings);
            return serializer.Deserialize<T>(jsonReader);
        }

        private static bool SaveToJsonStream<T>(T instance, string path, bool overwrite)
        {
            try
            {
                if (!overwrite) path = path.GenerateUniqueFilePath();

                using FileStream fileStream = new(path, FileMode.Create, FileAccess.Write, FileShare.Read, STREAM_BUFFER_SIZE, FileOptions.SequentialScan);
                using StreamWriter streamWriter = new(fileStream, m_Utf8WithoutBom, STREAM_BUFFER_SIZE);
                using JsonTextWriter jsonWriter = new(streamWriter);
                JsonSerializer serializer = JsonSerializer.Create(m_WriteSettings);
                serializer.Serialize(jsonWriter, instance);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        public static T LoadFromXML<T>(string path) where T : new()
        {
            T result = new();
            using (StreamReader streamReader = new(path))
            {
                XmlSerializer serializer = new(typeof(T));
                result = (T)serializer.Deserialize(streamReader.BaseStream);
                streamReader.Close();
            }
            return result;
        }
        public static bool SaveToXML<T>(T instance, string path, bool overwrite = false) where T : new()
        {
            try
            {
                if (!overwrite) path = path.GenerateUniqueFilePath();
                using (StreamWriter streamWriter = new(path))
                {
                    XmlSerializer serializer = new(typeof(T));
                    serializer.Serialize(streamWriter, instance);
                    streamWriter.Close();
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        private sealed class LegacyAssemblySerializationBinder : ISerializationBinder
        {
            private static readonly Dictionary<string, string> s_LegacyNamespacePrefixes = new()
            {
                { "HBP.Data.Database.", "HBP.Core.Database." },
                { "HBP.Data.Preferences.", "HBP.Core.Preferences." },
            };

            private static readonly HashSet<string> s_LegacyAssemblyNames = new()
            {
                "Assembly-CSharp",
                "Assembly-CSharp-firstpass",
                "Assembly-CSharp-Editor"
            };

            private readonly Dictionary<string, Type> m_TypesByFullName = BuildTypeRegistry();

            public Type BindToType(string assemblyName, string typeName)
            {
                if (!string.IsNullOrEmpty(typeName) && TryGetRegisteredType(typeName, out Type registeredType))
                {
                    return registeredType;
                }

                string migratedTypeName = GetMigratedTypeName(typeName);
                if (!string.IsNullOrEmpty(migratedTypeName) && TryGetRegisteredType(migratedTypeName, out registeredType))
                {
                    return registeredType;
                }

                Type resolvedType = ResolveType(typeName, assemblyName);
                if (resolvedType != null)
                {
                    return resolvedType;
                }

                resolvedType = ResolveType(migratedTypeName, assemblyName);
                if (resolvedType != null)
                {
                    return resolvedType;
                }

                if (s_LegacyAssemblyNames.Contains(assemblyName) && !string.IsNullOrEmpty(typeName))
                {
                    Debug.LogWarning($"Could not resolve legacy JSON type '{typeName}, {assemblyName}'.");
                }
                return null;
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = serializedType.Assembly.GetName().Name;
                typeName = serializedType.FullName;
            }

            private bool TryGetRegisteredType(string typeName, out Type type)
            {
                type = null;
                return !string.IsNullOrEmpty(typeName) && m_TypesByFullName.TryGetValue(typeName, out type);
            }

            private static string GetMigratedTypeName(string typeName)
            {
                if (string.IsNullOrEmpty(typeName)) return typeName;

                foreach (var legacyNamespacePrefix in s_LegacyNamespacePrefixes)
                {
                    if (typeName.StartsWith(legacyNamespacePrefix.Key, StringComparison.Ordinal))
                    {
                        return legacyNamespacePrefix.Value + typeName[legacyNamespacePrefix.Key.Length..];
                    }
                }
                return typeName;
            }

            private static Type ResolveType(string typeName, string assemblyName)
            {
                if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(assemblyName)) return null;
                return Type.GetType($"{typeName}, {assemblyName}");
            }

            private static Dictionary<string, Type> BuildTypeRegistry()
            {
                string[] supportedAssemblyNames =
                {
                    "HBP.Core.Runtime",
                    "HBP.Data.Runtime",
                    "Assembly-CSharp",
                    "Assembly-CSharp-firstpass"
                };

                return AppDomain.CurrentDomain.GetAssemblies()
                    .Where(assembly => supportedAssemblyNames.Contains(assembly.GetName().Name))
                    .SelectMany(GetSerializableTypes)
                    .GroupBy(type => type.FullName)
                    .Where(group => !string.IsNullOrEmpty(group.Key))
                    .ToDictionary(group => group.Key, group => group.First());
            }

            private static IEnumerable<Type> GetSerializableTypes(System.Reflection.Assembly assembly)
            {
                try
                {
                    return assembly.GetTypes()
                        .Where(type => type.IsClass && !type.IsAbstract)
                        .Where(type => type.GetCustomAttributes(typeof(JsonObjectAttribute), true).Length > 0);
                }
                catch (System.Reflection.ReflectionTypeLoadException e)
                {
                    return e.Types
                        .Where(type => type != null && type.IsClass && !type.IsAbstract)
                        .Where(type => type.GetCustomAttributes(typeof(JsonObjectAttribute), true).Length > 0);
                }
            }
        }
    }
}
