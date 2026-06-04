using System.Collections.ObjectModel;
using UnityEngine;
using UnityEditor;
using HBP.Data.Database;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using HBP.Core.Data;
using TMPro;

public class DatabaseDebugger : MonoBehaviour
{
}

#if UNITY_EDITOR
[CustomEditor(typeof(DatabaseDebugger))]
public class DatabaseDebuggerEditor : Editor
{
    private Dictionary<string, bool> foldouts = new();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!DatabaseManager.IsInitialized)
        {
            EditorGUILayout.HelpBox("Database is not initialized.", MessageType.Warning);
            return;
        }

        GlobalDatabase database = DatabaseManager.Database;

        EditorGUILayout.LabelField("Database Contents", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Is Loaded", database.IsLoaded.ToString());

        DrawFoldoutList("Patients", "Patients", database.Patients.ToList());
        DrawFoldoutList("Data", "Data", database.DataInfos.ToList());
    }

    private void DrawFoldoutList(string label, string foldoutID, IEnumerable<BaseData> ienumerable)
    {
        var list = ienumerable.ToList();

        if (!foldouts.ContainsKey(foldoutID))
        {
            foldouts[foldoutID] = false;
        }

        foldouts[foldoutID] = EditorGUILayout.Foldout(foldouts[foldoutID], label, true);
        if (foldouts[foldoutID])
        {
            EditorGUI.indentLevel++;
            if (list == null || list.Count == 0)
            {
                EditorGUILayout.LabelField("None");
            }
            else
            {
                foreach (var data in list)
                {
                    DrawFoldoutRecursive(data.ID, foldoutID + data.ID, data);
                }
            }
            EditorGUI.indentLevel--;
        }
    }

    private void DrawFoldoutRecursive(string label, string foldoutID, BaseData obj)
    {
        if (obj == null) return;

        if (!foldouts.ContainsKey(foldoutID))
        {
            foldouts[foldoutID] = false;
        }

        foldouts[foldoutID] = EditorGUILayout.Foldout(foldouts[foldoutID], label, true);
        if (foldouts[foldoutID])
        {
            EditorGUI.indentLevel++;
            Type objType = obj.GetType();
            foreach (PropertyInfo property in objType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                object fieldValue = property.GetValue(obj);
                if (fieldValue is IEnumerable<BaseData> ienumerable)
                {
                    DrawFoldoutList(property.Name, foldoutID + property.Name, ienumerable);
                }
                else if (fieldValue != null && fieldValue is BaseData data)
                {
                    DrawFoldoutRecursive(data.ID, foldoutID + data.ID, data);
                }
                else
                {
                    EditorGUILayout.LabelField(property.Name, fieldValue?.ToString() ?? "null");
                }
            }
            EditorGUI.indentLevel--;
        }
    }
}
#endif
