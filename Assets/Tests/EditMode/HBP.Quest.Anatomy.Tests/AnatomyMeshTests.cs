using QuestAnatomyView = HBP.Quest.Legacy.QuestAnatomyView;
using AnatomyMeshUploader = HBP.Quest.Legacy.AnatomyMeshUploader;
using System;
using System.IO;
using System.Linq;
using HBP.Quest;
using HBP.Quest.Legacy;
using HBP.Transfer.Anatomy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HBP.Tests.QuestAnatomy
{
    public class AnatomyMeshTests
    {
        private static readonly float[] Identity = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };

        private static AnatomySnapshot Snapshot(AnatomyWinding winding = AnatomyWinding.Clockwise, string frame = AnatomyMeshUploader.FrameId, AnatomyHandedness handedness = AnatomyHandedness.Left, AnatomyLengthUnit unit = AnatomyLengthUnit.Millimeter, uint mappingVersion = 1, float[] mapping = null, float opacity = 1)
        {
            return AnatomySnapshot.Create("transfer", "session", "visualization", "column", 1, new AnatomyCoordinateSpace(frame, handedness, unit, mappingVersion, mapping ?? Identity), winding, true, new[] { 0.2f, 0.4f, 0.7f, opacity }, new float[] { -70, -105, -50, 72, 0, 0, 0, 72, 83 }, new float[] { 0, 0, -1, 0, 0, -1, 0, 0, -1 }, new uint[] { 0, 1, 2 }, new float[] { 0, 0, 1, 0, 0, 1 });
        }

        [TestCase(AnatomyWinding.Clockwise)]
        [TestCase(AnatomyWinding.CounterClockwise)]
        public void Upload_PreservesPreparedBuffersAndConvertsOnlyWinding(AnatomyWinding winding)
        {
            var snapshot = Snapshot(winding);
            var before = AnatomySnapshotCodec.Encode(snapshot);
            Mesh mesh = AnatomyMeshUploader.CreateMesh(snapshot);
            try
            {
                AssertBuffers(snapshot, mesh);
                Assert.That(mesh.triangles, Is.EqualTo(winding == AnatomyWinding.Clockwise ? new[] { 0, 1, 2 } : new[] { 0, 2, 1 }));
                Assert.That(mesh.indexFormat, Is.EqualTo(IndexFormat.UInt16));
                Assert.That(mesh.bounds.min, Is.EqualTo(new Vector3(-70, -105, -50)));
                Assert.That(mesh.bounds.max, Is.EqualTo(new Vector3(72, 72, 83)));
                Assert.That(AnatomySnapshotCodec.Encode(snapshot), Is.EqualTo(before));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [TestCase("frame")]
        [TestCase("handedness")]
        [TestCase("unit")]
        [TestCase("version")]
        [TestCase("mapping")]
        [TestCase("opacity")]
        public void UnsupportedMetadata_IsRejectedBeforeAllocation(string field)
        {
            var translated = (float[])Identity.Clone();
            translated[3] = 1;
            var snapshot = Snapshot(frame: field == "frame" ? "unknown" : AnatomyMeshUploader.FrameId, handedness: field == "handedness" ? AnatomyHandedness.Right : AnatomyHandedness.Left, unit: field == "unit" ? AnatomyLengthUnit.Meter : AnatomyLengthUnit.Millimeter, mappingVersion: field == "version" ? 2u : 1u, mapping: field == "mapping" ? translated : Identity, opacity: field == "opacity" ? 0.5f : 1f);
            int meshes = Resources.FindObjectsOfTypeAll<Mesh>().Length;
            Assert.Throws<ArgumentException>(() => AnatomyMeshUploader.CreateMesh(snapshot));
            Assert.That(Resources.FindObjectsOfTypeAll<Mesh>().Length, Is.EqualTo(meshes));
        }

        [Test]
        public void Prefab_SerializesOneUnitConversionAndOpaqueMaterial()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tests/Support/QuestPrototype/QuestAnatomy.prefab");
            var view = new SerializedObject(prefab.GetComponent<QuestAnatomyView>());
            foreach (string field in new[] { "millimeterFrame", "meshFilter", "meshRenderer", "opaqueMaterial" })
                Assert.That(view.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            var filter = prefab.GetComponentInChildren<MeshFilter>();
            Assert.That(filter.sharedMesh, Is.Null);
            Assert.That(filter.transform.localScale, Is.EqualTo(Vector3.one * 0.001f));
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(filter.GetComponent<Renderer>().sharedMaterial.shader.name, Is.EqualTo("HiBoP Quest/Anatomy Opaque"));
            Assert.That(filter.GetComponent<Renderer>().enabled, Is.False);
        }

        [Test]
        public void ActualDesktopFixture_PreservesEveryComponentAndFullSurface()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "-questAnatomyFixture");
            if (index < 0) Assert.Ignore("Pass -questAnatomyFixture with the QUEST-006 Desktop HBNA export for this integration check.");
            byte[] bytes = File.ReadAllBytes(arguments[index + 1]);
            var snapshot = AnatomySnapshotCodec.Decode(bytes);
            Mesh mesh = AnatomyMeshUploader.CreateMesh(snapshot);
            try
            {
                Assert.That(snapshot.VertexCount, Is.EqualTo(69104));
                Assert.That(snapshot.Indices.Count, Is.EqualTo(414648));
                Assert.That(mesh.indexFormat, Is.EqualTo(IndexFormat.UInt32));
                AssertBuffers(snapshot, mesh);
                Assert.That(mesh.triangles, Is.EqualTo(snapshot.Indices.ToArray().Select(i => (int)i).ToArray()));
                Assert.That(AnatomySnapshotCodec.Encode(snapshot), Is.EqualTo(bytes));
                TestContext.Out.WriteLine($"vertices={mesh.vertexCount}; triangles={snapshot.Indices.Count / 3}; boundsMm={mesh.bounds}; surfaceBytes={snapshot.SurfaceByteLength}");
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        private static void AssertBuffers(AnatomySnapshot snapshot, Mesh mesh)
        {
            Assert.That(mesh.vertices.SelectMany(v => new[] { v.x, v.y, v.z }).Select(BitConverter.SingleToInt32Bits), Is.EqualTo(snapshot.Positions.ToArray().Select(BitConverter.SingleToInt32Bits)));
            Assert.That(mesh.normals.SelectMany(v => new[] { v.x, v.y, v.z }).Select(BitConverter.SingleToInt32Bits), Is.EqualTo(snapshot.Normals.ToArray().Select(BitConverter.SingleToInt32Bits)));
            Assert.That(mesh.uv.SelectMany(v => new[] { v.x, v.y }).Select(BitConverter.SingleToInt32Bits), Is.EqualTo(snapshot.Uvs.ToArray().Select(BitConverter.SingleToInt32Bits)));
        }
    }
}
