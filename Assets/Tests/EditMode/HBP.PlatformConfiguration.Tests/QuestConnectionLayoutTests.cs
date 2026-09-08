using HBP.UI.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.PlatformConfiguration
{
    public class QuestConnectionLayoutTests
    {
        [Test]
        public void MainMenuLayout_DoesNotMoveQuestPanelOutsideTheViewport()
        {
            var root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            try
            {
                var menu = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/General/Main menu.prefab"), root.transform);
                var controller = menu.GetComponentInChildren<DesktopQuestPanel>(true);
                Assert.That(controller, Is.Not.Null);
                Assert.That(controller.GetComponent<LayoutElement>().ignoreLayout, Is.True);
                var fields = new SerializedObject(controller);
                var panel = (GameObject)fields.FindProperty("panel").objectReferenceValue;
                panel.SetActive(true);
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)menu.transform);
                Assert.That(((RectTransform)controller.transform).anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(panel.GetComponent<Canvas>().overrideSorting, Is.True);
                Assert.That(panel.GetComponent<Canvas>().sortingOrder, Is.EqualTo(100));
                var send = (Button)fields.FindProperty("send").objectReferenceValue;
                Assert.That(send.GetComponentInChildren<Text>().rectTransform.rect.height, Is.GreaterThan(20));
                var button = menu.transform.Find("Left/Quest").GetComponent<Button>();
                Assert.That(button.onClick.GetPersistentTarget(0), Is.SameAs(controller));
                Assert.That(button.onClick.GetPersistentMethodName(0), Is.EqualTo("TogglePanel"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
