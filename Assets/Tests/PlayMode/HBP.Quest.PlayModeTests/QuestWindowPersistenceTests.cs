#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class QuestWindowPersistenceTests
    {
        private QuestWindowLayoutTestScope layoutScope;

        [SetUp]
        public void SetUp() => layoutScope = new();

        [TearDown]
        public void TearDown() => layoutScope.Dispose();

        [Test]
        public void MovedWindow_ReopensAndRestartsInTheWorkspaceRatherThanTheReleaseGaze()
        {
            Pose relative;
            using (var f = new Fixture())
            {
                var window = f.Window("Any future window");
                f.Head.SetPositionAndRotation(new Vector3(1, 1.6f, 2), Quaternion.Euler(25, 30, 15));
                f.Manager.RecenterOpen();
                var workspace = new Pose(f.Head.position, Quaternion.Euler(0, 30, 0));
                f.Head.rotation = Quaternion.Euler(0, -55, 0); // Looking at the left-hand destination must not change the frame.
                Vector3 destination = workspace.position + workspace.rotation * new Vector3(-.7f, .15f, 1.2f);
                f.Move(window, destination);
                relative = new Pose(new Vector3(-.7f, .15f, 1.2f), Quaternion.Inverse(workspace.rotation) * window.transform.rotation);
                Assert.That(File.Exists(layoutScope.Path), Is.True, "The move is durable before the app quits.");
                window.Close();
                f.Head.position += Vector3.right * 3;
                window.Open(); // Direct opens must use the same common mechanism as toolbar opens.
                AssertPose(window.transform, new Pose(destination, workspace.rotation * relative.rotation));
                f.Manager.Open(window.Key);
                AssertPose(window.transform, new Pose(destination, workspace.rotation * relative.rotation));
            }

            using (var restarted = new Fixture())
            {
                var window = restarted.Window("Any future window");
                restarted.Head.SetPositionAndRotation(new Vector3(-2, 1.4f, 4), Quaternion.Euler(-30, 110, 0));
                restarted.Manager.RecenterOpen();
                var yaw = Quaternion.Euler(0, 110, 0);
                AssertPose(window.transform, new Pose(restarted.Head.position + yaw * relative.position, yaw * relative.rotation));
            }
        }

        [Test]
        public void Recenter_DefinesTheFrameForLaterMovesClosedWindowsAndLateRegistration()
        {
            using var f = new Fixture();
            var window = f.Window("First");
            var closed = f.Window("Closed");
            f.Manager.RecenterOpen();
            f.Move(window, new Vector3(-.6f, .2f, 1));
            f.Move(closed, new Vector3(.7f, -.1f, 1.3f));
            closed.Close();
            var closedBefore = closed.transform.position;
            f.Head.SetPositionAndRotation(new Vector3(4, 1.5f, 3), Quaternion.Euler(20, 90, 0));
            f.Manager.RecenterOpen();
            Quaternion yaw = Quaternion.Euler(0, 90, 0);
            Assert.That(Vector3.Distance(window.transform.position, f.Head.position + yaw * new Vector3(-.6f, .2f, 1)), Is.LessThan(.0001f));
            Assert.That(closed.transform.position, Is.EqualTo(closedBefore), "Closed windows are restored lazily.");
            closed.Open();
            Assert.That(Vector3.Distance(closed.transform.position, f.Head.position + yaw * new Vector3(.7f, -.1f, 1.3f)), Is.LessThan(.0001f));
            var future = f.Window("Not in any persistence list");
            Assert.That(Vector3.Distance(future.transform.position, f.Head.position + yaw * future.Placement), Is.LessThan(.0001f));
            var nextPosition = new Vector3(-.9f, .4f, 1.1f);
            f.Move(window, f.Head.position + yaw * nextPosition);
            Assert.That(new QuestWindowLayoutStore(layoutScope.Path).TryGet("First", out var stored), Is.True);
            Assert.That(Vector3.Distance(stored.position, nextPosition), Is.LessThan(.0001f));
        }

        [TestCase("Focus")]
        [TestCase("Close")]
        [TestCase("Disable")]
        public void InterruptedMove_DoesNotOverwriteTheLastCompletedPlacement(string interruption)
        {
            using var f = new Fixture();
            var window = f.Window("Window");
            f.Manager.RecenterOpen();
            f.Move(window, new Vector3(-.4f, .1f, 1));
            var saved = new QuestWindowLayoutStore(layoutScope.Path);
            saved.TryGetState(window.Key, out var before);
            var dragger = window.GetComponentInChildren<QuestWindowDragger>();
            var hands = (Array)typeof(QuestPointerInput).GetField("hands", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Pointer);
            var hand = hands.GetValue(0);
            var data = (QuestPointerEventData)hand.GetType().GetField("Data").GetValue(hand);
            f.BeginMove(window, dragger, data);
            data.Ray = new Ray(new Vector3(2, 1, 1) - Vector3.forward, Vector3.forward);
            dragger.OnDrag(data);
            data.pointerDrag = dragger.gameObject;
            data.dragging = true;
            switch (interruption)
            {
                case "Focus": f.Pointer.SendMessage("OnApplicationFocus", false); break;
                case "Close": window.Close(); break;
                case "Disable": dragger.enabled = false; break;
            }

            dragger.OnEndDrag(data);
            Assert.That(window.IsManipulated, Is.False);
            Assert.That(new QuestWindowLayoutStore(layoutScope.Path).TryGetState(window.Key, out var after), Is.True);
            Assert.That(after.Pose.position, Is.EqualTo(before.Pose.position));
            Assert.That(after.Size, Is.EqualTo(before.Size));
            Assert.That(after.ZoomPercent, Is.EqualTo(before.ZoomPercent));
        }

        [Test]
        public void AutomaticFollowing_DoesNotReplaceTheChosenLayoutAndResumeDoesNotRecenter()
        {
            using var f = new Fixture();
            var window = f.Window("Window");
            f.Manager.RecenterOpen();
            f.Move(window, new Vector3(-.3f, .1f, 1));
            var chosen = window.transform.position;
            window.SetPinned(false);
            string saved = File.ReadAllText(layoutScope.Path);
            f.Head.rotation = Quaternion.Euler(0, 100, 0);
            window.GetComponent<QuestWindowFollower>().Step(5, true);
            var followed = window.transform.position;
            Assert.That(Vector3.Distance(followed, chosen), Is.GreaterThan(.2f));
            f.Manager.SendMessage("OnApplicationPause", true);
            f.Manager.SendMessage("OnApplicationPause", false);
            f.Manager.SendMessage("Update");
            Assert.That(window.transform.position, Is.EqualTo(followed));
            Assert.That(File.ReadAllText(layoutScope.Path), Is.EqualTo(saved));
            window.Close();
            f.Manager.Open(window.Key);
            Assert.That(Vector3.Distance(window.transform.position, chosen), Is.LessThan(.0001f));
        }

        [Test]
        public void Startup_LeavesTrackedDeskPlacementUnusedUntilExplicitRecenter()
        {
            using var f = new Fixture();
            var window = f.Window("Window");
            var tracker = f.Head.gameObject.AddComponent<QuestDevicePoseTracker>();
            typeof(QuestDevicePoseTracker).GetProperty("IsTracked").SetValue(tracker, true);
            var before = window.transform.position;
            f.Head.SetPositionAndRotation(new Vector3(3, .8f, 4), Quaternion.Euler(80, 20, 90));
            f.Manager.SendMessage("Update");
            window.GetComponent<QuestWindowFollower>().Step(10, true);
            Assert.That(f.Manager.HasWorkspace, Is.False);
            Assert.That(window.transform.position, Is.EqualTo(before));
            Assert.That(window.GetComponent<Canvas>().enabled, Is.False);
            Assert.That(File.Exists(layoutScope.Path), Is.False);
            f.Manager.RecenterOpen();
            Assert.That(f.Manager.HasWorkspace, Is.True);
            Assert.That(window.GetComponent<Canvas>().enabled, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnpinnedMove_AfterWalkingDoesNotDriftByAddingTheHeadTranslationTwice(bool horizontalRail)
        {
            using var f = new Fixture();
            var window = f.Window("Window");
            var follower = window.GetComponent<QuestWindowFollower>();
            QuestWindowInteractionTests.Set(follower, "horizontalOnly", horizontalRail);
            window.SetPinned(false);
            f.Manager.RecenterOpen();
            f.Head.position = Vector3.right * 2;
            var destination = new Vector3(2, 0, 1);
            f.Move(window, destination);
            follower.Step(5, true);
            Assert.That(Vector3.Distance(window.transform.position, destination), Is.LessThan(.0001f), "Following must start from the released pose relative to the current head.");
            Assert.That(new QuestWindowLayoutStore(layoutScope.Path).TryGet(window.Key, out var stored), Is.True);
            Assert.That(Vector3.Distance(stored.position, destination), Is.LessThan(.0001f), "Persistence still uses the original workspace frame.");
        }

        [Test]
        public async Task CompleteWindowState_RestoresClosedWindowSizeZoomAndPinAfterRestartAndRecenter()
        {
            Vector2 size;
            Pose pose;
            using (var f = new Fixture())
            {
                var window = f.Window("Future complete window");
                f.Manager.RecenterOpen();
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                window.GetComponentInChildren<QuestWindowContentZoom>(true).SetZoomPercent(130);
                f.Resize(window, new Vector2(420, 260));
                size = ((RectTransform)window.transform).rect.size;
                window.SetPinned(false);
                window.Close();
                var store = new QuestWindowLayoutStore(layoutScope.Path);
                Assert.That(store.TryGetState(window.Key, out var state), Is.True);
                pose = state.Pose;
                Assert.That(state.Size, Is.EqualTo(size));
                Assert.That(state.ZoomPercent, Is.EqualTo(130));
                Assert.That(state.Pinned, Is.False);
                Assert.That(state.Open, Is.False);
            }

            string saved = File.ReadAllText(layoutScope.Path);
            using (var f = new Fixture())
            {
                var window = f.Window("Future complete window");
                var zoom = window.GetComponentInChildren<QuestWindowContentZoom>(true);
                Assert.That(window.IsOpen, Is.False);
                Assert.That(window.IsPinned, Is.False);
                Assert.That(zoom.ZoomPercent, Is.EqualTo(130), "Hidden windows retain their pending zoom before layout can measure its baseline.");
                Assert.That(File.ReadAllText(layoutScope.Path), Is.EqualTo(saved), "Restoration must not write defaults over saved settings.");
                f.Head.SetPositionAndRotation(new Vector3(2, 1.6f, 3), Quaternion.Euler(0, 40, 0));
                f.Manager.RecenterOpen();
                window.Open();
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                Assert.That(Vector2.Distance(((RectTransform)window.transform).rect.size, size), Is.LessThan(.05f));
                Assert.That(zoom.Factor, Is.EqualTo(1.3f));
                Assert.That(window.IsPinned, Is.False);
                Assert.That(LayoutUtility.GetMinSize((RectTransform)window.transform, 0), Is.GreaterThan(0), "Restoring zoom must still measure unzoomed layout constraints.");
                AssertPose(window.transform, new Pose(f.Head.position + f.Head.rotation * pose.position, f.Head.rotation * pose.rotation));
                f.Head.position += Vector3.right * 2;
                f.Manager.RecenterOpen();
                Assert.That(Vector2.Distance(((RectTransform)window.transform).rect.size, size), Is.LessThan(.05f));
                Assert.That(zoom.ZoomPercent, Is.EqualTo(130));
                Assert.That(window.IsPinned, Is.False);
            }
        }

        [Test]
        public void SettingsBeforeTracking_SaveWithoutUsingTheDeskPoseAndRestoreStackingOrder()
        {
            using (var f = new Fixture())
            {
                var first = f.Window("First");
                var second = f.Window("Second");
                var third = f.Window("Third");
                f.Head.position = new Vector3(4, .7f, 2);
                first.GetComponentInChildren<QuestWindowContentZoom>(true).SetZoomPercent(70);
                first.SetPinned(false);
                first.Select();
                second.Close();
                Assert.That(f.Manager.HasWorkspace, Is.False);
                var store = new QuestWindowLayoutStore(layoutScope.Path);
                Assert.That(store.TryGetState(first.Key, out var state), Is.True);
                Assert.That(state.Pose.position, Is.EqualTo(first.Placement));
                Assert.That(state.ZoomPercent, Is.EqualTo(70));
                Assert.That(state.Pinned, Is.False);
            }

            using (var f = new Fixture())
            {
                var third = f.Window("Third");
                var second = f.Window("Second");
                var first = f.Window("First");
                Assert.That(first.IsOpen, Is.True);
                Assert.That(second.IsOpen, Is.False);
                Assert.That(first.IsPinned, Is.False);
                Assert.That(second.SortingOrder, Is.LessThan(third.SortingOrder));
                Assert.That(third.SortingOrder, Is.LessThan(first.SortingOrder));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task CancelledResize_DoesNotLeakItsDimensionsIntoLaterZoomOrPinSaves(bool disable)
        {
            using var f = new Fixture();
            var window = f.Window("Window");
            f.Manager.RecenterOpen();
            await UniTask.NextFrame();
            Canvas.ForceUpdateCanvases();
            f.Resize(window, new Vector2(300, 200));
            var size = ((RectTransform)window.transform).rect.size;
            var data = f.BeginResize(window, new Vector2(200, 150), out var resizer);
            if (disable) resizer.enabled = false;
            else data.Cancelled = true;
            resizer.OnEndDrag(data);
            window.SetPinned(false);
            window.GetComponentInChildren<QuestWindowContentZoom>(true).SetZoomPercent(120);
            Assert.That(new QuestWindowLayoutStore(layoutScope.Path).TryGetState(window.Key, out var state), Is.True);
            Assert.That(state.Size, Is.EqualTo(size));
            Assert.That(state.ZoomPercent, Is.EqualTo(120));
            f.Manager.RecenterOpen();
            Assert.That(Vector2.Distance(((RectTransform)window.transform).rect.size, size), Is.LessThan(.05f));
        }

        [Test]
        public async Task ConnectionWindow_KeepsUserSizeWhenContentChangesAndAfterRestart()
        {
            Vector2 size;
            using (var f = new Fixture())
            {
                var window = f.Window("Connection size", "Quest Connection Window");
                f.Manager.RecenterOpen();
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                f.Resize(window, new Vector2(300, 350));
                size = ((RectTransform)window.transform).rect.size;
                window.GetComponent<QuestStatusPanel>().ShowPaired("Synthetic Desktop", true);
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                Assert.That(Vector2.Distance(((RectTransform)window.transform).rect.size, size), Is.LessThan(.05f), "Automatic fitting must not replace a deliberately resized window.");
                window.Close();
            }

            using (var f = new Fixture())
            {
                var window = f.Window("Connection size", "Quest Connection Window");
                f.Manager.RecenterOpen();
                window.Open();
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                Assert.That(Vector2.Distance(((RectTransform)window.transform).rect.size, size), Is.LessThan(.05f));
            }
        }

        [Test]
        public void CatalogWindows_RestoreOpenInstancesWithoutWritingOrCreatingClosedWindows()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Window.prefab").GetComponent<QuestWindow>();
            var definitions = new[] { new QuestWindowsManager.Definition { Key = prefab.Key, Prefab = prefab } };
            var store = new QuestWindowLayoutStore(layoutScope.Path);
            store.SetState(prefab.Key, new QuestWindowLayoutStore.WindowState(Pose.identity, zoomPercent: 80, pinned: false, open: true));
            store.Save();
            string saved = File.ReadAllText(layoutScope.Path);
            using (var f = new Fixture(definitions))
            {
                var window = f.Manager.Find(prefab.Key);
                Assert.That(window, Is.Not.Null, "Future catalog windows opened last session must be recreated through their existing prefab definition.");
                Assert.That(window.IsOpen, Is.True);
                Assert.That(window.IsPinned, Is.False);
                Assert.That(window.GetComponentInChildren<QuestWindowContentZoom>(true).ZoomPercent, Is.EqualTo(80));
                Assert.That(File.ReadAllText(layoutScope.Path), Is.EqualTo(saved));
            }

            store.SetState(prefab.Key, new QuestWindowLayoutStore.WindowState(Pose.identity, open: false));
            store.Save();
            using (var f = new Fixture(definitions)) Assert.That(f.Manager.Find(prefab.Key), Is.Null);
        }

        private static void AssertPose(Transform actual, Pose expected)
        {
            Assert.That(Vector3.Distance(actual.position, expected.position), Is.LessThan(.0001f));
            Assert.That(Quaternion.Angle(actual.rotation, expected.rotation), Is.LessThan(.02f));
        }

        private sealed class Fixture : IDisposable
        {
            private readonly GameObject root = new("Workspace fixture");
            private readonly GameObject events = new("Workspace events", typeof(EventSystem));
            public readonly QuestWindowsManager Manager;
            public readonly Transform Head;
            public readonly QuestPointerInput Pointer;

            public Fixture(QuestWindowsManager.Definition[] catalogue = null)
            {
                root.SetActive(false);
                Head = new GameObject("Head").transform;
                Head.SetParent(root.transform, false);
                Manager = root.AddComponent<QuestWindowsManager>();
                QuestWindowInteractionTests.Set(Manager, "head", Head);
                if (catalogue != null) QuestWindowInteractionTests.Set(Manager, "catalogue", catalogue);
                root.SetActive(true);
                Pointer = events.AddComponent<QuestPointerInput>();
            }

            public QuestWindow Window(string key, string prefab = "Quest Window")
            {
                var window = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/" + prefab + ".prefab"), root.transform).GetComponent<QuestWindow>();
                QuestWindowInteractionTests.Set(window, "key", key);
                Manager.Register(window);
                return window;
            }

            public QuestPointerEventData BeginResize(QuestWindow window, Vector2 growth, out QuestWindowResizer resizer)
            {
                resizer = Array.Find(window.GetComponentsInChildren<QuestWindowResizer>(true), handle => handle.Edge == QuestWindowResizer.WindowEdge.BottomRight);
                var rect = (RectTransform)window.transform;
                Vector3 start = rect.TransformPoint(new Vector3(rect.rect.xMax, rect.rect.yMin));
                var data = new QuestPointerEventData(events.GetComponent<EventSystem>()) { pointerId = -10, Ray = new Ray(start - rect.forward, rect.forward) };
                resizer.OnBeginDrag(data);
                Vector3 destination = start + rect.TransformVector(new Vector3(growth.x, -growth.y));
                data.Ray = new Ray(destination - rect.forward, rect.forward);
                resizer.OnDrag(data);
                return data;
            }

            public void Resize(QuestWindow window, Vector2 growth)
            {
                var data = BeginResize(window, growth, out var resizer);
                resizer.OnEndDrag(data);
            }

            public void BeginMove(QuestWindow window, QuestWindowDragger dragger, QuestPointerEventData data)
            {
                data.Ray = new Ray(window.transform.position - Vector3.forward, Vector3.forward);
                data.pointerPressRaycast = new RaycastResult { worldPosition = window.transform.position };
                dragger.OnBeginDrag(data);
            }

            public void Move(QuestWindow window, Vector3 destination)
            {
                var dragger = window.GetComponentInChildren<QuestWindowDragger>();
                var data = new QuestPointerEventData(events.GetComponent<EventSystem>()) { pointerId = -10 };
                BeginMove(window, dragger, data);
                data.Ray = new Ray(destination - Vector3.forward, Vector3.forward);
                dragger.OnDrag(data);
                dragger.OnEndDrag(data);
            }

            public void Dispose()
            {
                Object.DestroyImmediate(events);
                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
