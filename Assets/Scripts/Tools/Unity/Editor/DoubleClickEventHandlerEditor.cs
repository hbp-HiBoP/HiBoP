using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;

namespace Tools.Unity
{
	[CustomEditor(typeof(DoubleClickEventHandler))]
	public class DoubleClickEventHandlerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
            DoubleClickEventHandler myTarget = (DoubleClickEventHandler)target;
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("OnSimpleClick()");
			myTarget.functionsToCallOnSimpleClick.GameObject = EditorGUILayout.ObjectField(myTarget.functionsToCallOnSimpleClick.GameObject, typeof(GameObject), true) as GameObject;
            if (myTarget.functionsToCallOnSimpleClick.GameObject != null)
            {
                // Components
                Component[] l_components = myTarget.functionsToCallOnSimpleClick.GameObject.GetComponents<Component>();
                string[] l_componentsLabels = new string[l_components.Length];
                for (int i = 0; i < l_components.Length; i++)
                {
                    l_componentsLabels[i] = l_components[i].GetType().Name;
                }
                myTarget.functionsToCallOnSimpleClick.IndexComponent = EditorGUILayout.Popup(myTarget.functionsToCallOnSimpleClick.IndexComponent, l_componentsLabels);

                // Methods
                MethodInfo[] l_methodInfos = myTarget.functionsToCallOnSimpleClick.Component.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public).Where(x => x.GetParameters().Length == 0).ToArray();
                string[] l_methodInfosLabels = new string[l_methodInfos.Length];
                for (int i = 0; i < l_methodInfos.Length; i++)
                {
                    l_methodInfosLabels[i] = l_methodInfos[i].Name + " ()";
                }
                myTarget.functionsToCallOnSimpleClick.IndexMethod = EditorGUILayout.Popup(myTarget.functionsToCallOnSimpleClick.IndexMethod, l_methodInfosLabels);
			}
			EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("OnDoubleClick()");
            myTarget.functionsToCallOnDoubleClick.GameObject = EditorGUILayout.ObjectField(myTarget.functionsToCallOnDoubleClick.GameObject, typeof(GameObject), true) as GameObject;
            if (myTarget.functionsToCallOnDoubleClick.GameObject != null)
            {
                // Components
                Component[] l_components = myTarget.functionsToCallOnDoubleClick.GameObject.GetComponents<Component>();
                string[] l_componentsLabels = new string[l_components.Length];
                for (int i = 0; i < l_components.Length; i++)
                {
                    l_componentsLabels[i] = l_components[i].GetType().Name;
                }
                myTarget.functionsToCallOnDoubleClick.IndexComponent = EditorGUILayout.Popup(myTarget.functionsToCallOnDoubleClick.IndexComponent, l_componentsLabels);

                // Methods
                MethodInfo[] l_methodInfos = myTarget.functionsToCallOnDoubleClick.Component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(x => x.GetParameters().Length == 0).ToArray();
                string[] l_methodInfosLabels = new string[l_methodInfos.Length];
                for (int i = 0; i < l_methodInfos.Length; i++)
                {
                    l_methodInfosLabels[i] = l_methodInfos[i].Name + " ()";
                }
                myTarget.functionsToCallOnDoubleClick.IndexMethod = EditorGUILayout.Popup(myTarget.functionsToCallOnDoubleClick.IndexMethod, l_methodInfosLabels);
            }
            EditorGUILayout.EndHorizontal();

            myTarget.delayBetweenClick=EditorGUILayout.FloatField(myTarget.delayBetweenClick);
			if(GUI.changed)
			{
				EditorUtility.SetDirty(myTarget);
			}
			
		}
		
	}
}