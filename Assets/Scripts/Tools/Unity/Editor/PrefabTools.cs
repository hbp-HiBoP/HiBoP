using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PrefabTools : EditorWindow
{
    string m_Search;
    Transform m_Transform;

    [MenuItem("Tools/Prefab...")]
    static void OpenWindow()
    {
        GetWindow(typeof(PrefabTools));
    }

     void OnGUI()
    {
        m_Search = EditorGUILayout.TextField("Search",m_Search);
        m_Transform = EditorGUILayout.ObjectField("Parent", m_Transform, typeof(Transform), true) as Transform;
        if(GUILayout.Button("Instantiate"))
        {
            InstantiatePrefabByName(m_Search,m_Transform);
        }
    }

    void InstantiatePrefabByName(string name, Transform parent)
    {
        IEnumerable<string> guids = AssetDatabase.FindAssets("t:Prefab " + name);
        IEnumerable<string> paths = guids.Select((guid) => AssetDatabase.GUIDToAssetPath(guid));
        IEnumerable<GameObject> prefabs = paths.Select((path) => AssetDatabase.LoadAssetAtPath<GameObject>(path));
        foreach (var prefab in prefabs)
        {
            GameObject obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            obj.transform.SetParent(parent);
        }
    }
}
