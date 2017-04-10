using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;

namespace Candlelight.Examples
{
	[CustomEditor(typeof(ArrayPropertySetterExample)), CanEditMultipleObjects]
	public class ArrayPropertySetterExampleEditor : Editor
	{
		private ReorderableList array;
		private ReorderableList list;
		private ReorderableList anotherList;

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
			array = new ReorderableList(serializedObject, serializedObject.FindProperty("m_ArrayProperty"));
			array.drawHeaderCallback = position => EditorGUI.LabelField(position, "Array Property");
			array.drawElementCallback =
				(position, index, isActive, isFocused) => DrawElementCallback(position, array, index);
			array.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			list = new ReorderableList(serializedObject, serializedObject.FindProperty("m_ListProperty"));
			list.drawHeaderCallback = position => EditorGUI.LabelField(position, "List Property");
			list.drawElementCallback =
				(position, index, isActive, isFocused) => DrawElementCallback(position, list, index);
			list.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			anotherList = new ReorderableList(serializedObject, serializedObject.FindProperty("m_AnotherListProperty"));
			anotherList.drawHeaderCallback = position => EditorGUI.LabelField(position, "Another List Property");
			anotherList.drawElementCallback =
				(position, index, isActive, isFocused) => DrawElementCallback(position, anotherList, index);
			anotherList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			array.DoLayoutList();
			list.DoLayoutList();
			anotherList.DoLayoutList();
			serializedObject.ApplyModifiedProperties();
		}
	}
}