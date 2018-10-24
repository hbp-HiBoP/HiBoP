using System;
using System.Linq;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace HBP.UI
{
    [CreateAssetMenu(menuName = "Tools/Prefabs/Referencer", fileName = "Referencer")]
    public class PrefabReferencer : ScriptableObject
    {
        public GameObject[] Prefabs;

        public GameObject GetPrefab(string name)
        {
            GameObject result = Prefabs.FirstOrDefault((prefab) => prefab.name == name);
            if(result == null) Debug.LogWarning("The prefab can not be found.");
            return result;
        }
        public GameObject GetPrefab(Type type) 
        {
            GameObject result = Prefabs.FirstOrDefault((prefab) => prefab.GetComponent(type) != null);
            if(result == null) Debug.LogWarning("The prefab can not be found.");
            return result;
        }
    }

    [Serializable]
    public struct Link
    {
        public string name;
        public GameObject Prefab;
    }
}