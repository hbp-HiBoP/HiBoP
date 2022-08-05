using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Animations;

namespace UnityEngine.UI
{
    [CustomEditor(typeof(RangeSlider))]
    public class RangeSliderEditor : Editor
    {
        #region Properties
        SerializedProperty m_InteractableProperty;
        SerializedProperty m_TypeProperty;

        SerializedProperty m_HandleTransitionProperty;
        SerializedProperty m_MinHandleTargetGraphicProperty;
        SerializedProperty m_MaxHandleTargetGraphicProperty;
        SerializedProperty m_HandleColorsProperty;
        SerializedProperty m_HandleSpriteStateProperty;
        SerializedProperty m_HandleAnimationTriggersProperty;

        SerializedProperty m_FillTransitionProperty;
        SerializedProperty m_FillTargetGraphicProperty;
        SerializedProperty m_FillColorsProperty;
        SerializedProperty m_FillSpriteStateProperty;
        SerializedProperty m_FillAnimationTriggersProperty;

        SerializedProperty m_NavigationProperty;
        SerializedProperty m_FillRectProperty;
        SerializedProperty m_MinHandleRectProperty;
        SerializedProperty m_MaxHandleRectProperty;

        SerializedProperty m_DirectionProperty;
        SerializedProperty m_MinLimitProperty;
        SerializedProperty m_MaxLimitProperty;
        SerializedProperty m_StepProperty;
        SerializedProperty m_MinValueProperty;
        SerializedProperty m_MaxValueProperty;

        SerializedProperty m_OnValueChangedProperty;

        GUIContent m_VisualizeNavigation = new GUIContent("Visualize", "Show navigation flows between selectable UI elements.");

        AnimBool m_HandleShowColorTint = new AnimBool();
        AnimBool m_HandleShowSpriteTrasition = new AnimBool();
        AnimBool m_HandleShowAnimTransition = new AnimBool();

        AnimBool m_FillShowColorTint = new AnimBool();
        AnimBool m_FillShowSpriteTrasition = new AnimBool();
        AnimBool m_FillShowAnimTransition = new AnimBool();

        static List<RangeSliderEditor> s_Editors = new List<RangeSliderEditor>();
        static bool s_ShowNavigation = false;
        static string s_ShowNavigationKey = "SelectableEditor.ShowNavigation";
        const float kArrowThickness = 2.5f;
        const float kArrowHeadSize = 1.2f;
        #endregion

        #region Public Methods
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_InteractableProperty);

            var type = GetSliderType(m_TypeProperty);
            EditorGUILayout.PropertyField(m_TypeProperty);

            EditorGUILayout.PrefixLabel("Handles");
            EditorGUI.indentLevel++;

            var handleTransition = GetTransition(m_HandleTransitionProperty);

            var minHandleGraphic = m_MinHandleTargetGraphicProperty.objectReferenceValue as Graphic;
            var maxHandleGraphic = m_MaxHandleTargetGraphicProperty.objectReferenceValue as Graphic;

            m_HandleShowColorTint.target = (!m_HandleTransitionProperty.hasMultipleDifferentValues && handleTransition == Button.Transition.ColorTint);
            m_HandleShowSpriteTrasition.target = (!m_HandleTransitionProperty.hasMultipleDifferentValues && handleTransition == Button.Transition.SpriteSwap);
            m_HandleShowAnimTransition.target = (!m_HandleTransitionProperty.hasMultipleDifferentValues && handleTransition == Button.Transition.Animation);

            EditorGUILayout.PropertyField(m_HandleTransitionProperty, new GUIContent("Transition"));
            EditorGUI.indentLevel++;

