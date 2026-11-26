using HBP.Quest;
using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.Tests.Quest
{
    public sealed class QuestControllerHelpTests
    {
        [Test]
        public void BothHandsCanFinishIndependentDwells()
        {
            var left = new QuestControllerHelpGesture();
            var right = new QuestControllerHelpGesture();
            Assert.That(left.Sample(false, true, true, .8f), Is.False);
            Assert.That(right.Sample(false, true, true, .25f), Is.False);
            Assert.That(left.Sample(false, true, true, .71f), Is.True);
            Assert.That(right.Sample(false, true, true, .71f), Is.False);
            Assert.That(right.Sample(false, true, true, .71f), Is.True);
            Assert.That(left.Visible, Is.True);
        }

        [Test]
        public void InteractionBlocksEntryAndDismissesVisibleHelpUntilAFreshDwell()
        {
            var gesture = new QuestControllerHelpGesture();
            Assert.That(gesture.Sample(false, true, true, 1.4f), Is.False);
            Assert.That(gesture.Sample(true, true, true, 2f), Is.False);
            Assert.That(gesture.Sample(false, true, true, 1.4f), Is.False);
            Assert.That(gesture.Sample(false, true, true, .11f), Is.True);
            Assert.That(gesture.Sample(true, true, true, .01f), Is.False);
            Assert.That(gesture.Sample(false, true, true, 1.4f), Is.False);
        }

        [Test]
        public void BriefLookAwayKeepsVisibleHelpButShortLookDownDoesNotOpenIt()
        {
            var gesture = new QuestControllerHelpGesture();
            Assert.That(gesture.Sample(false, true, true, 1.4f), Is.False);
            Assert.That(gesture.Sample(false, false, false, .05f), Is.False);
            Assert.That(gesture.Sample(false, true, true, 1.4f), Is.False);
            Assert.That(gesture.Sample(false, true, true, .11f), Is.True);
            Assert.That(gesture.Sample(false, false, false, .1f), Is.True);
            Assert.That(gesture.Sample(false, false, true, .1f), Is.True);
            Assert.That(gesture.Sample(false, false, false, .2f), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HelpOpensFromEitherSideAndStaysVisibleThroughAWristTurn(bool startOnBack)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/3D/QuestRig.prefab");
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var rig = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                var trackers = rig.GetComponentsInChildren<QuestDevicePoseTracker>();
                var head = trackers.Single(tracker => tracker.Role == QuestDevicePoseTracker.DeviceRole.Head).transform;
                var left = trackers.Single(tracker => tracker.Role == QuestDevicePoseTracker.DeviceRole.LeftController);
                var direction = new Vector3(0, -.25f, .4f).normalized;
                head.rotation = Quaternion.LookRotation(direction);
                left.transform.position = head.position + direction * .45f;
                Vector3 faceNormal = startOnBack ? direction : -direction;
                left.transform.rotation = Quaternion.FromToRotation(Vector3.up, faceNormal);
                var helpType = typeof(QuestPointerInput).Assembly.GetType("HBP.Quest.QuestControllerHelp", throwOnError: true);
                var help = System.Activator.CreateInstance(helpType, null, rig.transform, left, null);
                var sample = helpType.GetMethod("Sample");

                Assert.That(Sample(.02f), Is.False);
                Assert.That(Sample(1.49f), Is.False);
                Assert.That(Sample(.02f), Is.True);
                left.transform.rotation = Quaternion.FromToRotation(Vector3.up, Vector3.right);
                Assert.That(Sample(.2f), Is.True);
                left.transform.rotation = Quaternion.FromToRotation(Vector3.up, -faceNormal);
                Assert.That(Sample(.2f), Is.True);
                head.rotation = Quaternion.identity;
                Assert.That(Sample(.2f), Is.False);

                bool Sample(float deltaTime) => (bool)sample.Invoke(help, new object[] { 0, head, false, deltaTime });
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void QuestBootstrapReferencesNonInteractiveWorldSpaceCallouts()
        {
            var bootstrap = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var pointer = bootstrap.GetComponentInChildren<QuestPointerInput>(true);
            var callout = (GameObject)new SerializedObject(pointer).FindProperty("controllerCalloutPrefab").objectReferenceValue;
            Assert.That(callout, Is.Not.Null);
            Assert.That(callout.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(callout.GetComponentInChildren<GraphicRaycaster>(true), Is.Null);
            Assert.That(callout.GetComponentInChildren<Text>(true).supportRichText, Is.True);
            Assert.That(callout.GetComponent<LineRenderer>().useWorldSpace, Is.True);
            foreach (var graphic in callout.GetComponentsInChildren<Graphic>(true))
                Assert.That(graphic.raycastTarget, Is.False);
        }

        [TestCase(QuestDevicePoseTracker.DeviceRole.LeftController)]
        [TestCase(QuestDevicePoseTracker.DeviceRole.RightController)]
        public void ControllerHelpUsesTheTouchPlusMeshAndItsOwnControlAnchors(QuestDevicePoseTracker.DeviceRole role)
        {
            var bootstrap = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/QuestBootstrap.prefab");
            var tracker = bootstrap.GetComponentsInChildren<QuestDevicePoseTracker>(true).Single(candidate => candidate.Role == role);
            var serialized = new SerializedObject(tracker);
            var solid = (MeshRenderer)serialized.FindProperty("solidBackgroundController").objectReferenceValue;
            var mesh = solid.GetComponent<MeshFilter>().sharedMesh;
            var contourMesh = (MeshFilter)serialized.FindProperty("controllerHelpMesh").objectReferenceValue;
            Assert.That(contourMesh, Is.Not.Null);
            Assert.That(contourMesh.transform.parent, Is.EqualTo(tracker.transform));
            var contourRenderer = contourMesh.GetComponent<MeshRenderer>();
            Assert.That(contourRenderer.enabled, Is.False);
            var contour = contourRenderer.sharedMaterial;
            Assert.That(solid.transform.parent, Is.EqualTo(tracker.transform));
            Assert.That(solid.transform.localPosition.z, Is.EqualTo(.035f).Within(.0001f));
            Assert.That(mesh.subMeshCount, Is.EqualTo(1));
            Assert.That(solid.sharedMaterials.Length, Is.EqualTo(1));
            Assert.That(contour, Is.Not.Null);
            Assert.That(contour.shader.name, Is.EqualTo("HBP/Quest/Controller Help Contour"));
            Assert.That(contour.renderQueue, Is.EqualTo(4000));

            var primary = serialized.FindProperty("primaryButton").vector3Value;
            var stick = serialized.FindProperty("thumbstick").vector3Value;
            Assert.That(stick.z - primary.z, Is.GreaterThan(.01f), "The thumbstick is in front of A/X, on the opposite side from B/Y.");
            var secondary = serialized.FindProperty("secondaryButton").vector3Value;
            Assert.That(stick.x * secondary.x, Is.LessThan(0));
            foreach (var property in new[] { "primaryButton", "secondaryButton", "thumbstick", "frontTrigger", "sideGrip" })
                Assert.That(mesh.bounds.Contains(serialized.FindProperty(property).vector3Value), Is.True, property);
        }

        [Test]
        public void NativeContourAndButtonAnchorShareTheGripSpaceConversion()
        {
            const string json = @"{
                ""asset"": { ""version"": ""2.0"" },
                ""scenes"": [{ ""nodes"": [0] }],
                ""nodes"": [
                    { ""children"": [1, 2, 3, 4, 5, 6], ""translation"": [0.02, 0, 0] },
                    { ""name"": ""b_button_a"", ""mesh"": 0, ""translation"": [0, 0, 0.03] },
                    { ""name"": ""b_button_b"", ""mesh"": 0, ""translation"": [0.01, 0, 0.03] },
                    { ""name"": ""thumbstick"", ""mesh"": 0, ""translation"": [0.02, 0, 0.03] },
                    { ""name"": ""trigger"",    ""mesh"": 0, ""translation"": [0.03, 0, 0.03] },
                    { ""name"": ""squeeze"",    ""mesh"": 0, ""translation"": [0.04, 0, 0.03] },
                    { ""name"": ""b_button_oculus"", ""mesh"": 0, ""translation"": [0.01, 0.025, 0.03] }
                ],
                ""meshes"": [{ ""primitives"": [{ ""attributes"": { ""POSITION"": 0 } }] }],
                ""accessors"": [{ ""bufferView"": 0, ""componentType"": 5126, ""count"": 3, ""type"": ""VEC3"" }],
                ""bufferViews"": [{ ""buffer"": 0, ""byteLength"": 36 }],
                ""buffers"": [{ ""byteLength"": 36 }]
            }";
            byte[] bytes = WriteGlb(json, 36, WriteTriangle);
            var type = typeof(QuestPointerInput).Assembly.GetType("HBP.Quest.QuestControllerModelGeometry", throwOnError: true);
            var geometry = type.GetMethod("Read").Invoke(null, new object[] { bytes, false });
            var mesh = (Mesh)type.GetProperty("Mesh").GetValue(geometry);
            try
            {
                Assert.That(mesh.vertexCount, Is.EqualTo(18));
                Assert.That(mesh.vertices[0], Is.EqualTo(new Vector3(.02f, 0, -.03f)));
                Assert.That(mesh.triangles.Take(3), Is.EqualTo(new[] { 0, 2, 1 }));
                var anchor = type.GetMethod("Anchor");
                var stick = Enum.Parse(anchor.GetParameters()[0].ParameterType, "Thumbstick");
                Vector3 point = (Vector3)anchor.Invoke(geometry, new[] { stick });
                Assert.That(point.x, Is.InRange(.0399f, .0481f));
                Assert.That(point.z, Is.EqualTo(-.03f).Within(.00001f));
                var secondary = Enum.Parse(anchor.GetParameters()[0].ParameterType, "SecondaryButton");
                var buttonB = (Vector3)anchor.Invoke(geometry, new[] { secondary });
                Assert.That(buttonB.y, Is.InRange(-.00001f, .00801f), "The B anchor must lie on B's surface.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void NativeSkinnedContourUsesBonesAndBindsInsteadOfTheCarrierTransform()
        {
            const string json = @"{
                ""asset"": { ""version"": ""2.0"" },
                ""scenes"": [{ ""nodes"": [0] }],
                ""nodes"": [
                    { ""name"": ""grip"", ""children"": [1, 2] },
                    { ""name"": ""left_trigger_front"", ""translation"": [0.02, 0.03, 0.04] },
                    { ""name"": ""left_oculus_controller_mesh"", ""mesh"": 0, ""skin"": 0, ""translation"": [0.9, 0, 0] }
                ],
                ""skins"": [{ ""joints"": [1], ""inverseBindMatrices"": 3 }],
                ""meshes"": [{ ""primitives"": [{ ""attributes"": { ""POSITION"": 0, ""JOINTS_0"": 1, ""WEIGHTS_0"": 2 } }] }],
                ""accessors"": [
                    { ""bufferView"": 0, ""componentType"": 5126, ""count"": 3, ""type"": ""VEC3"" },
                    { ""bufferView"": 1, ""componentType"": 5121, ""count"": 3, ""type"": ""VEC4"" },
                    { ""bufferView"": 2, ""componentType"": 5121, ""count"": 3, ""type"": ""VEC4"", ""normalized"": true },
                    { ""bufferView"": 3, ""componentType"": 5126, ""count"": 1, ""type"": ""MAT4"" }
                ],
                ""bufferViews"": [
                    { ""buffer"": 0, ""byteLength"": 36 },
                    { ""buffer"": 0, ""byteOffset"": 36, ""byteLength"": 12 },
                    { ""buffer"": 0, ""byteOffset"": 48, ""byteLength"": 12 },
                    { ""buffer"": 0, ""byteOffset"": 60, ""byteLength"": 64 }
                ],
                ""buffers"": [{ ""byteLength"": 124 }]
            }";
            byte[] bytes = WriteGlb(json, 124, writer =>
            {
                WriteTriangle(writer);
                writer.Write(new byte[12]);
                for (int i = 0; i < 3; i++) writer.Write(new byte[] { 255, 0, 0, 0 });
                var bind = Matrix4x4.Translate(new Vector3(-.01f, 0, 0));
                for (int i = 0; i < 16; i++) writer.Write(bind[i]);
            });
            var type = typeof(QuestPointerInput).Assembly.GetType("HBP.Quest.QuestControllerModelGeometry", throwOnError: true);
            var geometry = type.GetMethod("Read").Invoke(null, new object[] { bytes, true });
            var mesh = (Mesh)type.GetProperty("Mesh").GetValue(geometry);
            try
            {
                Assert.That(mesh.vertexCount, Is.EqualTo(3));
                Assert.That(Vector3.Distance(mesh.vertices[0], new Vector3(.01f, .03f, -.04f)), Is.LessThan(.00001f));
                var hasAnchor = type.GetMethod("HasAnchor");
                var controlType = hasAnchor.GetParameters()[0].ParameterType;
                Assert.That(hasAnchor.Invoke(geometry, new[] { Enum.Parse(controlType, "FrontTrigger") }), Is.True);
                Assert.That(hasAnchor.Invoke(geometry, new[] { Enum.Parse(controlType, "SideGrip") }), Is.False, "The grip coordinate frame is not a control.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static void WriteTriangle(BinaryWriter writer)
        {
            foreach (float component in new[] { 0f, 0, 0, .008f, 0, 0, 0, .008f, 0 }) writer.Write(component);
        }

        private static byte[] WriteGlb(string json, int dataLength, Action<BinaryWriter> writeData)
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json.PadRight((json.Length + 3) / 4 * 4));
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(0x46546c67u);
            writer.Write(2u);
            writer.Write(28 + jsonBytes.Length + dataLength);
            writer.Write(jsonBytes.Length);
            writer.Write(0x4e4f534au);
            writer.Write(jsonBytes);
            writer.Write(dataLength);
            writer.Write(0x004e4942u);
            writeData(writer);
            return stream.ToArray();
        }
    }
}
