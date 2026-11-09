#if UNITY_EDITOR
using System;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;
using HBP.Quest;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class QuestROIControlsTests
    {
        [Test]
        public async Task ProductionPointer_CreatesRenamesAndRemovesRoisAndSpheres()
        {
            using var f = new Fixture();
            using var input = new QuestCutControlsTests.PointerFixture(f.Cuts, f.Handles);
            f.Window.GetComponent<Canvas>().worldCamera = f.Cuts.Camera;
            await UniTask.NextFrame();
            Canvas.ForceUpdateCanvases();
            input.Click(f.Control<Button>("addROI").GetComponent<RectTransform>());
            f.Panel.Refresh();
            await UniTask.NextFrame();
            Assert.That(f.Scene.ROIManager.ROIs.Count, Is.EqualTo(2));
            var created = f.Scene.ROIManager.SelectedROI;
            var name = f.Control<QuestTextInputField>("roiName");
            name.onValueChanged.Invoke("");
            name.onEndEdit.Invoke("");
            Assert.That(created.Name, Is.EqualTo("ROI"), "Invalid names must not mutate the model before wire validation.");
            name.onValueChanged.Invoke("Motor ROI");
            name.onEndEdit.Invoke("Motor ROI");
            Assert.That(created.Name, Is.EqualTo("Motor ROI"));
            Canvas.ForceUpdateCanvases();
            input.Click(f.Control<Button>("addSphere").GetComponent<RectTransform>());
            f.Panel.Refresh();
            Assert.That(created.Spheres.Count, Is.EqualTo(1), "The Add sphere action at the end of the list creates a sphere.");
            Assert.That(created.SelectedSphere.InfluenceRadius, Is.EqualTo(20f));
            Assert.That(f.Panel.Rows.Count, Is.EqualTo(1));
            var row = f.Panel.Rows.Single();
            Assert.That(((Text)Field(row, "title")).text, Is.EqualTo("1"));
            Assert.That(((Text)Field(row, "definition")).text, Does.Contain("Position").And.Contain("Radius 20.0 mm"));
            Assert.That(row.GetComponentsInChildren<Selectable>(), Has.Length.EqualTo(1), "Only the remove action remains in a sphere row.");
            Assert.That(f.Control<Button>("addSphere").transform.GetSiblingIndex(), Is.EqualTo(f.Panel.Rows.Count));
            ((Button)Field(row, "remove")).onClick.Invoke();
            Assert.That(created.Spheres, Is.Empty);
            input.Click(f.Control<Button>("removeROI").GetComponent<RectTransform>());
            Assert.That(f.Scene.ROIManager.ROIs.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task ProductionGrip_MovesScientificSphereLocksItsCopiesAndKeepsPrimarySeparate()
        {
            using var f = new Fixture();
            using var input = new QuestCutControlsTests.PointerFixture(f.Cuts, f.Handles);
            await UniTask.NextFrame();
            var proxy = f.Proxies[0];
            Vector3 start = proxy.Frame.TransformPoint(f.Sphere.Position);
            input.Track(start);
            input.PressSecondary(true);
            Assert.That(f.Handles.IsCaptured(0), Is.True);
            Assert.That(f.Handles.IsColumnLocked(proxy.Column.Manipulator), Is.True);
            f.Handles.SetHand(1, true, f.Proxies[1].Frame.TransformPoint(f.Sphere.Position), f.Proxies[1].Frame.TransformPoint(f.Sphere.Position));
            Assert.That(f.Handles.Begin(1, f.Proxies[1]), Is.False, "The other column represents the same sphere.");
            input.Track(start + proxy.Frame.TransformVector(new Vector3(4, -3, 2)));
            Assert.That(Vector3.Distance(f.Sphere.Position, new Vector3(4, -3, 2)), Is.LessThan(.0001f));
            Assert.That(f.Sphere.InfluenceRadius, Is.EqualTo(5));
            Assert.That(f.Scene.TryBeginCoordinatedActivityProjection(out _), Is.False, "A held scientific-input gesture reserves its computation inputs.");
            input.PressSecondary(false);
            Assert.That(f.Handles.IsCaptured(0), Is.False);
            input.Track(proxy.Column.Manipulator.GrabCenter);
            input.Press(true);
            input.PressSecondary(true);
            Assert.That(input.AnatomyCaptured, Is.True);
            Assert.That(f.Handles.IsCaptured(0), Is.False);
            input.Press(false);
            Assert.That(f.Handles.IsCaptured(0), Is.False, "A held grip cannot take over after the primary release.");
        }

        [Test]
        public async Task ProductionGrip_FollowsWristRotationAroundItsContactWithoutRotatingTheScientificSphere()
        {
            using var f = new Fixture();
            using var input = new QuestCutControlsTests.PointerFixture(f.Cuts, f.Handles);
            await UniTask.NextFrame();
            var proxy = f.Proxies[0];
            var center = proxy.Frame.TransformPoint(f.Sphere.Position);
            var hand = center + proxy.Frame.TransformVector(Vector3.left * 4);
            input.Track(hand);
            input.PressSecondary(true);
            Assert.That(f.Handles.IsCaptured(0), Is.True);
            var rotation = Quaternion.Euler(20, 50, -30);
            input.Track(new Pose(hand, rotation));
            Assert.That(Vector3.Distance(proxy.Frame.TransformPoint(f.Sphere.Position), hand + rotation * (center - hand)), Is.LessThan(.00001f));
            Assert.That(Quaternion.Angle(proxy.transform.rotation, rotation * proxy.Frame.rotation), Is.LessThan(.01f));
            Assert.That(f.Sphere.transform.localRotation, Is.EqualTo(Quaternion.identity), "Only the local cage rotates, never the shared scientific sphere.");
            input.PressSecondary(false);
            center = proxy.Frame.TransformPoint(f.Sphere.Position);
            input.Track(center);
            input.PressSecondary(true);
            int changes = 0;
            f.ROI.OnSphereDefinitionChanged.AddListener(_ => changes++);
            input.Track(new Pose(center, Quaternion.Euler(-40, 10, 70)));
            Assert.That(changes, Is.Zero, "Turning a cage about its center must not publish a scientific sphere mutation.");
            input.PressSecondary(false);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AddSphere_StartsAtTheHeadFacingSurfaceOrFallsBackToTheBrainCenter(bool surfaceAvailable)
        {
            using var f = new Fixture();
            var proxy = f.Proxies[0];
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);
            try
            {
                var center = new Vector3(10, 20, -15);
                mesh.vertices = mesh.vertices.Select(point => center + Vector3.Scale(point, new Vector3(160, 130, 140))).ToArray();
                mesh.RecalculateBounds();
                var brain = proxy.Column.Column.BrainMesh;
                brain.GetComponent<MeshFilter>().sharedMesh = mesh;
                var collider = brain.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.enabled = surfaceAvailable;
                QuestWindowInteractionTests.Set(proxy.Column.Manipulator, "rayTarget", collider);
                f.Cuts.Camera.tag = "MainCamera";
                f.Cuts.Camera.transform.position = proxy.Frame.TransformPoint(center + Vector3.back * 300);
                f.Cuts.Camera.transform.LookAt(proxy.Column.Manipulator.GrabCenter);
                Assert.That(Camera.main, Is.SameAs(f.Cuts.Camera));
                // A global Physics.Raycast would hit this unrelated collider first.
                primitive.transform.position = Vector3.Lerp(f.Cuts.Camera.transform.position, proxy.Column.Manipulator.GrabCenter, .2f);
                primitive.transform.localScale = Vector3.one * .04f;
                f.Control<Button>("addSphere").onClick.Invoke();
                var created = f.ROI.Spheres.Last();
                Assert.That(created.InfluenceRadius, Is.EqualTo(20));
                Assert.That(Vector3.Distance(created.Position, surfaceAvailable ? center + Vector3.back * 70 : center), Is.LessThan(.001f));
                Assert.That(f.Sphere.InfluenceRadius, Is.EqualTo(5), "Existing ROI spheres keep their scientific radius.");
            }
            finally
            {
                Object.DestroyImmediate(primitive);
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void GripPoses_MatchBrainTranslationRotationAndScaleWithTheSamePhysicalHands()
        {
            using var f = new Fixture();
            var proxy = f.Proxies[0];
            var referenceObject = new GameObject("Reference brain gesture", typeof(QuestAnatomyManipulator));
            try
            {
                var reference = referenceObject.GetComponent<QuestAnatomyManipulator>();
                reference.Bind(proxy.Column.Column);
                var center = proxy.Frame.TransformPoint(f.Sphere.Position);
                reference.transform.SetPositionAndRotation(center, proxy.transform.rotation);
                var left = new Pose(center + proxy.Frame.TransformVector(Vector3.left * 4), Quaternion.identity);
                var right = new Pose(left.position + new Vector3(.15f, .07f, .02f), Quaternion.identity);
                reference.Step(left, true, false, right, true, false);
                f.Handles.SetHand(0, true, left.position, center, left.rotation);
                Assert.That(f.Handles.Begin(0, proxy), Is.True);
                reference.Step(left, true, true, right, true, false);
                // Aim is closer, but grip is already touching: retain the same physical grip pose as anatomy.
                left = new Pose(left.position + new Vector3(.01f, -.02f, .03f), Quaternion.Euler(25, 50, -15));
                f.Handles.SetHand(0, true, left.position, left.position + Vector3.forward * .06f, left.rotation);
                f.Handles.Move(0);
                reference.Step(left, true, true, right, true, false);
                AssertSamePose();
                f.Handles.SetHand(1, true, right.position, right.position + Vector3.forward * .06f, right.rotation);
                Assert.That(f.Handles.Begin(1, proxy), Is.True);
                reference.Step(left, true, true, right, true, true);
                var midpoint = (left.position + right.position) * .5f;
                var span = right.position - left.position;
                var turn = Quaternion.Euler(-30, 10, 40);
                left.position = midpoint - turn * span;
                right.position = midpoint + turn * span;
                f.Handles.SetHand(0, true, left.position, left.position + Vector3.forward * .06f, left.rotation);
                f.Handles.SetHand(1, true, right.position, right.position + Vector3.forward * .06f, right.rotation);
                f.Handles.Move(0);
                reference.Step(left, true, true, right, true, true);
                AssertSamePose();
                Assert.That(f.Sphere.InfluenceRadius, Is.EqualTo(5 * reference.transform.localScale.x).Within(.0001));
                f.Handles.End(0);
                reference.Step(left, true, false, right, true, true);
                right = new Pose(right.position + Vector3.up * .01f, Quaternion.Euler(10, -20, 30));
                f.Handles.SetHand(1, true, right.position, right.position, right.rotation);
                f.Handles.Move(1);
                reference.Step(left, true, false, right, true, true);
                AssertSamePose();
                f.ColumnRefresh();
                AssertSamePose(); // Refreshing scientific geometry preserves this local cage orientation.

                void AssertSamePose()
                {
                    Assert.That(Vector3.Distance(proxy.Frame.TransformPoint(f.Sphere.Position), reference.transform.position), Is.LessThan(.00001f));
                    Assert.That(Quaternion.Angle(proxy.transform.rotation, reference.transform.rotation), Is.LessThan(.02f));
                }
            }
            finally
            {
                Object.DestroyImmediate(referenceObject);
            }
        }

        [Test]
        public void TwoHandGrip_ScalesAroundTheMidpointLikeTheBrainAndReanchorsWhenOneHandReleases()
        {
            using var f = new Fixture();
            var proxy = f.Proxies[0];
            var first = proxy.Frame.TransformPoint(Vector3.zero);
            f.Handles.SetHand(0, true, first, first);
            Assert.That(f.Handles.Begin(0, proxy), Is.True);
            var second = proxy.Frame.TransformPoint(Vector3.right * 60);
            f.Handles.SetHand(1, true, second, second);
            Assert.That(f.Handles.Candidate(1), Is.SameAs(proxy), "The second grip can join from a distance, as it does for the brain.");
            Assert.That(f.Handles.Begin(1, proxy), Is.True);
            Assert.That(f.Handles.ShareCapture(0, 1), Is.True);
            var left = proxy.Frame.TransformPoint(Vector3.left * 30);
            var right = proxy.Frame.TransformPoint(Vector3.right * 90);
            f.Handles.SetHand(0, true, left, left);
            f.Handles.SetHand(1, true, right, right);
            f.Handles.Move(0);
            Assert.That(f.Sphere.InfluenceRadius, Is.EqualTo(10).Within(.0001));
            Assert.That(Vector3.Distance(f.Sphere.Position, Vector3.left * 30), Is.LessThan(.0001));
            f.ROI.SetSphereDefinition(f.Sphere, Vector3.one, 12);
            f.Handles.Move(0);
            Assert.That(f.Sphere.Position, Is.EqualTo(Vector3.one));
            Assert.That(f.Sphere.InfluenceRadius, Is.EqualTo(12));
            f.Handles.End(1);
            f.Handles.Move(0);
            Assert.That(f.Sphere.Position, Is.EqualTo(Vector3.one), "Changing the number of hands must not snap the sphere.");
            var moved = left + proxy.Frame.TransformVector(Vector3.up * 4);
            f.Handles.SetHand(0, true, moved, moved);
            f.Handles.Move(0);
            Assert.That(Vector3.Distance(f.Sphere.Position, Vector3.one + Vector3.up * 4), Is.LessThan(.0001));
            Assert.That(f.Sphere.InfluenceRadius, Is.EqualTo(12));
            f.Handles.End(0);
            Assert.That(f.Scene.TryBeginCoordinatedActivityProjection(out var scope), Is.True);
            scope.Dispose();
        }

        [TestCase("close")]
        [TestCase("suspend")]
        [TestCase("tracking")]
        [TestCase("delete")]
        [TestCase("selection")]
        [TestCase("hide")]
        public void Capture_EndsWithoutRetargetingOrClearingScientificSelection(string interruption)
        {
            using var f = new Fixture();
            var proxy = f.Proxies[0];
            var start = proxy.Frame.TransformPoint(f.Sphere.Position);
            f.Handles.SetHand(0, true, start, start);
            Assert.That(f.Handles.Begin(0, proxy), Is.True);
            switch (interruption)
            {
                case "close": f.Window.Close(); break;
                case "suspend": f.Control<Button>("suspend").onClick.Invoke(); break;
                case "tracking": f.Handles.SetHand(0, false, start, start); break;
                case "delete":
                    f.ROI.RemoveSphere(0);
                    f.ROI.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Replacement", Vector3.one * 40, 7);
                    f.ColumnRefresh(); // Reuses the exact captured proxy for a different model.
                    break;
                case "selection":
                    var other = f.Scene.ROIManager.AddROI("Other");
                    other.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Other", Vector3.one * 50, 8);
                    f.ColumnRefresh();
                    break;
                case "hide": f.Scene.UpdateVisibleState(false, false); break;
            }

            Assert.That(f.Handles.Move(0), Is.False);
            Assert.That(f.Handles.IsCaptured(0), Is.False);
            if (interruption is "close" or "suspend" or "tracking") Assert.That(f.Scene.ROIManager.SelectedROI, Is.SameAs(f.ROI));
            if (interruption == "delete") Assert.That(f.ROI.Spheres.Single().Position, Is.EqualTo(Vector3.one * 40));
            if (interruption == "selection") Assert.That(f.Scene.ROIManager.SelectedROI.Spheres.Single().Position, Is.EqualTo(Vector3.one * 50));
            Assert.That(f.Scene.TryBeginCoordinatedActivityProjection(out var operation), Is.True, "Every cancellation must release its projection reservation.");
            operation.Dispose();
        }

        [Test]
        public async Task ProductionGrip_ArbitratesCutsAndRoisAndRequiresReleaseAfterTrackingLoss()
        {
            using var f = new Fixture();
            using var input = new QuestCutControlsTests.PointerFixture(f.Cuts, f.Handles);
            f.Cuts.Window.Open();
            f.Cuts.Panel.Refresh();
            f.Cuts.Handles.Refresh();
            await UniTask.NextFrame();
            var cut = f.Cuts.Handles.Gizmos.First(value => value.Column == f.Proxies[0].Column);
            var point = cut.PlaneCenter;
            f.ROI.SetSphereDefinition(f.Sphere, f.Proxies[0].Frame.InverseTransformPoint(point), 5);
            input.Track(point);
            input.PressSecondary(true);
            Assert.That(f.Handles.IsCaptured(0), Is.True, "At equal contact, the explicit ROI target wins.");
            Assert.That(f.Cuts.Handles.IsCaptured(0), Is.False);
            input.LoseTracking();
            Assert.That(f.Handles.IsCaptured(0), Is.False);
            input.Track(point);
            Assert.That(f.Handles.IsCaptured(0), Is.False, "A held grip must release after tracking returns.");
            input.PressSecondary(false);
            f.Control<Button>("suspend").onClick.Invoke();
            input.PressSecondary(true);
            Assert.That(f.Cuts.Handles.IsCaptured(0), Is.True);
            f.Control<Button>("suspend").onClick.Invoke();
            f.Handles.SetHand(1, true, point, point);
            Assert.That(f.Handles.Begin(1, f.Proxies[0]), Is.False, "A captured cut locks that column against ROI manipulation.");
            input.PressSecondary(false);
        }

        [Test]
        public void Capture_KeepsItsScientificTargetAcrossAnotherScenesSelection()
        {
            using var first = new Fixture();
            using var second = new Fixture();
            QuestSceneTestBinding.Attach(first.Cuts.View, second.Scene, QuestColumnDisplayMode.Split).AddRange(second.Cuts.View.Columns);
            first.Cuts.View.SelectContext(first.Scene, first.Scene.Columns[0]);
            var proxy = first.Proxies[0];
            var point = proxy.Frame.TransformPoint(first.Sphere.Position);
            first.Handles.SetHand(0, true, point, point);
            Assert.That(first.Handles.Begin(0, proxy), Is.True);
            first.Cuts.View.SelectContext(second.Scene, second.Scene.Columns[0], false);
            first.Panel.Refresh();
            var moved = point + proxy.Frame.TransformVector(Vector3.right * 3);
            first.Handles.SetHand(0, true, moved, moved);
            first.Handles.Move(0);
            Assert.That(first.Sphere.Position.x, Is.EqualTo(3).Within(.0001));
            Assert.That(second.Sphere.Position, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void SphereSelection_IsAtomicAndDoesNotRepublishAnUnchangedSelection()
        {
            using var f = new Fixture();
            f.ROI.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Second sphere", Vector3.one, 7);
            var observed = new System.Collections.Generic.List<Sphere>();
            f.ROI.OnChangeSphereSelectionState.AddListener(() => observed.Add(f.ROI.SelectedSphere));
            f.ROI.SelectSphere(0);
            Assert.That(observed, Is.EqualTo(new[] { f.Sphere }));
            Assert.That(f.Sphere.Selected, Is.True);
            Assert.That(f.ROI.Spheres[1].Selected, Is.False);
            f.ROI.SelectSphere(0);
            Assert.That(observed.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Cage_RendersAboveOrdinaryWindowsBelowModalOverlaysAndStillRespectsScientificDepth()
        {
            using var f = new Fixture();
            var root = new GameObject("ROI window compositing fixture");
            var material = new Material(f.Sphere.GetComponent<Renderer>().sharedMaterial);
            var opaque = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var uiMaterial = new Material(Shader.Find("HBP/UI/Mask"));
            var target = RenderTexture.GetTemporary(256, 256, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            var camera = f.Cuts.Camera;
            try
            {
                camera.cullingMask = 1 << 31;
                camera.orthographic = true;
                camera.orthographicSize = .7f;
                camera.transform.SetPositionAndRotation(Vector3.back * 2, Quaternion.identity);
                camera.targetTexture = target;
                var cage = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/3D/QuestROIProxy.prefab"), root.transform);
                cage.layer = 31;
                cage.transform.position = Vector3.forward * .3f;
                cage.transform.localScale = Vector3.one * .25f;
                cage.GetComponent<MeshFilter>().sharedMesh = f.Sphere.GetComponent<MeshFilter>().sharedMesh;
                material.SetColor("_WireColor", Color.red);
                material.SetColor("_ContrastColor", Color.red);
                cage.GetComponent<Renderer>().sharedMaterial = material;
                var panel = new GameObject("World window", typeof(RectTransform), typeof(Canvas), typeof(Image));
                panel.transform.SetParent(root.transform, false);
                panel.layer = 31;
                ((RectTransform)panel.transform).sizeDelta = Vector2.one;
                var canvas = panel.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                canvas.sortingOrder = 10;
                panel.GetComponent<Image>().material = uiMaterial;
                panel.GetComponent<Image>().color = Color.green;
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                Assert.That(ReadRedPixels(), Is.GreaterThan(100), "Ordinary windows must not cover the locally manipulated cage.");
                System.IO.Directory.CreateDirectory("Logs/QuestUI");
                System.IO.File.WriteAllBytes("Logs/QuestUI/ROI window layering.png", pixels.EncodeToPNG());
                canvas.sortingOrder = 30000;
                Assert.That(ReadRedPixels(), Is.Zero, "The loading overlay keeps priority over ROI guides.");
                canvas.sortingOrder = 31000;
                Assert.That(ReadRedPixels(), Is.Zero, "Modal dialogs keep priority over ROI guides.");
                canvas.sortingOrder = 10;
                var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blocker.transform.SetParent(root.transform, false);
                blocker.layer = 31;
                blocker.transform.position = Vector3.back * .5f;
                blocker.transform.localScale = new Vector3(.8f, .8f, .02f);
                opaque.SetColor("_BaseColor", Color.blue);
                blocker.GetComponent<Renderer>().sharedMaterial = opaque;
                Assert.That(ReadRedPixels(), Is.Zero, "Opaque scientific geometry must still occlude the cage; it is not an always-visible overlay.");

                int ReadRedPixels()
                {
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                    pixels.Apply();
                    return pixels.GetPixels32().Count(pixel => pixel.r > 150 && pixel.g < 70 && pixel.b < 70);
                }
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(opaque);
                Object.DestroyImmediate(uiMaterial);
                Object.DestroyImmediate(pixels);
            }
        }

        [Test]
        public async Task Window_RendersAndLocalGuidesFollowTheCutsLifecycle()
        {
            using var f = new Fixture();
            f.Cuts.Camera.transform.position = new Vector3(0, 0, -1.3f);
            f.Cuts.Camera.transform.rotation = Quaternion.identity;
            f.Window.GetComponent<Canvas>().worldCamera = f.Cuts.Camera;
            await UniTask.NextFrame();
            f.Panel.Refresh();
            Canvas.ForceUpdateCanvases();
            QuestWindowInteractionTests.Capture(f.Cuts.Camera, "Quest ROI Window.png");
            var proxy = f.Proxies[0];
            proxy.Feedback(AssetDatabase.LoadAssetAtPath<QuestCutGizmoSettings>("Assets/Resources/Themes/Quest/Settings/Quest Cut Gizmo.asset"), true, false);
            var properties = new MaterialPropertyBlock();
            proxy.GetComponent<Renderer>().GetPropertyBlock(properties);
            Assert.That(properties.isEmpty, Is.True, "Selected ROI color belongs to its shared scientific material, including during hover/capture.");
            Assert.That(proxy.GetComponent<Renderer>().sharedMaterial, Is.SameAs(f.Sphere.GetComponent<Renderer>().sharedMaterial));
            f.Handles.SendMessage("LateUpdate");
            f.ColumnRefresh();
            Assert.That(f.Proxies[0].gameObject.activeSelf, Is.True);
            f.Control<Button>("suspend").onClick.Invoke();
            f.Handles.SendMessage("LateUpdate");
            f.ColumnRefresh();
            Assert.That(f.Proxies[0].gameObject.activeSelf, Is.False, "Hide guides suspends manipulation and hides spheres, like the Cuts window.");
            f.Window.Close();
            f.Handles.SendMessage("LateUpdate");
            f.ColumnRefresh();
            Assert.That(f.Proxies[0].gameObject.activeSelf, Is.False);
            Assert.That(f.Scene.ROIManager.SelectedROI, Is.SameAs(f.ROI));
        }

        private static object Field(object target, string field) => target.GetType().GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(target);

        private sealed class Fixture : IDisposable
        {
            public readonly QuestCutControlsTests.Fixture Cuts = new();
            public Base3DScene Scene => Cuts.Scene;
            public readonly QuestROIPanel Panel;
            public readonly QuestWindow Window;
            public readonly QuestROIHandles Handles;
            public readonly ROI ROI;
            public Sphere Sphere => ROI.Spheres[0];
            public QuestROISphere[] Proxies => Cuts.View.AllColumns.Select(value => value.ROISpheres[0]).ToArray();
            public T Control<T>(string name) where T : UnityEngine.Object => (T)Field(Panel, name);

            public Fixture()
            {
                var parent = Cuts.Window.transform.parent;
                var displayed = (DisplayedObjects)Field(Scene, "m_DisplayedObjects");
                var manager = Scene.gameObject.AddComponent<ROIManager>();
                QuestWindowInteractionTests.Set(manager, "m_Scene", Scene);
                QuestWindowInteractionTests.Set(manager, "m_DisplayedObjects", displayed);
                QuestWindowInteractionTests.Set(Scene, "m_ROIManager", manager);
                QuestWindowInteractionTests.Set(displayed, "m_ROIParent", parent);
                QuestWindowInteractionTests.Set(displayed, "m_ROIPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/3D/Objects/ROI.prefab"));
                ROI = manager.AddROI("ROI fixture");
                ROI.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Sphere", Vector3.zero, 5);
                Sphere.GetComponent<Renderer>().forceRenderingOff = true;
                foreach (var column in Cuts.View.AllColumns)
                {
                    QuestWindowInteractionTests.Set(column, "scientificFrame", column.Column.BrainMesh.transform);
                    QuestWindowInteractionTests.Set(column, "roiPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/3D/QuestROIProxy.prefab").GetComponent<MeshRenderer>());
                }

                Cuts.View.SelectContext(Scene, Scene.Columns[0]);
                var window = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest ROI Window.prefab"), parent);
                Window = window.GetComponent<QuestWindow>();
                window.GetComponent<QuestWindowFollower>().enabled = false;
                window.transform.position = Vector3.zero;
                Panel = window.GetComponent<QuestROIPanel>();
                QuestWindowInteractionTests.Set(Panel, "view", Cuts.View);
                Handles = parent.gameObject.AddComponent<QuestROIHandles>();
                Handles.enabled = false;
                QuestWindowInteractionTests.Set(Handles, "view", Cuts.View);
                QuestWindowInteractionTests.Set(Handles, "panel", Panel);
                QuestWindowInteractionTests.Set(Handles, "cuts", Cuts.Handles);
                QuestWindowInteractionTests.Set(Handles, "policy", Cuts.policy);
                Window.Open();
                Panel.Refresh();
                manager.ROICreationMode = true;
                ColumnRefresh();
            }

            public void ColumnRefresh()
            {
                foreach (var column in Cuts.View.AllColumns) column.SendMessage("LateUpdate");
            }

            public void Dispose()
            {
                Handles.CancelAll();
                Cuts.Dispose();
            }
        }
    }
}
#endif