            if(handleTransition == Selectable.Transition.ColorTint || handleTransition == Selectable.Transition.SpriteSwap)
            {
                EditorGUILayout.PropertyField(m_MinHandleTargetGraphicProperty);
                EditorGUILayout.PropertyField(m_MaxHandleTargetGraphicProperty);
            }
            switch (handleTransition)
            {
                case Selectable.Transition.ColorTint:
                    if(minHandleGraphic == null || maxHandleGraphic == null)
                    {
                        EditorGUILayout.HelpBox("You must have both Graphic targets in order to use a color transition.", MessageType.Warning);
                    }
                    break;
                case Selectable.Transition.SpriteSwap:
                    if (minHandleGraphic as Image == null || maxHandleGraphic as Image == null)
                    {
                        EditorGUILayout.HelpBox("You must have both Image targets in order to use a sprite swap transition.", MessageType.Warning);
                    }
                    break;
            }

            if(EditorGUILayout.BeginFadeGroup(m_HandleShowColorTint.faded))
            {
                EditorGUILayout.PropertyField(m_HandleColorsProperty);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFadeGroup();

            if(EditorGUILayout.BeginFadeGroup(m_HandleShowSpriteTrasition.faded))
            {
                EditorGUILayout.PropertyField(m_HandleSpriteStateProperty);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFadeGroup();

            if(EditorGUILayout.BeginFadeGroup(m_HandleShowAnimTransition.faded))
            {
                EditorGUILayout.PropertyField(m_HandleAnimationTriggersProperty);
            }
            EditorGUILayout.EndFadeGroup();
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            if (type == RangeSlider.SliderType.Complete)
            {
                EditorGUILayout.PrefixLabel("Fill");

                var fillTransition = GetTransition(m_FillTransitionProperty);

                var fillGraphic = m_FillTargetGraphicProperty.objectReferenceValue as Graphic;

                m_FillShowColorTint.target = (!m_FillTransitionProperty.hasMultipleDifferentValues && fillTransition == Button.Transition.ColorTint);
                m_FillShowSpriteTrasition.target = (!m_FillTransitionProperty.hasMultipleDifferentValues && fillTransition == Button.Transition.SpriteSwap);
                m_FillShowAnimTransition.target = (!m_FillTransitionProperty.hasMultipleDifferentValues && fillTransition == Button.Transition.Animation);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_FillTransitionProperty, new GUIContent("Transition"));
                EditorGUI.indentLevel++;

                if(fillTransition == Selectable.Transition.ColorTint || fillTransition == Selectable.Transition.SpriteSwap)
                {
                    EditorGUILayout.PropertyField(m_FillTargetGraphicProperty);
                }
                switch (fillTransition)
                {
                    case Selectable.Transition.ColorTint:
                        if (fillGraphic == null)
                            EditorGUILayout.HelpBox("You must have a Graphic target in order to use a color transition.", MessageType.Warning);
                        break;
                    case Selectable.Transition.SpriteSwap:
                        if (fillGraphic as Image == null)
                            EditorGUILayout.HelpBox("You must have a Image target in order to use a sprite swap transition.", MessageType.Warning);
                        break;
                }


                if (EditorGUILayout.BeginFadeGroup(m_FillShowColorTint.faded))
                {
                    EditorGUILayout.PropertyField(m_FillColorsProperty);
                    EditorGUILayout.Space();
                }
                EditorGUILayout.EndFadeGroup();

                if (EditorGUILayout.BeginFadeGroup(m_FillShowSpriteTrasition.faded))
                {
                    EditorGUILayout.PropertyField(m_FillSpriteStateProperty);
                    EditorGUILayout.Space();
                }
                EditorGUILayout.EndFadeGroup();

                if (EditorGUILayout.BeginFadeGroup(m_FillShowAnimTransition.faded))
                {
                    EditorGUILayout.PropertyField(m_FillAnimationTriggersProperty);
                }
                EditorGUILayout.EndFadeGroup();

                EditorGUI.indentLevel--;
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(m_NavigationProperty);
            EditorGUI.BeginChangeCheck();
            Rect toggleRect = EditorGUILayout.GetControlRect();
            toggleRect.xMin += EditorGUIUtility.labelWidth;
            s_ShowNavigation = GUI.Toggle(toggleRect, s_ShowNavigation, m_VisualizeNavigation, EditorStyles.miniButton);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(s_ShowNavigationKey, s_ShowNavigation);
                SceneView.RepaintAll();
            }
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_FillRectProperty);
            EditorGUILayout.PropertyField(m_MinHandleRectProperty);
            EditorGUILayout.PropertyField(m_MaxHandleRectProperty);

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_DirectionProperty);
            if (EditorGUI.EndChangeCheck())
            {
                RangeSlider.Direction dir = (RangeSlider.Direction)m_DirectionProperty.enumValueIndex;
                foreach (var obj in serializedObject.targetObjects)
                {
                    RangeSlider slider = obj as RangeSlider;
                    slider.SetDirection(dir, true);
                }
            }
            EditorGUILayout.PropertyField(m_MinLimitProperty);
            EditorGUILayout.PropertyField(m_MaxLimitProperty);
            EditorGUILayout.PropertyField(m_StepProperty);


            EditorGUILayout.BeginHorizontal();

            float minValueTemp = m_MinValueProperty.floatValue, maxValueTemp = m_MaxValueProperty.floatValue;
            EditorGUILayout.MinMaxSlider("Value", ref minValueTemp, ref maxValueTemp, m_MinLimitProperty.floatValue, m_MaxLimitProperty.floatValue);
            float approMinValueTemp = Mathf.Round(minValueTemp * 1000) / 1000;
            float approMaxValueTemp = Mathf.Round(maxValueTemp * 1000) / 1000;
            minValueTemp = EditorGUILayout.FloatField(approMinValueTemp, GUILayout.Width(48));
            maxValueTemp = EditorGUILayout.FloatField(approMaxValueTemp, GUILayout.Width(48));
            m_MinValueProperty.floatValue = minValueTemp;
            m_MaxValueProperty.floatValue = maxValueTemp;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_OnValueChangedProperty);

            // Apply modified properties.
            serializedObject.ApplyModifiedProperties();
        }
        #endregion

        #region Private Methods
        protected virtual void OnEnable()
        {
            m_InteractableProperty = serializedObject.FindProperty("m_Interactable");
            m_TypeProperty = serializedObject.FindProperty("m_Type");

            m_HandleTransitionProperty = serializedObject.FindProperty("m_HandleTransition");
            m_MinHandleTargetGraphicProperty = serializedObject.FindProperty("m_MinHandleTargetGraphic");
            m_MaxHandleTargetGraphicProperty = serializedObject.FindProperty("m_MaxHandleTargetGraphic");
            m_HandleColorsProperty = serializedObject.FindProperty("m_HandleColors");
            m_HandleSpriteStateProperty = serializedObject.FindProperty("m_HandleSpriteState");
            m_HandleAnimationTriggersProperty = serializedObject.FindProperty("m_HandleAnimationTriggers");

            m_FillTransitionProperty = serializedObject.FindProperty("m_FillTransition");
            m_FillTargetGraphicProperty = serializedObject.FindProperty("m_FillTargetGraphic");
            m_FillColorsProperty = serializedObject.FindProperty("m_FillColors");
            m_FillSpriteStateProperty = serializedObject.FindProperty("m_FillSpriteState");
            m_FillAnimationTriggersProperty = serializedObject.FindProperty("m_FillAnimationTriggers");

            m_NavigationProperty = serializedObject.FindProperty("m_Navigation");

            m_FillRectProperty = serializedObject.FindProperty("m_FillRect");
            m_MinHandleRectProperty = serializedObject.FindProperty("m_MinHandleRect");
            m_MaxHandleRectProperty = serializedObject.FindProperty("m_MaxHandleRect");

            m_DirectionProperty = serializedObject.FindProperty("m_Direction");
            m_MinLimitProperty = serializedObject.FindProperty("m_MinLimit");
            m_MaxLimitProperty = serializedObject.FindProperty("m_MaxLimit");
            m_StepProperty = serializedObject.FindProperty("m_Step");
            m_MinValueProperty = serializedObject.FindProperty("m_MinValue");
            m_MaxValueProperty = serializedObject.FindProperty("m_MaxValue");
            m_OnValueChangedProperty = serializedObject.FindProperty("m_OnValueChanged");

            var handleTransition = GetTransition(m_HandleTransitionProperty);
            m_HandleShowColorTint.value = (handleTransition == Selectable.Transition.ColorTint);
            m_HandleShowSpriteTrasition.value = (handleTransition == Selectable.Transition.SpriteSwap);
            m_HandleShowAnimTransition.value = (handleTransition == Selectable.Transition.Animation);

            m_HandleShowColorTint.valueChanged.AddListener(Repaint);
            m_HandleShowSpriteTrasition.valueChanged.AddListener(Repaint);

            var fillTransition = GetTransition(m_FillTransitionProperty);
            m_FillShowColorTint.value = (fillTransition == Selectable.Transition.ColorTint);
            m_FillShowSpriteTrasition.value = (fillTransition == Selectable.Transition.SpriteSwap);
            m_FillShowAnimTransition.value = (fillTransition == Selectable.Transition.Animation);

            m_FillShowColorTint.valueChanged.AddListener(Repaint);
            m_FillShowSpriteTrasition.valueChanged.AddListener(Repaint);

            s_Editors.Add(this);
            RegisterStaticOnSceneGUI();

            s_ShowNavigation = EditorPrefs.GetBool(s_ShowNavigationKey);
        }
        protected virtual void OnDisable()
        {
            m_HandleShowColorTint.valueChanged.RemoveListener(Repaint);
            m_HandleShowSpriteTrasition.valueChanged.RemoveListener(Repaint);

            m_FillShowColorTint.valueChanged.RemoveListener(Repaint);
            m_FillShowSpriteTrasition.valueChanged.RemoveListener(Repaint);

            s_Editors.Remove(this);
            RegisterStaticOnSceneGUI();
        }
        protected virtual void RegisterStaticOnSceneGUI()
        {
            SceneView.onSceneGUIDelegate -= StaticOnSceneGUI;
            if (s_Editors.Count > 0)
                SceneView.onSceneGUIDelegate += StaticOnSceneGUI;
        }
        protected static Selectable.Transition GetTransition(SerializedProperty transition)
        {
            return (Selectable.Transition) transition.enumValueIndex;
        }
        protected static RangeSlider.SliderType GetSliderType(SerializedProperty type)
        {
            return (RangeSlider.SliderType) type.enumValueIndex;
        }
        private static string GetSaveControllerPath(Selectable target)
        {
            var defaultName = target.gameObject.name;
            var message = string.Format("Create a new animator for the game object '{0}':", defaultName);
            return EditorUtility.SaveFilePanelInProject("New Animation Contoller", defaultName, "controller", message);
        }
        private static void SetUpCurves(AnimationClip highlightedClip, AnimationClip pressedClip, string animationPath)
        {
            string[] channels = { "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" };

            var highlightedKeys = new[] { new Keyframe(0f, 1f), new Keyframe(0.5f, 1.1f), new Keyframe(1f, 1f) };
            var highlightedCurve = new AnimationCurve(highlightedKeys);
            foreach (var channel in channels)
                AnimationUtility.SetEditorCurve(highlightedClip, EditorCurveBinding.FloatCurve(animationPath, typeof(Transform), channel), highlightedCurve);

            var pressedKeys = new[] { new Keyframe(0f, 1.15f) };
            var pressedCurve = new AnimationCurve(pressedKeys);
            foreach (var channel in channels)
                AnimationUtility.SetEditorCurve(pressedClip, EditorCurveBinding.FloatCurve(animationPath, typeof(Transform), channel), pressedCurve);
        }
        private static string BuildAnimationPath(Selectable target)
        {
            // if no target don't hook up any curves.
            var highlight = target.targetGraphic;
            if (highlight == null)
                return string.Empty;

            var startGo = highlight.gameObject;
            var toFindGo = target.gameObject;

            var pathComponents = new Stack<string>();
            while (toFindGo != startGo)
            {
                pathComponents.Push(startGo.name);

                // didn't exist in hierarchy!
                if (startGo.transform.parent == null)
                    return string.Empty;

                startGo = startGo.transform.parent.gameObject;
            }

            // calculate path
            var animPath = new StringBuilder();
            if (pathComponents.Count > 0)
                animPath.Append(pathComponents.Pop());

            while (pathComponents.Count > 0)
                animPath.Append("/").Append(pathComponents.Pop());

            return animPath.ToString();
        }
        private static AnimationClip GenerateTriggerableTransition(string name, AnimatorController controller)
        {
            // Create the clip
            var clip = AnimatorController.AllocateAnimatorClip(name);
            AssetDatabase.AddObjectToAsset(clip, controller);

            // Create a state in the animatior controller for this clip
            var state = controller.AddMotion(clip);

            // Add a transition property
            controller.AddParameter(name, AnimatorControllerParameterType.Trigger);

            // Add an any state transition
            var stateMachine = controller.layers[0].stateMachine;
            var transition = stateMachine.AddAnyStateTransition(state);
            var condition = transition.conditions[0];
            condition.mode = AnimatorConditionMode.If;
            condition.parameter = name;
            return clip;
        }
        private static void StaticOnSceneGUI(SceneView view)
        {
            if (!s_ShowNavigation)
                return;

            for (int i = 0; i < Selectable.allSelectablesArray.Length; i++)
            {
                DrawNavigationForSelectable(Selectable.allSelectablesArray[i]);
            }
        }
        private static void DrawNavigationForSelectable(Selectable sel)
        {
            if (sel == null)
                return;

            Transform transform = sel.transform;
            bool active = Selection.transforms.Any(e => e == transform);
            Handles.color = new Color(1.0f, 0.9f, 0.1f, active ? 1.0f : 0.4f);
            DrawNavigationArrow(-Vector2.right, sel, sel.FindSelectableOnLeft());
            DrawNavigationArrow(Vector2.right, sel, sel.FindSelectableOnRight());
            DrawNavigationArrow(Vector2.up, sel, sel.FindSelectableOnUp());
            DrawNavigationArrow(-Vector2.up, sel, sel.FindSelectableOnDown());
        }
        private static void DrawNavigationArrow(Vector2 direction, Selectable fromObj, Selectable toObj)
        {
            if (fromObj == null || toObj == null)
                return;
            Transform fromTransform = fromObj.transform;
            Transform toTransform = toObj.transform;

            Vector2 sideDir = new Vector2(direction.y, -direction.x);
            Vector3 fromPoint = fromTransform.TransformPoint(GetPointOnRectEdge(fromTransform as RectTransform, direction));
            Vector3 toPoint = toTransform.TransformPoint(GetPointOnRectEdge(toTransform as RectTransform, -direction));
            float fromSize = HandleUtility.GetHandleSize(fromPoint) * 0.05f;
            float toSize = HandleUtility.GetHandleSize(toPoint) * 0.05f;
            fromPoint += fromTransform.TransformDirection(sideDir) * fromSize;
            toPoint += toTransform.TransformDirection(sideDir) * toSize;
            float length = Vector3.Distance(fromPoint, toPoint);
            Vector3 fromTangent = fromTransform.rotation * direction * length * 0.3f;
            Vector3 toTangent = toTransform.rotation * -direction * length * 0.3f;

            Handles.DrawBezier(fromPoint, toPoint, fromPoint + fromTangent, toPoint + toTangent, Handles.color, null, kArrowThickness);
            Handles.DrawAAPolyLine(kArrowThickness, toPoint, toPoint + toTransform.rotation * (-direction - sideDir) * toSize * kArrowHeadSize);
            Handles.DrawAAPolyLine(kArrowThickness, toPoint, toPoint + toTransform.rotation * (-direction + sideDir) * toSize * kArrowHeadSize);
        }
        private static Vector3 GetPointOnRectEdge(RectTransform rect, Vector2 dir)
        {
            if (rect == null)
                return Vector3.zero;
            if (dir != Vector2.zero)
                dir /= Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
            dir = rect.rect.center + Vector2.Scale(rect.rect.size, dir * 0.5f);
            return dir;
        }
        #endregion
    }
}