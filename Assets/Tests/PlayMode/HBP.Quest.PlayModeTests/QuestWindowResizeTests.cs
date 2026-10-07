#if UNITY_EDITOR
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class QuestWindowResizeTests
    {
        private const string Demo = "Assets/Prefabs/Quest/UI/Quest Resizable Window Demo.prefab";

        [Test]
        public async Task DemoScene_WiresTrackedPointerAndReopensBothWindowsFromToolbar()
        {
            const string path = "Assets/_Scenes/QuestUIDemo.unity";
            await EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            var scene = SceneManager.GetSceneByPath(path);
            try
            {
                await UniTask.NextFrame();
                var components = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                var manager = components.OfType<QuestWindowsManager>().Single();
                var standard = manager.Find("Connection");
                var resizable = manager.Find("Display");
                Assert.That(standard, Is.Not.Null);
                Assert.That(resizable, Is.Not.Null);
                Assert.That(standard.GetComponentInChildren<QuestWindowResizer>(), Is.Null);
                Assert.That(resizable.GetComponentInChildren<QuestWindowResizer>(), Is.Not.Null);
                Assert.That(standard.transform.position.x, Is.LessThan(resizable.transform.position.x));
                Assert.That(components.OfType<QuestConnectionPanel>(), Is.Empty);
                var pointer = components.OfType<QuestPointerInput>().Single();
                var fields = new SerializedObject(pointer);
                foreach (string field in new[] { "policy", "head", "left", "right", "trackingOrigin", "leftRay", "rightRay" })
                    Assert.That(fields.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                var toolbar = components.OfType<QuestToolbar>().Single();
                var toolbarFields = new SerializedObject(toolbar);
                standard.Close();
                resizable.Close();
                ((Button)toolbarFields.FindProperty("connectionButton").objectReferenceValue).onClick.Invoke();
                ((Button)toolbarFields.FindProperty("displayButton").objectReferenceValue).onClick.Invoke();
                Assert.That(standard.IsOpen && resizable.IsOpen, Is.True);
                await UniTask.NextFrame(); // Reopened graphics must rejoin the canvas before raycasting.
                var camera = resizable.GetComponent<Canvas>().worldCamera;
                Assert.That(camera, Is.Not.Null);
                camera.fieldOfView = 85;
                var handles = resizable.GetComponentsInChildren<QuestWindowResizer>();
                Assert.That(handles.Length, Is.EqualTo(4));
                Canvas.ForceUpdateCanvases();
                foreach (var handle in handles)
                {
                    var data = new QuestPointerEventData(pointer.GetComponent<EventSystem>())
                    {
                        trackedDevicePosition = camera.transform.position,
                        trackedDeviceOrientation = Quaternion.LookRotation(handle.transform.position - camera.transform.position)
                    };
                    var hits = new System.Collections.Generic.List<RaycastResult>();
                    resizable.GetComponent<TrackedDeviceRaycaster>().Raycast(data, hits);
                    Assert.That(hits.Any(h => h.gameObject == handle.gameObject), Is.True, handle.Edge.ToString());
                }

                var viewport = resizable.GetComponentInChildren<RectMask2D>();
                var information = viewport.GetComponentInChildren<Text>();
                Assert.That(information.preferredHeight, Is.GreaterThan(((RectTransform)viewport.transform).rect.height), "The initial window deliberately cannot display all the information.");
                QuestWindowInteractionTests.Capture(camera, "Quest UI Edges Initial.png");
                Vector3 scale = resizable.transform.localScale;
                int fontSize = information.fontSize;
                var top = handles.Single(h => h.Edge == QuestWindowResizer.WindowEdge.Top);
                var drag = Pointer(pointer.gameObject, -10, top.transform.position, resizable.transform.forward);
                top.OnBeginDrag(drag);
                drag.Ray = RayTo(top.transform.position + resizable.transform.TransformVector(Vector3.up * 400), resizable.transform.forward);
                top.OnDrag(drag);
                top.OnEndDrag(drag);
                Canvas.ForceUpdateCanvases();
                Assert.That(information.preferredHeight, Is.LessThanOrEqualTo(((RectTransform)viewport.transform).rect.height), "Increasing the area reveals the complete text.");
                Assert.That(information.fontSize, Is.EqualTo(fontSize));
                Assert.That(resizable.transform.localScale, Is.EqualTo(scale));
                QuestWindowInteractionTests.Capture(camera, "Quest UI Edges Expanded.png");
            }
            finally
            {
                await SceneManager.UnloadSceneAsync(scene);
            }
        }

        [TestCase(QuestWindowResizer.WindowEdge.Left)]
        [TestCase(QuestWindowResizer.WindowEdge.Right)]
        [TestCase(QuestWindowResizer.WindowEdge.Top)]
        [TestCase(QuestWindowResizer.WindowEdge.Bottom)]
        public void Resize_ChangesOneDimensionKeepsOppositeEdgeAndScaleAndStopsFollow(QuestWindowResizer.WindowEdge edge)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Demo));
            var events = new GameObject("Resize events", typeof(EventSystem));
            var head = new GameObject("Resize viewer");
            try
            {
                var window = root.GetComponent<QuestWindow>();
                var resizer = root.GetComponentsInChildren<QuestWindowResizer>().Single(r => r.Edge == edge);
                var rect = (RectTransform)root.transform;
                rect.pivot = new Vector2(.2f, .8f); // The opposite edge must also stay fixed with a non-central pivot.
                root.transform.SetPositionAndRotation(new Vector3(0.2f, 0.3f, 1), Quaternion.Euler(10, 25, 0));
                Vector3 scale = root.transform.localScale;
                Quaternion rotation = root.transform.rotation;
                Vector2 size = rect.rect.size;
                bool horizontal = edge == QuestWindowResizer.WindowEdge.Left || edge == QuestWindowResizer.WindowEdge.Right;
                bool positive = edge == QuestWindowResizer.WindowEdge.Right || edge == QuestWindowResizer.WindowEdge.Top;
                int axis = horizontal ? 0 : 1;
                Vector3 direction = horizontal ? Vector3.right : Vector3.up;
                direction *= positive ? 1 : -1;
                Vector3 opposite = OppositeEdge(rect, horizontal, positive);
                var text = root.GetComponentInChildren<Text>();
                int fontSize = text.fontSize;
                Vector3 point = resizer.transform.position;
                Vector3 growth = rect.TransformVector(direction * 100);
                var data = Pointer(events, -10, point, root.transform.forward);
                resizer.OnInitializePotentialDrag(data);
                Assert.That(data.useDragThreshold, Is.False);
                resizer.OnBeginDrag(data);
                resizer.OnDrag(data);
                Assert.That(rect.rect.size, Is.EqualTo(size), "No jump on capture.");
                Assert.That(data.eligibleForClick, Is.False);
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(resizer.gameObject), Is.Null);

                data.Ray = RayTo(point + growth, root.transform.forward);
                resizer.OnDrag(data);
                resizer.OnDrag(data);
                Assert.That(rect.rect.size[axis], Is.EqualTo(size[axis] + 100).Within(.01f));
                Assert.That(rect.rect.size[1 - axis], Is.EqualTo(size[1 - axis]));
                Assert.That(root.transform.localScale, Is.EqualTo(scale));
                Assert.That(Vector3.Distance(OppositeEdge(rect, horizontal, positive), opposite), Is.LessThan(.00001f));
                Assert.That(text.fontSize, Is.EqualTo(fontSize));
                Vector3 resizedPosition = root.transform.position;
                var follower = root.GetComponent<QuestWindowFollower>();
                follower.Configure(head.transform, window);
                head.transform.rotation = Quaternion.Euler(0, 100, 0);
                follower.Step(3, true);
                Assert.That(root.transform.position, Is.EqualTo(resizedPosition));
                Assert.That(root.transform.rotation, Is.EqualTo(rotation));

                data.Ray = RayTo(point + growth * 100, root.transform.forward);
                resizer.OnDrag(data);
                Assert.That(rect.rect.size[axis], Is.EqualTo(horizontal ? 1200 : 1000).Within(.01f));
                data.Ray = RayTo(point - growth * 100, root.transform.forward);
                resizer.OnDrag(data);
                Assert.That(rect.rect.size[axis], Is.EqualTo(horizontal ? 500 : 350).Within(.01f));
                Assert.That(root.transform.localScale, Is.EqualTo(scale));
                Assert.That(Vector3.Distance(OppositeEdge(rect, horizontal, positive), opposite), Is.LessThan(.00001f));
                resizer.OnEndDrag(data);
                Assert.That(window.IsManipulated, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(events);
                Object.DestroyImmediate(head);
            }
        }

        [Test]
        public void Resize_ExcludesOtherHandAndWindowDragAndCancelsOnCloseOrDisable()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Demo));
            var events = new GameObject("Resize cancellation events", typeof(EventSystem));
            try
            {
                var window = root.GetComponent<QuestWindow>();
                var resizer = root.GetComponentsInChildren<QuestWindowResizer>().Single(r => r.Edge == QuestWindowResizer.WindowEdge.Right);
                var otherEdge = root.GetComponentsInChildren<QuestWindowResizer>().Single(r => r.Edge == QuestWindowResizer.WindowEdge.Left);
                var dragger = root.GetComponentInChildren<QuestWindowDragger>();
                root.transform.position = Vector3.forward;
                Vector3 scale = root.transform.localScale, position = root.transform.position;
                var first = Pointer(events, -10, resizer.transform.position, Vector3.forward);
                var second = Pointer(events, -11, resizer.transform.position + Vector3.right, Vector3.forward);
                resizer.OnBeginDrag(first);
                resizer.OnBeginDrag(second);
                otherEdge.OnBeginDrag(second);
                otherEdge.OnDrag(second);
                otherEdge.OnEndDrag(second);
                resizer.OnDrag(second);
                resizer.OnEndDrag(second);
                dragger.OnBeginDrag(second);
                dragger.OnDrag(second);
                dragger.OnEndDrag(second);
                Assert.That(window.IsManipulated, Is.True);
                Assert.That(root.transform.localScale, Is.EqualTo(scale));
                Assert.That(root.transform.position, Is.EqualTo(position));
                window.Close();
                window.Open();
                first.Ray = second.Ray;
                resizer.OnDrag(first);
                Assert.That(window.IsManipulated, Is.False);
                Assert.That(root.transform.localScale, Is.EqualTo(scale), "Reopening must not resume the old gesture.");

                first = Pointer(events, -10, resizer.transform.position, Vector3.forward);
                resizer.OnBeginDrag(first);
                resizer.enabled = false;
                resizer.enabled = true;
                first.Ray = second.Ray;
                resizer.OnDrag(first);
                Assert.That(window.IsManipulated, Is.False);
                Assert.That(root.transform.localScale, Is.EqualTo(scale));
                first.Ray = new Ray(Vector3.zero, Vector3.right);
                resizer.OnBeginDrag(first);
                Assert.That(window.IsManipulated, Is.False, "Parallel rays cannot start a resize.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(events);
            }
        }

        private static Ray RayTo(Vector3 point, Vector3 normal) => new Ray(point - normal, normal);

        private static Vector3 OppositeEdge(RectTransform rect, bool horizontal, bool positive) => rect.TransformPoint(horizontal ? new Vector3(positive ? rect.rect.xMin : rect.rect.xMax, 0, 0) : new Vector3(0, positive ? rect.rect.yMin : rect.rect.yMax, 0));

        private static QuestPointerEventData Pointer(GameObject events, int id, Vector3 point, Vector3 normal) => new QuestPointerEventData(events.GetComponent<EventSystem>()) { pointerId = id, Ray = RayTo(point, normal), eligibleForClick = true };
    }
}
#endif
