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
        public void SceneControls_UseNestedPrefabsAndSerializedOwners()
        {
            var pairing = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/Windows/Quest Pairing window.prefab").GetComponent<QuestPairingWindow>();
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
        public void QuestMenu_UsesTwoWindows()
        {
            var menu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/General/Main menu.prefab");
            var manager = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers/Quest Manager.prefab");
            Assert.That(manager.GetComponent<QuestManager>(), Is.Not.Null);
            var quest = menu.GetComponentInChildren<QuestMenu>(true);
            Assert.That(quest, Is.Not.Null);
            Assert.That(quest.GetComponentsInChildren<MenuButton>(true).Length, Is.EqualTo(2));
            var questMenu = new SerializedObject(quest);
            var sendCondition = (InteractableConditions)questMenu.FindProperty("sendConditions").objectReferenceValue;
            Assert.That(sendCondition.NeedPairedQuest, Is.True);
            var pairWindow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/Windows/Quest Pairing window.prefab").GetComponent<QuestPairingWindow>();
            var sendWindow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/Windows/Quest Send window.prefab").GetComponent<QuestSendWindow>();
            Assert.That(pairWindow, Is.Not.Null);
            Assert.That(sendWindow, Is.Not.Null);
            foreach (var window in new Window[] { pairWindow, sendWindow })
                Assert.That(window.GetComponentsInChildren<Button>(true).Any(button => Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Any(index => button.onClick.GetPersistentTarget(index) == window && button.onClick.GetPersistentMethodName(index) == "Close")), Is.True);
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
