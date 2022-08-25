using UnityEditor;
using UnityEngine;

namespace HBP.UI.Informations.Graphs
{
    [CustomEditor(typeof(Graph))]
    public class GraphEditor : Editor
    {
        #region Properties
        // General
        SerializedProperty m_Title;
        SerializedProperty m_FontColor;
        SerializedProperty m_BackgroundColor;
        SerializedProperty m_UseDefaultDisplayRange;

        // Abscissa
        SerializedProperty m_AbscissaUnit;
        SerializedProperty m_AbscissaLabel;
        SerializedProperty m_AbscissaDisplayRange;
        SerializedProperty m_DefaultAbscissaDisplayRange;
        SerializedProperty m_DisplayCurrentTime;
        SerializedProperty m_CurrentTime;


        // Ordinate
        SerializedProperty m_OrdinateUnit;
        SerializedProperty m_OrdinateLabel;
        SerializedProperty m_OrdinateDisplayRange;
        SerializedProperty m_DefaultOrdinateDisplayRange;

        // Curves
        SerializedProperty m_Curves;

        // Eventss
        bool m_ShowEvents = false;
        SerializedProperty m_OnChangeTitle;
        SerializedProperty m_OnChangeAbscissaUnit;
        SerializedProperty m_OnChangeAbscissaLabel;
        SerializedProperty m_OnChangeOrdinateUnit;
        SerializedProperty m_OnChangeOrdinateLabel;
        SerializedProperty m_OnChangeFontColor;
        SerializedProperty m_OnChangeBackgroundColor;
        SerializedProperty m_OnChangeOrdinateDisplayRange;
        SerializedProperty m_OnChangeAbscissaDisplayRange;
        SerializedProperty m_OnChangeUseDefaultRange;
        SerializedProperty m_OnChangeDisplayCurrentTime;
        SerializedProperty m_OnChangeCurrentTime;
        SerializedProperty m_OnChangeCurves;
        #endregion

        #region Public Methods
        public void OnEnable()
        {
            // General
            m_Title = serializedObject.FindProperty("m_Title");
            m_AbscissaUnit = serializedObject.FindProperty("m_AbscissaUnit");
            m_AbscissaLabel = serializedObject.FindProperty("m_AbscissaLabel");
            m_OrdinateUnit = serializedObject.FindProperty("m_OrdinateUnit");
            m_OrdinateLabel = serializedObject.FindProperty("m_OrdinateLabel");
            m_FontColor = serializedObject.FindProperty("m_FontColor");
            m_BackgroundColor = serializedObject.FindProperty("m_BackgroundColor");
            m_OrdinateDisplayRange = serializedObject.FindProperty("m_OrdinateDisplayRange");
            m_AbscissaDisplayRange = serializedObject.FindProperty("m_AbscissaDisplayRange");
            m_DefaultOrdinateDisplayRange = serializedObject.FindProperty("m_DefaultOrdinateDisplayRange");
            m_DefaultAbscissaDisplayRange = serializedObject.FindProperty("m_DefaultAbscissaDisplayRange");
            m_UseDefaultDisplayRange = serializedObject.FindProperty("m_UseDefaultDisplayRange");
            m_DisplayCurrentTime = serializedObject.FindProperty("m_DisplayCurrentTime");
            m_CurrentTime = serializedObject.FindProperty("m_CurrentTime");
            m_Curves = serializedObject.FindProperty("m_Curves");

            // Events
            m_OnChangeTitle = serializedObject.FindProperty("m_OnChangeTitle");
            m_OnChangeAbscissaUnit = serializedObject.FindProperty("m_OnChangeAbscissaUnit");
            m_OnChangeAbscissaLabel = serializedObject.FindProperty("m_OnChangeAbscissaLabel");
            m_OnChangeOrdinateUnit = serializedObject.FindProperty("m_OnChangeOrdinateUnit");
            m_OnChangeOrdinateLabel = serializedObject.FindProperty("m_OnChangeOrdinateLabel");
            m_OnChangeFontColor = serializedObject.FindProperty("m_OnChangeFontColor");
            m_OnChangeBackgroundColor = serializedObject.FindProperty("m_OnChangeBackgroundColor");
            m_OnChangeAbscissaDisplayRange = serializedObject.FindProperty("m_OnChangeAbscissaDisplayRange");
            m_OnChangeOrdinateDisplayRange = serializedObject.FindProperty("m_OnChangeOrdinateDisplayRange");
            m_OnChangeUseDefaultRange = serializedObject.FindProperty("m_OnChangeUseDefaultRange");
            m_OnChangeDisplayCurrentTime = serializedObject.FindProperty("m_OnChangeDisplayCurrentTime");
            m_OnChangeCurrentTime = serializedObject.FindProperty("m_OnChangeCurrentTime");
            m_OnChangeCurves = serializedObject.FindProperty("m_OnChangeCurves");
        }
        public override void OnInspectorGUI()
        {
            // General
            EditorGUILayout.PrefixLabel(new GUIContent("General"));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_Title);
            EditorGUILayout.PropertyField(m_FontColor);
            EditorGUILayout.PropertyField(m_BackgroundColor);
            EditorGUILayout.PropertyField(m_UseDefaultDisplayRange);
            EditorGUILayout.PropertyField(m_DisplayCurrentTime);
            EditorGUILayout.PropertyField(m_CurrentTime);
            EditorGUI.indentLevel--;

