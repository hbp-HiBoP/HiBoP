using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace HBP.Core.Tools
{
    public static class ClassLoaderSaver
    {
        public static T LoadFromXML<T>(string path) where T : new()
        {
            T result = new T();
            try
            {
                using (StreamReader streamReader = new StreamReader(path))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    result = (T)serializer.Deserialize(streamReader.BaseStream);
                    streamReader.Close();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return result;
        }
        public static T LoadFromJson<T>(string path) where T : new()
        {
            T result = new T();
            try
            {
                using (StreamReader streamReader = new StreamReader(path))
                {
                    result = JsonConvert.DeserializeObject<T>(streamReader.ReadToEnd(), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return result;
        }
        public static T LoadFromJson2<T>(string path) where T : new()
        {
            T result = new T();
            try
            {
                using (StreamReader streamReader = new StreamReader(path))
                {
                    result = JsonConvert.DeserializeObject<T>(streamReader.ReadToEnd(), new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto });
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return result;
        }
        public static bool SaveToXML<T>(T instance, string path,bool overwrite = false) where T : new()
        {
            try
            {
                if(!overwrite) GenerateUniqueSavePath(ref path);
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
        public static bool SaveToJSon<T>(T instance, string path, bool overwrite = false) where T : new()
        {
            try
            {
                if(!overwrite) GenerateUniqueSavePath(ref path);
                using (StreamWriter streamWriter = new StreamWriter(path))
                {
                    string json = JsonConvert.SerializeObject(instance, Formatting.Indented, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto, TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple });
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
        public static void GenerateUniqueSavePath(ref string path)
        {
            string extension = Path.GetExtension(path);
            string pathWithoutExtension = Path.GetFullPath(path).Remove(Path.GetFullPath(path).Length - extension.Length);
            int count = 0;
            while (File.Exists(path))
            {
                string temp = string.Format("{0}({1})", pathWithoutExtension, ++count);
                path = temp + extension;
            }
        }
        public static void GenerateUniqueDirectoryPath(ref string path)
        {
            string fullPath = Path.GetFullPath(path);
            int count = 0;
            while (Directory.Exists(path))
            {
                path = string.Format("{0}({1})", fullPath, ++count);
            }
        }
    }
}