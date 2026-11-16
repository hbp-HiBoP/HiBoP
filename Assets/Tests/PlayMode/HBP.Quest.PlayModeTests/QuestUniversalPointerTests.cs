#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;
using HBP.Core.Object3D;
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
        public void ManyOpenings_PlaceInFrontWithoutMovingExistingScenes()
        {
            var root = new GameObject("Placement test");
            root.SetActive(false);
            var mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right * 100, Vector3.up * 100 }, triangles = new[] { 0, 1, 2 } };
            try
            {
                var view = root.AddComponent<QuestAnatomyView>();
                var input = root.AddComponent<QuestAnatomyInput>();
                var head = new GameObject("Head").AddComponent<QuestDevicePoseTracker>();
                head.transform.SetParent(root.transform, false);
                head.transform.SetPositionAndRotation(new Vector3(2, 1.6f, 3), Quaternion.Euler(0, 55, 0));
                QuestWindowInteractionTests.Set(input, "view", view);
                QuestWindowInteractionTests.Set(input, "head", head);
                var place = typeof(QuestAnatomyInput).GetMethod("Place", BindingFlags.Instance | BindingFlags.NonPublic);
                QuestColumnPresentation first = null;
                Vector3 firstMoved = new(9, 8, 7);
                for (int index = 0; index < 36; index++)
                {
                    var scene = QuestSceneTestBinding.VisibleScene(root);
                    var columns = QuestSceneTestBinding.Attach(view, scene);
                    var wrapper = new GameObject("Column");
                    wrapper.transform.SetParent(root.transform, false);
                    var brain = new GameObject("Brain", typeof(MeshFilter), typeof(MeshRenderer));
                    brain.transform.SetParent(wrapper.transform, false);
                    brain.transform.localScale = Vector3.one * .001f;
                    brain.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var column = wrapper.AddComponent<Column3DAnatomy>();
                    typeof(Column3D).GetProperty("BrainMesh").GetSetMethod(true).Invoke(column, new object[] { brain });
                    var manipulator = wrapper.AddComponent<QuestAnatomyManipulator>();
                    manipulator.Bind(column);
                    var presentation = wrapper.AddComponent<QuestColumnPresentation>();
                    QuestWindowInteractionTests.Set(presentation, "manipulator", manipulator);
                    QuestSceneTestBinding.Column(presentation, scene, column);
                    columns.Add(presentation);
                    var opening = view.Find(scene);
                    typeof(QuestAnatomyView.Presentation).GetProperty("IsPlaced").SetValue(opening, false);
                    place.Invoke(input, new object[] { opening });
                    Assert.That(Vector3.Dot(manipulator.GrabCenter - head.transform.position, head.transform.forward), Is.EqualTo(.65f).Within(.001f));
                    Assert.That(opening.IsPlaced, Is.True);
                    if (index == 0)
                    {
                        first = presentation;
                        first.transform.position = firstMoved;
                    }
                    else Assert.That(first.transform.position, Is.EqualTo(firstMoved));
                }
            }
            finally
            {
                QuestSceneTestBinding.Clear(root.GetComponent<QuestAnatomyView>());
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void NearbySelection_UsesExactMeshBoundsAndNearestCenterRegardlessOfColumnOrder()
        {
            var content = new GameObject("Inactive overlapping brain fixture");
            content.SetActive(false);
            var mesh = new Mesh { bounds = new Bounds(new Vector3(30, 0, 0), Vector3.one * 100) };
            try
            {
                var view = content.AddComponent<QuestAnatomyView>();
                var scene = QuestSceneTestBinding.VisibleScene(content);
                var columns = QuestSceneTestBinding.Attach(view, scene, QuestColumnDisplayMode.Split);

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
                    QuestSceneTestBinding.Column(presentation, scene, column);
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
                QuestSceneTestBinding.Clear(content.GetComponent<QuestAnatomyView>());
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
            var rayMesh = new Mesh();
            var policy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<QuestInteractionPolicy>("Assets/Resources/Themes/Quest/Quest Interaction Policy.asset"));
            try
            {
                var head = Tracker(root, "Head", QuestDevicePoseTracker.DeviceRole.Head);
                var left = Tracker(root, "Left", QuestDevicePoseTracker.DeviceRole.LeftController);
                var right = Tracker(root, "Right", QuestDevicePoseTracker.DeviceRole.RightController);
                var camera = head.gameObject.AddComponent<Camera>();
                camera.nearClipPlane = .01f;
                var view = content.AddComponent<QuestAnatomyView>();
                var columnObject = new GameObject("Scientific column");
                columnObject.transform.SetParent(content.transform, false);
                var column = columnObject.AddComponent<Column3DAnatomy>();
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
                var scene = QuestSceneTestBinding.VisibleScene(content);
                var columns = QuestSceneTestBinding.Attach(view, scene);
                QuestSceneTestBinding.Column(presentation, scene, column);
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
                var menuMarkerScale = Vector3.one * .007f;
                marker.transform.localScale = menuMarkerScale;
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
                var cubeMesh = cube.GetComponent<MeshFilter>().sharedMesh;
                mesh.vertices = System.Array.ConvertAll(cubeMesh.vertices, vertex => vertex * 100);
                mesh.triangles = cubeMesh.triangles;
                mesh.RecalculateBounds();
                rayMesh.vertices = System.Array.ConvertAll(cubeMesh.vertices, vertex => Vector3.Scale(vertex, new Vector3(50, 100, 100)));
                rayMesh.triangles = cubeMesh.triangles;
                rayMesh.RecalculateBounds();
                Object.DestroyImmediate(cube);
                presentation.enabled = false;
                wrapper.transform.SetParent(root.transform);
                wrapper.transform.SetPositionAndRotation(Vector3.forward, Quaternion.identity);
                wrapper.transform.localScale = Vector3.one;
                brain.transform.SetParent(wrapper.transform, false);
                brain.transform.localScale = Vector3.one * .001f;
                var collider = new GameObject("Anatomy Ray Target", typeof(MeshCollider)).GetComponent<MeshCollider>();
                collider.transform.SetParent(wrapper.transform, false);
                QuestWindowInteractionTests.Set(manipulator, "rayTarget", collider);
                QuestWindowInteractionTests.Set(presentation, "scientificFrame", wrapper.transform);
                presentation.Bind(scene, column);
                scene.OnBrainRaycastMeshUpdated.Invoke(rayMesh);
                Track(leftDevice, Vector3.zero);
                Track(rightDevice, Vector3.right * .02f);
                Tick();
                Assert.That(line.enabled, Is.True);
                Assert.That(marker.activeSelf, Is.True, "A distant brain hit shows its impact marker.");
                Assert.That(marker.transform.position, Is.EqualTo(line.GetPosition(1)));
                Assert.That(marker.transform.localScale, Is.EqualTo(Vector3.one * (line.endWidth * 1.25f)));
                Assert.That(marker.transform.position.z, Is.EqualTo(.95f).Within(.001));
                Assert.That(manipulator.Raycast(new Ray(new Vector3(.04f, 0, 0), Vector3.forward), 3, out _), Is.False, "A ray through the display mesh alone must miss the anatomy target.");
                var cut = new GameObject("Cut face", typeof(MeshRenderer), typeof(MeshCollider));
                cut.transform.SetParent(brain.transform, false);
                cut.transform.localPosition = new Vector3(20, 0, -30);
                cut.GetComponent<MeshCollider>().sharedMesh = rayMesh;
                column.BrainCutMeshes.Add(cut);
                Tick();
                Assert.That(marker.transform.position.z, Is.EqualTo(.92f).Within(.001), "The nearer visible cut face supplies the impact.");
                cut.GetComponent<Renderer>().forceRenderingOff = true;
                Tick();
                Assert.That(marker.transform.position.z, Is.EqualTo(.95f).Within(.001), "Hidden cut faces cannot intercept the ray.");
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
                Assert.That(marker.activeSelf, Is.True, "During distant grabbing, the marker follows the captured ray endpoint.");
                Assert.That(marker.transform.position, Is.EqualTo(line.GetPosition(1)));
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
                Assert.That(marker.activeSelf, Is.True);
                Assert.That(marker.transform.localScale, Is.EqualTo(menuMarkerScale), "Returning to a window restores the menu marker size.");
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
                // Moving a cut updates the shared raycast mesh in place.
                var oldRay = new Ray(new Vector3(.25f, .15f, 0), Vector3.forward);
                Assert.That(manipulator.Raycast(oldRay, 3, out _), Is.True);
                rayMesh.vertices = System.Array.ConvertAll(rayMesh.vertices, vertex => vertex + Vector3.right * 100);
                rayMesh.RecalculateBounds();
                scene.OnBrainRaycastMeshUpdated.Invoke(rayMesh);
                Assert.That(manipulator.Raycast(oldRay, 3, out _), Is.False);
                Assert.That(manipulator.Raycast(new Ray(new Vector3(.35f, .15f, 0), Vector3.forward), 3, out _), Is.True);
                scene.OnBrainRaycastMeshUpdated.Invoke(null);
                Assert.That(manipulator.Raycast(new Ray(new Vector3(.35f, .15f, 0), Vector3.forward), 3, out _), Is.False);

                // The loading cancel button stays targetable behind a closer independent window.
                var loading = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Loading Manager.prefab"), root.transform);
                var presenter = loading.GetComponentInChildren<QuestLoadingCircle>(true);
                var loadingFollower = loading.GetComponentInChildren<QuestLoadingFollower>();
                loadingFollower.enabled = false;
                loadingFollower.SetCamera(camera);
                loadingFollower.GetComponent<Canvas>().worldCamera = camera;
                presenter.Open(true, true);
                loadingFollower.Step(0, true);
                var cancel = presenter.GetComponentInChildren<Button>();
                int cancellations = 0;
                presenter.OnCancel.AddListener(() => cancellations++);
                await UniTask.NextFrame(); // Newly enabled loading graphics need their initial raycast depth.
                Canvas.ForceUpdateCanvases();
                var cancelRect = (RectTransform)cancel.transform;
                Vector3 cancelPoint = cancelRect.TransformPoint(cancelRect.rect.center);
                window.transform.position = new Vector3(cancelPoint.x, cancelPoint.y, .5f);
                Track(leftDevice, new Vector3(cancelPoint.x, cancelPoint.y, 0));
                Tick();
                var loadingHits = new List<RaycastResult>();
                ui.GetComponent<EventSystem>().RaycastAll(sampledData, loadingHits);
                Assert.That(sampledData.pointerCurrentRaycast.gameObject.GetComponentInParent<Button>(), Is.SameAs(cancel), $"The production ray must select the loading cancel button before pressing. Hits: {string.Join(",", loadingHits.ConvertAll(hit => hit.gameObject.name + ":" + hit.distance))}");
                Press(leftDevice, true);
                Tick();
                Press(leftDevice, false);
                Tick();
                Assert.That(cancellations, Is.EqualTo(1), "Visual loading priority must also determine the actual trigger target.");
                Assert.That(clicks, Is.EqualTo(1), "The window underneath must not receive the cancel click.");

                presenter.Close();
                await UniTask.NextFrame(); // Remove the closed loading canvas before sampling passive input.
                var bootstrap = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
                var tooltip = Object.Instantiate(bootstrap.GetComponentInChildren<QuestAtlasHoverTooltip>(true).gameObject, root.transform);
                var tooltipText = tooltip.GetComponentInChildren<Text>(true);
                QuestWindowInteractionTests.Set(pointer, "atlasTooltip", tooltip.GetComponent<QuestAtlasHoverTooltip>());
                var rightMarker = new GameObject("Right atlas reticle");
                rightMarker.transform.SetParent(ui.transform, false);
                QuestWindowInteractionTests.Set(pointer, "rightReticle", rightMarker.transform);
                policy.EnableDistantAnatomy = false;
                string marsFolder = Path.Combine(Application.dataPath, "Data", "Atlases", "MarsAtlas");
                using var testAtlas = new HBP.Core.DLL.MarsAtlas();
                Assert.That(testAtlas.Load(Path.Combine(marsFolder, "mars_atlas_index.csv"),
                    Path.Combine(marsFolder, "brodmann_areas.txt"), Path.Combine(marsFolder, "colin27_MNI_MarsAtlas.nii")), Is.True);
                var previousAtlas = Object3DManager.MarsAtlas;
                Mesh otherMesh = null;
                try
                {
                    Object3DManager.MarsAtlas = testAtlas;
                    const int testLabel = 1;
                    var atlasPoint = testAtlas.GetAreaCoordinates(testLabel).First(point => testAtlas.GetClosestAreaIndex(point, 1) == testLabel);
                    var atlasIndices = new[] { testLabel, testLabel, testLabel };
                    var atlas = BindAtlasFixture(scene, brain, atlasIndices);

                    var brainRenderer = brain.AddComponent<MeshRenderer>();
                    var vertices = new[] { atlasPoint + new Vector3(-20, -20, 0), atlasPoint + new Vector3(0, 20, 0), atlasPoint + new Vector3(20, -20, 0) };
                    rayMesh.Clear();
                    rayMesh.vertices = vertices;
                    rayMesh.triangles = new[] { 0, 1, 2 };
                    mesh.Clear();
                    mesh.vertices = vertices;
                    mesh.triangles = rayMesh.triangles;
                    scene.OnBrainRaycastMeshUpdated.Invoke(rayMesh);
                    wrapper.transform.SetPositionAndRotation(Vector3.forward, Quaternion.Euler(6, 24, 10));
                    wrapper.transform.localScale = Vector3.one * 1.75f;
                    var impact = brain.transform.TransformPoint(atlasPoint);
                    var aim = new Vector3(impact.x, impact.y, 0);
                    Vector3 away = Vector3.up * 3;
                    window.transform.position = away;
                    Track(leftDevice, aim);
                    Track(rightDevice, aim);
                    Tick();
                    Assert.That(tooltip.activeSelf, Is.True,
                        $"Atlas eligible={atlas.CanHover}, geometry current={scene.IsBrainRaycastGeometryCurrent}, loading visible={HBP.UI.Tools.LoadingManager.IsPresenting}, modal={QuestDialogBox.Current != null}, left tracked={left.IsTracked}, right tracked={right.IsTracked}.");
                    Assert.That(tooltipText.supportRichText, Is.True);
                    Assert.That(tooltipText.fontStyle, Is.EqualTo(FontStyle.Normal));
                    Assert.That(tooltipText.text.Split('\n'), Is.EqualTo(new[]
                    {
                        "<b>Name:</b> L_VCcm - ID:1",
                        "<b>Hemisphere:</b> L",
                        "<b>Lobe:</b> Occipital",
                        "<b>Name FS:</b> Medial Occipital",
                        "<b>Full Name:</b> L Caudal Medial Visual Cortex"
                    }));
                    Assert.That(atlas.HoveredArea, Is.EqualTo(testLabel));
                    Assert.That((rightMarker.transform.position - impact).magnitude, Is.LessThan(.001f));
                    Assert.That(marker.activeSelf, Is.False, "Only the winning atlas ray receives atlas feedback.");
                    Assert.That(mesh.colors, Is.EqualTo(testAtlas.ConvertIndicesToColors(atlasIndices, testLabel)));
                    Assert.That(scene.SceneInformation.BaseCutTexturesNeedUpdate, Is.False);

                    scene.SceneInformation.FunctionalCutTexturesNeedUpdate = false;
                    InputSystem.QueueDeltaStateEvent(rightDevice.isTracked, 0f);
                    Tick();
                    Assert.That(tooltip.activeSelf, Is.True, "The left hand takes over after tracking loss.");
                    Assert.That(marker.activeSelf, Is.True);
                    Assert.That(rightMarker.activeSelf, Is.False);
                    Assert.That(scene.SceneInformation.FunctionalCutTexturesNeedUpdate, Is.False, "The same label must not recolor cuts.");

                    cut.transform.localPosition = Vector3.zero;
                    cut.GetComponent<MeshCollider>().sharedMesh = null;
                    cut.GetComponent<MeshCollider>().sharedMesh = rayMesh;
                    cut.GetComponent<Renderer>().forceRenderingOff = false;
                    brainRenderer.forceRenderingOff = true;
                    Tick();
                    Assert.That(tooltip.activeSelf, Is.True, "The visible cut supplies the atlas point when the surface is hidden.");

                    window.transform.position = new Vector3(impact.x, impact.y, .5f);
                    Tick();
                    Assert.That(tooltip.activeSelf, Is.False, "A foreground window takes priority over the cut.");
                    Assert.That(atlas.HoveredArea, Is.EqualTo(-1));
                    Assert.That(mesh.colors, Is.EqualTo(testAtlas.ConvertIndicesToColors(atlasIndices, -1)));

                    window.transform.position = away;
                    Tick();
                    Assert.That(tooltip.activeSelf, Is.True);

                    var otherScene = QuestSceneTestBinding.VisibleScene(content);
                    var otherColumns = QuestSceneTestBinding.Attach(view, otherScene);
                    var otherWrapper = new GameObject("Second atlas column");
                    otherWrapper.transform.SetParent(root.transform, false);
                    otherWrapper.transform.position = Vector3.right * .5f + Vector3.forward;
                    var otherBrain = new GameObject("Second atlas brain", typeof(MeshFilter), typeof(MeshRenderer));
                    otherBrain.transform.SetParent(otherWrapper.transform, false);
                    otherBrain.transform.localScale = Vector3.one * .001f;
                    otherMesh = Object.Instantiate(mesh);
                    otherBrain.GetComponent<MeshFilter>().sharedMesh = otherMesh;
                    var otherColumn = otherWrapper.AddComponent<Column3DAnatomy>();
                    typeof(Column3D).GetProperty("BrainMesh").SetValue(otherColumn, otherBrain);
                    var otherManipulator = otherWrapper.AddComponent<QuestAnatomyManipulator>();
                    var otherCollider = new GameObject("Second atlas ray target", typeof(MeshCollider)).GetComponent<MeshCollider>();
                    otherCollider.transform.SetParent(otherWrapper.transform, false);
                    QuestWindowInteractionTests.Set(otherManipulator, "rayTarget", otherCollider);
                    otherManipulator.Bind(otherColumn);
                    otherCollider.sharedMesh = rayMesh;
                    var otherPresentation = otherWrapper.AddComponent<QuestColumnPresentation>();
                    QuestWindowInteractionTests.Set(otherPresentation, "manipulator", otherManipulator);
                    QuestSceneTestBinding.Column(otherPresentation, otherScene, otherColumn);
                    otherColumns.Add(otherPresentation);
                    var otherAtlas = BindAtlasFixture(otherScene, otherBrain, atlasIndices);
                    var otherImpact = otherBrain.transform.TransformPoint(atlasPoint);
                    Track(leftDevice, new Vector3(otherImpact.x, otherImpact.y, 0));
                    Tick();
                    Assert.That(tooltip.activeSelf, Is.True);
                    Assert.That(atlas.HoveredArea, Is.EqualTo(-1), "Changing visualizations clears the old scene even when the label is identical.");
                    Assert.That(otherAtlas.HoveredArea, Is.EqualTo(testLabel));
                    Track(leftDevice, aim);
                    Tick();
                    view.OnPresentationChanging.Invoke(scene);
                    Assert.That(tooltip.activeSelf, Is.False);
                    Assert.That(atlas.HoveredArea, Is.EqualTo(-1));

                    var juBrain = new AtlasInfo(true, default, AtlasInfo.AtlasType.JuBrainAtlas, "Area 7M (SPL) - ID:7");
                    var tooltipView = tooltip.GetComponent<QuestAtlasHoverTooltip>();
                    var show = typeof(QuestAtlasHoverTooltip).GetMethod("Show", BindingFlags.Instance | BindingFlags.NonPublic);
                    show.Invoke(tooltipView, new object[] { juBrain, impact, head.transform });
                    Assert.That(tooltipText.text, Is.EqualTo("<b>Region:</b> Area 7M (SPL) - ID:7"));
                }
                finally
                {
                    pointer.enabled = false;
                    Object3DManager.MarsAtlas = previousAtlas;
                    if (otherMesh != null) Object.DestroyImmediate(otherMesh);
                }
            }
            catch (System.Exception exception)
            {
                Assert.Fail(exception.ToString());
            }
            finally
            {
                Object.DestroyImmediate(root);
                QuestSceneTestBinding.Clear(content.GetComponent<QuestAnatomyView>());
                Object.DestroyImmediate(content);
                Object.DestroyImmediate(rayMesh);
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

        private static AtlasManager BindAtlasFixture(Base3DScene scene, GameObject brain, int[] atlasIndices)
        {
            var atlas = scene.gameObject.AddComponent<AtlasManager>();
            var meshes = scene.gameObject.AddComponent<MeshManager>();
            var fmri = scene.gameObject.AddComponent<FMRIManager>();
            var displayed = scene.gameObject.AddComponent<DisplayedObjects>();
            meshes.Meshes.Add(new SingleMesh3D(new HBP.Core.Data.SingleMesh(), HBP.Core.Enums.MeshType.MNI, false));
            typeof(DisplayedObjects).GetProperty("Brain").SetValue(displayed, brain);
            QuestWindowInteractionTests.Set(scene, "m_MeshManager", meshes);
            QuestWindowInteractionTests.Set(scene, "m_AtlasManager", atlas);
            QuestWindowInteractionTests.Set(scene, "m_FMRIManager", fmri);
            QuestWindowInteractionTests.Set(atlas, "m_Scene", scene);
            QuestWindowInteractionTests.Set(atlas, "m_DisplayedObjects", displayed);
            QuestWindowInteractionTests.Set(atlas, "m_DisplayMarsAtlas", true);
            QuestWindowInteractionTests.Set(atlas, "m_MarsAtlasIndices", atlasIndices);
            return atlas;
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
