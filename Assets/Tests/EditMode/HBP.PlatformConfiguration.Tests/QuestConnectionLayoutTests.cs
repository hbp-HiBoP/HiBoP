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
