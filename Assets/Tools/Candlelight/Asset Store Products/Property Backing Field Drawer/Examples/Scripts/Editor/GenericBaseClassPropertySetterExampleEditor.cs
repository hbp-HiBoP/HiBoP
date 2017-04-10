using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;

namespace Candlelight.Examples
{
	public class GenericBaseClassPropertySetterExampleEditor : Editor
	{
		private SerializedProperty single;
		private ReorderableList array;

		private void DrawElementCallback(Rect position, ReorderableList reorderableList, int index)
		{
			if (index < reorderableList.serializedProperty.arraySize)
			{
				position.height = EditorGUIUtility.singleLineHeight;
				EditorGUI.PropertyField(position, reorderableList.serializedProperty.GetArrayElementAtIndex(index));
			}
		}

		void OnEnable()
		{
			single = serializedObject.FindProperty("m_Single");
			array = new ReorderableList(serializedObject, serializedObject.FindProperty("m_Array"));
			array.drawHeaderCallback = position => EditorGUI.LabelField(position, "Array");
			array.drawElementCallback =
				(position, index, isActive, isFocused) => DrawElementCallback(position, array, index);
			array.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			EditorGUILayout.PropertyField(single);
			array.DoLayoutList();
			serializedObject.ApplyModifiedProperties();
		}
	}
}