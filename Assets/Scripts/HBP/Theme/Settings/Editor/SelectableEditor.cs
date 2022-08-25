using UnityEditor;

namespace HBP.Theme
{
    [CustomEditor(typeof(Selectable))]
    public class SelectableEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SerializedProperty transitionProperty = serializedObject.FindProperty("Transition");
            EditorGUILayout.PropertyField(transitionProperty);
            UnityEngine.UI.Selectable.Transition transition = (UnityEngine.UI.Selectable.Transition)transitionProperty.enumValueIndex;
            switch (transition)
            {
                case UnityEngine.UI.Selectable.Transition.ColorTint:
                    SerializedProperty colorBlockTransitionProperty = serializedObject.FindProperty("Colors");
                    EditorGUILayout.PropertyField(colorBlockTransitionProperty,true);
                    break;
                case UnityEngine.UI.Selectable.Transition.SpriteSwap:
                    SerializedProperty spriteStateProerty = serializedObject.FindProperty("SpriteState");
                    EditorGUILayout.PropertyField(spriteStateProerty);
                    break;
                case UnityEngine.UI.Selectable.Transition.Animation:
                    SerializedProperty animationTriggersProperty = serializedObject.FindProperty("AnimationTriggers");
                    EditorGUILayout.PropertyField(animationTriggersProperty);
                    break;
                default:
                    break;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}