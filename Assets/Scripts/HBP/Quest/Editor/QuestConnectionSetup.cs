using System;
using HBP.UI.Quest;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Quest.Editor
{
    /// <summary>Author the UI once in prefabs; runtime never constructs missing UI.</summary>
    public static class QuestConnectionSetup
    {
        public const string DesktopPath = "Assets/Prefabs/General/Quest Connection.prefab";
        private const string MenuPath = "Assets/Prefabs/General/Main menu.prefab";

        [MenuItem("Tools/Quest/Rebuild Connection UI")]
        public static void Apply()
        {
            ApplyDesktop();
            AttachQuest();
            AssetDatabase.SaveAssets();
            Debug.Log("QUEST-011 connection prefabs authored.");
        }

        public static void ApplyDesktop()
        {
            BuildDesktop();
            var menu = PrefabUtility.LoadPrefabContents(MenuPath);
            try
            {
                var old = menu.GetComponentInChildren<DesktopQuestPanel>(true);
                if (old != null) Object.DestroyImmediate(old.gameObject);
                Transform oldButton = menu.transform.Find("Left/Quest");
                if (oldButton != null) Object.DestroyImmediate(oldButton.gameObject);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DesktopPath), menu.transform);
                var button = Button(menu.transform.Find("Left"), "Quest", 0, 0, 70);
                var layout = button.gameObject.AddComponent<LayoutElement>();
                layout.preferredWidth = 70;
                layout.preferredHeight = 20;
                UnityEventTools.AddPersistentListener(button.onClick, instance.GetComponent<DesktopQuestPanel>().TogglePanel);
                PrefabUtility.SaveAsPrefabAsset(menu, MenuPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(menu);
            }

            AssetDatabase.SaveAssets();
        }

        public static void AttachQuest()
        {
            var root = PrefabUtility.LoadPrefabContents(QuestBootstrapSetup.PrefabPath);
            try
            {
                var camera = root.GetComponentInChildren<Camera>();
                Transform previous = camera.transform.Find("Quest Connection Panel");
                if (previous != null) Object.DestroyImmediate(previous.gameObject);
                var diagnosticText = camera.transform.Find("Anatomy Diagnostic Status");
                if (diagnosticText != null) diagnosticText.gameObject.SetActive(false);
                // Keep the diagnostic bootstrap component for tests/logging, hide its debug overlay.
                var bootstrap = new SerializedObject(root.GetComponent<QuestBootstrap>());
                var debugText = (TextMesh)bootstrap.FindProperty("statusText").objectReferenceValue;
                if (debugText != null) debugText.gameObject.SetActive(false);
                var panel = new GameObject("Quest Connection Panel");
                panel.transform.SetParent(camera.transform, false);
                panel.transform.localPosition = new Vector3(-0.48f, 0.38f, 1.25f);
                var text = panel.AddComponent<TextMesh>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
                text.fontSize = 64;
                text.characterSize = 0.01f;
                text.anchor = TextAnchor.UpperLeft;
                text.color = Color.white;
                text.text = "HiBoP | Quest connection\nPreparing secure pairing...";
                var serialized = new SerializedObject(panel.AddComponent<QuestConnectionPanel>());
                Set(serialized, "session", root.GetComponentInChildren<QuestAnatomySession>(true));
                Set(serialized, "view", root.GetComponentInChildren<QuestAnatomyView>(true));
                Set(serialized, "statusText", text);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, QuestBootstrapSetup.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildDesktop()
        {
            var root = new GameObject("Quest Connection", typeof(RectTransform));
            try
            {
                var rootRect = (RectTransform)root.transform;
                rootRect.anchorMin = rootRect.anchorMax = new Vector2(0, 1);
                rootRect.pivot = new Vector2(0, 1);
                root.AddComponent<LayoutElement>().ignoreLayout = true;
                var controller = root.AddComponent<DesktopQuestPanel>();
                var panel = Rect(root.transform, "Panel", 300, -30, 670, 580);
                var canvas = panel.gameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100;
                var canvasSettings = new SerializedObject(canvas);
                canvasSettings.FindProperty("m_OverrideSorting").boolValue = true;
                canvasSettings.ApplyModifiedPropertiesWithoutUndo();
                panel.gameObject.AddComponent<GraphicRaycaster>();
                panel.gameObject.AddComponent<Image>().color = new Color(0.09f, 0.11f, 0.14f, 0.99f);
                Label(panel, "Title", "Quest connection", 20, -15, 560, 32, 24);
                var close = Button(panel, "Close", 565, -15, 85);
                UnityEventTools.AddPersistentListener(close.onClick, controller.TogglePanel);
                Label(panel, "Instructions", "1. Open HiBoP on Quest. Enter the address shown in the headset.", 20, -60, 630, 30);
                var address = Input(panel, "Quest address", "192.168.1.x", 20, -100, 300);
                var inspect = Button(panel, "Inspect Quest", 335, -100, 180);
                Label(panel, "Compare", "2. Compare EVERY fingerprint group with the headset.", 20, -150, 630, 30);
                var fingerprint = Label(panel, "Fingerprint", "Fingerprint appears after Inspect Quest.", 20, -185, 630, 52, 18);
                var toggleRect = Rect(panel, "Confirm fingerprint", 20, -245, 630, 30);
                var toggle = toggleRect.gameObject.AddComponent<Toggle>();
                var box = Rect(toggleRect, "Box", 0, 0, 25, 25).gameObject.AddComponent<Image>();
                box.color = new Color(0.3f, 0.35f, 0.4f);
                var mark = Rect(box.transform, "Checked", 4, -4, 17, 17).gameObject.AddComponent<Image>();
                mark.color = new Color(0.3f, 0.9f, 0.8f);
                toggle.targetGraphic = box;
                toggle.graphic = mark;
                toggle.isOn = false;
                Label(toggleRect, "Label", "All groups match the fingerprint displayed in my Quest", 35, 0, 590, 30);
                var code = Input(panel, "Pairing code", "6-digit headset code", 20, -285, 300);
                code.characterLimit = 6;
                code.contentType = InputField.ContentType.IntegerNumber;
                var pair = Button(panel, "Pair", 335, -285, 180);
                var selection = Label(panel, "Selection", "Select a complete visualization anatomical column.", 20, -340, 630, 58);
                var send = Button(panel, "Envoyer au Quest", 20, -410, 230);
                var retry = Button(panel, "Retry same snapshot", 265, -410, 235);
                var cancel = Button(panel, "Cancel", 515, -410, 135);
                var status = Label(panel, "Status", "Not paired. Views remain independent.", 20, -465, 630, 90);
                var serialized = new SerializedObject(controller);
                Set(serialized, "panel", panel.gameObject);
                Set(serialized, "address", address);
                Set(serialized, "code", code);
                Set(serialized, "fingerprint", fingerprint);
                Set(serialized, "confirmed", toggle);
                Set(serialized, "status", status);
                Set(serialized, "selection", selection);
                Set(serialized, "inspect", inspect);
                Set(serialized, "pair", pair);
                Set(serialized, "send", send);
                Set(serialized, "retry", retry);
                Set(serialized, "cancel", cancel);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                panel.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, DesktopPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.gameObject.layer = parent.gameObject.layer;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static Text Label(Transform parent, string name, string value, float x, float y, float width, float height, int size = 17)
        {
            var text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        private static Button Button(Transform parent, string name, float x, float y, float width)
        {
            var rect = Rect(parent, name, x, y, width, 34);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.17f, 0.32f, 0.38f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = Label(rect, "Label", name, 0, 0, width, 34);
            label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = Vector2.zero;
            return button;
        }

        private static InputField Input(Transform parent, string name, string placeholder, float x, float y, float width)
        {
            var rect = Rect(parent, name, x, y, width, 34);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.23f, 0.28f);
            var input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = Label(rect, "Text", "", 8, -5, width - 16, 26);
            var hint = Label(rect, "Placeholder", placeholder, 8, -5, width - 16, 26);
            hint.color = new Color(0.65f, 0.7f, 0.75f);
            input.placeholder = hint;
            return input;
        }

        private static void Set(SerializedObject serialized, string field, Object value) => serialized.FindProperty(field).objectReferenceValue = value;
    }
}
