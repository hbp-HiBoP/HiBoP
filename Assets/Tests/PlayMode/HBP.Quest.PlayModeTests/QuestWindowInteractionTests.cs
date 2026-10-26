#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Quest;
using HBP.Transfer.Transport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class QuestWindowInteractionTests
    {
        private const string Folder = "Assets/Prefabs/Quest/UI/";
        internal static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [TestCase(true, true, true, QuestInteractionOwner.Anatomy)]
        [TestCase(true, true, false, QuestInteractionOwner.UI)]
        [TestCase(true, false, true, QuestInteractionOwner.UI)]
        [TestCase(false, true, false, QuestInteractionOwner.Anatomy)]
        [TestCase(false, false, true, QuestInteractionOwner.Empty)]
        public void TriggerCapture_SelectsOneOwnerAndNeverTransfersDuringHold(bool ui, bool brain, bool preferBrain, QuestInteractionOwner expected)
        {
            var capture = new QuestInteractionCapture();
            Assert.That(capture.Sample(true, true, ui, brain, preferBrain), Is.False, "Initially held triggers must first release.");
            capture.Sample(true, false, ui, brain, preferBrain);
            Assert.That(capture.Sample(true, true, ui, brain, preferBrain), Is.True);
            Assert.That(capture.Owner, Is.EqualTo(expected));
            capture.Sample(true, true, !ui, !brain, !preferBrain);
            Assert.That(capture.Owner, Is.EqualTo(expected));
            capture.Sample(true, false, false, false, false);
            Assert.That(capture.Owner, Is.EqualTo(QuestInteractionOwner.None));
        }

        [Test]
        public void TriggerCapture_TrackingLossAndCancellationRequireRelease()
        {
            var capture = new QuestInteractionCapture();
            capture.Sample(true, false, true, false, false);
            capture.Sample(true, true, true, false, false);
            capture.Sample(false, true, true, false, false);
            Assert.That(capture.Sample(true, true, true, false, false), Is.False);
            Assert.That(capture.Owner, Is.EqualTo(QuestInteractionOwner.None));
            capture.Sample(true, false, true, false, false);
            Assert.That(capture.Sample(true, true, true, false, false), Is.True);
            capture.Cancel();
            Assert.That(capture.Sample(true, true, true, false, false), Is.False);
        }

        [Test]
        public void Manager_ReusesClosedInstancesAndRecentersOnlyOpenUncapturedWindows()
        {
            var root = new GameObject("Quest manager fixture");
            root.SetActive(false);
            var head = new GameObject("Head");
            var first = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Quest Window.prefab"), root.transform).GetComponent<QuestWindow>();
            var second = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Quest Window.prefab"), root.transform).GetComponent<QuestWindow>();
            Set(second, "key", "Second");
            var manager = root.AddComponent<QuestWindowsManager>();
            Set(manager, "head", head.transform);
            Set(manager, "initialWindows", new[] { first, second });
            try
            {
                root.SetActive(true);
                manager.Open("Example");
                manager.Open("Second");
                first.Close();
                Assert.That(manager.Open("Example"), Is.SameAs(first));
                Assert.That(manager.Open("Example"), Is.SameAs(first));
                Assert.That(root.GetComponentsInChildren<QuestWindow>(true).Length, Is.EqualTo(2));
                second.Close();
                second.transform.position = Vector3.one * 4;
                first.transform.position = Vector3.one * 3;
                first.SetManipulated(true);
                manager.RecenterOpen();
                Assert.That(first.transform.position, Is.EqualTo(Vector3.one * 3));
                Assert.That(second.transform.position, Is.EqualTo(Vector3.one * 4));
                first.SetManipulated(false);
                first.SetPinned(true);
                manager.RecenterOpen();
                Assert.That(first.transform.position, Is.EqualTo(first.Placement));
                Object.DestroyImmediate(first.gameObject);
                Assert.That(manager.Find("Example"), Is.Null);
                Assert.That(manager.Find("Second"), Is.SameAs(second));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(head);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WindowDrag_KeepsItsOffsetExcludesTheOtherHandAndCancelsOnClose(bool pinned)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Quest Window.prefab"));
            var head = new GameObject("Head");
            var system = new GameObject("Window drag events", typeof(EventSystem));
            try
            {
                var window = root.GetComponent<QuestWindow>();
                window.SetPinned(pinned);
                var dragger = root.GetComponentInChildren<QuestWindowDragger>(true);
                dragger.Configure(window, head.transform);
                window.transform.position = Vector3.forward;
                var first = new QuestPointerEventData(system.GetComponent<EventSystem>()) { pointerId = -10, Ray = new Ray(Vector3.zero, Vector3.forward), pointerPressRaycast = new RaycastResult { worldPosition = Vector3.forward } };
                var second = new QuestPointerEventData(system.GetComponent<EventSystem>()) { pointerId = -11, Ray = new Ray(Vector3.left, Vector3.forward) };
                dragger.OnInitializePotentialDrag(first);
                Assert.That(first.useDragThreshold, Is.False);
                dragger.OnBeginDrag(first);
                dragger.OnBeginDrag(second);
                dragger.OnDrag(second);
                dragger.OnEndDrag(second);
                Assert.That(window.IsManipulated, Is.True);
                Assert.That(window.transform.position, Is.EqualTo(Vector3.forward));
                first.Ray = new Ray(Vector3.right * .2f, Vector3.forward);
                dragger.OnDrag(first);
                Assert.That(window.transform.position, Is.EqualTo(new Vector3(.2f, 0, 1)));
                Assert.That(Vector3.Dot(window.transform.up, Vector3.up), Is.EqualTo(1).Within(.001));
                window.Close();
                Assert.That(window.IsManipulated, Is.False);
                window.Open();
                first.Ray = new Ray(Vector3.right, Vector3.forward);
                dragger.OnDrag(first);
                Assert.That(window.transform.position, Is.EqualTo(new Vector3(.2f, 0, 1)), "Closing must clear the captured drag, including after reopening.");
                dragger.OnBeginDrag(first);
                Assert.That(window.IsManipulated, Is.True, "A new gesture can start after reopening.");
                dragger.enabled = false;
                Assert.That(window.IsManipulated, Is.False);
                dragger.enabled = true;
                first.Ray = new Ray(Vector3.right * 2, Vector3.forward);
                dragger.OnDrag(first);
                Assert.That(window.transform.position, Is.EqualTo(new Vector3(.2f, 0, 1)), "Re-enabling must not resume the old gesture.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(head);
                Object.DestroyImmediate(system);
            }
        }

        [Test]
        public void Follower_HasDeadZoneDelayPinAndHoverSuspension()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Quest Window.prefab"));
            var head = new GameObject("Head");
            try
            {
                var window = root.GetComponent<QuestWindow>();
                var follower = root.GetComponent<QuestWindowFollower>();
                follower.Configure(head.transform, window);
                follower.Recall();
                Vector3 initial = root.transform.position;
                var pin = root.transform.Find("Control Bar/Pin").GetComponent<Button>();
                var icon = pin.transform.Find("Pin Icon").GetComponent<Image>();
                var sprite = icon.sprite;
                var visibility = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Quest Scene Row.prefab").transform.Find("Header/Actions/Visibility").GetComponent<Toggle>();
                Assert.That(window.IsPinned, Is.True);
                Assert.That(pin.image.color, Is.EqualTo(visibility.graphic.color));
                head.transform.rotation = Quaternion.Euler(0, 90, 0);
                follower.Step(5, true);
                Assert.That(root.transform.position, Is.EqualTo(initial), "Windows start pinned.");
                pin.onClick.Invoke();
                Assert.That(window.IsPinned, Is.False);
                Assert.That(pin.image.color, Is.Not.EqualTo(visibility.graphic.color));
                Assert.That(icon.sprite, Is.SameAs(sprite));
                head.transform.rotation = Quaternion.Euler(0, 20, 0);
                follower.Step(2, true);
                Assert.That(root.transform.position, Is.EqualTo(initial));
                head.transform.rotation = Quaternion.Euler(0, 90, 0);
                follower.Step(.4f, true);
                Assert.That(root.transform.position, Is.EqualTo(initial));
                window.SetHovered(0, true);
                follower.Step(2, true);
                Assert.That(root.transform.position, Is.EqualTo(initial));
                window.SetHovered(0, false);
                follower.Step(.4f, true);
                Assert.That(root.transform.position, Is.EqualTo(initial), "Hover resets the delay.");
                follower.Step(.5f, true);
                Assert.That(root.transform.position.x, Is.GreaterThan(.1f));
                Assert.That(root.transform.position.x, Is.LessThan(window.Placement.z));
                window.SetPinned(true);
                Assert.That(pin.image.color, Is.EqualTo(visibility.graphic.color));
                Assert.That(icon.sprite, Is.SameAs(sprite));
                Vector3 pinned = root.transform.position;
                follower.Step(5, true);
                Assert.That(root.transform.position, Is.EqualTo(pinned));
                follower.Recall();
                Assert.That(root.transform.position.x, Is.EqualTo(window.Placement.z).Within(.001));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(head);
            }
        }

        [Test]
        public void ToolbarFollow_IgnoresHeadPitchAndStopsWhenTrackingIsLost()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Quest Window.prefab"));
            var head = new GameObject("Head");
            try
            {
                var follower = root.GetComponent<QuestWindowFollower>();
                root.GetComponent<QuestWindow>().SetPinned(false);
                Set(follower, "horizontalOnly", true);
                follower.Configure(head.transform, root.GetComponent<QuestWindow>());
                follower.Recall();
                Vector3 initial = root.transform.position;
                head.transform.rotation = Quaternion.Euler(70, 0, 0);
                follower.Step(3, true);
                Assert.That(root.transform.position, Is.EqualTo(initial));
                head.transform.rotation = Quaternion.Euler(0, 90, 0);
                follower.Step(3, false);
                Assert.That(root.transform.position, Is.EqualTo(initial));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(head);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ToolbarRail_FollowsAnArcWithoutRollWithHeadPitchAndRoll(bool tilted)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Quest Window.prefab"));
            var head = new GameObject("Head");
            try
            {
                var window = root.GetComponent<QuestWindow>();
                window.SetPinned(false);
                var follower = root.GetComponent<QuestWindowFollower>();
                Set(window, "placement", new Vector3(0, -.7f, .9f));
                Set(follower, "horizontalOnly", true);
                Set(follower, "tiltTowardHead", tilted);
                Set(follower, "exitAngle", 10f);
                Set(follower, "returnAngle", 3f);
                follower.Configure(head.transform, window);
                follower.Recall();
                Vector3 initial = root.transform.position;
                Quaternion initialRotation = root.transform.rotation;
                head.transform.rotation = Quaternion.Euler(35, 9, 40);
                follower.Step(2, true);
                Assert.That(root.transform.position, Is.EqualTo(initial), "The angular dead zone ignores pitch and roll.");
                Assert.That(Quaternion.Angle(root.transform.rotation, initialRotation), Is.LessThan(.01f));
                head.transform.rotation = Quaternion.Euler(35, 90, 40);
                follower.Step(.4f, true);
                Assert.That(root.transform.position, Is.EqualTo(initial), "The existing delay still applies.");
                for (int frame = 0; frame < 20; frame++)
                {
                    follower.Step(.1f, true);
                    Assert.That(Vector3.ProjectOnPlane(root.transform.position, Vector3.up).magnitude, Is.EqualTo(.9f).Within(.0001));
                    Assert.That(root.transform.position.y, Is.EqualTo(-.7f).Within(.0001));
                    Assert.That(root.transform.right.y, Is.EqualTo(0).Within(.0001), "The panel's left/right axis stays parallel to the floor.");
                    if (tilted)
                        Assert.That(Vector3.Angle(root.transform.forward, root.transform.position - head.transform.position), Is.LessThan(.1f), "The control surface faces the head throughout the arc.");
                    else
                        Assert.That(Vector3.Angle(root.transform.up, Vector3.up), Is.LessThan(.01f));
                }

                Assert.That(root.transform.position.x, Is.GreaterThan(.86f));
                Assert.That(root.transform.position.z, Is.GreaterThan(0), "Hysteresis stops before exact alignment.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(head);
            }
        }

        [TestCase(.2f, 0f)]
        [TestCase(0f, .2f)]
        public void ToolbarRail_FiltersSmallTranslationsAndFollowsLargerOnesWithoutTurning(float x, float y)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Quest Window.prefab"));
            var head = new GameObject("Head");
            try
            {
                var window = root.GetComponent<QuestWindow>();
                window.SetPinned(false);
                var follower = root.GetComponent<QuestWindowFollower>();
                Set(window, "placement", new Vector3(0, -.7f, .9f));
                Set(follower, "horizontalOnly", true);
                Set(follower, "tiltTowardHead", true);
                follower.Configure(head.transform, window);
                follower.Recall();
                Vector3 initial = root.transform.position;
                head.transform.position = new Vector3(.03f, .02f, 0);
                follower.Step(3, true);
                Assert.That(root.transform.position, Is.EqualTo(initial));
                head.transform.position = new Vector3(x, y, 0);
                follower.Step(.1f, true);
                Assert.That(Vector3.Distance(root.transform.position, initial), Is.GreaterThan(0));
                Assert.That(Vector3.Distance(root.transform.position, initial), Is.LessThan(.2f), "The center follows smoothly.");
                follower.Step(4, true);
                Assert.That(Vector3.Distance(root.transform.position, head.transform.position + window.Placement), Is.LessThan(.005f));
                Vector3 settled = root.transform.position;
                Quaternion settledRotation = root.transform.rotation;
                head.transform.position += new Vector3(.01f, .01f, 0);
                follower.Step(3, true);
                Assert.That(root.transform.position, Is.EqualTo(settled), "Micro movements are filtered again once the rail settles.");
                Assert.That(Quaternion.Angle(root.transform.rotation, settledRotation), Is.LessThan(.01f), "The inclined panel must not wobble toward unfiltered head positions.");
                window.SetHovered(0, true);
                head.transform.position += Vector3.one;
                follower.Step(3, true);
                Assert.That(root.transform.position, Is.EqualTo(settled), "Hover suspends translation too.");
                window.SetHovered(0, false);
                window.SetPinned(true);
                follower.Step(3, true);
                Assert.That(root.transform.position, Is.EqualTo(settled));
                window.SetPinned(false);
                follower.Step(3, false);
                Assert.That(root.transform.position, Is.EqualTo(settled));
                follower.Recall();
                Assert.That(Vector3.Distance(root.transform.position, head.transform.position + window.Placement), Is.LessThan(.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(head);
            }
        }

        [Test]
        public void ToolbarRail_AuthoredAngularAndCenterFollowStartTogetherWithMatchingSmoothing()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Quest Toolbar.prefab");
            var settings = new SerializedObject(source.GetComponent<QuestWindowFollower>());
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Quest Window.prefab"));
            var head = new GameObject("Head");
            try
            {
                var window = root.GetComponent<QuestWindow>();
                window.SetPinned(false);
                var follower = root.GetComponent<QuestWindowFollower>();
                Set(window, "placement", source.GetComponent<QuestWindow>().Placement);
                foreach (string field in new[] { "exitAngle", "returnAngle", "delay", "speed", "horizontalThreshold", "verticalThreshold" })
                    Set(follower, field, settings.FindProperty(field).floatValue);
                foreach (string field in new[] { "horizontalOnly", "tiltTowardHead" })
                    Set(follower, field, settings.FindProperty(field).boolValue);
                follower.Configure(head.transform, window);
                follower.Recall();
                Vector3 initial = root.transform.position;
                head.transform.SetPositionAndRotation(new Vector3(.02f, .02f, 0), Quaternion.Euler(0, 4, 0));
                follower.Step(.1f, true);
                Assert.That(root.transform.position, Is.EqualTo(initial), "Small rotations and translations both stay in the dead zone.");
                head.transform.SetPositionAndRotation(new Vector3(.1f, .1f, 0), Quaternion.Euler(0, 6, 0));
                follower.Step(.1f, true);
                float angularFraction = Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(root.transform.forward, Vector3.up), Vector3.up) / 6;
                float heightFraction = (root.transform.position.y - initial.y) / head.transform.position.y;
                Assert.That(angularFraction, Is.GreaterThan(0), "Angular follow starts on the first frame beyond 5 degrees.");
                Assert.That(heightFraction, Is.GreaterThan(0));
                Assert.That(angularFraction, Is.EqualTo(heightFraction).Within(.001f), "Rotation and rail translation must use the same smoothing.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(head);
            }
        }

        [Test]
        public void ToolbarRail_KeepsLastHeadingWhenLookingStraightUp()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Quest Window.prefab"));
            var head = new GameObject("Head");
            try
            {
                var follower = root.GetComponent<QuestWindowFollower>();
                root.GetComponent<QuestWindow>().SetPinned(false);
                Set(follower, "horizontalOnly", true);
                follower.Configure(head.transform, root.GetComponent<QuestWindow>());
                head.transform.rotation = Quaternion.Euler(0, 70, 0);
                follower.Recall();
                Vector3 initial = root.transform.position;
                head.transform.rotation = Quaternion.Euler(-90, 70, 40);
                follower.Step(3, true);
                Assert.That(Vector3.Distance(root.transform.position, initial), Is.LessThan(.0001f));
                follower.Recall();
                Assert.That(Vector3.Distance(root.transform.position, initial), Is.LessThan(.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(head);
            }
        }

        [Test]
        public async Task Toolbar_ConnectionCanReopenAndDisplayIsRegisteredWithoutDuplicates()
        {
            var root = new GameObject("Quest toolbar fixture");
            root.SetActive(false);
            var viewer = new GameObject("Preview camera", typeof(Camera));
            try
            {
                var camera = viewer.GetComponent<Camera>();
                camera.nearClipPlane = .01f;
                camera.fieldOfView = 65;
                var manager = root.AddComponent<QuestWindowsManager>();
                var connection = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Connection Window.prefab"), root.transform).GetComponent<QuestWindow>();
                var display = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Quest Display Window.prefab"), root.transform).GetComponent<QuestWindow>();
                var toolbar = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Quest Toolbar.prefab"), root.transform).GetComponent<QuestToolbar>();
                Set(manager, "head", viewer.transform);
                Set(manager, "uiCamera", camera);
                Set(manager, "initialWindows", new[] { connection, display, toolbar.GetComponent<QuestWindow>() });
                Set(toolbar, "windows", manager);
                root.SetActive(true);
                await UniTask.NextFrame();
                var card = connection.GetComponent<QuestStatusPanel>();
                card.ShowPairing(new PairingStatus("123456", TimeSpan.FromSeconds(90), 4, PairingPhase.Available), "192.168.1.18");
                connection.Close();
                card.ShowPaired("192.168.1.2", true);
                Assert.That(connection.IsOpen, Is.False);
                var fields = new SerializedObject(toolbar);
                var openConnection = (Button)fields.FindProperty("connectionButton").objectReferenceValue;
                openConnection.onClick.Invoke();
                Assert.That(connection.IsOpen, Is.True);
                Assert.That(connection.transform.Find("Background").gameObject.activeSelf, Is.True);
                openConnection.onClick.Invoke();
                Assert.That(connection.IsOpen, Is.False);
                openConnection.onClick.Invoke();
                ((Button)fields.FindProperty("displayButton").objectReferenceValue).onClick.Invoke();
                Assert.That(display.IsOpen, Is.True);
                Assert.That(root.GetComponentsInChildren<QuestWindow>(true).Length, Is.EqualTo(3));
                Assert.That(((Button)fields.FindProperty("cutsButton").objectReferenceValue).interactable, Is.False);
                var displayFields = new SerializedObject(display.GetComponent<QuestDisplayPanel>());
                Assert.That(((Button)displayFields.FindProperty("projection").objectReferenceValue).interactable, Is.False);
                Assert.That(display.transform.Find("Background/Content/Display Actions/Surface"), Is.Null);
                Assert.That(toolbar.transform.Find("Background/Recenter windows"), Is.Null);
                Assert.That(display.transform.Find("Background/Content/Patient Actions/Recenter brains"), Is.Null);
                card.ShowPairing(new PairingStatus("123456", TimeSpan.FromSeconds(90), 4, PairingPhase.Available), "192.168.1.18");
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                Capture(camera, "Quest Windows and Toolbar.png");
                camera.fieldOfView = 100;
                Capture(camera, "Quest Toolbar Peripheral.png"); // Wide-FOV preview; comfort still needs the headset.
                camera.fieldOfView = 65;
                camera.transform.rotation = Quaternion.LookRotation(toolbar.transform.position - camera.transform.position, Vector3.up);
                Capture(camera, "Quest Toolbar Desk.png");
                camera.transform.rotation = Quaternion.identity;
                connection.Close();
                var closedPosition = connection.transform.position;
                display.transform.position = Vector3.one * 3;
                manager.RecenterOpen();
                Assert.That(connection.transform.position, Is.EqualTo(closedPosition));
                Assert.That(display.transform.position, Is.Not.EqualTo(Vector3.one * 3));
            }
            catch (Exception exception)
            {
                Assert.Fail(exception.ToString());
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(viewer);
            }
        }

        [Test]
        public async Task DropdownPopup_UsesTrackedCameraAndAnOptionCanBeRaycastAndSelected()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Quest Theme Gallery.prefab"));
            var system = new GameObject("Quest UI test events", typeof(EventSystem));
            try
            {
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var canvas = root.GetComponent<Canvas>();
                var camera = root.GetComponentInChildren<Camera>(true);
                canvas.worldCamera = camera;
                var dropdown = root.GetComponentInChildren<QuestDropdown>(true);
                await UniTask.NextFrame(); // uGUI initializes its popup tween runner in Start.
                dropdown.Show();
                await UniTask.Yield();
                Canvas.ForceUpdateCanvases();
                var popup = dropdown.transform.Find("Dropdown List");
                Assert.That(popup, Is.Not.Null);
                Assert.That(popup.GetComponent<Canvas>().worldCamera, Is.SameAs(camera));
                Assert.That(popup.GetComponent<TrackedDeviceRaycaster>(), Is.Not.Null);
                var toggles = popup.GetComponentsInChildren<Toggle>().Where(t => t.gameObject.activeInHierarchy).ToArray();
                Assert.That(toggles.Length, Is.EqualTo(3));
                var chosen = toggles[1];
                Vector3 point = ((RectTransform)chosen.transform).TransformPoint(((RectTransform)chosen.transform).rect.center);
                var data = new QuestPointerEventData(system.GetComponent<EventSystem>()) { trackedDevicePosition = camera.transform.position, trackedDeviceOrientation = Quaternion.LookRotation(point - camera.transform.position) };
                var hits = new List<RaycastResult>();
                popup.GetComponent<TrackedDeviceRaycaster>().Raycast(data, hits);
                Assert.That(hits.Any(h => h.gameObject != null && h.gameObject.transform.IsChildOf(chosen.transform)), Is.True, "An option is targetable by the tracked ray.");
                ExecuteEvents.Execute(chosen.gameObject, data, ExecuteEvents.pointerClickHandler);
                Assert.That(dropdown.value, Is.EqualTo(1));
                dropdown.Hide();
                await UniTask.Delay(250, ignoreTimeScale: true);
                Capture(camera, "Quest Theme Gallery Runtime.png");
            }
            catch (Exception exception)
            {
                Assert.Fail(exception.ToString());
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(system);
            }
        }

        internal static void Capture(Camera camera, string file)
        {
            var texture = new RenderTexture(1200, 720, 24);
            var pixels = new Texture2D(1200, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0, 0, 1200, 720), 0, 0);
                pixels.Apply();
                string folder = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Logs/QuestUI"));
                System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, file), pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(texture);
            }
        }
    }
}
#endif
