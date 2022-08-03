using UnityEditor;

namespace HBP.UI
{
    [CustomEditor(typeof(LoadingCircle))]
    public class LoadingCircleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            LoadingCircle loadingCircle = (LoadingCircle)target;
            loadingCircle.Progress = EditorGUILayout.Slider("Progress", loadingCircle.Progress, 0.0f, 1.0f);
        }
    }
}