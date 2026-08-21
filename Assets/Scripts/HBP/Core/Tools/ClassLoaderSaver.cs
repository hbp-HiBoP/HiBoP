using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
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
        private static readonly GeneratedSerializationBinder m_Binder = new();
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
            return LoadFromJsonFile<T>(path);
        }

        public static T LoadFromJson<T>(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return LoadFromJsonStream<T>(stream);
        }

        public static async UniTask<T> LoadFromJsonAsync<T>(string path)
        {
            await UniTask.SwitchToThreadPool();
            return LoadFromJsonFile<T>(path);
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

        public static void SaveToJsonAtomicOrThrow<T>(T instance, string path)
        {
            string fullPath = Path.GetFullPath(path);
            string directoryPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directoryPath)) Directory.CreateDirectory(directoryPath);
            string temporaryPath = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
            string backupPath = fullPath + ".bak-" + Guid.NewGuid().ToString("N");
            bool published = false;

            try
            {
                if (!SaveToJsonStream(instance, temporaryPath, true))
                {
                    throw new IOException($"Unable to serialize '{fullPath}'.");
                }

                if (File.Exists(fullPath))
                {
                    File.Replace(temporaryPath, fullPath, backupPath);
                }
                else
                {
                    File.Move(temporaryPath, fullPath);
                }

                published = true;
            }
            catch
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                if (File.Exists(backupPath) && !File.Exists(fullPath)) File.Move(backupPath, fullPath);
                throw;
            }
            finally
            {
                if (published) TryDeleteObsoleteFile(backupPath);
            }
        }

        private static void TryDeleteObsoleteFile(string path)
        {
            if (!File.Exists(path)) return;
            try
            {
                File.Delete(path);
            }
            catch
            {
                // The replacement is already committed. Leaving an obsolete backup is safer than
                // reporting a failed save after the target file has changed.
            }
        }

        public static async UniTask SaveToJsonOrThrowAsync<T>(T instance, string path, bool overwrite = false)
        {
            if (!await SaveToJsonAsync(instance, path, overwrite))
            {
                throw new IOException($"Unable to serialize '{Path.GetFullPath(path)}'.");
            }
        }

        public static T LoadFromJsonString<T>(string jsonString)
        {
            using StringReader stringReader = new(jsonString);
            using JsonTextReader jsonReader = new(stringReader);
            return Deserialize<T>(jsonReader);
        }

        private static T LoadFromJsonFile<T>(string path)
        {
            using (FileStream fileStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, STREAM_BUFFER_SIZE, FileOptions.SequentialScan))
            {
                return LoadFromJsonStream<T>(fileStream);
            }
        }

        private static T LoadFromJsonStream<T>(Stream stream)
        {
            using (StreamReader streamReader = new(stream, Encoding.UTF8, true, STREAM_BUFFER_SIZE, true))
            using (JsonTextReader jsonReader = new(streamReader))
            {
                return Deserialize<T>(jsonReader);
            }
        }

        private static T Deserialize<T>(JsonReader jsonReader)
        {
            JsonSerializer serializer = JsonSerializer.Create(m_ReadSettings);
            try
            {
                return serializer.Deserialize<T>(jsonReader);
            }
            catch (JsonSerializationException exception) when (exception.InnerException is JsonSerializationException registryException && registryException.Message.Contains("generated HiBoP type registry"))
            {
                throw new JsonSerializationException(registryException.Message, exception);
            }
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

        private sealed class GeneratedSerializationBinder : ISerializationBinder
        {
            public Type BindToType(string assemblyName, string typeName)
            {
                return SerializationTypeRegistry.Resolve(assemblyName, typeName);
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                SerializationTypeRegistry.GetSerializedName(serializedType, out assemblyName, out typeName);
            }
        }
    }
}
