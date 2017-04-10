using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;

namespace Candlelight.Examples
{
	[CustomPropertyDrawer(typeof(CustomClassPropertySetterExample.TwoLineReorderableListElement))]
	public class TwoLineReorderableListElementDrawer : PropertyDrawer
	{
		public static readonly float height =
			EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;

		public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
		{
			return height;
		}

		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
		{
			position.height = EditorGUIUtility.singleLineHeight;
			property = property.Copy();
			property.NextVisible(true);
			EditorGUI.PropertyField(position, property);
			position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			property.NextVisible(true);
			EditorGUI.PropertyField(position, property);
		}
	}

	[CustomEditor(typeof(CustomClassPropertySetterExample)), CanEditMultipleObjects]
	public class CustomClassPropertySetterExampleEditor : Editor
	{
		private SerializedProperty characterProperty;
		private ReorderableList characters;
		private SerializedProperty ordinalNameProperty;
		private ReorderableList ordinalNames;

		private void DrawElementCallback(Rect position, ReorderableList reorderableList, int index)
		{
			if (index < reorderableList.serializedProperty.arraySize)
			{
				EditorGUI.PropertyField(position, reorderableList.serializedProperty.GetArrayElementAtIndex(index));
			}
		}

		void OnEnable()
		{
			characterProperty = serializedObject.FindProperty("m_Character");
			characters = new ReorderableList(serializedObject, serializedObject.FindProperty("m_Characters"));
			characters.drawHeaderCallback = position => EditorGUI.LabelField(position, "Characters");
			// Must draw the element property itself; otherwise the array setter won't be called.
			// Use a custom PropertyDrawer when custom class is a ReorderableList element and needs special drawing.
			characters.drawElementCallback =
				(position, index, isActive, isFocused) => DrawElementCallback(position, characters, index);
			characters.elementHeight = TwoLineReorderableListElementDrawer.height;
			ordinalNameProperty = serializedObject.FindProperty("m_OrdinalName");
			ordinalNames = new ReorderableList(serializedObject, serializedObject.FindProperty("m_OrdinalNames"));
			ordinalNames.drawHeaderCallback = position => EditorGUI.LabelField(position, "Ordinal Names");
			ordinalNames.drawElementCallback =
				(position, index, isActive, isFocused) => DrawElementCallback(position, ordinalNames, index);
			ordinalNames.elementHeight = TwoLineReorderableListElementDrawer.height;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			EditorGUILayout.PropertyField(characterProperty);
			EditorGUILayout.PropertyField(ordinalNameProperty);
			characters.DoLayoutList();
			ordinalNames.DoLayoutList();
			serializedObject.ApplyModifiedProperties();
		}
	}
}