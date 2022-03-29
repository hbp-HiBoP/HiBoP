using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

public class SelectPrefabsOfType : EditorWindow
{

    [MenuItem("Window/Prefab Finder")]
    static void ShowWindow()
    {
        EditorWindow.GetWindow<SelectPrefabsOfType>(true, "Prefab Finder");
    }

    List<Type> _types = null;
    string[] _typesArray = null;
    int _idx = 0;

    void OnGUI()
    {
        GUILayout.Label("Select Script");
        if (_types == null)
        {
            GetAllTypes();
        }

        _idx = EditorGUILayout.Popup(_idx, _typesArray);

        if (GUILayout.Button("Find all prefabs"))
        {
            ShowItemsOfTypeInProjectHierarchy(_types[_idx]);
        }
    }

    void GetAllTypes()
    {
        _types = new List<Type>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in assemblies)
        {
            var types = asm.GetTypes();
            foreach (var type in types)
            {
                if (type.IsSubclassOf(typeof(MonoBehaviour)))
                {
                    _types.Add(type);
                }
            }
        }
        _types.Sort((a, b) =>
        {
            return a.Name.CompareTo(b.Name);
        });

        _typesArray = new string[_types.Count];
        for (var i = 0; i < _types.Count; i++)
        {
            var type = _types[i];
            if (string.IsNullOrEmpty(type.Namespace))
            {
                _typesArray[i] = type.Name;
            }
            else
            {
                _typesArray[i] = string.Format("{0}.{1}", type.Namespace, type.Name);
            }
        }
    }

    void ShowItemsOfTypeInProjectHierarchy(Type type)
    {
        var guids = AssetDatabase.FindAssets("t:Prefab");
        var toSelect = new List<int>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var toCheck = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in toCheck)
            {
                var go = obj as GameObject;
                if (go == null)
                {
                    continue;
                }

                var comp = go.GetComponent(type);
                if (comp != null)
                {
                    toSelect.Add(go.GetInstanceID());
                }
                else
                {
                    var comps = go.GetComponentsInChildren(type);
                    if (comps.Length > 0)
                    {
                        toSelect.Add(go.GetInstanceID());
                    }
                }
            }
        }

        // clear the current selection
        Selection.instanceIDs = new int[0];
        ShowSelectionInProjectHierarchy();

        // show the prefabs we found
        Selection.instanceIDs = toSelect.ToArray();
        ShowSelectionInProjectHierarchy();
    }

    // use internal classes to update the selection in the project hierarchy.
    // it's dumb that we have to do this.
    void ShowSelectionInProjectHierarchy()
    {
        var pbType = GetType("UnityEditor.ProjectBrowser");
        var meth = pbType.GetMethod("ShowSelectedObjectsInLastInteractedProjectBrowser",
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Static);
        meth.Invoke(null, null);
    }

    // helper method to find a tyep of a given name
    Type GetType(string name)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in assemblies)
        {
            var type = asm.GetType(name);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }
}