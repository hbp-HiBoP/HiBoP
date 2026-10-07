#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
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

namespace HBP.Tests.Quest
{
    public class QuestUniversalPointerTests
    {
        [Test]
        public void NearbySelection_UsesExactMeshBoundsAndNearestCenterRegardlessOfColumnOrder()
        {
            var content = new GameObject("Inactive overlapping brain fixture");
            content.SetActive(false);
            var mesh = new Mesh { bounds = new Bounds(new Vector3(30, 0, 0), Vector3.one * 100) };
            try
            {
                var view = content.AddComponent<QuestAnatomyView>();
                var columns = (List<QuestColumnPresentation>)typeof(QuestAnatomyView).GetField("columns", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);

                QuestAnatomyManipulator MakeColumn(string name, Vector3 position)
                {
                    var wrapper = new GameObject(name);
                    wrapper.transform.SetParent(content.transform);
                    wrapper.transform.SetPositionAndRotation(position, Quaternion.Euler(0, 35, 0));
                    wrapper.transform.localScale = Vector3.one * 2;
                    var brain = new GameObject("Mesh", typeof(MeshFilter));
                    brain.transform.SetParent(wrapper.transform, false);
                    brain.transform.localScale = Vector3.one * .001f;
                    brain.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var column = wrapper.AddComponent<Column3DAnatomy>();
                    typeof(Column3D).GetProperty("BrainMesh").GetSetMethod(true).Invoke(column, new object[] { brain });
                    var manipulator = wrapper.AddComponent<QuestAnatomyManipulator>();
                    manipulator.Bind(column);
                    var presentation = wrapper.AddComponent<QuestColumnPresentation>();
                    QuestWindowInteractionTests.Set(presentation, "manipulator", manipulator);
                    columns.Add(presentation);
                    return manipulator;
                }

                var first = MakeColumn("First", Vector3.zero);
                var second = MakeColumn("Second", Vector3.right * .05f);
                var brainFrame = first.SharedMesh == null ? null : columns[0].transform.Find("Mesh");
                Assert.That(first.CanGrab(brainFrame.TransformPoint(new Vector3(79.9f, 0, 0))), Is.True);
                Assert.That(first.CanGrab(brainFrame.TransformPoint(new Vector3(80.1f, 0, 0))), Is.False, "No padding outside the mesh bounds, including under rotation and scale.");
                Assert.That(first.GrabCenter, Is.EqualTo(brainFrame.TransformPoint(mesh.bounds.center)));
                var pointer = content.AddComponent<QuestPointerInput>();
                QuestWindowInteractionTests.Set(pointer, "anatomy", view);
                var pick = typeof(QuestPointerInput).GetMethod("AnatomyCandidate", BindingFlags.Instance | BindingFlags.NonPublic);
                var point = Vector3.Lerp(first.GrabCenter, second.GrabCenter, .8f);
                Assert.That(first.CanGrab(point) && second.CanGrab(point), Is.True, "Both bounds overlap at the test point.");
                Assert.That(pick.Invoke(pointer, new object[] { point, false, null }), Is.EqualTo(second));
                columns.Reverse();
                Assert.That(pick.Invoke(pointer, new object[] { point, false, null }), Is.EqualTo(second));
                point = Vector3.Lerp(first.GrabCenter, second.GrabCenter, .2f);
                Assert.That(pick.Invoke(pointer, new object[] { point, false, null }), Is.EqualTo(first));
            }
            finally
            {
                Object.DestroyImmediate(content);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public async Task Triggers_ClickUIOrManipulateAnatomy_WithTwoHandsAndTrackingLoss()
        {
            var background = InputSystem.settings.backgroundBehavior;
            var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.RegisterLayout("{\"name\":\"QuestUITestController\",\"extend\":\"XRController\",\"controls\":[{\"name\":\"isTracked\",\"format\":\"FLT\",\"sizeInBits\":32,\"bit\":0},{\"name\":\"triggerPressed\",\"layout\":\"Button\",\"format\":\"FLT\"},{\"name\":\"pointerPosition\",\"layout\":\"Vector3\"},{\"name\":\"pointerRotation\",\"layout\":\"Quaternion\"}]}");
            var leftDevice = (XRController)InputSystem.AddDevice("QuestUITestController");
            var rightDevice = (XRController)InputSystem.AddDevice("QuestUITestController");
            var headDevice = InputSystem.AddDevice<XRHMD>();
            InputSystem.SetDeviceUsage(leftDevice, CommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(rightDevice, CommonUsages.RightHand);
            var root = new GameObject("Quest pointer fixture");
            var content = new GameObject("Inactive scientific fixture");
            content.SetActive(false);
            var mesh = new Mesh();
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100);
            var policy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<QuestInteractionPolicy>("Assets/Resources/Themes/Quest/Quest Interaction Policy.asset"));
            try
            {
                var head = Tracker(root, "Head", QuestDevicePoseTracker.DeviceRole.Head);
                var left = Tracker(root, "Left", QuestDevicePoseTracker.DeviceRole.LeftController);
                var right = Tracker(root, "Right", QuestDevicePoseTracker.DeviceRole.RightController);
                var camera = head.gameObject.AddComponent<Camera>();
                camera.nearClipPlane = .01f;
                var view = content.AddComponent<QuestAnatomyView>();
                var column = content.AddComponent<Column3DAnatomy>();
                var brain = new GameObject("Scientific mesh", typeof(MeshFilter));
                brain.transform.SetParent(content.transform);
                brain.GetComponent<MeshFilter>().sharedMesh = mesh;
                typeof(Column3D).GetProperty("BrainMesh").GetSetMethod(true).Invoke(column, new object[] { brain });
                var wrapper = new GameObject("Presentation wrapper");
                wrapper.transform.SetParent(content.transform);
                wrapper.transform.position = new Vector3(0, 0, .2f);
                brain.transform.SetParent(wrapper.transform, false);
                brain.transform.localScale = Vector3.one * .001f;
                var manipulator = wrapper.AddComponent<QuestAnatomyManipulator>();
                manipulator.Bind(column);
                var presentation = wrapper.AddComponent<QuestColumnPresentation>();
                QuestWindowInteractionTests.Set(presentation, "manipulator", manipulator);
                var columns = (List<QuestColumnPresentation>)typeof(QuestAnatomyView).GetField("columns", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
                columns.Add(presentation);
                var window = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Window.prefab"), root.transform);
                window.transform.SetPositionAndRotation(Vector3.forward * 1.15f, Quaternion.identity);
                window.GetComponent<Canvas>().worldCamera = camera;
                var button = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Button.prefab"), window.transform).GetComponent<Button>();
                ((RectTransform)button.transform).anchoredPosition = Vector2.zero;
                int clicks = 0;
                button.onClick.AddListener(() => clicks++);
                var ui = new GameObject("Universal cursor", typeof(EventSystem));
                ui.transform.SetParent(root.transform);
                ui.SetActive(false);
                var pointer = ui.AddComponent<QuestPointerInput>();
                QuestWindowInteractionTests.Set(pointer, "policy", policy);
                QuestWindowInteractionTests.Set(pointer, "head", head);
                QuestWindowInteractionTests.Set(pointer, "left", left);
                QuestWindowInteractionTests.Set(pointer, "right", right);
                QuestWindowInteractionTests.Set(pointer, "anatomy", view);
                QuestWindowInteractionTests.Set(pointer, "trackingOrigin", root.transform);
                var line = ui.AddComponent<LineRenderer>();
                QuestWindowInteractionTests.Set(pointer, "leftRay", line);
                var marker = new GameObject("Reticle");
                marker.transform.SetParent(ui.transform);
                QuestWindowInteractionTests.Set(pointer, "leftReticle", marker.transform);
                var input = ui.AddComponent<QuestAnatomyInput>();
                QuestWindowInteractionTests.Set(input, "view", view);
                QuestWindowInteractionTests.Set(input, "head", head);
                QuestWindowInteractionTests.Set(input, "left", left);
                QuestWindowInteractionTests.Set(input, "right", right);
                QuestWindowInteractionTests.Set(input, "pointer", pointer);
                ui.SetActive(true);
                await UniTask.NextFrame(); // Canvas graphics acquire their raycast depth after the first rebuild.
                InputSystem.QueueDeltaStateEvent(headDevice.isTracked, (byte)1);
                InputSystem.QueueDeltaStateEvent(headDevice.trackingState, 3);
                Track(leftDevice, new Vector3(-.07f, 0, .2f), new Vector3(-.04f, 0, .2f));
                Track(rightDevice, new Vector3(.04f, 0, .2f));

                void Tick()
                {
                    InputSystem.Update();
                    head.SendMessage("Update");
                    left.SendMessage("Update");
                    right.SendMessage("Update");
                    Canvas.ForceUpdateCanvases();
                    pointer.Process();
                    input.SendMessage("LateUpdate");
                }

                Tick();
                var sampledHands = (System.Array)typeof(QuestPointerInput).GetField("hands", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pointer);
                var sampledHand = sampledHands.GetValue(0);
                var sampledData = (QuestPointerEventData)sampledHand.GetType().GetField("Data").GetValue(sampledHand);
                var aimPositionAction = (InputAction)sampledHand.GetType().GetField("Position").GetValue(sampledHand);
                var aimRotationAction = (InputAction)sampledHand.GetType().GetField("Rotation").GetValue(sampledHand);
                Assert.That(sampledData.Ray.origin, Is.EqualTo(new Vector3(-.04f, 0, .2f)), $"Aim controls must drive the production ray: position={aimPositionAction.activeControl}, rotation={aimRotationAction.activeControl}, value={aimPositionAction.ReadValue<Vector3>()}.");
                Assert.That(line.enabled, Is.False, "Nearby anatomy has priority over distant UI.");
                Assert.That(manipulator.CanGrab(left.transform.position), Is.False, "Grip is outside, but the actual aim point contacts the exact brain bounds.");
                Assert.That(marker.activeSelf, Is.False, "Nearby brains must not show blue spheres.");
                Press(leftDevice, true);
                Tick();
                Assert.That(pointer.AllowsAnatomy(true), Is.True);
                Assert.That(manipulator.IsGrabbed, Is.True);
                Assert.That(pointer.TryGetAnatomyPose(true, manipulator, out var contactPose, out bool contactDistant), Is.True);
                Assert.That(contactDistant, Is.False, "An aim contact must start a direct grab, not a remote grab.");
                Assert.That(contactPose.position, Is.EqualTo(new Vector3(-.04f, 0, .2f)));
                Track(leftDevice, new Vector3(-.01f, 0, .2f), new Vector3(-.04f, 0, .2f));
                Tick();
                Assert.That(wrapper.transform.position, Is.EqualTo(Vector3.forward * .2f), "Entering with the grip must not switch an already captured aim origin or cause a jump.");
                Assert.That(marker.activeSelf, Is.False, "Grabbing must not add a blue sphere either.");
                Press(rightDevice, true);
                Tick();
                Vector3 scientificPosition = brain.transform.localPosition;
                Track(leftDevice, new Vector3(-.1f, 0, .2f));
                Track(rightDevice, new Vector3(.2f, 0, .2f));
                Tick();
                Assert.That(wrapper.transform.localScale.x, Is.GreaterThan(1));
                Assert.That(brain.transform.localPosition, Is.EqualTo(scientificPosition));
                Assert.That(clicks, Is.Zero);
                InputSystem.QueueDeltaStateEvent(leftDevice.isTracked, 0f);
                InputSystem.QueueDeltaStateEvent(rightDevice.isTracked, 0f);
                Tick();
                Assert.That(manipulator.IsGrabbed, Is.False);
                Assert.That(marker.activeSelf, Is.False);
                Track(leftDevice, new Vector3(0, 0, .2f));
                Track(rightDevice, new Vector3(.04f, 0, .2f));
                Tick();
                Assert.That(manipulator.IsGrabbed, Is.False, "Restoring tracking with held triggers must not resume.");
                Press(leftDevice, false);
                Press(rightDevice, false);
                Tick();
                policy.PreferNearbyAnatomy = false;
                Tick();
                Assert.That(line.enabled, Is.False, "Direct grab availability hides the ray even when UI has trigger priority.");
                Assert.That(marker.activeSelf, Is.False);
                Press(leftDevice, true);
                Tick();
                Assert.That(pointer.AllowsAnatomy(true), Is.False);
                Assert.That(line.enabled, Is.True, "An engaged UI interaction keeps its feedback until release.");
                Press(leftDevice, false);
                Tick();
                Assert.That(clicks, Is.EqualTo(1));
                Assert.That(manipulator.IsGrabbed, Is.False);

                // The empty part of a window remains a visible cursor surface, without a click.
                policy.PreferNearbyAnatomy = false;
                Track(leftDevice, new Vector3(.25f, .15f, .2f));
                Tick();
                Assert.That(line.enabled, Is.True, "Feedback must remain visible between buttons.");
                Press(leftDevice, true);
                Tick();
                Press(leftDevice, false);
                Tick();
                Assert.That(clicks, Is.EqualTo(1));

                // Real mesh-collider picking at a distance, through the production input adapter.
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mesh.vertices = System.Array.ConvertAll(cube.GetComponent<MeshFilter>().sharedMesh.vertices, vertex => vertex * 100);
                mesh.triangles = cube.GetComponent<MeshFilter>().sharedMesh.triangles;
                mesh.RecalculateBounds();
                Object.DestroyImmediate(cube);
                presentation.enabled = false;
                wrapper.transform.SetParent(root.transform);
                wrapper.transform.SetPositionAndRotation(Vector3.forward, Quaternion.identity);
                wrapper.transform.localScale = Vector3.one;
                brain.transform.SetParent(wrapper.transform, false);
                brain.transform.localScale = Vector3.one * .001f;
                var collider = brain.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                QuestWindowInteractionTests.Set(manipulator, "rayTarget", collider);
                Track(leftDevice, Vector3.zero);
                Track(rightDevice, Vector3.right * .02f);
                Tick();
                Assert.That(line.enabled, Is.True);
                Assert.That(marker.activeSelf, Is.False, "Distant brain targeting uses only the line.");
                Press(leftDevice, true);
                Tick();
                Assert.That(manipulator.IsGrabbed, Is.True);
                Assert.That(pointer.TryGetAnatomyPose(true, manipulator, out var pose, out bool distant), Is.True);
                Assert.That(distant, Is.True);
                Assert.That(pose.position.z, Is.EqualTo(.95f).Within(.001));
                // A captured distant target can be repositioned into reach between input samples.
                wrapper.transform.position = left.transform.position;
                Tick();
                Assert.That(line.enabled, Is.False, "Even a captured distant ray is hidden when direct grabbing is available.");
                Assert.That(marker.activeSelf, Is.False);
                Assert.That(pointer.TryGetAnatomyPose(true, manipulator, out pose, out distant), Is.True, "Hiding feedback must preserve the capture.");
                Assert.That(distant, Is.True);
                Tick();
                Assert.That(line.enabled, Is.True, "Distant feedback returns after leaving direct grab reach.");
                Track(rightDevice, Vector3.right);
                Tick();
                Press(rightDevice, true);
                Tick();
                Assert.That(pointer.AllowsAnatomy(false), Is.False, "The second hand must target the remotely grabbed brain.");
                Press(rightDevice, false);
                Tick();
                Track(rightDevice, Vector3.right * .02f);
                Tick();
                Press(rightDevice, true);
                Tick();
                Assert.That(pointer.TryGetAnatomyPose(false, manipulator, out var rightPose, out bool rightDistant), Is.True);
                Assert.That(rightDistant, Is.True);
                Assert.That(rightPose.position.z, Is.EqualTo(.95f).Within(.001));
                Assert.That(wrapper.transform.position, Is.EqualTo(Vector3.forward), "Joining must not snap the brain.");
                Press(rightDevice, false);
                Tick();
                Track(leftDevice, new Vector3(.2f, 0, 0));
                Tick();
                Assert.That(wrapper.transform.position.x, Is.EqualTo(.2f).Within(.001), "The captured ray moves its brain even after leaving the original hit.");
                Assert.That(brain.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(marker.activeSelf, Is.False);
                Assert.That(clicks, Is.EqualTo(1));
                InputSystem.QueueDeltaStateEvent(leftDevice.isTracked, 0f);
                Tick();
                Assert.That(manipulator.IsGrabbed, Is.False);
                Track(leftDevice, Vector3.zero);
                Tick();
                Assert.That(manipulator.IsGrabbed, Is.False);
                Press(leftDevice, false);
                Tick();

                // An intervening window blocks far grabbing, including its empty background.
                wrapper.transform.position = new Vector3(.25f, .15f, 2);
                Track(leftDevice, new Vector3(.25f, .15f, 0));
                Tick();
                var uiHits = new List<RaycastResult>();
                ui.GetComponent<EventSystem>().RaycastAll(new QuestPointerEventData(ui.GetComponent<EventSystem>()) { trackedDevicePosition = left.transform.position, trackedDeviceOrientation = left.transform.rotation }, uiHits);
                Assert.That(uiHits.Exists(hit => hit.gameObject != null && hit.gameObject.GetComponentInParent<QuestWindow>() != null), Is.True, $"The test ray must hit the intervening window: {window.transform.position}, hits={string.Join(",", uiHits.ConvertAll(hit => hit.gameObject.name + ":" + hit.distance))}");
                Press(leftDevice, true);
                Tick();
                Assert.That(pointer.AllowsAnatomy(true), Is.False);
                Assert.That(manipulator.IsGrabbed, Is.False);
                Press(leftDevice, false);
                Tick();
                policy.PreferNearbyAnatomy = true;
                Track(leftDevice, new Vector3(0, 2, .2f));
                Tick();
                Assert.That(line.enabled, Is.False);
                Assert.That(marker.activeSelf, Is.False);
                // A representation update can mutate the same mesh instance in place.
                var oldRay = new Ray(new Vector3(.25f, .15f, 0), Vector3.forward);
                Assert.That(manipulator.Raycast(oldRay, 3, out _), Is.True);
                mesh.vertices = System.Array.ConvertAll(mesh.vertices, vertex => vertex + Vector3.right * 100);
                mesh.RecalculateBounds();
                manipulator.InvalidateRayTarget();
                Assert.That(manipulator.Raycast(oldRay, 3, out _), Is.False);
                Assert.That(manipulator.Raycast(new Ray(new Vector3(.35f, .15f, 0), Vector3.forward), 3, out _), Is.True);
            }
            catch (System.Exception exception)
            {
                Assert.Fail(exception.ToString());
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(content);
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(policy);
                InputSystem.RemoveDevice(leftDevice);
                InputSystem.RemoveDevice(rightDevice);
                InputSystem.RemoveDevice(headDevice);
                InputSystem.RemoveLayout("QuestUITestController");
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            }
        }

        private static QuestDevicePoseTracker Tracker(GameObject parent, string name, QuestDevicePoseTracker.DeviceRole role)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent.transform);
            node.SetActive(false);
            var tracker = node.AddComponent<QuestDevicePoseTracker>();
            tracker.Configure(role, node.transform, null);
            node.SetActive(true);
            return tracker;
        }

        private static void Press(InputDevice device, bool value) => InputSystem.QueueDeltaStateEvent(device.GetChildControl<ButtonControl>("triggerPressed"), value ? 1f : 0f);

        private static void Track(XRController device, Vector3 position, Vector3? aim = null)
        {
            InputSystem.QueueDeltaStateEvent(device.devicePosition, position);
            InputSystem.QueueDeltaStateEvent(device.deviceRotation, Quaternion.identity);
            InputSystem.QueueDeltaStateEvent(device.GetChildControl<Vector3Control>("pointerPosition"), aim ?? position);
            // A non-default pose activates the aim action, as it does on real OpenXR controllers.
            InputSystem.QueueDeltaStateEvent(device.GetChildControl<QuaternionControl>("pointerRotation"), Quaternion.Euler(0, 0, 1));
            InputSystem.QueueDeltaStateEvent(device.isTracked, 1f);
            InputSystem.QueueDeltaStateEvent(device.trackingState, 3);
        }
    }
}
#endif
