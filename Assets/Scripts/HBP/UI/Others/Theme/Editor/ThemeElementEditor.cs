using UnityEditor;
using UnityEngine;

namespace HBP.UI.Theme
{
    [CustomEditor(typeof(ThemeElement))]
    public class ThemeElementEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ThemeElement themeElement = target as ThemeElement;
            themeElement.IgnoreTheme = EditorGUILayout.Toggle("Ignore Theme", themeElement.IgnoreTheme);
            if (!themeElement.IgnoreTheme)
            {
                themeElement.Zone = (ThemeElement.ZoneEnum)EditorGUILayout.EnumPopup("Zone", themeElement.Zone);
                switch (themeElement.Zone)
                {
                    case ThemeElement.ZoneEnum.General:
                        themeElement.General = (ThemeElement.GeneralEnum)EditorGUILayout.EnumPopup("Type", themeElement.General);
                        break;
                    case ThemeElement.ZoneEnum.Menu:
                        themeElement.Menu = (ThemeElement.MenuEnum)EditorGUILayout.EnumPopup("Type", themeElement.Menu);
                        break;
                    case ThemeElement.ZoneEnum.Window:
                        themeElement.Window = (ThemeElement.WindowEnum)EditorGUILayout.EnumPopup("Window", themeElement.Window);
                        switch (themeElement.Window)
                        {
                            case ThemeElement.WindowEnum.Header:
                                themeElement.Header = (ThemeElement.HeaderEnum)EditorGUILayout.EnumPopup("Type", themeElement.Header);
                                break;
                            case ThemeElement.WindowEnum.Content:
                                themeElement.Content = (ThemeElement.ContentEnum)EditorGUILayout.EnumPopup("Type", themeElement.Content);
                                if(themeElement.Content == ThemeElement.ContentEnum.Item)
                                {
                                    themeElement.Item = (ThemeElement.ItemEnum)EditorGUILayout.EnumPopup("Type", themeElement.Item);
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    case ThemeElement.ZoneEnum.Toolbar:
                        themeElement.Toolbar = (ThemeElement.ToolbarEnum)EditorGUILayout.EnumPopup("Type", themeElement.Toolbar);
                        break;
                    case ThemeElement.ZoneEnum.Visualization:
                        themeElement.Visualization = (ThemeElement.VisualizationEnum)EditorGUILayout.EnumPopup("Type", themeElement.Visualization);
                        break;
                    default:
                        break;
                }
                if (GUILayout.Button("Set"))
                {
                    themeElement.Set((Theme) Resources.Load("Themes/Dark"));
                }
            }
        }
    }
}