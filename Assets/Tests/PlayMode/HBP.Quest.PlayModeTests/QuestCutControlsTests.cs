#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Cut = HBP.Core.Object3D.Cut;
using Site = HBP.Core.Object3D.Site;

namespace HBP.Tests.Quest
{
    public class QuestCutControlsTests
    {
        [Test]
        public async Task ProductionPointer_RayHitsRowControlsAndTriggerChangesTheirCut()
        {
            using var f = new Fixture();
            f.Panel.Refresh();
            f.Window.Open(); // Production creates rows while this window is still closed.
            using var input = new PointerFixture(f);
            await UniTask.NextFrame();
            Canvas.ForceUpdateCanvases();
            var row = f.Panel.Rows.Single(r => r.Cut == f.Scene.Cuts[0]);
            var plus = row.transform.Find("Plus").GetComponent<Button>();
            input.AimAt((RectTransform)plus.transform);
            Assert.That(input.Target.GetComponentInParent<Button>(), Is.SameAs(plus), "Actual ray target: " + input.Target.name + " at " + input.Target.transform.parent.name + ". The production ray must reach the visible row button, not an overlapping graphic.");
            float before = row.Cut.Position;
            input.Press(true);
            input.Track(plus.transform.TransformPoint(((RectTransform)plus.transform).rect.center) - Vector3.forward * .6f + Vector3.right * .02f);
            input.Press(false);
            Assert.That(row.Cut.Position, Is.EqualTo(before + .002f).Within(1e-6));
            var minus = row.transform.Find("Minus").GetComponent<Button>();
            input.Click((RectTransform)minus.transform);
            Assert.That(row.Cut.Position, Is.EqualTo(before).Within(1e-6));
            var slider = row.transform.Find("Position").GetComponent<Slider>();
            input.AimAt((RectTransform)slider.transform);
            input.Press(true);
            input.Track(slider.transform.TransformPoint(((RectTransform)slider.transform).rect.center) - Vector3.forward * .6f + Vector3.right * .15f);
            input.Press(false);
            Assert.That(row.Cut.Position, Is.GreaterThan(.6f), "The real slider drag must modify the cut.");
            input.Click((RectTransform)row.transform.Find("Custom/X"));
            Assert.That(f.Keypad.IsOpen, Is.True);
            await UniTask.NextFrame(); // Register and lay out the newly activated modal graphics.
            Canvas.ForceUpdateCanvases();
            var keys = f.Window.transform.Find("Background/Keypad");
            foreach (string key in new[] { "Clear", "1", "Decimal", "5", "Apply" })
            {
                input.AimAt((RectTransform)keys.Find(key));
                Assert.That(input.Target != null ? input.Target.GetComponentInParent<Button>() : null, Is.SameAs(keys.Find(key).GetComponent<Button>()), "Numeric key first hit: " + key + " => " + (input.Target != null ? input.Target.name : "none"));
                input.Press(true);
                input.Press(false);
            }

            Assert.That(f.Keypad.IsOpen, Is.False);
            input.Click((RectTransform)row.transform.Find("Custom/Apply"));
            Assert.That(row.Cut.Normal.x, Is.EqualTo(1.5f));
            var dropdown = row.transform.Find("Orientation").GetComponent<QuestDropdown>();
            input.Click((RectTransform)dropdown.transform);
            await UniTask.NextFrame();
            Canvas.ForceUpdateCanvases();
            var options = dropdown.transform.Find("Dropdown List").GetComponentsInChildren<Toggle>().Where(t => t.gameObject.activeInHierarchy).ToArray();
            input.AimAt((RectTransform)options[0].transform);
            Assert.That(input.Target != null ? input.Target.GetComponentInParent<Toggle>() : null, Is.SameAs(options[0]), "Dropdown first hit: " + (input.Target != null ? input.Target.name + " / " + input.Target.transform.parent.name + " / " + input.Target.transform.parent.parent.name : "none"));
            input.Press(true);
            input.Press(false);
            Assert.That(row.Cut.Orientation, Is.EqualTo(CutOrientation.Axial));
            await UniTask.Delay(250, ignoreTimeScale: true);
            row.Refresh();
            input.Click((RectTransform)row.transform.Find("Flip"));
            Assert.That(row.Cut.Flip, Is.True);
            // Remove uses the actual domain command, without the fixture's unwired column renderers.
            f.Scene.Columns.Clear();
            var removed = row.Cut;
            input.Click((RectTransform)row.transform.Find("Remove"));
            Assert.That(f.Scene.Cuts.Contains(removed), Is.False);
            f.Panel.Refresh();
            Assert.That(f.Panel.Rows.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task ProductionPointer_ScrollsRowBackgroundAndClosesDropdownFromBlocker()
        {
            using var f = new Fixture();
            f.Window.Open();
            f.Panel.Refresh();
            using var input = new PointerFixture(f);
            await UniTask.NextFrame();
            Canvas.ForceUpdateCanvases();
            var row = f.Panel.Rows.Single(r => r.Cut == f.Scene.Cuts[0]);
            var scroll = f.Window.GetComponentInChildren<ScrollRect>();
            Vector3 start = row.transform.TransformPoint(new Vector3(-450, 0, 0));
            input.Track(start - Vector3.forward * .6f);
            input.Press(true);
            input.Track(start + Vector3.up * .04f - Vector3.forward * .6f);
            input.Track(start + Vector3.up * .08f - Vector3.forward * .6f);
            input.Press(false);
            Assert.That(scroll.verticalNormalizedPosition, Is.LessThan(.95f), "A row background still delegates dragging to the scroll view.");
            scroll.verticalNormalizedPosition = 1;
            Canvas.ForceUpdateCanvases();
            var dropdown = row.transform.Find("Orientation").GetComponent<QuestDropdown>();
            input.Click((RectTransform)dropdown.transform);
            await UniTask.NextFrame();
            Assert.That(dropdown.transform.Find("Dropdown List"), Is.Not.Null);
            input.Click((RectTransform)f.Window.transform.Find("Background/Heading"));
            await UniTask.Delay(250, ignoreTimeScale: true);
            Assert.That(dropdown.transform.Find("Dropdown List"), Is.Null, "The popup blocker must intercept clicks outside the options.");
            Assert.That(row.Cut.Orientation, Is.EqualTo(CutOrientation.Custom));
        }

        [Test]
        public async Task ProductionPointer_GrabsPlaneAwayFromCenterAndConstrainsMotionInItsOwnColumn()
        {
            using var f = new Fixture();
            f.Window.Open();
            f.Panel.Refresh();
            using var input = new PointerFixture(f);
            await UniTask.NextFrame();
            f.Handles.Refresh();
            var gizmo = f.Handles.Gizmos.First(g => g.Cut == f.Scene.Cuts[0]);
            Assert.That(gizmo.transform.Find("Handle"), Is.Null);
            var outline = gizmo.transform.Find("Outline").GetComponent<LineRenderer>();
            Assert.That(outline.positionCount, Is.EqualTo(4));
            for (int i = 0; i < 4; i++) Assert.That(Vector3.Dot(gizmo.Frame.InverseTransformPoint(outline.GetPosition(i)) - gizmo.Cut.Point, gizmo.Cut.Normal.normalized), Is.EqualTo(0).Within(1e-4));
            Vector3 start = Vector3.Lerp(outline.GetPosition(0), outline.GetPosition(2), .25f);
            Assert.That(Vector3.Distance(start, gizmo.PlaneCenter), Is.GreaterThan(.035f), "Contact must cover the surface, not just a new central handle.");
            input.Track(start);
            input.Press(true);
            Assert.That(f.Handles.IsCaptured(0), Is.True, "Plane contact must win over the nearby brain in the real trigger arbiter.");
            float before = gizmo.Cut.Position;
            float span = f.Scene.GetCutPositionGeometry(gizmo.Cut).Span;
            input.Track(start + gizmo.Frame.TransformVector(gizmo.Cut.Normal.normalized * span * .1f));
            Assert.That(gizmo.Cut.Position, Is.EqualTo(before + .1f).Within(1e-5));
            input.Press(false);
            Assert.That(f.Handles.IsCaptured(0), Is.False);
        }

        [Test]
        public async Task IntegratedGestures_CloseCutsGrabBrainLoseTrackingAndReturnToUIWithoutTransferringAHeldTrigger()
        {
            using var f = new Fixture();
            f.Window.Open();
            f.Panel.Refresh();
            using var input = new PointerFixture(f);
            await UniTask.NextFrame();
            f.Handles.Refresh();
            var gizmo = f.Handles.Gizmos.First();
            input.Track(gizmo.PlaneCenter);
            input.Press(false);
            input.Press(true);
            Assert.That(f.Handles.IsCaptured(0), Is.True);
            Assert.That(input.AnatomyCaptured, Is.False);

            f.Window.Close();
            input.Track(gizmo.Column.Manipulator.GrabCenter);
            Assert.That(f.Handles.IsCaptured(0), Is.False);
            Assert.That(input.AnatomyCaptured, Is.False, "Closing cut aids cannot transfer the held trigger to the brain.");
            input.Press(false);
            input.Press(true);
            Assert.That(input.AnatomyCaptured, Is.True);

            f.Window.Open();
            input.Track(gizmo.PlaneCenter);
            Assert.That(input.AnatomyCaptured, Is.True, "Opening cut aids cannot replace an existing brain capture.");
            Assert.That(f.Handles.IsCaptured(0), Is.False);
            input.LoseTracking();
            Assert.That(input.AnatomyCaptured, Is.False);
            input.Track(gizmo.PlaneCenter);
            Assert.That(f.Handles.IsCaptured(0), Is.False, "Tracking recovery requires a fresh trigger press.");
            input.Press(false);
            input.Press(true);
            Assert.That(f.Handles.IsCaptured(0), Is.True);

            var plus = f.Panel.Rows.First(r => r.Cut == gizmo.Cut).transform.Find("Plus");
            await UniTask.NextFrame(); // Reopened Canvas graphics must register before testing their ray hits.
            input.AimAt((RectTransform)plus);
            Assert.That(f.Handles.IsCaptured(0), Is.True, "A cut capture stays a cut when its ray reaches UI.");
            input.Press(false);
            Assert.That(f.Handles.IsCaptured(0), Is.False);
            Assert.That(input.Target != null ? input.Target.GetComponentInParent<Button>() : null, Is.SameAs(plus.GetComponent<Button>()));
            float before = gizmo.Cut.Position;
            input.Click((RectTransform)plus);
            Assert.That(gizmo.Cut.Position, Is.EqualTo(before + .002f).Within(1e-6));
            Assert.That(input.AnatomyCaptured, Is.False);
        }

        private sealed class PointerFixture : IDisposable
        {
            private readonly GameObject root = new("Production cut pointer fixture");
            private readonly XRController controller;
            private readonly XRHMD hmd;
            private readonly QuestDevicePoseTracker head, left;
            private readonly QuestPointerInput pointer;
            private readonly InputSettings.BackgroundBehavior background;
            private readonly InputSettings.EditorInputBehaviorInPlayMode editor;
            public GameObject Target => Data.pointerCurrentRaycast.gameObject;
            public bool AnatomyCaptured => pointer.AllowsAnatomy(true);

            private QuestPointerEventData Data
            {
                get
                {
                    var hands = (Array)typeof(QuestPointerInput).GetField("hands", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pointer);
                    var hand = hands.GetValue(0);
                    return (QuestPointerEventData)hand.GetType().GetField("Data").GetValue(hand);
                }
            }

            public PointerFixture(Fixture f)
            {
                background = InputSystem.settings.backgroundBehavior;
                editor = InputSystem.settings.editorInputBehaviorInPlayMode;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.RegisterLayout("{\"name\":\"QuestCutTestController\",\"extend\":\"XRController\",\"controls\":[{\"name\":\"isTracked\",\"format\":\"FLT\",\"sizeInBits\":32,\"bit\":0},{\"name\":\"triggerPressed\",\"layout\":\"Button\",\"format\":\"FLT\"},{\"name\":\"pointerPosition\",\"layout\":\"Vector3\"},{\"name\":\"pointerRotation\",\"layout\":\"Quaternion\"}]}");
                controller = (XRController)InputSystem.AddDevice("QuestCutTestController");
                hmd = InputSystem.AddDevice<XRHMD>();
                InputSystem.SetDeviceUsage(controller, CommonUsages.LeftHand);
                head = Tracker("Head", QuestDevicePoseTracker.DeviceRole.Head);
                left = Tracker("Left", QuestDevicePoseTracker.DeviceRole.LeftController);
                head.transform.position = new Vector3(0, 0, -1.3f); // Head tracking is normally applied by TrackedPoseDriver.
                f.Camera.transform.SetParent(head.transform, false);
                f.Window.GetComponent<Canvas>().worldCamera = f.Camera;
                InputSystem.QueueDeltaStateEvent(hmd.centerEyePosition, new Vector3(0, 0, -1.3f));
                InputSystem.QueueDeltaStateEvent(hmd.centerEyeRotation, Quaternion.identity);
                InputSystem.QueueDeltaStateEvent(hmd.isTracked, (byte)1);
                InputSystem.QueueDeltaStateEvent(hmd.trackingState, 3);
                var ui = new GameObject("Universal pointer", typeof(EventSystem));
                ui.transform.SetParent(root.transform);
                ui.SetActive(false);
                pointer = ui.AddComponent<QuestPointerInput>();
                QuestWindowInteractionTests.Set(pointer, "policy", f.policy);
                QuestWindowInteractionTests.Set(pointer, "head", head);
                QuestWindowInteractionTests.Set(pointer, "left", left);
                QuestWindowInteractionTests.Set(pointer, "anatomy", f.View);
                QuestWindowInteractionTests.Set(pointer, "cuts", f.Handles);
                QuestWindowInteractionTests.Set(pointer, "trackingOrigin", root.transform);
                QuestWindowInteractionTests.Set(f.Handles, "pointer", pointer);
                ui.SetActive(true);
            }

            private QuestDevicePoseTracker Tracker(string name, QuestDevicePoseTracker.DeviceRole role)
            {
                var node = new GameObject(name);
                node.transform.SetParent(root.transform);
                node.SetActive(false);
                var tracker = node.AddComponent<QuestDevicePoseTracker>();
                tracker.Configure(role, node.transform, null);
                node.SetActive(true);
                return tracker;
            }

            public void AimAt(RectTransform target) => Track(target.TransformPoint(target.rect.center) - Vector3.forward * .6f);

            public void Track(Vector3 point)
            {
                InputSystem.QueueDeltaStateEvent(controller.devicePosition, point);
                InputSystem.QueueDeltaStateEvent(controller.deviceRotation, Quaternion.identity);
                InputSystem.QueueDeltaStateEvent(controller.GetChildControl<Vector3Control>("pointerPosition"), point);
                InputSystem.QueueDeltaStateEvent(controller.GetChildControl<QuaternionControl>("pointerRotation"), Quaternion.Euler(0, 0, 1));
                InputSystem.QueueDeltaStateEvent(controller.isTracked, 1f);
                InputSystem.QueueDeltaStateEvent(controller.trackingState, 3);
                Tick();
            }

            public void Press(bool pressed)
            {
                InputSystem.QueueDeltaStateEvent(controller.GetChildControl<ButtonControl>("triggerPressed"), pressed ? 1f : 0f);
                Tick();
            }

            public void LoseTracking()
            {
                InputSystem.QueueDeltaStateEvent(controller.isTracked, 0f);
                InputSystem.QueueDeltaStateEvent(controller.trackingState, 0);
                Tick();
            }

            public void Click(RectTransform target)
            {
                AimAt(target);
                Press(true);
                Press(false);
            }

            private void Tick()
            {
                InputSystem.Update();
                head.SendMessage("Update");
                left.SendMessage("Update");
                Canvas.ForceUpdateCanvases();
                pointer.Process();
            }

            public void Dispose()
            {
                Object.DestroyImmediate(root);
                InputSystem.RemoveDevice(controller);
                InputSystem.RemoveDevice(hmd);
                InputSystem.RemoveLayout("QuestCutTestController");
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            }
        }

        [Test]
        public async Task CutRows_ExposeEveryCutRefreshWithoutEchoAndProvideControllerNumericDrafts()
        {
            using var f = new Fixture();
            f.Window.Open();
            f.Panel.Refresh();
            await UniTask.NextFrame();
            Assert.That(f.Panel.Rows.Count, Is.EqualTo(3));
            var row = f.Panel.Rows.Single(r => r.Cut == f.Scene.Cuts[1]);
            int intents = 0;
            f.Scene.CutDefinitionRouter = (cut, orientation, flip, position, normal) =>
            {
                intents++;
                return false;
            };
            row.transform.Find("Plus").GetComponent<Button>().onClick.Invoke();
            Assert.That(row.Cut.Position, Is.EqualTo(.502f).Within(1e-6));
            row.transform.Find("Position").GetComponent<Slider>().value = .7f;
            Assert.That(row.Cut.Position, Is.EqualTo(.7f));
            Assert.That(intents, Is.EqualTo(2));
            row.Cut.Position = .25f;
            row.Cut.Normal = new Vector3(0, 1, 1); // Incoming scientific state.
            row.Refresh();
            Assert.That(row.transform.Find("Position").GetComponent<Slider>().value, Is.EqualTo(.25f));
            Assert.That(intents, Is.EqualTo(2), "Remote refresh never invokes controls.");
            Assert.That(row.transform.Find("Flip").gameObject.activeSelf, Is.False, "Desktop hides flip for Custom.");
            row.transform.Find("Custom/X").GetComponent<Button>().onClick.Invoke();
            Assert.That(f.Keypad.IsOpen, Is.True);
            f.Keypad.Enter("Clear");
            f.Keypad.Enter("1");
            f.Keypad.Enter(".");
            f.Keypad.Enter("5");
            f.Keypad.Apply();
            Assert.That(row.Cut.Normal, Is.EqualTo(new Vector3(0, 1, 1)), "A numeric draft is local until the complete normal is applied.");
            row.ApplyNormal();
            Assert.That(row.Cut.Normal, Is.EqualTo(new Vector3(1.5f, 1, 1)));
            Assert.That(intents, Is.EqualTo(3));
            foreach (string axis in new[] { "X", "Y", "Z" })
            {
                row.transform.Find("Custom/" + axis).GetComponent<Button>().onClick.Invoke();
                f.Keypad.Enter("Clear");
                f.Keypad.Enter("0");
                f.Keypad.Apply();
            }

            row.ApplyNormal();
            Assert.That(intents, Is.EqualTo(3), "A zero normal must not publish.");
            Assert.That(row.transform.Find("Message").GetComponent<Text>().text, Is.Not.Empty);
            Assert.That(QuestCutCommands.TryNormal("NaN", "1", "0", out _), Is.False);
            Assert.That(QuestCutCommands.TryNormal("Infinity", "0", "1", out _), Is.False);
            Assert.That(QuestCutCommands.TryNormal("1,5", "-2", "0", out _), Is.True);
            f.Camera.transform.position = new Vector3(0, 0, -1.3f);
            f.Camera.fieldOfView = 45;
            f.Window.GetComponent<Canvas>().worldCamera = f.Camera;
            Canvas.ForceUpdateCanvases();
            QuestWindowInteractionTests.Capture(f.Camera, "cuts-window.png");
            row.transform.Find("Custom/X").GetComponent<Button>().onClick.Invoke();
            Canvas.ForceUpdateCanvases();
            QuestWindowInteractionTests.Capture(f.Camera, "cuts-keypad.png");
            f.Window.Close();
            Assert.That(f.Keypad.IsOpen, Is.False);
            Assert.That(f.Scene.Cuts.Count, Is.EqualTo(3));
            f.Window.Open();
            f.Panel.Refresh();
            Assert.That(f.Panel.Rows.Count, Is.EqualTo(3));
        }

        [Test]
        public void Handles_ConstrainObliqueMotionInAnatomicalFrameAndPreserveLiveRemoteFields()
        {
            using var f = new Fixture();
            f.Window.Open();
            f.Panel.Refresh();
            f.Handles.Refresh();
            var gizmo = f.Handles.Gizmos.First(g => g.Cut == f.Scene.Cuts[0]);
            Assert.That(f.Handles.Gizmos.Count, Is.EqualTo(6), "Every cut is represented in every column.");
            var frame = gizmo.Frame;
            var cut = gizmo.Cut;
            Vector3 start = gizmo.PlaneCenter;
            f.Handles.SetHand(0, true, start, start);
            Assert.That(f.Handles.Candidate(0), Is.SameAs(gizmo));
            Assert.That(f.Handles.Begin(0, gizmo), Is.True);
            float span = f.Scene.GetCutPositionGeometry(cut).Span;
            Vector3 tangent = Vector3.Cross(cut.Normal, Vector3.forward).normalized * 9;
            Vector3 point = start + frame.TransformVector(tangent);
            f.Handles.SetHand(0, true, point, point);
            f.Handles.Move(0);
            Assert.That(cut.Position, Is.EqualTo(.5f).Within(1e-6));
            point += frame.TransformVector(cut.Normal.normalized * span * .2f);
            f.Handles.SetHand(0, true, point, point);
            f.Handles.Move(0);
            Assert.That(cut.Position, Is.EqualTo(.7f).Within(1e-6));
            cut.Position = .3f;
            cut.Normal = Vector3.up;
            cut.Flip = true;
            f.Handles.Move(0); // Remote orientation change reanchors without snapping.
            Assert.That(cut.Position, Is.EqualTo(.3f));
            point += frame.TransformVector(Vector3.up * span * .1f);
            f.Handles.SetHand(0, true, point, point);
            f.Handles.Move(0);
            Assert.That(cut.Position, Is.EqualTo(.4f).Within(1e-6));
            Assert.That(cut.Flip, Is.True);
            Assert.That(cut.Normal, Is.EqualTo(Vector3.up));
            f.Handles.End(0);
            Assert.That(f.Handles.IsColumnLocked(gizmo.Column.Manipulator), Is.False);
        }

        [Test]
        public void Handles_CaptureIsExclusiveAndEndsOnSuspensionClosureDeletionTrackingAndRecenter()
        {
            using var f = new Fixture();
            f.Window.Open();
            f.Panel.Refresh();
            f.Handles.Refresh();
            var first = f.Handles.Gizmos[0];
            var sameOtherColumn = f.Handles.Gizmos.First(g => g.Cut == first.Cut && g.Column != first.Column);
            var different = f.Handles.Gizmos.First(g => g.Cut != first.Cut);
            f.Handles.SetHand(0, true, first.PlaneCenter, first.PlaneCenter);
            f.Handles.SetHand(1, true, sameOtherColumn.PlaneCenter, sameOtherColumn.PlaneCenter);
            Assert.That(f.Handles.Begin(0, first), Is.True);
            Assert.That(f.Handles.Begin(1, sameOtherColumn), Is.False);
            f.Handles.SetHand(1, true, different.PlaneCenter, different.PlaneCenter);
            Assert.That(f.Handles.Begin(1, different), Is.True);
            Assert.That(f.Handles.IsColumnLocked(first.Column.Manipulator), Is.True);
            var capture = new QuestInteractionCapture();
            capture.Sample(true, false, false, false, false, true);
            Assert.That(capture.Sample(true, true, true, true, true, true), Is.True);
            Assert.That(capture.Owner, Is.EqualTo(QuestInteractionOwner.Cut));
            capture.Cancel();
            Assert.That(capture.Sample(true, true, false, true, true, true), Is.False, "A cancelled held trigger must release before another capture.");
            f.Panel.transform.Find("Background/Suspend").GetComponent<Button>().onClick.Invoke();
            f.Handles.Refresh();
            Assert.That(f.Handles.IsCaptured(0) || f.Handles.IsCaptured(1), Is.False);
            f.Window.Close();
            f.Window.Open();
            Assert.That(f.Panel.Suspended, Is.True, "Recall keeps the user's suspension state.");
            f.Panel.transform.Find("Background/Suspend").GetComponent<Button>().onClick.Invoke();
            f.Handles.Refresh();
            Assert.That(f.Handles.Begin(0, first), Is.True);
            f.Handles.SetHand(0, false, default, default);
            Assert.That(f.Handles.IsCaptured(0), Is.False);
            f.Handles.SetHand(0, true, first.PlaneCenter, first.PlaneCenter);
            Assert.That(f.Handles.Begin(0, first), Is.True);
            f.Panel.Rows.Single(r => r.Cut == first.Cut).transform.Find("Custom/X").GetComponent<Button>().onClick.Invoke();
            Assert.That(f.Keypad.IsOpen, Is.True);
            f.Scene.Cuts.Remove(first.Cut); // A remote roster mutation invalidates the target before disposal.
            Assert.That(f.Handles.Move(0), Is.False);
            f.Handles.Refresh();
            f.Panel.Refresh();
            Assert.That(f.Keypad.IsOpen, Is.False);
            Assert.That(f.Panel.Rows.Count, Is.EqualTo(2));
            Assert.That(f.Handles.Gizmos.Count, Is.EqualTo(4));
            first.Cut.Dispose();
            different = f.Handles.Gizmos.First();
            f.Handles.SetHand(0, true, different.PlaneCenter, different.PlaneCenter);
            Assert.That(f.Handles.Begin(0, different), Is.True);
            f.Window.Close();
            f.Handles.Refresh();
            Assert.That(f.Handles.IsCaptured(0), Is.False);
            Assert.That(f.Handles.Gizmos.All(g => !g.IsVisible), Is.True);
        }

        [Test]
        public void Handles_UseHysteresisRetainFeedbackDuringCaptureAndBlockBrainOnSameFrame()
        {
            using var f = new Fixture();
            f.Window.Open();
            f.Panel.Refresh();
            f.Handles.Refresh();
            var gizmo = f.Handles.Gizmos[0];
            Vector3 far = gizmo.PlaneCenter + Vector3.one * 2;
            f.Handles.SetHand(0, true, far, far);
            f.Handles.SendMessage("LateUpdate");
            Assert.That(gizmo.IsVisible, Is.False);
            Vector3 near = gizmo.PlaneCenter;
            f.Handles.SetHand(0, true, near, near);
            f.Handles.SendMessage("LateUpdate");
            Assert.That(gizmo.IsVisible, Is.True);
            Assert.That(f.Handles.Gizmos.Where(g => g.Column == gizmo.Column).All(g => g.IsVisible), Is.True, "The nearby column exposes a coherent set of cuts.");
            Assert.That(f.Handles.Gizmos.Where(g => g.Column != gizmo.Column).All(g => !g.IsVisible), Is.True, "The other column must not show isolated handles for the same hand.");
            Vector3 outward = gizmo.Frame.TransformVector(Vector3.up).normalized;
            var bounds = gizmo.Column.Manipulator.SharedMesh.bounds;
            Vector3 edge = gizmo.Frame.TransformPoint(bounds.center + Vector3.up * bounds.extents.y);
            Vector3 middle = edge + outward * .21f;
            f.Handles.SetHand(0, true, middle, middle);
            f.Handles.SendMessage("LateUpdate");
            Assert.That(gizmo.IsVisible, Is.True, "The hide threshold is larger than the appearance threshold.");
            Vector3 outside = edge + outward * .25f;
            f.Handles.SetHand(0, true, outside, outside);
            f.Handles.SendMessage("LateUpdate");
            Assert.That(gizmo.IsVisible, Is.False);
            f.Handles.SetHand(0, true, near, near);
            f.Handles.SendMessage("LateUpdate");
            Assert.That(f.Handles.Begin(0, gizmo), Is.True);
            f.Handles.SetHand(0, true, far, far);
            f.Handles.SendMessage("LateUpdate");
            Assert.That(gizmo.IsVisible, Is.True, "Captured feedback stays visible beyond proximity bounds.");
            var pointerObject = new GameObject("Inactive pointer arbiter fixture", typeof(EventSystem));
            pointerObject.SetActive(false);
            try
            {
                var pointer = pointerObject.AddComponent<QuestPointerInput>();
                QuestWindowInteractionTests.Set(pointer, "anatomy", f.View);
                QuestWindowInteractionTests.Set(pointer, "cuts", f.Handles);
                var pick = typeof(QuestPointerInput).GetMethod("AnatomyCandidate", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.That(pick.Invoke(pointer, new object[] { gizmo.Column.Manipulator.GrabCenter, false, null }), Is.Not.SameAs(gizmo.Column.Manipulator));
                f.Handles.End(0);
                var hands = (Array)typeof(QuestPointerInput).GetField("hands", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(pointer);
                var hand = hands.GetValue(0);
                hand.GetType().GetField("AnatomyTarget").SetValue(hand, gizmo.Column.Manipulator);
                var handCapture = (QuestInteractionCapture)hand.GetType().GetField("Capture").GetValue(hand);
                handCapture.Sample(true, false, false, true, true);
                handCapture.Sample(true, true, false, true, true);
                QuestWindowInteractionTests.Set(f.Handles, "pointer", pointer);
                f.Handles.SetHand(1, true, near, near);
                Assert.That(f.Handles.Begin(1, gizmo), Is.False, "Brain capture precedes Manipulator.LateUpdate in the same frame.");
                handCapture.Cancel();
                Assert.That(f.Handles.Begin(1, gizmo), Is.True);
                pointer.CancelCutGrabs();
                Assert.That(f.Handles.IsCaptured(1), Is.False, "Brain recenter uses the same cancellation entry point before moving frames.");
            }
            finally
            {
                Object.DestroyImmediate(pointerObject);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public async Task PlaneContour_StaysAnatomicallyAlignedRegardlessOfCutOrderAndRailCrossesCenter(int axis)
        {
            using var f = new Fixture();
            f.Window.Open();
            await UniTask.NextFrame();
            var cut = f.Scene.Cuts[0];
            var normals = new[] { Vector3.right, Vector3.up, Vector3.forward };
            cut.Normal = normals[axis];
            f.Scene.Cuts[1].Normal = normals[(axis + 1) % 3];
            f.Scene.Cuts[2].Normal = normals[(axis + 2) % 3];
            f.Handles.Refresh();
            var gizmo = f.Handles.Gizmos.First(g => g.Cut == cut);
            var outline = gizmo.transform.Find("Outline").GetComponent<LineRenderer>();
            var rail = gizmo.transform.Find("Rail").GetComponent<LineRenderer>();
            var original = new Vector3[4];
            outline.GetPositions(original);
            for (int order = 0; order < 8; order++)
            {
                cut.Index = order;
                f.Handles.Refresh();
                for (int corner = 0; corner < 4; corner++) Assert.That(Vector3.Distance(outline.GetPosition(corner), original[corner]), Is.LessThan(1e-6), "Order must not rotate the contour.");
            }

            Vector3 edgeA = gizmo.Frame.InverseTransformVector(original[1] - original[0]).normalized;
            Vector3 edgeB = gizmo.Frame.InverseTransformVector(original[3] - original[0]).normalized;
            Assert.That(Mathf.Max(Mathf.Abs(edgeA.x), Mathf.Abs(edgeA.y), Mathf.Abs(edgeA.z)), Is.EqualTo(1).Within(1e-5));
            Assert.That(Mathf.Max(Mathf.Abs(edgeB.x), Mathf.Abs(edgeB.y), Mathf.Abs(edgeB.z)), Is.EqualTo(1).Within(1e-5));
            Assert.That(Vector3.Dot(edgeA, cut.Normal), Is.EqualTo(0).Within(1e-5));
            Assert.That(Vector3.Dot(edgeB, cut.Normal), Is.EqualTo(0).Within(1e-5));
            Assert.That(Vector3.Dot(edgeA, edgeB), Is.EqualTo(0).Within(1e-5));
            Vector3 railCrossing = Vector3.Lerp(rail.GetPosition(0), rail.GetPosition(1), cut.Position);
            Assert.That(Vector3.Distance(railCrossing, gizmo.PlaneCenter), Is.LessThan(1e-6));
        }

        [Test]
        public async Task PlaneContact_IsBoundedAndUsesWorldMetresUnderRotationAndNonuniformScale()
        {
            using var f = new Fixture();
            f.Window.Open();
            await UniTask.NextFrame();
            f.View.Columns[0].transform.localScale = new Vector3(1.3f, 1.7f, 2.1f);
            f.Handles.Refresh();
            var gizmo = f.Handles.Gizmos[0];
            var outline = gizmo.transform.Find("Outline").GetComponent<LineRenderer>();
            Vector3 a = outline.GetPosition(0), b = outline.GetPosition(1), d = outline.GetPosition(3);
            Vector3 normal = Vector3.Cross(b - a, d - a).normalized;
            Vector3 inside = Vector3.Lerp(a, outline.GetPosition(2), .3f);
            Assert.That(gizmo.ContactDistance(inside + normal * .025f), Is.EqualTo(.025f).Within(1e-5));
            Assert.That(gizmo.ContactDistance(inside - normal * .025f), Is.EqualTo(.025f).Within(1e-5), "Both sides can be grabbed.");
            Vector3 outside = a + (a - gizmo.PlaneCenter).normalized * .06f;
            Assert.That(gizmo.ContactDistance(outside), Is.GreaterThan(.05f), "The mathematical plane must not capture outside its contour.");
            f.Handles.SetHand(0, true, outside, outside);
            Assert.That(f.Handles.Begin(0, gizmo), Is.False);
            f.Handles.SetHand(0, true, inside + normal * .025f, inside + normal * .025f);
            Assert.That(f.Handles.Begin(0, gizmo), Is.True);
        }

        [Test]
        public async Task CrossingPlanes_PreviewOnlyTheNearestTargetAndKeepCaptureUntilRelease()
        {
            using var f = new Fixture();
            f.Window.Open();
            await UniTask.NextFrame();
            f.Scene.Cuts[0].Normal = Vector3.right;
            f.Scene.Cuts[1].Normal = Vector3.up;
            f.Scene.Cuts[2].Normal = Vector3.forward;
            f.Handles.Refresh();
            var first = f.Handles.Gizmos[0];
            var second = f.Handles.Gizmos.First(g => g.Column == first.Column && g.Cut == f.Scene.Cuts[1]);
            Vector3 contact = first.Frame.TransformPoint(new Vector3(1, 15, 30));
            f.Handles.SetHand(0, true, contact, contact);
            Assert.That(f.Handles.Candidate(0), Is.SameAs(first));
            f.Handles.SendMessage("LateUpdate");
            Assert.That(first.IsVisible && second.IsVisible, Is.True);
            var firstLine = first.transform.Find("Outline").GetComponent<LineRenderer>();
            var secondLine = second.transform.Find("Outline").GetComponent<LineRenderer>();
            Assert.That(firstLine.startColor, Is.Not.EqualTo(secondLine.startColor), "Only the actual capture candidate gets hover feedback.");
            Assert.That(f.Handles.Begin(0, first), Is.True);
            Vector3 next = second.Frame.TransformPoint(new Vector3(20, 1, 30));
            f.Handles.SetHand(0, true, next, next);
            f.Handles.Move(0);
            Assert.That(first.Cut.Position, Is.Not.EqualTo(.5f));
            Assert.That(second.Cut.Position, Is.EqualTo(.5f));
            f.Handles.End(0);
            f.Handles.Refresh();
            Vector3 retarget = second.Frame.TransformPoint(new Vector3(50, 0, 30));
            f.Handles.SetHand(0, true, retarget, retarget);
            Assert.That(f.Handles.Candidate(0), Is.SameAs(second), "Move away from the just-dragged plane to target another after release.");
            f.Handles.SendMessage("LateUpdate");
            f.Camera.transform.position = first.PlaneCenter + new Vector3(-.4f, .3f, -.6f);
            f.Camera.transform.LookAt(first.PlaneCenter);
            QuestWindowInteractionTests.Capture(f.Camera, "cut-planes.png");
        }

        private static void Auto(object target, string property, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField("<" + property + ">k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
            }

            throw new MissingFieldException(property);
        }

        private sealed class Fixture : IDisposable
        {
            private readonly GameObject data = new("Inactive cut scientific fixture");
            private readonly GameObject root = new("Cut presentation fixture");
            private readonly Mesh mesh = new();
            private readonly HBP.Core.DLL.Volume volume = new();
            private readonly string volumePath = Path.Combine(Application.temporaryCachePath, "quest-cut-input-" + Guid.NewGuid().ToString("N") + ".nii");
            public readonly QuestInteractionPolicy policy;
            public readonly Base3DScene Scene;
            public readonly QuestAnatomyView View;
            public readonly QuestCutsPanel Panel;
            public readonly QuestWindow Window;
            public readonly QuestNumericKeypad Keypad;
            public readonly QuestCutHandles Handles;
            public readonly Camera Camera;

            public Fixture()
            {
                data.SetActive(false);
                Scene = data.AddComponent<Base3DScene>();
                QuestWindowInteractionTests.Set(Scene, "m_MeshManager", data.AddComponent<MeshManager>());
                using (var stream = new FileStream(volumePath, FileMode.CreateNew))
                using (var writer = new BinaryWriter(stream))
                {
                    stream.SetLength(376);
                    writer.Write(348);
                    stream.Position = 40;
                    foreach (short value in new short[] { 3, 2, 3, 4 }) writer.Write(value);
                    stream.Position = 70;
                    writer.Write((short)2);
                    writer.Write((short)8);
                    stream.Position = 80;
                    writer.Write(1f);
                    writer.Write(2f);
                    writer.Write(3f);
                    stream.Position = 108;
                    writer.Write(352f);
                    stream.Position = 344;
                    writer.Write(new byte[] { (byte)'n', (byte)'+', (byte)'1', 0 });
                    stream.Position = 352;
                    writer.Write(Enumerable.Range(0, 24).Select(i => (byte)i).ToArray());
                }

                Assert.That(volume.LoadNIFTIFile(volumePath), Is.True);
                var mri = data.AddComponent<MRIManager>();
                QuestWindowInteractionTests.Set(mri, "m_Scene", Scene);
                QuestWindowInteractionTests.Set(Scene, "m_MRIManager", mri);
                mri.MRIs.Add(new MRI3D("Quest cut input test", volume));
                for (int i = 0; i < 3; i++) Scene.Cuts.Add(new Cut(Vector3.zero, new Vector3(1, 1, 0).normalized) { ID = "cut-" + i, Index = i, Orientation = CutOrientation.Custom });
                var displayed = data.AddComponent<DisplayedObjects>();
                QuestWindowInteractionTests.Set(Scene, "m_DisplayedObjects", displayed);
                Auto(displayed, "BrainCutMeshes", new List<GameObject>());
                var owned = (List<Mesh>)typeof(DisplayedObjects).GetField("m_OwnedCutMeshes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(displayed);
                for (int i = 0; i < 3; i++)
                {
                    var cutMesh = new GameObject("Cut mesh fixture");
                    cutMesh.transform.SetParent(data.transform, false);
                    displayed.BrainCutMeshes.Add(cutMesh);
                    owned.Add(null);
                }

                View = data.AddComponent<QuestAnatomyView>();
                var current = typeof(QuestAnatomyView).GetField("current", BindingFlags.Instance | BindingFlags.NonPublic);
                current.SetValue(View, current.FieldType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single().Invoke(new object[] { Scene, null, null }));
                mesh.bounds = new Bounds(Vector3.zero, new Vector3(160, 130, 140));
                var columns = (List<QuestColumnPresentation>)typeof(QuestAnatomyView).GetField("columns", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(View);
                for (int i = 0; i < 2; i++)
                {
                    var wrapper = new GameObject("Column " + i);
                    wrapper.transform.SetParent(root.transform);
                    wrapper.transform.SetPositionAndRotation(new Vector3(i * .4f, 0, .7f), Quaternion.Euler(25, 40, 70));
                    wrapper.transform.localScale = Vector3.one * 1.7f;
                    var brain = new GameObject("Anatomical frame", typeof(MeshFilter));
                    brain.transform.SetParent(wrapper.transform, false);
                    brain.transform.localScale = Vector3.one * .001f;
                    brain.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var column = wrapper.AddComponent<Column3DAnatomy>();
                    Auto(column, "ColumnData", new AnatomicColumn("Column " + i, new BaseConfiguration(), new AnatomicConfiguration(), "column-" + i));
                    Auto(column, "BrainMesh", brain);
                    Auto(column, "Sites", new List<Site>());
                    Scene.Columns.Add(column);
                    var manipulator = wrapper.AddComponent<QuestAnatomyManipulator>();
                    manipulator.Bind(column);
                    var presentation = wrapper.AddComponent<QuestColumnPresentation>();
                    presentation.enabled = false;
                    Auto(presentation, "Column", column);
                    QuestWindowInteractionTests.Set(presentation, "manipulator", manipulator);
                    columns.Add(presentation);
                }

                var window = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Cuts Window.prefab"), root.transform);
                Window = window.GetComponent<QuestWindow>();
                Window.GetComponent<QuestWindowFollower>().enabled = false;
                window.transform.position = Vector3.zero;
                Panel = window.GetComponent<QuestCutsPanel>();
                QuestWindowInteractionTests.Set(Panel, "view", View);
                Keypad = window.GetComponent<QuestNumericKeypad>();
                policy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<QuestInteractionPolicy>("Assets/Resources/Themes/Quest/Quest Interaction Policy.asset"));
                Handles = root.AddComponent<QuestCutHandles>();
                Handles.enabled = false;
                QuestWindowInteractionTests.Set(Handles, "view", View);
                QuestWindowInteractionTests.Set(Handles, "panel", Panel);
                QuestWindowInteractionTests.Set(Handles, "policy", policy);
                QuestWindowInteractionTests.Set(Handles, "gizmoPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Cut Gizmo.prefab").GetComponent<QuestCutGizmo>());
                Camera = new GameObject("Fixture camera", typeof(Camera)).GetComponent<Camera>();
                Camera.transform.SetParent(root.transform);
                Camera.clearFlags = CameraClearFlags.SolidColor;
                Camera.backgroundColor = new Color(.05f, .05f, .05f);
            }

            public void Dispose()
            {
                QuestWindowInteractionTests.Set(View, "current", null);
                Object.DestroyImmediate(root);
                foreach (var generator in Scene.CutGeometryGenerators) generator.Dispose();
                Scene.CutGeometryGenerators.Clear();
                foreach (var cut in Scene.Cuts) cut.Dispose();
                Scene.Cuts.Clear();
                Object.DestroyImmediate(data);
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(policy);
                volume.Dispose();
                File.Delete(volumePath);
            }
        }
    }
}
#endif
