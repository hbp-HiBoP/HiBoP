using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HBP.Module3D.DLL
{
    [CustomEditor(typeof(DLLDebugManager))]
    public class DLLDebugManagerEditor : Editor
    {
        private bool m_DLLObjectsPanelOpen = false;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            m_DLLObjectsPanelOpen = EditorGUILayout.Foldout(m_DLLObjectsPanelOpen, "DLL Objects");
            if (m_DLLObjectsPanelOpen)
            {
                DLLDebugManager manager = (DLLDebugManager)target;
                var list = manager.DLLObjects.OrderBy(t => t.Item1).ThenBy(t => t.Item3).ToList();
                GUIStyle normalStyle = new GUIStyle(EditorStyles.textField);
                GUIStyle cleanedByGCStyle = new GUIStyle(EditorStyles.textField);
                cleanedByGCStyle.normal.textColor = new Color(1, 0, 0);
                GUIStyle cleanedByDisposeStyle = new GUIStyle(EditorStyles.textField);
                cleanedByDisposeStyle.normal.textColor = new Color(0, 0, 1);
                EditorGUILayout.BeginVertical();
                foreach (var item in list)
                {
                    GUIStyle styleToUse;
                    switch (item.Item3)
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
                    EditorGUILayout.LabelField(item.Item1, styleToUse);
                    EditorGUILayout.LabelField(item.Item2.ToString(), styleToUse);
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