using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    [CustomEditor(typeof(DLLDebugManager))]
    public class DLLDebugManagerEditor : Editor
    {
        private bool m_DLLObjectsPanelOpen = false;
        private DLLDebugManager.DLLObject m_LastClickedObject = null;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            m_DLLObjectsPanelOpen = EditorGUILayout.Foldout(m_DLLObjectsPanelOpen, "DLL Objects");
            if (m_DLLObjectsPanelOpen)
            {
                DLLDebugManager manager = (DLLDebugManager)target;
                var list = manager.DLLObjects.OrderBy(t => t.Type).ThenBy(t => t.CleanedBy).ToList();
                GUIStyle normalStyle = new GUIStyle(EditorStyles.textField);
                GUIStyle cleanedByGCStyle = new GUIStyle(EditorStyles.textField);
                cleanedByGCStyle.normal.textColor = new Color(1, 0, 0);
                GUIStyle cleanedByDisposeStyle = new GUIStyle(EditorStyles.textField);
                cleanedByDisposeStyle.normal.textColor = new Color(0, 0, 1);
                EditorGUILayout.BeginVertical();
                foreach (var item in list)
                {
                    GUIStyle styleToUse;
                    switch (item.CleanedBy)
                    {
                        case DLLDebugManager.CleanedBy.NotCleaned:
                            styleToUse = normalStyle;
                            break;
                        case DLLDebugManager.CleanedBy.GC:
                            styleToUse = cleanedByGCStyle;
                            break;
                        case DLLDebugManager.CleanedBy.Dispose:
                            styleToUse = cleanedByDisposeStyle;
                            break;
                        default:
                            styleToUse = normalStyle;
                            break;
                    }
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(item.Type, styleToUse);
                    GUILayout.Label(item.ID.ToString(), styleToUse, GUILayout.MaxWidth(100));
                    if (GUILayout.Button(m_LastClickedObject != item ? "StackTrace" : "", GUILayout.Width(80)))
                    {
                        m_LastClickedObject = item;
                        Debug.Log(item.StackTrace);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            if(GUILayout.Button("GC Collect"))
            {
                System.GC.Collect();
            }
        }
    }
}