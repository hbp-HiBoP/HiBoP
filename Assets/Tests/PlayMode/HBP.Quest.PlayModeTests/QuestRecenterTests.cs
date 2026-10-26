#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;
using HBP.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class QuestRecenterTests
    {
        [Test]
        public async Task HoldX_FillsForTwoUnscaledSecondsThenRecentersAllScenesAndOpenWindowsOnce()
        {
            using var f = new Fixture();
            float timeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                f.Press(true);
                Assert.That(f.Presenter.gameObject.activeSelf, Is.True);
                f.AssertUnmoved();
                await UniTask.Delay(TimeSpan.FromSeconds(.7), ignoreTimeScale: true);
                f.AssertUnmoved();
                Assert.That(f.Ring.fillAmount, Is.InRange(.2f, .7f));
                Assert.That(f.Message.text, Is.EqualTo("Hold X to recenter"));
                await UniTask.Delay(TimeSpan.FromSeconds(1.5), ignoreTimeScale: true);
                Assert.That(f.Presenter.gameObject.activeSelf, Is.False);
                foreach (var column in f.Columns)
                {
                    Assert.That(Vector3.Distance(column.position, new Vector3(0, -.12f, .65f)), Is.LessThan(.001f));
                    Assert.That(column.localScale, Is.EqualTo(Vector3.one * 1.5f));
                }

                Assert.That(f.Open.transform.position, Is.EqualTo(f.Open.Placement));
                Assert.That(f.Closed.transform.position, Is.EqualTo(Vector3.one * 5));
                f.Displace();
                await UniTask.Delay(TimeSpan.FromSeconds(2.1), ignoreTimeScale: true);
                f.AssertUnmoved();
                Assert.That(f.Presenter.gameObject.activeSelf, Is.False, "A held button must only trigger once.");
                f.Press(false);
                await UniTask.NextFrame();
                f.Press(true);
                Assert.That(f.Presenter.gameObject.activeSelf, Is.True, "A new press starts a fresh hold.");
                f.Press(false);
                await UniTask.NextFrame();
            }
            finally
            {
                Time.timeScale = timeScale;
            }
        }

        [TestCase("Release"), TestCase("LeftTracking"), TestCase("HeadTracking")]
        [TestCase("Focus"), TestCase("Pause"), TestCase("Disable"), TestCase("Cancel")]
        public async Task InterruptedHold_ClosesLoadingWithoutMovingAndRequiresAFreshPress(string interruption)
        {
            using var f = new Fixture();
            f.Press(true);
            await UniTask.Delay(TimeSpan.FromSeconds(.15), ignoreTimeScale: true);
            switch (interruption)
            {
                case "Release": f.Press(false); break;
                case "LeftTracking": f.Track(f.Left, false); break;
                case "HeadTracking": f.Track(f.Head, false); break;
                case "Focus": f.Input.SendMessage("OnApplicationFocus", false); break;
                case "Pause": f.Input.SendMessage("OnApplicationPause", true); break;
                case "Disable": f.Input.enabled = false; break;
                case "Cancel": f.Presenter.OnCancel.Invoke(); break;
            }

            await UniTask.NextFrame();
            await UniTask.NextFrame();
            Assert.That(f.Presenter.gameObject.activeSelf, Is.False);
            f.AssertUnmoved();
            f.Track(f.Left, true);
            f.Track(f.Head, true);
            f.Input.SendMessage("OnApplicationFocus", true);
            f.Input.SendMessage("OnApplicationPause", false);
            f.Input.enabled = true;
            await UniTask.Delay(TimeSpan.FromSeconds(2.1), ignoreTimeScale: true);
            f.AssertUnmoved();
            Assert.That(f.Presenter.gameObject.activeSelf, Is.False);
            f.Press(false);
            await UniTask.NextFrame();
            f.Press(true);
            Assert.That(f.Presenter.gameObject.activeSelf, Is.True);
            f.Press(false);
            await UniTask.NextFrame();
        }

        private sealed class Fixture : IDisposable
        {
            private const string Layout = "QuestRecenterController";
            private readonly GameObject root = new("Recenter input fixture");
            private readonly GameObject data = new("Inactive recenter scientific fixture");
            private readonly GameObject loading;
            private readonly QuestAnatomyView view;
            private readonly Mesh mesh = new();
            private readonly InputSettings.BackgroundBehavior background = InputSystem.settings.backgroundBehavior;
            private readonly InputSettings.EditorInputBehaviorInPlayMode editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            public readonly XRController Left;
            public readonly XRHMD Head;
            public readonly QuestAnatomyInput Input;
            public readonly QuestLoadingCircle Presenter;
            public readonly Image Ring;
            public readonly Text Message;
            public readonly QuestWindow Open, Closed;
            public readonly Transform[] Columns = new Transform[2];

            public Fixture()
            {
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.RegisterLayout("{\"name\":\"" + Layout + "\",\"extend\":\"XRController\",\"controls\":[{\"name\":\"primaryButton\",\"layout\":\"Button\",\"format\":\"FLT\"},{\"name\":\"triggerPressed\",\"layout\":\"Button\",\"format\":\"FLT\"}]}");
                Left = (XRController)InputSystem.AddDevice(Layout);
                Head = InputSystem.AddDevice<XRHMD>();
                InputSystem.SetDeviceUsage(Left, CommonUsages.LeftHand);
                loading = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Loading Manager.prefab"));
                Presenter = loading.GetComponentInChildren<QuestLoadingCircle>(true);
                var fields = new SerializedObject(Presenter);
                Ring = (Image)fields.FindProperty("progressRing").objectReferenceValue;
                Message = (Text)fields.FindProperty("information").objectReferenceValue;
                data.SetActive(false);
                view = data.AddComponent<QuestAnatomyView>();
                mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100);
                for (int i = 0; i < Columns.Length; i++)
                {
                    var scene = QuestSceneTestBinding.VisibleScene(data);
                    var columns = QuestSceneTestBinding.Attach(view, scene);
                    var wrapper = new GameObject("Column " + i);
                    wrapper.transform.SetParent(data.transform, false);
                    Columns[i] = wrapper.transform;
                    Columns[i].localScale = Vector3.one * 1.5f;
                    var brain = new GameObject("Brain", typeof(MeshFilter));
                    brain.transform.SetParent(wrapper.transform, false);
                    brain.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var column = wrapper.AddComponent<Column3DAnatomy>();
                    typeof(Column3D).GetProperty("BrainMesh").SetValue(column, brain);
                    var manipulator = wrapper.AddComponent<QuestAnatomyManipulator>();
                    manipulator.Bind(column);
                    var presentation = wrapper.AddComponent<QuestColumnPresentation>();
                    Set(presentation, "manipulator", manipulator);
                    QuestSceneTestBinding.Column(presentation, scene, column);
                    columns.Add(presentation);
                }

                root.SetActive(false);
                var head = Tracker("Head", QuestDevicePoseTracker.DeviceRole.Head);
                var left = Tracker("Left", QuestDevicePoseTracker.DeviceRole.LeftController);
                var windows = root.AddComponent<QuestWindowsManager>();
                windows.enabled = false;
                Set(windows, "head", head.transform);
                Input = root.AddComponent<QuestAnatomyInput>();
                Set(Input, "head", head);
                Set(Input, "left", left);
                Set(Input, "right", left);
                Set(Input, "view", view);
                Set(Input, "windows", windows);
                root.SetActive(true);
                Open = Window("Open");
                Closed = Window("Closed");
                Closed.Close();
                windows.Register(Open);
                windows.Register(Closed);
                Track(Head, true);
                Track(Left, true);
                Press(false);
                Displace();
            }

            private QuestDevicePoseTracker Tracker(string name, QuestDevicePoseTracker.DeviceRole role)
            {
                var tracker = new GameObject(name).AddComponent<QuestDevicePoseTracker>();
                tracker.gameObject.SetActive(false);
                tracker.transform.SetParent(root.transform, false);
                tracker.Configure(role, tracker.transform, null);
                tracker.gameObject.SetActive(true);
                return tracker;
            }

            private QuestWindow Window(string key)
            {
                var window = new GameObject(key, typeof(RectTransform), typeof(QuestWindow)).GetComponent<QuestWindow>();
                window.transform.SetParent(root.transform, false);
                Set(window, "key", key);
                return window;
            }

            public void Track(InputDevice device, bool tracked)
            {
                InputSystem.QueueDeltaStateEvent(device.GetChildControl<ButtonControl>("isTracked"), tracked ? (byte)1 : (byte)0);
                InputSystem.QueueDeltaStateEvent(device.GetChildControl<IntegerControl>("trackingState"), tracked ? 3 : 0);
                Tick();
            }

            public void Press(bool pressed)
            {
                InputSystem.QueueDeltaStateEvent(Left.GetChildControl<ButtonControl>("primaryButton"), pressed ? 1f : 0f);
                Tick();
            }

            private void Tick()
            {
                InputSystem.Update();
                foreach (var tracker in root.GetComponentsInChildren<QuestDevicePoseTracker>()) tracker.SendMessage("Update");
                if (Input.enabled) Input.SendMessage("LateUpdate");
            }

            public void Displace()
            {
                foreach (var column in Columns) column.position = Vector3.one * 5;
                Open.transform.position = Closed.transform.position = Vector3.one * 5;
            }

            public void AssertUnmoved()
            {
                foreach (var column in Columns) Assert.That(column.position, Is.EqualTo(Vector3.one * 5));
                Assert.That(Open.transform.position, Is.EqualTo(Vector3.one * 5));
                Assert.That(Closed.transform.position, Is.EqualTo(Vector3.one * 5));
            }

            private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

            public void Dispose()
            {
                Object.DestroyImmediate(root);
                QuestSceneTestBinding.Clear(view);
                Object.DestroyImmediate(data);
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(loading);
                InputSystem.RemoveDevice(Left);
                InputSystem.RemoveDevice(Head);
                InputSystem.RemoveLayout(Layout);
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
            }
        }
    }
}
#endif
