using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Transfer.Anatomy;
using HBP.Transfer.Anatomy.Desktop;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HBP.Tests.Transfer.Anatomy.Desktop
{
    public class DesktopAnatomyCaptureTests
    {
        [Test]
        public async Task DeliveryOffer_FreezesTheSelectedCaptureAndKeepsItsIdentity()
        {
            var expected = await DesktopAnatomyCapture.CaptureSelectedAsync("delivery", "session", 1);
            var preparing = DesktopAnatomyCapture.CaptureDeliverySelectedAsync("delivery", "session", 1);
            m_Mesh.vertices = new[] { Vector3.zero, Vector3.one, Vector3.up };
            var offer = await preparing;
            byte[] encoded = AnatomySnapshotCodec.Encode(expected);
            Assert.That(offer.TransferId, Is.EqualTo("delivery"));
            Assert.That(offer.SessionId, Is.EqualTo("session"));
            Assert.That(offer.EncodedBytes, Is.EqualTo(encoded.Length));
            Assert.That(offer.ContentHash, Is.EqualTo(new HBP.Transfer.Transport.DeliveryReceipt(HBP.Transfer.Transport.TransportIdentity.Hash(encoded), HBP.Transfer.Transport.DeliveryStatus.Published).ContentHash));
        }

        private GameObject m_Root;
        private Base3DScene m_Scene;
        private Column3DAnatomy m_Column;
        private Mesh m_Mesh;
        private Texture2D m_Texture;
        private BrainMaterials m_Materials;
        private HBP.Core.DLL.Surface m_Surface;
        private object m_PreviousModule;

        [SetUp]
        public void SetUp()
        {
            m_PreviousModule = typeof(Singleton<Module3DMain>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            m_Root = new GameObject("Anatomy capture test");
            m_Root.SetActive(false);
            Module3DMain module = m_Root.AddComponent<Module3DMain>();
            typeof(Singleton<Module3DMain>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, module);
            m_Scene = m_Root.AddComponent<Base3DScene>();
            m_Column = m_Root.AddComponent<Column3DAnatomy>();
            m_Scene.Columns.Add(m_Column);
            SetField(m_Scene, "m_IsSelected", true);
            SetField(m_Column, "m_IsSelected", true);
            SetField(module, "m_Scenes", new List<Base3DScene> { m_Scene });
            SetField(m_Scene, "<Visualization>k__BackingField", new Visualization { ID = "viz opaque" });
            SetField(m_Column, "<ColumnData>k__BackingField", new AnatomicColumn("Selected anatomy", new BaseConfiguration(), new AnatomicConfiguration(), "column opaque"));
            m_Scene.SceneInformation.CompletelyLoaded = true;
            MeshManager manager = m_Root.AddComponent<MeshManager>();
            SetField(m_Scene, "m_MeshManager", manager);
            SetField(m_Scene, "m_TriangleEraser", m_Root.AddComponent<TriangleEraser>());
            m_Mesh = new Mesh
            {
                vertices = new[] { new Vector3(-12, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 18, 9) },
                normals = new[] { Vector3.forward, Vector3.up, Vector3.right },
                triangles = new[] { 2, 0, 1 },
                uv = new[] { new Vector2(0.1f, 0.2f), Vector2.zero, Vector2.one },
                uv2 = new[] { Vector2.one, Vector2.one, Vector2.one }
            };
            m_Surface = new HBP.Core.DLL.Surface();
            m_Surface.SetBuffers(m_Mesh.vertices, m_Mesh.triangles, m_Mesh.normals, m_Mesh.uv);
            manager.Meshes.Add(new LeftRightMesh3D("Test MNI", m_Surface, m_Surface, m_Surface, MeshType.MNI));
            SetField(manager, "<BrainSurface>k__BackingField", m_Surface);
            m_Root.AddComponent<MeshFilter>().sharedMesh = m_Mesh;
            m_Materials = new BrainMaterials();
            SetField(m_Scene, "<BrainMaterials>k__BackingField", m_Materials);
            m_Texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            m_Texture.SetPixel(0, 0, new Color(0.5f, 0.25f, 0.75f));
            m_Texture.Apply();
            m_Materials.SetBrainColorTexture(m_Texture);
            m_Materials.SetCuts(new List<HBP.Core.Object3D.Cut>(), 1, Quaternion.identity);
            m_Root.AddComponent<MeshRenderer>().sharedMaterial = m_Materials.BrainMaterial;
            SetField(m_Column, "<BrainMesh>k__BackingField", m_Root);
        }

        [TearDown]
        public void TearDown()
        {
            LeftRightMesh3D anatomy = (LeftRightMesh3D)m_Scene.MeshManager.Meshes[0];
            anatomy.SimplifiedLeft.Dispose();
            anatomy.SimplifiedRight.Dispose();
            anatomy.SimplifiedBoth.Dispose();
            Object.DestroyImmediate(m_Root);
            Object.DestroyImmediate(m_Mesh);
            Object.DestroyImmediate(m_Texture);
            foreach (FieldInfo field in typeof(BrainMaterials).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                if (field.GetValue(m_Materials) is Material material)
                    Object.DestroyImmediate(material);
            m_Surface.Dispose();
            typeof(Singleton<Module3DMain>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, m_PreviousModule);
        }

        [Test]
        public async Task Capture_FreezesBuffersAndIdsBeforeReturningTask()
        {
            Vector3[] original = m_Mesh.vertices;
            Task<AnatomySnapshot> pending = Capture();
            m_Mesh.vertices = new[] { Vector3.zero, Vector3.one, Vector3.up };
            m_Column.ColumnData.ID = "edited-column";
            m_Scene.Visualization.ID = "edited-viz";
            m_Root.transform.position = new Vector3(3000, 70, -12);
            AnatomySnapshot snapshot = await pending;
            Assert.That(snapshot.VisualizationId, Is.EqualTo("viz opaque"));
            Assert.That(snapshot.ColumnId, Is.EqualTo("column opaque"));
            Assert.That(snapshot.Positions.ToArray(), Is.EqualTo(original.SelectMany(v => new[] { v.x, v.y, v.z }).ToArray()));
            Assert.That(snapshot.Indices.ToArray(), Is.EqualTo(new uint[] { 2, 0, 1 }));
            Assert.That(snapshot.Coordinates.AssetToBrain.ToArray(), Is.EqualTo(new float[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }));
            Assert.That(snapshot.Coordinates.Unit, Is.EqualTo(AnatomyLengthUnit.Millimeter));
            Assert.That(snapshot.Winding, Is.EqualTo(AnatomyWinding.Clockwise));
        }

        [Test]
        public async Task Capture_UnchangedContentIsBitExactAndAppearanceIsLinear()
        {
            byte[] first = AnatomySnapshotCodec.Encode(await Capture());
            AnatomySnapshot snapshot = await Capture();
            Assert.That(AnatomySnapshotCodec.Encode(snapshot), Is.EqualTo(first));
            Color expected = ((Color)m_Texture.GetPixels32()[0]).linear;
            Assert.That(snapshot.Color.ToArray(), Is.EqualTo(new[] { expected.r, expected.g, expected.b, 1f }));
            Assert.That(m_Root.transform.position, Is.EqualTo(Vector3.zero));
        }

        [TestCase("selection")]
        [TestCase("column-type")]
        [TestCase("geometry")]
        [TestCase("functional")]
        [TestCase("transition")]
        [TestCase("hemisphere")]
        [TestCase("erasure")]
        [TestCase("deformation")]
        [TestCase("density")]
        [TestCase("opacity-transform")]
        [TestCase("palette")]
        [TestCase("property-block")]
        [TestCase("incomplete")]
        public void Capture_RefusesUnsupportedSelectionWithoutChangingIt(string scenario)
        {
            switch (scenario)
            {
                case "selection": SetField(m_Column, "m_IsSelected", false); break;
                case "column-type":
                    SetField(m_Column, "m_IsSelected", false);
                    Column3DIEEG other = m_Root.AddComponent<Column3DIEEG>();
                    m_Scene.Columns.Add(other);
                    SetField(other, "m_IsSelected", true);
                    break;
                case "geometry": m_Scene.SceneInformation.GeometryNeedsUpdate = true; break;
                case "functional": m_Scene.SceneInformation.FunctionalSurfaceNeedsUpdate = true; break;
                case "transition": SetField(m_Scene, "<IsSurfaceRepresentationTransitioning>k__BackingField", true); break;
                case "hemisphere": SetField(m_Scene.MeshManager, "<MeshPartToDisplay>k__BackingField", MeshPart.Left); break;
                case "erasure": SetField(m_Scene.TriangleEraser, "<MeshHasInvisibleTriangles>k__BackingField", true); break;
                case "deformation": m_Materials.BrainMaterial.SetFloat("_Amount", 0.4f); break;
                case "density": m_Mesh.uv2 = new[] { Vector2.zero, Vector2.one, Vector2.one }; break;
                case "opacity-transform": m_Materials.BrainMaterial.SetTextureOffset("_AoTex", new Vector2(0, -1)); break;
                case "palette":
                    m_Texture.Reinitialize(2, 1);
                    m_Texture.SetPixels(new[] { Color.red, Color.blue });
                    m_Texture.Apply();
                    break;
                case "property-block":
                    MaterialPropertyBlock block = new();
                    block.SetColor("_Color", Color.red);
                    m_Root.GetComponent<Renderer>().SetPropertyBlock(block);
                    break;
                case "incomplete": m_Mesh.triangles = Array.Empty<int>(); break;
            }

            Column3D selection = m_Scene.SelectedColumn;
            Assert.Throws<InvalidOperationException>(() => Capture());
            Assert.That(m_Scene.SelectedColumn, Is.SameAs(selection));
        }

        [Test]
        public async Task Capture_RejectsWorkerThread()
        {
            Exception caught = null;
            await Task.Run(async () =>
            {
                try
                {
                    await Capture();
                }
                catch (Exception exception)
                {
                    caught = exception;
                }
            });
            Assert.That(caught, Is.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Capture_RejectsCancellationBeforeReadingSelection()
        {
            Assert.Throws<OperationCanceledException>(() => DesktopAnatomyCapture.CaptureSelectedAsync("transfer", "session", 1, new CancellationToken(true)));
        }

        private static Task<AnatomySnapshot> Capture() => DesktopAnatomyCapture.CaptureSelectedAsync("transfer", "session", 1);

        private static void SetField(object target, string name, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }

            throw new MissingFieldException(name);
        }
    }
}
