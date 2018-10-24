using UnityEngine;
using UnityEditor;

namespace Tools.Unity.Components
{
    [CustomEditor(typeof(OldRangeSlider))]
    public class OldRangeSliderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            OldRangeSlider myTarget = (OldRangeSlider)target;

            myTarget.Interactable = EditorGUILayout.Toggle("Interactable", myTarget.Interactable);
            myTarget.Step = EditorGUILayout.FloatField("Step", myTarget.Step);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Limits");
            Vector2 limits;
            limits.x = EditorGUILayout.FloatField(myTarget.Range.x);
            limits.y = EditorGUILayout.FloatField(myTarget.Range.y);
            myTarget.Range = limits;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            float minValue = myTarget.Value.x, maxValue = myTarget.Value.y;
            Debug.Log("Before MinValue: " + minValue + " MaxValue: " + maxValue);

            EditorGUILayout.MinMaxSlider("Value", ref minValue, ref maxValue, myTarget.Range.x, myTarget.Range.y);
            if(myTarget.WholeNumbers)
            {
                minValue = EditorGUILayout.IntField((int)minValue, GUILayout.Width(60));
                maxValue = EditorGUILayout.IntField((int)maxValue, GUILayout.Width(60));
            }
            else
            {
                minValue = EditorGUILayout.FloatField(minValue, GUILayout.Width(60));
                maxValue = EditorGUILayout.FloatField(maxValue, GUILayout.Width(60));
            }
            Debug.Log("After MinValue: " + minValue +" MaxValue: " + maxValue);
            myTarget.Value = new Vector2(minValue, maxValue);
            EditorGUILayout.EndHorizontal();
            myTarget.WholeNumbers = EditorGUILayout.Toggle("Whole Numbers",myTarget.WholeNumbers);
        }
    }
}

