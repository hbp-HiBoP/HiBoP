using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;

namespace HBP.Core.Tools
{
    public static class ClassLoaderSaver
    {
        private static readonly JsonSerializerSettings m_Settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Formatting = Formatting.Indented,
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
    }
}