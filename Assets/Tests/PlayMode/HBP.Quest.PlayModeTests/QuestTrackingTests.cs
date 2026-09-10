#if UNITY_EDITOR
using HBP.Quest;
using HBP.Quest.Legacy;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace HBP.Tests.Quest
{
    public class QuestTrackingTests
    {
        [Test]
        public void Controller_LostOrPartialTrackingHidesMarkerAndDoesNotMoveIt()
        {
            var background = InputSystem.settings.backgroundBehavior;
            var editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var device = InputSystem.AddDevice<XRController>();
            InputSystem.SetDeviceUsage(device, CommonUsages.LeftHand);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.SetActive(false);
            var tracker = marker.AddComponent<QuestDevicePoseTracker>();
            tracker.Configure(QuestDevicePoseTracker.DeviceRole.LeftController, marker.transform, marker.GetComponent<Renderer>());
            try
            {
                marker.SetActive(true);
                var position = new Vector3(0.2f, 1.1f, 0.4f);
                InputSystem.QueueDeltaStateEvent(device.devicePosition, position);
                InputSystem.QueueDeltaStateEvent(device.deviceRotation, Quaternion.Euler(10, 20, 30));
                InputSystem.QueueDeltaStateEvent(device.isTracked, (byte)1);
                InputSystem.QueueDeltaStateEvent(device.trackingState, 3);
                InputSystem.Update();
                marker.SendMessage("Update");
                Assert.That(tracker.IsTracked, Is.True);
                Assert.That(marker.transform.localPosition, Is.EqualTo(position));
                Assert.That(Quaternion.Angle(marker.transform.localRotation, Quaternion.Euler(10, 20, 30)), Is.LessThan(0.01f));
                Assert.That(marker.GetComponent<Renderer>().enabled, Is.True);
                InputSystem.QueueDeltaStateEvent(device.trackingState, 1);
                InputSystem.QueueDeltaStateEvent(device.devicePosition, Vector3.one * 9);
                InputSystem.Update();
                marker.SendMessage("Update");
                Assert.That(tracker.IsTracked, Is.False);
                Assert.That(marker.GetComponent<Renderer>().enabled, Is.False);
                Assert.That(marker.transform.localPosition, Is.EqualTo(position));
                marker.SetActive(false);
                Assert.That(tracker.IsTracked, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(marker);
                InputSystem.RemoveDevice(device);
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
            }
        }
    }
}
#endif
