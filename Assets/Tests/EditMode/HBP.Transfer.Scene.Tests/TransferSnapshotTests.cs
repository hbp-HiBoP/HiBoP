using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using HBP.Core.DLL;
using HBP.Transfer.Scene;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Transfer
{
    public sealed class TransferSnapshotTests
    {
        private static Surface Triangle()
        {
            var surface = new Surface();
            surface.SetBuffers(new[] { new Vector3(1, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 8, 10) }, new[] { 0, 1, 2 }, new[] { Vector3.up, Vector3.up, Vector3.up }, new[] { Vector2.zero, Vector2.one, Vector2.up }, new[] { Color.red, Color.green, Color.blue });
            return surface;
        }

        [Test]
        public void DirectSurfaceSnapshotMatchesUnityExportAndInvalidatesGeometryButCopiesEveryMask()
        {
            using var surface = Triangle();
            var unityMesh = new Mesh();
            try
            {
                surface.UpdateMeshFromDLL(unityMesh);
                var first = new SurfaceCapture(surface);
                Assert.That(first.Data.Vertices, Is.EqualTo(unityMesh.vertices));
                Assert.That(first.Data.Triangles, Is.EqualTo(unityMesh.triangles));
                Assert.That(first.Data.Normals, Is.EqualTo(unityMesh.normals));
                Assert.That(first.Data.UV, Is.EqualTo(unityMesh.uv));
                Assert.That(first.Data.Colors, Is.EqualTo(unityMesh.colors));
                surface.UpdateVisibilityMask(new[] { 0 }).Dispose();
                var masked = new SurfaceCapture(surface);
                Assert.That(masked.Data, Is.SameAs(first.Data));
                Assert.That(masked.Mask, Is.EqualTo(new[] { 0 }));
                Assert.That(first.Mask, Is.EqualTo(new[] { 1 }));
                surface.ComputeNormals();
                var normals = new SurfaceCapture(surface);
                Assert.That(normals.Data, Is.Not.SameAs(first.Data));
                surface.FlipTriangles();
                var flipped = new SurfaceCapture(surface);
                Assert.That(flipped.Data, Is.Not.SameAs(normals.Data));
                using var other = Triangle();
                surface.SwapDLLHandle(other);
                Assert.That(new SurfaceCapture(surface).Data, Is.Not.SameAs(flipped.Data));
                Assert.That(first.Data.Triangles, Is.EqualTo(unityMesh.triangles), "Cached snapshot must survive source mutations.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unityMesh);
            }
        }

        [Test]
        public void ReceivedCacheEvictsWithoutInvalidatingReadersAndCannotRepopulateAfterClear()
        {
            var cache = new ReceivedSurfaceCache(4);
            long generation = cache.Generation;
            using var retained = cache.Open("a", new MemoryStream(new byte[] { 1, 2, 3, 4 }), default, generation);
            using (var hit = cache.Open("a", new ReadForbiddenStream(4), default, generation))
            {
                Assert.That(hit.ReadByte(), Is.EqualTo(1));
                Assert.Throws<NotSupportedException>(() => hit.WriteByte(9));
            }

            using (cache.Open("b", new MemoryStream(new byte[] { 5, 6, 7, 8 }), default, generation))
            {
            }

            Assert.That(retained.ReadByte(), Is.EqualTo(1));
            using (var miss = cache.Open("a", new MemoryStream(new byte[] { 9, 9, 9, 9 }), default, generation)) Assert.That(miss.ReadByte(), Is.EqualTo(9));
            cache.Clear();
            using (cache.Open("late", new MemoryStream(new byte[] { 1 }), default, generation))
            {
            }

            using (var fresh = cache.Open("late", new MemoryStream(new byte[] { 2 }), default, cache.Generation)) Assert.That(fresh.ReadByte(), Is.EqualTo(2));
        }

        [Test]
        public void SurfaceCacheCreatesIndependentNativeHandlesAndDoesNotHideCorruptIncomingPack()
        {
            string root = Path.Combine(Path.GetTempPath(), "hibop-surface-cache-" + Guid.NewGuid().ToString("N"));
            try
            {
                using var source = new SceneArchive(Path.Combine(root, "source"));
                using var surface = Triangle();
                var payload = PreparedSceneArchiveTests.Fixture(source);
                string name = source.AddSurface(surface);
                payload.Meshes[0].SimplifiedBoth = name;
                string file = Path.Combine(root, "scene.hbscene");
                source.Write(payload, file);
                var cache = new ReceivedSurfaceCache();
                using var target = new SceneArchive(Path.Combine(root, "target"), true, source.Globals) { SurfaceCache = cache };
                target.Read(file);
                using var first = target.ReadSurface(name);
                using var second = target.ReadSurface(name);
                first.UpdateVisibilityMask(new[] { 0 }).Dispose();
                Assert.That(second.VisibilityMask, Is.EqualTo(new[] { 1 }));
                Assert.That(first.VisibilityMask, Is.EqualTo(new[] { 0 }));
                using (var zip = ZipFile.Open(file, ZipArchiveMode.Update))
                {
                    var entry = zip.GetEntry(BufferPack.DataName);
                    byte[] bytes;
                    using (var memory = new MemoryStream())
                    {
                        using (var input = entry.Open()) input.CopyTo(memory);
                        bytes = memory.ToArray();
                    }

                    bytes[bytes.Length - 1] ^= 1;
                    entry.Delete();
                    using var output = zip.CreateEntry(BufferPack.DataName).Open();
                    output.Write(bytes, 0, bytes.Length);
                }

                using var corrupt = new SceneArchive(Path.Combine(root, "corrupt"), true, source.Globals) { SurfaceCache = cache };
                Assert.Throws<InvalidDataException>(() => corrupt.Read(file));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private sealed class ReadForbiddenStream : MemoryStream
        {
            internal ReadForbiddenStream(int length) : base(new byte[length])
            {
            }

            public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("A hit should not read its source again.");
        }
    }
}
