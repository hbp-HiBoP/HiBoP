using HBP.UI.Main;
using HBP.UI.Quest;
using HBP.UI.Tools;
using HBP.Quest;
using HBP.Quest.Desktop;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace HBP.Tests.PlatformConfiguration
{
    public class QuestConnectionLayoutTests
    {
        [Test]
        public void UsbStatusAndRetryAreAuthoredAndSerialized()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Connection Window.prefab");
            var panel = root.GetComponent<QuestStatusPanel>();
            var data = new SerializedObject(panel);
            var status = (Text)data.FindProperty("usbStatus").objectReferenceValue;
            var retry = (Button)data.FindProperty("retryUsb").objectReferenceValue;
            Assert.That(status, Is.Not.Null);
            Assert.That(retry, Is.Not.Null);
            Assert.That(status.transform.parent, Is.EqualTo(root.transform.Find("Background/Content")));
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(retry), Does.EndWith("Quest Button.prefab"));
        }

        [Test]
        public void SceneControls_UseNestedPrefabsAndSerializedOwners()
        {
            var pairing = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/Windows/Quest window.prefab").GetComponent<DesktopQuestWindow>();
            Assert.That(pairing.GetComponentsInChildren<Toggle>(true).Any(candidate => candidate.name == "Synchronize visualizations"), Is.False);
            var preferences = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/Windows/User preferences window.prefab").GetComponent<UserPreferencesModifier>();
            var quest = (QuestPreferencesSubModifier)new SerializedObject(preferences).FindProperty("m_QuestPreferencesSubModifier").objectReferenceValue;
            Assert.That(quest, Is.Not.Null);
            var toggle = (Toggle)new SerializedObject(quest).FindProperty("m_AutomaticallySynchronizeScenesToggle").objectReferenceValue;
            Assert.That(toggle, Is.Not.Null);
            Assert.That(toggle.isOn, Is.True);
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(toggle), Does.EndWith("Toggle with Text.prefab"));
            Assert.That(toggle.GetComponent<Tooltip>().Text, Does.Contain("closures").And.Contain("already shared"));
            var connect = (Toggle)new SerializedObject(quest).FindProperty("m_AutomaticallyConnectToggle").objectReferenceValue;
            Assert.That(connect, Is.Not.Null);
            Assert.That(connect.isOn, Is.True);
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(connect), Does.EndWith("Toggle with Text.prefab"));
            Assert.That(connect.GetComponent<Tooltip>().Text, Does.Contain("most recently").And.Contain("code"));
            Assert.That(new SerializedObject(pairing).FindProperty("disconnect").objectReferenceValue, Is.Not.Null);
            var selector = preferences.transform.Find("Content/Fields/Tabs/General Tab/Tab selectors/Quest tab selector").GetComponent<Toggle>();
            Assert.That(selector.group, Is.Not.Null);
            Assert.That(selector.onValueChanged.GetPersistentEventCount(), Is.EqualTo(1));
            Assert.That(selector.onValueChanged.GetPersistentTarget(0), Is.SameAs(quest.gameObject));
            Assert.That(selector.onValueChanged.GetPersistentMethodName(0), Is.EqualTo("SetActive"));
            var bootstrap = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var display = new SerializedObject(bootstrap.GetComponentInChildren<QuestDisplayPanel>(true));
            foreach (string field in new[] { "session", "view", "input", "openPatient", "rows", "rowPrefab" }) Assert.That(display.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            var row = (QuestSceneRow)display.FindProperty("rowPrefab").objectReferenceValue;
            foreach (string field in new[] { "title", "details", "visibility", "close", "selectedState" }) Assert.That(new SerializedObject(row).FindProperty(field).objectReferenceValue, Is.Not.Null, field);
        }

        [Test]
        public void QuestWindow_IsAvailableFromEditAndUsesStandardNestedControls()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/General/Main menu.prefab");
            var edit = menu.GetComponentInChildren<EditMenu>(true);
            Assert.That(new SerializedObject(edit).FindProperty("m_OpenQuestButton").objectReferenceValue, Is.Not.Null);
            Assert.That(menu.transform.Find("Left/Quest"), Is.Null);
            var window = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/Windows/Quest window.prefab").GetComponent<DesktopQuestWindow>();
            var data = new SerializedObject(window);
            foreach (string field in new[] { "devices", "automatic", "manual", "automaticFields", "manualFields", "address", "pair", "pairLabel", "disconnect", "visualizations", "send", "m_OKButton" })
                Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            foreach (string field in new[] { "automatic", "manual" })
            {
                var toggle = (Toggle)data.FindProperty(field).objectReferenceValue;
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(toggle), Does.EndWith("Toggle with Text.prefab"));
                Assert.That(toggle.group, Is.Not.Null);
            }

            var list = (VisualizationList)data.FindProperty("visualizations").objectReferenceValue;
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(list), Does.EndWith("Visualization list.prefab"));
            Assert.That(list.Actionable, Is.False);
            Assert.That(list.ItemSelection, Is.EqualTo(HBP.UI.Tools.Lists.SelectableList<HBP.Core.Data.Visualization>.SelectionType.MultipleItems));
            var ok = (Button)data.FindProperty("m_OKButton").objectReferenceValue;
            foreach (string field in new[] { "pair", "disconnect", "send" })
                Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.SameAs(ok));
            Assert.That(window.GetComponentsInChildren<Button>(true).Any(button => Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Any(index => button.onClick.GetPersistentTarget(index) == window && button.onClick.GetPersistentMethodName(index) == "Close")), Is.True);
            var help = window.GetComponentInChildren<DropWindow>(true);
            Assert.That(help.NoBlocker, Is.True);
            Assert.That(help.Window, Is.Not.Null);
            Assert.That(help.Window.GetComponent<DropWindowAdjuster>(), Is.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/Windows/Quest Pairing window.prefab"), Is.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/Windows/Quest Send window.prefab"), Is.Null);
        }

        [Test]
        public void QuestWindow_ResizeBordersAndCompactPairingFieldsAreWired()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/Windows/Quest window.prefab");
            var resizer = prefab.GetComponent<Resizer>();
            Assert.That(resizer, Is.Not.Null);
            Assert.That(resizer.enabled, Is.True);
            var borders = prefab.transform.Find("Borders").GetComponentsInChildren<UnityEngine.EventSystems.EventTrigger>(true);
            Assert.That(borders.Length, Is.EqualTo(8));
            foreach (var border in borders)
            {
                Assert.That(border.enabled && border.gameObject.activeSelf, Is.True, border.name);
                foreach (var entry in border.triggers)
                {
                    Assert.That(entry.callback.GetPersistentEventCount(), Is.EqualTo(1), border.name);
                    Assert.That(entry.callback.GetPersistentTarget(0), Is.SameAs(resizer), border.name + ": " + entry.eventID);
                }
            }

            var standard = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Elements/Content/Sections/Prefix.prefab").GetComponent<LayoutElement>();
            foreach (var row in new[] { "Automatic", "Manual" })
            {
                var fields = prefab.transform.Find("Content/Fields/Pairing/" + row);
                var prefix = fields.Find("Prefix");
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefix), Does.EndWith("Prefix.prefab"));
                Assert.That(fields.GetComponent<HorizontalLayoutGroup>(), Is.Not.Null);
                Assert.That(prefix.GetSiblingIndex(), Is.Zero);
                Assert.That(prefix.GetComponent<LayoutElement>().preferredWidth, Is.EqualTo(standard.preferredWidth));
                var text = prefix.GetComponentInChildren<Text>();
                Assert.That(text.text, Is.EqualTo(row == "Automatic" ? "Detected Quests" : "IP Address"));
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(standard.preferredWidth), row);
            }

            Assert.That(prefab.transform.Find("Content/Fields/Pairing/Status"), Is.Null);
            Assert.That(prefab.transform.Find("Content/Fields/Send visualizations/Selection status"), Is.Null);
            Assert.That(prefab.GetComponentInChildren<ListSelectionCounter>(true).gameObject.activeSelf, Is.False);
            var list = prefab.GetComponentInChildren<VisualizationList>(true).transform;
            Assert.That(list.parent.Find("Actions").GetSiblingIndex(), Is.EqualTo(list.GetSiblingIndex() + 1));
        }

        [Test]
        public void DropWindow_NoBlockerKeepsHelpOpenAndFollowsItsParent()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CustomUI/DropWindow.prefab");
            Assert.That(prefab.GetComponent<DropWindow>().NoBlocker, Is.False, "Existing dropdowns keep their click-outside behavior.");
            var root = Object.Instantiate(prefab);
            try
            {
                var help = root.GetComponent<DropWindow>();
                help.NoBlocker = true;
                help.Window.SetActive(false);
                help.Open();
                help.Open();
                Assert.That(help.Window.activeSelf, Is.True);
                Assert.That(root.transform.childCount, Is.EqualTo(prefab.transform.childCount));
                var before = help.Window.transform.position;
                root.transform.position += Vector3.right * 50;
                Assert.That(help.Window.transform.position.x, Is.EqualTo(before.x + 50));
                help.Close();
                Assert.That(help.Window.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void QuestPrefabs_HaveSerializedStatusAndLoadingReferences()
        {
            var bootstrap = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var connection = bootstrap.GetComponentInChildren<QuestConnectionPanel>(true);
            Assert.That(new SerializedObject(connection).FindProperty("statusPanel").objectReferenceValue, Is.Not.Null);
            var status = bootstrap.GetComponentsInChildren<QuestWindow>(true).Single(window => window.Key == "Connection");
            Assert.That(new SerializedObject(status).FindProperty("initiallyOpen").boolValue, Is.False);
            var manager = bootstrap.GetComponentInChildren<LoadingManager>(true);
            Assert.That(manager, Is.Not.Null);
            Assert.That(new SerializedObject(manager).FindProperty("m_LoadingCircle").objectReferenceValue, Is.Not.Null);
            var follower = bootstrap.GetComponentInChildren<QuestLoadingFollower>(true);
            Assert.That(new SerializedObject(follower).FindProperty("headCamera").objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void InputDialog_HasIndependentInputAndActions()
        {
            var manager = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers/Dialog Box Manager.prefab").GetComponent<DialogBoxManager>();
            var inputPrefab = (GameObject)new SerializedObject(manager).FindProperty("m_InputDialogBoxPrefab").objectReferenceValue;
            var dialog = inputPrefab.GetComponent<InputDialogBox>();
            var data = new SerializedObject(dialog);
            foreach (string field in new[] { "title", "message", "input", "placeholder", "confirm", "confirmLabel", "cancel", "cancelLabel" })
                Assert.That(data.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
        }
    }
}
