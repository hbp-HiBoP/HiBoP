using UnityEngine;
using UnityEditor;
using System.Linq;

namespace HBP.UI.Theme
{
    public class ThemeWindow : EditorWindow
    {
        #region Properties
        static Theme m_SelectedTheme;
        static Theme[] m_Themes;
        static string[] m_ThemeNames;
        #endregion

        [MenuItem("Tools/Theme...")]
        static void Init()
        {
            ThemeWindow window = (ThemeWindow)GetWindow(typeof(ThemeWindow));
            m_Themes = Resources.LoadAll<Theme>("Themes");
            m_ThemeNames = (from theme in m_Themes select theme.name).ToArray();
            window.Show();
        }

        void OnGUI()
        {
            int selectedTheme = System.Array.IndexOf(m_Themes, m_SelectedTheme);
            if (selectedTheme == -1) selectedTheme = 0;
            EditorGUILayout.Space();
            selectedTheme = EditorGUILayout.Popup("Themes", selectedTheme, m_ThemeNames);
            m_SelectedTheme = m_Themes[selectedTheme];
            if (GUILayout.Button("Set"))
            {
                SetTheme(m_SelectedTheme);
            }
        }

        [MenuItem("Tools/Apply new theme")]
        public static void SetTheme()
        {
            NewTheme.Components.ThemeElement[] ThemeElements = Resources.FindObjectsOfTypeAll<NewTheme.Components.ThemeElement>();
            foreach (var element in ThemeElements)
            {
                element.Set();
            }
        }

        public static void SetTheme(Theme theme)
        {
            if(Application.isPlaying) ApplicationState.GeneralSettings.ThemeName = theme.name;
            Theme.UpdateThemeElements(theme);
        }

        [MenuItem("Assets/Initialize Theme", false, 100)]
        public static void InitializeTheme()
        {
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            Theme theme = (Theme)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Theme));
            theme.SetDefaultValues();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Initialize Theme", true, 100)]
        public static bool CanInitializeTheme()
        {
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
            return asset is Theme;
        }
    }
}