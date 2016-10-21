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
            myTarget.FunctionsToCallOn.GameObject = EditorGUILayout.ObjectField(myTarget.FunctionsToCallOn.GameObject, typeof(GameObject), true) as GameObject;
            if (myTarget.FunctionsToCallOn.GameObject != null)
            {
                // Components
                Component[] l_components = myTarget.FunctionsToCallOn.GameObject.GetComponents<Component>();
                string[] l_componentsLabels = new string[l_components.Length];
                for (int i = 0; i < l_components.Length; i++)
                {
                    l_componentsLabels[i] = l_components[i].GetType().Name;
                }
                myTarget.FunctionsToCallOn.IndexComponent = EditorGUILayout.Popup(myTarget.FunctionsToCallOn.IndexComponent, l_componentsLabels);

                // Methods
                MethodInfo[] l_methodInfos = myTarget.FunctionsToCallOn.Component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public ).Where(x => x.GetParameters().Length == 0).ToArray();
                string[] l_methodInfosLabels = new string[l_methodInfos.Length];
                for (int i = 0; i < l_methodInfos.Length; i++)
                {
                    l_methodInfosLabels[i] = l_methodInfos[i].Name + " ()";
                }
                myTarget.FunctionsToCallOn.IndexMethod = EditorGUILayout.Popup(myTarget.FunctionsToCallOn.IndexMethod, l_methodInfosLabels);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("OnOff()");
            myTarget.FunctionsToCallOff.GameObject = EditorGUILayout.ObjectField(myTarget.FunctionsToCallOff.GameObject, typeof(GameObject), true) as GameObject;
            if (myTarget.FunctionsToCallOff.GameObject != null)
            {
                // Components
                Component[] l_components = myTarget.FunctionsToCallOff.GameObject.GetComponents<Component>();
                string[] l_componentsLabels = new string[l_components.Length];
                for (int i = 0; i < l_components.Length; i++)
                {
                    l_componentsLabels[i] = l_components[i].GetType().Name;
                }
                myTarget.FunctionsToCallOff.IndexComponent = EditorGUILayout.Popup(myTarget.FunctionsToCallOff.IndexComponent, l_componentsLabels);

                // Methods
                MethodInfo[] l_methodInfos = myTarget.FunctionsToCallOff.Component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(x => x.GetParameters().Length == 0).ToArray();
                string[] l_methodInfosLabels = new string[l_methodInfos.Length];
                for (int i = 0; i < l_methodInfos.Length; i++)
                {
                    l_methodInfosLabels[i] = l_methodInfos[i].Name + " ()";
                }
                myTarget.FunctionsToCallOff.IndexMethod = EditorGUILayout.Popup(myTarget.FunctionsToCallOff.IndexMethod, l_methodInfosLabels);
            }
            EditorGUILayout.EndHorizontal();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(myTarget);
            }

        }
    }
}
