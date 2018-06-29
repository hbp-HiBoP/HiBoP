using System;
using System.Linq;
using UnityEngine;

namespace HBP.UI
{
    [CreateAssetMenu(menuName = "Tools/Windows/Referencer", fileName = "Referencer")]
    public class WindowsReferencer : ScriptableObject
    {
        public Link[] Windows;

        public GameObject GetPrefab(Type type)
        {
            return Windows.FirstOrDefault((link) => link.Type == type.Name).Prefab;
        }
    }

    [Serializable]
    public struct Link
    {
        public string Type;
        public GameObject Prefab;
    }
}