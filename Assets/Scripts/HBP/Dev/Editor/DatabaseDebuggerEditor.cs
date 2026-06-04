using HBP.Core.Data;
using HBP.Data.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

[CustomEditor(typeof(DatabaseDebugger))]
public class DatabaseDebuggerEditor : Editor
{
    private readonly Dictionary<string, bool> m_Foldouts = new();

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

    private void DrawFoldoutList(string label, string foldoutID, IEnumerable<BaseData> enumerable)
    {
        var list = enumerable.ToList();

        if (!m_Foldouts.ContainsKey(foldoutID))
        {
            m_Foldouts[foldoutID] = false;
        }

        m_Foldouts[foldoutID] = EditorGUILayout.Foldout(m_Foldouts[foldoutID], label, true);
        if (!m_Foldouts[foldoutID]) return;

        EditorGUI.indentLevel++;
        if (list.Count == 0)
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

    private void DrawFoldoutRecursive(string label, string foldoutID, BaseData obj)
    {
        if (obj == null) return;

        if (!m_Foldouts.ContainsKey(foldoutID))
        {
            m_Foldouts[foldoutID] = false;
        }

        m_Foldouts[foldoutID] = EditorGUILayout.Foldout(m_Foldouts[foldoutID], label, true);
        if (!m_Foldouts[foldoutID]) return;

        EditorGUI.indentLevel++;
        Type objType = obj.GetType();
        foreach (PropertyInfo property in objType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
        {
            object fieldValue = property.GetValue(obj);
            if (fieldValue is IEnumerable<BaseData> enumerable)
            {
                DrawFoldoutList(property.Name, foldoutID + property.Name, enumerable);
            }
            else if (fieldValue is BaseData data)
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
