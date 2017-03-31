using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LoadingCircle))]
public class LoadingCircleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LoadingCircle loadingCircle = (LoadingCircle)target;
        loadingCircle.Progress = EditorGUILayout.Slider("Progress",loadingCircle.Progress, 0.0f, 1.0f);
    }
}
