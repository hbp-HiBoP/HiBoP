#if UNITY_EDITOR
using System;
using HBP.Quest;
using HBP.Transfer.Anatomy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class QuestManipulationInputTests
    {
        [Test]
        public void SerializedRig_TriggerBindingsTrackingFocusReplacementAndXRecenter()
        {
            var background = InputSystem.settings.backgroundBehavior;
            var editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.RegisterLayout("{\"name\":\"QuestTestController\",\"extend\":\"XRController\",\"controls\":[{\"name\":\"gripPressed\",\"layout\":\"Button\",\"format\":\"FLT\"},{\"name\":\"isTracked\",\"format\":\"FLT\",\"sizeInBits\":32,\"bit\":0},{\"name\":\"triggerPressed\",\"layout\":\"Button\",\"format\":\"FLT\"},{\"name\":\"primaryButton\",\"layout\":\"Button\",\"format\":\"FLT\"}]}");
            var left = (XRController)InputSystem.AddDevice("QuestTestController");
            var right = (XRController)InputSystem.AddDevice("QuestTestController");
            var head = InputSystem.AddDevice<XRHMD>();
            InputSystem.SetDeviceUsage(left, CommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(right, CommonUsages.RightHand);
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab"));
            root.GetComponentInChildren<QuestAnatomyDiagnostic>().enabled = false;
            var input = root.GetComponentInChildren<QuestAnatomyInput>();
            var view = root.GetComponentInChildren<QuestAnatomyView>();
            var manipulation = root.GetComponentInChildren<QuestAnatomyManipulator>();
            var snapshot = AnatomySnapshot.Create("input", "session", "visualization", "column", 1, new AnatomyCoordinateSpace(AnatomyMeshUploader.FrameId, AnatomyHandedness.Left, AnatomyLengthUnit.Millimeter, 1, new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }), AnatomyWinding.Clockwise, true, new float[] { 1, 1, 1, 1 }, new float[] { -50, -50, 0, 50, -50, 0, 0, 50, 0 }, new float[] { 0, 0, -1, 0, 0, -1, 0, 0, -1 }, new uint[] { 0, 1, 2 }, Array.Empty<float>());
            try
            {
                InputSystem.QueueDeltaStateEvent(head.isTracked, (byte)1);
                InputSystem.QueueDeltaStateEvent(head.trackingState, 3);
                Track(left, new Vector3(-0.1f, -0.12f, 0.65f));
                Track(right, new Vector3(0.1f, -0.12f, 0.65f));
                view.ApplySnapshot(snapshot);
                Tick(root, input);
                Tick(root, input); // Observe released triggers after initial placement.
                Vector3 initial = view.transform.position;
                Button(left, "gripPressed", true);
                Button(right, "gripPressed", true);
                Tick(root, input);
                Assert.That(manipulation.IsGrabbed, Is.False, "Side grips no longer initiate manipulation.");
                Button(left, "gripPressed", false);
                Button(right, "gripPressed", false);
                Tick(root, input);
                Button(left, "triggerPressed", true);
                Tick(root, input);
                foreach (var tracker in root.GetComponentsInChildren<QuestDevicePoseTracker>())
                    TestContext.Out.WriteLine($"{tracker.Role}: tracked={tracker.IsTracked}, position={tracker.transform.position}");
                Assert.That(manipulation.IsGrabbed, Is.True, $"left trigger={left.GetChildControl<ButtonControl>("triggerPressed").ReadValue()}, group={view.transform.position}, initial={initial}");
                Mesh grabbedMesh = view.SharedMesh;
                Button(right, "primaryButton", true);
                Tick(root, input);
                Assert.That(view.SurfaceHidden, Is.True, "Right A hides only the surface.");
                Assert.That(view.SurfaceVisible, Is.False);
                Assert.That(view.SharedMesh, Is.SameAs(grabbedMesh));
                Assert.That(manipulation.IsGrabbed, Is.True, "Hiding the surface must keep the current grab.");
                Button(right, "primaryButton", false);
                Tick(root, input);
                Track(left, new Vector3(-0.1f, 0.08f, 0.65f));
                Tick(root, input);
                Assert.That(Vector3.Distance(view.transform.position, initial + Vector3.up * 0.2f), Is.LessThan(0.0001f));
                Button(right, "triggerPressed", true);
                Tick(root, input);
                Track(right, new Vector3(0.5f, -0.12f, 0.65f));
                Tick(root, input);
                Assert.That(view.transform.localScale.x, Is.GreaterThan(1));
                Assert.That(view.SurfaceHidden, Is.True, "The hidden group still moves and scales.");
                Button(right, "primaryButton", true);
                Tick(root, input);
                Assert.That(view.SurfaceVisible, Is.True);
                Assert.That(manipulation.IsGrabbed, Is.True);
                Button(right, "primaryButton", false);
                Tick(root, input);
                Vector3 beforeLoss = view.transform.position;
                input.SendMessage("OnApplicationFocus", false);
                Track(left, Vector3.one * 5);
                Tick(root, input);
                input.SendMessage("OnApplicationFocus", true);
                Tick(root, input);
                Assert.That(view.transform.position, Is.EqualTo(beforeLoss));
                Assert.That(manipulation.IsGrabbed, Is.False);
                Button(left, "triggerPressed", false);
                Button(right, "triggerPressed", false);
                Tick(root, input);
                float scale = view.transform.localScale.x;
                Button(left, "primaryButton", true);
                Tick(root, input);
                Assert.That(Vector3.Distance(view.transform.position, initial), Is.LessThan(0.0001f));
                Assert.That(view.transform.localScale.x, Is.EqualTo(scale));
                Button(left, "primaryButton", false);
                Track(left, view.transform.position);
                Tick(root, input);
                Button(left, "triggerPressed", true);
                Tick(root, input);
                Assert.That(manipulation.IsGrabbed, Is.True);
                InputSystem.QueueDeltaStateEvent(left.trackingState, 1);
                TrackPositionOnly(left, Vector3.one * 10);
                Tick(root, input);
                Assert.That(manipulation.IsGrabbed, Is.False);
                Track(left, view.transform.position);
                Tick(root, input);
                Assert.That(manipulation.IsGrabbed, Is.False, "Tracking recovery with held trigger must not capture.");
                Button(left, "triggerPressed", false);
                Tick(root, input);
                Button(left, "triggerPressed", true);
                Tick(root, input);
                Assert.That(manipulation.IsGrabbed, Is.True);
                view.ApplySnapshot(snapshot);
                Tick(root, input);
                Assert.That(manipulation.IsGrabbed, Is.False, "Replacing a grabbed mesh cancels its trigger.");
                Assert.That(view.transform.localScale.x, Is.EqualTo(scale));
                input.enabled = false;
                Assert.That(manipulation.IsGrabbed, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                InputSystem.RemoveDevice(left);
                InputSystem.RemoveDevice(right);
                InputSystem.RemoveDevice(head);
                InputSystem.RemoveLayout("QuestTestController");
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
            }
        }

        private static void Button(InputDevice device, string name, bool pressed) => InputSystem.QueueDeltaStateEvent(device.GetChildControl<ButtonControl>(name), pressed ? 1f : 0f);

        private static void Track(XRController device, Vector3 position)
        {
            TrackPositionOnly(device, position);
            InputSystem.QueueDeltaStateEvent(device.deviceRotation, Quaternion.identity);
            InputSystem.QueueDeltaStateEvent(device.isTracked, 1f);
            InputSystem.QueueDeltaStateEvent(device.trackingState, 3);
        }

        private static void TrackPositionOnly(XRController device, Vector3 position) => InputSystem.QueueDeltaStateEvent(device.devicePosition, position);

        private static void Tick(GameObject root, QuestAnatomyInput input)
        {
            InputSystem.Update();
            foreach (var tracker in root.GetComponentsInChildren<QuestDevicePoseTracker>()) tracker.SendMessage("Update");
            input.SendMessage("LateUpdate");
        }
    }
}
#endif
