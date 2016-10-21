using UnityEngine;
using UnityEditor;

namespace Tools.Unity
{
    [CustomEditor(typeof(ZoneGestion))]
    public class ZoneGestionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ZoneGestion myTarget = (ZoneGestion)target;
            myTarget.Side = (ZoneGestion.SideEnum)EditorGUILayout.EnumPopup("Type", myTarget.Side);
            float l_min = myTarget.Limits.x;
            float l_max = myTarget.Limits.y;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.MinMaxSlider(new GUIContent("Limits"), ref l_min, ref l_max, 0, 1);
            myTarget.Limits = new Vector2(l_min, l_max);
            float l_initialWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 35;
            EditorGUILayout.LabelField("Min :", ((int)(l_min * 100)).ToString() + "%", GUILayout.Width(65));
            EditorGUILayout.LabelField("Max :", ((int)(l_max * 100)).ToString() + "%", GUILayout.Width(65));
            EditorGUIUtility.labelWidth = l_initialWidth;
            EditorGUILayout.EndHorizontal();
            GUIContent l_leftBot = new GUIContent();
            GUIContent l_rightTop = new GUIContent();
            if (myTarget.Side == ZoneGestion.SideEnum.Horizontal)
            {
                l_leftBot.text = "Left";
                l_rightTop.text = "Right";
            }
            else
            {
                l_leftBot.text = "Bot";
                l_rightTop.text = "Top";
            }
            myTarget.LeftBot = (RectTransform)EditorGUILayout.ObjectField(l_leftBot, myTarget.LeftBot, typeof(RectTransform), true);
            myTarget.RightTop = (RectTransform)EditorGUILayout.ObjectField(l_rightTop, myTarget.RightTop, typeof(RectTransform), true);
        }
    }
}
