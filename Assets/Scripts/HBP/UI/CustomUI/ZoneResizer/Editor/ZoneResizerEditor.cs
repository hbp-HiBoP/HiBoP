using UnityEngine;
using UnityEditor;
using HBP.Theme.Components;

namespace HBP.UI
{
    [CustomEditor(typeof(ZoneResizer))]
    public class ZoneResizerEditor : Editor
    {
        ZoneResizer instance;
        public void OnEnable()
        {
            instance = target as ZoneResizer;
        }

        public override void OnInspectorGUI()
        {
            instance.LeftRight = (Theme.State)EditorGUILayout.ObjectField("Left Right State", instance.LeftRight, typeof(Theme.State), true);
            instance.TopBottom = (Theme.State)EditorGUILayout.ObjectField("Top Bottom State", instance.TopBottom, typeof(Theme.State), true);
            instance.ThemeElement = (ThemeElement)EditorGUILayout.ObjectField("Theme Element", instance.ThemeElement, typeof(ThemeElement), true);
            instance.Direction = (ZoneResizer.DirectionType)EditorGUILayout.EnumPopup("Direction", instance.Direction);
            switch (instance.Direction)
            {
                case ZoneResizer.DirectionType.BottomToTop:
                    instance.BotRect = (RectTransform)EditorGUILayout.ObjectField("Bottom Rect", instance.BotRect, typeof(RectTransform), true);
                    instance.TopRect = (RectTransform)EditorGUILayout.ObjectField("Top Rect", instance.TopRect, typeof(RectTransform), true);
                    break;

                case ZoneResizer.DirectionType.LeftToRight:
                    instance.BotRect = (RectTransform)EditorGUILayout.ObjectField("Left Rect", instance.BotRect, typeof(RectTransform), true);
                    instance.TopRect = (RectTransform)EditorGUILayout.ObjectField("Right Rect", instance.TopRect, typeof(RectTransform), true);
                    break;
            }
            instance.HandleRect = (RectTransform)EditorGUILayout.ObjectField("Handle Rect", instance.HandleRect, typeof(RectTransform), true);
            instance.MarginWidth = EditorGUILayout.FloatField("Margin Width", instance.MarginWidth);
            EditorGUILayout.BeginHorizontal();
            float min = instance.Min;
            float max = instance.Max;
            EditorGUILayout.MinMaxSlider("Limits", ref min, ref max, 0.0f, 1.0f);
            instance.Min = min;
            instance.Max = max;
            float l_initialWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 35;
            EditorGUILayout.LabelField(min.ToString("0.00"), GUILayout.Width(30));
            EditorGUILayout.LabelField(max.ToString("0.00"), GUILayout.Width(30));
            EditorGUIUtility.labelWidth = l_initialWidth;
            EditorGUILayout.EndHorizontal();

            instance.Ratio = EditorGUILayout.Slider("Ratio", instance.Ratio, 0.0f, 1.0f);
        }
    }
}