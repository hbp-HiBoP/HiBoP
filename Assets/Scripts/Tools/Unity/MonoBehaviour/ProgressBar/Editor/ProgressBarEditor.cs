using UnityEditor;
using UnityEngine;

namespace Tools.Unity
{
    [CustomEditor(typeof(ProgressBar))]
    public class ProgressBarEditor : Editor
    {
        ProgressBar progressBar;

        void OnEnable()
        {
            progressBar = target as ProgressBar;
            progressBar.Set();
        }

        public override void OnInspectorGUI()
        {
            if(Application.isPlaying)
            {
                 EditorGUILayout.IntSlider(new GUIContent("Percentage"), progressBar.iPercentage, 0, 100);
            }
            else if(Application.isEditor)
            {
                progressBar.iPercentage = EditorGUILayout.IntSlider(new GUIContent("Percentage"), progressBar.iPercentage, 0, 100);
            }
            progressBar.BackGroundColor = EditorGUILayout.ColorField(new GUIContent("BackGround"), progressBar.BackGroundColor);
            progressBar.CheckMarkColor = EditorGUILayout.ColorField(new GUIContent("CheckMark"), progressBar.CheckMarkColor);
            progressBar.TextColor = EditorGUILayout.ColorField(new GUIContent("Text"), progressBar.TextColor);
        }
    }
}