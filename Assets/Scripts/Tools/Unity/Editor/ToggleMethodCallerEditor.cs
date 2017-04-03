using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;

namespace Tools.Unity
{
    [CustomEditor(typeof(ToggleMethodCaller))]
    public class ToggleMethodCallerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ToggleMethodCaller myTarget = (ToggleMethodCaller)target;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("OnOn()");
            myTarget.OnMethod.GameObject = EditorGUILayout.ObjectField(myTarget.OnMethod.GameObject, typeof(GameObject), true) as GameObject;
            if (myTarget.OnMethod.GameObject != null)
            {
                // Components
                Component[] l_components = myTarget.OnMethod.GameObject.GetComponents<Component>();
                string[] l_componentsLabels = new string[l_components.Length];
                for (int i = 0; i < l_components.Length; i++)
                {
                    l_componentsLabels[i] = l_components[i].GetType().Name;
                }
                myTarget.OnMethod.IndexComponent = EditorGUILayout.Popup(myTarget.OnMethod.IndexComponent, l_componentsLabels);

                // Methods
                MethodInfo[] l_methodInfos = myTarget.OnMethod.Component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public ).Where(x => x.GetParameters().Length == 0).ToArray();
                string[] l_methodInfosLabels = new string[l_methodInfos.Length];
                for (int i = 0; i < l_methodInfos.Length; i++)
                {
                    l_methodInfosLabels[i] = l_methodInfos[i].Name + " ()";
                }
                myTarget.OnMethod.IndexMethod = EditorGUILayout.Popup(myTarget.OnMethod.IndexMethod, l_methodInfosLabels);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("OnOff()");
            myTarget.OffMethod.GameObject = EditorGUILayout.ObjectField(myTarget.OffMethod.GameObject, typeof(GameObject), true) as GameObject;
            if (myTarget.OffMethod.GameObject != null)
            {
                // Components
                Component[] l_components = myTarget.OffMethod.GameObject.GetComponents<Component>();
                string[] l_componentsLabels = new string[l_components.Length];
                for (int i = 0; i < l_components.Length; i++)
                {
                    l_componentsLabels[i] = l_components[i].GetType().Name;
                }
                myTarget.OffMethod.IndexComponent = EditorGUILayout.Popup(myTarget.OffMethod.IndexComponent, l_componentsLabels);

                // Methods
                MethodInfo[] l_methodInfos = myTarget.OffMethod.Component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(x => x.GetParameters().Length == 0).ToArray();
                string[] l_methodInfosLabels = new string[l_methodInfos.Length];
                for (int i = 0; i < l_methodInfos.Length; i++)
                {
                    l_methodInfosLabels[i] = l_methodInfos[i].Name + " ()";
                }
                myTarget.OffMethod.IndexMethod = EditorGUILayout.Popup(myTarget.OffMethod.IndexMethod, l_methodInfosLabels);
            }
            EditorGUILayout.EndHorizontal();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(myTarget);
            }

        }
    }
}
