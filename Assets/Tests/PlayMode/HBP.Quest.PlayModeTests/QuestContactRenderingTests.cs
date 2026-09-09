#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using HBP.Quest;
using HBP.Transfer.Anatomy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public class QuestContactRenderingTests
    {
        private const string Prefab = "Assets/Prefabs/Quest/QuestAnatomy.prefab";

        private static AnatomySnapshot Snapshot(bool surfaceVisible = true, bool contacts = true, AnatomyContacts prepared = null)
        {
            var sites = prepared ?? new AnatomyContacts("MNI", true, new[] { "patient" }, new[]
            {
                new AnatomySite("left", "A1", "A", 0, 0, 0, new float[] { -20, 0, 20 }, new float[] { 1, 0, 0, 1 }, 10, true, AnatomySiteFlags.Masked, true),
                new AnatomySite("right", "B1", "B", 1, 0, 1, new float[] { 25, 10, 20 }, new float[] { 0, 1, 0, 1 }, 6, true, AnatomySiteFlags.Filtered, false),
                new AnatomySite("invisible", "C1", "C", 2, 0, 2, new float[] { 0, -25, 20 }, new float[] { 1, 1, 0, 1 }, 12, false, AnatomySiteFlags.Filtered, false)
            });
            return AnatomySnapshot.Create("contacts", "session", "visualization", "column", 1, new AnatomyCoordinateSpace(AnatomyMeshUploader.FrameId, AnatomyHandedness.Left, AnatomyLengthUnit.Millimeter, 1, new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }), AnatomyWinding.Clockwise, surfaceVisible, new float[] { 0.2f, 0.4f, 0.7f, 1 }, new float[] { -100, -100, 0, 100, -100, 0, 0, 100, 0 }, new float[] { 0, 0, -1, 0, 0, -1, 0, 0, -1 }, new uint[] { 0, 2, 1 }, Array.Empty<float>(), contacts: contacts ? sites : AnatomyContacts.Empty);
        }

        [Test]
        public void PreparedBuffer_PreservesCoordinatesColorOrderAndScientificMask_UsesOneSharedFrame()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            try
            {
                var view = root.GetComponent<QuestAnatomyView>();
                var renderer = root.GetComponentInChildren<QuestContactRenderer>();
                var snapshot = Snapshot();
                byte[] before = AnatomySnapshotCodec.Encode(snapshot);
                view.ApplySnapshot(snapshot);
                view.ToggleSurface();
                var manipulator = root.GetComponent<QuestAnatomyManipulator>();
                var hand = new Pose(Vector3.zero, Quaternion.identity);
                manipulator.Step(hand, true, false, hand, true, false);
                manipulator.Step(hand, true, true, hand, true, false);
                Assert.That(manipulator.IsGrabbed, Is.True, "A new grab remains possible with the surface hidden.");
                manipulator.CancelGrab();
                view.ToggleSurface();
                Assert.That(renderer.transform, Is.SameAs(root.GetComponentInChildren<MeshFilter>().transform));
                Assert.That(renderer.transform.localScale, Is.EqualTo(Vector3.one * 0.001f));
                Assert.That(root.GetComponentsInChildren<Transform>().Length, Is.EqualTo(2), "No per-contact objects.");
                Assert.That(renderer.SiteCount, Is.EqualTo(3));
                Assert.That(renderer.VisibleSiteCount, Is.EqualTo(2));
                Assert.That(renderer.BufferBytes, Is.EqualTo(96));
                var data = new GpuSite[3];
                Buffer(renderer).GetData(data);
                for (int i = 0; i < data.Length; i++)
                {
                    var site = snapshot.Contacts.Sites[i];
                    Assert.That(data[i].PositionRadius, Is.EqualTo(new Vector4(site.Position[0], site.Position[1], site.Position[2], site.Visible ? site.Diameter / 2 : 0)));
                    Assert.That(data[i].Color, Is.EqualTo(new Vector4(site.Color[0], site.Color[1], site.Color[2], site.Color[3])));
                }

                Bounds initial = renderer.WorldBounds;
                root.transform.localScale = Vector3.one * 3;
                Assert.That(Vector3.Distance(renderer.WorldBounds.size, initial.size * 3), Is.LessThan(0.00001f));
                root.transform.SetPositionAndRotation(new Vector3(1, 2, 3), Quaternion.Euler(20, 35, 70));
                Vector3 sitePosition = new Vector3(-20, 0, 20);
                Assert.That(Vector3.Distance(renderer.transform.TransformPoint(sitePosition), root.transform.TransformPoint(sitePosition * 0.001f)), Is.LessThan(0.00001f));
                Assert.That(renderer.WorldBounds.Contains(renderer.transform.TransformPoint(sitePosition)), Is.True);
                Assert.That(view.Contacts.Sites[0].EffectiveMasked, Is.True, "Scientific exclusion must not override prepared visibility.");
                Assert.That(AnatomySnapshotCodec.Encode(snapshot), Is.EqualTo(before));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CommonScientificAppearanceIsUploadedWithoutChangingPaletteOrScale()
        {
            var palette = Resources.Load<HBP.Core.Object3D.SharedMaterials>("Objects/Shared Materials").Site;
            var sites = new AnatomySite[8];
            for (int i = 0; i < sites.Length; i++)
            {
                var activity = HBP.Core.Object3D.SiteAppearance.FromActivity((i - 3) * 5, -10, 0, 10);
                var appearance = HBP.Core.Object3D.SiteAppearance.Resolve(activity.Scale, activity.Type == HBP.Core.Enums.SiteType.Positive, i == 6, false, true, i == 7, false, false, true);
                Color color = palette.GetSharedMaterial(false, appearance.Type, Color.white).GetColor("_Color");
                if (QualitySettings.activeColorSpace == ColorSpace.Linear) color = color.linear;
                sites[i] = new AnatomySite("id" + i, "S" + i, "S", i, 0, i, new float[] { i * 10, 0, 0 }, new[] { color.r, color.g, color.b, color.a }, 2 * appearance.Scale, appearance.Visible, AnatomySiteFlags.Filtered | (i == 6 ? AnatomySiteFlags.Masked : AnatomySiteFlags.None) | (i == 7 ? AnatomySiteFlags.Blacklisted : AnatomySiteFlags.None), i >= 6);
            }

            var snapshot = Snapshot(prepared: new AnatomyContacts("MNI", false, new[] { "patient" }, sites));
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            try
            {
                root.GetComponent<QuestAnatomyView>().ApplySnapshot(snapshot);
                var renderer = root.GetComponentInChildren<QuestContactRenderer>();
                var gpu = new GpuSite[sites.Length];
                Buffer(renderer).GetData(gpu);
                Assert.That(renderer.VisibleSiteCount, Is.EqualTo(7));
                for (int i = 0; i < sites.Length; i++)
                {
                    Assert.That(gpu[i].PositionRadius.w, Is.EqualTo(sites[i].Visible ? sites[i].Diameter / 2 : 0));
                    Assert.That(gpu[i].Color, Is.EqualTo(new Vector4(sites[i].Color[0], sites[i].Color[1], sites[i].Color[2], sites[i].Color[3])));
                }

                root.transform.localScale = Vector3.one * 4;
                root.transform.position = new Vector3(1, 2, 3);
                var moved = new GpuSite[sites.Length];
                Buffer(renderer).GetData(moved);
                Assert.That(moved, Is.EqualTo(gpu), "Local placement must not rewrite scientific data.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator ReplaceFailureClearAndDestroy_ReleaseContactBuffersAndPreserveHiddenSurface()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            var view = root.GetComponent<QuestAnatomyView>();
            var renderer = root.GetComponentInChildren<QuestContactRenderer>();
            try
            {
                for (int cycle = 0; cycle < 3; cycle++)
                {
                    view.ApplySnapshot(Snapshot());
                    view.ToggleSurface();
                    var first = Buffer(renderer);
                    Mesh mesh = view.SharedMesh;
                    var fields = new SerializedObject(renderer);
                    var material = fields.FindProperty("siteMaterial").objectReferenceValue;
                    fields.FindProperty("siteMaterial").objectReferenceValue = null;
                    fields.ApplyModifiedPropertiesWithoutUndo();
                    Assert.Throws<InvalidOperationException>(() => view.ApplySnapshot(Snapshot()));
                    Assert.That(view.SharedMesh, Is.SameAs(mesh));
                    Assert.That(Buffer(renderer), Is.SameAs(first));
                    Assert.That(first.IsValid(), Is.True);
                    Assert.That(view.SurfaceVisible, Is.False);
                    fields.FindProperty("siteMaterial").objectReferenceValue = material;
                    fields.ApplyModifiedPropertiesWithoutUndo();
                    view.ApplySnapshot(Snapshot());
                    Assert.That(first.IsValid(), Is.False);
                    var second = Buffer(renderer);
                    Assert.That(view.SurfaceHidden, Is.True);
                    Assert.That(view.SurfaceVisible, Is.False);
                    int uploads = view.UploadCount;
                    view.ToggleSurface();
                    yield return null;
                    yield return null;
                    Assert.That(view.SurfaceVisible, Is.True);
                    Assert.That(view.UploadCount, Is.EqualTo(uploads));
                    Assert.That(Buffer(renderer), Is.SameAs(second));
                    view.ApplySnapshot(Snapshot(contacts: false));
                    Assert.That(second.IsValid(), Is.False);
                    Assert.That(renderer.BufferBytes, Is.Zero);
                    Assert.That(renderer.SiteCount, Is.Zero);
                    view.ApplySnapshot(Snapshot());
                    var third = Buffer(renderer);
                    root.GetComponent<QuestAnatomySession>().Disconnect();
                    Assert.That(third.IsValid(), Is.True);
                    root.GetComponent<QuestAnatomySession>().CloseSession();
                    view.Clear();
                    Assert.That(third.IsValid(), Is.False);
                    Assert.That(view.BufferBytes, Is.Zero);
                    Assert.That(view.SurfaceHidden, Is.False);
                }

                view.ApplySnapshot(Snapshot(surfaceVisible: false));
                view.ToggleSurface();
                view.ToggleSurface();
                Assert.That(view.SurfaceVisible, Is.False, "Local show must respect Desktop prepared visibility.");
                var last = Buffer(renderer);
                Object.Destroy(root);
                yield return null;
                Assert.That(last.IsValid(), Is.False);
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator Shader_RendersPreparedContactsBehindHiddenSurface_AndScalesTheirDiameter()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("A graphics device is required for pixel verification.");
            var previousPipeline = GraphicsSettings.defaultRenderPipeline;
            var previousQuality = QualitySettings.renderPipeline;
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            var cameraObject = new GameObject("Contact render test camera", typeof(Camera));
            var target = new RenderTexture(256, 256, 24);
            var pixels = new Texture2D(256, 256, TextureFormat.RGB24, false);
            try
            {
                GraphicsSettings.defaultRenderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Rendering/HBP-Quest-URP.asset");
                Assert.That(GraphicsSettings.defaultRenderPipeline, Is.Not.Null);
                QualitySettings.renderPipeline = GraphicsSettings.defaultRenderPipeline;
                foreach (Transform child in root.GetComponentsInChildren<Transform>()) child.gameObject.layer = 31;
                var camera = cameraObject.GetComponent<Camera>();
                camera.cullingMask = 1 << 31;
                camera.transform.position = new Vector3(0, 0, -1);
                camera.orthographic = true;
                camera.orthographicSize = 0.08f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.targetTexture = target;
                var view = root.GetComponent<QuestAnatomyView>();
                view.ApplySnapshot(Snapshot());
                yield return null;
                yield return null;
                Read(target, pixels, "surface-visible");
                Assert.That(Count(pixels, true), Is.Zero, "Opaque surface must occlude internal contacts.");
                view.ToggleSurface();
                yield return null;
                yield return null;
                Read(target, pixels, "surface-hidden");
                int small = Count(pixels, true);
                Assert.That(small, Is.GreaterThan(30), "Masked but visible red contact must render.");
                Assert.That(Count(pixels, false), Is.GreaterThan(10), "Second instance must use its own position/color.");
                AssertColor(camera, pixels, new Vector3(-0.02f, 0, 0.02f), Color.red);
                AssertColor(camera, pixels, new Vector3(0.025f, 0.01f, 0.02f), Color.green);
                AssertColor(camera, pixels, new Vector3(0, -0.025f, 0.02f), Color.black);
                root.transform.localScale = Vector3.one * 2;
                yield return null;
                yield return null;
                Read(target, pixels, "surface-hidden-scale-2");
                int large = Count(pixels, true);
                Assert.That((float)large / small, Is.InRange(3.5f, 4.5f), "Doubling the diameter must quadruple projected area.");
                AssertColor(camera, pixels, new Vector3(-0.04f, 0, 0.04f), Color.red);
                TestContext.Out.WriteLine($"Red contact pixels: scale1={small}, scale2={large}; area ratio={(float)large / small}");

                root.transform.localScale = Vector3.one;
                root.AddComponent<ContactLatePoseTestDriver>();
                for (int frame = 0; frame < 6; frame++)
                {
                    // Resumes before the next LateUpdate: the pose and pixels must describe
                    // the same completed frame, even when a late pose writer moves every frame.
                    yield return null;
                    Read(target, pixels, "late-pose-" + frame);
                    AssertColor(camera, pixels, root.transform.TransformPoint(new Vector3(-0.02f, 0, 0.02f)), Color.red);
                    AssertColor(camera, pixels, root.transform.TransformPoint(new Vector3(0.025f, 0.01f, 0.02f)), Color.green);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
                GraphicsSettings.defaultRenderPipeline = previousPipeline;
                QualitySettings.renderPipeline = previousQuality;
            }
        }

        private static void AssertColor(Camera camera, Texture2D pixels, Vector3 world, Color expected)
        {
            Vector3 point = camera.WorldToViewportPoint(world);
            Color actual = pixels.GetPixel((int)(point.x * pixels.width), (int)(point.y * pixels.height));
            Assert.That(Vector3.Distance(new Vector3(actual.r, actual.g, actual.b), new Vector3(expected.r, expected.g, expected.b)), Is.LessThan(0.15f));
        }

        private static void Read(RenderTexture target, Texture2D pixels, string stage)
        {
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                pixels.Apply();
                string[] arguments = Environment.GetCommandLineArgs();
                int index = Array.IndexOf(arguments, "-questContactRenderEvidence");
                if (index >= 0)
                {
                    Directory.CreateDirectory(arguments[index + 1]);
                    File.WriteAllBytes(Path.Combine(arguments[index + 1], stage + ".png"), pixels.EncodeToPNG());
                }
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private static int Count(Texture2D texture, bool red)
        {
            int count = 0;
            foreach (Color32 pixel in texture.GetPixels32())
                if (red ? pixel.r > 50 && pixel.g < 10 && pixel.b < 10 : pixel.g > 50 && pixel.r < 10 && pixel.b < 10)
                    count++;
            return count;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GpuSite
        {
            public Vector4 PositionRadius, Color;
        }

        private static GraphicsBuffer Buffer(QuestContactRenderer renderer)
        {
            object current = typeof(QuestContactRenderer).GetField("current", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(renderer);
            return (GraphicsBuffer)current.GetType().GetField("Buffer").GetValue(current);
        }
    }

    // Runs after the default-order manipulation input and before contact submission.
    // Alternating the pose makes a one-frame matrix delay visible in the GPU pixels.
    [DefaultExecutionOrder(50)]
    public sealed class ContactLatePoseTestDriver : MonoBehaviour
    {
        private bool alternate;

        private void LateUpdate()
        {
            alternate = !alternate;
            float direction = alternate ? 1 : -1;
            transform.SetPositionAndRotation(new Vector3(direction * 0.025f, 0, 0), Quaternion.Euler(0, 0, direction * 20));
        }
    }
}
#endif
