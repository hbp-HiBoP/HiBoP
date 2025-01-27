using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using HBP.Core.Data;

namespace HBP.Core.Tools
{
    public static class ClassLoaderSaver
    {
        public static T LoadFromJson<T>(string path) where T : new()
        {
            T result = new T();
            using (StreamReader streamReader = new StreamReader(path))
            {
                result = JsonConvert.DeserializeObject<T>(streamReader.ReadToEnd(), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
            }
            return result;
        }
        public static async Task<T> LoadFromJsonAsync<T>(string path) where T : new()
        {
            T result = new T();
            using (StreamReader streamReader = new StreamReader(path))
            {
                result = await Task.Run(() => JsonConvert.DeserializeObject<T>(streamReader.ReadToEnd(), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto }));
            }
            return result;
        }
        public static bool SaveToJSon<T>(T instance, string path, bool overwrite = false) where T : new()
        {
            try
            {
                if (!overwrite) path = path.GenerateUniqueFilePath();
                using (StreamWriter streamWriter = new StreamWriter(path))
                {
                    string json = JsonConvert.SerializeObject(instance, Formatting.Indented, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple });
                    streamWriter.Write(json);
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
        public static async Task<bool> SaveToJSonAsync<T>(T instance, string path, bool overwrite = false) where T : new()
        {
            try
            {
                if (!overwrite) path = path.GenerateUniqueFilePath();
                using (StreamWriter streamWriter = new StreamWriter(path))
                {
                    string json = await Task.Run(() => JsonConvert.SerializeObject(instance, Formatting.Indented, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple }));
                    streamWriter.Write(json);
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
        public static T LoadFromXML<T>(string path) where T : new()
        {
            T result = new T();
            using (StreamReader streamReader = new StreamReader(path))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
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
                using (StreamWriter streamWriter = new StreamWriter(path))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
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