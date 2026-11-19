#if UNITY_EDITOR
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using HBP.Quest;
using HBP.UI.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public sealed class QuestDialogBoxTests
    {
        private GameObject host;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Dialog host", typeof(RectTransform), typeof(Canvas));
            var camera = new GameObject("Head", typeof(Camera));
            camera.transform.SetParent(host.transform, false);
            host.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            host.GetComponent<Canvas>().worldCamera = camera.GetComponent<Camera>();
            ((RectTransform)host.transform).sizeDelta = new Vector2(1000, 800);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(host);

        private DialogBox Create(bool quest)
        {
            string path = quest ? "Assets/Prefabs/Quest/UI/Quest Dialog Box.prefab" : "Assets/Prefabs/DialogBox/Dialog Box.prefab";
            return Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), host.transform).GetComponent<DialogBox>();
        }

        private static Button[] Buttons(DialogBox dialog)
        {
            var parent = (Transform)new SerializedObject(dialog).FindProperty("m_ButtonsParent").objectReferenceValue;
            return parent.GetComponentsInChildren<Button>();
        }

        [TestCase(false, 0), TestCase(false, 1), TestCase(true, 0), TestCase(true, 1)]
        public async Task DesktopAndQuest_ReturnSelectedButtonAndClose(bool quest, int selected)
        {
            var dialog = Create(quest);
            Task<int> pending = dialog.OpenAsync(DialogBoxType.Warning, "Forget association?", "A confirmation message.", new[] { "Forget association", "Cancel" }).AsTask();
            var buttons = Buttons(dialog);
            Assert.That(buttons.Length, Is.EqualTo(2));
            Assert.That(pending.IsCompleted, Is.False);
            if (quest)
            {
                Assert.That(QuestDialogBox.Current, Is.SameAs(dialog));
                var icon = (HBP.Theme.ThemeElement)new SerializedObject(dialog).FindProperty("icon").objectReferenceValue;
                Assert.That(icon.GetComponent<Image>().sprite, Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Google Material/Warning.png")));
                dialog.GetComponent<QuestLoadingFollower>().Step(0, true);
                Assert.That(dialog.transform.position.z, Is.EqualTo(1.15f).Within(.001));
                foreach (var theme in buttons.SelectMany(button => button.GetComponentsInChildren<HBP.Theme.ThemeElement>())) theme.Set();
                Canvas.ForceUpdateCanvases();
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Quest/UI/Materials/Quest Loading Overlay.mat");
                foreach (var graphic in dialog.GetComponentsInChildren<Graphic>()) Assert.That(graphic.material, Is.SameAs(material), graphic.name);
            }

            buttons[selected].onClick.Invoke();
            Assert.That(await pending, Is.EqualTo(selected));
            await UniTask.NextFrame();
            Assert.That(dialog == null, Is.True);
            Assert.That(QuestDialogBox.Current, Is.Null);
        }

        [TestCase(false), TestCase(true)]
        public async Task Cancellation_ClosesPresentationWithoutAccepting(bool quest)
        {
            using var stop = new CancellationTokenSource();
            var dialog = Create(quest);
            Task<int> pending = dialog.OpenAsync(DialogBoxType.Warning, "Title", "Message", new[] { "Accept", "Cancel" }, stop.Token).AsTask();
            stop.Cancel();
            bool cancelled = false;
            try
            {
                await pending;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            Assert.That(cancelled, Is.True);
            Assert.That(dialog.gameObject.activeSelf, Is.False);
            Assert.That(QuestDialogBox.Current, Is.Null);
        }

        [TestCase(0, 0), TestCase(120, 35), TestCase(180, 0), TestCase(270, -35)]
        public async Task OpeningAndFollowingHead_KeepsBodyAndButtonsVisible(float yaw, float pitch)
        {
            var camera = host.GetComponent<Canvas>().worldCamera;
            camera.transform.SetPositionAndRotation(new Vector3(4, 1.7f, -3), Quaternion.Euler(pitch, yaw, 0));
            var dialog = Create(true);
            Task<int> pending = dialog.OpenAsync(DialogBoxType.Warning, "Forget association?", "Forget the Desktop association? You will need to enter a new code on Desktop.", new[] { "Forget association", "Cancel" }).AsTask();
            try
            {
                var follower = dialog.GetComponent<QuestLoadingFollower>();
                follower.Step(0, true);
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                AssertDialogContentVisible(dialog);
                camera.transform.rotation = Quaternion.Euler(-pitch, yaw + 180, 0);
                follower.Step(10, true);
                Canvas.ForceUpdateCanvases();
                AssertDialogContentVisible(dialog);
            }
            finally
            {
                dialog.Close();
                await pending;
            }
        }

        private static void AssertDialogContentVisible(DialogBox dialog)
        {
            var message = (Text)new SerializedObject(dialog).FindProperty("message").objectReferenceValue;
            Assert.That(message.canvasRenderer.cull, Is.False, "The body must be visible without reactivating the headset.");
            foreach (var button in Buttons(dialog))
            {
                Assert.That(button.GetComponent<Image>().canvasRenderer.cull, Is.False, button.name);
                Assert.That(button.GetComponentInChildren<Text>().canvasRenderer.cull, Is.False, button.name + " label");
            }

            foreach (var mask in dialog.GetComponentsInChildren<RectMask2D>())
            {
                Assert.That(mask.canvasRect.width, Is.GreaterThan(0), mask.name);
                Assert.That(mask.canvasRect.height, Is.GreaterThan(0), mask.name);
            }
        }

        [Test]
        public async Task ClosingTopDialog_RestoresPreviousModalAndDestructionCompletesPendingResponse()
        {
            var first = Create(true);
            Task<int> firstPending = first.OpenAsync(DialogBoxType.Warning, "First", "Message", Array.Empty<string>()).AsTask();
            var second = Create(true);
            Task<int> secondPending = second.OpenAsync(DialogBoxType.Warning, "Second", "Message", Array.Empty<string>()).AsTask();
            Assert.That(QuestDialogBox.Current, Is.SameAs(second));
            second.Close();
            Assert.That(await secondPending, Is.EqualTo(-1));
            Assert.That(QuestDialogBox.Current, Is.SameAs(first));
            Object.DestroyImmediate(first.gameObject);
            Assert.That(await firstPending, Is.EqualTo(-1));
            Assert.That(QuestDialogBox.Current, Is.Null);
        }
    }
}
#endif
