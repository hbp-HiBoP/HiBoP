using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HBP.Theme
{
    public class ContextMenu : MonoBehaviour
    {
        [MenuItem("Assets/Save to json")]
        private static void Save()
        {
            ScriptableObject scriptableObject = (Selection.activeObject as ScriptableObject);
            string json = JsonUtility.ToJson(scriptableObject,true);
            string path = Application.dataPath + "/Data/" + scriptableObject.name + ".json";
            Debug.Log(path);
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.Write(json);
            }
        }
        [MenuItem("Assets/Save to json", true)]
        private static bool CanSave()
        {
            return Selection.activeObject is ScriptableObject;
        }

        [MenuItem("Assets/Load from json")]
        private static void Load()
        {
            ScriptableObject scriptableObject = (Selection.activeObject as ScriptableObject);
            string path = Application.dataPath + "/Data/" + scriptableObject.name + ".json";
            Debug.Log(scriptableObject.GetType());
            using (StreamReader sr = new StreamReader(path))
            {
                string json = sr.ReadToEnd();
                JsonUtility.FromJsonOverwrite(json, scriptableObject);
            }
        }
        [MenuItem("Assets/Load from json", true)]
        private static bool CanLoad()
        {
            return Selection.activeObject is ScriptableObject && File.Exists(Application.dataPath + "/Data/" + Selection.activeObject.name + ".json");
        }
    }
}

