using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Cysharp.Threading.Tasks;

namespace HBP.Core.Tools
{
    public static class ClassLoaderSaver
    {
        private static readonly LegacyAssemblySerializationBinder m_Binder = new();

        private static readonly JsonSerializerSettings m_Settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Formatting = Formatting.Indented,
            SerializationBinder = m_Binder,
        };

        public static T LoadFromJson<T>(string path) where T : new()
        {
            T result = new();
            using (StreamReader streamReader = new(path))
            {
                string jsonContent = streamReader.ReadToEnd();
                result = JsonConvert.DeserializeObject<T>(jsonContent, m_Settings);
            }
            return result;
        }
        public static async UniTask<T> LoadFromJsonAsync<T>(string path) where T : new()
        {
            await UniTask.SwitchToThreadPool();
            using StreamReader streamReader = new(path);
            T result = JsonConvert.DeserializeObject<T>(streamReader.ReadToEnd(), m_Settings);
            return result;
        }
        public static bool SaveToJSon<T>(T instance, string path, bool overwrite = false) where T : new()
        {
            try
            {
                if (!overwrite) path = path.GenerateUniqueFilePath();
                using StreamWriter streamWriter = new(path);
                string json = JsonConvert.SerializeObject(instance, m_Settings);
                streamWriter.Write(json);
                streamWriter.Close();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }
        public static async UniTask<bool> SaveToJsonAsync<T>(T instance, string path, bool overwrite = false) where T : new()
        {
            try
            {
                await UniTask.SwitchToThreadPool();
                if (!overwrite) path = path.GenerateUniqueFilePath();
                using StreamWriter streamWriter = new(path);
                string json = JsonConvert.SerializeObject(instance, m_Settings);
                streamWriter.Write(json);
                streamWriter.Close();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }
        public static T LoadFromJsonString<T>(string jsonString) where T : new()
        {
            return JsonConvert.DeserializeObject<T>(jsonString, m_Settings);
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
            private static readonly HashSet<string> s_LegacyAssemblyNames = new()
            {
                "Assembly-CSharp",
                "Assembly-CSharp-firstpass",
                "Assembly-CSharp-Editor"
            };

            private readonly Dictionary<string, Type> m_TypesByFullName = BuildTypeRegistry();

            public Type BindToType(string assemblyName, string typeName)
            {
                if (!string.IsNullOrEmpty(typeName) && m_TypesByFullName.TryGetValue(typeName, out Type registeredType))
                {
                    return registeredType;
                }

                Type resolvedType = Type.GetType($"{typeName}, {assemblyName}");
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
