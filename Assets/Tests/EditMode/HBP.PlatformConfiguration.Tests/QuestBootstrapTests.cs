using System.Linq;
using System.Reflection;
using HBP.Dev;
using HBP.Quest;
using HBP.Quest.Editor;
using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.OpenXR;

namespace HBP.Tests.PlatformConfiguration
{
    public class QuestBootstrapTests
    {
        [Test]
        public void QuestConfiguration_PassesGuard() => QuestBuildValidation.Validate();

        [Test]
        public void OpaqueCamera_IsRejectedBeforeBuild()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(QuestBootstrapSetup.PrefabPath);
            var camera = prefab.GetComponentInChildren<Camera>(true);
            var color = camera.backgroundColor;
            try
            {
                camera.backgroundColor = Color.black;
                Assert.Throws<BuildFailedException>(() => QuestBuildValidation.Validate());
            }
            finally
            {
                camera.backgroundColor = color;
            }
        }

        [Test]
        public void QuestPreload_IsPreservedWhenOpenXRIsActive()
        {
            var original = PlayerSettings.GetPreloadedAssets();
            var xr = ScriptableObject.CreateInstance<OpenXRSettings>();
            try
            {
                PlayerSettings.SetPreloadedAssets(new Object[] { xr });
                HBPBuildProfiles.RemoveUnusedOpenXRPreload(BuildTarget.Android);
                Assert.That(PlayerSettings.GetPreloadedAssets(), Does.Contain(xr));
            }
            finally
            {
                PlayerSettings.SetPreloadedAssets(original);
                Object.DestroyImmediate(xr);
            }
        }

        [Test]
        public void Rig_SerializesFloorOriginAndIndependentControllerPoses()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(QuestBootstrapSetup.RigPath);
            Assert.That(prefab.GetComponentInChildren<ARSession>(true), Is.Not.Null);
            var text = prefab.GetComponentInChildren<TextMesh>(true);
            Assert.That(text.font, Is.Not.Null);
            Assert.That(text.GetComponent<Renderer>().sharedMaterial, Is.Not.Null);
            var origin = prefab.GetComponentInChildren<XROrigin>(true);
            Assert.That(origin.RequestedTrackingOriginMode, Is.EqualTo(XROrigin.TrackingOriginMode.Floor));
            Assert.That(origin.Camera.transform.parent, Is.EqualTo(origin.CameraFloorOffsetObject.transform));
            Assert.That(origin.Camera.GetComponent<ARCameraManager>(), Is.Not.Null);
            Assert.That(origin.Camera.GetComponent<TrackedPoseDriver>().updateType, Is.EqualTo(TrackedPoseDriver.UpdateType.UpdateAndBeforeRender));
            var trackers = prefab.GetComponentsInChildren<QuestDevicePoseTracker>(true);
            Assert.That(trackers.Select(t => t.Role), Is.EquivalentTo(System.Enum.GetValues(typeof(QuestDevicePoseTracker.DeviceRole))));
            foreach (var tracker in trackers.Where(t => t.Role != QuestDevicePoseTracker.DeviceRole.Head))
            {
                Assert.That(tracker.transform.parent, Is.EqualTo(origin.CameraFloorOffsetObject.transform));
                Assert.That(tracker.GetComponent<Renderer>().enabled, Is.False, "Hide the marker until a complete pose is available.");
            }
        }
    }
}