            EditorGUILayout.PrefixLabel("Abscissa");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_AbscissaLabel, new GUIContent("Label"));
            EditorGUILayout.PropertyField(m_AbscissaUnit, new GUIContent("Unit"));
            EditorGUILayout.PropertyField(m_AbscissaDisplayRange, new GUIContent("Range"));
            EditorGUILayout.PropertyField(m_DefaultAbscissaDisplayRange, new GUIContent("Default Range"));
            EditorGUI.indentLevel--;

            EditorGUILayout.PrefixLabel("Ordinate");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_OrdinateLabel, new GUIContent("Label"));
            EditorGUILayout.PropertyField(m_OrdinateUnit, new GUIContent("Unit"));
            EditorGUILayout.PropertyField(m_OrdinateDisplayRange, new GUIContent("Range"));
            EditorGUILayout.PropertyField(m_DefaultOrdinateDisplayRange, new GUIContent("Default Range"));
            EditorGUI.indentLevel--;
        
            EditorGUILayout.PropertyField(m_Curves, true);

            // Events
            m_ShowEvents = EditorGUILayout.Foldout(m_ShowEvents, "Events");
            if(m_ShowEvents)
            {
                EditorGUILayout.PropertyField(m_OnChangeTitle);
                EditorGUILayout.PropertyField(m_OnChangeAbscissaUnit);
                EditorGUILayout.PropertyField(m_OnChangeAbscissaLabel);
                EditorGUILayout.PropertyField(m_OnChangeOrdinateUnit);
                EditorGUILayout.PropertyField(m_OnChangeOrdinateLabel);
                EditorGUILayout.PropertyField(m_OnChangeFontColor);
                EditorGUILayout.PropertyField(m_OnChangeBackgroundColor);
                EditorGUILayout.PropertyField(m_OnChangeAbscissaDisplayRange);
                EditorGUILayout.PropertyField(m_OnChangeOrdinateDisplayRange);
                EditorGUILayout.PropertyField(m_OnChangeUseDefaultRange);
                EditorGUILayout.PropertyField(m_OnChangeDisplayCurrentTime);
                EditorGUILayout.PropertyField(m_OnChangeCurrentTime);
                EditorGUILayout.PropertyField(m_OnChangeCurves);
            }

            serializedObject.ApplyModifiedProperties();
        }
        #endregion
    }

    //[CustomPropertyDrawer(typeof(CurveGroup))]
    //public class CurveGroupEditor : PropertyDrawer
    //{
    //    #region Properties
    //    // General
    //    SerializedProperty m_Name;
    //    SerializedProperty m_Enabled;
    //    SerializedProperty m_Curves;

    //    // Events
    //    bool m_ShowEvents = false;
    //    SerializedProperty m_OnChangeEnabledValue;
    //    SerializedProperty m_OnAddCurve;
    //    SerializedProperty m_OnRemoveCurve;
    //    #endregion

    //    #region Public Methods
    //    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    //    {
    //        float singleLineHeight = EditorGUIUtility.singleLineHeight;
    //        return 3 * singleLineHeight;
    //    }
    //    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //    {
    //        EditorGUI.BeginProperty(position, label, property);
    //        EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Group"));
    //        float singleLineHeight = EditorGUIUtility.singleLineHeight;

    //        // Calculate rects
    //        Rect nameRect = new Rect(position.x, position.y + singleLineHeight, position.width, singleLineHeight);
    //        Rect enabledRect = new Rect(position.x, nameRect.y + singleLineHeight, position.width, singleLineHeight);
    //        //Rect nameRect = new Rect(position.x + 90, position.y, position.width - 90, position.height);

    //        m_Name = property.FindPropertyRelative("m_Name");
    //        EditorGUI.PropertyField(nameRect, m_Name);
    //        m_Enabled = property.FindPropertyRelative("m_Enabled");
    //        EditorGUI.PropertyField(enabledRect, m_Enabled);

    //        EditorGUI.EndProperty();

    //    }
    //    //public void OnEnable()
    //    //{
    //    //    // General
    //    //    m_Name = serializedObject.FindProperty("m_Name");
    //    //    m_Enabled = serializedObject.FindProperty("m_Enabled");
    //    //    m_Curves = serializedObject.FindProperty("m_Curves");

    //    //    // Events
    //    //    m_OnChangeEnabledValue = serializedObject.FindProperty("m_OnChangeEnabledValue");
    //    //    m_OnAddCurve = serializedObject.FindProperty("m_OnAddCurve");
    //    //    m_OnRemoveCurve = serializedObject.FindProperty("m_OnRemoveCurve");
    //    //}
    //    //public override void OnInspectorGUI()
    //    //{
    //    //    // General
    //    //    EditorGUILayout.PrefixLabel(new GUIContent("General"));
    //    //    EditorGUI.indentLevel++;
    //    //    EditorGUILayout.PropertyField(m_Name);
    //    //    EditorGUILayout.PropertyField(m_Enabled);
    //    //    EditorGUILayout.PropertyField(m_Curves);
    //    //    EditorGUI.indentLevel--;

    //    //    // Events
    //    //    m_ShowEvents = EditorGUILayout.Foldout(m_ShowEvents, "Events");
    //    //    if (m_ShowEvents)
    //    //    {
    //    //        EditorGUILayout.PropertyField(m_OnChangeEnabledValue);
    //    //        EditorGUILayout.PropertyField(m_OnAddCurve);
    //    //        EditorGUILayout.PropertyField(m_OnRemoveCurve);
    //    //    }
    //    //    serializedObject.ApplyModifiedProperties();
    //    //}
    //    #endregion
    //}
}