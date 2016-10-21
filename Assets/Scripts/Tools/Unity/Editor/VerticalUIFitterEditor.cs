using UnityEditor;

namespace Tools.Unity
{
    [CustomEditor(typeof(VerticalUIFitter))]
    public class VerticalUIFitterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            VerticalUIFitter myTarget = (VerticalUIFitter)target;

            myTarget.Rotation = (VerticalUIFitter.RotationEnum)EditorGUILayout.EnumPopup("Rotation", myTarget.Rotation);
        }
    }
}