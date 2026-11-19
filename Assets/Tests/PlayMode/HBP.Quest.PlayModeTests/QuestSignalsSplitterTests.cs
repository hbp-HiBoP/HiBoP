#if UNITY_EDITOR
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class QuestSignalsSplitterTests
    {
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(150)]
        public async Task Drag_SnapsBothWaysAndCanRestoreBothPanesAtEveryZoom(int percent)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Signals Window.prefab"));
            var viewer = new GameObject("Splitter camera", typeof(Camera));
            var events = new GameObject("Splitter events", typeof(EventSystem));
            try
            {
                root.GetComponent<QuestSignalsPanel>().enabled = false;
                root.GetComponent<QuestWindowFollower>().enabled = false;
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                root.transform.localScale = Vector3.one * .001f;
                var camera = viewer.GetComponent<Camera>();
                camera.transform.position = Vector3.back * 2;
                camera.orthographic = true;
                camera.orthographicSize = 1;
                root.GetComponent<Canvas>().worldCamera = camera;
                root.GetComponent<QuestWindow>().Open();
                await UniTask.NextFrame();
                root.GetComponentInChildren<QuestWindowContentZoom>(true).SetZoomPercent(percent);
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                var splitter = root.GetComponentInChildren<QuestSignalsSplitter>(true);
                var fields = new SerializedObject(splitter);
                var body = (RectTransform)fields.FindProperty("available").objectReferenceValue;
                var graph = (LayoutElement)fields.FindProperty("graph").objectReferenceValue;
                var matrix = (LayoutElement)fields.FindProperty("matrix").objectReferenceValue;
                var handle = (RectTransform)splitter.transform;
                var data = new PointerEventData(events.GetComponent<EventSystem>())
                {
                    pointerPressRaycast = new RaycastResult { module = root.GetComponent<TrackedDeviceRaycaster>() }
                };
                foreach (float fraction in new[] { .01f, .5f, .99f, .5f })
                {
                    data.position = camera.WorldToScreenPoint(body.TransformPoint(new Vector3(0, Mathf.Lerp(body.rect.yMax, body.rect.yMin, fraction), 0)));
                    splitter.OnBeginDrag(data);
                    splitter.OnDrag(data);
                    await UniTask.NextFrame();
                    Canvas.ForceUpdateCanvases();
                    Assert.That(graph.gameObject.activeSelf, Is.EqualTo(fraction > .08f));
                    Assert.That(matrix.gameObject.activeSelf, Is.EqualTo(fraction < .92f));
                    Assert.That(handle.gameObject.activeInHierarchy, Is.True);
                    Assert.That(handle.rect.height * percent / 100f, Is.EqualTo(28).Within(.01f));
                    var corners = new Vector3[4];
                    handle.GetWorldCorners(corners);
                    foreach (var corner in corners)
                        Assert.That(body.InverseTransformPoint(corner).y, Is.InRange(body.rect.yMin - .01f, body.rect.yMax + .01f), "The grab area must stay fully inside the visible body.");
                    if (fraction < .08f) Assert.That(splitter.Ratio, Is.Zero);
                    if (fraction > .92f) Assert.That(splitter.Ratio, Is.EqualTo(1));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(viewer);
                Object.DestroyImmediate(events);
            }
        }
    }
}
#endif
