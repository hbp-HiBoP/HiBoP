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
using UnityEngine.XR.OpenXR.Features.Meta;

namespace HBP.Tests.PlatformConfiguration
{
    public class QuestBootstrapTests
    {
        [Test]
        public void QuestConfiguration_PassesGuard() => QuestBuildValidation.Validate();

        [Test]
        public void BoundaryVisibility_IsContextualAndWiredToPassthrough()
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var feature = settings.GetFeature<BoundaryVisibilityFeature>();
            Assert.That(feature.enabled, Is.True);
            Assert.That(new SerializedObject(feature).FindProperty("m_SuppressVisibility").boolValue, Is.False);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(QuestBootstrapSetup.RigPath);
            var boundary = new SerializedObject(prefab.GetComponent<QuestBoundaryVisibility>());
            Assert.That(boundary.FindProperty("passthrough").objectReferenceValue, Is.EqualTo(prefab.GetComponent<QuestPassthroughStatus>()));
            Assert.That(boundary.FindProperty("freeMovementInPassthrough").boolValue, Is.True);
            var desktop = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone).GetFeature<BoundaryVisibilityFeature>();
            Assert.That(desktop == null || !desktop.enabled, Is.True, "Desktop does not suppress the system boundary.");
        }

        [Test]
        public void AutomaticBoundarySuppression_IsRejectedBeforeBuild()
        {
            var feature = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android).GetFeature<BoundaryVisibilityFeature>();
            var serialized = new SerializedObject(feature);
            var property = serialized.FindProperty("m_SuppressVisibility");
            bool original = property.boolValue;
            try
            {
                property.boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<BuildFailedException>(() => QuestBuildValidation.Validate());
            }
            finally
            {
                property.boolValue = original;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        [TestCase("OnApplicationFocus", true)]
        [TestCase("OnApplicationPause", false)]
        public void BoundaryResume_InvalidatesAcceptedRequestFromPreviousSession(string callback, bool value)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(QuestBootstrapSetup.RigPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var boundary = instance.GetComponent<QuestBoundaryVisibility>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var accepted = typeof(QuestBoundaryVisibility).GetField("acceptedRequest", flags);
                var requested = typeof(QuestBoundaryVisibility).GetField("lastRequested", flags);
                accepted.SetValue(boundary, XrBoundaryVisibility.VisibilitySuppressed);
                requested.SetValue(boundary, XrBoundaryVisibility.VisibilitySuppressed);
                typeof(QuestBoundaryVisibility).GetMethod(callback, flags).Invoke(boundary, new object[] { value });
                Assert.That(accepted.GetValue(boundary), Is.Null, "A failed restore at session shutdown must not suppress the next session's request.");
                Assert.That(requested.GetValue(boundary), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

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
